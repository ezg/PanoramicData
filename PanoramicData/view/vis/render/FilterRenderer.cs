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
using PanoramicData.model.view;
using PanoramicData.model.data;

namespace PanoramicData.view.vis.render
{
    public class FilterRenderer : UserControl
    {
        public FilterRenderer()
        {
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
