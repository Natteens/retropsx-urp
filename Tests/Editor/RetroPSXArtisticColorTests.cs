using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RetroPSX.Tests
{
    public sealed class RetroPSXArtisticColorTests
    {
        static float Encode(float linear) => RetroPSXColorMath.LinearToSrgb(linear);

        [TestCase(2)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(64)]
        public void RgbPosterizeProducesExactLevelCount(int levels)
        {
            var seen = new HashSet<int>();
            for (int i = 0; i <= 1024; i++)
            {
                float value = RetroPSXColorMath.SrgbToLinear(i / 1024f);
                Vector3 result = RetroPSXColorMath.PosterizeRgb(new Vector3(value, value, value), levels);
                seen.Add(Mathf.RoundToInt(Encode(result.x) * (levels - 1) * 1000f));
                Assert.That(Encode(result.x) * (levels - 1), Is.EqualTo(Mathf.Round(Encode(result.x) * (levels - 1))).Within(1e-3f));
            }
            Assert.That(seen.Count, Is.EqualTo(levels));
            Assert.That(RetroPSXColorMath.PosterizeRgb(Vector3.zero, levels), Is.EqualTo(Vector3.zero));
            Assert.That(RetroPSXColorMath.PosterizeRgb(Vector3.one, levels).x, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void PosterizeHandlesInvalidInputWithoutNaN()
        {
            foreach (float value in new[] { float.NaN, float.PositiveInfinity, -1f, 1e6f, 0f })
            foreach (int levels in new[] { int.MinValue, 0, 1, 2, 64, 1000 })
            {
                float result = RetroPSXColorMath.PosterizeStep(value, levels, 0.5f, 0f);
                Assert.That(float.IsNaN(result) || float.IsInfinity(result), Is.False, $"{value} @ {levels}");
                Assert.That(result, Is.InRange(0f, 1f));
            }
            Vector3 fitted = RetroPSXColorMath.FitToDisplayRange(new Vector3(float.PositiveInfinity, 2f, -1f));
            Assert.That(float.IsNaN(fitted.x) || float.IsNaN(fitted.y) || float.IsNaN(fitted.z), Is.False);
        }

        [Test]
        public void PosterizeDitherStrengthZeroRoundsAndFullStrengthUsesThreshold()
        {
            Assert.That(RetroPSXColorMath.PosterizeStep(0.3f, 2, 0.1f, 0f), Is.EqualTo(0f));
            Assert.That(RetroPSXColorMath.PosterizeStep(0.3f, 2, 0.1f, 1f), Is.EqualTo(1f));
            Assert.That(RetroPSXColorMath.PosterizeStep(0.3f, 2, 0.9f, 1f), Is.EqualTo(0f));
            // A value already on a level never moves, whatever the threshold.
            for (int x = 0; x < 4; x++)
            for (int y = 0; y < 4; y++)
                Assert.That(RetroPSXColorMath.PosterizeStep(0.5f, 3, RetroPSXColorMath.DitherThreshold(RetroDitherMode.Bayer4x4, x, y), 1f), Is.EqualTo(0.5f));
        }

        [Test]
        public void DitherThresholdsAreNormalizedAndCoverTheirMatrix()
        {
            foreach (RetroDitherMode mode in new[] { RetroDitherMode.PSX, RetroDitherMode.Bayer2x2, RetroDitherMode.Bayer4x4 })
            {
                var values = new HashSet<float>();
                for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                {
                    float threshold = RetroPSXColorMath.DitherThreshold(mode, x, y);
                    Assert.That(threshold, Is.GreaterThan(0f).And.LessThan(1f));
                    Assert.That(RetroPSXColorMath.DitherThreshold(mode, x + 8, y + 12), Is.EqualTo(threshold), "Pattern must tile.");
                    values.Add(threshold);
                }
                int expected = mode switch { RetroDitherMode.Bayer2x2 => 4, RetroDitherMode.PSX => 8, _ => 16 };
                Assert.That(values.Count, Is.EqualTo(expected), mode.ToString());
            }
        }

        [Test]
        public void LuminancePosterizeMatchesRgbOnAchromaticInput()
        {
            for (int i = 0; i <= 255; i++)
            {
                float value = RetroPSXColorMath.SrgbToLinear(i / 255f);
                var gray = new Vector3(value, value, value);
                Vector3 luminance = RetroPSXColorMath.PosterizeLuminance(gray, 6);
                Vector3 rgb = RetroPSXColorMath.PosterizeRgb(gray, 6);
                Assert.That(Vector3.Distance(luminance, rgb), Is.LessThan(1e-4f), $"gray {i}");
                Assert.That(luminance.x, Is.EqualTo(luminance.y).Within(1e-6f));
            }
            Assert.That(RetroPSXColorMath.PosterizeLuminance(Vector3.zero, 6), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void LuminancePosterizeKeepsChromaticityAndHitsTargetLuminance()
        {
            var colors = new[] { new Vector3(0.4f, 0.1f, 0.05f), new Vector3(0.05f, 0.2f, 0.4f), new Vector3(0.1f, 0.3f, 0.1f), new Vector3(0.25f, 0.2f, 0.3f) };
            foreach (Vector3 color in colors)
            {
                Vector3 result = RetroPSXColorMath.PosterizeLuminance(color, 6);
                float y = RetroPSXColorMath.Luminance(result);
                float level = Encode(y) * 5f;
                Assert.That(level, Is.EqualTo(Mathf.Round(level)).Within(2e-3f), $"{color} is not on a luminance level");
                if (Mathf.Max(result.x, Mathf.Max(result.y, result.z)) < 0.999f)
                {
                    Vector3 a = color / (color.x + color.y + color.z), b = result / (result.x + result.y + result.z);
                    Assert.That(Vector3.Distance(a, b), Is.LessThan(1e-4f), $"{color} changed chromaticity");
                }
            }
        }

        [Test]
        public void LuminancePosterizeDesaturatesOutOfGamutAndHdrInsteadOfClipping()
        {
            // Saturated blue pushed up a level would exceed 1 in blue: it is desaturated at constant luminance.
            var blue = new Vector3(0.02f, 0.02f, 0.9f);
            Vector3 result = RetroPSXColorMath.PosterizeLuminance(blue, 3);
            Assert.That(RetroPSXColorMath.Luminance(result), Is.EqualTo(RetroPSXColorMath.SrgbToLinear(0.5f)).Within(1e-4f));
            Assert.That(result.z, Is.LessThanOrEqualTo(1f).And.GreaterThan(result.x));
            Assert.That(result.x, Is.EqualTo(result.y).Within(1e-5f), "Desaturation must not rotate the hue.");
            Vector3 bright = RetroPSXColorMath.FitToDisplayRange(new Vector3(2f, 0.3f, 0.1f));
            Assert.That(RetroPSXColorMath.Luminance(bright), Is.LessThanOrEqualTo(1f + 1e-5f));
            Assert.That(bright.x, Is.GreaterThan(bright.y).And.GreaterThan(bright.z), "Hue order must survive the HDR fit.");
            Vector3 nearBlack = RetroPSXColorMath.PosterizeLuminance(new Vector3(1e-7f, 0f, 2e-7f), 6, 0.01f, 1f);
            Assert.That(nearBlack.x, Is.EqualTo(nearBlack.y).Within(1e-6f), "Near-black has no reliable hue and must stay gray.");
        }

        [Test]
        public void PaletteMappingKeepsExactColorsAndMapsDarkToDark()
        {
            RetroPaletteProfile palette = Palette("101413", "242D2B", "414C43", "68745D", "A5A184", "D6CBB0");
            try
            {
                Assert.That(palette.TryGetShaderData(out int count, out Vector4[] linear, out Vector4[] oklab), Is.True);
                for (int index = 0; index < count; index++)
                {
                    RetroPSXColorMath.FindPalettePair(RetroPSXColorMath.LinearToOklab(linear[index]), oklab, count, true, out int first, out _, out float t);
                    Assert.That(first, Is.EqualTo(index));
                    Assert.That(t, Is.EqualTo(0f), "An exact palette color must not dither.");
                }
                RetroPSXColorMath.FindPalettePair(RetroPSXColorMath.LinearToOklab(new Vector3(0.002f, 0.001f, 0.004f)), oklab, count, false, out int dark, out _, out _);
                Assert.That(dark, Is.EqualTo(0));
                RetroPSXColorMath.FindPalettePair(RetroPSXColorMath.LinearToOklab(new Vector3(0.9f, 0.85f, 0.8f)), oklab, count, false, out int light, out _, out _);
                Assert.That(light, Is.EqualTo(5));
            }
            finally { Object.DestroyImmediate(palette); }
        }

        [Test]
        public void PaletteDitherPicksNeighborPairAndStaysInPalette()
        {
            RetroPaletteProfile palette = Palette("000000", "808080", "FFFFFF");
            RetroColorProfile profile = Profile(p =>
            {
                p.FindProperty("paletteEnabled").boolValue = true;
                p.FindProperty("palette").objectReferenceValue = palette;
                p.FindProperty("paletteDither").enumValueIndex = (int)RetroDitherMode.Bayer4x4;
            });
            try
            {
                palette.TryGetShaderData(out int count, out Vector4[] linear, out Vector4[] oklab);
                float between = RetroPSXColorMath.SrgbToLinear(0.62f);
                RetroPSXColorMath.FindPalettePair(RetroPSXColorMath.LinearToOklab(Vector3.one * between), oklab, count, true, out int first, out int second, out float t);
                Assert.That(new[] { first, second }, Is.EquivalentTo(new[] { 1, 2 }), "Gray between mid and white must dither mid/white, not black/white.");
                Assert.That(t, Is.GreaterThan(0f).And.LessThan(1f));

                var outputs = new HashSet<Vector3>();
                int alternate = 0;
                for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                {
                    Vector3 result = RetroPSXColorMath.ApplyArtistic(Vector3.one * between, profile, x, y);
                    outputs.Add(result);
                    if (result == (Vector3)linear[second]) alternate++;
                    Assert.That(Array.Exists(linear, c => (Vector3)c == result), Is.True, $"Off-palette output {result}");
                }
                Assert.That(outputs.Count, Is.EqualTo(2));
                Assert.That(alternate, Is.EqualTo(Mathf.RoundToInt(t * 16f)).Within(1), "Alternate count must follow the mix factor.");

                SetProperty(profile, "paletteDitherStrength", 0f);
                for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    Assert.That(RetroPSXColorMath.ApplyArtistic(Vector3.one * between, profile, x, y), Is.EqualTo((Vector3)linear[first]));
            }
            finally { Object.DestroyImmediate(profile); Object.DestroyImmediate(palette); }
        }

        [Test]
        public void PaletteStrengthZeroBypassesAndPartialMixes()
        {
            RetroPaletteProfile palette = Palette("000000", "FFFFFF");
            RetroColorProfile profile = Profile(p =>
            {
                p.FindProperty("paletteEnabled").boolValue = true;
                p.FindProperty("palette").objectReferenceValue = palette;
                p.FindProperty("paletteStrength").floatValue = 0f;
            });
            try
            {
                var input = new Vector3(0.02f, 0.03f, 0.01f);
                Assert.That(profile.PaletteActive, Is.False);
                Assert.That(RetroPSXColorMath.ApplyArtistic(input, profile, 0, 0), Is.EqualTo(input));
                SetProperty(profile, "paletteStrength", 0.5f);
                Vector3 half = RetroPSXColorMath.ApplyArtistic(input, profile, 0, 0);
                Assert.That(half.x, Is.GreaterThan(0f).And.LessThan(input.x));
            }
            finally { Object.DestroyImmediate(profile); Object.DestroyImmediate(palette); }
        }

        [Test]
        public void PaletteValidityCoversEmptySingleDuplicatesAndLimit()
        {
            RetroPaletteProfile palette = ScriptableObject.CreateInstance<RetroPaletteProfile>();
            try
            {
                palette.SetColors(Array.Empty<Color>());
                Assert.That(palette.IsValid, Is.False);
                Assert.That(palette.TryGetShaderData(out int count, out _, out _), Is.False);
                Assert.That(count, Is.Zero);

                palette.SetColors(new[] { Color.red });
                Assert.That(palette.IsValid, Is.True);

                palette.SetColors(new[] { Color.red, Color.red, Color.blue });
                palette.TryGetShaderData(out count, out _, out Vector4[] oklab);
                RetroPSXColorMath.FindPalettePair(RetroPSXColorMath.LinearToOklab(new Vector3(1f, 0f, 0f)), oklab, count, true, out int first, out int second, out float t);
                Assert.That(first, Is.EqualTo(0), "Ties resolve to the lowest index.");
                Assert.That(t, Is.Zero);

                var tooMany = new Color[RetroPaletteProfile.MaxColors + 1];
                palette.SetColors(tooMany);
                Assert.That(palette.IsValid, Is.False);

                palette.SetColors(new[] { new Color(float.NaN, 2f, -1f, 0.2f) });
                Assert.That(palette[0], Is.EqualTo(new Color(0f, 1f, 0f, 1f)), "Invalid channels are sanitized and alpha is ignored.");
            }
            finally { Object.DestroyImmediate(palette); }
        }

        [Test]
        public void PaletteCacheRebuildsWhenColorsChange()
        {
            RetroPaletteProfile palette = Palette("000000", "FFFFFF");
            try
            {
                palette.TryGetShaderData(out _, out Vector4[] linear, out _);
                Assert.That(linear[1].x, Is.EqualTo(1f).Within(1e-5f));
                var serialized = new SerializedObject(palette);
                serialized.FindProperty("colors").GetArrayElementAtIndex(1).colorValue = Color.red;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                palette.TryGetShaderData(out _, out Vector4[] rebuilt, out _);
                Assert.That(rebuilt[1].y, Is.EqualTo(0f).Within(1e-5f));
            }
            finally { Object.DestroyImmediate(palette); }
        }

        [Test]
        public void NewEffectsAreOffByDefaultAndForPreUpdateProfiles()
        {
            RetroColorProfile profile = ScriptableObject.CreateInstance<RetroColorProfile>();
            try
            {
                Assert.That(profile.PosterizeActive, Is.False);
                Assert.That(profile.PaletteActive, Is.False);
                // A profile serialized before this update has none of the new fields.
                JsonUtility.FromJsonOverwrite("{\"mode\":2,\"quantizeFinalImage\":true,\"finalImageDither\":1}", profile);
                Assert.That(profile.PosterizeMode, Is.EqualTo(RetroPosterizeMode.Off));
                Assert.That(profile.PaletteEnabled, Is.False);
                Assert.That(profile.FinalImagePrecisionActive, Is.True);
                Assert.That(RetroPSXColorMath.ApplyArtistic(new Vector3(0.3f, 0.2f, 0.1f), profile, 1, 2), Is.EqualTo(new Vector3(0.3f, 0.2f, 0.1f)));
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [Test]
        public void SerializedEnumValuesArePreserved()
        {
            Assert.That((int)RetroColorMode.Custom, Is.EqualTo(5));
            Assert.That((int)RetroDitherMode.BlueNoise, Is.EqualTo(5));
            Assert.That((int)RetroDebugMode.VolumetricLightVisibility, Is.EqualTo(10));
            Assert.That((int)RetroDebugMode.ArtisticColorChange, Is.EqualTo(11));
            Assert.That((int)RetroDebugMode.ArtisticDitherThreshold, Is.EqualTo(12));
            Assert.That((int)RetroPosterizeMode.Luminance, Is.EqualTo(2));
        }

        [Test]
        public void DefaultBlueNoiseIsUniformLinearPointData()
        {
            Texture2D noise = RetroColorProfile.DefaultBlueNoise;
            Assert.That(noise, Is.Not.Null, "The package must ship a default blue-noise texture.");
            Assert.That(noise.width, Is.EqualTo(64));
            Assert.That(noise.height, Is.EqualTo(64));
            Assert.That(noise.isDataSRGB, Is.False);
            Assert.That(noise.filterMode, Is.EqualTo(FilterMode.Point));
            Assert.That(noise.mipmapCount, Is.EqualTo(1));
            Assert.That(noise.isReadable, Is.True);
            Color32[] texels = noise.GetPixels32();
            var histogram = new int[256];
            foreach (Color32 texel in texels)
                histogram[texel.r]++;
            foreach (int count in histogram)
                Assert.That(count, Is.EqualTo(16), "Thresholds must be uniformly distributed (uncompressed).");

            // Blue noise keeps the darkest quarter apart; white noise would put ~25% of them next to each other.
            int ones = 0, adjacent = 0;
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                if (texels[y * 64 + x].r >= 64)
                    continue;
                ones++;
                if (texels[y * 64 + (x + 1) % 64].r < 64)
                    adjacent++;
            }
            Assert.That(adjacent, Is.LessThan(ones * 0.15f));
        }

        [Test]
        public void BlueNoiseWithoutTextureUsesTheDefaultAndCustomWithoutTextureIsSkipped()
        {
            RetroColorProfile profile = Profile(p =>
            {
                p.FindProperty("posterizeMode").enumValueIndex = (int)RetroPosterizeMode.RGB;
                p.FindProperty("posterizeLevels").intValue = 2;
                p.FindProperty("posterizeDither").enumValueIndex = (int)RetroDitherMode.Custom;
            });
            try
            {
                Assert.That(profile.EffectiveBlueNoise, Is.SameAs(RetroColorProfile.DefaultBlueNoise));
                Assert.That(profile.EffectiveDither(RetroDitherMode.BlueNoise), Is.EqualTo(RetroDitherMode.BlueNoise));
                Assert.That(profile.EffectiveDither(RetroDitherMode.Custom), Is.EqualTo(RetroDitherMode.Off));
                Assert.That(profile.PosterizeDitherActive, Is.False, "Custom without a texture must not dither.");
                Assert.That(profile.EffectiveDither(RetroDitherMode.Bayer4x4), Is.EqualTo(RetroDitherMode.Bayer4x4));

                // Blue noise really dithers: a flat value between two levels gets both at the expected ratio.
                var serialized = new SerializedObject(profile);
                serialized.FindProperty("posterizeDither").enumValueIndex = (int)RetroDitherMode.BlueNoise;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                float value = RetroPSXColorMath.SrgbToLinear(0.3f);
                int high = 0;
                for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                    if (RetroPSXColorMath.ApplyArtistic(Vector3.one * value, profile, x, y).x > 0.5f)
                        high++;
                Assert.That(high / 4096f, Is.EqualTo(0.3f).Within(0.01f));
            }
            finally { Object.DestroyImmediate(profile); }
        }

        [Test]
        public void PaletteDitherPatternIsContinuousWhereTheNearestColorFlips()
        {
            RetroPaletteProfile palette = Palette("000000", "FFFFFF");
            RetroColorProfile profile = Profile(p =>
            {
                p.FindProperty("paletteEnabled").boolValue = true;
                p.FindProperty("palette").objectReferenceValue = palette;
                p.FindProperty("paletteDither").enumValueIndex = (int)RetroDitherMode.Bayer4x4;
            });
            try
            {
                palette.TryGetShaderData(out int count, out _, out Vector4[] oklab);
                float low = 0f, high = 1f;
                for (int step = 0; step < 40; step++)
                {
                    float mid = (low + high) * 0.5f;
                    RetroPSXColorMath.FindPalettePair(RetroPSXColorMath.LinearToOklab(Vector3.one * mid), oklab, count, false, out int nearest, out _, out _);
                    if (nearest == 0) low = mid; else high = mid;
                }
                Vector3 below = Vector3.one * (low * 0.999f), above = Vector3.one * (high * 1.001f);
                for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    Assert.That(RetroPSXColorMath.ApplyArtistic(above, profile, x, y), Is.EqualTo(RetroPSXColorMath.ApplyArtistic(below, profile, x, y)),
                        $"Pattern inverts at ({x},{y}) where the nearest color flips.");
            }
            finally { Object.DestroyImmediate(profile); Object.DestroyImmediate(palette); }
        }

        [TestCase(RetroDitherMode.Bayer4x4)]
        [TestCase(RetroDitherMode.BlueNoise)]
        [TestCase(RetroDitherMode.PSX)]
        public void PaletteDitherKeepsExactPaletteColorsAndSupportsOneAndSixtyFourColors(RetroDitherMode dither)
        {
            var hex = new string[RetroPaletteProfile.MaxColors];
            for (int index = 0; index < hex.Length; index++)
                hex[index] = ColorUtility.ToHtmlStringRGB(Color.HSVToRGB(index / 64f, 0.3f + 0.7f * (index % 4) / 3f, 0.15f + 0.85f * (index % 8) / 7f));
            RetroPaletteProfile full = Palette(hex), single = Palette("68745D");
            RetroColorProfile profile = Profile(p =>
            {
                p.FindProperty("paletteEnabled").boolValue = true;
                p.FindProperty("palette").objectReferenceValue = full;
                p.FindProperty("paletteDither").enumValueIndex = (int)dither;
            });
            try
            {
                full.TryGetShaderData(out int count, out Vector4[] linear, out _);
                Assert.That(count, Is.EqualTo(64));
                for (int index = 0; index < count; index++)
                for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    Assert.That(RetroPSXColorMath.ApplyArtistic(linear[index], profile, x * 7, y * 5), Is.EqualTo((Vector3)linear[index]),
                        $"Palette color {index} changed by dithering.");
                var random = new System.Random(5);
                for (int sample = 0; sample < 512; sample++)
                {
                    var input = new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble());
                    Vector3 result = RetroPSXColorMath.ApplyArtistic(input, profile, sample, sample / 3);
                    Assert.That(Array.Exists(linear, c => (Vector3)c == result), Is.True, $"Off-palette output {result}");
                }

                var serialized = new SerializedObject(profile);
                serialized.FindProperty("palette").objectReferenceValue = single;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                single.TryGetShaderData(out _, out Vector4[] one, out _);
                for (int sample = 0; sample < 64; sample++)
                    Assert.That(RetroPSXColorMath.ApplyArtistic(new Vector3(sample / 63f, 0.5f, 1f - sample / 63f), profile, sample, sample), Is.EqualTo((Vector3)one[0]));
            }
            finally { Object.DestroyImmediate(profile); Object.DestroyImmediate(full); Object.DestroyImmediate(single); }
        }

        [TestCase("posterizeStrength")]
        [TestCase("paletteStrength")]
        public void StrengthNearZeroFadesInContinuouslyFromHdrInput(string strength)
        {
            RetroPaletteProfile palette = Palette("101413", "D6CBB0");
            RetroColorProfile profile = Profile(p =>
            {
                p.FindProperty("posterizeMode").enumValueIndex = strength == "posterizeStrength" ? (int)RetroPosterizeMode.RGB : 0;
                p.FindProperty("paletteEnabled").boolValue = strength == "paletteStrength";
                p.FindProperty("palette").objectReferenceValue = palette;
            });
            var hdr = new Vector3(4f, 2f, 0.5f);
            try
            {
                SetProperty(profile, strength, 0f);
                Assert.That(RetroPSXColorMath.ApplyArtistic(hdr, profile, 0, 0), Is.EqualTo(hdr), "Strength 0 must bypass.");
                Vector3 previous = hdr;
                foreach (float value in new[] { 0.001f, 0.01f, 0.1f, 0.5f, 1f })
                {
                    SetProperty(profile, strength, value);
                    Vector3 result = RetroPSXColorMath.ApplyArtistic(hdr, profile, 0, 0);
                    if (value <= 0.001f)
                        Assert.That(Vector3.Distance(result, hdr), Is.LessThan(0.02f), $"No jump at strength {value}.");
                    Assert.That(result.x, Is.LessThanOrEqualTo(previous.x + 1e-5f), "Monotonic toward the processed color.");
                    previous = result;
                }
                Assert.That(previous.x, Is.LessThanOrEqualTo(1f + 1e-5f), "Full strength stays in display range.");
            }
            finally { Object.DestroyImmediate(profile); Object.DestroyImmediate(palette); }
        }

        [TestCase(RetroDitherMode.Bayer2x2, 2)]
        [TestCase(RetroDitherMode.Bayer4x4, 4)]
        [TestCase(RetroDitherMode.PSX, 4)]
        [TestCase(RetroDitherMode.BlueNoise, 64)]
        public void DitherThresholdsAreDeterministicAndTileWithTheirPeriod(RetroDitherMode mode, int period)
        {
            Texture2D noise = RetroColorProfile.DefaultBlueNoise;
            for (int y = 0; y < period; y++)
            for (int x = 0; x < period; x++)
            {
                float threshold = RetroPSXColorMath.DitherThreshold(mode, x, y, noise);
                Assert.That(threshold, Is.GreaterThan(0f).And.LessThan(1f));
                Assert.That(RetroPSXColorMath.DitherThreshold(mode, x + period, y + period * 3, noise), Is.EqualTo(threshold));
            }
        }

        internal static RetroPaletteProfile Palette(params string[] hex)
        {
            var colors = new Color[hex.Length];
            for (int index = 0; index < hex.Length; index++)
                ColorUtility.TryParseHtmlString("#" + hex[index], out colors[index]);
            RetroPaletteProfile palette = ScriptableObject.CreateInstance<RetroPaletteProfile>();
            palette.SetColors(colors);
            return palette;
        }

        internal static RetroColorProfile Profile(Action<SerializedObject> configure)
        {
            RetroColorProfile profile = ScriptableObject.CreateInstance<RetroColorProfile>();
            var serialized = new SerializedObject(profile);
            configure(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }

        internal static void SetProperty(RetroColorProfile profile, string name, float value)
        {
            var serialized = new SerializedObject(profile);
            serialized.FindProperty(name).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
