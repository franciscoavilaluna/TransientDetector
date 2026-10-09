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
            Complex[] complexBuffer = new Complex[windowSize];
            float previousBassMagnitude = 0f;

            while (indexStart + windowSize <= song.samples)
            {
                float bassMagnitude = 0f;

                song.GetData(window, indexStart);
                indexStart += windowSize / 2;


                // SAMPLE x HANN COEFICIENT
                for (int i = 0; i < windowSize; i++)
                {
                    float hannCoefficient = 0.5f * ( 1f - Mathf.Cos(2f * Mathf.PI * (float)i / (windowSize - 1)) );
                    window[i] = window[i] * hannCoefficient;

                    complexBuffer[i] = new Complex(window[i], 0.0);
                }

                Fourier.Forward(complexBuffer, FourierOptions.Asymmetric);

                for (int k = 1; k <= 7; k++)
                {
                    bassMagnitude += (float)complexBuffer[k].Magnitude;
                }
                
                float flux = bassMagnitude - previousBassMagnitude;
                float spectralFlux = Mathf.Max(0, flux);
                previousBassMagnitude = bassMagnitude;
            }
            Debug.Log("Values for window: " + window[0]);
        }
    }
}
