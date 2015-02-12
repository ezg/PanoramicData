using starPadSDK.Geom;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2D = Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using DWrite = Microsoft.WindowsAPICodePack.DirectX.DirectWrite;

namespace PanoramicData.view.vis.render.direct2d
{
    public class ScatterPlotScene : XYScene
    {
        private int _borderTop = 10;
        private int _borderBottom = 25;
        private int _borderLeft = 40;
        private int _borderRight = 10;

        private DWrite.TextFormat textFormat;
        private DWrite.DWriteFactory writeFactory;
        
        private List<XYDataPoint> _dataPoints = new List<XYDataPoint>();
        private Dictionary<string, XYDataPointSeries> _series = new Dictionary<string, XYDataPointSeries>();

        public ScatterPlotScene()
            : base()
        {
            this.writeFactory = DWrite.DWriteFactory.CreateFactory();
        }

        public override void Render(List<XYDataPoint> dataPoints, Dictionary<string, XYDataPointSeries> series)
        {
            _dataPoints = dataPoints;
            _series = series;
            Render();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.writeFactory.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnCreateResources()
        {
            this.textFormat = this.writeFactory.CreateTextFormat("Arial", 24);
        }

        protected override void OnFreeResources()
        {
            if (this.textFormat != null)
            {
                this.textFormat.Dispose();
                this.textFormat = null;
            }
        }

        protected override void OnRender()
        {
            var size = this.RenderTarget.Size;
            if (size.Width > 10 && size.Height > 10)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                RenderTarget.BeginDraw();
                RenderTarget.Clear(new D2D.ColorF(1, 1, 1, 1));
                
                // translation because of borders
                RenderTarget.Transform = D2D.Matrix3x2F.Translation(_borderLeft, _borderTop);
                
                if (_dataPoints != null && _dataPoints.Count > 0)
                {
                    // calcualte data to scren transformation
                    Mat dataToScreen = caluclateDataToScreen();
                    float width = 4;
                    var rect = new D2D.RectF(0, 0, 0, 0);
                    // render points
                    foreach (var serie in _series.Values)
                    {
                        D2D.SolidColorBrush brush = RenderTarget.CreateSolidColorBrush(new D2D.ColorF(serie.Color.R / 255.0f, serie.Color.G / 255.0f, serie.Color.B / 255.0f));
                        foreach (var dataPoint in serie.XYDataPoints)
                        {
                            
                            if (dataPoint.XIsNull || dataPoint.YIsNull)
                            {
                                continue;
                            }
                            Pt screenPoint = dataToScreen * dataPoint.Point;
                            rect.Left = (float)screenPoint.X - width / 2.0f;
                            rect.Top = (float)screenPoint.Y - width / 2.0f;
                            rect.Width = rect.Height = width;
                            RenderTarget.FillRectangle(rect, brush);
                        }
                        brush.Dispose();
                    }
                    //string text = string.Format("Test");
                    //this.RenderTarget.DrawText(text, this.textFormat, new D2D.RectF(10, 10, 100, 20), this.whiteBrush);
                    //this.RenderTarget.meas
                }

                RenderTarget.EndDraw();

                Console.WriteLine("BarChart Rendering Millis: " + sw.ElapsedMilliseconds);
            }
        }

        Mat caluclateDataToScreen()
        {
            double maxY = _dataPoints.Where(dp => !dp.XIsNull && !dp.YIsNull).Max(dp => dp.Y);
            double maxX = _dataPoints.Where(dp => !dp.XIsNull && !dp.YIsNull).Max(dp => dp.X);
            double minY = _dataPoints.Where(dp => !dp.XIsNull && !dp.YIsNull).Min(dp => dp.Y);
            double minX = _dataPoints.Where(dp => !dp.XIsNull && !dp.YIsNull).Min(dp => dp.X);

            if (maxX == minX)
            {
                minX -= 1;
                maxX += 1;
            }
            if (maxY == minY)
            {
                minY = 0;
                maxY += 1;
            }

            Mat ret = Mat.Identity;

            double borderScale = 0.9;

            double dataAreaWidth = RenderTarget.Size.Width - _borderLeft - _borderRight;
            double dataAreaHeight = RenderTarget.Size.Height - _borderTop - _borderRight;

            Pt scale = new Pt(dataAreaWidth / (maxX - minX), -dataAreaHeight / (maxY - minY));
            ret = Mat.Translate(-minX, -minY) * Mat.Scale(scale.X, scale.Y) * Mat.Translate(0, dataAreaHeight);

            Pt borderOffset = ret.Inverse() *
                              new Pt((dataAreaWidth / 2.0),
                                     (dataAreaHeight / 2.0));

            ret = Mat.Translate(new Pt(-borderOffset.X, -borderOffset.Y)) *
                            Mat.Scale(borderScale, borderScale) *
                            Mat.Translate(new Pt(+borderOffset.X, +borderOffset.Y)) * ret;

            return ret;
        }
    }
}
