using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using GeoAPI.Geometries;
using Microsoft.Ink;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.Charts;
using Microsoft.Research.DynamicDataDisplay.Charts.Axes;
using Microsoft.Research.DynamicDataDisplay.Common.Auxiliary;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Microsoft.Research.DynamicDataDisplay.PointMarkers;
using starPadSDK.AppLib;
using PixelLab.Common;
using System.ComponentModel;
using PanoramicDataModel;
using starPadSDK.Geom;
using starPadSDK.Inq;
using starPadSDK.WPFHelp;
using PanoramicData.view.filter;
using PanoramicData.controller.data;
using PanoramicData.view.other;
using PanoramicData.model.view;

namespace PanoramicData.view.vis
{
    /// <summary>
    /// Interaction logic for SliderFilterRenderer.xaml
    /// </summary>
    public partial class SliderFilterRenderer : FilterRenderer
    {
        private long _dragTime = DateTime.Now.Ticks;
        private delegate List<List<object>> ExecuteQueryDelegate(string schema, string query);
        private List<DataWrapper<PanoramicDataRow>> _currentRows = new List<DataWrapper<PanoramicDataRow>>();
        AsyncVirtualizingCollection<PanoramicDataRow> _currentDataValues = null;
        private Rct _strokeBounds = Rect.Empty;
        private SimpleAPage _aPage = new SimpleAPage();
        private Canvas _feedbackCanvas = new Canvas();
        private List<RealBoundRenderer> _realBoundRenderers = new List<RealBoundRenderer>(); 

        private double _max = 0.0;
        private double _min = 0.0;
        
        public SliderFilterRenderer()
            : this(false)
        {
        }

        public SliderFilterRenderer(bool showSettings)
        {
            InitializeComponent();
            _aPage.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
            cnvApage.Children.Add(_aPage);
            cnvApage.Children.Add(_feedbackCanvas);
            _aPage.StroqAddedEvent += new starPadSDK.AppLib.InqScene.StroqHandler(stroqAddedEvent);
            _aPage.StroqRemovedEvent += new starPadSDK.AppLib.InqScene.StroqHandler(stroqRemovedEvent);
            _aPage.StroqsAddedEvent += new starPadSDK.AppLib.InqScene.StroqsHandler(stroqsAddedEvent);
            _aPage.StroqsRemovedEvent += new starPadSDK.AppLib.InqScene.StroqsHandler(stroqsRemovedEvent);
        }

        void DataValues_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            AsyncVirtualizingCollection<PanoramicDataRow> values = (AsyncVirtualizingCollection<PanoramicDataRow>)sender;
            
            foreach (var row in values)
            {
                row.PropertyChanged += row_PropertyChanged;
                _currentRows.Add(row);
            }
        }

