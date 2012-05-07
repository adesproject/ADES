using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace BOUNLib.NET.Toolbox
{
    /// <summary>
    /// File operations.
    /// </summary>
    class FileTools
    {
        /// <summary>
        /// Gets training files for NN and SVM.
        /// </summary>
        /// <param name="class_count"></param>
        /// <returns></returns>
        public static List<FileInfo> getTrainingFiles(ref int class_count)
        {
            List<FileInfo> trainingFiles = new List<FileInfo>(300);
            DirectoryInfo di = new DirectoryInfo(Constants.base_folder + "train");
            DirectoryInfo[] dirs = di.GetDirectories("*");
            foreach (DirectoryInfo dir in dirs)
            {
                FileInfo[] files = dir.GetFiles("*.bmp");
                int i = 0;
                foreach (FileInfo fi in files)
                {
                    if (i++ > Constants.MAX_TRAIN_SAMPLE)
                        break;
                    trainingFiles.Add(fi);
                }
                class_count++;
            }
            Console.WriteLine(trainingFiles.Count + " training files found!");
            return trainingFiles;
        }

        /// <summary>
        /// Writes a binary file to disk.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="o"></param>
        public static void binarySerialize(String filename, object o)
        {
            Stream stream = new FileStream(Constants.base_folder + filename, FileMode.Create, FileAccess.Write, FileShare.None);
            BinaryFormatter b = new BinaryFormatter();
            b.Serialize(stream, o);
            stream.Close();
        }

        /// <summary>
        /// Reads a binary file from disk.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static object binaryDeserialize(String filename)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter = new BinaryFormatter();
            FileStream stream = new FileStream(Constants.base_folder + filename, FileMode.Open, FileAccess.Read, FileShare.None);
            return formatter.Deserialize(stream);
        }

    }
}
