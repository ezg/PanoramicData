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
using PanoramicData.controller.physics;
using PanoramicData.view.vis;

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
                f = Observable.FromEventPattern<TableModelUpdatedEventArgs>(
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
            if (e is VisualizationContainerView)
            {
                VisualizationContainerView fh = e as VisualizationContainerView;
                foreach (var fe in this.Elements.Where((t) => t is LinkView))
                {
                    /*FilterModelAttachment edge = (fe as FilterModelAttachment);
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
                    }*/
                }
                //fh.FilterHolderViewModel.TableModel.RemoveNamedFilterModel(fh.FilterHolderViewModel);
            }
            else if (e is LinkView)
            {
               //(e as LinkView).CleanUp();
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

        override protected void init()
        {
            base.init();
        }
    }
}
