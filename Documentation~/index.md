# RetroPSX-URP documentation

RetroPSX-URP provides a small set of URP effects and Shader Graph assets for building a PlayStation-inspired visual style.

## Included effects

The package includes renderer features for:

- CRT presentation.
- Dithering.
- Fog.
- Pixelation.

It also includes lit and unlit Shader Graph assets with PSX-style vertex warping and material variants for common opaque and transparent use cases.

## Setup

1. Install the package in a URP project.
2. Select the active URP Renderer asset.
3. Add the renderer features needed by the project.
4. Configure each feature in the renderer inspector.
5. Assign the provided Shader Graph shaders or materials to scene objects that need vertex warping or the package material style.

Use only the effects the project needs. Combining every feature at high intensity usually reduces readability instead of producing a more convincing retro result.

## Suggested workflow

1. Start with pixelation or a lower internal resolution.
2. Add vertex warping to selected materials.
3. Introduce dithering to control color transitions.
4. Add fog to simplify distant geometry.
5. Use CRT treatment as the final presentation layer when appropriate.

Tune the effects against the target game camera and art direction rather than treating the presets as a fixed recreation of original hardware.

## URP notes

- The package supports the Universal Render Pipeline only.
- Shader Graph is provided through Unity's graphics packages and does not need a separate package declaration here.
- Renderer features must be added to every URP Renderer asset used by cameras that need the effect.
- Some effects depend on camera color or depth data. Confirm the relevant URP options if a pass does not render.

## Troubleshooting

### A renderer effect is missing

Confirm that the camera uses the renderer containing the feature and that the feature is enabled.

### A Shader Graph material appears incorrect

Confirm that the project is using URP, the graph compiled without errors and the material uses the intended lit, unlit or transparent graph.

### The image is too unstable

Reduce vertex warping, dithering or pixelation strength. Apply the strongest distortion selectively instead of globally.

## Requirements

- Unity 6000.0 or newer.
- Universal Render Pipeline 17.0.3 or a compatible version supplied by the Editor.