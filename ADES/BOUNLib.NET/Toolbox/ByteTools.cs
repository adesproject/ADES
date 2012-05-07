using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.InteropServices;

namespace BOUNLib
{
    namespace ToolBox
    {

        /// <summary>
        /// For CRC calculation.
        /// </summary>
        public enum CRC8_POLY
        {
            CRC8 = 0xd5,
            CRC8_CCITT = 0x07,
            CRC8_DALLAS_MAXIM = 0x31,
            CRC8_SAE_J1850 = 0x1D,
            CRC_8_WCDMA = 0x9b,
        };

        /// <summary>
        /// Class for calculating CRC8 checksums.
        /// usage:
        //  CRC8Calc crc_dallas = new CRC8Calc(CRC8_POLY.CRC8_DALLAS_MAXIM);
        //  checksum = crc_dallas.Checksum(testVal);
        /// </summary>
        public class CRC8Calc
        {
            
            private byte[] table = new byte[256];

            public byte Checksum(params byte[] val)
            {
                if (val == null)
                    throw new ArgumentNullException("val");

                byte c = 0;

                foreach (byte b in val)
                {
                    c = table[c ^ b];
                }

                return c;
            }

            public byte[] Table
            {
                get
                {
                    return this.table;
                }
                set
                {
                    this.table = value;
                }
            }

            public byte[] GenerateTable(CRC8_POLY polynomial)
            {
                byte[] csTable = new byte[256];

                for (int i = 0; i < 256; ++i)
                {
                    int curr = i;

                    for (int j = 0; j < 8; ++j)
                    {
                        if ((curr & 0x80) != 0)
                        {
                            curr = (curr << 1) ^ (int)polynomial;
                        }
                        else
                        {
                            curr <<= 1;
                        }
                    }

                    csTable[i] = (byte)curr;
                }

                return csTable;
            }

            public CRC8Calc(CRC8_POLY polynomial)
            {
                this.table = this.GenerateTable(polynomial);
            }
        }

        /// <summary>
        /// Various byte operations.
        /// </summary>
        public class ByteTools
        {
            /// <summary>
            /// Converts integer to byte array.
            /// </summary>
            /// <param name="data"></param>
            /// <param name="outBuf"></param>
            /// <param name="pos"></param>
            /// <returns></returns>
            public static int intToByteArr(int data, byte[] outBuf, int pos)
            {
                Buffer.BlockCopy(BitConverter.GetBytes(data), 0, outBuf, pos, sizeof(int));
                return pos + sizeof(int);
            }

            /// <summary>
            /// Copies a subarray of a byte array.
            /// </summary>
            /// <param name="inBuf"></param>
            /// <param name="outBuf"></param>
            /// <param name="pos"></param>
            /// <returns></returns>
            public static int byteArrToByteArr(byte[] inBuf, byte[] outBuf, int pos)
            {
                Buffer.BlockCopy(inBuf, 0, outBuf, pos, inBuf.Length);
                return pos + inBuf.Length;
            }

            /// <summary>
            /// Extracts integer from byte array.
            /// </summary>
            /// <param name="inBuf"></param>
            /// <param name="pos"></param>
            /// <returns></returns>
            public static int intFromByteArr(byte[] inBuf, int pos)
            {
                return BitConverter.ToInt32(inBuf, pos);
            }

            /// <summary>
            /// Returns byte array from a subarray of a byte array.
            /// </summary>
            /// <param name="inBuf"></param>
            /// <param name="pos"></param>
            /// <param name="size"></param>
            /// <returns></returns>
            public static byte[] byteArrFromByteArr(byte[] inBuf, int pos, int size)
            {
                byte[] outBuf = new byte[size];
                Buffer.BlockCopy(inBuf, pos, outBuf, 0, size);
                return outBuf;
            }

            /// <summary>
            /// Gets byte array from Bitmap.
            /// </summary>
            /// <param name="bmp"></param>
            /// <param name="pf"></param>
            /// <returns></returns>
            public static byte[] BmpToBytes(Bitmap bmp, PixelFormat pf)
            {
                BitmapData bData = bmp.LockBits(new Rectangle(new Point(), bmp.Size), ImageLockMode.ReadOnly, pf);
                int byteCount = bData.Stride * bmp.Height;
                byte[] bmpBytes = new byte[byteCount];
                Marshal.Copy(bData.Scan0, bmpBytes, 0, byteCount);
                bmp.UnlockBits(bData);
                return bmpBytes;
            }

            /// <summary>
            /// Constructs Bitmap from byte array.
            /// </summary>
            /// <param name="bmpBytes"></param>
            /// <param name="bmpWidth"></param>
            /// <param name="bmpHeight"></param>
            /// <param name="pf"></param>
            /// <returns></returns>
            public static Bitmap BytesToBmp(byte[] bmpBytes, int bmpWidth, int bmpHeight, PixelFormat pf)
            {
                Bitmap bmp = new Bitmap(bmpWidth, bmpHeight, pf);
                BitmapData bData = bmp.LockBits(new Rectangle(new Point(), bmp.Size), ImageLockMode.WriteOnly, pf);
                Marshal.Copy(bmpBytes, 0, bData.Scan0, bmpBytes.Length);
                bmp.UnlockBits(bData);
                return bmp;
            }

            /// <summary>
            /// Bits per pixel to PixelFormat
            /// </summary>
            /// <param name="bpp"></param>
            /// <returns></returns>
            public static PixelFormat bppToPixelFormat(int bpp)
            {
                if (bpp == 1)
                    return PixelFormat.Format8bppIndexed;
                else if (bpp == 3)
                    return PixelFormat.Format24bppRgb;
                else if (bpp == 4)
                    return PixelFormat.Format32bppRgb;
                else
                    return PixelFormat.Undefined;
            }

            /// <summary>
            /// PixelFormat to bits per pixel integer.
            /// </summary>
            /// <param name="pf"></param>
            /// <returns></returns>
            public static int pixelFormatToBPP(PixelFormat pf)
            {
                if (pf == PixelFormat.Format32bppRgb)
                    return 4;
                else if (pf == PixelFormat.Format24bppRgb)
                    return 3;
                else if (pf == PixelFormat.Format8bppIndexed)
                    return 1;
                else
                    return 0;
            }

            /// <summary>
            /// Center of Mass calculation for an image.
            /// </summary>
            /// <param name="bmp"></param>
            /// <param name="com_x"></param>
            /// <param name="com_y"></param>
            public static void imageCoM(Bitmap bmp, ref int com_x, ref int com_y)
            {
                int com_x_sum = 0, com_y_sum = 0, com_x_y_point_count = 0;
                System.Drawing.Imaging.BitmapData image_data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bmp.PixelFormat);
                int bpp = 3;
                int nOffset = image_data.Stride - bmp.Width * bpp;
                System.IntPtr Scan0 = image_data.Scan0;
                unsafe
                {
                    byte* p = (byte*)Scan0;
                    for (int y = 0; y < bmp.Height; y++)
                    {
                        for (int x = 0; x < bmp.Width; x++, p += bpp)
                        {
                            if (p[2] == 0)
                            {
                                com_x_sum += x;
                                com_y_sum += y;
                                com_x_y_point_count++;
                            }
                        }
                        p += nOffset;
                    }
                }
                bmp.UnlockBits(image_data);
                com_x = com_x_sum / com_x_y_point_count;
                com_y = com_y_sum / com_x_y_point_count;
            }

        }
    }
}
