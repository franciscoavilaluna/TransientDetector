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

        [Tooltip("Tamaño de FFT de esta banda. Graves necesitan más resolución en frecuencia (4096 u 8192).")]
        public int fftSize = 2048;

        [Tooltip("Compara cada bin con el máximo de sus vecinos del frame anterior (SuperFlux). Ignora oscilaciones/notas sostenidas del bajo.")]
        public bool vibratoSuppression = false;

        [Tooltip("Cuánto debe superar el pico a la mediana local (flujo normalizado ~0-1). Sube = menos detecciones.")]
        public float delta = 0.15f;

        [Tooltip("Flujo normalizado mínimo para considerar un pico.")]
        public float noiseFloor = 0.08f;

        [Tooltip("Separación mínima entre golpes (s). Si hay dos picos más cerca, gana el más fuerte.")]
        public float minInterval = 0.10f;

        public BandSettings() { }

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

        [Tooltip("Compresión logarítmica. Más alto = más sensible a golpes suaves.")]
        public float logGamma = 100f;

        [Tooltip("Radio (en frames) de la ventana de la mediana local.")]
        public int medianRadius = 8;

        [Tooltip("Un pico debe ser el máximo en +/- este número de frames.")]
        public int peakRadius = 3;

        [Tooltip("Compensación de latencia en segundos (positivo = los golpes se marcan más tarde).")]
        public float timeOffset = 0f;

        public BandSettings kick = new BandSettings("kick", 40f, 110f, 0.15f, 0.08f, 0.10f)
        {
            fftSize = 4096,
            vibratoSuppression = true
        };
        public BandSettings snare = new BandSettings("snare", 1500f, 5000f, 0.15f, 0.08f, 0.12f);
    }

    public class BeatMap
    {
        public List<float> kickBeats = new List<float>();
        public List<float> snareBeats = new List<float>();

        // Datos de depuración (para graficar / exportar)
        public float[] frameTimes;
        public float[] kickFlux, kickThreshold;
        public float[] snareFlux, snareThreshold;
    }

    public static class TransientDetector
    {
        // ---------- API pública ----------

        /// <summary>Síncrono (congela el hilo principal en canciones largas).</summary>
        public static BeatMap AnalyzeAudio(AudioClip song, DetectorSettings settings = null)
        {
            float[] mono = ToMono(song);
            return Analyze(mono, song.frequency, settings ?? new DetectorSettings());
        }

        /// <summary>Lee el clip en el hilo principal y analiza en un hilo aparte.</summary>
        public static async Task<BeatMap> AnalyzeAudioAsync(AudioClip song, DetectorSettings settings = null)
        {
            float[] mono = ToMono(song);
            int sampleRate = song.frequency;
            DetectorSettings s = settings ?? new DetectorSettings();
            return await Task.Run(() => Analyze(mono, sampleRate, s));
        }

        /// <summary>Mezcla el clip a mono. Debe llamarse desde el hilo principal.</summary>
        public static float[] ToMono(AudioClip clip)
        {
            if (clip == null) throw new ArgumentNullException(nameof(clip));

            if (clip.loadType == AudioClipLoadType.Streaming)
                throw new InvalidOperationException(
                    $"'{clip.name}' está en Load Type = Streaming; GetData no funciona así. Usa 'Decompress On Load'.");

            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();

            int channels = clip.channels;
            int frames = clip.samples;
            var raw = new float[frames * channels];

            if (!clip.GetData(raw, 0))
                throw new InvalidOperationException(
                    $"GetData falló en '{clip.name}'. Activa 'Preload Audio Data' y usa 'Decompress On Load'.");

            if (channels == 1) return raw;

            var mono = new float[frames];
            float inv = 1f / channels;
            for (int i = 0; i < frames; i++)
            {
                float sum = 0f;
                int b = i * channels;
                for (int c = 0; c < channels; c++) sum += raw[b + c];
                mono[i] = sum * inv;
            }
            return mono;
        }

        // ---------- Núcleo (seguro fuera del hilo principal) ----------

        public static BeatMap Analyze(float[] mono, int sampleRate, DetectorSettings s)
        {
            var map = new BeatMap();
            int H = s.hopSize;
            if (mono == null || mono.Length < 4 * H) return map;

            // Frames centrados: el frame f está centrado en la muestra f*H (con ceros fuera del audio).
            // Así todas las bandas comparten la misma rejilla de tiempo aunque usen FFT distintas.
            int frames = mono.Length / H + 1;
            var times = new float[frames];
            for (int f = 0; f < frames; f++)
                times[f] = (float)((double)f * H / sampleRate) + s.timeOffset;

            BandSettings[] bands = { s.kick, s.snare };
            for (int b = 0; b < 2; b++)
            {
                float[] flux = BandFlux(mono, sampleRate, H, frames, bands[b], s.logGamma);
                Normalize(flux);
                float[] thr = LocalMedianThreshold(flux, s.medianRadius, bands[b].delta);
                List<float> beats = PickPeaks(flux, thr, times, bands[b], s.peakRadius);

                if (b == 0) { map.kickBeats = beats; map.kickFlux = flux; map.kickThreshold = thr; }
                else { map.snareBeats = beats; map.snareFlux = flux; map.snareThreshold = thr; }
            }

            map.frameTimes = times;
            return map;
        }

        /// <summary>Flujo espectral log, rectificado por bin, para una banda con su propia FFT.</summary>
        static float[] BandFlux(float[] mono, int sampleRate, int H, int frames, BandSettings band, float gamma)
        {
            int N = band.fftSize;
            int half = N / 2;

            var hann = new float[N];
            for (int i = 0; i < N; i++)
                hann[i] = 0.5f * (1f - (float)Math.Cos(2.0 * Math.PI * i / N));

            double norm = 4.0 / N; // Hann: |X| = A*N/4

            // lo-1 y hi+1 se usan para el filtro de máximo, así que dejamos margen
            int lo = Clamp(Mathf.RoundToInt(band.lowHz * N / sampleRate), 2, half - 3);
            int hi = Clamp(Mathf.RoundToInt(band.highHz * N / sampleRate), lo, half - 2);

            var flux = new float[frames];
            var buffer = new Complex[N];
            var prev = new float[half + 1];
            var cur = new float[half + 1];

            for (int f = 0; f < frames; f++)
            {
                int start = f * H - N / 2;
                for (int i = 0; i < N; i++)
                {
                    int idx = start + i;
                    float x = (idx >= 0 && idx < mono.Length) ? mono[idx] : 0f;
                    buffer[i] = new Complex(x * hann[i], 0.0);
                }

                Fourier.Forward(buffer, FourierOptions.NoScaling);

                for (int k = lo - 1; k <= hi + 1; k++)
                    cur[k] = (float)Math.Log(1.0 + gamma * buffer[k].Magnitude * norm);

                if (f > 0)
                {
                    float sum = 0f;
                    for (int k = lo; k <= hi; k++)
                    {
                        float reference = prev[k];
                        if (band.vibratoSuppression)
                            reference = Math.Max(prev[k], Math.Max(prev[k - 1], prev[k + 1]));

                        float d = cur[k] - reference;
                        if (d > 0f) sum += d;
                    }
                    flux[f] = sum / (hi - lo + 1);
                }

                float[] tmp = prev; prev = cur; cur = tmp;
            }
            return flux;
        }

        // ---------- Helpers ----------

        static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

        /// <summary>Divide por el percentil 98 -> independiente del volumen del tema.</summary>
        static void Normalize(float[] flux)
        {
            var copy = (float[])flux.Clone();
            Array.Sort(copy);
            float reference = copy[Clamp((int)(0.98f * (copy.Length - 1)), 0, copy.Length - 1)];

            if (reference <= 1e-6f)
            {
                Array.Clear(flux, 0, flux.Length);
                return;
            }
            float inv = 1f / reference;
            for (int i = 0; i < flux.Length; i++) flux[i] *= inv;
        }

        /// <summary>Mediana en ventana centrada (offline, usa pasado y futuro) + delta.</summary>
        static float[] LocalMedianThreshold(float[] flux, int radius, float delta)
        {
            int n = flux.Length;
            var thr = new float[n];
            var win = new float[radius * 2 + 1];

            for (int i = 0; i < n; i++)
            {
                int a = Math.Max(0, i - radius);
                int z = Math.Min(n - 1, i + radius);
                int len = z - a + 1;
                Array.Copy(flux, a, win, 0, len);
                Array.Sort(win, 0, len);
                thr[i] = win[len / 2] + delta;
            }
            return thr;
        }

        static List<float> PickPeaks(float[] flux, float[] thr, float[] times, BandSettings band, int radius)
        {
            var beats = new List<float>();
            float lastValue = 0f;

            for (int i = 1; i < flux.Length - 1; i++)
            {
                float v = flux[i];
                if (v < band.noiseFloor || v < thr[i]) continue;

                // Máximo local en +/- radius frames
                bool isMax = true;
                int a = Math.Max(0, i - radius);
                int z = Math.Min(flux.Length - 1, i + radius);
                for (int j = a; j <= z; j++)
                {
                    if (j == i) continue;
                    if (flux[j] > v || (j < i && flux[j] == v)) { isMax = false; break; }
                }
                if (!isMax) continue;

                float t = times[i];

                // Supresión: dentro de minInterval gana el más fuerte (no el primero)
                if (beats.Count > 0 && t - beats[beats.Count - 1] < band.minInterval)
                {
                    if (v > lastValue)
                    {
                        beats[beats.Count - 1] = t;
                        lastValue = v;
                    }
                    continue;
                }

                beats.Add(t);
                lastValue = v;
            }
            return beats;
        }
    }
}
