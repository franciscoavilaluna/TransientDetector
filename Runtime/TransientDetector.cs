using UnityEngine;

namespace AudioBeatDetector
{
    public class TransientDetector : MonoBehaviour
    {
        public AudioClip song;

        void Start()
        {
            int indexStart = 0;
            int windowSize = 2048;
            float[] window = new float[windowSize];
            while (indexStart + windowSize <= song.samples)
            {
                song.GetData(window, indexStart);
                indexStart += windowSize / 2;
            }
            Debug.Log("Values for window: " + window[0]);
        }
    }
}
