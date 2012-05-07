﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Genetic;
using AForge.Math.Random;

using BOUNLib.Messages;
using BOUNLib.Messaging;
using BOUNLib.UDPServices;
using BOUNLib.Sources;
using BOUNLib.Processors;
using BOUNLib.ToolBox;
using BOUNLib.NET.ImageFilters;
using System.Collections;

using SVM;
using AForge.Imaging.ComplexFilters;
using BOUNLib.NET.Toolbox;
using AForge.Neuro;
using OpenSURF;

namespace BOUNLib
{
    namespace Processors
    {

        public delegate void TextMsgHandler(string message);

        public class TextMsgDisplayProcessor : MsgConsumer
        {
            TextMsgHandler tmh;
            public TextMsgDisplayProcessor(MsgService msgService, TextMsgHandler tmh)
                : base(msgService, TextMessage.msgType)
            {
                this.tmh = tmh;
            }

            public override void consumeMessage(IMessage message, int msgID)
            {
                TextMessage tm = message as TextMessage;
                tmh(tm.getMessage());
            }
        }
        
        public interface IPanelDisplay
        {
            Graphics getPanelGraphics();
            void setPanelSize(int width, int height);
            void setStatusText(string statusText);
        }

        public class PanelDisplayProcessor : MsgConsumer
        {
            private IPanelDisplay p;

            public PanelDisplayProcessor(IPanelDisplay p, MsgService msgService)
                : base(msgService, VisionMessage.msgType)
            {
                this.p = p;
            }

            public override void consumeMessage(IMessage message, int msgID)
            {
                if (p == null || p.getPanelGraphics() == null || message == null)
                    return;
                lock (message)
                {

                    long start = DateTime.Now.Ticks / 10000;
                    Globals.FRAMES_PROCESSED_PANELDISPLAY++;

                    VisionMessage vm = message as VisionMessage;
                    Bitmap bitmap = vm.Bitmap;
                    // p.setPanelSize(vm.BmpWidth, vm.BmpHeight);
                    p.getPanelGraphics().DrawImage(bitmap, 0, 0);

                    if (Constants.EVALUATE_TIME_ENABLED)
                    {
                        int x = (int)(DateTime.Now.Ticks / 10000 - start);
                        if (x >= 0)
                        {
                            Globals.TIME_PANELDISPLAY_MIN = x < Globals.TIME_PANELDISPLAY_MIN ? x : Globals.TIME_PANELDISPLAY_MIN;
                            Globals.TIME_PANELDISPLAY_MAX = x > Globals.TIME_PANELDISPLAY_MAX ? x : Globals.TIME_PANELDISPLAY_MAX;
                            Globals.TIME_PANELDISPLAY_TOTAL += x;
                        }
                    }
                    if (Constants.EVALUATE_SUCCESS_ENABLED)
                    {
                        p.setStatusText("Frame, sign, sign: " + Globals.SIGN_IN_FRAME);
                    }
                }
            }

        }

        /// <summary>
        /// Finds the histogram of the captured image for improing the binarization process.
        /// </summary>
        public class HistogramProcessor : MsgConsumer
        {

            public HistogramProcessor(MsgService msgService)
                : base(msgService, VisionMessage.msgType)
            {

            }


            private static byte fastGetPixelB(VisionMessage vm, int x, int y)
            {
                int imgWidth = vm.BmpWidth;
                int pixelSize = vm.BytePerPixel;
                byte[] img = ByteTools.BmpToBytes(vm.Bitmap, vm.Bitmap.PixelFormat);
                return img[y * pixelSize * imgWidth + x * pixelSize];
            }
            private static byte fastGetPixelG(VisionMessage vm, int x, int y)
            {
                int imgWidth = vm.BmpWidth;
                int pixelSize = vm.BytePerPixel;
                byte[] img = ByteTools.BmpToBytes(vm.Bitmap, vm.Bitmap.PixelFormat);
                return img[y * pixelSize * imgWidth + x * pixelSize + 1];
            }
            private static byte fastGetPixelR(VisionMessage vm, int x, int y)
            {
                int imgWidth = vm.BmpWidth;
                int pixelSize = vm.BytePerPixel;
                byte[] img = ByteTools.BmpToBytes(vm.Bitmap, vm.Bitmap.PixelFormat);
                return img[y * pixelSize * imgWidth + x * pixelSize + 2];
            }
            private static void fastSetPixel(VisionMessage vm, int x, int y, byte R, byte G, byte B)
            {
                int imgWidth = vm.BmpWidth;
                int pixelSize = vm.BytePerPixel;
                byte[] img = ByteTools.BmpToBytes(vm.Bitmap, vm.Bitmap.PixelFormat);
                img[y * pixelSize * imgWidth + x * pixelSize] = B;
                img[y * pixelSize * imgWidth + x * pixelSize + 1] = G;
                img[y * pixelSize * imgWidth + x * pixelSize + 2] = R;
            }

            public static void getVmHist(VisionMessage vm, int[] rHist, int[] gHist, int[] bHist)
            {
                for (int x = vm.BmpWidth / 2; x < vm.BmpWidth; x++)
                {
                    for (int y = 0; y < vm.BmpHeight; y++)
                    {
                        rHist[fastGetPixelR(vm, x, y)]++;
                        gHist[fastGetPixelG(vm, x, y)]++;
                        bHist[fastGetPixelB(vm, x, y)]++;
                        fastSetPixel(vm, x, y, 0, 0, 0);
                    }
                }
            }

            public override void consumeMessage(IMessage message, int msgID)
            {
                VisionMessage vm = message as VisionMessage;

                int[] rHist = new int[256];
                int[] gHist = new int[256];
                int[] bHist = new int[256];

                HistogramProcessor.getVmHist(vm, rHist, gHist, bHist);

                for (int i = 0; i < 256; i++)
                {
                    for (int j = 0; j < Math.Min(rHist[i], 5000) / 100; j++)
                    {
                        fastSetPixel(vm, i * 2, 250 - j, 255, 0, 0);
                        fastSetPixel(vm, i * 2 + 1, 250 - j, 255, 0, 0);
                    }
                    for (int j = 0; j < Math.Min(gHist[i], 5000) / 100; j++)
                    {
                        fastSetPixel(vm, i * 2, 250 - j - 75, 0, 255, 0);
                        fastSetPixel(vm, i * 2 + 1, 250 - j - 75, 0, 255, 0);
                    }
                    for (int j = 0; j < Math.Min(bHist[i], 5000) / 100; j++)
                    {
                        fastSetPixel(vm, i * 2, 250 - j - 150, 0, 0, 255);
                        fastSetPixel(vm, i * 2 + 1, 250 - j - 150, 0, 0, 255);
                    }
                }

                msgService.sendMsg(vm);
            }
        }

        /// <summary>
        /// A generic processor for utilizing the AForge image library features.
        /// </summary>
        public class AForgeProcessor : MsgConsumer
        {
            IFilter filter;

            public AForgeProcessor(IFilter filter, MsgService msgService)
                : base(msgService, VisionMessage.msgType)
            {
                if (filter == null)
                    throw new NullReferenceException("Filter cannot be null");
                this.filter = filter;
            }


            public override void consumeMessage(IMessage message, int msgID)
            {
                VisionMessage vm = message as VisionMessage;
                Bitmap rgb = vm.Bitmap;

                rgb = filter.Apply(rgb);

                if (rgb.PixelFormat == PixelFormat.Format8bppIndexed)
                {
                    rgb = new GrayscaleToRGB().Apply(rgb);
                }
                vm = new VisionMessage(rgb.Height, rgb.Width, ByteTools.pixelFormatToBPP(rgb.PixelFormat), rgb);
                msgService.sendMsg(vm);
            }
        }

        /// <summary>
        /// MHT and HMM base lane detector procesor.
        /// </summary>
        public class LaneDetectorProcessor : MsgConsumer
        {
            private HoughLineTransformation lineTransform = new HoughLineTransformation();
            HoughLine[] hls = new HoughLine[100];


            private static int MAX_HOUGH_RADIUS = (int)(Math.Sqrt(Math.Pow(Constants.IMAGE_WIDTH / 2, 2) + Math.Pow(Constants.IMAGE_HEIGHT - 172, 2)));

            double[,] theta_tx = new double[180, 180];
            double[,] theta_ox = new double[180, 180];
            double[,] r_tx = new double[MAX_HOUGH_RADIUS, MAX_HOUGH_RADIUS];
            double[,] r_ox = new double[MAX_HOUGH_RADIUS, MAX_HOUGH_RADIUS];

            int[] currentThetaStates = new int[] { int.MinValue, int.MinValue, int.MinValue };
            int[] currentRStates = new int[] { int.MinValue, int.MinValue, int.MinValue };
            double[] currentStateProbs = new double[] { 0, 0, 0 };

            double thetaInit = 1 / 180d;
            double rInit = 1 / (double)MAX_HOUGH_RADIUS;

            Pen p = new Pen(Color.DarkRed, 3);
            Pen p2 = new Pen(Color.DarkBlue, 3);

            bool threadRunning = false;

            //int[] widthRes = new int[] { 0, 320 }; 512 288
            //int[] heightRes = new int[] { 80, 240 }; 
            int[] widthRes = new int[] { 0, 32, 96, 224 };
            int[] heightRes = new int[] { 180, 200, 240, 288};
            //int[] widthRes = new int[] { 0, 16, 48, 112 };
            //int[] heightRes = new int[] { 96, 112, 128, 144 };

            LaneFilter lf = new LaneFilter(144, 144, 172);

            public LaneDetectorProcessor(MsgService msgService)
                : base(msgService, VisionMessage.msgType)
            {
                for (int i = -10; i < 190; i++)
                {
                    for (int j = -10; j < 190; j++)
                    {
                        theta_tx[(i + 180) % 180, (j + 180) % 180] = Statistics.NormalDistribution(j, i, 1, false);
                        theta_ox[(i + 180) % 180, (j + 180) % 180] = Statistics.NormalDistribution(j, i, 2, false);
                    }
                }

                for (int i = 0; i < MAX_HOUGH_RADIUS; i++)
                {
                    for (int j = 0; j < MAX_HOUGH_RADIUS; j++)
                    {
                        r_tx[i, j] = Statistics.NormalDistribution(j, i, 1, false);
                        r_ox[i, j] = Statistics.NormalDistribution(j, i, 2, false);
                    }
                }


            }

            private int getMax(double[,] array, int dim, int i)
            {
                int len = array.GetLength(dim);
                double jMax = double.MinValue;
                int jIndex = 0;
                for (int j = 0; j < len; j++)
                {
                    double val;
                    if (dim == 0)
                        val = array[j, i];
                    else
                        val = array[i, j];
                    if (jMax < val)
                    {
                        jIndex = j;
                        jMax = val;
                    }
                }
                return jIndex;
            }

            private double getProductMax(double[,] tx, double[,] ox, int currentState, int observation, ref int candidateState)
            {
                double maxVal = tx[currentState, currentState] * ox[currentState, observation];
                candidateState = currentState;
                int stateLen = tx.GetLength(1);
                for (int i = 0; i < stateLen; i++)
                {
                    double candidateVal = tx[currentState, i] * ox[i, observation];
                    if (candidateVal > maxVal)
                    {
                        maxVal = candidateVal;
                        candidateState = i;
                    }
                }
                return maxVal;
            }

            private HoughLine processSubImage(Bitmap subImg)
            {
                HoughLineTransformation hlt = new HoughLineTransformation();
                hlt.ProcessImage(subImg);
                HoughLine[] hlsin = hlt.GetMostIntensiveLines(1);
                if (hlsin == null || hlsin.Length == 0 || hlsin[0].Intensity < 0.000001)
                    return null;
                return hlsin[0];
            }


            private void drawLine(Graphics g, HoughLine hl, Rectangle sub)
            {
                double t = hl.Theta;
                short r = (short)(hl.Radius);

                int xOffset = sub.X + sub.Width / 2;
                int yOffset = sub.Y + sub.Height / 2;

                if (r < 0)
                {
                    r = Math.Abs(r);
                    t = t - 180;
                }

                float cos = (float)Math.Cos(Math.PI * t / 180d);
                float sin = (float)Math.Sin(Math.PI * t / 180d);
                if (Math.Abs(cos) > 0.00001 && Math.Abs(sin) > 0.00001)
                {
                    double cot = cos / sin;

                    int y1 = (int)(cot * sub.X + yOffset - cot * xOffset - r / sin);
                    int y2 = (int)(cot * (sub.X + sub.Width) + yOffset - cot * xOffset - r / sin);
                    int x1 = (int)((sub.Y - yOffset + r / sin + cot * xOffset) / cot);
                    int x2 = (int)((sub.Y + sub.Height - yOffset + r / sin + cot * xOffset) / cot);

                    Point p1 = Point.Empty;
                    Point p2 = Point.Empty;

                    if (Math.Abs(y1 - yOffset) <= sub.Height / 2)
                        p1 = new Point(sub.X, y1);
                    if (Math.Abs(y2 - yOffset) <= sub.Height / 2)
                        if (p1 == Point.Empty)
                            p1 = new Point(sub.X + sub.Width, y2);
                        else
                            p2 = new Point(sub.X + sub.Width, y2);
                    if (Math.Abs(x1 - xOffset) <= sub.Width / 2)
                        if (p1 == Point.Empty)
                            p1 = new Point(x1, sub.Y);
                        else
                            p2 = new Point(x1, sub.Y);
                    if (Math.Abs(x2 - xOffset) <= sub.Width / 2)
                        if (p1 == Point.Empty)
                            p1 = new Point(x2, sub.Y + sub.Height);
                        else
                            p2 = new Point(x2, sub.Y + sub.Height);

                    g.DrawLine(p, p1, p2);
                }
            }

            private HoughLine moveHL(HoughLine hl, float oldX, float oldY, float newX, float newY)
            {
                double t = hl.Theta;
                short r = hl.Radius;
                float cos = (float)Math.Cos(Math.PI * t / 180d);
                float sin = (float)Math.Sin(Math.PI * t / 180d);
                r = (short)(r + (oldX - newX) * cos + (newY - oldY) * sin);

                return new HoughLine(t, r, hl.Intensity, hl.RelativeIntensity);
            }
            Rectangle[] rect_orig = new Rectangle[100];
            int hlidx = 0;

