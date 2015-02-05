using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PanoramicData.utils
{
    public class Vector2
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Vector2()
        {
        }

        public Vector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double Dot(Vector2 other)
        {
            return X * other.X + Y * other.Y;
        }

        public double Cross(Vector2 other)
        {
            return X * other.Y - Y * other.X;
        }

        public Vector2 Perp()
        {
            return new Vector2(-Y, X);
        }

        public Vector2 GetNormalized()
        {
            double l = Length();
            if (Math.Abs(l) < MathUtil.EPSILON)
                return new Vector2();
            return this / l;
        }

        public static double PerpProduct(Vector2 v0, Vector2 v1)
        {
            return (v0.X * v1.Y - v0.Y * v1.X);
        }

        public double Length()
        {
            double d = X * X + Y * Y;
            if (Math.Abs(d) < MathUtil.EPSILON)
                return 0;
            return Math.Sqrt(X * X + Y * Y);
        }

        public double Length2()
        {
            return X * X + Y * Y;
        }

        public static Vector2 operator +(Vector2 v0, Vector2 v1)
        {
            return new Vector2(v0.X + v1.X, v0.Y + v1.Y);
        }

        public static Vector2 operator +(Point v0, Vector2 v1)
        {
            return new Vector2(v0.X + v1.X, v0.Y + v1.Y);
        }

        public static Vector2 operator -(Vector2 v0, Vector2 v1)
        {
            return new Vector2(v0.X - v1.X, v0.Y - v1.Y);
        }

        public static Vector2 operator -(Point v0, Vector2 v1)
        {
            return new Vector2(v0.X - v1.X, v0.Y - v1.Y);
        }

        public static Vector2 operator *(Vector2 v0, double d)
        {
            return new Vector2(v0.X * d, v0.Y * d);
        }

        public static double operator *(Vector2 v0, Vector2 v1)
        {
            return v0.Dot(v1);
        }

        public static Vector2 operator /(Vector2 v0, Vector2 v1)
        {
            return new Vector2(v0.X / v1.X, v0.Y / v1.Y);
        }

        public static Vector2 operator /(Vector2 v0, double r)
        {
            return new Vector2(v0.X / r, v0.Y / r);
        }

        public static implicit operator Point(Vector2 v)
        {
            return new System.Windows.Point(v.X, v.Y);
        }

        public static implicit operator Vector2(Point p)
        {
            return new Vector2(p.X, p.Y);
        }


        public void DrawPointOn(Canvas canvas, Color color, double perimeter = 4)
        {
            var e = new Ellipse
            {
                Width = perimeter,
                Height = perimeter,
                Fill = new SolidColorBrush(color)
            };

            Canvas.SetLeft(e, X - perimeter);
            Canvas.SetTop(e, Y - perimeter);
            canvas.Children.Add(e);
        }

        public override string ToString()
        {
            return "Vector2( " + X + ", " + Y + " )";
        }
    }
}
