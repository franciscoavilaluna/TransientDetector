using UnityEngine;
using MathNet.Numerics.IntegralTransforms;
using System.Numerics;
using System.Collections.Generic;

namespace AudioBeatDetector
{
    public class TransientDetector : MonoBehaviour
    {
        public List<float> AnalyzeAudio(AudioClip song)
        {
            int indexStart = 0;
            int windowSize = 2048;
            float[] window = new float[windowSize];
            Complex[] complexBuffer = new Complex[windowSize];
            float previousBassMagnitude = 0f;
            float sensitivity = 1.3f;
            float backgroundNoise = 0.1f;
            Queue<float> fluxHistory = new Queue<float>();
            List<float> beatTimes = new List<float>();

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

                Fourier.Forward(complexBuffer, FourierOptions.AsymmetricScaling);

                for (int k = 1; k <= 7; k++)
                {
                    bassMagnitude += (float)complexBuffer[k].Magnitude;
                }
                
                // spectral flux
                float flux = bassMagnitude - previousBassMagnitude;
                float spectralFlux = Mathf.Max(0, flux);
                previousBassMagnitude = bassMagnitude;

                // dynamic average
                float totalSum = 0f;
                float average = 0f;
                foreach (float k in fluxHistory)
                {
                    totalSum += k;
                }

                if (fluxHistory.Count > 0)
                {
                    average = totalSum / fluxHistory.Count;
                }
                else
                {
                    average = 0f;
                }

                float threshold = average * sensitivity;

                if (spectralFlux > threshold && spectralFlux > backgroundNoise)
                {
                    // Debug.Log("BEAT on sample: " + indexStart);
                    float t = (float)indexStart / song.frequency;
                    beatTimes.Add(t);
                }

                fluxHistory.Enqueue(spectralFlux);

                if (fluxHistory.Count > 15)
                {
                    fluxHistory.Dequeue();
                }

            }
            return beatTimes;
            //Debug.Log("Values for window: " + window[0]);
        }
    }
}
