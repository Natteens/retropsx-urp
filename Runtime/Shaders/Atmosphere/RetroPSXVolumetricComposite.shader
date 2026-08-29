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
            #include "../Includes/RetroPSXCommon.hlsl"

            float FinalDither(int mode, int2 pixel)
            {
                if (mode <= 3)
                    return RetroPSX_DitherValue(mode, pixel);
                float2 patternUV = (pixel + 0.5) * _RetroPSXInternalSize.zw;
                if (mode == 4)
                    return (SAMPLE_TEXTURE2D(_RetroCustomDither, sampler_RetroCustomDither, patternUV).r - 0.5) / 31.0;
                if (mode == 5)
                    return (SAMPLE_TEXTURE2D(_RetroBlueNoise, sampler_RetroBlueNoise, patternUV * 0.5).r - 0.5) / 31.0;
                return 0.0;
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
                half4 centerVolume = SAMPLE_TEXTURE2D(_RetroVolumeTexture, sampler_PointClamp, uv);
                half4 volume = centerVolume;
                float weightSum = 1.0;
                float centerLuminance = ScatteringLuminance(centerVolume.rgb);
                static const float2 offsets[5] = {
                    float2(0,0), float2(-1,0), float2(1,0), float2(0,-1), float2(0,1) };
                [unroll]
                for (int index = 1; index < 5; index++)
                {
                    float2 sampleUV = uv + offsets[index] * _RetroVolumeTexelSize.xy;
                    float sampleDepth = EyeDepth(sampleUV);
                    float relativeDepthDelta = abs(sampleDepth - centerDepth) / max(min(sampleDepth, centerDepth), 0.25);
                    half4 sampleVolume = SAMPLE_TEXTURE2D(_RetroVolumeTexture, sampler_PointClamp, sampleUV);

                    // Camera depth alone cannot identify a light-space shadow edge.
                    // Preserve sharp visibility changes by also rejecting neighbors
                    // whose scattering or transmittance differs from the center texel.
                    float sampleLuminance = ScatteringLuminance(sampleVolume.rgb);
                    float relativeScatteringDelta = abs(sampleLuminance - centerLuminance)
                        / max(max(sampleLuminance, centerLuminance), 0.02);
                    float transmittanceDelta = abs(sampleVolume.a - centerVolume.a);
                    float depthWeight = exp(-relativeDepthDelta * _RetroVolumeParams3.y * 8.0);
                    float visibilityEdgeWeight = exp(-relativeScatteringDelta * 8.0 - transmittanceDelta * 12.0);
                    float weight = depthWeight * visibilityEdgeWeight;
                    volume += sampleVolume * weight;
                    weightSum += weight;
                }
                volume /= max(weightSum, 1e-4);
                if (_RetroPSXDebugMode == 7)
                    return half4(volume.rgb, 1.0);
                if (_RetroPSXDebugMode == 10)
                    return half4(volume.rgb, 1.0);
                if (_RetroPSXDebugMode == 6)
                    return half4((1.0 - volume.a).xxx, 1.0);
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
                return half4(composite, baseColor.a);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
