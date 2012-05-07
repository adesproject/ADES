using System;
using System.Collections.Generic;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.Net;


using BOUNLib.Processors;
using BOUNLib.Sources;
using BOUNLib.Messages;
using BOUNLib.Messaging;
using BOUNLib.UDPServices;
using BOUNLib;
using BOUNLib.NET.Toolbox;
using BOUNLib.NET.ImageFilters;

using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using SVM;

using AForge.Imaging;
using AForge.Imaging.Filters;
using System.Diagnostics;

namespace ADES
{

    public partial class Main : Form
    {

        public class ADESPanel : IPanelDisplay
        {
            Panel p;

            public ADESPanel(Panel p)
            {
                this.p = p;
            }

            public Graphics getPanelGraphics()
            {
                if (p != null && !p.IsDisposed)
                    return p.CreateGraphics();
                else
                    return null;
            }

            public void setPanelSize(int width, int height)
            {
                if (p!=null && !p.IsDisposed && p.Parent != null && !p.Parent.IsDisposed)
                {
                    ((Main)p.Parent).setPanelSize(p, width, height);
                }
            }

            public void setStatusText(string statusText)
            {
                if (p != null && !p.IsDisposed && p.Parent != null && !p.Parent.IsDisposed)
                {
                    // ((Main)p.Parent).setStatusText(statusText);
                }

            }

        }

        public Main()
        {
            InitializeComponent();
        }


        private MessageFlow mf = null;

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (mf != null) {
                mf.stopFlow();
                mf = null;
            }
            
            ADESPanel p1 = new ADESPanel(panel1);
            ADESPanel p2 = new ADESPanel(panel2);
            ADESPanel p3 = new ADESPanel(panel3);
            ADESPanel p4 = new ADESPanel(panel4);
            ADESPanel p5 = new ADESPanel(panel5);

            mf = new MessageFlow();

            /*
            mf.addConsumer(new PanelDisplayProcessor(p1, new UDPMsgService(new UDPAddress[] { new UDPAddress(serviceIP, ServicePort.PANEL_DISPLAY_SP_1) }, null)));
            mf.addConsumer(new CircularSignDetectorProcessor(new UDPMsgService(new UDPAddress[] { new UDPAddress(serviceIP, ServicePort.SIGN_DETECTION_SP) }, new UDPAddress[] { new UDPAddress(serviceIP, ServicePort.PANEL_DISPLAY_SP_1)})));
            mf.addInitiator(new FileVisionSource("C:\\Documents and Settings\\Administrator\\Desktop\\d.avi", new UDPMsgService(null, new UDPAddress[] { new UDPAddress(serviceIP, ServicePort.SIGN_DETECTION_SP)})));
            */
            
            PanelDisplayProcessor pdp = new PanelDisplayProcessor(p1, new BasicMsgService(null));
            PanelDisplayProcessor pdp2 = new PanelDisplayProcessor(p2, new BasicMsgService(null));
            PanelDisplayProcessor pdp3 = new PanelDisplayProcessor(p3, new BasicMsgService(null));
            PanelDisplayProcessor pdp4 = new PanelDisplayProcessor(p4, new BasicMsgService(null));
            PanelDisplayProcessor pdp5 = new PanelDisplayProcessor(p5, new BasicMsgService(null));


            MsgConsumer cnn = null;
            MsgConsumer tnn = null;
            if ("NN".Equals(Constants.NN_SVM_SURF)) {
                cnn = new NN_Processor(new BasicMsgService(pdp4), Constants.SignType.circular);
                tnn = new NN_Processor(new BasicMsgService(pdp5), Constants.SignType.triangular);
            }
            else if ("NN_SURF".Equals(Constants.NN_SVM_SURF) || "NN_12SIMPLE".Equals(Constants.NN_SVM_SURF))
            {
                cnn = new NN_SURFProcessor(new BasicMsgService(pdp4), Constants.SignType.circular);
                tnn = new NN_SURFProcessor(new BasicMsgService(pdp5), Constants.SignType.triangular);
            }
            else if ("SVM_SURF".Equals(Constants.NN_SVM_SURF) || "SVM_12SIMPLE".Equals(Constants.NN_SVM_SURF))
            {
                cnn = new SVM_SURFProcessor(new BasicMsgService(pdp4), Constants.SignType.circular);
                tnn = new SVM_SURFProcessor(new BasicMsgService(pdp5), Constants.SignType.triangular);
            }
            else if ("SVM".Equals(Constants.NN_SVM_SURF) || "SVM_12SIMPLE".Equals(Constants.NN_SVM_SURF))
            {
                MessageBox.Show("SVM Processor not implemented !");
            }

