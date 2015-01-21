using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.data.sim
{
    public class SimAttributeModel : AttributeModel
    {
        public SimAttributeModel(string name, string attributeVisualizationType)
        {
            _name = name;
            _attributeVisualizationType = attributeVisualizationType;
        }

        private string _name = "";
        public override string Name
        {
            get
            {
                return _name;
            }
        }

        private string _attributeVisualizationType = "";
        public override string AttributeVisualizationType
        {
            get
            {
                return _attributeVisualizationType;
            }
        }
    }
}
