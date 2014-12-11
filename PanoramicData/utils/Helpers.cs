using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace PanoramicData.utils
{
    public class Helpers
    {
        public static Color GetColorFromString(string input)
        {
            Color result = Color.FromArgb(0, 0, 0, 0);
            if (input.StartsWith("#"))
            {
                byte red = Convert.ToByte(input.Substring(1, 2), 16);
                byte green = Convert.ToByte(input.Substring(3, 2), 16);
                byte blue = Convert.ToByte(input.Substring(5, 2), 16);
                result = Color.FromArgb(255, red, green, blue);
            }
            return result;
        }
        public static float Distance(Point p1, Point p2)
        {
            var p = new Point(p1.X - p2.X, p1.Y - p2.Y);
            return (float)Math.Sqrt(p.X * p.X + p.Y * p.Y);
        }
    }
}
