using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using MathNet.Numerics.IntegralTransforms;
using UnityEngine;

namespace AudioBeatDetector
{
    [Serializable]
    public class BandSettings
    {
        public string name = "band";
        public float lowHz = 35f;
        public float highHz = 130f;
        public int fftSize = 2048;
        public bool vibratoSuppression = false;
        public float delta = 0.15f;
        public float noiseFloor = 0.08f;
        public float minInterval = 0.10f;

        public BandSettings()
        {
        }

        public BandSettings(string name, float lowHz, float highHz, float delta, float noiseFloor, float minInterval)
        {
            this.name = name;
            this.lowHz = lowHz;
            this.highHz = highHz;
            this.delta = delta;
            this.noiseFloor = noiseFloor;
            this.minInterval = minInterval;
        }
    }

    [Serializable]
    public class DetectorSettings
    {
        public int hopSize = 512;
        public float logGamma = 100f;
        public int medianRadius = 8;
        public int peakRadius = 3;
        public float timeOffset = 0f;
        public BandSettings kick;
        public BandSettings snare;

        public DetectorSettings()
        {
            kick = new BandSettings("kick", 40f, 110f, 0.15f, 0.08f, 0.10f);
            kick.fftSize = 4096;
            kick.vibratoSuppression = true;

            snare = new BandSettings("snare", 1500f, 5000f, 0.15f, 0.08f, 0.12f);
        }
    }

    public class BeatMap
    {
        public List<float> kickBeats = new List<float>();
        public List<float> snareBeats = new List<float>();

        public float[] frameTimes;
        public float[] kickFlux;
        public float[] kickThreshold;
        public float[] snareFlux;
        public float[] snareThreshold;
    }

    public static class TransientDetector
    {
        public static BeatMap AnalyzeAudio(AudioClip song)
        {
            return AnalyzeAudio(song, new DetectorSettings());
        }

        public static BeatMap AnalyzeAudio(AudioClip song, DetectorSettings settings)
        {
            float[] mono = ToMono(song);
            return Analyze(mono, song.frequency, settings);
        }

        public static async Task<BeatMap> AnalyzeAudioAsync(AudioClip song, DetectorSettings settings)
        {
            if (settings == null)
            {
                settings = new DetectorSettings();
            }

            float[] mono = ToMono(song);
            int sampleRate = song.frequency;

            BeatMap result = await Task.Run(() => Analyze(mono, sampleRate, settings));
            return result;
        }

        public static float[] ToMono(AudioClip clip)
        {
            if (clip == null)
            {
                throw new ArgumentNullException("clip");
            }

            if (clip.loadType == AudioClipLoadType.Streaming)
            {
                throw new InvalidOperationException("El clip " + clip.name + " esta en Streaming. Usa Decompress On Load.");
            }

            if (clip.loadState == AudioDataLoadState.Unloaded)
            {
                clip.LoadAudioData();
            }

            int channels = clip.channels;
            int frames = clip.samples;
            float[] raw = new float[frames * channels];

            bool ok = clip.GetData(raw, 0);
            if (!ok)
            {
                throw new InvalidOperationException("GetData fallo en " + clip.name + ". Activa Preload Audio Data y Decompress On Load.");
            }

            if (channels == 1)
            {
                return raw;
            }

            float[] mono = new float[frames];
            float inverse = 1f / channels;

            for (int i = 0; i < frames; i++)
            {
                float sum = 0f;
                int baseIndex = i * channels;

                for (int c = 0; c < channels; c++)
                {
                    sum += raw[baseIndex + c];
                }

                mono[i] = sum * inverse;
            }

            return mono;
        }

        public static BeatMap Analyze(float[] mono, int sampleRate, DetectorSettings settings)
        {
            BeatMap map = new BeatMap();
            int hop = settings.hopSize;

            if (mono == null || mono.Length < 4 * hop)
            {
                return map;
            }

            int frames = mono.Length / hop + 1;
            float[] times = new float[frames];

            for (int f = 0; f < frames; f++)
            {
                times[f] = (float)((double)f * hop / sampleRate) + settings.timeOffset;
            }

            BandSettings[] bands = new BandSettings[2];
            bands[0] = settings.kick;
            bands[1] = settings.snare;

            for (int b = 0; b < 2; b++)
            {
                float[] flux = BandFlux(mono, sampleRate, hop, frames, bands[b], settings.logGamma);
                Normalize(flux);
                float[] threshold = LocalMedianThreshold(flux, settings.medianRadius, bands[b].delta);
                List<float> beats = PickPeaks(flux, threshold, times, bands[b], settings.peakRadius);

                if (b == 0)
                {
                    map.kickBeats = beats;
                    map.kickFlux = flux;
                    map.kickThreshold = threshold;
                }
                else
                {
                    map.snareBeats = beats;
                    map.snareFlux = flux;
                    map.snareThreshold = threshold;
                }
            }

            map.frameTimes = times;
            return map;
        }

