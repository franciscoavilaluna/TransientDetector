# Audio Beat and Transient Detector

Onset and transient detection for Unity based on spectral flux, with kick/snare bands and onset refinement.

## Requirements

- Unity 2021.3 or newer.
- `AudioClip` import settings: **Decompress On Load**, **Preload Audio Data** enabled, **not Streaming**.

## Installation

Add the package via UPM Git URL:

```text
https://github.com/franciscoavilaluna/TransientDetector.git#v1.1.0
```

Or add it to your project's `Packages/manifest.json`:

```json
"com.franciscoavilaluna.transientdetector": "https://github.com/franciscoavilaluna/TransientDetector.git#v1.1.0"
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
        var map = await TransientDetector.AnalyzeAudioAsync(clip, new DetectorSettings());

        foreach (var t in map.kickBeats)
            Debug.Log($"kick @ {t:F3}s");

        foreach (var t in map.snareBeats)
            Debug.Log($"snare @ {t:F3}s");
    }
}
```

## Current limitations

- Offline analysis only; there is no real-time streaming mode yet.
- The output is a list of onsets, not a BPM/beat grid/downbeat map yet.
- Not tested on WebGL.
- Long clips use significant memory; analyze them on a loading screen.

## License

MIT. MathNet.Numerics is MIT; see `THIRD_PARTY_NOTICES.md`.

