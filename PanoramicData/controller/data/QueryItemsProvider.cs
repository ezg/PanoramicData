using PanoramicDataDBConnector;
using PanoramicDataModel;
using System.Collections.Generic;
using System.ComponentModel;
using PanoramicData.model.view;

namespace PanoramicData.controller.data
{
    public class QueryItemsProvider : IItemsProvider<PanoramicDataRow>
    {
        private QueryGenerator _queryGenerator = null;

        private int _fetchCount = -1;

        public QueryItemsProvider(QueryGenerator queryGenerator)
        {
            _queryGenerator = queryGenerator;
        }

        /// <summary>
        /// Fetches the total number of items available.
        /// </summary>
        /// <returns></returns>
        public int FetchCount()
        {
            //Trace.WriteLine("FetchCount");

            string query = _queryGenerator.GenerateCountQuery();

            List<List<object>> result = DatabaseManager.ExecuteQuery(_queryGenerator.GetSchemaName(), query);
            if (result.Count == 0)
            {
                _fetchCount = 0;
            }
            else
            {
                _fetchCount = (int)result[0][0];
            }

            return _fetchCount;
        }

        /// <summary>
        /// Fetches a range of items.
        /// </summary>
        /// <param name="startIndex">The start index.</param>
        /// <param name="count">The number of items to fetch.</param>
        /// <returns></returns>
        public IList<PanoramicDataRow> FetchRange(int startIndex, int pageCount, out int overallCount)
        {
            //Trace.WriteLine("FetchRange: " + startIndex + "," + pageCount);

            // generate query
            List<PanoramicDataColumnDescriptor> relevantDescriptors = new List<PanoramicDataColumnDescriptor>();
            List<FilterModelColumnDescriptorPair> passFlagDescriptors = new List<FilterModelColumnDescriptorPair>();
            List<QueryColumnQualifier> queryColumnQualifiers = new List<QueryColumnQualifier>();
            Dictionary<string, PathInfo> relevantTables = new Dictionary<string, PathInfo>();
            string query = _queryGenerator.GenerateFetchQuery(true, startIndex + 1, pageCount, false, true, out relevantDescriptors, out passFlagDescriptors, out queryColumnQualifiers);

            // execute query
            List<List<object>> result = DatabaseManager.ExecuteQuery(_queryGenerator.GetSchemaName(), query);
            
            overallCount = _fetchCount;

            List<PanoramicDataRow> list = new List<PanoramicDataRow>();
            int rowCount = 0;
            foreach (var row in result)
            {
                PanoramicDataRow panoramicDataRow = _queryGenerator.ConvertToPanoramicDataRow(row, relevantDescriptors, passFlagDescriptors);
                _queryGenerator.PostProcessRow(panoramicDataRow);
                list.Add(panoramicDataRow);
                rowCount++;
            }
            return list;
        }

    }

    public class PanoramicDataValue
    {
        public string DataType { get; set; }
        public object Value { get; set; }
        public string StringValue { get; set; }
        public string ShortStringValue { get; set; }

        public PanoramicDataValue()
        {
        }

