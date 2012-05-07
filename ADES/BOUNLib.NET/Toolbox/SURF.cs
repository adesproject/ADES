using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using OpenSURF;

namespace BOUNLib.NET.Toolbox
{
    /// <summary>
    /// OpenSURF integration.
    /// </summary>
    public class SURF
    {

        bool upright; 
        int octaves;
        public int intervals;
        int init_sample;
        float thres;
        int interp_steps;

        public SURF(bool upright, int octaves, int intervals, int init_sample, float thres, int interp_steps)
        {
            this.upright = upright;
            this.octaves = octaves;
            this.intervals = intervals;
            this.init_sample = init_sample;
            this.thres = thres;
            this.interp_steps = interp_steps;
        }

        public List<Ipoint> getDescriptors(Bitmap theImage)
        {
            IplImage pIplImage = null;
            List<Ipoint> aIpoint = null;
            try
            {
                if (theImage == null) return null;

                pIplImage = IplImage.LoadImage(theImage);
                if (pIplImage == null) return null;

                COpenSURF.surfDetDes(null,
                                        pIplImage,
                                        out aIpoint,
                                        upright,
                                        octaves,
                                        intervals,
                                        init_sample,
                                        thres,
                                        interp_steps);

                //if (Constants.HARVEST_TYPE == Constants.HarvestType.harvestAll || Constants.HARVEST_TYPE == Constants.HarvestType.harvestMisses)
                //    paintSURFPoints(theImage, aIpoint);
            }
            catch (Exception E)
            {
                Console.WriteLine("updateSURFImage Exception=" + E.Message + " " + E.StackTrace);
            }
            finally
            {
            }
            return aIpoint;
        }

        public static Bitmap paintSURFPoints(Bitmap pBitmap, List<Ipoint> aIpoint)
        {
            if (pBitmap == null) return null;

            Bitmap vret = pBitmap;

            Graphics pgd = null;
            Pen ppenred = null;
            Pen ppenblue = null;
            try
            {
                pgd = Graphics.FromImage(vret);

                if (aIpoint == null) return vret;

                ppenred = new Pen(Color.Red);
                ppenblue = new Pen(Color.Blue);

                foreach (Ipoint pIpoint in aIpoint)
                {
                    if (pIpoint == null) continue;

                    int xd = (int)pIpoint.x;
                    int yd = (int)pIpoint.y;
                    float scale = pIpoint.scale;
                    float orientation = pIpoint.orientation;
                    float radius = scale * 2;

                    Pen ppen = (pIpoint.laplacian > 0 ? ppenred : ppenblue);

                    pgd.DrawEllipse(ppen, xd - radius, yd - radius, 2 * radius, 2 * radius);

                    double dx = radius * Math.Cos(orientation);
                    double dy = radius * Math.Sin(orientation);
                    pgd.DrawLine(ppen, new Point(xd, yd), new Point((int)(xd + dx), (int)(yd + dy)));
                }
            }
            finally
            {
                if (ppenred != null) ppenred.Dispose();
                if (ppenblue != null) ppenblue.Dispose();
                if (pgd != null) pgd.Dispose();
            }

            return vret;
        }

    }
}
