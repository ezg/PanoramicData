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
using PanoramicData.model.view_new;

namespace PanoramicData.view.vis.render
{
    public class FilterRenderer : UserControl
    {
        protected VisualizationViewResultModel VisualizationViewResultModel { get; set; }
        protected VisualizationViewModel VisualizationViewModel { get; set; }

        public FilterRenderer()
        {
            this.DataContextChanged += FilterRenderer_DataContextChanged;
        }

        void FilterRenderer_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                (e.OldValue as VisualizationViewModel).VisualizationViewResultModel.PropertyChanged -= VisualizationViewResultModel_PropertyChanged;
            }
            if (e.NewValue != null)
            {
                (e.NewValue as VisualizationViewModel).VisualizationViewResultModel.PropertyChanged += VisualizationViewResultModel_PropertyChanged;
                this.UpdateResults(); 
            }
        }

        void VisualizationViewResultModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.UpdateResults();
        }

        protected virtual void UpdateResults()
        {
            VisualizationViewModel = (DataContext as VisualizationViewModel);
            VisualizationViewResultModel = (DataContext as VisualizationViewModel).VisualizationViewResultModel;
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
