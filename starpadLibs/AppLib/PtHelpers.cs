using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using starPadSDK.Geom;
using starPadSDK.Inq;
using starPadSDK.Utils;

namespace starPadSDK.AppLib
{
    public class PtHelpers
    {
        public static Pt Mult(Pt p, double d)
        {
            return new Pt(p.X * d, p.Y * d);
        }
        public static Pt Div(Pt p, double d)
        {
            return new Pt(p.X / d, p.Y / d);
        }

        public static double samples = 80.0;
        
        private static double determineResampleSpacing(Stroq points, double samples = 80)
        {
            Rct bound = points.GetBounds();
            return (Distance(bound.TopLeft, bound.BottomRight) / samples);
        }

        public static double Distance(Pt a, Pt b)
        {
            double deltaX = a.X - b.X;
            double deltaY = a.Y - b.Y;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
        }

        public static Stroq Resample(Stroq stroq, double samples = 80)
        {
            Stroq points = stroq.Clone();
            double S = determineResampleSpacing(points, samples);

            double D = 0.0, d; // D is the distance accumulator of consecutive points, when D < S
            Stroq resampled = new Stroq(points[0]);
            int i, c = 0;

            for (i = 1; i < points.Count; i++)
            {
                d = Distance(points[i - 1], points[i]);

                if (D + d >= S)
                {
                    c = c + 1;
                    Pt q = new Pt();
                    q.X = points[i - 1].X + ((S - D) / d) * (points[i].X - points[i - 1].X);
                    q.Y = points[i - 1].Y + ((S - D) / d) * (points[i].Y - points[i - 1].Y);
                    resampled.Add(q);
                    points.Insert(i, q);
                    D = 0.0;
                }
                else
                    D += d;
            }
            return resampled;
        }

        public static List<Pt> Smooth(List<Pt> points, int filterSize)
        {
            List<Pt> ret = new List<Pt>();

            int avgCount = filterSize;
            for (int i = 0; i < points.Count; i++)
            {
                double f = avgCount + 1;
                double totalF = f;
                Pt p = Mult(points[i], f);
                for (int k = 0; k < avgCount; k++)
                {
                    int idx = k + 1;
                    f = (avgCount - k);
                    totalF += 2 * f;
                    p += Mult(GetAt(i + idx, points), f);
                    p += Mult(GetAt(i - idx, points), f);
                }
                ret.Add(Div(p, totalF));
            }

            return ret;
        }

        public static double[] Smooth(double[] points, int filterSize, double factor = 1.0)
        {
            double[] ret = new double[points.Length];

            int avgCount = filterSize;
            for (int i = 0; i < points.Length; i++)
            {
                double f = avgCount + 1;
                double totalF = f;
                double p = points[i] * f;
                for (int k = 0; k < avgCount; k++)
                {
                    int idx = k + 1;
                    f = (avgCount - k);
                    totalF += 2 * f;
                    p += GetAt(i + idx, points) * f;
                    p += GetAt(i - idx, points) * f;
                }
                ret[i] = (p / totalF) * factor;
            }

            return ret;
        }

        public static double GetAt(int i, double[] points)
        {
            if (i < 0)
            {
                return points[0];
            }
            else if (i > points.Length - 1)
            {
                return points[points.Length - 1];
            }
            else
            {
                return points[i];
            }
        }

        public static Pt GetAt(int i, List<Pt> points)
        {
            if (i < 0)
            {
                return points[0];
            }
            else if (i > points.Count - 1)
            {
                return points[points.Count - 1];
            }
            else
            {
                return points[i];
            }
        }
    }
}
