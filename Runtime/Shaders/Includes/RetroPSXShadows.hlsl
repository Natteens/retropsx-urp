#ifndef RETROPSX_SHADOWS_INCLUDED
#define RETROPSX_SHADOWS_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Shadow/ShadowSamplingTent.hlsl"

#define RETROPSX_MAX_SHADOW_CASCADES 4
#define RETROPSX_BEYOND_SHADOW_FAR(shadowCoord) shadowCoord.z <= 0.0 || shadowCoord.z >= 1.0

TEXTURE2D_X(_ScreenSpaceShadowmapTexture);
TEXTURE2D_SHADOW(_MainLightShadowmapTexture);
TEXTURE2D_SHADOW(_AdditionalLightsShadowmapTexture);
SAMPLER_CMP(sampler_LinearClampCompare);

#ifndef LIGHT_SHADOWS_NO_CBUFFER
CBUFFER_START(LightShadows)
#endif
float4x4 _MainLightWorldToShadow[RETROPSX_MAX_SHADOW_CASCADES + 1];
float4 _CascadeShadowSplitSpheres0;
float4 _CascadeShadowSplitSpheres1;
float4 _CascadeShadowSplitSpheres2;
float4 _CascadeShadowSplitSpheres3;
float4 _CascadeShadowSplitSphereRadii;
float4 _MainLightShadowOffset0;
float4 _MainLightShadowOffset1;
float4 _MainLightShadowParams;
float4 _MainLightShadowmapSize;
float4 _AdditionalShadowOffset0;
float4 _AdditionalShadowOffset1;
float4 _AdditionalShadowFadeParams;
float4 _AdditionalShadowmapSize;
#if defined(_ADDITIONAL_LIGHT_SHADOWS) && !USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
float4 _AdditionalShadowParams[MAX_VISIBLE_LIGHTS];
float4x4 _AdditionalLightsWorldToShadow[MAX_VISIBLE_LIGHTS];
#endif
#ifndef LIGHT_SHADOWS_NO_CBUFFER
CBUFFER_END
#endif

#if defined(_ADDITIONAL_LIGHT_SHADOWS) && USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
StructuredBuffer<float4> _AdditionalShadowParams_SSBO;
StructuredBuffer<float4x4> _AdditionalLightsWorldToShadow_SSBO;
#endif

struct RetroPSXShadowSamplingData
{
    half4 shadowOffset0;
    half4 shadowOffset1;
    float4 shadowmapSize;
};

RetroPSXShadowSamplingData GetRetroPSXMainLightShadowSamplingData()
{
    RetroPSXShadowSamplingData data;
    data.shadowOffset0 = half4(_MainLightShadowOffset0);
    data.shadowOffset1 = half4(_MainLightShadowOffset1);
    data.shadowmapSize = _MainLightShadowmapSize;
    return data;
}

RetroPSXShadowSamplingData GetRetroPSXAdditionalLightShadowSamplingData()
{
    RetroPSXShadowSamplingData data;
    data.shadowOffset0 = half4(_AdditionalShadowOffset0);
    data.shadowOffset1 = half4(_AdditionalShadowOffset1);
    data.shadowmapSize = _AdditionalShadowmapSize;
    return data;
}

half4 GetRetroPSXAdditionalLightShadowParams(int lightIndex)
{
#if defined(_ADDITIONAL_LIGHT_SHADOWS)
    #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
        return half4(_AdditionalShadowParams_SSBO[lightIndex]);
    #else
        return half4(_AdditionalShadowParams[lightIndex]);
    #endif
#else
    return half4(0.0, 0.0, 0.0, -1.0);
#endif
}

