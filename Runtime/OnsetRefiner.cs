using System;
using System.Collections.Generic;

namespace AudioBeatDetector
{
    public static class OnsetRefiner
    {
        public static List<float> Refine(float[] mono, int sampleRate, List<float> beats, BandSettings band, float timeOffset)
        {
            List<float> result = new List<float>();

            if (beats.Count == 0)
            {
                return result;
            }

            float[] envelope = BuildEnvelope(mono, sampleRate, band);

            int slopeSpan = Math.Max(2, (int)(0.004f * sampleRate));
            int searchBefore = (int)(0.01f * sampleRate);
            int searchAfter = (int)(0.6f * band.fftSize);

            foreach (float time in beats)
            {
                int center = (int)((time - timeOffset) * sampleRate);
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
                    result.Add(time);
                    continue;
                }

                float refined = (float)(bestIndex - slopeSpan / 2) / sampleRate + timeOffset;
                result.Add(refined);
            }

            result.Sort();
            return result;
        }

        static float[] BuildEnvelope(float[] mono, int sampleRate, BandSettings band)
        {
            float[] high = new float[mono.Length];
            Array.Copy(mono, high, mono.Length);
            OnePole(high, sampleRate, band.highHz, 2);

            float[] low = new float[mono.Length];
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
            return high;
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