        public override int GetHashCode()
        {
            int code = DataType.GetHashCode() ^ Value.GetHashCode();
            return code;
        }
        public override bool Equals(object obj)
        {
            if (obj is PanoramicDataValue)
            {
                var pv = obj as PanoramicDataValue;
                if (pv.DataType == DataType)
                {
                    if (pv.Value.Equals(Value))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public string ToSqlString()
        {
            if (DataType == DataTypeConstants.NVARCHAR ||
                DataType == DataTypeConstants.GEOGRAPHY)
            {
                return "'" + Value.ToString().Replace("'", "") + "'";
            }
            else if (DataType == DataTypeConstants.BIT)
            {
                return "'" + Value.ToString().Replace("'", "") + "'";
            }
            else if (DataType == DataTypeConstants.DATE)
            {
                return "convert(datetime, '" + Value.ToString() + "', 20)";
            }
            else if (DataType == DataTypeConstants.TIME)
            {
                return "convert(time, '" + Value.ToString() + "', 20)";
            }
            return Value.ToString();
        }

        public string ToCompareString()
        {
            if (DataType == DataTypeConstants.NVARCHAR ||
                DataType == DataTypeConstants.DATE)
            {
                return "'" + Value.ToString().Replace("'", "") + "'";
            }
            return Value.ToString();
        }

    }

    public class PanoramicDataMultiValue
    {
        public List<PanoramicDataValue> Values { get; set; }

        public PanoramicDataMultiValue()
        {
            Values = new List<PanoramicDataValue>();
        }

        public override int GetHashCode()
        {
            int code = 0;
            foreach (var k in Values)
                code ^= k.GetHashCode();
            return code;
        }
        public override bool Equals(object obj)
        {
            if (obj is PanoramicDataMultiValue)
            {
                var pv = obj as PanoramicDataMultiValue;
                if (pv.Values.Count == this.Values.Count)
                {
                    for (int i = 0; i < this.Values.Count; i++)
                    {
                        bool good = true;
                        if (!pv.Values[i].Equals(this.Values[i]))
                        {
                            good = false;
                        }
                        if (!good)
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }
    }

    public class PanoramicDataValueComparison
    {
        public PanoramicDataValue Value { get; set; }
        public Predicate Predicate { get; set; }

        public PanoramicDataValueComparison()
        {
        }

        public PanoramicDataValueComparison(PanoramicDataValue value, Predicate predicate)
        {
            this.Value = value;
            this.Predicate = predicate;
        }

        public override int GetHashCode()
        {
            int code = 0;
            code ^= Value.GetHashCode();
            code ^= Predicate.GetHashCode();
            return code;
        }

        public override bool Equals(object obj)
        {
            if (obj is PanoramicDataValueComparison)
            {
                var compareTo = obj as PanoramicDataValueComparison;
                return compareTo.Predicate.Equals(this.Predicate) && compareTo.Value.Equals(this.Value);
            }
            return false;
        }


        public bool Compare(PanoramicDataValue value)
        {
            if (this.Predicate == Predicate.GREATER_THAN_EQUAL)
            {
                if (this.Value.DataType == DataTypeConstants.FLOAT ||
                    this.Value.DataType == DataTypeConstants.INT ||
                    this.Value.DataType == DataTypeConstants.BIT)
                {
                    double d1 = 0.0;
                    double d2 = 0.0;
                    if (double.TryParse(this.Value.StringValue, out d1) && 
                        double.TryParse(value.StringValue, out d2))
                    {
                        return d2 >= d1;
                    }
                }
                else if (this.Value.DataType == DataTypeConstants.NVARCHAR)
                {
                    int cmp = value.StringValue.CompareTo(this.Value.StringValue);
                    if (cmp == 1 || cmp == 0)
                    {
                        return true;
                    }
                }
            }
            if (this.Predicate == Predicate.LESS_THAN_EQUAL)
            {
                if (this.Value.DataType == DataTypeConstants.FLOAT ||
                    this.Value.DataType == DataTypeConstants.INT ||
                    this.Value.DataType == DataTypeConstants.BIT)
                {
                    double d1 = 0.0;
                    double d2 = 0.0;
                    if (double.TryParse(this.Value.StringValue, out d1) && 
                        double.TryParse(value.StringValue, out d2))
                    {
                        return d2 <= d1;
                    }
                }
                else if (this.Value.DataType == DataTypeConstants.NVARCHAR)
                {
                    int cmp = value.StringValue.CompareTo(this.Value.StringValue);
                    if (cmp == -1 || cmp == 0)
                    {
                        return true;
                    }
                }
                
            }

            return false;
        }
    }

    public class FilteredItem
    {
        public Dictionary<PanoramicDataColumnDescriptor, PanoramicDataValueComparison> PrimaryKeyComparisonValues { get; set; }

        public Dictionary<PanoramicDataColumnDescriptor, PanoramicDataValueComparison> GroupComparisonValues { get; set; }

        public Dictionary<PanoramicDataColumnDescriptor, PanoramicDataValueComparison> ColumnComparisonValues { get; set; }

        public bool IsHandwrittenFilter { get; set; }

        public long RowNumber { get; set; }

        public FilteredItem()
        {
            PrimaryKeyComparisonValues = new Dictionary<PanoramicDataColumnDescriptor, PanoramicDataValueComparison>();
            GroupComparisonValues = new Dictionary<PanoramicDataColumnDescriptor, PanoramicDataValueComparison>();
            ColumnComparisonValues = new Dictionary<PanoramicDataColumnDescriptor, PanoramicDataValueComparison>();
        }

        public FilteredItem(PanoramicDataRow row)
        {
            PrimaryKeyComparisonValues = new Dictionary<PanoramicDataColumnDescriptor, PanoramicDataValueComparison>();
            GroupComparisonValues = new Dictionary<PanoramicDataColumnDescriptor, PanoramicDataValueComparison>();
            ColumnComparisonValues = new Dictionary<PanoramicDataColumnDescriptor, PanoramicDataValueComparison>();

            foreach (var k in row.PrimaryKeyValues.Keys)
            {
                PrimaryKeyComparisonValues.Add(k, new PanoramicDataValueComparison(row.PrimaryKeyValues[k], Predicate.EQUALS));
            }
            if (PrimaryKeyComparisonValues.Count == 0)
            {
                foreach (var k in row.GroupedValues.Keys)
                {
                    GroupComparisonValues.Add(k, new PanoramicDataValueComparison(row.GroupedValues[k], Predicate.EQUALS));
                }
                if (GroupComparisonValues.Count == 0)
                {
                    foreach (var k in row.ColumnValues.Keys)
                    {
                        ColumnComparisonValues.Add(k, new PanoramicDataValueComparison(row.ColumnValues[k], Predicate.EQUALS));
                    }
                }
            }
        }

        public override int GetHashCode()
        {
            int code = 0;
            foreach (var k in PrimaryKeyComparisonValues.Keys)
                code ^= k.GetHashCode() + PrimaryKeyComparisonValues[k].GetHashCode();
            foreach (var k in GroupComparisonValues.Keys)
                code ^= k.GetHashCode() + GroupComparisonValues[k].GetHashCode();
            foreach (var k in ColumnComparisonValues.Keys)
                code ^= k.GetHashCode() + ColumnComparisonValues[k].GetHashCode();
            return code;
        }

        public override bool Equals(object obj)
        {
            FilteredItem compareTo = null;
            if (obj is PanoramicDataRow)
            {
                
            }

            if (obj is FilteredItem)
            {
                compareTo = obj as FilteredItem;
                bool keyComp = compare(this.PrimaryKeyComparisonValues, compareTo.PrimaryKeyComparisonValues);
                if (!keyComp)
                    return false;

                bool groupComp = compare(this.GroupComparisonValues, compareTo.GroupComparisonValues);
                if (!groupComp)
                    return false;

                bool valComp = compare(this.ColumnComparisonValues, compareTo.ColumnComparisonValues);
                if (!valComp)
                    return false;

                return true;
            }
            return false;
        }

        public bool compare(Dictionary<PanoramicDataColumnDescriptor, PanoramicDataValueComparison> a, Dictionary<PanoramicDataColumnDescriptor, PanoramicDataValueComparison> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }
            foreach (PanoramicDataColumnDescriptor cd in a.Keys)
            {
                if (!b.ContainsKey(cd))
                {
                    return false;
                }
                if (!a[cd].Equals(b[cd]))
                {
                    return false;
                }
            }
            return true;
        }
    }

    public enum Predicate { EQUALS, BETWEEN, LIKE, GREATER_THAN, LESS_THAN, GREATER_THAN_EQUAL, LESS_THAN_EQUAL }

    public class PanoramicDataRow : INotifyPropertyChanged
    {
        private bool _isHighligthed = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public long RowNumber { get; set; }

        public List<PanoramicDataValue> Values { get; set; }

        public Dictionary<PanoramicDataColumnDescriptor, PanoramicDataValue> ColumnValues { get; set; }

        public Dictionary<PanoramicDataColumnDescriptor, PanoramicDataValue> PrimaryKeyValues { get; set; }

        public Dictionary<PanoramicDataColumnDescriptor, PanoramicDataValue> GroupedValues { get; set; }

        public Dictionary<FilterModel, double> PassesFilterModel { get; set; }

        public bool IsHighligthed
        {
            get { return _isHighligthed; }
            set
            {
                _isHighligthed = value;
                this.OnPropertyChanged("IsHighligthed");
            }
        }

        public PanoramicDataRow()
        {
            Values = new List<PanoramicDataValue>();
            ColumnValues = new Dictionary<PanoramicDataColumnDescriptor, PanoramicDataValue>();
            PrimaryKeyValues = new Dictionary<PanoramicDataColumnDescriptor, PanoramicDataValue>();
            GroupedValues = new Dictionary<PanoramicDataColumnDescriptor, PanoramicDataValue>();
            PassesFilterModel = new Dictionary<FilterModel, double>();
        }

        public PanoramicDataValue GetValue(PanoramicDataColumnDescriptor cd)
        {
            if (ColumnValues.ContainsKey(cd))
            {
                return ColumnValues[cd];
            }
            else
            {
                return null;
            }
        }
        public PanoramicDataValue GetGroupedValue(PanoramicDataColumnDescriptor cd)
        {
            if (GroupedValues.ContainsKey(cd))
            {
                return GroupedValues[cd];
            }
            else
            {
                return null;
            }
        }

        public override int GetHashCode()
        {
            int code = 0;
            foreach (var k in PrimaryKeyValues.Keys)
                code ^= k.GetHashCode();
            foreach (var v in PrimaryKeyValues.Values)
                code ^= v.GetHashCode();
            return code;
        }

        public override bool Equals(object obj)
        {
            if (obj is PanoramicDataRow)
            {
                var row = obj as PanoramicDataRow;
                if (row.PrimaryKeyValues.Count == PrimaryKeyValues.Count)
                {
                    foreach (var k1 in row.PrimaryKeyValues.Keys)
                    {
                        bool found = false;
                        foreach (var k2 in PrimaryKeyValues.Keys)
                        {
                            if (k1.Equals(k2))
                            {
                                if (row.PrimaryKeyValues[k1].Equals(PrimaryKeyValues[k2]))
                                {
                                    found = true;
                                }
                            }
                        }
                        if (!found)
                        {
                            return false;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private void OnPropertyChanged(string propertyName)
        {
            System.Diagnostics.Debug.Assert(this.GetType().GetProperty(propertyName) != null);
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
