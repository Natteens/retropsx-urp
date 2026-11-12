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
            #include "../Includes/RetroPSXFinalColor.hlsl"

            float _RetroPreserveAlpha;

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
                color = RetroPSX_ApplyFinalImagePrecision(color, pixel);
                half3 precise = color;
                // Fully transparent texels of alpha-preserving targets keep their RGB.
                if (_RetroPreserveAlpha < 0.5 || original.a > 0.0)
                    color = RetroPSX_ApplyArtisticColor(color, pixel);

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
                    color = abs(original.rgb - precise) * 8.0;
                else if (_RetroPSXDebugMode == 4)
                    color = RetroPSX_FinalImageDither(max(ditherMode, 1), pixel) * 16.0 + 0.5;
                else if (_RetroPSXDebugMode == 5)
                    color = fogFactor.xxx;
                else if (_RetroPSXDebugMode == 8)
                {
                    float rawDepth = SampleSceneDepth(uv);
                    float3 positionWS = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                    color = frac(abs(positionWS) * 0.1);
                }
                else if (_RetroPSXDebugMode == 11)
                    color = abs(precise - color) * 4.0;
                else if (_RetroPSXDebugMode == 12)
                    color = RetroPSX_ArtisticDitherThreshold(pixel).xxx;
                return half4(color, _RetroPreserveAlpha > 0.5 ? original.a : 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
