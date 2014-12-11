using NCalc;
using PanoramicDataModel;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PanoramicData.model.view;
using PanoramicData.controller.data;

namespace PanoramicData.controller.filter
{
    public class FilterExpressionEvaluator
    {
        private string notOperator = "not";

        public FilterExpressionEvaluator()
        {
        }

        public string GenerateFilterExpression(FilterModel filterModel, FilteringType filteringType, Dictionary<PanoramicDataColumnDescriptor, ExpressionValue> columnValues, 
            int count, bool addFirstFilter, bool invert)
        {
            string expr = generateFilterWhereClauseRecursive(filterModel, filteringType, columnValues, count, addFirstFilter, invert);
            return expr;
        }

        public bool EvaluateFilterExpression(FilterModel filterModel, FilteringType filteringType, Dictionary<PanoramicDataColumnDescriptor, ExpressionValue> columnValues,
            int count, bool addFirstFilter, bool invert, PanoramicDataColumnDescriptor pivotColumnDescriptor = null)
        {
            string expr = generateFilterWhereClauseRecursive(filterModel, filteringType, columnValues, count, addFirstFilter, invert);
            bool ret = true;
            if (expr != "")
            {
                Expression expression = new Expression(expr, EvaluateOptions.NoCache); // no cache seems way faster for some weird reseason, mabye bc of the debugging printlns.
                expression.EvaluateFunction += delegate(string name, FunctionArgs args)
                {
                    if (name == "like")
                    {
                        string val = args.Parameters[0].ParsedExpression.ToString().ToLower();
                        string pat = args.Parameters[1].ParsedExpression.ToString().ToLower();

                        if (val.Length >= 2 && val[0] == '\'' && val[val.Length - 1] == '\'')
                        {
                            val = val.Substring(1, val.Length - 2);
                        }
                        if (pat.Length >= 2 && pat[0] == '\'' && pat[pat.Length - 1] == '\'')
                        {
                            pat = pat.Substring(1, pat.Length - 2);
                        }
                        pat = pat.Replace("%", ".*");
                        Match m = Regex.Match(val, pat);
                        args.Result = m.Success;
                    }
                };

                ret = (bool)expression.Evaluate();
            }
            return ret;
        }

        private string generateFilterWhereClause(FilterModel filterModel, Dictionary<PanoramicDataColumnDescriptor, ExpressionValue> columnValues, bool invert, bool addFilters)
        {
            // embedded filters
            List<List<string>> embeddedWhereList = generateFilterWhereClause(filterModel.EmbeddedFilteredItems, columnValues);

            // filters
            List<List<string>> filteredWhereList = generateFilterWhereClause(filterModel.FilteredItems, columnValues);

            string embeddedWhere = string.Join(" or" + QueryGenerator.NEW, embeddedWhereList.Select(w => "(" + string.Join(" and ", w) + ")"));
            string filteredWhere = string.Join(" or" + QueryGenerator.NEW, filteredWhereList.Select(w => "(" + string.Join(" and ", w) + ")"));

            List<string> allWheres = new List<string>();
            if (embeddedWhere != "")
            {
                if (invert)
                {
                    allWheres.Add(notOperator + " (" + embeddedWhere + ")");
                }
                else
                {
                    allWheres.Add("(" + embeddedWhere + ")");
                }
            }
            if (filteredWhere != "" && addFilters)
            {
                if (invert)
                {
                    allWheres.Add(notOperator + " (" + filteredWhere + ")");
                }
                else
                {
                    allWheres.Add("(" + filteredWhere + ")");
                }
            }
            if (filteredWhere == "" && embeddedWhere == "" && invert)
            {
                allWheres.Add(notOperator + " 1 = 1");
            }
            return string.Join(" and" + QueryGenerator.NEW, allWheres);
        }

        private List<List<string>> generateFilterWhereClause(List<FilteredItem> filters, Dictionary<PanoramicDataColumnDescriptor, ExpressionValue> columnValues)
        {
            List<List<string>> filteredWhereList = new List<List<string>>();
            foreach (var filteredItem in filters.ToArray())
            {
                List<string> subWhere = new List<string>();
                filteredWhereList.Add(subWhere);
                generateFilterWhereClauseSingle(filteredItem.PrimaryKeyComparisonValues, columnValues, subWhere);
                generateFilterWhereClauseSingle(filteredItem.GroupComparisonValues, columnValues, subWhere);
                generateFilterWhereClauseSingle(filteredItem.ColumnComparisonValues, columnValues, subWhere);
                
            }
            return filteredWhereList;
        }

