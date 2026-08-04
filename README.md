<div align="center">

# RetroPSX-URP

A compact set of PSX-inspired rendering tools for Unity URP.

[![Release](https://img.shields.io/github/v/release/Natteens/retropsx-urp?style=flat-square)](https://github.com/Natteens/retropsx-urp/releases)
[![Unity](https://img.shields.io/badge/Unity-6000.0%2B-000000?style=flat-square&logo=unity)](https://unity.com)
[![URP](https://img.shields.io/badge/URP-17.0.3%2B-555555?style=flat-square)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/index.html)
[![License](https://img.shields.io/github/license/Natteens/retropsx-urp?style=flat-square)](LICENSE.md)

</div>

RetroPSX-URP combines renderer features and Shader Graph assets for projects that want a stylized low-resolution look without replacing the normal URP workflow.

## Features

- CRT, dithering, fog and pixelation renderer effects.
- Lit and unlit PSX-style Shader Graph assets.
- Vertex warping material variants.
- URP-native project integration.

## Installation

Add the package through `Window > Package Manager > Add package from git URL`:

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

The URP dependency is declared by the package. Shader Graph is supplied through Unity's graphics packages.

## Quick start

1. Select the active URP Renderer asset.
2. Add the renderer features needed by the project.
3. Configure their intensity in the renderer inspector.
4. Use the provided Shader Graph assets on materials that need vertex warping or the package material style.

## Documentation

Effect setup, workflow guidance and troubleshooting are available in [Documentation](Documentation~/index.md).

## License

MIT. See [LICENSE.md](LICENSE.md).