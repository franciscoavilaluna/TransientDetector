using UnityEngine;
using AudioBeatDetector;

[AddComponentMenu("Audio Beat Detector/Beat Demo")]
public class BeatDemo : MonoBehaviour
{
    public AudioSource source;
    [Range(-0.5f, 0.5f)]
    public float latencyOffset = 0f;
    public bool logOnsets = true;
    public bool logBeats = true;

    async void Start()
    {
        if (source == null || source.clip == null)
        {
            Debug.LogWarning("BeatDemo: assign an AudioSource with an AudioClip.");
            return;
        }

        var settings = new DetectorSettings();
        BeatMap map = await TransientDetector.AnalyzeAudioAsync(source.clip, settings);

        Debug.Log($"BeatDemo: bpm={map.bpm:F2} confidence={map.tempoConfidence:F2} onsets={map.onsets.Count} beats={map.beatGrid.Count} downbeats={map.downbeats.Count}");

        var player = gameObject.AddComponent<BeatMapPlayer>();
        player.source = source;
        player.latencyOffset = latencyOffset;

        if (logOnsets)
        {
            player.OnOnset += onset => Debug.Log($"BeatDemo: {onset.band} onset @ {onset.time:F3}s strength={onset.strength:F2}");
        }

        if (logBeats)
        {
            player.OnBeat += beat => Debug.Log($"BeatDemo: beat @ {beat.time:F3}s");
            player.OnDownbeat += beat => Debug.Log($"BeatDemo: DOWNBEAT @ {beat.time:F3}s");
        }

        player.Play(map);
    }
}
