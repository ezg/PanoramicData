using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PanoramicDataModel;
using starPadSDK.MathExpr;
using PanoramicData.model.view;

namespace PanoramicData.controller.sql
{
    public class SqlExprEvaluator
    {
        private CalculatedColumnDescriptorInfo _descriptorInfo = null;
        public SqlExprEvaluator(CalculatedColumnDescriptorInfo info)
        {
            _descriptorInfo = info;
        }

        public string GetQuery(List<QueryColumnQualifier> qualifiers)
        {
            string query = recursiveGetQuery(_descriptorInfo.Expr, qualifiers);
            if (query == "" || query.Contains("error"))
            {
                return "cast(null AS float)";
            }
            return query;
        }

        private string recursiveGetQuery(Expr e, List<QueryColumnQualifier> qualifiers)
        {
            string text = "";
            if (e is DoubleNumber)
            {
                text = String.Format("{0:0.##}", (e as DoubleNumber).Num);
                if (double.IsNaN((e as DoubleNumber).Num) ||
                    double.IsInfinity((e as DoubleNumber).Num))
                {
                    text = "cast(null AS float)";
                }
            }
            else if (e is IntegerNumber)
            {
                text = String.Format("{0:0.##}", (e as IntegerNumber).Num.AsDouble());
                if (double.IsNaN((e as IntegerNumber).Num.AsDouble()) ||
                    double.IsInfinity((e as IntegerNumber).Num.AsDouble()))
                {
                    text = "cast(null AS float)";
                }
            }
            else if (e is WordSym)
            {
                PanoramicDataColumnDescriptor columnDescriptor = getColumnDescriptorForWordSym(e as WordSym);
                if (columnDescriptor != null)
                {
                    QueryColumnQualifier qualifier = qualifiers.First(q => q.ColumnDescriptor.MatchSimple(columnDescriptor));
                    bool applyAggregation = columnDescriptor.AggregateFunction != AggregateFunction.None;//_descriptorInfo.ColumnDescriptors.Count(cd => cd.AggregateFunction != AggregateFunction.None) > 0;
                    bool applyGrouping = _descriptorInfo.ColumnDescriptors.Count(cd => cd.IsAnyGroupingOperationApplied()) > 0;
                    string name = qualifier.TableQualifier + "." + qualifier.FieldQualifier;
                    text = columnDescriptor.GetSQLSelect(qualifier, applyGrouping, applyAggregation, false,
                        "(case when isnumeric(" + name + ") = 1 then convert(float, " + name + ") else null end)");

                    if (columnDescriptor.DataType == AttributeDataTypeConstants.TIME)
                    {
                        text = columnDescriptor.GetSQLSelect(qualifier, applyGrouping, applyAggregation, false,
                            "(case when isnumeric(DATEDIFF(second, 0," + name + ") / 60.0) = 1 then convert(float, DATEDIFF(second, 0," + name + ") / 60.0) else null end)");
                    }

                    if (applyGrouping)
                    {
                        text += " over (partition by " +
                                string.Join(", ", _descriptorInfo.ColumnDescriptors.Where(cd => cd.IsAnyGroupingOperationApplied()).Select(cd2 =>
                                {
                                    var qual = qualifiers.First(iq => iq.ColumnDescriptor.MatchSimple(cd2));
                                    return qual.TableQualifier + "." + qual.FieldQualifier;
                                })) + ")";
                    }
                    else if (columnDescriptor.AggregateFunction != AggregateFunction.None)
                    {
                         text += " over (partition by null)";
                    }
                }
            }
            else if (e is CompositeExpr)
            {
                Expr comp = e as CompositeExpr;
                if (comp.Head() is WellKnownSym)
                {
                    List<string> textArgs = e.Args().Select(a => "(" + recursiveGetQuery(a, qualifiers) + ")").ToList();
                    string op = "";
                    if ((comp.Head() as WellKnownSym).ID == WKSID.plus)
                    {
                        op = "+";
                        text = string.Join("", textArgs.Select(t => op + t));
                    } 
                    else if ((comp.Head() as WellKnownSym).ID == WKSID.minus)
                    {
                        op = "-";
                        text = string.Join("", textArgs.Select(t => op + t));
                    }
                    else if ((comp.Head() as WellKnownSym).ID == WKSID.power &&
                        textArgs.Count == 2)
                    {
                        text = "power(" + textArgs[0] + "," + textArgs[1] + ")";
                    }
                    else if ((comp.Head() as WellKnownSym).ID == WKSID.sin &&
                        textArgs.Count == 1)
                    {
                        text = "sin(" + textArgs[0] + ")";
                    }
                    else if ((comp.Head() as WellKnownSym).ID == WKSID.cos &&
                        textArgs.Count == 1)
                    {
                        text = "cos(" + textArgs[0] + ")";
                    }
                    else if ((comp.Head() as WellKnownSym).ID == WKSID.log &&
                        textArgs.Count == 1)
                    {
                        text = "log(" + textArgs[0] + ")";
                    }
                    else if ((comp.Head() as WellKnownSym).ID == WKSID.times)
                    {
                        op = "*";
                        textArgs.Insert(0, "1.0");
                        text = string.Join(op, textArgs);
                    }
                    else if ((comp.Head() as WellKnownSym).ID == WKSID.divide)
                    {
                        op = "/";
                        //textArgs.Insert(0, "1.0");
                        text = "1.0/NULLIF(" + textArgs[0] + ", 0.0)";
                    }
                }
            }

            if (text == "")
            {
                text = "error";
            }
            return text;
        }

        public List<PanoramicDataColumnDescriptor> FindLabels(Expr e)
        {
            List<WordSym> wordSyms = new List<WordSym>();
            findLabels(e, wordSyms);
            List<PanoramicDataColumnDescriptor> cds = new List<PanoramicDataColumnDescriptor>();

            foreach (var wordSym in wordSyms)
            {
                PanoramicDataColumnDescriptor cd = getColumnDescriptorForWordSym(wordSym);
                if (cd != null)
                {
                    cds.Add(cd);
                }
            }

            return cds;
        }

        private PanoramicDataColumnDescriptor getColumnDescriptorForWordSym(WordSym wordSym)
        {
            Guid guid;
            if (Guid.TryParse(wordSym.Word, out guid))
            {
                return _descriptorInfo.GetColumnDescriptorForLabelGuid(guid);
            }
            return null;
        }

        private void findLabels(Expr e, List<WordSym> found)
        {
            if (e is WordSym)
            {
                found.Add(e as WordSym);
            }
            else
            {
                if (e.Args() != null)
                {
                    foreach (Expr ee in e.Args())
                    {
                        findLabels(ee, found);
                    }
                }
            }
        }
    }
}