real SampleRetroPSXShadowmapLow(TEXTURE2D_SHADOW_PARAM(shadowMap, shadowSampler), float4 shadowCoord, RetroPSXShadowSamplingData data)
{
    real4 attenuation;
    attenuation.x = real(SAMPLE_TEXTURE2D_SHADOW(shadowMap, shadowSampler, shadowCoord.xyz + float3(data.shadowOffset0.xy, 0.0)));
    attenuation.y = real(SAMPLE_TEXTURE2D_SHADOW(shadowMap, shadowSampler, shadowCoord.xyz + float3(data.shadowOffset0.zw, 0.0)));
    attenuation.z = real(SAMPLE_TEXTURE2D_SHADOW(shadowMap, shadowSampler, shadowCoord.xyz + float3(data.shadowOffset1.xy, 0.0)));
    attenuation.w = real(SAMPLE_TEXTURE2D_SHADOW(shadowMap, shadowSampler, shadowCoord.xyz + float3(data.shadowOffset1.zw, 0.0)));
    return dot(attenuation, real(0.25));
}

real SampleRetroPSXShadowmapMedium(TEXTURE2D_SHADOW_PARAM(shadowMap, shadowSampler), float4 shadowCoord, RetroPSXShadowSamplingData data)
{
    float weights[9];
    float2 sampleUV[9];
    SampleShadow_ComputeSamples_Tent_Filter_5x5(float, data.shadowmapSize, shadowCoord, weights, sampleUV);
    real attenuation = 0.0;
    UNITY_UNROLL
    for (int sampleIndex = 0; sampleIndex < 9; sampleIndex++)
        attenuation += weights[sampleIndex] * SAMPLE_TEXTURE2D_SHADOW(shadowMap, shadowSampler, float3(sampleUV[sampleIndex], shadowCoord.z));
    return attenuation;
}

real SampleRetroPSXShadowmapHigh(TEXTURE2D_SHADOW_PARAM(shadowMap, shadowSampler), float4 shadowCoord, RetroPSXShadowSamplingData data)
{
    float weights[16];
    float2 sampleUV[16];
    SampleShadow_ComputeSamples_Tent_Filter_7x7(float, data.shadowmapSize, shadowCoord, weights, sampleUV);
    real attenuation = 0.0;
    UNITY_UNROLL
    for (int sampleIndex = 0; sampleIndex < 16; sampleIndex++)
        attenuation += weights[sampleIndex] * SAMPLE_TEXTURE2D_SHADOW(shadowMap, shadowSampler, float3(sampleUV[sampleIndex], shadowCoord.z));
    return attenuation;
}

real SampleRetroPSXShadowmap(TEXTURE2D_SHADOW_PARAM(shadowMap, shadowSampler), float4 shadowCoord, RetroPSXShadowSamplingData data, half shadowStrength)
{
    real attenuation;
#if defined(_SHADOWS_SOFT_LOW)
    attenuation = SampleRetroPSXShadowmapLow(TEXTURE2D_SHADOW_ARGS(shadowMap, shadowSampler), shadowCoord, data);
#elif defined(_SHADOWS_SOFT_MEDIUM)
    attenuation = SampleRetroPSXShadowmapMedium(TEXTURE2D_SHADOW_ARGS(shadowMap, shadowSampler), shadowCoord, data);
#elif defined(_SHADOWS_SOFT_HIGH)
    attenuation = SampleRetroPSXShadowmapHigh(TEXTURE2D_SHADOW_ARGS(shadowMap, shadowSampler), shadowCoord, data);
#else
    attenuation = real(SAMPLE_TEXTURE2D_SHADOW(shadowMap, shadowSampler, shadowCoord.xyz));
#endif
    attenuation = LerpWhiteTo(attenuation, shadowStrength);
    return RETROPSX_BEYOND_SHADOW_FAR(shadowCoord) ? 1.0 : attenuation;
}

half RetroPSXComputeCascadeIndex(float3 positionWS)
{
    float3 fromCenter0 = positionWS - _CascadeShadowSplitSpheres0.xyz;
    float3 fromCenter1 = positionWS - _CascadeShadowSplitSpheres1.xyz;
    float3 fromCenter2 = positionWS - _CascadeShadowSplitSpheres2.xyz;
    float3 fromCenter3 = positionWS - _CascadeShadowSplitSpheres3.xyz;
    float4 distancesSquared = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));
    half4 weights = half4(distancesSquared < _CascadeShadowSplitSphereRadii);
    weights.yzw = saturate(weights.yzw - weights.xyz);
    return half(4.0) - dot(weights, half4(4.0, 3.0, 2.0, 1.0));
}

