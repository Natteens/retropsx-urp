#ifndef RETROPSX_GEOMETRY_INCLUDED
#define RETROPSX_GEOMETRY_INCLUDED

#include "RetroPSXCore.hlsl"

float4 RetroPSX_ApplyVertexPrecision(float4 clipPosition, float3 positionWS, float materialStrength)
{
    if (_RetroPSXGeometryParams.x < 0.5 || materialStrength <= 0.0)
        return clipPosition;

    float viewDepth = -TransformWorldToView(positionWS).z;
    float nearClip = max(_ProjectionParams.y, 1e-4);
    if (clipPosition.w <= 1e-5 || viewDepth <= nearClip)
        return clipPosition;

    float2 grid = max(_RetroPSXInternalSize.xy * _RetroPSXGeometryParams.z, 1.0);
    float2 ndc = clipPosition.xy / clipPosition.w;
    float2 pixel = (ndc * 0.5 + 0.5) * grid;
    float2 snappedNdc = (floor(pixel + 0.5) / grid) * 2.0 - 1.0;
    float viewDistance = distance(_WorldSpaceCameraPos, positionWS);
    float distanceFactor = lerp(1.0, saturate(viewDistance * 0.05), _RetroPSXGeometryParams.w);
    float nearFactor = _RetroPSXGeometryNearFade > 0.0
        ? smoothstep(nearClip, nearClip + _RetroPSXGeometryNearFade, viewDepth)
        : 1.0;
    float strength = saturate(_RetroPSXGeometryParams.y * materialStrength * distanceFactor * nearFactor);
    clipPosition.xy = lerp(clipPosition.xy, snappedNdc * clipPosition.w, strength);
    return clipPosition;
}

float3 RetroPSX_PackAffineUV(float2 uv, float clipW)
{
    return float3(uv * clipW, clipW);
}

float2 RetroPSX_UnpackAffineUV(float3 packedUV)
{
    float safeW = abs(packedUV.z) < 1e-5 ? (packedUV.z < 0.0 ? -1e-5 : 1e-5) : packedUV.z;
    return packedUV.xy / safeW;
}

float2 RetroPSX_GetAffineUV(float2 perspectiveUV, float2 affineUV, float materialStrength)
{
    float profileStrength = _RetroPSXAffineParams.x < 0.5 ? 0.0 : _RetroPSXAffineParams.y;
    return lerp(perspectiveUV, affineUV, saturate(profileStrength * materialStrength));
}

float4 RetroPSXSnapClipPosition(float4 clipPosition, float3 positionWS, float materialStrength)
{
    return RetroPSX_ApplyVertexPrecision(clipPosition, positionWS, materialStrength);
}

#endif
