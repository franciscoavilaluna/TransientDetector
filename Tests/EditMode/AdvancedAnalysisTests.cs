using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using AudioBeatDetector;

namespace AudioBeatDetector.Tests
{
    public class AdvancedAnalysisTests
    {
        const int SampleRate = 44100;

        static float[] CreateKickTrack(double seconds, double bpm, bool addHats = false)
        {
            int length = (int)(seconds * SampleRate);
            var samples = new float[length];
            var random = new System.Random(1234);

            for (int i = 0; i < length; i++)
            {
                samples[i] = (float)((random.NextDouble() * 2.0 - 1.0) * 0.003);
            }

            double step = 60.0 / bpm;

            for (double onset = 0.25; onset < seconds; onset += step)
            {
                int start = (int)(onset * SampleRate);

                for (int i = 0; start + i < length && i < SampleRate / 4; i++)
                {
                    double t = (double)i / SampleRate;
                    samples[start + i] += (float)(
                        0.9 * Math.Exp(-t / 0.05) *
                        Math.Sin(2.0 * Math.PI * 55.0 * t));
                }
            }

            if (addHats)
            {
                double hatStep = step / 4.0;

                for (double onset = 0.25 + hatStep * 0.5; onset < seconds; onset += hatStep)
                {
                    int start = (int)(onset * SampleRate);

                    for (int i = 0; start + i < length && i < SampleRate / 40; i++)
                    {
                        double t = (double)i / SampleRate;
                        samples[start + i] += (float)(
                            0.08 * Math.Exp(-t / 0.008) *
                            (random.NextDouble() * 2.0 - 1.0));
                    }
                }
            }

            return samples;
        }

        [TestCase(60.0)]
        [TestCase(90.0)]
        [TestCase(120.0)]
        [TestCase(150.0)]
        [TestCase(180.0)]
        public void TempoAndBeatGridAreCorrect(double expectedBpm)
        {
            var map = TransientDetector.Analyze(
                CreateKickTrack(8.0, expectedBpm),
                SampleRate,
                new DetectorSettings());

            Assert.That(map.bpm, Is.EqualTo(expectedBpm).Within(1.5));
            Assert.That(map.beatGrid.Count, Is.GreaterThanOrEqualTo(4));
            Assert.That(map.downbeats.Count, Is.GreaterThanOrEqualTo(1));
            Assert.That(map.tempoConfidence, Is.GreaterThan(0.05f));
        }

        [Test]
        public void SixteenthHatsDoNotChangeTempo()
        {
            var map = TransientDetector.Analyze(
                CreateKickTrack(8.0, 120.0, true),
                SampleRate,
                new DetectorSettings());

            Assert.That(map.bpm, Is.EqualTo(120.0).Within(1.5));
        }

        [Test]
        public void QuantizeTimeSnapsToGrid()
        {
            var map = TransientDetector.Analyze(
                CreateKickTrack(8.0, 120.0),
                SampleRate,
                new DetectorSettings());

            float quantized = map.QuantizeTime(map.firstBeatTime + 0.01f, 4);
            Assert.That(quantized, Is.EqualTo(map.firstBeatTime).Within(0.02f));
        }

        [Test]
        public void ConductorEmitsEventsInOrder()
        {
            var map = new BeatMap();
            map.onsets.Add(new BeatEvent(0.5f, 1.0f, BeatBand.Kick));
            map.beatGrid.Add(0.5f);
            map.beats.Add(new BeatEvent(0.5f, 1.0f, BeatBand.Full));
            map.downbeats.Add(0.5f);

            var conductor = new Conductor();
            conductor.Reset(map, 0f);

            int onsets = 0;
            int beats = 0;
            int downbeats = 0;

            conductor.Tick(0.49f, _ => onsets++, _ => beats++, _ => downbeats++);
            Assert.That(onsets, Is.EqualTo(0));

            conductor.Tick(0.5f, _ => onsets++, _ => beats++, _ => downbeats++);
            Assert.That(onsets, Is.EqualTo(1));
            Assert.That(beats, Is.EqualTo(1));
            Assert.That(downbeats, Is.EqualTo(1));
        }

