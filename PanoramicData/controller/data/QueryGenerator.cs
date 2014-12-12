using System.IO.Ports;
using FarseerPhysics.Common;
using NetTopologySuite.GeometriesGraph;
using NetTopologySuite.Index.Bintree;
using PanoramicDataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using RTools_NTS.Util;
using starPadSDK.AppLib;
using starPadSDK.MathExpr;
using Path = System.IO.Path;
using PanoramicData.model.view;
using PanoramicData.controller.sql;

namespace PanoramicData.controller.data
{
    public abstract class QueryGenerator
    {
        public static string TAB = "\t";
        public static string NEW = "\n";
        public static string P = ".";
        public static string C = ",";
        public static string S = " ";

        public TableModel TableModel { get; set; }
        public FilterModel FilterModel { get; set; }

        protected abstract void FindFilterColumnDescriptorsRecursive(
            FilterModel currentFilterModel,
            List<FilterModelColumnDescriptorPair> filterColumnDescriptors);

        protected abstract List<FilterModelColumnDescriptorPair> GenerateFilterModelPassFlagDescriptors();

        protected abstract FilterModelColumnDescriptorPair GenerateNamedFilterModelColumnQuery(
            NamedFilterModelColumnDescriptor namedColumnDescriptor,
            List<PanoramicDataColumnDescriptor> columnDescriptors,
            List<QueryColumnQualifier> innerQualifiers,
            int counter,
            bool addFirstFilter);

        protected abstract string GenerateFilterWhereClause(
            List<PanoramicDataColumnDescriptor> columnDescriptors,
            List<QueryColumnQualifier> qualifiers,
            bool addFirstFilter);

        protected abstract string GenerateFilterWhereClause(List<PanoramicDataColumnDescriptor> columnDescriptors,
            List<QueryColumnQualifier> qualifiers, FilterModel filterModel,
            List<PanoramicDataColumnDescriptor> sortedGroupedFilterModelColumnDescriptors,
            bool applyGrouping,
            bool addFirstFilter);

        protected abstract List<PanoramicDataColumnDescriptor> GetOuterQueryColumnDescriptors();

