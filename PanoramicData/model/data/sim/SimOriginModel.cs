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

            _idAttributeModel = new SimAttributeModel(this, "ID", AttributeDataTypeConstants.INT);
            for (int i = 0; i < names.Count; i++)
            {
                AttributeModel attributeModel = new SimAttributeModel(this, names[i], dataTypes[i]);
                _attributeModels.Add(attributeModel);
            }
            string line = file.ReadLine();
            int count = 0;
            while (line != null)
            {
                Dictionary<AttributeModel, object> items = new Dictionary<AttributeModel, object>();
                items[_idAttributeModel] = count;

                List<string> values = CSVParser.CSVLineSplit(line);
                for (int i = 0; i < values.Count; i++)
                {
                    object value = null;
                    if (_attributeModels[i].AttributeDataType == AttributeDataTypeConstants.NVARCHAR)
                    {
                        value = values[i].ToString();
                    }
                    else if (_attributeModels[i].AttributeDataType == AttributeDataTypeConstants.FLOAT)
                    {
                        double d = 0.0;
                        if (double.TryParse(values[i].ToString(), out d))
                        {
                            value = d;
                        }
                    }
                    else if (_attributeModels[i].AttributeDataType == AttributeDataTypeConstants.INT)
                    {
                        int d = 0;
                        if (int.TryParse(values[i].ToString(), out d))
                        {
                            value = d;
                        }
                    }
                    items[_attributeModels[i]] = value;
                }
                _data.Add(items);
                line = file.ReadLine();
                count++;
            }

            _attributeModels.Add(_idAttributeModel);
        }

        private AttributeModel _idAttributeModel = null;
        public AttributeModel IdAttributeModel
        {
            get
            {
                return _idAttributeModel;
            }
        }

        private List<Dictionary<AttributeModel, object>> _data = new List<Dictionary<AttributeModel, object>>();
        public List<Dictionary<AttributeModel, object>> Data
        {
            get
            {
                return _data;
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
