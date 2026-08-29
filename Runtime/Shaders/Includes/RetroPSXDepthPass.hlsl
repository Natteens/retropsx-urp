#ifndef RETROPSX_DEPTH_PASS_INCLUDED
#define RETROPSX_DEPTH_PASS_INCLUDED

#include "RetroPSXCommon.hlsl"
#include "RetroPSXMaterialInput.hlsl"

struct DepthAttributes
{
    float4 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct DepthVaryings
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

DepthVaryings RetroDepthVertex(DepthAttributes input)
{
    DepthVaryings output = (DepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    VertexPositionInputs positions = GetVertexPositionInputs(input.positionOS.xyz);
    output.positionCS = RetroPSX_ApplyVertexPrecision(positions.positionCS, positions.positionWS, _GeometrySnapStrength);
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    return output;
}

float3 _LightDirection;
float3 _LightPosition;

DepthVaryings RetroShadowVertex(DepthAttributes input)
{
    DepthVaryings output = (DepthVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
#if defined(_CASTING_PUNCTUAL_LIGHT_SHADOW)
    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
#else
    float3 lightDirectionWS = _LightDirection;
#endif
    float4 clipPosition = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
    // URP clamps only directional shadow casters to the near plane. Clamping
    // punctual casters collapses their perspective depth and corrupts spot and
    // point shadow maps (the point cubemap shows up as bright face-shaped lobes).
    output.positionCS = ApplyShadowClamping(clipPosition);
    output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
    return output;
}

half4 RetroDepthFragment(DepthVaryings input) : SV_Target
{
    if (_AlphaClip > 0.5)
    {
        half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
        clip(alpha - _Cutoff);
    }
    return 0;
}

#endif
