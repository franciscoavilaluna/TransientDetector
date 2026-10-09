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
            
            List<float> allFluxes = new List<float>();
            List<float> allTimes = new List<float>();

            while (indexStart + windowSize <= song.samples)
            {
                float bassMagnitude = 0f;

                song.GetData(window, indexStart);
                
                float currentTime = (float)indexStart / song.frequency;
                allTimes.Add(currentTime);

                indexStart += windowSize / 2;

                for (int i = 0; i < windowSize; i++)
                {
                    float hannCoefficient = 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * (float)i / (windowSize - 1)));
                    window[i] = window[i] * hannCoefficient;
                    complexBuffer[i] = new Complex(window[i], 0.0);
                }

                Fourier.Forward(complexBuffer, FourierOptions.AsymmetricScaling);

                for (int k = 2; k <= 5; k++)
                {
                    bassMagnitude += (float)complexBuffer[k].Magnitude;
                }

                float flux = bassMagnitude - previousBassMagnitude;
                float spectralFlux = Mathf.Max(0f, flux);
                previousBassMagnitude = bassMagnitude;

                allFluxes.Add(spectralFlux);
            }

            List<float> beatTimes = new List<float>();
            Queue<float> fluxHistory = new Queue<float>();
            
            float sensitivity = 1.8f;
            float backgroundNoise = 0.15f;
            float cooldown = 0.25f;
            float lastBeatTime = -1f;

            for (int i = 1; i < allFluxes.Count - 1; i++)
            {
                float currentFlux = allFluxes[i];
                float currentTime = allTimes[i];

                bool isLocalMaximum = (currentFlux > allFluxes[i - 1]) && (currentFlux >= allFluxes[i + 1]);

                float totalSum = 0f;
                foreach (float val in fluxHistory)
                {
                    totalSum += val;
                }
                float average = (fluxHistory.Count > 0) ? (totalSum / fluxHistory.Count) : 0f;
                float threshold = average * sensitivity;

                if (isLocalMaximum && currentFlux > threshold && currentFlux > backgroundNoise && (currentTime - lastBeatTime) >= cooldown)
                {
                    beatTimes.Add(currentTime);
                    lastBeatTime = currentTime;
                }
                fluxHistory.Enqueue(currentFlux);
                if (fluxHistory.Count > 15)
                {
                    fluxHistory.Dequeue();
                }
            }

            return beatTimes;
        }
    }
}