        public string GenerateFetchQuery(bool usePageing, int startIndex, int pageCount,
            bool addFirstFilter, bool includeRowNumber,
            out List<PanoramicDataColumnDescriptor> columnDescriptors,
            out List<FilterModelColumnDescriptorPair> filterModelPassFlagDescriptors,
            out List<QueryColumnQualifier> queryColumnQualifiers,
            string tableName = "tt")
        {
            bool applyGrouping = false;

            // get the additional filterColumnDescriptors
            List<FilterModelColumnDescriptorPair> filterColumnDescriptors = new List<FilterModelColumnDescriptorPair>();
            FindFilterColumnDescriptorsRecursive(null, filterColumnDescriptors);
            filterColumnDescriptors = filterColumnDescriptors.Distinct().ToList();

            // get all flags
            List<FilterModelColumnDescriptorPair> flags = GenerateFilterModelPassFlagDescriptors();

            // generate the most inner part of the query
            List<PanoramicDataColumnDescriptor> innerQueryColumnDescriptors = new List<PanoramicDataColumnDescriptor>();
            List<QueryColumnQualifier> innerQualifiers = new List<QueryColumnQualifier>();
            string innerQuery = generateInnerQuery(out innerQueryColumnDescriptors, out innerQualifiers, filterColumnDescriptors);

            // get the outermost column descriptors
            List<PanoramicDataColumnDescriptor> outerQueryColumnDescriptors = GetOuterQueryColumnDescriptors();

            // generate the list of all column descriptors that need to present in the mid-level query(ies)
            List<FilterModelColumnDescriptorPair> allMidQueryColumnDescriptors = new List<FilterModelColumnDescriptorPair>();
            allMidQueryColumnDescriptors.AddRange(filterColumnDescriptors);
            allMidQueryColumnDescriptors.AddRange(outerQueryColumnDescriptors.Where(cd => cd.IsBinned).Select(cd => new FilterModelColumnDescriptorPair(cd, FilterModel)));
            allMidQueryColumnDescriptors.AddRange(outerQueryColumnDescriptors.Where(cd => cd is NamedFilterModelColumnDescriptor).Distinct().Select(cd => new FilterModelColumnDescriptorPair(cd, FilterModel)));
            allMidQueryColumnDescriptors.AddRange(outerQueryColumnDescriptors.Where(cd => cd is CalculatedColumnDescriptor).Select(cd => new FilterModelColumnDescriptorPair(cd, FilterModel)));
            allMidQueryColumnDescriptors.AddRange(flags);
            innerQueryColumnDescriptors.AddRange(filterColumnDescriptors.Select(pair => pair.ColumnDescriptor).ToList());
            allMidQueryColumnDescriptors = allMidQueryColumnDescriptors.Distinct().ToList();

            // Calculate all necessary levels
            MidQueryLevel startLevel = new MidQueryLevel();
            List<QueryColumnQualifier> levelQualifiers =
                innerQualifiers.Select(
                    iq => new QueryColumnQualifier(iq.FieldQualifier, iq.TableQualifier, iq.ColumnDescriptor, 0)).ToList();
            startLevel.FromTableName = "t_inner";
            startLevel.ColumnDescriptors = allMidQueryColumnDescriptors;
            int levelCount = 1;
            int flagCounter = 0;

            List<FilterModelColumnDescriptorPair> pushUp = new List<FilterModelColumnDescriptorPair>();
            MidQueryLevel currentLevel = startLevel;
            int columnCounter = 0;
            do
            {
                string nextTableName = tableName + "_" + levelCount;
                bool stayOnSameLevel = false;

                foreach (var pair in currentLevel.ColumnDescriptors.ToArray())
                {
                    PanoramicDataColumnDescriptor columnDescriptor = pair.ColumnDescriptor;

                    if (pair.IsFlag)
                    {
                        List<FilterModelColumnDescriptorPair> flagFilterColumnDescriptors = new List<FilterModelColumnDescriptorPair>();
                        FindFilterColumnDescriptorsRecursive(pair.FilterModel, flagFilterColumnDescriptors);
                        flagFilterColumnDescriptors = flagFilterColumnDescriptors.Distinct().ToList();

                        // if any of the operators / labels are undefiend we need to push this to the next level
                        // and add the undefined ones to the current one. 
                        bool allDefined = flagFilterColumnDescriptors.Select(pp => pp.ColumnDescriptor)
                            .All(cd2 => levelQualifiers.Any(q => q.ColumnDescriptor.MatchComplete(cd2) && q.Level < levelCount));

                        if (!allDefined)
                        {
                            pushUp.Add(pair);
                            pair.PushedUp = true;
                            currentLevel.ColumnDescriptors.Remove(pair);
                            List<PanoramicDataColumnDescriptor> undefineds =
                                flagFilterColumnDescriptors.Select(pp => pp.ColumnDescriptor)
                                    .Where(
                                        cd2 =>
                                            !levelQualifiers.Any(
                                                q => q.ColumnDescriptor.MatchComplete(cd2) && q.Level < levelCount))
                                    .ToList();
                            currentLevel.ColumnDescriptors.AddRange(
                                undefineds.Select(cd => new FilterModelColumnDescriptorPair(cd, pair.FilterModel)));
                            stayOnSameLevel = true;
                        }
                        else
                        {
                            applyGrouping =
                                pair.FilterModel.ColumnDescriptors.Count(cd => cd.IsAnyGroupingOperationApplied()) > 0;
                            List<PanoramicDataColumnDescriptor> sortedGroupedColumnDescriptors =
                                pair.FilterModel.ColumnDescriptors.Where(cd => cd.IsAnyGroupingOperationApplied())
                                    .OrderBy(cd => cd.Order)
                                    .ToList();

                            string clause = GenerateFilterWhereClause(innerQueryColumnDescriptors,
                                levelQualifiers.Where(q => q.Level < levelCount).ToList(),
                                pair.FilterModel,
                                sortedGroupedColumnDescriptors, applyGrouping, true);

                            if (clause != "")
                            {
                                pair.IsEmptyFlag = false;
                                pair.FieldQualifier = "flag_" + flagCounter++;
                                currentLevel.Selects.Add("case when " + clause + " then 1.0 else 0.0 end as " +
                                                         pair.FieldQualifier);
                            }
                        }
                    }
                    else if (columnDescriptor != null)
                    {
                        QueryColumnQualifier qualifier = null;
                        if (levelQualifiers.Any(q => q.ColumnDescriptor.MatchSimple(columnDescriptor)))
                        {
                            qualifier = levelQualifiers.First(q => q.ColumnDescriptor.MatchSimple(columnDescriptor));
                        }
                        string returnFieldQualifierName = columnDescriptor.Name + "_" + columnCounter++;

                        if (columnDescriptor is NamedFilterModelColumnDescriptor)
                        {
                            FilterModel filterModel = (columnDescriptor as NamedFilterModelColumnDescriptor).FilterModel;

                            List<FilterModelColumnDescriptorPair> namedFilterColumnDescriptors = new List<FilterModelColumnDescriptorPair>();
                            FindFilterColumnDescriptorsRecursive(filterModel, namedFilterColumnDescriptors);
                            List<PanoramicDataColumnDescriptor> defined =
                                namedFilterColumnDescriptors.Select(pp => pp.ColumnDescriptor)
                                    .Where(
                                        cd2 =>
                                            levelQualifiers.Any(
                                                q => q.ColumnDescriptor.MatchComplete(cd2) && q.Level < levelCount))
                                    .ToList();
                            bool allDefined = defined.Count == namedFilterColumnDescriptors.Count;

                            if (!allDefined)
                            {
                                pushUp.Add(pair);
                                pair.PushedUp = true;
                                currentLevel.ColumnDescriptors.Remove(pair);
                                List<PanoramicDataColumnDescriptor> undefineds =
                                    namedFilterColumnDescriptors.Select(pp => pp.ColumnDescriptor)
                                        .Where(
                                            cd2 =>
                                                !levelQualifiers.Any(
                                                    q => q.ColumnDescriptor.MatchComplete(cd2) && q.Level < levelCount))
                                        .ToList();
                                currentLevel.ColumnDescriptors.AddRange(
                                    undefineds.Select(cd => new FilterModelColumnDescriptorPair(cd, pair.FilterModel)));
                                stayOnSameLevel = true;
                            }
                            else
                            {
                                applyGrouping =
                                    filterModel.ColumnDescriptors.Count(cd => cd.IsAnyGroupingOperationApplied()) > 0;
                                List<PanoramicDataColumnDescriptor> sortedGroupedColumnDescriptors =
                                    filterModel.ColumnDescriptors.Where(cd => cd.IsAnyGroupingOperationApplied())
                                        .OrderBy(cd => cd.Order)
                                        .ToList();

                                string clause = GenerateFilterWhereClause(innerQueryColumnDescriptors, levelQualifiers.Where(q => q.Level < levelCount).ToList(),
                                    filterModel,
                                    sortedGroupedColumnDescriptors, applyGrouping, addFirstFilter);
                                if (clause == "")
                                {
                                    clause = "1 = 1";
                                }
                                returnFieldQualifierName = "flag_" + flagCounter++;
                                currentLevel.Selects.Add("case when " + clause + " then 1.0 else 0.0 end as " +
                                                         returnFieldQualifierName);
                            }
                        }
                        else if (columnDescriptor is CalculatedColumnDescriptor)
                        {
                            CalculatedColumnDescriptor calcDescriptor = columnDescriptor as CalculatedColumnDescriptor;
                            CalculatedColumnDescriptorInfo info = calcDescriptor.CalculatedColumnDescriptorInfo;

                            // if any of the operators / labels are undefiend we need to push this to the next level
                            // and add the undefined ones to the current one. 
                            bool allDefined = info.ProvidedLabels.Select(kvp => kvp.Value)
                                .All(cd2 => levelQualifiers.Any(q => q.ColumnDescriptor.MatchSimple(cd2) && q.Level < levelCount));

                            if (!allDefined)
                            {
                                pushUp.Add(pair);
                                pair.PushedUp = true;
                                currentLevel.ColumnDescriptors.Remove(pair);
                                List<PanoramicDataColumnDescriptor> undefineds = info.ProvidedLabels.Select(kvp => kvp.Value)
                                    .Where(cd2 => !levelQualifiers.Any(q => q.ColumnDescriptor.MatchSimple(cd2) && q.Level < levelCount)).ToList();
                                currentLevel.ColumnDescriptors.AddRange(undefineds.Select(cd => new FilterModelColumnDescriptorPair(cd, pair.FilterModel)));
                                stayOnSameLevel = true;
                            }
                            else
                            {
                                SqlExprEvaluator sql = new SqlExprEvaluator(info);
                                string text = sql.GetQuery(levelQualifiers);
                                Console.WriteLine("====");
                                Console.WriteLine(text);
                                Console.WriteLine("====");
                                returnFieldQualifierName = "flag_" + flagCounter++;
                                currentLevel.Selects.Add("(" + text + ") as " + returnFieldQualifierName);
                                //currentLevel.Selects.Add("(3+3) as " + returnFieldQualifierName);
                            }
                        }
                        else if (columnDescriptor.IsBinned)
                        {
                            currentLevel.Selects.Add(
                                "floor(" + qualifier.TableQualifier + P + qualifier.FieldQualifier +
                                " / " + columnDescriptor.BinSize + ") * " + columnDescriptor.BinSize + " as " + returnFieldQualifierName);
                        }
                        else if (columnDescriptor.IsTiled)
                        {
                        }
                        else if (columnDescriptor.IsGrouped)
                        {
                            string name = columnDescriptor.GetSQLSelect(qualifier, false, false);
                            currentLevel.Selects.Add(name + " as " + returnFieldQualifierName);
                        }
                        else
                        {
                            //applyGrouping =
                            //    pair.FilterModel.ColumnDescriptors.Where(cd2 => !cd2.Equals(pair.ColumnDescriptor)).Count(cd2 => cd2.IsAnyGroupingOperationApplied()) > 0;

                            applyGrouping =
                                pair.FilterModel.ColumnDescriptors.Where(cd2 => !cd2.MatchSimple(pair.ColumnDescriptor)).Count(cd2 => cd2.IsAnyGroupingOperationApplied()) > 0;

                            List<PanoramicDataColumnDescriptor> sortedGroupedColumnDescriptors =
                                pair.FilterModel.ColumnDescriptors.Where(cd2 => cd2.IsAnyGroupingOperationApplied())
                                    .OrderBy(cd2 => columnDescriptor.Order)
                                    .ToList();

                            // if any of the operators / labels are undefiend we need to push this to the next level
                            // and add the undefined ones to the current one. 
                            bool allDefined = sortedGroupedColumnDescriptors
                                .All(cd2 => levelQualifiers.Any(q => q.ColumnDescriptor.MatchComplete(cd2) && q.Level < levelCount));

                            if (!allDefined)
                            {
                                pushUp.Add(pair);
                                pair.PushedUp = true;
                                currentLevel.ColumnDescriptors.Remove(pair);
                                List<PanoramicDataColumnDescriptor> undefineds = sortedGroupedColumnDescriptors
                                    .Where(cd2 => !levelQualifiers.Any(q => q.ColumnDescriptor.MatchComplete(cd2))).ToList();
                                currentLevel.ColumnDescriptors.AddRange(undefineds.Select(cd => new FilterModelColumnDescriptorPair(cd, pair.FilterModel)));
                                currentLevel.ColumnDescriptors = currentLevel.ColumnDescriptors.Distinct().ToList();
                                stayOnSameLevel = true;
                            }
                            else 
                            {
                                string name = columnDescriptor.GetSQLSelect(qualifier, applyGrouping, false);
                                if (applyGrouping)
                                {
                                    string overClause =  " over (partition by " +
                                            string.Join(", ", sortedGroupedColumnDescriptors.Select(cd2 =>
                                            {
                                                var qual = levelQualifiers.First(iq => iq.ColumnDescriptor.MatchComplete(cd2));
                                                return qual.TableQualifier + P + qual.FieldQualifier;
                                            })) + ")";
                                    if (columnDescriptor.DataType == DataTypeConstants.TIME)
                                    {
                                        name = name.Replace(", 0) as time)", "") + overClause + ", 0) as time)";
                                    }
                                    else
                                    {
                                        name += overClause;
                                    }
                                }
                            
                                else if (columnDescriptor.AggregateFunction != AggregateFunction.None)
                                {
                                    name = columnDescriptor.GetSQLSelect(qualifier, true, false);
                                    name += " over (partition by null)";
                                }

                                currentLevel.Selects.Add(name + " as " + returnFieldQualifierName);
                            }
                        }


                        // for all that we handeld on this level
                        pushUp.ForEach(p => p.PushedUp = true);
                        if (!pair.PushedUp)
                        {
                            levelQualifiers.Add(new QueryColumnQualifier(returnFieldQualifierName, nextTableName, columnDescriptor, levelCount));
                        }
                    }
                }

                if (pushUp.Count > 0 && !stayOnSameLevel)
                {
                    currentLevel.Parent = new MidQueryLevel();
                    currentLevel.Parent.FromTableName = nextTableName;
                    currentLevel.Parent.ColumnDescriptors = pushUp.ToArray().ToList();
                    currentLevel.Parent.ColumnDescriptors.ForEach(pair => pair.PushedUp = false);
                    pushUp.Clear();
                }
                if (!stayOnSameLevel)
                {
                    levelQualifiers.ForEach(iq => iq.TableQualifier = nextTableName);
                    currentLevel = currentLevel.Parent;
                    levelCount++;
                }
            } while (currentLevel != null);

            // assemble the level query
            string levelQuery = innerQuery;
            currentLevel = startLevel;
            do
            {
                levelQuery = "select " + NEW +
                            Indent(string.Join(C + NEW, currentLevel.Selects)) + NEW +
                            "from" + NEW +
                            "(" + NEW +
                            Indent(levelQuery) + NEW +
                            ") as " + currentLevel.FromTableName;
                currentLevel = currentLevel.Parent;
            } while (currentLevel != null);

            // switch all querycolumqulifier to the outer table name 
            levelQualifiers.ForEach(iq => iq.TableQualifier = tableName + "_" + levelCount);

            // create outer query
            List<QueryColumnQualifier> returnQueryColumnQualifiers = new List<QueryColumnQualifier>();
            List<PanoramicDataColumnDescriptor> relevantColumnDescriptors = new List<PanoramicDataColumnDescriptor>();
            List<string> outerSelectList = new List<string>();
            bool applyAggregation = outerQueryColumnDescriptors.Count(cd => cd.AggregateFunction != AggregateFunction.None) > 0;
            applyGrouping = outerQueryColumnDescriptors.Count(cd => cd.IsAnyGroupingOperationApplied()) > 0;
            int counter = 1;
            foreach (var cd in outerQueryColumnDescriptors.OrderBy(cd => cd.IsPrimaryKey))
            {
                QueryColumnQualifier qualifier = levelQualifiers.First(q => q.ColumnDescriptor.MatchSimple(cd));
                if (levelQualifiers.Any(q => q.ColumnDescriptor.MatchComplete(cd)))
                {
                    qualifier = levelQualifiers.First(q => q.ColumnDescriptor.MatchComplete(cd));
                }
                string returnFieldQualifierName = qualifier.FieldQualifier + "_" + counter++;

                // special case bin aggregates that are binned over a differnet column descriptor
                if (cd.AggregateFunction == AggregateFunction.Bin && !cd.IsBinned)
                {
                    if (levelQualifiers.Count(q => q.ColumnDescriptor.MatchSimple(cd) && q.ColumnDescriptor.IsBinned) > 0)
                    {
                        qualifier =
                            levelQualifiers.First(q => q.ColumnDescriptor.MatchSimple(cd) && q.ColumnDescriptor.IsBinned);
                    }
                }
                if (cd.AggregateFunction == AggregateFunction.None && !cd.IsGrouped && !cd.IsBinned &&
                    outerQueryColumnDescriptors.Count(cd2 => cd2.IsAnyGroupingOperationApplied() && cd2.MatchSimple(cd)) > 0)
                {
                    qualifier = levelQualifiers.First(q => q.ColumnDescriptor.MatchSimple(cd));
                    if (levelQualifiers.Count(q => q.ColumnDescriptor.MatchSimple(cd) && q.ColumnDescriptor.IsAnyGroupingOperationApplied()) > 0)
                    {
                        qualifier = levelQualifiers.First(q => q.ColumnDescriptor.MatchSimple(cd) && q.ColumnDescriptor.IsAnyGroupingOperationApplied());
                    }
                    outerSelectList.Add(
                        cd.GetSQLSelect(qualifier, false, false, false,
                            qualifier.TableQualifier + P + qualifier.FieldQualifier) + " as " +
                        returnFieldQualifierName);
                }
                else
                {
                    outerSelectList.Add(cd.GetSQLSelect(qualifier, applyGrouping, applyAggregation) + " as " +
                                        returnFieldQualifierName);
                }
                relevantColumnDescriptors.Add(cd);
                returnQueryColumnQualifiers.Add(new QueryColumnQualifier(returnFieldQualifierName, tableName, cd));
            }

            // add any grouping fields
            foreach (var cd in outerQueryColumnDescriptors.Where(cd => cd.IsAnyGroupingOperationApplied()))
            {
                QueryColumnQualifier qualifier = levelQualifiers.First(q => q.ColumnDescriptor.MatchSimple(cd));
                if (levelQualifiers.Any(q => q.ColumnDescriptor.MatchComplete(cd)))
                {
                    qualifier = levelQualifiers.First(q => q.ColumnDescriptor.MatchComplete(cd));
                }
                string name = cd.GetSQLSelect(qualifier, false, false);
                string returnFieldQualifierName = qualifier.FieldQualifier + "_" + counter++;

                outerSelectList.Add(name + " as " + returnFieldQualifierName);
                returnQueryColumnQualifiers.Add(new QueryColumnQualifier(returnFieldQualifierName, tableName, cd));
            }

            // filter where clauses
            string where = GenerateFilterWhereClause(innerQueryColumnDescriptors, levelQualifiers, addFirstFilter);

            // row number select
            List<string> rowNumbers = new List<string>();
            List<string> groupBy = new List<string>();
            List<PanoramicDataColumnDescriptor> outerSortColumnDescriptors = outerQueryColumnDescriptors.OrderBy(cd => cd.IsPrimaryKey).ThenBy(cd => cd.SortMode == SortMode.None).ThenBy(cd => cd.Order).ToList();
            foreach (var cd in outerSortColumnDescriptors)
            {
                QueryColumnQualifier qualifier = levelQualifiers.First(q => q.ColumnDescriptor.MatchSimple(cd));
                if (levelQualifiers.Any(q => q.ColumnDescriptor.MatchComplete(cd)))
                {
                    qualifier = levelQualifiers.First(q => q.ColumnDescriptor.MatchComplete(cd));
                }
                // special case bin aggregates that are binned over a differnet column descriptor
                if (cd.AggregateFunction == AggregateFunction.Bin && !cd.IsBinned)
                {
                    if (levelQualifiers.Count(q => q.ColumnDescriptor.MatchSimple(cd) && q.ColumnDescriptor.IsBinned) > 0)
                    {
                        qualifier =
                            levelQualifiers.First(q => q.ColumnDescriptor.MatchSimple(cd) && q.ColumnDescriptor.IsBinned);
                    }
                }
                string name = cd.GetSQLSelect(qualifier, false, false);

                string nameGrouped = cd.GetSQLSelect(qualifier, applyGrouping, applyAggregation, true);
                rowNumbers.Add(
                    "case when " + nameGrouped + " is null then 1 else 0 end, " +
                    nameGrouped + (cd.SortMode == SortMode.Desc ? " desc" : " asc"));

                if (cd.IsGrouped)
                {
                    groupBy.Add(name);
                }
                else if (cd.IsTiled)
                {
                    groupBy.Add(name);
                }
                else if (cd.IsBinned)
                {
                    groupBy.Add(name);
                }
            }

            // filter flags select
            List<string> filterPassFlagSelectList = new List<string>();
            flags = flags.Where(f => !f.IsEmptyFlag).ToList();
            foreach (var flag in flags)
            {
                if (applyGrouping || applyAggregation)
                {
                    filterPassFlagSelectList.Add("avg(" + tableName + "_" + levelCount + P + flag.FieldQualifier + ") as " +
                                                 flag.FieldQualifier + "_o");
                }
                else
                {
                    filterPassFlagSelectList.Add("" + tableName + "_" + levelCount + P + flag.FieldQualifier + " as " +
                                                flag.FieldQualifier + "_o");
                }
            }

            // construct final query
            string outerSelect = string.Join(C + NEW, filterPassFlagSelectList.Concat(outerSelectList));
            string rowNumber = "row_number() over (order by " + string.Join(C + S, rowNumbers) + ") as row";
            if (includeRowNumber)
            {
                outerSelect = rowNumber + C + NEW + outerSelect;
            }

            string query =
                    "select " + NEW +
                        Indent(outerSelect) + NEW +
                    "from " + NEW +
                    "(" + NEW +
                        Indent(levelQuery) + NEW +
                    ") as " + tableName + "_" + levelCount;
            
            if (where != "")
            {
                query += NEW + "where " + NEW + Indent(where);
            }
            if (groupBy.Count > 0)
            {
                query += NEW + "group by " + NEW + Indent(string.Join(C + S, groupBy));
            }

            query =
                "select * from" + NEW +
                "(" + NEW +
                Indent(query) + NEW +
                ") as " + tableName;

            if (usePageing)
            {
                query += NEW +
                    "where " + tableName + ".row between " + startIndex + " and " + (startIndex + pageCount) + ";";
            }

            columnDescriptors = relevantColumnDescriptors;
            filterModelPassFlagDescriptors = flags;
            queryColumnQualifiers = returnQueryColumnQualifiers;
            return query;
        }