            CircularSignDetectorProcessor csdp = new CircularSignDetectorProcessor(new BasicMsgService(cnn), p2);
            TriangularSignDetectorProcessor tsdp = new TriangularSignDetectorProcessor(new BasicMsgService(tnn), p3);

            // CircularSignDetectorProcessor csdp = new CircularSignDetectorProcessor(null, p2);
            // TriangularSignDetectorProcessor tsdp = new TriangularSignDetectorProcessor(null, p3);

            LaneDetectorProcessor ldp = new LaneDetectorProcessor(new BasicMsgService(pdp));

            //HistogramProcessor hp = new HistogramProcessor(new BasicMsgService(pdp2));
            //AForgeProcessor afp = new AForgeProcessor(new HSLFiltering(new IntRange(-30, +30), new DoubleRange(0.0, 1.0), new DoubleRange(0.0, 1.0)), new BasicMsgService(pdp2));
            //AutoBrightnessProcessor abp = new AutoBrightnessProcessor(0.5, new Rectangle(256,0,255,287), new BasicMsgService(pdp2));
            //HoughLineProcessor htp = new HoughLineProcessor(new BasicMsgService(pdp));
            //PanelDisplayProcessor pdp = new PanelDisplayProcessor(p1, new BasicMsgService(null));
            //AForgeProcessor afp = new AForgeProcessor(new SpatialColorLabelFilter(new Color[] { Color.White, Color.Red, Color.Blue, Color.Black }), new BasicMsgService(pdp));
            // FileVisionSource fvs = new FileVisionSource("D:\\Kml\\PhD\\misc\\signs.avi", new DispatchMsgService(new MsgConsumer[] {pdp, sdp}));
            //FileVisionSource fvs = new FileVisionSource(@"C:\java\eclipse\workspace\PhD_utils\resimler\avi\all_sign.avi", new DispatchMsgService(new MsgConsumer[] { laneDetectionToolStripMenuItem.Checked ? (MsgConsumer)ldp : (MsgConsumer)pdp, csdp, tsdp }));
            FileVisionSource fvs = new FileVisionSource(@"C:\java\eclipse\workspace\PhD_utils\resimler\avi\all_sign.avi", new DispatchMsgService(new MsgConsumer[] { ldp }));
            // FileVisionSource fvs = new FileVisionSource("D:\\Kml\\PhD\\PhD Thesis\\media\\all_sign.avi", new DispatchMsgService(new MsgConsumer[] { pdp, csdp, tsdp }));
            // DeviceVisionSource dvs = new DeviceVisionSource(new DispatchMsgService(new MsgConsumer[] { pdp, sdp }));

            mf.addInitiator(fvs);
           
            mf.startFlow();
            Globals.TIME_START_MILLISEC = DateTime.Now.Ticks / 10000;
        }


        delegate void SetStatusTextDelegete(string statusText);
        private void SetStatusText(string statusText)
        {
            if (toolStripStatusLabel1.Text != statusText)
                toolStripStatusLabel1.Text = statusText;
        }
        public void setStatusText(string statusText)
        {
            this.BeginInvoke(new SetStatusTextDelegete(SetStatusText), statusText);
        }

