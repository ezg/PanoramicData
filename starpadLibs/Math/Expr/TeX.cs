using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using starPadSDK.UnicodeNs;

namespace starPadSDK.MathExpr {
    public class TeX : GenericOutput<TeX.TeXStuff> {
        public abstract class TeXStuff {
            public static implicit operator TeXStuff(string s) { return new TeXPrim(s); }
            public abstract string Flatten();
        }
        class TeXRow : TeXStuff {
            private List<TeXStuff> _elts;
            public List<TeXStuff> Elts {
                get { return _elts; }
            }
            public TeXRow(List<TeXStuff> elts) { _elts = elts; }
            public TeXRow(params TeXStuff[] elts) { _elts = new List<TeXStuff>(elts); }
            public override string Flatten() {
                string[] args = new string[_elts.Count];
                for(int i = 0; i < _elts.Count; i++) {
                    args[i] = _elts[i] == null ? "" : _elts[i].Flatten();
                }
                return String.Join("", args);
            }
        }
        class TeXPrim : TeXStuff {
            private string _prim;
            public string Prim {
                get { return _prim; }
            }
            public TeXPrim(string s) { _prim = s; }
            public override string Flatten() {
                return _prim;
            }
        }

        public TeX() : base(false) { }
        public string Compose(Expr e) {
            return Translate(e).Flatten();
        }

        //Re Im arccos/sin/tan arg cos cosh cot coth csc deg det dim exp gcd hom inf ker lg lim ln log max min sec sin sinh tan tanh
        //\sqrt[n]{abc} nth root of abc
        //\frac{abc}{xyz}
        //\boldsymbol{+} used for all chars except letters; use \mathbf{l} for that
        //\operatorname{somewordoperator}
        //\left(blah\right)  use . instead of ) or ( for invisible delimiter in that position
        //\begin{matrix} 0 & 1 \\ 1 & 0 \end{matrix}    up to 10 cols; also pmatrix, bmatrix for (), []
        protected override TeX.TeXStuff __Translate(NullExpr e) {
            return "";
        }

