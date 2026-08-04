<div align="center">

# RetroPSX-URP

**Build a rougher, lower-resolution image while keeping the project inside URP.**

A modular collection of renderer effects and Shader Graph assets for PSX-inspired Unity visuals.

[![Release](https://img.shields.io/github/v/release/Natteens/retropsx-urp?sort=semver&label=release&style=flat-square)](https://github.com/Natteens/retropsx-urp/releases)
[![Unity](https://img.shields.io/badge/Unity-6000.0%2B-000000?style=flat-square&logo=unity)](https://unity.com)
[![URP](https://img.shields.io/badge/URP-17.0.3%2B-555555?style=flat-square)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/index.html)
[![License](https://img.shields.io/github/license/Natteens/retropsx-urp?style=flat-square)](./LICENSE.md)

[What It Is](#a-look-you-can-assemble) · [Effects](#whats-included) · [Installation](#installation) · [Documentation](#documentation)

</div>

---

## A Look You Can Assemble

RetroPSX-URP does not force one fixed "retro" preset onto the project. It provides a set of pieces
that can be combined deliberately: pixelation for image structure, dithering and fog for tone, CRT
processing for display character and vertex-warping shaders for unstable geometry.

Everything stays inside the normal URP workflow. Add only the Renderer Features the scene needs and
use the supplied Shader Graph assets where the material itself should participate in the effect.

## What's Included

<table>
<tr>
<td width="50%"><strong>Screen-space effects</strong><br><sub>CRT, pixelation, dithering and fog can be enabled independently on the active renderer.</sub></td>
<td width="50%"><strong>PSX-style materials</strong><br><sub>Lit and unlit Shader Graph assets provide a starting point for the package's material language.</sub></td>
</tr>
<tr>
<td width="50%"><strong>Vertex warping</strong><br><sub>Material variants reproduce the unstable geometry associated with low-precision rendering.</sub></td>
<td width="50%"><strong>Normal URP ownership</strong><br><sub>The project keeps its renderer assets, materials, lighting and scene organization.</sub></td>
</tr>
</table>

The package is a visual toolkit, not an exact hardware emulator. Final results depend on how its
effects, materials, resolution and art direction are combined.

## Installation

Requires Unity **6000.0** or newer with URP. The compatible URP dependency is declared by the
package; Shader Graph is supplied through Unity's graphics packages.

In the Package Manager, choose **Add package from git URL** and paste:

```text
https://github.com/Natteens/retropsx-urp.git
```

Or add it to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.natteens.retropsxurp": "https://github.com/Natteens/retropsx-urp.git"
  }
}
```

Use a release tag when the project should not follow changes on `main`.

## First Pass

1. Open the active URP Renderer asset.
2. Add one Renderer Feature rather than enabling every effect at once.
3. Tune it against the actual game camera and target resolution.
4. Apply a supplied Shader Graph material where vertex behavior or surface shading should change.
5. Layer additional effects only when they support the chosen visual direction.

## Documentation

Effect setup, material workflow, practical combinations and troubleshooting are documented in
[Documentation](./Documentation~/index.md). The advanced controls stay there so this README can
remain a readable introduction rather than a settings reference.

See the [changelog](./CHANGELOG.md) for release history.

## License

MIT. See [LICENSE.md](./LICENSE.md).
