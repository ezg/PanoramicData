#if SURFACE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using starPadSDK.AppLib;
using Microsoft.Surface;
using Microsoft.Surface.Presentation.Controls;

namespace starPadSDK.SurfaceLib
{
    public delegate void HoverEventHandler(object sender, HoverEventArgs e);

    public class HoverEventArgs : EventArgs
    {
        private int _ID;
        private double _Radius;
        private Point _Center;

        public int ID { get { return _ID; } }
        public double Radius { get { return _Radius; } }
        public Point Center { get { return _Center; } }

        public HoverEventArgs(int ID, double Radius, Point Center)
        {
            _ID = ID;
            _Radius = Radius;
            _Center = Center;
        }
    }

    public class HoverManager
    {
        private Microsoft.Surface.Core.ContactTarget contactTarget;
        private IntPtr hwnd;
        private byte[] normalizedImage;
        private Microsoft.Surface.Core.ImageMetrics normalizedMetrics;
        private bool imageAvailable;

        private Canvas HoverCanvas;
        private SurfaceWindow HoverWindow;

        private Ellipse HoverArea;

        public event HoverEventHandler HoverEvent;
        public byte[] NormalizedImage { get { return normalizedImage; } }
        public bool ShowHoverAreas { get; set; }
        public double HoverAreaScale { get; set; }

        protected virtual void OnHoverEvent(HoverEventArgs e)
        {
            if(HoverEvent != null)
                HoverEvent(this, e);
        }

        public Window Window { get; set; }
        public HoverManager(SurfaceWindow hoverWindow)
        {
            ShowHoverAreas = true;
            HoverAreaScale = 1.5;

            HoverWindow = hoverWindow;

            // Get the hWnd for the SurfaceWindow object after it has been loaded.
            hwnd = new System.Windows.Interop.WindowInteropHelper(HoverWindow).Handle;
            contactTarget = new Microsoft.Surface.Core.ContactTarget(hwnd);

            // Set up the ContactTarget object for the entire SurfaceWindow object.
            contactTarget.EnableInput();

            HoverArea = new Ellipse();
            HoverArea.IsHitTestVisible = false;
            HoverArea.StrokeThickness = 2;
            HoverArea.Stroke = Brushes.White;
            HoverArea.Visibility = Visibility.Hidden;
            HoverArea.Opacity = 0.75;
            HoverArea.Fill = Brushes.White;
            HoverArea.OpacityMask = new RadialGradientBrush(Colors.Black, Colors.Transparent);
        }

        public void StartHover(Canvas hoverCanvas)
        {
            if (hoverCanvas != null)
            {
                HoverCanvas = hoverCanvas;

            HoverCanvas.Children.Add(HoverArea);

            }
            EnableRawImage();

            // Attach an event handler for the FrameReceived event.
            contactTarget.FrameReceived -= new EventHandler<Microsoft.Surface.Core.FrameReceivedEventArgs>(OnContactTargetFrameReceived);
            contactTarget.FrameReceived += new EventHandler<Microsoft.Surface.Core.FrameReceivedEventArgs>(OnContactTargetFrameReceived);

            contactTarget.EnableImage(Microsoft.Surface.Core.ImageType.Normalized);
        }

        public void StopHover()
        {
            DisableRawImage();
        }

        private void EnableRawImage()
        {
            contactTarget.EnableImage(Microsoft.Surface.Core.ImageType.Normalized);
            contactTarget.FrameReceived += OnContactTargetFrameReceived;
        }

        private void DisableRawImage()
        {
            contactTarget.DisableImage(Microsoft.Surface.Core.ImageType.Normalized);
            contactTarget.FrameReceived -= OnContactTargetFrameReceived;
        }

        private double Distance(Point first, Point second)
        {
            return Math.Sqrt((first.X - second.X) * (first.X - second.X) + (first.Y - second.Y) * (first.Y - second.Y));
        }

