using starPadSDK.MathExpr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PanoramicData.utils.inq
{
    class FormulaEvaluator
    {
        private static BuiltInEngine _builtInEngine = new BuiltInEngine();
        private static List<Label> _labels = new List<Label>();

        public static Expr Calculate(Expr e)
        {
            List<Label> used = new List<Label>();
            Expr exprWithFunctionsReplaced = recursiveReplaceFunctions(e, used);
            used = new List<Label>();
            return _builtInEngine.Numericize(recursiveReplaceLabels(exprWithFunctionsReplaced, used, true));
        }

        public static Expr GetDisplayableExpr(Expr e)
        {
            List<Label> used = new List<Label>();
            return recursiveReplaceLabels(e, used, false);
        }

        public static void GetValue(Expr e, out string text, out double number)
        {
            text = "";
            number = 0.0;
            if (e is DoubleNumber)
            {
                text = String.Format("{0:0.##}", (e as DoubleNumber).Num);
                number = (e as DoubleNumber).Num;
            }
            else if (e is IntegerNumber)
            {
                text = String.Format("{0:0.##}", (e as IntegerNumber).Num.AsDouble());
                number = (e as IntegerNumber).Num.AsDouble();
            }
            else if (e is WordSym)
            {
                text = (e as WordSym).Word;
            }
            else if (e is CompositeExpr)
            {
                Expr res = FormulaEvaluator.Calculate(e);
                if (res is DoubleNumber)
                {
                    text = String.Format("{0:0.##}", (res as DoubleNumber).Num);
                    number = (res as DoubleNumber).Num;
                }
                else if (res is IntegerNumber)
                {
                    text = String.Format("{0:0.##}", (res as IntegerNumber).Num.AsDouble());
                    number = (res as IntegerNumber).Num.AsDouble();
                }
            }

            if (text == "")
            {
                text = "error";
            }
        }

        public static void AddLabel(Label l)
        {
            _labels.Add(l);
        }

        public static void RemoveLabel(Label l)
        {
            _labels.Remove(l);
        }

        public static void ClearAllLabels()
        {
            foreach (var l in _labels.ToArray())
            {
                l.FireLabelProviderDeleted();
            }
        }

        private static Expr recursiveReplaceFunctions(Expr e, List<Label> used)
        {
            List<CompositeExpr> toReplace = new List<CompositeExpr>();
            findFunctions(e, toReplace);

            Expr clone = e.Clone();

            foreach (CompositeExpr function in toReplace)
            {
                if (function.Args().Count() > 0)
                {
                    if (function.Args()[0] is WordSym)
                    {
                        WordSym ws = function.Args()[0] as WordSym;
                        Guid guid;
                        if (Guid.TryParse(ws.Word, out guid))
                        {
                            foreach (Label label in _labels)
                            {
                                if (label.ID == guid)
                                {
                                    Expr value = GetFunctionValue(function.Head as WellKnownSym, label);
                                    if (used.Contains(label))
                                    {
                                        return new ErrorMsgExpr("circular reference");
                                    }
                                    else
                                    {
                                        used.Add(label);
                                        clone = _builtInEngine._Substitute(clone, function, recursiveReplaceFunctions(value, used));
                                    }
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        return new ErrorMsgExpr("illegal spreadsheet function 2");
                    }
                }
                else
                {
                    return new ErrorMsgExpr("illegal spreadsheet function 2");
                }
            }

            return clone;
        }

        private static void findFunctions(Expr e, List<CompositeExpr> found)
        {
            if (e is CompositeExpr && (((CompositeExpr)e).Head == WellKnownSym.sum || ((CompositeExpr)e).Head == WellKnownSym.avg))
            {
                found.Add(e as CompositeExpr);
            }
            else
            {
                if (e.Args() != null)
                {
                    foreach (Expr ee in e.Args())
                    {
                        findFunctions(ee, found);
                    }
                }
            }
        }

        public static Expr GetFunctionValue(WellKnownSym functionType, Label label)
        {
            return label.LabelProvider.GetFunctionValue(functionType, label, label.LabelConsumer);
        }

        private static Expr recursiveReplaceLabels(Expr e, List<Label> used, bool calculate)
        {
            List<WordSym> toReplace = new List<WordSym>();
            findLabels(e, toReplace);

            Expr clone = e.Clone();

            foreach (WordSym ws in toReplace)
            {
                Guid guid;
                if (Guid.TryParse(ws.Word, out guid))
                {
                    foreach (Label label in _labels)
                    {
                        if (label.ID == guid)
                        {
                            Expr value = null;
                            if (calculate)
                            {
                                value = GetLabelValue(e, label);
                                if (value is WordSym)
                                {
                                    if ((value as WordSym).Word == "")
                                    {
                                        value = new DoubleNumber(0);
                                    }
                                }
                            }
                            else
                            {
                                value = new WordSym("(ref)");
                            }
                            if (used.Contains(label))
                            {
                                return new ErrorMsgExpr("circular reference");
                            }
                            else
                            {
                                used.Add(label);
                                clone = _builtInEngine._Substitute(clone, ws, recursiveReplaceLabels(value, used, calculate));
                            }
                            break;
                        }
                    }
                }
                else
                {
                    if (ws.Word == "")
                    {
                        clone = _builtInEngine._Substitute(clone, ws, recursiveReplaceLabels(new DoubleNumber(0), used, calculate));
                    }
                }
            }
            return clone;
        }

        private static void findLabels(Expr e, List<WordSym> found)
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

        public static Expr GetLabelValue(Expr e, Label label)
        {
            return label.LabelProvider.GetLabelValue(label, label.LabelConsumer);
        }

    }
    public enum DisplayMode { Edit, Result }
    public enum FormulaOperator { Sum, Average, Count, Value, Values }
}
