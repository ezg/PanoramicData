using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using starPadSDK.Inq.MSInkCompat;
using System.Windows.Media;
using System.Windows.Shapes;
using starPadSDK.Inq.BobsCusps;
using starPadSDK.WPFHelp;
using System.IO;
using starPadSDK.MathExpr.ExprWPF;
using starPadSDK.MathExpr.ExprWPF.Boxes;
using starPadSDK.MathExpr;
using starPadSDK.MathRecognizer;
using starPadSDK.CharRecognizer;
using starPadSDK.UnicodeNs;
using Constant = starPadSDK.MathExpr.MathConstant;
using Line = System.Windows.Shapes.Line;
using Microsoft.Research.DynamicDataDisplay.Charts;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.PointMarkers;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using System.Windows.Documents;

namespace starPadSDK.AppLib {
    public class CanonicalFunc {
        public class FreeVar {
            Expr val;
            public delegate void VarChangedHandler(FreeVar var);
            public event VarChangedHandler VarChangedEvent;
            public FreeVar(Expr var) { Var = var; val = 1;  }
            public Expr Val { get { return val; } set { val = value; if (VarChangedEvent != null) VarChangedEvent(this);  } }
            public Expr Var { get; set; }
        }
        public class Result {
            public double value;
            public double index;
            public Result(double i, double v) { index = i; value = double.IsInfinity(v) ? CoordinateTransform.NegToPosDiscontinuity : v; }
        }
        List<Expr>        cached = new List<Expr>();
        List<FreeVar>     vars;
        static List<Expr> freeVariables(Expr func) {
            List<Expr> vars = new List<Expr>();
            CompositeExpr ce = func as CompositeExpr;
            if (ce != null) {
                foreach (Expr ex in ce.Args)
                    vars.AddRange(freeVariables(ex).ToArray());
            }
            else if (func is LetterSym)
                vars.Add(func);
            return vars;
        }
        void              cacheFuncs() {
            cached.Clear();
            Critical.Clear();
            foreach (Expr e in Funcs) {
                int maxRecursion = 100;
                bool changed;
                Expr cacheExpr = (e.Head() == WellKnownSym.equals) ? e.Args()[1] : e;
                List<Constant> constants = new List<Constant>();
                foreach (FreeVar v in vars)
                    constants.Add(new Constant(v.Var, v.Val));
                cacheExpr = Engine.Substitute(cacheExpr, constants.ToArray());
                cached.Add(new BuiltInEngine().SubstFunctions(cacheExpr, out changed, ref maxRecursion));
            }
            // 
            // Code below is finding critical points-- if has 2 functions, sets solutions to be equal & solve, then that gives
            // to values of X & gets vlaue & always plot those points.
            //
            // In case of inequalities ... y < x or <= x, first function is inequality itself, 2nd func
            // is for line you draw, 3rd func is possible other root (e.g., for a circle).
            if (cached.Count == 2 || (IsInequality && cached.Count == 3)) {
                CompositeExpr solved = new CompositeExpr(WellKnownSym.equals, cached[0], cached[1]);
                try {
                    List<Expr> roots = (List<Expr>)typeof(Engine).InvokeMember("Solve", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                                                                                                  null, null, new object[] { new List<Expr>(new Expr[] { solved }), new List<Expr>(new Expr[] { IndepVar }), true });
                    foreach (Expr root in roots) {
                        double i = (double)root;
                        double d = (double)Evaluator.EvaluateExprFunc(cached[0], new Constant[] { new Constant(IndepVar, i) }, true);
                        Critical.Add(new Result(i, d));
                    }
                }
                catch (Exception) { }
            }
        }
        static double     chooseNiceCoord(double herex, double wpix) {
            if (double.IsNaN(herex) || double.IsInfinity(herex))
                return herex;
            double herexl = (herex + wpix);
            double herexr = (herex - wpix);
            int scale = (int)Math.Log10(Math.Abs(herex));
            while (herex != 0) {
                if ((herex < 0 && herex + wpix > 0) || (herex > 0 && herex - wpix < 0)) {
                    return 0;
                }
                if ((int)(herex / Math.Pow(10, scale)) != (int)(herexl / Math.Pow(10, scale)) ||
                    (int)(herex / Math.Pow(10, scale)) != (int)(herexr / Math.Pow(10, scale)))
                    return Math.Round(herex / Math.Pow(10, scale)) * Math.Pow(10, scale);
                scale--;
                if (scale < -10)
                    break;
            }
            return herex;
        }
        static bool inequalityTest(Expr func, Constant[] indexes) {
            double lval = (double)Engine.Approximate(func.Args()[0]);
            double rval = (double)Engine.Approximate(func.Args()[1]);
            switch ((func.Head() as WellKnownSym).ID) {
                case WKSID.lessequals: return !double.IsNaN(lval) && !double.IsNaN(rval) && lval <= rval;
                case WKSID.greaterequals: return !double.IsNaN(lval) && !double.IsNaN(rval) && lval >= rval;
                case WKSID.lessthan: return !double.IsNaN(lval) && !double.IsNaN(rval) && lval < rval;
                case WKSID.greaterthan: return !double.IsNaN(lval) && !double.IsNaN(rval) && lval > rval;
            }

            return !double.IsNaN(lval) && !double.IsNaN(rval) && lval < rval;
        }

