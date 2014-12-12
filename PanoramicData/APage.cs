using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Runtime.Serialization;
using System.IO;
using starPadSDK.Geom;
using starPadSDK.Inq;
using starPadSDK.AppLib;
using CombinedInputAPI;
using PanoramicDataDBConnector;
using PanoramicDataModel;
using starPadSDK.WPFHelp;
using Action = System.Action;
using PanoramicData.view.schema;
using PanoramicData.model.view;
using PanoramicData.utils.inq;
using PanoramicData.view.filter;
using PanoramicData.controller.physics;

namespace PanoramicData
{
    /// <summary>
    /// APage is an example of what you might do to customize an InqScene to do application specific things
    /// It defines a Gesture CommandSet, and marks a special region of the inking surface as title region for use in a Tabbed display
    /// </summary>
    [Serializable]
    public class APage : InqScene, ISerializable
    {
        private Point _startDrag1 = new Point();
        private Point _startDrag2 = new Point();

        private Point _current1 = new Point();
        private Point _current2 = new Point();
        private double length = 0.0;

        private TouchDevice _dragDevice1 = null;
        private TouchDevice _dragDevice2 = null;

        private Stopwatch _lastTapTimer = new Stopwatch();
        private Stopwatch _upTimer = new Stopwatch();

        private Point _currentRefPoint = new Point();

        private SchemaViewer _schemaViewer = new SchemaViewer();
        private IDisposable _tableModelDisposable = null;

