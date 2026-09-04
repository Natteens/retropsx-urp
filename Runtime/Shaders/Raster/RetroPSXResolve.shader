Shader "Hidden/RetroPSX/Resolve"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            Name "Canonical Resolve"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "../Includes/RetroPSXCommon.hlsl"

            TEXTURE2D(_RetroCustomDither);
            SAMPLER(sampler_RetroCustomDither);
            TEXTURE2D(_RetroBlueNoise);
            SAMPLER(sampler_RetroBlueNoise);
            float4 _RetroFinalColorParams;
            float _RetroPreserveAlpha;

            float ResolveDither(int mode, int2 pixel)
            {
                float value = 0.0;
                if (mode <= 3)
                    value = RetroPSXDitherValue(mode, pixel);
                else
                {
                    float2 uv = (pixel + 0.5) * _RetroPSXInternalSize.zw;
                    if (mode == 4)
                        value = (SAMPLE_TEXTURE2D(_RetroCustomDither, sampler_RetroCustomDither, uv).r - 0.5) / 31.0;
                    else if (mode == 5)
                        value = (SAMPLE_TEXTURE2D(_RetroBlueNoise, sampler_RetroBlueNoise, uv * 0.5).r - 0.5) / 31.0;
                }
                return value;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                half4 original = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, uv);
                half3 color = original.rgb;
                float fogFactor = 0.0;

                if (_RetroFinalColorParams.w > 0.5 || _RetroPSXDebugMode == 5)
                {
                    float rawDepth = SampleSceneDepth(uv);
                    float3 positionWS = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                    fogFactor = RetroPSXFogFactor(positionWS);
                    if (_RetroFinalColorParams.w > 0.5)
                        color = RetroPSXApplyFog(color, fogFactor);
                }

                int2 pixel = int2(input.positionCS.xy);
                int ditherMode = (int)_RetroFinalColorParams.y;
                if (_RetroFinalColorParams.x > 0.5 && _RetroPSXColorBits.w > 0.5)
                {
                    float3 srgb = LinearToSRGB(saturate((float3)color));
                    srgb += ResolveDither(ditherMode, pixel) * _RetroFinalColorParams.z;
                    float3 levels = exp2(_RetroPSXColorBits.xyz) - 1.0;
                    srgb = round(saturate(srgb) * levels) / levels;
                    color = (half3)SRGBToLinear(srgb);
                }

                if (_RetroPSXDebugMode == 1)
                    color = half3(frac(pixel.x / 16.0), frac(pixel.y / 16.0), 0.15);
                else if (_RetroPSXDebugMode == 2)
                {
                    float checker = fmod(pixel.x + pixel.y, 2.0);
                    float majorGrid = step(fmod(pixel.x, 8.0), 0.5) + step(fmod(pixel.y, 8.0), 0.5);
                    color = majorGrid > 0.0
                        ? half3(1.0, 0.15, 0.05)
                        : lerp(half3(0.04, 0.05, 0.07), half3(0.28, 0.31, 0.34), checker);
                }
                else if (_RetroPSXDebugMode == 3)
                    color = abs(original.rgb - color) * 8.0;
                else if (_RetroPSXDebugMode == 4)
                    color = ResolveDither(max(ditherMode, 1), pixel) * 16.0 + 0.5;
                else if (_RetroPSXDebugMode == 5)
                    color = fogFactor.xxx;
                else if (_RetroPSXDebugMode == 8)
                {
                    float rawDepth = SampleSceneDepth(uv);
                    float3 positionWS = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                    color = frac(abs(positionWS) * 0.1);
                }
                return half4(color, _RetroPreserveAlpha > 0.5 ? original.a : 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
