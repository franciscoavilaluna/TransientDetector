using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AudioBeatDetector
{
    public static class BeatMapCache
    {
        static readonly Dictionary<string, BeatMap> Cache = new Dictionary<string, BeatMap>();

        public static BeatMap GetOrAnalyze(AudioClip clip, DetectorSettings settings)
        {
            if (clip == null)
            {
                throw new ArgumentNullException(nameof(clip));
            }

            if (settings == null)
            {
                settings = new DetectorSettings();
            }

            string key = BuildKey(clip, settings);

            if (Cache.TryGetValue(key, out BeatMap cached))
            {
                return cached;
            }

            BeatMap map = TransientDetector.AnalyzeAudio(clip, settings);
            Cache[key] = map;
            return map;
        }

        public static string ToJson(BeatMap map)
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            return JsonUtility.ToJson(map);
        }

        public static BeatMap FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentException("json is empty", nameof(json));
            }

            return JsonUtility.FromJson<BeatMap>(json);
        }

        public static void SaveToFile(BeatMap map, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("path is empty", nameof(path));
            }

            File.WriteAllText(path, ToJson(map));
        }

        public static BeatMap LoadFromFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("path is empty", nameof(path));
            }

            return FromJson(File.ReadAllText(path));
        }

        public static void Clear()
        {
            Cache.Clear();
        }

        static string BuildKey(AudioClip clip, DetectorSettings settings)
        {
            return clip.name + "|" + clip.samples + "|" + clip.frequency + "|" + JsonUtility.ToJson(settings);
        }
    }
}
