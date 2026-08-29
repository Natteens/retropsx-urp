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

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.texcoord;
                float2 minimum = _RetroPresentationRect.xy;
                float2 maximum = minimum + _RetroPresentationRect.zw;
                if (any(uv < minimum) || any(uv > maximum))
                    return _RetroLetterboxColor;
                float2 imageUV = saturate((uv - minimum) / max(_RetroPresentationRect.zw, 1e-5));
                return SAMPLE_TEXTURE2D(_BlitTexture, sampler_PointClamp, imageUV);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
