# RetroPSX-URP technical notes

RetroPSX-URP is a modular PSX-style rendering toolkit for Unity 6 URP. It uses RenderGraph as its only image-effect path and separates hardware-motivated behavior, adjustable stylization, and modern extensions. It is not a cycle-accurate emulator.

The material design is informed by Calin Panaite's [PSX-style rendering in Godot 4](https://calinp.eu/blog/psx-type-rendering/), the hardware-oriented [PSX-SPX GPU documentation](https://psx-spx.consoledev.net/graphicsprocessingunitgpu/), and Unity's [URP RenderGraph documentation](https://docs.unity3d.com/6000.0/Documentation/Manual/urp/render-graph.html). The Unity C# and HLSL implementation is original.

## Feature classification

Hardware-style behavior includes RGB555 color, the PSX 4×4 dither matrix, dither-before-quantize ordering, nearest texture sampling, integer screen-space vertex coordinates, affine texture interpolation, vertex colors, flat/Gouraud shading, texture modulation, vertex lighting, distance fog, and the four semi-transparency equations where Unity fixed-function blending can represent them.

Stylized controls include fractional snap and affine strengths, custom channel precision, full-frame color conversion, stepped fog, distance-weighted snapping, near-camera fade, and Bayer/custom dithering.

Modern extensions include raymarched volumetric fog, directional scattering, bounded point and spot volumetric lights, projected patterns, depth-aware reconstruction, blue-noise final dithering, debug views, and CRT display simulation.

## Renderer setup

Install `RetroPSXRendererFeature` on the Universal Renderer used by the camera and assign a complete `RetroPSXPipelineProfile`. The feature resolves its `Hidden/RetroPSX/*` shaders in `Create()` and owns the engine materials. Users do not assign hidden shaders manually.

Preview, reflection, and overlay cameras receive neutral material globals and skip image passes. A final reset pass restores the same neutral state and clears the transient volumetric binding after every camera. This prevents a renderer without RetroPSX from inheriting another camera's pixel grid, geometry, fog, dither, light, debug, or volumetric state.

Install the feature in every Renderer Data that should render RetroPSX. Scene View uses the URP asset's default renderer, so that Renderer Data must contain the feature when Scene View preview is enabled. The simplest setup is to make the RetroPSX-capable Renderer Data the default and let game cameras inherit it. If a game camera deliberately selects another renderer, install the feature there too and reference the same pipeline profile.

**Scene View Preview** has three camera policies:

- **Off** publishes neutral material state and records no RetroPSX image passes.
- **World Effects** publishes material state using a native per-camera raster context. It records only the reduced-resolution volumetric raymarch and native-resolution atmosphere composite when volumetrics are enabled. Canonical resolve, final color, point presentation, and display simulation are skipped.
- **Full Pipeline** runs the same stages as a game camera, but derives the raster context from the current Scene View dimensions.

World Effects is the default. Because the atmosphere composite runs before editor gizmos, the grid, handles, icons, and overlays remain native-resolution editor output.

## Profiles

The root profile references these setting assets:

- `RetroRasterProfile`: raster mode, internal size, and presentation
- `RetroGeometryProfile`: integer precision, artistic snapping, and affine mapping
- `RetroColorProfile`: channel precision and material/final-image dithering
- `RetroLightingProfile`: default material lighting, ambient light, and bounded local-light count
- `RetroFogProfile`: distance color, distance modulation, and stepped distance color fog
- `RetroVolumetricProfile`: raymarch quality and integration controls
- `RetroDisplayProfile`: optional CRT/display modules
- `RetroUIProfile` (optional): native versus Retro world-space UI policy and its reserved native UI layer
- `RetroDebugProfile`: diagnostic output mode

`RetroTextureProfile` and `RetroVolumetricPattern` are independent authoring assets. Profiles contain settings only; they never own GPU resources. **Create Complete Profile Set** creates linked assets with defaults, not artistic looks.

## RenderGraph order

1. **Material globals, before opaques:** build the immutable per-camera `RetroRasterContext`, select the URP main light, and publish the active grid plus geometry, affine, color, lighting, fog, and debug constants.
2. **Normal URP scene rendering:** RetroPSX materials perform viewport snapping, affine/Gouraud interpolation, vertex lighting, modulation, fog, dithering, and quantization.
3. **Canonical resolve, before URP post-processing:** sample the URP camera color onto the canonical grid and optionally apply whole-frame fog or final-image color treatment.
4. **Volumetric raymarch, optional:** reconstruct world position from camera depth and integrate scattering in a reduced-resolution transient texture.
5. **Volumetric composite, optional:** depth-aware reconstruction composites scattering and transmittance into the canonical image.
6. **Point presentation when scaling is needed:** fill the camera output or use the explicitly selected Aspect Fit/Integer Fit viewport.
7. **Display simulation, optional:** run the CRT modules once at output resolution.
8. **Native world-space UI, optional:** redraw the reserved native UI layer after RetroPSX presentation, while the camera's opaque depth and stencil attachment is still available. This is omitted unless the profile is complete and a Native UI layer is configured.
9. **Normal URP post-processing and final resolve.** Native world-space UI skips RetroPSX image treatment, but later generic URP post-processing can still affect it.
10. **Screen-space overlay UI.** Screen-space overlay UI remains on URP's normal final overlay path.
11. **Material-state reset, after rendering:** restore the neutral 1×1 context, disable all material effects, and clear the transient volumetric texture binding.

The feature never reads the backbuffer. Each stage publishes the next `UniversalResourceData.cameraColor`. Native/full-size Stretch output skips the presentation copy.

In Scene View World Effects, the source color goes directly to volumetric composition; there is no canonical resolve or presentation copy. With volumetrics disabled, no image pass is recorded for that camera.

## Raster and aspect handling

Raster modes are Native, Internal Height, Fixed Resolution, and Scale Factor.

- **Native** uses the current camera dimensions.
- **Internal Height** keeps the requested height and derives width from the current camera aspect.
- **Fixed Resolution** uses the selected/custom reference height and likewise derives the active width from the camera aspect, avoiding a fixed-aspect framebuffer.
- **Scale Factor** scales both current camera dimensions.

With a requested height of 240, representative results are 427×240 at 1920×1080, 569×240 at 2560×1080, 573×240 at 3440×1440, and 853×240 at 5120×1440. Widths are rounded to the nearest positive integer. Camera projection remains Unity's normal projection for that target aspect.

Stretch is the default presentation and always returns the full source rectangle with zero viewport offset. It does not letterbox, pillarbox, crop horizontal visibility, or stretch a fixed 4:3 buffer. Aspect Fit and Integer Fit remain explicit presentation choices for projects that deliberately want a framed image; only those choices may expose the configured border color.

All grid-dependent shader work reads the current camera's `RetroRasterContext`. No shader assumes 320×240, 4:3, or 16:9. World Effects uses source size as both source and internal size, so material snapping and dithering follow the current Scene View viewport instead of the game raster. Resizing changes transient texture descriptors; it does not mutate shared profile state.

URP renders scene geometry before a renderer feature can safely replace every built-in attachment across all renderer and camera-stack configurations. The feature therefore locks material snapping and dithering to the requested grid, then resolves the scene to the canonical size before atmosphere and presentation. Match URP render scale or a camera target to the profile when every coverage sample must originate at low resolution.

## Geometry and affine mapping

Geometry wobble is integer raster snapping, not arbitrary noise. Clip-space NDC is mapped to the per-camera pixel grid, rounded, and mapped back while preserving clip W. Artistic strength, distance influence, and near-camera fade provide controlled mitigation; material strength gives viewmodels a direct opt-out.

Affine UVs use HLSL `noperspective` interpolation. Artistic Blend interpolates between affine and normal perspective-correct UVs. Mesh subdivision still determines the visible severity of affine warping.

Flat lighting uses non-interpolated provoking-vertex values. Gouraud and Vertex Lit interpolate vertex results. Texture modulation uses the practical floating-point approximation `texture × vertexColor × 2`, with half gray as neutral.

## Color and dithering

Color modes are Off, RGB444, RGB555, RGB565, RGB666, and Custom. RetroPSX converts the material color to sRGB, applies the selected ordered offset, rounds to channel levels, and returns to linear space for the rest of URP.

The PSX matrix is:

```text
-4  0 -3  1
 2 -2  3 -1
-3  1 -4  0
 3 -1  2 -2
```

Material dithering is locked to canonical pixel coordinates and runs before material quantization. Custom and blue-noise patterns are final-image options. Final-image quantization is an explicit opt-in for ordinary URP materials or for folding transparent and atmospheric blends back onto the palette. When volumetrics run, this processing is deferred until after their composite so the frame is quantized once. Leave it disabled when integrated materials already provide the desired palette treatment.

## UI Toolkit composition

Screen-space UI is left to URP/UI Toolkit and stays native by default. It is not part of the RetroPSX canonical resolve, final-image dither/quantization, or display pass.

For world-space UI, add `RetroPSXUI` to the GameObject that owns the UI Toolkit renderer. It supports two explicit choices:

- **Native** is the default marker policy. Choose a free project layer in `RetroUIProfile`; the package leaves it unconfigured by default and never claims layer 30 or any other user layer. Exclude the chosen layer from the Universal Renderer prepass, opaque, and transparent layer masks. `RetroPSXUI` preserves the object's original layer, temporarily applies the configured layer in Native mode, and restores the original in Retro mode or when disabled. RetroPSX redraws the chosen layer after its presentation pass. The redraw uses the active camera depth/stencil attachment: text stays native-resolution and sharp while still being hidden by opaque world geometry.
- **Retro** uses the restored original GameObject layer. It follows normal URP drawing and therefore intentionally participates in the configured raster grid, final-image processing, and CRT/display pass.

This is a renderer configuration requirement rather than a material override: RetroPSX never swaps UI shaders, re-renders arbitrary objects, creates an extra camera, or allocates a persistent UI texture. The native layer must contain only panels intended for this late redraw. Because Native UI is deliberately composed after the opaque/transparent scene presentation, exact sorting against arbitrary transparent world materials is not available; opaque depth occlusion and UI Toolkit clipping/stencil behavior are preserved.

The GameObject layer is required because the Universal Renderer opaque and transparent passes cannot exclude a renderer using `renderingLayerMask`; using a rendering layer only for the late pass would draw the panel twice. Native mode therefore changes only the marked renderer GameObject's layer, never its children. Physics, raycasts, camera culling, and other layer-based systems observe that temporary layer. Keep gameplay colliders or interaction components on separate GameObjects when their layer must remain unchanged. The original layer is serialized and restored when switching to Retro, disabling or removing the marker, or destroying the marked object.

The runtime marker is independent of the UI Toolkit renderer class and has no serialized or static dependency on `PanelRenderer` or `UIRenderer`. The development authoring helper uses the direct `PanelRenderer` API only behind `UNITY_6000_5_OR_NEWER`. Unity 6000.5.7f1 with `PanelRenderer` was runtime-tested; the architecture remains compile-time compatible with the package's Unity 6000.0 minimum, but Unity 6000.0–6000.4 were not launched for this validation.

The development test scene at `Assets/RetroPSXTest/RetroPSXTestScene.unity` contains a small UI validation area. Its authoring helper creates two Native world panels, one Retro world panel, and a screen-space panel, along with their `PanelSettings` assets. The test project deliberately reserves layer 30 as `RetroPSX Native UI` and excludes that layer from the test renderer's opaque and transparent masks; this is test-project configuration, not a package default. Run **Tools > RetroPSX > Create UI Validation** if the test objects need to be recreated.

## Shader compatibility

The renderer feature never replaces scene materials and never redraws the scene. Pipeline-level image effects therefore preserve the render queues, depth, stencil, blending, lighting, and custom passes of ordinary URP-compatible shaders.

| Feature | Any URP shader | RetroPSX shader | Integrated custom shader |
| --- | :---: | :---: | :---: |
| Native/low-resolution presentation | Yes | Yes | Yes |
| Final-image color and dither | Yes | Yes | Yes |
| Volumetric fog and lights | Yes | Yes | Yes |
| CRT/display pass | Yes | Yes | Yes |
| Frame-resource debug views | Yes | Yes | Yes |
| Vertex snapping | No | Yes | Yes |
| Affine texture interpolation | No | Yes | Yes |
| Vertex lighting and color modulation | No | Yes | Yes |
| Material color and dither | No | Yes | Yes |
| Material-integrated distance fog | No | Yes | Yes |
| Retro transparency equations | No | Yes | Optional |

`RetroPSX/Lit` and `RetroPSX/Unlit` are convenience shaders, not a compatibility requirement. URP/Lit, URP/Unlit, Shader Graph, custom ShaderLab/HLSL, and third-party URP shaders receive all applicable pipeline-level effects without conversion.

## Public HLSL integration

Custom shaders can include only the part they need, or use the umbrella include:

```hlsl
#include "Packages/com.natteens.retropsxurp/Runtime/Shaders/Includes/RetroPSXMaterial.hlsl"
```

- `RetroPSXCore.hlsl` exposes `RetroPSXRasterContext`, `RetroPSX_GetRasterContext`, and `RetroPSX_GetCanonicalPixel`.
- `RetroPSXGeometry.hlsl` exposes vertex precision and affine UV packing, reconstruction, and blending.
- `RetroPSXLighting.hlsl` exposes bounded vertex and modern diffuse-light evaluation.
- `RetroPSXColor.hlsl` exposes vertex-color modulation, ordered dither values, and channel precision.
- `RetroPSXFog.hlsl` exposes distance factor and material fog application.
- `RetroPSXMaterial.hlsl` includes the complete public surface and supplies `_float` wrappers for Shader Graph Custom Function nodes in File mode.

Affine interpolation still needs suitable varyings. A ShaderLab shader should pack the affine value in its vertex stage and interpolate it as required by its design; Shader Graph needs a vertex-stage Custom Function and custom interpolator. The package does not require Shader Graph and does not ship a master graph.

Per-material strengths are ordinary shader properties, so they can be set by a material or `MaterialPropertyBlock` without cloning a material. Set geometry and affine strengths to zero for a viewmodel opt-out. Lighting, dither, color precision, and fog participation can be reduced independently.

## Materials and transparency

`RetroPSX/Lit` and `RetroPSX/Unlit` share one material pass include. Both support point-sampled base textures, vertex colors, modulation, geometry precision, affine UVs, dither/quantization, cutout, fog participation, and transparency controls.

The material inspector maps Average, Additive, Subtractive, and Add Quarter to Unity fixed-function blending. Average uses half alpha, Add Quarter uses quarter-alpha additive blending, and Subtractive uses reverse subtraction. Modern Alpha is also available. The package does not emulate the framebuffer mask bit or per-texel semi-transparency flag.

Arbitrary transparent shaders retain their own queues, blending, clipping, depth writes, and stencil behavior. They are not overridden or double-rendered. Volumetric reconstruction uses opaque camera depth, so exact atmosphere ordering through arbitrary transparent layers is not available without shader participation or a separate transparent-depth system.

## Lighting and retro fog

Vertex Lit evaluates ambient, the main directional light, and a bounded number of URP additional point/spot lights at vertices. URP supplies range/spot attenuation and shadows where available. Fragment Lit evaluates the same explicit diffuse language per fragment; this is not a metallic/smoothness PBR shader.

`DistanceColor` blends toward the fog color by camera distance. `DistanceModulation` multiplies the material by the fog color. `SteppedDistanceColor` quantizes the distance factor before the color blend. Whole-frame fog is available for non-participating URP materials.

## Volumetrics and local lights

The volumetric path uses no extra camera, shell mesh, radial blur, history texture, compute buffer, or persistent render target. A fullscreen raymarch reconstructs world position from depth, samples each interval at its midpoint, and stops before opaque scene depth using the configured Geometry Depth Bias. It supports height density, extinction, scattering, Henyey-Greenstein anisotropy, screen-pixel jitter, quantized density and light attenuation, ambient/main-directional scattering, and early transmittance exit. The reduced-resolution volume is point sampled, then reconstructed with depth and scattering/transmittance edge rejection so surface silhouettes and light-space shadow edges are not blurred together.

Quality levels map to Low = divisor 4 / 16 steps, Medium = divisor 2 / 28 steps, and High = divisor 2 / 48 steps. Custom exposes divisor 1–8 and 4–128 steps.

Up to four registered `RetroVolumetricLight` point or spot components are copied from a reusable registry, frustum-culled, and packed into reused arrays. Spot cones support range falloff, edge softness, beam sharpness, intensity/density, blinking, distortion, pattern strength, and `RetroVolumetricPattern` assets. Patterns may be stripes, checker, radial, noise, or a texture with scale, offset, scroll, rotation, threshold, softness, inversion, and world/projector mapping.

`Volumetric Shadows` defaults to **Use Light Shadows**. It samples the associated Unity Light's realtime shadow data for every contributing raymarch sample; enable realtime shadows on that Light for opaque geometry to block direct volumetric scattering. If URP cannot provide a camera-valid additional-light index or shadow-atlas slice, the direct volumetric term is suppressed rather than silently becoming unshadowed. Choosing **Off** intentionally leaves the light unshadowed. The Volumetric Light Visibility debug view displays the selected packed local-light index as white when visible and black when occluded; packing is camera-dependent because local lights are frustum-culled.

When the underlying URP light has shadows enabled, the raymarch samples URP's main or additional-light shadow map at each contributing step. Local lights are matched to URP's additional-light index by `Light` identity rather than by the bounded volumetric array position. Spot shadows use the matching atlas slice; point shadows use URP's cubemap-face selection. For a local light whose medium can be viewed across occluders outside the camera frustum, keep Unity's view-frustum shadow-caster culling disabled so those occluders remain in the light's shadow map. A light with shadows disabled still obeys camera depth termination and cone/range bounds, but it cannot account for occluders between that light and a point in the medium. Temporal reprojection remains intentionally omitted; the bounded spatial implementation stays predictable at low resolution and has no history invalidation cost.

## CRT display simulation

CRT is disabled by default and runs after presentation. Scanlines, mask, curvature, overscan, vignette, horizontal/chroma bleed, chromatic offset, signal noise, brightness, interlacing, and pixel bloom have separate controls. Curvature and overscan intentionally break exact integer presentation when enabled.

## Texture import

Select a `RetroTextureProfile` together with one or more textures, then choose **Assets > RetroPSX > Apply Texture Profile**. The command changes only the selection and controls filtering, mipmaps, maximum size, wrap mode, compression, and alpha transparency handling. It does not mutate the whole project or pack palettes/CLUTs.

## Debug views

Debug modes cover internal resolution, pixel grid, quantization error, dither pattern, fog factor, volumetric density/buffer, depth reconstruction, and final composite. Compare material geometry or affine behavior by setting its local strength to zero; the renderer does not render the scene a second time for that comparison.

## Performance and ownership

Native/full-size Stretch rendering uses one canonical resolve pass. Low-resolution rendering adds point presentation. Volumetrics add a reduced-resolution raymarch and one canonical-size composite. CRT adds one output-size pass.

All color and volume textures are transient RenderGraph resources that resize with the camera. There are no history textures, persistent RTHandles, GraphicsBuffers, CPU readbacks, per-frame materials, LINQ queries, camera searches, or scene-wide light searches. Disabled volumetrics and CRT record no image passes. Ineligible cameras receive only the small neutral setup/reset passes used to prevent state leakage.

Volumetric cost is approximately `(internal pixels / divisor²) × ray steps × active local lights`, with a hard limit of four local lights. A shadowed light adds at most one atlas comparison per contributing step; samples outside its range/cone/pattern skip the lookup. The raymarch also exits once transmittance is negligible.

## First-person recommendations

Use one camera. Reduce Geometry Snap and Affine Strength on weapon/viewmodel materials, disable material dithering if it flickers, and opt those materials out of fog when needed. The global near-camera fade attenuates snapping by positive view-space depth after the camera near plane, and vertices behind or inside the near plane are never snapped.

## Known limitations

- GPU command ordering, framebuffer mask bits, VRAM/CLUT packing, dynamic tessellation, and a cycle-accurate framebuffer are not emulated.
- Provoking-vertex triangle depth is intentionally unsupported because it breaks normal URP depth consumers and intersecting geometry.
- Camera overlay stacks are skipped; apply RetroPSX to the base camera.
- Unity Light cookies, compute/tiled light lists, and temporal accumulation are not implemented.
- Volumetric shadows use opaque URP shadow casters. Transparent geometry without a shadow-casting depth representation cannot occlude the medium exactly.
- Exact low-resolution geometry coverage requires matching URP render scale or the camera target as described above.
