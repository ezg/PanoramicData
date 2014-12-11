using System.Linq;
using PanoramicDataDBConnector;
using PanoramicDataModel;
using PanoramicData.model.view;

namespace PanoramicData.controller.data
{
    public class ModelHelpers
    {
        public static PathInfo GeneratePathInfo(string schema, params string[] tables)
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

        public static TableModel GenerateTableModel(PathInfo[] pathInfos, string[][] fields)
        {
            TableModel tableModel = new TableModel();
            for (int i = 0; i < pathInfos.Length; i++)
            {
                PathInfo pathInfo = pathInfos[i];
                for (int j = 0; j < fields[i].Length; j++)
                {
                    tableModel.AddColumnDescriptor(
                        new DatabaseColumnDescriptor(
                            DatabaseManager.GetFieldInfo(
                                pathInfo.TableInfo.SchemaName,
                                pathInfo.TableInfo.Name,
                                fields[i][j]), pathInfo));
                }
            }

            return tableModel;
        }

        public static FilterModel GenerateFilterModel(PathInfo[] pathInfos, string[][] fields)
        {
            FilterModel filterModel = new FilterModel();
            /*filterModel.JoinedTableInfo = new JoinedTableInfo();

            
            for (int i = 0; i < pathInfos.Length; i++)
            {
                PathInfo pathInfo = pathInfos[i];
                List<FieldInfo> fieldInfos = new List<FieldInfo>();
                for (int j = 0; j < fields[i].Length; j++)
                {
                    fieldInfos.Add(DatabaseManager.GetFieldInfo(
                        pathInfo.TableInfo.SchemaName,
                        pathInfo.TableInfo.Name,
                        fields[i][j]));
                }

                filterModel.JoinedTableInfo.Fields.Add(pathInfo, fieldInfos);
            }*/

            return filterModel;
        }

    }
}