        delegate void SetPanelSizeDelegate(Panel p, int width, int height);
        private void SetPanelSize(Panel p, int width, int height)
        {
            p.Width = width;
            p.Height = height;
        }
        public void setPanelSize(Panel p, int width, int height)
        {
            this.BeginInvoke(new SetPanelSizeDelegate(SetPanelSize), p, width, height);
        }

        private void Main_Load(object sender, EventArgs e)
        {
        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (mf != null)
            {
                mf.stopFlow();
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void trainToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            NNTrain nntrain = new NNTrain();
            toolStripStatusLabel1.Text = "Running " +Constants.NN_SVM_SURF+ " Train...";
            Application.DoEvents();
            nntrain.startTrain();
            toolStripStatusLabel1.Text = Constants.NN_SVM_SURF + " Train finished!"; 
        }

        private void generateFeatureArrayToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int[] idx = null;
            toolStripStatusLabel1.Text = "Running LDA...";
            Application.DoEvents();
            idx = new LDA().start();
            MessageBox.Show("Feature array: " + string.Join(",", idx.Select(x => x.ToString()).ToArray()));
            toolStripStatusLabel1.Text = "LDA finished.";
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (!toolStripStatusLabel1.Text.EndsWith("..."))
                toolStripStatusLabel1.Text = "";
        }

        private void statusStrip1_TextChanged(object sender, EventArgs e)
        {
            timer1.Stop();
            timer1.Start();
        }

