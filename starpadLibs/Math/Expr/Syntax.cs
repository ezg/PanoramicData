using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using starPadSDK.UnicodeNs;

namespace starPadSDK.MathExpr {
    public class Syntax {
        /// <summary>
        /// Kind of operator or relation for syntax
        /// </summary>
        public enum K {
            /// <summary>
            /// Binary operator or relation, left-associative (A # B # C --> (A # B) # C); items from same set behave separately but at same level of precedence.
            /// </summary>
            BinLeft,
            /// <summary>
            /// Binary operator or relation, right associative (A # B # C --> A # (B # C)); items from same set behave separately but at same level precedence.
            /// </summary>
            BinRight,
            /// <summary>
            /// Binary operator or relation, must be used alone (eg, as A # B, never A # B # C).
            /// </summary>
            BinAlone,
            /// <summary>
            /// Binary operator or relation set, multiple of same item from set just generates one fn (A # B # C --> #(A, B, C));
            /// using different items from same set at the same level equivalent to logical and, like A = B > C --> A = B and B > C.
            /// </summary>
            BinAllOfLike,
            /// <summary>
            /// Binary operator or relation set, multiple of same item from set just generates one fn (A # B # C --> #(A, B, C));
            /// using different items from same set at the same level turns all but Expr == the first of the set into unary:
            /// A # B @ C # D --> #(A, B, @(C), D). This variant does not allow operators to be prefixes of the first element: no /a/b
            /// </summary>
            BinPrimaryAndSecondary,
            /// <summary>
            /// Binary operator or relation set, multiple of same item from set just generates one fn (A # B # C --> #(A, B, C));
            /// using different items from same set at the same level turns all but Expr == the first of the set into unary:
            /// A # B @ C # D --> #(A, B, @(C), D). This variant allows operators to be prefixes of the first element: -a+b.
            /// </summary>
            BinPrimaryAndSecondary2,
            /// <summary>
            /// Prefix operator
            /// </summary>
            Prefix,
            /// <summary>
            /// Postfix operator like !
            /// </summary>
            Postfix,
            /// <summary>
            /// Prefix operator, but can take no arguments
            /// </summary>
            PrefixOpt
        }
        /// <summary>
        /// Is it an operator or relation or what? These should be set from the TeX types, in same order. (See the TeXBook, p 158.)
        /// </summary>
        public enum TType {
            /// <summary>
            /// Ordinary symbol (a, b, etc)
            /// </summary>
            Ord,
            /// <summary>
            /// Large unary operator (∑, etc) or roman word unary operator (sin, etc) (TeX Op)
            /// </summary>
            LargeOp,
            /// <summary>
            /// Regular binary operator (+, -, /, etc) (TeX Bin)
            /// </summary>
            Op,
            /// <summary>
            /// Relation (=, ≠, &lt;, etc)
            /// </summary>
            Rel,
            /// <summary>
            /// Opening delimiter ('(')
            /// </summary>
            Open,
            /// <summary>
            /// Closing delimiter (')')
            /// </summary>
            Close,
            /// <summary>
            /// Punctuation (',', etc)
            /// </summary>
            Punct,
            /// <summary>
            /// A delimited subformula (p 170); also fractions
            /// </summary>
            Inner,
            /// <summary>
            /// Overlined atom like x with a bar over it
            /// </summary>
            Over,
            /// <summary>
            /// Underlined atom like x with a bar under it
            /// </summary>
            Under,
            /// <summary>
            /// Accented atom like x with ^ over it
            /// </summary>
            Acc,
            /// <summary>
            /// Radical atom like sqrt of 2
            /// </summary>
            Rad,
            /// <summary>
            /// Vbox to be centered, produced by \vcenter
            /// </summary>
            Vcent,
            /// <summary>
            /// *Not* in the TeXbook, this should never appear on a box; it's for use as a sentry in algorithms
            /// </summary>
            None,
        }
        public class WOrC {
            private char _character; public char Character { get { return _character; } }
            private string _word; public string Word { get { return _word; } }

