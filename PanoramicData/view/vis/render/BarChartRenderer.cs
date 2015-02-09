using PanoramicData.view.Direct2D;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using D2D = Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using DWrite = Microsoft.WindowsAPICodePack.DirectX.DirectWrite;

namespace PanoramicData.view.vis.render
{
    public class BarChartRenderer : FilterRenderer
    { 
        private BarChartScene _barChartScene = new BarChartScene();

        public BarChartRenderer()
        {
            Direct2DControl control = new Direct2DControl();
            control.Scene = _barChartScene;
            this.Content = control;
        }

        protected override void UpdateResults()
        {
            base.UpdateResults();
            _barChartScene.Render();
        }
    }

    public class BarChartScene : Scene
    {
        private D2D.SolidColorBrush redBrush;
        private D2D.SolidColorBrush whiteBrush;
        private DWrite.TextFormat textFormat;
        private DWrite.DWriteFactory writeFactory;

        public BarChartScene()
            : base()
        {
            this.writeFactory = DWrite.DWriteFactory.CreateFactory();
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
            this.redBrush = this.RenderTarget.CreateSolidColorBrush(new D2D.ColorF(0, 1, 0, 0.5f));
            this.whiteBrush = this.RenderTarget.CreateSolidColorBrush(new D2D.ColorF(1, 1, 1));

            this.textFormat = this.writeFactory.CreateTextFormat("Arial", 12);
        }

        protected override void OnFreeResources()
        {
            if (this.redBrush != null)
            {
                this.redBrush.Dispose();
                this.redBrush = null;
            }
            if (this.whiteBrush != null)
            {
                this.whiteBrush.Dispose();
                this.whiteBrush = null;
            }
            if (this.textFormat != null)
            {
                this.textFormat.Dispose();
                this.textFormat = null;
            }
        }

        protected override void OnRender()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            var size = this.RenderTarget.Size;
            if (size.Width > 10 && size.Height > 10)
            {
                float width = 4;
                var rect = new D2D.RectF(0, 0, 0, 0);

                // This draws the ellipse in red on a semi-transparent blue background
                this.RenderTarget.BeginDraw();
                this.RenderTarget.Clear(new D2D.ColorF(0, 0, 1, 0.5f));


                Random r = new Random();
                for (int i = 0; i < 20000; i++)
                {
                    rect.Top = (float)r.NextDouble() * size.Width;
                    rect.Left = (float)r.NextDouble() * size.Width;
                    rect.Width = rect.Height = width;
                    this.RenderTarget.FillRectangle(rect, this.redBrush);
                }

                string text = string.Format("Test");
                //this.RenderTarget.DrawText(text, this.textFormat, new D2D.RectF(10, 10, 100, 20), this.whiteBrush);

                this.RenderTarget.EndDraw();

                Console.WriteLine("BarChart Rendering Millis: " + sw.ElapsedMilliseconds);
            }
        }
    }
}
