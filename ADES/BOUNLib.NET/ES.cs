using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSBN3Lib;
using Prolog;
using System.Xml;
using System.IO;
using System.Globalization;
using System.Threading;

namespace BOUNLib
{
    namespace ES
    {
        /// <summary>
        /// Expert system interace
        /// </summary>
        public interface ExpertSystems
        {
            /// <summary>
            /// Initializes the expert system
            /// </summary>
            /// <param name="esParams"></param>
            /// <returns></returns>
            string init(params object[] esParams);
            /// <summary>
            /// Fact assertion.
            /// </summary>
            /// <param name="esParams"></param>
            /// <returns></returns>
            string assertFact(params object[] esParams);
            /// <summary>
            /// Fact retrection.
            /// </summary>
            /// <param name="esParams"></param>
            /// <returns></returns>
            string retractFact(params object[] esParams);
            /// <summary>
            /// Violation query feature.
            /// </summary>
            /// <param name="esParams"></param>
            /// <returns></returns>
            string[] query(params object[] esParams);
            /// <summary>
            /// The violation threshold setter for driver aggressivness
            /// </summary>
            /// <param name="threshold"></param>
            void setThreshold(double threshold);
            /// <summary>
            /// Violation threshold getter.
            /// </summary>
            /// <returns></returns>
            double getThreshold();
        }


        /// <summary>
        /// Prolog based ES implementation.
        /// </summary>
        public class PrologES : ExpertSystems
        {
            private PrologEngine engine = null;
            private XmlDocument dom = null;
            private double threshold = 0d;      

            #region ExpertSystems Members

            public string init(params object[] esParams)
            {
                engine = new PrologEngine();
                dom = new XmlDocument();
                dom.Load((string)esParams[0]);
                XmlNodeList nl = dom.SelectNodes("//Predicate");
                foreach (XmlNode n in nl)
                {
                    execProlog("retractall(" + ((XmlElement)n).InnerText + ").");
                    execProlog("assert(" + ((XmlElement)n).InnerText + ").");
                }
                return null;
            }

            public string assertFact(params object[] esParams)
            {
                string fact = (string)esParams[0];
                string prob = (string)esParams[1];
                StringBuilder sb = new StringBuilder();
                sb.Append(execProlog("retractall(" + fact + ")."));
                sb.Append(execProlog("assert(\"" + prob + "::T::" + fact + "\")."));
                return sb.ToString();
            }

            public string retractFact(params object[] esParams)
            {
                string fact = (string)esParams[0];
                StringBuilder sb = new StringBuilder();
                sb.Append(execProlog("retractall(" + fact + ")."));
                return sb.ToString();
            }

            public string[] query(params object[] esParams)
            {
                StringBuilder sb = new StringBuilder();
                XmlNodeList nl = dom.SelectNodes("//Query");
                foreach (XmlNode n in nl)
                {
                    sb.Append(execProlog(((XmlElement)n).InnerText + "."));
                }
                string msg = sb.ToString();

                if (msg.IndexOf("P =") > 0)
                {
                    msg = msg.Substring(msg.IndexOf("P ="));
                    double prob = Double.Parse(msg.Split('\n')[0].Split('=')[1].Trim());
                    string violation = msg.Split('\n')[1].Split('=')[1].Trim();
                    string arguments = msg.Split('\n')[2].Split('=')[1].Trim();
                    return new string[] { "Y", prob.ToString("0.0000"), violation, arguments };
                }
                else
                {
                    return new string[] { "N" };
                }
            }

            public void setThreshold(double threshold)
            {
                this.threshold = threshold;
            }

            public double getThreshold()
            {
                return threshold;
            }

            #endregion

            private string execProlog(String str)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Exec: " + str);
                engine.SetCurrentOutput(new StringWriter(sb));
                bool result = false;
                if (str != null)
                {
                    String query = new String(str.ToCharArray());
                    result = engine.ExecuteQuery(ref query);
                    if (query == null) return sb.ToString();
                }
                else
                {
                    result = engine.More();
                }
                engine.TryCloseCurrentOutput();
                if (!result)
                    sb.Append(PrologEngine.NO + "\n");
                else
                    sb.Append(engine.Answer + "\n");

                Console.WriteLine(sb.ToString());
                return sb.ToString();
            }

        }

        /// <summary>
        /// Belief Networks based ES implementation.
        /// </summary>
        public class BeliefNetworkES : ExpertSystems
        {

            MSBN aMSBN = null;
            private double threshold = 0d;

            #region ExpertSystems Members

            public string init(params object[] esParams)
            {
                aMSBN = new MSBN();
                foreach (string modelFile in esParams)
                {
                    loadModel(modelFile);
                }
                return null;
            }

            public string assertFact(params object[] esParams)
            {
                string modelName = (string)esParams[0];
                string nodeName = (string)esParams[1];
                string state = (string)esParams[2];
                double prob = Double.Parse((string)esParams[3]);
                String s = modelName + ":" + nodeName + assignStateDist(modelName, nodeName, state, prob);
                Console.WriteLine(s);
                return s;
            }

            public string retractFact(params object[] esParams)
            {
                string modelName = (string)esParams[0];
                string nodeName = (string)esParams[1];
                String s = modelName + ":" + nodeName + assignStateDist(modelName, nodeName, "None", 0.9);
                Console.WriteLine(s);
                return s;
            }

            public string[] query(params object[] esParams)
            {

                foreach (Model model in aMSBN.Models)
                {
                    double belief = model.Engine.Belief("Violation", "Yes");
                    Console.WriteLine(model.Name + " belief: " + belief.ToString("0.0000"));
                    if (belief > 0.8) 
                        return new string[] { "Y", belief.ToString("0.0000"), model.Name, ""};
                }
                return new string[] { "N" };
            }

            public void setThreshold(double threshold)
            {
                this.threshold = threshold;
            }

            public double getThreshold()
            {
                return threshold;
            }

            #endregion

            private string assignStateDist(string modelName, string nodeName, string state, double prob)
            {
                Model model = aMSBN.Models[modelName];
                MSBN3Lib.Node theNode = model.ModelNodes[nodeName];
                Dist theDist = theNode.get_Dist();
                Assignment assign2 = (Assignment)theDist.KeyObjects[0];
                foreach (state aState in theNode.get_States())
                {
                    if (aState.Name == state)
                    {
                        theDist[assign2, aState] = prob;
                    }
                    else
                    {
                        theDist[assign2, aState] = (1 - prob) / (theNode.get_States().Count - 1);
                    }
                }
                return theDist.Description;
            }

            private Model loadModel(string modelFile)
            {
                return aMSBN.Models.Add(modelFile.Split('\\')[modelFile.Split('\\').Length - 1].Split('.')[0], modelFile, Directory.GetCurrentDirectory() + "\\NoTurnModelErr.log", INFERENUM.ine_Default, RECOMMENDENUM.recommendtype_Default);
            }

        }

    }
}
