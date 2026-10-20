using UnityEngine;

namespace AudioBeatDetector
{
    [CreateAssetMenu(
        fileName = "BeatMap",
        menuName = "Audio Beat Detector/Beat Map Asset",
        order = 1)]
    public class BeatMapAsset : ScriptableObject
    {
        public BeatMap map;
    }
}
