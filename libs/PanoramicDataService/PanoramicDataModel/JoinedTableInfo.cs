using System.Data.Entity;
using starPadSDK.Inq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicDataModel
{
    public enum UpdatedMode { UI, Structure, Incoming, FilteredItemsChange, FilteredItemsStatus, Database }
    public enum SubUpdatedMode { Color, None, RenderStyle }

    public class PanoramicDataColumnDescriptorUpdatedEventArgs : EventArgs
    {
        public object Sender { get; set; }
        public UpdatedMode Mode { get; set; }
        public SubUpdatedMode SubMode { get; set; }

        public PanoramicDataColumnDescriptorUpdatedEventArgs(UpdatedMode mode, SubUpdatedMode subMode = SubUpdatedMode.None, object sender = null)
        {
            Mode = mode;
            SubMode = subMode;
            Sender = sender;
        }
    }

    public abstract class PanoramicDataGroupDescriptor
    {
        public abstract string GetLabel();
    }

    public abstract class PanoramicDataColumnDescriptor : ViewModelBase, ICloneable
    {
        public delegate void PanoramicDataColumnDescriptorUpdatedHandler(object sender, PanoramicDataColumnDescriptorUpdatedEventArgs e);
        public event PanoramicDataColumnDescriptorUpdatedHandler PanoramicDataColumnDescriptorUpdated;

        protected int _order = int.MaxValue;
        public int Order
        {
            get
            {
                return _order;
            }
            set
            {
                if (_order != value)
                {
                    _order = value;
                    OnPropertyChanged("Order");
                }
            }
        }

        public bool IsAnyGroupingOperationApplied()
        {
            return this.IsGrouped || this.IsBinned || this.IsTiled;
        }

        protected bool _isBinned = false;
        public bool IsBinned
        {
            get
            {
                return _isBinned;
            }
            set
            {
                if (_isBinned != value)
                {
                    _isBinned = value;
                    if (_isBinned)
                    {
                        _isTiled = false;
                        _isGrouped = false;
                    }
                    else if (_aggregateFunction == AggregateFunction.Bin)
                    {
                        _aggregateFunction = AggregateFunction.None;
                    }
                    OnPropertyChanged("IsBinned");
                }
            }
        }

        protected double _binUpperBound = 0;
        public double BinUpperBound
        {
            get
            {
                return _binUpperBound;
            }
            set
            {
                if (_binUpperBound != value)
                {
                    _binUpperBound = value;
                    OnPropertyChanged("BinUpperBound");
                }
            }
        }

        protected double _binLowerBound = 0;
        public double BinLowerBound
        {
            get
            {
                return _binLowerBound;
            }
            set
            {
                if (_binLowerBound != value)
                {
                    _binLowerBound = value;
                    OnPropertyChanged("BinLowerBound");
                }
            }
        }

        protected bool _binSizeSetByUser = false;
        protected double _binSize = 1.0;
        public double BinSize
        {
            get
            {
                if (_binSizeSetByUser)
                {
                    return _binSize;
                }
                else
                {
                    double? binSize = ResourceManager.GetDouble(ExternalQualifier);
                    if (binSize.HasValue)
                    {
                        return (double)binSize.Value;
                    }
                    else
                    {
                        return _binSize;
                    }
                }
            }
            set
            {
                if (_binSize != value)
                {
                    if (value != 0)
                    {
                        _binSize = value;
                    }
                    else
                    {
                        _binSize = 1;
                    }
                    _binSizeSetByUser = true;
                    OnPropertyChanged("BinSize");

                    ResourceManager.Add(ExternalQualifier, _binSize.ToString());
                }
            }
        }

        protected bool _isTiled = false;
        public bool IsTiled
        {
            get
            {
                return _isTiled;
            }
            set
            {
                if (_isTiled != value)
                {
                    _isTiled = value;
                    if (_isTiled)
                    {
                        _isBinned = false;
                        _isGrouped = false;
                    }
                    OnPropertyChanged("IsTiled");
                }
            }
        }

        protected int _numberOfTiles = 5;
        public int NumberOfTiles
        {
            get
            {
                return _numberOfTiles;
            }
            set
            {
                if (_numberOfTiles != value)
                {
                    _numberOfTiles = value;
                    OnPropertyChanged("NumberOfTiles");
                }
            }
        }

        protected bool _isGrouped = false;
        public bool IsGrouped
        {
            get
            {
                return _isGrouped;
            }
            set
            {
                if (_isGrouped != value)
                {
                    _isGrouped = value;
                    if (_isGrouped)
                    {
                        _isBinned = false;
                        _isTiled = false;
                    }
                    OnPropertyChanged("IsGrouped");
                }
            }
        }

        protected bool _isHidden = false;
        public bool IsHidden
        {
            get
            {
                return _isHidden;
            }
            set
            {
                if (_isHidden != value)
                {
                    _isHidden = value;
                    OnPropertyChanged("IsHidden");
                }
            }
        }

        protected bool _isVisualization = false;
        public bool IsVisualization
        {
            get
            {
                return _isVisualization;
            }
            set
            {
                if (_isVisualization != value)
                {
                    _isVisualization = value;
                    OnPropertyChanged("IsVisualization");
                }
            }
        }

        protected SortMode _sortMode = SortMode.None;
        public SortMode SortMode
        {
            get
            {
                return _sortMode;
            }
            set
            {
                if (_sortMode != value)
                {
                    _sortMode = value;
                    OnPropertyChanged("SortMode");
                }
            }
        }

        protected StroqCollection _filterStroqs = null;
        public StroqCollection FilterStroqs
        {
            get
            {
                return _filterStroqs;
            }
            set
            {
                if (_filterStroqs != value)
                {
                    _filterStroqs = value;
                    OnPropertyChanged("FilterStroqs");
                }
            }
        }

        public bool AggregateFunctionSetByUser { get; set; }

        protected AggregateFunction _aggregateFunction = AggregateFunction.Concat;
        public AggregateFunction AggregateFunction
        {
            get
            {
                return _aggregateFunction;
            }
            set
            {
                if (_aggregateFunction != value)
                {
                    _aggregateFunction = value;
                    AggregateFunctionSetByUser = true;
                    OnPropertyChanged("AggregateFunction");
                }
            }
        }

        protected ScaleFunction _scaleFunction = ScaleFunction.None;
        public ScaleFunction ScaleFunction
        {
            get
            {
                return _scaleFunction;
            }
            set
            {
                if (_scaleFunction != value)
                {
                    _scaleFunction = value;
                    OnPropertyChanged("ScaleFunction");
                }
            }
        }

        protected abstract string ExternalQualifier { get; }

        public abstract string DataType { get; }
        
        public abstract string VisualizationType { get; }

        public abstract string Name { get; }

        public abstract Nullable<double> MaxValue { get; }

        public abstract Nullable<double> MinValue { get; }

        public abstract bool IsPrimaryKey { get; set; }

        public abstract PanoramicDataGroupDescriptor PanoramicDataGroupDescriptor { get; }

        public string GetSQLSelect(QueryColumnQualifier qualifier, bool applyGrouping, 
            bool applyAggregation, bool sort = false, string specialName = null)
        {
            string name = qualifier.TableQualifier + "." + qualifier.FieldQualifier;
            if (specialName != null)
            {
                name = specialName;
            }

            if ((!applyGrouping && !applyAggregation) || (IsGrouped && AggregateFunction == AggregateFunction.None) || (IsBinned && AggregateFunction == AggregateFunction.None))
            {
                return name;
            }
            else
            {
                string sqlFunction = "";
                if (_aggregateFunction == AggregateFunction.Avg)
                {
                    sqlFunction = "avg";
                    if (DataType == AttributeDataTypeConstants.TIME)
                    {
                        sqlFunction = "cast(dateadd(ss, avg(datediff(ss, 0, " + name + ")), 0) as time)";
                        return sqlFunction;
                    } 
                    if (DataType == AttributeDataTypeConstants.BIT || 
                        DataType == AttributeDataTypeConstants.INT)
                    {
                        sqlFunction = "avg(cast(" + name + " as float))";
                        return sqlFunction;
                    }
                }
                else if (_aggregateFunction == AggregateFunction.Min)
                {
                    sqlFunction = "min";
                }
                else if (_aggregateFunction == AggregateFunction.Max)
                {
                    sqlFunction = "max";
                }
                else if (_aggregateFunction == AggregateFunction.Sum)
                {
                    sqlFunction = "sum";
                }
                else if (_aggregateFunction == AggregateFunction.Count)
                {
                    sqlFunction = "count";
                }
                else if (_aggregateFunction == AggregateFunction.Concat)
                {
                    sqlFunction = "dbo.group_concat";
                }
                else if (_aggregateFunction == AggregateFunction.Bin)
                {
                    //(cast(t.bin_8 as nvarchar) + ' - ' + cast((t.bin_8 + 5) as nvarchar))
                    /*if (!sort)
                    {
                        //binQualifier.TableQualifier + "." + binQualifier.FieldQualifier;
                        sqlFunction = "(cast(" + name + " as nvarchar) + '/' + cast((" + name + " + " +
                                      this.BinSize +
                                      ") as nvarchar))";
                        return sqlFunction;
                    }
                    else*/
                    {
                        return name;
                    }
                }
                else /*if (_aggregateFunction == AggregateFunction.None)*/
                {
                    //sqlFunction = "dbo.group_concat";
                    if (DataType == AttributeDataTypeConstants.BIT)
                    {
                        sqlFunction = "max(cast(" + name + " as float))";
                        return sqlFunction;
                    }
                    sqlFunction = "max";
                }
                return sqlFunction + "(" + name + ")";
            }
        }

        public string LabelFromBinAggregate(string value)
        {
            return string.Join(" - ", value.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries));
        }

        public abstract string GetSimpleLabel();

        public abstract void GetLabels(out string mainLabel, out string subLabel, bool addDetails = true);

        public string GetCombinedLabel(string separator = "\n")
        {
            string main, sub = "";
            GetLabels(out main, out sub);
            return main + separator + sub;
        }

        protected string AddDetailToLabel(string name)
        {
            if (this.AggregateFunction == AggregateFunction.Avg)
            {
                name = "Avg(" + name + ")";
            }
            else if (this.AggregateFunction == AggregateFunction.Concat)
            {
                name = "Concat(" + name + ")";
            }
            else if (this.AggregateFunction == AggregateFunction.Concat)
            {
                name = "Concat(" + name + ")";
            }
            else if (this.AggregateFunction == AggregateFunction.Count)
            {
                name = "Count(" + name + ")";
            }
            else if (this.AggregateFunction == AggregateFunction.Max)
            {
                name = "Max(" + name + ")";
            }
            else if (this.AggregateFunction == AggregateFunction.Min)
            {
                name = "Min(" + name + ")";
            }
            else if (this.AggregateFunction == AggregateFunction.Sum)
            {
                name = "Sum(" + name + ")";
            }
            else if (this.AggregateFunction == AggregateFunction.Vis)
            {
                name = "Vis(" + name + ")";
            }
            else if (this.AggregateFunction == AggregateFunction.Bin)
            {
                name = "Bin Range(" + name + ")";
            }

            if (this.ScaleFunction != ScaleFunction.None)
            {
                if (this.ScaleFunction == ScaleFunction.Log)
                {
                    name += " [Log]";
                }
                else if (this.ScaleFunction == ScaleFunction.Normalize)
                {
                    name += " [Normalize]";
                }
                else if (this.ScaleFunction == ScaleFunction.RunningTotal)
                {
                    name += " [RT]";
                }
                else if (this.ScaleFunction == ScaleFunction.RunningTotalNormalized)
                {
                    name += " [RT Norm]";
                }
            }
            return name;
        }

        public abstract bool MatchSimple(PanoramicDataColumnDescriptor cd);

        public abstract bool MatchComplete(PanoramicDataColumnDescriptor cd);

        public abstract object Clone();

        public abstract PanoramicDataColumnDescriptor SimpleClone();

        public void FirePanoramicDataColumnDescriptorUpdated(UpdatedMode mode, SubUpdatedMode subMode = SubUpdatedMode.None)
        {
            if (PanoramicDataColumnDescriptorUpdated != null)
            {
                PanoramicDataColumnDescriptorUpdated(this,
                    new PanoramicDataColumnDescriptorUpdatedEventArgs(mode, subMode, this));
            }
        }
    }

    public class DatabaseColumnDescriptor : PanoramicDataColumnDescriptor
    {
        public DatabaseColumnDescriptor(FieldInfo fieldInfo, PathInfo pathInfo, bool isPrimaryKey)
        {
            _fieldInfo = fieldInfo;
            _pathInfo = pathInfo;
            _isPrimaryKey = isPrimaryKey;
            _sortMode = SortMode.None;
            _aggregateFunction = AggregateFunction.None;

            _binUpperBound = _fieldInfo.MaxValue.HasValue ? _fieldInfo.MaxValue.Value : 100;
            _binLowerBound = _fieldInfo.MinValue.HasValue ? _fieldInfo.MinValue.Value : 100;
        }

        public DatabaseColumnDescriptor(FieldInfo fieldInfo, PathInfo pathInfo) :
            this(fieldInfo, pathInfo, fieldInfo.PrimaryKeyTableInfos.Count != 0) { }

        private FieldInfo _fieldInfo = null;
        public FieldInfo FieldInfo
        {
            get
            {
                return _fieldInfo;
            }
            set
            {
                if (_fieldInfo != value)
                {
                    _fieldInfo = value;
                    OnPropertyChanged("FieldInfo");
                }
            }
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
                if (_pathInfo != value)
                {
                    _pathInfo = value;
                    OnPropertyChanged("PathInfo");
                }
            }
        }

        private bool _isPrimaryKey = false;
        public override bool IsPrimaryKey
        {
            get
            {
                return _isPrimaryKey;
            }
            set
            {
                if (_isPrimaryKey != value)
                {
                    _isPrimaryKey = value;
                    OnPropertyChanged("IsPrimaryKey");
                }
            }
        }

        protected override string ExternalQualifier
        {
            get
            {
                return
                    PathInfo.TableInfo.SchemaName + "." +
                    PathInfo.TableInfo.Name + "." +
                    FieldInfo.Name;
            }
        }

        public override string DataType
        {
            get
            {
                return _fieldInfo.DataType;
            }
        }

        public override string VisualizationType
        {
            get
            {
                return _fieldInfo.VisualizationType;
            }
        }

        public override string Name
        {
            get
            {
                return _fieldInfo.Name;
            }
        }

        public override Nullable<double> MaxValue
        {
            get
            {
                return _fieldInfo.MaxValue;
            }
        }

        public override Nullable<double> MinValue
        {
            get
            {
                return _fieldInfo.MinValue;
            }
        }

        public override PanoramicDataGroupDescriptor PanoramicDataGroupDescriptor
        {
            get
            {
                return PathInfo;
            }
        }

        public string SchemaName
        {
            get
            {
                return _fieldInfo.TableInfo.SchemaName;
            }
        }

        public override string GetSimpleLabel()
        {
            string mainLabel = this.Name;
            if (this.FieldInfo.FieldAliases.Count > 0)
            {
                mainLabel = this.FieldInfo.FieldAliases.First().Alias;
            }

            mainLabel = AddDetailToLabel(mainLabel);

            if (IsPrimaryKey)
            {
                if (this.PathInfo.Path.Count > 0 && this.PathInfo.Path.Last().RelationshipName != "")
                {
                    mainLabel = this.PathInfo.Path.Last().RelationshipName;
                }
            }

            return mainLabel;
        }

        public override void GetLabels(out string mainLabel, out string subLabel, bool addDetails = true)
        {
            mainLabel = this.Name;
            if (this.FieldInfo.FieldAliases.Count > 0)
            {
                mainLabel = this.FieldInfo.FieldAliases.First().Alias;
            }
            subLabel = "";

            if (addDetails)
            {
                if (this.IsGrouped)
                {
                    mainLabel = "[" + mainLabel + "]";
                }
                else if (this.IsTiled)
                {
                    mainLabel = "[" + mainLabel + "] / " + this.NumberOfTiles;
                }
                else if (this.IsBinned)
                {
                    mainLabel = "[" + mainLabel + "] / [" + this.BinSize + "]";
                }
                else
                {
                    mainLabel = AddDetailToLabel(mainLabel);
                }
            }

            subLabel = this.PathInfo.TableInfo.Name;
            if (this.PathInfo.TableInfo.TableAliases.Count > 0)
            {
                subLabel = this.PathInfo.TableInfo.TableAliases.First().Alias;
            }

            if (this.PathInfo.Path.Count > 0 && this.PathInfo.Path.Last().RelationshipName != "")
            {
                subLabel = this.PathInfo.Path.Last().RelationshipName;
            }
        }

        public override bool MatchSimple(PanoramicDataColumnDescriptor cd)
        {
            if (cd is DatabaseColumnDescriptor)
            {
                var fc = cd as DatabaseColumnDescriptor;
                return matchSimple(fc.FieldInfo, fc.PathInfo);
            }
            else
            {
                return false;
            }
        }

        private bool matchSimple(FieldInfo fieldInfo, PathInfo pathInfo)
        {
            return this.FieldInfo == fieldInfo && this.PanoramicDataGroupDescriptor.Equals(pathInfo);
        }

        public override bool MatchComplete(PanoramicDataColumnDescriptor cd)
        {
            if (cd is DatabaseColumnDescriptor)
            {
                var fc = cd as DatabaseColumnDescriptor;
                return matchComplete(fc._fieldInfo, fc._pathInfo, fc._aggregateFunction, fc._sortMode,
                    fc._isGrouped, fc._isBinned, fc._binSize, fc._isTiled);
            }
            else
            {
                return false;
            }
        }

        private bool matchComplete(FieldInfo fieldInfo, PathInfo pathInfo, AggregateFunction aggregateFunction,
            SortMode sortMode, bool isGrouped, bool isBinned, double binSize, bool isTiled)
        {
            return
                this._fieldInfo == fieldInfo &&
                this._pathInfo.Equals(pathInfo) &&
                this._aggregateFunction == aggregateFunction &&
                this._sortMode == sortMode &&
                this._isGrouped == isGrouped &&
                this._isBinned == isBinned &&
                this._binSize == binSize &&
                this._isTiled == isTiled;
        }

        public override bool Equals(object obj)
        {
            if (obj is DatabaseColumnDescriptor)
            {
                var fc = obj as DatabaseColumnDescriptor;
                return matchComplete(fc._fieldInfo, fc._pathInfo, fc._aggregateFunction, fc._sortMode,
                    fc._isGrouped, fc._isBinned, fc._binSize, fc._isTiled);
            }
            return false;
        }

        public override int GetHashCode()
        {
            int code = 0;
            code ^= this.FieldInfo.GetHashCode();
            code ^= this.PanoramicDataGroupDescriptor.GetHashCode();
            return code;
        }

        public override object Clone()
        {
            PanoramicDataColumnDescriptor cd = new DatabaseColumnDescriptor(this.FieldInfo, this.PathInfo, this.IsPrimaryKey);
            cd.IsGrouped = this.IsGrouped;
            cd.IsBinned = this.IsBinned;
            cd.IsTiled = this.IsTiled;
            //cd.BinSize = this.BinSize;
            cd.BinLowerBound = this.BinLowerBound;
            cd.BinUpperBound = this.BinUpperBound;
            cd.NumberOfTiles = this.NumberOfTiles;
            cd.SortMode = this.SortMode;
            cd.AggregateFunction = this.AggregateFunction;
            cd.Order = this.Order;

            return cd;
        }

        public override PanoramicDataColumnDescriptor SimpleClone()
        {
            PanoramicDataColumnDescriptor cd = new DatabaseColumnDescriptor(this.FieldInfo, this.PathInfo, this.IsPrimaryKey);
            return cd;
        }
    }

    public class QueryColumnQualifier
    {
        public int Level { get; set; }
        public String FieldQualifier { get; set; }
        public String TableQualifier { get; set; }
        public PanoramicDataColumnDescriptor ColumnDescriptor { get; set; }

        public QueryColumnQualifier(String fieldQualifier, String tableQualifier, PanoramicDataColumnDescriptor columnDescriptor, int level = 0)
        {
            FieldQualifier = fieldQualifier;
            TableQualifier = tableQualifier;
            ColumnDescriptor = columnDescriptor;
            Level = level;
        }

        public override bool Equals(object obj)
        {
            if (obj is QueryColumnQualifier)
            {
                var fc = obj as QueryColumnQualifier;
                return this.ColumnDescriptor.Equals(fc.ColumnDescriptor);
            }
            return false;
        }

        public override int GetHashCode()
        {
            int code = 0;
            code ^= this.ColumnDescriptor.GetHashCode();
            return code;
        }
    }

    public enum SortMode { Asc, Desc, None }

    public enum AggregateFunction { None, Sum, Count, Min, Max, Avg, Concat, Vis, Bin };

    public enum ScaleFunction { None, Log, Normalize, RunningTotal, RunningTotalNormalized };
}