            public void findLines(object o)
            {
                DateTime tick = DateTime.Now;

                Bitmap bitmap = (Bitmap)o;

                for (int i = 0; i < hls.Length; i++)
                    hls[i] = null;
                hlidx = 0;

                for (int i = 0; i < heightRes.Length - 1; i++)
                {
                    int y = heightRes[i];
                    int yDelta = heightRes[i + 1] - heightRes[i];
                    int xDelta = widthRes[i + 1] - widthRes[i];

                    for (int x = 0; x < bitmap.Width; x += xDelta)
                    {
                        Rectangle sub = new Rectangle(x, y, xDelta, yDelta);
                        Bitmap subImg = (Bitmap)bitmap.Clone(sub, bitmap.PixelFormat);
                        HoughLine hl = processSubImage(subImg);
                        if (hl != null)
                        {
                            //hls[hlidx] = hl; return;
                            rect_orig[hlidx] = sub;
                            float xOffset = subImg.Width / 2f + x;
                            float yOffset = subImg.Height / 2f + y;
                            hl = moveHL(hl, xOffset, yOffset, bitmap.Width / 2f, bitmap.Height);
                            hls[hlidx++] = hl;
                            //Console.WriteLine(hl.Theta + " --- " + hl.Radius);
                        }
                    }
                }
                short[] bestThreeRadius = new short[] { 0, 0, 0 };
                double[] bestThreeIntensity = new double[] { 0, 0, 0 };
                double[] bestThreeTheta = new double[] { double.MinValue, double.MinValue, double.MinValue };
                foreach (HoughLine hl in hls)
                {
                    if (hl == null)
                        continue;


                    short rNew = (short)(hl.Radius);
                    double tNew = hl.Theta;
                    double sNew = 1; //hl.RelativeIntensity;

                    bool isNew = true;
                    for (int i = 0; i < 3; i++)
                    {
                        short r = bestThreeRadius[i];
                        double t = bestThreeTheta[i];
                        double s = bestThreeIntensity[i];

                        if (t != double.MinValue && (Math.Abs(t - tNew) < 30 || 180 - Math.Abs(t - tNew) < 30) && Math.Abs(r - rNew) < 30)
                        {
                            bestThreeRadius[i] = (short)(((r * s) + (rNew * sNew)) / (s + sNew));
                            bestThreeTheta[i] = (short)(((t * s) + (tNew * sNew)) / (s + sNew));
                            bestThreeIntensity[i] += sNew;
                            isNew = false;
                            break;
                        }
                    }

                    if (isNew)
                    {
                        double minS = double.MaxValue;
                        int minSidx = 0;
                        for (int i = 0; i < 3; i++)
                        {
                            if (bestThreeIntensity[i] < minS)
                            {
                                minS = bestThreeIntensity[i];
                                minSidx = i;
                            }
                        }
                        if (sNew > bestThreeIntensity[minSidx])
                        {
                            bestThreeRadius[minSidx] = rNew;
                            bestThreeIntensity[minSidx] = sNew;
                            bestThreeTheta[minSidx] = tNew;
                        }
                    }
                }
                //for (int i = 0; i < 3; i++)
                //{
                //    if (bestThreeTheta[i] == double.MinValue)
                //        continue;
                //    if (Math.Abs(bestThreeTheta[i] % 180 - 90) < 45)
                //        continue;
                //    if (bestThreeIntensity[i] < 2)
                //        continue; 
                //    drawLine(g, new HoughLine(bestThreeTheta[i], (short)bestThreeRadius[i], (short)bestThreeIntensity[i], 0d), new Rectangle(0, 0, bitmap.Width, bitmap.Height));
                //}
                for (int i = 0; i < 3; i++)
                {
                    if (bestThreeTheta[i] == double.MinValue || bestThreeRadius[i] < 0 || bestThreeIntensity[i] < 2)
                        continue;
                    int observed_t = ((int)bestThreeTheta[i] + 180) % 180;
                    int observed_r = (int)bestThreeRadius[i];
                    int max_t = getMax(theta_ox, 0, observed_t);
                    int max_r = getMax(r_ox, 0, observed_r);
                    double newLineProb = thetaInit * theta_ox[max_t, observed_t] *
                                            rInit * r_ox[max_r, observed_r];
                    bool isNewLine = true;
                    for (int j = 0; j < 3; j++)
                    {
                        if (currentThetaStates[j] == int.MinValue)
                        {
                            currentStateProbs[j] = double.MinValue;
                            continue;
                        }
                        int newThetaState = -1;
                        int newRState = -1;
                        double maxValTheta = getProductMax(theta_tx, theta_ox, currentThetaStates[j], observed_t, ref newThetaState);
                        double maxValR = getProductMax(r_tx, r_ox, currentRStates[j], observed_r, ref newRState);
                        if (maxValR * maxValTheta > newLineProb)
                        {
                            currentRStates[j] = newRState;
                            currentThetaStates[j] = newThetaState;
                            currentStateProbs[j] = maxValR * maxValTheta;
                            isNewLine = false;
                            break;
                        }
                    }
                    if (isNewLine)
                    {
                        double minVal = double.MaxValue;
                        int idxMin = -1;
                        for (int j = 0; j < 3; j++)
                        {
                            if (currentStateProbs[j] < minVal)
                            {
                                idxMin = j;
                                minVal = currentStateProbs[j];
                            }
                        }
                        currentThetaStates[idxMin] = observed_t;
                        currentRStates[idxMin] = observed_r;
                        currentStateProbs[idxMin] = newLineProb;
                    }
                }
                for (int j = 0; j < 3; j++)
                {
                    if (currentStateProbs[j] <= 0)
                    {
                        currentStateProbs[j] = double.MinValue;
                        continue;
                    }
                    currentStateProbs[j] /= 2d;
                    int max_t = getMax(theta_ox, 0, currentThetaStates[j]);
                    int max_r = getMax(r_ox, 0, currentRStates[j]);
                    double newLineProb = thetaInit * theta_ox[max_t, currentThetaStates[j]] *
                                            rInit * r_ox[max_r, currentRStates[j]];
                    if (currentStateProbs[j] < newLineProb)
                    {
                        currentStateProbs[j] = double.MinValue;
                    }
                }
                threadRunning = false;
                DateTime tack = DateTime.Now;
                //Console.WriteLine(tack.Subtract(tick).Milliseconds);
            }

            public override void consumeMessage(IMessage message, int msgID)
            {
                VisionMessage vm = message as VisionMessage;
                if (vm.bypass)
                {
                    vm.bypass = false;
                    msgService.sendMsg(vm);
                    return;
                }

                // Bitmap bitmap = ByteTools.BytesToBmp(vm.Bitmap, vm.BmpWidth, vm.BmpHeight, ByteTools.bppToPixelFormat(vm.BytePerPixel));
                Bitmap bitmap = vm.Bitmap;
                Bitmap rgb = bitmap;

                bitmap = lf.Apply(bitmap);
                rgb = new GrayscaleToRGB().Apply(bitmap);

                Graphics g = Graphics.FromImage(rgb);
                if (g == null)
                {
                    Console.WriteLine("Null Graphics!");
                    return;
                }

                lock (this)
                {
                    if (!threadRunning)
                    {
                        threadRunning = true;
                        /*for (int i = 0; i < 3; i++)
                        {
                            if (g != null && currentStateProbs[i] > double.MinValue)
                            {
                                drawLine(g, new HoughLine((double)currentThetaStates[i], (short)currentRStates[i], 0, 0d),
                                   //new Rectangle(0, heightRes[0], bitmap.Width, heightRes[heightRes.Length - 1]-heightRes[0])
                                   new Rectangle(0,0,bitmap.Width, bitmap.Height*2));
                            }
                        }
                        */
                        for (int i = 0; i < hlidx; i++)
                        {
                            drawLine(g, hls[i], new Rectangle(0, 0, bitmap.Width, bitmap.Height * 2));
                        }
                        
                        
                        for (int i = 0; i < heightRes.Length - 1; i++)
                        {
                            int y = heightRes[i];
                            int yDelta = heightRes[i + 1] - heightRes[i];
                            int xDelta = widthRes[i + 1] - widthRes[i];

                            for (int x = 0; x < bitmap.Width; x += xDelta)
                            {
                                Rectangle sub = new Rectangle(x, y, xDelta, yDelta);
                                Bitmap subImg = (Bitmap)bitmap.Clone(sub, bitmap.PixelFormat);
                                HoughLine hl = processSubImage(subImg);
                                g.DrawRectangle(p2, sub);
                            }
                        }
                        ThreadStart starter = delegate { findLines(bitmap); };
                        Thread innerThread = new Thread(starter);
                        innerThread.Start();
                    }
                }
                vm = new VisionMessage(rgb.Height, rgb.Width, ByteTools.pixelFormatToBPP(rgb.PixelFormat), rgb);
                msgService.sendMsg(vm);
            }

            public override string ToString()
            {
                return "LaneDetectorProcessor";
            }

        }

        /// <summary>
        /// Genetic algorithm processing class.
        /// </summary>
        public class GeoTransChromosome : ChromosomeBase, IFitnessFunction
        {
            public static int NUM_OF_POINTS = 18;
            private static Random rg = new Random(System.DateTime.Now.Millisecond);
            private static UniformGenerator xug = null;
            private static UniformGenerator yug = null;
            private static UniformGenerator wug = null;

            public Bitmap bmp = null;
            public SignFilter sf = null;
            private float[,] redTestPoints = new float[NUM_OF_POINTS, 2];
            private float[,] nonRedTestPoints = new float[NUM_OF_POINTS, 2];

            TriangularSignDetectorProcessor processor_triangle = null;
            CircularSignDetectorProcessor processor_circular = null;

            private int x;
            public int X
            {
                get { return x; }
                set { x = value; }
            }

            private int y;
            public int Y
            {
                get { return y; }
                set { y = value; }
            }

            private int width;
            public int Width
            {
                get { return width; }
                set { width = value; }
            }

            private void init()
            {
                init(false);
            }

            private void init(bool force)
            {
                if (xug == null || force)
                {
                    xug = new UniformGenerator(new DoubleRange(Constants.XUG_START, Constants.XUG_END), rg.Next());
                    yug = new UniformGenerator(new DoubleRange(Constants.YUG_START, Constants.YUG_END), rg.Next());
                    wug = new UniformGenerator(new DoubleRange(Constants.WUG_START, Constants.WUG_END), rg.Next());    // radius            
                }
            }

            public GeoTransChromosome(Bitmap bmp, SignFilter sf, TriangularSignDetectorProcessor processor_triangle, CircularSignDetectorProcessor processor_circular)
            {
                init();
                this.bmp = bmp;
                this.sf = sf;
                this.processor_triangle = processor_triangle;
                this.processor_circular = processor_circular;
                Generate();
            }

            public GeoTransChromosome(Bitmap bmp, SignFilter sf, TriangularSignDetectorProcessor processor_triangle, CircularSignDetectorProcessor processor_circular, int x, int y, int width)
            {
                init();
                this.bmp = bmp;
                this.sf = sf;
                this.processor_triangle = processor_triangle;
                this.processor_circular = processor_circular;
                X = x;
                Y = y;
                Width = width;
            }

            /*
            public GeoTransChromosome(Bitmap bmp, SignFilter sf, float[,] redTestPoints, float[,] nonRedTestPoints)
            {
                init();
                this.bmp = bmp;
                this.sf = sf;
                this.redTestPoints = redTestPoints;
                this.nonRedTestPoints = nonRedTestPoints;
                Generate();
            }


            public GeoTransChromosome(Bitmap bmp, SignFilter sf, float[,] redTestPoints, float[,] nonRedTestPoints, int x, int y, int width)
            {
                init();
                this.bmp = bmp;
                this.sf = sf;
                this.redTestPoints = redTestPoints;
                this.nonRedTestPoints = nonRedTestPoints;
                X = x;
                Y = y;
                Width = width;
            }
            */


            public override IChromosome Clone()
            {
                // return new GeoTransChromosome(bmp, sf, redTestPoints, nonRedTestPoints, X, Y, Width);
                return new GeoTransChromosome(bmp, sf, processor_triangle, processor_circular, X, Y, Width);
            }

            public override IChromosome CreateNew()
            {
                // return new GeoTransChromosome(bmp, sf, redTestPoints, nonRedTestPoints);
                return new GeoTransChromosome(bmp, sf, processor_triangle, processor_circular);
            }

            public override void Crossover(IChromosome pair)
            {
                GeoTransChromosome sg = (GeoTransChromosome)pair;
                this.shift(sg);
                sg.shift(this);
            }

            public override void Generate()
            {
                int i = 0;
                while (i < (Constants.IMAGE_WIDTH / 16))
                {
                    X = (int)xug.Next();
                    i++;
                    if (sf.xProj[x] > 2)
                        break;
                }
                i = 0;
                int y_best = -1, y_best_score = -1;
                while (i < (Constants.IMAGE_HEIGHT / 16))
                {
                    Y = (int)yug.Next();
                    i++;

                    if (sf.yProj[y] > y_best_score)
                    {
                        y_best_score = sf.yProj[y];
                        y_best = Y;
                    }

                    Color clr = bmp.GetPixel(X, Y);
                    if (sf.yProj[y] > 2 && clr.R == 0)
                    {
                        break;
                    }
                    else if (i == (Constants.IMAGE_HEIGHT / 16) - 1)
                    {
                        Y = y_best;
                    }
                }
                if (Y == 0 || X == 0)
                {
                    init(true);
                }
                Width = (int)wug.Next();
            }

            public override void Mutate()
            {
                // shift(new GeoTransChromosome(bmp, sf, redTestPoints, nonRedTestPoints));
                shift(new GeoTransChromosome(bmp, sf, processor_triangle, processor_circular));
            }

            public void shift(GeoTransChromosome sg)
            {
                X = (sg.X + 5 * X) / 6;
                Y = (sg.Y + 5 * Y) / 6;
                Width = (sg.Width + 5 * Width) / 6;
            }

            public void copyContent(GeoTransChromosome gtc)
            {
                X = gtc.X;
                Y = gtc.Y;
                Width = gtc.Width;
            }


            public double Evaluate(IChromosome chromosome)
            {
                GeoTransChromosome gtc = (GeoTransChromosome)chromosome;

                Color clr = gtc.bmp.GetPixel(X, Y);
                if (clr.R == 255)
                    return 0;
                int rx = 0, ry = 0, rr = 0;
                float rs = 0;
                if (processor_triangle != null)
                    processor_triangle.findTriangle(gtc.x, gtc.y, gtc.bmp, ref rx, ref ry, ref rr, ref rs);
                else if (processor_circular != null)
                    processor_circular.findCircle(gtc.x, gtc.y, gtc.bmp, ref rx, ref ry, ref rr, ref rs);
                if (rx > 0)
                {
                    gtc.x = rx;
                    gtc.y = ry;
                }
                gtc.width = rr;
                return rs;
                /*
                long start = DateTime.Now.Ticks / 10000;
                Globals.FRAMES_PROCESSED_GA_EVALUATE++;

                //Bitmap xxx = new Bitmap(Constants.IMAGE_WIDTH, Constants.IMAGE_HEIGHT, PixelFormat.Format24bppRgb);
                //Graphics g = Graphics.FromImage(xxx);
                //g.DrawImage(bmp, 0, 0);

                double totalScore = 0;
                int nonred = 0;
                lock (bmp)
                {
                    int bmpWidth = bmp.Width;
                    int bmpHeight = bmp.Height;
                    for (int i = 0; i < NUM_OF_POINTS; i++)
                    {
                        // GeoTransChromosome gtc = (GeoTransChromosome)chromosome;
                        int[] pointRed = gtc.applyTransformation(redTestPoints[i, 0], redTestPoints[i, 1]);
                        int[] pointNonRed = gtc.applyTransformation(nonRedTestPoints[i, 0], nonRedTestPoints[i, 1]);

                        //g.DrawRectangle(Pens.Red, new Rectangle(new Point(pointRed[0], pointRed[1]), new Size(2, 2)));
                        //g.DrawRectangle(Pens.Yellow, new Rectangle(new Point(pointNonRed[0], pointNonRed[1]), new Size(2, 2)));

                        if (pointRed[0] < 0 || pointRed[1] < 0 ||
                            pointRed[0] >= bmpWidth || pointRed[1] >= bmpHeight ||
                            pointNonRed[0] < 0 || pointNonRed[1] < 0 ||
                            pointNonRed[0] >= bmpWidth || pointNonRed[1] >= bmpHeight)
                        {
                            //totalScore -= 1;
                        }
                        else
                        {
                            if (bmp.GetPixel(pointRed[0], pointRed[1]).R > 128)
                            {
                                totalScore += 1;
                            }
                            if (bmp.GetPixel(pointNonRed[0], pointNonRed[1]).R < 128)
                            {
                                nonred += 1;
                            }
                        }
                    }
                }

                //xxx.Save(Constants.base_folder + "hasat\\zzz\\resim_" +Globals.HARVEST_SIGN_ID+ "_" +X+ "_" +Y+ "_" +Width+ ".bmp");

                if (Constants.EVALUATE_TIME_ENABLED)
                {
                    int x = (int)(DateTime.Now.Ticks / 10000 - start);
                    if (x >= 0)
                    {
                        Globals.TIME_GA_EVALUATE_MIN = x < Globals.TIME_GA_EVALUATE_MIN ? x : Globals.TIME_GA_EVALUATE_MIN;
                        Globals.TIME_GA_EVALUATE_MAX = x > Globals.TIME_GA_EVALUATE_MAX ? x : Globals.TIME_GA_EVALUATE_MAX;
                        Globals.TIME_GA_EVALUATE_TOTAL += x;
                    }
                }

                if (nonred < 2 * NUM_OF_POINTS / 3)
                    return -1;
                else if (totalScore > 2 * NUM_OF_POINTS / 3)
                    return totalScore * 2;
                else
                    return totalScore;
                 * */
            }


