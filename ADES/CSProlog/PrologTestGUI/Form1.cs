using System;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using Prolog;


namespace PrologTestGUI
{
    public partial class Form1 : Form
    {
        PrologEngine engine = null;
        StreamWriter sw= null;

        public Form1()
        {
            InitializeComponent();
            sw = new StreamWriter("log.txt", true);
            writeLog("------------------------------------------------------------------------------------",true);
            writeLog("------------------------------------ EXECUTION STARTED -----------------------------", true);
            writeLog("------------------------------------------------------------------------------------", true);
        }

        private void writeLog(String logLine)
        {
            writeLog(logLine, false);
        }
        private void writeLog(String logLine, bool onlyFile)
        {
            sw.WriteLine(DateTime.Now.ToLongTimeString() + logLine);
            sw.Flush();
            if (!onlyFile)
            {
                richTextBox1.Text += logLine;
                richTextBox1.ScrollToCaret();
            }
        }

        private void execProlog(String str)
        {

            StringBuilder sb=new StringBuilder();
            engine.SetCurrentOutput(new StringWriter(sb));
            bool result = false;
            if (str != null)
            {
                writeLog("-----> Executing:" + str);
                String query = new String(str.ToCharArray());
                result = engine.ExecuteQuery(ref query);
                if (query == null) return;
            } else {
                result = engine.More();
            }
            writeLog(sb.ToString());
            engine.TryCloseCurrentOutput();
            if (!result)
                writeLog(PrologEngine.NO + "\n");
            else
                writeLog(engine.Answer + "\n");
            toolStripStatusLabel1.Text = "Probability of the result of the last execution: " + engine.resProb;
            button4.Enabled = engine.CanBacktrack(false);

        }

        

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadXml();
            
            engine = new PrologEngine();
            execProlog("\"PROLOG ENGINE INITIALIZED!\".");
            
        }

        private void LoadXml()
        {
            try
            {
                // SECTION 1. Create a DOM Document and load the XML data into it.
                XmlDocument dom = new XmlDocument();
                dom.Load("Actions.xml");

                // SECTION 2. Initialize the TreeView control.
                treeView1.Nodes.Clear();
                treeView1.Nodes.Add(new TreeNode(dom.DocumentElement.Name));
                TreeNode tNode = new TreeNode();
                tNode = treeView1.Nodes[0];

                // SECTION 3. Populate the TreeView with the DOM nodes.
                AddNode(dom.DocumentElement, tNode);
                treeView1.ExpandAll();
            }
            catch (XmlException xmlEx)
            {
                MessageBox.Show(xmlEx.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void AddNode(XmlNode inXmlNode, TreeNode inTreeNode)
        {
            XmlNode xNode;
            TreeNode tNode;
            XmlNodeList nodeList;
            int i;

            // Loop through the XML nodes until the leaf is reached.
            // Add the nodes to the TreeView during the looping process.
            if (inXmlNode.HasChildNodes && inXmlNode.FirstChild.HasChildNodes)
            {
                nodeList = inXmlNode.ChildNodes;
                for (i = 0; i <= nodeList.Count - 1; i++)
                {
                    xNode = inXmlNode.ChildNodes[i];
                    inTreeNode.Nodes.Add(xNode.Name, xNode.Name);
                    tNode = inTreeNode.Nodes[i];
                    AddNode(xNode, tNode);
                }
            }
            else
            {
                // Here you need to pull the data from the XmlNode based on the
                // type of node, whether attribute values are required, and so forth.
                String str = (inXmlNode.InnerText).Trim();
                TreeNode parent = inTreeNode.Parent;
                inTreeNode.Remove();
                parent.Nodes.Add(str, str);
                //inTreeNode.Text = (inXmlNode.InnerText).Trim();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (treeView1.SelectedNode.Nodes.Count == 0 && !listBox1.Items.Contains(treeView1.SelectedNode.Text))
            {
                listBox1.Items.Add(treeView1.SelectedNode.Text);
            }
        }

        private void treeView1_DoubleClick(object sender, EventArgs e)
        {
            button1_Click(sender, e);
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown2.Enabled = checkBox1.Checked;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            execProlog(null);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1) return;
            button4.Enabled = false;
            foreach (string theItem in listBox1.SelectedItems)
            {
                if (treeView1.Nodes.Find("Queries", true)[0].Nodes.Find(theItem, true).Length > 0)
                {
                    execProlog(theItem + ".");
                }
                else if (treeView1.Nodes.Find("Predicates", true)[0].Nodes.Find(theItem, true).Length > 0)
                {
                    execProlog("retractall(" + theItem + ").");
                    execProlog("assert(" + theItem + ").");
                }
                else
                {
                    execProlog("retractall(" + theItem + ").");
                    StringBuilder sb = new StringBuilder("assert(\"");
                    if (treeView1.Nodes.Find("Facts", true)[0].Nodes.Find(theItem, true).Length > 0)
                    {
                        //if (numericUpDown1.Value < 1)
                        sb.Append(numericUpDown1.Value.ToString().Replace(",", ".") + "::");
                        if (checkBox1.Checked)
                        {
                            if (numericUpDown2.Value > 0)
                                sb.Append(numericUpDown2.Value + "::");
                            else
                                sb.Append("T::");
                        }
                    }
                    sb.Append(theItem);
                    sb.Append("\").");
                    execProlog(sb.ToString());
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex == -1) return;
            button4.Enabled = false;
            String[] sa = new String[listBox1.SelectedItems.Count];
            listBox1.SelectedItems.CopyTo(sa,0);
            foreach (string theItem in sa)
            {
                button4.Enabled = false;
                execProlog("retractall(" + theItem + ").");
                listBox1.Items.Remove(theItem);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            execProlog(textBox1.Text);
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == 13)
                button6_Click(sender, null);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("notepad.exe","log.txt");

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            sw.Flush();
            sw.Close();
        }         
    }
}
