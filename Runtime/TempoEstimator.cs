using System;
using System.Collections.Generic;

namespace AudioBeatDetector
{
    public static class TempoEstimator
    {
        const int PhaseBins = 64;

        public static void Estimate(BeatMap map, DetectorSettings settings, int hopSize, int sampleRate)
        {
            map.beatsPerBar = Math.Max(1, settings.beatsPerBar);

            if (map.onsets.Count < 4 || map.duration <= 0f)
            {
                return;
            }

            float bestScore = 0f;
            float bestBpm = 0f;
            float bestPhase = 0f;
            var histogram = new float[PhaseBins];

            for (float bpm = settings.minBpm; bpm <= settings.maxBpm; bpm += 0.25f)
            {
                float period = 60f / bpm;
                Array.Clear(histogram, 0, PhaseBins);

                double weightSum = 0.0;

                for (int i = 0; i < map.onsets.Count; i++)
                {
                    BeatEvent onset = map.onsets[i];
                    float phase = onset.time % period;

                    if (phase < 0f)
                    {
                        phase += period;
                    }

                    int bin = (int)(phase / period * PhaseBins);

                    if (bin < 0)
                    {
                        bin = 0;
                    }
                    else if (bin >= PhaseBins)
                    {
                        bin = PhaseBins - 1;
                    }

                    float weight = Math.Max(0.01f, onset.strength);
                    histogram[bin] += weight;
                    weightSum += weight;
                }

                if (weightSum <= 0.0)
                {
                    continue;
                }

                int bestBin = 0;
                float bestBinWeight = 0f;

                for (int i = 0; i < PhaseBins; i++)
                {
                    int previous = (i + PhaseBins - 1) % PhaseBins;
                    int next = (i + 1) % PhaseBins;
                    float value = histogram[i] + 0.5f * (histogram[previous] + histogram[next]);

                    if (value > bestBinWeight)
                    {
                        bestBinWeight = value;
                        bestBin = i;
                    }
                }

                float concentration = (float)(bestBinWeight / weightSum);
                float phaseCenter = (bestBin + 0.5f) / PhaseBins * period;
                float tolerance = period * 0.10f;
                float firstBeat = phaseCenter;

                while (firstBeat - period >= 0f)
                {
                    firstBeat -= period;
                }

                int onsetIndex = 0;
                int matched = 0;
                int total = 0;

                for (float beat = firstBeat; beat <= map.duration + 1e-3f; beat += period)
                {
                    while (onsetIndex < map.onsets.Count && map.onsets[onsetIndex].time < beat - tolerance)
                    {
                        onsetIndex++;
                    }

                    if (onsetIndex < map.onsets.Count && map.onsets[onsetIndex].time <= beat + tolerance)
                    {
                        matched++;

                        while (onsetIndex < map.onsets.Count && map.onsets[onsetIndex].time <= beat + tolerance)
                        {
                            onsetIndex++;
                        }
                    }

                    total++;
                }

                if (total <= 0)
                {
                    continue;
                }

                float coverage = matched / (float)total;
                double x = Math.Log(bpm / settings.beatPriorBpm, 2.0) / settings.tempoPriorWidthOctaves;
                double prior = Math.Exp(-0.5 * x * x);
                float score = concentration * coverage * (float)prior;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestBpm = bpm;
                    bestPhase = firstBeat;
                }
            }

            if (bestBpm <= 0f || bestScore < 0.05f)
            {
                return;
            }

            map.bpm = bestBpm;
            map.tempoConfidence = Math.Min(1f, bestScore);
            map.beatPeriod = 60f / bestBpm;
            map.firstBeatTime = bestPhase;
            map.beatGrid.Clear();
            map.beats.Clear();
            map.downbeats.Clear();

            float snapTolerance = map.beatPeriod * 0.12f;
            int snapIndex = 0;
            int guard = 0;

            for (float time = bestPhase; time <= map.duration + 1e-3f && guard < 100000; time += map.beatPeriod, guard++)
            {
                while (snapIndex < map.onsets.Count && map.onsets[snapIndex].time < time - snapTolerance)
                {
                    snapIndex++;
                }

                BeatEvent nearest = null;

                if (snapIndex < map.onsets.Count && Math.Abs(map.onsets[snapIndex].time - time) <= snapTolerance)
                {
                    nearest = map.onsets[snapIndex];
                }

                float beatTime = nearest != null ? nearest.time : time;
                float strength = nearest != null ? nearest.strength : 0f;

                map.beatGrid.Add(beatTime);
                map.beats.Add(new BeatEvent(beatTime, strength, BeatBand.Full));
            }

            if (map.beatGrid.Count == 0)
            {
                return;
            }

            int beatsPerBar = map.beatsPerBar;
            int bestOffset = 0;
            double bestOffsetScore = double.NegativeInfinity;

            for (int offset = 0; offset < beatsPerBar; offset++)
            {
                double sum = 0.0;

                for (int i = offset; i < map.beatGrid.Count; i += beatsPerBar)
                {
                    sum += SampleBandFlux(map, map.beatGrid[i], BeatBand.Kick);
                }

                if (sum > bestOffsetScore)
                {
                    bestOffsetScore = sum;
                    bestOffset = offset;
                }
            }

            for (int i = bestOffset; i < map.beatGrid.Count; i += beatsPerBar)
            {
                map.downbeats.Add(map.beatGrid[i]);
            }
        }

        static float SampleBandFlux(BeatMap map, float time, BeatBand band)
        {
            BandResult result = null;

            for (int i = 0; i < map.bands.Count; i++)
            {
                if (map.bands[i].band == band)
                {
                    result = map.bands[i];
                    break;
                }
            }

            if (result == null && band == BeatBand.Kick)
            {
                for (int i = 0; i < map.bands.Count; i++)
                {
                    if (map.bands[i].band == BeatBand.Full)
                    {
                        result = map.bands[i];
                        break;
                    }
                }
            }

            if (result == null || result.flux.Length == 0 || map.frameTimes.Length == 0)
            {
                return 0f;
            }

            int index = FindNearestFrame(map.frameTimes, time);
            return result.flux[index];
        }

        static int FindNearestFrame(float[] times, float time)
        {
            int lo = 0;
            int hi = times.Length - 1;

            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;

                if (times[mid] < time)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            int best = Math.Max(0, Math.Min(times.Length - 1, lo));

            if (lo > 0 && Math.Abs(times[lo - 1] - time) < Math.Abs(times[best] - time))
            {
                best = lo - 1;
            }

            return best;
        }
    }
}