            internal WOrC() {
                _character = (char)0;
                _word = null;
            }
            public WOrC(char c) {
                // allow null WOrCs: don't Trace.Assert(c != (char)0);
                _character = c;
                _word = null;
            }
            public WOrC(string w) {
                Trace.Assert(w != null);
                _character = (char)0;
                _word = w;
            }
            public WOrC(WOrC r) {
                _character = r._character;
                _word = r._word;
            }
            public static implicit operator WOrC(char c) { return new WOrC(c); }
            public static implicit operator WOrC(string w) { return new WOrC(w); }

            public static bool operator==(WOrC r, char c) {
                return c == (char)0 ? (r.Character == (char)0 && r.Word == null)
                    : (r.Character == c);
            }
            public static bool operator!=(WOrC r, char c) { return !(r == c); }
            public static bool operator==(WOrC r, string w) {
                return w == null ? (Object.ReferenceEquals(r, null) || (r.Character == (char)0 && r.Word == null))
                    : (r.Word == w);
            }
            public static bool operator!=(WOrC r, string w) { return !(r == w); }

            public static bool operator==(WOrC r, WOrC r2) {
                return (r == null && r2 == null) ||
                    (r != null && r2 != null && r.Character == r2.Character && r.Word == r2.Word);
            }
            public static bool operator!=(WOrC r, WOrC r2) { return !(r == r2); }

            public override bool Equals(object obj) {
                WOrC r2 = obj as WOrC;
                if(r2 == null) return false;
                return this == r2;
            }
            public override int GetHashCode() {
                return Character.GetHashCode() ^ (Word == null ? 0 : Word.GetHashCode());
            }
            public override string ToString() {
                return Word != null ? Word : Character.ToString();
            }

            public string Label() {
                string label;
                if(Character != (char)0) label = Character.ToString() + " (" + Unicode.NameOf(Character) + ")";
                else if(Word != null) label = Word;
                else label = "null?";
                return label;
            }
        }
            
        public class OpRel {
            private K _kind;
            public K Kind {
                get { return _kind; }
            }
            private Expr _identity;
            public Expr Identity {
                get { return _identity; }
            }
            private WOrC[] _ops;
            public WOrC[] Ops {
                get { return _ops; }
            }
            private Expr[] _heads;
            public Expr[] Heads {
                get { return _heads; }
            }
            private TType[] _types;
            public TType[] Types {
                get { return _types; }
            }
	
