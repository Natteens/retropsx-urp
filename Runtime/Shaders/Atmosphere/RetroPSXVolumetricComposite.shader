Shader "Hidden/RetroPSX/VolumetricComposite"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            Name "Depth Aware Composite"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            TEXTURE2D(_RetroVolumeTexture);
            SAMPLER(sampler_RetroVolumeTexture);
            TEXTURE2D(_RetroCustomDither);
            SAMPLER(sampler_RetroCustomDither);
            TEXTURE2D(_RetroBlueNoise);
            SAMPLER(sampler_RetroBlueNoise);
            float4 _RetroVolumeTexelSize;
            float4 _RetroVolumeParams3;
            float4 _RetroFinalColorParams;
            float _RetroPreserveAlpha;
            #include "../Includes/RetroPSXCommon.hlsl"

            float FinalDither(int mode, int2 pixel)
            {
                float value = 0.0;
                if (mode <= 3)
                    value = RetroPSX_DitherValue(mode, pixel);
                else
                {
                    float2 patternUV = (pixel + 0.5) * _RetroPSXInternalSize.zw;
                    if (mode == 4)
                        value = (SAMPLE_TEXTURE2D(_RetroCustomDither, sampler_RetroCustomDither, patternUV).r - 0.5) / 31.0;
                    else if (mode == 5)
                        value = (SAMPLE_TEXTURE2D(_RetroBlueNoise, sampler_RetroBlueNoise, patternUV * 0.5).r - 0.5) / 31.0;
                }
                return value;
            }

            float EyeDepth(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            float ScatteringLuminance(half3 color)
            {
                return dot(color, half3(0.2126, 0.7152, 0.0722));
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 baseColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, uv);
                float centerDepth = EyeDepth(uv);
                // Match each volume sample to the exact depth UV used by its raymarch.
                // The destination UV can lie on an object while the containing low-res
                // texel was raymarched through the background.
                float2 volumePixel = floor(uv * _RetroVolumeTexelSize.zw);
                half4 samples[9];
                float depthDeltas[9];
                float nearestDelta = 1e20;
                int nearestIndex = 0;
                [unroll]
                for (int index = 0; index < 9; index++)
                {
                    float2 offset = float2(index % 3 - 1, index / 3 - 1);
                    float2 samplePixel = clamp(volumePixel + offset, 0.0, _RetroVolumeTexelSize.zw - 1.0);
                    float2 sampleUV = (samplePixel + 0.5) * _RetroVolumeTexelSize.xy;
                    float sampleDepth = EyeDepth(sampleUV);
                    depthDeltas[index] = abs(sampleDepth - centerDepth) / max(min(sampleDepth, centerDepth), 0.25);
                    samples[index] = SAMPLE_TEXTURE2D(_RetroVolumeTexture, sampler_PointClamp, sampleUV);
                    // Prefer the central sample when depths tie, retaining shadow edges.
                    if (depthDeltas[index] < nearestDelta || (index == 4 && depthDeltas[index] <= nearestDelta))
                    {
                        nearestDelta = depthDeltas[index];
                        nearestIndex = index;
                    }
                }
                half4 referenceVolume = samples[nearestIndex];
                float referenceLuminance = ScatteringLuminance(referenceVolume.rgb);
                half4 volume = 0;
                float weightSum = 0.0;
                [unroll]
                for (int index = 0; index < 9; index++)
                {
                    half4 sampleVolume = samples[index];
                    float sampleLuminance = ScatteringLuminance(sampleVolume.rgb);
                    float relativeScatteringDelta = abs(sampleLuminance - referenceLuminance)
                        / max(max(sampleLuminance, referenceLuminance), 0.02);
                    float transmittanceDelta = abs(sampleVolume.a - referenceVolume.a);
                    float depthWeight = exp(-depthDeltas[index] * _RetroVolumeParams3.y * 8.0);
                    float visibilityEdgeWeight = exp(-relativeScatteringDelta * 8.0 - transmittanceDelta * 12.0);
                    float weight = depthWeight * visibilityEdgeWeight;
                    volume += sampleVolume * weight;
                    weightSum += weight;
                }
                // A thin surface may have no corresponding low-res sample. Never
                // substitute unrelated background scattering for that surface.
                volume = weightSum > 1e-4 ? volume / weightSum : half4(0.0, 0.0, 0.0, 1.0);
                if (_RetroPSXDebugMode == 7)
                    return half4(volume.rgb, _RetroPreserveAlpha > 0.5 ? baseColor.a : 1.0);
                if (_RetroPSXDebugMode == 10)
                    return half4(volume.rgb, _RetroPreserveAlpha > 0.5 ? baseColor.a : 1.0);
                if (_RetroPSXDebugMode == 6)
                    return half4((1.0 - volume.a).xxx, _RetroPreserveAlpha > 0.5 ? baseColor.a : 1.0);
                half3 composite = baseColor.rgb * volume.a + volume.rgb;
                if (_RetroFinalColorParams.x > 0.5)
                {
                    int2 pixel = int2(input.positionCS.xy);
                    float3 srgb = LinearToSRGB(saturate((float3)composite));
                    srgb += FinalDither((int)_RetroFinalColorParams.y, pixel) * _RetroFinalColorParams.z;
                    float3 levels = exp2(_RetroPSXColorBits.xyz) - 1.0;
                    srgb = round(saturate(srgb) * levels) / levels;
                    composite = (half3)SRGBToLinear(srgb);
                }
                return half4(composite, _RetroPreserveAlpha > 0.5 ? baseColor.a : 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
