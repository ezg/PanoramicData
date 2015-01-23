using PanoramicData.controller.data;
using PanoramicData.controller.data.sim;
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
        private QueryExecuter _queryExecuter = null;

        public SimSchemaModel(DatasetConfiguration datasetConfiguration) 
        {
            _rootOriginModel = new SimOriginModel(datasetConfiguration);
            _queryExecuter = new SimQueryExecuter();
        }

        public override QueryExecuter QueryExecuter
        {
            get
            {
                return _queryExecuter;
            }
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
