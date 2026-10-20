using System;
using UnityEngine;

namespace AudioBeatDetector
{
    [AddComponentMenu("Audio Beat Detector/Beat Map Player")]
    public class BeatMapPlayer : MonoBehaviour
    {
        public AudioSource source;
        public BeatMapAsset beatMap;
        [Tooltip("Seconds added to every event. Use it to compensate audio output latency.")]
        public float latencyOffset = 0f;
        public bool playOnStart = false;

        public event Action<BeatEvent> OnOnset;
        public event Action<BeatEvent> OnBeat;
        public event Action<BeatEvent> OnDownbeat;

        Conductor _conductor;
        double _startDspTime;
        float _pausedTime;
        bool _playing;

        public bool IsPlaying => _playing;
        public float SongTime => _playing
            ? (float)(AudioSettings.dspTime - _startDspTime)
            : _pausedTime;

        void Start()
        {
            if (playOnStart && beatMap != null && beatMap.map != null)
            {
                Play(beatMap.map);
            }
        }

        public void Play()
        {
            if (beatMap == null || beatMap.map == null)
            {
                throw new InvalidOperationException("No BeatMapAsset assigned.");
            }

            Play(beatMap.map);
        }

        public void Play(BeatMap map)
        {
            if (source == null)
            {
                throw new InvalidOperationException("No AudioSource assigned.");
            }

            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            _conductor = new Conductor
            {
                LatencyOffset = latencyOffset
            };

            _conductor.Reset(map, 0f);
            _startDspTime = AudioSettings.dspTime;
            _pausedTime = 0f;
            source.time = 0f;
            source.Play();
            _playing = true;
        }

        public void Pause()
        {
            if (!_playing)
            {
                return;
            }

            _pausedTime = SongTime;
            source.Pause();
            _playing = false;
        }

        public void Resume()
        {
            if (_playing || _conductor == null || source == null)
            {
                return;
            }

            _startDspTime = AudioSettings.dspTime - _pausedTime;
            source.UnPause();
            _playing = true;
        }

        public void Seek(float songTime)
        {
            if (_conductor == null || source == null)
            {
                return;
            }

            songTime = Mathf.Max(0f, songTime);
            source.time = songTime;
            _startDspTime = AudioSettings.dspTime - songTime;
            _pausedTime = songTime;
            _conductor.LatencyOffset = latencyOffset;
            _conductor.Reset(_conductor.Map, songTime);
        }

        public void Stop()
        {
            if (source != null)
            {
                source.Stop();
            }

            _playing = false;
            _pausedTime = 0f;
        }

        void Update()
        {
            if (!_playing || _conductor == null)
            {
                return;
            }

            _conductor.Tick(SongTime, FireOnset, FireBeat, FireDownbeat);
        }

        void FireOnset(BeatEvent beat)
        {
            OnOnset?.Invoke(beat);
        }

        void FireBeat(BeatEvent beat)
        {
            OnBeat?.Invoke(beat);
        }

        void FireDownbeat(BeatEvent beat)
        {
            OnDownbeat?.Invoke(beat);
        }

        void OnValidate()
        {
            latencyOffset = Mathf.Clamp(latencyOffset, -1f, 1f);
        }
    }
}