        public delegate void CacheChangedHandler(CanonicalFunc func);
        public event CacheChangedHandler CacheChangedEvent;
        public CanonicalFunc(Expr indep, Expr dep, List<Expr> funcs, List<FreeVar> free, bool isIneq) {
            foreach (FreeVar v in free)
                v.VarChangedEvent += new FreeVar.VarChangedHandler((CanonicalFunc.FreeVar var) => {
                    cacheFuncs();
                    if (CacheChangedEvent != null)
                        CacheChangedEvent(this);
                });
            Critical = new List<Result>();
            IndepVar = indep;
            DepVar = dep;
            Funcs = funcs;
            vars = free;
            IsInequality = isIneq;
            cacheFuncs();
        }

        public List<Result>       Critical     { get; set; }
        public Expr               IndepVar     { get; set; }
        public Expr               DepVar       { get; set; }
        public bool               IsInequality { get; set; }
        public List<Expr>         Funcs        { get; set; }
        public List<Expr>         CachedFuncs  { get { return cached; } }
        public List<FreeVar>      Vars         { get { return vars; } }
        // coudl do binary search stuff here..
        // Also look at ChooseNiceCoord (0.1 rather than 0.099999999999)..
        // so if y = 1/x.. won't get y value at 0.00000001, but at 0 and get INF.
        // ChooseNiceCoord could be extended to know about PI-based values.
        // if knew, would prefer to find PI/3.1-- pi, 7 ... 3.1286, 7.03...
        public List<Result>       SampleRange(Expr func, double start, double stop, double step) {
            List<Result> result = new List<Result>();
            int i = 1;
            MathConstant ival = new Constant(IndepVar, new DoubleNumber(0));
            Expr substitutedExpr = Evaluator.SubstituteExprFunc(func, new Constant[] { ival });
            if (IndepVar == new LetterSym(Unicode.G.GREEK_SMALL_LETTER_THETA))
                for (double indep = start; indep < stop; indep = start + i++ * step) {
                    try {
                        (ival.Value as DoubleNumber).Num = indep;
                        double dval = (double)Engine.Approximate(substitutedExpr);
                        result.Add(new Result(Math.Cos(indep)*dval, Math.Sin(indep)*dval));
                    }
                    catch (Exception) { }
                }
            else
                for (double indep = start; indep < stop; indep = start + i++ * step) {
                    double niceCoord = chooseNiceCoord(indep, step / 2);
                    try {
                        (ival.Value as DoubleNumber).Num = niceCoord;
                        double dval = (double)Engine.Approximate(substitutedExpr);
                        result.Add(new Result(niceCoord, dval));
                    }
                    catch (Exception) { }
                }
            result.AddRange(Critical.ToArray());
            return result;
        }
        public List<Pt>           SampleArea(Expr func, Rct area, Vec step) {
            List<Pt> results = new List<Pt>();
            MathConstant ival = new Constant(IndepVar, new DoubleNumber(0));
            MathConstant dval = new Constant(DepVar, new DoubleNumber(0));
            MathConstant[] indexes = new Constant[] { ival, dval };
            Expr substitutedExpr = Evaluator.SubstituteExprFunc(func, new Constant[] { ival, dval });
            for (double i = area.Left; i < area.Right; i += step.X)
                for (double j = area.Top; j < area.Bottom; j += step.Y) {
                    (ival.Value as DoubleNumber).Num = i;
                    (dval.Value as DoubleNumber).Num = j;
                    if (inequalityTest(substitutedExpr, indexes))
                        results.Add(new Pt(i, j));
                }
            return results;
        }
                          
