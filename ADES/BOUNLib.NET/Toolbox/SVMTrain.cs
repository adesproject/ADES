using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Drawing;
using System.Runtime.Serialization.Formatters.Binary;
using SVM;
using System.Text;
using OpenSURF;
using AForge.Imaging;
using AForge.Imaging.Filters;
using BOUNLib.ToolBox;

namespace BOUNLib.NET.Toolbox
{


    /// <summary>
    /// SVM training process.
    /// </summary>
    public class SVMTrain
    {

        public static void fillFeatures_SURF(Bitmap inbmp, int com_x, int com_y, Node[] nds)
        {
            int radius = 16;
            if ("circle".Equals(Constants.CIRCLE_TRIANGLE))
                radius = 25;
            int diameter = 2 * radius + 1;
            List<Ipoint> featureList = null;
            if ("SVM_SURF".Equals(Constants.NN_SVM_SURF))
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
                for (int i = 0; i < NNTrain.surf_array.Length && NNTrain.surf_array[i] != null; i++)
                {
                    start = DateTime.Now.Ticks / 10000;
                    List<Ipoint> featureList_i = NNTrain.surf_array[i].getDescriptors(bmp);
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
                featureList = featureList.GetRange(0, (int)Math.Min(featureList.Count, NNTrain.NUM_MAX_INTEREST_POINTS));

                /*
                 // bmp.Save(Constants.base_folder + "hasat\\zzz\\5_" + inbmp.Tag);
                 Graphics g = Graphics.FromImage(bmp);
                 g.DrawRectangle(Pens.Cyan, com_x-1, com_y-1, 2, 2);
                 SURF.paintSURFPoints(bmp, featureList);
                 bmp.Save(Constants.base_folder + "hasat\\zzz\\6_" + inbmp.Tag);
                */
                int CURRENT_POINT_INDEX = 0;

                for (int xx = 0; xx < nds.Length; xx++)
                {
                    nds[xx] = new Node(xx + 1, 0);
                }

                float com_linear_pos = 128 * com_y + com_x;
                for (int j = 0; j < NNTrain.NUM_MAX_INTEREST_POINTS; j++, CURRENT_POINT_INDEX++)
                {
                    if (CURRENT_POINT_INDEX == featureList.Count)
                        CURRENT_POINT_INDEX = 0;

                    if (featureList.Count == 0)
                    {
                        // nds[j] = new Node(j + 1, 0);
                    }
                    else
                    {
                        float xx = featureList[CURRENT_POINT_INDEX].x;
                        float yy = featureList[CURRENT_POINT_INDEX].y;

                        int y_quantized = (int)Math.Round((yy - com_y) / NNTrain.quantizer);
                        int x_quantized = (int)Math.Round((xx - com_x) / NNTrain.quantizer);
                        y_quantized += 4;
                        x_quantized += 4;
                        int indx = y_quantized * NNTrain.num_surf_quants + x_quantized;
                        if (indx >= 0 && indx < (NNTrain.num_surf_quants * NNTrain.num_surf_quants))
                            nds[indx].Value += 1d / NNTrain.NUM_MAX_INTEREST_POINTS;
                        else
                            Console.Write("");
                    }
                }
            }
            else if ("SVM_12SIMPLE".Equals(Constants.NN_SVM_SURF))
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
                        nds[yy * 12 + xx] = new Node(yy * 12 + xx + 1, 1d - Constants.getColorValForLabeling(inbmp.GetPixel(xx, yy)));
                    }
                }
            }

            //Graphics g = Graphics.FromImage(bmp);
            //g.DrawRectangle(Pens.Cyan, com_x, com_y, 2, 2);
        }

        public void startSurfTrain()
        {
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

            double[] class_labels = new double[trainingFiles.Count];
            Node[][] nodes = new Node[trainingFiles.Count][];

            for (int i = 0; i < trainingFiles.Count; i++)
            {
                Bitmap bmp = (Bitmap)Bitmap.FromFile(trainingFiles[i].FullName, false);

                int com_x_sum = 0, com_y_sum = 0, com_x_y_point_count = 0;
                System.Drawing.Imaging.BitmapData image_data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
                int bpp = 3;
                int nOffset = image_data.Stride - bmp.Width * bpp;
                System.IntPtr Scan0 = image_data.Scan0;
                unsafe
                {
                    byte* p = (byte*)Scan0;
                    for (int y = 0; y < Constants.SIGN_HEIGHT; y++)
                    {
                        for (int x = 0; x < Constants.SIGN_WIDTH; x++, p += bpp)
                        {
                            if (p[2] == 0)
                            {
                                com_x_sum += x;
                                com_y_sum += y;
                                com_x_y_point_count++;
                            }
                        }
                        p += nOffset;
                    }
                }
                bmp.UnlockBits(image_data);
                int com_x = com_x_sum / com_x_y_point_count;
                int com_y = com_y_sum / com_x_y_point_count;


                Node[] nds = new Node[NNTrain.numOfinputs];
                nodes[i] = nds;

                bmp.Tag = trainingFiles[i].Name;
                fillFeatures_SURF(bmp, com_x, com_y, nds);
                class_labels[i] = Double.Parse(trainingFiles[i].Directory.Name);
            }
            Problem problem = new Problem(nodes.Length, class_labels, nodes, NNTrain.numOfinputs + 1);
            // RangeTransform range = Scaling.DetermineRange(problem);
            // problem = Scaling.Scale(problem, range);

            Parameter param = new Parameter();
            param.KernelType = KernelType.POLY;
            // param.KernelType = KernelType.LINEAR;
            // param.KernelType = KernelType.RBF;
            param.SvmType = SvmType.NU_SVC;


            param.C = 2;
            param.Gamma = .5;
            //param.KernelType = KernelType.POLY;

            /* double C, Gamma;
            ParameterSelection.Grid(problem, param, Constants.base_folder + "params_" + type + ".txt", out C, out Gamma);
            param.C = C;
            param.Gamma = Gamma;
            //param.Probability = true;
            */
            Model model = Training.Train(problem, param);

            Stream stream = new FileStream(Constants.base_folder + Constants.NN_SVM_SURF + "_" + Constants.CIRCLE_TRIANGLE + ".dat", FileMode.Create, FileAccess.Write, FileShare.None);
            BinaryFormatter b = new BinaryFormatter();
            b.Serialize(stream, model);
            stream.Close();
        }

    }


}
