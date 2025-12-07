Shader "PostEffect/Pixelation"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    float _WidthPixelation;
    float _HeightPixelation;
    float _ColorPrecision;

    half4 Frag(Varyings input) : SV_Target
    {
        float2 uv = input.texcoord;
        
        // Pixelation
        uv. x = floor(uv.x * _WidthPixelation) / _WidthPixelation;
        uv.y = floor(uv.y * _HeightPixelation) / _HeightPixelation;

        half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
        
        // Color precision
        color = floor(color * _ColorPrecision) / _ColorPrecision;
        
        return color;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "Pixelation Effect"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}