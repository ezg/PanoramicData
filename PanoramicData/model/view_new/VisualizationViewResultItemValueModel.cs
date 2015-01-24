using PanoramicData.model.data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.view_new
{
    public class VisualizationViewResultItemValueModel
    {
        public object Value { get; set; }
        public string StringValue { get; set; }
        public string ShortStringValue { get; set; }
        
        public VisualizationViewResultItemValueModel()
        {
        }

        public override int GetHashCode()
        {
            int code = Value.GetHashCode();
            return code;
        }
        public override bool Equals(object obj)
        {
            if (obj is VisualizationViewResultItemValueModel)
            {
                var pv = obj as VisualizationViewResultItemValueModel;
                if (pv.Value.Equals(Value))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
