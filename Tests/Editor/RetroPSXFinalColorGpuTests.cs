using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RetroPSX.Tests
{
    // Renders the real Resolve and Volumetric Composite shaders and reads the result back.
    public sealed class RetroPSXFinalColorGpuTests
    {
        const int Width = 64, Height = 8;
        const string Resolve = "Hidden/RetroPSX/Resolve", Composite = "Hidden/RetroPSX/VolumetricComposite";

        [TestCase(Resolve)]
        [TestCase(Composite)]
        public void DisabledArtisticStagesLeaveTheImageUntouched(string shader)
        {
            Color[] input = Gradient();
            Color[] output = Render(shader, input, _ => { });
            for (int index = 0; index < input.Length; index++)
                AssertColor(output[index], input[index], 1e-4f, index);
        }

        [TestCase(Resolve)]
        [TestCase(Composite)]
        public void ColorModeOffDisablesQuantizeFinalImageOnBothPaths(string shader)
        {
            Color[] input = Gradient();
            Color[] output = Render(shader, input, m =>
            {
                m.SetVector("_RetroPSXColorBits", new Vector4(8, 8, 8, 0));
                m.SetVector("_RetroFinalColorParams", new Vector4(1, (float)RetroDitherMode.PSX, 0.5f, 0));
            });
            for (int index = 0; index < input.Length; index++)
                AssertColor(output[index], input[index], 1e-4f, index);
        }

        [TestCase(Resolve)]
        [TestCase(Composite)]
        public void LegacyFinalPrecisionMatchesTheOriginalFormula(string shader)
        {
            Color[] input = Gradient();
            Color[] output = Render(shader, input, m =>
            {
                m.SetVector("_RetroPSXColorBits", new Vector4(5, 6, 5, 1));
                m.SetVector("_RetroFinalColorParams", new Vector4(1, 0, 0.5f, 0));
            });
            for (int index = 0; index < input.Length; index++)
            {
                Color c = input[index];
                Color expected = new(Legacy(c.r, 5), Legacy(c.g, 6), Legacy(c.b, 5), c.a);
                AssertColor(output[index], expected, 1e-4f, index);
            }
        }

        [TestCase(Resolve, 5)]
        [TestCase(Composite, 5)]
        [TestCase(Resolve, 3)]
        public void RgbPosterizeOutputsExactLevels(string shader, int levels)
        {
            Color[] output = Render(shader, Gradient(), m => m.SetVector("_RetroPosterizeParams", new Vector4(1, levels, 1, 0)));
            var seen = new HashSet<int>();
            foreach (Color color in output)
            {
                float encoded = RetroPSXColorMath.LinearToSrgb(color.r) * (levels - 1);
                Assert.That(encoded, Is.EqualTo(Mathf.Round(encoded)).Within(2e-3f));
                seen.Add(Mathf.RoundToInt(encoded));
            }
            Assert.That(seen.Count, Is.EqualTo(levels));
        }

        [Test]
        public void ShaderMatchesCpuMirrorWithoutDither()
        {
            RetroPaletteProfile palette = RetroPSXArtisticColorTests.Palette("101413", "242D2B", "414C43", "68745D", "A5A184", "D6CBB0");
            RetroColorProfile profile = RetroPSXArtisticColorTests.Profile(p =>
            {
                p.FindProperty("posterizeMode").enumValueIndex = (int)RetroPosterizeMode.Luminance;
                p.FindProperty("posterizeLevels").intValue = 6;
                p.FindProperty("paletteEnabled").boolValue = true;
                p.FindProperty("palette").objectReferenceValue = palette;
            });
            try
            {
                Color[] input = Random();
                Color[] output = Render(Resolve, input, m => Configure(m, profile));
                int mismatches = 0;
                for (int index = 0; index < input.Length; index++)
                {
                    Vector3 expected = RetroPSXColorMath.ApplyArtistic(new Vector3(input[index].r, input[index].g, input[index].b), profile, 0, 0);
                    if (Vector3.Distance(expected, new Vector3(output[index].r, output[index].g, output[index].b)) > 1e-4f)
                        mismatches++;
                }
                Assert.That(mismatches, Is.LessThanOrEqualTo(input.Length / 100), "GPU and CPU mirror disagree beyond rounding ties.");
            }
            finally { Object.DestroyImmediate(profile); Object.DestroyImmediate(palette); }
        }

        [TestCase(Resolve, RetroDitherMode.Bayer4x4)]
        [TestCase(Composite, RetroDitherMode.Bayer4x4)]
        [TestCase(Resolve, RetroDitherMode.Bayer2x2)]
        [TestCase(Resolve, RetroDitherMode.PSX)]
        public void PaletteDitherAtFullStrengthPreservesTransparentTexelsAndMapsVisibleColors(string shader, RetroDitherMode dither)
        {
            RetroPaletteProfile palette = RetroPSXArtisticColorTests.Palette("101413", "242D2B", "414C43", "68745D", "A5A184", "D6CBB0");
            RetroColorProfile profile = RetroPSXArtisticColorTests.Profile(p =>
            {
                p.FindProperty("posterizeMode").enumValueIndex = (int)RetroPosterizeMode.Luminance;
                p.FindProperty("posterizeLevels").intValue = 6;
                p.FindProperty("paletteEnabled").boolValue = true;
                p.FindProperty("palette").objectReferenceValue = palette;
                p.FindProperty("paletteDither").enumValueIndex = (int)dither;
                p.FindProperty("paletteDitherStrength").floatValue = 0.65f;
            });
            try
            {
                palette.TryGetShaderData(out int count, out Vector4[] linear, out _);
                Color[] input = Random();
                input[3].a = 0f;
                input[4].a = 0.5f;
                Color[] output = Render(shader, input, m =>
                {
                    m.SetVector("_RetroPSXColorBits", new Vector4(5, 5, 5, 1));
                    m.SetVector("_RetroFinalColorParams", new Vector4(1, 0, 0, 0));
                    Configure(m, profile);
                });
                var used = new HashSet<int>();
                for (int index = 0; index < output.Length; index++)
                {
                    Color original = input[index];
                    if (original.a == 0f)
                    {
                        var expected = new Color(Legacy(original.r, 5), Legacy(original.g, 5), Legacy(original.b, 5), original.a);
                        AssertColor(output[index], expected, 1e-4f, index);
                        continue;
                    }
                    int match = Array.FindIndex(linear, 0, count, c => Mathf.Abs(c.x - output[index].r) < 1e-5f && Mathf.Abs(c.y - output[index].g) < 1e-5f && Mathf.Abs(c.z - output[index].b) < 1e-5f);
                    Assert.That(match, Is.GreaterThanOrEqualTo(0), $"Off-palette pixel {output[index]} at {index}.");
                    used.Add(match);
                    Assert.That(output[index].a, Is.EqualTo(input[index].a).Within(1e-5f), "Alpha must follow the pipeline contract.");
                }
                Assert.That(used.Count, Is.GreaterThan(3));
            }
            finally { Object.DestroyImmediate(profile); Object.DestroyImmediate(palette); }
        }

        [Test]
        public void PaletteDitherRatioFollowsMixFactorOnFlatArea()
        {
            RetroPaletteProfile palette = RetroPSXArtisticColorTests.Palette("000000", "808080", "FFFFFF");
            RetroColorProfile profile = RetroPSXArtisticColorTests.Profile(p =>
            {
                p.FindProperty("paletteEnabled").boolValue = true;
                p.FindProperty("palette").objectReferenceValue = palette;
                p.FindProperty("paletteDither").enumValueIndex = (int)RetroDitherMode.Bayer4x4;
            });
            try
            {
                float value = RetroPSXColorMath.SrgbToLinear(0.62f);
                var input = new Color[Width * Height];
                for (int index = 0; index < input.Length; index++)
                    input[index] = new Color(value, value, value, 1f);
                Color[] output = Render(Resolve, input, m => Configure(m, profile));
                int white = 0;
                foreach (Color color in output)
                    if (color.r > 0.99f) white++;
                palette.TryGetShaderData(out int count, out _, out Vector4[] oklab);
                RetroPSXColorMath.FindPalettePair(RetroPSXColorMath.LinearToOklab(Vector3.one * value), oklab, count, true, out _, out _, out float t);
                Assert.That(white, Is.EqualTo(Mathf.RoundToInt(t * 16f) * input.Length / 16).Within(input.Length / 16));
            }
            finally { Object.DestroyImmediate(profile); Object.DestroyImmediate(palette); }
        }

        [Test]
        public void ResolveAndCompositeProduceIdenticalArtisticOutput()
        {
            RetroPaletteProfile palette = RetroPSXArtisticColorTests.Palette("0B0D19", "1C2338", "343E60", "656F8F", "999BB8", "D9D6E4");
            RetroColorProfile profile = RetroPSXArtisticColorTests.Profile(p =>
            {
                p.FindProperty("posterizeMode").enumValueIndex = (int)RetroPosterizeMode.RGB;
                p.FindProperty("posterizeLevels").intValue = 7;
                p.FindProperty("posterizeStrength").floatValue = 0.5f;
                p.FindProperty("paletteEnabled").boolValue = true;
                p.FindProperty("palette").objectReferenceValue = palette;
                p.FindProperty("paletteStrength").floatValue = 0.8f;
                p.FindProperty("paletteDither").enumValueIndex = (int)RetroDitherMode.Bayer2x2;
            });
            try
            {
                Color[] input = Random();
                Color[] resolve = Render(Resolve, input, m => Configure(m, profile));
                Color[] composite = Render(Composite, input, m => Configure(m, profile));
                for (int index = 0; index < input.Length; index++)
                    AssertColor(composite[index], resolve[index], 1e-5f, index);
            }
            finally { Object.DestroyImmediate(profile); Object.DestroyImmediate(palette); }
        }

        static void Configure(Material material, RetroColorProfile profile)
        {
            material.SetVector("_RetroPosterizeParams", profile.PosterizeActive
                ? new Vector4((float)profile.PosterizeMode, profile.PosterizeLevels, profile.PosterizeStrength, profile.PosterizeDitherStrength)
                : Vector4.zero);
            material.SetVector("_RetroArtisticDither", new Vector4((float)profile.PosterizeDither, 0, 0, 0));
            if (profile.PaletteActive && profile.Palette.TryGetShaderData(out int count, out Vector4[] linear, out Vector4[] oklab))
            {
                material.SetVectorArray("_RetroPaletteLinear", linear);
                material.SetVectorArray("_RetroPaletteOklab", oklab);
                material.SetVector("_RetroPaletteParams", new Vector4(count, profile.PaletteStrength, (float)profile.PaletteDither, profile.PaletteDitherStrength));
            }
        }

        static float Legacy(float linear, int bits)
        {
            float levels = Mathf.Pow(2f, bits) - 1f;
            return RetroPSXColorMath.SrgbToLinear(Mathf.Round(RetroPSXColorMath.LinearToSrgb(Mathf.Clamp01(linear)) * levels) / levels);
        }

        static Color[] Gradient()
        {
            var pixels = new Color[Width * Height];
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                float value = RetroPSXColorMath.SrgbToLinear((x + y * Width) / (float)(Width * Height - 1));
                pixels[y * Width + x] = new Color(value, value, value, 1f);
            }
            return pixels;
        }

        static Color[] Random()
        {
            var random = new System.Random(1234);
            var pixels = new Color[Width * Height];
            for (int index = 0; index < pixels.Length; index++)
                pixels[index] = new Color((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble() * 1.4f, 1f);
            return pixels;
        }

        static void AssertColor(Color actual, Color expected, float tolerance, int index)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance), $"r at {index}");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance), $"g at {index}");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance), $"b at {index}");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(tolerance), $"a at {index}");
        }

        static Color[] Render(string shaderName, Color[] pixels, Action<Material> configure)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("Requires GPU rendering.");
            var shader = Shader.Find(shaderName);
            Assert.That(shader, Is.Not.Null);
            var material = new Material(shader);
            var color = new Texture2D(Width, Height, TextureFormat.RGBAFloat, false, true) { filterMode = FilterMode.Point };
            var depth = new Texture2D(Width, Height, TextureFormat.RGBAFloat, false, true) { filterMode = FilterMode.Point };
            var volume = new Texture2D(Width / 4, Height / 4, TextureFormat.RGBAFloat, false, true) { filterMode = FilterMode.Point };
            var output = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Width, Height, TextureFormat.RGBAFloat, false, true);
            var previousTarget = RenderTexture.active;
            var previousDepth = Shader.GetGlobalTexture("_CameraDepthTexture");
            var previousZ = Shader.GetGlobalVector("_ZBufferParams");
            var previousScreen = Shader.GetGlobalVector("_ScreenSize");
            var previousScale = Shader.GetGlobalVector("_RTHandleScale");
            var previousDebug = Shader.GetGlobalFloat("_RetroPSXDebugMode");
            var previousBits = Shader.GetGlobalVector("_RetroPSXColorBits");
            try
            {
                color.SetPixels(pixels);
                var depthPixels = new Color[Width * Height];
                for (int index = 0; index < depthPixels.Length; index++)
                    depthPixels[index] = new Color(0.01f, 0, 0, 1);
                depth.SetPixels(depthPixels);
                var volumePixels = new Color[volume.width * volume.height];
                for (int index = 0; index < volumePixels.Length; index++)
                    volumePixels[index] = new Color(0, 0, 0, 1);
                volume.SetPixels(volumePixels);
                color.Apply(); depth.Apply(); volume.Apply(); output.Create();
                material.SetTexture("_BlitTexture", color);
                material.SetVector("_BlitScaleBias", new Vector4(1, 1, 0, 0));
                material.SetTexture("_RetroVolumeTexture", volume);
                material.SetVector("_RetroVolumeTexelSize", new Vector4(4f / Width, 4f / Height, Width / 4, Height / 4));
                material.SetVector("_RetroVolumeParams3", new Vector4(0.05f, 4, 0, 1));
                material.SetVector("_RetroPSXInternalSize", new Vector4(Width, Height, 1f / Width, 1f / Height));
                material.SetVector("_RetroFinalColorParams", Vector4.zero);
                material.SetVector("_RetroPSXColorBits", new Vector4(8, 8, 8, 0));
                material.SetTexture("_RetroCustomDither", Texture2D.grayTexture);
                material.SetTexture("_RetroBlueNoise", Texture2D.grayTexture);
                material.SetFloat("_RetroPreserveAlpha", 1);
                configure(material);
                // The composite reads color bits from the global set by the setup pass.
                Shader.SetGlobalVector("_RetroPSXColorBits", material.GetVector("_RetroPSXColorBits"));
                Shader.SetGlobalTexture("_CameraDepthTexture", depth);
                Shader.SetGlobalVector("_ZBufferParams", new Vector4(0, 0, 1, 0));
                Shader.SetGlobalVector("_ScreenSize", new Vector4(Width, Height, 1f / Width, 1f / Height));
                Shader.SetGlobalVector("_RTHandleScale", Vector4.one);
                Shader.SetGlobalFloat("_RetroPSXDebugMode", 0);
                using (var cmd = new CommandBuffer())
                {
                    cmd.SetRenderTarget(output);
                    cmd.DrawProcedural(Matrix4x4.identity, material, 0, MeshTopology.Triangles, 3);
                    Graphics.ExecuteCommandBuffer(cmd);
                }
                RenderTexture.active = output;
                readback.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                readback.Apply();
                return readback.GetPixels();
            }
            finally
            {
                RenderTexture.active = previousTarget;
                Shader.SetGlobalTexture("_CameraDepthTexture", previousDepth);
                Shader.SetGlobalVector("_ZBufferParams", previousZ);
                Shader.SetGlobalVector("_ScreenSize", previousScreen);
                Shader.SetGlobalVector("_RTHandleScale", previousScale);
                Shader.SetGlobalFloat("_RetroPSXDebugMode", previousDebug);
                Shader.SetGlobalVector("_RetroPSXColorBits", previousBits);
                Object.DestroyImmediate(material); Object.DestroyImmediate(color); Object.DestroyImmediate(depth);
                Object.DestroyImmediate(volume); Object.DestroyImmediate(output); Object.DestroyImmediate(readback);
            }
        }
    }
}