        public string GenerateCountQuery()
        {
            if (GetOuterQueryColumnDescriptors().Count == 0)
            {
                return "select 0";
            }
            List<PanoramicDataColumnDescriptor> columnDescriptors = new List<PanoramicDataColumnDescriptor>();
            List<FilterModelColumnDescriptorPair> passFlagDescriptors = new List<FilterModelColumnDescriptorPair>();
            List<QueryColumnQualifier> queryColumnQualifiers = new List<QueryColumnQualifier>();
            string fetchQuery = GenerateFetchQuery(false, 0, 0, false, true, out columnDescriptors, out passFlagDescriptors, out queryColumnQualifiers);

            string query = "select count(*) from" + QueryGenerator.NEW + "(" + NEW +
                QueryGenerator.Indent(fetchQuery) + NEW + ") as ttt";
            return query;
        }

        public abstract void PostProcessRow(PanoramicDataRow row);

        public string GetSchemaName()
        {
            if (GetOuterQueryColumnDescriptors().Count > 0)
            {
                return TableModel.ColumnDescriptors.Values.ToList()[0][0].SchemaName;
            }
            else
            {
                return "";
            }
        }

        public virtual PanoramicDataRow ConvertToPanoramicDataRow(List<object> row,
            List<PanoramicDataColumnDescriptor> columnDescriptors,
            List<FilterModelColumnDescriptorPair> filterModelPassFlagDescriptors)
        {
            PanoramicDataRow panoramicDataRow = new PanoramicDataRow();
            // set rownumber (should always be the first column)
            panoramicDataRow.RowNumber = (long)row[0];
            bool isGrouped = columnDescriptors.Count(cd => cd.IsAnyGroupingOperationApplied()) > 0;

            int colCount = 0;
            foreach (var flagDescriptor in filterModelPassFlagDescriptors)
            {
                panoramicDataRow.PassesFilterModel.Add(flagDescriptor.FilterModel, double.Parse(row[colCount + 1].ToString()));
                colCount++;
            }

            foreach (var columnDescriptor in columnDescriptors.Where(cd => !cd.IsPrimaryKey))
            {
                PanoramicDataValue dv = new PanoramicDataValue();
                dv.DataType = columnDescriptor.DataType;
                dv.Value = row[colCount + 1]; // ignore rownumber() column

                double d = 0.0;
                if (double.TryParse(dv.Value.ToString(), out d))
                {
                    dv.StringValue = dv.Value.ToString().Contains(".") ? d.ToString("N") : dv.Value.ToString();
                    if (columnDescriptor.AggregateFunction == AggregateFunction.Bin)
                    {
                        dv.StringValue = d + " - " + (d + columnDescriptor.BinSize);
                    }
                    else if (dv.DataType == DataTypeConstants.BIT)
                    {
                        if (d == 1.0)
                        {
                            dv.StringValue = "True";
                        }
                        else if (d == 0.0)
                        {
                            dv.StringValue = "False";
                        }
                    }
                }
                else
                {
                    dv.StringValue = dv.Value.ToString();
                    if (dv.Value is DateTime)
                    {
                        dv.StringValue = ((DateTime) dv.Value).ToShortDateString();
                    }
                }

                if (dv.DataType == DataTypeConstants.GEOGRAPHY)
                {

                    string toSplit = dv.StringValue;
                    if (toSplit.Contains("(") && toSplit.Contains(")"))
                    {
                        toSplit = toSplit.Substring(toSplit.IndexOf("("));
                        toSplit = toSplit.Substring(1, toSplit.IndexOf(")") - 1);
                    }
                    dv.ShortStringValue = dv.StringValue.Replace("(" +toSplit+")", "");
                }
                else
                {
                    dv.ShortStringValue = dv.StringValue.TrimTo(300);
                }
                if (!panoramicDataRow.ColumnValues.ContainsKey(columnDescriptor))
                {
                    panoramicDataRow.ColumnValues.Add(columnDescriptor, dv);
                    panoramicDataRow.Values.Add(dv);
                }
                colCount++;

                // uncomment if xml_concat is on instead of group concat
                /*if (isGrouped && 
                    (columnDescriptor.AggregateFunction == AggregateFunction.Concat || columnDescriptor.AggregateFunction == AggregateFunction.None) &&
                    !(columnDescriptor.AggregateFunction == AggregateFunction.None && columnDescriptor.IsAnyGroupingOperationApplied()))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(dv.Value as string);
                    XmlNode xmlRow = doc.ChildNodes[0];
                    StringBuilder sb = new StringBuilder();
                    foreach (XmlElement e in xmlRow.ChildNodes)
                    {
                        sb.Append(e.InnerText + ", ");
                    }
                    dv.StringValue = sb.Remove(sb.Length - 2, 2).ToString();
                }*/
            }
            foreach (var columnDescriptor in columnDescriptors.Where(cd => cd.IsPrimaryKey))
            {
                if (colCount + 1 < row.Count)
                {
                    PanoramicDataValue dv = new PanoramicDataValue();
                    dv.DataType = columnDescriptor.DataType;
                    dv.Value = row[colCount + 1]; // ignore rownumber() column
                    dv.StringValue = dv.Value.ToString();

                    panoramicDataRow.PrimaryKeyValues.Add(columnDescriptor, dv);
                    colCount++;
                }
            }
            foreach (var columnDescriptor in columnDescriptors.Where(cd => cd.IsAnyGroupingOperationApplied()))
            {
                if (colCount + 1 < row.Count)
                {
                    PanoramicDataValue dv = new PanoramicDataValue();
                    dv.DataType = columnDescriptor.DataType;
                    dv.Value = row[colCount + 1]; // ignore rownumber() column

                    double d = 0.0;
                    if (double.TryParse(dv.Value.ToString(), out d))
                    {
                        dv.StringValue = dv.Value.ToString().Contains(".") ? d.ToString("N") : dv.Value.ToString();
                    }
                    else
                    {
                        dv.StringValue = dv.Value.ToString();
                    }
                    if (!panoramicDataRow.GroupedValues.ContainsKey(columnDescriptor))
                    {
                        panoramicDataRow.GroupedValues.Add(columnDescriptor, dv);
                        panoramicDataRow.Values.Add(dv);
                    }
                    colCount++;
                }
            }
            return panoramicDataRow;
        }

