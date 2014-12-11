using PanoramicData.controller.filter;
using PanoramicData.model.view;
using PanoramicDataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.controller.data
{
    public class FilterQueryGenerator : QueryGenerator
    {
        public FilterQueryGenerator(FilterModel filterModel)
        {
            FilterModel = filterModel;
            TableModel = filterModel.TableModel;
        }

        protected override List<PanoramicDataColumnDescriptor> GetOuterQueryColumnDescriptors()
        {
            return FilterModel.ColumnDescriptors.ToList();
        }

        protected override void FindFilterColumnDescriptorsRecursive(
            FilterModel currentFilterModel,
            List<FilterModelColumnDescriptorPair> filterColumnDescriptors)
        {
            if (currentFilterModel == null)
            {
                currentFilterModel = FilterModel;
            }

            if (currentFilterModel != FilterModel)
            {
                foreach (var fi in currentFilterModel.FilteredItems)
                {
                    foreach (var cd in fi.ColumnComparisonValues.Keys.Concat(fi.PrimaryKeyComparisonValues.Keys).Concat(fi.GroupComparisonValues.Keys))
                    {
                        foreach (var pi in TableModel.ColumnDescriptors.Keys)
                        {
                            if (TableModel.ColumnDescriptors[pi].Where(tcd => tcd.MatchComplete(cd)).Count() == 0)
                            {
                                FilterModelColumnDescriptorPair pair = new FilterModelColumnDescriptorPair();
                                pair.FilterModel = currentFilterModel;
                                pair.ColumnDescriptor = cd;
                                filterColumnDescriptors.Add(pair);
                            }
                        }
                    }
                }
            }
            foreach (var fi in currentFilterModel.EmbeddedFilteredItems)
            {
                foreach (var cd in fi.ColumnComparisonValues.Keys.Concat(fi.PrimaryKeyComparisonValues.Keys).Concat(fi.GroupComparisonValues.Keys))
                {
                    foreach (var pi in TableModel.ColumnDescriptors.Keys)
                    {
                        //if (TableModel.ColumnDescriptors[pi].Where(tcd => tcd.MatchComplete(cd)).Count() == 0)
                        {
                            FilterModelColumnDescriptorPair pair = new FilterModelColumnDescriptorPair();
                            pair.FilterModel = currentFilterModel;
                            pair.ColumnDescriptor = cd;
                            filterColumnDescriptors.Add(pair);
                        }
                    }
                }
            }
            foreach (var fm in currentFilterModel.GetIncomingFilterModels(FilteringType.Brush, FilteringType.Filter))
            {
                FindFilterColumnDescriptorsRecursive(fm, filterColumnDescriptors);
            }
            foreach (var namedColumnDescriptor in currentFilterModel.ColumnDescriptors.Where(cd => cd is NamedFilterModelColumnDescriptor))
            {
                FindFilterColumnDescriptorsRecursive(((NamedFilterModelColumnDescriptor)namedColumnDescriptor).FilterModel,
                    filterColumnDescriptors);
            }
        }

        protected override FilterModelColumnDescriptorPair GenerateNamedFilterModelColumnQuery(
            NamedFilterModelColumnDescriptor namedColumnDescriptor,
            List<PanoramicDataColumnDescriptor> columnDescriptors,
            List<QueryColumnQualifier> innerQualifiers,
            int counter,
            bool addFirstFilter)
        {
            FilterModel filterModel = namedColumnDescriptor.FilterModel;
            bool applyGrouping = filterModel.ColumnDescriptors.Count(cd => cd.IsAnyGroupingOperationApplied()) > 0;
            List<PanoramicDataColumnDescriptor> sortedGroupedColumnDescriptors =
                filterModel.ColumnDescriptors.Where(cd => cd.IsAnyGroupingOperationApplied())
                    .OrderBy(cd => cd.Order)
                    .ToList();

            string clause = GenerateFilterWhereClause(columnDescriptors, innerQualifiers, filterModel,
                sortedGroupedColumnDescriptors, applyGrouping, addFirstFilter);
            if (clause == "")
            {
                clause = "1 = 1";
            }
            FilterModelColumnDescriptorPair flag = new FilterModelColumnDescriptorPair();
            flag.FilterModel = filterModel;
            flag.ColumnDescriptor = namedColumnDescriptor;
            flag.FieldQualifier = "flag_" + counter++;
            flag.Query = "case when " + clause + " then 1.0 else 0.0 end";

            return flag;
        }

        protected override List<FilterModelColumnDescriptorPair> GenerateFilterModelPassFlagDescriptors()
        {
            List<FilterModelColumnDescriptorPair> flags = new List<FilterModelColumnDescriptorPair>();
            foreach (FilterModel filterModel in FilterModel.GetIncomingFilterModels(FilteringType.Brush).ToArray())
            {
                if (filterModel.Selected)
                {
                    FilterModelColumnDescriptorPair flag = new FilterModelColumnDescriptorPair();
                    flag.IsFlag = true;
                    flag.FilterModel = filterModel;
                    flags.Add(flag);
                }
            }
            return flags;
        }

        protected override string GenerateFilterWhereClause(List<PanoramicDataColumnDescriptor> columnDescriptors,
            List<QueryColumnQualifier> qualifiers, FilterModel filterModel,
            List<PanoramicDataColumnDescriptor> sortedGroupedFilterModelColumnDescriptors,
            bool applyGrouping,
            bool addFirstFilter)
        {
            FilterExpressionEvaluator eval = new FilterExpressionEvaluator();
            Dictionary<PanoramicDataColumnDescriptor, ExpressionValue> columnValues = new Dictionary<PanoramicDataColumnDescriptor, ExpressionValue>();

            foreach (var cd in columnDescriptors)
            {
                if (!columnValues.ContainsKey(cd))
                {
                    if (qualifiers.Where(q => q.ColumnDescriptor.MatchComplete(cd)).Count() > 0)
                    {
                        QueryColumnQualifier qualifier = qualifiers.Last(q => q.ColumnDescriptor.MatchComplete(cd));

                        if (cd.IsBinned)
                        {
                            columnValues.Add(cd, new ExpressionValue(
                                "(floor(" + qualifier.TableQualifier + P + qualifier.FieldQualifier +
                                " / " + cd.BinSize + ") * " + cd.BinSize + ")"));
                        }
                        else if (cd.IsTiled)
                        {
                            List<string> sortOrderList = new List<string>();
                            /*
                            foreach (var sortCd in innerSortColumnDescriptors)
                            {
                                QueryColumnQualifier qualifier = innerQualifiers.Single(q => q.ColumnDescriptor.MatchComplete(sortCd));
                                string name = fd.GetSQLSelect(qualifier, true, false, null);
                                sortOrderList.Add(
                                    "case when " + name + " is null then 1 else 0 end, " +
                                    name + (fd.SortMode == SortMode.Desc ? " desc" : " asc"));
                            }
                            innerSelectList.Add("ntile(" + fd.NumberOfTiles + ") over (order by " + string.Join(C + S, sortOrderList) + ") as " + outerQualifier.FieldQualifier);
                            */
                            columnValues.Add(cd, new ExpressionValue(qualifier.TableQualifier + P + qualifier.FieldQualifier));
                        }
                        else if (cd.IsGrouped || cd.IsPrimaryKey)
                        {
                            columnValues.Add(cd,
                                new ExpressionValue(qualifier.TableQualifier + P + qualifier.FieldQualifier));
                        }
                        else
                        {
                            string name =
                                cd.GetSQLSelect(
                                    new QueryColumnQualifier(qualifier.FieldQualifier, qualifier.TableQualifier, cd),
                                    applyGrouping, false);
                            if (applyGrouping)
                            {
                                try
                                {
                                    name += " over (partition by " +
                                        string.Join(", ", sortedGroupedFilterModelColumnDescriptors.Select(cd2 =>
                                        {
                                            var qual = qualifiers.First(iq => iq.ColumnDescriptor.MatchSimple(cd2));
                                            return qual.TableQualifier + P + qual.FieldQualifier;
                                        })) + ")";
                                }
                                catch (Exception)
                                {


                                }

                            }
                            columnValues.Add(cd, new ExpressionValue(name));
                        }
                    }
                }
            }
            return eval.GenerateFilterExpression(filterModel, FilteringType.Filter, columnValues, 1, addFirstFilter,
                FilterModel.GetInvertedIncomingFilterModels(FilteringType.Brush).Contains(filterModel));
        }

        protected override string GenerateFilterWhereClause(
            List<PanoramicDataColumnDescriptor> columnDescriptors,
            List<QueryColumnQualifier> qualifiers,
            bool addFirstFilter)
        {
            FilterExpressionEvaluator eval = new FilterExpressionEvaluator();
            Dictionary<PanoramicDataColumnDescriptor, ExpressionValue> columnValues = new Dictionary<PanoramicDataColumnDescriptor, ExpressionValue>();

            foreach (var fd in columnDescriptors)
            {
                if (!columnValues.ContainsKey(fd))
                {
                    if (qualifiers.Where(q => q.ColumnDescriptor.MatchComplete(fd)).Count() > 0)
                    {
                        QueryColumnQualifier qualifier = qualifiers.Last(q => q.ColumnDescriptor.MatchComplete(fd));
                        columnValues.Add(fd, new ExpressionValue(qualifier.TableQualifier + P + qualifier.FieldQualifier));
                    }
                }
            }

            return eval.GenerateFilterExpression(FilterModel, FilteringType.Filter, columnValues, 0, addFirstFilter, false);
        }

        public override void PostProcessRow(PanoramicDataRow row)
        {
            if (FilterModel.FilterRendererType == FilterRendererType.Table)
            {
                FilteredItem fiRow = new FilteredItem(row);
                foreach (var fi in FilterModel.FilteredItems.ToArray())
                {
                    if (fi != null)
                    {
                        if (fi.Equals(fiRow))
                        {
                            row.IsHighligthed = true;
                        }
                    }
                }
            }
        }
    }
}
