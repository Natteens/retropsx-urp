#ifndef RETROPSX_FINAL_COLOR_INCLUDED
#define RETROPSX_FINAL_COLOR_INCLUDED

// Final color treatment shared by Canonical Resolve and Volumetric Composite. Exactly one of the
// two applies it per camera: the resolve without volumetrics, the composite with them.
// Order: legacy final RGB precision -> artistic posterization -> palette mapping + palette dither.

#include "RetroPSXColor.hlsl"

#define RETROPSX_MAX_PALETTE_COLORS 64

// Threshold textures are read texel by texel (LOAD), never filtered.
TEXTURE2D(_RetroCustomDither);
TEXTURE2D(_RetroBlueNoise);
float4 _RetroFinalColorParams;
// x: mode (0 off, 1 RGB, 2 luminance), y: levels, z: strength, w: dither strength.
float4 _RetroPosterizeParams;
// x: color count (0 = off), y: strength, z: dither mode, w: dither strength.
float4 _RetroPaletteParams;
// x: posterize dither mode, y: custom pattern sRGB flag, z: blue noise sRGB flag.
float4 _RetroArtisticDither;
// xy: custom pattern size, zw: blue noise size.
float4 _RetroArtisticDitherSize;
float4 _RetroPaletteLinear[RETROPSX_MAX_PALETTE_COLORS];
float4 _RetroPaletteOklab[RETROPSX_MAX_PALETTE_COLORS];

static const float RetroPSX_PSXThreshold[16] = {
    0.0625, 0.5625, 0.1875, 0.6875, 0.8125, 0.3125, 0.9375, 0.4375,
    0.1875, 0.6875, 0.0625, 0.5625, 0.9375, 0.4375, 0.8125, 0.3125 };
static const float RetroPSX_Bayer2Threshold[4] = { 0.125, 0.625, 0.875, 0.375 };
static const float RetroPSX_Bayer4Threshold[16] = {
    0.03125, 0.53125, 0.15625, 0.65625,
    0.78125, 0.28125, 0.90625, 0.40625,
    0.21875, 0.71875, 0.09375, 0.59375,
    0.96875, 0.46875, 0.84375, 0.34375 };

// Normalized ordered-dither threshold in (0, 1), tiled in internal-resolution pixels. Unlike the legacy
// amplitude it does not depend on a bit depth, so it is valid for any level count or palette.
// The PSX table is the legacy PSX matrix ((value + 4.5) / 8); Bayer tables are the legacy ones + 0.5.
float RetroPSX_DitherThreshold(int mode, int2 pixel)
{
    int2 cell = int2(pixel.x & 3, pixel.y & 3);
    float threshold = 0.5;
    if (mode == 1)
        threshold = RetroPSX_PSXThreshold[cell.y * 4 + cell.x];
    else if (mode == 2)
        threshold = RetroPSX_Bayer2Threshold[(cell.y & 1) * 2 + (cell.x & 1)];
    else if (mode == 3)
        threshold = RetroPSX_Bayer4Threshold[cell.y * 4 + cell.x];
    else if (mode == 4 || mode == 5)
    {
        bool custom = mode == 4;
        int2 size = (int2)max(custom ? _RetroArtisticDitherSize.xy : _RetroArtisticDitherSize.zw, 1.0);
        int2 texel = int2((uint)pixel.x % (uint)size.x, (uint)pixel.y % (uint)size.y);
        float value = custom ? LOAD_TEXTURE2D(_RetroCustomDither, texel).r : LOAD_TEXTURE2D(_RetroBlueNoise, texel).r;
        // Thresholds are data: undo the decode of a texture imported as sRGB.
        if ((custom ? _RetroArtisticDither.y : _RetroArtisticDither.z) > 0.5)
            value = LinearToSRGB(value.xxx).x;
        threshold = clamp(value, 1.0 / 512.0, 511.0 / 512.0);
    }
    return threshold;
}

// Legacy amplitude, scaled for RGB bit quantization. Texture patterns tile per internal pixel like the
// artistic stages (they used to be stretched over the whole image and filtered).
float RetroPSX_FinalImageDither(int mode, int2 pixel)
{
    if (mode <= 3)
        return RetroPSX_DitherValue(mode, pixel);
    return (RetroPSX_DitherThreshold(mode, pixel) - 0.5) / 31.0;
}

