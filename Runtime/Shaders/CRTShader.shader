Shader "PostEffect/CRTShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    sampler2D _MainTex;
    sampler2D _CameraDepthTexture;

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
        dist -= _ProjectionParams.y * 10;
        return dist;
    }

    float scanLines(float2 uv, float repetation, float speed)
    {
        return sin(uv.y * repetation + _Time.z * speed);
    }

    //This is a random function I stole from somewhere
    float random(float2 uv)
    {
        return frac(sin(dot(uv, float2(15.5151, 42.2561))) * 12341.14122 * sin(_Time.y * 0.03));
    }

    //This is the noise method from book of shaders
    float noise(float2 uv)
    {
        float2 i = floor(uv);
        float2 f = frac(uv);

        float a = random(i);
        float b = random(i + float2(1.0, 0.0));
        float c = random(i + float2(0.0, 1.0));
        float d = random(i + float2(1.0, 0.0));

        float2 u = smoothstep(0.0, 1.0, f);
        return lerp(a, b, u.x) + (c - a) * u.y * (1. - u.x) + (d - b) * u.x * u.y;
    }

    // https://gist.github.com/ayamflow/c06bc0c8a64f985dd431bd0ac5b557cd
    float2 rotateUV(float2 uv, float rotation, float mid)
    {
        return float2(
            cos(rotation) * (uv.x - mid) + sin(rotation) * (uv.y - mid) + mid,
            cos(rotation) * (uv.y - mid) - sin(rotation) * (uv.x - mid) + mid
        );
    }

    struct appdata
    {
        float4 vertex : POSITION;
        float2 uv : TEXCOORD0;
        fixed4 color : COLOR;
    };

    struct v2f
    {
        float2 uv : TEXCOORD0;
        float4 vertex : SV_POSITION;
        fixed4 color : COLOR;
    };


    v2f Vert(appdata v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.uv = v.uv;
        return o;
    }

    float4 Frag(v2f i) : SV_Target
    {
        //pixelation 
        float2 uv = i.uv;
        //uv.x = floor(uv.x * _WidthPixelation) / _WidthPixelation;
        //uv.y = floor(uv.y * _HeightPixelation) / _HeightPixelation;


        const float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
        float linearDepth = Linear01Depth(depth);
        const float dist = ComputeDistance(depth);
        const half fog = saturate(exp2(1 * dist));

        //---SCREEN BEND---
        //Map the screen uv between 0 and 1, bend the screen then return it back to its normal map
        uv -= 0.5;
        uv *= 2.0;
        uv.x *= 1.0 + pow(abs(uv.y) / _ScreenBendX, 2.0);
        uv.y *= 1.0 + pow(abs(uv.x) / _ScreenBendY, 2.0);
        uv /= 2;
        uv += 0.5;


        //---VIGNETTE---
        //Copies the UV and shifts it to the center, exponentially reducing value is subtracted by 1 to simulate distance from the center
        float shiftUVx = (uv.x - 0.5) * _VignetteSize;
        float shiftUVy = (uv.y - 0.5) * _VignetteSize;
        float vignetteAmount = 1.0 - (sqrt(
            pow(abs(shiftUVx), _VignetteRounding) + pow(abs(shiftUVy), _VignetteRounding)) * (_VignetteAmount));
        vignetteAmount = smoothstep(0.0, _VignetteSmoothing, vignetteAmount);


        //Scan lines
        float scanlines = _ScanlinesWeight * (scanLines(uv, _ScanLinesDensity, _ScanLinesSpeed));
        //float disruptiveLines = (scanLines(uv, 0.005, -5));

        //Noise
        float valNoise = _NoiseWeight * (noise(uv * _NoiseAmount)); //noise size

        //Modify for chromatic abberation
        half2 redShift = _ChromaticRed;
        half2 greenShift = _ChromaticGreen;
        half2 blueShift = _ChromaticBlue;

        fixed4 textureColor = fixed4(
            tex2D(_MainTex, uv + redShift).r,
            tex2D(_MainTex, uv + greenShift).g,
            tex2D(_MainTex, uv + blueShift).b,
            tex2D(_MainTex, uv).a
        );
        fixed4 mainTextureColor = tex2D(_MainTex, i.uv);

        // Rotate Grille UV
        float grilleUvRotationRadians = _GrilleUvRotation * 3.14159265 / 180.0;
        float2 grilleUv = rotateUV(i.uv, grilleUvRotationRadians, _GrilleUvMidPoint);
        float2 grilleUvCounter = rotateUV(i.uv, grilleUvRotationRadians * 2, _GrilleUvMidPoint);


        // Screen-grille effect
        float4 grilleEffect = float4(mainTextureColor.x, mainTextureColor.y, mainTextureColor.z, _GrilleOpacity);
        float g_r = smoothstep(0.85, 0.95, abs(sin(_GrilleShift.x + grilleUv * (_GrilleResolution * 3.14159265))));
        float g_r_c = smoothstep(
            0.85, 0.95, abs(sin(_GrilleShift.x + grilleUvCounter * (_GrilleCounterResolution * 3.14159265))));
        grilleEffect.x = lerp(grilleEffect.x, grilleEffect.x * g_r, _GrilleOpacity) * lerp(
            grilleEffect.x, grilleEffect.x * g_r_c, _GrilleCounterOpacity);
        grilleEffect.x = clamp(grilleEffect.x * _GrilleBrightness, 0.0, 1.0);

        float g_g = smoothstep(0.85, 0.95, abs(sin(_GrilleShift.y + 1.05 + grilleUv * (_GrilleResolution * 3.14159265))));
        float g_g_c = smoothstep(
            0.85, 0.95, abs(sin(_GrilleShift.y + 1.05 + grilleUvCounter * (_GrilleCounterResolution * 3.14159265))));
        grilleEffect.y = lerp(grilleEffect.y, grilleEffect.y * g_g, _GrilleOpacity) * lerp(
            grilleEffect.y, grilleEffect.y * g_g_c, _GrilleCounterOpacity);
        grilleEffect.y = clamp(grilleEffect.y * _GrilleBrightness, 0.0, 1.0);

        float b_b = smoothstep(0.85, 0.95, abs(sin(_GrilleShift.z + 2.1 + grilleUv * (_GrilleResolution * 3.14159265))));
        float b_b_c = smoothstep(
            0.85, 0.95, abs(sin(_GrilleShift.z + 2.1 + grilleUvCounter * (_GrilleCounterResolution * 3.14159265))));
        grilleEffect.z = lerp(grilleEffect.z, grilleEffect.z * b_b, _GrilleOpacity) * lerp(
            grilleEffect.z, grilleEffect.z * b_b_c, _GrilleCounterOpacity);
        grilleEffect.z = clamp(grilleEffect.z * _GrilleBrightness, 0.0, 1.0);


        //Set main color
        float4 color = lerp(lerp(textureColor, scanlines, 0.05), valNoise, 0.15) * fog * vignetteAmount;
        return grilleEffect * color;
    }
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            ENDCG
        }
    }
}