using System;
using System.Collections.Generic;

namespace AudioBeatDetector
{
    public enum BeatBand
    {
        Custom = 0,
        Kick = 1,
        Snare = 2,
        Hat = 3,
        Full = 4
    }

    [Serializable]
    public class BeatEvent
    {
        public float time;
        public float strength;
        public BeatBand band;

        public BeatEvent()
        {
        }

        public BeatEvent(float time, float strength, BeatBand band)
        {
            this.time = time;
            this.strength = strength;
            this.band = band;
        }
    }

    [Serializable]
    public class BandResult
    {
        public string name = "band";
        public BeatBand band = BeatBand.Custom;
        public List<BeatEvent> beats = new List<BeatEvent>();
        public float[] flux = Array.Empty<float>();
        public float[] threshold = Array.Empty<float>();
    }
}