            public class OpExprOrType {
                private WOrC _op;
                public WOrC Op {
                    get { return _op; }
                }
                private Expr _exp;
                public Expr Exp {
                    get { return _exp; }
                }
                private TType _type;
                public TType Type {
                    get { return _type; }
                }
                public enum which { Op, Expr, Type };
                private which _which;
                public which Which {
                    get { return _which; }
                }
                public OpExprOrType(WOrC op) { _op = op; _which = which.Op; }
                public OpExprOrType(char op) : this((WOrC)op) { }
                public OpExprOrType(string op) : this((WOrC)op) { }
                public OpExprOrType(Expr exp) { _exp = exp; _which = which.Expr; }
                public OpExprOrType(TType type) { _type = type; _which = which.Type; }
                public static implicit operator OpExprOrType(WOrC op) { return new OpExprOrType(op); }
                public static implicit operator OpExprOrType(char op) { return new OpExprOrType(op); }
                public static implicit operator OpExprOrType(string op) { return new OpExprOrType(op); }
                public static implicit operator OpExprOrType(Expr exp) { return new OpExprOrType(exp); }
                public static implicit operator OpExprOrType(TType type) { return new OpExprOrType(type); }
            }
            public OpRel(K kind, TType type, params OpExprOrType[] ooes) : this(kind, null, type, ooes) { }
            public OpRel(K kind, Expr identity, TType type, params OpExprOrType[] ooes) {
                _kind = kind;
                TType deftype = type;
                _identity = identity;
                List<WOrC> ops = new List<WOrC>();
                List<Expr> heads = new List<Expr>();
                List<TType> types = new List<TType>();
                foreach(OpExprOrType ooe in ooes) {
                    switch(ooe.Which) {
                        case OpExprOrType.which.Op:
                            ops.Add(ooe.Op);
                            heads.Add(null);
                            types.Add(deftype);
                            break;
                        case OpExprOrType.which.Expr:
                            heads[heads.Count-1] = ooe.Exp;
                            break;
                        case OpExprOrType.which.Type:
                            types[types.Count-1] = ooe.Type;
                            break;
                    }
                }
                _ops = ops.ToArray();
                _heads = heads.ToArray();
                _types = types.ToArray();
            }
        }
        private OpRel[] _table;
        /// <summary>
        /// Ordered from high precedence to low.
        /// </summary>
        public OpRel[] Table {
            get { return _table; }
        }
        public struct OpRelPos {
            public int Precedence;
            public int Position;
            public OpRelPos(int pr, int po) { Precedence = pr; Position = po; }
        }
        private Dictionary<Expr, OpRelPos> _eToORP = new Dictionary<Expr, OpRelPos>();
        public OpRelPos Find(Expr e) {
            OpRelPos orp;
            if(_eToORP.TryGetValue(e, out orp)) return orp;
            else return new OpRelPos(-1, -1);
        }
        private Dictionary<WOrC, Expr> _cToHead = new Dictionary<WOrC, Expr>();
        public Expr Translate(WOrC c) {
            Expr e;
            if(_cToHead.TryGetValue(c, out e)) return e.Clone();
            else return null;
        }
        public Syntax(params OpRel[] ors) {
            _table = ors;
            for(int i = 0; i < ors.Length; i++) {
                for(int j = 0; j < ors[i].Ops.Length; j++) {
                    if(!_eToORP.ContainsKey(ors[i].Heads[j])) _eToORP[ors[i].Heads[j]] = new OpRelPos(i, j);
                    _cToHead[ors[i].Ops[j]] = ors[i].Heads[j];
                }
            }
        }
        public static Syntax Alternation = new Syntax(
            new OpRel(K.Prefix, TType.Ord, '∀', new LetterSym('∀')),
            new OpRel(K.Prefix, TType.LargeOp, "if", new WordSym("if")),
            new OpRel(K.PrefixOpt, TType.LargeOp, "else", new WordSym("else")),
            new OpRel(K.Prefix, TType.LargeOp, "while", new WordSym("while")),
            //new OpRel(K.Prefix, TType.LargeOp, "func", new WordSym("func")),
            new OpRel(K.BinAllOfLike, TType.Rel, Unicode.I.IDENTICAL_TO, new WordSym("ident")),
            // FIXME everything except element_of here really needs yet another new kind of syntax that takes a*b*c@d -> a*b and b*c and c%d (not a*b*c and c%d)
            new OpRel(K.BinAlone, TType.Op, Unicode.E.ELEMENT_OF, WellKnownSym.elementof, Unicode.S.SUBSET_OF, WellKnownSym.subsetof,
                Unicode.S.SUPERSET_OF, WellKnownSym.supersetof, Unicode.S.SUBSET_OF_OR_EQUAL_TO, WellKnownSym.subseteq,
                Unicode.S.SUPERSET_OF_OR_EQUAL_TO, WellKnownSym.superseteq),
            // FIXME: need => and -> here, just as infix (suffix done as special case at start of parse)
            new OpRel(K.BinAllOfLike, TType.Punct, ',', new WordSym("comma"), Unicode.I.INVISIBLE_SEPARATOR, new WordSym("comma")),
            // precedence switched for element_of and comma 
            new OpRel(K.BinAlone, TType.Rel, Unicode.E.EQUAL_TO_BY_DEFINITION, WellKnownSym.definition, Unicode.C.COLON_EQUALS, WellKnownSym.definition),
            new OpRel(K.BinRight, TType.Rel, Unicode.L.LEFTWARDS_ARROW, WellKnownSym.assignment),
            new OpRel(K.BinLeft, TType.Op, Unicode.L.LOGICAL_AND, WellKnownSym.logand, Unicode.L.LOGICAL_OR, WellKnownSym.logor),
            new OpRel(K.BinAllOfLike, TType.Rel, '=', WellKnownSym.equals, '<', WellKnownSym.lessthan, '>', WellKnownSym.greaterthan, '≤', WellKnownSym.lessequals,
                '≥', WellKnownSym.greaterequals, '≠', WellKnownSym.notequals, Unicode.A.ASYMPTOTICALLY_EQUAL_TO, new WordSym("asymp="),
                Unicode.A.APPROXIMATELY_EQUAL_TO, new WordSym("approx="), Unicode.A.ALMOST_EQUAL_TO, new WordSym("almost=")),
            new OpRel(K.Prefix, TType.LargeOp, Unicode.N.N_ARY_SUMMATION, WellKnownSym.summation),
            new OpRel(K.BinPrimaryAndSecondary2, TType.Op, '+', WellKnownSym.plus, Unicode.M.MINUS_SIGN, WellKnownSym.minus,
                Unicode.P.PLUS_MINUS_SIGN, WellKnownSym.plusminus, Unicode.M.MINUS_OR_PLUS_SIGN, WellKnownSym.minusplus, '-', WellKnownSym.minus),
            new OpRel(K.Prefix, TType.Ord, Unicode.N.NOT_SIGN, WellKnownSym.lognot),
            new OpRel(K.BinAlone, TType.Op, Unicode.M.MULTIPLICATION_SIGN, WellKnownSym.cross),
            new OpRel(K.BinAlone, TType.Op, "mod", WellKnownSym.mod),
            // below this line is not used by the parser except to translate characters
            new OpRel(K.BinPrimaryAndSecondary, new IntegerNumber(1), TType.Op, (char)0, WellKnownSym.times, Unicode.D.DOT_OPERATOR, WellKnownSym.dot,
                Unicode.I.INVISIBLE_TIMES, WellKnownSym.times, '/', WellKnownSym.divide, TType.Ord, Unicode.D.DIVISION_SIGN, WellKnownSym.divide, TType.Ord),
            new OpRel(K.Postfix, TType.Ord, '!', WellKnownSym.factorial)
            );