        void row_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == "Data" &&
                    FilterModel.ColumnDescriptors.Count == 3)
                {
                    PanoramicDataRow loadedRow = ((DataWrapper<PanoramicDataRow>) sender).Data;
                    loadedRow.PropertyChanged -= row_PropertyChanged;
                    string minStrg = loadedRow.GetValue(FilterModel.ColumnDescriptors[0]).StringValue;
                    string maxStrg = loadedRow.GetValue(FilterModel.ColumnDescriptors[1]).StringValue;

                    double dMin = 0.0;
                    bool nullMin = getPointValue(loadedRow, FilterModel.ColumnDescriptors[0], out dMin);
                    double dMax = 0.0;
                    bool nullMax = getPointValue(loadedRow, FilterModel.ColumnDescriptors[1], out dMax);

                    if (!nullMin && ! nullMax)
                    {
                        _min = dMin;
                        _max = dMax;

                        Label l1 = new Label();
                        l1.RenderTransform = new TranslateTransform(_strokeBounds.Left - 30, _strokeBounds.Top - 30);
                        l1.Width = 60;
                        l1.Height = 30;
                        l1.HorizontalContentAlignment = HorizontalAlignment.Center;
                        l1.VerticalContentAlignment = VerticalAlignment.Center;
                        l1.Content = minStrg;

                        Label l2 = new Label();
                        l2.RenderTransform = new TranslateTransform(_strokeBounds.Right - 35, _strokeBounds.Top - 30);
                        l2.Width = 60;
                        l2.Height = 30;
                        l2.HorizontalContentAlignment = HorizontalAlignment.Center;
                        l2.VerticalContentAlignment = VerticalAlignment.Center;
                        l2.Content = maxStrg;

                        cnvLabel.Children.Clear();
                        cnvLabel.Children.Add(l1);
                        cnvLabel.Children.Add(l2);

                        triggerAlgorithm();
                    }
                }
            }
            catch (Exception exc)
            {
            }
        }

        bool getPointValue(PanoramicDataRow loadedRow, PanoramicDataColumnDescriptor columnDescriptor, out double value)
        {
            bool isNull = false;
            value = 0.0;
            string dataType = FilterModel.GetDataTypeOfPanoramicDataColumnDescriptor(columnDescriptor, true);
            PanoramicDataValue dataValue = loadedRow.GetValue(columnDescriptor);

            if (dataType == DataTypeConstants.FLOAT ||
                dataType == DataTypeConstants.INT)
            {
                double d = 0;
                if (dataValue.Value != DBNull.Value && double.TryParse(dataValue.StringValue, out d))
                {

                    value = d;
                }
                else
                {
                    isNull = true;
                }
            }
            else if (dataType == DataTypeConstants.NVARCHAR ||
                     dataType == DataTypeConstants.GEOGRAPHY ||
                     dataType == DataTypeConstants.GUID)
            {
                value = 0.0;
                isNull = true;
            }
            else if (dataType == DataTypeConstants.DATE ||
                     dataType == DataTypeConstants.TIME)
            {
                value = 0.0;
                isNull = true;
            }

            return isNull;
        }

        protected override void Init(bool resetViewport)
        {
            if (FilterModel == null)
            {
                return;
            }
            
            inkPresenter.Strokes.Clear();
            Stroq s = (FilterModel as FilterHolderViewModel).Stroq.Clone();
            s.Move(new Vec(30, 30));

            inkPresenter.Strokes.Add(s.BackingStroke);
            _strokeBounds = s.GetBounds();
            
            Canvas.SetTop(_aPage, _strokeBounds.Top - 10);
            Canvas.SetLeft(_aPage, _strokeBounds.Left - 10);
            _aPage.Width = _strokeBounds.Width + 20;
            _aPage.Height = _strokeBounds.Height + 20;

            Canvas.SetTop(_feedbackCanvas, _strokeBounds.Bottom + 5);
            Canvas.SetLeft(_feedbackCanvas, _strokeBounds.Left - 10);
            _feedbackCanvas.Width = _strokeBounds.Width + 20;
            _feedbackCanvas.Height = _strokeBounds.Height + 20;

            // clean out old rows
            foreach (var row in _currentRows)
            {
                row.PropertyChanged -= row_PropertyChanged;
            }
            _currentRows.Clear();
            if (_currentDataValues != null)
            {
                _currentDataValues.CollectionChanged -= this.DataValues_CollectionChanged;
            }
            
            QueryItemsProvider queryItemsProvider = new QueryItemsProvider(new FilterQueryGenerator(FilterModel));
            _currentDataValues = new AsyncVirtualizingCollection<PanoramicDataRow>(queryItemsProvider, 1000 /*page size*/, 1000 /*timeout*/);

            _currentDataValues.CollectionChanged += this.DataValues_CollectionChanged;

            Console.WriteLine(_currentDataValues.Count);
        }

        void DataValues_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            /*if (listView.ItemsSource != null && listView.ItemsSource is AsyncVirtualizingCollection<List<DataValue>>)
            {
                AsyncVirtualizingCollection<List<DataValue>> dataValues = (AsyncVirtualizingCollection<List<DataValue>>) listView.ItemsSource;
                if (dataValues.IsInitializing || dataValues.IsLoading)
                {
                    //loadingAnim.Visibility = System.Windows.Visibility.Visible;
                }
                else
                {
                    //loadingAnim.Visibility = System.Windows.Visibility.Collapsed;
                }
            }*/
        }

        void stroqAddedEvent(Stroq s)
        {
            triggerAlgorithm();
        }

        void stroqsAddedEvent(Stroq[] stroqs)
        {
            triggerAlgorithm();
        }

        void stroqRemovedEvent(Stroq s)
        {
            triggerAlgorithm();
        }

        void stroqsRemovedEvent(Stroq[] stroqs)
        {
            triggerAlgorithm();
        }

        private void updateFilterItems()
        {
            FilterModel.RemoveFilteredItems(FilterModel.FilteredItems.ToArray().ToList(), this);
            foreach (var realBoundRenderer in _realBoundRenderers)
            {
                // lower bound

                FilteredItem fi = new FilteredItem();

                PanoramicDataValue val = new PanoramicDataValue();
                val.Value = realBoundRenderer.RealBound.Left;
                val.DataType = DataTypeConstants.FLOAT;
                fi.ColumnComparisonValues.Add(FilterModel.ColumnDescriptors[2], new PanoramicDataValueComparison(val, Predicate.GREATER_THAN));

                
                // lower bound
                val = new PanoramicDataValue();
                val.Value = realBoundRenderer.RealBound.Right;
                val.DataType = DataTypeConstants.FLOAT;
                fi.GroupComparisonValues.Add(FilterModel.ColumnDescriptors[2], new PanoramicDataValueComparison(val, Predicate.LESS_THAN));
                FilterModel.AddFilteredItem(fi, this);
            }
        }

        private void triggerAlgorithm()
        {
            foreach (var rr in _realBoundRenderers)
            {
                rr.MouseDown -= r_MouseDown;
            }
            _feedbackCanvas.Children.Clear();
            _realBoundRenderers.Clear();
            
            List<KeyValuePair<Rct, StroqCollection>> realBounds = new List<KeyValuePair<Rct, StroqCollection>>();

            if (_aPage.Stroqs.Count > 0)
            {
                double mostLeft = 0.0;
                bool[] hist = getHistogram(_aPage.Stroqs, out mostLeft);
                List<Rct> segBounds = getSegmentationBounds(hist);
                Rct feedbackRct = new Rct(0,0,_aPage.GetBounds().Width, _aPage.GetBounds().Height);

                double length = _max - _min;

                foreach (var segBound in segBounds)
                {
                    Rct newSegBound = new Rct(segBound);
                    newSegBound.Left += mostLeft;
                    newSegBound.Right += mostLeft;

                    Rct inter = feedbackRct.Intersection(newSegBound);
                    if (!inter.IsNull())
                    {
                        inter.Top = 0;
                        inter.Bottom = 1;
                        inter.Left -= feedbackRct.Left;
                        inter.Left = (inter.Left/feedbackRct.Width)*length + _min;
                        inter.Right -= feedbackRct.Left;
                        inter.Right = (inter.Right/feedbackRct.Width)*length + _min;

                        StroqCollection stroqs = new StroqCollection();
                        Rct tempSegBound = new Rct(newSegBound);
                        tempSegBound.Bottom += 400;
                        foreach (var stroq in _aPage.Stroqs)
                        {
                            if (stroq.GetBounds().IntersectsWith(tempSegBound))
                            {
                                stroqs.Add(stroq);
                            }
                        }

                        realBounds.Add(new KeyValuePair<Rct, StroqCollection>(inter, stroqs));
                    }
                }

                foreach (var realBound in realBounds)
                {
                    double left = ((realBound.Key.Left - _min)/(_max - _min))*feedbackRct.Width + feedbackRct.Left;
                    double right = ((realBound.Key.Right - _min)/(_max - _min))*feedbackRct.Width + feedbackRct.Left;
                    
                    RealBoundRenderer r = new RealBoundRenderer(realBound.Key, right - left, realBound.Value);

                    r.RenderTransform = new TranslateTransform(
                        (left + (right - left) / 2.0) - 15,
                        feedbackRct.Top + realBound.Key.Top - 1);
                    _feedbackCanvas.Children.Add(r);

                    r.MouseDown += r_MouseDown;
                    _realBoundRenderers.Add(r);
                }
            }
            updateFilterItems();
        }
        private Point _realBoundRendererStartDrag1 = new Point();

        void r_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            RealBoundRenderer rr = sender as RealBoundRenderer;
            e.MouseDevice.Capture(rr);
            _realBoundRendererStartDrag1 = rr.TranslatePoint(e.GetPosition(rr), (FrameworkElement)_feedbackCanvas);

            rr.MouseUp += rr_MouseUp;
            rr.MouseMove += rr_MouseMove;
        }

        void rr_MouseUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            RealBoundRenderer rr = sender as RealBoundRenderer;
            e.MouseDevice.Capture(null);

            rr.MouseUp -= rr_MouseUp;
            rr.MouseMove -= rr_MouseMove;

            triggerAlgorithm();
        }

        void rr_MouseMove(object sender, MouseEventArgs e)
        {
            e.Handled = true;
            RealBoundRenderer rr = sender as RealBoundRenderer;
            Point curDrag = rr.TranslatePoint(e.GetPosition(rr), (FrameworkElement)_feedbackCanvas);
            Vector vec = curDrag - _realBoundRendererStartDrag1;
           
            double currX = (rr.RenderTransform as TranslateTransform).X;
            
            rr.RenderTransform = new TranslateTransform(
                Math.Min(Math.Max(-15, (rr.RenderTransform as TranslateTransform).X + vec.X), _feedbackCanvas.Width - rr.Width),
                (rr.RenderTransform as TranslateTransform).Y);
            rr.Stroqs.Move(new Vec((rr.RenderTransform as TranslateTransform).X - currX, 0));
            
            Rct feedbackRct = new Rct(0,0,_aPage.GetBounds().Width, _aPage.GetBounds().Height);
            double length = _max - _min;
            Rct newSegBound = rr.Stroqs.GetBounds();
            Rct inter = feedbackRct.Intersection(newSegBound);
            if (!inter.IsNull())
            {
                inter.Top = 0;
                inter.Bottom = 1;
                inter.Left -= feedbackRct.Left;
                inter.Left = (inter.Left/feedbackRct.Width)*length + _min;
                inter.Right -= feedbackRct.Left;
                inter.Right = (inter.Right/feedbackRct.Width)*length + _min;
                rr.RealBound = inter;
            }

            updateFilterItems();
            _realBoundRendererStartDrag1 = curDrag;
        }

        private bool[] getHistogram(StroqCollection stroqs, out double mostLeft)
        {
            List<Rct> bounds = new List<Rct>();

            // calculate stroq boundaries
            Rct allInputBounds = Rct.Null;
            foreach (var stroq in stroqs)
            {
                bounds.Add(stroq.GetBounds());
                allInputBounds = allInputBounds.Union(stroq.GetBounds());
            }

            bool[] xHist = new bool[(int)Math.Ceiling(allInputBounds.Width)];

            foreach (var b in bounds)
            {
                double xFrom = b.Left - allInputBounds.Left;
                double xTo = xFrom + b.Width;
                for (int x = (int)Math.Floor(xFrom); x < (int)Math.Ceiling(xTo); x++)
                {
                    xHist[x] = true;
                }
            }
            mostLeft = allInputBounds.Left;
            return xHist;
        }

        private List<Range> getRanges(bool[] hist)
        {
            List<Range> ret = new List<Range>();
            Range r = new Range();
            bool ink = hist[0];
            double size = 0.0;
            for (int i = 0; i < hist.Length; i++)
            {
                bool flipped = false;
                if (hist[i] && !ink)
                {
                    flipped = true;
                }
                else if (!hist[i] && ink)
                {
                    flipped = true;
                }
                if (flipped)
                {
                    r.Size = size;
                    r.Ink = ink;
                    ret.Add(r);
                    r = new Range();

                    ink = !ink;
                    size = 1.0;
                }
                else
                {
                    size += 1;
                }
            }
            r.Size = size;
            r.Ink = ink;
            ret.Add(r);
            return ret;
        }

        private List<Rct> getSegmentationBounds(bool[] hist)
        {
            List<Rct> segmentation = new List<Rct>();
            List<Range> cols = getRanges(hist);
            int nrOfCols = 0;
            double xOffset = 0.0;
            foreach (var col in cols)
            {
                if (col.Ink)
                {
                    nrOfCols++;
                    Rect r = new Rect(xOffset, 0, col.Size, 3);
                    segmentation.Add(r);
                }
                xOffset += col.Size;
            }
            return segmentation;
        }

        public class Range
        {
            public double Size { get; set; }
            public bool Ink { get; set; }
        }

    }

    public class RealBoundRenderer : Canvas
    {
        public Rct RealBound { get; set; }
        public StroqCollection Stroqs { get; set; }
    
        public RealBoundRenderer(Rct realBound, double width, StroqCollection stroqs)
        {
            this.Width = 30;// Math.Max(30, width);

            this.RealBound = realBound;
            this.Stroqs = stroqs;


            Ellipse e = new Ellipse();
            e.Width = 30;
            e.Height = 30;
            e.RenderTransform = new TranslateTransform(0, 5);
            e.Stroke = Brushes.Gray;
            e.Fill = Brushes.White;
            e.StrokeThickness = 3;
            this.Children.Add(e);
        }
    }
}
