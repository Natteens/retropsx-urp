#ifndef RETROPSX_MATERIAL_PASS_INCLUDED
#define RETROPSX_MATERIAL_PASS_INCLUDED

#include "RetroPSXCommon.hlsl"
#include "RetroPSXMaterialInput.hlsl"

struct RetroAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
    half4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct RetroVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    half3 normalWS : TEXCOORD1;
    float2 uvPerspective : TEXCOORD2;
    noperspective float2 uvAffine : TEXCOORD3;
    noperspective half4 color : TEXCOORD4;
    nointerpolation half4 flatColor : TEXCOORD5;
    noperspective half3 vertexLight : TEXCOORD6;
    nointerpolation half3 flatVertexLight : TEXCOORD7;
    float fogFactor : TEXCOORD8;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

RetroVaryings RetroVertex(RetroAttributes input)
{
    RetroVaryings output = (RetroVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normals = GetVertexNormalInputs(input.normalOS);
    output.positionWS = positions.positionWS;
    output.normalWS = normalize(normals.normalWS);
    output.positionCS = RetroPSX_ApplyVertexPrecision(positions.positionCS, positions.positionWS, _GeometrySnapStrength);
    float2 transformedUV = TRANSFORM_TEX(input.uv, _BaseMap);
    output.uvPerspective = transformedUV;
    output.uvAffine = transformedUV;
    output.color = input.color;
    output.flatColor = input.color;
    output.vertexLight = RetroPSX_VertexLighting(positions.positionWS, output.normalWS, _VertexLightingStrength);
    output.flatVertexLight = output.vertexLight;
    output.fogFactor = RetroPSX_GetFogFactor(positions.positionWS);
    return output;
}

half4 RetroFragment(RetroVaryings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    float2 uv = RetroPSX_GetAffineUV(input.uvPerspective, input.uvAffine, _AffineStrength);
    half4 texel = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv) * _BaseColor;

    if (_AlphaClip > 0.5)
        clip(texel.a - _Cutoff);
    if (_BlackTransparent > 0.5 && max(texel.r, max(texel.g, texel.b)) < (0.5 / 255.0))
        discard;

    half4 vertexColor = _ShadingMode < 0.5 ? input.flatColor : input.color;
    half3 vertexLight = _ShadingMode < 0.5 ? input.flatVertexLight : input.vertexLight;
    half3 color = RetroPSX_ApplyVertexColor(texel.rgb, vertexColor.rgb, _VertexColorMode, _BaseColor.rgb);

    if (_TextureModulation > 0.5)
    {
        half3 modulation = vertexColor.rgb * _ModulationTint.rgb * lerp(1.0h, 2.0h, _Overbright);
        color *= lerp(half3(1.0, 1.0, 1.0), modulation, _ModulationStrength);
    }

    float lightingMode = _LightingMode > 2.5 ? _RetroPSXLightingParams.x : _LightingMode;
    if (lightingMode > 1.5)
        color *= RetroPSX_ModernLighting(input.positionWS, input.normalWS, _VertexLightingStrength);
    else if (lightingMode > 0.5)
        color *= vertexLight;

    color = RetroPSX_ApplyFog(color, input.fogFactor, _FogParticipation);

    int ditherMode = _MaterialDither > 0.5 ? (int)_RetroPSXMaterialDither.x : 0;
    if (_MaterialColorPrecision > 0.5)
    {
        color = RetroPSX_ApplyColorPrecision(
            color,
            ditherMode,
            _RetroPSXMaterialDither.y * _MaterialDitherStrength,
            RetroPSX_GetCanonicalPixel(input.positionCS));
    }
    half alpha = texel.a;
    if (_TransparencyMode > 2.5 && _TransparencyMode < 3.5) alpha = 0.5h;
    if (_TransparencyMode > 5.5) alpha = 0.25h;
    return half4(color, alpha);
}

#endif
