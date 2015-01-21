using PanoramicData.controller.input;
using PanoramicData.utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.data.sim
{
    public class SimOriginModel : OriginModel
    {
        private DatasetConfiguration _datasetConfiguration { get; set; }

        public SimOriginModel(DatasetConfiguration datasetConfiguration)
        {
            _datasetConfiguration = datasetConfiguration;

            StreamReader file = new StreamReader(@"Resources\configs\" + _datasetConfiguration.DataFile);
            List<string> names = CSVParser.CSVLineSplit(file.ReadLine());
            List<string> dataTypes = CSVParser.CSVLineSplit(file.ReadLine());

            for (int i = 0; i < names.Count; i++)
            {
                _attributeModels.Add(new SimAttributeModel(names[i], dataTypes[i]));
            }
        }

        public override string Name
        {
            get
            {
                return _datasetConfiguration.Name;
            }
        }

        private List<AttributeModel> _attributeModels = new List<AttributeModel>();
        public override List<AttributeModel> AttributeModels
        {
            get
            {
                return _attributeModels;
            }
        }

        public override List<OriginModel> OriginModels
        {
            get
            {
                return new List<OriginModel>();
            }
        }
    }
}
