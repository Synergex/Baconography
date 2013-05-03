﻿using System;
using FluxJpeg.Core.Decoder;

namespace FluxJpeg.Core
{
    partial class DCT
    {
        public const int N = 8;

        public int[][] quantum = new int[2][];
        public double[][] divisors = new double[2][];

        // Quantitization Matrix for luminace.
        public double[] DivisorsLuminance = new double[N * N];

        // Quantitization Matrix for chrominance.
        public double[] DivisorsChrominance = new double[N * N];

        public DCT(int quality) : this()
        {
            Initialize(quality);
        }

        private void Initialize(int quality)
        {
            double[] aanScaleFactor = 
            {
                1.0, 1.387039845, 1.306562965, 1.175875602,
                1.0, 0.785694958, 0.541196100, 0.275899379
            };

            int i, j, index, Quality;

            // jpeg_quality_scaling
            if (quality <= 0) Quality = 1;
            else if (quality > 100) Quality = 100;
            else if (quality < 50) Quality = 5000 / quality;
            else Quality = 200 - quality * 2;

            int[] scaledLum = JpegQuantizationTable.K1Luminance
                .getScaledInstance(Quality / 100f, true).Table;

            index = 0;
            for (i = 0; i < 8; i++)
            {
                for (j = 0; j < 8; j++)
                {
                    DivisorsLuminance[index] =
                        (double)1.0 /
                        ((double)scaledLum[index] * aanScaleFactor[i] * aanScaleFactor[j] * 8.0);

                    index++;
                }
            }

            // Creating the chrominance matrix
            int[] scaledChrom = JpegQuantizationTable.K2Chrominance
                .getScaledInstance(Quality / 100f, true).Table;

            index = 0;
            for (i = 0; i < 8; i++)
            {
                for (j = 0; j < 8; j++)
                {
                    DivisorsChrominance[index] = (double)((double)1.0 / ((double)scaledChrom[index] * aanScaleFactor[i] * aanScaleFactor[j] * (double)8.0));
                    index++;
                }
            }

            quantum[0] = scaledLum;
            divisors[0] = DivisorsLuminance;
            quantum[1] = scaledChrom;
            divisors[1] = DivisorsChrominance;
        }

        internal float[,] FastFDCT(float[,] input)
        {
            float[,] output = new float[N, N];

            float tmp0, tmp1, tmp2, tmp3, tmp4, tmp5, tmp6, tmp7;
            float tmp10, tmp11, tmp12, tmp13;
            float z1, z2, z3, z4, z5, z11, z13;
            int i, j;

            for (i = 0; i < 8; i++)
                for (j = 0; j < 8; j++)
                    output[i, j] = input[i, j] - 128f;

            // Pass 1: process rows.

            for (i = 0; i < 8; i++)
            {
                tmp0 = output[i, 0] + output[i, 7];
                tmp7 = output[i, 0] - output[i, 7];
                tmp1 = output[i, 1] + output[i, 6];
                tmp6 = output[i, 1] - output[i, 6];
                tmp2 = output[i, 2] + output[i, 5];
                tmp5 = output[i, 2] - output[i, 5];
                tmp3 = output[i, 3] + output[i, 4];
                tmp4 = output[i, 3] - output[i, 4];

                // Even part
                tmp10 = tmp0 + tmp3;
                tmp13 = tmp0 - tmp3;
                tmp11 = tmp1 + tmp2;
                tmp12 = tmp1 - tmp2;

                output[i, 0] = tmp10 + tmp11;
                output[i, 4] = tmp10 - tmp11;

                z1 = (tmp12 + tmp13) * (float)0.707106781;
                output[i, 2] = tmp13 + z1;
                output[i, 6] = tmp13 - z1;

                // Odd part
                tmp10 = tmp4 + tmp5;
                tmp11 = tmp5 + tmp6;
                tmp12 = tmp6 + tmp7;

                // The rotator is modified from fig 4-8 to avoid extra negations.
                z5 = (tmp10 - tmp12) * (float)0.382683433;
                z2 = ((float)0.541196100) * tmp10 + z5;
                z4 = ((float)1.306562965) * tmp12 + z5;
                z3 = tmp11 * ((float)0.707106781);

                z11 = tmp7 + z3;
                z13 = tmp7 - z3;

                output[i, 5] = z13 + z2;
                output[i, 3] = z13 - z2;
                output[i, 1] = z11 + z4;
                output[i, 7] = z11 - z4;
            }

            // Pass 2: process columns

            for (i = 0; i < 8; i++)
            {
                tmp0 = output[0, i] + output[7, i];
                tmp7 = output[0, i] - output[7, i];
                tmp1 = output[1, i] + output[6, i];
                tmp6 = output[1, i] - output[6, i];
                tmp2 = output[2, i] + output[5, i];
                tmp5 = output[2, i] - output[5, i];
                tmp3 = output[3, i] + output[4, i];
                tmp4 = output[3, i] - output[4, i];

                // Even part
                tmp10 = tmp0 + tmp3;
                tmp13 = tmp0 - tmp3;
                tmp11 = tmp1 + tmp2;
                tmp12 = tmp1 - tmp2;

                output[0, i] = tmp10 + tmp11;
                output[4, i] = tmp10 - tmp11;

                z1 = (tmp12 + tmp13) * (float)0.707106781;
                output[2, i] = tmp13 + z1;
                output[6, i] = tmp13 - z1;

                // Odd part
                tmp10 = tmp4 + tmp5;
                tmp11 = tmp5 + tmp6;
                tmp12 = tmp6 + tmp7;

                // The rotator is modified from fig 4-8 to avoid extra negations.
                z5 = (tmp10 - tmp12) * (float)0.382683433;
                z2 = ((float)0.541196100) * tmp10 + z5;
                z4 = ((float)1.306562965) * tmp12 + z5;
                z3 = tmp11 * ((float)0.707106781);

                z11 = tmp7 + z3;
                z13 = tmp7 - z3;

                output[5, i] = z13 + z2;
                output[3, i] = z13 - z2;
                output[1, i] = z11 + z4;
                output[7, i] = z11 - z4;
            }

            return output;
        }


        internal int[] QuantizeBlock(float[,] inputData, int code)
        {
            int[] result = new int[N * N];
            int index = 0;

            for (int i = 0; i < N; i++)
                for (int j = 0; j < N; j++)
                {
                    result[index] = (int)(Math.Round(inputData[i, j] * divisors[code][index]));
                    index++;
                }

            return result;
        }


    }
}
