# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.0] - 2026-10-20

### Added

- Onset refinement (`OnsetRefiner`) using a band-pass envelope and steepest-slope search.
- Local flux normalization and configurable refinement options in `DetectorSettings`.

### Fixed

- The package now ships `MathNet.Numerics.dll` in `Runtime/Plugins`, so it compiles in a clean Unity project.
- Removed build artifacts (`bin/`, `obj/`) and stale `.meta` files from the package.

### Changed

- Moved the standalone `.csproj`/`.slnx` development files to `Tools~/`.

## [1.0.0] - 2026-10-08

### Added

- Initial spectral-flux transient detector with kick and snare bands.
