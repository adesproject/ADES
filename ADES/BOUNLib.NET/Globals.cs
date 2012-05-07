using System;
using System.Collections;
using System.Text;

namespace BOUNLib
{
    /// <summary>
    /// Global options for logging.
    /// </summary>
    public class Globals
    {
        public static int HARVEST_SIGN_ID = 0;


        public static long CURRENT_FRAME_INDEX = 1;
        public static int CURRENT_R_MEAN = -1;
        public static int CURRENT_G_MEAN = -1;
        public static int CURRENT_B_MEAN = -1;

        public static Hashtable FRAME_SIGN_HASH = new Hashtable();
        public static string SIGN_IN_FRAME = Constants.NO_SIGN;
        public static bool SIGN_IN_FRAME_IS_DETECTED = false;
        public static bool RIGHT_PANEL_SHOWING_STH = false;


        public static int RUN_COUNT = 1;
        public static int FRAMES_PROCESSED_CIRCULAR = 0;
        public static int FRAMES_PROCESSED_TRIANGULAR = 0;
        public static int FRAMES_PROCESSED_NN = 0;
        public static int FRAMES_PROCESSED_SVM = 0;
        public static int FRAMES_PROCESSED_NNSURF = 0;
        public static int FRAMES_PROCESSED_SURF = 0;
        public static int FRAMES_PROCESSED_PANELDISPLAY = 0;
        public static int FRAMES_PROCESSED_AUTOBRIGHTNESS = 0;
        public static int FRAMES_PROCESSED_FINDCIRCLE = 0;
        public static int FRAMES_PROCESSED_FINDTRIANGLE = 0;
        public static int FRAMES_PROCESSED_GA_RUNEPOCH = 0;
        public static int FRAMES_PROCESSED_GA_EVALUATE = 0;
        // public static int FRAMES_PROCESSED_GEOTRANSCHROMOSOME = 0;
        public static int NUMBER_OF_MISSES = 0;
        public static int TRUE_POSITIVES = 0;
        public static int FALSE_POSITIVES = 0;


        public static long TIME_START_MILLISEC = 0;
        public static int TIME_CIRCULAR_MIN = 1000000;
        public static int TIME_CIRCULAR_MAX = 0;
        public static int TIME_CIRCULAR_TOTAL = 0;
        public static int TIME_TRIANGLE_MIN = 1000000;
        public static int TIME_TRIANGLE_MAX = 0;
        public static int TIME_TRIANGLE_TOTAL = 0;
        public static int TIME_NNSURF_MIN = 1000000;
        public static int TIME_NNSURF_MAX = 0;
        public static int TIME_NNSURF_TOTAL = 0;

        public static int TIME_SURF_MIN = 1000000;
        public static int TIME_SURF_MAX = 0;
        public static int TIME_SURF_TOTAL = 0;

        public static int TIME_NN_MIN = 1000000;
        public static int TIME_NN_MAX = 0;
        public static int TIME_NN_TOTAL = 0;
        public static int TIME_SVM_MIN = 1000000;
        public static int TIME_SVM_MAX = 0;
        public static int TIME_SVM_TOTAL = 0;

        public static int TIME_PANELDISPLAY_MIN = 1000000;
        public static int TIME_PANELDISPLAY_MAX = 0;
        public static int TIME_PANELDISPLAY_TOTAL = 0;
        public static int TIME_AUTOBRIGHTNESS_MIN = 1000000;
        public static int TIME_AUTOBRIGHTNESS_MAX = 0;
        public static int TIME_AUTOBRIGHTNESS_TOTAL = 0;
        //public static int TIME_GEOTRANSCHROMOSOME_MIN = 1000000;
        //public static int TIME_GEOTRANSCHROMOSOME_MAX = 0;
        //public static int TIME_GEOTRANSCHROMOSOME_TOTAL = 0;
        public static int TIME_FINDCIRCLE_MIN = 1000000;
        public static int TIME_FINDCIRCLE_MAX = 0;
        public static int TIME_FINDCIRCLE_TOTAL = 0;
        public static int TIME_FINDTRIANGLE_MIN = 1000000;
        public static int TIME_FINDTRIANGLE_MAX = 0;
        public static int TIME_FINDTRIANGLE_TOTAL = 0;
        public static int TIME_GA_RUNEPOCH_MIN = 1000000;
        public static int TIME_GA_RUNEPOCH_MAX = 0;
        public static int TIME_GA_RUNEPOCH_TOTAL = 0;
        public static int TIME_GA_EVALUATE_MIN = 1000000;
        public static int TIME_GA_EVALUATE_MAX = 0;
        public static int TIME_GA_EVALUATE_TOTAL = 0;

