using System;
using upisEx;
using System.Diagnostics;

namespace AdesUnrealController
{
    class Unreal
    {
        private const string MAP_NAME = "KK3";
        //private const string APP_PARAMS = " 127.0.0.1?spectatoronly=1?quickstart=true -ini=usarsim.ini";
        //private const string PROCESS_NAME = "UT2004";
        //private const string APP_EXE = PROCESS_NAME + ".exe";
        private const string SERVER_EXE = "ucc.exe";
        private const string SERVER_PARAMS = "server " + MAP_NAME + "?game=USARBot.USARDeathMatch?TimeLimit=0?GameStats=False -ini=USARSim.ini -log=usar_server.log";

        private Upis upis;
        //private Process utc;
        private Process ucc;
        private String appPath;

        public Unreal(String appPath)
        {
            this.appPath = appPath;
        }

        /*
        public void startApp()
        {
            utc = new Process();
            utc.StartInfo.FileName = appPath + APP_EXE;
            utc.StartInfo.Arguments = APP_PARAMS;
            utc.Start();
        }
        */

        public void startUTServer()
        {
            if (ucc != null && !ucc.HasExited)
            {
                ucc.Kill();
            }
            ucc = new Process();
            ucc.StartInfo.FileName = appPath + SERVER_EXE;
            ucc.StartInfo.Arguments = SERVER_PARAMS;
            ucc.Start();
        }


        /*public void closeApp()
        {
            Process[] processes = Process.GetProcessesByName(PROCESS_NAME);
            foreach (Process p in processes)
            {
                Console.WriteLine("Closing Process:" + p.Id);
                p.Kill();
            }
        }*/

        public void startImageServer()
        {
            upis = new Upis();
        }
    }
}