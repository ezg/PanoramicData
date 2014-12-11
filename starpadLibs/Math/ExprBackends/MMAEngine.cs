using System;
using System.Collections.Generic;
using System.Text;
using Wolfram.NETLink;
using MMAExpr = Wolfram.NETLink.Expr;
using System.Diagnostics;
using System.Reflection;

namespace starPadSDK.MathExpr {
    public class MMAEngine : Engine {
        private IKernelLink _ml = null;
        private Expr Evaluate(Expr e) {
            MMAExpr mmae = Convert(e);
            _ml.Evaluate(mmae);
            _ml.WaitForAnswer();
            MMAExpr result = _ml.GetExpr();
            Expr cresult = Convert(result);
            return cresult;
        }

        public override Expr _Simplify(Expr e) {
            return Evaluate(new CompositeExpr(new WordSym("Simplify"), e));
        }

        public override Expr _Approximate(Expr e) {
            return Evaluate(new CompositeExpr(new WordSym("N"), e));
        }

        public override Expr _Substitute(Expr e, Expr orig, Expr replacement) {
            // FIXME: use MMA here!
            return (new BuiltInEngine())._Substitute(e, orig, replacement);
        }
        public override Expr _Substitute(Expr e, MathConstant[] consts) {
            // FIXME: use MMA here!
            return (new BuiltInEngine())._Substitute(e, consts);
        }
        public override Expr _Replace(Expr e, Expr orig, Expr replacement) {
            // FIXME: use MMA here!
            return (new BuiltInEngine())._Replace(e, orig, replacement);
        }

        private string[] _names;
        private string[] _cmdlines;
        private int _variant = 0;
        public override string[] Names { get { return _names; } }
        public override int Variant {
            get {
                return _variant;
            }
            set {
                Deactivate();
                _variant = value;
            }
        }
        public override string Name { get { return "Mathematica"; } }

