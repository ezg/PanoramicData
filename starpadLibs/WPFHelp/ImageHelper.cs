using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Resources;
using System.Windows.Markup;
using System.Runtime.InteropServices;
using System.IO;
using Bitmap = System.Drawing.Bitmap;

namespace starPadSDK.WPFHelp
{
    static public class ImageHelper
    {
        [DllImport("gdi32")]
        static extern int DeleteObject(IntPtr o);
        public static BitmapSource       LoadBitmap(this Bitmap source)
        {
            IntPtr ip = source.GetHbitmap();
            BitmapSource bs = null;
            try { bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip, IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions()); }
            finally { DeleteObject(ip); }
            return bs;
        }
        public static Image              ConvertBitmapToWPFImage(this System.Drawing.Bitmap b, double initialWidth)
        {
            Image img = new Image();
            img.VerticalAlignment = VerticalAlignment.Top;
            img.Width = b.Width;
            img.Height = b.Height;
            img.Source = b.LoadBitmap();
            if (initialWidth > 0)
                img.RenderTransform = new ScaleTransform(initialWidth / img.Width, initialWidth / img.Width);
            return img;
        }
        public static Image              CropImg(this FrameworkElement page, Point UpperLeft, Point LowerRight)
        {
            int Lft = (int)UpperLeft.X;
            int Upr = (int)UpperLeft.Y;
            int Rgt = (int)LowerRight.X;
            int Lwr = (int)LowerRight.Y;
            VisualBrush b = new VisualBrush();
            b.Visual = page;
            b.Stretch = Stretch.None;
            Canvas r = new Canvas();
            r.Background = b;
            r.Width = page.Width;
            r.Height = page.Height;
            r.RenderTransform = new TranslateTransform(-Lft, -Upr);
            r.Clip = new RectangleGeometry(new Rect(UpperLeft, LowerRight));
            Size s = new Size(r.ActualWidth, r.ActualHeight);
            r.Measure(s);
            r.Arrange(new Rect(s));
            RenderTargetBitmap rb = new RenderTargetBitmap(Rgt - Lft, Lwr - Upr, 96d, 96d, PixelFormats.Default);
            rb.Render(r);
            Image clip = new Image();
            clip.Source = BitmapFrame.Create(rb);
            clip.Stretch = Stretch.Uniform;

            return clip;
        }
        public static BitmapImage        LoadImage(this Bitmap bitmap)
        {
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();
            return bi;
        }
        public static LinkedList<Bitmap> Split(System.Drawing.Image img, int portionHeight, int portionWidth, string path, string fileName)
        {
            LinkedList<System.Drawing.Bitmap> result = new LinkedList<System.Drawing.Bitmap>();
            

            for (int y = 0; y < img.Height; y += portionHeight)
            {
                for (int x = 0; x < img.Width; x += portionWidth)
                {
                    System.Drawing.Bitmap imgSplitted = new System.Drawing.Bitmap(portionWidth, portionHeight);
                    System.Drawing.Graphics gr = System.Drawing.Graphics.FromImage(imgSplitted);

                    gr.Clear(System.Drawing.Color.White);
                    gr.DrawImage(img, new System.Drawing.Rectangle(0, 0, imgSplitted.Width, imgSplitted.Height), x, y, portionWidth,
                                 portionHeight, System.Drawing.GraphicsUnit.Pixel);
                    gr.Save();

                    result.AddLast(imgSplitted);
                    //imgSplitted.Save(Path.Combine(path, fileName + x.ToString() + "_" + y.ToString() + ".png"));
                }
            }

            return result;
        }

        public static BitmapSource[]     SplitVertically(BitmapSource img, int heightSplit)
        {
            int stride = Convert.ToInt32(img.Width);
            int height = Convert.ToInt32(img.Height);

            int n = (height - (height % heightSplit)) / heightSplit;

            int[] pixels = new int[stride * Convert.ToInt32(img.Height)];
            img.CopyPixels(pixels, stride, 0);

            BitmapSource[] results = new BitmapSource[n];

            for (int i = 0; i < n; i++)
            {
                // Create pixel array

                int[] pix = new int[stride * heightSplit];

                int startY = i * heightSplit;

                int startOffset = startY * stride;

                for (int j = startOffset; j < (startOffset + pix.Length); j++)
                {
                    pix[j - startOffset] = pixels[j];
                }

                // Create image

                results[i] = BitmapImage.Create(stride, heightSplit, img.DpiX, img.DpiY, img.Format, img.Palette, pix, stride);
            }

            return results;
        }
    }
}