        private void generateFilterWhereClauseSingle(Dictionary<PanoramicDataColumnDescriptor, PanoramicDataValueComparison> compValues, Dictionary<PanoramicDataColumnDescriptor, ExpressionValue> columnValues, List<string> subWhere)
        {
            foreach (var column in compValues.Keys)
            {
                if (columnValues.ContainsKey(column))
                {
                    ExpressionValue expressionValue = columnValues[column];
                    string value = expressionValue.Value;
                    if (value == "")
                    {
                        if (column.DataType == DataTypeConstants.FLOAT ||
                            column.DataType == DataTypeConstants.INT)
                        {
                            value = int.MaxValue.ToString();
                        }
                        else
                        {
                            value = "'DBNULL'";
                        }
                    }
                    object val = compValues[column].Value;
                    if (val is PanoramicDataValue)
                    {
                        if (compValues[column].Predicate == Predicate.EQUALS)
                        {
                            if (column.DataType == DataTypeConstants.FLOAT)
                            {
                                // fixes sql server precision issues when comparing floats. 
                                // t0.age_8  > 28.6870706185852 - 0.0001 AND t0.age_8 < 28.6870706185852 + 0.0001
                                subWhere.Add(value + " > " + ((PanoramicDataValue)val).ToSqlString() + " - 0.0001 AND " +
                                                value + " < " + ((PanoramicDataValue)val).ToSqlString() + " + 0.0001");
                            }
                            else
                            {
                                if ((val as PanoramicDataValue).Value != System.DBNull.Value)
                                {
                                    subWhere.Add(value + " = " + ((PanoramicDataValue)val).ToSqlString());
                                }
                                else
                                {
                                    subWhere.Add(value + " is null ");
                                }
                            }
                        }
                        else if (compValues[column].Predicate == Predicate.GREATER_THAN)
                        {
                            if (column.DataType == DataTypeConstants.FLOAT ||
                                column.DataType == DataTypeConstants.INT ||
                                column.DataType == DataTypeConstants.NVARCHAR ||
                                column.DataType == DataTypeConstants.TIME ||
                                column.DataType == DataTypeConstants.DATE)
                            {
                                subWhere.Add(value + " > " + ((PanoramicDataValue)val).ToSqlString());
                            }
                        }
                        else if (compValues[column].Predicate == Predicate.LESS_THAN)
                        {
                            if (column.DataType == DataTypeConstants.FLOAT ||
                                column.DataType == DataTypeConstants.INT ||
                                column.DataType == DataTypeConstants.NVARCHAR ||
                                column.DataType == DataTypeConstants.TIME ||
                                column.DataType == DataTypeConstants.DATE)
                            {
                                subWhere.Add(value + " < " + ((PanoramicDataValue)val).ToSqlString());
                            }
                        }
                        else if (compValues[column].Predicate == Predicate.GREATER_THAN_EQUAL)
                        {
                            if (column.DataType == DataTypeConstants.FLOAT ||
                                column.DataType == DataTypeConstants.INT ||
                                column.DataType == DataTypeConstants.NVARCHAR ||
                                column.DataType == DataTypeConstants.TIME ||
                                column.DataType == DataTypeConstants.DATE)
                            {
                                subWhere.Add(value + " >= " + ((PanoramicDataValue)val).ToSqlString());
                            }
                            else if (column.DataType == DataTypeConstants.BIT)
                            {
                                subWhere.Add(value + " >= " + ((PanoramicDataValue)val).ToSqlString());
                            }
                        }
                        else if (compValues[column].Predicate == Predicate.LESS_THAN_EQUAL)
                        {
                            if (column.DataType == DataTypeConstants.FLOAT ||
                                column.DataType == DataTypeConstants.INT ||
                                column.DataType == DataTypeConstants.NVARCHAR ||
                                column.DataType == DataTypeConstants.TIME ||
                                column.DataType == DataTypeConstants.DATE) 
                            {
                                subWhere.Add(value + " <= " + ((PanoramicDataValue)val).ToSqlString());
                            }
                            else if (column.DataType == DataTypeConstants.BIT)
                            {
                                subWhere.Add(value + " <= " + ((PanoramicDataValue)val).ToSqlString());
                            }
                        }
                        else if (compValues[column].Predicate == Predicate.LIKE)
                        {
                            subWhere.Add(value + " like '%" + ((PanoramicDataValue)val).ToSqlString().Replace("'", "") + "%'");
                        }
                    }
                    else if (val is PanoramicDataMultiValue)
                    {
                        PanoramicDataMultiValue multiVal = ((PanoramicDataMultiValue)val);
                        PanoramicDataValue v1 = multiVal.Values[0];
                        PanoramicDataValue v2 = multiVal.Values[1];
                        subWhere.Add(value + " >= " + v1.ToSqlString() + " and " + value + " <= " + v2.ToSqlString());
                    }
                }
            }
        }

