# RetroPSX-URP

RetroPSX-URP is a PSX-style rendering toolkit for Unity 6 URP. It adds an aspect-aware pixel grid, low-precision material features, fog, volumetric lighting, color processing, and an optional CRT pass through a RenderGraph renderer feature.

It is intended for games, not cycle-accurate hardware emulation.

## Features

- Native, internal-height, fixed, and resolution-scale raster modes
- Full-viewport output at 16:9, ultrawide, and arbitrary camera sizes
- Integer screen-space vertex snapping and affine texture interpolation
- Vertex colors, vertex lighting, point sampling, RGB precision, and ordered dithering
- Distance color, distance modulation, and stepped distance fog
- Reduced-resolution volumetric fog with directional, point, and spot lights
- Optional projected volumetric patterns, CRT treatment, and debug views
- RenderGraph resources scoped to each camera

## Requirements

- Unity 6000.0 or newer
- Universal Render Pipeline 17.0.3 or newer

## Installation

Add the repository URL in Package Manager, or add it to `Packages/manifest.json`:

```json
"com.natteens.retropsxurp": "https://github.com/Natteens/retropsx-urp.git"
```

You can also embed the package or use a local package reference while developing it.

## Setup

1. Add `RetroPSXRendererFeature` to the Universal Renderer used by the camera.
2. Choose **Assets > Create > RetroPSX > Complete Pipeline Profile** and assign the new root profile to the feature.
3. Adjust the linked raster, geometry, color, lighting, fog, volumetric, display, UI, and debug profiles.

Normal URP materials continue to render normally. Low-resolution presentation, final-image color processing, volumetrics, display effects, and frame-resource debug views do not require a RetroPSX shader.

Add the feature to every Renderer Data that should render RetroPSX. Scene View uses the URP asset's default renderer, so the usual setup is to make the RetroPSX-capable Renderer Data the default and let game cameras inherit it. Cameras that intentionally select another renderer need the feature there as well, using the same pipeline profile.

**Scene View Preview** defaults to **World Effects**. It keeps the editor viewport at native resolution while previewing integrated materials, fog, and volumetrics. **Off** leaves Scene View as ordinary URP, while **Full Pipeline** also previews the low-resolution presentation, final color treatment, and display pass.

Base game cameras targeting a RenderTexture run the same pipeline as screen cameras, including canonical resolve, presentation, and display effects. The target keeps its configured output size; the Raster profile controls the internal pixel grid. Alpha is preserved when URP enables alpha output. For transparent inventory previews, use an RGBA target and a transparent camera clear color; if URP post-processing is enabled, also enable **Alpha Processing** on the URP asset.

## Profiles

The root pipeline profile links separate assets for raster, geometry, color, lighting, fog, volumetrics, display, and debug settings. Profiles own configuration only; per-camera textures are transient RenderGraph resources.

The profile command creates linked assets with neutral defaults. The package does not ship artistic preset assets.

## Materials

`RetroPSX/Lit` and `RetroPSX/Unlit` are optional, ready-made shaders for the complete material-side feature set. Use them when you want affine textures, vertex precision, vertex lighting, vertex color modulation, material dithering, or material fog with minimal setup.

Custom shaders can include the public material library and opt into individual helpers:

```hlsl
#include "Packages/com.natteens.retropsxurp/Runtime/Shaders/Includes/RetroPSXMaterial.hlsl"

positionCS = RetroPSX_ApplyVertexPrecision(positionCS, positionWS, _GeometryStrength);
float2 uv = RetroPSX_GetAffineUV(perspectiveUV, affineUV, _AffineStrength);
color = RetroPSX_ApplyColorPrecision(color, ditherMode, ditherStrength, pixel);
color = RetroPSX_ApplyFog(color, RetroPSX_GetFogFactor(positionWS), _FogStrength);
```

These functions also have `_float` entry points for Shader Graph Custom Function nodes in File mode. Shader Graph is not a package dependency.

Material color processing and final-image color processing are separate. Final-image processing is off by default, so integrated materials are not quantized twice unless both paths are deliberately enabled.

## UI Toolkit

Screen-space UI Toolkit panels remain normal, native-resolution Unity UI. World-space panels can be marked with `RetroPSXUI`:

- **Native** (the default) is drawn after RetroPSX presentation, so text and controls stay sharp and skip final RGB processing, dithering, and CRT. It still depth-tests against opaque world geometry.
- **Retro** stays in the ordinary world render and is deliberately included in the RetroPSX raster and display treatment.

For Native world-space UI, choose a free project layer in `RetroUIProfile`, exclude it from the Universal Renderer prepass, opaque, and transparent layer masks, and do not put ordinary scene renderers on that layer. The package leaves this unconfigured instead of claiming a user layer. `RetroPSXUI` temporarily applies the chosen layer in Native mode and restores the object's original layer in Retro mode or when disabled. The runtime marker does not depend on a specific UI Toolkit renderer type. The development path was validated with `PanelRenderer` on Unity 6000.5.7f1; earlier Unity 6 editor versions were not runtime-tested in this pass.

## Volumetrics

The volumetric pass reconstructs world position from URP depth and raymarches into a reduced-resolution transient texture. It terminates conservatively at opaque depth and uses depth-aware reconstruction at geometry edges. The main directional light can provide atmosphere, while `RetroVolumetricLight` components add a bounded set of point or spot lights. Set a local light's Volumetric Shadows option to Use Light Shadows and enable realtime shadows on its Unity Light to block direct scattering behind opaque geometry. In that mode, direct scattering is suppressed when URP has not allocated a valid shadow slice rather than falling back to an unshadowed beam. Local lights can also use procedural or texture patterns, stepped attenuation, and sharper beam falloff.

This is a modern extension rather than an original PlayStation GPU feature.

## Test scene / development

The package-development project keeps its technical scene at `Assets/RetroPSXTest/RetroPSXTestScene.unity`. It is outside the package and is not installed as a UPM sample.

## Notes and limitations

- Geometry snapping, affine UVs, vertex lighting, and material fog require a RetroPSX-integrated shader; they cannot be added safely to an arbitrary surface shader after it renders.
- Transparent URP materials keep their own queues, blending, depth writes, and shader semantics. Volumetric composition uses opaque camera depth, so exact fog ordering through arbitrary transparent surfaces is approximate.
- Camera overlay stacks are skipped. Apply the feature to the base camera.
- Exact low-resolution triangle coverage requires the URP render scale or camera target to match the intended raster size. The canonical resolve still provides the final pixel grid for any URP shader.
- Stretch presentation is the default and fills the viewport. Aspect Fit and Integer Fit are optional presentation choices that may show the configured border color.

See [Documentation~/index.md](Documentation~/index.md) for the pass order, compatibility matrix, and shader integration details.

## References

- [PSX-SPX GPU documentation](https://psx-spx.consoledev.net/graphicsprocessingunitgpu/)
- [PSX-style rendering in Godot 4](https://calinp.eu/blog/psx-type-rendering/)
- [Unity URP RenderGraph documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/render-graph.html)

## License

[MIT](LICENSE.md)
