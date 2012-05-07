using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace BOUNLib
{
    namespace ToolBox
    {
        /// <summary>
        /// Statistics logger.
        /// </summary>
        public class Logger
        {
            public static void log(string str)
            {
                StreamWriter writer = new StreamWriter(new FileStream(Constants.base_folder + "hasat\\data.txt", FileMode.Append));
                writer.WriteLine(str);
                writer.Close();
            }
        }
    }

}
