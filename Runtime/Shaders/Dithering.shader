Shader "PostEffect/Dithering"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    uint _PatternIndex;
    float _DitherThreshold;
    float _DitherStrength;
    float _DitherScale;

    float4x4 GetDitherPattern(uint index)
    {
        float4x4 pattern;

        if (index == 0)
        {
            pattern = float4x4(
                0, 1, 0, 1,
                1, 0, 1, 0,
                0, 1, 0, 1,
                1, 0, 1, 0
            );
        }
        else if (index == 1)
        {
            pattern = float4x4(
                0.23, 0.2, 0.6, 0.2,
                0.2, 0.43, 0.2, 0.77,
                0.88, 0.2, 0.87, 0.2,
                0.2, 0.46, 0.2, 0
            );
        }
        else if (index == 2)
        {
            pattern = float4x4(
                -4.0, 0.0, -3.0, 1.0,
                2.0, -2.0, 3.0, -1.0,
                -3.0, 1.0, -4.0, 0.0,
                3.0, -1.0, 2.0, -2.0
            );
        }
        else if (index == 3)
        {
            pattern = float4x4(
                1, 0, 0, 1,
                0, 1, 1, 0,
                0, 1, 1, 0,
                1, 0, 0, 1
            );
        }
        else
        {
            pattern = float4x4(
                1, 1, 1, 1,
                1, 1, 1, 1,
                1, 1, 1, 1,
                1, 1, 1, 1
            );
        }

        return pattern;
    }

    float PixelBrightness(float3 col)
    {
        return (col.r + col.g + col.b) / 3.0;
    }

    float Get4x4TexValue(float2 uv, float brightness, float4x4 pattern)
    {
        uint x = uint(uv.x) % 4;
        uint y = uint(uv.y) % 4;

        if ((brightness * _DitherThreshold) < pattern[x][y])
            return 0;
        else
            return 1;
    }

    half4 Frag(Varyings input) : SV_Target
    {
        float2 uv = input. texcoord;

        // Base texture
        half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);

        // Dithering
        float2 screenPos = input.texcoord;
        uint2 ditherCoordinate = screenPos * _ScreenParams.xy;
        ditherCoordinate /= uint(_DitherScale);

        float brightness = PixelBrightness(color.rgb);
        float4x4 ditherPattern = GetDitherPattern(_PatternIndex);
        float ditherPixel = Get4x4TexValue(float2(ditherCoordinate), brightness, ditherPattern);

        return color * ditherPixel;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "Dithering Effect"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}