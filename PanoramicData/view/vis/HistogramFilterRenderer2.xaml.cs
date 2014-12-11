using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using starPadSDK.AppLib;
using OxyPlot;
using OxyPlot.Series;
using System.ComponentModel;
using OxyPlot.Axes;
using PanoramicDataModel;
using System.Windows.Media;
using System.IO;
using PanoramicData.view.filter;
using PanoramicData.controller.data;
using PanoramicData.model.view;

namespace PanoramicData.view.vis
{
    /// <summary>
    /// Interaction logic for HistogramFilterRenderer2.xaml
    /// </summary>
    public partial class HistogramFilterRenderer2 : FilterRenderer
    {
        private long _dragTime = DateTime.Now.Ticks;
        private delegate List<List<object>> ExecuteQueryDelegate(string schema, string query);
        private readonly List<DataWrapper<PanoramicDataRow>> _currentRows = new List<DataWrapper<PanoramicDataRow>>();
        AsyncVirtualizingCollection<PanoramicDataRow> _currentDataValues = null;
        private Dictionary<object, ColumnSeriesItem> _series = null;
        private Dictionary<object, List<HistogramItem>> _data = null;
        private Dictionary<object, PanoramicDataValue[]> _groupingValues = null;
        private int _toLoad = 0;
        private int _loaded = 0;
        private PlotModel _plotModel = null;
        private CategoryAxis _catAxis = null;
        private List<HistogramItem> _labels = null;
        private const string DEFAULT_GROUPING = "default";

        public HistogramFilterRenderer2()
            : this(false)
        {
        }

        public HistogramFilterRenderer2(bool showSettings)
        {
            InitializeComponent();
            this.TouchDown += HistogramFilterRenderer2_TouchDown;
            this.MouseDown += HistogramFilterRenderer2_MouseDown;
        }

        public override byte[] CreateImage()
        {
            plot.SaveBitmap("c:\\temp\\test.png", 800, 600);
            byte[] bytes = File.ReadAllBytes("c:\\temp\\test.png");
            return bytes;
        }

