using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

using BOUNLib.ToolBox;

namespace BOUNLib
{

    namespace Messages
    {

        /// <summary>
        /// Messaging interface implemented by message classes
        /// </summary>
        public interface IMessage
        {
            byte[] toByteArray();
            IMessage fromByteArray(byte[] message);
            bool isValid();
            IMessage duplicate();
        }

        /// <summary>
        /// Any basic message format. All messages can be converted to this type for serialization purposes.
        /// </summary>
        class GenericMessage : IMessage
        {
            byte[] msgData;
            public static Type msgType = new GenericMessage().GetType();

            public byte[] toByteArray()
            {
                return msgData;
            }

            public IMessage fromByteArray(byte[] message)
            {
                msgData = new byte[message.Length];
                Buffer.BlockCopy(message, 0, msgData, 0, message.Length);
                return this;
            }

            public bool isValid()
            {
                return msgData.Length > 0;
            }

            public IMessage duplicate()
            {
                GenericMessage gm = new GenericMessage();
                return gm.fromByteArray(this.toByteArray());
            }
        }

        /// <summary>
        /// Transferres information in distributed deployments of the processors and source classes.
        /// </summary>
        public class UDPMessage : IMessage
        {

            int messageOffset;
            int messageID;
            int messageSize;
            byte[] data;

            public byte[] Data
            {
                get { return data; }
                set { data = value; }
            }

            public int MessageOffset
            {
                get { return messageOffset; }
                set { messageOffset = value; }
            }
            public int MessageID
            {
                get { return messageID; }
                set { messageID = value; }
            }
            public int MessageSize
            {
                get { return messageSize; }
                set { messageSize = value; }
            }

            public UDPMessage(int dataLen)
            {
                data = new byte[dataLen];
            }

            public UDPMessage(byte[] msgData)
            {
                fromByteArray(msgData);
            }

            public byte[] toByteArray()
            {
                byte[] outBuf = new Byte[3 * sizeof(int) + data.Length];
                int pos = 0;
                pos = ByteTools.intToByteArr(messageOffset, outBuf, pos);
                pos = ByteTools.intToByteArr(messageID, outBuf, pos);
                pos = ByteTools.intToByteArr(messageSize, outBuf, pos);
                pos = ByteTools.byteArrToByteArr(data, outBuf, pos);
                return outBuf;
            }

            public IMessage fromByteArray(byte[] message)
            {
                int pos = 0;
                messageOffset = ByteTools.intFromByteArr(message, pos);
                pos += sizeof(int);
                messageID = ByteTools.intFromByteArr(message, pos);
                pos += sizeof(int);
                messageSize = ByteTools.intFromByteArr(message, pos);
                pos += sizeof(int);
                data = ByteTools.byteArrFromByteArr(message, pos, message.Length - pos);

                return this;
            }
            
            public override string ToString()
            {
                return "Vision message message num:" + messageOffset + " message ID:" + messageID + " size:" + messageSize + "\n";
            }

            public static int getHeaderSize()
            {
                return 3 * sizeof(int);
            }
            public bool isValid()
            {
                return messageSize > 0;
            }

            public IMessage duplicate()
            {
                return new UDPMessage(this.toByteArray());
            }

        }
      
        /// <summary>
        /// The basic image information from the source objects.
        /// </summary>
        public class VisionMessage : IMessage
        {
            private int bmpHeight;
            private int bmpWidth;
            private int bytePerPixel;
            // private byte[] bitmap;
            Bitmap bitmap;
            private int com_x;
            private int com_y;
            public bool bypass = false;
            
            public int BmpHeight
            {
                get { return bmpHeight; }
                set { bmpHeight = value; }
            }

            public int BmpWidth
            {
                get { return bmpWidth; }
                set { bmpWidth = value; }
            }

            public int BytePerPixel
            {
                get { return bytePerPixel; }
                set { bytePerPixel = value; }
            }

            public int CoM_X
            {
                get { return com_x; }
                set { com_x = value; }
            }

            public int CoM_Y
            {
                get { return com_y; }
                set { com_y = value; }
            }

            public Bitmap Bitmap
            {
                get { return bitmap; }
                set { bitmap = value; }
            }

            public static Type msgType = new VisionMessage().GetType();

            public VisionMessage()
                : base()
            {
            }

            public VisionMessage(int bmpHeight, int bmpWidth, int bytePerPixel, Bitmap bitmap)
            {
                this.BmpHeight = bmpHeight;
                this.BmpWidth = bmpWidth;
                this.bytePerPixel = bytePerPixel;
                this.bitmap = bitmap;
            }
            
            public byte[] toByteArray()
            {
                byte[] outBuf = new Byte[3 * sizeof(int) + bmpHeight * bmpWidth * bytePerPixel];
                int pos = 0;
                pos = ByteTools.intToByteArr(bmpHeight, outBuf, pos);
                pos = ByteTools.intToByteArr(bmpWidth, outBuf, pos);
                pos = ByteTools.intToByteArr(bytePerPixel, outBuf, pos);
                // pos = ByteTools.byteArrToByteArr(bitmap, outBuf, pos);
                pos = ByteTools.byteArrToByteArr(ByteTools.BmpToBytes(bitmap, bitmap.PixelFormat), outBuf, pos);
                return outBuf;
            }

            public IMessage fromByteArray(byte[] message)
            {
                int pos = 0;
                bmpHeight = ByteTools.intFromByteArr(message, pos);
                pos += sizeof(int);
                bmpWidth = ByteTools.intFromByteArr(message, pos);
                pos += sizeof(int);
                bytePerPixel = ByteTools.intFromByteArr(message, pos);
                pos += sizeof(int);
                bitmap = ByteTools.BytesToBmp(ByteTools.byteArrFromByteArr(message, pos, bmpHeight * bmpWidth * bytePerPixel), BmpWidth, BmpHeight, ByteTools.bppToPixelFormat(BytePerPixel));

                if (bitmap == null)
                    throw new Exception("Bitmap is null!");
                // if (bmpHeight * bmpWidth * bytePerPixel != bitmap.Length)
                //    throw new Exception("Bitmap size error! w:" + bmpWidth + " h:" + bmpHeight + " bpp:" + bytePerPixel);

                return this;
            }

            public override string ToString()
            {
                return "Vision message width:" + bmpWidth + " height:" + bmpHeight + " bpp:" + bytePerPixel +"\n";
            }

            public bool isValid()
            {
                return bmpHeight > 0 && bmpWidth > 0 && bytePerPixel > 0;
            }

            public IMessage duplicate()
            {
                VisionMessage vm = new VisionMessage();
                return vm.fromByteArray(this.toByteArray());
            }
        }

        /// <summary>
        /// Text based infromation provided by the source objects.
        /// </summary>
        public class TextMessage : IMessage
        {

            private string msg;
            public static Type msgType = new TextMessage().GetType();

            public TextMessage()
                : base()
            {
            }

            public TextMessage(string theMsg)
                : base()
            {
                this.msg = theMsg;
            }

            public string getMessage()
            {
                return msg;
            }

            #region IMessage Members

            public byte[] toByteArray()
            {
                return Encoding.UTF8.GetBytes(msg);
            }

            public IMessage fromByteArray(byte[] message)
            {
                return new TextMessage(Encoding.UTF8.GetString(message));
            }

            public bool isValid()
            {
                return (this.msg != null);
            }

            public IMessage duplicate()
            {
                return new TextMessage(msg);
            }

            #endregion
        }
    }
}
