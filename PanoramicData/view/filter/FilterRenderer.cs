using System.Collections.Generic;
using System.Windows.Documents;
using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PanoramicDataModel;
using starPadSDK.WPFHelp;
using PanoramicData.model.view;

namespace PanoramicData.view.filter
{
    public class FilterRenderer : UserControl
    {
        private IDisposable _filterModelDisposable = null;
        private Grid _frozen = null;
        private object _oldContent = null;

        public delegate void DataLoadingCompleteHandler(object sender, List<XYValue> values);
        public event DataLoadingCompleteHandler DataLoadingComplete;

        protected virtual void FireDataLoadingComplete(List<XYValue> values)
        {
            if (DataLoadingComplete != null)
            {
                DataLoadingComplete(this, values);
            }
        }

        public static readonly DependencyProperty FilterModelProperty = DependencyProperty.Register("FilterModel", typeof(FilterModel), typeof(FilterRenderer), new PropertyMetadata(OnFilterModelChanged));

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
            (obj as FilterRenderer).OnFilterModelChanged(args);
        }

        protected virtual void OnFilterModelChanged(DependencyPropertyChangedEventArgs args)
        {
            if (_filterModelDisposable != null)
            {
                _filterModelDisposable.Dispose();
            }
            if (args.NewValue != null)
            {
                _filterModelDisposable = Observable.FromEventPattern<FilterModelUpdatedEventArgs>(
                    (FilterModel) args.NewValue, "FilterModelUpdated")
                    .Where(
                        arg =>
                            arg.EventArgs != null &&
                            (arg.EventArgs.Mode != UpdatedMode.UI || (arg.EventArgs.Mode == UpdatedMode.UI && (arg.EventArgs.SubMode == SubUpdatedMode.Color || arg.EventArgs.SubMode == SubUpdatedMode.RenderStyle))) &&
                            arg.EventArgs.Mode != UpdatedMode.FilteredItemsStatus)
                    .Throttle(TimeSpan.FromMilliseconds(50))
                    .Subscribe((arg) =>
                    {
                        Application.Current.Dispatcher.BeginInvoke(new System.Action(() =>
                        {
                            if (arg.EventArgs.Mode == UpdatedMode.Structure ||
                                arg.EventArgs.Mode == UpdatedMode.Incoming ||
                                (arg.EventArgs.Mode == UpdatedMode.UI && (arg.EventArgs.SubMode == SubUpdatedMode.Color || arg.EventArgs.SubMode == SubUpdatedMode.RenderStyle)) ||
                                (arg.EventArgs.Mode == UpdatedMode.FilteredItemsChange &&
                                 (arg.EventArgs.Sender != this || arg.EventArgs.Sender == null)))
                            {
                                Init(arg.EventArgs.Mode != UpdatedMode.Incoming);
                            }
                            if (arg.EventArgs.Mode == UpdatedMode.Structure)
                            {
                                FilterModel.ClearFilteredItems();
                            }
                        }));
                    });

                Init(true);
            }
        }

        public FilterRenderer()
        {
            this.DataLoadingComplete += FilterRenderer_DataLoadingComplete;
        }

        public virtual byte[] CreateImage()
        {
            return null;
        }

        protected virtual void Init(bool resetViewport)
        {
            if (this.Content is FrameworkElement && _oldContent == null)
            {
                FrameworkElement contentElement = this.Content as FrameworkElement;
                if (contentElement.ActualWidth != 0.0 &&
                    contentElement.ActualHeight != 0.0)
                {
                    _oldContent = this.Content;
                    _frozen = new Grid();

                    Image img = FrostyFreeze.CreateImageFromControl(this.Content as FrameworkElement);
                    //img.Opacity = 0.5;
                    img.Width = img.Height = double.NaN;

                    _frozen.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                    _frozen.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;

                    _frozen.Children.Add(img);

                    this.Content = _frozen;
                }
            }
        }

        void FilterRenderer_DataLoadingComplete(object sender, List<XYValue> values)
        {
            if (_oldContent != null)
            {
                this.Content = _oldContent;
                _oldContent = null;
            }
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
        {
            return new GeometryHitTestResult(this, IntersectionDetail.Intersects);
        }
    }

    public class XYValue
    {
        public string X { get; set; }
        public double Y { get; set; }
    }
}
