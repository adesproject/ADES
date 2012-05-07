using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;

using System.Runtime.InteropServices.ComTypes;

using DirectShowLib;

using BOUNLib.UDPServices;
using BOUNLib.Messages;
using BOUNLib.Messaging;
using BOUNLib.Processors;
using BOUNLib.ToolBox;
using System.Collections;


namespace BOUNLib
{
    namespace Sources
    {

        /// <summary>
        /// Read vision information from video file.
        /// </summary>
        public class FileVisionSource : MsgInitiator, ISampleGrabberCB
        {
            [DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory")]
            private static extern void CopyMemory(IntPtr Destination, IntPtr Source, [MarshalAs(UnmanagedType.U4)] uint Length);

            /// <summary> graph builder interface. </summary>
            private IFilterGraph2 m_FilterGraph = null;
            IMediaControl m_mediaCtrl = null;
            IMediaEvent m_MediaEvent = null;

            /// <summary> Dimensions of the image, calculated once in constructor. </summary>
            public int m_videoWidth;
            public int m_videoHeight;
            private int m_stride;
            public int m_Count = 0;
            VideoInfoHeader videoInfoHeader;

            public FileVisionSource(string FileName, MsgService msgService)
                :base(msgService)
            {
                try
                {
                    Globals.SOURCE_NAME = FileName.Split(Path.DirectorySeparatorChar).Last().Split('.').First();
                    if (Constants.EVALUATE_SUCCESS_ENABLED /*&& Globals.FRAME_SIGN_HASH.Keys.Count==0*/ )
                    {
                        Globals.FRAME_SIGN_HASH = new Hashtable();
                        String line = null;
                        System.IO.StreamReader file = new System.IO.StreamReader(Constants.base_folder + "labels_" + Globals.SOURCE_NAME + ".txt");
                        while ((line = file.ReadLine()) != null)
                        {
                            string[] frameno_signno = line.Split(',');
                            if (frameno_signno.Length < 2)
                            {
                                file.Close();
                                throw new Exception("Invalid label file for " + Globals.SOURCE_NAME);
                            }

                            Globals.FRAME_SIGN_HASH.Add(frameno_signno[0], line);
                            Globals.FRAME_COUNT = long.Parse(line.Split(',').First())-1;
                        }
                        file.Close();
                    }

                    // Set up the capture graph
                    Configure(FileName);
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            private void Configure(string FileName) {
                int hr;

                ISampleGrabber sampGrabber = null;
                IBaseFilter baseGrabFlt = null;
                IBaseFilter capFilter = null;
                IBaseFilter nullrenderer = null;

                // Get the graphbuilder object
                m_FilterGraph = new FilterGraph() as IFilterGraph2;
                m_mediaCtrl = m_FilterGraph as IMediaControl;
                m_MediaEvent = m_FilterGraph as IMediaEvent;

                IMediaFilter mediaFilt = m_FilterGraph as IMediaFilter;

                try
                {
                    // Add the video source
                    hr = m_FilterGraph.AddSourceFilter(FileName, "Ds.NET FileFilter", out capFilter);
                    DsError.ThrowExceptionForHR(hr);

                    // Get the SampleGrabber interface
                    sampGrabber = new SampleGrabber() as ISampleGrabber;
                    baseGrabFlt = sampGrabber as IBaseFilter;

                    ConfigureSampleGrabber(sampGrabber);

                    // Add the frame grabber to the graph
                    hr = m_FilterGraph.AddFilter(baseGrabFlt, "Ds.NET Grabber");
                    DsError.ThrowExceptionForHR(hr);

                    // ---------------------------------
                    // Connect the file filter to the sample grabber

                    // Hopefully this will be the video pin, we could check by reading it's mediatype
                    IPin iPinOut = DsFindPin.ByDirection(capFilter, PinDirection.Output, 0);

                    // Get the input pin from the sample grabber
                    IPin iPinIn = DsFindPin.ByDirection(baseGrabFlt, PinDirection.Input, 0);

                    hr = m_FilterGraph.Connect(iPinOut, iPinIn);
                    DsError.ThrowExceptionForHR(hr);

                    // Add the null renderer to the graph
                    nullrenderer = new NullRenderer() as IBaseFilter;
                    hr = m_FilterGraph.AddFilter(nullrenderer, "Null renderer");
                    DsError.ThrowExceptionForHR(hr);

                    // ---------------------------------
                    // Connect the sample grabber to the null renderer

                    iPinOut = DsFindPin.ByDirection(baseGrabFlt, PinDirection.Output, 0);
                    iPinIn = DsFindPin.ByDirection(nullrenderer, PinDirection.Input, 0);

                    hr = m_FilterGraph.Connect(iPinOut, iPinIn);
                    DsError.ThrowExceptionForHR(hr);

                    if (!Constants.DIRECTSHOW_TURN_ON_CLOCK)
                    {
                        // Turn off the clock.  This causes the frames to be sent
                        // thru the graph as fast as possible
                        hr = mediaFilt.SetSyncSource(null);
                        DsError.ThrowExceptionForHR(hr);
                    }

                    // Read and cache the image sizes
                    SaveSizeInfo(sampGrabber);
                }
                finally
                {
                    if (capFilter != null)
                    {
                        Marshal.ReleaseComObject(capFilter);
                        capFilter = null;
                    }
                    if (sampGrabber != null)
                    {
                        Marshal.ReleaseComObject(sampGrabber);
                        sampGrabber = null;
                    }
                    if (nullrenderer != null)
                    {
                        Marshal.ReleaseComObject(nullrenderer);
                        nullrenderer = null;
                    }
                }
            }

            /// <summary> Set the options on the sample grabber </summary>
            private void ConfigureSampleGrabber(ISampleGrabber sampGrabber)
            {
                AMMediaType media;
                int hr;

                // Set the media type to Video/RBG24
                media = new AMMediaType();
                media.majorType = MediaType.Video;
                media.subType = MediaSubType.RGB24;
                media.formatType = FormatType.VideoInfo;
                hr = sampGrabber.SetMediaType(media);
                DsError.ThrowExceptionForHR(hr);

                DsUtils.FreeAMMediaType(media);
                media = null;

                // Choose to call BufferCB instead of SampleCB
                hr = sampGrabber.SetCallback(this, 1);
                DsError.ThrowExceptionForHR(hr);
            }

            /// <summary> Read and store the properties </summary>
            private void SaveSizeInfo(ISampleGrabber sampGrabber)
            {
                int hr;

                // Get the media type from the SampleGrabber
                AMMediaType media = new AMMediaType();
                hr = sampGrabber.GetConnectedMediaType(media);
                DsError.ThrowExceptionForHR(hr);

                if ((media.formatType != FormatType.VideoInfo) || (media.formatPtr == IntPtr.Zero))
                {
                    throw new NotSupportedException("Unknown Grabber Media Format");
                }

                // Grab the size info
                videoInfoHeader = (VideoInfoHeader)Marshal.PtrToStructure(media.formatPtr, typeof(VideoInfoHeader));
                m_videoWidth = videoInfoHeader.BmiHeader.Width;
                m_videoHeight = videoInfoHeader.BmiHeader.Height;
                m_stride = m_videoWidth * (videoInfoHeader.BmiHeader.BitCount / 8);

                DsUtils.FreeAMMediaType(media);
                media = null;
            }

            /// <summary> release everything. </summary>
            public override void Dispose()
            {
                CloseInterfaces();
            }
            
            // Destructor
            ~FileVisionSource()
            {
                CloseInterfaces();
            }

            /// <summary> Shut down capture </summary>
            private void CloseInterfaces()
            {
                int hr;
                try
                {
                    if (m_mediaCtrl != null)
                    {
                        hr = m_mediaCtrl.Stop();
                        m_mediaCtrl = null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                if (m_FilterGraph != null)
                {
                    Marshal.ReleaseComObject(m_FilterGraph);
                    m_FilterGraph = null;
                }
                GC.Collect();
            }

            override public void start()
            {
                int hr = m_mediaCtrl.Run();
                DsError.ThrowExceptionForHR(hr);
            }

            override public void stop()
            {
                int hr = m_mediaCtrl.Pause();
                DsError.ThrowExceptionForHR(hr);
            }

            public void WaitUntilDone()
            {
                int hr;
                EventCode evCode;
                const int E_Abort = unchecked((int)0x80004004);

                do
                {
                    //System.Windows.Forms.Application.DoEvents();
                    hr = this.m_MediaEvent.WaitForCompletion(100, out evCode);
                } while (hr == E_Abort);
                //DsError.ThrowExceptionForHR(hr);
            }


            private static Bitmap currentFrame = null;
            /// <summary> buffer callback, COULD BE FROM FOREIGN THREAD. </summary>
            int ISampleGrabberCB.BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen)
            {

                Bitmap bitmap = new Bitmap(m_videoWidth, m_videoHeight, m_stride, PixelFormat.Format24bppRgb, pBuffer);
                bitmap.RotateFlip(RotateFlipType.Rotate180FlipX);
                // byte[] bmp = ByteTools.BmpToBytes(bitmap, PixelFormat.Format24bppRgb);
                VisionMessage vm = new VisionMessage(m_videoHeight, m_videoWidth, ByteTools.pixelFormatToBPP(PixelFormat.Format24bppRgb), bitmap);
                msgService.sendMsg(vm);
                //Thread.Sleep((int)(videoInfoHeader.AvgTimePerFrame / 10000));
                
                if (Constants.EVALUATE_SUCCESS_ENABLED)
                {
                    string tmp = (string)Globals.FRAME_SIGN_HASH["" + Globals.CURRENT_FRAME_INDEX];
                    if (tmp != null)
                    {   // 3 seconds passed. Now there is another sign in the video.
                        if (!Globals.SIGN_IN_FRAME.Contains(Constants.NO_SIGN) && !Globals.SIGN_IN_FRAME_IS_DETECTED)
                        {
                            Globals.NUMBER_OF_MISSES++;
                            Console.WriteLine("MISS:" + Globals.SIGN_IN_FRAME.Substring(Globals.SIGN_IN_FRAME.IndexOf(',')+1));
                            if (currentFrame!=null && (Constants.HARVEST_TYPE == Constants.HarvestType.harvestAll || Constants.HARVEST_TYPE == Constants.HarvestType.harvestMisses))
                                currentFrame.Save(Constants.base_folder + "hasat\\" + Globals.HARVEST_SIGN_ID + "_miss.bmp");
                        }
                        Globals.SIGN_IN_FRAME = tmp;
                        Globals.SIGN_IN_FRAME_IS_DETECTED = false;
                    }
                }
                else if (Constants.LABELING_ENABLED)
                {
                    AForge.Imaging.ImageStatistics statistics = new AForge.Imaging.ImageStatistics(bitmap);
                    if (Math.Abs(Globals.CURRENT_R_MEAN - Math.Round(statistics.Red.Mean)) > 1 || Math.Abs(Globals.CURRENT_G_MEAN - Math.Round(statistics.Green.Mean)) > 1 || Math.Abs(Globals.CURRENT_B_MEAN - Math.Round(statistics.Blue.Mean)) > 2)
                    {
                        Globals.CURRENT_R_MEAN = (int)Math.Round(statistics.Red.Mean);
                        Globals.CURRENT_G_MEAN = (int)Math.Round(statistics.Green.Mean);
                        Globals.CURRENT_B_MEAN = (int)Math.Round(statistics.Blue.Mean);
                    }
                }
                if (Globals.FRAME_COUNT == Globals.CURRENT_FRAME_INDEX)
                {
                    Globals.init();
                }
                else
                {
                    Globals.CURRENT_FRAME_INDEX++;
                }

                currentFrame = bitmap;
                return 0;
            }
            /// <summary> sample callback, NOT USED. </summary>
            int ISampleGrabberCB.SampleCB(double SampleTime, IMediaSample pSample)
            {
                Marshal.ReleaseComObject(pSample);
                return 0;
            }
        }

        /// <summary>
        /// Read vision information from a connected camera.
        /// </summary>
        public class DeviceVisionSource : MsgInitiator, ISampleGrabberCB
        {

            [DllImport("Kernel32.dll", EntryPoint = "RtlMoveMemory")]
            private static extern void CopyMemory(IntPtr Destination, IntPtr Source, [MarshalAs(UnmanagedType.U4)] uint Length);

            /// <summary> graph builder interface. </summary>
            private IFilterGraph2 m_FilterGraph = null;
            IMediaControl m_mediaCtrl = null;
            IMediaEvent m_MediaEvent = null;
            //private ICaptureGraphBuilder2 captureGraphBuilder = null;

            /// <summary> Dimensions of the image, calculated once in constructor. </summary>
            public int m_videoWidth;
            public int m_videoHeight;
            private int m_stride;
            public int m_Count = 0;
            VideoInfoHeader videoInfoHeader;

            public DeviceVisionSource(MsgService msgService)
                : base(msgService)
            {
                try
                {
                    // Set up the capture graph
                    Configure();
                }
                catch
                {
                    Dispose();
                    throw;
                }
            }

            private IBaseFilter FindCaptureDevice()
            {
                IEnumMoniker classEnum = null;
                IMoniker[] moniker = new IMoniker[1];
                object source = null;

                ICreateDevEnum devEnum = (ICreateDevEnum)(new CreateDevEnum());
                int hr = devEnum.CreateClassEnumerator(FilterCategory.VideoInputDevice, out classEnum, CDef.None);
                DsError.ThrowExceptionForHR(hr);
                Marshal.ReleaseComObject(devEnum);

                if (classEnum == null)
                {
                    throw new ApplicationException("No video capture device was detected.");
                }

                IntPtr none = new IntPtr(0);

                if (classEnum.Next(moniker.Length, moniker, none) == 0)
                {
                    Guid iid = typeof(IBaseFilter).GUID;
                    moniker[0].BindToObject(null, null, ref iid, out source);
                }
                else
                {
                    throw new ApplicationException("Unable to access video capture device!");
                }

                Marshal.ReleaseComObject(moniker[0]);
                Marshal.ReleaseComObject(classEnum);

                return (IBaseFilter)source;
            }

            private void Configure() {
                int hr;

                ISampleGrabber sampGrabber = null;
                IBaseFilter baseGrabFlt = null;
                IBaseFilter capFilter = null;
                IBaseFilter nullrenderer = null;
                IBaseFilter sourceFilter = null;

                // Get the graphbuilder object
                m_FilterGraph = new FilterGraph() as IFilterGraph2;
                m_mediaCtrl = m_FilterGraph as IMediaControl;
                m_MediaEvent = m_FilterGraph as IMediaEvent;

                IMediaFilter mediaFilt = m_FilterGraph as IMediaFilter;

                try
                {
                    sourceFilter = FindCaptureDevice();

                    hr = this.m_FilterGraph.AddFilter(sourceFilter, "WebCamControl Video");
                    DsError.ThrowExceptionForHR(hr);

                    // Get the SampleGrabber interface
                    sampGrabber = new SampleGrabber() as ISampleGrabber;
                    baseGrabFlt = sampGrabber as IBaseFilter;

                    ConfigureSampleGrabber(sampGrabber);

                    // Add the frame grabber to the graph
                    hr = m_FilterGraph.AddFilter(baseGrabFlt, "Ds.NET Grabber");
                    DsError.ThrowExceptionForHR(hr);

                    // ---------------------------------
                    // Connect the file filter to the sample grabber

                    // Hopefully this will be the video pin, we could check by reading it's mediatype
                    IPin iPinOut = DsFindPin.ByDirection(sourceFilter, PinDirection.Output, 0);

                    // Get the input pin from the sample grabber
                    IPin iPinIn = DsFindPin.ByDirection(baseGrabFlt, PinDirection.Input, 0);

                    hr = m_FilterGraph.Connect(iPinOut, iPinIn);
                    DsError.ThrowExceptionForHR(hr);

                    // Add the null renderer to the graph
                    nullrenderer = new NullRenderer() as IBaseFilter;
                    hr = m_FilterGraph.AddFilter(nullrenderer, "Null renderer");
                    DsError.ThrowExceptionForHR(hr);

                    // ---------------------------------
                    // Connect the sample grabber to the null renderer

                    iPinOut = DsFindPin.ByDirection(baseGrabFlt, PinDirection.Output, 0);
                    iPinIn = DsFindPin.ByDirection(nullrenderer, PinDirection.Input, 0);

                    hr = m_FilterGraph.Connect(iPinOut, iPinIn);
                    DsError.ThrowExceptionForHR(hr);

                    if (!Constants.DIRECTSHOW_TURN_ON_CLOCK)
                    {
                        // Turn off the clock.  This causes the frames to be sent
                        // thru the graph as fast as possible
                        hr = mediaFilt.SetSyncSource(null);
                        DsError.ThrowExceptionForHR(hr);
                    }

                    // Read and cache the image sizes
                    SaveSizeInfo(sampGrabber);
                }
                finally
                {
                    if (capFilter != null)
                    {
                        Marshal.ReleaseComObject(capFilter);
                        capFilter = null;
                    }
                    if (sourceFilter != null)
                    {
                        Marshal.ReleaseComObject(sourceFilter);
                        capFilter = null;
                    }
                    if (sampGrabber != null)
                    {
                        Marshal.ReleaseComObject(sampGrabber);
                        sampGrabber = null;
                    }
                    if (nullrenderer != null)
                    {
                        Marshal.ReleaseComObject(nullrenderer);
                        nullrenderer = null;
                    }
                }
            }

            /// <summary> Set the options on the sample grabber </summary>
            private void ConfigureSampleGrabber(ISampleGrabber sampGrabber)
            {
                AMMediaType media;
                int hr;

                // Set the media type to Video/RBG24
                media = new AMMediaType();
                media.majorType = MediaType.Video;
                media.subType = MediaSubType.RGB24;
                media.formatType = FormatType.VideoInfo;
                hr = sampGrabber.SetMediaType(media);
                DsError.ThrowExceptionForHR(hr);

                DsUtils.FreeAMMediaType(media);
                media = null;

                // Choose to call BufferCB instead of SampleCB
                hr = sampGrabber.SetCallback(this, 1);
                DsError.ThrowExceptionForHR(hr);
            }

            /// <summary> Read and store the properties </summary>
            private void SaveSizeInfo(ISampleGrabber sampGrabber)
            {
                int hr;

                // Get the media type from the SampleGrabber
                AMMediaType media = new AMMediaType();
                hr = sampGrabber.GetConnectedMediaType(media);
                DsError.ThrowExceptionForHR(hr);

                if ((media.formatType != FormatType.VideoInfo) || (media.formatPtr == IntPtr.Zero))
                {
                    throw new NotSupportedException("Unknown Grabber Media Format");
                }

                // Grab the size info
                videoInfoHeader = (VideoInfoHeader)Marshal.PtrToStructure(media.formatPtr, typeof(VideoInfoHeader));
                m_videoWidth = videoInfoHeader.BmiHeader.Width;
                m_videoHeight = videoInfoHeader.BmiHeader.Height;
                m_stride = m_videoWidth * (videoInfoHeader.BmiHeader.BitCount / 8);

                DsUtils.FreeAMMediaType(media);
                media = null;
            }

            /// <summary> release everything. </summary>
            public override void Dispose()
            {
                CloseInterfaces();
            }
            
            // Destructor
            ~DeviceVisionSource()
            {
                CloseInterfaces();
            }

            /// <summary> Shut down capture </summary>
            private void CloseInterfaces()
            {
                int hr;
                try
                {
                    if (m_mediaCtrl != null)
                    {
                        hr = m_mediaCtrl.Stop();
                        m_mediaCtrl = null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
                if (m_FilterGraph != null)
                {
                    Marshal.ReleaseComObject(m_FilterGraph);
                    m_FilterGraph = null;
                }
                GC.Collect();
            }

            override public void start()
            {
                int hr = m_mediaCtrl.Run();
                DsError.ThrowExceptionForHR(hr);
            }

            override public void stop()
            {
                int hr = m_mediaCtrl.Pause();
                DsError.ThrowExceptionForHR(hr);
            }

            public void WaitUntilDone()
            {
                int hr;
                EventCode evCode;
                const int E_Abort = unchecked((int)0x80004004);

                do
                {
                    //System.Windows.Forms.Application.DoEvents();
                    hr = this.m_MediaEvent.WaitForCompletion(100, out evCode);
                } while (hr == E_Abort);
                //DsError.ThrowExceptionForHR(hr);
            }

            /// <summary> buffer callback, COULD BE FROM FOREIGN THREAD. </summary>
            int ISampleGrabberCB.BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen)
            {

                Bitmap bitmap = new Bitmap(m_videoWidth, m_videoHeight, m_stride, PixelFormat.Format24bppRgb, pBuffer);
                bitmap.RotateFlip(RotateFlipType.Rotate180FlipX);
                // byte[] bmp = ByteTools.BmpToBytes(bitmap, PixelFormat.Format24bppRgb);
                VisionMessage vm = new VisionMessage(m_videoHeight, m_videoWidth, ByteTools.pixelFormatToBPP(PixelFormat.Format24bppRgb), bitmap);
                msgService.sendMsg(vm);
                Thread.Sleep((int)(videoInfoHeader.AvgTimePerFrame/20000));
                return 0;
            }
            /// <summary> sample callback, NOT USED. </summary>
            int ISampleGrabberCB.SampleCB(double SampleTime, IMediaSample pSample)
            {
                Marshal.ReleaseComObject(pSample);
                return 0;
            }
        }

        /// <summary>
        /// Read vision information from memory.
        /// </summary>
        public class BitmapMemoryVisionSource : MsgInitiator
        {
            bool isStarted = false;

            public BitmapMemoryVisionSource(MsgService msgService)
                : base(msgService)
            {
                isStarted = false;
            }

            public override void start()
            {
                isStarted = true;
            }

            public override void stop()
            {
                isStarted = false;
            }

            public void sendBmp(Bitmap bitmap) 
            {
                if (isStarted)
                {
                    // byte[] bmp = ByteTools.BmpToBytes(bitmap, PixelFormat.Format24bppRgb);
                    VisionMessage vm = new VisionMessage(bitmap.Height, bitmap.Width, ByteTools.pixelFormatToBPP(PixelFormat.Format24bppRgb), bitmap);
                    msgService.sendMsg(vm);
                }
            }
        }
    }
}
