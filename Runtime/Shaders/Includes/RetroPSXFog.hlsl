#ifndef RETROPSX_FOG_INCLUDED
#define RETROPSX_FOG_INCLUDED

#include "RetroPSXCore.hlsl"

float RetroPSX_GetFogFactor(float3 positionWS)
{
    if (_RetroPSXFogParams.x < 0.5)
        return 0.0;

    float distanceToCamera = distance(_WorldSpaceCameraPos, positionWS);
    float factor = saturate((distanceToCamera - _RetroPSXFogParams.y) / max(_RetroPSXFogParams.z - _RetroPSXFogParams.y, 1e-4));
    if (_RetroPSXFogParams.x > 2.5)
    {
        float steps = max(_RetroPSXFogParams.w, 2.0);
        factor = round(factor * steps) / steps;
    }
    return factor;
}

half3 RetroPSX_ApplyFog(half3 color, float factor, float materialStrength)
{
    if (_RetroPSXFogParams.x < 0.5 || materialStrength <= 0.0)
        return color;

    factor = saturate(factor * _RetroPSXFogStrength * materialStrength);
    if (_RetroPSXFogParams.x > 1.5 && _RetroPSXFogParams.x < 2.5)
        return color * lerp(half3(1.0, 1.0, 1.0), _RetroPSXFogColor.rgb, factor);
    return lerp(color, _RetroPSXFogColor.rgb, factor);
}

float RetroPSXFogFactor(float3 positionWS)
{
    return RetroPSX_GetFogFactor(positionWS);
}

half3 RetroPSXApplyFog(half3 color, float factor)
{
    return RetroPSX_ApplyFog(color, factor, 1.0);
}

#endif
