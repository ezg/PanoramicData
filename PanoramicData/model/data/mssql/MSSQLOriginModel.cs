using PanoramicData.controller.input;
using PanoramicDataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.model.data.mssql
{
    public class MSSQLOriginModel : OriginModel
    {
        public MSSQLOriginModel(PathInfo pathInfo)
        {
            _pathInfo = pathInfo;
        }

        private PathInfo _pathInfo = null;
        public PathInfo PathInfo
        {
            get
            {
                return _pathInfo;
            }
            set
            {
                this.SetProperty(ref _pathInfo, value);
            }
        }

        public override string Name
        {
            get 
            {
                return _pathInfo.GetLabel();
            }
        }

        public override List<AttributeModel> AttributeModels
        {
            get
            {
                List<AttributeModel> childAttributeModels = new List<AttributeModel>();
                foreach (var field in _pathInfo.TableInfo.FieldInfos)
                {
                    if (_pathInfo.TableInfo.PrimaryKeyFieldInfo == field || !field.Visible)
                    {
                        continue;
                    }
                    AttributeModel childAttributeModel = new MSSQLAttributeModel(this, field);
                    childAttributeModels.Add(childAttributeModel);
                }
                return childAttributeModels.OrderBy(item => item.Name).ToList();
            }
        }

        public override List<OriginModel> OriginModels
        {
            get
            {
                List<OriginModel> childOriginModels = new List<OriginModel>();
                foreach (var dep in _pathInfo.TableInfo.FromTableDependencies)
                {
                    if (!_pathInfo.Path.Contains(dep))
                    {
                        if (_pathInfo.Path.Count == 0 ||
                            (_pathInfo.Path.Last().ToTableInfo != dep.FromTableInfo &&
                             _pathInfo.Path.Last().FromTableInfo != dep.ToTableInfo))
                        {
                            OriginModel childOriginModel = new MSSQLOriginModel(new PathInfo(_pathInfo as PathInfo, dep));
                            childOriginModel.SchemaModel = this.SchemaModel;
                            childOriginModels.Add(childOriginModel);
                        }
                    }
                }
                return childOriginModels.OrderBy(item => item.Name).ToList();
            }
        }
    }
}