        private static string _quoteChars = "#$%&_{}";
        private static string _moreQuoted = "~^\\";
        private string Quote(string s) {
            StringBuilder sb = new StringBuilder();
            foreach(char c in s) {
                if(_moreQuoted.IndexOf(c) >= 0) {
                    sb.Append("\verb|");
                    sb.Append(c);
                    sb.Append("|");
                } else {
                    if(_quoteChars.IndexOf(c) >= 0) sb.Append('\\');
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
        protected override TeX.TeXStuff __Translate(ErrorMsgExpr e) {
            return "\\text{" + Quote(e.Msg) + "}";
        }

        protected override TeX.TeXStuff __TranslateOperator(Expr expr, object exprix, Syntax.WOrC op, Syntax.TType type) {
            return _texmappings[op]; // FIXME we should not ignore Syntax.TType here; may need to tell TeX to use different formatting
        }

        private static HashSet<string> _builtinOps = new HashSet<string> { "arg", "ln", "log", "sin", "cos", "tan", "sec", "csc", "cot", "sinh", "cosh", "tanh", "coth" };
        protected override TeX.TeXStuff __TranslateWord(Expr expr, string op, Syntax.TType type) {
            if(op == "limit") return "\\lim";
            else if(_builtinOps.Contains(op)) return "\\" + op;
            else return "\\operatorname{" + op + "}";
        }

        protected override TeX.TeXStuff __TranslateDelims(Expr e, bool emph, object lexprix, char l, TeX.TeXStuff t, object rexprix, char r) {
            return new TeXRow("\\left " + _texmappings[l], t, "\\right " + _texmappings[r]);
        }

        protected override TeX.TeXStuff __WrapTranslatedExpr(Expr expr, List<TeX.TeXStuff> lt) {
            return new TeXRow(lt);
        }

        protected override TeX.TeXStuff __TranslateVerticalFraction(Expr e, Expr divlineexpr, TeX.TeXStuff num, TeX.TeXStuff den) {
            return new TeXRow("\\frac{", num, "}{", den, "}");
        }

        protected override TeX.TeXStuff __TranslateBigOp(Expr wholeexpr, Expr opexpr, char op, TeX.TeXStuff lowerlimit, TeX.TeXStuff upperlimit, TeX.TeXStuff contents) {
            // FIXME hm, can't make operator grow? should we?
            return new TeXRow(_texmappings[op] + "_{", lowerlimit, "}^{", upperlimit, "}", contents);
        }

        protected override TeX.TeXStuff __TranslateFunctionApplication(Expr e, TeX.TeXStuff fn, TeX.TeXStuff args) {
            return new TeXRow(fn, args);
        }

        protected override TeX.TeXStuff __TranslateOperatorApplication(Expr e, TeX.TeXStuff op, TeX.TeXStuff args) {
            return new TeXRow(op, args);
        }

        protected override TeX.TeXStuff __AddSuperscript(Expr e, TeX.TeXStuff nuc, TeX.TeXStuff sup) {
            return new TeXRow(nuc, "^{", sup, "}");
        }

        protected override TeX.TeXStuff __AddSubscript(Expr e, TeX.TeXStuff nuc, TeX.TeXStuff sub) {
            return new TeXRow(nuc, "_{", sub, "}");
        }

        protected override TeX.TeXStuff __TranslateRadical(Expr e, TeX.TeXStuff radicand, TeX.TeXStuff index) {
            if(index == null) return new TeXRow("\\sqrt{", radicand, "}");
            else return new TeXRow("\\sqrt[", index, "]{", radicand, "}");
        }

        protected override TeX.TeXStuff __TranslateIntegralInternals(TeX.TeXStuff integrand, TeX.TeXStuff dxthing) {
            return new TeXRow(integrand, "\\,", dxthing);
        }

        protected override TeX.TeXStuff __Translate(DoubleNumber n) {
            if(Double.IsNaN(n.Num)) {
                return "\\text{NaN}";
            } else if(Double.IsNegativeInfinity(n.Num)) {
                return "-\\infty";
            } else if(Double.IsPositiveInfinity(n.Num)) {
                return "\\infty";
            } else {
                string num = n.Num.ToString("R");
                int e = num.IndexOfAny(new char[] { 'e', 'E' });
                string significand, exponent;
                if(e == -1) {
                    significand = num;
                    exponent = null;
                } else {
                    significand = num.Substring(0, e);
                    exponent = num.Substring(e+1, num.Length-(e+1));
                    if(exponent[0] == '+') exponent = exponent.Substring(1, exponent.Length-1);
                }
                if(significand.IndexOf('.') == -1) significand = significand + ".";
                if(exponent == null) return significand;
                else {
                    return new TeXRow(significand, "\\times 10^{", exponent, "}");
                }
            }
        }

        protected override TeX.TeXStuff __TranslateNumber(Expr e, string n) {
            return n;
        }

        protected override TeX.TeXStuff __Translate(ArrayExpr e) {
            List<TeXStuff> a = new List<TeXStuff>();
            a.Add("\\begin{matrix}");
            if(e.Elts.Rank == 1) {
                foreach(Expr elt in e.Elts) {
                    if(a.Count != 1) a.Add("&");
                    a.Add(Translate(elt));
                }
            } else if(e.Elts.Rank == 2) {
                int h = e.Elts.GetLength(0);
                int w = e.Elts.GetLength(1);
                for(int i = 0; i < h; i++) {
                    if(i != 0) a.Add("\\\\");
                    for(int j = 0; j < w; j++) {
                        if(j != 0) a.Add("&");
                        a.Add(Translate(e[i, j]));
                    }
                }
            } else {
                throw new NotImplementedException();
            }
            a.Add("\\end{matrix}");
            return new TeXRow(a);
        }

        protected override TeX.TeXStuff __Translate(LetterSym s) {
            if(s.Subscript is NullExpr) return _texmappings[s.Letter];
            else return new TeXRow(_texmappings[s.Letter], "_{", Translate(s.Subscript), "}");
        }

        protected override TeX.TeXStuff __Translate(WordSym s) {
            if(s.Subscript is NullExpr) return "\\text{" + s.Word + "}";
            else return new TeXRow("\\text{" + s.Word + "}", "_{", Translate(s.Subscript), "}");
        }

        class LetterOrTeXcode {
            public char Letter;
            public string TeXcode;
            public LetterOrTeXcode(char letter) { Letter = letter; TeXcode = null; }
            public LetterOrTeXcode(string texcode) { Letter = (char)0; TeXcode = texcode; }
            public static implicit operator LetterOrTeXcode(char l) { return new LetterOrTeXcode(l); }
            public static implicit operator LetterOrTeXcode(string tc) { return new LetterOrTeXcode(tc); }
        }
        private static Dictionary<Syntax.WOrC, string> _texmappings = new Dictionary<Syntax.WOrC, string>();
        private static void AddMappings(params LetterOrTeXcode[] mapelts) {
            Trace.Assert(mapelts.Length%2 == 0);
            for(int i = 0; i < mapelts.Length; i+=2) {
                _texmappings[mapelts[i].Letter] = mapelts[i+1].TeXcode;
            }
        }
        static TeX() {
            string identity = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ.:;,?!()[]-/*@+=|<>";
            foreach(char c in identity) _texmappings[c] = c.ToString();
            _texmappings["mod"] = "\\bmod ";
            AddMappings('α', "\\alpha ",
                'β', "\\beta ",
                'γ', "\\gamma ",
                'δ', "\\delta ",
                'ε', "\\epsilon ",
                'ζ', "\\zeta ",
                'η', "\\eta ",
                'θ', "\\theta ",
                'ι', "\\iota ",
                'κ', "\\kappa ",
                'λ', "\\lamda ",
                'μ', "\\mu ",
                'ν', "\\nu ",
                'ξ', "\\xi ",
                //'', "\\OMICRON ",
                'π', "\\pi ",
                'ρ', "\\rho ",
                //'', "\\FINAL SIGMA ",
                'σ', "\\sigma ",
                'τ', "\\tau ",
                //'', "\\UPSILON ",
                'φ', "\\phi ",
                'χ', "\\chi ",
                'ψ', "\\psi ",
                'ω', "\\omega ",
                'Γ', "\\Gamma ",
                'Δ', "\\Delta ",
                'Θ', "\\Theta ",
                'Λ', "\\Lambda ",
                'Ξ', "\\Xi ",
                Unicode.G.GREEK_CAPITAL_LETTER_PI, "\\Pi ",
                'Σ', "\\Sigma ",
                Unicode.G.GREEK_CAPITAL_LETTER_UPSILON, "\\Upsilon ",
                'Φ', "\\Phi ",
                'Ψ', "\\Psi ",
                'Ω', "\\Omega ",
                Unicode.P.PLUS_MINUS_SIGN, "\\pm ",
                Unicode.M.MINUS_OR_PLUS_SIGN, "\\mp ",
                Unicode.M.MULTIPLICATION_SIGN, "\\times ",
                Unicode.D.DIVISION_SIGN, "\\div ",
                '*', "\\ast ",
                Unicode.S.STAR_OPERATOR, "\\star ",
                Unicode.R.RING_OPERATOR, "\\circ ",
                Unicode.B.BULLET_OPERATOR, "\\bullet ",
                Unicode.D.DOT_OPERATOR, "\\cdot ",
                Unicode.I.INTERSECTION, "\\cap ",
                Unicode.U.UNION, "\\cup ",
                // uplus, sqcap, sqcup
                Unicode.L.LOGICAL_OR, "\\vee ",
                Unicode.L.LOGICAL_AND, "\\wedge ",
                Unicode.S.SET_MINUS, "\\setminus ",
                // wr, diamond, bigtriangleup, bigtriangledown, triangleleft, triangleright ...
                Unicode.C.CIRCLED_PLUS, "\\oplus ",
                Unicode.C.CIRCLED_MINUS, "\\ominus ",
                Unicode.C.CIRCLED_TIMES, "\\otimes ",
                Unicode.C.CIRCLED_DIVISION_SLASH, "\\oslash ",
                Unicode.C.CIRCLED_DOT_OPERATOR, "\\odot ",
                // bigcirc
                Unicode.D.DAGGER, "\\dagger ",
                Unicode.D.DOUBLE_DAGGER, "\\ddagger ",
                Unicode.A.AMALGAMATION_OR_COPRODUCT, "\\amalg ",
                Unicode.L.LESS_THAN_OR_EQUAL_TO, "\\leq ",
                Unicode.G.GREATER_THAN_OR_EQUAL_TO, "\\geq ",
                Unicode.I.IDENTICAL_TO, "\\equiv ",
                // models, prec, succ
                '~', "\\sim ",
                Unicode.T.TILDE_OPERATOR, "\\sim ",
                // perp, preceq, succeq
                Unicode.A.ASYMPTOTICALLY_EQUAL_TO, "\\simeq ",
                Unicode.D.DIVIDES, "\\mid ",
                // ll ...
                Unicode.S.SUBSET_OF, "\\subset ",
                Unicode.S.SUPERSET_OF, "\\supset ",
                Unicode.A.ALMOST_EQUAL_TO, "\\approx ",
                // bowtie...
                Unicode.S.SUBSET_OF_OR_EQUAL_TO, "\\subseteq ",
                Unicode.S.SUPERSET_OF_OR_EQUAL_TO, "\\supseteq ",
                Unicode.A.APPROXIMATELY_EQUAL_TO, "\\cong ",
                // join...
                Unicode.N.NOT_EQUAL_TO, "\\neq ",
                // smile...
                Unicode.E.ELEMENT_OF, "\\in ",
                // ni...
                Unicode.P.PROPORTIONAL_TO, "\\propto ",
                // vdash, dashv
                Unicode.L.LEFTWARDS_ARROW, "\\leftarrow ",
                Unicode.R.RIGHTWARDS_ARROW, "\\rightarrow ",
                Unicode.R.RIGHTWARDS_DOUBLE_ARROW, "\\Rightarrow ",
                Unicode.U.UPWARDS_ARROW, "\\uparrow ",
                Unicode.D.DOWNWARDS_ARROW, "\\downarrow ",
                Unicode.R.RIGHTWARDS_HARPOON_WITH_BARB_UPWARDS, "\\rightharpoonup ",
                Unicode.N.NORTH_EAST_ARROW, "\\nearrow ",
                Unicode.S.SOUTH_EAST_ARROW, "\\searrow ",
                Unicode.S.SOUTH_WEST_ARROW, "\\swarrow ",
                Unicode.N.NORTH_WEST_ARROW, "\\nwarrow ",
                // skipping all the other arrows
                Unicode.F.FOR_ALL, "\\forall ",
                Unicode.I.INFINITY, "\\infty ",
                Unicode.E.EMPTY_SET, "\\emptyset ",
                Unicode.T.THERE_EXISTS, "\\exists ",
                Unicode.N.NABLA, "\\nabla ",
                Unicode.N.NOT_SIGN, "\\neg ",
                Unicode.S.SQUARE_ROOT, "\\surd ",
                Unicode.B.BLACK_LETTER_CAPITAL_R, "\\Re ",
                Unicode.B.BLACK_LETTER_CAPITAL_I, "\\Im ",
                '\\', "\\backslash ",
                Unicode.A.ANGLE, "\\angle ",
                Unicode.P.PARTIAL_DIFFERENTIAL, "\\partial ",
                // spadesuit, mho
                Unicode.N.N_ARY_SUMMATION, "\\sum ",
                Unicode.N.N_ARY_PRODUCT, "\\prod ",
                Unicode.N.N_ARY_COPRODUCT, "\\coprod ",
                Unicode.I.INTEGRAL, "\\int ",
                Unicode.C.CONTOUR_INTEGRAL, "\\oint ",
                //skipping more...
                '{', "\\{",
                '}', "\\}",
                Unicode.V.VERTICAL_ELLIPSIS, "\\vdots ",
                Unicode.M.MIDLINE_HORIZONTAL_ELLIPSIS, "\\cdots ",
                Unicode.D.DOWN_RIGHT_DIAGONAL_ELLIPSIS, "\\ddots ",
                Unicode.L.LEFT_FLOOR, "\\lfloor ",
                Unicode.R.RIGHT_FLOOR, "\\rfloor ",
                Unicode.L.LEFT_CEILING, "\\lceil ",
                Unicode.R.RIGHT_CEILING, "\\rceil ",
                // lots and lots of other stuff skipped
                Unicode.D.DOUBLE_STRUCK_CAPITAL_C, "\\Bbb{C}",
                Unicode.D.DOUBLE_STRUCK_CAPITAL_H, "\\Bbb{H}",
                Unicode.D.DOUBLE_STRUCK_CAPITAL_N, "\\Bbb{N}",
                Unicode.D.DOUBLE_STRUCK_CAPITAL_P, "\\Bbb{P}",
                Unicode.D.DOUBLE_STRUCK_CAPITAL_Q, "\\Bbb{Q}",
                Unicode.D.DOUBLE_STRUCK_CAPITAL_R, "\\Bbb{R}",
                Unicode.D.DOUBLE_STRUCK_CAPITAL_Z, "\\Bbb{Z}",
                Unicode.D.DOUBLE_STRUCK_ITALIC_SMALL_D, "d", // FIXME differential d -- this and the next two should have been intercepted as WKSs
                Unicode.D.DOUBLE_STRUCK_ITALIC_SMALL_E, "e", // FIXME base of natural log
                Unicode.D.DOUBLE_STRUCK_ITALIC_SMALL_I, "i", // FIXME imaginary i
                //others
                Unicode.D.DIVISION_SLASH, "/",
                Unicode.M.MINUS_SIGN, "-"
                );
        }
    }
}
