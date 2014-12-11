using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PixelLab.Common;
using PanoramicDataModel;
using starPadSDK.Inq;
using System.Reactive.Linq;
using PanoramicData.model.view;
using PanoramicData.utils.inq;
using PanoramicData.controller.data;
using PanoramicData.view.table;

namespace PanoramicData.view.filter
{
    /// <summary>
    /// Interaction logic for Filter.xaml
    /// </summary>
    public partial class Filter : UserControl, StroqListener
    {
        private IDisposable _filterModelDisposable = null;
        private InqAnalyzer _inqAnalyser = new InqAnalyzer();

        public static readonly DependencyProperty FilterModelProperty = DependencyProperty.Register("FilterModel", typeof(FilterModel), typeof(Filter), new PropertyMetadata(OnFilterModelChanged));

        public FilterModel FilterModel
        {
            get
            {
                return (FilterModel)GetValue(FilterModelProperty);
            }
            set
            {
                SetValue(FilterModelProperty, value);
            }
        }

        static void OnFilterModelChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            (obj as Filter).OnFilterModelChanged(args);
        }

        private void OnFilterModelChanged(DependencyPropertyChangedEventArgs args)
        {
            if (_filterModelDisposable != null)
            {
                _filterModelDisposable.Dispose();
            }
            _filterModelDisposable = Observable.FromEventPattern<FilterModelUpdatedEventArgs>((FilterModel)args.NewValue, "FilterModelUpdated")
                    .Throttle(TimeSpan.FromMilliseconds(50))
                          .Subscribe((arg) =>
                          {
                              Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                              {
                                  if (arg.EventArgs.Mode != UpdatedMode.FilteredItemsChange &&
                                      arg.EventArgs.Mode != UpdatedMode.UI)
                                  {
                                      init();
                                  }
                              }));
                          });

            init();
        }

        public Filter()
        {
            InitializeComponent();
            inputGrid.Visibility = System.Windows.Visibility.Collapsed;
            //_mathManager = new MathManager(null, recogRenderer, aPage, null);
            //_mathManager.RecognitionChanged += _mathManager_RecognitionChanged;
            //_mathManager.Width = 50;
            //_mathManager.Height = 50;
            _inqAnalyser.ResultsUpdated += _inqAnalyser_ResultsUpdated;

            aPage.StroqAddedEvent += new starPadSDK.AppLib.InqScene.StroqHandler(stroqAddedEvent);
            aPage.StroqRemovedEvent += new starPadSDK.AppLib.InqScene.StroqHandler(stroqRemovedEvent);
            aPage.StroqsAddedEvent += new starPadSDK.AppLib.InqScene.StroqsHandler(stroqsAddedEvent);
            aPage.StroqsRemovedEvent += new starPadSDK.AppLib.InqScene.StroqsHandler(stroqsRemovedEvent);

        }

        void _inqAnalyser_ResultsUpdated(object sender, System.Windows.Ink.ResultsUpdatedEventArgs e)
        {
            result.Content = _inqAnalyser.GetRecognizedString();
            
            FilterModel.ClearEmbeddedFilteredItems();

            foreach (var cd in FilterModel.ColumnDescriptors)
            {
                FilteredItem fi = new FilteredItem();
                //fi.Columns.Add(cd);
                //fi.Predicates.Add(Predicate.LIKE);
                PanoramicDataValue val = new PanoramicDataValue();
                val.Value = _inqAnalyser.GetRecognizedString();
                val.DataType = DataTypeConstants.NVARCHAR;
                //fi.Values.Add(val);
                FilterModel.AddEmbeddedFilteredItem(fi);
            }
        }

        void stroqAddedEvent(Stroq s)
        {
            //_mathManager.UpdateMathRecognition(aPage.Stroqs);
            _inqAnalyser.AddStroke(s);
            _inqAnalyser.BackgroundAnalyze();
        }

        void stroqsAddedEvent(Stroq[] stroqs)
        {
            foreach (var s in stroqs)
            {
                _inqAnalyser.AddStroke(s);
            }
            _inqAnalyser.BackgroundAnalyze();
            //_mathManager.UpdateMathRecognition(aPage.Stroqs);
        }

        void stroqRemovedEvent(Stroq s)
        {
            //_mathManager.UpdateMathRecognition(aPage.Stroqs);
            _inqAnalyser.RemoveStroke(s);
            _inqAnalyser.BackgroundAnalyze();
        }

        void stroqsRemovedEvent(Stroq[] stroqs)
        {
            //_mathManager.UpdateMathRecognition(aPage.Stroqs);
            foreach (var s in stroqs)
            {
                _inqAnalyser.RemoveStroke(s);
            }
            _inqAnalyser.BackgroundAnalyze();
        }

        void inputGrid_Click(object sender, EventArgs e)
        {
            if (inputGrid.Visibility == System.Windows.Visibility.Visible) 
            {
                inputGrid.Visibility = System.Windows.Visibility.Collapsed;
            }
            else if (inputGrid.Visibility == System.Windows.Visibility.Collapsed) 
            {
                inputGrid.Visibility = System.Windows.Visibility.Visible;
                inputGrid.Height = 100;
            }
        }

        private void init()
        {
            int index = tabs.SelectedIndex;

            // add FilterRenderers based on fields
            List<FilterRendererType> Renderers = new List<FilterRendererType>();

            if (FilterModel.ColumnDescriptors.Count > 1)
            {
//                Renderers.Add(FilterRendererType.DB);
                Renderers.Add(FilterRendererType.Plot);
            }
            else if (FilterModel.ColumnDescriptors.Count > 0)
            {
                PanoramicDataColumnDescriptor columnDescriptor = FilterModel.ColumnDescriptors.First();
                if (columnDescriptor.IsPrimaryKey)
                {

                }
                else if (columnDescriptor.VisualizationType == VisualizationTypeConstants.NUMERIC)
                {
                    Renderers.Add(FilterRendererType.Histogram);
                }
                else if (columnDescriptor.VisualizationType == VisualizationTypeConstants.DATE)
                {
                }
                else if (columnDescriptor.VisualizationType == VisualizationTypeConstants.GEOGRAPHY)
                {
                    Renderers.Add(FilterRendererType.Map);
                }
                else if (columnDescriptor.VisualizationType == VisualizationTypeConstants.CATEGORY)
                {
                }
                else if (columnDescriptor.VisualizationType == VisualizationTypeConstants.ENUM)
                {
                    Renderers.Add(FilterRendererType.Pie);
                }
                else
                {
                }

                Renderers.Add(FilterRendererType.Table);
            }
            tabs.ItemsSource = Renderers;
            tabs.ContentTemplateSelector = new FilterRendererDataTemplateSelector(FilterModel);
            tabs.SelectedIndex = 0;
            
        }

        public void NotifyStroqAdded(Stroq s)
        {
            IEnumerable<FilterRenderer> renderers = this.VisualDescendentsOfType<FilterRenderer>();
            foreach (var r in renderers)
            {
                if (r is StroqListener)
                {
                    (r as StroqListener).NotifyStroqAdded(s);
                }
            }
        }

        public void NotifyStroqRemoved(Stroq s)
        {
        }

        public void NotifyStroqsRemoved(StroqCollection sc)
        {
        }

        public void NotifyStroqsAdded(StroqCollection sc)
        {
        }
    }

    public class FilterRendererDataTemplateSelector : DataTemplateSelector
    {
        private FilterModel _filterModel = null;

        public FilterRendererDataTemplateSelector(FilterModel filterModel)
        {
            _filterModel = filterModel;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item != null && item is FilterRendererType)
            {
               /* FilterRendererType filterRenderer = (FilterRendererType)item;
                FrameworkElementFactory factory = null;
                DataTemplate dataTemplate = new DataTemplate();

                if (filterRenderer == FilterRendererType.Table)
                {
                    factory = new FrameworkElementFactory(typeof(TableFilterRenderer));
                    dataTemplate.VisualTree = factory;
                }
                else if (filterRenderer == FilterRendererType.Histogram)
                {
                    factory = new FrameworkElementFactory(typeof(HistogramFilterRenderer));
                    dataTemplate.VisualTree = factory;
                }
                else if (filterRenderer == FilterRendererType.Map)
                {
                    factory = new FrameworkElementFactory(typeof(MapFilterRenderer));
                    dataTemplate.VisualTree = factory;
                }
                else if (filterRenderer == FilterRendererType.Plot)
                {
                    factory = new FrameworkElementFactory(typeof(PlotFilterRenderer));
                    dataTemplate.VisualTree = factory;
                }
                else if (filterRenderer == FilterRendererType.Pie)
                {
                    factory = new FrameworkElementFactory(typeof(PieFilterRenderer));
                    dataTemplate.VisualTree = factory;
                }

                if (factory != null)
                {
                    factory.SetValue(FilterRenderer.FilterModelProperty, _filterModel);
                    factory.SetValue(FilterRenderer.MarginProperty, new Thickness(0, 0, 0, 2));
                }

                return dataTemplate;*/
                return null;
                    
            }

            return null;
        }
    }
}
