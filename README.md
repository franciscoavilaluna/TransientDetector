# Audio Beat and Transient Detector

Onset, beat and tempo analysis for Unity. The package detects transients with spectral flux, estimates BPM, builds a beat grid and downbeats, exposes onset strength, and includes a runtime conductor, a JSON cache and an Editor analysis window.

## Features

- Spectral-flux onset detection with kick, snare, hi-hat and full-band detectors.
- Onset strength and band labels via `BeatEvent`.
- BPM estimation with a beat grid and 4/4 downbeats.
- Quantization helpers (`QuantizeTime`, `GetBeatIndex`, `GetBeatTime`).
- Runtime `Conductor` and `BeatMapPlayer` using `AudioSettings.dspTime`.
- JSON/ScriptableObject caching of analysis results.
- Editor window with waveform, onset, beat and downbeat visualization.
- Own radix-2 FFT; no external DSP dependency.
- EditMode test suite.

## Requirements

- Unity 2021.3 or newer.
- `AudioClip` import settings: **Decompress On Load**, **Preload Audio Data** enabled, **not Streaming**.

## Installation

Stable release:

```text
https://github.com/franciscoavilaluna/TransientDetector.git#v2.0.0
```

Or add it to your project's `Packages/manifest.json`:

```json
"com.franciscoavilaluna.transientdetector": "https://github.com/franciscoavilaluna/TransientDetector.git#v2.0.0"
```

## Quick start

```csharp
using AudioBeatDetector;
using UnityEngine;

public class BeatDemo : MonoBehaviour
{
    public AudioClip clip;

    async void Start()
    {
        BeatMap map = await TransientDetector.AnalyzeAudioAsync(clip, new DetectorSettings());

        Debug.Log($"BPM: {map.bpm:F2} (confidence {map.tempoConfidence:F2})");
        Debug.Log($"Onsets: {map.onsets.Count}, beats: {map.beatGrid.Count}, downbeats: {map.downbeats.Count}");

        foreach (BeatEvent onset in map.onsets)
            Debug.Log($"{onset.band} @ {onset.time:F3}s strength={onset.strength:F2}");

        foreach (float downbeat in map.downbeats)
            Debug.Log($"downbeat @ {downbeat:F3}s");
    }
}
```

`BeatMap` also keeps the legacy `kickBeats` and `snareBeats` lists for compatibility.

## Runtime playback

Add a `BeatMapPlayer` to a GameObject with an `AudioSource`, assign a `BeatMapAsset`, and call `Play()`:

```csharp
using AudioBeatDetector;
using UnityEngine;

public class PlayBeats : MonoBehaviour
{
    public BeatMapPlayer player;

    void Start()
    {
        player.OnOnset += onset => Debug.Log($"onset {onset.time:F3}");
        player.OnDownbeat += beat => Debug.Log($"downbeat {beat.time:F3}");
        player.Play();
    }
}
```

`BeatMapPlayer` drives events with `AudioSettings.dspTime`. Use `latencyOffset` to compensate for audio output latency. Positive values delay the visual events to match the heard audio.

## Cache, save and load

```csharp
BeatMap map = BeatMapCache.GetOrAnalyze(clip, new DetectorSettings());
BeatMapCache.SaveToFile(map, "Assets/beatmap.json");
BeatMap loaded = BeatMapCache.LoadFromFile("Assets/beatmap.json");
```

You can also store a `BeatMap` in a `BeatMapAsset` (`Create > Audio Beat Detector > Beat Map Asset`) and assign it to `BeatMapPlayer`.

## Editor window

Open:

```text
Window > Audio Beat Detector > Beat Analysis
```

Select an `AudioClip`, tweak the detector settings, press **Analyze**, and inspect the waveform, onsets, beat grid, downbeats, BPM and confidence.

## Tests

The package includes EditMode tests. In a consuming project:

1. Install `com.unity.test-framework`.
2. Add the package to `testables` in `Packages/manifest.json`:

```json
"testables": [
  "com.franciscoavilaluna.transientdetector"
]
```

3. Open `Window > General > Test Runner > EditMode` and run the tests.

The suite covers silence, kick/snare/hat tracks, dynamic range, pure tones, tempo from 60 to 180 BPM, 16th-note hi-hats, quantization, the conductor, JSON/cache round-trips, FFT correctness against a direct DFT, cancellation and progress.

A CI-ready Unity project lives in `Tests~/UnityProject`. The GitHub Actions workflow uses Unity 2021.3 LTS via `game-ci/unity-test-runner` and requires a `UNITY_LICENSE` repository secret.

## Where is the package installed?

When installed from a Git URL, Unity does **not** copy the package into the physical `Packages/` folder. It caches it under:

```text
Library/PackageCache/com.franciscoavilaluna.transientdetector@<hash>/
```

In the Unity Project window it still appears as:

```text
Packages/com.franciscoavilaluna.transientdetector/Runtime
```

To inspect it on disk:

```bash
find Library/PackageCache -maxdepth 1 -name 'com.franciscoavilaluna.transientdetector*'
```

If you use a local `file:` dependency, it stays in the referenced folder instead of `Library/PackageCache`.

## Current limitations

- Offline analysis only; there is no real-time streaming detector yet.
- The snare band can also react to the broadband attack of a kick, so one physical hit may appear in both band lists.
- Tempo estimation assumes a mostly constant tempo. Long tempo changes or very dense subdivisions may need tuning of `minBpm`, `maxBpm`, `beatPriorBpm` and `tempoPriorWidthOctaves`.
- Long clips use significant memory; analyze them on a loading screen and cache the result.
- At sample rates below 32 kHz, lower `full.highHz` and `hat.highHz` below the Nyquist frequency or disable those bands; otherwise validation throws.
- WebGL is not supported for threaded analysis. The synchronous `Analyze` path can be used on small clips, but it blocks the main thread.
- The CI workflow is configured for Unity 2021.3 LTS. Local verification in this repository was done on Unity 6; running CI requires a `UNITY_LICENSE` secret.

## Architecture

- `Runtime/TransientDetector.cs` — analysis pipeline, settings, `BeatMap`.
- `Runtime/TempoEstimator.cs` — BPM, beat grid and downbeats.
- `Runtime/BeatEvents.cs` — `BeatEvent`, `BandResult`, `BeatBand`.
- `Runtime/OnsetRefiner.cs` — sub-frame onset refinement.
- `Runtime/FastFourierTransform.cs` — radix-2 FFT.
- `Runtime/Conductor.cs`, `BeatMapPlayer.cs` — runtime scheduling.
- `Runtime/BeatMapCache.cs`, `BeatMapAsset.cs` — persistence.
- `Editor/BeatAnalysisWindow.cs` — analysis and visualization window.
- `Samples~/Demo` — runtime demo.
- `Tests/EditMode` — EditMode tests.
- `Tests~/UnityProject` — Unity project used by CI.

## License

MIT.
