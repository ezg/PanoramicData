using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Maps.MapControl.WPF;
using starPadSDK.AppLib;
using starPadSDK.WPFHelp;
using PixelLab.Common;
using System.ComponentModel;
using PanoramicDataModel;
using PanoramicData.view.filter;
using PanoramicData.utils.inq;
using PanoramicData.controller.data;
using PanoramicData.model.view;

namespace PanoramicData.view.vis
{
    /// <summary>
    /// Interaction logic for MapFilterRenderer2.xaml
    /// </summary>
    public partial class MapFilterRenderer2 : FilterRenderer, StroqListener
    {
        public static string BingMapKey = "AsCWUwaCGfYJHrpJn-EP_Ha46W8xqc4bDQIZS213vQw4leiPW4bx9sNi4dJImIcZ";

        private long _dragTime = DateTime.Now.Ticks;
        private delegate List<List<object>> ExecuteQueryDelegate(string schema, string query);
        private List<DataWrapper<PanoramicDataRow>> _currentRows = new List<DataWrapper<PanoramicDataRow>>();
        AsyncVirtualizingCollection<PanoramicDataRow> _currentDataValues = null;

        private Dictionary<object, Color> _series = null;
        private static string DEFAULT_GROUPING = "default";

        private int _toLoad = 0;
        private int _loaded = 0;

        public MapFilterRenderer2()
            : this(false)
        {
        }

        public MapFilterRenderer2(bool showSettings)
        {
            InitializeComponent();
           
            map.MouseDown += map_MouseDown;
            map.IsManipulationEnabled = true; // hack for making the touch events work properly
        }
        
