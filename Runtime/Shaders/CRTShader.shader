Shader "PostEffect/CRTShader"
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

    float _ScanlinesWeight;
    float _NoiseWeight;

    float _ScreenBendX;
    float _ScreenBendY;
    float _VignetteAmount;
    float _VignetteSize;
    float _VignetteRounding;
    float _VignetteSmoothing;

    float _ScanLinesDensity;
    float _ScanLinesSpeed;
    float _NoiseAmount;
    half2 _ChromaticRed;
    half2 _ChromaticGreen;
    half2 _ChromaticBlue;

    float _GrilleOpacity;
    float _GrilleCounterOpacity;
    float _GrilleResolution;
    float _GrilleCounterResolution;
    float _GrilleBrightness;
    float _GrilleUvRotation;
    float _GrilleUvMidPoint;
    float3 _GrilleShift;

    float ComputeDistance(float depth)
    {
        float dist = depth * _ProjectionParams.z;
        dist -= _ProjectionParams. y * 10;
        return dist;
    }

    float scanLines(float2 uv, float repetation, float speed)
    {
        return sin(uv.y * repetation + _Time. z * speed);
    }

    float random(float2 uv)
    {
        return frac(sin(dot(uv, float2(15.5151, 42.2561))) * 12341.14122 * sin(_Time.y * 0.03));
    }

    float noise(float2 uv)
    {
        float2 i = floor(uv);
        float2 f = frac(uv);

        float a = random(i);
        float b = random(i + float2(1.0, 0.0));
        float c = random(i + float2(0.0, 1.0));
        float d = random(i + float2(1.0, 0.0));

        float2 u = smoothstep(0.0, 1.0, f);
        return lerp(a, b, u. x) + (c - a) * u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
    }

    float2 rotateUV(float2 uv, float rotation, float mid)
    {
        return float2(
            cos(rotation) * (uv.x - mid) + sin(rotation) * (uv.y - mid) + mid,
            cos(rotation) * (uv.y - mid) - sin(rotation) * (uv.x - mid) + mid
        );
    }

    half4 Frag(Varyings input) : SV_Target
    {
        float2 uv = input. texcoord;
        float2 originalUV = uv;

        // Depth
        float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
        float linearDepth = Linear01Depth(depth, _ZBufferParams);
        float dist = ComputeDistance(depth);
        half fog = saturate(exp2(1 * dist));

        // Screen bend
        uv -= 0.5;
        uv *= 2.0;
        uv.x *= 1.0 + pow(abs(uv.y) / _ScreenBendX, 2.0);
        uv.y *= 1.0 + pow(abs(uv.x) / _ScreenBendY, 2.0);
        uv /= 2;
        uv += 0.5;

        // Vignette
        float shiftUVx = (uv.x - 0.5) * _VignetteSize;
        float shiftUVy = (uv. y - 0.5) * _VignetteSize;
        float vignetteAmount = 1.0 - (sqrt(
            pow(abs(shiftUVx), _VignetteRounding) + pow(abs(shiftUVy), _VignetteRounding)) * (_VignetteAmount));
        vignetteAmount = smoothstep(0.0, _VignetteSmoothing, vignetteAmount);

        // Scanlines
        float scanlines = _ScanlinesWeight * (scanLines(uv, _ScanLinesDensity, _ScanLinesSpeed));

        // Noise
        float valNoise = _NoiseWeight * (noise(uv * _NoiseAmount));

        // Chromatic aberration
        half2 redShift = _ChromaticRed;
        half2 greenShift = _ChromaticGreen;
        half2 blueShift = _ChromaticBlue;

        half4 textureColor = half4(
            SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + redShift).r,
            SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + greenShift). g,
            SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv + blueShift).b,
            SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv).a
        );
        half4 mainTextureColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, originalUV);

        // Grille UV rotation
        float grilleUvRotationRadians = _GrilleUvRotation * 3.14159265 / 180.0;
        float2 grilleUv = rotateUV(originalUV, grilleUvRotationRadians, _GrilleUvMidPoint);
        float2 grilleUvCounter = rotateUV(originalUV, grilleUvRotationRadians * 2, _GrilleUvMidPoint);

        // Grille effect
        float4 grilleEffect = float4(mainTextureColor.xyz, _GrilleOpacity);

        float g_r = smoothstep(0.85, 0.95, abs(sin(_GrilleShift. x + grilleUv.x * (_GrilleResolution * 3.14159265))));
        float g_r_c = smoothstep(0.85, 0.95, abs(sin(_GrilleShift.x + grilleUvCounter.x * (_GrilleCounterResolution * 3.14159265))));
        grilleEffect.x = lerp(grilleEffect.x, grilleEffect.x * g_r, _GrilleOpacity) * lerp(grilleEffect.x, grilleEffect.x * g_r_c, _GrilleCounterOpacity);
        grilleEffect.x = clamp(grilleEffect.x * _GrilleBrightness, 0.0, 1.0);

        float g_g = smoothstep(0.85, 0.95, abs(sin(_GrilleShift.y + 1.05 + grilleUv.x * (_GrilleResolution * 3.14159265))));
        float g_g_c = smoothstep(0.85, 0.95, abs(sin(_GrilleShift.y + 1.05 + grilleUvCounter.x * (_GrilleCounterResolution * 3.14159265))));
        grilleEffect.y = lerp(grilleEffect.y, grilleEffect.y * g_g, _GrilleOpacity) * lerp(grilleEffect.y, grilleEffect. y * g_g_c, _GrilleCounterOpacity);
        grilleEffect.y = clamp(grilleEffect.y * _GrilleBrightness, 0.0, 1.0);

        float b_b = smoothstep(0.85, 0.95, abs(sin(_GrilleShift.z + 2.1 + grilleUv.x * (_GrilleResolution * 3.14159265))));
        float b_b_c = smoothstep(0.85, 0.95, abs(sin(_GrilleShift.z + 2.1 + grilleUvCounter.x * (_GrilleCounterResolution * 3.14159265))));
        grilleEffect.z = lerp(grilleEffect.z, grilleEffect.z * b_b, _GrilleOpacity) * lerp(grilleEffect.z, grilleEffect. z * b_b_c, _GrilleCounterOpacity);
        grilleEffect.z = clamp(grilleEffect.z * _GrilleBrightness, 0.0, 1.0);

        // Final color
        float4 color = lerp(lerp(textureColor, scanlines, 0.05), valNoise, 0.15) * fog * vignetteAmount;
        return grilleEffect * color;
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off Cull Off ZTest Always

        Pass
        {
            Name "CRT Effect"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDHLSL
        }
    }
}