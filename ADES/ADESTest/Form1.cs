using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BOUNLib;
using BOUNLib.Messaging;
using BOUNLib.Processors;
using BOUNLib.Sources;

namespace ADESTest
{
    public partial class Form1 : Form
    {
        private static string imgName;
        MessageFlow mf = null;


        public Form1()
        {
            InitializeComponent();
            Constants.base_folder = Application.StartupPath + "\\media\\";
            Constants.HARVEST_TYPE = Constants.HarvestType.noHarvest;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            //openFileDialog1.InitialDirectory = "c:\\";
            openFileDialog1.Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                imgName = openFileDialog1.FileName;
                Image i = Image.FromFile(imgName);
                pictureBox1.Height = i.Height;
                pictureBox1.Width = i.Width;
                pictureBox1.Image = i;

            }
        }

        public void SignDetectionTextMessageHandler(string msg)
        {
            toolStripStatusLabel1.Text = msg;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = "";
            panel1.CreateGraphics().Clear(Color.Transparent);
            if (mf != null)
            {
                mf.stopFlow();
                mf = null;
            }
            TextMsgHandler csdtmh = new TextMsgHandler(SignDetectionTextMessageHandler);
            ADESPanel p1 = new ADESPanel(panel1);
            TextMsgDisplayProcessor tmdp = new TextMsgDisplayProcessor(new BasicMsgService(null), csdtmh);
            PanelDisplayProcessor pdp = new PanelDisplayProcessor(p1, new BasicMsgService(null));
            MsgConsumer cnn = new NN_Processor(new DispatchMsgService(new MsgConsumer[] { pdp, tmdp }), Constants.SignType.circular);
            //MsgConsumer tnn = new NN_Processor(new DispatchMsgService(new MsgConsumer[] { pdp, tmdp }), Constants.SignType.triangular);
            MsgConsumer csdp = new CircularSignDetectorProcessor(new BasicMsgService(cnn), null);
            //MsgConsumer tsdp = new TriangularSignDetectorProcessor(new BasicMsgService(tnn), null);
            BitmapMemoryVisionSource bmps = new BitmapMemoryVisionSource(new DispatchMsgService(new MsgConsumer[] { csdp/*, tsdp*/ }));

            mf = new MessageFlow();
            mf.addInitiator(bmps);
            mf.startFlow();

            Bitmap bmp = new Bitmap(imgName);
            bmps.sendBmp(bmp);

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

    }
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
            if (p != null && !p.IsDisposed && p.Parent != null && !p.Parent.IsDisposed)
            {
                ((Form1)p.Parent).setPanelSize(p, width, height);
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

}
