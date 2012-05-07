using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;

using BOUNLib.Messages;
using BOUNLib.Messaging;
using BOUNLib.Processors;

namespace BOUNLib
{
    namespace UDPServices
    {

        public enum ServicePort
        {
            NONE = -1,
            HOUGH_TRANSFORMATION_SP = 2000,
            PANEL_DISPLAY_SP_1 = 2001,
            PANEL_DISPLAY_SP_2 = 2002,
            SPLITTER_SP = 2003,
            EDGE_DETECTION_SP = 2004,
            SIGN_DETECTION_SP = 2005
        }
        
        /// <summary>
        /// UDP connection address definition
        /// </summary>
        public class UDPAddress
        {
            private string hostIP;
            public string HostIP
            {
                get { return hostIP; }
                set { hostIP = value; }
            }

            private ServicePort sp;
            public ServicePort Sp
            {
                get { return sp; }
                set { sp = value; }
            }

            public UDPAddress(String hostIP, ServicePort sp)
            {
                HostIP = hostIP;
                Sp = sp;
            }

            public override string ToString()
            {
                 return "UDPAddress Host IP:" + HostIP + " Port:" + Sp;
            }
        }

        /// <summary>
        /// UDP Messaging service.
        /// </summary>
        public class UDPMsgService : MsgService
        {
            private UDPAddress[] local = null;
            private UDPAddress[] remote = null;

            private UDPClient[] udpClients = null;
            private UDPServer[] udpServers = null;

            private int serverCount = 0;
            private int clientCount = 0;
            
            SplitThread[] deliveryThreads =null;

            private class SplitThread
            {
                int index;
                IMessage msgBox;
                UDPClient udpClient;
                Thread innerThread;
                int msgID = 1;
                bool isRunning = true;

                public bool IsRunning
                {
                    get { return isRunning; }
                    set { isRunning = value; }
                }

                public IMessage MsgBox
                {
                    get { return msgBox; }
                    set { msgBox = value; }
                }

                public SplitThread(int index, UDPClient udpClient, int baseMsgID)
                {
                    this.msgID = baseMsgID;
                    this.index = index;
                    this.udpClient = udpClient;
                    innerThread = new Thread(new ThreadStart(distribute));
                    innerThread.Start();
                    Thread.Sleep(5);
                }

                public void distribute()
                {
                    while (isRunning)
                    {
                        if (msgBox != null && msgBox.isValid())
                        {
                            udpClient.sendMessage(msgBox, msgID++);
                            msgBox = null;
                        }
                        Thread.Sleep(5);
                    }
                }
            };
            
            public UDPMsgService(UDPAddress[] local, UDPAddress[] remote)
                : base()
            {
                this.local = local;
                this.remote = remote;

                if (local != null)
                {
                    serverCount = local.Length;
                    udpServers = new UDPServer[serverCount];
                }
                if (remote != null)
                {
                    clientCount = remote.Length;
                    udpClients = new UDPClient[clientCount];
                }
            }

            public override void  init(MsgConsumer consumer)
            {
                for (int i = 0; i < serverCount;i++ )
                {
                    udpServers[i] = new UDPServer(local[i], this);
                }
                for (int i=0; i< clientCount;i++) 
                {
                    udpClients[i] = new UDPClient(remote[i]);
                }
                if (clientCount > 1)
                {
                    deliveryThreads = new SplitThread[clientCount];
                    for (int i=0; i < clientCount; i++)
                    {
                        deliveryThreads[i] = new SplitThread(i, udpClients[i], msgID);
                    }
                }
                base.init(consumer);
            }

            public override void sendMsg(IMessage msg)
            {
                if (clientCount == 1)
                {
                    udpClients[0].sendMessage(msg, msgID);
                }
                else if (clientCount > 1)
                {
                    for (int i = 0; i < clientCount; i++)
                    {
                        if (deliveryThreads[i].MsgBox == null)
                            deliveryThreads[i].MsgBox = new GenericMessage().fromByteArray(msg.toByteArray());
                    }
                }
            }

            public override void Dispose()
            {
                if (deliveryThreads != null)
                {
                    foreach (SplitThread t in deliveryThreads)
                    {
                        t.IsRunning = false;
                    }
                }
                if (udpClients != null)
                {
                    foreach (UDPClient udpClient in udpClients)
                    {
                        if (udpClient != null)
                            udpClient.Dispose();
                    }
                }
                if (udpServers != null)
                {
                    foreach (UDPServer udpServer in udpServers)
                    {
                        if (udpServer != null)
                            udpServer.Dispose();
                    }
                }
                base.Dispose();
            }
        }

        /// <summary>
        /// UDP server imlementation.
        /// </summary>
        class UDPServer:IDisposable
        {

            public const int UDP_MAX_PACKET_SIZE = 65507;

            private UDPMsgService owner = null;
            private Thread serverThread = null;
            private UDPAddress udpAddress = null;

            public UDPServer(UDPAddress udpAddress, UDPMsgService owner)
            {
                try
                {
                    this.owner = owner;
                    this.udpAddress = udpAddress;
                    serverThread = new Thread(new ThreadStart(StartReceive));
                    serverThread.Start();
                    Console.WriteLine("UDPServer thread started for " + udpAddress);
                }
                catch (Exception e)
                {
                    Console.WriteLine("An UDP Exception has occurred!" + e.ToString());
                    serverThread.Abort();
                }
            }