        void map_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true; // hack for making the touch events work properly
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
                    FilterModel.GetColumnDescriptorsForOption(Option.Location).Count > 0 &&
                    FilterModel.GetColumnDescriptorsForOption(Option.Label).Count > 0)
                {
                    PanoramicDataRow loadedRow = ((DataWrapper<PanoramicDataRow>)sender).Data;
                    _loaded++; 
                    SimpleMapPushpin p = new SimpleMapPushpin();
                    p.Row = loadedRow;

                    // use series to identify the corrcet color
                    string grouping = DEFAULT_GROUPING;
                    if (FilterModel.GetColumnDescriptorsForOption(Option.ColorBy).Count > 0)
                    {
                        grouping = loadedRow.GetValue(FilterModel.GetColumnDescriptorsForOption(Option.ColorBy)[0]).StringValue;
                    }
                    if (!_series.ContainsKey(grouping))
                    {
                        Color c = FilterModel.Color;
                        if (grouping != DEFAULT_GROUPING)
                        {
                            c = FilterRendererResources.GetGroupingColor(grouping);
                        }
                        _series.Add(grouping, c);
                    }
                    Color color = _series[grouping];

                    // compute label from xDescriptors
                    List<string> labelParts = new List<string>();
                    foreach (var cd in FilterModel.GetColumnDescriptorsForOption(Option.Label))
                    {
                        string labelPart = loadedRow.GetValue(cd).StringValue;
                        labelParts.Add(labelPart.TrimTo(15));
                    }
                    string label = string.Join("\n", labelParts);

                    p.init(loadedRow.GetValue(FilterModel.GetColumnDescriptorsForOption(Option.Location)[0]).StringValue, 
                        label, this, color);
                    p.FilteredItem = new FilteredItem(loadedRow);

                    if (FilterModel != null && FilterModel.FilteredItems.Contains(p.FilteredItem))
                    {
                        p.Selected = true;
                    }

                    map.Children.Add(p);

                    if (_loaded == _toLoad)
                    {
                        List<Microsoft.Maps.MapControl.WPF.Location> locs = new List<Microsoft.Maps.MapControl.WPF.Location>();
                        
                        foreach (var pp in map.Children)
                        {
                            if (pp is SimpleMapPushpin)
                            {
                                if (((SimpleMapPushpin)pp).Location != null)
                                {
                                    locs.Add(((SimpleMapPushpin)pp).Location);
                                }
                            }
                        }

                        LocationRect rect = new LocationRect(locs);
                        map.SetView(rect);
                        map.ZoomLevel = map.ZoomLevel * 0.7;

                        // remove unused / invisible FilterItems
                        List<FilteredItem> toRemove = new List<FilteredItem>();
                        foreach (var fi in FilterModel.FilteredItems.ToArray())
                        {
                            bool found = false;
                            foreach (var pin in map.Children)
                            {
                                if (pin is SimpleMapPushpin)
                                {
                                    if (fi.Equals(new FilteredItem(((SimpleMapPushpin)pin).Row)))
                                    {
                                        found = true;
                                        break;
                                    }
                                }
                            }
                            if (!found)
                            {
                                toRemove.Add(fi);
                            }
                        }
                        FilterModel.RemoveFilteredItems(toRemove, this);
                    }
                }
            }
            catch (Exception exc)
            {
            }
        }

        protected override void UpdateRendering()
        {
            base.UpdateRendering();
            if (FilterModel == null)
            {
                return;
            }
            
            _series = new Dictionary<object, Color>();
            map.Children.Clear();

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

            if (xErrMsg != "" || yErrMsg != "" || seriesErrMsg != "")
            {
                errMain.Visibility = System.Windows.Visibility.Visible;
                return;
            }

            QueryItemsProvider queryItemsProvider = new QueryItemsProvider(new FilterQueryGenerator(FilterModel));
            _currentDataValues = new AsyncVirtualizingCollection<PanoramicDataRow>(queryItemsProvider, 1000 /*page size*/, 1000 /*timeout*/);

            _currentDataValues.CollectionChanged += this.DataValues_CollectionChanged;

            Console.WriteLine(_currentDataValues.Count);
        }

        public bool TogglePushpins(GeoAPI.Geometries.IGeometry geom)
        {
            InqScene inqScene = this.FindParent<InqScene>();
            bool anyChanges = false;
            bool anySelected = false;
            foreach (FrameworkElement elem in map.Children)
            {
                if (elem is SimpleMapPushpin)
                {
                    if (geom.Intersects(elem.GetBounds(inqScene).GetPolygon()))
                    {
                        if ((elem as SimpleMapPushpin).Selected)
                        {
                            anySelected = true;
                        }
                    }
                }
            }

            foreach (FrameworkElement elem in map.Children)
            {
                if (elem is SimpleMapPushpin)
                {
                    if (geom.Intersects(elem.GetBounds(inqScene).GetPolygon()))
                    {
                        if (anySelected)
                        {
                            (elem as SimpleMapPushpin).Selected = false;
                        }
                        else
                        {
                            (elem as SimpleMapPushpin).Selected = true;
                        }
                        anyChanges = true;
                    }
                }
            }
            return anyChanges;
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

        public void setFiltredItem(FilteredItem FilteredItem, bool _selected)
        {
            if (_selected)
            {
                if (!FilterModel.FilteredItems.Contains(FilteredItem))
                {
                    FilterModel.AddFilteredItem(FilteredItem, this);
                }
            }
            else
            {
                if (FilterModel.FilteredItems.Contains(FilteredItem))
                {
                    FilterModel.RemoveFilteredItem(FilteredItem, this);
                }
            }
        }

        private void validityCheck(out string xErrMsg, out string yErrMsg, out string seriesErrMsg)
        {
            xErrMsg = "";
            yErrMsg = "";
            seriesErrMsg = "";

            if (FilterModel.GetColumnDescriptorsForOption(Option.Label).Count == 0)
            {
                xErrMsg = "Please specify label column";
            }

            if (FilterModel.GetColumnDescriptorsForOption(Option.Location).Count > 0)
            {
                if (FilterModel.GetDataTypeOfPanoramicDataColumnDescriptor(FilterModel.GetColumnDescriptorsForOption(Option.Location)[0], true) !=
                    AttributeDataTypeConstants.GEOGRAPHY) 
                {
                    yErrMsg = "Not a geo datatype";
                }
            }
            else
            {
                yErrMsg = "Please specify data column";
            }
        }

        public void NotifyStroqAdded(starPadSDK.Inq.Stroq s)
        {
            InqScene inqScene = this.FindParent<InqScene>();
            bool strokeUsed = this.TogglePushpins(s.GetPolygon());
            if (strokeUsed)
            {
                inqScene.Rem(s);
            }
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
    }
}
