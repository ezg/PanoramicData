using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Controls;
using System.Windows.Documents;

namespace starPadSDK.WPFHelp
{
    public class FrostyFreeze
    {

        protected static RenderTargetBitmap CreateBitmapFromControl(FrameworkElement ele, double maxWidth = double.NaN)
        {
            ele.UpdateLayout();
            if (ele is Window)
                ele = (ele as Window).Content as FrameworkElement;

            bool clipToBounds = ele.ClipToBounds;
            ele.ClipToBounds = true;

            double owidth = Math.Max(1,ele.ActualWidth == 0 ? ele.Width : ele.ActualWidth);
            double oheight = Math.Max(1, ele.ActualHeight == 0 ? ele.Height : ele.ActualHeight);
            double width = double.IsNaN(maxWidth) ? owidth : Math.Min(maxWidth, owidth);
            double height = width / owidth * oheight;
            RenderTargetBitmap rmp = new RenderTargetBitmap((int)width, (int)height , 96, 96, PixelFormats.Pbgra32);

            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(ele);
                dc.DrawRectangle(vb, null, new Rect(new Point(), new Size(width, height)));
            }

            rmp.Render(dv);

            ele.ClipToBounds = clipToBounds;
            return rmp;
        }

        public static System.Windows.Controls.Image CreateImageFromControl(FrameworkElement dragBubble, double maxWidth=double.NaN)
        {
            RenderTargetBitmap rmp = CreateBitmapFromControl(dragBubble, maxWidth);
            System.Windows.Controls.Image dragImage = new System.Windows.Controls.Image();
            dragImage.Width = rmp.Width;
            dragImage.Height = rmp.Height;
            dragImage.Source = rmp;
            return dragImage;
        }

        public static System.Drawing.Bitmap CreateBmpFromForm_Uncropped(System.Windows.Forms.Control c)
        {
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(c.Width, c.Height);
            c.DrawToBitmap(bmp, new System.Drawing.Rectangle(0, 0, c.Width, c.Height));
            return bmp;
        }


        protected static Dictionary<FrameworkElement, Image> FrozenItems = new Dictionary<FrameworkElement,Image>();

        public static bool IsFrozen(FrameworkElement elt)
        {
            return FrozenItems.ContainsKey(elt);
        }

        public static Image Freeze(FrameworkElement elt)
        {
            if (IsFrozen(elt))
            {
                return null;
            }

            Panel panel = (Panel)elt.Parent;

            Image img = CreateImageFromControl(elt);

            elt.Visibility = Visibility.Collapsed;
            panel.Children.Add(img);
            img.RenderTransform = elt.RenderTransform.Clone();
            FrozenItems.Add(elt, img);
            WPFUtil.BringToFront(img);

            return img;
        }

        public static void Unfreeze(FrameworkElement elt)
        {
            if (!IsFrozen(elt))
            {
                return;
            }

            Panel panel = (Panel)elt.Parent;

            elt.Visibility = Visibility.Visible;
            panel.Children.Remove(FrozenItems[elt]);

            FrozenItems.Remove(elt);
        }
    }
}
