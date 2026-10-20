using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace AudioBeatDetector
{
    [Serializable]
    public class BandSettings
    {
        public string name = "band";
        public BeatBand band = BeatBand.Custom;
        public float lowHz = 35f;
        public float highHz = 130f;
        public int fftSize = 2048;
        public bool vibratoSuppression = false;
        public bool refineOnsets = true;
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
        public bool refineOnsets = true;
        public bool localNormalization = true;
        public float localWindowSeconds = 3f;
        public float localFloor = 0.0001f;
        public BandSettings kick;
        public BandSettings snare;
        public bool analyzeHat = true;
        public BandSettings hat;
        public bool analyzeFullBand = true;
        public BandSettings full;
        public int beatsPerBar = 4;
        public float minBpm = 60f;
        public float maxBpm = 200f;
        public float beatPriorBpm = 120f;
        public float tempoPriorWidthOctaves = 1.0f;

        public DetectorSettings()
        {
            kick = new BandSettings("kick", 40f, 110f, 0.6f, 0.08f, 0.10f)
            {
                band = BeatBand.Kick,
                fftSize = 4096,
                vibratoSuppression = true
            };

            snare = new BandSettings("snare", 1500f, 5000f, 0.6f, 0.08f, 0.12f)
            {
                band = BeatBand.Snare
            };

            hat = new BandSettings("hat", 5000f, 10000f, 0.6f, 0.08f, 0.05f)
            {
                band = BeatBand.Hat,
                refineOnsets = false
            };

            full = new BandSettings("full", 40f, 16000f, 0.6f, 0.08f, 0.05f)
            {
                band = BeatBand.Full,
                vibratoSuppression = true,
                refineOnsets = false
            };
        }

        public BandSettings[] GetActiveBands()
        {
            var bands = new List<BandSettings>(4);

            if (kick != null)
            {
                bands.Add(kick);
            }

            if (snare != null)
            {
                bands.Add(snare);
            }

            if (analyzeHat && hat != null)
            {
                bands.Add(hat);
            }

            if (analyzeFullBand && full != null)
            {
                bands.Add(full);
            }

            return bands.ToArray();
        }

        public void Validate(int sampleRate)
        {
            if (sampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate), "sampleRate must be greater than zero.");
            }

            if (hopSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hopSize), "hopSize must be greater than zero.");
            }

            if (!IsFinite(logGamma) || logGamma <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(logGamma), "logGamma must be finite and greater than zero.");
            }

            if (medianRadius < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(medianRadius), "medianRadius must be at least 1.");
            }

            if (peakRadius < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(peakRadius), "peakRadius must be at least 1.");
            }

            if (!IsFinite(localWindowSeconds) || localWindowSeconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(localWindowSeconds), "localWindowSeconds must be finite and greater than zero.");
            }

            if (!IsFinite(localFloor) || localFloor <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(localFloor), "localFloor must be finite and greater than zero.");
            }

            if (!IsFinite(timeOffset))
            {
                throw new ArgumentOutOfRangeException(nameof(timeOffset), "timeOffset must be finite.");
            }

            if (beatsPerBar < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(beatsPerBar), "beatsPerBar must be at least 1.");
            }

            if (!IsFinite(minBpm) || minBpm <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(minBpm), "minBpm must be finite and greater than zero.");
            }

            if (!IsFinite(maxBpm) || maxBpm <= minBpm)
            {
                throw new ArgumentOutOfRangeException(nameof(maxBpm), "maxBpm must be finite and greater than minBpm.");
            }

            if (!IsFinite(beatPriorBpm) || beatPriorBpm <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(beatPriorBpm), "beatPriorBpm must be finite and greater than zero.");
            }

            if (!IsFinite(tempoPriorWidthOctaves) || tempoPriorWidthOctaves <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(tempoPriorWidthOctaves), "tempoPriorWidthOctaves must be finite and greater than zero.");
            }

            ValidateBand(kick, nameof(kick), sampleRate);
            ValidateBand(snare, nameof(snare), sampleRate);

            if (analyzeHat)
            {
                ValidateBand(hat, nameof(hat), sampleRate);
            }

            if (analyzeFullBand)
            {
                ValidateBand(full, nameof(full), sampleRate);
            }

            BandSettings[] bands = GetActiveBands();

            for (int i = 0; i < bands.Length; i++)
            {
                if (hopSize > bands[i].fftSize)
                {
                    throw new ArgumentOutOfRangeException(nameof(hopSize), "hopSize must not be greater than any active band fftSize.");
                }
            }
        }

        static void ValidateBand(BandSettings band, string paramName, int sampleRate)
        {
            if (band == null)
            {
                throw new ArgumentNullException(paramName);
            }

            if (band.fftSize < 16 || (band.fftSize & (band.fftSize - 1)) != 0)
            {
                throw new ArgumentOutOfRangeException(paramName + ".fftSize", "fftSize must be a power of two greater than or equal to 16.");
            }

            if (!IsFinite(band.lowHz) || band.lowHz <= 0f)
            {
                throw new ArgumentOutOfRangeException(paramName + ".lowHz", "lowHz must be finite and greater than zero.");
            }

            if (!IsFinite(band.highHz) || band.highHz <= band.lowHz)
            {
                throw new ArgumentOutOfRangeException(paramName + ".highHz", "highHz must be finite and greater than lowHz.");
            }

            if (band.highHz >= sampleRate * 0.5f)
            {
                throw new ArgumentOutOfRangeException(paramName + ".highHz", "highHz must be below the Nyquist frequency.");
            }

            if (!IsFinite(band.delta) || band.delta < 0f)
            {
                throw new ArgumentOutOfRangeException(paramName + ".delta", "delta must be finite and non-negative.");
            }

            if (!IsFinite(band.noiseFloor) || band.noiseFloor < 0f)
            {
                throw new ArgumentOutOfRangeException(paramName + ".noiseFloor", "noiseFloor must be finite and non-negative.");
            }

            if (!IsFinite(band.minInterval) || band.minInterval < 0f)
            {
                throw new ArgumentOutOfRangeException(paramName + ".minInterval", "minInterval must be finite and non-negative.");
            }
        }

        static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }

    [Serializable]
    public class BeatMap
    {
        public List<float> kickBeats = new List<float>();
        public List<float> snareBeats = new List<float>();
        public List<BeatEvent> onsets = new List<BeatEvent>();
        public List<BeatEvent> beats = new List<BeatEvent>();
        public List<float> beatGrid = new List<float>();
        public List<float> downbeats = new List<float>();
        public List<BandResult> bands = new List<BandResult>();
        public float bpm;
        public float tempoConfidence;
        public int beatsPerBar = 4;
        public float duration;
        public int sampleRate;
        public float firstBeatTime;
        public float beatPeriod;
        public string algorithmVersion = "2.0.0";
        public float[] frameTimes = Array.Empty<float>();
        public float[] kickFlux = Array.Empty<float>();
        public float[] kickThreshold = Array.Empty<float>();
        public float[] snareFlux = Array.Empty<float>();
        public float[] snareThreshold = Array.Empty<float>();
        public float[] energy = Array.Empty<float>();

        public float QuantizeTime(float time, int subdivisionsPerBeat)
        {
            if (beatPeriod <= 0f || subdivisionsPerBeat < 1)
            {
                return time;
            }

            float beatPosition = (time - firstBeatTime) / beatPeriod;
            float quantized = (float)Math.Round(beatPosition * subdivisionsPerBeat) / subdivisionsPerBeat;
            return firstBeatTime + quantized * beatPeriod;
        }

        public int GetBeatIndex(float time)
        {
            if (beatPeriod <= 0f)
            {
                return -1;
            }

            return (int)Math.Round((time - firstBeatTime) / beatPeriod);
        }

        public float GetBeatTime(int index)
        {
            return firstBeatTime + index * beatPeriod;
        }
    }

    public static class TransientDetector
    {
        public const string AlgorithmVersion = "2.0.0";

        public static BeatMap AnalyzeAudio(AudioClip song)
        {
            return AnalyzeAudio(song, new DetectorSettings());
        }

        public static BeatMap AnalyzeAudio(AudioClip song, DetectorSettings settings)
        {
            return AnalyzeAudio(song, settings, null, CancellationToken.None);
        }

        public static BeatMap AnalyzeAudio(AudioClip song, DetectorSettings settings, IProgress<float> progress)
        {
            return AnalyzeAudio(song, settings, progress, CancellationToken.None);
        }

        public static BeatMap AnalyzeAudio(AudioClip song, DetectorSettings settings, IProgress<float> progress, CancellationToken cancellationToken)
        {
            if (settings == null)
            {
                settings = new DetectorSettings();
            }

            cancellationToken.ThrowIfCancellationRequested();
            float[] mono = ToMono(song);
            return Analyze(mono, song.frequency, settings, progress, cancellationToken);
        }

        public static Task<BeatMap> AnalyzeAudioAsync(AudioClip song, DetectorSettings settings)
        {
            return AnalyzeAudioAsync(song, settings, null, CancellationToken.None);
        }

        public static Task<BeatMap> AnalyzeAudioAsync(AudioClip song, DetectorSettings settings, IProgress<float> progress)
        {
            return AnalyzeAudioAsync(song, settings, progress, CancellationToken.None);
        }

        public static async Task<BeatMap> AnalyzeAudioAsync(AudioClip song, DetectorSettings settings, IProgress<float> progress, CancellationToken cancellationToken)
        {
            if (settings == null)
            {
                settings = new DetectorSettings();
            }

            cancellationToken.ThrowIfCancellationRequested();
            float[] mono = ToMono(song);
            int sampleRate = song.frequency;

            BeatMap result = await Task.Run(
                () => Analyze(mono, sampleRate, settings, progress, cancellationToken),
                cancellationToken).ConfigureAwait(false);

            return result;
        }

        public static float[] ToMono(AudioClip clip)
        {
            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip));
            }

            if (clip.loadType == AudioClipLoadType.Streaming)
            {
                throw new InvalidOperationException("El clip " + clip.name + " esta en Streaming. Usa Decompress On Load.");
            }

            if (clip.loadState == AudioDataLoadState.Failed)
            {
                throw new InvalidOperationException("La carga de audio fallo en " + clip.name + ".");
            }

            if (clip.loadState == AudioDataLoadState.Unloaded)
            {
                clip.LoadAudioData();
            }

            int channels = clip.channels;
            int frames = clip.samples;

            if (frames <= 0 || channels <= 0)
            {
                return Array.Empty<float>();
            }

            if (channels == 1)
            {
                float[] monoSamples = new float[frames];

                if (!clip.GetData(monoSamples, 0))
                {
                    throw new InvalidOperationException("GetData fallo en " + clip.name + ". Activa Preload Audio Data y Decompress On Load.");
                }

                return monoSamples;
            }

            const int ChunkFrames = 4096;
            float[] result = new float[frames];
            float[] chunk = new float[Math.Min(ChunkFrames, frames) * channels];
            float inverse = 1f / channels;

            for (int offset = 0; offset < frames; offset += ChunkFrames)
            {
                int count = Math.Min(ChunkFrames, frames - offset);
                int sampleCount = count * channels;

                if (!clip.GetData(chunk, offset))
                {
                    throw new InvalidOperationException("GetData fallo en " + clip.name + ". Activa Preload Audio Data y Decompress On Load.");
                }

                for (int i = 0; i < count; i++)
                {
                    int baseIndex = i * channels;
                    float sum = 0f;

                    for (int c = 0; c < channels; c++)
                    {
                        sum += chunk[baseIndex + c];
                    }

                    result[offset + i] = sum * inverse;
                }
            }

            return result;
        }

        public static BeatMap Analyze(float[] mono, int sampleRate, DetectorSettings settings)
        {
            return Analyze(mono, sampleRate, settings, null, CancellationToken.None);
        }

        public static BeatMap Analyze(float[] mono, int sampleRate, DetectorSettings settings, IProgress<float> progress, CancellationToken cancellationToken)
        {
            if (mono == null)
            {
                throw new ArgumentNullException(nameof(mono));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            settings.Validate(sampleRate);
            cancellationToken.ThrowIfCancellationRequested();

            BeatMap map = new BeatMap
            {
                sampleRate = sampleRate,
                algorithmVersion = AlgorithmVersion,
                duration = (float)mono.Length / sampleRate + settings.timeOffset
            };

            if (mono.Length == 0)
            {
                progress?.Report(1f);
                return map;
            }

            Sanitize(mono);

            int hop = settings.hopSize;
            BandSettings[] bands = settings.GetActiveBands();
            int maxFft = 0;

            for (int i = 0; i < bands.Length; i++)
            {
                maxFft = Math.Max(maxFft, bands[i].fftSize);
            }

            if (mono.Length < maxFft || bands.Length == 0)
            {
                progress?.Report(1f);
                return map;
            }

            int frames = (mono.Length - maxFft) / hop + 1;
            float[] times = new float[frames];
            float[] energy = new float[frames];

            for (int f = 0; f < frames; f++)
            {
                times[f] = (float)(((double)f * hop + maxFft * 0.5) / sampleRate) + settings.timeOffset;

                int start = f * hop;
                int end = Math.Min(start + hop, mono.Length);
                double sum = 0.0;

                for (int i = start; i < end; i++)
                {
                    float value = mono[i];
                    sum += value * value;
                }

                energy[f] = end > start ? (float)Math.Sqrt(sum / (end - start)) : 0f;
            }

            float[] envelope = settings.refineOnsets ? new float[mono.Length] : null;
            float[] lowBuffer = settings.refineOnsets ? new float[mono.Length] : null;
            int totalSteps = bands.Length + 1;
            int processed = 0;
            var groups = new Dictionary<int, List<BandSettings>>();

            for (int i = 0; i < bands.Length; i++)
            {
                int fftSize = bands[i].fftSize;

                if (!groups.TryGetValue(fftSize, out List<BandSettings> group))
                {
                    group = new List<BandSettings>();
                    groups[fftSize] = group;
                }

                group.Add(bands[i]);
            }

            progress?.Report(0f);

            foreach (KeyValuePair<int, List<BandSettings>> group in groups)
            {
                cancellationToken.ThrowIfCancellationRequested();
                List<BandSettings> groupBands = group.Value;
                float[][] fluxes = BandFluxGroup(
                    mono,
                    sampleRate,
                    hop,
                    frames,
                    groupBands,
                    settings.logGamma,
                    maxFft,
                    cancellationToken);

                for (int i = 0; i < groupBands.Count; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    BandSettings band = groupBands[i];
                    float[] flux = fluxes[i];

                    if (settings.localNormalization)
                    {
                        int radiusFrames = Math.Max(8, (int)(settings.localWindowSeconds * sampleRate / hop));
                        NormalizeLocal(flux, radiusFrames, settings.localFloor);
                    }
                    else
                    {
                        Normalize(flux);
                    }

                    float[] threshold = LocalMedianThreshold(flux, settings.medianRadius, band.delta);
                    List<BeatEvent> beats = PickPeaks(flux, threshold, times, band, settings.peakRadius);

                    if (settings.refineOnsets && band.refineOnsets)
                    {
                        beats = OnsetRefiner.Refine(mono, sampleRate, beats, band, settings.timeOffset, envelope, lowBuffer);
                    }

                    var result = new BandResult
                    {
                        name = band.name,
                        band = band.band,
                        beats = beats,
                        flux = flux,
                        threshold = threshold
                    };

                    map.bands.Add(result);

                    if (band.band == BeatBand.Kick)
                    {
                        map.kickBeats = ToTimes(beats);
                        map.kickFlux = flux;
                        map.kickThreshold = threshold;
                    }
                    else if (band.band == BeatBand.Snare)
                    {
                        map.snareBeats = ToTimes(beats);
                        map.snareFlux = flux;
                        map.snareThreshold = threshold;
                    }

                    processed++;
                    progress?.Report(processed / (float)totalSteps);
                }
            }

            map.frameTimes = times;
            map.energy = energy;
            map.onsets = MergeOnsets(map.bands, 0.02f);

            cancellationToken.ThrowIfCancellationRequested();
            TempoEstimator.Estimate(map, settings, hop, sampleRate);
            progress?.Report(1f);

            return map;
        }

        static List<float> ToTimes(List<BeatEvent> events)
        {
            var result = new List<float>(events.Count);

            for (int i = 0; i < events.Count; i++)
            {
                result.Add(events[i].time);
            }

            return result;
        }

        static List<BeatEvent> MergeOnsets(List<BandResult> bands, float window)
        {
            var all = new List<BeatEvent>();

            for (int b = 0; b < bands.Count; b++)
            {
                all.AddRange(bands[b].beats);
            }

            all.Sort((a, b) => a.time.CompareTo(b.time));

            var merged = new List<BeatEvent>();

            for (int i = 0; i < all.Count; i++)
            {
                BeatEvent current = all[i];

                if (merged.Count > 0 && current.time - merged[merged.Count - 1].time <= window)
                {
                    BeatEvent last = merged[merged.Count - 1];

                    if (current.strength > last.strength)
                    {
                        last.time = current.time;
                        last.strength = current.strength;
                        last.band = current.band;
                    }
                }
                else
                {
                    merged.Add(new BeatEvent(current.time, current.strength, current.band));
                }
            }

            return merged;
        }

        static float[][] BandFluxGroup(
            float[] mono,
            int sampleRate,
            int hop,
            int frames,
            List<BandSettings> bands,
            float gamma,
            int maxFft,
            CancellationToken cancellationToken)
        {
            int n = bands[0].fftSize;
            int half = n / 2;
            int count = bands.Count;

            float[] hann = new float[n];

            for (int i = 0; i < n; i++)
            {
                hann[i] = 0.5f * (1f - (float)Math.Cos(2.0 * Math.PI * i / n));
            }

            double norm = 4.0 / n;
            int[] lo = new int[count];
            int[] hi = new int[count];

            for (int i = 0; i < count; i++)
            {
                lo[i] = ClampInt(Mathf.RoundToInt(bands[i].lowHz * n / sampleRate), 2, half - 3);
                hi[i] = ClampInt(Mathf.RoundToInt(bands[i].highHz * n / sampleRate), lo[i], half - 2);
            }

            var fluxes = new float[count][];
            var prev = new float[count][];
            var cur = new float[count][];

            for (int i = 0; i < count; i++)
            {
                fluxes[i] = new float[frames];
                prev[i] = new float[half + 1];
                cur[i] = new float[half + 1];
            }

            Complex[] buffer = new Complex[n];

            for (int f = 0; f < frames; f++)
            {
                if ((f & 63) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                int start = f * hop + (maxFft - n) / 2;

                for (int i = 0; i < n; i++)
                {
                    int index = start + i;
                    float x = index >= 0 && index < mono.Length ? mono[index] : 0f;
                    buffer[i] = new Complex(x * hann[i], 0.0);
                }

                FastFourierTransform.Forward(buffer);

                for (int b = 0; b < count; b++)
                {
                    int bandLo = lo[b];
                    int bandHi = hi[b];
                    float[] current = cur[b];
                    float[] previous = prev[b];

                    for (int k = bandLo - 1; k <= bandHi + 1; k++)
                    {
                        current[k] = (float)Math.Log(1.0 + gamma * buffer[k].Magnitude * norm);
                    }

                    if (f > 0)
                    {
                        float sum = 0f;
                        bool vibrato = bands[b].vibratoSuppression;

                        for (int k = bandLo; k <= bandHi; k++)
                        {
                            float reference = previous[k];

                            if (vibrato)
                            {
                                reference = Math.Max(previous[k], Math.Max(previous[k - 1], previous[k + 1]));
                            }

                            float difference = current[k] - reference;

                            if (difference > 0f)
                            {
                                sum += difference;
                            }
                        }

                        fluxes[b][f] = sum / (bandHi - bandLo + 1);
                    }

                    float[] temp = prev[b];
                    prev[b] = cur[b];
                    cur[b] = temp;
                }
            }

            return fluxes;
        }

        static void Sanitize(float[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                float value = data[i];

                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    data[i] = 0f;
                }
            }
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

        static void NormalizeLocal(float[] flux, int radius, float floorValue)
        {
            int count = flux.Length;

            if (count == 0)
            {
                return;
            }

            int stride = Math.Max(1, radius / 8);
            int anchorCount = count / stride + 2;
            float[] scale = new float[anchorCount];

            for (int a = 0; a < anchorCount; a++)
            {
                int center = a * stride;
                int first = Math.Max(0, center - radius);
                int last = Math.Min(count - 1, center + radius);
                double sum = 0.0;

                for (int i = first; i <= last; i++)
                {
                    sum += flux[i];
                }

                float local = (float)(sum / (last - first + 1));

                if (float.IsNaN(local) || float.IsInfinity(local) || local < floorValue)
                {
                    local = floorValue;
                }

                scale[a] = local;
            }

            for (int i = 0; i < count; i++)
            {
                float position = (float)i / stride;
                int lower = (int)position;
                float fraction = position - lower;
                float local = scale[lower] * (1f - fraction) + scale[lower + 1] * fraction;
                flux[i] = flux[i] / local;
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

        static List<BeatEvent> PickPeaks(float[] flux, float[] threshold, float[] times, BandSettings band, int radius)
        {
            var beats = new List<BeatEvent>();
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

                if (beats.Count > 0 && time - beats[beats.Count - 1].time < band.minInterval)
                {
                    if (value > lastValue)
                    {
                        beats[beats.Count - 1].time = time;
                        beats[beats.Count - 1].strength = value;
                        lastValue = value;
                    }

                    continue;
                }

                beats.Add(new BeatEvent(time, value, band.band));
                lastValue = value;
            }

            return beats;
        }
    }
}