            private int[] applyTransformation(double x, double y)
            {
                double a = Width;
                double c = X;
                double e = Width;
                double f = Y;

                double d = 0;
                double b = 0;
                double g = 0;
                double h = 0;

                return new int[] { (int)((a * x + b * y + c) / (g * x + h * y + 1)), (int)((d * x + e * y + f) / (g * x + h * y + 1)) };
            }

            public void draw(Graphics g)
            {
                lock (bmp)
                {
                    Pen redpen = new Pen(Color.Red, 5);
                    for (int i = 0; i < NUM_OF_POINTS; i++)
                    {
                        int[] point = applyTransformation(redTestPoints[i, 0], redTestPoints[i, 1]);
                        if (point[0] < 0 || point[1] < 0 ||
                            point[0] > bmp.Width || point[1] > bmp.Height)
                        {
                            continue;
                        }
                        g.DrawEllipse(redpen, (float)point[0], (float)point[1], 1f, 1f);
                    }
                    //Console.WriteLine(fitness);
                }
            }

        }

        /// <summary>
        /// Processor for detecting circular signs.
        /// </summary>
        public class CircularSignDetectorProcessor : MsgConsumer
        {
            private bool threadRunning = false;
            static SignFilter sf = new SignFilter(Constants.SIGNFILTER_GREEN_START_COEFF, Constants.SIGNFILTER_BLUE_START_COEFF, 2);
            Bitmap gtcBmp;

            private static Population population = null;
            private IPanelDisplay panel = null;

            private static float inner_circle_scale = 0.6f;

            /*
            private static float[,] redTestPoints = null;
            private static float[,] nonRedTestPoints = null;

            void calculateRedTestPoints()
            {
                redTestPoints = new float[GeoTransChromosome.NUM_OF_POINTS, 2];
                nonRedTestPoints = new float[GeoTransChromosome.NUM_OF_POINTS, 2];
                for (int t = 0; t < 360; t += (360 / GeoTransChromosome.NUM_OF_POINTS))
                {
                    int p = t / (360 / GeoTransChromosome.NUM_OF_POINTS);
                    redTestPoints[p, 0] = (float)Math.Cos(t * Math.PI / 180);
                    redTestPoints[p, 1] = (float)Math.Sin(t * Math.PI / 180);
                    nonRedTestPoints[p, 0] = inner_circle_scale * redTestPoints[p, 0];
                    nonRedTestPoints[p, 1] = inner_circle_scale * redTestPoints[p, 1];
                }
            }
            */
            static int RS_THRESHOLD = 30;
            void searchSolution(Bitmap rgb)
            {
                long start = DateTime.Now.Ticks / 10000;
                Globals.FRAMES_PROCESSED_CIRCULAR++;
                int com_x_sum = 0, com_y_sum = 0, com_x_y_point_count = 0;
                Globals.HARVEST_SIGN_ID++;
                Bitmap bmp = sf.Apply(rgb);

                //if (redTestPoints == null)
                //    calculateRedTestPoints();

                // GeoTransChromosome sampleChromosome = new GeoTransChromosome(bmp, sf, redTestPoints, nonRedTestPoints);
                GeoTransChromosome sampleChromosome = new GeoTransChromosome(bmp, sf, null, this);

                if (population == null || population.BestChromosome == null || population.BestChromosome.Fitness < RS_THRESHOLD - 10)
                {   // fresh population
                    population = new Population(Constants.GA_POPULATION_SIZE,
                        sampleChromosome,
                        sampleChromosome,
                        new EliteSelection()
                        );
                    population.MutationRate = Constants.GA_MUTATION_RATE;
                    population.CrossoverRate = Constants.GA_CROSSOVER_RATE;
                }
                else
                {
                    // half from previous
                    Population tmpPopulation = new Population(Constants.GA_POPULATION_SIZE,
                        sampleChromosome,
                        sampleChromosome,
                        new EliteSelection()
                        );
                    tmpPopulation.MutationRate = Constants.GA_MUTATION_RATE;
                    tmpPopulation.CrossoverRate = Constants.GA_CROSSOVER_RATE;
                    for (int j = 0; j < tmpPopulation.Size / 2; j++)
                    {
                        ((GeoTransChromosome)tmpPopulation[j]).copyContent((GeoTransChromosome)population.BestChromosome);
                    }
                    population = tmpPopulation;
                }

                int i = 0;
                do
                {
                    long start_epoch = DateTime.Now.Ticks / 10000;
                    Globals.FRAMES_PROCESSED_GA_RUNEPOCH++;

                    // run one epoch of genetic algorithm
                    population.RunEpoch();
                    i++;

                    if (Constants.EVALUATE_TIME_ENABLED)
                    {
                        int x = (int)(DateTime.Now.Ticks / 10000 - start_epoch);
                        if (x >= 0)
                        {
                            Globals.TIME_GA_RUNEPOCH_MIN = x < Globals.TIME_GA_RUNEPOCH_MIN ? x : Globals.TIME_GA_RUNEPOCH_MIN;
                            Globals.TIME_GA_RUNEPOCH_MAX = x > Globals.TIME_GA_RUNEPOCH_MAX ? x : Globals.TIME_GA_RUNEPOCH_MAX;
                            Globals.TIME_GA_RUNEPOCH_TOTAL += x;
                        }
                    }
                } while (i < Constants.GA_NUMBER_ITERATIONS);


                GeoTransChromosome bestChromo = null;
                if (population.BestChromosome != null && population.BestChromosome.Fitness > RS_THRESHOLD)
                {
                    bestChromo = (GeoTransChromosome)population.BestChromosome;
                    // int rx = 0, ry = 0, rr = 0;
                    // float rs = 0f;

                    int rx = bestChromo.X, ry = bestChromo.Y, rr = bestChromo.Width;
                    float rs = (float)bestChromo.Fitness;

                    // findCircle(bestChromo.X, bestChromo.Y, bmp, ref rx, ref ry, ref rr, ref rs);
                    if (rs < RS_THRESHOLD || rr==0)
                    {
                        bestChromo = null;
                    }
                    else
                    {
                        rr = (int)(Constants.CAPTURE_RESIZE_CIRCLE * rr);

                        // rr = rr - 2;
                        // Graphics g = Graphics.FromImage(rgb);
                        // g.DrawEllipse(Pens.Cyan, rx - rr, ry - rr, rr * 2, rr * 2);

                        if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestAll || Constants.HARVEST_TYPE == Constants.HarvestType.harvestMisses)
                        {
                            rgb.Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_0.bmp");
                            bmp.Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_1.bmp");
                        }


                        rgb = new Crop(new Rectangle(rx - rr,
                                                        ry - rr,
                                                        (rx + rr > rgb.Width - 1 ? rgb.Width - 1 - rx : 2 * rr + 2),
                                                        (ry + rr > rgb.Height - 1 ? rgb.Height - 1 - ry : 2 * rr + 2))).Apply(rgb);
                        rgb = new ResizeBicubic(Constants.SIGN_WIDTH, Constants.SIGN_HEIGHT).Apply(rgb);
                        if (panel != null)
                        {
                            Graphics g = panel.getPanelGraphics();
                            g.DrawRectangle(Pens.Silver, 5, 4, Constants.SIGN_WIDTH, Constants.SIGN_HEIGHT);
                            g.DrawImage(rgb, new Point(5, 4));
                        }

                        if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestAll || Constants.HARVEST_TYPE == Constants.HarvestType.harvestMisses)
                            rgb.Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_3_" +rs+ ".bmp");

                        rgb = AutoBrightnessProcessor.autoBrightness(rgb, Rectangle.Empty, Constants.AUTOBRIGHTNESS_BASE_LUM);

                        if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestAll || Constants.HARVEST_TYPE == Constants.HarvestType.harvestMisses)
                            rgb.Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_4.bmp");

                        if (Constants.LABELING_TYPE == Constants.LabelingType.redBlackWhite)
                            rgb = new ColorLabelFilter(new Color[] { Color.FromArgb(255, 0, 0), Color.White, Color.Black }, true).Apply(rgb);
                        else if (Constants.LABELING_TYPE == Constants.LabelingType.blackAndWhite)
                        {
                            int dynamic_gray = 0;
                            rgb = new ColorLabelFilter(new Color[] { Color.White, Color.FromArgb(dynamic_gray, dynamic_gray, dynamic_gray) }, true).Apply(rgb);
                        }


                        /* int start_x = 0, start_y = 0;
                        int y = 0;
                        for (; y < Constants.SIGN_HEIGHT && (start_x + start_y) == 0; y++)
                        {
                            for (int x = 0; x < Constants.SIGN_WIDTH; x++)
                            {
                                Color clr = rgb.GetPixel(x, y);
                                if (clr.R == 0)
                                {
                                    start_x = x;
                                    start_y = y;
                                    break;
                                }
                            }
                        }
                        PointedColorFloodFill filter = new PointedColorFloodFill();
                        filter.Tolerance = Color.FromArgb(0, 0, 0);
                        filter.FillColor = Color.FromArgb(255, 255, 255);
                        filter.StartingPoint = new Point(start_x, start_y);
                        Bitmap rgb_flood_fill = filter.Apply(rgb);

                        bool black_region_found_at_center = false;
                        y = 4 * (Constants.SIGN_HEIGHT / 10);
                        for (int x = 4 * (Constants.SIGN_WIDTH / 10); x < 6 * (Constants.SIGN_WIDTH / 10); x += 2)
                        {
                            Color clr = rgb_flood_fill.GetPixel(x, y);
                            if (clr.R == 0)
                            {
                                black_region_found_at_center = true;
                            }
                        }
                        if (!black_region_found_at_center)
                        {
                            y = 6 * (Constants.SIGN_HEIGHT / 10);
                            for (int x = 4 * (Constants.SIGN_WIDTH / 10); x < 6 * (Constants.SIGN_WIDTH / 10); x += 2)
                            {
                                Color clr = rgb_flood_fill.GetPixel(x, y);
                                if (clr.R == 0)
                                {
                                    black_region_found_at_center = true;
                                    break;
                                }
                            }
                        }
                        if (black_region_found_at_center)
                        {
                            rgb = rgb_flood_fill;
                        }
                        */

                        BitmapData image_data = rgb.LockBits(new Rectangle(0, 0, rgb.Width, rgb.Height), ImageLockMode.ReadWrite, rgb.PixelFormat);
                        int bpp = 3;
                        int nOffset = image_data.Stride - rgb.Width * bpp;
                        System.IntPtr Scan0 = image_data.Scan0;
                        unsafe
                        {
                            byte* p = (byte*)Scan0;
                            for (int y = 0; y < Constants.SIGN_HEIGHT; y++)
                            {
                                // for each pixel
                                for (int x = 0; x < Constants.SIGN_WIDTH; x++, p += bpp)
                                {
                                    if (y >= Constants.SIGN_HEIGHT - 10 || y <= 10 || x >= Constants.SIGN_WIDTH - 10 || x <= 10 ||
                                        Math.Sqrt( (y - Constants.SIGN_HEIGHT / 2) * (y - Constants.SIGN_HEIGHT / 2) + (x - Constants.SIGN_WIDTH / 2) * (x - Constants.SIGN_WIDTH / 2)) > 24
                                        )
                                    {
                                        p[RGB.R] = 255;
                                        p[RGB.G] = 255;
                                        p[RGB.B] = 255;
                                    }
                                    else if (p[RGB.R] == 0)
                                    {
                                        com_x_sum += x;
                                        com_y_sum += y;
                                        com_x_y_point_count++;
                                    }
                                }
                                p += nOffset;
                            }

                        }
                        rgb.UnlockBits(image_data);

                        // Graphics g = Graphics.FromImage(rgb);
                        // g.DrawEllipse(Pens.Green, 32 - 24, 32 - 24, 24 * 2, 24 * 2);

                        if (com_x_y_point_count < 10)
                            bestChromo = null;
                        else if (Constants.HARVEST_TYPE > Constants.HarvestType.noHarvest)
                        {
                            if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestIntoFolder)
                            {
                                string[] signs = Globals.SIGN_IN_FRAME.Split(',');

                                for (int j = 1; j < signs.Length; j++)
                                {
                                    if (!Directory.Exists(Constants.base_folder + "hasat\\" + signs[j]))
                                        Directory.CreateDirectory(Constants.base_folder + "hasat\\" + signs[j]);
                                    lock (Globals.HARVEST_LOCK)
                                    {
                                        rgb.Save(Constants.base_folder + "hasat\\" + signs[j] + "\\" + Globals.HARVEST_SIGN_ID + ".bmp");
                                    }
                                }
                            }
                            else
                            {
                                rgb.Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_6.bmp");
                            }
                        }