        private class OrdinalComparer : IComparer<string> {
            public int Compare(string a, string b) {
                return -String.CompareOrdinal(a, b);
            }
        }
        public MMAEngine() {
            try {
                string path;
                // First, look for the WolframProductRegistry file, which exists for 4.2 and later.
                string wriFile = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) +
                                                        @"\Mathematica\WolframProductRegistry";
                bool gotit = false;
                if(System.IO.File.Exists(wriFile)) {
                    SortedList<string, string> recs = new SortedList<string, string>(new OrdinalComparer());
                    using(System.IO.FileStream strm = System.IO.File.OpenRead(wriFile)) {
                        System.IO.StreamReader reader = new System.IO.StreamReader(strm);
                        for(string line = reader.ReadLine(); line != null; line = reader.ReadLine()) {
                            // Look for lines like: "Mathematica x.x=path"
                            if(line.StartsWith("Mathematica ")) {
                                string version = line.Split(' ', '=')[1];
                                path = line.Split('=')[1];
                                try { recs.Add(version, path); } catch(Exception) {/* ignore duplicate keys */}
                            }
                        }
                    }
                    List<string> cmdlines = new List<string>(), names = new List<string>();
                    // recs will be sorted with highest version numbers first.
                    foreach(KeyValuePair<string,string> de in recs) {
                        path = de.Value;
                        if(System.IO.File.Exists(path)) {
                            // To avoid backslash/quoting problems, just replace \ with /, which work fine.
                            path = path.Replace(@"\", "/").Replace("Mathematica.exe", "MathKernel.exe");
                            cmdlines.Add("-linkmode launch -linkname \"" + path + "\"");
                            names.Add("Mathematica " + de.Key);
                        }
                    }
                    if(cmdlines.Count > 0) {
                        _cmdlines = cmdlines.ToArray();
                        _names = names.ToArray();
                        gotit = true;
                    }
                }
                if(!gotit) {
                    // Will get here if either the WolframProductRegistry was not found or it did not have any useful info.
                    // Next technique is to look at registry key HKCR/MathematicaNB/DefaultIcon. It contains a string like
                    // "d:\math41\Mathematica.exe,-102".
                    if (Microsoft.Win32.Registry.ClassesRoot.OpenSubKey("MathematicaNB") != null)
                    {
                        string keyStr = (string)Microsoft.Win32.Registry.ClassesRoot.OpenSubKey("MathematicaNB").OpenSubKey("DefaultIcon").GetValue("");
                        path = keyStr.Split(',')[0];
                        // To avoid backslash/quoting problems, just replace \ with /, which work fine.
                        path = path.Replace(@"\", "/");
                        if (path[0] == '\"' && path[path.Length - 1] == '\"') path = path.Substring(1, path.Length - 1);
                        _names = null;
                        _cmdlines = new string[] { "-linkmode launch -linkname \"" + path.Replace("Mathematica.exe", "MathKernel.exe") + "\"" };
                    }
                    else
                    {    // Just return a basic launch string and let MathLink's "find a program to launch" dialog appear.
                        _cmdlines = new string[] { "-linkmode launch" };
                        _names = null;
                     }
                }
            } catch(Exception) {
                // Don't want to propagate exceptions. Just return a basic launch string and let MathLink's
                // "find a program to launch" dialog appear.
                _cmdlines = new string[] { "-linkmode launch" };
                _names = null;
            }
        }
        public override void Activate() {
            if(_ml == null) {
                //_ml = MathLinkFactory.CreateKernelLink();
                _ml = MathLinkFactory.CreateKernelLink(_cmdlines[_variant]);
                // Discard the initial InputNamePacket the kernel will send when launched
                _ml.WaitAndDiscardAnswer();
                LoadFunctions();
            }
        }
        private void LoadFunction(string def) {
            _ml.Evaluate(def);
            _ml.WaitForAnswer();
            MMAExpr er = _ml.GetExpr();
            Trace.Assert((object)er.Head == null);
        }
        private void LoadFunctions() {
            LoadFunction(@"mytimes1[1, b_] := b;
mytimes1[a_, b_] := a . b /; MatrixQ[a] && MatrixQ[b];
mytimes1[a_, b_] := a b /; NumericQ[a] || NumericQ[b];");
            LoadFunction(@"mytimes[e__] := Fold[mytimes1, 1, List[e]]");
        }

        public override void Deactivate() {
            if(_ml != null) {
                _ml.Close();
                _ml = null;
            }
        }
        ~MMAEngine() {
            Deactivate();
        }

        /// <summary>
        /// variables we deal with (displayed to the user) which are just one letter have this prefixed to them when used in MMA
        /// </summary>
        private const string _userVarPrefix = "u$";
        /// <summary>
        /// variables which in MMA are just one letter have this prefixed to them when we deal with (and display) them
        /// </summary>
        private const string _mmaVarPrefix = "$";

        // FIXME missing: DirectedInfinity, summations
        static private Expr Convert(MMAExpr e) {
            if(Object.ReferenceEquals(e, null)) return new NullExpr();
            else if(e.IntegerQ()) return new IntegerNumber(e.ToString());
            else if(e.RealQ()) return new DoubleNumber(e.AsDouble());
            else if(e.StringQ()) throw new NotImplementedException();
            else if(e.SymbolQ()) {
                string s = e.ToString();
                WKSID wksid;
                if(_mmatoexpr.TryGetValue(s, out wksid)) return new WellKnownSym(wksid);
                else if(s.Length == 1) return Char.IsLetter(s[0]) ? (Expr)new WordSym(_mmaVarPrefix + s) : (Expr)new LetterSym(s[0]);
                else if(s.StartsWith(_userVarPrefix)) {
                    if(s.Length == _userVarPrefix.Length + 1) return new LetterSym(s[_userVarPrefix.Length]);
                    else return new WordSym(s.Remove(0, _userVarPrefix.Length)); // not really supported
                } else return new WordSym(s);
            } else if(e.MatrixQ()) {
                int h = e.Length;
                int w = e[1].Length;
                Expr[,] a = new Expr[h,w];
                for(int i = 1; i <= h; i++) {
                    for(int j = 1; j <= w; j++) {
                        a[i-1, j-1] = Convert(e[i][j]);
                    }
                }
                ArrayExpr ae = new ArrayExpr(a);
                ae.Annotations["Force Parentheses"] = 1;
                return ae;
            } else {
                if(Object.ReferenceEquals(e.Head, null)) return new NullExpr();
                if(e.Head.SymbolQ()) {
                    if(e.Head.ToString() == "Complex") {
                        return new ComplexNumber((RealNumber)Convert(e[1]), (RealNumber)Convert(e[2]));
                    } else if(e.Head.ToString() == "Rational") {
                        BigInt num = new BigInt(e[1].ToString()), den = new BigInt(e[2].ToString());
                        return new RationalNumber(new BigRat(num, den));
                    } else if(e.Head.ToString() == "Power" && e.Length == 2) {
                        if(e[2].Head.SymbolQ() && e[2].Head.ToString() == "Rational") {
                            if(e[2][1].ToString() == "1") {
                                return new CompositeExpr(WellKnownSym.root, Convert(e[2][2]), Convert(e[1]));
                            } else if(e[2][1].ToString() == "-1") {
                                return new CompositeExpr(WellKnownSym.divide, new CompositeExpr(WellKnownSym.root, Convert(e[2][2]), Convert(e[1])));
                            } else if(e[2][1].ToString()[0] == '-') {
                                return new CompositeExpr(WellKnownSym.divide, new CompositeExpr(WellKnownSym.power, Convert(e[1]),
                                    new RationalNumber(new BigRat(new BigInt(e[2][1].ToString().Substring(1)), new BigInt(e[2][2].ToString())))));
                            }
                        } else if(e[2].IntegerQ() && e[2].ToString()[0] == '-') {
                            return new CompositeExpr(WellKnownSym.divide, e[2].ToString() == "-1" ? Convert(e[1])
                                : new CompositeExpr(WellKnownSym.power, Convert(e[1]), new IntegerNumber(e[2].ToString().Substring(1))));
                        }
                    } else if(e.Head.ToString() == "Times" && e.Length > 1) {
                        if(e[1].IntegerQ() && e[1].ToString() == "-1") {
                            if(e.Length > 2) {
                                MMAExpr[] args = new MMAExpr[e.Length-1];
                                Array.Copy(e.Args, 1, args, 0, e.Length-1);
                                return new CompositeExpr(WellKnownSym.minus, new CompositeExpr(WellKnownSym.times, Convert(args)));
                            } else return new CompositeExpr(WellKnownSym.minus, Convert(e[2]));
                        }
                    } else if(e.Head.ToString() == "Subscript" && e.Length == 2 && e[1].SymbolQ()) {
                        Expr itm = Convert(e[1]);
                        WordSym ws = itm as WordSym;
                        LetterSym ls = itm as LetterSym;
                        if(ws != null) {
                            ws.Subscript = Convert(e[2]);
                            return ws;
                        }
                        if(ls != null) {
                            ls.Subscript = Convert(e[2]);
                            return ls;
                        }
                        /* else fall through, handle genericly */
                    } else if(e.Head.ToString() == "Integrate" && e.Length == 2) {
                        Expr body = Convert(e[1]);
                        if(e[2].Head.ToString() != "List") {
                            return new CompositeExpr(WellKnownSym.integral, body, new CompositeExpr(WellKnownSym.differentiald, Convert(e[2])));
                        } else if(e[2].Length == 3) {
                            return new CompositeExpr(WellKnownSym.integral, body, new CompositeExpr(WellKnownSym.differentiald, Convert(e[2][1])), Convert(e[2][2]), Convert(e[2][3]));
                        } /* else fall though, handle genericly */
                    } else if(e.Head.ToString() == "Sum" && e.Length == 2) {
                        Expr body = Convert(e[1]);
                        if(e[2].Head.ToString() == "List" && e[2].Length == 3) {
                            return new CompositeExpr(WellKnownSym.summation, new CompositeExpr(WellKnownSym.equals, Convert(e[2][1]), Convert(e[2][2])),
                                Convert(e[2][3]), body);
                        } /* else fall through, handle genericly */
                    } else if(e.Head.ToString() == "Set" && e.Length == 2) {
                        return new CompositeExpr(WellKnownSym.assignment, Convert(e[1]), Convert(e[2]));
                    } else if(e.Head.ToString() == "SetDelayed" && e.Length == 2) {
                        // FIXME: Set and SetDelayed are probably handled wrong
                        return new CompositeExpr(WellKnownSym.definition, Convert(e[1]), Convert(e[2]));
                    } else if(e.Head.ToString() == "List") {
                        Expr[] args = new Expr[e.Length];
                        for(int i = 0; i < e.Length; i++) args[i] = Convert(e[i + 1]);
                        return new CompositeExpr(new WordSym("brace"), args);
                    } else if(e.Head.ToString() == "Rule" && e.Length == 2) {
                        return new CompositeExpr(new LetterSym('→'), Convert(e[1]), Convert(e[2]));
                    } else if(e.Head.ToString() == "Part" && e.Length == 2) {
                        Expr array = Convert(e[1]);
                        Expr[] args = new Expr[e.Length - 1];
                        for(int i = 2; i <= e.Length; i++) args[i - 1] = Convert(e[i]);
                        ArrayExpr ae = new ArrayExpr(args);
                        ae.Annotations["Force Parentheses"] = 1;
                        return new CompositeExpr(WellKnownSym.index, args[0], ae);
                    }
                }
                return new CompositeExpr(Convert(e.Head), Convert(e.Args));
            }
        }
        static private Expr[] Convert(MMAExpr[] earray) {
            return Array.ConvertAll<MMAExpr,Expr>(earray, Convert);
        }

        // FIXME missing: SummationExpr
        static private MMAExpr Convert(Expr e) {
            return (MMAExpr)typeof(MMAEngine).InvokeMember("_Convert", BindingFlags.InvokeMethod|BindingFlags.NonPublic|BindingFlags.Static,
                null, null, new object[] { e });
        }
        static private MMAExpr _Convert(ErrorExpr ee) {
            // FIXME: is this right? should be do something else?
            return MMASymbol("$Failed");
        }
        static private MMAExpr _Convert(NullExpr e) {
            throw new NotSupportedException("NullExprs should not exist outside of subscripts and such");
        }
        static private MMAExpr[] Convert(Expr[] earray) {
            return Array.ConvertAll<Expr,MMAExpr>(earray, Convert);
        }
        static private MMAExpr _Convert(CompositeExpr e) {
            CompositeExpr ce;
            if(e.Head is WellKnownSym) {
                WKSID id = ((WellKnownSym)e.Head).ID;
                switch(id) {
                    case WKSID.power:
                        if(e.Args[0] is ArrayExpr)
                            return new MMAExpr(MMASymbol("MatrixPower"),Convert(e.Args));                        
                        break;
                    case WKSID.root:
                        Trace.Assert(e.Args.Length == 2);
                        return new MMAExpr(MMASymbol("Power"), Convert(e.Args[1]), Convert(new CompositeExpr(WellKnownSym.divide, e.Args[0])));
                    case WKSID.divide:
                        Trace.Assert(e.Args.Length == 1);
                        return new MMAExpr(MMASymbol("Divide"), MMAInteger("1"), Convert(e.Args[0]));
                    case WKSID.integral:
                        Trace.Assert(e.Args.Length >= 2 && e.Args.Length <= 4);
                        ce = e.Args[1] as CompositeExpr;
                        if(ce != null && ce.Head == WellKnownSym.differentiald && ce.Args.Length == 1) {
                            if(e.Args.Length == 2) return new MMAExpr(MMASymbol("Integrate"), Convert(e.Args[0]), Convert(ce.Args[0]));
                            else if(e.Args.Length == 4) {
                                MMAExpr lims = new MMAExpr(MMASymbol("List"), Convert(ce.Args[0]), Convert(e.Args[2]), Convert(e.Args[3]));
                                return new MMAExpr(MMASymbol("Integrate"), Convert(e.Args[0]), lims);
                            }
                        }
                        break;
                    case WKSID.summation:
                        Trace.Assert(e.Args.Length >= 1 && e.Args.Length <= 3);
                        if(e.Args.Length == 3) {
                            ce = e.Args[0] as CompositeExpr;
                            if(ce != null && ce.Head == WellKnownSym.equals && ce.Args.Length == 2) {
                                MMAExpr lims = new MMAExpr(MMASymbol("List"), Convert(ce.Args[0]), Convert(ce.Args[1]), Convert(e.Args[1]));
                                return new MMAExpr(MMASymbol("Sum"), Convert(e.Args[2]), lims);
                            }
                        }
                        break;
                    case WKSID.definition:
                        if(e.Args.Length == 2) {
                            ce = e.Args[0] as CompositeExpr;
                            if(ce != null && Array.TrueForAll(ce.Args, delegate(Expr a) { return a is LetterSym || a is WordSym; })) {
                                MMAExpr head = Convert(ce.Head);
                                MMAExpr[] args = new MMAExpr[ce.Args.Length];
                                for(int i = 0; i < ce.Args.Length; i++) {
                                    args[i] = new MMAExpr(MMASymbol("Pattern"), Convert(ce.Args[i]), new MMAExpr(MMASymbol("Blank"), new object[0]));
                                }
                                MMAExpr fn = new MMAExpr(head, args);
                                return new MMAExpr(Convert(e.Head), fn, Convert(e.Args[1]));
                            }
                        }
                        break;
                    case WKSID.index:
                        if(e.Args.Length == 2 || e.Args.Length == 3) {
                            MMAExpr head = Convert(e.Args[0]);
                            MMAExpr indices = null;
                            if (e.Args.Length == 2) indices = Convert(e.Args[1]);
                            else {
                                Expr[] a = new Expr[2];
                                a[0] = e.Args[1];
                                a[1] = e.Args[2];
                                indices = Convert(new ArrayExpr(a));
                            }
                            return new MMAExpr(MMASymbol("Part"), head, new MMAExpr(MMASymbol("Apply"), MMASymbol("Sequence"), indices));
                        }
                        break;
                }
            } else if(e.Head == new WordSym("brace")) return new MMAExpr(MMASymbol("List"), Convert(e.Args));
            else if(e.Head == new LetterSym('→') && e.Args.Length == 2) return new MMAExpr(MMASymbol("Rule"), Convert(e.Args));
            else if(e.Head == new WordSym("Sequence") && e.Args.Length > 0) return new MMAExpr(MMASymbol("CompoundExpression"), Convert(e.Args));
            else if(e.Head == new WordSym("if") && e.Args.Length >= 2 && e.Args.Length <= 3) return new MMAExpr(MMASymbol("If"), Convert(e.Args));
            else if(e.Head == new WordSym("for") && e.Args.Length >= 3) {
                Expr var = e.Args[0];
                CompositeExpr range = e.Args[1] as CompositeExpr;
                if(range != null && range.Args.Length == 2) {
                    Expr lo = range.Args[0];
                    Expr hi = range.Args[1];
                    Expr[] stmts = new Expr[e.Args.Length - 2];
                    Array.Copy(e.Args, 2, stmts, 0, stmts.Length);
                    return new MMAExpr(MMASymbol("Do"), new MMAExpr(MMASymbol("CompoundExpression"), Convert(stmts)),
                        new MMAExpr(MMASymbol("List"), Convert(var), Convert(lo), Convert(hi)));
                }
            }
            return new MMAExpr(Convert(e.Head), Convert(e.Args));
        }
        static private MMAExpr _Convert(DoubleNumber n) {
            if(Double.IsNaN(n.Num)) {
                return MMASymbol("Indeterminate");
            } else if(Double.IsNegativeInfinity(n.Num)) {
                return new MMAExpr(MMASymbol("DirectedInfinity"), MMAInteger("-1"));
            } else if(Double.IsPositiveInfinity(n.Num)) {
                return new MMAExpr(MMASymbol("DirectedInfinity"), MMAInteger("1"));
            } else return MMAReal(n.Num.ToString("R"));
        }
        static private MMAExpr _Convert(IntegerNumber n) {
            return MMAInteger(n.Num.ToString());
        }
        static private MMAExpr _Convert(ComplexNumber n) {
            MMAExpr re = Convert(n.Re);
            MMAExpr im = Convert(n.Im);
            return new MMAExpr(MMASymbol("Complex"), re, im);
        }
        static private MMAExpr _Convert(RationalNumber n) {
            return new MMAExpr(MMASymbol("Rational"), MMAInteger(n.Num.Num.ToString()), MMAInteger(n.Num.Denom.ToString()));
        }
        static private MMAExpr ConvertAE(ArrayExpr e, int[] dims) {
            if(dims.Length == e.Elts.Rank) return Convert(e[dims]);
            int[] dims2 = new int[dims.Length + 1];
            dims.CopyTo(dims2, 0);
            int len = e.Elts.GetLength(dims.Length);
            MMAExpr[] slice = new MMAExpr[len];
            for(int i = 0; i < len; i++) {
                dims2[dims.Length] = i;
                slice[i] = ConvertAE(e, dims2);
            }
            return new MMAExpr(MMASymbol("List"), slice);
        }
        static private MMAExpr _Convert(ArrayExpr e) {
            return ConvertAE(e, new int[0]);
        }
        private static LetterSym _degree = new LetterSym('°');
        static private MMAExpr _Convert(LetterSym s) {
            if(s == _degree) return new MMAExpr(MMASymbol("Times"), MMASymbol("Pi"), new MMAExpr(MMASymbol("Rational"), MMAInteger("1"), MMAInteger("180")));
            /* TODO: need to handle accent, format */
            string mmaname = Char.IsLetter(s.Letter) ? _userVarPrefix + s.Letter.ToString() : s.Letter.ToString();
            MMAExpr e = MMASymbol(mmaname);
            if(s.Subscript != new NullExpr()) {
                e = new MMAExpr(MMASymbol("Subscript"), e, Convert(s.Subscript));
            }
            return e;
        }
        static private MMAExpr _Convert(GroupedLetterSym s) {
            /* TODO: need to handle accent */
            throw new NotImplementedException("MMA doesn't handle groupedlettersym yet");
        }
        static private MMAExpr _Convert(WordSym s) {
            /* TODO: need to handle accent, format */
            string mmaname = s.Word.StartsWith(_mmaVarPrefix) ? s.Word.Remove(0, _mmaVarPrefix.Length) : s.Word;
            MMAExpr e = MMASymbol(mmaname);
            if(s.Subscript != new NullExpr()) {
                e = new MMAExpr(MMASymbol("Subscript"), e, Convert(s.Subscript));
            }
            return e;
        }
        static Dictionary<string, WKSID> _mmatoexpr = new Dictionary<string, WKSID>();
        static Dictionary<WKSID, string> _exprtomma = new Dictionary<WKSID, string>();
        static private void AddMapping(string mma, WKSID expr) {
            _mmatoexpr[mma] = expr;
            _exprtomma[expr] = mma;
        }
        static MMAEngine() {
            AddMapping("I", WKSID.i);
            AddMapping("E", WKSID.e);
            AddMapping("Pi", WKSID.pi);
            AddMapping("Infinity", WKSID.infinity);
            //AddMapping(??, WKSID.Naturals);
            AddMapping("Integers", WKSID.integers);
            AddMapping("Rationals", WKSID.rationals);
            AddMapping("Reals", WKSID.reals);
            AddMapping("Complexes", WKSID.complexes);
            AddMapping("Re", WKSID.re);
            AddMapping("Im", WKSID.im);
            AddMapping("Arg", WKSID.arg);
            AddMapping("Abs", WKSID.magnitude);
            _mmatoexpr["Log"] = WKSID.log;
            _exprtomma[WKSID.log] = "Log";
            _exprtomma[WKSID.ln] = "Log";
            AddMapping("DifferentialD", WKSID.differentiald);
            AddMapping("PartialD", WKSID.partiald);
            AddMapping("Sin", WKSID.sin);
            AddMapping("Cos", WKSID.cos);
            AddMapping("Tan", WKSID.tan);
            AddMapping("Sec", WKSID.sec);
            AddMapping("Csc", WKSID.csc);
            AddMapping("Cot", WKSID.cot);
            AddMapping("Sinh", WKSID.sinh);
            AddMapping("Cosh", WKSID.cosh);
            AddMapping("Tanh", WKSID.tanh);
            AddMapping("Sech", WKSID.sech);
            AddMapping("Csch", WKSID.csch);
            AddMapping("Coth", WKSID.coth);
            AddMapping("ArcSin", WKSID.arcsin);
            AddMapping("ArcCos", WKSID.arccos);
            AddMapping("ArcTan", WKSID.arctan);
            AddMapping("ArcSec", WKSID.asec);
            AddMapping("ArcCsc", WKSID.acsc);
            AddMapping("ArcCot", WKSID.acot);
            AddMapping("ArcSinh", WKSID.asinh);
            AddMapping("ArcCosh", WKSID.acosh);
            AddMapping("ArcTanh", WKSID.atanh);
            AddMapping("ArcSech", WKSID.asech);
            AddMapping("ArcCsch", WKSID.acsch);
            AddMapping("ArcCoth", WKSID.acoth);
            AddMapping("Plus", WKSID.plus);
            AddMapping("Minus", WKSID.minus);
            AddMapping("Sum", WKSID.sum);
            AddMapping("Avg", WKSID.avg);
#if DOMATRIXMULT
            AddMapping("mytimes", WKSID.times);
            _mmatoexpr["Times"] = WKSID.times;
            _mmatoexpr["mytimes1"] = WKSID.times;
#else
            AddMapping("Times", WKSID.times);
#endif
            AddMapping("Divide", WKSID.divide);
            AddMapping("Mod", WKSID.mod);
            AddMapping("Power", WKSID.power);
            AddMapping("Factorial", WKSID.factorial);
            // FIXME: Set and SetDelayed are probably handled wrong
            AddMapping("Set", WKSID.assignment);
            AddMapping("SetDelayed", WKSID.definition);
            AddMapping("Equal", WKSID.equals);
            AddMapping("Greater", WKSID.greaterthan);
            AddMapping("Less", WKSID.lessthan);
            AddMapping("GreaterEqual", WKSID.greaterequals);
            AddMapping("LessEqual", WKSID.lessequals);
            AddMapping("Unequal", WKSID.notequals);
            AddMapping("True", WKSID.True);
            AddMapping("False", WKSID.False);
            AddMapping("Not", WKSID.lognot);
            AddMapping("And", WKSID.logand);
            AddMapping("Or", WKSID.logor);
            AddMapping("Floor", WKSID.floor);
            AddMapping("Ceiling", WKSID.ceiling);
            AddMapping("Abs", WKSID.magnitude);
            AddMapping("Dot", WKSID.dot);
            AddMapping("Cross", WKSID.cross);
            AddMapping("Part", WKSID.index);
            AddMapping("Subscript", WKSID.subscript);
            AddMapping("Element", WKSID.elementof);
        }
        static private MMAExpr _Convert(WellKnownSym s) {
            switch(s.ID) { // log and root need special treatment for arg order etc
                case WKSID.del:
                case WKSID.integral:
                case WKSID.summation:
                case WKSID.limit:
                case WKSID.root: // root should have been handled in compositeexpr if a function; otherwise who knows?
                case WKSID.plusminus:
                case WKSID.minusplus:
                    throw new NotImplementedException("MMA isn't complete yet");
                default:
                    return MMASymbol(_exprtomma[s.ID]);
            }
        }

        static private MMAExpr MMAInteger(string s) { return new MMAExpr(ExpressionType.Integer, s); }
        static private MMAExpr MMAReal(string s) { return new MMAExpr(ExpressionType.Real, s); }
        static private MMAExpr MMAString(string s) { return new MMAExpr(ExpressionType.String, s); }
        static private MMAExpr MMASymbol(string s) { return new MMAExpr(ExpressionType.Symbol, s); }
        static private MMAExpr MMABoolean(string s) { return new MMAExpr(ExpressionType.Boolean, s); }
    }
}
