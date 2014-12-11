using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using starPadSDK.CharRecognizer;
using starPadSDK.UnicodeNs;
using starPadSDK.MathRecognizer;
using starPadSDK.MathExpr;
using Microsoft.Ink;
using Constant = starPadSDK.MathExpr.MathConstant;
using ParseResult = starPadSDK.MathRecognizer.Parser.ParseResult;
using starPadSDK.Inq;
using starPadSDK.Utils;
using System.Diagnostics;

namespace starPadSDK.AppLib {
    public class Evaluator {
        static Constant[] constants = new Constant[] { /*new Constant(new LetterSym('g'), new DoubleNumber(9.8))*/ }; // temporarily commented out for NIST study
        static public Expr SubstMathVars(Constant[] substitutes, Expr toEval) {
            try {
                for(int i = 0; i < substitutes.Length; i++)
                    toEval = Engine.Substitute(toEval, substitutes[i].Name, substitutes[i].Value.Clone());
                return toEval;
            } catch(Exception) {
                return new NullExpr();
            }
        }

        static public Expr EvaluateExprFunc(Expr expr, Constant[] substitutes, bool approximate) {
            if(substitutes!=null) for(int i = 0; i < substitutes.Length; i++)
                    expr = Engine.Substitute(expr, substitutes[i
                        ].Name, substitutes[i].Value.Clone());
            Expr exprplus = Engine.Substitute(expr, WellKnownSym.plusminus, WellKnownSym.plus);
            if(exprplus != expr) {
                Expr exprminus = Engine.Substitute(expr, WellKnownSym.plusminus, WellKnownSym.minus);
                return new CompositeExpr(new WordSym("brace"), Engine.Approximate(Engine.Simplify(exprplus)), Engine.Approximate(Engine.Simplify(exprminus)));
            }
            return approximate ? Engine.Approximate(expr) : Engine.Simplify(expr);  // function evaluation
        }


        static public Expr EvalStmt(Expr toEval, List<Expr> variables, List<Expr> values) {
            if(toEval is IntegerNumber) return toEval;
            if(toEval is LetterSym) {
                if(variables.IndexOf(toEval) == -1) return null;
                return values[variables.IndexOf(toEval)];
            }
            Expr final = toEval;
            for(int k = 0; k < variables.Count; k++)
                final = Engine.Substitute(final, variables[k], values[k].Clone());
            return EvaluateExprFunc(final.Clone(), constants, false);
        }

        static public Expr genMMA(Expr ep0, ArrayExpr ep1, String op, Parser.ParseResult pr) {
            if(op == "All") {
                if(!(ep0 is CompositeExpr)) return ep0;
                CompositeExpr ce = ep0 as CompositeExpr;
                if(ce.Head == WellKnownSym.power && ce.Args[1] is WordSym && ce.Args[0] is ArrayExpr) {
                    pr.matrixOperationResult = new CompositeExpr(ce.Args[1], new ArrayExpr((Array)(ce.Args[0] as ArrayExpr).Elts.Clone()));
                    return pr.matrixOperationResult;
                }
                int c = ce.Args.Length;
                Expr[] newExprElts = new Expr[c];
                for(int i = 0; i < c; i++)
                    newExprElts[i] = genMMA(ce.Args[i], null, "All", pr);
                return new CompositeExpr(ce.Head, newExprElts);
            }
            Expr newExpr = new CompositeExpr(new WordSym(op), new ArrayExpr((Array)ep1.Elts.Clone()));
            if(ep0.Equals(ep1) && op == "No Matrix Operation") return ep1;
            else if(ep0.Equals(ep1) && op != "No Matrix Operation") return newExpr;
            if(ep0 is CompositeExpr) {
                CompositeExpr ce = ep0 as CompositeExpr;
                if(ce.Head == new LetterSym('⇒') || ce.Head == new LetterSym('→')) {
                    if(ce.Args[0].Equals(ep1) || ce.Args[0] is CompositeExpr && (ce.Args[0] as CompositeExpr).Head == WellKnownSym.power &&
                        (ce.Args[0] as CompositeExpr).Args[0].Equals(ep1))
                        return op != "No Matrix Operation" ? newExpr : ep1;
                    else return genMMA(ce.Args[0], ep1, op, null);
                } else if(ce.Head == WellKnownSym.power && ce.Args[0] is ArrayExpr) {
                    if(ce.Args[0].Equals(ep1))
                        return op != "No Matrix Operation" ? new CompositeExpr(new WordSym(op), ce.Args[0]) : ce.Args[0];
                    else if(!(ce.Args[1] is WordSym)) return ce;
                    else return (ce.Args[1] as WordSym).Word != "No Matrix Operation" ? new CompositeExpr(ce.Args[1], ce.Args[0]) : ce.Args[0];
                }
                int c = (ep0 as CompositeExpr).Args.Length;
                Expr[] newExprElts = new Expr[c];
                for(int i = 0; i < c; i++)
                    newExprElts[i] = genMMA((ep0 as CompositeExpr).Args[i], ep1, op, null);
                return new CompositeExpr((ep0 as CompositeExpr).Head, newExprElts);
            }
            return ep0;
        }
        static public Expr SubstituteExprFunc(Expr expr, Constant[] substitutes) {
            if (substitutes != null)
                expr = Engine.Substitute(expr, substitutes);
            Expr exprplus = Engine.Substitute(expr, WellKnownSym.plusminus, WellKnownSym.plus);
            if (exprplus != expr) {
                Expr exprminus = Engine.Substitute(expr, WellKnownSym.plusminus, WellKnownSym.minus);
                return new CompositeExpr(new WordSym("brace"), exprplus, exprminus);
            }
            return expr;
        }

