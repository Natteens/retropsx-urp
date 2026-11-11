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
            #include "../Includes/RetroPSXColor.hlsl"
            #include "../Includes/RetroPSXShadows.hlsl"

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
            float4 _RetroLocalShaftShape[4];
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
                g = clamp(g, -0.8, 0.8);
                cosine = clamp(cosine, -1.0, 1.0);
                float g2 = g * g;
                float phaseBase = max(1.0 + g2 - 2.0 * g * cosine, 1e-4);
                return (1.0 - g2) / max(12.56637 * pow(phaseBase, 1.5), 1e-4);
            }

            float SamplePatternTexture(int index, float2 uv)
            {
                float value = 1.0;
                if (index == 0)
                    value = SAMPLE_TEXTURE2D_LOD(_RetroPattern0, sampler_RetroPattern0, uv, 0.0).r;
                else if (index == 1)
                    value = SAMPLE_TEXTURE2D_LOD(_RetroPattern1, sampler_RetroPattern1, uv, 0.0).r;
                else if (index == 2)
                    value = SAMPLE_TEXTURE2D_LOD(_RetroPattern2, sampler_RetroPattern2, uv, 0.0).r;
                else if (index == 3)
                    value = SAMPLE_TEXTURE2D_LOD(_RetroPattern3, sampler_RetroPattern3, uv, 0.0).r;
                return value;
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
                value = pow(saturate(value), max(parameters.y, 0.01));
                value = smoothstep(parameters.z - parameters.w, parameters.z + parameters.w, value);
                if (_RetroLocalPatternExtra[index].x > 0.5) value = 1.0 - value;
                float blinkRate = _RetroLocalPatternExtra[index].z;
                if (blinkRate > 0.0)
                    value *= step(frac(_Time.y * blinkRate), _RetroLocalPatternExtra[index].w);
                return value;
            }

            half4 LocalShadowParams(int lightIndex)
            {
                float4 style = _RetroLocalLightStylization[lightIndex];
                if (style.w < 0.5)
                    return half4(0.0, 0.0, 0.0, -2.0);
                // A light requesting shadows without an atlas slice contributes no scattering.
                return style.z >= 0.0 ? GetRetroPSXAdditionalLightShadowParams((int)style.z) : half4(0.0, 0.0, 0.0, -1.0);
            }

            float LocalShadow(float3 positionWS, float3 relative, half4 shadowParams)
            {
                if (shadowParams.w < -1.5) return 1.0;
                if (shadowParams.w < 0.0) return 0.0;
                return SampleRetroPSXAdditionalLightShadow(positionWS, SafeNormalize(-relative), shadowParams);
            }

            bool ClipShaft(float3 origin, float3 ray, float4 cone, inout float entry, inout float exit)
            {
                float sphereCenter = -dot(origin, ray);
                float3 closest = origin + sphereCenter * ray;
                float radiusSquared = 1.0 - dot(closest, closest);
                if (radiusSquared <= 0.0) return false;

                float radius = sqrt(radiusSquared);
                entry = max(entry, sphereCenter - radius);
                exit = min(exit, sphereCenter + radius);
                if (exit <= entry) return false;

                // Rebase within the range sphere before solving the double cone, then keep its forward lobe.
                origin += entry * ray;
                float interval = exit - entry;
                float height = dot(origin, cone.xyz);
                float slope = dot(ray, cone.xyz);
                float lower = 0.0;
                float upper = interval;
                if (height < 0.0)
                {
                    if (height + slope * interval <= 0.0) return false;
                    lower = -height / slope;
                }
                else if (height + slope * interval < 0.0)
                    upper = -height / slope;

                float cosineSquared = cone.w * cone.w;
                float a = slope * slope - cosineSquared;
                float b = height * slope - cosineSquared * dot(origin, ray);
                float c = height * height - cosineSquared * dot(origin, origin);
                float discriminant = b * b - a * c;
                if (abs(a) < 1e-7)
                {
                    if (abs(b) > 1e-7)
                    {
                        float root = -c / (2.0 * b);
                        if (height + slope * root >= 0.0)
                        {
                            if (b > 0.0) lower = max(lower, root);
                            else upper = min(upper, root);
                        }
                    }
                }
                else if (discriminant > 0.0)
                {
                    float q = -b - (b >= 0.0 ? 1.0 : -1.0) * sqrt(discriminant);
                    float first = q / a;
                    float second = c / q;
                    if (height + slope * first >= 0.0)
                    {
                        if (b >= 0.0) upper = min(upper, first);
                        else lower = max(lower, first);
                    }
                    if (height + slope * second >= 0.0)
                    {
                        if (b >= 0.0) lower = max(lower, second);
                        else upper = min(upper, second);
                    }
                }

                if (upper <= lower) return false;
                float3 midpoint = origin + (0.5 * (lower + upper)) * ray;
                if (dot(midpoint, cone.xyz) < cone.w * length(midpoint)) return false;
                exit = entry + upper;
                entry += lower;
                return true;
            }

            float3 MarchShaft(int index, float3 rayOrigin, float3 ray, float nearDistance, float farDistance,
                float fogDistance, float fogTransmittance, int steps, float dither,
                inout float visibilitySum, inout float visibilityWeight)
            {
                float4 source = _RetroLocalLightPosRange[index];
                float4 lightColor = _RetroLocalLightColorDensity[index];
                if (source.w <= 1e-4 || lightColor.w <= 0.0 || !any(lightColor.rgb > 0.0))
                    return 0.0;

                float4 patternExtra = _RetroLocalPatternExtra[index];
                if (patternExtra.z > 0.0 && frac(_Time.y * patternExtra.z) > patternExtra.w)
                    return 0.0;

                float4 cone = _RetroLocalLightDirAngle[index];
                float entry = nearDistance / source.w;
                float exit = farDistance / source.w;
                if (!ClipShaft((rayOrigin - source.xyz) / source.w, ray, cone, entry, exit))
                    return 0.0;

                half4 shadowParams = LocalShadowParams(index);
                if (shadowParams.w >= -1.5 && shadowParams.w < 0.0) return 0.0;
                float4 shape = _RetroLocalShaftShape[index];
                float tangent = sqrt(max(0.0, 1.0 - cone.w * cone.w)) / max(cone.w, 1e-4);
                float stepLength = (exit - entry) * source.w / steps;
                float distanceAlongRay = entry * source.w + 0.5 * stepLength;
                float bandCount = _RetroVolumeParams2.y;
                float bandOffset = dither * shape.w;
                float sharpness = _RetroLocalLightStylization[index].x;
                float patternStrength = _RetroLocalLightStylization[index].y;
                bool debugLight = _RetroPSXDebugMode == 10 && index == (int)round(_RetroVolumeParams3.z);
                // Approximate fog optical depth along the shaft using the existing atmosphere integral.
                float fogRate = -log(max(fogTransmittance, 1e-5)) / max(fogDistance, 1e-4);
                float throughput = 1.0;
                float integral = 0.0;

                [loop]
                for (int sampleIndex = 0; sampleIndex < steps; sampleIndex++, distanceAlongRay += stepLength)
                {
                    float3 positionWS = rayOrigin + distanceAlongRay * ray;
                    float3 relative = positionWS - source.xyz;
                    float distance01 = saturate(length(relative) / source.w);
                    float axial = dot(relative, cone.xyz);
                    float radial = saturate(length(relative - axial * cone.xyz) / max(axial * tangent, 1e-6));
                    float edge = 1.0 - smoothstep(1.0 - _RetroLocalLightParams[index].y, 1.0, radial);
                    float falloff = pow(1.0 - distance01, sharpness);
                    if (shape.y > 0.0) falloff *= smoothstep(0.0, shape.y, distance01);
                    if (shape.z > 0.0) falloff *= smoothstep(0.0, shape.z, 1.0 - distance01);
                    float attenuation = edge * falloff * (1.0 - radial * shape.x);
                    if (bandCount > 1.0)
                        attenuation = round(attenuation * bandCount + bandOffset) / bandCount;
                    if (attenuation <= 1e-4) continue;

                    attenuation *= lerp(1.0, Pattern(index, positionWS, relative, source.w, cone.xyz), patternStrength);
                    float opacity = 1.0 - exp(-0.045 * lightColor.w * attenuation * stepLength);
                    float weight = throughput * opacity;
                    float visibility = LocalShadow(positionWS, relative, shadowParams);
                    if (debugLight)
                    {
                        visibilitySum += visibility * weight;
                        visibilityWeight += weight;
                    }
                    integral += weight * visibility * exp(-fogRate * min(distanceAlongRay, fogDistance));
                    throughput *= 1.0 - opacity;
                    if (throughput < 0.01) break;
                }
                return lightColor.rgb * integral * _RetroVolumeParams1.y;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float rawDepth = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    float deviceDepth = rawDepth;
                    float nearDepth = 1.0;
                    bool opaque = rawDepth > 0.0;
                #else
                    float deviceDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
                    float nearDepth = UNITY_NEAR_CLIP_VALUE;
                    bool opaque = rawDepth < 1.0;
                #endif
                float3 endWS = ComputeWorldSpacePosition(uv, deviceDepth, UNITY_MATRIX_I_VP);
                float3 nearWS = ComputeWorldSpacePosition(uv, nearDepth, UNITY_MATRIX_I_VP);
                float3 rayOrigin = unity_OrthoParams.w > 0.5 ? nearWS : _WorldSpaceCameraPos;
                float3 ray = endWS - rayOrigin;
                float sceneDistance = length(ray);
                float rayLength = max(0.0, min(sceneDistance, _RetroVolumeParams0.y) - _RetroVolumeParams3.x);
                float3 rayDirection = ray / max(sceneDistance, 1e-4);
                int steps = max(1, (int)_RetroVolumeParams2.x);
                float stepLength = rayLength / steps;
                float jitter = (Hash12(input.positionCS.xy) - 0.5) * _RetroVolumeParams2.z;
                float3 positionWS = rayOrigin + rayDirection * (stepLength * (0.5 + jitter));
                float transmittance = 1.0;
                float3 scattering = 0.0;
                float visibilitySum = 0.0;
                float visibilityWeight = 0.0;

                [loop]
                for (int stepIndex = 0; stepIndex < steps && rayLength > 0.0 && _RetroVolumeParams0.x > 0.0; stepIndex++)
                {
                    float densityShape = exp(-max(0.0, positionWS.y - _RetroVolumeParams0.z) * _RetroVolumeParams0.w);
                    if (_RetroVolumeParams2.w > 1.0)
                        densityShape = round(densityShape * _RetroVolumeParams2.w) / _RetroVolumeParams2.w;
                    float density = _RetroVolumeParams0.x * densityShape;

                    float phase = PhaseHG(dot(-rayDirection, normalize(_RetroPSXMainLightDirection.xyz)), _RetroVolumeParams1.z);
                    float3 incoming = _RetroVolumeAmbient.rgb;
                    if (_RetroVolumeParams1.w > 1e-4)
                    {
                        float mainShadow = SampleRetroPSXMainLightShadow(positionWS);
                        incoming += _RetroPSXMainLightColor.rgb * phase * _RetroVolumeParams1.w * mainShadow;
                    }

                    [loop]
                    for (int lightIndex = 0; lightIndex < _RetroLocalLightCount; lightIndex++)
                    {
                        if (_RetroLocalLightParams[lightIndex].x > 1.5) continue;
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
                        float shadowVisibility = LocalShadow(positionWS, relative, LocalShadowParams(lightIndex));
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
                float nearDistance = length(nearWS - rayOrigin);
                float farDistance = sceneDistance - (opaque ? _RetroVolumeParams3.x : 0.0);
                if (farDistance > nearDistance)
                {
                    float dither = RetroPSX_DitherValue(3, int2(input.positionCS.xy)) * 31.0;
                    [loop]
                    for (int index = 0; index < _RetroLocalLightCount; index++)
                    {
                        if (_RetroLocalLightParams[index].x < 1.5) continue;
                        scattering += MarchShaft(index, rayOrigin, rayDirection, nearDistance, farDistance,
                            rayLength, transmittance, steps, dither, visibilitySum, visibilityWeight);
                    }
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
