using GeoAPI.Geometries;
using PanoramicData.controller.data;
using PanoramicData.model.data;
using PanoramicData.model.view;
using PanoramicData.view.Direct2D;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
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

namespace PanoramicData.view.vis.render
{
    /// <summary>
    /// Interaction logic for XYRenderer.xaml
    /// </summary>
    public partial class XYRenderer : FilterRenderer
    {
        private static string DEFAULT_GROUPING = "default";

        private IDisposable _xObservableDisposable = null;
        private IDisposable _yObservableDisposable = null;

        private List<XYDataPoint> _dataPoints = new List<XYDataPoint>();
        private Dictionary<string, XYDataPointSeries> _series = new Dictionary<string, XYDataPointSeries>();
        private string _xDataType = "";
        private string _yDataType = ""; 
        private Dictionary<string, double> _xUniqueLabels = new Dictionary<string, double>();
        private Dictionary<string, double> _yUniqueLabels = new Dictionary<string, double>();

        private int _toLoad = 0;
        private int _loaded = 0;


        private XYRendererContent _renderContent = null;
        public XYRendererContent RenderContent
        {
            get
            {
                return _renderContent;
            }
            set
            {
                _renderContent = value;
                contentGrid.Children.Add(_renderContent);
            }
        }

        public XYRenderer()
        {
            InitializeComponent();
            this.DataContextChanged += XYRenderer_DataContextChanged; 
            xPlaceHolder.Changed += XPlaceHolderOnChanged;
            yPlaceHolder.Changed += YPlaceHolderOnChanged;
        }

        void YPlaceHolderOnChanged(object sender, AttributeViewModelEventArgs e)
        {
            QueryModel queryModel = (DataContext as VisualizationViewModel).QueryModel;
            queryModel.RemoveFunctionAttributeOperationModel(AttributeFunction.Y, queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Y).First());
            queryModel.AddFunctionAttributeOperationModel(AttributeFunction.Y, e.AttributeOperationModel);
        }

        void XPlaceHolderOnChanged(object sender, AttributeViewModelEventArgs e)
        {
            QueryModel queryModel = (DataContext as VisualizationViewModel).QueryModel;
            queryModel.RemoveFunctionAttributeOperationModel(AttributeFunction.X, queryModel.GetFunctionAttributeOperationModel(AttributeFunction.X).First());
            queryModel.AddFunctionAttributeOperationModel(AttributeFunction.X, e.AttributeOperationModel);
        }

