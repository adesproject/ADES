using System;
using System.Drawing;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using BOUNLib;
using BOUNLib.Messaging;
using BOUNLib.Processors;
using BOUNLib.Sources;
using BOUNLib.ES;
using System.IO;
using System.Drawing.Drawing2D;
using System.Collections;
using Microsoft.DirectX.DirectInput;
using System.Xml;

namespace AdesUnrealController
{
    public partial class AdesDlg : Form
    {

        private Messaging msgSys;
        private ImageParser imageParser;
        private Unreal unreal;
        private Actor actor;
        private MessageFlow mf = null;
        protected BitmapMemoryVisionSource bmvs = null;
        private ExpertSystems expert;
        private static int selectedES = 0;

        private XmlDocument gis = null;

        public void circularSignDetectionTextMessageHandler(string msg)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            toolStripStatusLabel1.Text = msg;
            string signID = msg.Split(" ".ToCharArray())[0].Split(":".ToCharArray())[1];
            string prob = msg.Split(" ".ToCharArray())[1].Split(":".ToCharArray())[1];
            prob = prob.Replace(",", ".");
            string esOut = "";
            switch (signID) {
                case "1":
                    if (selectedES==0)
                        esOut += expert.assertFact("sign_detected(speed_limit,30)", prob);
                    else
                        esOut += expert.assertFact("SpeedLimit", "Sign_Detected", "Limit_30",  prob);
                    break;
                case "13":
                    if (selectedES == 0)
                        esOut += expert.assertFact("sign_detected(no_turn_left)", prob);
                    else
                        esOut += expert.assertFact("NoTurn", "Sign_Detected", "No_Turn_Left", prob);
                    break;
            }
            //Console.WriteLine(esOut);
            queryViolation();
        }

        private void queryViolation()
        {
            string esOut = "";
            if (selectedES == 0)
            {
                esOut += expert.retractFact("velocity_exceeds(X)");
                if (actor.getSpeed() > 90)
                {
                    esOut += expert.assertFact("velocity_exceeds(90)", "1");
                }
                else if (actor.getSpeed() > 50)
                {
                    esOut += expert.assertFact("velocity_exceeds(50)", "1");
                }
                else if (actor.getSpeed() > 30)
                {
                    esOut += expert.assertFact("velocity_exceeds(30)", "1");
                }
            }
            else
            {
                if (actor.getSpeed() > 90)
                {
                    esOut += expert.assertFact("SpeedLimit", "Vehicle_Speed", "Exceed_90", "1");
                }
                else if (actor.getSpeed() > 50)
                {
                    esOut += expert.assertFact("SpeedLimit", "Vehicle_Speed", "Exceed_50", "1");
                }
                else if (actor.getSpeed() > 30)
                {
                    esOut += expert.assertFact("SpeedLimit", "Vehicle_Speed", "Exceed_30", "1");
                }
                else
                {
                    esOut += expert.retractFact("SpeedLimit", "Vehicle_Speed");
                }
            }
           
           
            //Console.WriteLine(esOut);
            this.BeginInvoke(new ExpertWriteDelegate(ExpertWrite), rtbES, esOut);
            long tick = DateTime.Now.Ticks;
            string[] res = expert.query();
            Console.WriteLine("Query time = " + (DateTime.Now.Ticks / 10000 - tick / 10000));
            this.BeginInvoke(new ExpertCommentDelegate(ExpertComment), panel2, lblViolation, res);
        }

        delegate void ExpertWriteDelegate(RichTextBox rtb, string msg);
        private void ExpertWrite(RichTextBox rtb, string msg)
        {
            rtb.Clear();
            rtb.Text = msg;
        }

        delegate void ExpertCommentDelegate(Panel p, Label l, string[] msg);
        private void ExpertComment(Panel p, Label l, string[] msg)
        {
            if (msg[0] == "Y")
            {
                p.BackColor = Color.Red;
                l.Text = "Violation: " + msg[2] + "(" + msg[3]+ ")\n" + "With Prob: " + msg[1];
                if (progressBar1.Value > progressBar1.Minimum)
                {
                    if (lastViolation != msg[2])
                    {
                        if (progressBar1.Value < progressBar1.Maximum)
                            progressBar1.Value += 1;
                        setProgressColor();
                        lastViolation = msg[2];
                    }
                }
            }
            else
            {
                p.BackColor = Color.Green;
                l.Text = "No violation";
            }
        }

