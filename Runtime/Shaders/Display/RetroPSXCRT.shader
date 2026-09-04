Shader "Hidden/RetroPSX/CRT"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off

        Pass
        {
            Name "CRT Display"

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _RetroCRTParams0;
            float4 _RetroCRTParams1;
            float4 _RetroCRTParams2;
            float _RetroPixelBloom;

            float Hash12(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 centered = uv * 2.0 - 1.0;
                float radius2 = dot(centered, centered);

                centered *= 1.0 + radius2 * _RetroCRTParams0.w;
                uv = centered * 0.5 + 0.5;
                uv = (uv - 0.5) * (1.0 + _RetroCRTParams1.x) + 0.5;

                if (any(uv < 0.0) || any(uv > 1.0))
                    return 0;

                float2 texel = _BlitTexture_TexelSize.xy;
                float chroma = _RetroCRTParams2.x * texel.x;

                half4 centerSample = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                half red = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(chroma, 0)).r;
                half green = centerSample.g;
                half blue = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - float2(chroma, 0)).b;
                half3 color = half3(red, green, blue);

                half3 neighbors =
                    SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + float2(texel.x, 0)).rgb +
                    SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv - float2(texel.x, 0)).rgb;

                color = lerp(color, (color * 2.0 + neighbors) * 0.25, saturate(_RetroCRTParams1.z));

                half luma = dot(color, half3(0.299, 0.587, 0.114));
                color = lerp(color, lerp(color, luma.xxx, 0.25), saturate(_RetroCRTParams1.w));

                float scan = lerp(1.0, 0.65, fmod(floor(input.positionCS.y), 2.0));
                color *= lerp(1.0, scan, _RetroCRTParams0.x);

                int maskMode = (int)_RetroCRTParams0.z;

                if (maskMode > 0)
                {
                    float phase = fmod(input.positionCS.x, 3.0);
                    half3 mask = phase < 1.0
                        ? half3(1.0, 0.75, 0.75)
                        : phase < 2.0
                            ? half3(0.75, 1.0, 0.75)
                            : half3(0.75, 0.75, 1.0);

                    if (maskMode == 1 && fmod(input.positionCS.y, 2.0) > 1.0)
                        mask = mask.bgr;

                    color *= lerp(half3(1.0, 1.0, 1.0), mask, _RetroCRTParams0.y);
                }

                float vignette = saturate(1.0 - radius2 * _RetroCRTParams1.y);
                color *= vignette;

                color += (Hash12(input.positionCS.xy + _Time.y) - 0.5) * _RetroCRTParams2.y;

                if (_RetroCRTParams2.w > 0.5)
                    color *= lerp(0.78, 1.0, step(0.5, frac((floor(input.positionCS.y) + floor(_Time.y * 60.0)) * 0.5)));

                color += max(color - 0.7, 0.0) * _RetroPixelBloom;
                color *= _RetroCRTParams2.z;

                return half4(max(color, 0.0), centerSample.a);
            }

            ENDHLSL
        }
    }

    Fallback Off
}