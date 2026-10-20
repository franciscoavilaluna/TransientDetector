# Audio Beat and Transient Detector

Onset and transient detection for Unity based on spectral flux, with kick/snare bands and onset refinement.

## Requirements

- Unity 2021.3 or newer.
- `AudioClip` import settings: **Decompress On Load**, **Preload Audio Data** enabled, **not Streaming**.

## Installation

Latest branch:

```text
https://github.com/franciscoavilaluna/TransientDetector.git#chore/package-p0
```

Stable release:

```text
https://github.com/franciscoavilaluna/TransientDetector.git#v1.1.0
```

Or add it to your project's `Packages/manifest.json`:

```json
"com.franciscoavilaluna.transientdetector": "https://github.com/franciscoavilaluna/TransientDetector.git#chore/package-p0"
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

## Where is the package installed?

When installed from a Git URL, Unity does **not** copy the package into the physical `Packages/` folder. It caches it under:

```text
Library/PackageCache/com.franciscoavilaluna.transientdetector@<hash>/
```

Inside that folder you will find `Runtime/`, `Tests/`, `package.json`, etc.

In the Unity Project window the package still appears as:

```text
Packages/com.franciscoavilaluna.transientdetector/Runtime
```

To inspect it on disk:

```bash
find Library/PackageCache -maxdepth 1 -name 'com.franciscoavilaluna.transientdetector*'
```

If you use a local `file:` dependency, the package stays in the referenced folder instead of `Library/PackageCache`.

## Tests

The package includes EditMode tests. To run them from a consuming project:

1. Make sure `com.unity.test-framework` is installed.
2. Add the package to `testables` in `Packages/manifest.json`:

```json
"testables": [
  "com.franciscoavilaluna.transientdetector"
]
```

3. Open `Window > General > Test Runner > EditMode` and run the tests.

The suite covers silence, kick/snare tracks, dynamic range, pure tones, short input, non-finite samples, invalid settings and the `AudioClip` path.

## Current limitations

- Offline analysis only; there is no real-time streaming mode yet.
- The output is a list of onsets, not a BPM/beat grid/downbeat map yet.
- Kick attacks can also trigger the snare band, so one physical hit may appear in both lists.
- Not tested on WebGL.
- Unity 2021.3 LTS compatibility is not verified in CI; testing has been done on Unity 6.
- Long clips use significant memory; analyze them on a loading screen.

## License

MIT. MathNet.Numerics is MIT; see `THIRD_PARTY_NOTICES.md`.