                        gtcBmp = rgb;
                    }
                }

                if (Constants.EVALUATE_TIME_ENABLED)
                {
                    int x = (int)(DateTime.Now.Ticks / 10000 - start);
                    if (x >= 0)
                    {
                        Globals.TIME_CIRCULAR_MIN = x < Globals.TIME_CIRCULAR_MIN ? x : Globals.TIME_CIRCULAR_MIN;
                        Globals.TIME_CIRCULAR_MAX = x > Globals.TIME_CIRCULAR_MAX ? x : Globals.TIME_CIRCULAR_MAX;
                        Globals.TIME_CIRCULAR_TOTAL += x;
                    }
                }

                if (bestChromo != null)
                {
                    if (panel != null)
                    {
                        Graphics g = panel.getPanelGraphics();
                        g.DrawRectangle(Pens.Silver, 5, 74, Constants.SIGN_WIDTH, Constants.SIGN_HEIGHT);
                        g.DrawImage(gtcBmp, new Point(5, 74));
                    }
                    if (msgService != null)
                    {
                        VisionMessage vm = new VisionMessage(gtcBmp.Height, gtcBmp.Width, ByteTools.pixelFormatToBPP(gtcBmp.PixelFormat), gtcBmp);

                        // Center of Mass
                        int com_x = com_x_sum / (com_x_y_point_count == 0 ? 1 : com_x_y_point_count);
                        int com_y = com_y_sum / (com_x_y_point_count == 0 ? 1 : com_x_y_point_count);
                        vm.CoM_X = com_x;
                        vm.CoM_Y = com_y;
                        msgService.sendMsg(vm);
                    }
                }
                else
                {
                    if (Globals.RIGHT_PANEL_SHOWING_STH)
                    {
                        Globals.RIGHT_PANEL_SHOWING_STH = false;
                        // Bitmap img = new Bitmap(160, Constants.IMAGE_HEIGHT, PixelFormat.Format24bppRgb);
                        Graphics g = panel.getPanelGraphics();
                        g.Clear(Color.Silver);

                        /*if (msgService != null)
                        {
                            VisionMessage vm = new VisionMessage(img.Height, img.Width, ByteTools.pixelFormatToBPP(img.PixelFormat), img);
                            vm.bypass = true;
                            msgService.sendMsg(vm);
                        }*/
                    }
                }

                threadRunning = false;
            }

            public CircularSignDetectorProcessor(MsgService msgService, IPanelDisplay panel)
                : base(msgService, VisionMessage.msgType)
            {
                this.panel = panel;
            }

            public override void consumeMessage(IMessage message, int msgID)
            {
                VisionMessage vm = message as VisionMessage;

                lock (this)
                {
                    if (!threadRunning)
                    {
                        threadRunning = true;
                        ThreadStart starter = delegate { searchSolution(vm.Bitmap); };
                        Thread innerThread = new Thread(starter);
                        innerThread.Start();
                    }
                }
            }



            /****************************/
            private unsafe void goLeft(ref byte* p, ref int x, int Width)
            {
                int i = 1;
                for (; i < max_uzaklik; i++)
                {
                    x -= pixel_increment;
                    if (x <= 0)
                    {
                        x = 0;
                        break;
                    }
                    p -= bpp * pixel_increment;
                    if (p[0] == 255)
                    {
                        return;
                    }
                }
                if (i >= max_uzaklik - 1)
                    x = 0;
            }
            private unsafe int skipLeft(ref byte* p, ref int x, int Width)
            {
                while (x > 0)
                {
                    x -= pixel_increment;
                    p -= bpp * pixel_increment;
                    if (p[0] != 255)
                    {
                        break;
                    }
                }
                return x;
            }

            private unsafe void goRight(ref byte* p, ref int x, int Width)
            {
                int i = 1;
                for (; i < max_uzaklik; i++)
                {
                    x += pixel_increment;
                    if (x >= Width)
                    {
                        x = 0;
                        break;
                    }
                    p += bpp * pixel_increment;
                    if (p[0] == 255)
                    {
                        return;
                    }
                }
                if (i >= max_uzaklik - 1)
                    x = 0;
            }

            private unsafe int skipRight(ref byte* p, ref int x, int Width)
            {
                while (x < Width - 1)
                {
                    x += pixel_increment;
                    p += bpp * pixel_increment;
                    if (p[0] != 255)
                    {
                        break;
                    }
                }
                return x;
            }

            private unsafe void goUp(ref byte* p, ref int y, int Height, int line_pixels)
            {
                int i = 1;
                for (; i < max_uzaklik; i++)
                {
                    y -= pixel_increment;
                    if (y <= 0)
                    {
                        y = 0;
                        break;
                    }
                    p -= line_pixels * pixel_increment;
                    if (p[0] == 255)
                    {
                        return;
                    }
                }
                if (i >= max_uzaklik - 1)
                    y = 0;
            }
            private unsafe void skipUp(ref byte* p, ref int y, int Height, int line_pixels)
            {
                while (y > 0)
                {
                    y -= pixel_increment;
                    p -= line_pixels * pixel_increment;
                    if (p[0] != 255)
                    {
                        break;
                    }
                }
            }

            private unsafe void goDown(ref byte* p, ref int y, int Height, int line_pixels)
            {
                int i = 1;
                for (; i < max_uzaklik; i++)
                {
                    y += pixel_increment;
                    if (y >= Height)
                    {
                        y = 0;
                        break;
                    }
                    p += line_pixels * pixel_increment;
                    if (p[0] == 255)
                    {
                        return;
                    }
                }
                if (i >= max_uzaklik - 1)
                    y = 0;
            }
            private unsafe void skipDown(ref byte* p, ref int y, int Height, int line_pixels)
            {
                while (y < Height - 1)
                {
                    y += pixel_increment;
                    p += line_pixels * pixel_increment;
                    if (p[0] != 255)
                    {
                        break;
                    }
                }
            }

            int max_uzaklik = 50;
            int pixel_increment = 1;
            int bpp = 1;
            Bitmap m_Bitmap;

            public void findCircle(int e_X, int e_Y, Bitmap image, ref int result_x, ref int result_y, ref int result_radius, ref float result_score)
            {
                long start = DateTime.Now.Ticks / 10000;
                Globals.FRAMES_PROCESSED_FINDCIRCLE++;

                m_Bitmap = image;

                int max_level = 2;
                int e_X_orig = e_X;
                int e_Y_orig = e_Y;
                BitmapData image_data = null;
                try
                {
                    int[] left_points = new int[max_level];
                    int[] right_points = new int[max_level];
                    ArrayList x_y_radius = new ArrayList();
                    ArrayList x_endpoints = new ArrayList();
                    ArrayList y_endpoints = new ArrayList();

                    int left_x = e_X, right_x = e_X;

                    // image_data = m_Bitmap.LockBits(new Rectangle(0, 0, m_Bitmap.Width, m_Bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
                    image_data = image.LockBits(new Rectangle(0, 0, m_Bitmap.Width, m_Bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
                    int nOffset = image_data.Stride - m_Bitmap.Width * bpp;

                    System.IntPtr Scan0 = image_data.Scan0;
                    unsafe
                    {
                        byte* p = (byte*)(void*)Scan0;
                        p += (((image.Width * bpp + nOffset) * e_Y) + (e_X * bpp));
                        byte* pOrig = p;

                        for (int level = 0; level < max_level; )
                        {
                            goLeft(ref p, ref left_x, image.Width);
                            if (left_x != 0)
                            {
                                x_endpoints.Add(new int[] { left_x, e_Y_orig });
                                left_points[level] = left_x;
                                level++;
                                if (level < max_level)
                                    skipLeft(ref p, ref left_x, image.Width);
                            }
                            else
                                break;
                        }


                        p = pOrig;

                        for (int level = 0; level < max_level; )
                        {
                            goRight(ref p, ref right_x, image.Width);
                            if (right_x != 0)
                            {
                                x_endpoints.Add(new int[] { right_x, e_Y_orig });
                                right_points[level] = right_x;
                                level++;
                                if (level < max_level)
                                    skipRight(ref p, ref right_x, image.Width);
                            }
                            else
                                break;
                        }
                    }

                    for (int left_index = 0; left_index < max_level; left_index++)
                    {
                        if (left_points[left_index] == 0)
                            break;
                        for (int right_index = 0; right_index < max_level; right_index++)
                        {
                            if (right_points[right_index] == 0)
                                break;

                            int[] up_points = new int[max_level];
                            int[] down_points = new int[max_level];

                            int center_x = (left_points[left_index] + right_points[right_index]) / 2;
                            Scan0 = image_data.Scan0;
                            unsafe
                            {
                                byte* p = (byte*)(void*)Scan0;
                                p += (((image.Width * bpp + nOffset) * e_Y_orig) + (center_x * bpp));
                                byte* pOrig = p;

                                int y_tmp1 = e_Y_orig;
                                for (int level = 0; level < max_level; )
                                {
                                    goUp(ref p, ref y_tmp1, image.Height, (image.Width * bpp + nOffset));
                                    if (y_tmp1 != 0)
                                    {
                                        up_points[level] = y_tmp1;
                                        y_endpoints.Add(new int[] { center_x, y_tmp1 });
                                        level++;
                                        if (level < max_level)
                                            skipUp(ref p, ref y_tmp1, image.Height, (image.Width * bpp + nOffset));
                                    }
                                    else
                                        break;
                                }


                                p = pOrig;

                                int y_tmp2 = e_Y_orig;
                                for (int level = 0; level < max_level; )
                                {
                                    goDown(ref p, ref y_tmp2, image.Height, (image.Width * bpp + nOffset));
                                    if (y_tmp2 != 0)
                                    {
                                        y_endpoints.Add(new int[] { center_x, y_tmp2 });
                                        down_points[level] = y_tmp2;
                                        level++;
                                        if (level < max_level)
                                            skipDown(ref p, ref y_tmp2, image.Height, (image.Width * bpp + nOffset));
                                    }
                                    else
                                        break;
                                }
                            }


                            int radius_X = (right_points[right_index] - left_points[left_index]) / 2;
                            for (int up_index = 0; up_index < max_level; up_index++)
                            {
                                if (up_points[up_index] == 0)
                                    break;
                                for (int down_index = 0; down_index < max_level; down_index++)
                                {
                                    if (down_points[down_index] == 0)
                                        break;
                                    int radius_Y = (down_points[down_index] - up_points[up_index]) / 2;

                                    // int radius = ((radius_Y + radius_X) / 2) + 2;
                                    int radius = radius_Y + 2;
                                    if (radius >= Constants.MIN_RADIUS)
                                    {
                                        x_y_radius.Add(new int[] { center_x, (up_points[up_index] + down_points[down_index]) / 2, radius, 0 });
                                    }
                                }
                            }

                        }
                    }


                    m_Bitmap.UnlockBits(image_data);

                    Size size = new Size(1, 1);
                    int best_circle_index = -1;
                    float best_circle_score = -1f;
                    for (int circle_index = 0; circle_index < x_y_radius.Count; circle_index++)
                    {
                        int[] x_y_radius_element = (int[])x_y_radius[circle_index];
                        int center_x = x_y_radius_element[0];
                        int center_y = x_y_radius_element[1];
                        int radius = x_y_radius_element[2];
                        float score = 0;
                        for (int angle = 0; angle < 90; angle += 10)
                        {
                            int xx = (int)(radius * Math.Cos(Math.PI * angle / 180));
                            int yy = (int)(radius * Math.Sin(Math.PI * angle / 180));
                            // MessageBox.Show("[" + angle + "] (xx, yy): (" +xx+ ", " +yy+ ")");
                            Point pt = new Point(center_x + xx, center_y - yy);
                            Color clr, clr2;
                            if (center_x + xx < image.Width && center_y - yy >= 0)
                            {
                                clr = ((Bitmap)image).GetPixel(center_x + xx, center_y - yy);
                                clr2 = ((Bitmap)image).GetPixel((int)(center_x + xx * inner_circle_scale), (int)(center_y - yy * inner_circle_scale));
                                if (clr.R == 255)
                                    score += 1;
                                else
                                    score -= 0.5f;
                                if (clr2.R == 255)
                                    score -= 1.0f;
                            }
                            pt = new Point(center_x - xx, center_y - yy);
                            if (center_x - xx >= 0 && center_y - yy >= 0)
                            {
                                clr = ((Bitmap)image).GetPixel(center_x - xx, center_y - yy);
                                clr2 = ((Bitmap)image).GetPixel((int)(center_x - xx * inner_circle_scale), (int)(center_y - yy * inner_circle_scale));
                                if (clr.R == 255)
                                    score += 1;
                                else
                                    score -= 0.5f;
                                if (clr2.R == 255)
                                    score -= 1.0f;
                            }
                            pt = new Point(center_x - xx, center_y + yy);
                            if (center_x - xx >= 0 && center_y + yy < image.Height)
                            {
                                clr = ((Bitmap)image).GetPixel(center_x - xx, center_y + yy);
                                clr2 = ((Bitmap)image).GetPixel((int)(center_x - xx * inner_circle_scale), (int)(center_y + yy * inner_circle_scale));
                                if (clr.R == 255)
                                    score += 1;
                                else
                                    score -= 0.5f;
                                if (clr2.R == 255)
                                    score -= 1.0f;
                            }
                            pt = new Point(center_x + xx, center_y + yy);
                            if (center_x + xx < image.Width && center_y + yy < image.Height)
                            {
                                clr = ((Bitmap)image).GetPixel(center_x + xx, center_y + yy);
                                clr2 = ((Bitmap)image).GetPixel((int)(center_x + xx * inner_circle_scale), (int)(center_y - yy * inner_circle_scale));
                                if (clr.R == 255)
                                    score += 1;
                                else
                                    score -= 0.5f;
                                if (clr2.R == 255)
                                    score -= 1.0f;
                            }
                        }
                        if (score > best_circle_score)
                        {
                            best_circle_index = circle_index;
                            best_circle_score = score;
                        }
                    }

                    if (best_circle_index != -1)
                    {
                        int[] best_circle = (int[])x_y_radius[best_circle_index];
                        result_x = best_circle[0];
                        result_y = best_circle[1];
                        result_radius = best_circle[2];
                        result_score = best_circle_score;

                        /*
                        if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestAll || Constants.HARVEST_TYPE == Constants.HarvestType.harvestMisses)
                        {
                            Bitmap img_24bpp = new Bitmap(Constants.IMAGE_WIDTH, Constants.IMAGE_HEIGHT, PixelFormat.Format24bppRgb);
                            Graphics g = Graphics.FromImage(img_24bpp);
                            g.Clear(Color.Black);
                            g.DrawImage(image, 0, 0);
                            Point pt = new Point(e_X, e_Y);
                            g.DrawRectangle(Pens.Red, new Rectangle(pt, new Size(2, 2)));

                            g.DrawRectangle(Pens.Cyan, new Rectangle(new Point(result_x, result_y), new Size(2, 2)));
                            g.DrawEllipse(Pens.Green, result_x - result_radius, result_y - result_radius, result_radius * 2, result_radius * 2);
                            g.DrawEllipse(Pens.Yellow, result_x - result_radius * inner_circle_scale, result_y - result_radius * inner_circle_scale, result_radius * 2 * inner_circle_scale, result_radius * 2 * inner_circle_scale);
                            img_24bpp.Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_findCircle_" + e_X + "_" + e_Y + ".bmp");
                        }
                         */
                    }
                    else
                    {
                        result_x = 0;
                        result_y = 0;
                        result_radius = 0;
                        result_score = 0;
                    }
                }
                catch (Exception)
                {
                    //MessageBox.Show("HATA: " + ex.ToString());
                }

                if (Constants.EVALUATE_TIME_ENABLED)
                {
                    int x = (int)(DateTime.Now.Ticks / 10000 - start);
                    if (x >= 0)
                    {
                        Globals.TIME_FINDCIRCLE_MIN = x < Globals.TIME_FINDCIRCLE_MIN ? x : Globals.TIME_FINDCIRCLE_MIN;
                        Globals.TIME_FINDCIRCLE_MAX = x > Globals.TIME_FINDCIRCLE_MAX ? x : Globals.TIME_FINDCIRCLE_MAX;
                        Globals.TIME_FINDCIRCLE_TOTAL += x;
                    }
                }
            }
        }

        /// <summary>
        /// Processor for detecting triangular signs.
        /// </summary>
        public class TriangularSignDetectorProcessor : MsgConsumer
        {
            private bool threadRunning = false;
            static SignFilter sf = new SignFilter(Constants.SIGNFILTER_GREEN_START_COEFF, Constants.SIGNFILTER_BLUE_START_COEFF, 2);
            Bitmap gtcBmp = null;

            private static Population population = null;
            private IPanelDisplay panel = null;

            private static float inner_triangle_scale = 0.6f;

            /*
            private static float[,] redTestPoints = null;
            private static float[,] nonRedTestPoints = null;

            void calculateRedTestPoints()
            {
                redTestPoints = new float[GeoTransChromosome.NUM_OF_POINTS, 2];
                nonRedTestPoints = new float[GeoTransChromosome.NUM_OF_POINTS, 2];


                float float_tmp = (float)(GeoTransChromosome.NUM_OF_POINTS / 3 + 1);
                for (int t = 0; t < GeoTransChromosome.NUM_OF_POINTS / 3; t++)
                {
                    int part = t + 1;
                    redTestPoints[t, 0] = -0.5f + part / float_tmp;
                    redTestPoints[t, 1] = 0.5f;
                    nonRedTestPoints[t, 0] = inner_triangle_scale * redTestPoints[t, 0];
                    nonRedTestPoints[t, 1] = inner_triangle_scale * redTestPoints[t, 1];
                }

                for (int t = GeoTransChromosome.NUM_OF_POINTS / 3; t < 2 * GeoTransChromosome.NUM_OF_POINTS / 3; t++)
                {
                    int part = t % (GeoTransChromosome.NUM_OF_POINTS / 3) + 1;
                    redTestPoints[t, 0] = -0.5f + part / (2 * float_tmp);
                    redTestPoints[t, 1] = 0.5f - part / float_tmp;
                    nonRedTestPoints[t, 0] = inner_triangle_scale * redTestPoints[t, 0];
                    nonRedTestPoints[t, 1] = inner_triangle_scale * redTestPoints[t, 1];
                }

                for (int t = 2 * GeoTransChromosome.NUM_OF_POINTS / 3; t < GeoTransChromosome.NUM_OF_POINTS; t++)
                {
                    int part = t % (2 * GeoTransChromosome.NUM_OF_POINTS / 3) + 1;
                    redTestPoints[t, 0] = 0.5f - part / (2 * float_tmp);
                    redTestPoints[t, 1] = 0.5f - part / float_tmp;
                    nonRedTestPoints[t, 0] = inner_triangle_scale * redTestPoints[t, 0];
                    nonRedTestPoints[t, 1] = inner_triangle_scale * redTestPoints[t, 1];
                }
            }
             */


            static int RS_THRESHOLD = 16;
            void searchSolution(Bitmap rgb)
            {
                long start = DateTime.Now.Ticks / 10000;
                Globals.FRAMES_PROCESSED_TRIANGULAR++;
                int com_x_sum = 0, com_y_sum = 0, com_x_y_point_count = 0;
                Globals.HARVEST_SIGN_ID++;

                //if (redTestPoints == null)
                //    calculateRedTestPoints();

                Bitmap bmp = sf.Apply(rgb);
                GeoTransChromosome sampleChromosome = new GeoTransChromosome(bmp, sf, this, null);

                Population tmpPopulation = new Population(Constants.GA_POPULATION_SIZE,
                    sampleChromosome,
                    sampleChromosome,
                    new EliteSelection()
                );
                tmpPopulation.MutationRate = Constants.GA_MUTATION_RATE;
                tmpPopulation.CrossoverRate = Constants.GA_CROSSOVER_RATE;

                if (population == null || population.BestChromosome == null || population.BestChromosome.Fitness < RS_THRESHOLD / 4)
                {   
                    // fresh population
                }
                else
                {
                    // half from previous
                    for (int j = 0; j < tmpPopulation.Size / 2; j++)
                    {
                        ((GeoTransChromosome)tmpPopulation[j]).copyContent((GeoTransChromosome)population[j]);
                    }
                }
                population = tmpPopulation;


                Graphics gg = null;


                /*
                Bitmap bmp_x = null;
                if (1!=1)
                {
                    bmp_x = new Bitmap(Constants.IMAGE_WIDTH, Constants.IMAGE_HEIGHT * 2, PixelFormat.Format24bppRgb);
                    gg = Graphics.FromImage(bmp_x);
                    gg.DrawImage(bmp, 0, 0);
                    gg.DrawImage(rgb, 0, Constants.IMAGE_HEIGHT);
                    for (int x = 0; x < population.Size; x++)
                    {
                        GeoTransChromosome chromo = (GeoTransChromosome)population[x];
                        gg.DrawRectangle(Pens.Cyan, chromo.X, chromo.Y, 1, 1);
                    }
                }
                */

                int i = 0;
                do
                {
                    long start_epoch = DateTime.Now.Ticks / 10000;
                    Globals.FRAMES_PROCESSED_GA_RUNEPOCH++;

                    // run one epoch of genetic algorithm
                    population.RunEpoch();

                    if (Constants.EVALUATE_TIME_ENABLED)
                    {
                        int x = (int)(DateTime.Now.Ticks / 10000 - start_epoch);
                        if (x >= 0)
                        {
                            Globals.TIME_GA_RUNEPOCH_MIN = x < Globals.TIME_GA_RUNEPOCH_MIN ? x : Globals.TIME_GA_RUNEPOCH_MIN;
                            Globals.TIME_GA_RUNEPOCH_MAX = x > Globals.TIME_GA_RUNEPOCH_MAX ? x : Globals.TIME_GA_RUNEPOCH_MAX;
                            Globals.TIME_GA_RUNEPOCH_TOTAL += x;
                        }
                    }
                    i++;
                } while (i < Constants.GA_NUMBER_ITERATIONS);

                GeoTransChromosome bestChromo = null;
                float rs = 0;
                if (population.BestChromosome != null && population.BestChromosome.Fitness > RS_THRESHOLD)
                {
                    bestChromo = (GeoTransChromosome)population.BestChromosome;

                    int rx = bestChromo.X, ry = bestChromo.Y, rr = bestChromo.Width;
                    rs = (float)bestChromo.Fitness;

                    if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestAll || Constants.HARVEST_TYPE == Constants.HarvestType.harvestMisses)
                    {
                        rgb.Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_0.bmp");
                        bmp.Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_1.bmp");
                    }
                    // findTriangle(bestChromo.X, bestChromo.Y, bmp, ref rx, ref ry, ref rr, ref rs);

                    if (rs < RS_THRESHOLD || rr==0)
                    {
                        if (gg != null)
                        {
                            gg.DrawRectangle(Pens.Red, bestChromo.X - 1, bestChromo.Y - 1, 3, 3);
                            gg.DrawRectangle(Pens.Red, bestChromo.X - bestChromo.Width / 2, bestChromo.Y - bestChromo.Width / 2, bestChromo.Width, bestChromo.Width);
                        }
                        bestChromo = null;
                    }
                    else
                    {
                        rr = (int)(Constants.CAPTURE_RESIZE_TRIANGLE * rr);
                        int xx = (int)(rr * Math.Cos(Math.PI * 30 / 180));  // üçgenin kenarı: 2 . xx
                        int yy = (int)(rr * Math.Sin(Math.PI * 30 / 180));  // üçgenin yüksekliği: 3 . yy

                        int x_left = rx - xx;
                        int y_top = ry - (2 * yy) - ((3*yy)/10);
                        rgb = new Crop(new Rectangle(x_left,
                                                    y_top,
                                                    (x_left + 2 * xx > rgb.Width - 1 ? rgb.Width - 1 - x_left : 2 * xx),
                                                    (y_top + 2 * xx > rgb.Height - 1 ? rgb.Height - 1 - y_top : 2 * xx))).Apply(rgb);
                        if (gg != null)
                        {
                            gg.DrawImage(rgb, 1, Constants.IMAGE_HEIGHT + 1);
                            gg.DrawRectangle(Pens.DarkOrchid, rx - 1, ry - 1, 3, 3);
                            gg.DrawRectangle(Pens.Green, x_left, y_top, rgb.Width, rgb.Height);
                        }

                        rgb = new ResizeBicubic(Constants.SIGN_WIDTH, Constants.SIGN_HEIGHT).Apply(rgb);
                        if (panel != null)
                        {
                            Graphics g = panel.getPanelGraphics();
                            g.DrawRectangle(Pens.Silver, 5, 4, Constants.SIGN_WIDTH, Constants.SIGN_HEIGHT);
                            g.DrawImage(rgb, new Point(5, 4));
                        }

                        if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestAll || Constants.HARVEST_TYPE == Constants.HarvestType.harvestMisses)
                            rgb.Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_3_" +rs+ ".bmp");
                        rgb = AutoBrightnessProcessor.autoBrightness(rgb, Rectangle.Empty, Constants.AUTOBRIGHTNESS_BASE_LUM);

                        if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestAll || Constants.HARVEST_TYPE == Constants.HarvestType.harvestMisses)
                            rgb.Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_4.bmp");

                        int dynamic_gray = 0;
                        if (Constants.LABELING_TYPE == Constants.LabelingType.redBlackWhite)
                            rgb = new ColorLabelFilter(new Color[] { Color.FromArgb(255, 0, 0), Color.White, Color.Black }, true).Apply(rgb);
                        else if (Constants.LABELING_TYPE == Constants.LabelingType.blackAndWhite)
                        {
                            rgb = new ColorLabelFilter(new Color[] { Color.White, Color.FromArgb(dynamic_gray, dynamic_gray, dynamic_gray) }, true).Apply(rgb);
                        }
                        if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestAll || Constants.HARVEST_TYPE == Constants.HarvestType.harvestMisses)
                            rgb.Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_5.bmp");


                        int start_x = 0, start_y = 0;
                        int y = 0;
                        for (; y < Constants.SIGN_HEIGHT && (start_x+start_y)==0; y++)
                        {
                            for (int x = 0; x < Constants.SIGN_WIDTH; x++)
                            {
                                Color clr = rgb.GetPixel(x, y);
                                if (clr.R == 0)
                                {
                                    start_x = x;
                                    start_y = y;
                                    break;
                                }
                            }
                        }
                        PointedColorFloodFill filter = new PointedColorFloodFill();
                        filter.Tolerance = Color.FromArgb(0, 0, 0);
                        filter.FillColor = Color.FromArgb(255, 255, 255);
                        filter.StartingPoint = new Point(start_x, start_y);
                        Bitmap rgb_flood_fill = filter.Apply(rgb);

                        int black_region_found_at_center = 0;
                        y = 4 * (Constants.SIGN_HEIGHT / 10);
                        for (int x = 4 * (Constants.SIGN_WIDTH / 10); x < 6 * (Constants.SIGN_WIDTH / 10); x += 2)
                        {
                            Color clr = rgb_flood_fill.GetPixel(x, y);
                            if (clr.R == 0)
                            {
                                black_region_found_at_center++;
                            }
                        }
                        if (black_region_found_at_center < 8)
                        {
                            y = 6 * (Constants.SIGN_HEIGHT / 10);
                            for (int x = 4 * (Constants.SIGN_WIDTH / 10); x < 6 * (Constants.SIGN_WIDTH / 10); x += 2)
                            {
                                Color clr = rgb_flood_fill.GetPixel(x, y);
                                if (clr.R == 0)
                                {
                                    black_region_found_at_center++;
                                }
                            }
                        }
                        if (black_region_found_at_center >= 5)
                        {
                            rgb = rgb_flood_fill;
                        } 

                        BitmapData image_data = rgb.LockBits(new Rectangle(0, 0, rgb.Width, rgb.Height), ImageLockMode.ReadWrite, rgb.PixelFormat);
                        int bpp = 3;
                        int nOffset = image_data.Stride - rgb.Width * bpp;
                        System.IntPtr Scan0 = image_data.Scan0;
                        unsafe
                        {
                            byte* p = (byte*)Scan0;
                            for (y = 0; y < Constants.SIGN_HEIGHT; y++)
                            {
                                // for each pixel
                                for (int x = 0; x < Constants.SIGN_WIDTH; x++, p += bpp)
                                {
                                    if (y >= Constants.SIGN_HEIGHT - 12 || y <= 14 || x >= Constants.SIGN_WIDTH - 12 || x <= 12
                                        ||
                                             (x <= Constants.SIGN_WIDTH / 2 && y <= (Constants.SIGN_HEIGHT) - 2 * x + 8)
                                        || (x > Constants.SIGN_WIDTH / 2 && y <= 2 * (x - Constants.SIGN_HEIGHT / 2) + 8)
                                        
                                        )
                                    {
                                        p[RGB.R] = 255;
                                        p[RGB.G] = 255;
                                        p[RGB.B] = 255;
                                    }
                                    else if (p[RGB.R] == 0)
                                    {
                                        com_x_sum += x;
                                        com_y_sum += y;
                                        com_x_y_point_count++;
                                    }
                                }
                                p += nOffset;
                            }
                        }
                        rgb.UnlockBits(image_data);

                        if (com_x_y_point_count < 10)
                            bestChromo = null;
                        else if (Constants.HARVEST_TYPE > Constants.HarvestType.noHarvest)
                        {
                            if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestIntoFolder)
                            {
                                string[] signs = Globals.SIGN_IN_FRAME.Split(',');

                                for (int j = 1; j < signs.Length; j++)
                                {
                                    if (!Directory.Exists(Constants.base_folder + "hasat\\" + signs[j]))
                                        Directory.CreateDirectory(Constants.base_folder + "hasat\\" + signs[j]);
                                    lock (Globals.HARVEST_LOCK)
                                    {
                                        rgb.Save(Constants.base_folder + "hasat\\" + signs[j] + "\\" + Globals.HARVEST_SIGN_ID + ".bmp");
                                    }
                                }
                            }
                            else
                            {
                                rgb.Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_6.bmp");
                            }
                        }

                        gtcBmp = rgb;
                    }
                }



                if (Constants.EVALUATE_TIME_ENABLED)
                {
                    int x = (int)(DateTime.Now.Ticks / 10000 - start);
                    if (x >= 0)
                    {
                        Globals.TIME_TRIANGLE_MIN = x < Globals.TIME_TRIANGLE_MIN ? x : Globals.TIME_TRIANGLE_MIN;
                        Globals.TIME_TRIANGLE_MAX = x > Globals.TIME_TRIANGLE_MAX ? x : Globals.TIME_TRIANGLE_MAX;
                        Globals.TIME_TRIANGLE_TOTAL += x;
                    }
                }

                if (bestChromo != null)
                {
                    if (panel != null)
                    {
                        Graphics g = panel.getPanelGraphics();
                        g.DrawRectangle(Pens.Silver, 5, 74, Constants.SIGN_WIDTH, Constants.SIGN_HEIGHT);
                        g.DrawImage(gtcBmp, new Point(5, 74));
                    }
                    if (msgService != null)
                    {
                        VisionMessage vm = new VisionMessage(gtcBmp.Height, gtcBmp.Width, ByteTools.pixelFormatToBPP(gtcBmp.PixelFormat), gtcBmp);

                        // Center of Mass
                        vm.CoM_X = com_x_sum / (com_x_y_point_count == 0 ? 1 : com_x_y_point_count);
                        vm.CoM_Y = com_y_sum / (com_x_y_point_count == 0 ? 1 : com_x_y_point_count);
                        msgService.sendMsg(vm);
                    }
                }
                else
                {
                    if (Globals.RIGHT_PANEL_SHOWING_STH)
                    {
                        Globals.RIGHT_PANEL_SHOWING_STH = false;
                        Graphics g = panel.getPanelGraphics();
                        g.Clear(Color.Silver);
                    }
                }

                //if (rs > RS_THRESHOLD)
                //    bmp_x.Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_" +rs+ "_" +sf.BlueCoeff+ "_" +sf.GreenCoeff+ ".bmp");

                threadRunning = false;
            }

            public TriangularSignDetectorProcessor(MsgService msgService, IPanelDisplay panel)
                : base(msgService, VisionMessage.msgType)
            {
                this.panel = panel;
            }

            public override void consumeMessage(IMessage message, int msgID)
            {
                VisionMessage vm = message as VisionMessage;

                lock (this)
                {
                    if (!threadRunning)
                    {
                        threadRunning = true;
                        ThreadStart starter = delegate { searchSolution(vm.Bitmap); };
                        Thread innerThread = new Thread(starter);
                        innerThread.Start();
                    }
                }
            }



            /****************************/
            private unsafe void goLeft(ref byte* p, ref int x, int Width)
            {
                int i = 1;
                for (; i < max_uzaklik; i++)
                {
                    x -= pixel_increment;
                    if (x <= 0)
                    {
                        x = 0;
                        break;
                    }
                    p -= bpp * pixel_increment;
                    if (p[0] == 255)
                    {
                        return;
                    }
                }
                if (i >= max_uzaklik - 1)
                    x = 0;
            }
            private unsafe int skipLeft(ref byte* p, ref int x, int Width)
            {
                while (x > 0)
                {
                    x -= pixel_increment;
                    p -= bpp * pixel_increment;
                    if (p[0] != 255)
                    {
                        break;
                    }
                }
                return x;
            }

            private unsafe void goRight(ref byte* p, ref int x, int Width)
            {
                int i = 1;
                for (; i < max_uzaklik; i++)
                {
                    x += pixel_increment;
                    if (x >= Width)
                    {
                        x = 0;
                        break;
                    }
                    p += bpp * pixel_increment;
                    if (p[0] == 255)
                    {
                        return;
                    }
                }
                if (i >= max_uzaklik - 1)
                    x = 0;
            }

            private unsafe int skipRight(ref byte* p, ref int x, int Width)
            {
                while (x < Width - 1)
                {
                    x += pixel_increment;
                    p += bpp * pixel_increment;
                    if (p[0] != 255)
                    {
                        break;
                    }
                }
                return x;
            }

            private unsafe void goUp(ref byte* p, ref int y, int Height, int line_pixels)
            {
                int i = 1;
                for (; i < max_uzaklik; i++)
                {
                    y -= pixel_increment;
                    if (y <= 0)
                    {
                        y = 0;
                        break;
                    }
                    p -= line_pixels * pixel_increment;
                    if (p[0] == 255)
                    {
                        return;
                    }
                }
                if (i >= max_uzaklik - 1)
                    y = 0;
            }
            private unsafe void skipUp(ref byte* p, ref int y, int Height, int line_pixels)
            {
                while (y > 0)
                {
                    y -= pixel_increment;
                    p -= line_pixels * pixel_increment;
                    if (p[0] != 255)
                    {
                        break;
                    }
                }
            }

            private unsafe void goDown(ref byte* p, ref int y, int Height, int line_pixels)
            {
                int i = 1;
                for (; i < max_uzaklik; i++)
                {
                    y += pixel_increment;
                    if (y >= Height)
                    {
                        y = 0;
                        break;
                    }
                    p += line_pixels * pixel_increment;
                    if (p[0] == 255)
                    {
                        return;
                    }
                }
                if (i >= max_uzaklik - 1)
                    y = 0;
            }
            private unsafe void skipDown(ref byte* p, ref int y, int Height, int line_pixels)
            {
                while (y < Height - 1)
                {
                    y += pixel_increment;
                    p += line_pixels * pixel_increment;
                    if (p[0] != 255)
                    {
                        break;
                    }
                }
            }

            int max_uzaklik = 50;
            int pixel_increment = 1;
            int bpp = 1;
            Bitmap m_Bitmap;

            public void findTriangle(int e_X, int e_Y, Bitmap image, ref int result_x, ref int result_y, ref int result_radius, ref float result_score)
            {
                long start = DateTime.Now.Ticks / 10000;
                Globals.FRAMES_PROCESSED_FINDTRIANGLE++;

                Bitmap img_24bpp = null;
                Graphics g = null;
                if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestAll || Constants.HARVEST_TYPE == Constants.HarvestType.harvestMisses)
                {
                    img_24bpp = new Bitmap(Constants.IMAGE_WIDTH, Constants.IMAGE_HEIGHT, PixelFormat.Format24bppRgb);
                    g = Graphics.FromImage(img_24bpp);
                    g.Clear(Color.Black);
                    g.DrawImage(image, 0, 0);
                }


                m_Bitmap = image;
                int max_level = 2;
                int e_X_orig = e_X;
                int e_Y_orig = e_Y;
                BitmapData image_data = null;
                try
                {
                    int[] left_points = new int[max_level];
                    int[] right_points = new int[max_level];
                    ArrayList x_y_radius = new ArrayList();
                    ArrayList x_endpoints = new ArrayList();
                    ArrayList y_endpoints = new ArrayList();

                    int left_x = e_X, right_x = e_X;

                    image_data = image.LockBits(new Rectangle(0, 0, m_Bitmap.Width, m_Bitmap.Height), ImageLockMode.ReadWrite, PixelFormat.Format8bppIndexed);
                    int nOffset = image_data.Stride - m_Bitmap.Width * bpp;

                    System.IntPtr Scan0 = image_data.Scan0;
                    unsafe
                    {
                        byte* p = (byte*)(void*)Scan0;
                        p += (((image.Width * bpp + nOffset) * e_Y) + (e_X * bpp));
                        byte* pOrig = p;

                        for (int level = 0; level < max_level; )
                        {
                            goLeft(ref p, ref left_x, image.Width);
                            if (left_x != 0)
                            {
                                x_endpoints.Add(new int[] { left_x, e_Y_orig });
                                left_points[level] = left_x;
                                level++;
                                if (level < max_level)
                                    skipLeft(ref p, ref left_x, image.Width);
                            }
                            else
                                break;
                        }


                        p = pOrig;

                        for (int level = 0; level < max_level; )
                        {
                            goRight(ref p, ref right_x, image.Width);
                            if (right_x != 0)
                            {
                                x_endpoints.Add(new int[] { right_x, e_Y_orig });
                                right_points[level] = right_x;
                                level++;
                                if (level < max_level)
                                    skipRight(ref p, ref right_x, image.Width);
                            }
                            else
                                break;
                        }
                    }

                    for (int left_index = 0; left_index < max_level; left_index++)
                    {
                        if (left_points[left_index] == 0)
                            break;
                        for (int right_index = 0; right_index < max_level; right_index++)
                        {
                            if (right_points[right_index] == 0)
                                break;

                            int[] up_points = new int[max_level];
                            int[] down_points = new int[max_level];


                            int center_x = (left_points[left_index] + right_points[right_index]) / 2;
                            Scan0 = image_data.Scan0;
                            unsafe
                            {
                                byte* p = (byte*)(void*)Scan0;
                                p += (((image.Width * bpp + nOffset) * e_Y_orig) + (center_x * bpp));
                                byte* pOrig = p;

                                int y_tmp1 = e_Y_orig;
                                for (int level = 0; level < max_level; )
                                {
                                    goUp(ref p, ref y_tmp1, image.Height, (image.Width * bpp + nOffset));
                                    if (y_tmp1 != 0)
                                    {
                                        up_points[level] = y_tmp1;
                                        y_endpoints.Add(new int[] { center_x, y_tmp1 });
                                        level++;
                                        if (level < max_level)
                                            skipUp(ref p, ref y_tmp1, image.Height, (image.Width * bpp + nOffset));
                                    }
                                    else
                                        break;
                                }


                                p = pOrig;

                                int y_tmp2 = e_Y_orig;
                                for (int level = 0; level < max_level; )
                                {
                                    goDown(ref p, ref y_tmp2, image.Height, (image.Width * bpp + nOffset));
                                    if (y_tmp2 != 0)
                                    {
                                        y_endpoints.Add(new int[] { center_x, y_tmp2 });
                                        down_points[level] = y_tmp2;
                                        level++;
                                        if (level < max_level)
                                            skipDown(ref p, ref y_tmp2, image.Height, (image.Width * bpp + nOffset));
                                    }
                                    else
                                        break;
                                }
                            }


                            for (int up_index = 0; up_index < max_level; up_index++)
                            {
                                if (up_points[up_index] == 0)
                                    break;
                                for (int down_index = 0; down_index < max_level; down_index++)
                                {
                                    if (down_points[down_index] == 0)
                                        break;
                                    int radius = 2 * ((down_points[up_index] - up_points[down_index]) / 3);
                                    if (radius >= Constants.MIN_RADIUS)
                                        x_y_radius.Add(new int[] { center_x, down_points[down_index] - ((down_points[up_index] - up_points[down_index]) / 3), radius + 4, 0 });
                                }
                            }

                        }
                    }


                    m_Bitmap.UnlockBits(image_data);

                    Size size = new Size(1, 1);
                    int best_triangle_index = -1;
                    float best_triangle_score = -1f;

                    for (int triangle_index = 0; triangle_index < x_y_radius.Count; triangle_index++)
                    {
                        int[] x_y_radius_element = (int[])x_y_radius[triangle_index];
                        int center_x = x_y_radius_element[0];
                        int center_y = x_y_radius_element[1];
                        int radius = x_y_radius_element[2];
                        float score = 0;

                        // Left-bottom point
                        int xx = (int)(radius * Math.Cos(Math.PI * 30 / 180));  // üçgenin kenarı: 2 . xx
                        int yy = (int)(radius * Math.Sin(Math.PI * 30 / 180));  // üçgenin yüksekliği: 3 . yy
                        int xx_inner = (int)Math.Round(xx * inner_triangle_scale);  // üçgenin kenarı: 2 . xx
                        int yy_inner = (int)Math.Round(yy * inner_triangle_scale);  // üçgenin yüksekliği: 3 . yy
                        Color clr, clr2;

                        Point pt_left_bottom = new Point(center_x - xx, center_y + yy);
                        Point pt_left_bottom_inner = new Point(center_x - xx_inner, center_y + yy_inner);
                        Point pt_right_bottom = new Point(center_x + xx, center_y + yy);
                        Point pt_right_bottom_inner = new Point(center_x + xx_inner, center_y + yy_inner);

                        Point pt = new Point(center_x, center_y - 2 * yy);
                        Point pt_inner;
                        Point pt_top = pt;

                        float x_increment = (float)xx / 10;
                        float y_increment = (float)(3 * yy) / 10;
                        float x_increment_inner = x_increment * inner_triangle_scale;
                        float y_increment_inner = y_increment * inner_triangle_scale;
                        for (int cnt = 1; cnt <= 10; cnt++)
                        {
                            pt = new Point((int)(pt_left_bottom.X + x_increment * cnt), (int)(pt_left_bottom.Y - y_increment * cnt));
                            if (pt.X >= 0 && pt.X < image.Width && pt.Y >= 0 && pt.Y < image.Height)
                            {
                                clr = ((Bitmap)image).GetPixel(pt.X, pt.Y);
                                pt_inner = new Point((int)(pt_left_bottom_inner.X + x_increment_inner * cnt), (int)(pt_left_bottom_inner.Y - y_increment_inner * cnt));
                                clr2 = ((Bitmap)image).GetPixel(pt_inner.X, pt_inner.Y);
                                if (clr.R == 255)
                                {
                                    score += 1f;
                                    if (g != null)
                                        g.DrawRectangle(Pens.Green, new Rectangle(new Point(pt.X, pt.Y), size));
                                }
                                else
                                {
                                    score -= 0.5f;
                                    if (g != null)
                                        g.DrawRectangle(Pens.Red, new Rectangle(new Point(pt.X, pt.Y), size));
                                }
                                if (clr2.R == 255)
                                    score -= 1.5f;
                                if (g != null)
                                    g.DrawRectangle(Pens.DarkOrchid, new Rectangle(new Point(pt_inner.X, pt_inner.Y), size));
                            }
                        }

                        for (int cnt = 0; cnt < 10; cnt++)
                        {
                            pt = new Point((int)(pt_right_bottom.X - x_increment * cnt), (int)(pt_right_bottom.Y - y_increment * cnt));
                            if (pt.X >= 0 && pt.X < image.Width && pt.Y >= 0 && pt.Y < image.Height)
                            {
                                clr = ((Bitmap)image).GetPixel(pt.X, pt.Y);
                                pt_inner = new Point((int)(pt_right_bottom_inner.X - x_increment_inner * cnt), (int)(pt_left_bottom_inner.Y - y_increment_inner * cnt));
                                clr2 = ((Bitmap)image).GetPixel(pt_inner.X, pt_inner.Y);
                                if (clr.R == 255)
                                {
                                    score += 1f;
                                    if (g != null)
                                        g.DrawRectangle(Pens.Green, new Rectangle(new Point(pt.X, pt.Y), size));
                                }
                                else
                                {
                                    score -= 0.5f;
                                    if (g != null)
                                        g.DrawRectangle(Pens.Red, new Rectangle(new Point(pt.X, pt.Y), size));
                                }
                                if (clr2.R == 255)
                                    score -= 1.5f;
                                if (g != null)
                                    g.DrawRectangle(Pens.DarkOrchid, new Rectangle(new Point(pt_inner.X, pt_inner.Y), size));
                            }
                        }

                        x_increment = (float)xx * 2 / 10;
                        x_increment_inner = x_increment * inner_triangle_scale;
                        for (int cnt = 0; cnt < 10; cnt++)
                        {
                            pt = new Point((int)(pt_left_bottom.X + x_increment * cnt), pt_right_bottom.Y);
                            if (pt.X >= 0 && pt.X < image.Width && pt.Y >= 0 && pt.Y < image.Height)
                            {
                                clr = ((Bitmap)image).GetPixel(pt.X, pt.Y);
                                pt_inner = new Point((int)(pt_left_bottom_inner.X + x_increment_inner * cnt), pt_left_bottom_inner.Y);
                                clr2 = ((Bitmap)image).GetPixel(pt_inner.X, pt_inner.Y);
                                if (clr.R == 255)
                                {
                                    score += 1f;
                                    if (g != null)
                                        g.DrawRectangle(Pens.Green, new Rectangle(new Point(pt.X, pt.Y), size));
                                }
                                else
                                {
                                    score -= 0.5f;
                                    if (g != null)
                                        g.DrawRectangle(Pens.Red, new Rectangle(new Point(pt.X, pt.Y), size));
                                }
                                if (clr2.R == 255)
                                    score -= 1.5f;
                                if (g != null)
                                    g.DrawRectangle(Pens.DarkOrchid, new Rectangle(new Point(pt_inner.X, pt_inner.Y), size));
                            }
                        }


                        if (score > best_triangle_score)
                        {
                            best_triangle_index = triangle_index;
                            best_triangle_score = score;
                        }
                    }

                    if (best_triangle_index != -1)
                    {
                        int[] best_triangle = (int[])x_y_radius[best_triangle_index];
                        result_x = best_triangle[0];
                        result_y = best_triangle[1];
                        result_radius = best_triangle[2];
                        result_score = best_triangle_score;
                        /*
                        if (result_score > RS_THRESHOLD)
                        {
                            Bitmap xxx = new Crop(new Rectangle(result_x - result_radius,
                                                            result_y - result_radius,
                                                            result_radius*2,
                                                            result_radius*2)).Apply(image);
                            xxx.Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_triangle_" + result_score+".bmp");
                        }
                        */
                        if (img_24bpp != null)
                        {
                            Point pt = new Point(e_X-1, e_Y-1);
                            g.DrawRectangle(Pens.Red, new Rectangle(pt, new Size(3, 3)));

                            g.DrawRectangle(Pens.Cyan, new Rectangle(new Point(result_x-1, result_y-1), new Size(3, 3)));
                            g.DrawEllipse(Pens.Green, result_x - result_radius, result_y - result_radius, result_radius * 2, result_radius * 2);
                            g.DrawEllipse(Pens.Green, result_x - result_radius - 1, result_y - result_radius - 1, result_radius * 2 + 2, result_radius * 2 + 2);

                            img_24bpp.Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_findTriangle_" + e_X + "_" + e_Y + ".bmp");
                        }
                    }
                    else
                    {
                        result_x = 0;
                        result_y = 0;
                        result_radius = 0;
                        result_score = 0;
                        if (img_24bpp != null)
                        {
                            Point pt = new Point(e_X-1, e_Y-1);
                            g.DrawRectangle(Pens.Red, new Rectangle(pt, new Size(3, 3)));
                            // img_24bpp.Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_findTriangle_" + e_X + "_" + e_Y + ".bmp");
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("HATA: " + ex.ToString());
                }

                if (Constants.EVALUATE_TIME_ENABLED)
                {
                    int x = (int)(DateTime.Now.Ticks / 10000 - start);
                    if (x >= 0)
                    {
                        Globals.TIME_FINDTRIANGLE_MIN = x < Globals.TIME_FINDTRIANGLE_MIN ? x : Globals.TIME_FINDTRIANGLE_MIN;
                        Globals.TIME_FINDTRIANGLE_MAX = x > Globals.TIME_FINDTRIANGLE_MAX ? x : Globals.TIME_FINDTRIANGLE_MAX;
                        Globals.TIME_FINDTRIANGLE_TOTAL += x;
                    }
                }
            }
        }

        /// <summary>
        /// Hough line processor for classical lane detection method.
        /// </summary>
        public class HoughLineProcessor : MsgConsumer
        {
            private HoughLineTransformation lineTransform = new HoughLineTransformation();


            byte[] redMap;
            byte[] greenMap;
            byte[] blueMap;

            int[] currentThetaStates = new int[] { int.MinValue, int.MinValue, int.MinValue };
            int[] currentRStates = new int[] { int.MinValue, int.MinValue, int.MinValue };
            double[] currentStateProbs = new double[] { 0, 0, 0 };

            Pen p = new Pen(Color.DarkRed, 3);
            Pen p2 = new Pen(Color.DarkBlue, 3);

            bool threadRunning = false;

            int[] widthRes = new int[] { 0, Constants.IMAGE_WIDTH };
            int[] heightRes = new int[] { Constants.IMAGE_SKIP_TOP_PIXELS, Constants.IMAGE_HEIGHT };

            public HoughLineProcessor(MsgService msgService)
                : base(msgService, VisionMessage.msgType)
            {
                redMap = new byte[256];
                greenMap = new byte[256];
                blueMap = new byte[256];

                for (int i = 0; i < 256; i++)
                {
                    redMap[i] = (byte)(i > 196 ? 255 : 0);
                    greenMap[i] = (byte)(i > 196 ? 255 : 0);
                    blueMap[i] = (byte)(i > 206 ? 255 : 0);

                }
            }

            private int getMax(double[,] array, int dim, int i)
            {
                int len = array.GetLength(dim);
                double jMax = double.MinValue;
                int jIndex = 0;
                for (int j = 0; j < len; j++)
                {
                    double val;
                    if (dim == 0)
                        val = array[j, i];
                    else
                        val = array[i, j];
                    if (jMax < val)
                    {
                        jIndex = j;
                        jMax = val;
                    }
                }
                return jIndex;
            }

            private double getProductMax(double[,] tx, double[,] ox, int currentState, int observation, ref int candidateState)
            {
                double maxVal = tx[currentState, currentState] * ox[currentState, observation];
                candidateState = currentState;
                int stateLen = tx.GetLength(1);
                for (int i = 0; i < stateLen; i++)
                {
                    double candidateVal = tx[currentState, i] * ox[i, observation];
                    if (candidateVal > maxVal)
                    {
                        maxVal = candidateVal;
                        candidateState = i;
                    }
                }
                return maxVal;
            }



            private void drawLine(Graphics g, HoughLine hl, Rectangle sub)
            {
                double t = hl.Theta;
                short r = (short)(hl.Radius);

                int xOffset = sub.X + sub.Width / 2;
                int yOffset = sub.Y + sub.Height / 2;

                if (r < 0)
                {
                    r = Math.Abs(r);
                    t = t - 180;
                }

                float cos = (float)Math.Cos(Math.PI * t / 180d);
                float sin = (float)Math.Sin(Math.PI * t / 180d);
                if (Math.Abs(cos) > 0.00001 && Math.Abs(sin) > 0.00001)
                {
                    double cot = cos / sin;

                    int y1 = (int)(cot * sub.X + yOffset - cot * xOffset - r / sin);
                    int y2 = (int)(cot * (sub.X + sub.Width) + yOffset - cot * xOffset - r / sin);
                    int x1 = (int)((sub.Y - yOffset + r / sin + cot * xOffset) / cot);
                    int x2 = (int)((sub.Y + sub.Height - yOffset + r / sin + cot * xOffset) / cot);

                    Point p1 = Point.Empty;
                    Point p2 = Point.Empty;

                    if (Math.Abs(y1 - yOffset) <= sub.Height / 2)
                        p1 = new Point(sub.X, y1);
                    if (Math.Abs(y2 - yOffset) <= sub.Height / 2)
                        if (p1 == Point.Empty)
                            p1 = new Point(sub.X + sub.Width, y2);
                        else
                            p2 = new Point(sub.X + sub.Width, y2);
                    if (Math.Abs(x1 - xOffset) <= sub.Width / 2)
                        if (p1 == Point.Empty)
                            p1 = new Point(x1, sub.Y);
                        else
                            p2 = new Point(x1, sub.Y);
                    if (Math.Abs(x2 - xOffset) <= sub.Width / 2)
                        if (p1 == Point.Empty)
                            p1 = new Point(x2, sub.Y + sub.Height);
                        else
                            p2 = new Point(x2, sub.Y + sub.Height);

                    g.DrawLine(p, p1, p2);
                }
            }

            private HoughLine moveHL(HoughLine hl, float oldX, float oldY, float newX, float newY)
            {
                double t = hl.Theta;
                short r = hl.Radius;
                float cos = (float)Math.Cos(Math.PI * t / 180d);
                float sin = (float)Math.Sin(Math.PI * t / 180d);
                r = (short)(r + (oldX - newX) * cos + (newY - oldY) * sin);

                return new HoughLine(t, r, hl.Intensity, hl.RelativeIntensity);
            }

            public void findLines(object o)
            {
                DateTime tick = DateTime.Now;
                const int lineCount = 10;
                Bitmap bitmap = (Bitmap)o;
                HoughLine[] hls = new HoughLine[lineCount];

                for (int i = 0; i < hls.Length; i++)
                    hls[i] = null;

                for (int i = 0; i < heightRes.Length - 1; i++)
                {
                    int y = heightRes[i];
                    int yDelta = heightRes[i + 1] - heightRes[i];
                    int xDelta = widthRes[i + 1] - widthRes[i];

                    for (int x = 0; x < bitmap.Width; x += xDelta)
                    {
                        Rectangle sub = new Rectangle(x, y, xDelta, yDelta);
                        Bitmap subImg = (Bitmap)bitmap.Clone(sub, bitmap.PixelFormat);
                        HoughLineTransformation hlt = new HoughLineTransformation();
                        hlt.ProcessImage(subImg);
                        hls = hlt.GetMostIntensiveLines(lineCount);
                        for (int j = 0; j < lineCount; j++)
                        {
                            if (hls.Length > j && hls[j] != null)
                            {
                                float xOffset = subImg.Width / 2f + x;
                                float yOffset = subImg.Height / 2f + y;
                                hls[j] = moveHL(hls[j], xOffset, yOffset, bitmap.Width / 2f, bitmap.Height);
                            }
                        }
                    }
                }
                short[] bestThreeRadius = new short[] { 0, 0, 0 };
                double[] bestThreeIntensity = new double[] { 0, 0, 0 };
                double[] bestThreeTheta = new double[] { double.MinValue, double.MinValue, double.MinValue };
                foreach (HoughLine hl in hls)
                {
                    if (hl == null)
                        continue;

                    short rNew = (short)(hl.Radius);
                    double tNew = hl.Theta;
                    double sNew = hl.RelativeIntensity;

                    bool isNew = true;
                    for (int i = 0; i < 3; i++)
                    {
                        short r = bestThreeRadius[i];
                        double t = bestThreeTheta[i];
                        double s = bestThreeIntensity[i];

                        if (t != double.MinValue && (Math.Abs(t - tNew) < 30 || 180 - Math.Abs(t - tNew) < 30) && Math.Abs(r - rNew) < 30)
                        {
                            bestThreeRadius[i] = (short)(((r * s) + (rNew * sNew)) / (s + sNew));
                            bestThreeTheta[i] = (short)(((t * s) + (tNew * sNew)) / (s + sNew));
                            bestThreeIntensity[i] += sNew;
                            isNew = false;
                            break;
                        }
                    }

                    if (isNew)
                    {
                        double minS = double.MaxValue;
                        int minSidx = 0;
                        for (int i = 0; i < 3; i++)
                        {
                            if (bestThreeIntensity[i] < minS)
                            {
                                minS = bestThreeIntensity[i];
                                minSidx = i;
                            }
                        }
                        if (sNew > bestThreeIntensity[minSidx])
                        {
                            bestThreeRadius[minSidx] = rNew;
                            bestThreeIntensity[minSidx] = sNew;
                            bestThreeTheta[minSidx] = tNew;
                        }
                    }
                }
                for (int i = 0; i < 3; i++)
                {
                    currentStateProbs[i] = double.MinValue;
                    if (bestThreeTheta[i] == double.MinValue || bestThreeIntensity[i] < 1)
                    {
                        bestThreeTheta[i] = double.MinValue;
                        continue;
                    }

                    currentThetaStates[i] = (int)bestThreeTheta[i];
                    currentRStates[i] = bestThreeRadius[i];
                    currentStateProbs[i] = bestThreeIntensity[i];
                }
                threadRunning = false;
                DateTime tack = DateTime.Now;
                //Console.WriteLine(tack.Subtract(tick).Milliseconds);
            }

            public override void consumeMessage(IMessage message, int msgID)
            {
                VisionMessage vm = message as VisionMessage;
                // Bitmap bitmap = ByteTools.BytesToBmp(vm.Bitmap, vm.BmpWidth, vm.BmpHeight, ByteTools.bppToPixelFormat(vm.BytePerPixel));
                Bitmap bitmap = vm.Bitmap;
                Bitmap rgb = bitmap;

                bitmap = new ColorRemapping(redMap, greenMap, blueMap).Apply(bitmap);
                bitmap = new GrayscaleBT709().Apply(bitmap);
                bitmap = new Threshold(32).Apply(bitmap);

                //rgb = new GrayscaleToRGB().Apply(bitmap);

                Graphics g = Graphics.FromImage(rgb);

                if (g == null)
                {
                    Console.WriteLine("Null Graphics!");
                    return;
                }

                if (!threadRunning)
                {
                    threadRunning = true;
                    for (int i = 0; i < 3; i++)
                    {
                        if (g != null && currentStateProbs[i] > double.MinValue)
                        {
                            drawLine(g, new HoughLine((double)currentThetaStates[i], (short)currentRStates[i], 0, 0d),
                               new Rectangle(0, heightRes[0], bitmap.Width, (heightRes[heightRes.Length - 1] - heightRes[0]) * 2));
                        }
                    }
                    ThreadStart starter = delegate { findLines(bitmap); };
                    Thread innerThread = new Thread(starter);
                    innerThread.Start();
                }

                vm = new VisionMessage(rgb.Height, rgb.Width, ByteTools.pixelFormatToBPP(rgb.PixelFormat), rgb);
                msgService.sendMsg(vm);
            }

            public override string ToString()
            {
                return "LaneDetectorProcessor";
            }

        }

        /// <summary>
        /// Automatic brightness correction processor.
        /// </summary>
        public class AutoBrightnessProcessor : MsgConsumer
        {
            double baseLum;
            Rectangle baseRect;
            public AutoBrightnessProcessor(double baseLum, Rectangle baseRect, MsgService msgService)
                : base(msgService, VisionMessage.msgType)
            {
                this.baseLum = baseLum;
                this.baseRect = baseRect;
            }

            public override void consumeMessage(IMessage message, int msgID)
            {
                long start = DateTime.Now.Ticks / 10000;
                Globals.FRAMES_PROCESSED_AUTOBRIGHTNESS++;

                VisionMessage vm = message as VisionMessage;
                Bitmap rgb = vm.Bitmap;
                rgb = autoBrightness(rgb, baseRect, baseLum);
                vm = new VisionMessage(rgb.Height, rgb.Width, ByteTools.pixelFormatToBPP(rgb.PixelFormat), rgb);

                if (Constants.EVALUATE_TIME_ENABLED)
                {
                    int x = (int)(DateTime.Now.Ticks / 10000 - start);
                    if (x >= 0)
                    {
                        Globals.TIME_AUTOBRIGHTNESS_MIN = x < Globals.TIME_AUTOBRIGHTNESS_MIN ? x : Globals.TIME_AUTOBRIGHTNESS_MIN;
                        Globals.TIME_AUTOBRIGHTNESS_MAX = x > Globals.TIME_AUTOBRIGHTNESS_MAX ? x : Globals.TIME_AUTOBRIGHTNESS_MAX;
                        Globals.TIME_AUTOBRIGHTNESS_TOTAL += x;
                    }
                }
                msgService.sendMsg(vm);
            }

            public static Bitmap autoBrightness(Bitmap rgb, Rectangle baseRect, double baseLum)
            {
                ImageStatisticsHSL ist = null;
                if (baseRect.IsEmpty)
                    ist = new ImageStatisticsHSL(rgb);
                else
                    // baseRect = new Rectangle(new Point(0, 0), rgb.Size);
                    ist = new ImageStatisticsHSL(new Crop(baseRect).Apply(rgb));
                //Console.WriteLine("Lum:" + ist.Luminance.Mean + " Sat:" + ist.Saturation.Mean);
                return new BrightnessCorrection(baseLum - ist.Luminance.Mean).Apply(rgb);
            }
        }

        /// <summary>
        /// Sign classification processor uses SVM based on SURF features or grid based features.
        /// </summary>
        public class SVM_SURFProcessor : MsgConsumer
        {
            Model model = null;
            static Bitmap[] signs = null;
            Constants.SignType signType;

            public SVM_SURFProcessor(MsgService msgService, Constants.SignType signType)
                : base(msgService, VisionMessage.msgType)
            {
                this.signType = signType;
                if (signs == null)
                {
                    signs = new Bitmap[Constants.NUM_OF_SIGN_TYPES + 1];
                    ResizeBicubic resizer = new ResizeBicubic(32, 32);
                    for (int i = 1; i <= Constants.NUM_OF_SIGN_TYPES; i++)
                    {
                        String file_name = "signs\\sign_" + (i < 10 ? ("0" + i) : ("" + i)) + ".bmp";
                        signs[i] = resizer.Apply((Bitmap)Bitmap.FromFile(Constants.base_folder + file_name, false));
                    }
                }
                if (model == null)
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    FileStream stream = new FileStream(Constants.base_folder + Constants.NN_SVM_SURF + "_" + (signType == Constants.SignType.circular ? "circle" : "triangle") + ".dat", FileMode.Open, FileAccess.Read, FileShare.None);
                    model = (Model)formatter.Deserialize(stream);
                    stream.Close();
                }
            }


            public override void consumeMessage(IMessage message, int msgID)
            {
                long start = DateTime.Now.Ticks / 10000;
                Globals.FRAMES_PROCESSED_SVM++;

                VisionMessage vm = message as VisionMessage;
                if (vm.bypass)
                {
                    vm.bypass = false;
                    msgService.sendMsg(vm);
                    return;
                }
                Bitmap rgb = vm.Bitmap;

                Node[] nds = new Node[NNTrain.numOfinputs];

                SVMTrain.fillFeatures_SURF(rgb, vm.CoM_X, vm.CoM_Y, nds);

                /*
                Bitmap img = new Bitmap(160, Constants.IMAGE_HEIGHT, PixelFormat.Format24bppRgb);
                Graphics g = Graphics.FromImage(img);
                g.Clear(Color.Silver);
                g.DrawImage(rb.Apply(rgb), new Point(16, 6));
                */
                if (this.msgService == null)    // Main'den test amacli cagirdigimiz zaman
                    rgb.Save(Constants.base_folder + "hasat\\zzz\\" + Globals.HARVEST_SIGN_ID + "_surf.bmp");
                else if (Constants.HARVEST_TYPE > Constants.HarvestType.noHarvest)
                    rgb.Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_surf.bmp");

                double[] arr = Prediction.PredictProbability(model, nds);
                int sign_class = (int)Prediction.Predict(model, nds);

                if (arr[0] + arr[1] + arr[2] != 0)
                    Console.WriteLine("");

                if (signType == Constants.SignType.triangular && !Constants.triangles.Contains(sign_class))
                    return;
                if (signType == Constants.SignType.circular && !Constants.circles.Contains(sign_class))
                    return;

                // g.DrawImage(rb.Apply(signs[sign_class]), new Point(16, 150));

                if (Constants.EVALUATE_SUCCESS_ENABLED)
                {
                    if (Globals.SIGN_IN_FRAME.Contains("," + sign_class))
                    {
                        Globals.TRUE_POSITIVES++;
                        Globals.SIGN_IN_FRAME_IS_DETECTED = true;
                        if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestAll || Constants.HARVEST_TYPE == Constants.HarvestType.harvestMisses)
                            signs[sign_class].Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_true.bmp");
                    }
                    else
                    {
                        Globals.FALSE_POSITIVES++;
                        if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestAll || Constants.HARVEST_TYPE == Constants.HarvestType.harvestMisses)
                            signs[sign_class].Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_false.bmp");
                    }
                }

                if (this.msgService == null)    // Main'den test amacli cagirdigimiz zaman
                {
                    signs[sign_class].Save(Constants.base_folder + "hasat\\zzz\\" + Globals.HARVEST_SIGN_ID + "_svm_decide.bmp");
                    return;
                }

                if (Constants.EVALUATE_TIME_ENABLED)
                {
                    int x = (int)(DateTime.Now.Ticks / 10000 - start);
                    if (x >= 0)
                    {
                        Globals.TIME_SVM_MIN = x < Globals.TIME_SVM_MIN ? x : Globals.TIME_SVM_MIN;
                        Globals.TIME_SVM_MAX = x > Globals.TIME_SVM_MAX ? x : Globals.TIME_SVM_MAX;
                        Globals.TIME_SVM_TOTAL += x;
                    }
                }

                // Globals.RIGHT_PANEL_SHOWING_STH = true;
                Bitmap img = signs[sign_class];
                vm = new VisionMessage(img.Height, img.Width, ByteTools.pixelFormatToBPP(img.PixelFormat), img);
                msgService.sendMsg(vm);
            }
        }

        /// <summary>
        /// Sign classification processor uses NN based on grid based features.
        /// </summary>
        public class NN_Processor : MsgConsumer
        {
            Dictionary<int, Network> networks = null;
            static Bitmap[] signs = null; 
            Constants.SignType signType;

            public NN_Processor(MsgService msgService, Constants.SignType signType)
                : base(msgService, VisionMessage.msgType)
            {
                this.signType = signType;
                if (signs == null)
                {
                    signs = new Bitmap[Constants.NUM_OF_SIGN_TYPES + 1];
                    ResizeBicubic resizer = new ResizeBicubic(32, 32);
                    for (int i = 1; i <= Constants.NUM_OF_SIGN_TYPES; i++)
                    {
                        String file_name = "signs\\sign_" + (i < 10 ? ("0" + i) : ("" + i)) + ".bmp";
                        signs[i] = resizer.Apply((Bitmap)Bitmap.FromFile(Constants.base_folder + file_name, false));
                    }
                }
                if (networks == null)
                {
                    networks = NNTrain.loadNetworks();
                }
            }


            public override void consumeMessage(IMessage message, int msgID)
            {
                long start = DateTime.Now.Ticks / 10000;
                VisionMessage vm = message as VisionMessage;
                if (vm.bypass)
                {
                    msgService.sendMsg(vm);
                    return;
                }
                Bitmap rgb = vm.Bitmap; // ByteTools.BytesToBmp(vm.Bitmap, vm.BmpWidth, vm.BmpHeight, ByteTools.bppToPixelFormat(vm.BytePerPixel));

                int numOfinputs = 144; //NNTrain.NUM_MAX_INTEREST_POINTS * NNTrain.NUM_FEATURES;
                double[] input = new double[numOfinputs];

                NNTrain.fillFeatures(rgb, vm.CoM_X, vm.CoM_Y, input);

                SortedDictionary<double, int> outputs = new SortedDictionary<double, int>();
                int[] availSigns = networks.Keys.ToArray();
                foreach (int sign in availSigns)
                {
                     //triangle
                    if (signType == Constants.SignType.triangular && !Constants.triangles.Contains(sign))
                        continue;
                    if (signType == Constants.SignType.circular && !Constants.circles.Contains(sign))
                        continue;
                    outputs.Add(networks[sign].Compute(input)[0], sign);
                }

                double max_value = outputs.Last().Key;
                int sign_class = outputs.Last().Value;
                //Console.WriteLine("mv: " + max_value + " sign:" + sign_class);

                if (max_value > Constants.NN_THRESHOLD)
                {
                    msgService.sendMsg(new TextMessage("Sign:" + sign_class + " Prob:" + max_value.ToString("0.0000")));
                    Bitmap img = signs[sign_class];

                    if (Constants.EVALUATE_SUCCESS_ENABLED)
                    {
                        if (Globals.SIGN_IN_FRAME.Contains("," + sign_class))
                        {
                            Globals.TRUE_POSITIVES++;
                            Console.WriteLine("TRUE:" + sign_class);
                            Globals.SIGN_IN_FRAME_IS_DETECTED = true;
                            if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestAll || Constants.HARVEST_TYPE == Constants.HarvestType.harvestMisses)
                                signs[sign_class].Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_true_" + max_value + ".bmp");
                        }
                        else
                        {
                            Console.WriteLine("FALSE:" + sign_class, "," + Globals.SIGN_IN_FRAME);
                            Globals.FALSE_POSITIVES++;
                            if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestAll || Constants.HARVEST_TYPE == Constants.HarvestType.harvestMisses)
                                signs[sign_class].Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_false_" + max_value + ".bmp");
                        }
                    }
                    vm = new VisionMessage(img.Height, img.Width, ByteTools.pixelFormatToBPP(img.PixelFormat), img);
                    msgService.sendMsg(vm);
                }
                else
                {
                    if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestAll || Constants.HARVEST_TYPE == Constants.HarvestType.harvestMisses)
                        signs[sign_class].Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_nodecide_" + max_value + ".bmp");
                }

                if (this.msgService == null)    // Main'den test amacli cagirdigimiz zaman
                {
                    signs[sign_class].Save(Constants.base_folder + "hasat\\zzz\\" + Globals.HARVEST_SIGN_ID + "_surf_decide_" + max_value + ".bmp");
                    return;
                }

                if (Constants.EVALUATE_TIME_ENABLED)
                {
                    int x = (int)(DateTime.Now.Ticks / 10000 - start);
                    if (x >= 0)
                    {
                        Globals.TIME_SURF_MIN = x < Globals.TIME_SURF_MIN ? x : Globals.TIME_SURF_MIN;
                        Globals.TIME_SURF_MAX = x > Globals.TIME_SURF_MAX ? x : Globals.TIME_SURF_MAX;
                        Globals.TIME_SURF_TOTAL += x;
                    }
                }
            }
        }

        /// <summary>
        /// Sign classification processor uses NN based on SURF features.
        /// </summary>
        public class NN_SURFProcessor : MsgConsumer
        {
            Network network = null;
            static Bitmap[] signs = null;
            ResizeBicubic rb = new ResizeBicubic(128, 128);
            Constants.SignType signType;

            public NN_SURFProcessor(MsgService msgService, Constants.SignType signType)
                : base(msgService, VisionMessage.msgType)
            {
                this.signType = signType;
                if (signs == null)
                {
                    signs = new Bitmap[Constants.NUM_OF_SIGN_TYPES + 1];
                    ResizeBicubic resizer = new ResizeBicubic(32, 32);
                    for (int i = 1; i <= Constants.NUM_OF_SIGN_TYPES; i++)
                    {
                        String file_name = "signs\\sign_" + (i < 10 ? ("0" + i) : ("" + i)) + ".bmp";
                        signs[i] = resizer.Apply((Bitmap)Bitmap.FromFile(Constants.base_folder + file_name, false));
                    }
                }
                if (network == null)
                {
                    network = Network.Load(Constants.base_folder + Constants.NN_SVM_SURF + "_" + (signType == Constants.SignType.circular ? "circle" : "triangle") + ".dat");
                }
            }


            public override void consumeMessage(IMessage message, int msgID)
            {
                long start = DateTime.Now.Ticks / 10000;
                Globals.FRAMES_PROCESSED_NNSURF++;

                VisionMessage vm = message as VisionMessage;
                if (vm.bypass)
                {
                    msgService.sendMsg(vm);
                    return;
                }

                Bitmap rgb = vm.Bitmap;
                double[] input = new double[NNTrain.numOfinputs];


                NNTrain.fillFeatures_SURF(rgb, vm.CoM_X, vm.CoM_Y, input);

                double[] output = network.Compute(input);

                // Bitmap img = new Bitmap(160, Constants.IMAGE_HEIGHT, PixelFormat.Format24bppRgb);
                // Graphics g = Graphics.FromImage(img);
                // g.Clear(Color.Silver);
                // g.DrawImage(rb.Apply(rgb), new Point(16, 6));

                if (this.msgService == null)    // Main'den test amacli cagirdigimiz zaman
                    rgb.Save(Constants.base_folder + "hasat\\zzz\\" + Globals.HARVEST_SIGN_ID + "_surf.bmp");
                else if (Constants.HARVEST_TYPE > Constants.HarvestType.noHarvest)
                    rgb.Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_surf.bmp");

                int sign_class = 0;
                double max_value = Constants.MAX(output, ref sign_class);
                sign_class++;
                if (max_value > Constants.SURF_MATCH_THRESHOLD)        // default 0.9
                {
                    //if (signType == Constants.SignType.triangular && !Constants.triangles.Contains(sign_class))
                    //    return;
                    //if (signType == Constants.SignType.circular && !Constants.circles.Contains(sign_class))
                    //    return;

                    // Globals.RIGHT_PANEL_SHOWING_STH = true;
                    // g.DrawImage(rb.Apply(signs[sign_class]), new Point(16, 150));

                    if (Constants.EVALUATE_SUCCESS_ENABLED)
                    {
                        if (Globals.SIGN_IN_FRAME.Contains("," + sign_class))
                        {
                            Globals.TRUE_POSITIVES++;
                            Globals.SIGN_IN_FRAME_IS_DETECTED = true;
                            if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestAll || Constants.HARVEST_TYPE == Constants.HarvestType.harvestMisses)
                                signs[sign_class].Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_true_" + max_value + ".bmp");
                        }
                        else
                        {
                            Globals.FALSE_POSITIVES++;
                            if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestAll || Constants.HARVEST_TYPE == Constants.HarvestType.harvestMisses)
                                signs[sign_class].Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_false_" + max_value + ".bmp");
                        }
                    }

                    Bitmap img = signs[sign_class];
                    vm = new VisionMessage(img.Height, img.Width, ByteTools.pixelFormatToBPP(img.PixelFormat), img);
                    msgService.sendMsg(vm);
                }
                else
                {
                    if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestAll || Constants.HARVEST_TYPE == Constants.HarvestType.harvestMisses)
                        signs[sign_class].Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_nodecide_" + max_value + ".bmp");
                }

                if (this.msgService == null)    // Main'den test amacli cagirdigimiz zaman
                {
                    signs[sign_class].Save(Constants.base_folder + "hasat\\zzz\\" + Globals.HARVEST_SIGN_ID + "_surf_decide_" + max_value + ".bmp");
                    return;
                }

                if (Constants.EVALUATE_TIME_ENABLED)
                {
                    int x = (int)(DateTime.Now.Ticks / 10000 - start);
                    if (x >= 0)
                    {
                        Globals.TIME_NNSURF_MIN = x < Globals.TIME_NNSURF_MIN ? x : Globals.TIME_NNSURF_MIN;
                        Globals.TIME_NNSURF_MAX = x > Globals.TIME_NNSURF_MAX ? x : Globals.TIME_NNSURF_MAX;
                        Globals.TIME_NNSURF_TOTAL += x;
                    }
                }
            }
        }


    }
}