half3 RetroPSX_ApplyFinalImagePrecision(half3 color, int2 pixel)
{
    if (_RetroFinalColorParams.x > 0.5 && _RetroPSXColorBits.w > 0.5)
    {
        float3 srgb = LinearToSRGB(saturate((float3)color));
        srgb += RetroPSX_FinalImageDither((int)_RetroFinalColorParams.y, pixel) * _RetroFinalColorParams.z;
        float3 levels = exp2(_RetroPSXColorBits.xyz) - 1.0;
        srgb = round(saturate(srgb) * levels) / levels;
        color = (half3)SRGBToLinear(srgb);
    }
    return color;
}

float RetroPSX_Luminance(float3 linearColor)
{
    return dot(linearColor, float3(0.2126, 0.7152, 0.0722));
}

// HDR policy: luminance is clamped to 1 and out-of-range channels are desaturated toward white at
// constant luminance, so bright saturated highlights keep their hue instead of clipping per channel.
// Only the stage input is fitted: partial strength mixes with the unfitted color, so HDR values fade in
// continuously from strength 0 instead of jumping to the display range at the first non-zero strength.
float3 RetroPSX_FitToDisplayRange(float3 linearColor)
{
    float3 color = max(linearColor, 0.0);
    float luminance = RetroPSX_Luminance(color);
    if (luminance >= 1.0)
        return 1.0;
    float peak = max(color.r, max(color.g, color.b));
    if (peak > 1.0)
        color = luminance + (color - luminance) * ((1.0 - luminance) / (peak - luminance));
    return saturate(color);
}

// value is in [0, 1]; dither strength 0 rounds, 1 steps up where the fraction exceeds the threshold.
float3 RetroPSX_PosterizeSteps(float3 value, float levels, float threshold, float ditherStrength)
{
    float steps = max(levels - 1.0, 1.0);
    float offset = lerp(0.5, 1.0 - threshold, saturate(ditherStrength));
    return saturate(floor(saturate(value) * steps + offset) / steps);
}

// Works in sRGB-encoded values, like the legacy PSX quantization.
float3 RetroPSX_PosterizeRGB(float3 linearColor, float levels, float threshold, float ditherStrength)
{
    return SRGBToLinear(RetroPSX_PosterizeSteps(LinearToSRGB(linearColor), levels, threshold, ditherStrength));
}

// Quantizes sRGB-encoded luminance and rescales linear RGB, which keeps chromaticity. Achromatic input
// therefore gets exactly the RGB posterize levels. Colors pushed past 1 are desaturated at constant
// luminance; hue information below ~1/256 linear is unreliable and fades to gray.
float3 RetroPSX_PosterizeLuminance(float3 linearColor, float levels, float threshold, float ditherStrength)
{
    float luminance = RetroPSX_Luminance(linearColor);
    float encoded = LinearToSRGB(luminance.xxx).x;
    float target = SRGBToLinear(RetroPSX_PosterizeSteps(encoded.xxx, levels, threshold, ditherStrength)).x;
    if (luminance <= 1e-6)
        return target;
    float3 color = lerp(target.xxx, linearColor * (target / luminance), saturate(luminance * 256.0));
    float peak = max(color.r, max(color.g, color.b));
    if (peak > 1.0)
        color = target + (color - target) * ((1.0 - target) / max(peak - target, 1e-6));
    return saturate(color);
}

float3 RetroPSX_LinearToOklab(float3 c)
{
    float l = 0.4122214708 * c.r + 0.5363325363 * c.g + 0.0514459929 * c.b;
    float m = 0.2119034982 * c.r + 0.6806995451 * c.g + 0.1073969566 * c.b;
    float s = 0.0883024619 * c.r + 0.2817188376 * c.g + 0.6299787005 * c.b;
    l = pow(max(l, 0.0), 1.0 / 3.0);
    m = pow(max(m, 0.0), 1.0 / 3.0);
    s = pow(max(s, 0.0), 1.0 / 3.0);
    return float3(
        0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s,
        1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s,
        0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s);
}

