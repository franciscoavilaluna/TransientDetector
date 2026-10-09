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

                // SAMPLE x HANN COEFICIENT
                for (int i = 0; i < windowSize; i++)
                {
                    float hannCoefficient = 0.5f * ( 1f - Mathf.Cos(2f * Mathf.PI * (float)i / (windowSize - 1)) );
                    window[i] = window[i] * hannCoefficient;
                }
            }
            Debug.Log("Values for window: " + window[0]);
        }
    }
}