        void DataValues_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            AsyncVirtualizingCollection<PanoramicDataRow> values = (AsyncVirtualizingCollection<PanoramicDataRow>)sender;
            _toLoad = values.Count;
            _loaded = 0;
            
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
                    FilterModel.GetColumnDescriptorsForOption(Option.X).Count > 0 &&
                    FilterModel.GetColumnDescriptorsForOption(Option.Y).Count > 0)
                {
                    PanoramicDataRow loadedRow = ((DataWrapper<PanoramicDataRow>)sender).Data;
                    _loaded++; 
                    loadedRow.PropertyChanged -= row_PropertyChanged;

                    string grouping = DEFAULT_GROUPING;
                    PanoramicDataValue groupingDataValue = null;
                    PanoramicDataValue groupedDataValue = null;
                    if (FilterModel.GetColumnDescriptorsForOption(Option.ColorBy).Count > 0)
                    {
                        groupingDataValue = loadedRow.GetValue(FilterModel.GetColumnDescriptorsForOption(Option.ColorBy)[0]);
                        groupedDataValue = loadedRow.GetGroupedValue(FilterModel.GetColumnDescriptorsForOption(Option.ColorBy)[0]);
                        grouping = groupingDataValue.StringValue;
                    }
                    if (!_data.ContainsKey(grouping))
                    {
                        _data.Add(grouping, new List<HistogramItem>());
                        _groupingValues.Add(grouping, new PanoramicDataValue[] {groupingDataValue, groupedDataValue});
                    }

                    HistogramItem item = new HistogramItem {Row = loadedRow};
                    PanoramicDataValue value = loadedRow.GetValue(FilterModel.GetColumnDescriptorsForOption(Option.Y)[0]);

                    item.Value = double.Parse(value.Value == DBNull.Value ? "0" : value.StringValue);
                    List<string> labelParts = new List<string>();
                    foreach (var cd in FilterModel.GetColumnDescriptorsForOption(Option.X))
                    {
                        string labelPart = loadedRow.GetValue(cd).StringValue;
                        
                        var gdv = loadedRow.GetGroupedValue(cd);
                        if (gdv != null && cd.IsBinned)
                        {
                            if (gdv.Value == DBNull.Value)
                            {
                                gdv.Value = Double.MaxValue;
                                labelPart = "Null";
                            } 
                            item.LabelGroupedDataValue = gdv;
                        }

                        labelParts.Add(labelPart.TrimTo(15));
                    }
                    item.Label = string.Join("\n", labelParts);
                    if (_labels.All(i => i.Label != item.Label))
                    {
                        _labels.Add(item);
                    }
                    _data[grouping].Add(item);

                    // isHighlighted because it was previously selected
                    if (loadedRow.IsHighligthed)
                    {
                        item.Color = OxyColor.FromArgb(FilterModel.Color.A, FilterModel.Color.R, FilterModel.Color.G, FilterModel.Color.B);
                        foreach (var fi in FilterModel.FilteredItems.ToArray())
                        {
                            if (fi.RowNumber == item.Row.RowNumber)
                            {
                                FilterModel.FilteredItems.Remove(fi);
                            }
                        }
                        toggleFilteredItem(item);
                    }

                    // all rows are loaded
                    if (_loaded == _toLoad)
                    {
                        // remove unused / invisible FilterItems
                        List<FilteredItem> toRemove = new List<FilteredItem>();
                        foreach (var fi in FilterModel.FilteredItems.ToArray())
                        {
                            bool found = false;
                            foreach (var k in _data.Keys)
                            {
                                foreach (var pi in _data[k])
                                {
                                    if (fi.Equals(new FilteredItem(pi.Row)))
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                                if (found)
                                    break;
                            }

                            if (!found)
                            {
                                toRemove.Add(fi);
                            }
                        }
                        FilterModel.RemoveFilteredItems(toRemove, this);

                        var tmpData = _data.Keys.ToDictionary(@group => @group, @group => new List<HistogramItem>());

                        // add non existing outer groups if the label is binned
                        if (FilterModel.GetColumnDescriptorsForOption(Option.X).Count(cd => cd.IsAnyGroupingOperationApplied()) > 0 &&
                            FilterModel.GetColumnDescriptorsForOption(Option.X)[0].IsBinned)
                        {
                            PanoramicDataColumnDescriptor binnedCD =
                                FilterModel.GetColumnDescriptorsForOption(Option.X).First(cd => cd.IsBinned);
                            for (double d = binnedCD.BinLowerBound; d <= binnedCD.BinUpperBound; d += binnedCD.BinSize)
                            {
                                double dd = Math.Floor(d/binnedCD.BinSize)*binnedCD.BinSize;
                                PanoramicDataValue groupValue = new PanoramicDataValue();
                                groupValue.DataType = binnedCD.DataType;
                                groupValue.Value = dd;
                                if (groupValue.DataType == DataTypeConstants.INT)
                                {
                                    groupValue.Value = (int)dd;
                                }
                                groupValue.StringValue = binnedCD.LabelFromBinAggregate(dd + "/" + (dd + binnedCD.BinSize)).TrimTo(15);
                                
                                if (_labels.Count(v => v.LabelGroupedDataValue.Equals(groupValue)) == 0)
                                {
                                    HistogramItem lItem = new HistogramItem();
                                    lItem.Label = groupValue.StringValue;
                                    lItem.LabelGroupedDataValue = groupValue;
                                    _labels.Add(lItem);
                                }
                            }

                            List<HistogramItem> tmp;
                            if (binnedCD.SortMode == SortMode.Asc ||
                                binnedCD.SortMode == SortMode.None)
                            {
                                tmp = _labels.OrderBy(l => l.LabelGroupedDataValue.Value).ToList();
                            }
                            else
                            {
                                tmp = _labels.OrderByDescending(l => l.LabelGroupedDataValue.Value).ToList();
                            }
                            _labels.Clear();
                            _labels.AddRange(tmp);
                        }


                        // add non existing inner groups
                        foreach (var labelItem in _labels)
                        {
                            foreach (var group in _data.Keys)
                            {
                                if (_data[group].Any(v => v.Label == labelItem.Label))
                                {
                                    tmpData[group].Add(_data[group].First(v => v.Label == labelItem.Label));
                                }
                                else
                                {
                                    HistogramItem zeroItem = new HistogramItem();
                                    zeroItem.Label = labelItem.Label;
                                    zeroItem.Value = 0.0;
                                    tmpData[group].Add(zeroItem);
                                }
                            }
                        }
                        _data = tmpData;
                        foreach (var group in _groupingValues.Keys)
                        {
                            createSeries(group.ToString(), _groupingValues[group][0], _groupingValues[group][1]);
                        }

                        // convert to statistical represenation and trigger dataloaded event
                        List<XYValue> values = new List<XYValue>();
                        foreach (var obj in _data.Keys)
                        {
                            foreach (var histogramItem in _data[obj])
                            {
                                values.Add(new XYValue()
                                {
                                     X = obj.ToString() + histogramItem.Label,
                                     Y = histogramItem.Value
                                });
                            }
                        }
                        FireDataLoadingComplete(values);

                        plot.Model.RefreshPlot(true);
                        if (_series.Count > 1 ||
                            _series.Count ==1 && _series.Keys.First() != DEFAULT_GROUPING)
                        {
                            legend.Visibility = Visibility.Visible;
                            legend.ItemsSource = _series.Values;
                        }
                    }
                }
            }
            catch (Exception exc)
            {
            }
        }

        void createSeries(string grouping, PanoramicDataValue dataValue, PanoramicDataValue groupedDataValue)
        {
            if (FilterModel == null)
            {
                return;
            }
            ColumnSeries series = new ColumnSeries();
            series.StrokeThickness = 3;
            series.ValueField = "Value";
            series.ColorField = "Color";
            series.MouseDown += series_MouseDown;

            Color c = FilterModel.Color;
            if (grouping != DEFAULT_GROUPING)
            {
                c = FilterRendererResources.GetGroupingColor(grouping);
            }

            ColumnSeriesItem sItem = new ColumnSeriesItem();
            sItem.Color = new SolidColorBrush(c);
            //series.FillColor = OxyColor.FromArgb(128, c.R, c.G, c.B);
            series.StrokeColor = OxyColor.FromArgb(c.A, c.R, c.G, c.B);
   
            sItem.Label = grouping;
            sItem.Series = series;
            sItem.DataValue = dataValue;
            sItem.GroupedDataValue = groupedDataValue;

            _series.Add(grouping, sItem);
            series.ItemsSource = _data[grouping];
            foreach (var item in _data[grouping])
            {
                item.Color = OxyColor.FromArgb(128, c.R, c.G, c.B);
            }
            _plotModel.Series.Add(series);
        }

        protected override void Init(bool resetViewport)
        {
            base.Init(resetViewport);
            if (FilterModel == null)
            {
                return;
            }
           
            legend.Visibility = Visibility.Collapsed;
            
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

            
            errMain.Visibility = Visibility.Collapsed;

            string xErrMsg = "";
            string yErrMsg = "";
            string seriesErrMsg = "";
            validityCheck(out xErrMsg, out yErrMsg);

            _plotModel= new PlotModel()
            {
                LegendPlacement = LegendPlacement.Outside,
                LegendPosition = LegendPosition.BottomCenter,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendBorderThickness = 0,
                PlotMargins = new OxyThickness(0, 0, 0, 0)
            };

            _catAxis = new CategoryAxis(AxisPosition.Bottom) { LabelField = "Label", Angle = 0 };
            _plotModel.Axes.Add(_catAxis);
            LinearAxis linAxis = new LinearAxis(AxisPosition.Left);
            _plotModel.Axes.Add(linAxis);

            _data = new Dictionary<object, List<HistogramItem>>();
            _groupingValues = new Dictionary<object, PanoramicDataValue[]>();
            _series = new Dictionary<object, ColumnSeriesItem>();
            _labels = new List<HistogramItem>();

            _catAxis.ItemsSource = _labels;
            plot.Model = _plotModel;

            if (xErrMsg != "" || yErrMsg != "")
            {
                errMain.Visibility = Visibility.Visible;
                return;
            }

            QueryItemsProvider queryItemsProvider = new QueryItemsProvider(new FilterQueryGenerator(FilterModel));
            _currentDataValues = new AsyncVirtualizingCollection<PanoramicDataRow>(queryItemsProvider, 1000 /*page size*/, 1000 /*timeout*/);

            _currentDataValues.CollectionChanged += this.DataValues_CollectionChanged;

            Console.WriteLine(_currentDataValues.Count);
        }

        void HistogramFilterRenderer2_TouchDown(object sender, TouchEventArgs e)
        {
            /*var point = e.GetTouchPoint(plot).Position;
            foreach (var grouping in _series.Keys)
            {
                TrackerHitResult result = _series[grouping].Series.GetNearestPoint(new ScreenPoint(point.X, point.Y), false);
                if (result != null)
                {
                    if (result.Item != null)
                    {
                        toggleFilteredItem((HistogramItem)result.Item);
                        //e.Handled = true;

                        plot.Model.RefreshPlot(true);
                    }
                }
            }*/
        }

        private void HistogramFilterRenderer2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var point = e.GetPosition(plot);
            foreach (var grouping in _series.Keys)
            {
                TrackerHitResult result = _series[grouping].Series.GetNearestPoint(new ScreenPoint(point.X, point.Y), false);
                if (result != null)
                {
                    if (result.Item != null)
                    {
                        toggleFilteredItem((HistogramItem)result.Item);
                        //e.Handled = true;

                        plot.Model.RefreshPlot(true);
                    }
                }
            }
        }

        void series_MouseDown(object sender, OxyMouseEventArgs e)
        {
            if (e.ChangedButton == OxyMouseButton.Left)
            {
                HistogramItem item = (HistogramItem)e.HitTestResult.Item;
                if (item == null)
                {
                    return;
                }
                toggleFilteredItem(item);

                plot.Model.RefreshPlot(true);
                e.Handled = true;
            }
        }

        private void LegendItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ColumnSeriesItem ssi = ((FrameworkElement)sender).DataContext as ColumnSeriesItem;
            ssi.Selected = !ssi.Selected;

            FilteredItem fi = new FilteredItem();
            PanoramicDataColumnDescriptor cd = FilterModel.GetColumnDescriptorsForOption(Option.ColorBy)[0];

            if (cd.IsAnyGroupingOperationApplied())
            {
                fi.GroupComparisonValues.Add(cd, new PanoramicDataValueComparison(ssi.GroupedDataValue, Predicate.EQUALS));
            }
            else
            {
                fi.ColumnComparisonValues.Add(cd, new PanoramicDataValueComparison(ssi.DataValue, Predicate.EQUALS));
            }

            if (ssi.Selected)
            {
                if (!FilterModel.FilteredItems.Contains(fi))
                {
                    FilterModel.AddFilteredItem(fi, this);
                }
            }
            else
            {
                if (FilterModel.FilteredItems.Contains(fi))
                {
                    FilterModel.RemoveFilteredItem(fi, this);
                }
            }
        }

        private void toggleFilteredItem(HistogramItem item)
        {
            if (item.Selected)
            {
                item.Color = OxyColor.FromArgb(128, item.Color.R, item.Color.G, item.Color.B);
            }
            else
            {
                item.Color = OxyColor.FromArgb(255, item.Color.R, item.Color.G, item.Color.B);
            }
            item.Selected = !item.Selected;

            FilteredItem fi = new FilteredItem(item.Row);

            if (item.Selected)
            {
                if (!FilterModel.FilteredItems.Contains(fi))
                {
                    FilterModel.AddFilteredItem(fi, this);
                }
            }
            else
            {
                if (FilterModel.FilteredItems.Contains(fi))
                {
                    FilterModel.RemoveFilteredItem(fi, this);
                }
            }
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

        private void validityCheck(out string xErrMsg, out string yErrMsg)
        {
            xErrMsg = "";
            yErrMsg = "";

            if (FilterModel.GetColumnDescriptorsForOption(Option.X).Count == 0)
            {
                xErrMsg = "Please specify label column";
            }

            if (FilterModel.GetColumnDescriptorsForOption(Option.Y).Count > 0)
            {
                if (!FilterModel.IsDataTypeOfPanoramicDataColumnDescriptorNumeric(FilterModel.GetColumnDescriptorsForOption(Option.Y)[0], true)) 
                {
                    yErrMsg = "Not a numeric datatype";
                }
            }
            else
            {
                yErrMsg = "Please specify data column";
            }
        }
    }

    public class HistogramItem : BarItemBase
    {
        public double From { get; set; }
        public double To { get; set; }
        public bool Selected { get; set; }
        public string Label { get; set; }
        public PanoramicDataRow Row { get; set; }
        public PanoramicDataValue LabelGroupedDataValue { get; set; }
    }

    public class ColumnSeriesItem : ViewModelBase
    {
        public ColumnSeries Series { get; set; }
        public PanoramicDataValue DataValue { get; set; }
        public PanoramicDataValue GroupedDataValue { get; set; }
        public Brush Color { get; set; }
        public string Label { get; set; }
        private bool _selected = false;
        public bool Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                _selected = value;
                OnPropertyChanged("Selected");
            }
        }
    }
}
