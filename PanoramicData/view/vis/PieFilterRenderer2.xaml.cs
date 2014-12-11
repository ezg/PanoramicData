using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using PanoramicDataModel;
using starPadSDK.AppLib;
using OxyPlot;
using OxyPlot.Series;
using System.ComponentModel;
using PanoramicData.view.filter;
using PanoramicData.controller.data;
using PanoramicData.model.view;
namespace PanoramicData.view.vis
{
    /// <summary>
    /// Interaction logic for PieFilterRenderer2.xaml
    /// </summary>
    public partial class PieFilterRenderer2 : FilterRenderer
    {
        private int _toLoad = 0;
        private int _loaded = 0;
        private long _dragTime = DateTime.Now.Ticks;
        private delegate List<List<object>> ExecuteQueryDelegate(string schema, string query);
        private List<DataWrapper<PanoramicDataRow>> _currentRows = new List<DataWrapper<PanoramicDataRow>>();
        AsyncVirtualizingCollection<PanoramicDataRow> _currentDataValues = null;
        private PieSeries _series = null;

        public PieFilterRenderer2() : this(false)
        {
        }

        ~PieFilterRenderer2()
        {

        }

        public PieFilterRenderer2(bool showSettings)
        {
            InitializeComponent();
            this.TouchDown += PieFilterRenderer2_TouchDown;
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
                    FilterModel.GetColumnDescriptorsForOption(Option.Label).Count > 0 &&
                    FilterModel.GetColumnDescriptorsForOption(Option.SegmentSize).Count > 0)
                {
                    PanoramicDataRow loadedRow = ((DataWrapper<PanoramicDataRow>)sender).Data;
                    _loaded++; 
                    CustomPieSlice slice = new CustomPieSlice();
                    slice.Row = loadedRow;
                    PanoramicDataValue value = loadedRow.GetValue(FilterModel.GetColumnDescriptorsForOption(Option.SegmentSize)[0]);

                    slice.Value = double.Parse(value.Value == DBNull.Value ? "0" : value.StringValue);
                    List<string> labelParts = new List<string>();
                    foreach (var cd in FilterModel.GetColumnDescriptorsForOption(Option.Label))
                    {
                        string labelPart = loadedRow.GetValue(cd).StringValue;
                        labelParts.Add(labelPart.TrimTo(15));
                    }
                    slice.Label = string.Join("\n", labelParts);

                    Color c = FilterModel.Color;
                    if (_toLoad != 1)
                    {
                        c = FilterRendererResources.GetGroupingColor(slice.Label);
                    }
                    slice.Fill = OxyColor.FromArgb(128, c.R, c.G, c.B);
                    slice.Stroke = OxyColor.FromArgb(c.A, c.R, c.G, c.B);
                    _series.Slices.Add(slice);

                    // isHighlighted because it was previously selected
                    if (loadedRow.IsHighligthed)
                    {
                        slice.Selected = true;
                        slice.Fill = OxyColor.FromArgb(c.A, c.R, c.G, c.B);
                        /*foreach (var fi in FilterModel.FilteredItems.ToArray())
                        {
                            if (fi.RowNumber == slice.Row.RowNumber)
                            {
                                FilterModel.FilteredItems.Remove(fi);
                            }
                        }
                        toggleFilteredItem(slice);*/
                    }
                    if (_loaded == _toLoad)
                    {
                        // convert to statistical represenation and trigger dataloaded event
                        List<XYValue> values = new List<XYValue>();
                        foreach (var s in _series.Slices)
                        {
                            values.Add(new XYValue()
                            {
                                X = s.Label,
                                Y = s.Value
                            });
                        }

                        // remove unused / invisible FilterItems
                        List<FilteredItem> toRemove = new List<FilteredItem>();
                        foreach (var fi in FilterModel.FilteredItems.ToArray())
                        {
                            bool found = false;
                            foreach (var s in _series.Slices)
                            {
                                if (fi.Equals(new FilteredItem(((CustomPieSlice) s).Row)))
                                {
                                    found = true;
                                    break;
                                }
                            }
                            if (!found)
                            {
                                toRemove.Add(fi);
                            }
                        }
                        FilterModel.RemoveFilteredItems(toRemove, this);

                        FireDataLoadingComplete(values);
                        plot.Model.RefreshPlot(true);
                    }
                }
            }
            catch (Exception exc)
            {
            }
        }

        protected override void Init(bool resetViewport)
        {
            base.Init(resetViewport);
            if (FilterModel == null)
            {
                return;
            }
            
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

            errMain.Visibility = System.Windows.Visibility.Collapsed;

            string xErrMsg = "";
            string yErrMsg = "";
            validityCheck(out xErrMsg, out yErrMsg);

            var tmp = new PlotModel()
            {
                LegendPlacement = LegendPlacement.Outside,
                LegendPosition = LegendPosition.BottomCenter,
                LegendOrientation = LegendOrientation.Horizontal,
                LegendBorderThickness = 0,
                PlotMargins = new OxyThickness(0, 0, 0, 0)
            };
            _series = new PieSeries();
            _series.StrokeThickness = 3;
            tmp.Series.Add(_series);
            _series.MouseDown += series_MouseDown;

            plot.Model = tmp;

            if (xErrMsg != "" || yErrMsg != "")
            {
                errMain.Visibility = System.Windows.Visibility.Visible;
                return;
            }

            QueryItemsProvider queryItemsProvider = new QueryItemsProvider(new FilterQueryGenerator(FilterModel));
            _currentDataValues = new AsyncVirtualizingCollection<PanoramicDataRow>(queryItemsProvider, 1000 /*page size*/, 1000 /*timeout*/);

            _currentDataValues.CollectionChanged += this.DataValues_CollectionChanged;

            Console.WriteLine(_currentDataValues.Count);
        }

        void PieFilterRenderer2_TouchDown(object sender, TouchEventArgs e)
        {
            var point = e.GetTouchPoint(plot).Position;
            TrackerHitResult result = _series.GetNearestPoint(new ScreenPoint(point.X, point.Y), false);
            if (result != null)
            {
                if (result.Item != null)
                {
                    toggleFilteredItem((CustomPieSlice)result.Item);
                    //e.Handled = true;

                    plot.Model.RefreshPlot(true);
                }
            }
        }

        void series_MouseDown(object sender, OxyMouseEventArgs e)
        {
            if (e.ChangedButton == OxyMouseButton.Left)
            {
                CustomPieSlice customPieSlice = (CustomPieSlice)e.HitTestResult.Item;
                if (customPieSlice == null)
                {
                    return;
                }
                toggleFilteredItem(customPieSlice);

                plot.Model.RefreshPlot(true);
                e.Handled = true;
            }
        }

        private void toggleFilteredItem(CustomPieSlice item)
        {
            if (item.Selected)
            {
                item.Fill = OxyColor.FromArgb(128, item.Fill.R, item.Fill.G, item.Fill.B);
            }
            else
            {
                item.Fill = OxyColor.FromArgb(255, item.Fill.R, item.Fill.G, item.Fill.B);
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

            if (FilterModel.GetColumnDescriptorsForOption(Option.Label).Count == 0)
            {
                xErrMsg = "Please specify label column";
            }

            if (FilterModel.GetColumnDescriptorsForOption(Option.SegmentSize).Count > 0)
            {
                if (!FilterModel.IsDataTypeOfPanoramicDataColumnDescriptorNumeric(FilterModel.GetColumnDescriptorsForOption(Option.SegmentSize)[0], true)) 
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


    public class CustomPieSlice : PieSlice
    {
        public PanoramicDataRow Row { get; set; }
        public bool Selected { get; set; }
    }

}
