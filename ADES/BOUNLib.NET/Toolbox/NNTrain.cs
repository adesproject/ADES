using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

using AForge.Imaging;
using AForge.Imaging.Filters;

using AForge.Neuro;
using AForge.Neuro.Learning;
using AForge;
using System.Drawing;
using System.IO;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;
using OpenSURF;
using BOUNLib.ToolBox;

namespace BOUNLib.NET.Toolbox
{
    /// <summary>
    /// Neural Network training process.
    /// </summary>
    public class NNTrain
    {
        /* to be used for surf */
        // Heuristic parameters
        public static int INCREMENT = 3;
        public static int WIDTH = 30;
        public static int NUM_MAX_INTEREST_POINTS = 16;     // default 15
        public static SURF[] surf_array = new SURF[] {  
                                                        new SURF(true, 3, 5, 4, 0.000f, 5),
                                                        // new SURF(true, 3, 4, 4, 0.000f, 5)
        };
        /****************************************************************************************/

        public static int num_surf_quants = 9;     // default: 9
        public static int quantizer = 12;           // default: 12 ( num_surf_quants x quantizer < 128 )

        public static int numOfinputs = "NN_SURF".Equals(Constants.NN_SVM_SURF) ? num_surf_quants * num_surf_quants : 12 * 12;
        public static List<Ipoint> fillFeatures(Bitmap bmp, int com_x, int com_y, double[] input_i)
        {
            /*int cropx = Math.Max(com_x - 12, 0);
            int cropy = Math.Max(com_y - 12, 0);
            bmp = new Crop(new Rectangle(cropx, cropy, 24, 24)).Apply(bmp);*/
            bmp = new Crop(new Rectangle(16, 16, 48, 48)).Apply(bmp);
            bmp = new ResizeBicubic(12, 12).Apply(bmp);

            //bmp.Save(Constants.base_folder + "hasat\\zzz\\" + DateTime.Now.Ticks + ".bmp");

            for (int yy = 0; yy < 12; yy++)
            {
                for (int xx = 0; xx < 12; xx++)
                {
                    input_i[yy * 12 + xx] = 1d - Constants.getColorValForLabeling(bmp.GetPixel(xx, yy));
                }
            }

            return null;
        }

