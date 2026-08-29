Shader "Hidden/RetroPSX/VolumetricRaymarch"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            Name "Raymarch"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "../Includes/RetroPSXCore.hlsl"

            float4 _RetroVolumeParams0;
            float4 _RetroVolumeParams1;
            float4 _RetroVolumeParams2;
            float4 _RetroVolumeParams3;
            half4 _RetroVolumeAmbient;
            float4 _RetroPSXMainLightDirection;
            half4 _RetroPSXMainLightColor;
            int _RetroLocalLightCount;
            float4 _RetroLocalLightPosRange[4];
            float4 _RetroLocalLightDirAngle[4];
            float4 _RetroLocalLightColorDensity[4];
            float4 _RetroLocalLightParams[4];
            float4 _RetroLocalPatternTransform[4];
            float4 _RetroLocalPatternParams[4];
            float4 _RetroLocalPatternExtra[4];
            float4 _RetroLocalLightStylization[4];
            TEXTURE2D(_RetroPattern0); SAMPLER(sampler_RetroPattern0);
            TEXTURE2D(_RetroPattern1); SAMPLER(sampler_RetroPattern1);
            TEXTURE2D(_RetroPattern2); SAMPLER(sampler_RetroPattern2);
            TEXTURE2D(_RetroPattern3); SAMPLER(sampler_RetroPattern3);

            float Hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float PhaseHG(float cosine, float g)
            {
                float g2 = g * g;
                return (1.0 - g2) / max(12.56637 * pow(1.0 + g2 - 2.0 * g * cosine, 1.5), 1e-4);
            }

            float SamplePatternTexture(int index, float2 uv)
            {
                if (index == 0) return SAMPLE_TEXTURE2D(_RetroPattern0, sampler_RetroPattern0, uv).r;
                if (index == 1) return SAMPLE_TEXTURE2D(_RetroPattern1, sampler_RetroPattern1, uv).r;
                if (index == 2) return SAMPLE_TEXTURE2D(_RetroPattern2, sampler_RetroPattern2, uv).r;
                return SAMPLE_TEXTURE2D(_RetroPattern3, sampler_RetroPattern3, uv).r;
            }

            float Pattern(int index, float3 positionWS, float3 relative, float range, float3 direction)
            {
                int type = (int)_RetroLocalLightParams[index].z;
                if (type == 0)
                    return 1.0;

                float mapping = _RetroLocalPatternExtra[index].y;
                float2 uv;
                if (mapping < 0.5)
                    uv = positionWS.xz;
                else
                {
                    float3 referenceUp = abs(direction.y) > 0.95 ? float3(1, 0, 0) : float3(0, 1, 0);
                    float3 right = normalize(cross(referenceUp, direction));
                    float3 up = cross(direction, right);
                    uv = float2(dot(relative, right), dot(relative, up)) / max(range, 1e-3) + 0.5;
                }
                float4 transform = _RetroLocalPatternTransform[index];
                uv = uv * transform.xy + transform.zw;
                float distortion = _RetroLocalLightParams[index].w;
                uv += (float2(Hash12(positionWS.xy), Hash12(positionWS.zy)) - 0.5) * distortion * 0.08;
                float angle = _RetroLocalPatternParams[index].x;
                float2 centered = uv - 0.5;
                float sine = sin(angle);
                float cosine = cos(angle);
                uv = float2(centered.x * cosine - centered.y * sine, centered.x * sine + centered.y * cosine) + 0.5;

                float value = 1.0;
                if (type == 1) value = 0.5 + 0.5 * sin(uv.x * 6.28318);
                else if (type == 2) value = fmod(floor(uv.x * 8.0) + floor(uv.y * 8.0), 2.0);
                else if (type == 3) value = saturate(1.0 - length(uv - 0.5) * 2.0);
                else if (type == 4) value = Hash12(floor(uv * 64.0));
                else if (type == 5) value = SamplePatternTexture(index, uv);

                float4 parameters = _RetroLocalPatternParams[index];
                value = pow(saturate(value), parameters.y);
                value = smoothstep(parameters.z - parameters.w, parameters.z + parameters.w, value);
                if (_RetroLocalPatternExtra[index].x > 0.5) value = 1.0 - value;
                float blinkRate = _RetroLocalPatternExtra[index].z;
                if (blinkRate > 0.0)
                    value *= step(frac(_Time.y * blinkRate), _RetroLocalPatternExtra[index].w);
                return value;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float rawDepth = SampleSceneDepth(uv);
                float3 endWS = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float3 ray = endWS - _WorldSpaceCameraPos;
                float sceneDistance = length(ray);
                float rayLength = max(0.0, min(sceneDistance, _RetroVolumeParams0.y) - _RetroVolumeParams3.x);
                float3 rayDirection = ray / max(sceneDistance, 1e-4);
                int steps = max(1, (int)_RetroVolumeParams2.x);
                float stepLength = rayLength / steps;
                float jitter = (Hash12(input.positionCS.xy) - 0.5) * _RetroVolumeParams2.z;
                float3 positionWS = _WorldSpaceCameraPos + rayDirection * (stepLength * (0.5 + jitter));
                float transmittance = 1.0;
                float3 scattering = 0.0;
                float visibilitySum = 0.0;
                float visibilityWeight = 0.0;

                [loop]
                for (int stepIndex = 0; stepIndex < steps; stepIndex++)
                {
                    float densityShape = exp(-max(0.0, positionWS.y - _RetroVolumeParams0.z) * _RetroVolumeParams0.w);
                    if (_RetroVolumeParams2.w > 1.0)
                        densityShape = round(densityShape * _RetroVolumeParams2.w) / _RetroVolumeParams2.w;
                    float density = _RetroVolumeParams0.x * densityShape;

                    float phase = PhaseHG(dot(-rayDirection, normalize(_RetroPSXMainLightDirection.xyz)), _RetroVolumeParams1.z);
                    float3 incoming = _RetroVolumeAmbient.rgb;
                    if (_RetroVolumeParams1.w > 1e-4)
                    {
                        float mainShadow = MainLightRealtimeShadow(TransformWorldToShadowCoord(positionWS));
                        incoming += _RetroPSXMainLightColor.rgb * phase * _RetroVolumeParams1.w * mainShadow;
                    }

                    [loop]
                    for (int lightIndex = 0; lightIndex < _RetroLocalLightCount; lightIndex++)
                    {
                        float3 relative = positionWS - _RetroLocalLightPosRange[lightIndex].xyz;
                        float distanceToLight = length(relative);
                        float range = _RetroLocalLightPosRange[lightIndex].w;
                        if (distanceToLight >= range) continue;
                        float normalizedDistance = distanceToLight / max(range, 1e-3);
                        float attenuation = saturate(1.0 - normalizedDistance);
                        float sharpness = max(_RetroLocalLightStylization[lightIndex].x, 0.25);
                        // A point volume should read as scattering around a source, not as a visible
                        // translucent range sphere. Its additional falloff leaves a tighter core while
                        // retaining the authored sharpness control.
                        attenuation = pow(attenuation, sharpness + (_RetroLocalLightParams[lightIndex].x > 0.5 ? 0.0 : 1.0));
                        if (_RetroLocalLightParams[lightIndex].x > 0.5)
                        {
                            float cone = dot(normalize(relative), normalize(_RetroLocalLightDirAngle[lightIndex].xyz));
                            float outer = _RetroLocalLightDirAngle[lightIndex].w;
                            attenuation *= smoothstep(outer, min(1.0, outer + _RetroLocalLightParams[lightIndex].y), cone);
                        }
                        float pattern = Pattern(lightIndex, positionWS, relative, range, normalize(_RetroLocalLightDirAngle[lightIndex].xyz));
                        attenuation *= lerp(1.0, pattern, _RetroLocalLightStylization[lightIndex].y);
                        if (attenuation <= 1e-4)
                            continue;
                        int shadowIndex = (int)round(_RetroLocalLightStylization[lightIndex].z);
                        bool requiresLightShadow = _RetroLocalLightStylization[lightIndex].w > 0.5;
                        float shadowVisibility = 1.0;
                        if (requiresLightShadow)
                        {
                            // AdditionalLightRealtimeShadow deliberately returns one when
                            // URP has no shadow slice for an index. That fallback is correct
                            // for ordinary material lighting, but not for a separate direct
                            // volumetric term: it causes a light to shine through geometry
                            // whenever the current camera did not receive its atlas entry.
                            // Volumetric Shadows = Use Light Shadows therefore fails closed.
                            half4 shadowParams = half4(0.0, 0.0, 0.0, -1.0);
                            if (shadowIndex >= 0)
                                shadowParams = GetAdditionalLightShadowParams(shadowIndex);
                            if (shadowParams.w >= 0.0)
                                shadowVisibility = AdditionalLightRealtimeShadow(
                                    shadowIndex,
                                    positionWS,
                                    normalize(-relative),
                                    shadowParams,
                                    GetAdditionalLightShadowSamplingData(shadowIndex));
                            else
                                shadowVisibility = 0.0;
                        }
                        if (_RetroPSXDebugMode == 10 && lightIndex == (int)round(_RetroVolumeParams3.z))
                        {
                            float sampleWeight = attenuation * density * stepLength;
                            visibilitySum += shadowVisibility * sampleWeight;
                            visibilityWeight += sampleWeight;
                        }
                        if (shadowVisibility <= 1e-4)
                            continue;
                        attenuation *= shadowVisibility;
                        if (_RetroVolumeParams2.y > 1.0)
                            attenuation = round(attenuation * _RetroVolumeParams2.y) / _RetroVolumeParams2.y;
                        incoming += _RetroLocalLightColorDensity[lightIndex].rgb * attenuation * _RetroLocalLightColorDensity[lightIndex].w;
                    }

                    scattering += transmittance * incoming * density * _RetroVolumeParams1.y * stepLength;
                    transmittance *= exp(-density * _RetroVolumeParams1.x * stepLength);
                    if (transmittance < 0.01) break;
                    positionWS += rayDirection * stepLength;
                }
                if (_RetroPSXDebugMode == 10)
                {
                    float visibility = visibilityWeight > 1e-5 ? visibilitySum / visibilityWeight : 0.0;
                    return half4(visibility.xxx, 1.0);
                }
                return half4(scattering, transmittance);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