        private void toolStripButton2_Click_1(object sender, EventArgs e)
        {
            BOUNLib.ToolBox.Logger.log("");
            // RUN_COUNT, SURELER, LUMINANCE_DIVISOR
            BOUNLib.ToolBox.Logger.log("EXCEL: " + Globals.RUN_COUNT + ", " + Constants.NN_SVM_SURF + ", " + Constants.CIRCLE_TRIANGLE + ", " + Constants.GA_POPULATION_SIZE + ", " + Constants.GA_NUMBER_ITERATIONS + ", " + Constants.GA_MUTATION_RATE + ", " + Constants.GA_CROSSOVER_RATE
                 + ", " + ((DateTime.Now.Ticks / 10000 - Globals.TIME_START_MILLISEC) / 1000) + ", " + NNTrain.numOfinputs + ", " + Constants.SURF_MATCH_THRESHOLD + ", " + Globals.TRUE_POSITIVES + ", " + Globals.FALSE_POSITIVES + ", " + Globals.NUMBER_OF_MISSES);

            BOUNLib.ToolBox.Logger.log("TRUE, FALSE, MISS: " + Globals.TRUE_POSITIVES + ", " + Globals.FALSE_POSITIVES + ", " + Globals.NUMBER_OF_MISSES);
            BOUNLib.ToolBox.Logger.log("TOTAL_TIME      : " + ((DateTime.Now.Ticks/10000 - Globals.TIME_START_MILLISEC) / 1000) + " sec");
            if (Globals.FRAMES_PROCESSED_CIRCULAR > 0)
                BOUNLib.ToolBox.Logger.log("FRAMES_PROCESSED_CIRCULAR: " + Globals.FRAMES_PROCESSED_CIRCULAR + "  [" +Globals.TIME_CIRCULAR_MIN+ ", " +Globals.TIME_CIRCULAR_MAX+ ", " +(Globals.TIME_CIRCULAR_TOTAL / Globals.FRAMES_PROCESSED_CIRCULAR)+ "][" +(Globals.TIME_CIRCULAR_TOTAL / 1000)+ "]");
            if (Globals.FRAMES_PROCESSED_TRIANGULAR > 0)
                BOUNLib.ToolBox.Logger.log("FRAMES_PROCESSED_TRIANGULAR: " + Globals.FRAMES_PROCESSED_TRIANGULAR + "  [" + Globals.TIME_TRIANGLE_MIN + ", " + Globals.TIME_TRIANGLE_MAX + ", " + (Globals.TIME_TRIANGLE_TOTAL / Globals.FRAMES_PROCESSED_TRIANGULAR) + "][" + (Globals.TIME_TRIANGLE_TOTAL / 1000) + "]");
            if (Globals.FRAMES_PROCESSED_NN > 0)
                BOUNLib.ToolBox.Logger.log("FRAMES_PROCESSED_NN: " + Globals.FRAMES_PROCESSED_NN + "  [" + Globals.TIME_NN_MIN + ", " + Globals.TIME_NN_MAX + ", " + (Globals.TIME_NN_TOTAL / Globals.FRAMES_PROCESSED_NN) + "][" + (Globals.TIME_NN_TOTAL / 1000) + "]");
            if (Globals.FRAMES_PROCESSED_SVM > 0)
                BOUNLib.ToolBox.Logger.log("FRAMES_PROCESSED_SVM: " + Globals.FRAMES_PROCESSED_SVM + "  [" + Globals.TIME_SVM_MIN + ", " + Globals.TIME_SVM_MAX + ", " + (Globals.TIME_SVM_TOTAL / Globals.FRAMES_PROCESSED_SVM) + "][" + (Globals.TIME_SVM_TOTAL / 1000) + "]");
            if (Globals.FRAMES_PROCESSED_NNSURF > 0)
                BOUNLib.ToolBox.Logger.log("FRAMES_PROCESSED_NNSURF: " + Globals.FRAMES_PROCESSED_NNSURF + "  [" + Globals.TIME_NNSURF_MIN + ", " + Globals.TIME_NNSURF_MAX + ", " + (Globals.TIME_NNSURF_TOTAL / Globals.FRAMES_PROCESSED_NNSURF) + "][" + (Globals.TIME_NNSURF_TOTAL / 1000) + "]");
            if (Globals.FRAMES_PROCESSED_SURF > 0)
                BOUNLib.ToolBox.Logger.log("FRAMES_PROCESSED_SURF: " + Globals.FRAMES_PROCESSED_SURF + "  [" + Globals.TIME_SURF_MIN + ", " + Globals.TIME_SURF_MAX + ", " + (Globals.TIME_SURF_TOTAL / Globals.FRAMES_PROCESSED_SURF) + "][" + (Globals.TIME_SURF_TOTAL / 1000) + "]");
            if (Globals.FRAMES_PROCESSED_PANELDISPLAY > 0)
                BOUNLib.ToolBox.Logger.log("FRAMES_PROCESSED_PANELDISPLAY: " + Globals.FRAMES_PROCESSED_PANELDISPLAY + "  [" + Globals.TIME_PANELDISPLAY_MIN + ", " + Globals.TIME_PANELDISPLAY_MAX + ", " + (Globals.TIME_PANELDISPLAY_TOTAL / Globals.FRAMES_PROCESSED_PANELDISPLAY) + "][" + (Globals.TIME_PANELDISPLAY_TOTAL / 1000) + "]");
            if (Globals.FRAMES_PROCESSED_AUTOBRIGHTNESS > 0)
                BOUNLib.ToolBox.Logger.log("FRAMES_PROCESSED_AUTOBRIGHTNESS: " + Globals.FRAMES_PROCESSED_AUTOBRIGHTNESS + "  [" + Globals.TIME_AUTOBRIGHTNESS_MIN + ", " + Globals.TIME_AUTOBRIGHTNESS_MAX + ", " + (Globals.TIME_AUTOBRIGHTNESS_TOTAL / Globals.FRAMES_PROCESSED_AUTOBRIGHTNESS) + "][" + (Globals.TIME_AUTOBRIGHTNESS_TOTAL / 1000) + "]");
            if (Globals.FRAMES_PROCESSED_FINDCIRCLE > 0)
                BOUNLib.ToolBox.Logger.log("FRAMES_PROCESSED_FINDCIRCLE: " + Globals.FRAMES_PROCESSED_FINDCIRCLE + "  [" + Globals.TIME_FINDCIRCLE_MIN + ", " + Globals.TIME_FINDCIRCLE_MAX + ", " + (Globals.TIME_FINDCIRCLE_TOTAL / Globals.FRAMES_PROCESSED_FINDCIRCLE) + "][" + (Globals.TIME_FINDCIRCLE_TOTAL / 1000) + "]");
            if (Globals.FRAMES_PROCESSED_FINDTRIANGLE > 0)
                BOUNLib.ToolBox.Logger.log("FRAMES_PROCESSED_FINDTRIANGLE: " + Globals.FRAMES_PROCESSED_FINDTRIANGLE + "  [" + Globals.TIME_FINDTRIANGLE_MIN + ", " + Globals.TIME_FINDTRIANGLE_MAX + ", " + (Globals.TIME_FINDTRIANGLE_TOTAL / Globals.FRAMES_PROCESSED_FINDTRIANGLE) + "][" + (Globals.TIME_FINDTRIANGLE_TOTAL / 1000) + "]");
            if (Globals.FRAMES_PROCESSED_GA_EVALUATE > 0)
                BOUNLib.ToolBox.Logger.log("FRAMES_PROCESSED_GA_EVALUATE: " + Globals.FRAMES_PROCESSED_GA_EVALUATE + "  [" + Globals.TIME_GA_EVALUATE_MIN + ", " + Globals.TIME_GA_EVALUATE_MAX + ", " + (Globals.TIME_GA_EVALUATE_TOTAL / Globals.FRAMES_PROCESSED_GA_EVALUATE) + "][" + (Globals.TIME_GA_EVALUATE_TOTAL / 1000) + "]");
            if (Globals.FRAMES_PROCESSED_GA_RUNEPOCH > 0)
                BOUNLib.ToolBox.Logger.log("FRAMES_PROCESSED_GA_RUNEPOCH: " + Globals.FRAMES_PROCESSED_GA_RUNEPOCH + "  [" + Globals.TIME_GA_RUNEPOCH_MIN + ", " + Globals.TIME_GA_RUNEPOCH_MAX + ", " + (Globals.TIME_GA_RUNEPOCH_TOTAL / Globals.FRAMES_PROCESSED_GA_RUNEPOCH) + "][" + (Globals.TIME_GA_RUNEPOCH_TOTAL / 1000) + "]");
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                Globals.HARVEST_SIGN_ID = Convert.ToInt32(textBox1.Text);
                int com_x_sum = 0, com_y_sum = 0, com_x_y_point_count = 0;

                if (false)
                {
                    /*
                    Bitmap bmp_3 = (Bitmap)Bitmap.FromFile(Constants.base_folder + "hasat\\" + textBox1.Text + "_3.bmp");
                    Bitmap bmp_4 = AutoBrightnessProcessor.autoBrightness(bmp_3, Rectangle.Empty, Constants.AUTOBRIGHTNESS_BASE_LUM);
                    int dynamic_gray = 0;
                    Bitmap bmp_4x = new ColorLabelFilter(new Color[] { Color.White, Color.FromArgb(dynamic_gray, dynamic_gray, dynamic_gray) }, true).Apply(bmp_4);
                    bmp_4x.Save(Constants.base_folder + "hasat\\zzz\\bmp_4a.bmp");

                    int y = 0;
                    if ("NN_SURF".Equals(Constants.NN_SVM_SURF))
                    {
                        int start_x = 0, start_y = 0;
                        for (; y < Constants.SIGN_HEIGHT && (start_x + start_y) == 0; y++)
                        {
                            for (int x = 0; x < Constants.SIGN_WIDTH; x++)
                            {
                                Color clr = bmp_4x.GetPixel(x, y);
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
                        Bitmap rgb_flood_fill = filter.Apply(bmp_4x);

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
                            bmp_4x = rgb_flood_fill;
                        }
                        bmp_4x.Save(Constants.base_folder + "hasat\\zzz\\bmp_4x.bmp");
                    }


                    System.Drawing.Imaging.BitmapData image_data = bmp_4x.LockBits(new Rectangle(0, 0, bmp_4x.Width, bmp_4x.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp_4x.PixelFormat);
                    int bpp = 3;
                    int nOffset = image_data.Stride - bmp_4x.Width * bpp;
                    System.IntPtr Scan0 = image_data.Scan0;
                    unsafe
                    {
                        byte* p = (byte*)Scan0;
                        for (y = 0; y < Constants.SIGN_HEIGHT; y++)
                        {
                            // for each pixel
                            for (int x = 0; x < Constants.SIGN_WIDTH; x++, p += bpp)
                            {
                                if (y >= Constants.SIGN_HEIGHT - 14 || y <= 14 || x >= Constants.SIGN_WIDTH - 12 || x <= 12
                                    ||
                                         (x <= Constants.SIGN_WIDTH / 2 && y <= (Constants.SIGN_HEIGHT) - 2 * x + 8)
                                    || (x > Constants.SIGN_WIDTH / 2 && y <= 2 * (x - Constants.SIGN_HEIGHT / 2) + 8)

                                    )
                                {
                                    RGB pixel = new RGB(p[RGB.R], p[RGB.G], p[RGB.B]);
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
                    bmp_4x.UnlockBits(image_data);
                    */
                }


                Bitmap bmp_6 = (Bitmap)Bitmap.FromFile(Constants.base_folder + "hasat\\" + textBox1.Text + "_6.bmp");
                bmp_6.Save(Constants.base_folder + "hasat\\zzz\\" + textBox1.Text + "_6.bmp");

                System.Drawing.Imaging.BitmapData image_data = bmp_6.LockBits(new Rectangle(0, 0, bmp_6.Width, bmp_6.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp_6.PixelFormat);
                int bpp = 3;
                int nOffset = image_data.Stride - bmp_6.Width * bpp;
                System.IntPtr Scan0 = image_data.Scan0;
                unsafe
                {
                    byte* p = (byte*)Scan0;
                    for (int y = 0; y < Constants.SIGN_HEIGHT; y++)
                    {
                        for (int x = 0; x < Constants.SIGN_WIDTH; x++, p += bpp)
                        {
                            if (p[RGB.R] == 0)
                            {
                                com_x_sum += x;
                                com_y_sum += y;
                                com_x_y_point_count++;
                            }
                        }
                        p += nOffset;
                    }
                }
                bmp_6.UnlockBits(image_data);

                // Center of Mass
                int com_x = com_x_sum / (com_x_y_point_count == 0 ? 1 : com_x_y_point_count);
                int com_y = com_y_sum / (com_x_y_point_count == 0 ? 1 : com_x_y_point_count);


                VisionMessage vm = new VisionMessage(bmp_6.Height, bmp_6.Width, BOUNLib.ToolBox.ByteTools.pixelFormatToBPP(bmp_6.PixelFormat), bmp_6);
                vm.CoM_X = com_x;
                vm.CoM_Y = com_y;
                if ("NN_SURF".Equals(Constants.NN_SVM_SURF))
                {
                    NN_SURFProcessor SURF = new NN_SURFProcessor(null, Constants.SignType.triangular);
                    SURF.consumeMessage(vm, 111);
                }
                else if ("SVM_SURF".Equals(Constants.NN_SVM_SURF))
                {
                    SVM_SURFProcessor SURF = new SVM_SURFProcessor(null, Constants.SignType.triangular);
                    SURF.consumeMessage(vm, 111);
                }
            }
        }

        private void trainToolStripMenuItem_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "Running " + Constants.NN_SVM_SURF + " Train...";
            BOUNLib.NET.Toolbox.SVMTrain trn = new BOUNLib.NET.Toolbox.SVMTrain();
            trn.startSurfTrain();
            toolStripStatusLabel1.Text = Constants.NN_SVM_SURF + " Train finished...";
        }

        private void statisticsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process p = new Process();
            p.StartInfo.FileName = "NOTEPAD.EXE";
            p.StartInfo.Arguments = Constants.base_folder + "hasat\\data.txt";
            p.Start();
        }


    }

}


