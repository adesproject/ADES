namespace AdesUnrealController
{
    partial class AdesDlg
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AdesDlg));
            this.serverHost = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.serverPort = new System.Windows.Forms.TextBox();
            this.commandLine = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.appPath = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.imgPort = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.button3 = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.button4 = new System.Windows.Forms.Button();
            this.button5 = new System.Windows.Forms.Button();
            this.button6 = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.label5 = new System.Windows.Forms.Label();
            this.rtbES = new System.Windows.Forms.RichTextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.cmbES = new System.Windows.Forms.ComboBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.lblViolation = new System.Windows.Forms.Label();
            this.pbSafe = new System.Windows.Forms.PictureBox();
            this.pbAggressive = new System.Windows.Forms.PictureBox();
            this.label7 = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.label8 = new System.Windows.Forms.Label();
            this.imgServerHost = new System.Windows.Forms.TextBox();
            this.pnlSpeed = new System.Windows.Forms.Panel();
            this.pnlMap = new System.Windows.Forms.Panel();
            this.rtbGPS = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbSafe)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbAggressive)).BeginInit();
            this.SuspendLayout();
            // 
            // serverHost
            // 
            this.serverHost.Location = new System.Drawing.Point(69, 6);
            this.serverHost.Name = "serverHost";
            this.serverHost.Size = new System.Drawing.Size(76, 20);
            this.serverHost.TabIndex = 0;
            this.serverHost.Text = "127.0.0.1";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 161);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(133, 24);
            this.button1.TabIndex = 1;
            this.button1.Text = "Connect";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(32, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Host:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 35);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(29, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Port:";
            // 
            // serverPort
            // 
            this.serverPort.Location = new System.Drawing.Point(69, 32);
            this.serverPort.Name = "serverPort";
            this.serverPort.Size = new System.Drawing.Size(76, 20);
            this.serverPort.TabIndex = 4;
            this.serverPort.Text = "3000";
            // 
            // commandLine
            // 
            this.commandLine.Location = new System.Drawing.Point(12, 191);
            this.commandLine.Multiline = true;
            this.commandLine.Name = "commandLine";
            this.commandLine.Size = new System.Drawing.Size(133, 36);
            this.commandLine.TabIndex = 7;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(12, 233);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(133, 24);
            this.button2.TabIndex = 5;
            this.button2.Text = "Send Command";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // appPath
            // 
            this.appPath.Location = new System.Drawing.Point(69, 110);
            this.appPath.Name = "appPath";
            this.appPath.Size = new System.Drawing.Size(76, 20);
            this.appPath.TabIndex = 9;
            this.appPath.Text = "C:\\UT2004\\System\\";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 113);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(32, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Path:";
            // 
            // imgPort
            // 
            this.imgPort.Location = new System.Drawing.Point(69, 84);
            this.imgPort.Name = "imgPort";
            this.imgPort.Size = new System.Drawing.Size(76, 20);
            this.imgPort.TabIndex = 13;
            this.imgPort.Text = "5003";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 87);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(49, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "Img Port:";
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(12, 323);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(133, 24);
            this.button3.TabIndex = 14;
            this.button3.Text = "Start Img Server";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(151, 6);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(640, 480);
            this.pictureBox1.TabIndex = 15;
            this.pictureBox1.TabStop = false;
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(12, 263);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(133, 24);
            this.button4.TabIndex = 16;
            this.button4.Text = "Release/Attach Camera";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(12, 293);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(133, 24);
            this.button5.TabIndex = 17;
            this.button5.Text = "Start UT Server";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // button6
            // 
            this.button6.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("button6.BackgroundImage")));
            this.button6.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.button6.ImageAlign = System.Drawing.ContentAlignment.TopLeft;
            this.button6.Location = new System.Drawing.Point(12, 358);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(133, 128);
            this.button6.TabIndex = 18;
            this.button6.Text = "Drive";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click);
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Location = new System.Drawing.Point(800, 6);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(90, 95);
            this.panel1.TabIndex = 19;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 492);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1084, 22);
            this.statusStrip1.TabIndex = 20;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(0, 17);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(797, 113);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(77, 13);
            this.label5.TabIndex = 21;
            this.label5.Text = "Expert System:";
            // 
            // rtbES
            // 
            this.rtbES.Location = new System.Drawing.Point(797, 129);
            this.rtbES.Name = "rtbES";
            this.rtbES.Size = new System.Drawing.Size(272, 87);
            this.rtbES.TabIndex = 22;
            this.rtbES.Text = "";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(12, 138);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(40, 13);
            this.label6.TabIndex = 24;
            this.label6.Text = "Expert:";
            // 
            // cmbES
            // 
            this.cmbES.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbES.FormattingEnabled = true;
            this.cmbES.Items.AddRange(new object[] {
            "Prolog",
            "Belief Networks"});
            this.cmbES.Location = new System.Drawing.Point(69, 135);
            this.cmbES.Name = "cmbES";
            this.cmbES.Size = new System.Drawing.Size(76, 21);
            this.cmbES.TabIndex = 25;
            this.cmbES.SelectedIndexChanged += new System.EventHandler(this.cmbES_SelectedIndexChanged);
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.Green;
            this.panel2.Controls.Add(this.lblViolation);
            this.panel2.Location = new System.Drawing.Point(896, 6);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(176, 95);
            this.panel2.TabIndex = 26;
            // 
            // lblViolation
            // 
            this.lblViolation.AutoSize = true;
            this.lblViolation.Location = new System.Drawing.Point(8, 9);
            this.lblViolation.Name = "lblViolation";
            this.lblViolation.Size = new System.Drawing.Size(0, 13);
            this.lblViolation.TabIndex = 0;
            // 
            // pbSafe
            // 
            this.pbSafe.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pbSafe.BackgroundImage")));
            this.pbSafe.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pbSafe.Location = new System.Drawing.Point(797, 432);
            this.pbSafe.Name = "pbSafe";
            this.pbSafe.Size = new System.Drawing.Size(54, 54);
            this.pbSafe.TabIndex = 28;
            this.pbSafe.TabStop = false;
            // 
            // pbAggressive
            // 
            this.pbAggressive.BackColor = System.Drawing.Color.White;
            this.pbAggressive.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pbAggressive.BackgroundImage")));
            this.pbAggressive.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pbAggressive.Location = new System.Drawing.Point(1018, 432);
            this.pbAggressive.Name = "pbAggressive";
            this.pbAggressive.Size = new System.Drawing.Size(54, 54);
            this.pbAggressive.TabIndex = 29;
            this.pbAggressive.TabStop = false;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(893, 459);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(90, 13);
            this.label7.TabIndex = 30;
            this.label7.Text = "Driver Aggression";
            // 
            // progressBar1
            // 
            this.progressBar1.ForeColor = System.Drawing.Color.Gray;
            this.progressBar1.Location = new System.Drawing.Point(857, 432);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(155, 24);
            this.progressBar1.Step = 1;
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar1.TabIndex = 31;
            this.progressBar1.Value = 50;
            this.progressBar1.Visible = false;
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(12, 61);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(55, 13);
            this.label8.TabIndex = 33;
            this.label8.Text = "Img. Host:";
            // 
            // imgServerHost
            // 
            this.imgServerHost.Location = new System.Drawing.Point(69, 58);
            this.imgServerHost.Name = "imgServerHost";
            this.imgServerHost.Size = new System.Drawing.Size(76, 20);
            this.imgServerHost.TabIndex = 32;
            this.imgServerHost.Text = "127.0.0.1";
            // 
            // pnlSpeed
            // 
            this.pnlSpeed.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pnlSpeed.BackgroundImage")));
            this.pnlSpeed.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pnlSpeed.Location = new System.Drawing.Point(671, 390);
            this.pnlSpeed.Name = "pnlSpeed";
            this.pnlSpeed.Size = new System.Drawing.Size(120, 96);
            this.pnlSpeed.TabIndex = 20;
            this.pnlSpeed.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlSpeed_Paint);
            // 
            // pnlMap
            // 
            this.pnlMap.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("pnlMap.BackgroundImage")));
            this.pnlMap.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pnlMap.Location = new System.Drawing.Point(800, 233);
            this.pnlMap.Name = "pnlMap";
            this.pnlMap.Size = new System.Drawing.Size(166, 178);
            this.pnlMap.TabIndex = 34;
            // 
            // rtbGPS
            // 
            this.rtbGPS.BackColor = System.Drawing.SystemColors.ControlLight;
            this.rtbGPS.Location = new System.Drawing.Point(972, 230);
            this.rtbGPS.Name = "rtbGPS";
            this.rtbGPS.Size = new System.Drawing.Size(100, 181);
            this.rtbGPS.TabIndex = 35;
            this.rtbGPS.Text = "";
            // 
            // AdesDlg
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1084, 514);
            this.Controls.Add(this.rtbGPS);
            this.Controls.Add(this.pnlMap);
            this.Controls.Add(this.pnlSpeed);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.imgServerHost);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.pbAggressive);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.pbSafe);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.cmbES);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.rtbES);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.imgPort);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.appPath);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.commandLine);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.serverPort);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.serverHost);
            this.Name = "AdesDlg";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.AdesDlg_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbSafe)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbAggressive)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox serverHost;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox serverPort;
        private System.Windows.Forms.TextBox commandLine;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox appPath;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox imgPort;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.RichTextBox rtbES;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox cmbES;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label lblViolation;
        private System.Windows.Forms.PictureBox pbSafe;
        private System.Windows.Forms.PictureBox pbAggressive;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox imgServerHost;
        private System.Windows.Forms.Panel pnlSpeed;
        private System.Windows.Forms.Panel pnlMap;
        private System.Windows.Forms.RichTextBox rtbGPS;
    }
}

