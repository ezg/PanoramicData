using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace InputFramework.DeviceDriver
{
    public class PressurePoint
    {
        public Point Point { get; set; }
        public float Intensity { get; set; }

        public PressurePoint(Point point, float intensity)
        {
            Point = point;
            Intensity = intensity;
        }

        public PressurePoint(Point point) : this(point, 1.0f)
        {
        }

        public PressurePoint(double x, double y) : this(new Point(x, y), 1.0f)
        {
        }

        public PressurePoint(double x, double y, float intensity) : this(new Point(x, y), intensity)
        {
        }

        public PressurePoint() : this(new Point(), 1.0f)
        {
        }

        public double X 
        {
            get
            {
                return Point.X;
            }

            set
            {
                Point point = Point;
                point.X = value;
                Point = point;
            }
        }

        public double Y
        {
            get
            {
                return Point.Y;
            }

            set
            {
                Point point = Point;
                point.Y = value;
                Point = point;
            }
        }

        public override string ToString()
        {
            return "X: " + Point.X + ", Y: " + Point.Y + ", Intensity: " + Intensity;
        }
    }
}