        public static string Indent(string text)
        {
            string ret = TAB + text.Replace(NEW, NEW + TAB);
            if (ret.EndsWith("\t"))
            {
                ret = ret.Remove(ret.Count() - 1);
            }
            return ret;
        }

        private void recurisvePathInfoGraphJoin(Node currentNode, Node parentNode, TableDependency edgeDependency, List<string> froms, List<string> clauses)
        {
            froms.Add(currentNode.TableInfo.SchemaName + P + currentNode.TableInfo.Name + " as " + currentNode.TableIdentifier);

            if (parentNode != null)
            {
                clauses.Add(parentNode.TableIdentifier + P + edgeDependency.FromFieldInfo.Name + " = " +
                            currentNode.TableIdentifier + P + edgeDependency.ToFieldInfo.Name);
            }
            else
            {
                foreach (var dep in currentNode.Children.Keys)
                {
                    foreach (var node in currentNode.Children[dep])
                    {
                        recurisvePathInfoGraphJoin(node, currentNode, dep, froms, clauses);
                    }
                }
            }
        }

        private string generateInnerQuery(out List<PanoramicDataColumnDescriptor> fieldDescriptors,
            out List<QueryColumnQualifier> qualifiers,
            List<FilterModelColumnDescriptorPair> filterColumnDescriptors)
        {
            fieldDescriptors = new List<PanoramicDataColumnDescriptor>();
            qualifiers = new List<QueryColumnQualifier>();

            Node rootNode = null;
            Node currentNode = null;
            int count = 0;
            Dictionary<PanoramicDataGroupDescriptor, string> tableNames = new Dictionary<PanoramicDataGroupDescriptor, string>();
            foreach (var panoramicDataGroupDescriptor in TableModel.ColumnDescriptors.Keys)
            {
                if (panoramicDataGroupDescriptor is PathInfo)
                {
                    PathInfo pathInfo = panoramicDataGroupDescriptor as PathInfo;
                    if (currentNode == null && pathInfo.Path.Count == 0)
                    {
                        rootNode = new Node("t" + count++, pathInfo.TableInfo);
                        currentNode = rootNode;
                    }
                    foreach (var dep in pathInfo.Path)
                    {
                        if (currentNode == null)
                        {
                            rootNode = new Node("t" + count++, dep.FromTableInfo);
                            currentNode = rootNode;
                        }
                        if (!currentNode.Children.ContainsKey(dep))
                        {
                            currentNode.Children.Add(dep, new List<Node>());
                        }
                        Node newNode = new Node("t" + count++, dep.ToTableInfo);
                        currentNode.Children[dep].Add(newNode);
                        currentNode = newNode;
                    }
                    if (!tableNames.ContainsKey(pathInfo))
                    {
                        tableNames.Add(pathInfo, currentNode.TableIdentifier);
                    }
                    currentNode = rootNode;
                }
            }

            List<string> froms = new List<string>();
            List<string> wheres = new List<string>();
            List<string> selects = new List<string>();
            recurisvePathInfoGraphJoin(rootNode, null, null, froms, wheres);

            int pathCount = 0;
            foreach (var panoramicDataGroupDescriptor in TableModel.ColumnDescriptors.Keys)
            {
                if (panoramicDataGroupDescriptor is PathInfo)
                {
                    PathInfo pathInfo = panoramicDataGroupDescriptor as PathInfo;
                    string tableName = tableNames[panoramicDataGroupDescriptor];
                    List<PrimaryKeyReference> primaryKeyReferences = calculateRelevantPrimaryKeys(pathInfo);
                    List<PanoramicDataColumnDescriptor> currentColumnDescriptors =
                        new List<PanoramicDataColumnDescriptor>();
                    List<QueryColumnQualifier> currentQualifiers = new List<QueryColumnQualifier>();

                    List<FilterModelColumnDescriptorPair> currentFilterColumnDescriptorPairs =
                        filterColumnDescriptors.Where(
                            pair => pair.ColumnDescriptor.PanoramicDataGroupDescriptor.Equals(pathInfo)).ToList();
                    List<PanoramicDataColumnDescriptor> currentFilterColumnDescriptors =
                        currentFilterColumnDescriptorPairs.Select(pair => pair.ColumnDescriptor).ToList();

                    calculateFieldNames(out currentColumnDescriptors, out currentQualifiers, pathCount++, tableName,
                        primaryKeyReferences, TableModel.ColumnDescriptors[pathInfo], currentFilterColumnDescriptors,
                        pathInfo);

                    selects.AddRange(currentQualifiers.Select(
                        q => q.TableQualifier + P + q.ColumnDescriptor.Name + " as " + q.FieldQualifier));

                    foreach (var columnDescriptor in currentColumnDescriptors)
                    {
                        if (!fieldDescriptors.Contains(columnDescriptor))
                        {
                            fieldDescriptors.Add(columnDescriptor);
                        }
                    }
                    foreach (var qualifier in currentQualifiers)
                    {
                        if (!qualifiers.Contains(qualifier))
                        {
                            qualifiers.Add(qualifier);
                        }
                    }
                }
            }
            qualifiers.ForEach(q => q.TableQualifier = "t_inner");

            string query =
                "select" + NEW +
                    Indent(string.Join(C + NEW, selects)) + NEW +
                "from" + NEW +
                    Indent(string.Join(C + NEW, froms));

            if (wheres.Count > 0)
            {
                query +=
                   NEW +
                   "where" + NEW +
                       Indent(string.Join(S + "and" + NEW, wheres));
            }

            /*query =
                "(" + NEW +
                    Indent(query) +
                ") as t_inner";*/
            return query;
        }

