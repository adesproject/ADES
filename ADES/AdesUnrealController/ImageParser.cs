using System;
using System.Net.Sockets;
using System.Windows.Forms;
using FreeImageAPI;
using System.IO;
using System.Drawing;
using System.Threading;
using System.Net;
using System.Text;
using BOUNLib.Sources;

namespace AdesUnrealController
{
    class ImageParser
    {
        private Socket threadSocket;
        private bool isStopped = false;
        PictureBox pb;
        BitmapMemoryVisionSource bmvs;

        public ImageParser(String serverHost, String serverPort, PictureBox pb, BitmapMemoryVisionSource bmvs)
        {
            IPEndPoint ip = new IPEndPoint(IPAddress.Parse(serverHost), int.Parse(serverPort));
            threadSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            threadSocket.Connect(ip);
            Thread imageParserThread = new Thread(RunThread);
            imageParserThread.Start();
            this.pb = pb;
            this.bmvs = bmvs;
        }

        public void stopThread()
        {
            isStopped = true;
        }

        private void RunThread()
        {
            while (!isStopped)
            {
                byte[] data = new byte[1024 * 1024];
                byte[] chunk = new byte[1024 * 32];
                int receivedDataLength = 0;
                int offset = 0;
                int skip = 5; // 1->imgtype, 4->imglength                
                /*try
                {*/
                    do
                    {
                        threadSocket.Send(Encoding.ASCII.GetBytes("OK"), SocketFlags.None);
                        receivedDataLength = threadSocket.Receive(chunk);
                        if (receivedDataLength == 1)
                        {
                            if (offset > 5)
                            {
                                MemoryStream mStream = new MemoryStream(data, skip, offset - skip);
                                FIBITMAP fib = FreeImage.LoadFromStream(mStream, FREE_IMAGE_LOAD_FLAGS.JPEG_ACCURATE);
                                if (!fib.IsNull)
                                {
                                    Bitmap bmp = FreeImage.GetBitmap(fib);
                                    if (bmp != null && bmvs != null)
                                        bmvs.sendBmp((Bitmap)bmp.Clone()); 
                                    pb.Image = bmp;
                                    FreeImage.Unload(fib);
                                }
                                offset = 0;
                            }
                        }
                        Array.Copy(chunk, 0, data, offset, receivedDataLength);
                        offset += receivedDataLength;
                    } while (receivedDataLength > 0 && !isStopped);

                /*}
                catch (Exception se)
                {
                    Console.WriteLine("ImageParser Error occured:" + se.Message);
                    break;
                }*/
            }
            Console.WriteLine("ImageParser Thread stopping...");
            threadSocket.Close();
        }

    }
}