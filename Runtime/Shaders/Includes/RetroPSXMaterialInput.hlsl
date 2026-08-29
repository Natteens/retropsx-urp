#ifndef RETROPSX_MATERIAL_INPUT_INCLUDED
#define RETROPSX_MATERIAL_INPUT_INCLUDED

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half4 _BaseColor;
half4 _ModulationTint;
float _Cutoff;
float _AlphaClip;
float _BlackTransparent;
float _VertexColorMode;
float _ShadingMode;
float _TextureModulation;
float _ModulationStrength;
float _Overbright;
float _LightingMode;
float _VertexLightingStrength;
float _GeometrySnapStrength;
float _AffineStrength;
float _MaterialColorPrecision;
float _MaterialDither;
float _MaterialDitherStrength;
float _FogParticipation;
float _TransparencyMode;
CBUFFER_END

#endif