        public static List<Ipoint> fillFeatures_SURF(Bitmap inbmp, int com_x, int com_y, double[] input_i)
        {
            int radius = 16;
            if ("circle".Equals(Constants.CIRCLE_TRIANGLE))
                radius = 25;
            int diameter = 2*radius+1;
            List<Ipoint> featureList = null;
            if ("NN_SURF".Equals(Constants.NN_SVM_SURF))
            {
                //inbmp.Save(Constants.base_folder + "hasat\\zzz\\0_" + inbmp.Tag);
                int cropx = Math.Max(com_x - radius, 0);
                int cropy = Math.Max(com_y - radius, 0);

                int x_min = 100, x_max = 0;
                int y_min = 100, y_max = 0;

                Bitmap bmp = new Crop(new Rectangle(cropx, cropy, diameter, diameter)).Apply(inbmp);
                //bmp.Save(Constants.base_folder + "hasat\\zzz\\1_" + inbmp.Tag);

                for (int yy = 0; yy < diameter; yy++)
                {
                    for (int xx = 0; xx < diameter; xx++)
                    {
                        if (bmp.GetPixel(xx, yy).R < 128)
                        {
                            x_min = Math.Min(x_min, xx);
                            y_min = Math.Min(y_min, yy);
                            x_max = Math.Max(x_max, xx);
                            y_max = Math.Max(y_max, yy);
                        }
                    }
                }
                x_min--; y_min--; x_max++; y_max++;
                x_min = Math.Max(x_min, 0);
                y_min = Math.Max(y_min, 0);

                int x_length = x_max - x_min;
                int y_length = y_max - y_min;
                if (x_length > y_length)
                    bmp = new Crop(new Rectangle(x_min, Math.Max((radius - x_length / 2), 0), x_length, x_length)).Apply(bmp);
                else
                    bmp = new Crop(new Rectangle(Math.Max((radius - y_length / 2), 0), y_min, y_length, y_length)).Apply(bmp);

                //bmp.Save(Constants.base_folder + "hasat\\zzz\\2_" + inbmp.Tag);
                bmp = new ResizeBicubic(128, 128).Apply(bmp);
                //bmp.Save(Constants.base_folder + "hasat\\zzz\\3_" + inbmp.Tag);
                bmp = new ImageFilters.ColorLabelFilter(new Color[] { Color.White, Color.Black }, true).Apply(bmp);
                //bmp.Save(Constants.base_folder + "hasat\\zzz\\4_" + inbmp.Tag);

                ByteTools.imageCoM(bmp, ref com_x, ref com_y);

                long start = DateTime.Now.Ticks / 10000;
                Hashtable feat_hash = new Hashtable();
                for (int i = 0; i < surf_array.Length && surf_array[i] != null; i++)
                {
                    start = DateTime.Now.Ticks / 10000;
                    List<Ipoint> featureList_i = surf_array[i].getDescriptors(bmp);
                    // Console.WriteLine("TIME_2_" + i + "[" + featureList_i.Count + "]: " + (DateTime.Now.Ticks / 10000 - start));
                    start = DateTime.Now.Ticks / 10000;
                    if (featureList_i == null) continue;
                    for (int x = 0; x < featureList_i.Count; x++)
                    {
                        if (bmp.GetPixel((int)featureList_i[x].x, (int)featureList_i[x].y).R > 128)
                            continue;
                        if (!feat_hash.ContainsKey(Math.Round(featureList_i[x].x) + "_" + Math.Round(featureList_i[x].y)))
                        {
                            feat_hash.Add(Math.Round(featureList_i[x].x) + "_" + Math.Round(featureList_i[x].y), featureList_i[x]);
                        }
                    }
                }

                featureList = new List<Ipoint>(feat_hash.Values.Count);
                IEnumerator enm = feat_hash.GetEnumerator();
                while (enm.MoveNext())
                    featureList.Add((Ipoint)((DictionaryEntry)enm.Current).Value);
                featureList = featureList.GetRange(0, (int)Math.Min(featureList.Count, NUM_MAX_INTEREST_POINTS));
                
                /*
                 // bmp.Save(Constants.base_folder + "hasat\\zzz\\5_" + inbmp.Tag);
                 Graphics g = Graphics.FromImage(bmp);
                 g.DrawRectangle(Pens.Cyan, com_x-1, com_y-1, 2, 2);
                 SURF.paintSURFPoints(bmp, featureList);
                 bmp.Save(Constants.base_folder + "hasat\\zzz\\6_" + inbmp.Tag);
                */
                int CURRENT_POINT_INDEX = 0;

                float com_linear_pos = 128 * com_y + com_x;
                for (int j = 0; j < NUM_MAX_INTEREST_POINTS; j++, CURRENT_POINT_INDEX++)
                {
                    if (CURRENT_POINT_INDEX == featureList.Count)
                        CURRENT_POINT_INDEX = 0;

                    if (featureList.Count == 0)
                    {
                        // input_i[j] = 0;
                    }
                    else
                    {
                        float xx = featureList[CURRENT_POINT_INDEX].x;
                        float yy = featureList[CURRENT_POINT_INDEX].y;

                        int y_quantized = (int)Math.Round((yy - com_y) / quantizer);
                        int x_quantized = (int)Math.Round((xx - com_x) / quantizer);
                        y_quantized += 4;
                        x_quantized += 4;
                        int indx = y_quantized * num_surf_quants + x_quantized;
                        if (indx >= 0 && indx < (num_surf_quants * num_surf_quants))
                            input_i[indx] += 1d / NUM_MAX_INTEREST_POINTS;
                        else
                            Console.Write("");
                    }
                }
            }
            else if ("NN_12SIMPLE".Equals(Constants.NN_SVM_SURF))
            {
                string filename = (string)inbmp.Tag;
                int cropx = Math.Max(com_x - radius, 0);
                int cropy = Math.Max(com_y - radius, 0);
                
                /*
                Graphics g = Graphics.FromImage(inbmp);
                g.DrawRectangle(Pens.Cyan, com_x-1, com_y-1, 2, 2);
                g.DrawRectangle(Pens.Red, cropx, cropy, diameter, diameter);
                inbmp.Save(Constants.base_folder + "hasat\\zzz\\0_" + filename);
                 */ 
                
                inbmp = new Crop(new Rectangle(cropx, cropy, diameter, diameter)).Apply(inbmp);

                int x_min = 100, x_max = 0;
                int y_min = 100, y_max = 0;
                for (int yy = 0; yy < diameter; yy++)
                {
                    for (int xx = 0; xx < diameter; xx++)
                    {
                        if (inbmp.GetPixel(xx, yy).R < 128)
                        {
                            x_min = Math.Min(x_min, xx);
                            y_min = Math.Min(y_min, yy);
                            x_max = Math.Max(x_max, xx);
                            y_max = Math.Max(y_max, yy);
                        }
                    }
                }
                x_min--; y_min--; x_max++; y_max++;
                x_min = Math.Max(x_min, 0);
                y_min = Math.Max(y_min, 0);

                int x_length = x_max - x_min;
                int y_length = y_max - y_min;
                if (x_length > y_length)
                {
                    inbmp = new Crop(new Rectangle(x_min, Math.Max((radius - x_length / 2), 0), x_length, x_length)).Apply(inbmp);
                }
                else
                {
                    inbmp = new Crop(new Rectangle(Math.Max((radius - y_length / 2), 0), y_min, y_length, y_length)).Apply(inbmp);
                }
                // inbmp.Save(Constants.base_folder + "hasat\\zzz\\8_" + filename);

                inbmp = new ResizeBicubic(12, 12).Apply(inbmp);

                //inbmp.Save(Constants.base_folder + "hasat\\zzz\\9_" + filename);

                for (int yy = 0; yy < 12; yy++)
                {
                    for (int xx = 0; xx < 12; xx++)
                    {
                        input_i[yy * 12 + xx] = 1d - Constants.getColorValForLabeling(inbmp.GetPixel(xx, yy));
                    }
                }
            }

            return featureList;
        }