// Nearest palette color in OKLab (lowest index wins ties). With dithering, the second color is the one
// whose segment from the nearest passes closest to the pixel, lightly penalized by its length so
// gradients dither between neighbors instead of distant colors; t is the position along that segment.
void RetroPSX_FindPalettePair(float3 lab, int count, bool findSecond, out int first, out int second, out float t)
{
    first = 0;
    float best = 1e20;
    [loop]
    for (int index = 0; index < count; index++)
    {
        float3 delta = lab - _RetroPaletteOklab[index].xyz;
        float distance = dot(delta, delta);
        if (distance < best)
        {
            best = distance;
            first = index;
        }
    }
    second = first;
    t = 0.0;
    if (!findSecond)
        return;
    float3 origin = _RetroPaletteOklab[first].xyz;
    float3 offset = lab - origin;
    float bestCost = 1e20;
    [loop]
    for (int candidate = 0; candidate < count; candidate++)
    {
        float3 segment = _RetroPaletteOklab[candidate].xyz - origin;
        float length2 = dot(segment, segment);
        if (candidate == first || length2 < 1e-8)
            continue;
        float position = saturate(dot(offset, segment) / length2);
        if (position <= 0.0)
            continue;
        float3 error = offset - segment * position;
        float cost = dot(error, error) + 0.05 * length2;
        if (cost < bestCost)
        {
            bestCost = cost;
            second = candidate;
            t = position;
        }
    }
}

// Ordered dither between the pair. The pattern is anchored to the pair's palette order, not to which of
// the two is nearest: where the nearest color flips along a gradient the coverage stays continuous and
// the pattern does not invert into its complement (which showed as a seam of clumped pixels).
int RetroPSX_PickPaletteColor(int first, int second, float towardSecond, float threshold)
{
    if (second == first)
        return first;
    bool ordered = first < second;
    float towardHigh = ordered ? towardSecond : 1.0 - towardSecond;
    bool high = towardHigh > threshold;
    return high == ordered ? second : first;
}

// Mixes in sRGB-encoded values; strength 1 returns the processed color untouched.
float3 RetroPSX_MixStrength(float3 original, float3 processed, float strength)
{
    if (strength >= 1.0)
        return processed;
    return SRGBToLinear(lerp(LinearToSRGB(original), LinearToSRGB(processed), strength));
}

bool RetroPSX_ArtisticColorActive()
{
    return (_RetroPosterizeParams.x > 0.5 && _RetroPosterizeParams.z > 0.0)
        || (_RetroPaletteParams.x > 0.5 && _RetroPaletteParams.y > 0.0);
}

// Threshold of the last active artistic dither stage, for the debug view.
float RetroPSX_ArtisticDitherThreshold(int2 pixel)
{
    int mode = _RetroPaletteParams.x > 0.5 ? (int)_RetroPaletteParams.z : (int)_RetroArtisticDither.x;
    return mode > 0 ? RetroPSX_DitherThreshold(mode, pixel) : 0.5;
}

half3 RetroPSX_ApplyArtisticColor(half3 linearColor, int2 pixel)
{
    if (!RetroPSX_ArtisticColorActive())
        return linearColor;

    float3 color = (float3)linearColor;
    bool palette = _RetroPaletteParams.x > 0.5 && _RetroPaletteParams.y > 0.0;
    if (_RetroPosterizeParams.x > 0.5 && _RetroPosterizeParams.z > 0.0)
    {
        // Only the last active artistic stage dithers: with a palette, the palette stage owns it.
        int ditherMode = palette ? 0 : (int)_RetroArtisticDither.x;
        float ditherStrength = ditherMode > 0 ? _RetroPosterizeParams.w : 0.0;
        float threshold = ditherMode > 0 ? RetroPSX_DitherThreshold(ditherMode, pixel) : 0.5;
        float levels = clamp(round(_RetroPosterizeParams.y), 2.0, 64.0);
        float3 fitted = RetroPSX_FitToDisplayRange(color);
        float3 posterized = _RetroPosterizeParams.x > 1.5
            ? RetroPSX_PosterizeLuminance(fitted, levels, threshold, ditherStrength)
            : RetroPSX_PosterizeRGB(fitted, levels, threshold, ditherStrength);
        color = RetroPSX_MixStrength(color, posterized, _RetroPosterizeParams.z);
    }

    if (palette)
    {
        int count = min((int)_RetroPaletteParams.x, RETROPSX_MAX_PALETTE_COLORS);
        int ditherMode = (int)_RetroPaletteParams.z;
        float ditherStrength = ditherMode > 0 ? saturate(_RetroPaletteParams.w) : 0.0;
        int first, second;
        float t;
        RetroPSX_FindPalettePair(RetroPSX_LinearToOklab(RetroPSX_FitToDisplayRange(color)), count, ditherStrength > 0.0, first, second, t);
        int chosen = RetroPSX_PickPaletteColor(first, second, t * ditherStrength, RetroPSX_DitherThreshold(ditherMode, pixel));
        color = RetroPSX_MixStrength(color, _RetroPaletteLinear[chosen].rgb, _RetroPaletteParams.y);
    }
    return (half3)color;
}

#endif