        static public void UpdateMath(IEnumerable<Parser.ParseResult> results) {
            foreach(Parser.ParseResult pr in results) if(pr != null && pr.expr is CompositeExpr && !pr.parseError) {
                    CompositeExpr ce = pr.expr as CompositeExpr;
# if false             
                if (/*(pr.parseError && ce.Head != WellKnownSym.equals && ce.Args.Length != 1) ||*/
                                      (ce.Head == WellKnownSym.equals && (ce.Args.Length != 2 || ce.Args[0] is Parse2.SyntaxErrorExpr ||
                                               !(ce.Args[1] is Parse2.SyntaxErrorExpr)))) {
                    if (ce.Args.Length > 1) {
                        Expr theMath = SubstMathVars(results, ce.Args[1]);
                        pr.isNum = (Engine.Current is BuiltInEngine) ? (Engine.Current as BuiltInEngine).IsNum(theMath) : false;
                    }
                    continue;
                }
#endif
                    if(ce != null && (/*ce.Head == WellKnownSym.equals || */ce.Head == new LetterSym(Unicode.R.RIGHTWARDS_DOUBLE_ARROW) || ce.Head == new LetterSym('→'))) {
                        Expr theMath = SubstMathVars(results, ce.Args[0]);
                        //pr.finalSimp = ce.Head == new LetterSym('→') ? Engine.Simplify(theMath.Clone()) : EvaluateExprFunc(theMath.Clone(), constants, true);
                        pr.finalSimp = ce.Head == new LetterSym('→') ? Engine.Simplify(genMMA(theMath.Clone(), null, "All", pr)) : EvaluateExprFunc(genMMA(theMath.Clone(), null, "All", pr), constants, true);
                        if(pr.matrixOperationResult != null) pr.matrixOperationResult = pr.finalSimp;
                        pr.isNum = (Engine.Current is BuiltInEngine) ? (Engine.Current as BuiltInEngine).IsNum(pr.finalSimp) : false;
                    } else {
                        //Expr theMath = SubstMathVars(results, pr.expr);
                        //pr.isNum = (Engine.Current is BuiltInEngine) ? (Engine.Current as BuiltInEngine).IsNum(theMath) : false;
                    }
                } else {
                    //Expr theMath = SubstMathVars(results, pr.expr);
                    //pr.isNum = (Engine.Current is BuiltInEngine) ? (Engine.Current as BuiltInEngine).IsNum(theMath) : false;
                }
        }
        static public Expr SubstMathVars(IEnumerable<Parser.ParseResult> results, Expr toEval) {
            List<Expr> variables = new List<Expr>();
            List<Expr> values = new List<Expr>();
            try {
                foreach(var i in results) {
                    CompositeExpr ce2 = i.expr as CompositeExpr;
                    if(ce2 != null && !i.parseError && ce2.Head == WellKnownSym.equals && ce2.Args[0] is LetterSym) {
                        Expr val = ce2.Args[1].Clone();
                        for(int k = 0; k < variables.Count; k++) {
                            for(int k2 = 0; k2 < variables.Count; k2++)
                                if(k2 != k) {
                                    Expr oVal = values[k2];
                                    oVal = Engine.Substitute(oVal, variables[k], values[k].Clone());
                                    values[k2] = oVal;
                                }
                            val = Engine.Substitute(val, variables[k], values[k].Clone());
                        }
                        variables.Add(ce2.Args[0]);
                        values.Add(val);
                    }
                }
                Expr final = toEval;
                final = SubstSubstript(variables, values, final);
                return final;
            } catch(Exception) {
            }
            return new NullExpr();
        }
        static public Expr SubstMathVarsBegin(IEnumerable<ParseResult> results, Expr toEval) {
            return SubstMathVars(results, toEval);
        }

        private static Expr SubstSubstript(List<Expr> variables, List<Expr> values, Expr final) {
            if(final is IntegerNumber) return final;
            if(final is LetterSym && (final as LetterSym).Subscript != null) final = new LetterSym((final as LetterSym).Letter, SubstSubstript(variables, values, (final as LetterSym).Subscript));
            if(final is CompositeExpr) {
                int len = (final as CompositeExpr).Args.Length;
                Expr[] theArg = new Expr[len];
                for(int i = 0; i<len; i++) {
                    theArg[i] = SubstSubstript(variables, values, (final as CompositeExpr).Args[i]);
                }
                return new CompositeExpr((final as CompositeExpr).Head, theArg);
            }
            for(int k = 0; k < variables.Count; k++)
                final = Engine.Substitute(final, variables[k], values[k].Clone());
            return final;
        }
        static public void TestForFunctionDefinition(MathEditor mrec, IEnumerable<ParseResult> results) { }
    }
}
