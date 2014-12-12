using System.Linq;
using System.Text.RegularExpressions;
using FarseerPhysics.Common;
using PanoramicDataModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;

namespace PanoramicData.model.view
{
    public class TableModel
    {
        public delegate void TableModelUpdatedHandler(object sender, TableModelUpdatedEventArgs e);
        public event TableModelUpdatedHandler TableModelUpdated;

        public TableModel()
        {
        }

        private Dictionary<PathInfo, List<DatabaseColumnDescriptor>> _columnDescriptors = new Dictionary<PathInfo, List<DatabaseColumnDescriptor>>();
        public Dictionary<PathInfo, List<DatabaseColumnDescriptor>> ColumnDescriptors
        {
            get
            {
                return _columnDescriptors;
            }
            set
            {
                _columnDescriptors = value;
            }
        }
        public void AddColumnDescriptor(PanoramicDataColumnDescriptor descriptor)
        {
            if (descriptor is DatabaseColumnDescriptor)
            {
                DatabaseColumnDescriptor databaseColumnDescriptor = descriptor as DatabaseColumnDescriptor;
                PathInfo pathInfo = (PathInfo) databaseColumnDescriptor.PanoramicDataGroupDescriptor;

                if (_columnDescriptors.ContainsKey(pathInfo))
                {
                    if (
                        _columnDescriptors[pathInfo].Count(
                            cd => cd.MatchSimple(descriptor)) == 0)
                    {
                        _columnDescriptors[pathInfo].Add(databaseColumnDescriptor);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    _columnDescriptors.Add(pathInfo,
                        new List<DatabaseColumnDescriptor>(new DatabaseColumnDescriptor[] { databaseColumnDescriptor }));
                }
                descriptor.PropertyChanged += descriptor_PropertyChanged;
                fireTableModelUpdated(UpdatedMode.Structure);
            }
        }
        public void RemoveColumnDescriptor(PanoramicDataColumnDescriptor descriptor)
        {

            if (descriptor is DatabaseColumnDescriptor)
            {
                DatabaseColumnDescriptor databaseColumnDescriptor = descriptor as DatabaseColumnDescriptor;
                PathInfo pathInfo = (PathInfo) databaseColumnDescriptor.PanoramicDataGroupDescriptor;

                if (_columnDescriptors.ContainsKey(pathInfo))
                {
                    _columnDescriptors[pathInfo].Remove(databaseColumnDescriptor);
                    descriptor.PropertyChanged -= descriptor_PropertyChanged;

                    if (_columnDescriptors[pathInfo].Count == 0)
                    {
                        _columnDescriptors.Remove(pathInfo);
                    }
                    fireTableModelUpdated(UpdatedMode.Structure);
                }
            }
        }

        private Dictionary<CalculatedColumnDescriptorInfo, string> _calculatedColumnDescriptorInfos = new Dictionary<CalculatedColumnDescriptorInfo, string>();
        public Dictionary<CalculatedColumnDescriptorInfo, string> CalculatedColumnDescriptorInfos
        {
            get
            {
                return _calculatedColumnDescriptorInfos;
            }
            set
            {
                _calculatedColumnDescriptorInfos = value;
            }
        }
        public void UpdateCalculatedColumnDescriptorInfo(CalculatedColumnDescriptorInfo calculatedColumnDescriptorInfo)
        {
            if (_calculatedColumnDescriptorInfos.ContainsKey(calculatedColumnDescriptorInfo))
            {
                if (calculatedColumnDescriptorInfo.Name == "")
                {
                    //_namedFilterModels.Remove(filterModel);
                }
                else
                {
                    _calculatedColumnDescriptorInfos[calculatedColumnDescriptorInfo] = calculatedColumnDescriptorInfo.Name;
                }
            }
            else
            {
                _calculatedColumnDescriptorInfos.Add(calculatedColumnDescriptorInfo, calculatedColumnDescriptorInfo.Name);
            }
            fireTableModelUpdated(UpdatedMode.Database);
        }
        public void RemoveCalculatedColumnDescriptorInfo(CalculatedColumnDescriptorInfo calculatedColumnDescriptorInfo)
        {
            if (_calculatedColumnDescriptorInfos.ContainsKey(calculatedColumnDescriptorInfo))
            {
                _calculatedColumnDescriptorInfos.Remove(calculatedColumnDescriptorInfo);
                fireTableModelUpdated(UpdatedMode.Database);
            }
        }

        private Dictionary<FilterModel, string> _namedFilterModels = new Dictionary<FilterModel, string>();
        public Dictionary<FilterModel, string> NamedFilterModels
        {
            get
            {
                return _namedFilterModels;
            }
            set
            {
                _namedFilterModels = value;
            }
        }
        public void UpdateNamedFilterModel(FilterModel filterModel)
        {
            if (_namedFilterModels.ContainsKey(filterModel))
            {
                if (filterModel.Name == "")
                {
                    //_namedFilterModels.Remove(filterModel);
                }
                else
                {
                    _namedFilterModels[filterModel] = filterModel.Name;
                }
            }
            else
            {
                _namedFilterModels.Add(filterModel, filterModel.Name);
            }
            fireTableModelUpdated(UpdatedMode.Database);
        }
        public void RemoveNamedFilterModel(FilterModel filterModel)
        {
            if (_namedFilterModels.ContainsKey(filterModel))
            {
                _namedFilterModels.Remove(filterModel);
                fireTableModelUpdated(UpdatedMode.Database);
            }
        }

        public void Clear()
        {
            _namedFilterModels.Clear();
            _calculatedColumnDescriptorInfos.Clear();
            fireTableModelUpdated(UpdatedMode.Database);
        }

        public List<PathInfo> CalculateRecursivePathInfos()
        {
            List<PathInfo> pathInfos = new List<PathInfo>();
             PanoramicDataGroupDescriptor groupDescriptor = this.ColumnDescriptors.Keys.First();
            if (groupDescriptor is PathInfo)
            {
                PathInfo pi = groupDescriptor as PathInfo;
                TableInfo root = pi.TableInfo;
                if (pi.Path.Count > 0)
                {
                    root = pi.Path.First().FromTableInfo;
                }
                recursivePathInfos(new PathInfo(root), null, pathInfos);
            }
            return pathInfos;
        }

        private void recursivePathInfos(PathInfo currentPathInfo, PathInfo parentPathInfo, List<PathInfo> pathInfos)
        {
            pathInfos.Add(currentPathInfo);
            foreach (var dep in currentPathInfo.TableInfo.FromTableDependencies)
            {
                if (parentPathInfo == null || dep.ToTableInfo != parentPathInfo.TableInfo)
                {
                    recursivePathInfos(new PathInfo(currentPathInfo, dep), currentPathInfo, pathInfos);
                }
            }
        }

        void descriptor_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            fireTableModelUpdated(UpdatedMode.Structure);
        }
        protected void fireTableModelUpdated(UpdatedMode mode)
        {
            if (TableModelUpdated != null)
            {
                TableModelUpdated(this, new TableModelUpdatedEventArgs(mode));
            }
        }
    }

    public enum FilterMode { None, And, Or }

    public class TableModelUpdatedEventArgs : EventArgs
    {
        public UpdatedMode Mode { get; set; }
        public TableModelUpdatedEventArgs(UpdatedMode mode)
            : base()
        {
            Mode = mode;
        }
    }
}
