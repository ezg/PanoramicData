using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Antlr.Runtime.Tree;
using GeoAPI.Geometries;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.Charts;
using Microsoft.Research.DynamicDataDisplay.Charts.Axes;
using PixelLab.Wpf;
using SharpGL;
using SharpGL.Enumerations;
using SharpGL.SceneGraph.Assets;
using SharpGL.SceneGraph.Core;
using SharpGL.SceneGraph.Primitives;
using SharpGL.SceneGraph.Shaders;
using SharpGL.SceneGraph;
using starPadSDK.AppLib;
using PixelLab.Common;
using System.ComponentModel;
using PanoramicDataModel;
using starPadSDK.Geom;
using starPadSDK.Inq;
using Matrix = System.Windows.Media.Matrix;
using System.Windows.Threading;
using PanoramicData.Properties;
using PanoramicData.view.filter;
using PanoramicData.utils.inq;
using PanoramicData.controller.data;
using PanoramicData.model.view;
using PanoramicData.view.table;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using PanoramicData.view.other;
using CombinedInputAPI;
using PanoramicData.model.view_new;

namespace PanoramicData.view.vis
{
    /// <summary>
    /// Interaction logic for PlotFilterRenderer4.xaml
    /// </summary>
    public partial class PlotFilterRenderer4 : FilterRenderer, StroqListener
    {
        private DateTime _last = DateTime.MinValue;

        private List<DataWrapper<PanoramicDataRow>> _currentRows = new List<DataWrapper<PanoramicDataRow>>();
        private AsyncVirtualizingCollection<PanoramicDataRow> _currentDataValues = null;
        private List<DataPoint> _currentDataPoints = new List<DataPoint>();
        private List<DataPoint> _newDataPoints = new List<DataPoint>();
        private Dictionary<string, double> _xUniqueLabels = new Dictionary<string, double>();
        private Dictionary<string, double> _yUniqueLabels = new Dictionary<string, double>();
        private Dictionary<string, List<DataPoint>> _xLabelDatapointMapping = new Dictionary<string, List<DataPoint>>();
        private Dictionary<string, List<DataPoint>> _yLabelDatapointMapping = new Dictionary<string, List<DataPoint>>();

        private Texture _worldTexture = null;
        private double _worldWidth = 1355;
        private double _worldHeight = 1317;

        private Dictionary<PanoramicDataColumnDescriptor, Dictionary<PanoramicDataValue, List<DataPoint>>>
            _uniqueValueDatapointMapping =
                new Dictionary<PanoramicDataColumnDescriptor, Dictionary<PanoramicDataValue, List<DataPoint>>>();

        private Dictionary<Pt, List<DataPoint>> _xyValueDatapointMapping = new Dictionary<Pt, List<DataPoint>>();
        private Dictionary<string, DataPointSeries> _series = new Dictionary<string, DataPointSeries>();

        private const double PI2 = Math.PI*2f;

        private string _xDataType = AttributeDataTypeConstants.FLOAT;
        private string _yDataType = AttributeDataTypeConstants.FLOAT;

        private int _toLoad = 0;
        private int _loaded = 0;
        private static string DEFAULT_GROUPING = "default";

        private uint[] _colorBufID = new uint[1];
        private uint[] _mframeBufID = new uint[1];
        private uint[] _frameBufID = new uint[1];
        private uint[] _textureId = new uint[1];

        private int _windowWidth = -1;
        private int _windowHeight = -1;
        private int _textureWidth = -1;
        private int _textureHeight = -1;
        private Mat _dataToScreen = Mat.Identity;
        private Mat _graphTempTransform = Mat.Identity;

        private int _borderTop = 10;
        private int _borderBottom = 25;
        private int _borderLeft = 40; 
        private int _borderRight = 10;

        private int _newBorderLeft = 40;

        private Pt _openGlControlOffset = new Pt();

        private bool _renderGraph = true;
        private bool _resetGraphTransform = true;

        private FilterRendererType _renderStyle = FilterRendererType.Plot;

        public FilterRendererType RenderStyle
        {
            get
            {
                return _renderStyle;
            }
            set
            {
                if (_renderStyle != value)
                {
                    _renderStyle = value;
                    if (_renderStyle == FilterRendererType.Pie || _renderStyle == FilterRendererType.Map)
                    {
                        _borderTop = 10;
                        _borderBottom = 10;
                        _borderLeft = 10;
                        _newBorderLeft = 10;
                    }
                    else
                    {
                        _borderTop = 10;
                        _borderBottom = 25;
                        _borderLeft = 40;
                        _borderRight = 10;
                    }
                }
            }
        }

        private Stopwatch _renderStopwatch = new Stopwatch();
        private Stopwatch _dataStopwatch = new Stopwatch();
        private Stopwatch _doubleTapStopwatch = new Stopwatch();

        public PlotFilterRenderer4()
            : this(false)
        {
        }

        public PlotFilterRenderer4(bool showSettings)
        {
            InitializeComponent();

            glControl.AddHandler(FrameworkElement.TouchDownEvent, new EventHandler<TouchEventArgs>(PlotFilterRenderer4_TouchDownEvent));
            this.MouseWheel += PlotFilterRenderer4_MouseWheel;
            xPlaceHolder.Changed += XPlaceHolderOnChanged;    
            yPlaceHolder.Changed += YPlaceHolderOnChanged;
        }

        private void YPlaceHolderOnChanged(object sender, AttributeViewModelEventArgs e)
        {
            FilterModel.RemoveOptionColumnDescriptor(Option.Y, FilterModel.GetColumnDescriptorsForOption(Option.Y)[0]);
            //FilterModel.AddOptionColumnDescriptor(Option.Y, (PanoramicDataColumnDescriptor) e.ColumnDescriptor.Clone());
        }

        private void XPlaceHolderOnChanged(object sender, AttributeViewModelEventArgs e)
        {
            FilterModel.RemoveOptionColumnDescriptor(Option.X, FilterModel.GetColumnDescriptorsForOption(Option.X)[0]);
            //FilterModel.AddOptionColumnDescriptor(Option.X, (PanoramicDataColumnDescriptor)e.ColumnDescriptor.Clone());
        }
        
        void DataValues_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            AsyncVirtualizingCollection<PanoramicDataRow> values = (AsyncVirtualizingCollection<PanoramicDataRow>)sender;

            double maxPlotItems = Settings.Default.PanoramicDataMaxPlotItems;
            _toLoad = (int)Math.Min(values.Count, maxPlotItems);
            _loaded = 0;

            for (int i = 0; i < _toLoad; i++)
            {
                DataWrapper<PanoramicDataRow> row = values[i];
                row.PropertyChanged += row_PropertyChanged;
                _currentRows.Add(row);
            }
        }

        void row_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //try
            {
                if (e.PropertyName == "Data" &&
                    ((FilterModel.GetColumnDescriptorsForOption(Option.X).Count > 0 &&
                      FilterModel.GetColumnDescriptorsForOption(Option.Y).Count > 0) ||
                     (FilterModel.GetColumnDescriptorsForOption(Option.X).Count > 0 &&
                      _renderStyle == FilterRendererType.OneD)))
                {
                    PanoramicDataRow loadedRow = ((DataWrapper<PanoramicDataRow>) sender).Data;
                    _loaded++;
                    loadedRow.PropertyChanged -= row_PropertyChanged;

                    string grouping = DEFAULT_GROUPING;
                    List<PanoramicDataValue> dataValues = new List<PanoramicDataValue>();
                    List<PanoramicDataValue> groupedDataValues = new List<PanoramicDataValue>();
                    if (FilterModel.GetColumnDescriptorsForOption(Option.ColorBy).Count > 0)
                    {
                        List<string> groupingList = new List<string>();
                        foreach (var columnDescriptor in FilterModel.GetColumnDescriptorsForOption(Option.ColorBy))
                        {
                            PanoramicDataValue dataValue = loadedRow.GetValue(columnDescriptor);
                            if (dataValue != null)
                            {
                                dataValues.Add(dataValue);
                            }
                            PanoramicDataValue groupedDataValue = loadedRow.GetValue(columnDescriptor);
                            if (groupedDataValue != null)
                            {
                                groupedDataValues.Add(groupedDataValue);
                            }

                            string main = "";
                            string sub = "";
                            columnDescriptor.GetLabels(out main, out sub, false);

                            groupingList.Add(main + ":" + sub + ":" + dataValue.StringValue);
                        }
                        grouping = string.Join(":", groupingList);
                    }
                    else
                    {
                        
                    }
                    DataPointSeries series = null;
                    if (!_series.ContainsKey(grouping))
                    {
                        series = createSeries(grouping, dataValues, groupedDataValues);
                        _series[grouping] = series;
                    }
                    else
                    {
                        series = _series[grouping];
                    }

                    double valueX;
                    string labelX;
                    DateTime dateX;
                    PanoramicDataValue dataValueX;
                    bool xIsNull = getDataPointValue(loadedRow, FilterModel.GetColumnDescriptorsForOption(Option.X)[0],
                        _xDataType, _xUniqueLabels,
                        out valueX, out labelX, out dateX, out dataValueX);

                    double valueY;
                    string labelY;
                    DateTime dateY;
                    PanoramicDataValue dataValueY;
                    bool yIsNull = false;
                    if (_renderStyle != FilterRendererType.OneD)
                    {
                        yIsNull = getDataPointValue(loadedRow, FilterModel.GetColumnDescriptorsForOption(Option.Y)[0],
                            _yDataType, _yUniqueLabels,
                            out valueY, out labelY, out dateY, out dataValueY);
                    }
                    else
                    {
                        valueY = 0;
                        labelY = "";
                        dateY = DateTime.Now;
                        dataValueY = null;
                    }

                    if (!xIsNull && !yIsNull)
                    {

                        DataPoint dataPoint = new DataPoint();
                        dataPoint.X = valueX;
                        dataPoint.Y = valueY;
                        dataPoint.LabelX = labelX;
                        dataPoint.LabelY = labelY;
                        dataPoint.DateX = dateX;
                        dataPoint.DateY = dateY;
                        dataPoint.DataValueX = dataValueX;
                        dataPoint.DataValueY = dataValueY;
                        dataPoint.Row = loadedRow;
                        dataPoint.Series = series;
                        series.DataPoints.Add(dataPoint);
                        dataPoint.IsSelected = loadedRow.IsHighligthed;


                        if (!_xUniqueLabels.ContainsKey(labelX))
                        {
                            _xUniqueLabels.Add(labelX, valueX);
                        }
                        if (!_yUniqueLabels.ContainsKey(labelY))
                        {
                            _yUniqueLabels.Add(labelY, valueY);
                        }

                        if (!_xLabelDatapointMapping.ContainsKey(labelX))
                        {
                            _xLabelDatapointMapping[labelX] = new List<DataPoint>();
                        }
                        _xLabelDatapointMapping[labelX].Add(dataPoint);

                        if (!_yLabelDatapointMapping.ContainsKey(labelY))
                        {
                            _yLabelDatapointMapping[labelY] = new List<DataPoint>();
                        }
                        _yLabelDatapointMapping[labelY].Add(dataPoint);

                        Pt p = new Pt(valueX, valueY);
                        if (!_xyValueDatapointMapping.ContainsKey(p))
                        {
                            _xyValueDatapointMapping[p] = new List<DataPoint>();
                        }
                        _xyValueDatapointMapping[p].Add(dataPoint);

                        addUniqueColumnValues(dataPoint);

                        _newDataPoints.Add(dataPoint);
                    }
                    else
                    {
                        
                    }

                    if (_loaded == _toLoad)
                    {
                        // remove unused / invisible FilterItems
                        Dictionary<PanoramicDataColumnDescriptor, List<PanoramicDataValue>> sortedDictionary = new Dictionary<PanoramicDataColumnDescriptor, List<PanoramicDataValue>>();
                        foreach (var columnDescriptor in _uniqueValueDatapointMapping.Keys)
                        {
                            sortedDictionary.Add(columnDescriptor, _uniqueValueDatapointMapping[columnDescriptor].Keys.OrderBy(dataValue => dataValue.Value).ToList());
                        }

                        List<FilteredItem> toRemove = new List<FilteredItem>();
                        foreach (var fi in FilterModel.FilteredItems)
                        {
                            List<PanoramicDataValue> froms = new List<PanoramicDataValue>();
                            List<PanoramicDataValue> tos = new List<PanoramicDataValue>();
                            foreach (var cd in fi.GroupComparisonValues.Keys)
                            {
                                froms.Add(fi.ColumnComparisonValues[cd].Value);
                                tos.Add(fi.GroupComparisonValues[cd].Value);
                            }

                            int count = 0;
                            List<List<DataPoint>> dataPointsToTest = new List<List<DataPoint>>();
                            foreach (var cd in fi.GroupComparisonValues.Keys)
                            {
                                int indexFrom = sortedDictionary[cd].IndexOf(froms[count]);
                                int indexTo = sortedDictionary[cd].IndexOf(tos[count]);

                                if (indexFrom != -1 && indexTo != -1)
                                {
                                    List<DataPoint> datapoints = new List<DataPoint>();
                                    for (int i = indexFrom; i <= indexTo; i++)
                                    {
                                        datapoints.AddRange(_uniqueValueDatapointMapping[cd][sortedDictionary[cd][i]]);
                                    }
                                    dataPointsToTest.Add(datapoints);
                                }

                                count ++;
                            }
                            var intersection = intersectAll(dataPointsToTest).ToList();
                            if (intersection.Count > 0)
                            {
                                intersection.ForEach(dp => dp.IsSelected = true);
                            }
                            else 
                            {
                                toRemove.Add(fi);
                            }
                        }
                        FilterModel.RemoveFilteredItems(toRemove, this);


                        _currentDataPoints = _newDataPoints.ToArray().ToList();

                        foreach (var xydps in _xyValueDatapointMapping.Values)
                        {
                            xydps.Shuffle();
                        }

                        // mess with the labels
                        reorderLabels();
                        _currentDataPoints.ForEach(dp => dp.LabelX = dp.LabelX.TrimTo(15));
                        _currentDataPoints.ForEach(dp => dp.LabelY = dp.LabelY.TrimTo(15));

                        // calcluate eventual scale Functions
                        calculateScaleFunctions();

                        if (_currentDataPoints.Count > 0)
                        {
                            int maxStringLength = _currentDataPoints.Select(dp => dp.LabelY).Max(s => s.Length);
                            double maxLength =
                                CommonExtensions.MeasureString(
                                    _currentDataPoints.Select(dp => dp.LabelY)
                                        .Where(s => s.Length == maxStringLength)
                                        .First()).Width;
                            _newBorderLeft = (int) maxLength + 15;
                        }

                        /*
                        if (_series.Count > 1)
                        {
                            legend.Visibility = Visibility.Visible;
                            legend.ItemsSource = _series.Values;
                        }*/

                        _renderGraph = true;
                        FireDataLoadingComplete(new List<XYValue>());

                        Console.WriteLine(("Data Time : " + _dataStopwatch.ElapsedMilliseconds));
                    }
                }
            }
            //catch (Exception exc)
            //{
                
