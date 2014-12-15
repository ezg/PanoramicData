using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using starPadSDK.WPFHelp;
using PixelLab.Common;
using starPadSDK.AppLib;
using PanoramicDataModel;
using PanoramicData.model.view;
using PanoramicData.model.view_new;
using PanoramicData.view.inq;

namespace PanoramicData.view.table
{
    /// <summary>
    /// Interaction logic for AttributePlaceholderview.xaml
    /// </summary>
    public partial class AttributePlaceholderview : UserControl, AttributeViewModelEventHandler
    {
        public delegate void ChangedHandler(object sender, AttributeViewModelEventArgs e);
        public event ChangedHandler Changed;

        public static readonly DependencyProperty LabelTextProperty =
            DependencyProperty.Register("LabelText", typeof(string),
            typeof(AttributePlaceholderview),
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
            typeof(AttributePlaceholderview),
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

        public AttributePlaceholderview()
        {
            InitializeComponent();
        }


        public  void AttributeViewModelMoved(AttributeViewModel sender, AttributeViewModelEventArgs e, bool overElement)
        {
            borderHighlight.Visibility = Visibility.Collapsed;

            InkableScene inkableScene = this.FindParent<InkableScene>();
            if (overElement)
            {
                try
                {
                    if (gridContent.GetBounds(inkableScene).IntersectsWith(e.Bounds))
                    {
                        borderHighlight.Visibility = Visibility.Visible;
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public void AttributeViewModelDropped(AttributeViewModel sender, AttributeViewModelEventArgs e)
        {
            borderHighlight.Visibility = Visibility.Collapsed;

            InkableScene inkableScene = this.FindParent<InkableScene>();
            if (inkableScene != null)
            {
                if (gridContent.GetBounds(inkableScene).IntersectsWith(e.Bounds))
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
