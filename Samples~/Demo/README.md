# Beat Demo

1. Create an empty scene.
2. Add a GameObject with an `AudioSource`.
3. Assign an `AudioClip` to the `AudioSource`.
4. Set the clip import settings to **Decompress On Load** and **Preload Audio Data** (not Streaming).
5. Add the `BeatDemo` component to the same GameObject.
6. Assign the `AudioSource` to the `BeatDemo.source` field.
7. Press Play.

The demo analyzes the clip on Start, creates a `BeatMapPlayer`, and logs onsets, beats and downbeats. Use `latencyOffset` to compensate for audio output latency.