            //}
          
        }

        private List<T> intersectAll<T>(IEnumerable<IEnumerable<T>> lists)
        {
            HashSet<T> hashSet = null;
            foreach (var list in lists)
            {
                if (hashSet == null)
                {
                    hashSet = new HashSet<T>(list);
                }
                else
                {
                    hashSet.IntersectWith(list);
                }
            }
            return hashSet == null ? new List<T>() : hashSet.ToList();
        }

        private void addUniqueColumnValues(DataPoint dataPoint)
        {
            foreach (var key in dataPoint.Row.ColumnValues.Keys)
            {
                if (!_uniqueValueDatapointMapping.ContainsKey(key))
                {
                    _uniqueValueDatapointMapping[key] = new Dictionary<PanoramicDataValue, List<DataPoint>>();
                }
                if (dataPoint.Row.ColumnValues[key].Value != DBNull.Value)
                {
                    if (!_uniqueValueDatapointMapping[key].ContainsKey(dataPoint.Row.ColumnValues[key]))
                    {
                        _uniqueValueDatapointMapping[key][dataPoint.Row.ColumnValues[key]] = new List<DataPoint>();
                    }
                    _uniqueValueDatapointMapping[key][dataPoint.Row.ColumnValues[key]].Add(dataPoint);
                }
            }
        }

        private List<DataPoint> getAllDataPointsFromUniqueValues(DataPoint dataPoint)
        {
            List<List<DataPoint>> allDps = new List<List<DataPoint>>();
            foreach (var key in dataPoint.Row.ColumnValues.Keys)
            {
                if (_uniqueValueDatapointMapping.ContainsKey(key))
                {
                    if (_uniqueValueDatapointMapping[key].ContainsKey(dataPoint.Row.ColumnValues[key]))
                    {
                        allDps.Add(_uniqueValueDatapointMapping[key][dataPoint.Row.ColumnValues[key]]);
                    }
                }
            }

            return intersectAll(allDps).ToList();
        }

        private void calculateScaleFunctions()
        {
            if (FilterModel.GetColumnDescriptorsForOption(Option.Y)[0].ScaleFunction != ScaleFunction.None)
            {
                var columDescriptor = FilterModel.GetColumnDescriptorsForOption(Option.Y)[0];
                var updatedXyValueDatapointMapping = new Dictionary<Pt, List<DataPoint>>();
                
                Dictionary<DataPointSeries, double> maxY = new Dictionary<DataPointSeries, double>();
                Dictionary<DataPointSeries, double> minY = new Dictionary<DataPointSeries, double>();
                Dictionary<DataPointSeries, double> yDiff = new Dictionary<DataPointSeries, double>();
                Dictionary<DataPointSeries, Pt> lastPt = new Dictionary<DataPointSeries, Pt>();

                foreach (var serie in _series.Values)
                {
                    if (serie.DataPoints.Count > 0)
                    {
                        maxY.Add(serie, serie.DataPoints.Max(dp => dp.Y));
                        minY.Add(serie, serie.DataPoints.Min(dp => dp.Y));
                        yDiff.Add(serie, maxY[serie] - minY[serie]);
                        lastPt.Add(serie, new Pt(0, 0));
                    }
                }
                if (columDescriptor.ScaleFunction == ScaleFunction.RunningTotalNormalized)
                {
                    foreach (var pt in _xyValueDatapointMapping.Keys.OrderBy(pt => pt.X))
                    {
                        foreach (var serie in _xyValueDatapointMapping[pt].Select(dp => dp.Series).Distinct())
                        {
                            Pt newPt = new Pt(0, 0);
                            double sumY =_xyValueDatapointMapping[pt].Where(dp => dp.Series == serie).Sum(dp => dp.Y);
                            newPt = new Pt(pt.X, sumY + lastPt[serie].Y);

                            maxY[serie] = Math.Max(maxY[serie], newPt.Y);
                            minY[serie] = Math.Min(minY[serie], newPt.Y);
                            yDiff[serie] = maxY[serie] - minY[serie];

                            lastPt[serie] = newPt;
                        }
                    }
                }
                foreach (var serie in _series.Values)
                {
                    lastPt[serie] = new Pt(0, 0);
                }

                foreach (var pt in _xyValueDatapointMapping.Keys.OrderBy(pt => pt.X))
                {
                    foreach (var serie in _xyValueDatapointMapping[pt].Select(dp => dp.Series).Distinct())
                    {
                        Pt newPt = new Pt(0, 0);
                        if (columDescriptor.ScaleFunction == ScaleFunction.RunningTotalNormalized)
                        {
                            double sumY = _xyValueDatapointMapping[pt].Where(dp => dp.Series == serie).Sum(dp => dp.Y);
                            newPt = new Pt(pt.X, sumY + lastPt[serie].Y);
                            lastPt[serie] = newPt;

                            if (yDiff[serie] != 0.0)
                            {
                                newPt = new Pt(newPt.X, (newPt.Y - minY[serie]) / yDiff[serie]);
                            }
                            else
                            {
                                newPt = new Pt(newPt.X, 1.0);
                            }
                        }
                        else if (columDescriptor.ScaleFunction == ScaleFunction.RunningTotal)
                        {
                            double sumY = _xyValueDatapointMapping[pt].Where(dp => dp.Series == serie).Sum(dp => dp.Y);
                            newPt = new Pt(pt.X, sumY + lastPt[serie].Y);
                        }
                        else if (columDescriptor.ScaleFunction == ScaleFunction.Log)
                        {
                            if (pt.Y != 0.0)
                                newPt = new Pt(pt.X, Math.Log10(pt.Y));
                        }
                        else if (columDescriptor.ScaleFunction == ScaleFunction.Normalize)
                        {
                            if (yDiff[serie] != 0.0)
                            {
                                newPt = new Pt(pt.X, (pt.Y - minY[serie]) / yDiff[serie]);
                            }
                            else
                            {
                                newPt = new Pt(pt.X, 1.0);
                            }
                        }

                        _xyValueDatapointMapping[pt].Where(dp => dp.Series == serie).ForEach(dp => dp.Y = newPt.Y);
                        _xyValueDatapointMapping[pt].Where(dp => dp.Series == serie).ForEach(dp => dp.LabelY = newPt.Y.ToString("N"));

                        if (updatedXyValueDatapointMapping.ContainsKey(newPt))
                        {
                            updatedXyValueDatapointMapping[newPt] = _xyValueDatapointMapping[pt].Where(dp => dp.Series == serie).ToList();
                        }
                        else
                        {
                            updatedXyValueDatapointMapping.Add(newPt, _xyValueDatapointMapping[pt].Where(dp => dp.Series == serie).ToList());
                        }
                        if (columDescriptor.ScaleFunction != ScaleFunction.RunningTotalNormalized)
                        {
                            lastPt[serie] = newPt;
                        }
                    }

                }
                _xyValueDatapointMapping = updatedXyValueDatapointMapping;
            }

        }

        private void reorderLabels()
        {
            if ((_xDataType == AttributeDataTypeConstants.NVARCHAR ||
                _xDataType == AttributeDataTypeConstants.GEOGRAPHY ||
                _xDataType == AttributeDataTypeConstants.GUID) &&
                FilterModel.GetColumnDescriptorsForOption(Option.X)[0].AggregateFunction != AggregateFunction.Bin)
            {
                int count = 0;
                if (FilterModel.GetColumnDescriptorsForOption(Option.X)[0].SortMode == SortMode.Desc)
                {
                    foreach (var label in _xUniqueLabels.Keys.OrderByDescending(s => s))
                    {
                        if (_renderStyle == FilterRendererType.Pie)
                            count = 1;
                        _xLabelDatapointMapping[label].ForEach(dp => dp.X = count);
                        count++;
                    }
                }
                else
                {
                    foreach (var label in _xUniqueLabels.Keys.OrderBy(s => s))
                    {
                        if (_renderStyle == FilterRendererType.Pie)
                            count = 1;
                        _xLabelDatapointMapping[label].ForEach(dp => dp.X = count);
                        count++;
                    }
                }
            }

            if ((_yDataType == AttributeDataTypeConstants.NVARCHAR ||
                _yDataType == AttributeDataTypeConstants.GEOGRAPHY ||
                _yDataType == AttributeDataTypeConstants.GUID) &&
                FilterModel.GetColumnDescriptorsForOption(Option.Y)[0].AggregateFunction != AggregateFunction.Bin)
            {
                int count = 0;
                if (FilterModel.GetColumnDescriptorsForOption(Option.Y)[0].SortMode == SortMode.Desc)
                {
                    foreach (var label in _yUniqueLabels.Keys.OrderByDescending(s => s))
                    {
                        if (_renderStyle == FilterRendererType.Pie)
                            count = 1;
                        _yLabelDatapointMapping[label].ForEach(dp => dp.Y = count);
                        count++;
                    }
                }
                else
                {
                    foreach (var label in _yUniqueLabels.Keys.OrderBy(s => s))
                    {
                        if (_renderStyle == FilterRendererType.Pie)
                            count = 1;
                        _yLabelDatapointMapping[label].ForEach(dp => dp.Y = count);
                        count++;
                    }
                }
            }
        }

        bool getDataPointValue(PanoramicDataRow loadedRow, PanoramicDataColumnDescriptor columnDescriptor, string dataType, 
            Dictionary<string, double> currentLabelList,
            out double value, out string label, out DateTime date, out PanoramicDataValue dataValue)
        {
            value = 0;
            date = DateTime.Now;
            bool isNull = false;

            dataValue = loadedRow.GetValue(columnDescriptor);
            label = dataValue.StringValue;

            if (dataType == AttributeDataTypeConstants.FLOAT ||
                dataType == AttributeDataTypeConstants.INT)
            {
                double d = 0;
                if (dataValue.Value != DBNull.Value && double.TryParse(dataValue.StringValue, out d))
                {
                    value = d;
                }
                else
                {
                    if (dataValue.Value != DBNull.Value && double.TryParse(dataValue.Value.ToString(), out d))
                    {
                        value = d;
                    }
                    else
                    {
                        isNull = true;
                    }
                }
            }
            else if (dataType == AttributeDataTypeConstants.BIT)
            {
                double d = 0;
                if (dataValue.Value != DBNull.Value && double.TryParse(dataValue.Value.ToString(), out d))
                {
                    value = d;
                }
                else
                {
                    if (dataValue.Value != DBNull.Value && dataValue.Value.ToString().Equals("True"))
                    {
                        value = 1;
                    }
                    else if (dataValue.Value != DBNull.Value && dataValue.Value.ToString().Equals("False"))
                    {
                        value = 0;
                    }
                    else
                    {
                        isNull = true;
                    }
                }
            }
            else if (dataType == AttributeDataTypeConstants.NVARCHAR ||
                     dataType == AttributeDataTypeConstants.GEOGRAPHY ||
                     dataType == AttributeDataTypeConstants.GUID)
            {
                value = loadedRow.RowNumber - 1;
                if (dataValue.Value != DBNull.Value)
                {
                    if (columnDescriptor.AggregateFunction == AggregateFunction.Bin)
                    {
                        double d = 0;

                        if (dataValue.Value != DBNull.Value && double.TryParse(dataValue.Value.ToString(), out d))
                        //if (dataValue.Value != DBNull.Value &&
                        //    double.TryParse(loadedRow.GetGroupedValue(columnDescriptor).StringValue, out d))
                        {

                            value = d/columnDescriptor.BinSize;
                        }
                    }
                    else
                    {
                        string thisLabel = dataValue.StringValue;
                        if (currentLabelList.ContainsKey(thisLabel))
                        {
                            value = currentLabelList[thisLabel];
                        }
                        else
                        {
                            if (currentLabelList.Count == 0)
                            {
                                value = 0;
                            }
                            else
                            {
                                value = currentLabelList.Values.Max() + 1;
                            }
                        }
                    }
                }
                else
                {
                    isNull = true;
                }
            }
            else if (dataType == AttributeDataTypeConstants.DATE)
            {
                if (dataValue.Value != DBNull.Value)
                {
                    value = ((DateTime) dataValue.Value).Ticks;
                    date = (DateTime)dataValue.Value;
                }
                else
                {
                    isNull = true;
                }
            }
            else if (dataType == AttributeDataTypeConstants.TIME)
            {
                if (dataValue.Value != DBNull.Value)
                {
                    value = ((TimeSpan)dataValue.Value).TotalSeconds;
                }
                else
                {
                    isNull = true;
                }
            }

            return isNull;
        }

        DataPointSeries createSeries(string grouping, List<PanoramicDataValue> dataValues, List<PanoramicDataValue> groupedDataValues)
        {
            DataPointSeries series = new DataPointSeries();
            series.Color = FilterModel.Color;
            if (grouping != DEFAULT_GROUPING)
            {
                series.Color = FilterRendererResources.GetGroupingColor(grouping);
                if (groupedDataValues.Count > 0)
                {
                    series.Label = string.Join(", ", groupedDataValues.Select(val => val.StringValue));
                }
                else
                {
                    series.Label = string.Join(", ", dataValues.Select(val => val.StringValue));
                }
            }
            series.DataValues = dataValues;
            series.GroupedDataValues = groupedDataValues;

            return series;
        }

        private void OpenGLControl_OnOpenGLDraw(object sender, OpenGLEventArgs args)
        {
            // process events
            if (_renderStyle != FilterRendererType.Pie)
            {
                if (_dragDevice1 != null && _dragDevice2 == null)
                {
                    Vector dragBy = _current1 - _startDrag1;
                    _graphTempTransform = _graphTempTransform * Mat.Translate(dragBy);
                    _dataToScreen = _dataToScreen * Mat.Translate(new Pt(dragBy.X, -dragBy.Y));
                    _startDrag1 = _current1;
                }
                if (_dragDevice1 != null && _dragDevice2 != null)
                {
                    double newLength = (_current1.GetVec() - _current2.GetVec()).Length;
                    if (_length != 0.0 && newLength / _length != 1.0)
                    {
                        Vector scalePos = (_current1.GetVec() + _current2.GetVec())/2.0;
                        double scale = newLength / _length;

                        Matrix m1 = Matrix.Identity;
                        m1.ScaleAt(scale, scale, scalePos.X, scalePos.Y);
                        m1 = ((Matrix) _dataToScreen);
                        m1.ScaleAt(scale, scale, scalePos.X, _windowHeight - scalePos.Y);
                        _dataToScreen = m1;

                        m1 = _graphTempTransform;
                        m1.ScaleAt(scale, scale, scalePos.X, scalePos.Y);
                        _graphTempTransform = m1;
                    }
                    _length = newLength;
                }
            }

            OpenGL gl = args.OpenGL;
            gl.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

            gl.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);

            gl.MatrixMode(OpenGL.GL_MODELVIEW);
            gl.LoadIdentity();

            // check if we need to update sizes
            if (_newBorderLeft != _borderLeft &&
                _renderStyle != FilterRendererType.Pie &&
                _renderStyle != FilterRendererType.Map)
            {
                _borderLeft = _newBorderLeft;
                updateSizes(gl);
            }

            // check if we need to re-render the graph
            if (_renderGraph && _currentDataPoints.Count > 0)
            {
                // calcuclate Matrix
                double maxY = _currentDataPoints.Max(dp => dp.Y);
                double maxX = _currentDataPoints.Max(dp => dp.X);
                double minY = _currentDataPoints.Min(dp => dp.Y);
                double minX = _currentDataPoints.Min(dp => dp.X);

                if (_renderStyle == FilterRendererType.Histogram)
                {
                    maxY = Math.Max(0, maxY);
                    minY = Math.Min(0, minY);
                }

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

                if (_resetGraphTransform)
                {
                    double borderScale = 0.9;
                    if (_xyValueDatapointMapping.Count < 10 && _renderStyle != FilterRendererType.Histogram)
                    {
                        borderScale = 0.7;
                    }
                    if (_renderStyle == FilterRendererType.Map)
                    {
                        minX = 0;
                        minY = 0;
                        maxX = _worldWidth;
                        maxY = _worldHeight;
                        borderScale = 1.0;

                        Pt scale = new Pt(Math.Min(_textureWidth, _textureHeight) / (maxX - minX), Math.Min(_textureWidth, _textureHeight) / (maxY - minY));
                        _dataToScreen =
                            Mat.Translate(-minX, -minY) * Mat.Translate(0, 0) *
                            Mat.Scale(scale.X, scale.Y);

                        Pt borderOffset = _dataToScreen.Inverse() *
                            new Pt((Math.Max(_textureWidth, _textureHeight) / 2.0),
                                   (Math.Max(_textureWidth, _textureHeight) / 2.0));

                        _dataToScreen = Mat.Translate(new Pt(-borderOffset.X, -borderOffset.Y)) *
                                        Mat.Scale(borderScale, borderScale) *
                                        Mat.Translate(new Pt(+borderOffset.X, +borderOffset.Y)) * _dataToScreen;
                        _resetGraphTransform = false;
                    }
                    else
                    {
                        Pt scale = new Pt(_textureWidth/(maxX - minX), _textureHeight/(maxY - minY));
                        _dataToScreen =
                            Mat.Translate(-minX, -minY)*Mat.Translate(0, 0)*
                            Mat.Scale(scale.X, scale.Y);

                        Pt borderOffset = _dataToScreen.Inverse()*
                                          new Pt((_textureWidth/2.0),
                                              (_textureHeight/2.0));

                        _dataToScreen = Mat.Translate(new Pt(-borderOffset.X, -borderOffset.Y))*
                                        Mat.Scale(borderScale, borderScale)*
                                        Mat.Translate(new Pt(+borderOffset.X, +borderOffset.Y))*_dataToScreen;
                        _resetGraphTransform = false;
                    }
                }
                if (_renderStyle == FilterRendererType.Pie)
                {
                    _dataToScreen = Mat.Identity;
                }
                _graphTempTransform = Mat.Identity;
                
                _renderStopwatch.Restart();
                drawGraphToTexture(gl);
                _renderStopwatch.Stop();
                Console.WriteLine(("Render Time : " + _renderStopwatch.ElapsedMilliseconds));

                _renderGraph = false;
            }

            // change projection 
            gl.MatrixMode(OpenGL.GL_PROJECTION);
            gl.LoadIdentity();
            gl.Ortho(0, (float) _windowWidth, (float) _windowHeight, 0, 0, 1);
            gl.MatrixMode(OpenGL.GL_MODELVIEW);

            // draw axes
            gl.Color(0.0, 0.0, 0.0);
            if (_currentDataPoints.Count > 0 && _renderStyle != FilterRendererType.Pie && _renderStyle != FilterRendererType.Map &&
                _graphTempTransform == Mat.Identity)
            {
                // calculate range
                Pt bottomRight = _dataToScreen.Inverse()*new Pt(0, 0);
                Pt topLeft = _dataToScreen.Inverse()*new Pt(_textureWidth, _textureHeight);

                // x axis
                gl.PushMatrix();
                gl.Translate(0, _borderTop + _textureHeight, 0f);
                IAxisRenderer xAxisRenderer = createAxisRenderer(
                    _xDataType,
                    new Range<double>(bottomRight.X, topLeft.X),
                    _borderLeft, _borderRight,
                    _textureWidth, _borderBottom, _textureHeight,
                    _dataToScreen, false,
                    _currentDataPoints);
                xAxisRenderer.RenderTicks(gl);
                gl.PopMatrix();

                // y axis
                gl.PushMatrix();
                gl.Translate(_borderLeft, 0f, 0f);
                IAxisRenderer yAxisRenderer = createAxisRenderer(
                    _yDataType,
                    new Range<double>(bottomRight.Y, topLeft.Y),
                    _borderTop, _borderBottom,
                    _borderLeft, _textureHeight, _textureWidth,
                    _dataToScreen, true,
                    _currentDataPoints);
                yAxisRenderer.RenderTicks(gl);
                gl.PopMatrix();
            }
            gl.LoadIdentity();

            // Set the scissor rectangle, this will clip fragments
            gl.Enable(OpenGL.GL_SCISSOR_TEST);
            gl.Scissor((int) _borderLeft, (int) _borderBottom, (int) _textureWidth, (int) _textureHeight);

            // draw textured quad
            if (_currentDataPoints.Count > 0)
            {
                gl.BindTexture(OpenGL.GL_TEXTURE_2D, _textureId[0]);
                gl.Enable(OpenGL.GL_TEXTURE_2D);

                gl.Translate(_borderLeft, _borderTop, 0f);
                Pt xy = _graphTempTransform*new Pt(0, 0);
                Pt wh = new Pt(_textureWidth * _graphTempTransform[0, 0], _textureHeight * _graphTempTransform[1,1]);//_graphTempTransform * new Pt(_textureWidth, _textureHeight);
                //gl.Translate(xy.X, xy.Y, 0.0f);

                gl.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);
                gl.Color(1.0, 1.0, 1.0);
                gl.Begin(OpenGL.GL_QUADS);
                {
                    gl.TexCoord(0, (_renderStyle != FilterRendererType.Pie) ? 0.0f : 1.0f);
                    gl.Vertex(xy.X, xy.Y);

                    gl.TexCoord(1.0f, (_renderStyle != FilterRendererType.Pie) ? 0.0f : 1.0f);
                    gl.Vertex(xy.X + wh.X, xy.Y);

                    gl.TexCoord(1.0f, (_renderStyle != FilterRendererType.Pie) ? 1.0f : 0.0f);
                    gl.Vertex(xy.X + wh.X, xy.Y + wh.Y);

                    gl.TexCoord(0, (_renderStyle != FilterRendererType.Pie) ? 1.0f : 0.0f);
                    gl.Vertex(xy.X, xy.Y + wh.Y);
                }
                gl.End();
            
                gl.Disable(OpenGL.GL_TEXTURE_2D);
            }
            gl.LoadIdentity();
            gl.Disable(OpenGL.GL_SCISSOR_TEST);
            gl.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);

            // draw rectangle border around the whole graph
            if (_renderStyle != FilterRendererType.Pie && _renderStyle != FilterRendererType.Map)
            {
                gl.PushMatrix();
                gl.LineWidth(1.0f);
                gl.Color(0, 0, 0);
                gl.Begin(OpenGL.GL_LINE_LOOP);
                gl.Vertex(_borderLeft, _borderTop);
                gl.Vertex(_borderLeft + _textureWidth, _borderTop);
                gl.Vertex(_borderLeft + _textureWidth, _borderTop + _textureHeight);
                gl.Vertex(_borderLeft, _borderTop + _textureHeight);
                gl.End();
                gl.PopMatrix();
            }
        }

        private void OpenGLControl_OpenGLInitialized(object sender, OpenGLEventArgs args)
        {
            OpenGL gl = args.OpenGL;
            gl.Enable(OpenGL.GL_POINT_SMOOTH);
            gl.Enable(OpenGL.GL_LINE_SMOOTH);
            gl.Enable(OpenGL.GL_POLYGON_SMOOTH);
            gl.Hint(OpenGL.GL_LINE_SMOOTH_HINT, OpenGL.GL_NICEST);
            gl.Hint(OpenGL.GL_POINT_SMOOTH_HINT, OpenGL.GL_NICEST);
            gl.Hint(OpenGL.GL_POLYGON_SMOOTH_HINT, OpenGL.GL_NICEST);
            gl.Enable(OpenGL.GL_BLEND);

            //_worldTexture = new Texture();
            //_worldTexture.Create(gl, @"C:\Users\ez\Desktop\world.png");
        }

        private void OpenGLControl_Resized(object sender, OpenGLEventArgs args)
        {
            OpenGL gl = args.OpenGL;

            // update sizes
            _windowWidth = gl.RenderContextProvider.Width;
            _windowHeight = gl.RenderContextProvider.Height;

            updateSizes(gl);

            gl.MatrixMode(OpenGL.GL_PROJECTION);
            gl.LoadIdentity();
            gl.Ortho(0, (float)_windowWidth, (float)_windowHeight, 0, 0, 1);
            gl.MatrixMode(OpenGL.GL_MODELVIEW);

            _renderGraph = true;
        }

        private void updateSizes(OpenGL gl)
        {
            double newTextureWidth = _windowWidth - (_borderLeft + _borderRight);
            double newTextureHeight = _windowHeight - (_borderTop + _borderBottom);

            if (_renderStyle == FilterRendererType.Map)
            {
                double scale = Math.Max(newTextureWidth/(double) _worldWidth, newTextureHeight/(double) _worldHeight);
                _dataToScreen =
                    _dataToScreen *
                    Mat.Scale(scale, scale);
            }
            else 
            {
                _dataToScreen =
                    _dataToScreen*
                    Mat.Scale(newTextureWidth/(double) _textureWidth, newTextureHeight/(double) _textureHeight);
            }

            _textureWidth = (int)newTextureWidth;
            _textureHeight = (int)newTextureHeight;

            createGraphTexture(gl);
        }

        private void createGraphTexture(OpenGL gl)
        {
            gl.DeleteTextures(1, _textureId);
            gl.DeleteRenderbuffersEXT(1, _colorBufID);
            gl.DeleteFramebuffersEXT(1, _mframeBufID);
            gl.DeleteFramebuffersEXT(1, _frameBufID);

            gl.Enable(OpenGL.GL_DEPTH_TEST);
            gl.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);

            // color texture
            gl.GenTextures(1, _textureId);
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, _textureId[0]);
            gl.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGBA8, _textureWidth, _textureHeight, 0,
                         OpenGL.GL_RGBA, OpenGL.GL_FLOAT, null);
            gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_LINEAR);
            gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_LINEAR);
            gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_CLAMP_TO_EDGE);
            gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_CLAMP_TO_EDGE);


            // multi sampled color buffer
            gl.GenRenderbuffersEXT(1, _colorBufID);
            gl.BindRenderbufferEXT(OpenGL.GL_RENDERBUFFER_EXT, _colorBufID[0]);

            bool multiSample = Settings.Default.PanoramicDataIsMultiSamplingEnabled;
            if (multiSample)
            {
                gl.RenderbufferStorageMultisampleEXT(OpenGL.GL_RENDERBUFFER_EXT, 4, OpenGL.GL_RGBA, _textureWidth,
                    _textureHeight);
            }
            else
            {
                gl.RenderbufferStorageEXT(OpenGL.GL_RENDERBUFFER_EXT, OpenGL.GL_RGBA, _textureWidth, _textureHeight);
            }

            // unbind
            gl.BindRenderbufferEXT(OpenGL.GL_RENDERBUFFER_EXT, 0);

            // create fbo for multi sampled content and attach buffers
            gl.GenFramebuffersEXT(1, _mframeBufID);
            gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, _mframeBufID[0]);
            gl.FramebufferRenderbufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_COLOR_ATTACHMENT0_EXT, OpenGL.GL_RENDERBUFFER_EXT, _colorBufID[0]);

            // create final fbo and attach textures
            gl.GenFramebuffersEXT(1, _frameBufID);
            gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, _frameBufID[0]);
            gl.FramebufferTexture2DEXT(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_COLOR_ATTACHMENT0_EXT, OpenGL.GL_TEXTURE_2D, _textureId[0], 0);


            // unbind
            gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, 0);
        }

        private void drawGraphToTexture(OpenGL gl)
        {
            Console.WriteLine(">> drawGraphToTexture " + this.GetHashCode() );
            _openGlControlOffset = glControl.TranslatePoint(new Point(0, 0), this);

            gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, _mframeBufID[0]);
            {
                gl.PushAttrib(OpenGL.GL_VIEWPORT_BIT);
                gl.MatrixMode(OpenGL.GL_PROJECTION);
                gl.LoadIdentity();
                gl.Ortho(0, (float)_textureWidth, (float)_textureHeight, 0, 0, 1);
                gl.Viewport(0, 0, _textureWidth, _textureHeight);

                gl.MatrixMode(OpenGL.GL_MODELVIEW);
                gl.LoadIdentity();
                gl.ClearColor(1.0f, 1.0f, 1.0f, 0.0f);
                gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

                var cc = _series.Values.Select(serie => _currentDataPoints.Count(dp => dp.Series == serie));

                if (_currentDataPoints.Count > 0)
                {
                    bool anySelected = _currentDataPoints.Any(dp => dp.IsSelected);

                    if (_renderStyle == FilterRendererType.Histogram)
                    {
                        renderBarChart(gl, anySelected);
                    }
                    else if (_renderStyle == FilterRendererType.Plot)
                    {
                        renderScatterPlot(gl, anySelected);
                    }
                    else if (_renderStyle == FilterRendererType.Line)
                    {
                        renderScatterPlot(gl, anySelected, true);
                    }
                    else if (_renderStyle == FilterRendererType.Pie)
                    {
                        renderPieChart(gl, anySelected);
                    }
                    else if (_renderStyle == FilterRendererType.Map)
                    {
                        renderMap(gl, anySelected);
                    }
                }
                gl.PopAttrib();
            }
            // blit from multisample FBO to final FBO
            gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, 0);
            gl.BindFramebufferEXT(OpenGL.GL_READ_FRAMEBUFFER_EXT, _mframeBufID[0]);
            gl.BindFramebufferEXT(OpenGL.GL_DRAW_FRAMEBUFFER_EXT, _frameBufID[0]);
            gl.BlitFramebufferEXT(0, 0, _textureWidth, _textureHeight, 0, 0, _textureWidth, _textureHeight,
                //OpenGL.GL_COLOR_BUFFER_BIT, OpenGL.GL_NEAREST);
                OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT, OpenGL.GL_NEAREST);
            gl.BindFramebufferEXT(OpenGL.GL_READ_FRAMEBUFFER_EXT, 0);
            gl.BindFramebufferEXT(OpenGL.GL_DRAW_FRAMEBUFFER_EXT, 0);

            gl.LoadIdentity();
        }

        private void renderBarChart(OpenGL gl, bool anySelected)
        {
            double smallestXDataDistance = double.MaxValue;
            if (_currentDataPoints.Count < 1500)
            {
                foreach (var dp1 in _currentDataPoints)
                {
                    foreach (var dp2 in _currentDataPoints)
                    {
                        if (dp1 != dp2 && dp1.Series == dp2.Series && dp1.X != dp2.X)
                        {
                            smallestXDataDistance = Math.Min(smallestXDataDistance, Math.Abs(dp1.X - dp2.X));
                        }
                    }
                }
            }
            else
            {
                smallestXDataDistance = 0;
            }
            if (_series.Count > _currentDataPoints.Count)
            {
                smallestXDataDistance = 0;
            }
            double smallestXScreenDistance = Math.Abs(_dataToScreen[0, 0]*smallestXDataDistance);
            smallestXScreenDistance = Math.Max(smallestXScreenDistance, 2.0);
            int index = 0;
            double availableHalfWidth = (smallestXScreenDistance*0.99)/2.0;
            double barWidth = Math.Min(60.0f, (availableHalfWidth*2)/_series.Count);

            // No Brushing
            if (FilterModel.GetIncomingFilterModels(FilteringType.Brush).Count == 0)
            {
                float maxDatapointsPerSeries = 1.0f;
                if (_series.Count > 1)
                {
                    //maxDatapointsPerSeries = _series.Values.Max(s => _currentDataPoints.Where(d => d.Series == s).Count());
                    maxDatapointsPerSeries = _series.Count();
                }
                foreach (var serie in _series.Values)
                {
                    foreach (var dp in _currentDataPoints.Where(d => d.Series == serie).OrderBy(dp => dp.IsSelected))
                    {
                        gl.PushMatrix();
                        Pt trans = _dataToScreen*dp.Pt;
                        double origin = (_dataToScreen*new Pt(0, 0)).Y - trans.Y;

                        gl.Color(serie.Color.R, serie.Color.G, serie.Color.B, (byte) 255);
                        gl.Translate((trans.X - (barWidth * maxDatapointsPerSeries) / 2.0) + index * barWidth, trans.Y, 0);
                        gl.Begin(OpenGL.GL_QUADS);
                        gl.Vertex(0, 0);
                        gl.Vertex(Math.Max(barWidth, 1), 0);
                        gl.Vertex(Math.Max(barWidth, 1), origin);
                        gl.Vertex(0, origin);
                        gl.End();

                        // check if some selection is going on
                        if (!(dp.IsSelected || !anySelected))
                        {
                            gl.Color(1f, 1f, 1f, 0.7f);
                            gl.Begin(OpenGL.GL_QUADS);
                            gl.Vertex(0, 0);
                            gl.Vertex(Math.Max(barWidth, 1), 0);
                            gl.Vertex(Math.Max(barWidth, 1), origin);
                            gl.Vertex(0, origin);
                            gl.End();
                        }
                        gl.PopMatrix();

                        // create geometry
                        Rct r =
                            new Rct(0, 0, Math.Max(barWidth, 1), origin).Translated(
                                new Vec((trans.X - (barWidth * maxDatapointsPerSeries) / 2.0) + index * barWidth,
                                    trans.Y));
                        r.Top = _textureHeight - r.Top;
                        r.Bottom = _textureHeight - r.Bottom;
                        r = r.Translated(new Vec(_borderLeft + _openGlControlOffset.X, _borderTop + _openGlControlOffset.Y));
                        dp.Geometry = r.GetPolygon();

                        /*Polyline pl = new Polyline();
                        pl.Points = new PointCollection(dp.Geometry.Coordinates.Select((corrd) => new Point(corrd.X, corrd.Y)).ToArray());
                        pl.Stroke = Brushes.Green;
                        pl.StrokeThickness = 1;
                        InqScene _inqScene = this.FindParent<InqScene>();
                        Pt pp = this.TranslatePoint(new Point(0, 0), _inqScene);
                        pl.RenderTransform = new TranslateTransform(pp.X, pp.Y);
                        _inqScene.AddNoUndo(pl);*/
                    }
                    if (maxDatapointsPerSeries != 1.0f)
                    {
                        index++;
                    }
                }
            }
            // Do Brushing
            else
            {
                double widthPerBrush = barWidth / (double)FilterModel.GetIncomingFilterModels(FilteringType.Brush).Count;
                List<FilterModel> brushModels = FilterModel.GetIncomingFilterModels(FilteringType.Brush);
                
                foreach (var serie in _series.Values)
                {
                    foreach (var dp in _currentDataPoints.Where(d => d.Series == serie))
                    {
                        gl.PushMatrix();
                        Pt trans = _dataToScreen * dp.Pt;
                        double origin = (_dataToScreen * new Pt(0, 0)).Y - trans.Y;
                        
                        gl.Translate((trans.X - barWidth / 2.0) + index * barWidth, trans.Y, 0);

                        gl.PushMatrix();
                        GLRectangle rectangle = new GLRectangle(widthPerBrush, origin, true, true);
                        GLRectangle rectangleOutline = new GLRectangle(barWidth, origin, false, true);
                        foreach (var brushModel in brushModels)
                        {
                            renderBrushRectangle(gl, brushModel, rectangle, dp, origin, widthPerBrush, true);
                            gl.Translate(widthPerBrush, 0, 0);
                        }
                        gl.PopMatrix();

                        // check if some selection is going on
                        if (dp.IsSelected)
                        {
                            gl.Color(0f, 0f, 0f, 1f);
                            rectangleOutline.Render(gl, RenderMode.Render);
                        }
                        gl.PopMatrix();

                        // create geometry
                        Rct r =
                            new Rct(0, 0, barWidth, origin).Translated(
                                new Vec((trans.X - barWidth / 2.0) + index * barWidth,
                                    trans.Y));
                        r.Top = _textureHeight - r.Top;
                        r.Bottom = _textureHeight - r.Bottom;
                        r = r.Translated(new Vec(_borderLeft + _openGlControlOffset.X, _borderTop + _openGlControlOffset.Y));
                        dp.Geometry = r.GetPolygon();
                    }
                    index++;
                }
            }
        }

        private void renderMap(OpenGL gl, bool anySelected)
        {
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, _worldTexture.TextureName);
            gl.Enable(OpenGL.GL_TEXTURE_2D);

            //Pt xy = new Pt(0, 0);
            //Pt wh = new Pt(_worldWidth, _worldHeight);

            Mat temp = _dataToScreen * Mat.Scale(1.0 / _dataToScreen[0, 0], 1.0 / _dataToScreen[1, 1]);
            temp = _dataToScreen * Mat.Scale(Math.Min(_dataToScreen[0, 0], _dataToScreen[1, 1]), Math.Min(_dataToScreen[0, 0], _dataToScreen[1, 1]));

            Pt xy = _dataToScreen * new Pt(0, 0);
            //xy = new Pt(xy.X/_dataToScreen[0, 0], xy.Y/_dataToScreen[1, 1]);
            //xy = new Pt(xy.X * Math.Min(_dataToScreen[0, 0], _dataToScreen[1, 1]), xy.Y * Math.Min(_dataToScreen[0, 0], _dataToScreen[1, 1]));

            //Pt wh = new Pt(Math.Min(_dataToScreen[0, 0], _dataToScreen[1, 1]) * _worldWidth, Math.Min(_dataToScreen[0, 0], _dataToScreen[1, 1]) * _worldHeight);
            Pt wh = new Pt(_dataToScreen[0, 0] * _worldWidth, _dataToScreen[1, 1] * _worldHeight);
            //_dataToScreen * new Pt(_worldWidth, _worldHeight);// 
            gl.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);
            gl.Color(1.0, 1.0, 1.0);
            gl.Begin(OpenGL.GL_QUADS);
            {
                gl.TexCoord(0, _renderStyle != FilterRendererType.Map ? 0.0f : 1.0f);
                gl.Vertex(xy.X, xy.Y);

                gl.TexCoord(1.0f, _renderStyle != FilterRendererType.Map ? 0.0f : 1.0f);
                gl.Vertex(xy.X + wh.X, xy.Y);

                gl.TexCoord(1.0f, _renderStyle != FilterRendererType.Map ? 1.0f : 0.0f);
                gl.Vertex(xy.X + wh.X, xy.Y + wh.Y);

                gl.TexCoord(0, _renderStyle != FilterRendererType.Map ? 1.0f : 0.0f);
                gl.Vertex(xy.X, xy.Y + wh.Y);
            }
            gl.End();

            gl.Disable(OpenGL.GL_TEXTURE_2D);
        }

        private void renderPieChart(OpenGL gl, bool anySelected)
        {
            int maxSlices = 20;
            double total = _currentDataPoints.Sum(dp => dp.Y);
            double radPerData = PI2/total;

            // measure strings and render labels
            double maxWidthLabel = double.MinValue;
            double totalTextHeight = 0;
            foreach (var dp in _currentDataPoints)
            {                
                string label = dp.Series.Label;
                if (label == null && _currentDataPoints.Count == 1)
                {
                    label = "All";
                }
                else if (label == null)
                {
                    label = dp.LabelY;
                }
                Size textSize = CommonExtensions.MeasureString(label);
                maxWidthLabel = Math.Max(maxWidthLabel, textSize.Width + 20);
                totalTextHeight += textSize.Height;
            }
            double pieAreaWidth = _textureWidth - maxWidthLabel;

            gl.PushMatrix();
            gl.Translate(pieAreaWidth / 2.0, _textureHeight / 2.0, 0);

            double radius = Math.Min(pieAreaWidth / 2.0, _textureHeight / 2.0) - 2.0f - (FilterModel.GetIncomingFilterModels(FilteringType.Brush).Count == 0 ? 0 : 30);
            double currentAngle = 0;

            int dpCount = 0;
            foreach (var dp in _currentDataPoints)
            {
                if (dp.Y == 0.0)
                {
                    continue;
                }
                dpCount++;
                
                List<Pt> geometryPoints = new List<Pt>();
                double precision = Math.Min(Math.Ceiling((PI2/(dp.Y*radPerData))*80), 250);
                double percentage = (dp.Y*radPerData)/total;

                // draw the pie
                gl.Color(dp.Series.Color.R, dp.Series.Color.G, dp.Series.Color.B, (byte) 255);
                gl.Begin(OpenGL.GL_POLYGON);
                gl.Vertex(0, 0);
                geometryPoints.Add(new Pt(0, 0));
                for (float i = 0; i <= precision; i++)
                {
                    double angle = (i*(dp.Y*radPerData)/precision) + currentAngle;
                    Pt pt = new Pt((float) Math.Sin(angle)*radius, (float) Math.Cos(angle)*radius);
                    gl.Vertex(pt.X, pt.Y);
                    geometryPoints.Add(pt);
                }
                gl.End();

                if (!(dp.IsSelected || !anySelected))
                {
                    gl.Color(1f, 1f, 1f, 0.8f);
                    gl.Begin(OpenGL.GL_POLYGON);
                    gl.Vertex(0, 0);
                    for (float i = 0; i <= precision; i++)
                    {
                        double angle = (i*(dp.Y*radPerData)/precision) + currentAngle;
                        Pt pt = new Pt((float) Math.Sin(angle)*radius, (float) Math.Cos(angle)*radius);
                        gl.Vertex(pt.X, pt.Y);
                    }
                    gl.End();
                }

                currentAngle += dp.Y*radPerData;
                // create geometry 
                dp.Geometry = geometryPoints.Select(pt => pt +
                                                          new Pt(_borderLeft + pieAreaWidth / 2.0 + _openGlControlOffset.X,
                                                              _borderRight + _textureHeight/2.0 + _openGlControlOffset.Y)).GetPolygon();

                // draw the rest of the circle
                if (dpCount == maxSlices)
                {
                    List<DataPoint> restDataPoints = new List<DataPoint>();
                    for (int i = dpCount - 1; i < _currentDataPoints.Count; i++)
                    {
                        restDataPoints.Add(_currentDataPoints[i]);
                    }

                    gl.Color(dp.Series.Color.R, dp.Series.Color.G, dp.Series.Color.B, (byte)255);
                    gl.Begin(OpenGL.GL_POLYGON);
                    gl.Vertex(0, 0);
                    geometryPoints.Add(new Pt(0, 0));
                    precision = 180;
                    for (float i = 0; i <= precision; i++)
                    {
                        double angle = (i * (PI2 - currentAngle) / precision) + currentAngle;
                        Pt pt = new Pt((float)Math.Sin(angle) * radius, (float)Math.Cos(angle) * radius);
                        gl.Vertex(pt.X, pt.Y);
                        geometryPoints.Add(pt);
                    }
                    gl.End();

                    if (!(restDataPoints.Any(dp2 => dp2.IsSelected)|| !anySelected))
                    {
                        gl.Color(1f, 1f, 1f, 0.8f);
                        gl.Begin(OpenGL.GL_POLYGON);
                        gl.Vertex(0, 0);
                        for (float i = 0; i <= precision; i++)
                        {
                            double angle = (i * (PI2 - currentAngle) / precision) + currentAngle;
                            Pt pt = new Pt((float)Math.Sin(angle) * radius, (float)Math.Cos(angle) * radius);
                            gl.Vertex(pt.X, pt.Y);
                        }
                        gl.End();
                    }

                    IPolygon poly = geometryPoints.Select(pt => pt +
                                                          new Pt(_borderLeft + pieAreaWidth / 2.0 + _openGlControlOffset.X,
                                                              _borderRight + _textureHeight / 2.0 + _openGlControlOffset.Y)).GetPolygon();
                    restDataPoints.ForEach(dp2 => dp2.Geometry = poly);
                    break;
                }

            }
            gl.PopMatrix();

            gl.PushMatrix();
            currentAngle = 0;
            dpCount = 0;
            double currentY = 0;
            GLRectangle legendRectangle = new GLRectangle(10, 10, true, true);
            foreach (var dp in _currentDataPoints)
            {
                if (dp.Y == 0.0)
                {
                    continue;
                } 
                dpCount++;
                
                // draw the inner slice label
                double scaleX = (double) _windowWidth/(double) _textureWidth;
                // text drawing does not incorporate the viewport changes
                double scaleY = (double) _windowHeight/(double) _textureHeight;;
                string label = dp.Series.Label;
                if (label == null && _currentDataPoints.Count == 1)
                {
                    label = "All";
                }
                else if (label == null)
                {
                    label = dp.LabelY;
                }
                Size textSize = CommonExtensions.MeasureString(label);
                gl.DrawText(
                    (int) ((pieAreaWidth + 14)*scaleX),
                    (int) (_textureHeight - ((_textureHeight - totalTextHeight) / 2.0f + currentY - textSize.Height / 2.0f)*scaleY),
                    0, 0, 0, "Arial", 12,
                    label);

                gl.Color(dp.Series.Color.R, dp.Series.Color.G, dp.Series.Color.B, (byte)255);
                gl.PushMatrix();
                gl.Translate(pieAreaWidth, (_textureHeight - totalTextHeight) / 2.0f + currentY + 2, 0);
                legendRectangle.Render(gl, RenderMode.Render);
                if (!(dp.IsSelected || !anySelected))
                {
                    gl.Color(1f, 1f, 1f, 0.8f);
                    legendRectangle.Render(gl, RenderMode.Render);
                }
                gl.PopMatrix();

                Rct rct = new Rct(new Pt(
                    pieAreaWidth + _borderLeft + _openGlControlOffset.X,
                    (_textureHeight - totalTextHeight) / 2.0f + currentY + 2 + _borderTop + _openGlControlOffset.Y), 
                    new Vec(maxWidthLabel, textSize.Height));
                dp.Geometry = dp.Geometry.Union(rct.GetPolygon());

                currentY += textSize.Height;

                if (dpCount == maxSlices)
                {
                    // draw the inner slice label
                    scaleX = (double)_windowWidth / (double)_textureWidth;
                    // text drawing does not incorporate the viewport changes
                    scaleY = (double)_windowHeight / (double)_textureHeight;
                    textSize = CommonExtensions.MeasureString("Rest");
                    gl.DrawText(
                        (int)(-textSize.Width / 2.0) +
                        (int)((_textureWidth / 2.0 + Math.Sin(currentAngle + (PI2 - currentAngle) / 2.0) * (radius / 2.0)) * scaleX),
                        (int)(-textSize.Height / 2.0) +
                        (int)
                            ((_textureHeight - (_textureHeight / 2.0 + Math.Cos(currentAngle + (PI2 - currentAngle) / 2.0) * (radius / 2.0))) *
                             scaleY),
                        0, 0, 0, "Arial", 12,
                        "Rest");

                    break;
                }
            }
            gl.PopMatrix();

            // do brushing
            if (FilterModel.GetIncomingFilterModels(FilteringType.Brush).Count > 0)
            {
                double width = 20;
                double widthPerBrush = width / (double)FilterModel.GetIncomingFilterModels(FilteringType.Brush).Count;

                GLRectangle rectangle = new GLRectangle(widthPerBrush, width, true, true);
                GLRectangle rectangleOutline = new GLRectangle(widthPerBrush, width, false, true);
                List<FilterModel> brushModels = FilterModel.GetIncomingFilterModels(FilteringType.Brush);

                dpCount = 0;
                gl.Translate(pieAreaWidth / 2.0, _textureHeight / 2.0, 0);
                foreach (var dp in _currentDataPoints)
                {
                    if (dp.Y == 0.0)
                    {
                        continue;
                    }
                    dpCount++;
                    Pt trans = _dataToScreen * dp.Pt;

                    gl.PushMatrix();
                    gl.Translate(
                        Math.Sin(currentAngle + (dp.Y*radPerData)/2.0)*(radius + 20) - width / 2.0,
                        Math.Cos(currentAngle + (dp.Y*radPerData)/2.0)*(radius + 20) - width / 2.0, 0);
                    foreach (var brushModel in brushModels)
                    {
                        renderBrushRectangle(gl, brushModel, rectangle, dp, width, widthPerBrush, true);
                        gl.Translate(widthPerBrush, 0, 0);
                    }
                    gl.PopMatrix();

                    currentAngle += dp.Y * radPerData;
                    if (dpCount == maxSlices)
                    {
                        break;
                    }
                }
            }
        }

        private void renderScatterPlot(OpenGL gl, bool anySelected, bool renderLines = false)
        {
            GLCircle circleBig = new GLCircle(4, 8);

            double smallestXDataDistance = double.MaxValue;
            double smallestYDataDistance = double.MaxValue;
            if (_xyValueDatapointMapping.Count < 1500)
            {
                foreach (var pt1 in _xyValueDatapointMapping.Keys)
                {
                    foreach (var pt2 in _xyValueDatapointMapping.Keys)
                    {
                        if (pt1 != pt2)
                        {
                            if (pt1.X != pt2.X)
                            {
                                smallestXDataDistance = Math.Min(smallestXDataDistance, Math.Abs(pt1.X - pt2.X));
                            }
                            if (pt1.Y != pt2.Y)
                            {
                                smallestYDataDistance = Math.Min(smallestYDataDistance, Math.Abs(pt1.Y - pt2.Y));
                            }
                        }
                    }
                }
            }
            else
            {
                smallestXDataDistance = 0;
                smallestYDataDistance = 0;
            }
            if (_series.Count > _currentDataPoints.Count)
            {
                smallestXDataDistance = 0;
                smallestYDataDistance = 0;
            }
            float scaleCoefficient = 4.0f;
            if (_xUniqueLabels.Count > 15 && _yUniqueLabels.Count > 15)
            {
                scaleCoefficient = 2.0f;
            }

            // render lines first if needed
            if (renderLines)
            {
                foreach (var serie in _series.Values)
                {
                    Pt? last = null;
                    if (FilterModel.GetIncomingFilterModels(FilteringType.Brush).Count == 0)
                    {
                        gl.Color(serie.Color.R, serie.Color.G, serie.Color.B, (byte) 255);
                    }
                    else
                    {
                        gl.Color(0.5, 0.5, 0.5, 1.0);
                    }
                    gl.Color(serie.Color.R, serie.Color.G, serie.Color.B, (byte)255);
                    gl.LineWidth(3);
                    gl.Begin(OpenGL.GL_LINES);

                    foreach (var dp in _currentDataPoints.Where(d => d.Series == serie).OrderBy(dp => dp.X))
                    {
                        Pt trans = _dataToScreen * dp.Pt;
                        if (last.HasValue)
                        {
                            gl.Vertex(last.Value.X, last.Value.Y);
                            gl.Vertex(trans.X, trans.Y);
                        }
                        last = trans;
                    }

                    gl.End();
                }
            }

            float sceenRadiusX = (float)Math.Abs(_dataToScreen[0, 0] * (smallestXDataDistance / scaleCoefficient));
            float sceenRadiusY = (float)Math.Abs(_dataToScreen[1, 1] * (smallestYDataDistance / scaleCoefficient));

            if (float.IsInfinity(sceenRadiusX) && float.IsInfinity(sceenRadiusY))
            {
                sceenRadiusX = 30;
                sceenRadiusY = 30;
            }
            else if (float.IsInfinity(sceenRadiusX))
            {
                sceenRadiusX = sceenRadiusY;
            }
            else if (float.IsInfinity(sceenRadiusY))
            {
                sceenRadiusY = sceenRadiusX;
            }

            float sceenRadius = Math.Min(sceenRadiusX, sceenRadiusY);
            sceenRadius = (float)Math.Max(sceenRadius, 2.0);


            // No Brushing
            if (FilterModel.GetIncomingFilterModels(FilteringType.Brush).Count == 0)
            {
                foreach (Pt pt in _xyValueDatapointMapping.Keys)
                {
                    List<Pt> vogelPoints = vogelLayouter(_xyValueDatapointMapping[pt].Count, sceenRadiusX, sceenRadiusY);

                    int index = 0;
                    Pt center = _dataToScreen * _xyValueDatapointMapping[pt][0].Pt;
                    foreach (var dp in _xyValueDatapointMapping[pt])
                    {
                        Pt trans = new Pt(center.X, center.Y);
                        if (index > 0)
                        {
                            trans += vogelPoints[index];
                        }
                        gl.PushMatrix();
                        gl.Color(0.5, 0.5, 0.5, 0.7);
                        gl.LineWidth(1);
                        gl.Begin(OpenGL.GL_LINES);
                        gl.Vertex(center.X, center.Y);
                        gl.Vertex(trans.X, trans.Y);
                        gl.End();
                        gl.PopMatrix();

                        index++;
                    }

                    index = 0;
                    foreach (var dp in _xyValueDatapointMapping[pt])
                    {
                        Pt trans = _dataToScreen * dp.Pt;
                        if (index > 0)
                        {
                            trans += vogelPoints[index];
                        }

                        // rendering
                        gl.PushMatrix();
                        gl.Translate(trans.X, trans.Y, 0);
                        gl.Color(dp.Series.Color.R, dp.Series.Color.G, dp.Series.Color.B, (byte) 255);
                        circleBig.Render(gl, RenderMode.Render);

                        // check if some selection is going on
                        if (!(dp.IsSelected || !anySelected))
                        {
                            gl.Color(1f, 1f, 1f, 0.7f);
                            circleBig.Render(gl, RenderMode.Render);
                        }
                        gl.PopMatrix();

                        // create geometry
                        dp.Geometry = new Pt(
                            trans.X + _borderLeft + _openGlControlOffset.X,
                            _textureHeight - (trans.Y) + _borderTop + _openGlControlOffset.Y).GetPoint();
                        index++;
                    }
                }
            }
            // Do Brushing
            else
            {
                double width = /*(FilterModel.GetIncomingFilterModels(FilteringType.Brush).Count)*/ 1 * (_currentDataPoints.Count > 20 ? 10 : 20);
                double widthPerBrush = width / (double)FilterModel.GetIncomingFilterModels(FilteringType.Brush).Count;
               
                GLRectangle rectangle = new GLRectangle(widthPerBrush, width, true, true);
                GLRectangle rectangleOutline = new GLRectangle(width, width, false, true);
                List<FilterModel> brushModels = FilterModel.GetIncomingFilterModels(FilteringType.Brush);
                
                foreach (Pt pt in _xyValueDatapointMapping.Keys)
                {
                    List<Pt> vogelPoints = vogelLayouter(_xyValueDatapointMapping[pt].Count, sceenRadiusX, sceenRadiusY);

                    Pt center = _dataToScreen*_xyValueDatapointMapping[pt][0].Pt;
                    int index = 0;
                    foreach (var dp in _xyValueDatapointMapping[pt])
                    {
                        if (index > 0)
                        {
                            dp.VogelPt = vogelPoints[index];
                        }
                        index++;
                    } 
                }

                IEnumerable<DataPoint> orderedDataPoints =
                     _currentDataPoints.OrderBy(dp => dp.Row.PassesFilterModel.Sum(kvp => kvp.Value))
                         .ThenBy(dp => dp.IsSelected);

                foreach (var dp in orderedDataPoints)
                {
                    Pt trans = _dataToScreen * dp.Pt;
                    trans += dp.VogelPt;

                    gl.PushMatrix();
                    gl.Translate(trans.X - width/2.0, trans.Y - width/2.0, 0);
                    gl.PushMatrix();
                    foreach (var brushModel in brushModels)
                    {
                        renderBrushRectangle(gl, brushModel, rectangle, dp, width, widthPerBrush, false);
                        gl.Translate(widthPerBrush, 0, 0);
                    }
                    gl.PopMatrix();

                    if (dp.IsSelected)
                    {
                        gl.Color(0f, 0f, 0f, 1f);
                        rectangleOutline.Render(gl, RenderMode.Render);
                    }
                    gl.PopMatrix();

                    // create geometry
                    dp.Geometry = new Pt(
                        trans.X + _borderLeft + _openGlControlOffset.X,
                        _textureHeight - (trans.Y) + _borderTop + _openGlControlOffset.Y).GetPoint();
                }
            }
        }

        private void renderBrushRectangle(OpenGL gl, FilterModel brushModel, GLRectangle rectangle, DataPoint dp, double height,
            double widthPerBrush, bool flipY)
        {
            gl.Color(brushModel.Color.R, brushModel.Color.G, brushModel.Color.B, (byte)255);
            rectangle.Render(gl, RenderMode.Render);
            double percantage = 0.0;
            if (dp.Row.PassesFilterModel.ContainsKey(brushModel))
            {
                percantage = dp.Row.PassesFilterModel[brushModel];
            }
            gl.Color(1f, 1f, 1f, 0.7f);
            if (percantage == 0.0)
            {
                rectangle.Render(gl, RenderMode.Render);
            }
            else if (percantage > 0.0 && percantage < 1.0)
            {
                gl.PushMatrix();
                gl.Translate(0, flipY ? 0 : height * percantage, 0);
                renderRectangle(gl, widthPerBrush, height * (1.0 - percantage));
                gl.PopMatrix();
            }
        }

        private void renderRectangle(OpenGL gl, double width, double height, bool flipY = true)
        {
            gl.Begin(OpenGL.GL_QUADS);
            gl.Vertex(0, flipY ? height : 0);
            gl.Vertex(width, flipY ? height : 0);
            gl.Vertex(width, flipY ? 0 : height);
            gl.Vertex(0, flipY ? 0 : height);
            gl.End();   
        }

        private Point _startDrag1 = new Point();
        private Point _firstFingerStartPoint = new Point();
        private double _lastMoveLength = 0;
        private Point _startDrag2 = new Point();

        private Point _current1 = new Point();
        private Point _current2 = new Point();
        private double _length = 0.0;

        private TouchDevice _dragDevice1 = null;
        private TouchDevice _dragDevice2 = null;

        void PlotFilterRenderer4_TouchDownEvent(Object sender, TouchEventArgs e)
        {
            if ((e.TouchDevice is MouseTouchDevice) && (e.TouchDevice as MouseTouchDevice).IsStylus)
                return;

            if (_dragDevice1 == null)
            {
                e.Handled = true;
                e.TouchDevice.Capture(glControl);
                _startDrag1 = e.GetTouchPoint((FrameworkElement)glControl).Position;
                _current1 = e.GetTouchPoint((FrameworkElement)glControl).Position;
                _firstFingerStartPoint = e.GetTouchPoint((FrameworkElement)glControl).Position;

                glControl.AddHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(PlotFilterRenderer4_TouchMoveEvent));
                glControl.AddHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(PlotFilterRenderer4_TouchUpEvent));
                _dragDevice1 = e.TouchDevice;
                _graphTempTransform = Mat.Identity;
            }
            else if (_dragDevice2 == null)
            {
                e.Handled = true;
                e.TouchDevice.Capture(glControl);
                _dragDevice2 = e.TouchDevice;
                _current2 = e.GetTouchPoint((FrameworkElement)glControl).Position;
            }
        }

        void PlotFilterRenderer4_TouchUpEvent(object sender, TouchEventArgs e)
        {
            if (e.TouchDevice == _dragDevice1)
            {
                e.Handled = true;
                e.TouchDevice.Capture(null);
                _dragDevice1 = null;
                _length = 0.0;

                if (_dragDevice2 != null)
                {
                    _dragDevice1 = _dragDevice2;
                    _startDrag1 = _startDrag2;
                    _dragDevice2 = null;
                }
                else
                {
                    // double Tap detected
                    bool doubleTap = false;
                    if (_doubleTapStopwatch.ElapsedMilliseconds < 400 && _doubleTapStopwatch.IsRunning)
                    {
                        if (_lastMoveLength < 10)
                        {
                            _resetGraphTransform = true;
                            doubleTap = true;
                        }
                    }
                    Point curDrag = e.GetTouchPoint((FrameworkElement)glControl).Position;
                    _lastMoveLength = (_firstFingerStartPoint - curDrag).Length;
                    _doubleTapStopwatch.Restart();

                    // check if we hit a datapoint
                    if (!doubleTap && _lastMoveLength < 10)
                    {
                        curDrag = e.GetTouchPoint((FrameworkElement)this).Position;
                        IPolygon poly = new Rct(
                            curDrag.X - 1, curDrag.Y - 1,
                            curDrag.X + 1, curDrag.Y + 1).GetPolygon();

                        // datapoints within hit rect
                        List<DataPoint> selectedDataPoints = new List<DataPoint>();
                        foreach (var dp in _currentDataPoints)
                        {
                            if (dp.Geometry != null && poly.Intersects(dp.Geometry))
                            {
                                selectedDataPoints.Add(dp);
                            }
                            /*Polyline pl = new Polyline();
                            pl.Points = new PointCollection(dp.Geometry.Coordinates.Select((corrd) => new Point(corrd.X, corrd.Y)).ToArray());
                            pl.Stroke = Brushes.Green;
                            pl.StrokeThickness = 1;
                            InqScene _inqScene = this.FindParent<InqScene>();
                            Pt pp = this.TranslatePoint(new Point(0, 0), _inqScene);
                            pl.RenderTransform = new TranslateTransform(pp.X, pp.Y);
                            _inqScene.AddNoUndo(pl);*/
                        }
                        if (selectedDataPoints.Count > 0)
                        {
                            toggleSelection(new List<DataPoint>(new DataPoint[] {selectedDataPoints[0]}));
                        }
                    }
                }
            }
            else if (e.TouchDevice == _dragDevice2)
            {
                e.Handled = true;
                e.TouchDevice.Capture(null);
                _dragDevice2 = null;
                _length = 0.0;
            }
            if (_dragDevice1 == null && _dragDevice2 == null)
            {
                glControl.RemoveHandler(FrameworkElement.TouchMoveEvent, new EventHandler<TouchEventArgs>(PlotFilterRenderer4_TouchMoveEvent));
                glControl.RemoveHandler(FrameworkElement.TouchUpEvent, new EventHandler<TouchEventArgs>(PlotFilterRenderer4_TouchUpEvent));
                _renderGraph = true;
            }
        }

        void PlotFilterRenderer4_TouchMoveEvent(object sender, TouchEventArgs e)
        {
            if (_renderStyle != FilterRendererType.Pie)
            {
                if (e.TouchDevice == _dragDevice1)
                {
                    _current1 = e.GetTouchPoint((FrameworkElement)glControl).Position;
                    //_startDrag1 = _current1;
                    e.Handled = true;
                }
                else if (e.TouchDevice == _dragDevice2)
                {
                    _startDrag2 = e.GetTouchPoint((FrameworkElement)glControl).Position;
                    _current2 = e.GetTouchPoint((FrameworkElement)glControl).Position;
                    e.Handled = true;
                }
            }
        }

        void PlotFilterRenderer4_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double scale = e.Delta > 0 ? 1.05 : 0.95;
           /* Matrix m1 = _dataToScreen;
            m1.ScaleAtPrepend(scale, scale, e.GetTouchPoint(this).X, e.GetTouchPoint(this).Y);
            _dataToScreen = m1;
            */
            Matrix m1 = Matrix.Identity;
            m1.ScaleAt(scale, scale, e.GetPosition(glControl).X, _windowHeight - e.GetPosition(glControl).Y);
            _dataToScreen = _dataToScreen*(Mat)m1;//trans*Mat.Scale(scale, scale)*trans.Inverse();

            m1 = _graphTempTransform;
            m1.ScaleAt(scale, scale, e.GetPosition(glControl).X, _windowHeight - e.GetPosition(glControl).Y);
            _graphTempTransform = m1;

            /*
            m1 = _graphTempTransform;
            m1.ScaleAtPrepend(scale, scale, e.GetTouchPoint(this).X, e.GetTouchPoint(this).Y);
            _graphTempTransform = m1;*/

            e.Handled = true;
            if (!Keyboard.IsKeyDown(Key.A))
            {
                _renderGraph = true;
            }
        }
        protected override void UpdateRendering()
        {
            base.UpdateRendering();
            if (FilterModel == null)
            {
                return;
            }
            base.Init(resetViewport);
            //legend.Visibility = Visibility.Collapsed;
            
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
            string seriesErrMsg = "";
            validityCheck(out xErrMsg, out yErrMsg, out seriesErrMsg);

            _series.Clear();
            _newDataPoints.Clear();
            _xUniqueLabels.Clear();
            _yUniqueLabels.Clear();
            _xLabelDatapointMapping.Clear();
            _yLabelDatapointMapping.Clear(); 
            _xyValueDatapointMapping.Clear();
            _uniqueValueDatapointMapping.Clear();
            _dataStopwatch.Restart();

        //    _data = new Dictionary<object, List<PlotItem>>();
         //   _selectedData = new Dictionary<object, List<PlotItem>>();
        //    _series = new Dictionary<object, PlotFilterSeriesItem>();
        //    _selectedSeries = new Dictionary<object, PlotFilterSeriesItem>();
        //    _uniqueXValues = new Dictionary<object, double>();
         //   _uniqueYValues = new Dictionary<object, double>();

            if (xErrMsg != "" || yErrMsg != "" || seriesErrMsg != "")
            {
                errMain.Visibility = System.Windows.Visibility.Visible;
                return;
            }

            _resetGraphTransform = resetViewport || RenderStyle != FilterModel.FilterRendererType;
            RenderStyle = FilterModel.FilterRendererType;

            _xDataType = FilterModel.GetDataTypeOfPanoramicDataColumnDescriptor(
                FilterModel.GetColumnDescriptorsForOption(Option.X)[0], true);
            _yDataType = FilterModel.GetDataTypeOfPanoramicDataColumnDescriptor(
                FilterModel.GetColumnDescriptorsForOption(Option.Y)[0], true);

            if (_renderStyle != FilterRendererType.Pie && _renderStyle != FilterRendererType.Map)
            {
                xPlaceHolder.Visibility = Visibility.Visible;
                yPlaceHolder.Visibility = Visibility.Visible;

                /*xPlaceHolder.Init(FilterModel, FilterModel.GetColumnDescriptorsForOption(Option.X)[0],
                    _xDataType == DataTypeConstants.FLOAT || _xDataType == DataTypeConstants.INT);
                yPlaceHolder.Init(FilterModel, FilterModel.GetColumnDescriptorsForOption(Option.Y)[0],
                    _yDataType == DataTypeConstants.FLOAT || _yDataType == DataTypeConstants.INT);*/
                xPlaceHolder.ErrorMessage = null;
                yPlaceHolder.ErrorMessage = null;
                //xLabel.Content = FilterModel.GetColumnDescriptorsForOption(Option.X)[0].GetSimpleLabel();
                //yLabel.Content = FilterModel.GetColumnDescriptorsForOption(Option.Y)[0].GetSimpleLabel();
            }
            else
            {
                /*yPlaceHolder.Visibility = Visibility.Visible;
                yPlaceHolder.Init(FilterModel, FilterModel.GetColumnDescriptorsForOption(Option.Y)[0],
                    _yDataType == DataTypeConstants.FLOAT || _yDataType == DataTypeConstants.INT);
                yPlaceHolder.ErrorMessage = null;


                xPlaceHolder.Visibility = Visibility.Collapsed;
                xPlaceHolder.Init(FilterModel, FilterModel.GetColumnDescriptorsForOption(Option.X)[0],
                    _xDataType == DataTypeConstants.FLOAT || _xDataType == DataTypeConstants.INT);
                xPlaceHolder.ErrorMessage = null;*/
            }

            QueryItemsProvider queryItemsProvider = new QueryItemsProvider(new FilterQueryGenerator(FilterModel));
            _currentDataValues = new AsyncVirtualizingCollection<PanoramicDataRow>(queryItemsProvider, 20000 /*page size*/, 1000 /*timeout*/);

            _currentDataValues.CollectionChanged += this.DataValues_CollectionChanged;
            _currentDataValues.PropertyChanged += this.DataValues_PropertyChanged;

            Console.WriteLine(_currentDataValues.Count);
        }

        void DataValues_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (!(_currentDataValues.IsInitializing || _currentDataValues.IsLoading) &&
                _currentDataValues.Count == 0)
            {
                FireDataLoadingComplete(new List<XYValue>());
            }
            FilterModel.RowCount = _currentDataValues.Count;
        }

        private void validityCheck(out string xErrMsg, out string yErrMsg, out string seriesErrMsg)
        {
            xErrMsg = "";
            yErrMsg = "";
            seriesErrMsg = "";

            if (FilterModel.GetColumnDescriptorsForOption(Option.X).Count == 0)
            {
                xErrMsg = "Please specify data column";
            }

            if (FilterModel.GetColumnDescriptorsForOption(Option.Y).Count == 0)
            {
                yErrMsg = "Please specify data column";
            }
        }

        private void LegendItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            PlotFilterSeriesItem ssi = ((FrameworkElement)sender).DataContext as PlotFilterSeriesItem;
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

        public void NotifyStroqAdded(starPadSDK.Inq.Stroq s)
        {
            InqScene inqScene = this.FindParent<InqScene>();
            List<Point> points = s.Select(p => inqScene.TranslatePoint(p, this)).ToList();
            IPolygon poly = points.GetPolygon();
            
            // datapoints within selection
            List<DataPoint> selectedDataPoints = new List<DataPoint>();
            foreach (var dp in _currentDataPoints)
            {
                if (dp.Geometry != null && poly.Intersects(dp.Geometry))
                {
                    selectedDataPoints.Add(dp);
                }
            }
            if (selectedDataPoints.Count > 0)
            {
                toggleSelection(selectedDataPoints);
            }
            inqScene.Rem(s);
            _renderGraph = true;
        }

        private OctreeNode createOctree(
            List<PanoramicDataColumnDescriptor> descriptorsToUse,
            Dictionary<PanoramicDataColumnDescriptor, List<PanoramicDataValue>> sortedElements,
            Dictionary<PanoramicDataColumnDescriptor, Dictionary<PanoramicDataValue, List<DataPoint>>> dataPoints,
            List<DataPoint> currentDataPoints,
            int dimensionIndex, 
            List<PanoramicDataValue> froms,
            List<int> fromsIndex,
            List<PanoramicDataValue> tos,
            List<int> tosIndex)
        {
            OctreeNode node = new OctreeNode();
            PanoramicDataColumnDescriptor cd = descriptorsToUse[dimensionIndex];
            node.ColumnDescriptor = cd;
            int indexFrom = fromsIndex[dimensionIndex];
            int indexTo = tosIndex[dimensionIndex];
            node.Froms = froms;
            node.Tos = tos;

            List<DataPoint> dataPointsToTest = new List<DataPoint>();
            bool allSelected = true;
            List<List<DataPoint>> dps = new List<List<DataPoint>>();
            for (int i = indexFrom; i <= indexTo; i++)
            {
                dataPointsToTest.AddRange(dataPoints[cd][sortedElements[cd][i]]);
            }
            dataPointsToTest = currentDataPoints.Intersect(dataPointsToTest).ToList();
            //var cs = dataPointsToTest.Count(dp => dp.IsSelected);
            //var uniqueX = dataPointsToTest.Select(dp => dp.LabelX).Distinct().ToList();
            //var uniqueY = dataPointsToTest.Select(dp => dp.LabelY).Distinct().ToList();
            if (dataPointsToTest.All(dp => dp.IsSelected) || !dataPointsToTest.Any(dp => dp.IsSelected) || dataPoints.Count == 0)
            {
                node.DataPoints = dataPointsToTest;
                node.Selected = dataPointsToTest.Any(dp => dp.IsSelected);
                return node;
            }
            else
            {
                int count = 0;
                do
                {
                    int nextDimension = (dimensionIndex + count + 1) % descriptorsToUse.Count;
                    var nextColumnDescriptor = descriptorsToUse[nextDimension];
                    indexFrom = fromsIndex[nextDimension];
                    indexTo = tosIndex[nextDimension];

                    int diff = indexTo - indexFrom;
                    if (diff >= 1)
                    {
                        int centerIndex = (int)Math.Floor(diff / 2.0);
                        int leftIndexFrom = indexFrom;
                        int leftIndexTo = indexFrom + centerIndex;
                        List<PanoramicDataValue> leftFroms = froms.ToArray().ToList();
                        List<PanoramicDataValue> leftTos = tos.ToArray().ToList();
                        List<int> leftFromsIndex = fromsIndex.ToArray().ToList();
                        List<int> leftTosIndex = tosIndex.ToArray().ToList();
                        leftFroms[nextDimension] = sortedElements[nextColumnDescriptor][leftIndexFrom];
                        leftTos[nextDimension] = sortedElements[nextColumnDescriptor][leftIndexTo];
                        leftFromsIndex[nextDimension] = leftIndexFrom;
                        leftTosIndex[nextDimension] = leftIndexTo;

                        int rightIndexFrom = indexFrom + centerIndex + 1;
                        int rightIndexTo = indexTo;
                        List<PanoramicDataValue> rightFroms = froms.ToArray().ToList();
                        List<PanoramicDataValue> rightTos = tos.ToArray().ToList();
                        List<int> rightFromsIndex = fromsIndex.ToArray().ToList();
                        List<int> rightTosIndex = tosIndex.ToArray().ToList();
                        rightFroms[nextDimension] = sortedElements[nextColumnDescriptor][rightIndexFrom];
                        rightTos[nextDimension] = sortedElements[nextColumnDescriptor][rightIndexTo];
                        rightFromsIndex[nextDimension] = rightIndexFrom;
                        rightTosIndex[nextDimension] = rightIndexTo;

                        node.Left = createOctree(descriptorsToUse, sortedElements, dataPoints, dataPointsToTest, nextDimension, leftFroms, leftFromsIndex, leftTos, leftTosIndex);
                        node.Right = createOctree(descriptorsToUse, sortedElements, dataPoints, dataPointsToTest, nextDimension, rightFroms, rightFromsIndex, rightTos, rightTosIndex);
                        node.Left.Parent = node;
                        node.Right.Parent = node;
                        return node;
                    }
                    count++;
                } while (count < descriptorsToUse.Count);
            }
            return node;
        }

        private void getOctreeLeafs(OctreeNode octreeNode, List<OctreeNode> leafs)
        {
            if (octreeNode != null) 
            {
                if (octreeNode.DataPoints != null && octreeNode.Selected)
                {
                    leafs.Add(octreeNode);
                }
                getOctreeLeafs(octreeNode.Left, leafs);
                getOctreeLeafs(octreeNode.Right, leafs);
            }
        }
        private void printOctree(OctreeNode octreeNode, int level)
        {
            if (octreeNode != null)
            {
                string pre = string.Join("", Enumerable.Repeat("  ", level).ToList());
                Console.WriteLine(pre + "" + octreeNode.ColumnDescriptor.GetSimpleLabel());
                Console.WriteLine(pre + " s: " + octreeNode.Selected);
                if (octreeNode.DataPoints != null)
                    Console.WriteLine(pre + " c: " + octreeNode.DataPoints.Count);
                //if (OctreeNode.From != null)
                //    Console.WriteLine(pre + " f: " + OctreeNode.From.Value);
                //if (OctreeNode.To != null)
                //    Console.WriteLine(pre + " t: " + OctreeNode.To.Value);

                printOctree(octreeNode.Left, level + 1);
                printOctree(octreeNode.Right, level + 1);
            }
        }

        private void toggleSelection(List<DataPoint> dataPoints)
        {
            if (_currentDataPoints.Count == 0)
            {
                return;
            }
            bool anyAlreadySelected = dataPoints.Any(dp => dp.IsSelected);
            foreach (var sDp in dataPoints)
            {
                if (sDp.IsSelected)
                {
                    sDp.IsSelected = !sDp.IsSelected;
                }
                else if (!anyAlreadySelected)
                {
                    sDp.IsSelected = !sDp.IsSelected;
                }
            }

            // make sure all are selected
            foreach (var datapoint in dataPoints.Where(dp => dp.IsSelected))
            {
                getAllDataPointsFromUniqueValues(datapoint).ForEach(dp => dp.IsSelected = true);
            }


            List<PanoramicDataValue> froms = new List<PanoramicDataValue>();
            List<PanoramicDataValue> tos = new List<PanoramicDataValue>();
            List<int> fromsIndex = new List<int>();
            List<int> tosIndex = new List<int>();
            Dictionary<PanoramicDataColumnDescriptor, List<PanoramicDataValue>> sortedDictionary = new Dictionary<PanoramicDataColumnDescriptor, List<PanoramicDataValue>>();
            List<PanoramicDataColumnDescriptor> toUse =
                _uniqueValueDatapointMapping.Keys.Where(cd => cd.IsAnyGroupingOperationApplied()).ToList();
            if (toUse.Count == 0)
            {
                toUse = _uniqueValueDatapointMapping.Keys.ToList();
            }

            foreach (var columnDescriptor in toUse)
            {
                sortedDictionary.Add(columnDescriptor, _uniqueValueDatapointMapping[columnDescriptor].Keys.OrderBy(dataValue => dataValue.Value).ToList());
                froms.Add(sortedDictionary[columnDescriptor].First());
                tos.Add(sortedDictionary[columnDescriptor].Last());
                fromsIndex.Add(0);
                tosIndex.Add(sortedDictionary[columnDescriptor].Count - 1);
            }

            List<OctreeNode> leafs = new List<OctreeNode>();
            List<DataPoint> newSelectedDataPoints = new List<DataPoint>();
            if (toUse.Count > 0)
            {
                OctreeNode root = createOctree(toUse, sortedDictionary, _uniqueValueDatapointMapping, _currentDataPoints,
                    0, froms, fromsIndex, tos, tosIndex);
                //Console.WriteLine("---");
                //printOctree(root, 0);

                getOctreeLeafs(root, leafs);

                //Console.WriteLine("==> 1: " + newSelectedDataPoints.Count);
                //Console.WriteLine("==> 2: " + leafs.Count);
            }

            List<FilteredItem> toAdd = new List<FilteredItem>();
            foreach (var node in leafs)
            {
                newSelectedDataPoints.AddRange(node.DataPoints);
                FilteredItem fi = new FilteredItem();
                int count = 0;
                foreach (var columnDescriptor in toUse)
                {
                    fi.ColumnComparisonValues.Add(columnDescriptor,
                        new PanoramicDataValueComparison(node.Froms[count], Predicate.GREATER_THAN_EQUAL));

                    fi.GroupComparisonValues.Add(columnDescriptor,
                        new PanoramicDataValueComparison(node.Tos[count], Predicate.LESS_THAN_EQUAL));
                   

                    count++;
                }
                toAdd.Add(fi);
            }

            FilterModel.ClearFilteredItems();
            FilterModel.AddFilteredItems(toAdd, this);

            _currentDataPoints.ForEach(dp => dp.IsSelected = false);
            newSelectedDataPoints.ForEach(dp => dp.IsSelected = true);

            // if all are selected then we again select nothing
            if (_currentDataPoints.All(dp => dp.IsSelected))
            {
                FilterModel.ClearFilteredItems();
            }

            //selectedIntersection.ForEach(dp => dp.IsSelected = true);

            /*
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
            FilterModel.AddFilteredItem(fi, this);*/
            /*
            FilterModel.RemoveFilteredItems(toRemove, this);
            if (toRemove.Count == 0)
            {
                FilterModel.AddFilteredItems(toAdd, this);
            }

            */
            // update the all datapoints that match the currently selected datapoints
            /*_currentDataPoints.ForEach(dp => dp.IsSelected = false);
            foreach (var fi in FilterModel.FilteredItems.ToArray())
            {
                _currentDataPoints.Where(dp => fi.Equals(new FilteredItem(dp.Row))).ForEach(dp => dp.IsSelected = true);
            }
            FilterModel.RemoveFilteredItems(toRemove, this);*/
        }

        public void NotifyStroqRemoved(starPadSDK.Inq.Stroq s)
        {
        }

        public void NotifyStroqsRemoved(starPadSDK.Inq.StroqCollection sc)
        {
        }

        public void NotifyStroqsAdded(starPadSDK.Inq.StroqCollection sc)
        {
        }

        private IAxisRenderer createAxisRenderer(string dataType,
            Range<double> range,
            double offset1, double offset2,
            double width, double height, double ticksBackgroundSize,
            Mat dataToScreen, bool flipped,
            IList<DataPoint> dataPoints)
        {
            IAxisRenderer axisRenderer = null;

            if (dataType == AttributeDataTypeConstants.NVARCHAR ||
                dataType == AttributeDataTypeConstants.GEOGRAPHY ||
                dataType == AttributeDataTypeConstants.GUID)
            {
                Range<int> intRange = new Range<int>((int)range.Min, (int)range.Max);
                axisRenderer = new IntegerAxisRenderer(
                    intRange, offset1, offset2, width, height, ticksBackgroundSize, dataToScreen, flipped);
               
                var lp = new CollectionLabelProvider<int>();
                Dictionary<int, string> dict = new Dictionary<int, string>();
                foreach (var dp in dataPoints)
                {
                    if (!flipped)
                    {
                        if (!dict.Keys.Contains((int)dp.X))
                        {
                            dict.Add((int)dp.X, dp.LabelX);
                        }
                    }
                    else
                    {
                        if (!dict.Keys.Contains((int)dp.Y))
                        {
                            dict.Add((int)dp.Y, dp.LabelY);
                        }
                    }
                }
                lp.Collection = dict;
                ((AxisRenderer<int>) axisRenderer).LabelProvider = lp;
            }
            else if (dataType == AttributeDataTypeConstants.FLOAT ||
                     dataType == AttributeDataTypeConstants.BIT)
            {
                axisRenderer = new NumericAxisRenderer(
                    range, offset1, offset2, width, height, ticksBackgroundSize, dataToScreen, flipped);
            }
            else if (dataType == AttributeDataTypeConstants.INT)
            {
                Range<int> intRange = new Range<int>((int)range.Min, (int)range.Max);
                axisRenderer = new IntegerAxisRenderer(
                    intRange, offset1, offset2, width, height, ticksBackgroundSize, dataToScreen, flipped);
            }
            else if (dataType == AttributeDataTypeConstants.DATE)
            {
                Range<DateTime> dateRange = new Range<DateTime>(
                   new DateTime((long)range.Min), new DateTime((long)range.Max));
                axisRenderer = new DateTimeAxisRenderer(
                    dateRange, offset1, offset2, width, height, ticksBackgroundSize, dataToScreen, flipped);
            }
            else if (dataType == AttributeDataTypeConstants.TIME) 
            {
                Range<int> intRange = new Range<int>((int)range.Min, (int)range.Max);
                axisRenderer = new IntegerAxisRenderer(
                    intRange, offset1, offset2, width, height, ticksBackgroundSize, dataToScreen, flipped);

                ((IntegerAxisRenderer)axisRenderer).LabelProvider.CustomFormatter = (tickInfo) =>
                {
                    int hours = (tickInfo.Tick / 3600);
                    int minutes = (tickInfo.Tick % 3600) / 60;
                    int seconds = (tickInfo.Tick % 60);

                    if ((minutes < 10) && (seconds < 10))
                        return hours + ":0" + minutes + ":0" + seconds;
                    else if ((minutes > 9) && (seconds < 10))
                        return hours + ":" + minutes + ":0" + seconds;
                    else if ((minutes < 10) && (seconds > 9))
                        return hours + ":0" + minutes + ":" + seconds;
                    else
                        return hours + ":" + minutes + ":" + seconds;
                };

            }

            return axisRenderer;
        }

        private List<Pt> vogelLayouter(int n, float xScale, float yScale)
        {
            float goldenAngle = (float)(Math.PI * (3 - Math.Sqrt(5)));

            List<Pt> points = new List<Pt>();

            for (int i = 0; i < n; i++)
            {
                float theta = i * goldenAngle;
                float r = (float)(Math.Sqrt(i) / Math.Sqrt(n));
                points.Add(new Pt(r * Math.Cos(theta) * xScale, r * Math.Sin(theta) * yScale));
            }
            return points;
        } 
    }

    public interface IAxisRenderer
    {
        void RenderTicks(OpenGL gl);
    }

    public class NumericAxisRenderer : AxisRenderer<double>
    {
        public NumericAxisRenderer(
            Range<double> range,
            double offset1, double offset2,
            double width, double height, double ticksBackgroundSize,
            Mat dataToScreen, bool flipped) : base(
                new NumericTicksProvider(1), new ExponentialLabelProvider(1), range,
                offset1, offset2, width, height, ticksBackgroundSize, dataToScreen, flipped)
        {

            ConvertToDouble = d => d;
        }
    }

    public class DateTimeAxisRenderer : AxisRenderer<DateTime>
    {
        public DateTimeAxisRenderer(
            Range<DateTime> range,
            double offset1, double offset2,
            double width, double height, double ticksBackgroundSize,
            Mat dataToScreen, bool flipped)
            : base(
                new DateTimeTicksProvider(), new DateTimeLabelProvider(), range,
                offset1, offset2, width, height, ticksBackgroundSize, dataToScreen, flipped)
        {
            ConvertToDouble = dt => dt.Ticks;
        }
    }

    public class IntegerAxisRenderer : AxisRenderer<int>
    {
        public IntegerAxisRenderer(
            Range<int> range,
            double offset1, double offset2,
            double width, double height, double ticksBackgroundSize,
            Mat dataToScreen, bool flipped)
            : base(
                new IntegerTicksProvider(), new GenericLabelProvider<int>(), range,
                offset1, offset2, width, height, ticksBackgroundSize, dataToScreen, flipped)
        {

            ConvertToDouble = d => d;
        }
    }

    public abstract class AxisRenderer<T> : IAxisRenderer
    {
        public LabelProviderBase<T> LabelProvider;

        protected Func<T, double> ConvertToDouble;

        private ITicksProvider<T> _ticksProvider;
        private Range<T> _range;
        private double _offset1 = 0;
        private double _offset2 = 0;
        private double _ticksBackgroundSize = 0;
        private Mat _dataToScreen = Mat.Identity;

        private double[] _screenTicks;
        private int _previousTickCount = 5;
        private ITicksInfo<T> _ticksInfo;
        private T[] _ticks;
        private UIElement[] _labels;
        private const double _increaseRatio = 3.0;
        private const double _decreaseRatio = 1.6;

        private Func<Size, double> getSize;
        private Func<Pt, double> getDimension;

        private Size _size;
        private bool _flipped = false;


        protected AxisRenderer(ITicksProvider<T> ticksProvider, 
            LabelProviderBase<T> labelProvider, Range<T> range,
            double offset1, double offset2,
            double width, double height, double ticksBackgroundSize,
            Mat dataToScreen, bool flipped)
        {
            _ticksProvider = ticksProvider;
            LabelProvider = labelProvider;
            _range = range;
            _offset1 = offset1;
            _offset2 = offset2;
            _ticksBackgroundSize = ticksBackgroundSize;
            _dataToScreen = dataToScreen;
            _size = new Size(width, height);
            _flipped = flipped;

            if (!flipped)
            {
                getSize = size => size.Width;
                getDimension = pt => pt.X;
            }
            else
            {
                getSize = size => size.Height;
                getDimension = pt => pt.Y; 
            }
        }

        public void RenderTicks(OpenGL gl)
        {
            createTicks();

            // removing unfinite screen ticks
            var tempTicks = new List<T>(_ticks);
            var tempScreenTicks = new List<double>(_ticks.Length);
            var tempLabels = new List<UIElement>(_labels);

            int i = 0;
            while (i < tempTicks.Count)
            {
                T tick = tempTicks[i];
                double screenTick = GetCoordinateFromTick(tick);
                if (screenTick.IsFinite())
                {
                    tempScreenTicks.Add(screenTick);
                    i++;
                }
                else
                {
                    tempTicks.RemoveAt(i);
                    tempLabels.RemoveAt(i);
                }
            }

            _ticks = tempTicks.ToArray();
            _screenTicks = tempScreenTicks.ToArray();
            _labels = tempLabels.ToArray();

            drawTicks(_screenTicks, gl);
        }


        private void createTicks()
        {
            TickCountChange result = TickCountChange.OK;

            int prevActualTickCount = -1;

            int tickCount = _previousTickCount;
            int iteration = 0;
            do
            {
                _ticksInfo = _ticksProvider.GetTicks(_range, tickCount);
                _ticks = _ticksInfo.Ticks;

                if (_ticks.Length == prevActualTickCount)
                {
                    //result = TickCountChange.OK;
                    //break;
                }

                prevActualTickCount = _ticks.Length;

                _labels = LabelProvider.CreateLabels(_ticksInfo);

                TickCountChange prevResult = result;
                result = CheckLabelsArrangement(_labels, _ticks);

                if (prevResult == TickCountChange.Decrease && result == TickCountChange.Increase)
                {
                    // stop tick number oscillating
                    result = TickCountChange.OK;
                }

                if (result != TickCountChange.OK)
                {
                    int prevTickCount = tickCount;
                    if (result == TickCountChange.Decrease)
                        tickCount = _ticksProvider.DecreaseTickCount(tickCount);
                    else
                    {
                        tickCount = _ticksProvider.IncreaseTickCount(tickCount);
                        //DebugVerify.Is(tickCount >= prevTickCount);
                    }

                    // ticks provider could not create less ticks or tick number didn't change
                    if (tickCount == 0 || prevTickCount == tickCount)
                    {
                        tickCount = prevTickCount;
                        result = TickCountChange.OK;
                    }
                }
            } while (result != TickCountChange.OK);

            _previousTickCount = tickCount;
        }

        private TickCountChange CheckLabelsArrangement(UIElement[] labels, T[] ticks)
        {
            var actualLabels = labels.Select((label, i) => new { Label = label, Index = i })
                .Where(el => el.Label != null)
                .Select(el => new { Label = el.Label, Tick = ticks[el.Index] })
                .ToList();

            actualLabels.ForEach(item => item.Label.Measure(_size));

            var sizeInfos = actualLabels.Select(item =>
                new { Dimension = GetCoordinateFromTick(item.Tick), Size = getSize(item.Label.DesiredSize) })
                .OrderBy(item => item.Dimension).ToArray();

            TickCountChange res = TickCountChange.OK;

            int increaseCount = 0;
            for (int i = 0; i < sizeInfos.Length - 1; i++)
            {
                if ((sizeInfos[i].Dimension + sizeInfos[i].Size * _decreaseRatio) > sizeInfos[i + 1].Dimension)
                {
                    res = TickCountChange.Decrease;
                    break;
                }
                if ((sizeInfos[i].Dimension + sizeInfos[i].Size * _increaseRatio) < sizeInfos[i + 1].Dimension)
                {
                    increaseCount++;
                }
            }
            if (increaseCount > sizeInfos.Length / 2)
                res = TickCountChange.Increase;

            return res;
        }

        private void drawTicks(double[] screenTicks, OpenGL gl)
        {
            for (int i = 0; i < screenTicks.Length; i++)
            {
                if (_labels[i] == null)
                    continue;

                //if (_labels[i] is TextBlock && (labels[i] as TextBlock).Text == "0" && !ShowZeros) // bcz:added
                //    continue;

                if (screenTicks[i] < 0 || screenTicks[i] > getSize(_size))
                {
                    continue;
                }
                gl.LineWidth(1.0f);
                gl.Begin(OpenGL.GL_LINES);
                int textX = 0;
                int textY = 0;
                if (!_flipped)
                {
                    gl.Color(0,0,0);
                    gl.Vertex(screenTicks[i] + _offset1, -2);
                    gl.Vertex(screenTicks[i] + _offset1, +2);

                    gl.Color(0.4f, 0.4f, 0.4f, 0.2f);
                    gl.Vertex(screenTicks[i] + _offset1, 0);
                    gl.Vertex(screenTicks[i] + _offset1, -_ticksBackgroundSize);

                    textX = (int) (screenTicks[i] + _offset1 - getSize(_labels[i].DesiredSize)/2.0);
                    textY = (int)(_size.Height - _labels[i].DesiredSize.Height);
                }
                else
                {
                    gl.Color(0, 0, 0);
                    gl.Vertex(-2, getSize(_size) - screenTicks[i] + _offset1);
                    gl.Vertex(+2, getSize(_size) - screenTicks[i] + _offset1);

                    gl.Color(0.4f, 0.4f, 0.4f, 0.2f);
                    gl.Vertex(0, getSize(_size) - screenTicks[i] + _offset1);
                    gl.Vertex(_ticksBackgroundSize, getSize(_size) - screenTicks[i] + _offset1);

                    textX = (int)(_size.Width - _labels[i].DesiredSize.Width - 5);
                    textY = (int)(screenTicks[i] + _offset2 - getSize(_labels[i].DesiredSize) / 3.0);
                }
                gl.End();

                gl.DrawText(
                    textX, textY, 0, 0, 0, "Arial", 12,
                    (_labels[i] as TextBlock).Text);
            }
        }

        private double GetCoordinateFromTick(T tick)
        {
            return getDimension(_dataToScreen * new Pt(ConvertToDouble(tick), ConvertToDouble(tick)));
        }
    }

    public class OctreeNode
    {
        public PanoramicDataColumnDescriptor ColumnDescriptor { get; set; }
        public List<PanoramicDataValue> Froms { get; set; }
        public List<PanoramicDataValue> Tos { get; set; }
        public List<DataPoint> DataPoints { get; set; }
        public bool Selected { get; set; }
        public OctreeNode Left { get; set; }
        public OctreeNode Right { get; set; }
        public OctreeNode Parent { get; set; }
    }

    public class Range
    {
        public PanoramicDataValue From { get; set; }
        public PanoramicDataValue To { get; set; }
        public bool Selected { get; set; }
        public int Count { get; set; }
        public List<DataPoint> DataPoints { get; set; }

        public Range()
        {
            DataPoints = new List<DataPoint>();
        }
    }


    public class DataPoint
    {
        public PanoramicDataValue DataValueX { get; set; }
        public PanoramicDataValue DataValueY { get; set; }
        public PanoramicDataValue GroupedDataValue { get; set; }
        public PanoramicDataRow Row { get; set; }
        public bool IsSelected { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public string LabelY { get; set; }
        public string LabelX { get; set; }
        public DateTime DateX { get; set; }
        public DateTime DateY { get; set; }
        public DataPointSeries Series { get; set; }
        public IGeometry Geometry { get; set; }
        public Pt VogelPt { get; set; }
        public Pt Pt
        {
            get
            {
                return new Pt(X, Y);
            }
        }
    }

    public class DataPointSeries
    {
        public List<PanoramicDataValue> DataValues { get; set; }
        public List<DataPoint> DataPoints { get; set; }
        public List<PanoramicDataValue> GroupedDataValues { get; set; }
        public bool IsSelected { get; set; }
        public string Label { get; set; }
        public Color Color { get; set; }

        public DataPointSeries()
        {
            DataPoints = new List<DataPoint>();
        }
    }

    public class GLRectangle : IRenderable
    {
        private double _width = -1;
        private double _height = -1;
        private bool _filled = false;
        private bool _flipY = false;
        private DisplayList _displayList = null;

        public GLRectangle(double width, double height, bool filled, bool flipY)
        {
            _width = width;
            _height = height;
            _filled = filled;
            _flipY = flipY;
        }

        public void Render(OpenGL gl, RenderMode renderMode)
        {
            if (_displayList != null)
            {
                _displayList.Call(gl);
            }
            else
            {
                _displayList = new DisplayList();
                _displayList.Generate(gl);
                _displayList.New(gl, DisplayList.DisplayListMode.Compile);
                
                gl.LineWidth(2);
                gl.Begin(_filled ? OpenGL.GL_QUADS : OpenGL.GL_LINE_LOOP);
                gl.Vertex(0, _flipY ? _height : 0);
                gl.Vertex(_width, _flipY ? _height : 0);
                gl.Vertex(_width, _flipY ? 0 : _height);
                gl.Vertex(0, _flipY ? 0 : _height);
                gl.End();  

                _displayList.End(gl);
                _displayList.Call(gl);
            }
        }
    }



    public class GLCircle : IRenderable
    {
        private double _radius = -1;
        private double _precision = -1;
        private DisplayList _displayList = null;

        public GLCircle(double radius, double precision)
        {
            _radius = radius;
            _precision = precision;
        }

        public void Render(OpenGL gl, RenderMode renderMode)
        {
            if (_displayList != null)
            {
                _displayList.Call(gl);
            }
            else
            {
                _displayList = new DisplayList();
                _displayList.Generate(gl);
                _displayList.New(gl, DisplayList.DisplayListMode.Compile);

                gl.Begin(OpenGL.GL_POLYGON);
                for (float i = 0; i < _precision; i++)
                {
                    gl.Vertex(
                        (float) Math.Sin(i*Math.PI*2f/(float) _precision)*_radius,
                        (float) Math.Cos(i*Math.PI*2f/(float) _precision)*_radius, 0);
                }
                gl.End();

                _displayList.End(gl);
                _displayList.Call(gl);
            }
        }
    }

    public class PlotFilterSeriesItem : ViewModelBase
    {
        public PanoramicDataValue DataValue { get; set; }
        public PanoramicDataValue GroupedDataValue { get; set; }
        public MarkerPointsGraph MarkerPointsGraph { get; set; }
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
