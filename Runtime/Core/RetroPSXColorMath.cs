using UnityEngine;

namespace RetroPSX
{
    /// <summary>
    /// CPU mirror of RetroPSXFinalColor.hlsl, used by Inspector previews and tests. Keep both in sync.
    /// </summary>
    public static class RetroPSXColorMath
    {
        public static float LinearToSrgb(float value)
        {
            value = Mathf.Max(value, 0f);
            return value <= 0.0031308f ? value * 12.92f : 1.055f * Mathf.Pow(value, 1f / 2.4f) - 0.055f;
        }

        public static float SrgbToLinear(float value)
        {
            return value <= 0.04045f ? value / 12.92f : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
        }

        public static Vector3 LinearToSrgb(Vector3 c) => new(LinearToSrgb(c.x), LinearToSrgb(c.y), LinearToSrgb(c.z));
        public static Vector3 SrgbToLinear(Vector3 c) => new(SrgbToLinear(c.x), SrgbToLinear(c.y), SrgbToLinear(c.z));

        public static float Luminance(Vector3 linear) => 0.2126f * linear.x + 0.7152f * linear.y + 0.0722f * linear.z;

        /// <summary>Normalized threshold in (0, 1) for the procedural patterns; texture patterns return 0.5 here.</summary>
        public static float DitherThreshold(RetroDitherMode mode, int x, int y)
        {
            int cx = x & 3, cy = y & 3;
            switch (mode)
            {
                case RetroDitherMode.PSX:
                {
                    int[] psx = { -4, 0, -3, 1, 2, -2, 3, -1, -3, 1, -4, 0, 3, -1, 2, -2 };
                    return (psx[cy * 4 + cx] + 4.5f) / 8f;
                }
                case RetroDitherMode.Bayer2x2:
                {
                    float[] bayer2 = { 0.125f, 0.625f, 0.875f, 0.375f };
                    return bayer2[(cy & 1) * 2 + (cx & 1)];
                }
                case RetroDitherMode.Bayer4x4:
                {
                    float[] bayer4 =
                    {
                        0.03125f, 0.53125f, 0.15625f, 0.65625f,
                        0.78125f, 0.28125f, 0.90625f, 0.40625f,
                        0.21875f, 0.71875f, 0.09375f, 0.59375f,
                        0.96875f, 0.46875f, 0.84375f, 0.34375f
                    };
                    return bayer4[cy * 4 + cx];
                }
                default:
                    return 0.5f;
            }
        }

        public static Vector3 FitToDisplayRange(Vector3 linear)
        {
            Vector3 color = Vector3.Max(linear, Vector3.zero);
            float luminance = Luminance(color);
            if (luminance >= 1f)
                return Vector3.one;
            float peak = Mathf.Max(color.x, Mathf.Max(color.y, color.z));
            if (peak > 1f)
                color = Vector3.one * luminance + (color - Vector3.one * luminance) * ((1f - luminance) / (peak - luminance));
            return Saturate(color);
        }

        public static float PosterizeStep(float value, int levels, float threshold, float ditherStrength)
        {
            if (float.IsNaN(value))
                value = 0f;
            float steps = Mathf.Clamp(levels, RetroColorProfile.MinPosterizeLevels, RetroColorProfile.MaxPosterizeLevels) - 1;
            float offset = Mathf.Lerp(0.5f, 1f - threshold, Mathf.Clamp01(ditherStrength));
            return Mathf.Clamp01(Mathf.Floor(Mathf.Clamp01(value) * steps + offset) / steps);
        }

        public static Vector3 PosterizeRgb(Vector3 linear, int levels, float threshold = 0.5f, float ditherStrength = 0f)
        {
            Vector3 srgb = LinearToSrgb(linear);
            return SrgbToLinear(new Vector3(
                PosterizeStep(srgb.x, levels, threshold, ditherStrength),
                PosterizeStep(srgb.y, levels, threshold, ditherStrength),
                PosterizeStep(srgb.z, levels, threshold, ditherStrength)));
        }

        public static Vector3 PosterizeLuminance(Vector3 linear, int levels, float threshold = 0.5f, float ditherStrength = 0f)
        {
            float luminance = Luminance(linear);
            float target = SrgbToLinear(PosterizeStep(LinearToSrgb(luminance), levels, threshold, ditherStrength));
            if (luminance <= 1e-6f)
                return Vector3.one * target;
            Vector3 color = Vector3.Lerp(Vector3.one * target, linear * (target / luminance), Mathf.Clamp01(luminance * 256f));
            float peak = Mathf.Max(color.x, Mathf.Max(color.y, color.z));
            if (peak > 1f)
                color = Vector3.one * target + (color - Vector3.one * target) * ((1f - target) / Mathf.Max(peak - target, 1e-6f));
            return Saturate(color);
        }

