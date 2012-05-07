using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;

using AForge.Imaging;
using AForge.Imaging.Filters;

namespace BOUNLib.NET
{
    namespace ImageFilters
    {
        /// <summary>
        /// Custom AForge filter for sign detection.
        /// </summary>
        public class SignFilter : BaseFilter
        {
            private const int THRESHOLD_MIN = 600;
            private const int THRESHOLD_MAX = 4000;
            int adjustParamCounter = 0;
            int adjustParams = 0;

            double greenCoeff = 0;

            public double GreenCoeff
            {
                get { return greenCoeff; }
                set { greenCoeff = value; }
            }
            double blueCoeff = 0;

            public double BlueCoeff
            {
                get { return blueCoeff; }
                set { blueCoeff = value; }
            }

            double saturationCoeff = 0;

            public double SaturationCoeff
            {
                get { return saturationCoeff; }
                set { saturationCoeff = value; }
            }

            public int[] xProj = null;
            public int[] yProj = null;

            // private format translation dictionary
            private Dictionary<PixelFormat, PixelFormat> formatTransalations = new Dictionary<PixelFormat, PixelFormat>();

            public SignFilter(double greenCoefficient, double blueCoefficient, int adjustParams)
            {
                this.adjustParams = adjustParams;
                adjustParamCounter = adjustParams + 1;
                greenCoeff = greenCoefficient;
                blueCoeff = blueCoefficient;
                formatTransalations[PixelFormat.Format24bppRgb] = PixelFormat.Format8bppIndexed;
                formatTransalations[PixelFormat.Format32bppRgb] = PixelFormat.Format8bppIndexed;
            }

            public override Dictionary<PixelFormat, PixelFormat> FormatTransalations
            {
                get { return formatTransalations; }
            }

            protected unsafe override void ProcessFilter(UnmanagedImage sourceData, UnmanagedImage destinationData)
            {
                bool adjustParameters = false;
                if (adjustParamCounter > 0)
                {
                    if (adjustParamCounter == 1)
                    {
                        adjustParameters = true;
                        adjustParamCounter = adjustParams;
                    }
                    else
                    {
                        adjustParamCounter--;
                    }
                }

                int numOfPoints = 0;
                // get width and height
                int width = sourceData.Width;
                int height = sourceData.Height;
                PixelFormat srcPixelFormat = sourceData.PixelFormat;

                xProj = new int[width];
                yProj = new int[height];

                if (
                    (srcPixelFormat == PixelFormat.Format24bppRgb) ||
                    (srcPixelFormat == PixelFormat.Format32bppRgb) ||
                    (srcPixelFormat == PixelFormat.Format32bppArgb))
                {
                    int pixelSize = (srcPixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;
                    int srcOffset = sourceData.Stride - width * pixelSize;
                    int dstOffset = destinationData.Stride - width;

                    // do the job
                    byte* src = (byte*)sourceData.ImageData.ToPointer();
                    byte* dst = (byte*)destinationData.ImageData.ToPointer();

                    for (int y = 0; y < height; y++)
                    {
                        // for each pixel
                        for (int x = 0; x < width; x++, src += pixelSize, dst++)
                        {
                            if (x > Constants.IMAGE_WIDTH * 0.25 && x < Constants.IMAGE_WIDTH * 0.75 && y < Constants.IMAGE_HEIGHT * 0.25)
                            {
                                *dst = (byte)0;
                            }
                            else if (src[RGB.R] > src[RGB.G] * greenCoeff && src[RGB.R] > src[RGB.B] * blueCoeff)
                            // if (src[RGB.R]*2 > (src[RGB.G] * greenCoeff + src[RGB.B] * blueCoeff))
                            {
                                *dst = (byte)255;
                                xProj[x]++;
                                yProj[y]++;
                                numOfPoints++;
                            }
                            else
                            {
                                *dst = (byte)0;
                            }
                        }
                        src += srcOffset;
                        dst += dstOffset;
                    }
                }
                if (adjustParameters)
                {
                    if (numOfPoints < THRESHOLD_MIN)
                    {
                        greenCoeff /= 1.04;
                        blueCoeff /= 1.04;
                    }
                    else if (numOfPoints > THRESHOLD_MAX)
                    {
                        greenCoeff *= 1.04;
                        blueCoeff *= 1.04;
                    }
                    else if (numOfPoints > THRESHOLD_MIN * 1.3)
                    {   // Try to minimize the number of red points
                        greenCoeff *= 1.02;
                        blueCoeff *= 1.02;
                    }
                    //if (blueCoeff > 1.5d)
                    //    blueCoeff = 1.5d;
                    //if (greenCoeff > 1.5d)
                    //    greenCoeff = 1.5d;
                    if (blueCoeff < 1.1)
                        blueCoeff = 1.1;
                    if (greenCoeff < 1.1)
                        greenCoeff = 1.1;
                }
            }
        }

