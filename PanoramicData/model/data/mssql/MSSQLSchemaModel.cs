using PanoramicData.controller.data;
using PanoramicData.controller.input;
using PanoramicDataDBConnector;
using PanoramicDataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.data.mssql
{
    public class MSSQLSchemaModel : SchemaModel
    {
        private PathInfo _rootPathInfo = null;
        private OriginModel _rootOriginModel = null;

        public MSSQLSchemaModel(DatasetConfiguration datasetConfiguration) 
        {
            _rootPathInfo = generatePathInfo(datasetConfiguration.Schema, datasetConfiguration.Table);
            _rootOriginModel = new MSSQLOriginModel(_rootPathInfo);
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

        private PathInfo generatePathInfo(string schema, params string[] tables)
        {
            PathInfo pathInfo = null;

            TableInfo prev = null;
            foreach (var table in tables)
            {
                if (prev == null)
                {
                    prev = DatabaseManager.GetTableInfo(schema, table);
                    pathInfo = new PathInfo(prev);
                }
                else
                {
                    foreach (var dep in prev.FromTableDependencies)
                    {
                        if (dep.ToTableInfo.Name == table)
                        {
                            prev = DatabaseManager.GetTableInfo(schema, table);
                            pathInfo = new PathInfo(pathInfo, dep);
                            break;
                        }
                    }
                }
            }
            return pathInfo;
        }
    }
}
