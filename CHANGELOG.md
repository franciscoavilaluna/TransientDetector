# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.2.0] - 2026-10-20

### Added

- `DetectorSettings.Validate(int sampleRate)` with clear `ArgumentException`s for invalid hop size, FFT size, bands, radii, normalization values and Nyquist violations.
- EditMode test suite covering silence, kick tracks, snare tracks, dynamic range, pure tones, short input, non-finite samples, invalid settings and the `AudioClip` path.
- Empty `BeatMap` arrays are now initialized instead of being left `null` for short input.

### Fixed

- False positives on steady tones: local normalization now divides by the local mean instead of the local maximum, so a sustained tone no longer looks like a sequence of onsets.
- Start/end boundary frames no longer create artificial onsets; analysis uses only complete analysis windows.
- `hopSize = 0` now throws `ArgumentOutOfRangeException` instead of `DivideByZeroException`.
- The standalone development project (`Tools~/AudioBeatDetector.csproj`) now compiles `Runtime/**/*.cs`; moving it to `Tools~` had left it building an empty assembly.
- `AnalyzeAudioAsync` now uses `ConfigureAwait(false)` for its background work.

### Changed

- Local normalization uses the local flux mean; default `delta` is `0.6` and default `localFloor` is `0.0001`.
- `Analyze` sanitizes `NaN`/`Infinity` samples and validates settings before processing.
- Package version bumped to `1.2.0`.

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