        /// <summary>
        /// Custom AForge filter for lane detection
        /// </summary>
        public class LaneFilter : BaseFilter
        {
            double red = 0;
            double green = 0;
            double blue = 0;

            public static int[] xProj = null;
            public static int[] yProj = null;

            // private format translation dictionary
            private Dictionary<PixelFormat, PixelFormat> formatTransalations = new Dictionary<PixelFormat, PixelFormat>();

            public LaneFilter(double red, double green, double blue)
            {
                this.red = red;
                this.green = green;
                this.blue = blue;
                formatTransalations[PixelFormat.Format24bppRgb] = PixelFormat.Format8bppIndexed;
                formatTransalations[PixelFormat.Format32bppRgb] = PixelFormat.Format8bppIndexed;
            }

            public override Dictionary<PixelFormat, PixelFormat> FormatTransalations
            {
                get { return formatTransalations; }
            }

            protected unsafe override void ProcessFilter(UnmanagedImage sourceData, UnmanagedImage destinationData)
            {
                // get width and height
                int width = sourceData.Width;
                int height = sourceData.Height;
                PixelFormat srcPixelFormat = sourceData.PixelFormat;


                if (
                    (srcPixelFormat == PixelFormat.Format24bppRgb) ||
                    (srcPixelFormat == PixelFormat.Format32bppRgb) ||
                    (srcPixelFormat == PixelFormat.Format32bppArgb))
                {
                    int pixelSize = (srcPixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;
                    int srcOffset = sourceData.Stride - width * pixelSize;
                    int dstOffset = destinationData.Stride - width;

                    // do the job
                    byte* src = (byte*)sourceData.ImageData.ToPointer();
                    byte* dst = (byte*)destinationData.ImageData.ToPointer();

                    for (int y = 0; y < height; y++)
                    {
                        // for each pixel
                        for (int x = 0; x < width; x++, src += pixelSize, dst++)
                        {
                            if (src[RGB.R] > red && src[RGB.G] > green && src[RGB.B] > blue)
                            {
                                *dst = (byte)255;

                            }
                            else
                            {
                                *dst = (byte)0;
                            }
                        }
                        src += srcOffset;
                        dst += dstOffset;
                    }
                }
            }
        }

        /// <summary>
        /// Custom AForge filter for color labeling
        /// </summary>
        public class ColorLabelFilter : BaseFilter
        {

            private Color[] colors = null;
            private bool isRGB = true;
            // private format translation dictionary
            private Dictionary<PixelFormat, PixelFormat> formatTransalations = new Dictionary<PixelFormat, PixelFormat>();

            public ColorLabelFilter(Color[] colors, bool isRGB)
            {
                this.colors = colors;
                this.isRGB = isRGB;
                formatTransalations[PixelFormat.Format24bppRgb] = PixelFormat.Format24bppRgb;
                formatTransalations[PixelFormat.Format32bppRgb] = PixelFormat.Format24bppRgb;
            }

            public override Dictionary<PixelFormat, PixelFormat> FormatTransalations
            {
                get { return formatTransalations; }
            }

