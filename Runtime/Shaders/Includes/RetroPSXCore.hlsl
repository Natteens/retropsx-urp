#ifndef RETROPSX_CORE_INCLUDED
#define RETROPSX_CORE_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

float4 _RetroPSXInternalSize;
float4 _RetroPSXSourceSize;
float4 _RetroPSXGeometryParams;
float _RetroPSXGeometryNearFade;
float4 _RetroPSXAffineParams;
float4 _RetroPSXColorBits;
float4 _RetroPSXMaterialDither;
float4 _RetroPSXLightingParams;
half4 _RetroPSXAmbientColor;
float4 _RetroPSXFogParams;
float _RetroPSXFogStrength;
half4 _RetroPSXFogColor;
int _RetroPSXDebugMode;

struct RetroPSXRasterContext
{
    float2 internalSize;
    float2 internalTexelSize;
    float2 sourceSize;
    float2 sourceTexelSize;
};

RetroPSXRasterContext RetroPSX_GetRasterContext()
{
    RetroPSXRasterContext context;
    context.internalSize = max(_RetroPSXInternalSize.xy, 1.0);
    context.internalTexelSize = _RetroPSXInternalSize.zw;
    context.sourceSize = max(_RetroPSXSourceSize.xy, 1.0);
    context.sourceTexelSize = _RetroPSXSourceSize.zw;
    return context;
}

int2 RetroPSX_GetCanonicalPixel(float4 positionCS)
{
    RetroPSXRasterContext context = RetroPSX_GetRasterContext();
    return int2(floor(positionCS.xy * context.internalSize / context.sourceSize));
}

int2 RetroPSXCanonicalPixel(float4 positionCS)
{
    return RetroPSX_GetCanonicalPixel(positionCS);
}

#endif
