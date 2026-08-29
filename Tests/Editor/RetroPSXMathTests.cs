using NUnit.Framework;
using UnityEngine;

namespace RetroPSX.Tests
{
    public sealed class RetroPSXMathTests
    {
        [TestCase(RetroResolutionPreset.R256x224, 256, 224)]
        [TestCase(RetroResolutionPreset.R320x240, 320, 240)]
        [TestCase(RetroResolutionPreset.R320x180, 320, 180)]
        [TestCase(RetroResolutionPreset.R368x240, 368, 240)]
        [TestCase(RetroResolutionPreset.R512x240, 512, 240)]
        public void PresetsReturnDocumentedSizes(RetroResolutionPreset preset, int width, int height)
        {
            Assert.That(RetroPSXMath.PresetSize(preset, Vector2Int.one), Is.EqualTo(new Vector2Int(width, height)));
        }

        [Test]
        public void HeightModePreservesSourceAspect()
        {
            Vector2Int size = RetroPSXMath.InternalSize(1920, 1080, RetroRasterMode.InternalHeight, RetroResolutionPreset.Custom, Vector2Int.one, 240, 1f);
            Assert.That(size, Is.EqualTo(new Vector2Int(427, 240)));
        }

        [TestCase(1920, 1080, 427, 240)]
        [TestCase(2560, 1440, 427, 240)]
        [TestCase(2560, 1080, 569, 240)]
        [TestCase(3440, 1440, 573, 240)]
        [TestCase(5120, 1440, 853, 240)]
        public void HeightModeAdaptsToWideViewports(int sourceWidth, int sourceHeight, int expectedWidth, int expectedHeight)
        {
            Vector2Int size = RetroPSXMath.InternalSize(sourceWidth, sourceHeight, RetroRasterMode.InternalHeight, RetroResolutionPreset.Custom, Vector2Int.one, 240, 1f);
            Assert.That(size, Is.EqualTo(new Vector2Int(expectedWidth, expectedHeight)));
            Assert.That(size.x / (float)size.y, Is.EqualTo(sourceWidth / (float)sourceHeight).Within(1f / expectedHeight));
        }

        [TestCase(1920, 1080)]
        [TestCase(2560, 1440)]
        [TestCase(2560, 1080)]
        [TestCase(3440, 1440)]
        [TestCase(5120, 1440)]
        public void StretchPresentationAlwaysFillsViewport(int sourceWidth, int sourceHeight)
        {
            Vector2Int source = new(sourceWidth, sourceHeight);
            Vector2Int internalSize = RetroPSXMath.InternalSize(sourceWidth, sourceHeight, RetroRasterMode.InternalHeight, RetroResolutionPreset.Custom, Vector2Int.one, 240, 1f);
            RectInt viewport = RetroPSXMath.PresentationViewport(source, internalSize, RetroPresentationMode.Stretch);
            Assert.That(viewport, Is.EqualTo(new RectInt(0, 0, sourceWidth, sourceHeight)));
            Assert.That(viewport.x, Is.Zero);
            Assert.That(viewport.y, Is.Zero);
        }

        [Test]
        public void FixedResolutionUsesReferenceHeightWithoutForcingFourByThree()
        {
            Vector2Int size = RetroPSXMath.InternalSize(3440, 1440, RetroRasterMode.FixedResolution, RetroResolutionPreset.R320x240, Vector2Int.one, 240, 1f);
            Assert.That(size, Is.EqualTo(new Vector2Int(573, 240)));
        }

        [Test]
        public void NativeModeTracksResizeExactly()
        {
            Assert.That(
                RetroPSXMath.InternalSize(3440, 1440, RetroRasterMode.Native, RetroResolutionPreset.R320x240, Vector2Int.one, 240, 1f),
                Is.EqualTo(new Vector2Int(3440, 1440)));
        }

        [Test]
        public void ScaleFactorUsesCurrentCameraDimensions()
        {
            Vector2Int size = RetroPSXMath.InternalSize(3440, 1440, RetroRasterMode.ScaleFactor, RetroResolutionPreset.Custom, Vector2Int.one, 240, 0.5f);
            Assert.That(size, Is.EqualTo(new Vector2Int(1720, 720)));
        }

        [Test]
        public void PerCameraCalculationsDoNotShareResizeState()
        {
            Vector2Int first = RetroPSXMath.InternalSize(1920, 1080, RetroRasterMode.InternalHeight, RetroResolutionPreset.Custom, Vector2Int.one, 240, 1f);
            Vector2Int ultrawide = RetroPSXMath.InternalSize(5120, 1440, RetroRasterMode.InternalHeight, RetroResolutionPreset.Custom, Vector2Int.one, 240, 1f);
            Vector2Int firstAgain = RetroPSXMath.InternalSize(1920, 1080, RetroRasterMode.InternalHeight, RetroResolutionPreset.Custom, Vector2Int.one, 240, 1f);
            Assert.That(first, Is.EqualTo(new Vector2Int(427, 240)));
            Assert.That(ultrawide, Is.EqualTo(new Vector2Int(853, 240)));
            Assert.That(firstAgain, Is.EqualTo(first));
        }

        [Test]
        public void AspectFitProducesCenteredLetterbox()
        {
            RectInt viewport = RetroPSXMath.PresentationViewport(new Vector2Int(1920, 1080), new Vector2Int(320, 240), RetroPresentationMode.AspectFit);
            Assert.That(viewport, Is.EqualTo(new RectInt(240, 0, 1440, 1080)));
        }

        [Test]
        public void IntegerFitUsesWholePixelScale()
        {
            RectInt viewport = RetroPSXMath.PresentationViewport(new Vector2Int(1366, 768), new Vector2Int(320, 240), RetroPresentationMode.IntegerFit);
            Assert.That(viewport.width, Is.EqualTo(960));
            Assert.That(viewport.height, Is.EqualTo(720));
        }

        [TestCase(RetroColorMode.RGB444, 4, 4, 4)]
        [TestCase(RetroColorMode.RGB555, 5, 5, 5)]
        [TestCase(RetroColorMode.RGB565, 5, 6, 5)]
        [TestCase(RetroColorMode.RGB666, 6, 6, 6)]
        public void ColorModesMapToChannelBits(RetroColorMode mode, int red, int green, int blue)
        {
            Assert.That(RetroPSXMath.ColorBits(mode, Vector3Int.one), Is.EqualTo(new Vector3Int(red, green, blue)));
        }

        [Test]
        public void QualityPresetOverridesAdvancedValues()
        {
            int divisor = 8;
            int steps = 99;
            RetroPSXMath.VolumetricQuality(RetroVolumetricQuality.Medium, ref divisor, ref steps);
            Assert.That((divisor, steps), Is.EqualTo((2, 28)));
        }
    }
}