float4 TransformWorldToRetroPSXShadowCoord(float3 positionWS)
{
#if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    return float4(ComputeNormalizedDeviceCoordinatesWithZ(positionWS, GetWorldToHClipMatrix()), 1.0);
#else
    #if defined(_MAIN_LIGHT_SHADOWS_CASCADE)
        half cascadeIndex = RetroPSXComputeCascadeIndex(positionWS);
    #else
        half cascadeIndex = half(0.0);
    #endif
    return float4(mul(_MainLightWorldToShadow[cascadeIndex], float4(positionWS, 1.0)).xyz, 0.0);
#endif
}

half SampleRetroPSXScreenSpaceShadow(float4 shadowCoord)
{
    float safeW = shadowCoord.w >= 0.0 ? max(shadowCoord.w, 1e-5) : min(shadowCoord.w, -1e-5);
    shadowCoord.xy /= safeW;
    shadowCoord.xy = UnityStereoTransformScreenSpaceTex(shadowCoord.xy);
#if defined(UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION)
    shadowCoord.xy = RemovePretransformRotation(shadowCoord.xy);
#endif
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    return SAMPLE_TEXTURE2D_ARRAY(_ScreenSpaceShadowmapTexture, sampler_PointClamp, shadowCoord.xy, unity_StereoEyeIndex).x;
#else
    return half(SAMPLE_TEXTURE2D(_ScreenSpaceShadowmapTexture, sampler_PointClamp, shadowCoord.xy).x);
#endif
}

half SampleRetroPSXMainLightShadow(float3 positionWS)
{
#if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
    float4 shadowCoord = TransformWorldToRetroPSXShadowCoord(positionWS);
    #if defined(_MAIN_LIGHT_SHADOWS_SCREEN)
        return SampleRetroPSXScreenSpaceShadow(shadowCoord);
    #else
        return SampleRetroPSXShadowmap(
            TEXTURE2D_SHADOW_ARGS(_MainLightShadowmapTexture, sampler_LinearClampCompare),
            shadowCoord,
            GetRetroPSXMainLightShadowSamplingData(),
            half(_MainLightShadowParams.x));
    #endif
#else
    return 1.0;
#endif
}

half SampleRetroPSXAdditionalLightShadow(float3 positionWS, half3 lightDirection, half4 shadowParams)
{
#if defined(_ADDITIONAL_LIGHT_SHADOWS)
    int shadowSliceIndex = (int)shadowParams.w;
    if (shadowSliceIndex < 0)
        return 0.0;
    if (shadowSliceIndex >= MAX_VISIBLE_LIGHTS)
        return 0.0;
    if (shadowParams.z > 0.5)
        shadowSliceIndex += CubeMapFaceID(-lightDirection);
    if (shadowSliceIndex >= MAX_VISIBLE_LIGHTS)
        return 0.0;

    #if USE_STRUCTURED_BUFFER_FOR_LIGHT_DATA
        float4 shadowCoord = mul(_AdditionalLightsWorldToShadow_SSBO[shadowSliceIndex], float4(positionWS, 1.0));
    #else
        float4 shadowCoord = mul(_AdditionalLightsWorldToShadow[shadowSliceIndex], float4(positionWS, 1.0));
    #endif
    float safeW = shadowCoord.w >= 0.0 ? max(shadowCoord.w, 1e-5) : min(shadowCoord.w, -1e-5);
    shadowCoord.xyz /= safeW;
    return SampleRetroPSXShadowmap(
        TEXTURE2D_SHADOW_ARGS(_AdditionalLightsShadowmapTexture, sampler_LinearClampCompare),
        shadowCoord,
        GetRetroPSXAdditionalLightShadowSamplingData(),
        shadowParams.x);
#else
    return 1.0;
#endif
}

#endif
