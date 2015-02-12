using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CombinedInputAPI;
using PixelLab.Common;
using starPadSDK.AppLib;
using starPadSDK.Geom;
using PanoramicData.model.view;
using PanoramicData.view.other;
using System.Reactive.Linq;
using PanoramicDataModel;
using PanoramicData.controller.view;

namespace PanoramicData.view.schema
{
    /// <summary>
    /// Interaction logic for SchemaViewer.xaml
    /// </summary>
    public partial class SchemaViewer : UserControl
    {
        private Point _startDrag1 = new Point();
        private Point _current1 = new Point();
        private TouchDevice _dragDevice1 = null;
        private Delegate _outsidePointDelegate = null;
        private IDisposable _schemaViewModelDisposable = null;

        public SchemaViewer()
        {
            InitializeComponent();
            headerGrid.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(headerGrid_TouchDownEvent));
            resizeGrid.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(resizeGrid_TouchDownEvent));
            plusCanvas.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(plusCanvas_TouchDownEvent));
            _outsidePointDelegate = new EventHandler<TouchEventArgs>(PointOutsideDownEvent);
            Application.Current.MainWindow.AddHandler(FrameworkElement.TouchDownEvent, _outsidePointDelegate, true);

            this.DataContextChanged += SchemaViewer_DataContextChanged;
        }

        void SchemaViewer_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                (e.OldValue as SchemaViewModel).PropertyChanged -= SchemaViewer_PropertyChanged;
                if (_schemaViewModelDisposable != null)
                {
                    _schemaViewModelDisposable.Dispose();
                }
            }
            if (e.NewValue != null)
            {
                SchemaViewModel model = (e.NewValue as SchemaViewModel);
                model.PropertyChanged += SchemaViewer_PropertyChanged;

                _schemaViewModelDisposable = Observable.FromEventPattern<SchemaViewModelUpdatedEventArgs>(
                    model, "SchemaViewModelUpdated")
                    .Where(
                        arg => arg.EventArgs != null)
                    .Throttle(TimeSpan.FromMilliseconds(50))
                    .Subscribe((arg) =>
                    {
                        Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                        {
                            this.updateRendering();
                        }));
                    });
                
                this.updateRendering();
            }
        }
        
        void SchemaViewer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
        }

        private void updateRendering()
        {
            tree.InitTree((DataContext as SchemaViewModel));
        }

        void PointOutsideDownEvent(Object sender, TouchEventArgs e)
        {
            if (e.Source != this)
            {
                //Application.Current.MainWindow.RemoveHandler(FrameworkElement.TouchDownEvent, _outsidePointDelegate);
            }
        }

        void plusCanvas_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            /*InqScene inqScene = this.FindParent<InqScene>();
            Point fromInqScene = e.GetTouchPoint(inqScene).Position;

            CalculatedColumnDescriptorInfo calculatedColumnDescriptorInfo = new CalculatedColumnDescriptorInfo();
            calculatedColumnDescriptorInfo.Name = "Calculated Field " + FilterModel.TableModel.CalculatedColumnDescriptorInfos.Count;
            calculatedColumnDescriptorInfo.TableModel = FilterModel.TableModel;

            MathEditor me = new MathEditor(
                new ResizerMathEditorExecution(inqScene, calculatedColumnDescriptorInfo), FilterModel, calculatedColumnDescriptorInfo);
            me.SetPosition(fromInqScene.X - RadialControl.SIZE / 2,
                fromInqScene.Y - RadialControl.SIZE / 2);
            inqScene.AddNoUndo(me);

            e.Handled = true;
            
            if (inqScene != null)
            {
                //inqScene.Rem(this);
            }*/
        }

        void headerGrid_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            if (_dragDevice1 == null)
            {
                e.Handled = true;

                e.TouchDevice.Capture(headerGrid);
                _startDrag1 = e.GetTouchPoint((IInputElement)this.Parent).Position;
                _current1 = e.GetTouchPoint((IInputElement)this.Parent).Position;

                headerGrid.AddHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(headerGrid_TouchMoveEvent));
                headerGrid.AddHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(headerGrid_TouchUpEvent));

                _dragDevice1 = e.TouchDevice;
            }
        }

        void headerGrid_TouchUpEvent(Object sender, TouchEventArgs e)
        {
            if (e.TouchDevice == _dragDevice1)
            {
                e.Handled = true;
                e.TouchDevice.Capture(null);
                _dragDevice1 = null;
                headerGrid.RemoveHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(headerGrid_TouchMoveEvent));
                headerGrid.RemoveHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(headerGrid_TouchUpEvent));
            }
        }

        void headerGrid_TouchMoveEvent(Object sender, TouchEventArgs e)
        {
            if (e.TouchDevice == _dragDevice1)
            {
                Point curDrag = e.GetTouchPoint((IInputElement)this.Parent).Position;

                Vector vec = curDrag - _startDrag1;
                Point dragDelta = new Point(vec.X, vec.Y);

                _startDrag1 = curDrag;
                _current1 = curDrag;
                e.Handled = true;

                (DataContext as SchemaViewModel).Position = new Point(
                    (DataContext as SchemaViewModel).Position.X + dragDelta.X,
                    (DataContext as SchemaViewModel).Position.Y + dragDelta.Y);
            }
        }

        void resizeGrid_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            //if (e.DeviceType != InputFramework.DeviceType.MultiTouch)
            //return;

            if (_dragDevice1 == null)
            {
                e.Handled = true;
                
                e.TouchDevice.Capture(resizeGrid);
                _startDrag1 = e.GetTouchPoint((IInputElement)this.Parent).Position;
                _current1 = e.GetTouchPoint((IInputElement)this.Parent).Position;

                resizeGrid.AddHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(resizeGrid_TouchMoveEvent));
                resizeGrid.AddHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(resizeGrid_TouchUpEvent));
                _dragDevice1 = e.TouchDevice;
            }
        }

        void resizeGrid_TouchUpEvent(object sender, TouchEventArgs e)
        {
            if (e.TouchDevice == _dragDevice1)
            {
                e.Handled = true;
                e.TouchDevice.Capture(null);
                _dragDevice1 = null;
                resizeGrid.RemoveHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(resizeGrid_TouchMoveEvent));
                resizeGrid.RemoveHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(resizeGrid_TouchUpEvent));
            }
        }

        void resizeGrid_TouchMoveEvent(object sender, TouchEventArgs e)
        {
            Point curDrag = e.GetTouchPoint((IInputElement)this.Parent).Position;

            if (e.TouchDevice == _dragDevice1)
            {
                Vec currentSize = (DataContext as SchemaViewModel).Size;
                Vec currentMinSize = new Vec(100, 100);
                Vec vec = curDrag - _startDrag1;
                Point dragDelta = new Point(vec.X, vec.Y);

                _startDrag1 = curDrag;
                _current1 = e.GetTouchPoint((IInputElement)this.Parent).Position;
                e.Handled = true;

                Vec newSize = new Vec(currentSize.X + dragDelta.X, currentSize.Y + dragDelta.Y);

                (DataContext as SchemaViewModel).Size = new Vec(
                    Math.Max(newSize.X, currentMinSize.X),
                    Math.Max(newSize.Y, currentMinSize.Y));

                this.Width = Math.Max(newSize.X, currentMinSize.X);
                this.Height = Math.Max(newSize.Y, currentMinSize.Y);
            }
        }
    }
}