        public static string SOURCE_NAME = "";
        public static long FRAME_COUNT = 0;
        public static string HARVEST_LOCK = "I am a global lock object";

        /// <summary>
        /// Intializes the options with default values.
        /// </summary>
        public static void init()
        {
            CURRENT_FRAME_INDEX = 1;
            CURRENT_R_MEAN = -1;
            CURRENT_G_MEAN = -1;
            CURRENT_B_MEAN = -1;

            SIGN_IN_FRAME_IS_DETECTED = false;


            FRAMES_PROCESSED_CIRCULAR = 0;
            FRAMES_PROCESSED_TRIANGULAR = 0;
            FRAMES_PROCESSED_NN = 0;
            FRAMES_PROCESSED_SVM = 0;
            FRAMES_PROCESSED_NNSURF = 0;
            FRAMES_PROCESSED_SURF = 0;
            FRAMES_PROCESSED_PANELDISPLAY = 0;
            FRAMES_PROCESSED_AUTOBRIGHTNESS = 0;
            FRAMES_PROCESSED_FINDCIRCLE = 0;
            FRAMES_PROCESSED_FINDTRIANGLE = 0;
            FRAMES_PROCESSED_GA_RUNEPOCH = 0;
            FRAMES_PROCESSED_GA_EVALUATE = 0;
            // FRAMES_PROCESSED_GEOTRANSCHROMOSOME = 0;
            NUMBER_OF_MISSES = 0;
            TRUE_POSITIVES = 0;
            FALSE_POSITIVES = 0;


            TIME_CIRCULAR_MIN = 1000000;
            TIME_CIRCULAR_MAX = 0;
            TIME_CIRCULAR_TOTAL = 0;
            TIME_TRIANGLE_MIN = 1000000;
            TIME_TRIANGLE_MAX = 0;
            TIME_TRIANGLE_TOTAL = 0;
            TIME_NNSURF_MIN = 1000000;
            TIME_NNSURF_MAX = 0;
            TIME_NNSURF_TOTAL = 0;
            TIME_SURF_MIN = 1000000;
            TIME_SURF_MAX = 0;
            TIME_SURF_TOTAL = 0;
            TIME_NN_MIN = 1000000;
            TIME_NN_MAX = 0;
            TIME_NN_TOTAL = 0;
            TIME_SVM_MIN = 1000000;
            TIME_SVM_MAX = 0;
            TIME_SVM_TOTAL = 0;

            TIME_PANELDISPLAY_MIN = 1000000;
            TIME_PANELDISPLAY_MAX = 0;
            TIME_PANELDISPLAY_TOTAL = 0;
            TIME_AUTOBRIGHTNESS_MIN = 1000000;
            TIME_AUTOBRIGHTNESS_MAX = 0;
            TIME_AUTOBRIGHTNESS_TOTAL = 0;

            TIME_FINDCIRCLE_MIN = 1000000;
            TIME_FINDCIRCLE_MAX = 0;
            TIME_FINDCIRCLE_TOTAL = 0;
            TIME_FINDTRIANGLE_MIN = 1000000;
            TIME_FINDTRIANGLE_MAX = 0;
            TIME_FINDTRIANGLE_TOTAL = 0;
            TIME_GA_RUNEPOCH_MIN = 1000000;
            TIME_GA_RUNEPOCH_MAX = 0;
            TIME_GA_RUNEPOCH_TOTAL = 0;
            TIME_GA_EVALUATE_MIN = 1000000;
            TIME_GA_EVALUATE_MAX = 0;
            TIME_GA_EVALUATE_TOTAL = 0;
        }

    }
}