        public void startTrain()
        {
            int class_count = 0;
            if ("NN".Equals(Constants.NN_SVM_SURF))
            {
                double sigmoidAlphaValue = 1.0;
                double learningRate = 100;
                int max_epoch = 50;
                double min_err = 0.000000001;

                List<FileInfo> trainingFiles = FileTools.getTrainingFiles(ref class_count);
                int samples = trainingFiles.Count;
                // prepare learning data
                double[][] input = new double[samples][];
                Dictionary<int, double[][]> outputs = new Dictionary<int, double[][]>();

                for (int i = 0; i < samples; i++)
                {
                    int currentImageClass = Int32.Parse(trainingFiles[i].Directory.Name);
                    Bitmap bmp = (Bitmap)Bitmap.FromFile(trainingFiles[i].FullName, false);
                    int com_x = 0, com_y = 0;
                    ByteTools.imageCoM(bmp, ref com_x, ref com_y);

                    input[i] = new double[numOfinputs];

                    List<Ipoint> featureList = fillFeatures(bmp, com_x, com_y, input[i]);
                    if (!outputs.ContainsKey(currentImageClass))
                    {
                        outputs.Add(currentImageClass, new double[samples][]);
                        for (int j = 0; j < samples; j++)
                        {
                            outputs[currentImageClass][j] = new double[] { 0d };
                        }
                    }
                    outputs[currentImageClass][i][0] = 1d;
                }

                Dictionary<int, ActivationNetwork> networks = new Dictionary<int, ActivationNetwork>();

                int[] availSigns = outputs.Keys.ToArray();
                foreach (int sign in availSigns)
                {
                    ActivationNetwork network = new ActivationNetwork(new SigmoidFunction(sigmoidAlphaValue),
                        numOfinputs, new int[] { Constants.NUM_OF_NN_HIDDEN_LAYER_NODES, 1 });
                    Accord.Neuro.Learning.LevenbergMarquardtLearning teacher = new Accord.Neuro.Learning.LevenbergMarquardtLearning(network);
                    teacher.LearningRate = learningRate;
                    int epoch = 0;
                    double error;
                    while (true)
                    {
                        // run epoch of learning procedure
                        error = teacher.RunEpoch(input, outputs[sign]) / samples;
                        Console.WriteLine("Epoch:" + epoch + " Error:" + error);
                        if (epoch++ > max_epoch || error < min_err)
                            break;
                    }
                    networks.Add(sign, network);
                    network.Save(Constants.base_folder + "nn_12x12_" + sign + ".dat");
                    Logger.log("Error: " + error + " Epoch:" + epoch);
                }
            }
            else if ("NN_SURF".Equals(Constants.NN_SVM_SURF) || "NN_12SIMPLE".Equals(Constants.NN_SVM_SURF))
            {
                double sigmoidAlphaValue = 1.0;
                if ("NN_SURF".Equals(Constants.NN_SVM_SURF)) {
                    if ("triangle".Equals(Constants.CIRCLE_TRIANGLE))
                        sigmoidAlphaValue = 6.0;
                    if ("circle".Equals(Constants.CIRCLE_TRIANGLE))
                        sigmoidAlphaValue = 6.0;
                }
                else if ("NN_12SIMPLE".Equals(Constants.NN_SVM_SURF))
                {
                    if ("triangle".Equals(Constants.CIRCLE_TRIANGLE))
                        sigmoidAlphaValue = 1.0;
                    if ("circle".Equals(Constants.CIRCLE_TRIANGLE))
                        sigmoidAlphaValue = 1.0;
                }
                double learningRate = 1.00;
                int max_epoch = 3000;
                double min_err = 0.000001;
                
                
                ActivationNetwork network = new ActivationNetwork(new SigmoidFunction(sigmoidAlphaValue),
                    numOfinputs, Constants.NUM_OF_SIGN_TYPES);
                DeltaRuleLearning teacher = new DeltaRuleLearning(network);
                teacher.LearningRate = learningRate;
                
                /*
                ActivationNetwork network = new ActivationNetwork(new SigmoidFunction(sigmoidAlphaValue),
                    numOfinputs, new int[] { (numOfinputs + Constants.NUM_OF_SIGN_TYPES)/2, Constants.NUM_OF_SIGN_TYPES });
                BackPropagationLearning teacher = new BackPropagationLearning(network);
                teacher.LearningRate = learningRate;
                //teacher.Momentum = momentum;
                */


                List<FileInfo> trainingFiles = new List<FileInfo>(1000);
                DirectoryInfo di = new DirectoryInfo(Constants.base_folder + "train_" + Constants.CIRCLE_TRIANGLE);
                DirectoryInfo[] dirs = di.GetDirectories("*");
                foreach (DirectoryInfo dir in dirs)
                {
                    int i = 0;
                    FileInfo[] files = dir.GetFiles("*.bmp");
                    foreach (FileInfo fi in files)
                    {
                        trainingFiles.Add(fi);
                        if (i++ > Constants.MAX_TRAIN_SAMPLE)
                            break;
                    }
                }
                // List<FileInfo> trainingFiles = FileTools.getTrainingFiles(ref class_count);

                int samples = trainingFiles.Count;
                // prepare learning data
                double[][] input = new double[samples][];
                double[][] output = new double[samples][];

                for (int i = 0; i < samples; i++)
                {
                    Bitmap bmp = (Bitmap)Bitmap.FromFile(trainingFiles[i].FullName, false);
                    int com_x = 0, com_y = 0;
                    ByteTools.imageCoM(bmp, ref com_x, ref com_y);

                    input[i] = new double[numOfinputs];
                    output[i] = new double[Constants.NUM_OF_SIGN_TYPES];

                    bmp.Tag = trainingFiles[i].Directory.Name + "_" + trainingFiles[i].Name;
                    fillFeatures_SURF(bmp, com_x, com_y, input[i]);
                    output[i][Int32.Parse(trainingFiles[i].Directory.Name) - 1] = 1d;
                }


                int epoch = 0;
                double error = 0;
                while (true)
                {
                    // run epoch of learning procedure
                    error = teacher.RunEpoch(input, output) / samples;
                    Console.WriteLine("Epoch:" + epoch + " Error:" + error);
                    if (epoch++ > max_epoch || error < min_err)
                        break;
                }
                network.Save(Constants.base_folder + Constants.NN_SVM_SURF + "_" + Constants.CIRCLE_TRIANGLE + ".dat");

                Logger.log("NNTrain [" + error + "]: " + Constants.NN_SVM_SURF + ", " + Constants.CIRCLE_TRIANGLE + ", " + learningRate + ", " + sigmoidAlphaValue);
            }
        }


        public static Dictionary<int, Network> loadNetworks()
            {
            Dictionary<int, Network> networks = new Dictionary<int, Network>();
            DirectoryInfo dir = new DirectoryInfo(Constants.base_folder);
            FileInfo[] files = dir.GetFiles("nn_12x12_*.dat");
                foreach (FileInfo fi in files)
                {
                string[] fileNameParts = fi.Name.Split('_', '.');
                networks.Add(Int32.Parse(fileNameParts[fileNameParts.Length - 2]), Network.Load(fi.OpenRead()));
                }
            return networks;
        }

    }
}