            protected unsafe override void ProcessFilter(UnmanagedImage sourceData, UnmanagedImage destinationData)
            {
                // get width and height
                int width = sourceData.Width;
                int height = sourceData.Height;
                PixelFormat srcPixelFormat = sourceData.PixelFormat;


                if (
                    (srcPixelFormat == PixelFormat.Format24bppRgb) ||
                    (srcPixelFormat == PixelFormat.Format32bppRgb) ||
                    (srcPixelFormat == PixelFormat.Format32bppArgb))
                {
                    int pixelSize = (srcPixelFormat == PixelFormat.Format24bppRgb) ? 3 : 4;
                    int srcOffset = sourceData.Stride - width * pixelSize;
                    int dstOffset = destinationData.Stride - width * 3;

                    // do the job
                    byte* src = (byte*)sourceData.ImageData.ToPointer();
                    byte* dst = (byte*)destinationData.ImageData.ToPointer();

                    for (int y = 0; y < height; y++)
                    {
                        // for each pixel
                        for (int x = 0; x < width; x++, src += pixelSize, dst += 3)
                        {
                            int dist = Int32.MaxValue;
                            int idx = 0;
                            for (int i = 0; i < colors.Length; i++)
                            {
                                if (isRGB)
                                {
                                    int tmp_dist = Math.Abs(src[RGB.R] - colors[i].R) + Math.Abs(src[RGB.G] - colors[i].G) + Math.Abs(src[RGB.B] - colors[i].B);
                                    if (tmp_dist < dist)
                                    {
                                        idx = i;
                                        dist = tmp_dist;
                                    }
                                }
                                else
                                {
                                    HSL color = HSL.FromRGB(new RGB(colors[i]));
                                    HSL pixel = HSL.FromRGB(new RGB(src[RGB.R], src[RGB.G], src[RGB.B]));
                                    int tmp_dist = (int) ((Math.Abs(color.Hue - pixel.Hue) % 180) +
                                        Math.Abs(color.Luminance - pixel.Luminance) * 180 +
                                        Math.Abs(color.Saturation - pixel.Saturation) * 180);

                                    if (tmp_dist < dist)
                                    {
                                        idx = i;
                                        dist = tmp_dist;
                                    }

                                }
                            }
                            dst[RGB.R] = colors[idx].R;
                            dst[RGB.G] = colors[idx].G;
                            dst[RGB.B] = colors[idx].B;
                        }
                        src += srcOffset;
                        dst += dstOffset;
                    }
                }

            }
        }
        
        public class SpatialColorLabelFilter : BaseFilter
        {

            private Color[] colors = null;
            private int spatialRange = 20;
            private int spatialStep = 1;
            // private format translation dictionary
            private Dictionary<PixelFormat, PixelFormat> formatTransalations = new Dictionary<PixelFormat, PixelFormat>();

            public SpatialColorLabelFilter(Color[] colors, int spatialRange, int spatialStep)
            {
                this.colors = colors;
                this.spatialRange = spatialRange;
                this.spatialStep = spatialStep;
                formatTransalations[PixelFormat.Format24bppRgb] = PixelFormat.Format24bppRgb;
            }

            public SpatialColorLabelFilter(Color[] colors)
            {
                this.colors = colors;
                formatTransalations[PixelFormat.Format24bppRgb] = PixelFormat.Format24bppRgb;
            }

            public override Dictionary<PixelFormat, PixelFormat> FormatTransalations
            {
                get { return formatTransalations; }
            }

            protected unsafe override void ProcessFilter(UnmanagedImage sourceData, UnmanagedImage destinationData)
            {

                // get width and height
                int width = sourceData.Width;
                int height = sourceData.Height;
                PixelFormat srcPixelFormat = sourceData.PixelFormat;

                if (srcPixelFormat == PixelFormat.Format24bppRgb)
                {
                    byte* src = (byte*)sourceData.ImageData.ToPointer();
                    byte* dst = (byte*)destinationData.ImageData.ToPointer();
                    for (int i = 0; i < height * width * 3; )
                    {
                        dst[i] = 0; i++;// src[i++];
                        dst[i] = 0; i++;
                        dst[i] = src[i]; i++;// src[i++];
                    }
                }
            }
        }
    }
}
