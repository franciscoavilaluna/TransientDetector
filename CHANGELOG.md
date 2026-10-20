# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.1] - 2026-10-20

### Fixed

- Tempo estimation on real music: lower acceptance threshold, 0.5 BPM search resolution and a near-tie tie-break based on explained onsets.
- Verified on real tracks: Get Lucky (116 BPM), Smells Like Teen Spirit (115.5), Psycho Killer (123), SICKO MODE (155).
- Synthetic 60/90/120/150/180 BPM tracks, 16th-note hats, silence and pure tones still pass.

## [2.0.0] - 2026-10-20

### Added

- Multi-band analysis: kick, snare, hi-hat and full-band detectors.
- `BeatEvent` with time, strength and band; `BandResult` per band.
- BPM estimation, beat grid, downbeats and quantization helpers.
- `Conductor` and `BeatMapPlayer` using `AudioSettings.dspTime`.
- `BeatMapCache` with JSON save/load and `BeatMapAsset`.
- Editor analysis window with waveform, onset, beat and downbeat visualization.
- `Samples~/Demo` runtime sample.
- `Tests~/UnityProject` CI-ready Unity project.
- 27 EditMode tests covering detection, tempo, grid, quantization, conductor, cache/JSON, FFT, cancellation and progress.
- Cancellation, progress reporting and chunked multichannel audio reads.
- Own radix-2 FFT (`FastFourierTransform`).

### Changed

- `BeatMap` now exposes `onsets`, `beats`, `beatGrid`, `downbeats`, `bands`, `bpm`, `tempoConfidence`, `duration`, `sampleRate`, `firstBeatTime`, `beatPeriod`, `energy` and `algorithmVersion`.
- `BandSettings` now includes `band` and `refineOnsets`.
- Tempo defaults: `minBpm = 60`, `maxBpm = 200`, `beatPriorBpm = 120`, `tempoPriorWidthOctaves = 1.0`.
- High bands use shared FFT work and skip time-domain refinement for faster analysis.

### Removed

- MathNet.Numerics dependency and vendored DLL.
- `THIRD_PARTY_NOTICES.md` (no third-party runtime dependency remains).

### Fixed

- False positives on steady tones.
- Boundary-frame false onsets.
- `hopSize = 0` now throws `ArgumentOutOfRangeException`.
- Empty `BeatMap` arrays for short input.
- `Tools~/AudioBeatDetector.csproj` now compiles the Runtime sources.

## [1.2.0] - 2026-10-20

### Added

- `DetectorSettings.Validate(int sampleRate)`.
- EditMode test suite.
- Empty `BeatMap` arrays for short input.

### Fixed

- Steady-tone false positives via local-mean normalization.
- Boundary frames no longer create artificial onsets.
- `hopSize = 0` throws `ArgumentOutOfRangeException`.
- `Tools~/AudioBeatDetector.csproj` compiles `Runtime/**/*.cs`.

## [1.1.0] - 2026-10-20

### Added

- Onset refinement (`OnsetRefiner`).
- Local flux normalization.

### Fixed

- Package ships `MathNet.Numerics.dll` for clean Unity installs.
- Removed `bin/`, `obj/` and stale `.meta` files.

## [1.0.0] - 2026-10-08

### Added

- Initial spectral-flux transient detector with kick and snare bands.