        void setXPlaceHolder()
        {
             QueryModel queryModel = (DataContext as VisualizationViewModel).QueryModel;
             if (queryModel.GetFunctionAttributeOperationModel(AttributeFunction.X).Count > 0)
             {
                 xPlaceHolder.DataContext = new AttributeViewModel((DataContext as VisualizationViewModel),
                     queryModel.GetFunctionAttributeOperationModel(AttributeFunction.X).First())
                     {
                         IsNoChrome = true
                     };
             }
        }
        void setYPlaceHolder()
        {
            QueryModel queryModel = (DataContext as VisualizationViewModel).QueryModel;
            if (queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Y).Count > 0)
            {
                yPlaceHolder.DataContext = new AttributeViewModel((DataContext as VisualizationViewModel),
                    queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Y).First())
                {
                    IsNoChrome = true
                };
            }
        }

        void XYRenderer_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                if (_xObservableDisposable != null)
                {
                    _xObservableDisposable.Dispose();
                }
                if (_yObservableDisposable != null)
                {
                    _yObservableDisposable.Dispose();
                }
                QueryModel queryModel = (e.OldValue as VisualizationViewModel).QueryModel;
                queryModel.QueryResultModel.PropertyChanged -= QueryResultModel_PropertyChanged;
            }
            if (e.NewValue != null)
            {
                QueryModel queryModel = (e.NewValue as VisualizationViewModel).QueryModel;
                queryModel.QueryResultModel.PropertyChanged += QueryResultModel_PropertyChanged;

                if (_xObservableDisposable != null)
                {
                    _xObservableDisposable.Dispose();
                }
                _xObservableDisposable = Observable.FromEventPattern<NotifyCollectionChangedEventArgs>(
                    queryModel.GetFunctionAttributeOperationModel(AttributeFunction.X), "CollectionChanged")
                    .Throttle(TimeSpan.FromMilliseconds(50))
                    .Subscribe((arg) =>
                    {
                        Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                        {
                            setXPlaceHolder();
                        }));
                    });

                if (_yObservableDisposable != null)
                {
                    _yObservableDisposable.Dispose();
                }
                _yObservableDisposable = Observable.FromEventPattern<NotifyCollectionChangedEventArgs>(
                    queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Y), "CollectionChanged")
                    .Throttle(TimeSpan.FromMilliseconds(50))
                    .Subscribe((arg) =>
                    {
                        Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                        {
                            setYPlaceHolder();
                        }));
                    });

                setXPlaceHolder();
                setYPlaceHolder();
                if (queryModel.QueryResultModel.QueryResultItemModels != null)
                {
                    CollectionChangedEventManager.AddHandler(queryModel.QueryResultModel.QueryResultItemModels, QueryResultItemModels_CollectionChanged);
                    var count = queryModel.QueryResultModel.QueryResultItemModels.Count;
                }
                if (queryModel.QueryResultModel.QueryResultItemModels.Count != 0)
                {
                    parseData();
                    render();
                }
            }
        }
        
        void QueryResultModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            QueryResultModel resultModel = (DataContext as VisualizationViewModel).QueryModel.QueryResultModel;
            if (e.PropertyName == resultModel.GetPropertyName(() => resultModel.QueryResultItemModels))
            {
                CollectionChangedEventManager.AddHandler(resultModel.QueryResultItemModels, QueryResultItemModels_CollectionChanged);
                var count = resultModel.QueryResultItemModels.Count;
            }            
        }

        void QueryResultItemModels_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            parseData();   
        }

        void row_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Data")
            {
                DataWrapper<QueryResultItemModel> row = ((DataWrapper<QueryResultItemModel>)sender);
                row.PropertyChanged -= row_PropertyChanged;

                XYDataPoint dataPoint = createDataPointFromQueryResultItemModel(row.Data);                
            }
        }

        private void parseData()
        {
            QueryModel queryModel = (DataContext as VisualizationViewModel).QueryModel;

            // clear out all collections
            _dataPoints.Clear();
            _series.Clear();
            _xUniqueLabels.Clear();
            _yUniqueLabels.Clear();


            // get datatypes for x and y
            _xDataType = queryModel.GetDataType(queryModel.GetFunctionAttributeOperationModel(AttributeFunction.X).First(), true);
            _yDataType = queryModel.GetDataType(queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Y).First(), true);

            QueryResultModel resultModel = (DataContext as VisualizationViewModel).QueryModel.QueryResultModel;
            _toLoad = resultModel.QueryResultItemModels.Count;
            _loaded = 0;
            foreach (DataWrapper<QueryResultItemModel> row in resultModel.QueryResultItemModels)
            {
                if (row.IsLoading)
                {
                    row.PropertyChanged += row_PropertyChanged;
                }
                else
                {
                    XYDataPoint dataPoint = createDataPointFromQueryResultItemModel(row.Data);
                }
            }
        }

        private void render()
        {
            _renderContent.Render(_dataPoints, _series);
        }

        XYDataPoint createDataPointFromQueryResultItemModel(QueryResultItemModel queryResultItemModel)
        {
            QueryModel queryModel = (DataContext as VisualizationViewModel).QueryModel;

            double valueX;
            string labelX;
            DateTime dateX;
            QueryResultItemValueModel dataValueX;
            bool xIsNull = getDataPointValue(
                queryResultItemModel, 
                queryModel.GetFunctionAttributeOperationModel(AttributeFunction.X).First(),
                _xDataType, _xUniqueLabels,
                out valueX, out labelX, out dateX, out dataValueX);

            double valueY;
            string labelY;
            DateTime dateY;
            QueryResultItemValueModel dataValueY;
            bool yIsNull = getDataPointValue(
                    queryResultItemModel,
                    queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Y).First(),
                    _yDataType, _yUniqueLabels,
                    out valueY, out labelY, out dateY, out dataValueY);
            
            XYDataPoint dataPoint = new XYDataPoint();
            dataPoint.X = valueX;
            dataPoint.Y = valueY;
            dataPoint.LabelX = labelX;
            dataPoint.LabelY = labelY;
            dataPoint.DateX = dateX;
            dataPoint.DateY = dateY;
            dataPoint.ValueModelX = dataValueX;
            dataPoint.ValueModelY = dataValueY;
            dataPoint.XIsNull = xIsNull;
            dataPoint.YIsNull = yIsNull;
            dataPoint.IsSelected = queryResultItemModel.IsSelected;

            XYDataPointSeries series = createSeries(queryResultItemModel);
            dataPoint.XYDataPointSeries = series;
            series.XYDataPoints.Add(dataPoint);
            _dataPoints.Add(dataPoint);

            _loaded++;

            if (_toLoad == _loaded)
            {
                render();
            }
            return dataPoint;
        }

        XYDataPointSeries createSeries(QueryResultItemModel queryResultItemModel)
        {
            VisualizationViewModel visualizationModel = (DataContext as VisualizationViewModel);
            QueryModel queryModel = visualizationModel.QueryModel;

            string grouping = DEFAULT_GROUPING;
            List<QueryResultItemValueModel> dataValues = new List<QueryResultItemValueModel>();
            if (queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Color).Count > 0)
            {
                List<string> groupingList = new List<string>();
                foreach (var attributeOperationModel in queryModel.GetFunctionAttributeOperationModel(AttributeFunction.Color))
                {
                    QueryResultItemValueModel dataValue = queryResultItemModel.Values[attributeOperationModel];
                    if (dataValue != null)
                    {
                        dataValues.Add(dataValue);
                    }
                    groupingList.Add(attributeOperationModel.GetHashCode().ToString() + ":" + dataValue.StringValue);
                }
                grouping = string.Join(":", groupingList);
            }

            XYDataPointSeries series = null;
            if (!_series.ContainsKey(grouping))
            {
                series = new XYDataPointSeries();
                series.Color = visualizationModel.Color;
                if (grouping != DEFAULT_GROUPING)
                {
                    series.Color = RendererResources.GetGroupingColor(grouping);
                }
                series.ValueModels = dataValues;
                _series[grouping] = series;
            }
            else
            {
                series = _series[grouping];
            }
            return series;
        }

        bool getDataPointValue(QueryResultItemModel queryResultItemModel, AttributeOperationModel attributeOperationModel, string dataType,
            Dictionary<string, double> currentLabelList,
            out double value, out string label, out DateTime date, out QueryResultItemValueModel valueModel)
        {
            value = 0;
            date = DateTime.Now;
            bool isNull = false;

            valueModel = queryResultItemModel.Values[attributeOperationModel];
            label = valueModel.StringValue;

            if (dataType == AttributeDataTypeConstants.FLOAT ||
                dataType == AttributeDataTypeConstants.INT)
            {
                double d = 0;
                if (valueModel.Value != null && double.TryParse(valueModel.StringValue, out d))
                {
                    value = d;
                }
                else
                {
                    if (valueModel.Value != null && double.TryParse(valueModel.Value.ToString(), out d))
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
                if (valueModel.Value != null && double.TryParse(valueModel.Value.ToString(), out d))
                {
                    value = d;
                }
                else
                {
                    if (valueModel.Value != null && valueModel.Value.ToString().Equals("True"))
                    {
                        value = 1;
                    }
                    else if (valueModel.Value != null && valueModel.Value.ToString().Equals("False"))
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
                value = queryResultItemModel.RowNumber - 1;
                if (valueModel.Value != null)
                {
                    /*if (attributeOperationModel.AggregateFunction == AggregateFunction.Bin)
                    {
                        double d = 0;

                        if (valueModel.Value != null && double.TryParse(valueModel.Value.ToString(), out d))
                        //if (valueModel.Value != null &&
                        //    double.TryParse(loadedRow.GetGroupedValue(columnDescriptor).StringValue, out d))
                        {

                            value = d / attributeOperationModel.BinSize;
                        }
                    }
                    else*/
                    {
                        string thisLabel = valueModel.StringValue;
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
                if (valueModel.Value != null)
                {
                    value = ((DateTime)valueModel.Value).Ticks;
                    date = (DateTime)valueModel.Value;
                }
                else
                {
                    isNull = true;
                }
            }
            else if (dataType == AttributeDataTypeConstants.TIME)
            {
                if (valueModel.Value != null)
                {
                    value = ((TimeSpan)valueModel.Value).TotalSeconds;
                }
                else
                {
                    isNull = true;
                }
            }

            return isNull;
        }
    }

    public class XYDataPoint
    {
        public QueryResultItemValueModel ValueModelX { get; set; }
        public QueryResultItemValueModel ValueModelY { get; set; }
        public bool IsSelected { get; set; }
        public bool XIsNull { get; set; }
        public bool YIsNull { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public string LabelY { get; set; }
        public string LabelX { get; set; }
        public DateTime DateX { get; set; }
        public DateTime DateY { get; set; }
        public XYDataPointSeries XYDataPointSeries { get; set; }
        public IGeometry Geometry { get; set; }
        public Point Point
        {
            get
            {
                return new Point(X, Y);
            }
        }
    }
    public class XYDataPointSeries
    {
        public XYDataPointSeries()
        {
            ValueModels = new List<QueryResultItemValueModel>();
            XYDataPoints = new List<XYDataPoint>();
        }
        public Color Color { get; set; }
        public List<QueryResultItemValueModel> ValueModels { get; set; }
        public List<XYDataPoint> XYDataPoints { get; set; }
    }
}