        private Panel progressPanel = null;
        private string lastViolation = "";

        private void setProgressColor()
        {
            if (expert != null)
            {
                expert.setThreshold(expert.getThreshold() + 0.05 - progressBar1.Value / (progressBar1.Maximum * 10d));
                toolStripStatusLabel1.Text = "Expert Prob:" + expert.getThreshold().ToString("0.0000");
                if (progressPanel == null)
                {
                    progressPanel = new Panel();
                    progressPanel.Location = progressBar1.Location;
                    progressPanel.Size = progressBar1.Size;
                    progressBar1.Visible = false;
                    this.Controls.Add(progressPanel);
                }
                progressPanel.Width = (int)(progressBar1.Width * progressBar1.Value / (progressBar1.Maximum * 1.0d));
                progressPanel.BackColor = Color.FromArgb((int)(255 * progressBar1.Value / (progressBar1.Maximum * 1.0d)),
                                                         (int)(255 * (progressBar1.Maximum - progressBar1.Value) / (progressBar1.Maximum * 1.0d)),
                                                         0);
            }
        }

        private bool cameraAttached = true;
        private Device keyboard;
        
        public AdesDlg()
        {
            InitializeComponent();
            //create keyboard device.
            keyboard = new Device(SystemGuid.Keyboard);
            if (keyboard == null) throw new Exception("No keyboard found.");
        }

        private void AdesDlg_Load(object sender, EventArgs e)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            cmbES.SelectedIndex = 0;
            serverHost.Text = getLocalIP();
            imgServerHost.Text = getLocalIP();
            unreal = new Unreal(appPath.Text);

            // Init ADES messageFlow for vision
            ADESPanel p1 = new ADESPanel(panel1);
            TextMsgHandler csdtmh = new TextMsgHandler(circularSignDetectionTextMessageHandler);