        private void OnContactTargetFrameReceived(object sender, Microsoft.Surface.Core.FrameReceivedEventArgs e)
        {
            imageAvailable = false;
            int paddingLeft, paddingRight;

            if (normalizedImage == null)
            {
                imageAvailable = e.TryGetRawImage(Microsoft.Surface.Core.ImageType.Normalized,
                    Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Left,
                    Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Top,
                    Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Width,
                    Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Height,
                    out normalizedImage,
                    out normalizedMetrics,
                    out paddingLeft,
                    out paddingRight);
            }
            else
            {
                imageAvailable = e.UpdateRawImage(Microsoft.Surface.Core.ImageType.Normalized,
                     normalizedImage,
                     Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Left,
                     Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Top,
                     Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Width,
                     Microsoft.Surface.Core.InteractiveSurface.DefaultInteractiveSurface.Height);
            }

            Rect hoverRect = Rect.Empty;
            if (imageAvailable && !(hoverRect = FindHover()).IsEmpty)
            {
                HoverArea.Visibility = Visibility.Hidden;
            }
            else
            {
                double midX = (hoverRect.Left + hoverRect.Right) / 2.0;
                double midY = (hoverRect.Top + hoverRect.Bottom) / 2.0;
                double radius = Distance(new Point(midX, midY), new Point(hoverRect.Right, hoverRect.Bottom));
                OnHoverEvent(new HoverEventArgs(0, radius, new Point(midX, midY)));

                if (ShowHoverAreas)
                {
                    radius *= HoverAreaScale;
                    HoverArea.Visibility = Visibility.Visible;
                    Canvas.SetLeft(HoverArea, midX - radius);
                    Canvas.SetTop(HoverArea, midY - radius);
                    HoverArea.Width = radius * 2;
                    HoverArea.Height = radius * 2;
                }
            }

        }
        List<TimestampedHover> hoverEvents = new List<TimestampedHover>();

        public bool HoverInTimeSpaceRnage(Point where, DateTime when, double distance, double millisecs, out List<TimestampedHover> lost)
        {
            lost = new List<TimestampedHover>();
            int closestInd = -1;
            foreach (TimestampedHover thover in hoverEvents)
            {
                double dist = Distance(where, new Point((thover.where.Left + thover.where.Right) / 2, (thover.where.Top + thover.where.Bottom) / 2));
                if (when.Subtract(thover.when).TotalMilliseconds < millisecs && dist < distance)
                {
                    distance = dist;
                    closestInd = hoverEvents.IndexOf(thover);
                }
            }
            if (closestInd != -1)
            {
                for (int j = closestInd; j < hoverEvents.Count; j++)
                    lost.Add(hoverEvents[j]);
                return true;
            }
            return false;
        }
        public double GetHoverIntensity(Rect area, Rect skip)
        {
            double intensity = 0;
            int num = 0;
            if (imageAvailable)
            {
                for (int row = (int)(area.Top*.75); row < area.Bottom*.75; row++)
                    for (int col = (int)(area.Left*.75); col < area.Right*.75; col++)
                        if (!skip.Contains(new Point(col*4/3,  row*4/3))) 
                    {
                        double pixel = normalizedImage[(row * 768) + col];
                        if (pixel > 5)
                        {
                            num++;
                            intensity = Math.Max(intensity, pixel);
                        }
                    }
            }
            return Math.Min(1,intensity/40);
        }
        public Rect FindHover()
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = 0, maxY = 0;
            bool found = false;

            if (imageAvailable)
                for (int row = 20; row < 576 - 20; row += 2)
                {
                    int rowOffset = row * 768;
                    for (int col = 20; col < 768 - 20; col += 2)
                    {
                        if (normalizedImage[rowOffset + col] > 254)
                        {
                            for (int rr = row-2; rr < row+20; rr++)
                                for (int cc = col-2; cc < col + 20; cc++)
                                    if (normalizedImage[rowOffset + cc] > 254)
                                    {
                                    int aRow = (int)Math.Floor(rr / 0.75);
                                    int aCol = (int)Math.Floor(cc / 0.75);
                                    minX = Math.Min(aCol, minX);
                                    minY = Math.Min(aRow, minY);
                                    maxX = Math.Max(aCol, maxX);
                                    maxY = Math.Max(aRow, maxY);
                                }
                            found = true;
                            goto foundIt;
                        }
                    }
                    }
            foundIt:
            if (found)
            {
                Rect hoverRect = new Rect(new Point(minX, minY-6), new Size(maxX - minX, maxY - minY));
                hoverEvents.Add(new TimestampedHover(DateTime.Now, hoverRect));
                if (hoverEvents.Count > 50)
                    hoverEvents.RemoveRange(0,hoverEvents.Count-50);
                return hoverRect;
            }
            return Rect.Empty;
        }

    }
}
#endif