        private List<PrimaryKeyReference> calculateRelevantPrimaryKeys(PathInfo pathInfo)
        {
            List<PrimaryKeyReference> relevantPrimaryKeys = new List<PrimaryKeyReference>();

            PathInfo currentPath = pathInfo;
            while (currentPath.Path.Count > 0)
            {
                relevantPrimaryKeys.Add(new PrimaryKeyReference(currentPath.TableInfo.PrimaryKeyFieldInfo, currentPath));
                currentPath = currentPath.LevelUp();
            }

            relevantPrimaryKeys.Add(new PrimaryKeyReference(currentPath.TableInfo.PrimaryKeyFieldInfo, currentPath));
            return relevantPrimaryKeys;
        }

        private void calculateFieldNames(
            out List<PanoramicDataColumnDescriptor> fieldDescriptors,
            out List<QueryColumnQualifier> qualifiers,
            int pathCount,
            string tableName,
            List<PrimaryKeyReference> relevantPrimaryKeys,
            List<DatabaseColumnDescriptor> relevantColumns,
            List<PanoramicDataColumnDescriptor> filterColumnDescriptors,
            PathInfo relevantFieldsPathInfo)
        {
            fieldDescriptors = new List<PanoramicDataColumnDescriptor>();
            qualifiers = new List<QueryColumnQualifier>();

            int counter = 0;
            foreach (var pk in relevantPrimaryKeys)
            {
                string name = pk.PrimaryKeyField.Name + "_" + pathCount + "_" + (counter + 1);
                PanoramicDataColumnDescriptor fc = new DatabaseColumnDescriptor(pk.PrimaryKeyField, pk.PanoramicDataGroupDescriptor as PathInfo, true);
                fieldDescriptors.Add(fc);
                qualifiers.Add(new QueryColumnQualifier(name, tableName, fc));
                counter++;
            }
            foreach (var cd in relevantColumns)
            {
                string name = cd.Name + "_" + pathCount + "_" + (counter + 1);
                QueryColumnQualifier qualifier = new QueryColumnQualifier(name, tableName, cd);
                if (!fieldDescriptors.Contains(cd))
                {
                    fieldDescriptors.Add(cd);
                    counter++;
                }
                if (!qualifiers.Contains(qualifier))
                {
                    qualifiers.Add(qualifier);
                }
            }
            foreach (var cd in filterColumnDescriptors)
            {
                string name = cd.Name + "_" + pathCount + "_" + (counter + 1);
                QueryColumnQualifier qualifier = new QueryColumnQualifier(name, tableName, cd);
                if (fieldDescriptors.Where(fd => fd.MatchSimple(cd)).Count() == 0)
                {
                    if (!fieldDescriptors.Contains(cd))
                    {
                        fieldDescriptors.Add(cd);
                        counter++;
                    }
                    if (!qualifiers.Contains(qualifier))
                    {
                        qualifiers.Add(qualifier);
                    }
                }
            }
        }
    }

