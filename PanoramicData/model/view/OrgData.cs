using System.Collections.Generic;
using System.Linq;
using PixelLab.Common;
using starPadSDK.Inq;
using PanoramicDataModel;

namespace PanoramicData.model.view

{
    public abstract class OrgItem : Changeable
    {
        private bool _isVisible;
        private bool _isExpanded;
        private bool _isSelected;
        private OrgItem _parent;

        protected OrgItem(object data, string name)
        {
            Data = data;
            Name = name;
        }

        public string Name { get; set; }

        public object Data { get; private set; }

        public bool IsVisible
        {
            get { return _isVisible; }
            set
            {
                UpdateProperty("IsVisible", ref _isVisible, value);
            }
        }

        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                UpdateProperty("IsExpanded", ref _isExpanded, value);
            }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                UpdateProperty("IsSelected", ref _isSelected, value);
            }
        }

        public OrgItem Parent
        {
            get { return _parent; }
            set { _parent = value; }
        }

        public virtual IEnumerable<OrgItem> Children
        {
            get { return new List<OrgItem>(); }
        }
    }

    public class CustomFieldsRootOrgItem : OrgItem
    {
        private IEnumerable<OrgItem> _children = null;
        private TableModel _tableModel = null;

        public CustomFieldsRootOrgItem(TableModel tableModel, string name)
            : base(tableModel, name)
        {
            _tableModel = tableModel;
        }

        public override IEnumerable<OrgItem> Children
        {
            get
            {
                if (_children == null)
                {
                    _children = ConstructChildren();
                }
                return _children;
            }
        }

        private IEnumerable<OrgItem> ConstructChildren()
        {
            List<OrgItem> children = new List<OrgItem>();
            foreach (var key in _tableModel.NamedFilterModels.Keys)
            {
                OrgItem oi = new CustomFieldOrgItem(key, _tableModel.NamedFilterModels[key]);
                children.Add(oi);
            }
            return children;
        }
    }

    public class CaclculatedFieldsRootOrgItem : OrgItem
    {
        private IEnumerable<OrgItem> _children = null;
        private TableModel _tableModel = null;

        public CaclculatedFieldsRootOrgItem(TableModel tableModel, string name)
            : base(tableModel, name)
        {
            _tableModel = tableModel;
        }

        public override IEnumerable<OrgItem> Children
        {
            get
            {
                if (_children == null)
                {
                    _children = ConstructChildren();
                }
                return _children;
            }
        }

        private IEnumerable<OrgItem> ConstructChildren()
        {
            List<OrgItem> children = new List<OrgItem>();
            foreach (var key in _tableModel.CalculatedColumnDescriptorInfos.Keys)
            {
                OrgItem oi = new CalculatedFieldOrgItem(key, _tableModel.CalculatedColumnDescriptorInfos[key]);
                children.Add(oi);
            }
            return children;
        }
    }

    public class CustomFieldOrgItem : OrgItem
    {
        public CustomFieldOrgItem(FilterModel filterModel, string name)
            : base(filterModel, name)
        {
        }
    }

    public class CalculatedFieldOrgItem : OrgItem
    {
        public CalculatedFieldOrgItem(CalculatedColumnDescriptorInfo calculatedColumnDescriptorInfo, string name)
            : base(calculatedColumnDescriptorInfo, name)
        {
        }
    }


    public class DatabaseRootOrgItem : OrgItem
    {
        private IEnumerable<OrgItem> _children = null;
        private FilterModel _filterModel = null;

        public DatabaseRootOrgItem(FilterModel filterModel, string name)
            : base(filterModel, name)
        {
            _filterModel = filterModel;
        }

        public override IEnumerable<OrgItem> Children
        {
            get {
                if (_children == null)
                {
                    _children = ConstructChildren();
                }
                return _children;
            }
        }

        private IEnumerable<OrgItem> ConstructChildren()
        {
            List<OrgItem> children = new List<OrgItem>();
            PanoramicDataGroupDescriptor groupDescriptor = _filterModel.TableModel.ColumnDescriptors.Keys.First();
            OrgItem oi = new TableInfoOrgItem(groupDescriptor, _filterModel, _filterModel.TableModel);
            children.Add(oi);
            return children;
        }
    }

    public class TableInfoOrgItem : OrgItem
    {
        private IEnumerable<OrgItem> _children = null;
        private FilterModel _filterModel = null;
        private TableModel _tableModel = null;

        public TableInfoOrgItem(PanoramicDataGroupDescriptor groupDescriptor, FilterModel filterModel, TableModel tableModel)
            : base(groupDescriptor, groupDescriptor.GetLabel())
        {
            _filterModel = filterModel;
            _tableModel = tableModel;
        }

        public override IEnumerable<OrgItem> Children
        {
            get {
                if (_children == null)
                {
                    _children = ConstructChildren(Data as PanoramicDataGroupDescriptor);
                }
                return _children;
            }
        }

        private IEnumerable<OrgItem> ConstructChildren(PanoramicDataGroupDescriptor groupDescriptor)
        {
            List<OrgItem> children = new List<OrgItem>();

            if (groupDescriptor is PathInfo)
            {
                PathInfo pathInfo = groupDescriptor as PathInfo;
                foreach (var field in pathInfo.TableInfo.FieldInfos)
                {
                    if (pathInfo.TableInfo.PrimaryKeyFieldInfo == field ||
                        !field.Visible)
                    {
                        continue;
                    }
                    OrgItem oi = new FieldInfoOrgItem(field);
                    oi.Parent = this;
                    children.Add(oi);

                    PanoramicDataColumnDescriptor cd = new DatabaseColumnDescriptor(field, pathInfo,
                        field.PrimaryKeyTableInfos.Count != 0);
                    if (_filterModel.ColumnDescriptors.Contains(cd))
                    {
                        oi.IsSelected = true;
                    }
                }
                children = children.OrderBy(item => item.Name).ToList();

                List<OrgItem> tempChildren = new List<OrgItem>();
                foreach (var dep in pathInfo.TableInfo.FromTableDependencies)
                {
                    if (!pathInfo.Path.Contains(dep))
                    {
                        if (pathInfo.Path.Count == 0 ||
                            (pathInfo.Path.Last().ToTableInfo != dep.FromTableInfo &&
                             pathInfo.Path.Last().FromTableInfo != dep.ToTableInfo))
                        {
                            OrgItem oi = new TableInfoOrgItem(new PathInfo(Data as PathInfo, dep), _filterModel,
                                _tableModel);
                            oi.Parent = this;
                            tempChildren.Add(oi);
                        }
                    }
                }
                tempChildren = tempChildren.OrderBy(item => item.Name).ToList();

                children = children.Union(tempChildren).ToList();
            }
            return children;
        }
    }

    public class FieldInfoOrgItem : OrgItem
    {
        public FieldInfoOrgItem(FieldInfo fieldInfo)
            : base(fieldInfo, fieldInfo.Name)
        {
        }
    }
}