        static public CanonicalFunc Convert(Expr func, Guid funcId) {
            List<Expr> vars = freeVariables(func);
            List<CanonicalFunc.FreeVar> freeVarsXY = new List<CanonicalFunc.FreeVar>();
            List<CanonicalFunc.FreeVar> freeVarsRTh = new List<CanonicalFunc.FreeVar>();
            CanonicalFunc cfuncX = null, cfuncY = null;
            foreach (Expr v in vars) {
                if (v != new LetterSym('x') && v != new LetterSym('y'))
                    freeVarsXY.Add(new CanonicalFunc.FreeVar(v));
                if (v != new LetterSym('r') && v != new LetterSym('θ'))
                    freeVarsRTh.Add(new CanonicalFunc.FreeVar(v));
            }

            if (func.Head() == WellKnownSym.lessthan || func.Head() == WellKnownSym.greaterthan ||
                func.Head() == WellKnownSym.lessequals || func.Head() == WellKnownSym.greaterequals) {
                CanonicalFunc borderFunc = Convert(new CompositeExpr(WellKnownSym.equals, func.Args()[0], func.Args()[1]), funcId);
                if (borderFunc != null) {
                    List<Expr> funcs = new List<Expr>(borderFunc.Funcs.ToArray());
                    funcs.Insert(0, func);
                    cfuncX = new CanonicalFunc(borderFunc.IndepVar, borderFunc.DepVar, funcs, freeVarsXY, true);
                    cfuncX.Critical = borderFunc.Critical;
                }
                else
                    cfuncX = new CanonicalFunc(new LetterSym('x'), new LetterSym('y'), new List<Expr>(new Expr[]{func}), freeVarsXY, true);
                return cfuncX;
            }

            cfuncY = createFuncFromRoots(func, new LetterSym('x'), new LetterSym('y'), vars, freeVarsXY);

            if (cfuncY == null || cfuncY.Funcs.Count == 0 || cfuncY.Funcs.Count > 1)
                cfuncX = createFuncFromRoots(func, new LetterSym('y'), new LetterSym('x'), vars, freeVarsXY);

            if (cfuncY == null && cfuncX == null && (vars.Contains(new LetterSym('r')) || vars.Contains(new LetterSym('θ'))))
                return createFuncFromRoots(func, new LetterSym('θ'),  new LetterSym('r'),vars, freeVarsRTh);

            if (cfuncY == null || (cfuncX != null && cfuncY.Funcs.Count > cfuncX.Funcs.Count))
                return cfuncX;
            return cfuncY;
        }

        // handles circle case, but also
        // y = x^2... or x = y^2.. march along y axis compute x's
        // y = -x^2... try to solve both of them & find out which has simpler solution.
        // y = +/- x ...
        // if solve other way & get x = f(y), then  easier..
        // Goal:
        // 1) which axis is better to sample along?  which is a function?
        // 2) converts non-function into sets of functions...
        // 3) given those sets of functions, find those critical points -- left & right points of zero.
        // CacheFuncs
        static CanonicalFunc createFuncFromRoots(Expr func, LetterSym indepVar, LetterSym depVar, List<Expr> vars, List<CanonicalFunc.FreeVar> freeVars) {
            List<Expr> yRoots = new List<Expr>();
            if (func.Head() == WellKnownSym.equals) {
                if (vars.Contains(depVar)) {
                    if (func.Args()[0] == depVar && !freeVariables(func.Args()[1]).Contains(depVar))
                        yRoots = new List<Expr>(new Expr[] { func.Args()[1] });
                    // bcz: Remove if Engine's Solve method is unavailable
                    else try {
                            // yRoots = Engine.Solve(new List<Expr>(new Expr[] { func }), new List<Expr>(new Expr[] { depVar }), true);
                            yRoots = (List<Expr>)typeof(Engine).InvokeMember("Solve", System.Reflection.BindingFlags.InvokeMethod | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
                                                                                                          null, null, new object[] { new List<Expr>(new Expr[] { func }), new List<Expr>(new Expr[] { depVar }), true });
                        }
                        catch (Exception e) {
                        }
                }
            }
            else if (!vars.Contains(depVar))
                yRoots.Add(func);
            if (yRoots.Count > 0)
                return new CanonicalFunc(indepVar, depVar, yRoots, freeVars, false);
            return null;
        }
    }
}