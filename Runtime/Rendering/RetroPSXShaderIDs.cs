using UnityEngine;

namespace RetroPSX.Rendering
{
    internal static class RetroPSXShaderIDs
    {
        internal static readonly int InternalSize = Shader.PropertyToID("_RetroPSXInternalSize");
        internal static readonly int SourceSize = Shader.PropertyToID("_RetroPSXSourceSize");
        internal static readonly int GeometryParams = Shader.PropertyToID("_RetroPSXGeometryParams");
        internal static readonly int GeometryNearFade = Shader.PropertyToID("_RetroPSXGeometryNearFade");
        internal static readonly int AffineParams = Shader.PropertyToID("_RetroPSXAffineParams");
        internal static readonly int ColorBits = Shader.PropertyToID("_RetroPSXColorBits");
        internal static readonly int MaterialDither = Shader.PropertyToID("_RetroPSXMaterialDither");
        internal static readonly int LightingParams = Shader.PropertyToID("_RetroPSXLightingParams");
        internal static readonly int AmbientColor = Shader.PropertyToID("_RetroPSXAmbientColor");
        internal static readonly int FogParams = Shader.PropertyToID("_RetroPSXFogParams");
        internal static readonly int FogStrength = Shader.PropertyToID("_RetroPSXFogStrength");
        internal static readonly int FogColor = Shader.PropertyToID("_RetroPSXFogColor");
        internal static readonly int DebugMode = Shader.PropertyToID("_RetroPSXDebugMode");
        internal static readonly int MainLightDirection = Shader.PropertyToID("_RetroPSXMainLightDirection");
        internal static readonly int MainLightColor = Shader.PropertyToID("_RetroPSXMainLightColor");

        internal static readonly int FinalColorParams = Shader.PropertyToID("_RetroFinalColorParams");
        internal static readonly int CustomDither = Shader.PropertyToID("_RetroCustomDither");
        internal static readonly int BlueNoise = Shader.PropertyToID("_RetroBlueNoise");
        internal static readonly int PresentationRect = Shader.PropertyToID("_RetroPresentationRect");
        internal static readonly int LetterboxColor = Shader.PropertyToID("_RetroLetterboxColor");
        internal static readonly int PreserveAlpha = Shader.PropertyToID("_RetroPreserveAlpha");
        internal static readonly int VolumeTexture = Shader.PropertyToID("_RetroVolumeTexture");
        internal static readonly int VolumeTexelSize = Shader.PropertyToID("_RetroVolumeTexelSize");
        internal static readonly int VolumeParams0 = Shader.PropertyToID("_RetroVolumeParams0");
        internal static readonly int VolumeParams1 = Shader.PropertyToID("_RetroVolumeParams1");
        internal static readonly int VolumeParams2 = Shader.PropertyToID("_RetroVolumeParams2");
        internal static readonly int VolumeParams3 = Shader.PropertyToID("_RetroVolumeParams3");
        internal static readonly int VolumeAmbient = Shader.PropertyToID("_RetroVolumeAmbient");
        internal static readonly int LocalLightCount = Shader.PropertyToID("_RetroLocalLightCount");
        internal static readonly int LocalLightPosRange = Shader.PropertyToID("_RetroLocalLightPosRange");
        internal static readonly int LocalLightDirAngle = Shader.PropertyToID("_RetroLocalLightDirAngle");
        internal static readonly int LocalLightColorDensity = Shader.PropertyToID("_RetroLocalLightColorDensity");
        internal static readonly int LocalLightParams = Shader.PropertyToID("_RetroLocalLightParams");
        internal static readonly int LocalPatternTransform = Shader.PropertyToID("_RetroLocalPatternTransform");
        internal static readonly int LocalPatternParams = Shader.PropertyToID("_RetroLocalPatternParams");
        internal static readonly int LocalPatternExtra = Shader.PropertyToID("_RetroLocalPatternExtra");
        internal static readonly int LocalLightStylization = Shader.PropertyToID("_RetroLocalLightStylization");
        internal static readonly int Pattern0 = Shader.PropertyToID("_RetroPattern0");
        internal static readonly int Pattern1 = Shader.PropertyToID("_RetroPattern1");
        internal static readonly int Pattern2 = Shader.PropertyToID("_RetroPattern2");
        internal static readonly int Pattern3 = Shader.PropertyToID("_RetroPattern3");

        internal static readonly int CRTParams0 = Shader.PropertyToID("_RetroCRTParams0");
        internal static readonly int CRTParams1 = Shader.PropertyToID("_RetroCRTParams1");
        internal static readonly int CRTParams2 = Shader.PropertyToID("_RetroCRTParams2");
        internal static readonly int PixelBloom = Shader.PropertyToID("_RetroPixelBloom");
    }
}
