using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RetroPSX.Editor
{
    /// <summary>Result of reading a palette source. Nothing is applied until the user confirms.</summary>
    public sealed class RetroPaletteImportResult
    {
        public readonly List<Color32> Colors = new();
        public readonly List<string> Errors = new();
        public readonly List<string> Warnings = new();
        public string Source;
        public int Duplicates;

        public bool OverLimit => Colors.Count > RetroPaletteProfile.MaxColors;
        public bool CanApply => Colors.Count > 0 && !OverLimit && Errors.Count == 0;
    }

    public static class RetroPaletteImport
    {
        /// <summary>One color per line: RRGGBB with optional '#', case-insensitive; blank lines are ignored.</summary>
        public static RetroPaletteImportResult ParseHex(string text, string source = "HEX")
        {
            var result = new RetroPaletteImportResult { Source = source };
            var seen = new HashSet<int>();
            var invalid = new List<string>();
            string[] lines = (text ?? string.Empty).Split('\n');
            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index].Trim();
                if (line.Length == 0)
                    continue;
                if (!TryParseHex(line, out Color32 color))
                {
                    invalid.Add($"Line {index + 1}: '{Truncate(line)}' is not RRGGBB.");
                    continue;
                }
                if (seen.Add(Key(color)))
                    result.Colors.Add(color);
                else
                    result.Duplicates++;
            }
            if (invalid.Count > 0)
            {
                int shown = Math.Min(invalid.Count, 5);
                result.Errors.AddRange(invalid.GetRange(0, shown));
                if (invalid.Count > shown)
                    result.Errors.Add($"{invalid.Count - shown} more invalid lines.");
            }
            Finish(result);
            return result;
        }

        /// <summary>
        /// Unique RGB colors in reading order (top row first, left to right). Fully transparent pixels are skipped.
        /// The bytes are decoded in memory, so the texture's import settings are never touched.
        /// </summary>
        public static RetroPaletteImportResult ParsePng(byte[] bytes, string source = "PNG")
        {
            var result = new RetroPaletteImportResult { Source = source };
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                if (bytes == null || bytes.Length == 0 || !texture.LoadImage(bytes, false))
                {
                    result.Errors.Add("The file could not be decoded as an image.");
                    return result;
                }
                ReadPixels(texture.GetPixels32(), texture.width, texture.height, result);
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
            Finish(result);
            return result;
        }

        internal static void ReadPixels(Color32[] pixels, int width, int height, RetroPaletteImportResult result)
        {
            var seen = new HashSet<int>();
            int transparent = 0, translucent = 0;
            for (int y = height - 1; y >= 0; y--)
            for (int x = 0; x < width; x++)
            {
                Color32 pixel = pixels[y * width + x];
                if (pixel.a == 0)
                {
                    transparent++;
                    continue;
                }
                if (pixel.a < 255)
                    translucent++;
                pixel.a = 255;
                if (seen.Add(Key(pixel)))
                    result.Colors.Add(pixel);
            }
            if (transparent > 0)
                result.Warnings.Add($"{transparent} fully transparent pixels were skipped.");
            if (translucent > 0)
                result.Warnings.Add($"{translucent} semi-transparent pixels were read as opaque RGB.");
        }

        public static string ToHex(Color color) => ColorUtility.ToHtmlStringRGB(color);

        public static string ToHexText(IReadOnlyList<Color> colors, bool hash)
        {
            var builder = new StringBuilder();
            for (int index = 0; index < colors.Count; index++)
                builder.Append(hash ? "#" : string.Empty).Append(ToHex(colors[index])).Append('\n');
            return builder.ToString();
        }

        public static bool TryParseHex(string text, out Color32 color)
        {
            color = default;
            string value = (text ?? string.Empty).Trim();
            if (value.StartsWith("#", StringComparison.Ordinal))
                value = value.Substring(1);
            if (value.Length != 6 || !int.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
                return false;
            color = new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
            return true;
        }

        private static void Finish(RetroPaletteImportResult result)
        {
            if (result.Duplicates > 0)
                result.Warnings.Add($"{result.Duplicates} duplicate colors were ignored.");
            if (result.OverLimit)
                result.Errors.Add($"Found {result.Colors.Count} unique colors; a palette holds at most {RetroPaletteProfile.MaxColors}. " +
                    "Images with antialiasing, gradients or lossy compression are not palettes. Nothing was imported.");
            else if (result.Colors.Count == 0 && result.Errors.Count == 0)
                result.Errors.Add("No colors found.");
        }

        private static int Key(Color32 color) => color.r << 16 | color.g << 8 | color.b;
        private static string Truncate(string text) => text.Length > 24 ? text.Substring(0, 24) + "…" : text;
    }
}
