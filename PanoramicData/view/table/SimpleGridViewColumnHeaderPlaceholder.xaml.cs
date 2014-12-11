using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using starPadSDK.WPFHelp;
using PixelLab.Common;
using starPadSDK.AppLib;
using PanoramicDataModel;
using PanoramicData.model.view;

namespace PanoramicData.view.table
{
    /// <summary>
    /// Interaction logic for SimpleGridViewColumnHeaderPlaceholder.xaml
    /// </summary>
    public partial class SimpleGridViewColumnHeaderPlaceholder : UserControl, ColumnHeaderEventHandler
    {
        public delegate void ChangedHandler(object sender, ColumnHeaderEventArgs e);
        public event ChangedHandler Changed;

        public static readonly DependencyProperty LabelTextProperty =
            DependencyProperty.Register("LabelText", typeof(string),
            typeof(SimpleGridViewColumnHeaderPlaceholder),
            new PropertyMetadata("Default Label:"));

        public string LabelText
        {
            get
            {
                return (string)GetValue(LabelTextProperty);
            }
            set
            {
                SetValue(LabelTextProperty, value);
            }
        }

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register("ErrorMessage", typeof(string),
            typeof(SimpleGridViewColumnHeaderPlaceholder),
            new PropertyMetadata("Default Error Message"));

        public string ErrorMessage
        {
            get
            {
                return (string)GetValue(ErrorMessageProperty);
            }
            set
            {
                SetValue(ErrorMessageProperty, value);
            }
        }

        public void Init(FilterModel filterModel, PanoramicDataColumnDescriptor descriptor, bool enableScaleFunction)
        {
            gridContent.Children.Clear();
            if (descriptor != null)
            {
                SimpleGridViewColumnHeader ch = new SimpleGridViewColumnHeader();
                ch.IsSimpleRendering = true;
                ch.EnableRemoveOptionInRadialMenu = false;
                ch.IsInteractive = true;
                ch.DataContext = descriptor;
                ch.FilterModel = filterModel;
                ch.EnableScaleFunctionInRadialMenu = enableScaleFunction;
                gridContent.Children.Add(ch);
            }
        }

        public SimpleGridViewColumnHeaderPlaceholder()
        {
            InitializeComponent();
        }

        public void ColumnHeaderMoved(SimpleGridViewColumnHeader sender, ColumnHeaderEventArgs e, bool overElement)
        {
            borderHighlight.Visibility = Visibility.Collapsed;

            InqScene inqScene = this.FindParent<InqScene>();
            if (overElement)
            {
                try
                {
                    if (gridContent.GetBounds(inqScene).IntersectsWith(e.Bounds))
                    {
                        borderHighlight.Visibility = Visibility.Visible;
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public void ColumnHeaderDropped(SimpleGridViewColumnHeader sender, ColumnHeaderEventArgs e)
        {
            borderHighlight.Visibility = Visibility.Collapsed;

            InqScene inqScene = this.FindParent<InqScene>();
            if (inqScene != null)
            {
                if (gridContent.GetBounds(inqScene).IntersectsWith(e.Bounds))
                {
                    if (Changed != null)
                    {
                        Changed(this, e);
                    }
                }
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
}
