using NUnit.Framework;
using RetroPSX.Editor;
using UnityEngine;

namespace RetroPSX.Tests
{
    public sealed class RetroPSXPaletteImportTests
    {
        [Test]
        public void HexAcceptsHashCaseWhitespaceAndBlankLines()
        {
            RetroPaletteImportResult result = RetroPaletteImport.ParseHex("#101413\r\n  242d2b  \n\n414C43\n#101413\n");
            Assert.That(result.CanApply, Is.True);
            Assert.That(result.Colors, Is.EqualTo(new[] { new Color32(0x10, 0x14, 0x13, 255), new Color32(0x24, 0x2D, 0x2B, 255), new Color32(0x41, 0x4C, 0x43, 255) }));
            Assert.That(result.Duplicates, Is.EqualTo(1));
            Assert.That(result.Warnings, Has.Some.Contains("duplicate"));
        }

        [Test]
        public void HexReportsInvalidLinesAndBlocksApply()
        {
            RetroPaletteImportResult result = RetroPaletteImport.ParseHex("ffffff\nnot-a-color\n12345\n#GG0000");
            Assert.That(result.CanApply, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("Line 2"));
            Assert.That(result.Errors, Has.Some.Contains("Line 4"));
            Assert.That(RetroPaletteImport.ParseHex("\n \n").Errors, Has.Some.Contains("No colors"));
        }

        [Test]
        public void HexOverLimitIsReportedNotTruncated()
        {
            var text = new System.Text.StringBuilder();
            for (int index = 0; index < RetroPaletteProfile.MaxColors + 6; index++)
                text.AppendLine(index.ToString("X6"));
            RetroPaletteImportResult result = RetroPaletteImport.ParseHex(text.ToString());
            Assert.That(result.CanApply, Is.False);
            Assert.That(result.Colors.Count, Is.EqualTo(RetroPaletteProfile.MaxColors + 6));
            Assert.That(result.Errors, Has.Some.Contains((RetroPaletteProfile.MaxColors + 6).ToString()));
        }

        [TestCase(4, 1)]
        [TestCase(1, 4)]
        public void PngStripReadsInOrder(int width, int height)
        {
            var colors = new[] { new Color32(255, 0, 0, 255), new Color32(0, 255, 0, 255), new Color32(0, 0, 255, 255), new Color32(9, 9, 9, 255) };
            var pixels = new Color32[4];
            for (int index = 0; index < 4; index++)
                pixels[width == 1 ? 3 - index : index] = colors[index]; // GetPixels32 rows run bottom-up.
            RetroPaletteImportResult result = RetroPaletteImport.ParsePng(Encode(pixels, width, height));
            Assert.That(result.CanApply, Is.True, string.Join("\n", result.Errors));
            Assert.That(result.Colors, Is.EqualTo(colors));
        }

        [Test]
        public void PngSwatchGridSkipsTransparentAndDeduplicates()
        {
            // 4x2 image of 2x1 blocks, top row: red red | transparent transparent; bottom row: blue blue | red red.
            var clear = new Color32(0, 0, 0, 0);
            Color32 red = new(200, 10, 10, 255), blue = new(10, 10, 200, 255);
            var pixels = new[] { blue, blue, red, red, red, red, clear, clear };
            RetroPaletteImportResult result = RetroPaletteImport.ParsePng(Encode(pixels, 4, 2));
            Assert.That(result.Colors, Is.EqualTo(new[] { red, blue }));
            Assert.That(result.Warnings, Has.Some.Contains("transparent"));
        }

        [Test]
        public void PngOverLimitAndInvalidBytesAreReported()
        {
            var pixels = new Color32[16 * 16];
            for (int index = 0; index < pixels.Length; index++)
                pixels[index] = new Color32((byte)index, (byte)(index * 7), 3, 255);
            RetroPaletteImportResult result = RetroPaletteImport.ParsePng(Encode(pixels, 16, 16));
            Assert.That(result.CanApply, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("256"));
            Assert.That(RetroPaletteImport.ParsePng(new byte[] { 1, 2, 3 }).CanApply, Is.False);
        }

        static byte[] Encode(Color32[] pixels, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            byte[] bytes = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            return bytes;
        }
    }
}
