using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Collections;
using System.Runtime.Serialization.Formatters.Binary;


namespace BOUNLib.NET.Toolbox
{
    /// <summary>
    /// LDA Calculation from training files.
    /// </summary>
    public class LDA
    {
        public static int[] loadLDA()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter = new BinaryFormatter();
            FileStream stream = new FileStream(Constants.base_folder + "lda_" + Constants.CIRCLE_TRIANGLE + "_01.dat", FileMode.Open, FileAccess.Read, FileShare.None);
            ArrayList l = (ArrayList)formatter.Deserialize(stream);
            return (int[])l.ToArray(new int().GetType());
        }

        public int[] start()
        {
            List<FileInfo> trainingFiles = new List<FileInfo>(300);
            DirectoryInfo di = new DirectoryInfo(Constants.base_folder + "train_" + Constants.CIRCLE_TRIANGLE);
            DirectoryInfo[] dirs = di.GetDirectories("*");
            int class_count = 0;
            foreach (DirectoryInfo dir in dirs)
            {
                //int i = 0;
                FileInfo[] files = dir.GetFiles("*.bmp");
                foreach (FileInfo fi in files)
                {
                    trainingFiles.Add(fi);
                    /*if (i++ > Constants.MAX_TRAIN_SAMPLE)
                        break;*/
                }
                class_count++;
            }
            Console.WriteLine(trainingFiles.Count + " training files found!");

            double[,] xy = new double[trainingFiles.Count, Constants.NUM_MAX_ATTRIBUTES + 1];
            double increment = (double)(Constants.SIGN_WIDTH * Constants.SIGN_HEIGHT) / Constants.NUM_MAX_ATTRIBUTES;
            for (int i = 0; i < trainingFiles.Count; i++)
            {
                Bitmap bmp = (Bitmap)Bitmap.FromFile(trainingFiles[i].FullName, false);
                int j = 0;
                for (j = 0; j < Constants.NUM_MAX_ATTRIBUTES; j++)
                {
                    int y = (int)((increment * j) / Constants.SIGN_WIDTH);
                    int x = (int)((increment * j) - (y * Constants.SIGN_WIDTH));
                    Color clr = bmp.GetPixel(x, y);
                    if (Constants.LABELING_TYPE > Constants.LabelingType.noLabeling)
                    {
                        if (clr.R == 255 && clr.G == 255 && clr.B == 255)
                            xy[i, j] = Constants.COLOR_WHITE * 1d;
                        else if (clr.R == 255 && clr.G == 0 && clr.B == 0)
                            xy[i, j] = Constants.COLOR_RED * 1d;
                        else if (clr.R == 0)
                            xy[i, j] = Constants.COLOR_BLACK * 1d;
                    }
                    else if (Constants.LABELING_TYPE == Constants.LabelingType.blackAndWhite)
                    {
                        if (clr.R == 0)
                            xy[i, j] = Constants.COLOR_BLACK;
                        else
                            xy[i, j] = Constants.COLOR_WHITE;
                    }
                    else
                    {
                        xy[i, j] = clr.GetHue() * 1d;
                    }
                    
                    if (clr.R == 0)
                        xy[i, j] = Constants.COLOR_BLACK;
                    else
                        xy[i, j] = Constants.COLOR_WHITE;
                }
                xy[i, j] = Double.Parse(trainingFiles[i].Directory.Name);
            }

            int info = 0;
            double[] w = null;

            lda.fisherlda(ref xy, trainingFiles.Count, Constants.NUM_MAX_ATTRIBUTES, /*class_count*/ Constants.NUM_OF_SIGN_TYPES + 1, ref info, ref w);
            int[] idx_tmp = new int[Constants.NUM_MAX_ATTRIBUTES];
            for (int i = 0; i < Constants.NUM_MAX_ATTRIBUTES; i++)
                idx_tmp[i] = i;
            Array.Sort<int>(idx_tmp, new Comparison<int>(delegate(int i, int j) { return (w[i].CompareTo(w[j])); }));

            int[] idx = new int[Constants.NUM_LDA_ATTRIBUTES];
            for (int i = 0; i < Constants.NUM_LDA_ATTRIBUTES; i++)
                idx[i] = idx_tmp[i];
            ArrayList l = new ArrayList(idx);
            Stream stream = new FileStream(Constants.base_folder + "lda_" + Constants.CIRCLE_TRIANGLE + "_01.dat", FileMode.Create, FileAccess.Write, FileShare.None);
            BinaryFormatter b = new BinaryFormatter();
            b.Serialize(stream, l);
            stream.Close();


            return idx;
        }
    }
}