        private string generateFilterWhereClauseRecursive(
            FilterModel filterModel, FilteringType filteringType, 
            Dictionary<PanoramicDataColumnDescriptor, ExpressionValue> columnValues,
            int count, bool addFirstFilter, bool invert)
        {
            string query = "";

            if (filterModel.Pivots.Any(p => p.Selected) && (count != 0 || (addFirstFilter && count == 0)))
            {
                FilterQueryGenerator filterQueryGenerator = new FilterQueryGenerator(filterModel);
                List<PanoramicDataColumnDescriptor> relevantDescriptors = new List<PanoramicDataColumnDescriptor>();
                List<FilterModelColumnDescriptorPair> passFlagDescriptors = new List<FilterModelColumnDescriptorPair>(); 
                List<QueryColumnQualifier> queryColumnQualifiers = new List<QueryColumnQualifier>();
                string innerQuery = filterQueryGenerator.GenerateFetchQuery(false, -1, -1, false, false,
                    out relevantDescriptors, out passFlagDescriptors,  out queryColumnQualifiers,"subquery_" + count);
                
                List<string> clauses = new List<string>();
                foreach (var pivot in filterModel.Pivots.Where(p => p.Selected))
                {
                    var expressionValue = columnValues[pivot.ColumnDescriptor];
                    QueryColumnQualifier qualifier = queryColumnQualifiers.Where(q => q.ColumnDescriptor.MatchSimple(pivot.ColumnDescriptor)).First();
                    clauses.Add(expressionValue.Value + " = " + qualifier.TableQualifier + "." + qualifier.FieldQualifier);
                }
                query = (invert ? "not " : "") + "exists " + QueryGenerator.NEW +
                        "(" + QueryGenerator.NEW +
                            QueryGenerator.Indent(innerQuery) + QueryGenerator.NEW +
                        "where" + QueryGenerator.NEW +
                            QueryGenerator.Indent(string.Join(" and" + QueryGenerator.NEW, clauses)) + QueryGenerator.NEW +
                        ")" + QueryGenerator.NEW;
            }
            else
            {
                if (filterModel.GetIncomingFilterModels(filteringType).Count == 0)
                {
                    query += generateFilterWhereClause(filterModel, columnValues, invert, count != 0 || (addFirstFilter && count == 0));
                }
                else if (filterModel.GetIncomingFilterModels(filteringType).Count > 1)
                {
                    List<string> clauses = new List<string>();
                    clauses.Add(generateFilterWhereClause(filterModel, columnValues, invert, count != 0 || (addFirstFilter && count == 0)));

                    List<string> innerClauses = new List<string>();

                    // order the incomingFilterModels (only needed for DIFF operation)
                    List<FilterModel> orderedIncomingFilterModels = new List<FilterModel>();
                    if (filterModel.DiffSourceFilterModel != null)
                    {
                        orderedIncomingFilterModels.Add(filterModel.DiffSourceFilterModel);
                    }
                    foreach (var incomingFilterModel in filterModel.GetIncomingFilterModels(filteringType))
                    {
                        if (!orderedIncomingFilterModels.Contains(incomingFilterModel))
                        {
                            orderedIncomingFilterModels.Add(incomingFilterModel);
                        }
                    }

                    foreach (var incomingFilterModel in orderedIncomingFilterModels)
                    {
                        innerClauses.Add(generateFilterWhereClauseRecursive(incomingFilterModel, filteringType, columnValues, ++count, addFirstFilter,
                            filterModel.GetInvertedIncomingFilterModels(filteringType).Contains(incomingFilterModel)));
                    }

                    string setOperation = "and";
                    if (filterModel.GetFilterModelLinkType(filteringType) == FilterModelLinkType.OR)
                    {
                        setOperation = "or";
                    }
                    string innerQuery = string.Join(QueryGenerator.NEW + setOperation + QueryGenerator.NEW, innerClauses.Where(q => q.Trim() != ""));

                    if (innerQuery != "")
                    {
                        clauses.Add("(" + QueryGenerator.NEW + QueryGenerator.Indent(innerQuery) + QueryGenerator.NEW + ")");
                        query += string.Join(QueryGenerator.NEW + "and" + QueryGenerator.NEW, clauses.Where(q => q.Trim() != ""));
                    }
                }
                else if (filterModel.GetIncomingFilterModels(filteringType).Count == 1)
                {
                    List<string> clauses = new List<string>();
                    clauses.Add(generateFilterWhereClause(filterModel, columnValues, invert, count != 0 || (addFirstFilter && count == 0)));
                    clauses.Add(generateFilterWhereClauseRecursive(
                        filterModel.GetIncomingFilterModels(filteringType)[0],
                        filteringType, columnValues, ++count, addFirstFilter,
                        filterModel.GetInvertedIncomingFilterModels(filteringType).Contains(filterModel.GetIncomingFilterModels(filteringType)[0])));
                    query += string.Join(QueryGenerator.NEW + "and" + QueryGenerator.NEW, clauses.Where(q => q.Trim() != ""));
                }
            }
            if (query != "")
            {
                query = "(" + query + ")";
            }
            return query;
        }
    }

    public class ExpressionValue
    {
        public string Value { get; set; }
        public ExpressionValue(string value)
        {
            Value = value;
        }
    }
}
