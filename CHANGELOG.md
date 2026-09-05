## [1.1.4](https://github.com/Natteens/retropsx-urp/compare/v1.1.3...v1.1.4) (2026-09-05)


### Bug Fixes

* apply RetroPSX pipeline to RenderTexture cameras ([d4d4539](https://github.com/Natteens/retropsx-urp/commit/d4d45395dd3f1de085d0a5e7be05d26abc667eb9))

## [1.1.3](https://github.com/Natteens/retropsx-urp/compare/v1.1.2...v1.1.3) (2026-09-04)


### Bug Fixes

* Harden volumetric shadow sampling path ([deca9d0](https://github.com/Natteens/retropsx-urp/commit/deca9d002b2c335be3f82cea6bfccdf394f8322b))

## [1.1.2](https://github.com/Natteens/retropsx-urp/compare/v1.1.1...v1.1.2) (2026-09-04)


### Bug Fixes

* stabilize player rendering and offscreen cameras ([384de2d](https://github.com/Natteens/retropsx-urp/commit/384de2d186e801e351690b41d1162931f7b53131))

## [1.1.1](https://github.com/Natteens/retropsx-urp/compare/v1.1.0...v1.1.1) (2026-09-04)


### Bug Fixes

* transparency ([0fd7c55](https://github.com/Natteens/retropsx-urp/commit/0fd7c5558cf0f0ae366f091ccc9cdb76db8e3994))

# [1.1.0](https://github.com/Natteens/retropsx-urp/compare/v1.0.0...v1.1.0) (2026-08-29)


### Features

* preserve native UI rendering ([eb36bee](https://github.com/Natteens/retropsx-urp/commit/eb36bee9bd25e778099b6ae4005e0cedebeb172c))

# [1.0.0](https://github.com/Natteens/retropsx-urp/compare/v0.1.1...v1.0.0) (2026-08-29)


* feat!: rebuild RetroPSX rendering pipeline ([cb4fd97](https://github.com/Natteens/retropsx-urp/commit/cb4fd97763f2858d5c13cbdd305661f6bbc09bb8))


### BREAKING CHANGES

* removes the previous CRT, dithering, fog, pixelation, controller, Shader Graph, and legacy material APIs in favor of the new RetroPSX pipeline architecture.

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