        public static Vector3 LinearToOklab(Vector3 c)
        {
            float l = 0.4122214708f * c.x + 0.5363325363f * c.y + 0.0514459929f * c.z;
            float m = 0.2119034982f * c.x + 0.6806995451f * c.y + 0.1073969566f * c.z;
            float s = 0.0883024619f * c.x + 0.2817188376f * c.y + 0.6299787005f * c.z;
            l = Mathf.Pow(Mathf.Max(l, 0f), 1f / 3f);
            m = Mathf.Pow(Mathf.Max(m, 0f), 1f / 3f);
            s = Mathf.Pow(Mathf.Max(s, 0f), 1f / 3f);
            return new Vector3(
                0.2104542553f * l + 0.7936177850f * m - 0.0040720468f * s,
                1.9779984951f * l - 2.4285922050f * m + 0.4505937099f * s,
                0.0259040371f * l + 0.7827717662f * m - 0.8086757660f * s);
        }

        public static void FindPalettePair(Vector3 lab, Vector4[] paletteOklab, int count, bool findSecond, out int first, out int second, out float t)
        {
            first = 0;
            float best = float.MaxValue;
            for (int index = 0; index < count; index++)
            {
                Vector3 delta = lab - (Vector3)paletteOklab[index];
                float distance = Vector3.Dot(delta, delta);
                if (distance < best)
                {
                    best = distance;
                    first = index;
                }
            }
            second = first;
            t = 0f;
            if (!findSecond)
                return;
            Vector3 origin = paletteOklab[first];
            Vector3 offset = lab - origin;
            float bestCost = float.MaxValue;
            for (int candidate = 0; candidate < count; candidate++)
            {
                Vector3 segment = (Vector3)paletteOklab[candidate] - origin;
                float length2 = Vector3.Dot(segment, segment);
                if (candidate == first || length2 < 1e-8f)
                    continue;
                float position = Mathf.Clamp01(Vector3.Dot(offset, segment) / length2);
                if (position <= 0f)
                    continue;
                Vector3 error = offset - segment * position;
                float cost = Vector3.Dot(error, error) + 0.05f * length2;
                if (cost < bestCost)
                {
                    bestCost = cost;
                    second = candidate;
                    t = position;
                }
            }
        }

        public static Vector3 MixStrength(Vector3 original, Vector3 processed, float strength)
        {
            if (strength >= 1f)
                return processed;
            return SrgbToLinear(Vector3.Lerp(LinearToSrgb(original), LinearToSrgb(processed), strength));
        }

        /// <summary>Artistic stage of the final color for one pixel, with a procedural threshold pattern.</summary>
        public static Vector3 ApplyArtistic(Vector3 linear, RetroColorProfile profile, int x, int y)
        {
            if (profile == null || (!profile.PosterizeActive && !profile.PaletteActive))
                return linear;
            Vector3 color = FitToDisplayRange(linear);
            bool palette = profile.PaletteActive;
            if (profile.PosterizeActive)
            {
                RetroDitherMode dither = palette ? RetroDitherMode.Off : profile.PosterizeDither;
                float strength = dither != RetroDitherMode.Off ? profile.PosterizeDitherStrength : 0f;
                float threshold = DitherThreshold(dither, x, y);
                Vector3 posterized = profile.PosterizeMode == RetroPosterizeMode.Luminance
                    ? PosterizeLuminance(color, profile.PosterizeLevels, threshold, strength)
                    : PosterizeRgb(color, profile.PosterizeLevels, threshold, strength);
                color = MixStrength(color, posterized, profile.PosterizeStrength);
            }
            if (palette)
            {
                profile.Palette.TryGetShaderData(out int count, out Vector4[] paletteLinear, out Vector4[] paletteOklab);
                float ditherStrength = profile.PaletteDither != RetroDitherMode.Off ? profile.PaletteDitherStrength : 0f;
                FindPalettePair(LinearToOklab(color), paletteOklab, count, ditherStrength > 0f, out int first, out int second, out float t);
                int chosen = t * ditherStrength > DitherThreshold(profile.PaletteDither, x, y) ? second : first;
                color = MixStrength(color, paletteLinear[chosen], profile.PaletteStrength);
            }
            return color;
        }

        private static Vector3 Saturate(Vector3 v) => new(Mathf.Clamp01(v.x), Mathf.Clamp01(v.y), Mathf.Clamp01(v.z));
    }
}