            mf = new MessageFlow();
            TextMsgDisplayProcessor tmdp = new TextMsgDisplayProcessor(new BasicMsgService(null), csdtmh);
            PanelDisplayProcessor pdp = new PanelDisplayProcessor(p1, new BasicMsgService(null));
            MsgConsumer cnn = new NN_Processor(new DispatchMsgService(new MsgConsumer[]{pdp, tmdp}), Constants.SignType.circular);
            CircularSignDetectorProcessor csdp = new CircularSignDetectorProcessor(new BasicMsgService(cnn), null);
            bmvs = new BitmapMemoryVisionSource(new BasicMsgService(csdp));
            mf.addInitiator(bmvs);
            mf.startFlow();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            closeAll(false);
        }

        private void closeAll(bool closeApp)
        {
            if (unreal != null && closeApp)
            {
                //unreal.closeApp();
            }
            if (msgSys != null)
            {
                msgSys.stopMessaging();
            }
            if (imageParser != null)
            {
                imageParser.stopThread();
            }
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            if (gis == null)
            {
                gis = new XmlDocument();
                gis.Load("GIS.xml");
            }
            if (selectedES == 0)
            {
                expert = new PrologES();
                expert.init("Actions.xml");
                expert.setThreshold(0.9d);
            }
            else
            {
                expert = new BeliefNetworkES();
                expert.init(Directory.GetFiles(Directory.GetCurrentDirectory() + "\\BNModels"));
                expert.setThreshold(0.8d);
            }
            progressBar1.Value = 50;
            setProgressColor();
            try
            {
                closeAll(false);
                imageParser = new ImageParser(imgServerHost.Text, imgPort.Text, pictureBox1, bmvs);
                msgSys = new Messaging(serverHost.Text, serverPort.Text);
                actor = new Actor(msgSys);
                actor.initVehicle();

                // common sensory handler
                msgSys.UnrealMsg += new Messaging.UnrealMsgHandler(this.UnrealCallback); 
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error Occured:" + ex.Message);
                return;
            }
        }

        Image map = null;
        Pen mapPen = new Pen(Color.Red);
        string lastNode = "";
        public void UnrealCallback(object sender, UnrealMsgArgs ua)
        {
            if (ua.getMsgType() == Messaging.MSG_SEN_RFID)
            {
                if (msgSys.getMsgResponse(Messaging.MSG_SEN_RFID) != null &&
                    msgSys.getMsgResponse(Messaging.MSG_SEN_RFID)["Mem"] != null)
                {
                    string rfMem = (string)msgSys.getMsgResponse(Messaging.MSG_SEN_RFID)["Mem"];
                    if (rfMem.IndexOf('_') > 0)
                    {
                        string signId = rfMem.Split('_')[0];
                        string preOrPost = rfMem.Split('_')[1];
                        //Console.WriteLine("S:" + signId + " P:" + preOrPost);

                        string esOut = "";
                        switch (signId)
                        {
                            case "13":
                                if (selectedES == 0)
                                {
                                    if (preOrPost == "0")
                                        esOut += expert.assertFact("rfid_detected(pre,no_turn_left)", "1");
                                    else
                                        esOut += expert.assertFact("rfid_detected(post,no_turn_left)", "1");
                                    break;
                                }
                                else
                                {
                                    if (preOrPost == "0")
                                        esOut += expert.assertFact("NoTurn", "Pre_RFID_Detected", "Pre_No_Turn_Left", "1");
                                    else
                                        esOut += expert.assertFact("NoTurn", "Post_RFID_Detected", "Post_No_Turn_Left", "1");
                                    break;
                                }
                        }
                        //Console.WriteLine(esOut);
                        queryViolation();

                    }
                }
            } 
            else if (ua.getMsgType() == Messaging.MSG_SEN_GPS)
            {
                if (ua.getMsgNum() % 5 == 0) {
                    if (msgSys.getMsgResponse(Messaging.MSG_SEN_GPS) != null &&
                        msgSys.getMsgResponse(Messaging.MSG_SEN_GPS)["Latitude"] != null)
                    {
                        String lat = (string)msgSys.getMsgResponse(Messaging.MSG_SEN_GPS)["Latitude"];
                        String lon = (string)msgSys.getMsgResponse(Messaging.MSG_SEN_GPS)["Longitude"];

                        int yOffset = 160;
                        int xOffset = 60;
                        double yp2gp = 770;
                        double xp2gp = 500;
                        double ns = Double.Parse(lat.Split(new char[] { ',' })[1]);
                        double ew = Double.Parse(lon.Split(new char[] { ',' })[1]);
                        if (!lat.EndsWith("N"))
                        {
                            ns = -ns;
                        }
                        if (!lon.EndsWith("E"))
                        {
                            ew = -ew;
                        }
                        //Console.WriteLine(ns + " !!! " + ew);
                        int x = (int)(ew * xp2gp) + xOffset;
                        int y = yOffset - (int)(ns * yp2gp);
                        Graphics g = pnlMap.CreateGraphics();
                        if (map == null)
                        {
                            map = pnlMap.BackgroundImage;
                        }
                        g.DrawImage(map, 0, 0);
                        g.FillEllipse(mapPen.Brush, x - 4, y - 4, 8, 8);

                        String rules = "";
                        string esOut = "";
                        Double prob = Math.Pow(Double.Parse("0." + lat.Substring(lat.IndexOf('.') + 3, 2)), 2) + Math.Pow(Double.Parse("0." + lon.Substring(lat.IndexOf('.') + 3, 2)), 2);
                        prob = Math.Sqrt(prob);
                        prob = 1 - prob / 10d;
                        string latGIS = lat.Remove(lat.IndexOf('.') + 3, 2);
                        string lonGIS = lon.Remove(lat.IndexOf('.') + 3, 2);
                        if (!lastNode.Equals(latGIS + lonGIS))
                        {
                            lastNode = latGIS + lonGIS;
                            XmlNodeList nl = gis.SelectNodes("//NodeProp[@lat='" + latGIS + "' and @lon='" + lonGIS + "']");
                            if (nl.Count > 0)
                            {
                                foreach (XmlElement elem in nl)
                                {
                                    String rule = elem.GetAttribute("rule");
                                    String type = elem.GetAttribute("type");
                                    rules += rule + ",";
                                    if (rule == "1")
                                    {
                                        if (selectedES == 0)
                                        {
                                            esOut += expert.assertFact("gps_node_property(speed_limit,30)", prob.ToString("0.0000"));
                                        }
                                        else
                                        {
                                            esOut += expert.assertFact("SpeedLimit", "GPS_Node_Property", "Limit_30", prob.ToString("0.0000"));
                                        }
                                    }
                                    else if (rule == "13")
                                    {
                                        if (selectedES == 0)
                                        {
                                            esOut += expert.assertFact("gps_node_property(" + type + ",no_turn_left)", prob.ToString("0.0000"));
                                        }
                                        else
                                        {
                                            if (type == "pre")
                                                esOut += expert.assertFact("NoTurn", "Pre_GPS_Node_Property", "Pre_No_Turn_Left", prob.ToString("0.0000"));
                                            else
                                                esOut += expert.assertFact("NoTurn", "Post_GPS_Node_Property", "Post_No_Turn_Left", prob.ToString("0.0000"));

                                        }
                                    }
                                }
                                queryViolation();
                            }
                            else
                            {
                                if (selectedES == 0)
                                {
                                    esOut += expert.retractFact("gps_node_property(speed_limit,Y)");
                                    esOut += expert.retractFact("gps_node_property(post,Y)");
                                }
                                else
                                {
                                    esOut += expert.retractFact("SpeedLimit", "GPS_Node_Property");
                                    esOut += expert.retractFact("NoTurn", "Post_GPS_Node_Property");
                                }
                                
                            }
                            String rtbText = "GPS INFO:\n\nLatitude:\n" + lat + "\nLongitude:\n" + lon + "\n\nRule:" + rules + "\nProb:" + prob.ToString("0.0000");
                            this.BeginInvoke(new ExpertWriteDelegate(ExpertWrite), rtbGPS, rtbText);
                            //Console.WriteLine(esOut);
                        }
                    }
                }
            }
        }


        private void button2_Click(object sender, EventArgs e)
        {

            msgSys.sendCommand(commandLine.Text);

        }

        private void button3_Click(object sender, EventArgs e)
        {
            //unreal.closeApp();
            unreal.startImageServer();
        }


        private void button4_Click(object sender, EventArgs e)
        {
            if (cameraAttached)
                actor.setCamera(null);
            else
                actor.setCamera(Actor.VEHICLE_CAMERA_1);
            cameraAttached = !cameraAttached;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            unreal.startUTServer();
        }


        private void button6_Click(object sender, EventArgs e)
        {

        }

        // Utility Functions
        public static String getLocalIP()
        {
            IPAddress[] ips = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            foreach (IPAddress ip in ips)
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork) continue;
                return ip.ToString();
            }
            return "";
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
                    ((AdesDlg)p.Parent).setPanelSize(p, width, height);
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

        Image bg = null;
        Pen speedometer = new Pen(Color.Red, 3);
        float scale = 400f / 120f;

        private void drawSpeedometer()
        {
            if (actor != null)
            {
                int speed = actor.getSpeed();
                if (speed >= 0)
                {
                    double angle = speed * 2.4d - 36;
                    float pointX = 200f - 150f * (float)Math.Cos(Math.PI * angle / 180d);
                    float pointY = 200f - 150f * (float)Math.Sin(Math.PI * angle / 180d);
                    Graphics g = pnlSpeed.CreateGraphics();
                    if (bg == null)
                    {
                        bg = pnlSpeed.BackgroundImage;
                    }
                    else
                    {
                        g.DrawImage(bg, 0, 0, pnlSpeed.Width, pnlSpeed.Height);
                    }

                    g.DrawLine(speedometer, 200f / scale, 200f / scale, pointX / scale, pointY / scale);
                }
            }
        }

        private void cmbES_SelectedIndexChanged(object sender, EventArgs e)
        {
            selectedES = cmbES.SelectedIndex;
        }

        private long i = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (i++ % 600 == 0)
            {
                lastViolation = "";
                if (progressBar1.Value > progressBar1.Minimum)
                    progressBar1.Value -= 1;
                setProgressColor();
            }
          
            if (button6.Focused)
            {
                keyboard.Acquire();
                foreach (Key k in keyboard.GetPressedKeys())
                {
                    switch (k)
                    {
                        case Key.W:
                        case Key.Up:
                            actor.increaseSpeed();
                            break;
                        case Key.S:
                        case Key.Down:
                            actor.decreaseSpeed();
                            break;
                        case Key.A:
                        case Key.Left:
                            actor.steerLeft();
                            break;
                        case Key.D:
                        case Key.Right:
                            actor.steerRight();
                            break;
                        default:
                            break;
                    }
                }
            }

            drawSpeedometer();
        }

        private void pnlSpeed_Paint(object sender, PaintEventArgs e)
        {

        }

    }
}
