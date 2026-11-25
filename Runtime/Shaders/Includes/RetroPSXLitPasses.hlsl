#ifndef RETROPSX_LIT_PASSES_INCLUDED
#define RETROPSX_LIT_PASSES_INCLUDED

// RetroPSX/Lit = URP Lit + optional PS1 vertex jitter + optional color grading.
// Lighting, shadows and every URP Lit feature come untouched from URP's own passes;
// this file only wraps their vertex (snap) and forward fragment (grade).

// Snaps a clip-space position to a virtual low-res pixel grid (the PS1 "wobble").
float4 RetroPSX_SnapToGrid(float4 positionCS)
{
    if (_PSXJitter <= 0.0 || positionCS.w <= 1e-5)
        return positionCS;

    float rows = max(_PSXResolution, 1.0);
    float2 grid = float2(rows * _ScreenParams.x / _ScreenParams.y, rows) * 0.5;
    float2 ndc = positionCS.xy / positionCS.w;
    float2 snapped = round(ndc * grid) / grid;
    positionCS.xy = lerp(positionCS.xy, snapped * positionCS.w, _PSXJitter);
    return positionCS;
}

// Exposure (stops), saturation, HDR tint and optional posterize. Defaults leave the color untouched.
half3 RetroPSX_Grade(half3 color)
{
    color *= exp2(_PSXExposure);
    half luma = dot(color, half3(0.2126h, 0.7152h, 0.0722h));
    color = lerp(luma.xxx, color, _PSXSaturation);
    color *= _PSXTint.rgb;
    if (_PSXColorSteps >= 2.0)
        color = floor(color * _PSXColorSteps + 0.5h) / _PSXColorSteps;
    return color;
}

#if defined(UNIVERSAL_FORWARD_LIT_PASS_INCLUDED)
Varyings PSXLitPassVertex(Attributes input)
{
    Varyings output = LitPassVertex(input);
    output.positionCS = RetroPSX_SnapToGrid(output.positionCS);
    return output;
}

void PSXLitPassFragment(
    Varyings input
    , out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out uint outRenderingLayers : SV_Target1
#endif
)
{
    LitPassFragment(input, outColor
#ifdef _WRITE_RENDERING_LAYERS
        , outRenderingLayers
#endif
    );
    outColor.rgb = RetroPSX_Grade(outColor.rgb);
}
#endif

#if defined(UNIVERSAL_LIT_GBUFFER_PASS_INCLUDED)
// shortcut: grading is not applied in Deferred (lighting happens after the GBuffer), only the snap is. Upgrade if a project uses Deferred.
Varyings PSXLitGBufferPassVertex(Attributes input)
{
    Varyings output = LitGBufferPassVertex(input);
    output.positionCS = RetroPSX_SnapToGrid(output.positionCS);
    return output;
}
#endif

#if defined(UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED)
// Depth must snap like color, or the depth prepass and the lit pass disagree and flicker.
Varyings PSXDepthOnlyVertex(Attributes input)
{
    Varyings output = DepthOnlyVertex(input);
    output.positionCS = RetroPSX_SnapToGrid(output.positionCS);
    return output;
}
#endif

#if defined(UNIVERSAL_FORWARD_LIT_DEPTH_NORMALS_PASS_INCLUDED)
Varyings PSXDepthNormalsVertex(Attributes input)
{
    Varyings output = DepthNormalsVertex(input);
    output.positionCS = RetroPSX_SnapToGrid(output.positionCS);
    return output;
}
#endif

#endif