            private Socket soUdp;
            private bool isRunning = true;
            public void StartReceive()
            {
                try
                {
                    //Create a UDP socket.
                    soUdp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    soUdp.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    soUdp.ReceiveTimeout = 2000;
                    IPEndPoint localIpEndPoint = new IPEndPoint(IPAddress.Parse(udpAddress.HostIP), (int)udpAddress.Sp);
                    soUdp.Bind(localIpEndPoint);
                    byte[] received = new Byte[UDP_MAX_PACKET_SIZE];
                    byte[] msgBuffer = null;
                    Dictionary<int, byte[]> messageDict = new Dictionary<int, byte[]>(100);

                    while (isRunning)
                    {
                        EndPoint remoteEP = (EndPoint)(new IPEndPoint(IPAddress.Any, 0));

                        int size = 0;

                        try
                        {
                            if (soUdp!=null && soUdp.IsBound)
                                size = soUdp.ReceiveFrom(received, ref remoteEP);
                        }
                        catch (SocketException seInner)
                        {
                            if (seInner.ErrorCode == (int)SocketError.TimedOut)
                                Console.WriteLine("Socket timedout " + udpAddress);
                            else
                                throw seInner;
                        }
                        if (size > 0)
                        {
                            byte[] data = new byte[size];
                            Buffer.BlockCopy(received, 0, data, 0, size);
                            //Console.WriteLine("received from "+ servicePort+":" + received);
                            UDPMessage udpMsg = new UDPMessage(data);
                            if (!messageDict.ContainsKey(udpMsg.MessageID))
                            { // init buffer
                                msgBuffer = new byte[udpMsg.MessageSize];
                                messageDict.Add(udpMsg.MessageID, msgBuffer);
                            }
                            else
                            {
                                messageDict.TryGetValue(udpMsg.MessageID, out msgBuffer);
                            }
                            Buffer.BlockCopy(udpMsg.Data, 0, msgBuffer, udpMsg.MessageOffset, udpMsg.Data.Length);
                            if (udpMsg.MessageOffset + udpMsg.Data.Length >= udpMsg.MessageSize - 1)
                            {
                                owner.receiveMsg(msgBuffer);
                                messageDict.Remove(udpMsg.MessageID);
                                msgBuffer = null;
                            }
                            //String dataReceived = System.Text.Encoding.ASCII.GetString(received);
                            //Byte[] returningByte = System.Text.Encoding.ASCII.GetBytes(returningString.ToCharArray());
                            //soUdp.SendTo(returningByte, remoteEP);
                        }
                    }
                }
                catch (SocketException se)
                {
                    Console.WriteLine("A Socket Exception has occurred!" + se.ToString());
                }
            }

            public void Dispose()
            {
                if (soUdp != null)
                    soUdp.Close();
                isRunning = false;
            }

        }

        /// <summary>
        /// UDP client implementation.
        /// </summary>
        public class UDPClient : IDisposable
        {
            UDPAddress udpAddress;
            Socket so = null;

            public UDPClient(UDPAddress udpAddress)
            {
                this.udpAddress = udpAddress;
                connect();
            }

            public void connect()
            {
                if (so != null && so.IsBound)
                    so.Disconnect(true);
                try
                {
                    //Create the UDP socket.
                    so = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    so.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 2000);
                    try
                    {
                        //IPHostEntry localHostEntry = Dns.GetHostEntry(Dns.GetHostName());
                        IPEndPoint localIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                        so.Bind(localIpEndPoint);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Local Host not found" + e.ToString()); // fail
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("An Exception Occurred - connect!" + e.ToString());
                }
            }

            public void sendMessage(IMessage msg, int msgID)
            {
                int dataLen = UDPServer.UDP_MAX_PACKET_SIZE - UDPMessage.getHeaderSize();
                byte[] buffer = msg.toByteArray();
                for (int i = 0; i < buffer.Length; i = i + dataLen)
                {
                    UDPMessage udpMsg = null;
                    if (i + dataLen < buffer.Length)
                    {
                        udpMsg = new UDPMessage(dataLen);
                    }
                    else
                    {
                        udpMsg = new UDPMessage(buffer.Length - i);
                    }
                    Buffer.BlockCopy(buffer, i, udpMsg.Data, 0, udpMsg.Data.Length);
                    udpMsg.MessageID = msgID;
                    udpMsg.MessageOffset = i;
                    udpMsg.MessageSize = buffer.Length;
                    sendData(udpMsg);
                }
            }

            public void sendData(UDPMessage udpMessage)
            {
                try
                {
                    //IPHostEntry remoteHostEntry = Dns.GetHostEntry(host);
                    IPEndPoint remoteIpEndPoint = new IPEndPoint(IPAddress.Parse(udpAddress.HostIP), (int)udpAddress.Sp);
                    EndPoint remoteEndPoint = (remoteIpEndPoint);
                    int nBytesSent = so.SendTo(udpMessage.toByteArray(), remoteIpEndPoint);
                }
                catch (Exception e)
                {
                    Console.WriteLine("An Exception Occurred - sendData!" + e.ToString());
                }
            }

            public void Dispose()
            {
                if (so != null && so.IsBound)
                    so.Close();
            }

        }

    }
}
