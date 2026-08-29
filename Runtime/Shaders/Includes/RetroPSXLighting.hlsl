#ifndef RETROPSX_LIGHTING_INCLUDED
#define RETROPSX_LIGHTING_INCLUDED

#include "RetroPSXCore.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

half3 RetroPSX_VertexLighting(float3 positionWS, half3 normalWS, float materialStrength)
{
    half3 result = _RetroPSXAmbientColor.rgb;
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
    half ndotl = saturate(dot(normalWS, mainLight.direction));
    result += mainLight.color * ndotl * mainLight.distanceAttenuation * mainLight.shadowAttenuation;

    uint count = min(GetAdditionalLightsCount(), (uint)_RetroPSXLightingParams.z);
    [loop]
    for (uint index = 0u; index < count; index++)
    {
        Light light = GetAdditionalLight(index, positionWS);
        half attenuation = light.distanceAttenuation * light.shadowAttenuation;
        result += light.color * saturate(dot(normalWS, light.direction)) * attenuation;
    }
    half profileStrength = _RetroPSXLightingParams.y * _RetroPSXLightingParams.w;
    return lerp(half3(1.0, 1.0, 1.0), result * profileStrength, saturate(materialStrength));
}

half3 RetroPSX_ModernLighting(float3 positionWS, half3 normalWS, float materialStrength)
{
    return RetroPSX_VertexLighting(positionWS, normalize(normalWS), materialStrength);
}

half3 RetroPSXVertexLighting(float3 positionWS, half3 normalWS)
{
    return RetroPSX_VertexLighting(positionWS, normalWS, 1.0);
}

half3 RetroPSXModernLighting(float3 positionWS, half3 normalWS)
{
    return RetroPSX_ModernLighting(positionWS, normalWS, 1.0);
}

#endif