        private bool _isDraggable = false;
        public bool IsDraggable
        {
            get
            {
                return _isDraggable;
            }
            set
            {
                _isDraggable = value;
                if (_isDraggable)
                {
                    this.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(aPage_TouchDownEvent));
                }
                else
                {
                    this.RemoveHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(aPage_TouchDownEvent));
                }
            }
        }

        public StroqCollection AxisStroqs { get; set; }

        /// <summary>
        /// This creates an InqScene and installs the initial gesture set
        /// </summary>
        public APage()
        {
            AxisStroqs = new StroqCollection();

            StroqAddedEvent += new StroqHandler(stroqAddedEvent);
            StroqRemovedEvent += new StroqHandler(stroqRemovedEvent);
            StroqsAddedEvent += new StroqsHandler(stroqsAddedEvent);
            StroqsRemovedEvent += new StroqsHandler(stroqsRemovedEvent);
            ElementRemovedEvent += new ElementHandler(elementRemovedEvent);
            ElementsClearedEvent += new ElementsHandler(elementsClearedEvent);
            ElementAddedEvent += new ElementHandler(elementAddedEvent);
            ElementsAddedEvent += new ElementsHandler(elementsAddedEvent);
            MouseMove += APage_MouseMove;
            StylusInAirMove += APage_StylusInAirMove;
            
            SetImmediateDrag(true); // bcz: if false, then scale/rotate widgets are displayed

            // load ink font
            //if (!SimpleInqFont.LoadInkFont(Properties.Resources.font))
            //{
            //    throw new FileNotFoundException("could not load inq font");
            //}

            _schemaViewer.Width = 190;
            _schemaViewer.Height = 425;
        }
        /*
        public void ClearSchemaViewer()
        {
            _schemaViewer.FilterModel.TableModel.Clear();
        }

        public void SetSchemaViewerFilterModel(PanoramicData.model.view.FilterModel filterModel)
        {
            _schemaViewer.FilterModel = filterModel;
            if (_tableModelDisposable != null)
            {
                _tableModelDisposable.Dispose();
            }
            if (_schemaViewer.FilterModel != null)
            {
                _tableModelDisposable = Observable.FromEventPattern<TableModelUpdatedEventArgs>(
                    _schemaViewer.FilterModel.TableModel, "TableModelUpdated")
                    .Where(
                        arg =>
                            arg.EventArgs != null && arg.EventArgs.Mode != UpdatedMode.UI &&
                            arg.EventArgs.Mode != UpdatedMode.FilteredItemsStatus)
                    .Throttle(TimeSpan.FromMilliseconds(50))
                    .Subscribe((arg) =>
                    {
                        Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                        {
                            if (arg.EventArgs.Mode == UpdatedMode.Database)
                            {
                                if (_schemaViewer != null)
                                {
                                    _schemaViewer.Init();
                                }
                            }
                        }));
                    });

                _schemaViewer.Init();
            }
        }
        */
        public void ClearInput()
        {
            _dragDevice1 = null;
            _dragDevice2 = null;
        }

        override protected CommandSet initCommands() { return new ACommandSet(this); }

        void APage_MouseMove(object sender, MouseEventArgs e)
        {
            NotifyDeviceMoved(e.GetPosition(this));
        }

        void APage_StylusInAirMove(object sender, StylusEventArgs e)
        {
            if (e.InAir)
            {
                NotifyDeviceMoved(e.GetPosition(this));
            }
        }

        void stroqAddedEvent(Stroq s)
        {
            var stroqListeners = this.GetIntersectedTypesRecursive<StroqListener>(s);

            foreach (var sl in stroqListeners)
            {
                ((StroqListener)sl).NotifyStroqAdded(s);
                return;
            }
        }

        void stroqsAddedEvent(Stroq[] stroqs)
        {
            StroqCollection sc = new StroqCollection(stroqs);

            var stroqListeners = this.GetIntersectedTypesRecursive<StroqListener>(sc.GetBounds());

            foreach (var sl in stroqListeners)
            {
                ((StroqListener)sl).NotifyStroqsAdded(sc);
                return;
            }
        }

        void stroqRemovedEvent(Stroq s)
        {
            if (AxisStroqs.Contains(s))
            {
                AxisStroqs.Remove(s);
            }
            var stroqListeners = this.GetIntersectedTypesRecursive<StroqListener>(s);

            foreach (var sl in stroqListeners)
            {
                ((StroqListener)sl).NotifyStroqRemoved(s);
                return;
            }
        }

        void stroqsRemovedEvent(Stroq[] stroqs)
        {
            foreach (var s in stroqs)
            {
                if (AxisStroqs.Contains(s))
                {
                    AxisStroqs.Remove(s);
                }
            }
            StroqCollection sc = new StroqCollection(stroqs);

            var stroqListeners = this.GetIntersectedTypesRecursive<StroqListener>(sc.GetBounds());

            foreach (var sl in stroqListeners)
            {
                ((StroqListener)sl).NotifyStroqsRemoved(sc);
                return;
            }
        }

        void elementRemovedEvent(FrameworkElement e)
        {
            if (e is FilterHolder)
            {
                FilterHolder fh = e as FilterHolder;
                foreach (var fe in this.Elements.Where((t) => t is FilterModelAttachment))
                {
                    FilterModelAttachment edge = (fe as FilterModelAttachment);
                    if (edge.Destination.GetIncomingFilterModels(FilteringType.Filter).Contains(fh.FilterHolderViewModel))
                    {
                        edge.Destination.RemoveIncomingFilter(fh.FilterHolderViewModel, FilteringType.Filter);
                    }
                    if (edge.Destination.GetIncomingFilterModels(FilteringType.Brush).Contains(fh.FilterHolderViewModel))
                    {
                        edge.Destination.RemoveIncomingFilter(fh.FilterHolderViewModel, FilteringType.Brush);
                    }
                    if (edge.Destination == fh.FilterHolderViewModel)
                    {
                        Rem(edge);
                    }
                }
                //fh.FilterHolderViewModel.TableModel.RemoveNamedFilterModel(fh.FilterHolderViewModel);
            }
            else if (e is CombinedFilterHolder)
            {
               /* CombinedFilterHolder cfh = e as CombinedFilterHolder;
                foreach (var fe in this.Elements.Where((t) => t is FilterHolderInEdge))
                {
                    FilterHolderInEdge edge = (fe as FilterHolderInEdge);
                    if (edge.Destination == cfh.FilterHolderViewModel || edge.Source == cfh.FilterHolderViewModel)
                    {
                        (fe as FilterHolderInEdge).Destination.RemoveIncomingFilter((fe as FilterHolderInEdge).Source);
                    }
                }
                cfh.FilterHolderViewModel.TableModel.RemoveIncomingFilter(cfh.FilterHolderViewModel);
                cfh.FilterHolderViewModel.Removed = true;*/
            }
            else if (e is FilterModelAttachment)
            {
                (e as FilterModelAttachment).CleanUp();
            }

            if (e is MovableElement)
            {
                PhysicsController.Instance.RemovePhysicalObject(e as MovableElement);
            }
        }

        void elementAddedEvent(FrameworkElement e)
        {
           
        }

        void elementsAddedEvent(FrameworkElement[] elems)
        {
           
        }

        void elementsClearedEvent(FrameworkElement[] elems)
        {
            foreach (var e in elems)
            {
                elementRemovedEvent(e);
            }
        }

        void aPage_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            if ((e.TouchDevice is MouseTouchDevice) && (e.TouchDevice as MouseTouchDevice).IsStylus)
                return;

            
            var elements = new List<FrameworkElement>(this.GetIntersectedElements(new Rct(e.GetTouchPoint(this).Position, new Vec(1,1))).Where((t) => t is
                FrameworkElement).Select((t) => t as FrameworkElement));

            if (elements.Count > 0)
                return;
            
            if (_dragDevice1 == null)
            {
                _upTimer.Restart();
                e.Handled = true;
                e.TouchDevice.Capture(this);
                _startDrag1 = e.GetTouchPoint((FrameworkElement)this.Parent).Position;
                _current1 = e.GetTouchPoint((FrameworkElement)this).Position;

                this.AddHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(aPage_TouchMoveEvent));
                this.AddHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(aPage_TouchUpEvent));
                _dragDevice1 = e.TouchDevice;
            }
            else if (_dragDevice2 == null)
            {
                e.Handled = true;
                e.TouchDevice.Capture(this);
                _dragDevice2 = e.TouchDevice;
                _current2 = e.GetTouchPoint((FrameworkElement)this).Position;
            }
        }

        void aPage_TouchUpEvent(object sender, TouchEventArgs e)
        {
            if (e.TouchDevice == _dragDevice1)
            {
                e.Handled = true;
                e.TouchDevice.Capture(null);
                _dragDevice1 = null;
                length = 0.0;

                if (_dragDevice2 != null)
                {
                    _dragDevice1 = _dragDevice2;
                    _startDrag1 = _startDrag2;
                    _dragDevice2 = null;
                }
                else
                {
                    this.RemoveHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(aPage_TouchMoveEvent));
                    this.RemoveHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(aPage_TouchUpEvent));
                }

                Console.WriteLine();
                Console.WriteLine(_upTimer.ElapsedMilliseconds);
                Console.WriteLine(_lastTapTimer.ElapsedMilliseconds);
                if (_upTimer.ElapsedMilliseconds < 200 && _lastTapTimer.ElapsedMilliseconds != 0 &&
                    _lastTapTimer.ElapsedMilliseconds < 300)
                {
                    Pt pos = e.GetTouchPoint(this).Position;
                    _schemaViewer.RenderTransform = new TranslateTransform(
                        pos.X - _schemaViewer.Width / 2.0,
                        pos.Y - _schemaViewer.Height / 2.0);
                    this.AddNoUndo(_schemaViewer);
                    this.UpdateLayout();
                    Console.WriteLine((pos));
                    Console.WriteLine(this.TranslatePoint(pos, _schemaViewer));
                }


                _upTimer.Stop();
                _lastTapTimer.Restart();
            }
            else if (e.TouchDevice == _dragDevice2)
            {
                e.Handled = true;
                e.TouchDevice.Capture(null);
                _dragDevice2 = null;
                length = 0.0;
            }

        }

        void aPage_TouchMoveEvent(object sender, TouchEventArgs e)
        {
            Point curDrag = e.GetTouchPoint((FrameworkElement)this.Parent).Position;

            if (e.TouchDevice == _dragDevice1)
            {
                if (_dragDevice2 == null)
                {
                    Vector dragBy = curDrag - _startDrag1;
                    this.RenderTransform = new MatrixTransform(((Mat)this.RenderTransform.Value) * Mat.Translate(dragBy));
                }
                _startDrag1 = curDrag;
                _current1 = e.GetTouchPoint((FrameworkElement)this).Position;
                e.Handled = true;
            }
            else if (e.TouchDevice == _dragDevice2)
            {
                _startDrag2 = curDrag;
                _current2 = e.GetTouchPoint((FrameworkElement)this).Position;
                e.Handled = true;
            }

            if (_dragDevice1 != null && _dragDevice2 != null)
            {
                double newLength = (_current1.GetVec() - _current2.GetVec()).Length;
                if (length != 0.0)
                {
                    Vector scalePos = (_current1.GetVec() + _current2.GetVec()) / 2.0;
                    double scale = newLength / length;

                    Matrix m1 = this.RenderTransform.Value;
                    m1.ScaleAtPrepend(scale, scale, scalePos.X, scalePos.Y);

                    this.RenderTransform = new MatrixTransform(m1);
                }
                length = newLength;
            }
        }

        override protected void init()
        {
            base.init();
        }
    }
}
