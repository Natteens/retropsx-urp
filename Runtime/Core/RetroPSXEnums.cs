namespace RetroPSX
{
    public enum RetroSceneViewMode { Off, WorldEffects, FullPipeline }
    /// <summary>Controls whether a world-space UI Toolkit panel is composed after the RetroPSX presentation or inside it.</summary>
    public enum RetroUIRenderMode { Native, Retro }
    public enum RetroRasterMode { Native, FixedResolution, InternalHeight, ScaleFactor }
    public enum RetroResolutionPreset { Custom, R256x224, R320x240, R320x180, R368x240, R512x240 }
    public enum RetroPresentationMode { Stretch, AspectFit, IntegerFit }
    public enum RetroColorMode { Off, RGB444, RGB555, RGB565, RGB666, Custom }
    public enum RetroDitherMode { Off, PSX, Bayer2x2, Bayer4x4, Custom, BlueNoise }
    public enum RetroAffineMode { Off, Authentic, ArtisticBlend }
    public enum RetroGeometryPrecisionMode { Off, AuthenticInteger, Artistic }
    public enum RetroLightingMode { Unlit, VertexLit, ModernLit }
    public enum RetroFogMode { Off, DistanceColor, DistanceModulation, SteppedDistanceColor }
    public enum RetroVolumetricQuality { Low, Medium, High, Custom }
    /// <summary>Controls whether a local volumetric light consumes the associated Unity Light's realtime shadows.</summary>
    public enum RetroVolumetricShadowMode { UseLightShadows, Off }
    public enum RetroVolumetricPatternType { None, Stripes, Checker, Radial, Noise, Texture }
    public enum RetroPatternMapping { World, Local, Projector }
    public enum RetroCRTMaskMode { Off, ShadowMask, ApertureGrille }
    public enum RetroTransparencyMode { Opaque, AlphaTest, ModernAlpha, Average, Additive, Subtractive, AddQuarter }
    public enum RetroDebugMode
    {
        None,
        InternalResolution,
        PixelGrid,
        RGBQuantization,
        DitherPattern,
        FogFactor,
        VolumetricDensity,
        VolumetricBuffer,
        DepthReconstruction,
        FinalComposite,
        VolumetricLightVisibility
    }
}
