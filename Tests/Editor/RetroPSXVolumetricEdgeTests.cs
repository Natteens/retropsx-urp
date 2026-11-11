using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RetroPSX.Tests
{
    public sealed class RetroPSXVolumetricEdgeTests
    {
        const int Width = 32, Height = 16, Divisor = 4;

        [Test]
        public void BackgroundVolumeDoesNotBleedOntoForegroundSilhouette([Values(false, true)] bool pixelated)
        {
            static bool Foreground(int x) => x >= 11 && x <= 20;
            Color[] pixels = Render(pixelated,
                (x, y) => Foreground(x) ? 1f / 6f : 1f / 100f,
                (x, y) => Foreground(x) ? new Color(0.04f, 0.04f, 0.04f, 0.8f) : new Color(0.7f, 0.7f, 0.7f, 0.2f),
                x => Foreground(x) ? 0.65f : 0f);
            for (int x = 11; x <= 20; x++)
            {
                Color pixel = pixels[Height / 2 * Width + x];
                Assert.That(pixel.r, Is.EqualTo(0.12f).Within(0.005f), $"Incorrect foreground scattering at x={x}.");
                Assert.That(pixel.a, Is.EqualTo(0.65f).Within(0.005f), $"Coverage changed at x={x}.");
            }
        }

        // Ground seen at a grazing angle changes depth far more than any fixed relative tolerance per
        // volume texel. It must keep its own scattering instead of falling back to an unfogged band.
        [Test]
        public void GrazingGroundKeepsItsScattering([Values(false, true)] bool pixelated)
        {
            Color[] pixels = Render(pixelated, (x, y) => 0.2f - 0.01f * y, (x, y) => new Color(0.3f, 0.3f, 0.3f, 0.5f), x => 1f);
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                Assert.That(pixels[y * Width + x].r, Is.EqualTo(0.35f).Within(0.005f), $"Missing scattering at ({x}, {y}).");
        }

        // A surface thinner than a volume texel has no sample of its own. It receives the nearest
        // sample's medium rescaled to its own distance, never that sample's full scattering.
        [Test]
        public void ThinSurfaceRescalesNearestMedium([Values(false, true)] bool pixelated)
        {
            Color[] pixels = Render(pixelated, (x, y) => x == 7 ? 1f / 20f : 1f / 100f, (x, y) => new Color(0.7f, 0.7f, 0.7f, 0.2f), x => 1f);
            float transmittance = Mathf.Pow(0.2f, 0.2f);
            float scattering = 0.7f * (1f - transmittance) / 0.8f;
            for (int y = 0; y < Height; y++)
                Assert.That(pixels[y * Width + 7].r, Is.EqualTo(0.1f * transmittance + scattering).Within(0.005f), $"Incorrect thin surface medium at y={y}.");
        }

        static Color[] Render(bool pixelated, Func<int, int, float> inverseDepth, Func<int, int, Color> volumeAt, Func<int, float> alpha)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
                Assert.Ignore("This regression requires GPU rendering.");
            var shader = Shader.Find("Hidden/RetroPSX/VolumetricComposite");
            Assert.That(shader, Is.Not.Null);

            var material = new Material(shader);
            var color = new Texture2D(Width, Height, TextureFormat.RGBAFloat, false, true) { filterMode = FilterMode.Point };
            var depth = new Texture2D(Width, Height, TextureFormat.RGBAFloat, false, true) { filterMode = FilterMode.Point };
            var volume = new Texture2D(Width / Divisor, Height / Divisor, TextureFormat.RGBAFloat, false, true) { filterMode = FilterMode.Point };
            var output = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var readback = new Texture2D(Width, Height, TextureFormat.RGBAFloat, false, true);
            var previousTarget = RenderTexture.active;
            var previousDepth = Shader.GetGlobalTexture("_CameraDepthTexture");
            var previousZ = Shader.GetGlobalVector("_ZBufferParams");
            var previousScreen = Shader.GetGlobalVector("_ScreenSize");
            var previousScale = Shader.GetGlobalVector("_RTHandleScale");
            var previousDepthSize = Shader.GetGlobalVector("_CameraDepthTexture_TexelSize");
            var previousDebug = Shader.GetGlobalFloat("_RetroPSXDebugMode");
            try
            {
                for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                {
                    color.SetPixel(x, y, new Color(0.1f, 0.1f, 0.1f, alpha(x)));
                    depth.SetPixel(x, y, new Color(inverseDepth(x, y), 0, 0, 1));
                }
                // Each volume texel is marched against depth pixel (texel + 0.5) * Divisor - 1.
                for (int y = 0; y < Height / Divisor; y++)
                for (int x = 0; x < Width / Divisor; x++)
                    volume.SetPixel(x, y, volumeAt(x * Divisor + Divisor / 2 - 1, y * Divisor + Divisor / 2 - 1));
                color.Apply(); depth.Apply(); volume.Apply(); output.Create();
                material.SetTexture("_BlitTexture", color);
                material.SetVector("_BlitScaleBias", new Vector4(1, 1, 0, 0));
                material.SetTexture("_RetroVolumeTexture", volume);
                material.SetVector("_RetroVolumeTexelSize", new Vector4((float)Divisor / Width, (float)Divisor / Height, Width / Divisor, Height / Divisor));
                material.SetVector("_RetroVolumeParams3", new Vector4(0.05f, 4, 0, pixelated ? 1 : 0));
                material.SetVector("_RetroFinalColorParams", Vector4.zero);
                material.SetFloat("_RetroPreserveAlpha", 1);
                Shader.SetGlobalTexture("_CameraDepthTexture", depth);
                Shader.SetGlobalVector("_ZBufferParams", new Vector4(0, 0, 1, 0));
                Shader.SetGlobalVector("_ScreenSize", new Vector4(Width, Height, 1f / Width, 1f / Height));
                Shader.SetGlobalVector("_RTHandleScale", Vector4.one);
                Shader.SetGlobalVector("_CameraDepthTexture_TexelSize", new Vector4(1f / Width, 1f / Height, Width, Height));
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
                Shader.SetGlobalVector("_CameraDepthTexture_TexelSize", previousDepthSize);
                Shader.SetGlobalFloat("_RetroPSXDebugMode", previousDebug);
                Object.DestroyImmediate(material); Object.DestroyImmediate(color); Object.DestroyImmediate(depth);
                Object.DestroyImmediate(volume); Object.DestroyImmediate(output); Object.DestroyImmediate(readback);
            }
        }
    }
}
