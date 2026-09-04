Shader "Hidden/RetroPSX/Presentation"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off
        Pass
        {
            Name "Point Presentation"
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _RetroPresentationRect;
            half4 _RetroLetterboxColor;
            float _RetroPreserveAlpha;

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 minimum = _RetroPresentationRect.xy;
                float2 maximum = minimum + _RetroPresentationRect.zw;
                if (any(uv < minimum) || any(uv > maximum))
                    return half4(_RetroLetterboxColor.rgb, _RetroPreserveAlpha > 0.5 ? _RetroLetterboxColor.a : 1.0);
                float2 imageUV = saturate((uv - minimum) / max(_RetroPresentationRect.zw, 1e-5));
                half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, imageUV);
                return half4(color.rgb, _RetroPreserveAlpha > 0.5 ? color.a : 1.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