        //FIXME: more needed as above
        public static Syntax ErrorForm = new Syntax(
            // FIXME: need => and -> here, just as infix (suffix done as special case at start of parse)
            new OpRel(K.BinAllOfLike, TType.Punct, ',', new WordSym("comma"), Unicode.I.INVISIBLE_SEPARATOR, new WordSym("comma")),
            new OpRel(K.BinAllOfLike, TType.Rel, Unicode.I.IDENTICAL_TO, new WordSym("ident")),
            // FIXME everything except element_of here really needs yet another new kind of syntax that takes a*b*c@d -> a*b and b*c and c%d (not a*b*c and c%d)
            new OpRel(K.BinAlone, TType.Op, Unicode.E.ELEMENT_OF, WellKnownSym.elementof, Unicode.S.SUBSET_OF, WellKnownSym.subsetof,
                Unicode.S.SUPERSET_OF, WellKnownSym.supersetof, Unicode.S.SUBSET_OF_OR_EQUAL_TO, WellKnownSym.subseteq,
                Unicode.S.SUPERSET_OF_OR_EQUAL_TO, WellKnownSym.superseteq),
            new OpRel(K.BinAlone, TType.Rel, Unicode.E.EQUAL_TO_BY_DEFINITION, WellKnownSym.definition, Unicode.C.COLON_EQUALS, WellKnownSym.definition),
            new OpRel(K.BinRight, TType.Rel, Unicode.L.LEFTWARDS_ARROW, WellKnownSym.assignment),
            new OpRel(K.BinLeft, TType.Op, Unicode.L.LOGICAL_AND, WellKnownSym.logand, Unicode.L.LOGICAL_OR, WellKnownSym.logor),
            new OpRel(K.BinAllOfLike, TType.Rel, '=', WellKnownSym.equals, '<', WellKnownSym.lessthan, '>', WellKnownSym.greaterthan, '≤', WellKnownSym.lessequals,
                '≥', WellKnownSym.greaterequals, '≠', WellKnownSym.notequals, Unicode.A.ASYMPTOTICALLY_EQUAL_TO, new WordSym("asymp="),
                Unicode.A.APPROXIMATELY_EQUAL_TO, new WordSym("approx="), Unicode.A.ALMOST_EQUAL_TO, new WordSym("almost=")),
            new OpRel(K.Prefix, TType.LargeOp, Unicode.N.N_ARY_SUMMATION, WellKnownSym.summation),
            new OpRel(K.BinPrimaryAndSecondary2, TType.Op, '+', WellKnownSym.plus, Unicode.M.MINUS_SIGN, WellKnownSym.minus),
            new OpRel(K.BinAlone, TType.Op, Unicode.P.PLUS_MINUS_SIGN, WellKnownSym.plusminus),
            new OpRel(K.Prefix, TType.Ord, Unicode.N.NOT_SIGN, WellKnownSym.lognot),
            new OpRel(K.BinAlone, TType.Op, Unicode.M.MULTIPLICATION_SIGN, WellKnownSym.cross),
            // below this line is not used by the parser except to translate characters
            new OpRel(K.BinPrimaryAndSecondary, new IntegerNumber(1), TType.Op, (char)0, WellKnownSym.times, Unicode.D.DOT_OPERATOR, WellKnownSym.dot,
                Unicode.I.INVISIBLE_TIMES, WellKnownSym.times, '/', WellKnownSym.divide, TType.Ord, Unicode.D.DIVISION_SIGN, WellKnownSym.divide, TType.Ord),
            new OpRel(K.Postfix, TType.Ord, '!', WellKnownSym.factorial)
            );
        /// <summary>
        /// Infix, prefix, postfix, but not groupings (){}[]...
        /// </summary>
        public static Syntax Fixes = Alternation;

