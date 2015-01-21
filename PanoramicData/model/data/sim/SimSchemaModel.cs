using PanoramicData.controller.data;
using PanoramicData.controller.input;
using PanoramicDataDBConnector;
using PanoramicDataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.data.sim
{
    public class SimSchemaModel : SchemaModel
    {
        private OriginModel _rootOriginModel = null;

        public SimSchemaModel(DatasetConfiguration datasetConfiguration) 
        {
            _rootOriginModel = new SimOriginModel(datasetConfiguration);
        }

        public override List<OriginModel> OriginModels
        {
            get
            {
                List<OriginModel> originModels = new List<OriginModel>();
                originModels.Add(_rootOriginModel);
                return originModels;
            }
        }

        public override Dictionary<CalculatedAttributeModel, string> CalculatedAttributeModels
        {
            get
            {
                return new Dictionary<CalculatedAttributeModel, string>();
            }
        }

        public override Dictionary<NamedAttributeModel, string> NamedAttributeModels
        {
            get
            {
                return new Dictionary<NamedAttributeModel, string>();
            }
        }
    }
}
