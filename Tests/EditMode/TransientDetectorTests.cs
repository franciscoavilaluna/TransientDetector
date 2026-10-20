using System;
using NUnit.Framework;
using UnityEngine;
using AudioBeatDetector;

namespace AudioBeatDetector.Tests
{
    public class TransientDetectorTests
    {
        const int SampleRate = 44100;

        static float[] CreateKickTrack(double seconds, double amplitude = 0.9)
        {
            int length = (int)(seconds * SampleRate);
            var samples = new float[length];
            var random = new System.Random(1234);

            for (int i = 0; i < length; i++)
            {
                samples[i] = (float)((random.NextDouble() * 2.0 - 1.0) * 0.003);
            }

            for (double onset = 0.25; onset < seconds; onset += 0.5)
            {
                int start = (int)(onset * SampleRate);

                for (int i = 0; start + i < length && i < SampleRate / 4; i++)
                {
                    double t = (double)i / SampleRate;
                    samples[start + i] += (float)(
                        amplitude * Math.Exp(-t / 0.05) *
                        Math.Sin(2.0 * Math.PI * 55.0 * t));
                }
            }

            return samples;
        }

        static float[] CreateSnareTrack(double seconds)
        {
            int length = (int)(seconds * SampleRate);
            var samples = new float[length];
            var random = new System.Random(77);

            for (int i = 0; i < length; i++)
            {
                samples[i] = (float)((random.NextDouble() * 2.0 - 1.0) * 0.002);
            }

            for (double onset = 0.75; onset < seconds; onset += 1.0)
            {
                int start = (int)(onset * SampleRate);

                for (int i = 0; start + i < length && i < SampleRate / 8; i++)
                {
                    double t = (double)i / SampleRate;
                    samples[start + i] += (float)(
                        0.6 * Math.Exp(-t / 0.04) *
                        (random.NextDouble() * 2.0 - 1.0));
                }
            }

            return samples;
        }

        static float[] CreateTone(double hz, double seconds, double amplitude = 0.5)
        {
            int length = (int)(seconds * SampleRate);
            var samples = new float[length];

            for (int i = 0; i < length; i++)
            {
                samples[i] = (float)(
                    amplitude * Math.Sin(2.0 * Math.PI * hz * i / SampleRate));
            }

            return samples;
        }

        [Test]
        public void SilenceProducesNoBeats()
        {
            var map = TransientDetector.Analyze(
                new float[SampleRate * 2],
                SampleRate,
                new DetectorSettings());

            Assert.That(map.kickBeats.Count, Is.EqualTo(0));
            Assert.That(map.snareBeats.Count, Is.EqualTo(0));
        }

        [Test]
        public void KickTrackDetectsAllKicks()
        {
            var map = TransientDetector.Analyze(
                CreateKickTrack(8.0),
                SampleRate,
                new DetectorSettings());

            Assert.That(map.kickBeats.Count, Is.EqualTo(16));
        }

        [Test]
        public void DynamicKickTrackDetectsAllKicks()
        {
            int length = SampleRate * 8;
            var samples = new float[length];
            var random = new System.Random(5);

            for (int i = 0; i < length; i++)
            {
                samples[i] = (float)((random.NextDouble() * 2.0 - 1.0) * 0.003);
            }

            for (double onset = 0.25; onset < 8.0; onset += 0.5)
            {
                double amplitude = onset < 4.0 ? 0.9 : 0.08;
                int start = (int)(onset * SampleRate);

                for (int i = 0; start + i < length && i < SampleRate / 4; i++)
                {
                    double t = (double)i / SampleRate;
                    samples[start + i] += (float)(
                        amplitude * Math.Exp(-t / 0.05) *
                        Math.Sin(2.0 * Math.PI * 55.0 * t));
                }
            }

            var map = TransientDetector.Analyze(samples, SampleRate, new DetectorSettings());
            Assert.That(map.kickBeats.Count, Is.EqualTo(16));
        }

        [Test]
        public void SnareTrackDetectsSnares()
        {
            var map = TransientDetector.Analyze(
                CreateSnareTrack(8.0),
                SampleRate,
                new DetectorSettings());

            Assert.That(map.snareBeats.Count, Is.GreaterThanOrEqualTo(7));
        }

        [Test]
        public void PureToneProducesNoBeats()
        {
            var map = TransientDetector.Analyze(
                CreateTone(440.0, 4.0),
                SampleRate,
                new DetectorSettings());

            Assert.That(map.kickBeats.Count, Is.EqualTo(0));
            Assert.That(map.snareBeats.Count, Is.EqualTo(0));
        }

        [Test]
        public void ShortInputReturnsEmptyArrays()
        {
            var map = TransientDetector.Analyze(
                new float[100],
                SampleRate,
                new DetectorSettings());

            Assert.That(map.frameTimes, Is.Not.Null);
            Assert.That(map.kickFlux, Is.Not.Null);
            Assert.That(map.kickThreshold, Is.Not.Null);
            Assert.That(map.kickBeats.Count, Is.EqualTo(0));
        }

        [Test]
        public void NonFiniteSamplesAreSanitized()
        {
            var samples = CreateKickTrack(2.0);
            samples[100] = float.NaN;
            samples[200] = float.PositiveInfinity;
            samples[300] = float.NegativeInfinity;

            Assert.DoesNotThrow(() =>
                TransientDetector.Analyze(samples, SampleRate, new DetectorSettings()));
        }

        [Test]
        public void InvalidHopSizeThrows()
        {
            var settings = new DetectorSettings { hopSize = 0 };

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                TransientDetector.Analyze(CreateTone(440.0, 1.0), SampleRate, settings));
        }

        [Test]
        public void BandAboveNyquistThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                TransientDetector.Analyze(CreateTone(440.0, 1.0), 8000, new DetectorSettings()));
        }

        [Test]
        public void InvalidLocalFloorThrows()
        {
            var settings = new DetectorSettings { localFloor = 0.0f };

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                TransientDetector.Analyze(CreateTone(440.0, 1.0), SampleRate, settings));
        }

        [Test]
        public void NullBandThrows()
        {
            var settings = new DetectorSettings { kick = null };

            Assert.Throws<ArgumentNullException>(() =>
                TransientDetector.Analyze(CreateTone(440.0, 1.0), SampleRate, settings));
        }

        [Test]
        public void NullSettingsThrows()
        {
            Assert.Throws<ArgumentNullException>(() =>
                TransientDetector.Analyze(CreateTone(440.0, 1.0), SampleRate, null));
        }

        [Test]
        public void AudioClipAnalysisWorks()
        {
            var samples = CreateKickTrack(4.0);
            var clip = AudioClip.Create("test", samples.Length, 1, SampleRate, false);
            clip.SetData(samples, 0);

            try
            {
                var map = TransientDetector.AnalyzeAudio(clip, new DetectorSettings());
                Assert.That(map.kickBeats.Count, Is.EqualTo(8));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }
    }
}
