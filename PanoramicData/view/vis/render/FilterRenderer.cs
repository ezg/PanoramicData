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
using PanoramicData.model.data;

namespace PanoramicData.view.vis.render
{
    public class FilterRenderer : UserControl
    {
        protected VisualizationViewModel VisualizationViewModel { get; set; }

        public FilterRenderer()
        {
            this.DataContextChanged += FilterRenderer_DataContextChanged;
        }

        void FilterRenderer_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue != null)
            {
                (e.OldValue as VisualizationViewModel).QueryModel.QueryResultModel.PropertyChanged -= QueryResultModel_PropertyChanged;
            }
            if (e.NewValue != null)
            {
                (e.NewValue as VisualizationViewModel).QueryModel.QueryResultModel.PropertyChanged += QueryResultModel_PropertyChanged;
                this.UpdateResults(); 
            }
        }

        void QueryResultModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.UpdateResults();
        }

        protected virtual void UpdateResults()
        {
            VisualizationViewModel = (DataContext as VisualizationViewModel);
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