    public class MidQueryLevel
    {
        public List<FilterModelColumnDescriptorPair> ColumnDescriptors { get; set; }
        public MidQueryLevel Parent { get; set; }
        public List<string> Selects { get; set; }
        public string FromTableName { get; set; }

        public MidQueryLevel()
        {
            ColumnDescriptors = new List<FilterModelColumnDescriptorPair>();
            Selects = new List<string>();
            Selects.Add("*");
        }
    }

    public class FilterModelColumnDescriptorPair
    {
        public PanoramicDataColumnDescriptor ColumnDescriptor { get; set; }
        public FilterModel FilterModel { get; set; }
        public String FieldQualifier { get; set; }
        public String Query { get; set; }
        public bool PushedUp { get; set; }
        public bool IsFlag { get; set; }
        public bool IsEmptyFlag { get; set; }

        public FilterModelColumnDescriptorPair()
        {
            PushedUp = false;
            IsFlag = false;
            IsEmptyFlag = true;
        }
        public FilterModelColumnDescriptorPair(PanoramicDataColumnDescriptor columnDescriptor, FilterModel filterModel)
        {
            ColumnDescriptor = columnDescriptor;
            FilterModel = filterModel;
        }

        public override int GetHashCode()
        {
            int code = 0;
            code ^= FilterModel.GetHashCode();
            if (ColumnDescriptor != null)
            {
                code ^= ColumnDescriptor.GetHashCode();
            }
            return code;
        }

