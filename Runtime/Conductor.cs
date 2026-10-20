using System;
using System.Collections.Generic;

namespace AudioBeatDetector
{
    public class Conductor
    {
        public BeatMap Map { get; private set; }
        public float LatencyOffset { get; set; }

        int _onsetIndex;
        int _beatIndex;
        int _downbeatIndex;

        public int OnsetIndex => _onsetIndex;
        public int BeatIndex => _beatIndex;
        public int DownbeatIndex => _downbeatIndex;

        public void Reset(BeatMap map, float songTime)
        {
            Map = map;

            if (map == null)
            {
                _onsetIndex = 0;
                _beatIndex = 0;
                _downbeatIndex = 0;
                return;
            }

            _onsetIndex = LowerBound(map.onsets, songTime);
            _beatIndex = LowerBound(map.beatGrid, songTime);
            _downbeatIndex = LowerBound(map.downbeats, songTime);
        }

        public void Tick(
            float songTime,
            Action<BeatEvent> onOnset,
            Action<BeatEvent> onBeat,
            Action<BeatEvent> onDownbeat)
        {
            if (Map == null)
            {
                return;
            }

            while (_onsetIndex < Map.onsets.Count &&
                   Map.onsets[_onsetIndex].time + LatencyOffset <= songTime)
            {
                onOnset?.Invoke(Map.onsets[_onsetIndex]);
                _onsetIndex++;
            }

            while (_beatIndex < Map.beats.Count &&
                   Map.beats[_beatIndex].time + LatencyOffset <= songTime)
            {
                onBeat?.Invoke(Map.beats[_beatIndex]);
                _beatIndex++;
            }

            while (_downbeatIndex < Map.downbeats.Count &&
                   Map.downbeats[_downbeatIndex] + LatencyOffset <= songTime)
            {
                onDownbeat?.Invoke(
                    new BeatEvent(Map.downbeats[_downbeatIndex], 1f, BeatBand.Full));
                _downbeatIndex++;
            }
        }

        static int LowerBound(List<BeatEvent> events, float time)
        {
            int lo = 0;
            int hi = events.Count;

            while (lo < hi)
            {
                int mid = (lo + hi) / 2;

                if (events[mid].time < time)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            return lo;
        }

        static int LowerBound(List<float> times, float time)
        {
            int lo = 0;
            int hi = times.Count;

            while (lo < hi)
            {
                int mid = (lo + hi) / 2;

                if (times[mid] < time)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            return lo;
        }
    }
}
