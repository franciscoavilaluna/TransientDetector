using UnityEngine;
using MathNet.Numerics.IntegralTransforms;
using System.Numerics;

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

                Complex[] compexBuffer = new Complex[windowSize];

                // SAMPLE x HANN COEFICIENT
                for (int i = 0; i < windowSize; i++)
                {
                    float hannCoefficient = 0.5f * ( 1f - Mathf.Cos(2f * Mathf.PI * (float)i / (windowSize - 1)) );
                    window[i] = window[i] * hannCoefficient;

                    compexBuffer[i] = new Complex(window[i], 0.0);
                }

                Fourier.Forward(compexBuffer, FourierOptions.Asymmetric);

                float bassMagnitude = 0f;
                for (int k = 1; k <= 7; k++)
                {
                    bassMagnitude += (float)compexBuffer[k].Magnitude;
                }
            }
            Debug.Log("Values for window: " + window[0]);
        }
    }
}
