# Changelog

Notable changes to RetroPSX-URP are documented here. The project follows [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Changed

- Rebuilt the package as a coordinated, RenderGraph-first rendering pipeline.
- Replaced the legacy independent CRT, dithering, fog, pixelation, controller, Shader Graph, and subgraph implementations.

### Added

- Independent raster, geometry, color, lighting, fog, volumetric, display, debug, texture, and pattern profile types.
- Explicit Lit and Unlit PSX material shaders with integer viewport snapping, affine interpolation, vertex lighting, texture modulation, RGB quantization, ordered dithering, fog, and semi-transparency modes.
- Native and aspect-aware low-resolution resolve, point presentation, reduced-resolution raymarched volumetrics, modular CRT simulation, texture import tooling, debug views, and edit-mode tests.

## [0.1.1](https://github.com/Natteens/retropsx-urp/compare/v0.1.0...v0.1.1) - 2026-08-04

### Fixed

- Corrected the package identifier and requirements.
- Declared the URP dependency.
- Preserved UPM dependencies during automated releases.

## [0.1.0] - 2025-12-06

### Added

- Initial package structure, documentation, tests, and samples.
