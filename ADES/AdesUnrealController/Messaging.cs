using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Globalization;

namespace AdesUnrealController
{
    class Messaging
    {
       

        public static String CMD_INIT = "INIT";
        public static String CMD_SET_CAMERA = "SET {Type Camera}";
        public static String CMD_GETSTARTPOSES = "GETSTARTPOSES";
        public static String CMD_DRIVE = "DRIVE";
        public static String CMD_GETGEO = "GETGEO {Type Sonar}"; 

        public static String MSG_SEN_GT = "SEN.*{Type GroundTruth}";
        public static String MSG_MISSTA_CPT = "MISSTA.*{Name CameraPanTilt}";
        public static String MSG_SEN_RFID = "SEN.*{Type RFID}";
        public static String MSG_STA_GV = "STA.*{Type GroundVehicle}";
        public static String MSG_NFO_SP = "NFO.*{StartPoses.*}";
        public static String MSG_NFO_GT = "NFO.*{Gametype.*}";
        public static String MSG_SEN_GPS = "SEN.*{Type GPS}";
        
        public static string[] messages = new String[] {
            MSG_SEN_GT, 
            MSG_MISSTA_CPT, 
            MSG_STA_GV, 
            MSG_NFO_SP,
            MSG_NFO_GT,
            MSG_SEN_RFID,
            MSG_SEN_GPS
        };

        private Socket serverSocket;
        private MessageParser messageParser;

        public Messaging(String serverHost, String serverPort)
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Parse(serverHost), int.Parse(serverPort));
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Connect(ip);
            messageParser = new MessageParser(serverSocket, this);
        }

        public void sendCommand(String msgStr)
        {
            byte[] msg = Encoding.UTF8.GetBytes(msgStr + "\r\n");
            int i = serverSocket.Send(msg);
            //Console.WriteLine("Sent {0} bytes. {1}", i, msgStr);
        }

        // waitFor must be one of MSG_ constants
        public UnrealMsg sendAndReceive(String cmd, String waitFor)
        {
            UnrealMsg retval = null;
            lock (waitFor)
            {
                sendCommand(cmd);
                Monitor.Wait(waitFor, 3000, true);
                retval = messageParser.getMsgResponse(waitFor);
            }
            return retval;
        }

        public void stopMessaging()
        {
            messageParser.stopThread();
        }

        public UnrealMsg getMsgResponse(String msg)
        {
            return messageParser.getMsgResponse(msg);
        }

        // delegate declaration
        public delegate void UnrealMsgHandler(object sender, UnrealMsgArgs ua);

        // event declaration
        public event UnrealMsgHandler UnrealMsg;

        public void messageParseCallback(long msgNum, string msgType)
        {
            UnrealMsg(this, new UnrealMsgArgs(msgNum, msgType));
        }    
    }

    // custom attributes
    public class UnrealMsgArgs : System.EventArgs
    {

        private long msgNum;
        private string msgType;

        public UnrealMsgArgs(long msgNum, string msgType)
        {
            this.msgNum = msgNum;
            this.msgType = msgType;
        }

        public long getMsgNum()
        {
            return msgNum;
        }

        public String getMsgType() {
            return msgType;
        }

    } 

    class MessageParser
    {
        private Socket threadSocket;
        public bool isStopped = false;
        private Hashtable msgBox = new Hashtable(10);
        Messaging parent;

        public MessageParser(Socket serverSocket, Messaging parent)
        {
            this.threadSocket = serverSocket;
            this.parent = parent;
            Thread messageParserThread = new Thread(RunThread);
            messageParserThread.Start();
        }

        public void stopThread()
        {
            isStopped = true;
        }

        private void RunThread()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            int receivedDataLength = -1;
            long msgNum = 0;
            string msgType= "";
            while (!isStopped)
            {
                byte[] data = new byte[4096*32];
                try
                {
                    threadSocket.Blocking = true;
                    threadSocket.ReceiveTimeout = 1000;
                    while (!isStopped && (receivedDataLength = threadSocket.Receive(data, 0, data.Length, SocketFlags.None)) > 0)
                    {
                        if (receivedDataLength == data.Length)
                        {
                            throw new Exception("MessageParser message length > buffer length ");
                        }

                        String received = Encoding.ASCII.GetString(data, 0, receivedDataLength);
                        //Console.WriteLine("Received:" + received);
                        String[] parts = received.Split(new string[]{"\r\n","\r","\n"},StringSplitOptions.RemoveEmptyEntries);
                        foreach (String part in parts)
                        {
                            bool msgProcessed = false;
                            for (int i = 0; i < Messaging.messages.Length; i++)
                            {
                                if (Regex.IsMatch(part, Messaging.messages[i]))
                                {
                                    msgType = Messaging.messages[i];
                                    //Console.WriteLine("Matched:" + Messaging.messages[i] + " for " + part);
                                    Regex r = new Regex("{([^}]*)}");
                                    MatchCollection mc = r.Matches(part);
                                    UnrealMsg vals = new UnrealMsg(Messaging.messages[i]);
                                    String link = "";
                                    foreach (Match m in mc)
                                    {
                                        String[] pair = m.Groups[1].ToString().Split(new char[] { ' ' });
                                        if (pair[0] == "Link")
                                            link = pair[1];
                                        if (!vals.ContainsKey(pair[0]+link))
                                            vals.Add(pair[0] + link, pair.Length > 0 ? pair[1] : "");
                                        else
                                            Console.WriteLine("Dub " + pair[0] + link + " for " + part);
                                    }
                                    msgProcessed = true;


                                    lock (Messaging.messages[i])
                                    {
                                        if (msgBox.Contains(Messaging.messages[i]))
                                            msgBox.Remove(Messaging.messages[i]);
                                        msgBox.Add(Messaging.messages[i], vals);
                                        Monitor.Pulse(Messaging.messages[i]);
                                    }
                                    break;
                                }
                            }
                            if (!msgProcessed)
                            {
                                Console.WriteLine("Unhandled Msg:" + received);
                            }
                            else
                            {
                                parent.messageParseCallback(msgNum++, msgType);
                            }
                        }
                    }
                }
                catch (SocketException se)
                {
                    if ((SocketError)se.ErrorCode == SocketError.TimedOut)
                    {
                        Console.Write(".");
                    }
                    else
                    {
                        Console.WriteLine("Error occured:" + se.Message);
                        break;
                    }
                }
            }
            Console.WriteLine("MessageParser Thread stopping...");
            threadSocket.Close();
        }

        public UnrealMsg getMsgResponse(String msg)
        {
            if (msg == null || !msgBox.Contains(msg)) return null;
            return (UnrealMsg)msgBox[msg];
        }
    }

    class UnrealMsg:Hashtable
    {
        String msgName;
        public UnrealMsg(String msgName):base(3)
        {
            this.msgName = msgName;
        }

        override public String ToString()
        {
            StringBuilder sb = new StringBuilder("");
            foreach (String key in this.Keys)
            {
                sb.Append(key + ":" + this[key] + " ");
            }
            return sb.ToString();
        }
        
    }
}