        [Test]
        public void JsonRoundTripPreservesBeatMap()
        {
            var map = TransientDetector.Analyze(
                CreateKickTrack(4.0, 120.0),
                SampleRate,
                new DetectorSettings());

            string json = BeatMapCache.ToJson(map);
            BeatMap roundTrip = BeatMapCache.FromJson(json);

            Assert.That(roundTrip.bpm, Is.EqualTo(map.bpm).Within(0.01f));
            Assert.That(roundTrip.kickBeats.Count, Is.EqualTo(map.kickBeats.Count));
            Assert.That(roundTrip.onsets.Count, Is.EqualTo(map.onsets.Count));
            Assert.That(roundTrip.bands.Count, Is.EqualTo(map.bands.Count));
        }

        [Test]
        public void CacheReturnsCachedInstance()
        {
            var clip = AudioClip.Create("cache-test", SampleRate * 2, 1, SampleRate, false);
            clip.SetData(CreateKickTrack(2.0, 120.0), 0);
            BeatMapCache.Clear();

            try
            {
                var first = BeatMapCache.GetOrAnalyze(clip, new DetectorSettings());
                var second = BeatMapCache.GetOrAnalyze(clip, new DetectorSettings());
                Assert.That(ReferenceEquals(first, second), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
                BeatMapCache.Clear();
            }
        }

        [Test]
        public void FftMatchesDirectDft()
        {
            foreach (int n in new[] { 8, 16, 64 })
            {
                var input = new Complex[n];
                var actual = new Complex[n];
                var random = new System.Random(42);

                for (int i = 0; i < n; i++)
                {
                    input[i] = new Complex(
                        random.NextDouble() * 2.0 - 1.0,
                        random.NextDouble() * 2.0 - 1.0);
                    actual[i] = input[i];
                }

                FastFourierTransform.Forward(actual);

                for (int k = 0; k < n; k++)
                {
                    Complex expected = Complex.Zero;

                    for (int t = 0; t < n; t++)
                    {
                        double angle = -2.0 * Math.PI * k * t / n;
                        expected += input[t] * new Complex(Math.Cos(angle), Math.Sin(angle));
                    }

                    Assert.That(
                        Complex.Abs(actual[k] - expected),
                        Is.LessThan(1e-8),
                        $"n={n}, k={k}");
                }
            }
        }

        [Test]
        public void CancellationIsObserved()
        {
            using (var source = new CancellationTokenSource())
            {
                source.Cancel();

                Assert.Throws<OperationCanceledException>(() =>
                    TransientDetector.Analyze(
                        CreateKickTrack(2.0, 120.0),
                        SampleRate,
                        new DetectorSettings(),
                        null,
                        source.Token));
            }
        }

        [Test]
        public void ProgressIsReported()
        {
            var recorder = new ProgressRecorder();
            var map = TransientDetector.Analyze(
                CreateKickTrack(4.0, 120.0),
                SampleRate,
                new DetectorSettings(),
                recorder,
                CancellationToken.None);

            Assert.That(map.bpm, Is.GreaterThan(0f));
            Assert.That(recorder.Values.Count, Is.GreaterThan(0));
            Assert.That(recorder.Values[recorder.Values.Count - 1], Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void SaveAndLoadFromFile()
        {
            var map = TransientDetector.Analyze(
                CreateKickTrack(4.0, 120.0),
                SampleRate,
                new DetectorSettings());

            string path = Path.Combine(Path.GetTempPath(), "audio-beat-detector-test.json");

            try
            {
                BeatMapCache.SaveToFile(map, path);
                BeatMap loaded = BeatMapCache.LoadFromFile(path);
                Assert.That(loaded.bpm, Is.EqualTo(map.bpm).Within(0.01f));
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        class ProgressRecorder : IProgress<float>
        {
            public readonly List<float> Values = new List<float>();

            public void Report(float value)
            {
                Values.Add(value);
            }
        }
    }
}
