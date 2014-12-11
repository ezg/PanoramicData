using OxyPlot.Axes;
using PanoramicData.view.other;
using System.Collections.Generic;

namespace PanoramicData.view.other
{
    /// <summary>
    /// Represents an axis with linear scale.
    /// </summary>
    public class CustomDataTimeAxis : DateTimeAxis
    {
        public List<PlotItem> Data { get; set; }
        public bool DataUseXField { get; set; }

        /// <summary>
        /// Formats the value to be used on the axis.
        /// </summary>
        /// <param name="x">
        /// The value.
        /// </param>
        /// <returns>
        /// The formatted value.
        /// </returns>
        public override string FormatValue(double x)
        {
            var index = (int)x;
            if (this.Data != null && index >= 0 && index < this.Data.Count)
            {
                if (DataUseXField)
                {
                    return base.FormatValue(ToDouble(this.Data[index].DateX));
                }
                else
                {
                    return base.FormatValue(ToDouble(this.Data[index].DateY));
                }
            }

            return null;
        }
    }
}