        public class CharWKSOrT {
            public WOrC C;
            public WKSID? WKS;
            public TType? T;
            public CharWKSOrT(WOrC c) { C = c; WKS = null; T = null; }
            public CharWKSOrT(WKSID wks) { C = null; WKS = wks; T = null; }
            public CharWKSOrT(TType t) { C = null; WKS = null; T = t; }
            public static implicit operator CharWKSOrT(WOrC c) { return new CharWKSOrT(c); }
            public static implicit operator CharWKSOrT(char c) { return new CharWKSOrT(c); }
            public static implicit operator CharWKSOrT(string c) { return new CharWKSOrT(c); }
            public static implicit operator CharWKSOrT(WKSID w) { return new CharWKSOrT(w); }
            public static implicit operator CharWKSOrT(TType t) { return new CharWKSOrT(t); }
        }
        public class CharWKSMapper {
            private Dictionary<WOrC, WKSID> _mapc = new Dictionary<WOrC, WKSID>();
            private Dictionary<WKSID, KeyValuePair<WOrC, TType>> _mapw = new Dictionary<WKSID, KeyValuePair<WOrC, TType>>();
            public WellKnownSym this[WOrC c] {
                get {
                    WKSID id;
                    if(_mapc.TryGetValue(c, out id)) return new WellKnownSym(id);
                    else return null;
                }
            }
            public KeyValuePair<WOrC, TType>? this[WellKnownSym wks] {
                get {
                    KeyValuePair<WOrC, TType> val;
                    if(_mapw.TryGetValue(wks.ID, out val)) return val;
                    else return null;
                }
            }
            public CharWKSMapper(params CharWKSOrT[] cows) {
                WOrC c = (char)0;
                TType t = TType.Ord;
                foreach(CharWKSOrT cow in cows) {
                    if(cow.C != null) {
                        Trace.Assert(c == (char)0);
                        c = cow.C;
                        t = TType.Ord;
                    } else if(cow.T.HasValue) {
                        Trace.Assert(c != (char)0);
                        t = cow.T.Value;
                    } else {
                        Trace.Assert(c != (char)0);
                        _mapc[c] = cow.WKS.Value;
                        _mapw[cow.WKS.Value] = new KeyValuePair<WOrC, TType>(c, t);
                        c = (char)0;
                        t = TType.Ord;
                    }
                }
            }
        }
        /// <summary>
        /// This is for WKS's corresponding to chars that are not operators.
        /// </summary>
        public static CharWKSMapper CharWKSMap = new CharWKSMapper(Unicode.I.INTEGRAL, TType.LargeOp, WKSID.integral, 'π', WKSID.pi, '∞', WKSID.infinity,
            Unicode.D.DOUBLE_STRUCK_CAPITAL_N, WKSID.naturals, Unicode.D.DOUBLE_STRUCK_CAPITAL_Z, WKSID.integers,
            Unicode.D.DOUBLE_STRUCK_CAPITAL_Q, WKSID.rationals, Unicode.D.DOUBLE_STRUCK_CAPITAL_R, WKSID.reals,
            Unicode.D.DOUBLE_STRUCK_CAPITAL_C, WKSID.complexes);
    }
}
