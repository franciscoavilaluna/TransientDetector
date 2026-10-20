using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using AudioBeatDetector;

namespace AudioBeatDetector.Editor
{
    public class BeatAnalysisWindow : EditorWindow
    {
        [SerializeField] AudioClip _clip;
        [SerializeField] DetectorSettings _settings = new DetectorSettings();
        BeatMap _map;
        Texture2D _waveform;
        Vector2 _scroll;
        string _error;

        [MenuItem("Window/Audio Beat Detector/Beat Analysis")]
        static void Open()
        {
            var window = GetWindow<BeatAnalysisWindow>("Beat Analysis");
            window.minSize = new Vector2(640f, 420f);
            window.Show();
        }

        void OnDisable()
        {
            DestroyWaveform();
        }

        void OnGUI()
        {
            EditorGUILayout.Space();
            _clip = (AudioClip)EditorGUILayout.ObjectField(
                "Audio Clip",
                _clip,
                typeof(AudioClip),
                false);

            var serialized = new SerializedObject(this);
            serialized.Update();
            SerializedProperty settingsProperty = serialized.FindProperty("_settings");

            if (settingsProperty != null)
            {
                EditorGUILayout.PropertyField(
                    settingsProperty,
                    new GUIContent("Detector Settings"),
                    true);
                serialized.ApplyModifiedProperties();
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_clip == null))
                {
                    if (GUILayout.Button("Analyze", GUILayout.Height(28f)))
                    {
                        Analyze();
                    }
                }

                if (GUILayout.Button("Clear", GUILayout.Height(28f), GUILayout.Width(80f)))
                {
                    _map = null;
                    _error = null;
                    DestroyWaveform();
                }
            }

            if (!string.IsNullOrEmpty(_error))
            {
                EditorGUILayout.HelpBox(_error, MessageType.Error);
            }

            if (_map == null)
            {
                EditorGUILayout.HelpBox(
                    "Select an AudioClip with Decompress On Load and Preload Audio Data enabled, then press Analyze.",
                    MessageType.Info);
                return;
            }

            DrawSummary();
            DrawWaveform();
            DrawOnsets();
        }

        void Analyze()
        {
            _error = null;
            DestroyWaveform();

            try
            {
                _map = TransientDetector.AnalyzeAudio(_clip, _settings, new EditorProgress());
                BuildWaveform(_clip);
            }
            catch (Exception exception)
            {
                _map = null;
                _error = exception.GetType().Name + ": " + exception.Message;
                Debug.LogException(exception);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        void DrawSummary()
        {
            EditorGUILayout.LabelField("Analysis", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("BPM", _map.bpm > 0f ? _map.bpm.ToString("F2") : "not detected");
            EditorGUILayout.LabelField("Tempo confidence", _map.tempoConfidence.ToString("F2"));
            EditorGUILayout.LabelField("Duration", _map.duration.ToString("F2") + " s");
            EditorGUILayout.LabelField("Onsets", _map.onsets.Count.ToString());
            EditorGUILayout.LabelField("Beat grid", _map.beatGrid.Count.ToString());
            EditorGUILayout.LabelField("Downbeats", _map.downbeats.Count.ToString());
            EditorGUILayout.LabelField("Bands", _map.bands.Count.ToString());

            for (int i = 0; i < _map.bands.Count; i++)
            {
                BandResult band = _map.bands[i];
                EditorGUILayout.LabelField(
                    "  " + band.name,
                    band.beats.Count + " onsets");
            }
        }

        void DrawWaveform()
        {
            Rect rect = GUILayoutUtility.GetRect(10f, 140f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, new Color(0.11f, 0.11f, 0.11f));

            if (_waveform != null)
            {
                GUI.DrawTexture(rect, _waveform, ScaleMode.StretchToFill);
            }

            if (_map.duration <= 0f)
            {
                return;
            }

            foreach (float time in _map.beatGrid)
            {
                DrawMarker(rect, time, new Color(0.2f, 0.85f, 1f, 0.55f), 1f);
            }

            foreach (float time in _map.downbeats)
            {
                DrawMarker(rect, time, new Color(1f, 0.25f, 0.25f, 0.9f), 2f);
            }

            foreach (BeatEvent onset in _map.onsets)
            {
                DrawMarker(rect, onset.time, new Color(1f, 0.85f, 0.2f, 0.8f), 1f);
            }
        }

        void DrawMarker(Rect rect, float time, Color color, float width)
        {
            float x = rect.x + Mathf.Clamp01(time / _map.duration) * rect.width;
            EditorGUI.DrawRect(new Rect(x, rect.y, width, rect.height), color);
        }

        void DrawOnsets()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("First onsets", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(120f));

            int count = Mathf.Min(40, _map.onsets.Count);

            for (int i = 0; i < count; i++)
            {
                BeatEvent onset = _map.onsets[i];
                EditorGUILayout.LabelField(
                    onset.time.ToString("F3") + " s",
                    onset.band + "  strength " + onset.strength.ToString("F2"));
            }

            EditorGUILayout.EndScrollView();
        }

        void BuildWaveform(AudioClip clip)
        {
            float[] samples = TransientDetector.ToMono(clip);

            if (samples.Length == 0)
            {
                return;
            }

            const int width = 1024;
            const int height = 140;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color32[width * height];

            for (int x = 0; x < width; x++)
            {
                int start = (int)((long)x * samples.Length / width);
                int end = (int)((long)(x + 1) * samples.Length / width);
                end = Mathf.Max(start + 1, end);

                float min = 1f;
                float max = -1f;

                for (int i = start; i < end && i < samples.Length; i++)
                {
                    float value = samples[i];
                    min = Mathf.Min(min, value);
                    max = Mathf.Max(max, value);
                }

                int low = Mathf.Clamp(
                    Mathf.RoundToInt((min * 0.5f + 0.5f) * (height - 1)),
                    0,
                    height - 1);
                int high = Mathf.Clamp(
                    Mathf.RoundToInt((max * 0.5f + 0.5f) * (height - 1)),
                    0,
                    height - 1);

                for (int y = low; y <= high; y++)
                {
                    pixels[y * width + x] = new Color32(90, 170, 210, 255);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            _waveform = texture;
        }

        void DestroyWaveform()
        {
            if (_waveform != null)
            {
                DestroyImmediate(_waveform);
                _waveform = null;
            }
        }

        class EditorProgress : IProgress<float>
        {
            public void Report(float value)
            {
                EditorUtility.DisplayProgressBar(
                    "Analyzing audio",
                    Mathf.RoundToInt(value * 100f) + "%",
                    Mathf.Clamp01(value));
            }
        }
    }
}
