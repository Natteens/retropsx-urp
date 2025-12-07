Shader "PostEffect/Fog"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

    TEXTURE2D(_CameraDepthTexture);
    SAMPLER(sampler_CameraDepthTexture);

    float _FogDensity;
    float _FogDistance;
    float4 _FogColor;
    float4 _AmbientColor;

    float _FogNear;
    float _FogFar;
    float _FogAltScale;
    float _FogThinning;

    float _NoiseScale;
    float _NoiseStrength;

    // Voronoi noise functions
    float2 voronoiRandom2(float2 p)
    {
        return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453);
    }

    float cnoise(float2 p)
    {
        float2 i_st = floor(p);
        float2 f_st = frac(p);

        float m_dist = 1.0;

        for (int y = -1; y <= 1; y++)
        {
            for (int x = -1; x <= 1; x++)
            {
                float2 neighbor = float2(float(x), float(y));
                float2 randomPoint = voronoiRandom2(i_st + neighbor);
                randomPoint = 0.5 + 0.5 * sin(_Time.y + 6.2831 * randomPoint);
                float2 diff = neighbor + randomPoint - f_st;
                float dist = length(diff);
                m_dist = min(m_dist, dist);
            }
        }

        return m_dist;
    }

    float ComputeDistance(float depth)
    {
        float dist = depth * _ProjectionParams.z;
        dist -= _ProjectionParams.y * _FogDistance;
        return dist;
    }

    half ComputeFog(float z, float density)
    {
        half fog = exp2(density * z);
        return saturate(fog);
    }

    half4 Frag(Varyings input) : SV_Target
    {
        float2 uv = input.texcoord;

        // Base texture
        half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);

        // Depth
        float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
        float dist = ComputeDistance(depth);
        float fog = 1.0 - ComputeFog(dist, _FogDensity);

        // Screen noise
        float2 screenPos = uv;
        float2 screenParam = _ScreenParams.xy;
        float screenNoise = cnoise(screenPos * screenParam / _NoiseScale);

        // Mix fog
        half4 foggedColor = lerp(color, _FogColor * _AmbientColor, saturate(fog + (screenNoise * _NoiseStrength)));

        return foggedColor;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "Fog Effect"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}