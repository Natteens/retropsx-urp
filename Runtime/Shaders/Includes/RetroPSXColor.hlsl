#ifndef RETROPSX_COLOR_INCLUDED
#define RETROPSX_COLOR_INCLUDED

#include "RetroPSXCore.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

float RetroPSX_DitherValue(int mode, int2 pixel)
{
    pixel = int2(pixel.x & 3, pixel.y & 3);
    float value = 0.0;
    if (mode == 1)
    {
        static const int psx[16] = { -4, 0, -3, 1, 2, -2, 3, -1, -3, 1, -4, 0, 3, -1, 2, -2 };
        value = psx[pixel.y * 4 + pixel.x] / 255.0;
    }
    else if (mode == 2)
    {
        static const float bayer2[4] = { -0.375, 0.125, 0.375, -0.125 };
        value = bayer2[(pixel.y & 1) * 2 + (pixel.x & 1)] / 31.0;
    }
    else if (mode == 3)
    {
        static const float bayer4[16] = {
            -0.46875, 0.03125, -0.34375, 0.15625,
             0.28125,-0.21875,  0.40625,-0.09375,
            -0.28125, 0.21875, -0.40625, 0.09375,
             0.46875,-0.03125,  0.34375,-0.15625 };
        value = bayer4[pixel.y * 4 + pixel.x] / 31.0;
    }
    return value;
}

half3 RetroPSX_ApplyColorPrecision(half3 linearColor, int ditherMode, float ditherStrength, int2 pixel)
{
    if (_RetroPSXColorBits.w < 0.5)
        return linearColor;

    float3 srgb = LinearToSRGB(saturate((float3)linearColor));
    srgb += RetroPSX_DitherValue(ditherMode, pixel) * ditherStrength;
    float3 levels = exp2(_RetroPSXColorBits.xyz) - 1.0;
    srgb = round(saturate(srgb) * levels) / levels;
    return (half3)SRGBToLinear(srgb);
}

half3 RetroPSX_ApplyVertexColor(half3 color, half3 vertexColor, float mode, half3 albedo)
{
    if (mode > 1.5)
        return vertexColor * albedo;
    if (mode > 0.5)
        return color * vertexColor;
    return color;
}

float RetroPSXDitherValue(int mode, int2 pixel)
{
    return RetroPSX_DitherValue(mode, pixel);
}

half3 RetroPSXQuantize(half3 linearColor, int ditherMode, float ditherStrength, int2 pixel)
{
    return RetroPSX_ApplyColorPrecision(linearColor, ditherMode, ditherStrength, pixel);
}

#endif
