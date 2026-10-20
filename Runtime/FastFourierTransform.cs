using System;
using System.Collections.Concurrent;
using System.Numerics;

namespace AudioBeatDetector
{
    public static class FastFourierTransform
    {
        static readonly ConcurrentDictionary<int, Complex[]> Roots = new ConcurrentDictionary<int, Complex[]>();

        public static void Forward(Complex[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            int n = buffer.Length;

            if (n <= 1)
            {
                return;
            }

            if ((n & (n - 1)) != 0)
            {
                throw new ArgumentException("FFT length must be a power of two.", nameof(buffer));
            }

            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;

                for (; (j & bit) != 0; bit >>= 1)
                {
                    j ^= bit;
                }

                j ^= bit;

                if (i < j)
                {
                    Complex temp = buffer[i];
                    buffer[i] = buffer[j];
                    buffer[j] = temp;
                }
            }

            Complex[] roots = GetRoots(n);

            for (int length = 2; length <= n; length <<= 1)
            {
                int half = length >> 1;
                int step = n / length;

                for (int i = 0; i < n; i += length)
                {
                    for (int j = 0; j < half; j++)
                    {
                        Complex w = roots[j * step];
                        Complex u = buffer[i + j];
                        Complex v = buffer[i + j + half] * w;
                        buffer[i + j] = u + v;
                        buffer[i + j + half] = u - v;
                    }
                }
            }
        }

        static Complex[] GetRoots(int n)
        {
            return Roots.GetOrAdd(n, size =>
            {
                var roots = new Complex[size / 2];

                for (int k = 0; k < roots.Length; k++)
                {
                    double angle = -2.0 * Math.PI * k / size;
                    roots[k] = new Complex(Math.Cos(angle), Math.Sin(angle));
                }

                return roots;
            });
        }
    }
}