        static float[] BandFlux(float[] mono, int sampleRate, int hop, int frames, BandSettings band, float gamma)
        {
            int n = band.fftSize;
            int half = n / 2;

            float[] hann = new float[n];
            for (int i = 0; i < n; i++)
            {
                hann[i] = 0.5f * (1f - (float)Math.Cos(2.0 * Math.PI * i / n));
            }

            double norm = 4.0 / n;

            int lo = ClampInt(Mathf.RoundToInt(band.lowHz * n / sampleRate), 2, half - 3);
            int hi = ClampInt(Mathf.RoundToInt(band.highHz * n / sampleRate), lo, half - 2);

            float[] flux = new float[frames];
            Complex[] buffer = new Complex[n];
            float[] prev = new float[half + 1];
            float[] cur = new float[half + 1];

            for (int f = 0; f < frames; f++)
            {
                int start = f * hop - n / 2;

                for (int i = 0; i < n; i++)
                {
                    int index = start + i;
                    float x = 0f;

                    if (index >= 0 && index < mono.Length)
                    {
                        x = mono[index];
                    }

                    buffer[i] = new Complex(x * hann[i], 0.0);
                }

                Fourier.Forward(buffer, FourierOptions.NoScaling);

                for (int k = lo - 1; k <= hi + 1; k++)
                {
                    cur[k] = (float)Math.Log(1.0 + gamma * buffer[k].Magnitude * norm);
                }

                if (f > 0)
                {
                    float sum = 0f;

                    for (int k = lo; k <= hi; k++)
                    {
                        float reference = prev[k];

                        if (band.vibratoSuppression)
                        {
                            reference = Math.Max(prev[k], Math.Max(prev[k - 1], prev[k + 1]));
                        }

                        float difference = cur[k] - reference;

                        if (difference > 0f)
                        {
                            sum += difference;
                        }
                    }

                    flux[f] = sum / (hi - lo + 1);
                }

                float[] temp = prev;
                prev = cur;
                cur = temp;
            }

            return flux;
        }

        static int ClampInt(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        static void Normalize(float[] flux)
        {
            float[] sorted = new float[flux.Length];
            Array.Copy(flux, sorted, flux.Length);
            Array.Sort(sorted);

            int percentileIndex = ClampInt((int)(0.98f * (sorted.Length - 1)), 0, sorted.Length - 1);
            float reference = sorted[percentileIndex];

            if (reference <= 0.000001f)
            {
                Array.Clear(flux, 0, flux.Length);
                return;
            }

            float inverse = 1f / reference;

            for (int i = 0; i < flux.Length; i++)
            {
                flux[i] = flux[i] * inverse;
            }
        }

        static float[] LocalMedianThreshold(float[] flux, int radius, float delta)
        {
            int count = flux.Length;
            float[] threshold = new float[count];
            float[] window = new float[radius * 2 + 1];

            for (int i = 0; i < count; i++)
            {
                int first = Math.Max(0, i - radius);
                int last = Math.Min(count - 1, i + radius);
                int length = last - first + 1;

                Array.Copy(flux, first, window, 0, length);
                Array.Sort(window, 0, length);

                threshold[i] = window[length / 2] + delta;
            }

            return threshold;
        }

        static List<float> PickPeaks(float[] flux, float[] threshold, float[] times, BandSettings band, int radius)
        {
            List<float> beats = new List<float>();
            float lastValue = 0f;

            for (int i = 1; i < flux.Length - 1; i++)
            {
                float value = flux[i];

                if (value < band.noiseFloor || value < threshold[i])
                {
                    continue;
                }

                bool isMaximum = true;
                int first = Math.Max(0, i - radius);
                int last = Math.Min(flux.Length - 1, i + radius);

                for (int j = first; j <= last; j++)
                {
                    if (j == i)
                    {
                        continue;
                    }

                    if (flux[j] > value)
                    {
                        isMaximum = false;
                        break;
                    }

                    if (j < i && flux[j] == value)
                    {
                        isMaximum = false;
                        break;
                    }
                }

                if (!isMaximum)
                {
                    continue;
                }

                float time = times[i];

                if (beats.Count > 0 && time - beats[beats.Count - 1] < band.minInterval)
                {
                    if (value > lastValue)
                    {
                        beats[beats.Count - 1] = time;
                        lastValue = value;
                    }

                    continue;
                }

                beats.Add(time);
                lastValue = value;
            }

            return beats;
        }
    }
}
