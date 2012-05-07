using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace BOUNLib
{
    /// <summary>
    /// Options for processors.
    /// </summary>
    public class Constants
    {
        public enum LabelingType {
            noLabeling = 0,
            blackAndWhite= 1,
            redBlackWhite = 2        
        }
        
        public enum HarvestType
        {
            noHarvest = 0,
            // harvestLabeled = 1,
            harvestAll = 2,
            harvestMisses = 3,
            harvestIntoFolder = 4
        }

        public enum SignType
        {
            circular = 0,
            triangular = 1
        }

        public static int[] circles = new int[] { 1, 2, 8, 12, 13, 14 };
        public static int[] triangles = new int[] { 3, 4, 5, 6, 7, 9, 10, 11 };

        public static int NUM_OF_SIGN_TYPES = circles.Length + triangles.Length;

		//public static string base_folder = "C:\\caner\\cmpe\\2009\\VS2008\\ADES_PROJ_UTILS\\resimler\\";

        /// <summary>
        /// The base folder definition for training files and system outputs.
        /// </summary>
        public static string base_folder = "C:\\java\\eclipse\\workspace\\phd_utils\\resimler\\";

        

        public static int MAX_TRAIN_SAMPLE = 40;       // Max train samples from each folder

        public static int NUM_MAX_ATTRIBUTES = 1024;    // Number of maximum attributes for each image
        public static int NUM_LDA_ATTRIBUTES = 256;     // Number of maximum attributes for each image

        public static short COLOR_BLACK = 0;
        public static short COLOR_WHITE = 1;
        public static short COLOR_RED = 2;

        /// <summary>
        /// The width of the image
        /// </summary>
        public static short IMAGE_WIDTH = 512;
        /// <summary>
        /// the height of the image
        /// </summary>
        public static short IMAGE_HEIGHT = 288;
       // public static short IMAGE_WIDTH = 640;
       // public static short IMAGE_HEIGHT = 480;
        public static short IMAGE_SKIP_TOP_PIXELS = 192;
        public static short SIGN_WIDTH = 64;
        public static short SIGN_HEIGHT = 64;

        public static double CAPTURE_RESIZE_CIRCLE = 1.0;
        public static double CAPTURE_RESIZE_TRIANGLE = 1.1;



        public static string CIRCLE_TRIANGLE = "triangle";        // circle, triangle
        public static string NN_SVM_SURF = "NN";          // NN, NN_SURF, NN_12SIMPLE, SVM_SURF, SVM_12SIMPLE

        public static double NN_THRESHOLD = 0.9;       // Default 0.9 
        public static int NUM_OF_NN_HIDDEN_LAYER_NODES = 4;

        public static HarvestType HARVEST_TYPE = HarvestType.noHarvest;
        public static LabelingType LABELING_TYPE = LabelingType.blackAndWhite;

        public static int MIN_RADIUS = 7;                        // Min radius for findCircle function
        public static bool EVALUATE_SUCCESS_ENABLED = true;

        // Both variables cannot be true at the same time.
        public static bool LABELING_ENABLED = false;
        public static bool EVALUATE_TIME_ENABLED = true;

        public static bool DIRECTSHOW_TURN_ON_CLOCK = true;


        public static int GA_POPULATION_SIZE = 60;
        public static int GA_NUMBER_ITERATIONS = 2;
        public static double GA_MUTATION_RATE = 0.35;
        public static double GA_CROSSOVER_RATE = 0.75;

        public static double SIGNFILTER_GREEN_START_COEFF = 1.4;
        public static double SIGNFILTER_BLUE_START_COEFF = 1.4;     
        public static double AUTOBRIGHTNESS_BASE_LUM = 0.5;    // Changed from 0.5 to 0.45

        // public static int SURF_TRAIN_INTERVALS = 5;             // Default 5
        // public static int SURF_TRAIN_INIT_SAMPLES = 4;          // Default 4
        // public static float SURF_TRAIN_THRESHOLD = 1f / 1000f;
        // public static int SURF_TRAIN_OCTAVES = 3;               // Default 3
        public static bool SURF_TRAIN_UPRIGHT = true;
        public static double SURF_MATCH_THRESHOLD = 0.9;       // Default 0.9 


        public static double XUG_START = 0;
        public static double XUG_END = Constants.IMAGE_WIDTH;
        public static double YUG_START = 0;
        public static double YUG_END = (Constants.IMAGE_HEIGHT * 2.0) / 3.0;
        public static double WUG_START = Constants.SIGN_WIDTH / 8;   // radius
        public static double WUG_END = Constants.SIGN_WIDTH / 2;   // radius

        public static string NO_SIGN = "-1";


        private static Random rand = new Random();
        public static void init()
        {
            //CIRCLE_TRIANGLE = "triangle";
            // CIRCLE_TRIANGLE = (new string[]{"circle", "triangle"})[rand.Next(0, 2)];
            // GA_POPULATION_SIZE = 20 + rand.Next(0, 11) * 5;     // [20, 25, ..., 65, 70]
            // GA_NUMBER_ITERATIONS = rand.Next(3, 9);             // [3, 4, ..., 8]
            // GA_MUTATION_RATE = 0.01 + Math.Round( ((double)rand.Next(0, 50)) / 100.0, 2);   // [0.01, 0.02, ..., 0.50]
            // GA_CROSSOVER_RATE = 1.00 - Math.Round( ((double)rand.Next(0, 51)) / 100.0, 2);  // [1.00, 0.99, ..., 0.50]

            // LUMINANCE_DIVISOR = 1.0 + Math.Round(((double)rand.Next(0, 9)) * 0.25, 2);     // [1.00, 1.25, ..., 10.0]
            // FITNESS_THRESHOLD = rand.Next(2, 9);     // [2, 3, ..., 8]
        }


        /*
         * Static functions
         */
        /// <summary>
        /// Gets angle with atan(X2-X1/Y2-Y1).
        /// </summary>
        /// <param name="X1"></param>
        /// <param name="Y1"></param>
        /// <param name="X2"></param>
        /// <param name="Y2"></param>
        /// <returns></returns>
        public static float getAngle(int X1, int Y1, int X2, int Y2)
        {
            float X = X2 - X1;
            float Y = Y2 - Y1;
            if (X > 0 && Y <= 0)
                return (float)Math.Atan(-Y / X);

            if (X < 0 && Y <= 0)
                return (float)(Math.PI - Math.Atan(Y / X));

            if (X < 0 && Y >= 0)
                return (float)(Math.PI + Math.Atan(-Y / X));

            if (X > 0 && Y >= 0)
                return (float)(2 * Math.PI - Math.Atan(Y / X));

            return 0;
        }

        /// <summary>
        /// Euclidean distance.
        /// </summary>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <param name="y1"></param>
        /// <param name="y2"></param>
        /// <returns></returns>
        public static double DIST(int x1, int x2, int y1, int y2)
        {
            return Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        }

        /// <summary>
        /// Euclidean distance.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double DIST(Point p1, Point p2)
        {
            return DIST (p1.X, p2.X, p1.Y, p2.Y);
        }

        /// <summary>
        /// Gets MAX of array.
        /// </summary>
        /// <param name="inArr"></param>
        /// <param name="idx">Returns index of the max</param>
        /// <returns></returns>
        public static double MAX(double[] inArr, ref int idx)
        {
            double max = double.MinValue;
            idx = 0;
            for (int i = 0; i < inArr.Length; i++)
            {
                if (inArr[i] > max)
                {
                    max = inArr[i];
                    idx = i;
                }
            }
            return max;
        }

        /// <summary>
        /// Gets MIN of array.
        /// </summary>
        /// <param name="inArr"></param>
        /// <param name="idx">Returns index of the min </param>
        /// <returns></returns>
        public static double MIN(double[] inArr, ref int idx)
        {
            double min = double.MaxValue;
            idx = 0;
            for (int i = 0; i < inArr.Length; i++)
            {
                if (inArr[i] < min)
                {
                    min = inArr[i];
                    idx = i;
                }
            }
            return min;
        }

        /// <summary>
        /// Arranges labeling color.
        /// </summary>
        /// <param name="clr"></param>
        /// <returns></returns>
        public static double getColorValForLabeling(Color clr)
        {
            if (Constants.LABELING_TYPE == Constants.LabelingType.redBlackWhite)
            {
                if (clr.R == 255 && clr.G == 255 && clr.B == 255)
                    return Constants.COLOR_WHITE * 1d;
                else if (clr.R == 255 && clr.G == 0 && clr.B == 0)
                    return Constants.COLOR_RED * 1d;
                else if (clr.R == 0)
                    return Constants.COLOR_BLACK * 1d;
                else
                    return 0d;
            }
            else if (Constants.LABELING_TYPE == Constants.LabelingType.blackAndWhite)
            {
                if (clr.R < 128)
                    return Constants.COLOR_BLACK * 1d;
                else
                    return Constants.COLOR_WHITE * 1d;
            }
            else
            {
                return clr.GetHue() * 1d;
            }
        }


    }
}
