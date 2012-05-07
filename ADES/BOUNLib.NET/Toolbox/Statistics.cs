using System;
using System.Collections.Generic;
using System.Text;

namespace BOUNLib
{
    namespace ToolBox
    {
        public class Statistics
        {
            public static double NormalDistribution(double x, double mean, double deviation, bool cumulative)
            {
                if (cumulative)
                    return CumulativeDistribution(x, mean, deviation);
                else
                    return NormalDensity(x, mean, deviation);
            }

            private static double NormalDensity(double x, double mean, double deviation)
            {
                return Math.Exp(-(Math.Pow((x - mean) / deviation, 2) / 2)) / Math.Sqrt(2 * Math.PI) / deviation;
            }

            private static double CumulativeDistribution(double x, double mean, double deviation)
            {
                // TODO: Change the number of iterations (16) for more or less precision.
                // You could also change the logic of the recursive function (stop calling
                // for more terms, when the values are below a specific threshold for example.
                return (ErrorFunction((x - mean) / deviation / Math.Sqrt(2), 0, 16) + 1) / 2;
            }

            private static double ErrorFunction(double x, int iteration, int iterations)
            {
                double partValue;
                partValue = 2 / Math.Sqrt(Math.PI) * Math.Pow(-1, iteration) * Math.Pow(x, 2 * iteration + 1) / Factorial(iteration) / (2 * iteration + 1);

                if (iteration == iterations)
                    return partValue;
                else
                    return ErrorFunction(x, iteration + 1, iterations) + partValue;
            }

            private static int Factorial(int x)
            {
                if (x == 0)
                    return 1;
                else
                    return x * Factorial(x - 1);
            }

            public static double GetMean(int[] data)
            {
                int len = data.Length;
                if (len == 0)
                    throw new Exception("No data");

                double sum = 0;
                for (int i = 0; i < data.Length; i++)
                    sum += data[i];
                return sum / len;
            }

            /// <summary>
            /// Get variance
            /// </summary>
            public static double GetVariance(int[] data)
            {
                int len = data.Length;
                // Get average
                double avg = GetMean(data);

                double sum = 0;
                for (int i = 0; i < data.Length; i++)
                    sum += Math.Pow((data[i] - avg), 2);
                return sum / len;
            }

            /// <summary>
            /// Get standard deviation
            /// </summary>
            public static double GetStdev(int[] data)
            {
                return Math.Sqrt(GetVariance(data));
            } 

        }
    }
}
