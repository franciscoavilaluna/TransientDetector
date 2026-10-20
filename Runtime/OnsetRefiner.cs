using System;
using System.Collections.Generic;

namespace AudioBeatDetector
{
    public static class OnsetRefiner
    {
        public static List<BeatEvent> Refine(
            float[] mono,
            int sampleRate,
            List<BeatEvent> beats,
            BandSettings band,
            float timeOffset,
            float[] envelope,
            float[] lowBuffer)
        {
            var result = new List<BeatEvent>(beats.Count);

            if (beats.Count == 0)
            {
                return result;
            }

            BuildEnvelope(mono, sampleRate, band, envelope, lowBuffer);

            int slopeSpan = Math.Max(2, (int)(0.004f * sampleRate));
            int searchBefore = (int)(0.01f * sampleRate);
            int searchAfter = Math.Max(1, (int)(0.04f * sampleRate));

            for (int b = 0; b < beats.Count; b++)
            {
                BeatEvent beat = beats[b];
                int center = (int)((beat.time - timeOffset) * sampleRate);
                int first = Math.Max(slopeSpan, center - searchBefore);
                int last = Math.Min(envelope.Length - 1, center + searchAfter);

                float bestSlope = 0f;
                int bestIndex = -1;

                for (int n = first; n <= last; n++)
                {
                    float slope = envelope[n] - envelope[n - slopeSpan];

                    if (slope > bestSlope)
                    {
                        bestSlope = slope;
                        bestIndex = n;
                    }
                }

                if (bestIndex < 0)
                {
                    result.Add(new BeatEvent(beat.time, beat.strength, beat.band));
                    continue;
                }

                float refined = (float)(bestIndex - slopeSpan / 2) / sampleRate + timeOffset;
                result.Add(new BeatEvent(refined, beat.strength, beat.band));
            }

            result.Sort((a, b) => a.time.CompareTo(b.time));

            if (result.Count < 2)
            {
                return result;
            }

            var deduped = new List<BeatEvent>(result.Count);
            deduped.Add(result[0]);

            for (int i = 1; i < result.Count; i++)
            {
                if (result[i].time - deduped[deduped.Count - 1].time > 0.001f)
                {
                    deduped.Add(result[i]);
                }
            }

            return deduped;
        }

        static void BuildEnvelope(float[] mono, int sampleRate, BandSettings band, float[] high, float[] low)
        {
            Array.Copy(mono, high, mono.Length);
            OnePole(high, sampleRate, band.highHz, 2);

            Array.Copy(mono, low, mono.Length);
            OnePole(low, sampleRate, band.lowHz, 2);

            for (int i = 0; i < high.Length; i++)
            {
                float value = high[i] - low[i];

                if (value < 0f)
                {
                    value = -value;
                }

                high[i] = value;
            }

            float smoothHz = Math.Min(band.highHz * 0.5f, 300f);
            OnePole(high, sampleRate, smoothHz, 1);
        }

        static void OnePole(float[] data, int sampleRate, float cutoff, int passes)
        {
            float coefficient = 1f - (float)Math.Exp(-2.0 * Math.PI * cutoff / sampleRate);

            for (int pass = 0; pass < passes; pass++)
            {
                float state = 0f;

                for (int n = 0; n < data.Length; n++)
                {
                    state += coefficient * (data[n] - state);
                    data[n] = state;
                }
            }
        }
    }
}