        public override bool Equals(object obj)
        {
            if (obj is FilterModelColumnDescriptorPair)
            {
                FilterModelColumnDescriptorPair that = obj as FilterModelColumnDescriptorPair;

                return this.FilterModel.Equals(that.FilterModel) && 
                    ((this.ColumnDescriptor == null && that.ColumnDescriptor == null) || this.ColumnDescriptor.Equals(that.ColumnDescriptor));
            }
            return false;
        }
    }

    public class PrimaryKeyReference
    {
        public FieldInfo PrimaryKeyField { get; set; }
        public PanoramicDataGroupDescriptor PanoramicDataGroupDescriptor { get; set; }

        public PrimaryKeyReference(FieldInfo fieldInfo, PanoramicDataGroupDescriptor panoramicDataGroupDescriptor)
        {
            PrimaryKeyField = fieldInfo;
            PanoramicDataGroupDescriptor = panoramicDataGroupDescriptor;
        }

        public override int GetHashCode()
        {
            int code = 0;
            code ^= PrimaryKeyField.GetHashCode();
            code ^= PanoramicDataGroupDescriptor.GetHashCode();
            return code;
        }

        public override bool Equals(object obj)
        {
            if (obj is PrimaryKeyReference)
            {
                PrimaryKeyReference that = obj as PrimaryKeyReference;

                var bb = this.PanoramicDataGroupDescriptor.Equals(that.PanoramicDataGroupDescriptor);

                if (this.PrimaryKeyField == that.PrimaryKeyField &&
                    this.PanoramicDataGroupDescriptor.Equals(that.PanoramicDataGroupDescriptor))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public class Node
    {
        public string TableIdentifier { get; set; }
        public TableInfo TableInfo { get; set; }
        public Dictionary<TableDependency, List<Node>> Children { get; set; }

        public Node(string tableIdentifier, TableInfo tableInfo = null)
        {
            TableIdentifier = tableIdentifier;
            TableInfo = tableInfo;
            Children = new Dictionary<TableDependency, List<Node>>();
        }
    }
}
