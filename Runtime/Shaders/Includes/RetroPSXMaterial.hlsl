#ifndef RETROPSX_MATERIAL_INCLUDED
#define RETROPSX_MATERIAL_INCLUDED

#include "RetroPSXGeometry.hlsl"
#include "RetroPSXColor.hlsl"
#include "RetroPSXLighting.hlsl"
#include "RetroPSXFog.hlsl"

// Float-precision entry points for Shader Graph Custom Function nodes in File mode.
void RetroPSX_ApplyVertexPrecision_float(float4 positionCS, float3 positionWS, float strength, out float4 result)
{
    result = RetroPSX_ApplyVertexPrecision(positionCS, positionWS, strength);
}

void RetroPSX_PackAffineUV_float(float2 uv, float clipW, out float3 result)
{
    result = RetroPSX_PackAffineUV(uv, clipW);
}

void RetroPSX_UnpackAffineUV_float(float3 packedUV, out float2 result)
{
    result = RetroPSX_UnpackAffineUV(packedUV);
}

void RetroPSX_ApplyColorPrecision_float(float3 color, float2 positionCS, float ditherStrength, out float3 result)
{
    result = RetroPSX_ApplyColorPrecision((half3)color, (int)_RetroPSXMaterialDither.x, ditherStrength, int2(positionCS));
}

void RetroPSX_GetFogFactor_float(float3 positionWS, out float result)
{
    result = RetroPSX_GetFogFactor(positionWS);
}

void RetroPSX_ApplyFog_float(float3 color, float factor, float strength, out float3 result)
{
    result = RetroPSX_ApplyFog((half3)color, factor, strength);
}

#endif
