using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Windows.Controls.Ribbon;
using BrownRecognitionCommon;
using starPadSDK.Inq;

namespace SharpShapes
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow()
        {
            try { 
                InitializeComponent(); 
            } 
            catch ( Exception ex ) 
            { 
            
            } 
        }

        private void btnCleanUp_DropDownOpened(object sender, EventArgs e)
        {
            btnCleanUp.Items.Clear();

            foreach (BrownTemplate template in shapeCanvas.CurrentRecognizedTemplates)
            {
                InkCanvas dummyInqCanvas = new InkCanvas();
                dummyInqCanvas.Background = shapeCanvas.InqCanvas.Background;

                foreach (Stroq s in shapeCanvas.InqCanvas.Stroqs)
                {
                    dummyInqCanvas.Strokes.Add(s.BackingStroke);
                }
                foreach (UIElement c in shapeCanvas.InqCanvas.Children)
                {
                    if (c is BrownShapeRenderer)
                    {
                        BrownShapeRenderer bsr = c as BrownShapeRenderer;
                        dummyInqCanvas.Children.Add(new BrownShapeRenderer(bsr.BrownShape, bsr.BrownTemplate));
                    }
                }
                
                List<BrownShape> elements = BrownRecognitionAPI.API.GetCleanTemplateShapes(template);

                // add all the cleaned up geometry. 
                foreach (BrownShape bs in elements)
                {
                    // remove the stroqs that were part of this brownShape
                    foreach (BrownInputStroke bis in bs.BrownInputStrokes)
                    {
                        if (bis.Data is Stroq)
                        {
                            dummyInqCanvas.Strokes.Remove((bis.Data as Stroq).BackingStroke);
                        }
                        else if (bis.Data is BrownShapeRenderer)
                        {
                            List<BrownShapeRenderer> removeList = new List<BrownShapeRenderer>();
                            foreach (UIElement c in dummyInqCanvas.Children)
                            {
                                if (c is BrownShapeRenderer)
                                {
                                    BrownShapeRenderer bb = c as BrownShapeRenderer;
                                    if (bb.BrownShape == (bis.Data as BrownShapeRenderer).BrownShape)
                                    {
                                        removeList.Add(bb);
                                    }
                                }
                            }
                            foreach (BrownShapeRenderer bb in removeList)
                            {
                                dummyInqCanvas.Children.Remove(bb);
                            }
                        }
                    }

                    BrownShapeRenderer bsr = new BrownShapeRenderer(bs, template);
                    dummyInqCanvas.Children.Add(bsr);
                }

                Size sizeOfControl = new Size(shapeCanvas.InqCanvas.ActualWidth, shapeCanvas.InqCanvas.ActualHeight);
                dummyInqCanvas.Measure(sizeOfControl);
                dummyInqCanvas.Arrange(new Rect(sizeOfControl));
                RenderTargetBitmap renderBitmap = new RenderTargetBitmap((Int32)sizeOfControl.Width, (Int32)sizeOfControl.Height, 96d, 96d, PixelFormats.Pbgra32);
                renderBitmap.Render(dummyInqCanvas);

                MenuItem menuItem = new MenuItem();
                menuItem.Header = template.TemplateType.ToString();
                Image img = new Image();
                menuItem.Icon = img;
                double ratio = shapeCanvas.InqCanvas.ActualWidth / shapeCanvas.InqCanvas.ActualHeight;
                img.Source = renderBitmap;
                img.Margin = new Thickness(0, 4, 0, 4);
                img.Width = 400;
                img.Height = img.Width / ratio;

                menuItem.Click += (s, args) =>
                {
                    shapeCanvas.CleanUpTemplate(((MenuItem)s).Header.ToString());
                    btnCleanUp.IsEnabled = false;
                    SharpShapesCommands.UpdateAllCanExecute();
                }; 

                btnCleanUp.Items.Add(menuItem);
            }
        }
    }
}
