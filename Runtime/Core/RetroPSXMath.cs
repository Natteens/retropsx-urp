using UnityEngine;

namespace RetroPSX
{
    /// <summary>Allocation-free pure helpers shared by profiles, renderer code, and tests.</summary>
    public static class RetroPSXMath
    {
        public static Vector2Int PresetSize(RetroResolutionPreset preset, Vector2Int custom)
        {
            return preset switch
            {
                RetroResolutionPreset.R256x224 => new Vector2Int(256, 224),
                RetroResolutionPreset.R320x240 => new Vector2Int(320, 240),
                RetroResolutionPreset.R320x180 => new Vector2Int(320, 180),
                RetroResolutionPreset.R368x240 => new Vector2Int(368, 240),
                RetroResolutionPreset.R512x240 => new Vector2Int(512, 240),
                _ => new Vector2Int(Mathf.Max(1, custom.x), Mathf.Max(1, custom.y))
            };
        }

        public static Vector2Int InternalSize(
            int sourceWidth,
            int sourceHeight,
            RetroRasterMode mode,
            RetroResolutionPreset preset,
            Vector2Int custom,
            int internalHeight,
            float scaleFactor)
        {
            sourceWidth = Mathf.Max(1, sourceWidth);
            sourceHeight = Mathf.Max(1, sourceHeight);

            switch (mode)
            {
                case RetroRasterMode.FixedResolution:
                {
                    Vector2Int requested = PresetSize(preset, custom);
                    int width = Mathf.Max(1, Mathf.RoundToInt(requested.y * (sourceWidth / (float)sourceHeight)));
                    return new Vector2Int(width, requested.y);
                }
                case RetroRasterMode.InternalHeight:
                {
                    int height = Mathf.Clamp(internalHeight, 1, sourceHeight);
                    int width = Mathf.Max(1, Mathf.RoundToInt(height * (sourceWidth / (float)sourceHeight)));
                    return new Vector2Int(width, height);
                }
                case RetroRasterMode.ScaleFactor:
                    return new Vector2Int(
                        Mathf.Max(1, Mathf.RoundToInt(sourceWidth * Mathf.Clamp01(scaleFactor))),
                        Mathf.Max(1, Mathf.RoundToInt(sourceHeight * Mathf.Clamp01(scaleFactor))));
                default:
                    return new Vector2Int(sourceWidth, sourceHeight);
            }
        }

        public static RectInt PresentationViewport(Vector2Int source, Vector2Int internalSize, RetroPresentationMode mode)
        {
            if (mode == RetroPresentationMode.Stretch)
                return new RectInt(0, 0, source.x, source.y);

            float fit = Mathf.Min(source.x / (float)internalSize.x, source.y / (float)internalSize.y);
            if (mode == RetroPresentationMode.IntegerFit)
                fit = Mathf.Max(1f, Mathf.Floor(fit));

            int width = Mathf.Clamp(Mathf.RoundToInt(internalSize.x * fit), 1, source.x);
            int height = Mathf.Clamp(Mathf.RoundToInt(internalSize.y * fit), 1, source.y);
            return new RectInt((source.x - width) / 2, (source.y - height) / 2, width, height);
        }

        public static Vector3Int ColorBits(RetroColorMode mode, Vector3Int custom)
        {
            return mode switch
            {
                RetroColorMode.RGB444 => new Vector3Int(4, 4, 4),
                RetroColorMode.RGB555 => new Vector3Int(5, 5, 5),
                RetroColorMode.RGB565 => new Vector3Int(5, 6, 5),
                RetroColorMode.RGB666 => new Vector3Int(6, 6, 6),
                RetroColorMode.Custom => new Vector3Int(
                    Mathf.Clamp(custom.x, 1, 8), Mathf.Clamp(custom.y, 1, 8), Mathf.Clamp(custom.z, 1, 8)),
                _ => new Vector3Int(8, 8, 8)
            };
        }

        public static void VolumetricQuality(RetroVolumetricQuality quality, ref int divisor, ref int steps)
        {
            switch (quality)
            {
                case RetroVolumetricQuality.Low:
                    divisor = 4; steps = 16; break;
                case RetroVolumetricQuality.Medium:
                    divisor = 2; steps = 28; break;
                case RetroVolumetricQuality.High:
                    divisor = 2; steps = 48; break;
                default:
                    divisor = Mathf.Clamp(divisor, 1, 8);
                    steps = Mathf.Clamp(steps, 4, 128);
                    break;
            }
        }
    }
}
