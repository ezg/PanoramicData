using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Ink;
using System.Collections;
using starPadSDK.MathExpr;
using starPadSDK.MathExpr.ExprWPF;
using starPadSDK.MathExpr.ExprWPF.Boxes;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using starPadSDK.CharRecognizer;
using starPadSDK.UnicodeNs;
using starPadSDK.Geom;
using System.Windows.Media;

namespace starPadSDK.MathRecognizer {
    public class Parse2 {
        class Parse2Exception : ApplicationException {
            public string Expln;
            public Parse2Exception(string expln)
                : base("A 2nd-level parse error occurred: " + expln) {
                Expln = expln;
            }
        }
        class ReparseException : ApplicationException {
            public string Expln;
            public ReparseException(string expln)
                : base("A 2nd-level parse error occurred: " + expln) {
                Expln = expln;
            }
        }
        class MissingValueExpr : ErrorExpr, GenericOutput<Box>.GenericOutputAwareExpr,
            TextAwareExpr {
            private class MissingValueBox : StringBox {
                public MissingValueBox() : base(null, "??", true, false, Syntax.TType.Ord) { }
            }
            public Box Translate(GenericOutput<Box>.Translator xlator) { return new MissingValueBox(); }
            public MissingValueExpr() { }
            public override bool Equals(Object obj) {
                return obj.GetType() == this.GetType();
            }
            public override int GetHashCode() {
                return 0;
            }
            public override Expr Clone() { return new MissingValueExpr(); }

            public string Convert(Func<Expr, string> t) {
                return "?missing?";
            }
            public string InputConvert(Func<Expr, string> t) {
                return "?missing?";
            }
        }
        public class UndersquiggleBox : Box {
            private Box _main;
            private Color _c;
            public UndersquiggleBox(Box main, Color c) : this(null, main, c) { }
            public UndersquiggleBox(Expr expr, Box main, Color c)
                : base(expr) {
                _main = main;
                _c = c;
            }
            protected override void _Measure(EDrawingContext edc) {
                Call_Measure(_main, edc);
                _bbox = _main.bbox;
                _bbox.Bottom += 5;
                _nombbox = _main.nombbox;
            }
            protected override void SaveMeasure(Pt refpt) {
                CallSaveMeasure(_main, refpt);
                SaveMeasureBBox(refpt);
            }
            public override void Draw(EDrawingContext edc, Pt refpt) {
                _main.Draw(edc, refpt);
                Pen pen = new Pen(new SolidColorBrush(Color.FromArgb(edc.Colr.A, 255, 0, 0)), 1);
                double top = bbox.Bottom - 3 + refpt.Y;
                double bot = top + 2;
                double x = bbox.Left + refpt.X;
                double stop = bbox.Right + refpt.X;
                bool attop = true;
                try {
                    edc.DC.PushClip(new RectangleGeometry(bbox + (Vec)refpt));
                    while(x < stop) {
                        double newx = x + 2;
                        edc.DC.DrawLine(pen, new Pt(x, attop ? top : bot), new Pt(newx, attop ? bot : top));
                        x = newx;
                        attop = !attop;
                    }
                } finally {
                    edc.DC.Pop();
                }
            }
            protected override Geometry _ComputeGeometry(EDrawingContext edc, Pt refpt) {
                GeometryGroup gg = new GeometryGroup();
                gg.Children.Add(Call_ComputeGeometry(_main, edc, refpt));
                double top = bbox.Bottom - 3 + refpt.Y;
                double bot = top + 2;
                double x = bbox.Left + refpt.X;
                double stop = bbox.Right + refpt.X;
                bool attop = true;
                RectangleGeometry clip = new RectangleGeometry(bbox + (Vec)refpt);
                // this would probably be better done by accumulating linesegments to send to pg.Figures and then get widened vers of pg, but it was faster to copy code from draw
                PathGeometry pg = new PathGeometry();
                while(x < stop) {
                    double newx = x + 2;
                    LineGeometry lg = new LineGeometry(new Pt(x, attop ? top : bot), new Pt(newx, attop ? bot : top));
                    pg.AddGeometry(lg.GetWidenedPathGeometry(new Pen(Brushes.Black, 1)));
                    x = newx;
                    attop = !attop;
                }
                gg.Children.Add(new CombinedGeometry(GeometryCombineMode.Intersect, clip, pg));
                return Geometry = gg;
            }
            public override IEnumerable<Box> SubBoxes { get { yield return _main; } }
        }
        class SyntaxErrorBox : NullBox, EWPF.LeafModifierBox {
            private bool _left; public bool Left { get { return _left; } }
            public SyntaxErrorBox(bool left) { _left = left; }
            public void ModifyLeft(EWPF.BoxPath left, EWPF.BoxPath self) {
                if(Left) left.Final.Target = new UndersquiggleBox(left.Final.Target, Colors.Red);
            }
            public void ModifyRight(EWPF.BoxPath self, EWPF.BoxPath right) {
                if(!Left) right.Final.Target = new UndersquiggleBox(right.Final.Target, Colors.Red);
            }
        }
        public class SyntaxErrorExpr : ErrorExpr, GenericOutput<Box>.GenericOutputAwareExpr,
            TextAwareExpr {
            private bool _left; public bool Left { get { return _left; } }
            public Box Translate(GenericOutput<Box>.Translator xlator) { return new SyntaxErrorBox(Left); }
            public SyntaxErrorExpr(bool left) { _left = left; }
            public override bool Equals(Object obj) {
                return obj.GetType() == this.GetType() && ((SyntaxErrorExpr)obj)._left == _left;
            }
            public override int GetHashCode() {
                return _left.GetHashCode();
            }
            public override Expr Clone() { return new SyntaxErrorExpr(Left); }

            public string Convert(Func<Expr, string> t) {
                return _left ? "<-error!" : "!error->";
            }
            public string InputConvert(Func<Expr, string> t) {
                return _left ? "<-error!" : "!error->";
            }
        }
        public class SyntaxJunkErrorExpr : ErrorExpr, GenericOutput<Box>.GenericOutputAwareExpr,
            TextAwareExpr {
            private Expr _e; public Expr E { get { return _e; } }
            private List<Expr> _le; public List<Expr> LE { get { return _le; } }
            public Box Translate(GenericOutput<Box>.Translator xlator) {
                List<Box> lb = new List<Box>();
                foreach(Expr e in LE) lb.Add(xlator(e));
                return new HBox(xlator(E), new UndersquiggleBox(new HBox(lb), Colors.Plum));
            }
            public SyntaxJunkErrorExpr(Expr e, List<Expr> le) { _e = e; _le = le; }
            public override bool Equals(Object obj) {
                // FIXME this is wrong
                return obj.GetType() == this.GetType() && ((SyntaxJunkErrorExpr)obj)._e == _e && ((SyntaxJunkErrorExpr)obj)._le == _le;
            }
            public override int GetHashCode() {
                // FIXME this is wrong
                return _e.GetHashCode() ^ _le.GetHashCode();
            }
            // FIXME this is wrong
            public override Expr Clone() { return new SyntaxJunkErrorExpr(E, LE); }

            public string Convert(Func<Expr, string> t) {
                string result = "!(" + t(E) + " !";
                foreach(Expr e in LE) result += " " + t(e);
                result += ")!";
                return result;
            }
            public string InputConvert(Func<Expr, string> t) {
                string result = "!(" + t(E) + " !";
                foreach(Expr e in LE) result += " " + t(e);
                result += ")!";
                return result;
            }
        }
        private Parser.ParseResult _parseResult; public Parser.ParseResult ParseResult { get { return _parseResult; } }
        private bool _parseError; public bool ParseError { get { return _parseError; } }
        private bool _finalSubsts;
        public Parser MyParser { get; private set; }
        public Parse2(Parser p, Parser.ParseResult pr) : this(p, pr, false) { }
        public Parse2(Parser p, Parser.ParseResult pr, bool finalsub) {
            MyParser = p;
            _parseResult = pr;
            _finalSubsts = finalsub;
        }
        public static Parser.ParseResult Parse(Parser p1, Parser.ParseResult pr) {
            Parse2 p = new Parse2(p1, pr);
            p.Parse();
            return p.ParseResult;
        }
        public void Parse() {
            bool okay = false;
            while(!okay) {
                try {
                    _parseError = false;
                    if(_parseResult != null) {
                        _parseResult.expr = Parse(_parseResult.root);
                        _parseResult.parseError = _parseError;
                        //Console.WriteLine("Parsed as " + Text.InputConvert(_parseResult.expr));
                    }
                    okay = true;
                } catch(Parse2Exception pe) {
                    Console.WriteLine("Parsed as null: " + pe.Expln);
                    _parseResult.expr = null;
                    okay = true;
                } catch(ReparseException) {
                }
            }
        }
        public abstract class Grouping {
            /// <summary>
            /// return -1 if not at something beginning this grouping, otherwise return # of chars to skip (generally 1).
            /// optional is true if the beginning may have meaning without a matching close--and therefore if the alt should
            /// never change away from an open for this.
            /// </summary>
            public abstract int IsBegin(Line l, int i, out bool optional);
            /// <summary>
            /// return -1 if not at something ending this grouping, otherwise return # of chars to skip (generally 1).
            /// optional is true if the end grouping may have meaning without a matching open (eg, dx, dy, etc).
            /// </summary>
            public abstract int IsEnd(Line l, int i, out bool optional);
            /// <summary>
            /// Given the Expr of the contents of the group, and the index of the chars beginning and ending the group, return the
            /// Expr for the group.
            /// </summary>
            public abstract Expr Package(Parse2 p, Expr e, Line l, int start, int end);
        }
        public class BasicGrouping : Grouping {
            private char _startchar;
            private char _endchar;
            private Expr _head;
            public BasicGrouping(char schar, char echar, Expr head) {
                _startchar = schar;
                _endchar = echar;
                _head = head;
            }
            // FIXME: this is an annoying hack to be stuck at this level
            private bool isntmatrix(Symbol s) {
                ParenSym ps = s as ParenSym;
                return ps == null || ps.OneRow;
            }
            public override int IsBegin(Line l, int i, out bool opt) { opt = false; return l._syms[i].r.alt == _startchar && isntmatrix(l._syms[i]) ? 1 : -1; }
            public override int IsEnd(Line l, int i, out bool opt) { opt = false; return l._syms[i].r.alt == _endchar ? 1 : -1; }
            public override Expr Package(Parse2 p, Expr e, Line l, int start, int end) {
                if(_head != null) {
                    CompositeExpr ce = e as CompositeExpr;
                    if(ce != null && ce.Head == new WordSym("comma")) {
                        e = new CompositeExpr(_head.Clone(), ce.Args);
                        Symbol[] syms = ce.Annotations["Symbols"] as Symbol[];
                        if(syms != null) {
                            e.Annotations["Symbols"] = syms;
                            foreach(Symbol s in syms) if(s != null) s.expr = e;
                        }
                    } else e = new CompositeExpr(_head.Clone(), e);

                    Dictionary<string, Symbol> otherdelims = e.Annotations["otherdelims"] as Dictionary<string, Symbol>;
                    if(otherdelims == null) otherdelims = new Dictionary<string, Symbol>();
                    if(_startchar == _endchar) {
                        otherdelims[_startchar.ToString()+"l"] = l._syms[start];
                        otherdelims[_endchar.ToString()+"r"] = l._syms[end];
                    } else {
                        otherdelims[_startchar.ToString()] = l._syms[start];
                        otherdelims[_endchar.ToString()] = l._syms[end];
                    }
                    e.Annotations["otherdelims"] = otherdelims;
                } else {
                    int? num = e.Annotations["Force Parentheses"] as int?;
                    if(!num.HasValue) num = 1;
                    else num++;
                    e.Annotations["Force Parentheses"] = num.Value;

                    List<Symbol> delims = e.Annotations["delimiters"] as List<Symbol>;
                    if(delims == null) delims = new List<Symbol>();
                    delims.Insert(0, l._syms[start]);
                    delims.Add(l._syms[end]);
                    e.Annotations["delimiters"] = delims;
                }
                l._syms[start].expr = e;
                l._syms[end].expr = e;

                if(!l._syms[end].Sub.Empty) {
                    Expr sub = p.Parse(l._syms[end].Sub);
                    e = new CompositeExpr(WellKnownSym.subscript, e, sub);
                }
                if(!l._syms[end].Super.Empty) {
                    Expr super = p.Parse(l._syms[end].Super);
                    e = new CompositeExpr(WellKnownSym.power, e, super);
                }
                return e;
            }
        }
        public class AbstractGrouping : Grouping {
            public delegate int IsBeginner(Line l, int i, out bool opt);
            public delegate int IsEnder(Line l, int i, out bool opt);
            private IsBeginner _isBegin;
            private IsEnder _isEnd;
            public delegate Expr Packager(Parse2 p, Expr e, Line l, int start, int end);
            private Packager _package;
            public AbstractGrouping(IsBeginner isBegin, IsEnder isEnd, Packager package) {
                _isBegin = isBegin;
                _isEnd = isEnd;
                _package = package;
            }
            public override int IsBegin(Line l, int i, out bool opt) { return _isBegin(l, i, out opt); }
            public override int IsEnd(Line l, int i, out bool opt) { return _isEnd(l, i, out opt); }
            public override Expr Package(Parse2 p, Expr e, Line l, int start, int end) { return _package(p, e, l, start, end); }
        }
        public class IntegralGrouping : Grouping {
            public override int IsBegin(Line l, int i, out bool opt) { opt = true; return l._syms[i].r.alt == Unicode.I.INTEGRAL ? 1 : -1; }
            public override int IsEnd(Line l, int i, out bool opt) {
                opt = true;
                if(l._syms[i].r.alt == 'd' && l._syms[i].Sub.Empty && l._syms[i].Super.Empty) {
                    if(i < l._syms.Count - 1 && Char.IsLetter(l._syms[i + 1].r.alt.Character)) {
                        return 2;
                    } else return -1;
                } else {
                    string symb = l._syms[i].r.alt.Word;
                    if(symb != null && symb.Length == 2 && symb[0] == 'd') {
                        return 1;
                    } else return -1;
                }
            }
            public override Expr Package(Parse2 p, Expr e, Line l, int start, int end) {
                Expr dwhatever;
                if(l._syms[end].r.alt == 'd') {
                    Expr d = WellKnownSym.differentiald;
                    d.Annotations["Symbols"] = l._syms[end];
                    l._syms[end].expr = d;
                    Symbol v = l._syms[end + 1];
                    LetterSym ls = new LetterSym(v.r.alt.Character);
                    ls.Annotations["Symbols"] = v;
                    v.expr = ls;
                    if(!v.Sub.Empty) {
                        Expr sub = p.Parse(v.Sub);
                        ls.Subscript = sub;
                    }
                    dwhatever = new CompositeExpr(d, ls);
                    if(!v.Super.Empty) {
                        Expr super = p.Parse(v.Super);
                        dwhatever = new CompositeExpr(WellKnownSym.power, dwhatever, super);
                    }
                } else {
                    dwhatever = p.ConvertSymToExpr(l._syms[end]);
                }
                bool haslow = !l._syms[start].Sub.Empty;
                Expr low = haslow ? p.Parse(l._syms[start].Sub) : null;
                bool hashigh = !l._syms[start].Super.Empty;
                Expr high = hashigh ? p.Parse(l._syms[start].Super) : null;
                Expr i = WellKnownSym.integral;
                i.Annotations["Symbols"] = l._syms[start];
                l._syms[start].expr = i;
                if(haslow && hashigh) {
                    e = new CompositeExpr(i, e, dwhatever, low, high);
                } else {
                    if(haslow && !hashigh) e = new CompositeExpr(i, e, dwhatever, low);
                    else if(!haslow && hashigh) {
                        //throw new Parse2Exception("We don't handle integrals with just an upper bound");
                        e = new CompositeExpr(i, e, dwhatever, new MissingValueExpr(), high);
                        p._parseError = true;
                    } else e = new CompositeExpr(i, e, dwhatever);
                }
                return e;
            }
        }
        public class AbsGrouping : BasicGrouping {
            public AbsGrouping(char schar, Expr head) : base(schar, schar, head) { }
            public bool IsAbs(List<Symbol> l, int j) { return l[j].r.alt.Character == '|'; }
            public bool CanBeAbs(List<Symbol> l, int j) {
                if (l[j].r.levelsetby <= 2)
                    return IsAbs(l, j);
                for (int i = 0; i < l[j].r.alts.Length; i++)
                    if (l[j].r.alts[i].Character == '|')
                        return true;
                return false;
            }
            public int IsBegin(List<Symbol> l, int i, bool strict) {
                if (!CanBeAbs(l, i)  || i == l.Count-1)
                    return -1;
                // strict:   |a    a|a ?|a
                if (strict)
                    if ((i == 0 || (i > 0 && (!char.IsDigit(l[i-1].r.alt.Character) || l[i].r.alt.Character == '|'))) && 
                        i < l.Count-1 && (char.IsLetter(l[i+1].r.alt.Character) || l[i+1].r.alt.Character == '(' ||(l[i].r.alt.Character == '|' && (char.IsLetterOrDigit(l[i+1].r.alt.Character)))))
                        return 1;
                // unstrict: |a |# ?|a ?|#
                if (!strict)
                    if (i < l.Count-1 && (l[i+1].r.alt.Character == '(' || char.IsLetterOrDigit(l[i+1].r.alt.Character)))
                        return 1;

                return -1;
            }
            public int IsEnd(List<Symbol> l, int i,bool strict) {
                if (!CanBeAbs(l, i) || i == 0)
                    return -1;
                // strict:   a|  a|a  a|?
                // unstrict: a| #| a|? #|?
                if (strict)
                    if ((i == l.Count-1 || (i < l.Count-1 && (!char.IsDigit(l[i+1].r.alt.Character) || l[i].r.alt.Character == '|'))) &&  
                       i > 0 && (char.IsLetter(l[i-1].r.alt.Character) || l[i-1].r.alt.Character == ')' || (l[i].r.alt.Character == '|' && (char.IsLetterOrDigit(l[i-1].r.alt.Character)))))
                        return 1;
                if (!strict)
                    if ((i == l.Count-1 || !char.IsDigit(l[i+1].r.alt.Character) || l[i+1].r.alt.Character == '1') && i > 0 && (l[i-1].r.alt.Character == ')' || char.IsLetterOrDigit(l[i-1].r.alt.Character)))
                        return 1;
                return -1;
            }
            public void coerceAbs(List<Symbol> l, int i) {
                if (!IsAbs(l, i))
                    for (int j = 0; j < l[i].r.alts.Length; j++)
                        if (l[i].r.alts[j].Character == '|' && l[i].r.alts[0].Character != ')' && l[i].r.alts[0].Character != '(') {
                            l[i].r.curalt = j;
                            l[i].r.levelsetby = 2;
                            break;
                        }
            }
            public int IsBeginOrEnd(List<Symbol> l, int i, bool strict) { 
                return (IsBegin(l, i, strict) != -1 && IsEnd(l, i, strict) != -1) ? 1 : -1;
            }
            private int[] markAllKnownAbs(List<Symbol> l, int[] changemap) {
                int[] absMap = new int[l.Count];
                int parencount = 0;
                for (int i = 0; i < absMap.Length; i++) {
                    if (l[i].r.alt.Character == '(')
                        parencount++;
                    else if (l[i].r.alt.Character == ')')
                        parencount--;
                    if (parencount > 0)
                        absMap[i] = 101;
                    else if (changemap[i] == -1)
                        absMap[i] = IsBeginOrEnd(l, i, true)!=-1 ? 0 : (IsBegin(l, i, true)!=-1 ? -1 : IsEnd(l, i, true)!=-1? 1 : 100);
                }
                return absMap;
            }
            public void doAbsGroupingLoop(List<Symbol> l, int[] changemap) {
                List<Symbol> baseLevel = new List<Symbol>();
                List<Symbol> subLevel = new List<Symbol>();
                int parencount = 0;
                for (int i = 0; i < l.Count; i++) {
                    if (l[i].r.alt.Character == '(') {
                        if (parencount == 0)
                            baseLevel.Add(l[i]);
                        parencount++;
                        continue;
                    }
                    if (l[i].r.alt.Character == ')') {
                        parencount--;
                        if (parencount == 0) {
                            doAbsGroupingLoop(subLevel, changemap);
                            subLevel.Clear();
                            baseLevel.Add(l[i]);
                        }
                        continue;
                    }
                    if (parencount > 0)
                        subLevel.Add(l[i]);
                    else baseLevel.Add(l[i]);
                }
                doAbsGrouping(baseLevel, changemap);

            }
            private void doAbsGrouping(List<Symbol> l, int[] changemap) {
                bool matched = true;
                int count = 0;
                int numAbs = 0;
                for (int i = 0; i < l.Count; i++)
                    if (l[i].r.alt.Character == '|')
                        numAbs++;
                do {
                    for (int j = 0; j < l.Count; j++)
                        if (IsBegin(l, j, true) != -1 || IsEnd(l, j, true) != -1)
                            coerceAbs(l, j);
                    int[] absMap = markAllKnownAbs(l, changemap);
                    count = 0;
                    int lastAbs = -1;
                    matched = true;
                    for (int i = 0; i < l.Count; i++) {
                        if (absMap[i] >= 100)
                            continue;
                        else if (count % 2 == 0) {
                            if (absMap[i] < 1) {
                                lastAbs = i;
                                count++;
                            } else { // try to find an open
                                for (int c = lastAbs+1; c < i; c++)
                                    if (IsBegin(l, c, false) != -1) {
                                        coerceAbs(l, c);
                                        matched = false;
                                    }
                                if (matched && l[i].r.levelsetby > 1)
                                    l[i].r.curalt = 0;
                                break;
                            }
                        } else {
                            if (absMap[i] > -1) {
                                lastAbs = i;
                                count++;
                            } else { // try to find a close
                                for (int c = lastAbs+2; c < i; c++)
                                    if (IsEnd(l, c, false) != -1) {
                                        coerceAbs(l, c);
                                        matched = false;
                                    }
                                break;
                            }
                        }
                    }
                    if (matched && (count%2) == 1) {
                        for (int c = lastAbs+1; c < l.Count; c++)
                            if (absMap[c] < 101 && IsEnd(l, c, false) != -1) {
                                if (!IsAbs(l, c)) {
                                    coerceAbs(l, c);
                                    matched = false;
                                }
                                break;
                            }
                    }
                    if (matched && (count%2) == 1) {
                        for (int c = lastAbs-1; c >= 0; c--)
                            if (absMap[c] < 101 && IsBegin(l, c, false) != -1) {
                                if (!IsAbs(l, c)) {
                                    coerceAbs(l, c);
                                    matched = false;
                                }
                                break;
                            }
                    }
                    if (matched && (count%2) == 1) {
                        int c = 0;
                        for (; c < l.Count; c++)
                            if (absMap[c] < 101 && IsBegin(l, c, false) != -1) {
                                for (int j = c+2; j < l.Count; j++)
                                    if (absMap[j] < 101 && IsEnd(l, j, false) != -1 && !IsAbs(l, j)) {
                                        coerceAbs(l, j);
                                        matched = false;
                                        c = l.Count;
                                        break;
                                    }
                            }
                    }
                    if (matched && (count%2) ==1) {
                        for (int i = 0; i < l.Count && matched; i++)
                            if (absMap[i] < 1 && i < l.Count-2) {
                                for (int j = 0; j < l[i].r.alts.Length; j++)
                                    if (l[i].r.alts[j].Character == 'l' && l[i+1].r.alt.Character == 'n' && (char.IsLetterOrDigit(l[i+2].r.alt.Character) || l[i+2].r.alt.Character == '(')) {
                                        l[i].r.curalt = j;
                                        l[i].r.levelsetby = 2;
                                        matched = false;
                                        break;
                                    }
                            }
                    }
                } while (!matched);

                for (int i = 0; i < l.Count; i++)
                    if (l[i].r.alt.Character == '|')
                        numAbs--;
                if (numAbs != 0) {
                    for (int i = 0; i < l.Count; i++)
                        if (l[i].r.levelsetby <= 2)
                            l[i].r.levelsetby = -l[i].r.levelsetby;
                    throw new Reparse1Exception("fixed some abs signs");
                }
            }
        }
        private static Grouping[] _groupings = new Grouping[] {
            new BasicGrouping('(', ')', null),
            new BasicGrouping('[', ']', new WordSym("bracket")),
            new BasicGrouping('{', '}', new WordSym("brace")),
            new AbsGrouping('|', WellKnownSym.magnitude),
            new BasicGrouping(Unicode.L.LEFT_FLOOR, Unicode.R.RIGHT_FLOOR, WellKnownSym.floor),
            new BasicGrouping(Unicode.L.LEFT_CEILING, Unicode.R.RIGHT_CEILING, WellKnownSym.ceiling),
            new IntegralGrouping()
        };
        public class ParenMatch {
            public Grouping type;
            public int size;
            public int matching;
            public ParenMatch() {
                type = null;
                size = -1;
                matching = -1;
            }
        }
        private void AddSymsToLine(Line tgt, Line src) {
            List<Symbol> srcsyms = new List<Symbol>();
            foreach(Symbol s in src._syms)
                srcsyms.Add(s);
            for(int i = 0; i < srcsyms.Count; i++) {
                Symbol s = srcsyms[i];
                if(s.Sym == Unicode.D.DIVISION_SLASH && s.Super.Empty && !s.Sub.Empty) {
                    tgt._syms.Add(s);
                    Symbol refs = s;
                    foreach(Symbol sub in s.Sub._syms) {
                        tgt._syms.Add(sub);
                        src._syms.Insert(src._syms.IndexOf(refs)+1, sub);
                    }
                    s.Sub._syms.Clear();
                } else
                    tgt._syms.Add(s);
                IntSym ints = s as IntSym;
                if(ints != null) AddSymsToLine(tgt, ints.Integrand);
                ParenSym ps = s as ParenSym;
                if(ps != null) {
                    if(ps.OneRow) {
                        if(ps.rows.Count > 0) AddSymsToLine(tgt, ps.rows[0][0]);
                    } else if(i < srcsyms.Count - 1 && srcsyms[i+1].Sym == ')') {
                        ps.Parse2Closing = srcsyms[i+1];
                        i++;
                    }
                }
            }
        }
        private void MoveOrphanedCommas(Line tgt, Line sibLine, int sibIndex) {
            for(int i = 0; i < tgt._syms.Count; i++) {
                if(i == tgt._syms.Count - 1) {
                    if((tgt._syms[i].Sym == ',' || tgt._syms[i].Sym == '.') && sibLine != null) {
                        sibLine._syms.Insert(sibIndex, tgt._syms[i]);
                        tgt._syms.RemoveAt(i);
                        return;
                    }
                }
                if(!tgt._syms[i].Sub.Empty)
                    MoveOrphanedCommas(tgt._syms[i].Sub, tgt, i + 1);
                if(tgt._syms[i] is ParenSym) {
                    foreach(List<Line> ll in (tgt._syms[i] as ParenSym).rows) {
                        foreach(Line pl in ll) {
                            MoveOrphanedCommas(pl, null, 0);
                        }
                    }
                }
            }
        }
        private Expr Parse(Line l0) {
            MoveOrphanedCommas(l0, null, 0);
            /* Split contents of integrals and single-line parensyms out into actual siblings as contents of the line */
            Line l = new Line();
            AddSymsToLine(l, l0);

            /* pick alternates so words match */
            for(int i = 0; i < l._syms.Count; i++) {
                Expr e = MaybeMatchMultisymbol(l._syms, ref i);
                if(e != null) i--;
            }

            /* pick alternates so parens balance. simplistic method, only handles a very few things */
            BalanceParens(l);


            /* match parens, integral-dwhatever, and any other groupings */
            ParenMatch[] parens = MatchParens(l);

            /* convert to expr */
            return ConvertRunToExpr(l, 0, l._syms.Count - 1, parens);
        }

        private void BalanceParens(Line l) {
            /* Count mismatched opens and closes of each type */
            int[] opens = new int[_groupings.Length];
            for(int i = 0; i < opens.Length; i++) opens[i] = 0;

            /* get rid of any opens at the end of an expression */
            if(_finalSubsts && l._syms.Count > 0) {
                int lastix = l._syms.Count - 1;
                Symbol last = l._syms[lastix];
                if(last.r.levelsetby > 2) {
                    bool mustchange = Array.Exists(_groupings, delegate(Grouping g) { bool opt; return g.IsBegin(l, lastix, out opt) > -1 && !opt; });
                    if(mustchange) {
                        int origalt = last.r.curalt;
                        for(int i = 0; i < last.r.alts.Length && mustchange; i++) {
                            last.r.curalt = i;
                            mustchange = Array.Exists(_groupings, delegate(Grouping g) { bool opt; return g.IsBegin(l, lastix, out opt) > -1 && !opt; });
                        }
                        if(mustchange) last.r.curalt = origalt;
                        else last.r.levelsetby = 2;
                    }
                }
            }

            int[] changemap = new int[l._syms.Count];
            for(int i = 0; i < l._syms.Count; i++) changemap[i] = -1;
            int numfixed = 0;
            for (int gi = 0; gi < _groupings.Length; gi++) {
                List<Symbol> balanced = new List<Symbol>();
                Grouping g = _groupings[gi];
                if (g is AbsGrouping) {
                    (g as AbsGrouping).doAbsGroupingLoop(l._syms, changemap);
                    numfixed++;
                    continue;
                }
                for(int i = 0; i < l._syms.Count; i++) {
                    if (changemap[i] != -1)
                        continue;
                    bool opt;
                    int size = g.IsBegin(l, i, out opt);
                    if (opens[gi] == 0) {
                        if (size > -1 && !opt) {
                            opens[gi]++;
                            balanced.Add(l._syms[i]);
                            i += size - 1;
                            continue;
                        }
                    }
                    size = g.IsEnd(l, i, out opt);
                    if(size > -1) {
                        if(opens[gi] <= 0 && opt) continue;
                        opens[gi]--;
                        balanced.Add(l._syms[i]);
                        i += size - 1;
                    }
                }
                if(opens[gi] == 0) {
                    foreach (Symbol s in balanced)
                        s.r.levelsetby = Math.Min(s.r.levelsetby, 2);
                    numfixed++;
                    continue;
                } else if(opens[gi] > 0) {
                    /* change something to a close or change an open to something else */
                    int nfix = numfixed;
                    if(BalanceParens1(l, delegate(Line ll, int i) { bool opt; return _groupings[gi].IsEnd(ll, i, out opt) != -1; },
                        delegate(Line ll, int i) { bool opt; return  _finalSubsts &&  _groupings[gi].IsBegin(ll, i, out opt) != -1; },
                        changemap, opens[gi], true, gi, _groupings[gi])) {
                        numfixed++;
                    }
                    if (nfix == numfixed) {
                        _finalSubsts = !_finalSubsts;
                        if (BalanceParens1(l, delegate(Line ll, int i) { bool opt; return _groupings[gi].IsEnd(ll, i, out opt) != -1; },
                            delegate(Line ll, int i) { bool opt; return _finalSubsts &&  _groupings[gi].IsBegin(ll, i, out opt) != -1; },
                            changemap, opens[gi], true, gi, _groupings[gi])) {
                            numfixed++;
                        }
                        _finalSubsts = !_finalSubsts;
                    }
                } else {
                    /* change something to an open or change a close to something else */
                    if (BalanceParens1(l, delegate(Line ll, int i) { bool opt; return _groupings[gi].IsBegin(ll, i, out opt) != -1; },
                        delegate(Line ll, int i) { return false; }, // bool opt; return _groupings[gi].IsEnd(ll, i, out opt) != -1; }, // bcz: turned off until it can be more selective (ie, don't change ) to , but to a 1)
                        changemap, -opens[gi], true, gi, _groupings[gi])) {
                        numfixed++;
                    }
                }
            }
            if (numfixed == _groupings.Length) {
                bool changed = false;
                for (int i = 0; i < l._syms.Count; i++) {
                    if (changemap[i] != -1) {
                        changed = true;
                        l._syms[i].r.curalt = changemap[i];
                        l._syms[i].r.levelsetby = -l._syms[i].r.levelsetby;
                    }
                }
                if (changed)
                    throw new Reparse1Exception("balanced parens");
            }
        }

        private delegate bool IsA(Line l, int i);
        /// <summary>
        /// change something to an A or change a B to something else
        /// </summary>
        private bool BalanceParens1(Line l, IsA isanA, IsA isaB, int[] changemap, int extraBs, bool domultiple, int gihack, Grouping g) {
            int[] changetoA = new int[l._syms.Count];
            int changetoAcount = 0;
            int[] changefromB = new int[l._syms.Count];
            int changefromBcount = 0;
            for(int i = 0; i < l._syms.Count; i++) {
                changetoA[i] = -1;
                changefromB[i] = -1;
                Symbol s = l._syms[i];
                if(s.r.levelsetby <= 2) continue;
                if(changemap[i] != -1) continue;
                int origalt = s.r.curalt;
                int was = 0;
                if(isanA(l, i)) was = 1;
                else if(isaB(l, i)) was = -1;
                for(int j = 0; j < s.r.alts.Length; j++) {
                    if(j == origalt) continue;
                    s.r.curalt = j;
                    if(changetoA[i] == -1 && was == 0 && isanA(l, i)) {
                        changetoA[i] = j;
                        changetoAcount++;
                    }
                    if(changefromB[i] == -1 && was == -1 && !isaB(l, i)) {
                        changefromB[i] = j;
                        changefromBcount++;
                    }
                }
                s.r.curalt = origalt;
            }
            if (changefromBcount == extraBs && changetoAcount == 0) {
                for (int i = 0; i < l._syms.Count; i++) {
                    if (changefromB[i] != -1) changemap[i] = changefromB[i];
                }
                return true;
            } else if (changetoAcount == extraBs && changefromBcount == 0) {
                for (int i = 0; i < l._syms.Count; i++) {
                    if (changetoA[i] != -1) changemap[i] = changetoA[i];
                }
                return true;
            }
            /*else if(domultiple && changetoAcount > extraBs) {
                int rightB;
                bool opt;
                for(rightB = l._syms.Count-1; rightB >= 0 && _groupings[gihack].IsEnd(l, rightB, out opt) == -1; rightB--) ;
                for(int i = rightB-1; i >= 0 && extraBs > 0; i--) {
                    if(changetoA[i] != -1) {
                        changemap[i] = changetoA[i];
                        extraBs--;
                    }
                }
                return true;
            }*/
            return false;
        }

        private ParenMatch[] MatchParens(Line l) {
            ParenMatch[] parens = new ParenMatch[l._syms.Count];
            for(int i = 0; i < parens.Length; i++) parens[i] = new ParenMatch();
            Stack<int> openparens = new Stack<int>();
            ParenMatch[] optparens = new ParenMatch[l._syms.Count];
            for(int i = 0; i < optparens.Length; i++) optparens[i] = new ParenMatch();
            for(int i = 0; i < l._syms.Count; i++) {
                foreach(Grouping g in _groupings) {
                    bool opt;
                    int size = g.IsBegin(l, i, out opt);
                    if (!(g is AbsGrouping) || openparens.Count == 0) {
                        if (size > -1) {
                            if (opt) {
                                optparens[i].size = size;
                                optparens[i].type = g;
                            } else {
                                parens[i].size = size;
                                parens[i].type = g;
                            }
                            openparens.Push(i);
                            i += size - 1;
                            break;
                        }
                    }
                    size = g.IsEnd(l, i, out opt);
                    if(size > -1) {
                        if(opt) {
                            bool continueouterloop = false;
                            Stack<int> savedparens = new Stack<int>();
                            do {
                                if(openparens.Count == 0) { continueouterloop = true; break; }
                                int pix = openparens.Peek();
                                bool curopt = optparens[pix].type != null;
                                if(!curopt && parens[pix].type != g) /* || parens[pix].matching != -1 ?? */ { continueouterloop = true; break; }
                                if(curopt && optparens[pix].type == g) {
                                    parens[pix] = optparens[pix];
                                    optparens[pix] = new ParenMatch();
                                    curopt = false;
                                }
                                /* cases now: either not optional and was us or optional and wasn't us */
                                if(!curopt) {
                                    savedparens.Clear();
                                    break;
                                }
                                savedparens.Push(openparens.Pop());
                            } while(true);
                            if(continueouterloop) {
                                while(savedparens.Count != 0) openparens.Push(savedparens.Pop());
                                continue;
                            }
                            Trace.Assert(savedparens.Count == 0);
                        } else {
                            bool breakouterloop = false;
                            Stack<int> savedparens = new Stack<int>();
                            do {
                                if(openparens.Count == 0) {
                                    if(l._syms[i].r.levelsetby >= 2 && Array.IndexOf(l._syms[i].r.alts, new Recognition.Result('1')) > -1) {
                                        l._syms[i].r.curalt = Array.IndexOf(l._syms[i].r.alts, new Recognition.Result('1'));
                                        l._syms[i].r.levelsetby = 2;
                                    }
                                    breakouterloop = true;
                                    break;
                                }
                                int pix = openparens.Peek();
                                bool curopt = optparens[pix].type != null;
                                if(!curopt && parens[pix].type != g) /* || parens[pix].matching != -1 ?? */ { breakouterloop = true; break; }
                                if(curopt && optparens[pix].type == g) {
                                    parens[pix] = optparens[pix];
                                    optparens[pix] = new ParenMatch();
                                    curopt = false;
                                }
                                /* cases now: either not optional and was us or optional and wasn't us */
                                if(!curopt) {
                                    savedparens.Clear();
                                    break;
                                }
                                savedparens.Push(openparens.Pop());
                            } while(true);
                            if(breakouterloop) {
                                while(savedparens.Count != 0) openparens.Push(savedparens.Pop());
                                break;
                            }
                            Trace.Assert(savedparens.Count == 0);
                        }
                        int o = openparens.Pop();
                        if(parens[o].type != g) {
                            //throw new Parse2Exception("Misbalanced grouping");
                            break; // ignore them both; should be parsed as lettersyms or whatever
                        }
                        Trace.Assert(parens[o].matching == -1);
                        Debug.Assert(parens[i].matching == -1);
                        parens[o].matching = i;
                        parens[i].matching = o;
                        parens[i].size = size;
                        parens[i].type = g;
                        i += size - 1;
                        break;
                    }
                }
            }
            // just ignore too many opens...should be parsed as lettersyms or whatever
            //if(openparens.Count > 0) throw new Parse2Exception("Unbalanced grouping (too many opens)");
            return parens;
        }
        private Expr ConvertRunToExpr(Line l, int start, int end, ParenMatch[] parens) {
            List<Expr> terms = new List<Expr>();
            int i = start;
            if (l._syms.Count > start && l._syms[i].r.alt == ',') { // don't allow any leading commas
                bool match_dot = false;
                if (l._syms.Count > start+1) {
                    if (l._syms[start].StrokeBounds.Height/(float)l._syms[start+1].StrokeBounds.Height < 0.5)
                        match_dot = true;
                }
                for (int a = 1; a < l._syms[i].r.alts.Length; a++)
                    if ((!match_dot && l._syms[i].r.alts[a] == '1') || (match_dot && l._syms[i].r.alts[a] == '.')) {
                        l._syms[i].r.curalt = a;
                        l._syms[i].r.levelsetby = 2;
                        break;
                    }
            }
            Line accum = new Line();
            while(i <= end) {
                /* Find the sequence of symbols without parentheses (grouping, in general, including integrals etc) starting at start.
                 * If we're at a parenthesis, recurse */
                if(parens[i].matching != -1) {
                    if(!accum.Empty) {
                        TranslitBasicRun(accum, terms);
                        accum = new Line();
                    }
                    Trace.Assert(parens[i].matching <= end);
                    Expr e = ConvertRunToExpr(l, i + parens[i].size, parens[i].matching - 1, parens);
                    e = parens[i].type.Package(this, e, l, i, parens[i].matching);
                    terms.Add(e);
                    i = parens[i].matching + parens[parens[i].matching].size;
                } else {
                    accum._syms.Add(l._syms[i]);
                    i++;
                }
            }
            if(!accum.Empty) {
                TranslitBasicRun(accum, terms);
                accum = new Line();
            }

            /* Is there an evaluation operator? */
            /* Also, parse the (rest of the) terms: looking for higher precedence operators/relations, in order of increasing precedence */
            if(terms.Count > 1 && (terms[terms.Count - 1] == new LetterSym(Unicode.R.RIGHTWARDS_DOUBLE_ARROW) || terms[terms.Count - 1] == new LetterSym('→'))) {
                Expr eval = terms[terms.Count - 1];
                terms.RemoveAt(terms.Count - 1);
                Expr e = Split(terms, 0); // SplitComma(terms);
                return new CompositeExpr(eval, e);
            } else if(terms.Count > 1 && terms[0] == WellKnownSym.assignment) { // look for return statement
                Expr retn = new LetterSym(Unicode.L.LEFTWARDS_ARROW);
                foreach(object o in terms[0].Annotations.Keys) {
                    retn.Annotations[o] = terms[0].Annotations[o];
                }
                terms.RemoveAt(0);
                Expr e = Split(terms, 0);
                return new CompositeExpr(retn, e);
            } else if(terms.Count == 0)
                return new NullExpr();
            return Split(terms, 0); // SplitComma(terms);
        }

        private static LetterSym _degree = new LetterSym('°');

        private void TranslitBasicRun(Line accum, List<Expr> terms) {
            int i = 0;
            string numeric = "";
            List<Symbol> numericsyms = new List<Symbol>();
            bool havenumdot = false;
            while(i < accum._syms.Count) {
                Expr e = MaybeMatchMultisymbol(accum._syms, ref i);
                if(e != null) {
                    Expr n = CloseOutNumeric(ref numeric, numericsyms);
                    if(n != null) terms.Add(n);
                    havenumdot = false;
                    terms.Add(e);
                } else if("0123456789.".IndexOf(accum._syms[i].r.alt.Character) >= 0) {
                    if(accum._syms[i].r.alt.Character == '.') {
                        if(havenumdot) {
                            Expr n = CloseOutNumeric(ref numeric, numericsyms);
                            if(n != null) terms.Add(n);
                        }
                        havenumdot = true;
                    }
                    Symbol sym = accum._syms[i];
                    numeric += sym.r.alt.Character;
                    numericsyms.Add(sym);
                    if(!sym.Sub.Empty || !sym.Super.Empty) {
                        Expr n = CloseOutNumeric(ref numeric, numericsyms);
                        if(!sym.Sub.Empty) {
                            /* to use subscripts to determine the radix of literal numbers, this is going to be wrong */
                            Expr sub = Parse(sym.Sub);
                            n = new CompositeExpr(WellKnownSym.subscript, n, sub);
                        }
                        if(!sym.Super.Empty) {
                            if(sym.Super._syms.Count == 1 && sym.Super._syms[0].r.alt == '0' && sym.Super._syms[0].r.levelsetby >= 2 && n is RealNumber) {
                                sym.Super._syms[0].r.addorsetalt(Unicode.D.DEGREE_SIGN, sym.Super._syms[0].r.baseline, sym.Super._syms[0].r.xheight);
                                sym.Super._syms[0].r.levelsetby = 2;
                            }
                            Expr super = Parse(sym.Super);
                            n = new CompositeExpr(super == _degree ? WellKnownSym.times : WellKnownSym.power, n, super);
                        }
                        terms.Add(n);
                    }
                    i++;
                } else {
                    e = ConvertSymToExpr(accum._syms[i]);
                    Expr n = CloseOutNumeric(ref numeric, numericsyms);
                    if(n != null)
                        terms.Add(n);
                    havenumdot = false;

                    terms.Add(e);
                    i++;
                }
            }
            Expr num = CloseOutNumeric(ref numeric, numericsyms);
            if(num != null) terms.Add(num);
            havenumdot = false;
        }
        private Expr MaybeMatchMultisymbol(List<Symbol> row, ref int s) {
            foreach(KeyValuePair<string, Expr> kv in _strictTokenmap) {
                Expr e = CheckMultisymbol(kv.Key, kv.Value, row, s, true);
                if(e != null) {
                    s += kv.Key.Length;
                    return e;
                }
            }
            foreach(KeyValuePair<string, Expr> kv in _tokenmap) {
                Expr e = CheckMultisymbol(kv.Key, kv.Value, row, s, false);
                if(e != null) {
                    s += kv.Key.Length;
                    return e;
                }
            }
            return null;
        }
        private Expr CheckMultisymbol(string ms, Expr template, List<Symbol> row, int s, bool strict) {
            /* Must be long enough to contain the multicharacter symbol */
            if(row.Count < s + ms.Length) return null;
            List<Symbol> syms = row.GetRange(s, ms.Length);
            /* No characters other than the last one can have super or subscripts */
            for(int i = 0; i < syms.Count - 1; i++) {
                if(!syms[i].Sub.Empty) return null;
                if(!syms[i].Super.Empty) return null;
            }
            /* Find the alternates which make what the user wrote match (or return failure). */
            int[] alt = new int[ms.Length];
            if(strict) {
                for(int i = 0; i < ms.Length; i++) {
                    Recognition r = syms[i].r;
                    if(r.alt.Character != ms[i]) return null;
                    alt[i] = r.curalt;
                }
            } else {
                for(int i = 0; i < ms.Length; i++) {
                    Recognition r = syms[i].r;
                    if(r.levelsetby <= 2) {
                        alt[i] = r.curalt;
                    } else if(ms == "if" && i == 1 && r.alt != 'F') {
                        alt[i] = r.curalt;
                    } else if (ms == "ln") {//&& (s == 0 ||(s > 0 && row[s-1].r.alt.Character != '=' && row[s-1].r.alt.Character != '('))) {
                        alt[i] = r.curalt;
                    }  else {
                        int j;
                        for(j = 0; j < r.alts.Length; j++) {
                            if(ms != "=>" || ms[i] != '>' || r.alt != '7' || r.strokes.GetBoundingBox().IntersectsWith(row[s].Bounds))
                                if(r.alts[j] == ms[i]) {
                                    if(ms[i] == 'o' && i > 0) {
                                        Recognition pr = syms[i-1].r;
                                        bool smaller = "bdfhklt0123456789".IndexOf(ms[i-1]) != -1 || char.IsUpper(ms[i-1]);
                                        int xhgt = smaller ? (pr.strokes.GetBoundingBox().Top+pr.strokes.GetBoundingBox().Bottom)/2 : pr.strokes.GetBoundingBox().Top;
                                        if((r.strokes.GetBoundingBox().Top - xhgt+0.0)/pr.strokes.GetBoundingBox().Height > -0.25) {
                                            alt[i] = j;
                                            break;
                                        }
                                    } else {
                                        alt[i] = j;
                                        break;
                                    }
                                }
                        }
                    }
                    if(r.alts[alt[i]].Character != ms[i]) return null;
                }
                /* Did too many of the alternates have to be changed? */
                int typos = 0;
                for(int i = 0; i < alt.Length; i++) 
                    if (syms[i].r.alt.Character != syms[i].r.alts[alt[i]].Character && // special case typos that aren't typos o->0 i->im
                        (syms[i].r.alts[alt[i]].Character != 'i' || syms[i].r.alt.Other != Recognition.Result.Special.Imaginary)&&
                        (syms[i].r.alts[alt[i]].Character != 'o' || syms[i].r.alt.Character != '0')) 
                        typos++;
                if(typos > 2) return null;
                /* record that we picked certain alternates! */
                for(int i = 0; i < alt.Length; i++) {
                    if(syms[i].r.curalt != alt[i]) {
                        syms[i].r.curalt = alt[i];
                        syms[i].r.levelsetby = Math.Min(syms[i].r.levelsetby, 2);
                    }
                }
                // guard against converting 11 into y when 1st is drawn at an angle
                if (ms == "\\1" && (syms[1].StrokeBounds.Bottom-syms[0].StrokeBounds.Bottom)/(float)syms[1].StrokeBounds.Height < 0.25)
                    return null;
            }
            foreach (Symbol sy in syms)
                sy.r.levelsetby =Math.Min(sy.r.levelsetby, 2);
            /* Ok, it matched, make the expr */
            Expr e = template.Clone();
            e.Annotations["Symbols"] = syms.ToArray();
            e.Annotations["chars"] = ms;
            foreach(Symbol ss in syms) ss.expr = e;
            Symbol final = syms[syms.Count - 1];
            if(!final.Sub.Empty) {
                Expr esub = Parse(final.Sub);
                LetterSym ls = e as LetterSym;
                WordSym ws = e as WordSym;
                if(ls != null) ls.Subscript = esub;
                else if(ws != null) ws.Subscript = esub;
                else e = new CompositeExpr(WellKnownSym.subscript, e, esub);
            }
            if(!final.Super.Empty) {
                Expr esup = Parse(final.Super);
                e = new CompositeExpr(WellKnownSym.power, e, esup);
            }
            return e;
        }
        private Expr CloseOutNumeric(ref string numeric, List<Symbol> numericsyms) {
            if(numeric.Length == 0) return null;
            Expr e;
            if(numeric == ".") e = new LetterSym('.');
            else if(numeric.IndexOf('.') >= 0) e = new DoubleNumber(Double.Parse(numeric[0] == '.' ? "0"+numeric:numeric));
            else e = new IntegerNumber(numeric);
            e.Annotations["chars"] = numeric;
            e.Annotations["Symbols"] = numericsyms.ToArray();
            foreach(Symbol s in numericsyms) s.expr = e;
            numeric = "";
            numericsyms.Clear();
            return e;
        }
        
        private Expr Split(List<Expr> terms, int precedence) {
            if(precedence == Syntax.Fixes.Table.Length) {
                throw new Exception("not implemented; shouldn't happen");
            } else {
                Syntax.OpRel or = Syntax.Fixes.Table[precedence];
                switch(or.Kind) {
                    case Syntax.K.BinAllOfLike:
                        return SplitBinAllOfLike(terms, or, precedence);
                    case Syntax.K.BinAlone:
                        return SplitBinAlone(terms, or, precedence);
                    case Syntax.K.BinLeft:
                        return SplitBinLeft(terms, or, precedence);
                    case Syntax.K.BinPrimaryAndSecondary:
                    case Syntax.K.BinPrimaryAndSecondary2:
                        return SplitBinPrimaryAndSecondary(terms, or, precedence, or.Kind == Syntax.K.BinPrimaryAndSecondary2);
                    case Syntax.K.BinRight:
                        return SplitBinRight(terms, or, precedence);
                    case Syntax.K.Postfix:
                        return SplitPostfix(terms, or, precedence);
                    case Syntax.K.Prefix:
                    case Syntax.K.PrefixOpt:
                        return SplitPrefix(terms, or, precedence, or.Kind == Syntax.K.PrefixOpt);
                    default:
                        throw new ArgumentException("This should never happen");
                }
            }
        }

        private Expr SplitBinAllOfLike(List<Expr> terms, Syntax.OpRel or, int precedence) {
            List<Expr> curterm = new List<Expr>();
            List<Expr> split = new List<Expr>();
            List<Expr> relations = new List<Expr>();
            Expr currel = null;
            List<Symbol> relsyms = new List<Symbol>();
            foreach(Expr ex in terms) {
                if(Array.IndexOf(or.Heads, ex) != -1) {
                    if(curterm.Count == 0) {
                        //throw new Parse2Exception("Relation with nothing on left (and/or right) side");
                        curterm.Add(new SyntaxErrorExpr(false));
                        _parseError = true;
                    }
                    split.Add(Split(curterm, precedence + 1));
                    curterm = new List<Expr>();
                    if(currel == null) {
                        currel = ex;
                        relsyms.Add(ex.Annotations["Symbols"] as Symbol); // fixme?
                    } else if(currel != ex) {
                        if(split.Count < 2) {
                            //throw new Parse2Exception("Relations with nothing on left and right side");
                            split.Add(new SyntaxErrorExpr(true));
                            _parseError = true;
                        }
                        CompositeExpr ce = new CompositeExpr(currel, split.ToArray());
                        ce.Annotations["Symbols"] = relsyms.ToArray();
                        foreach(Symbol s in relsyms) if(s != null) s.expr = ce;
                        relations.Add(ce);
                        Expr lastbit = split[split.Count - 1]; // XXX or clone it?
                        relsyms.Clear();
                        currel = ex;
                        relsyms.Add(ex.Annotations["Symbols"] as Symbol); // fixme?
                        split = new List<Expr>();
                        split.Add(lastbit);
                    } else relsyms.Add(ex.Annotations["Symbols"] as Symbol); // fixme?
                } else curterm.Add(ex);
            }
            if(currel != null) {
                if(curterm.Count == 0) {
                    //throw new Parse2Exception("Relation with nothing on right (or maybe left) side");
                    curterm.Add(new SyntaxErrorExpr(split.Count > 0));
                    _parseError = true;
                }
                split.Add(Split(curterm, precedence + 1));
                if(split.Count < 2) {
                    //throw new Parse2Exception("Relation with nothing on left side");
                    split.Add(new SyntaxErrorExpr(true));
                    _parseError = true;
                }
                CompositeExpr ce = new CompositeExpr(currel, split.ToArray());
                ce.Annotations["Symbols"] = relsyms.ToArray();
                foreach(Symbol s in relsyms) if(s != null) s.expr = ce;
                relations.Add(ce);
                Expr lhs = relations[0];
                for(int i = 1; i < relations.Count; i++) lhs = new CompositeExpr(WellKnownSym.logand, lhs, relations[i]);
                return lhs;
            }
            Debug.Assert(split.Count == 0);
            if(curterm.Count == 0) throw new Parse2Exception("Nothing entered as an expression!");
            return Split(curterm, precedence + 1);
        }
        private Expr Reduce(Expr lhs, Expr op, Expr rhs) {
            CompositeExpr ce = new CompositeExpr(op, lhs, rhs);
            if(op.Annotations["Symbols"] is Symbol) {
                ce.Annotations["Symbols"] = new Symbol[] { (Symbol)op.Annotations["Symbols"] };
                ((Symbol)op.Annotations["Symbols"]).expr = ce;
            } else if(op.Annotations["Symbols"] is Symbol[]) {
                ce.Annotations["Symbols"] = op.Annotations["Symbols"];
                ce.Annotations["chars"] = op.Annotations["chars"];
                foreach(Symbol s in (Symbol[])op.Annotations["Symbols"]) {
                    s.expr = ce;
                }
            }
            return ce;
        }
        private Expr SplitBinAlone(List<Expr> terms, Syntax.OpRel or, int precedence) {
            int opix = terms.FindIndex(delegate(Expr e) { return Array.IndexOf(or.Heads, e) != -1; });
            if(opix == -1) return Split(terms, precedence + 1);
            List<Expr> arga = terms.GetRange(0, opix);
            if(arga.Count == 0) arga.Add(new SyntaxErrorExpr(false));
            int opix2 = terms.FindIndex(opix+1, delegate(Expr e) { return Array.IndexOf(or.Heads, e) != -1; });
            List<Expr> argb = terms.GetRange(opix + 1, (opix2 == -1 ? terms.Count : opix2) - (opix + 1)); ;
            if(argb.Count == 0) argb.Add(new SyntaxErrorExpr(true));
            Expr ee = Reduce(Split(arga, precedence + 1), terms[opix], Split(argb, precedence + 1));
            if(opix2 == -1) return ee;
            else return new SyntaxJunkErrorExpr(ee, terms.GetRange(opix2, terms.Count - opix2));
        }
        private Expr SplitBinLeft(List<Expr> terms, Syntax.OpRel or, int precedence) {
            List<Expr> curterm = new List<Expr>();
            Expr lhs = null;
            Expr op = null;
            foreach(Expr ex in terms) {
                if(Array.IndexOf(or.Heads, ex) != -1) {
                    if(curterm.Count == 0) {
                        curterm.Add(new SyntaxErrorExpr(false));
                        _parseError = true;
                    }
                    /* reduce "lhs op curterm" or "curterm" */
                    if(lhs != null) lhs = Reduce(lhs, op, Split(curterm, precedence + 1));
                    else lhs = Split(curterm, precedence + 1);
                    op = ex;
                    curterm = new List<Expr>();
                } else curterm.Add(ex);
            }
            if(curterm.Count == 0) {
                curterm.Add(new SyntaxErrorExpr(true));
                _parseError = true;
            }
            /* reduce "lhs op curterm" or "curterm" */
            if(lhs != null) lhs = Reduce(lhs, op, Split(curterm, precedence + 1));
            else lhs = Split(curterm, precedence + 1);
            return lhs;
        }
        private Expr SplitBinPrimaryAndSecondary(List<Expr> terms, Syntax.OpRel or, int precedence, bool allowpfx) {
            if(or.Heads[0] == WellKnownSym.times) return SplitDivision(terms); // FIXME HACK just for now!
            List<Expr> curterm = new List<Expr>();
            List<Expr> split = new List<Expr>();
            Expr lastrel = null;
            List<Symbol> relsyms = new List<Symbol>();
            foreach(Expr ex in terms) {
                /* If the exp has no starting + or minus, there must be an initial null added to relsyms.
                 * If the exp starts with a +, add it to relsyms and be sure to set "initial op" on the expr.
                 * If the exp starts with a -, add null to relsyms. (just don't do that for '-' in between exps--ie -1-2 1st minus gets null, 2nd doesn't)
                 * 
                 * So, if get a nonplus or minus,
                 *   if split is empty and curterm is empty and lastrel is null, add null to relsyms (assert it's empty).
                 *   otherwise nothing special
                 * And, if get a plus or minus,
                 *   if split is empty and curterm is empty and lastrel is null, then is +
                 *   if split is empty and curterm is empty and lastrel is nonnull, then is -+ or ++
                 *   if split is empty and curterm is full and lastrel is null, then is 1+
                 *   if split is empty and curterm is full and lastrel is nonnull, then is +1+ but not +1+1+
                 *   if split is nonempty and curterm is empty, then is +1++ or 1++ or 1+1++ or +1+1++
                 *   if split is nonempty and curterm is full, then is +1+1+ or 1+1+
                 * And, when ending the expression,
                 *   if split is empty and curterm is empty and lastrel is null, then is empty
                 *   if split is empty and curterm is empty and lastrel is nonnull, then is - or +
                 *   if split is empty and curterm is full and lastrel is null, then is 1
                 *   if split is empty and curterm is full and lastrel is nonnull, then is +1 but not +1+1
                 *   if split is nonempty and curterm is empty, then is +1+ or 1+ or 1+1+ or +1+1+
                 *   if split is nonempty and curterm is full, then is +1+1 or 1+1
                 */
                if(Array.IndexOf(or.Heads, ex) != -1) {
                    if(relsyms.Count == 0 && ex != or.Heads[0]) relsyms.Add(null);
                    else relsyms.Add(ex.Annotations["Symbols"] as Symbol);

                    if(lastrel != null && curterm.Count == 0) {
                        // disallow 5 + - 3 for now
                        //throw new Parse2Exception("Adjacent additive operators");
                        curterm.Add(new SyntaxErrorExpr(false));
                        _parseError = true;
                    }
                    if(curterm.Count > 0) {
                        Expr e = Split(curterm, precedence + 1);
                        if(lastrel == null || lastrel == or.Heads[0]) {
                            split.Add(e);
                        } else {
                            split.Add(new CompositeExpr(lastrel, e));
                        }
                        curterm = new List<Expr>();
                    }

                    lastrel = ex;
                } else {
                    if(split.Count == 0 && curterm.Count == 0 && lastrel == null) {
                        Debug.Assert(relsyms.Count == 0);
                        relsyms.Add(null);
                    }
                    curterm.Add(ex);
                }
            }
            /* And, when ending the expression,
             *   if split is empty and curterm is empty and lastrel is null, then is empty
             * E if split is empty and curterm is empty and lastrel is nonnull, then is - or +
             *   if split is empty and curterm is full and lastrel is null, then is 1
             *   if split is empty and curterm is full and lastrel is nonnull, then is +1 but not +1+1
             * E if split is nonempty and curterm is empty, then is +1+ or 1+ or 1+1+ or +1+1+
             *   if split is nonempty and curterm is full, then is +1+1 or 1+1
             */
            if(lastrel != null && curterm.Count == 0) {
                curterm.Add(new SyntaxErrorExpr(true));
                _parseError = true;
            }
            if(curterm.Count > 0) {
                Expr e = Split(curterm, precedence + 1);
                if(lastrel == null)
                    split.Add(e);
                else if(lastrel == or.Heads[0]) {
                    split.Add(e);
                } else {
                    split.Add(new CompositeExpr(lastrel, e));
                }
            }
            if(split.Count == 0) throw new Parse2Exception("No expression entered to convert!");
            else if(split.Count == 1 && relsyms[0] == null) return split[0];
            else {
                CompositeExpr ce = new CompositeExpr(or.Heads[0], split.ToArray());
                ce.Annotations["Symbols"] = relsyms.ToArray();
                foreach(Symbol s in relsyms) if(s != null) s.expr = ce;
                if(relsyms[0] != null) ce.Annotations["initial op"] = relsyms[0].r.alt.Character;
                return ce;
            }
        }
        private Expr SplitBinRight(List<Expr> terms, Syntax.OpRel or, int precedence) {
            List<Expr> curterm = new List<Expr>();
            Stack<Expr> lhs = new Stack<Expr>();
            Stack<Expr> op = new Stack<Expr>();
            foreach(Expr ex in terms) {
                if(Array.IndexOf(or.Heads, ex) != -1) {
                    if(curterm.Count == 0) {
                        curterm.Add(new SyntaxErrorExpr(false));
                        _parseError = true;
                    }
                    /* dump on stack, reduction must happen at very end */
                    lhs.Push(Split(curterm, precedence + 1));
                    op.Push(ex);
                    curterm = new List<Expr>();
                } else curterm.Add(ex);
            }
            if(curterm.Count == 0) {
                curterm.Add(new SyntaxErrorExpr(true));
                _parseError = true;
            }
            /* reduce everything */
            Expr tree = Split(curterm, precedence + 1);
            while(lhs.Count > 0) {
                tree = Reduce(lhs.Pop(), op.Pop(), tree);
            }
            return tree;
        }
        private Expr BaseOp(Expr op) {
            CompositeExpr pe = op as CompositeExpr;
            if(pe != null && pe.Head == WellKnownSym.power) op = pe.Args[0];
            CompositeExpr se = op as CompositeExpr;
            if(se != null && se.Head == WellKnownSym.subscript) op = se.Args[0];
            return op;
        }
        private Expr SplitPrefix(List<Expr> terms, Syntax.OpRel or, int precedence, bool opt) {
            List<Expr> curchunk = new List<Expr>();
            Stack<List<Expr>> termchunks = new Stack<List<Expr>>();
            Stack<Expr> ops = new Stack<Expr>();
            Stack<Expr> baseops = new Stack<Expr>();
            if(terms.Count == 0) throw new Parse2Exception("No expression entered to convert!");
            foreach(Expr ex in terms) {
                Expr bop = BaseOp(ex);
                if(Array.IndexOf(or.Heads, bop) != -1) {
                    termchunks.Push(curchunk);
                    curchunk = new List<Expr>();
                    ops.Push(ex);
                    baseops.Push(bop);
                } else {
                    curchunk.Add(ex);
                }
            }
            if(!opt && curchunk.Count == 0) {
                curchunk.Add(new SyntaxErrorExpr(true));
                _parseError = true;
            }
            termchunks.Push(curchunk);
            Expr accum = null;
            while(ops.Count != 0) {
                Expr op = ops.Pop();
                List<Expr> chunk = termchunks.Pop();
                Expr bop = baseops.Pop();
                if(accum != null) chunk.Add(accum);
                Trace.Assert(opt || chunk.Count > 0);
                if(opt && chunk.Count == 0) accum = null;
                else accum = Split(chunk, precedence + 1);
                if(op != bop) {
                    Expr sub = null, sup = null;
                    CompositeExpr pe = op as CompositeExpr;
                    if(pe != null && pe.Head == WellKnownSym.power) {
                        op = pe.Args[0];
                        sup = pe.Args[1];
                    }
                    CompositeExpr se = op as CompositeExpr;
                    if(se != null && se.Head == WellKnownSym.subscript) {
                        op = se.Args[0];
                        sub = se.Args[1];
                    }
                    Trace.Assert(op == bop);
                    if(sub == null && sup == null) accum = new CompositeExpr(op, accum);
                    else if(sub == null && sup != null) accum = new CompositeExpr(op, new MissingValueExpr(), sup, accum);
                    else if(sub != null && sup == null) accum = new CompositeExpr(op, sub, accum);
                    else accum = new CompositeExpr(op, sub, sup, accum);
                } else accum = accum == null ? new CompositeExpr(op) : new CompositeExpr(op, accum);
            }
            curchunk = termchunks.Pop();
            Trace.Assert(termchunks.Count == 0);
            if(accum != null) curchunk.Add(accum);
            return Split(curchunk, precedence + 1);
        }
        private Expr SplitPostfix(List<Expr> terms, Syntax.OpRel or, int precedence) {
            List<Expr> curterm = new List<Expr>();
            foreach(Expr ex in terms) {
                if(Array.IndexOf(or.Heads, ex) != -1) {
                    if(curterm.Count == 0) {
                        curterm.Add(new SyntaxErrorExpr(false));
                        _parseError = true;
                    }
                    Expr e = new CompositeExpr(ex, Split(curterm, precedence + 1));
                    curterm.Clear();
                    curterm.Add(e);
                } else curterm.Add(ex);
            }
            return Split(curterm, precedence + 1);
        }

        private void AddMaybeDivide(ref WellKnownSym division, ref WellKnownSym afterdot, List<Expr> termlist, Expr ex) {
            if(division != null) {
                CompositeExpr ce = new CompositeExpr(division, ex);
                ce.Annotations["inline"] = true;
                Trace.Assert(afterdot == null);
                termlist.Add(ce);
            } else if(afterdot != null) {
                Trace.Assert(termlist.Count != 0);
                ex.Annotations["dot before"] = afterdot.Annotations["Symbols"];
                ((Symbol)afterdot.Annotations["Symbols"]).expr = ex;
                termlist.Add(ex);
            } else {
                /* check for function notation. Heuristics:
                 * a word or letter sym followed by a lettersym with explicit parens around it
                 * a word or letter sym followed by a "comma" compositeexpr (which in order to see here must have been explic. parend)
                 * a word sym followed by anything explicitly parenthesized
                 * */
                CompositeExpr exce = ex as CompositeExpr;
                Expr lastterm = termlist.Count > 0 ? termlist[termlist.Count - 1] : null;
                if((lastterm is WordSym && ex.Annotations.ContainsKey("Force Parentheses"))
                    || (lastterm is LetterSym
                    && ((ex is LetterSym && ex.Annotations.ContainsKey("Force Parentheses"))
                        || (exce != null && exce.Head == new WordSym("comma") && (int)exce.Annotations["Force Parentheses"] == 1)))) {
                    termlist.RemoveAt(termlist.Count - 1);
                    CompositeExpr ce;
                    if(exce != null && exce.Head == new WordSym("comma")) {
                        ce = new CompositeExpr(lastterm, exce.Args);
                        if(lastterm.Annotations.Contains("dot before")) {
                            Symbol s = (Symbol)lastterm.Annotations["dot before"];
                            ce.Annotations["dot before"] = s;
                            lastterm.Annotations.Remove("dot before");
                            s.expr = ce;
                        }
                        Symbol[] syms = (Symbol[])(ce.Annotations["Symbols"] = exce.Annotations["Symbols"]);
                        foreach(Symbol s in syms) if(s != null) s.expr = ce;
                    } else {
                        ce = new CompositeExpr(lastterm, ex);
                    }
                    if(ex.Annotations.ContainsKey("Force Parentheses")) {
                        List<Symbol> delims = (List<Symbol>)ex.Annotations["delimiters"];
                        Trace.Assert(delims.Count == 2);
                        ce.Annotations["fnargparens"] = delims.ToArray();
                        foreach(Symbol s in delims) if(s != null) s.expr = ce;
                    }
                    termlist.Add(ce);
                } else termlist.Add(ex);
            }
            division = null;
            afterdot = null;
        }
        static private List<WKSID> _expfns = new List<WKSID>(new WKSID[]{ WKSID.arccos, WKSID.acosh, WKSID.acot, WKSID.acoth, WKSID.acsc, WKSID.acsch,
            WKSID.asec, WKSID.asech, WKSID.arcsin, WKSID.asinh, WKSID.arctan, WKSID.atanh, WKSID.cos, WKSID.cosh, WKSID.cot, WKSID.coth,
            WKSID.csc, WKSID.csch, WKSID.ln, WKSID.log, WKSID.sec, WKSID.sech, WKSID.sin, WKSID.sinh, WKSID.tan,
            WKSID.tanh, WKSID.sum, WKSID.avg });
        private bool IsFn(Expr e) {
            // FIXME: Bob wanted sin^2 etc., so we look for them here, but we need to implement a display rule to show properly
            WellKnownSym wks = e as WellKnownSym;
            if(wks != null && Array.BinarySearch(_wkfns, wks.ID) >= 0) return true;
            CompositeExpr ce = e as CompositeExpr;
            if(ce != null && ce.Head == WellKnownSym.subscript && ce.Args[0] == WellKnownSym.log) return true;
            if(ce != null && ce.Head == WellKnownSym.power) {
                WellKnownSym wks2 = ce.Args[0] as WellKnownSym;
                if(wks2 != null && _expfns.BinarySearch(wks2.ID) >= 0) return true;
                CompositeExpr ce2 = ce.Args[0] as CompositeExpr;
                if(ce2 != null && ce2.Head == WellKnownSym.subscript && ce2.Args[0] == WellKnownSym.log) return true;
            }
            return false;
        }
        private Expr SplitDivision(List<Expr> iterms) {
            /* handle postfix factorial in a hacky way */
            List<Expr> terms = new List<Expr>();
            Expr curelt = null;
            foreach(Expr ex in iterms) {
                if(ex == WellKnownSym.factorial) {
                    if(curelt == null || curelt == WellKnownSym.dot || curelt == WellKnownSym.divide || IsFn(curelt)) {
                        if(curelt != null) terms.Add(curelt);
                        curelt = new SyntaxErrorExpr(false);
                        _parseError = true;
                    }
                    curelt = new CompositeExpr(ex, curelt);
                } else {
                    if(curelt != null) terms.Add(curelt);
                    curelt = ex;
                }
            }
            if(curelt != null) terms.Add(curelt);

            List<Expr> termlist = new List<Expr>();
            WellKnownSym division = null;
            List<Expr> fnstack = new List<Expr>();
            WellKnownSym afterdot = null;
            int tix = -1;
            foreach(Expr ex in terms) {
                tix++;
                WellKnownSym wks = ex as WellKnownSym;
                if(wks == WellKnownSym.divide) {
                    if(fnstack.Count != 0) {
                        AddMaybeDivide(ref division, ref afterdot, termlist, fnstack[0]);
                        fnstack.RemoveAt(0);
                        termlist.AddRange(fnstack);
                        fnstack = new List<Expr>();
                    }
                    if(afterdot != null || division != null) {
                        AddMaybeDivide(ref division, ref afterdot, termlist, new SyntaxErrorExpr(true));
                        _parseError = true;
                    }
                    if(termlist.Count == 0) {
                        Debug.Assert(division == null);
                        Symbol dsym = (ex.Annotations["Symbols"] as Symbol);
                        bool canFlip = true;
                        if (tix < terms.Count - 1) {
                            WellKnownSym wksnext = terms[tix+1] as WellKnownSym;
                            if (wksnext != null) {
                                Symbol next = (wksnext.Annotations["Symbols"] as Symbol);
                                if (next.Sym == '/' && next.r.strokes.GetBoundingBox().IntersectsWith(dsym.r.strokes.GetBoundingBox())) {
                                    double ang = FeaturePointDetector.angle(dsym.r.strokes[0].GetPoint(0), dsym.r.strokes[0].GetPoint(dsym.r.strokes[0].GetPoints().Length - 1),
                                        V2D.Sub(next.r.strokes[0].GetPoint(0), next.r.strokes[0].GetPoint(next.r.strokes[0].GetPoints().Length - 1)));
                                    if (ang < 10)
                                        canFlip = false;
                                }
                            }
                        }
                        if(canFlip && dsym != null && dsym.r.levelsetby != 0) {
                            dsym.r.addorsetalt('1', dsym.r.baseline, dsym.r.xheight);
                            dsym.r.levelsetby = 2;
                            throw new ReparseException("Fraction reintepreted as a 1");
                        }
                        //throw new Parse2Exception("Fractions must have numerators");
                        termlist.Add(new SyntaxErrorExpr(false));
                        _parseError = true;
                    }
                    division = wks;
                } else if(ex == WellKnownSym.dot) {
                    /* treat as multiplication for now */
                    if(fnstack.Count != 0) {
                        AddMaybeDivide(ref division, ref afterdot, termlist, fnstack[0]);
                        fnstack.RemoveAt(0);
                        termlist.AddRange(fnstack);
                        fnstack = new List<Expr>();
                    }
                    if(afterdot != null || division != null) {
                        AddMaybeDivide(ref division, ref afterdot, termlist, new SyntaxErrorExpr(true));
                        _parseError = true;
                    }
                    if(termlist.Count == 0) {
                        termlist.Add(new SyntaxErrorExpr(false));
                        _parseError = true;
                    }
                    afterdot = wks;
                } else if(IsFn(ex))
                    fnstack.Add(ex);
                else if(fnstack.Count != 0) {
                    Expr e = ex;
                    for(int i = fnstack.Count - 1; i >= 0; i--) {
                        if(fnstack[i] is WellKnownSym) e = new CompositeExpr(fnstack[i], e);
                        else if(fnstack[i] is CompositeExpr) {
                            CompositeExpr ce = (CompositeExpr)fnstack[i];
                            if(ce.Head == WellKnownSym.subscript) {
                                Trace.Assert(ce.Args[0] == WellKnownSym.log);
                                e = new CompositeExpr(ce.Args[0], ce.Args[1], e);
                            } else {
                                Trace.Assert(ce.Head == WellKnownSym.power);
                                if(ce.Args[0] is WellKnownSym) e = new CompositeExpr(ce.Args[0], e);
                                else {
                                    CompositeExpr ce2 = (CompositeExpr)ce.Args[0];
                                    Trace.Assert(ce2.Head == WellKnownSym.subscript);
                                    Trace.Assert(ce2.Args[0] == WellKnownSym.log);
                                    e = new CompositeExpr(ce2.Args[0], ce2.Args[1], e);
                                }
                                e = new CompositeExpr(ce.Head, e, ce.Args[1]);
                            }
                        }
                    }
                    AddMaybeDivide(ref division, ref afterdot, termlist, e);
                    fnstack = new List<Expr>();
                } else {
                    AddMaybeDivide(ref division, ref afterdot, termlist, ex);
                }
            }
            if(fnstack.Count != 0) {
                //throw new Parse2Exception("Function(s) applied to nothing");
                AddMaybeDivide(ref division, ref afterdot, termlist, fnstack[0]);
                fnstack.RemoveAt(0);
                termlist.AddRange(fnstack);
                fnstack = new List<Expr>();
            }
            if(afterdot != null || division != null) {
                AddMaybeDivide(ref division, ref afterdot, termlist, new SyntaxErrorExpr(true));
                _parseError = true;
            }
            if(termlist.Count == 1) return termlist[0];
            return new CompositeExpr(WellKnownSym.times, termlist.ToArray());
        }
        private WKSID? TryAsWKSID(string s) {
            string[] names = Enum.GetNames(typeof(WKSID));
            int ix = Array.FindIndex(names, delegate (string es) { return es == s || es.ToLower() == s; });
            if(ix >= 0) return (WKSID)Enum.Parse(typeof(WKSID), names[ix]);
            else return null;
        }
        private Expr ConvertSymToExpr(Symbol s) {
            Expr e;
            if(s.r.alt.Word != null) {
                WKSID? wksid;
                if(s.r.alt.Tag != null) {
                    WordSym ws = new WordSym(s.r.alt.Word);
                    ws.Tag = s.r.alt.Tag;
                    ws.Annotations["Symbols"] = s;
                    s.expr = ws;
                    if(!s.Sub.Empty) ws.Subscript = Parse(s.Sub);
                    e = ws;
                    if(!s.Super.Empty) {
                        Expr super = Parse(s.Super);
                        e = new CompositeExpr(WellKnownSym.power, e, super);
                    }
                } else if((wksid = TryAsWKSID(s.r.alt.Word)).HasValue && Array.BinarySearch(WKFns, wksid) >= 0) {
                    e = new WellKnownSym(wksid.Value);
                    e.Annotations["Symbols"] = s;
                    s.expr = e;
                    if(!s.Sub.Empty) {
                        Expr sub = Parse(s.Sub);
                        e = new CompositeExpr(WellKnownSym.subscript, e, sub);
                    }
                    if(!s.Super.Empty) {
                        Expr super = Parse(s.Super);
                        e = new CompositeExpr(WellKnownSym.power, e, super);
                    }
                } else if(s.r.alt.Word.Length == 2 && s.r.alt.Word[0] == 'd') {
                    LetterSym ls = new LetterSym(s.r.alt.Word[1]);
                    ls.Annotations["Symbols"] = s;
                    if(!s.Sub.Empty) {
                        Expr sub = Parse(s.Sub);
                        ls.Subscript = sub;
                    }
                    WellKnownSym wks = (WellKnownSym)WellKnownSym.differentiald.Clone();
                    wks.Annotations["Symbols"] = s;
                    e = new CompositeExpr(wks, ls);
                    e.Annotations["Symbols"] = s;
                    // FIXME: one symbol maps to multiple primitive exprs when displayed, but GDIplus doesn't prop bboxes up yet
                    s.expr = e;
                } else {
                    WordSym ws = new WordSym(s.r.alt.Word);
                    ws.Annotations["Symbols"] = s;
                    s.expr = ws;
                    if(!s.Sub.Empty) ws.Subscript = Parse(s.Sub);
                    e = ws;
                    if(!s.Super.Empty) {
                        Expr super = Parse(s.Super);
                        e = new CompositeExpr(WellKnownSym.power, e, super);
                    }
                }
            } else if(s is DivSym) {
                Expr num = s.Super.Empty ? new MissingValueExpr() : Parse(s.Super);
                Expr den = s.Sub.Empty ? new MissingValueExpr() : Parse(s.Sub);
                if(num is MissingValueExpr || den is MissingValueExpr) _parseError = true;
                Expr divhead = WellKnownSym.divide;
                divhead.Annotations["Symbols"] = s;
                s.expr = divhead;
                // to differentiate between Symbol and Symbol[]
                bool denIsSymbol = (den.Annotations["Symbols"] is Symbol);
                bool numIsSymbol = (num.Annotations["Symbols"] is Symbol);

                if (num is MissingValueExpr && den is MissingValueExpr) {
                    Strokes sks = s.r.strokes;
                    MyParser.Charreco.FullClassify(s.r.strokes[0],
new Recognition(sks, Unicode.M.MINUS_SIGN.ToString(), sks.GetBoundingBox().Bottom, (sks.GetBoundingBox().Top+sks.GetBoundingBox().Bottom)/2, false));
                    throw new Reparse1Exception("division reinterpreted as a '"+Unicode.M.MINUS_SIGN+"'");

                }
                //bcz: hack to allow T's and pi's to be interpreted when there is no numerator.
                //     the problem is that if you add a numerator, the T/pi won't go back to being a division.
                //     we need to allow the parser to set alternates that have different numbers of strokes
                //     than other alternates.
                if(num is MissingValueExpr && ((den is IntegerNumber && ((IntegerNumber)den).Num == 1) ||
                    (den is CompositeExpr && (den as CompositeExpr).Head == new WordSym("comma")) ||
                    (den is LetterSym && "()1ybs,".Contains((den as LetterSym).Letter.ToString())))) {
                    Strokes sks = s.r.strokes.Ink.CreateStrokes();
                    sks.Add(s.r.strokes[0]);
                    if(denIsSymbol)
                        sks.Add(((Symbol)den.Annotations["Symbols"]).r.strokes[0]);
                    else sks.Add(((Symbol[])den.Annotations["Symbols"])[0].r.strokes[0]);
                    char replace = (den is LetterSym && "ybs".Contains((den as LetterSym).Letter.ToString())) ? '5' : 'T';
                    if ((sks[0].GetBoundingBox().Top - sks[1].GetBoundingBox().Bottom +0.0)/sks[1].GetBoundingBox().Height > 0.25) {
                        sks.Remove(sks[0]);
                        MyParser.Charreco.FullClassify(sks[0],
                        new Recognition(sks, "-", sks.GetBoundingBox().Bottom, (sks.GetBoundingBox().Top+sks.GetBoundingBox().Bottom)/2, false));
                        MyParser.Charreco.Classification(sks[0]).levelsetby = -1;
                        throw new Reparse1Exception("division reinterpreted as a '"+Unicode.M.MINUS_SIGN+"'");
                    }
                
                    if (replace == 'T') {
                        if (V2D.Straightness(sks[sks.Count-2].GetPoints()) > 0.15)
                            replace = 'J';
                        float[] ints = sks[1].FindIntersections(sks[0].Ink.CreateStrokes(new int[] { sks[0].Id }));
                        if(ints.Length > 0) {
                            var pt = FeaturePointDetector.getPt(ints[0], sks[1].GetPoints());
                            if((pt.Y - sks[0].GetBoundingBox().Top + 0.0)/sks[1].GetBoundingBox().Height >0.1)
                                replace = '+';
                        }
                    }
                    MyParser.Charreco.FullClassify(s.r.strokes[0],
                        new Recognition(sks, replace.ToString(), sks.GetBoundingBox().Bottom, (sks.GetBoundingBox().Top+sks.GetBoundingBox().Bottom)/2, false));
                    throw new Reparse1Exception("division reinterpreted as a '"+replace+"'");
                } else if(den is MissingValueExpr && numIsSymbol && ((Symbol)num.Annotations["Symbols"]).r.allograph.Contains("\\") &&
                    90-Math.Abs(90-FeaturePointDetector.angle(s.r.strokes[0].GetPoint(0),s.r.strokes[0].GetPoint(s.r.strokes[0].GetPoints().Length-1),
                                               new System.Drawing.PointF(1, 0))) > 40) {
                    Strokes sks = s.r.strokes.Ink.CreateStrokes();
                    sks.Add(s.r.strokes[0]);
                    if(numIsSymbol)
                        sks.Add(((Symbol)num.Annotations["Symbols"]).r.strokes);
                    else
                        sks.Add(((Symbol[])num.Annotations["Symbols"])[0].r.strokes);
                    MyParser.Charreco.FullClassify(s.r.strokes[0],
                        new Recognition(sks, "y", (sks.GetBoundingBox().Bottom+sks.GetBoundingBox().Top)/2, (sks.GetBoundingBox().Bottom+sks.GetBoundingBox().Top)/2, false));
                    throw new Reparse1Exception("division reinterpreted as a 'y'");
                } else if(den is MissingValueExpr && numIsSymbol && ((Symbol)num.Annotations["Symbols"]).r.alt == Unicode.I.INTEGRAL &&
                           s.r.strokes.GetBoundingBox().Width < ((Symbol)num.Annotations["Symbols"]).r.strokes.GetBoundingBox().Width) {
                    s.r.addorsetalt(Unicode.M.MINUS_SIGN, s.r.baseline, s.r.xheight);
                    s.r.levelsetby = 0;
                    throw new Reparse1Exception("division reinterpreted as a '-'");
                } else if(den is MissingValueExpr && 
                    (numIsSymbol && ((Symbol)num.Annotations["Symbols"]).r.allograph.Contains("T"))) {
                    Strokes sks = s.r.strokes.Ink.CreateStrokes();
                    sks.Add(s.r.strokes[0]);
                    if(numIsSymbol)
                        sks.Add(((Symbol)num.Annotations["Symbols"]).r.strokes);
                    MyParser.Charreco.FullClassify(s.r.strokes[0],
                        new Recognition(sks, "I", sks.GetBoundingBox().Bottom, (sks.GetBoundingBox().Top + sks.GetBoundingBox().Bottom) / 2, false));
                    throw new Reparse1Exception("division reinterpreted as a 'I'");
                } else if(num is MissingValueExpr && den is IntegerNumber && ((IntegerNumber)den).Num == 11) {
                    Strokes sks = s.r.strokes.Ink.CreateStrokes();
                    sks.Add(s.r.strokes[0]);
                    sks.Add(((Symbol[])den.Annotations["Symbols"])[0].r.strokes[0]);
                    sks.Add(((Symbol[])den.Annotations["Symbols"])[1].r.strokes[0]);
                    MyParser.Charreco.FullClassify(s.r.strokes[0],
                        new Recognition(sks, "pi", sks.GetBoundingBox().Bottom, sks.GetBoundingBox().Top, false));
                    throw new Reparse1Exception("division reinterpreted as a 'π'");
                } else if(num is MissingValueExpr &&  den is CompositeExpr && ((CompositeExpr)den).Args.Length == 2 &&
                    ((((CompositeExpr)den).Args[0] is IntegerNumber && ((IntegerNumber)((CompositeExpr)den).Args[0]).Num == 1) ||
                     (((CompositeExpr)den).Args[0] is LetterSym && "()1".Contains(((LetterSym)((CompositeExpr)den).Args[0]).Letter.ToString()))) &&
                    ((((CompositeExpr)den).Args[1] is IntegerNumber && ((IntegerNumber)((CompositeExpr)den).Args[1]).Num == 1) ||
                     (((CompositeExpr)den).Args[1] is LetterSym && "()1".Contains(((LetterSym)((CompositeExpr)den).Args[1]).Letter.ToString())))) {
                    Strokes sks = s.r.strokes.Ink.CreateStrokes();
                    sks.Add(s.r.strokes[0]);
                    if((((CompositeExpr)den).Args[0].Annotations["Symbols"]) is Symbol)
                        sks.Add(((Symbol)((CompositeExpr)den).Args[0].Annotations["Symbols"]).r.strokes[0]);
                    else sks.Add(((Symbol[])((CompositeExpr)den).Args[0].Annotations["Symbols"])[0].r.strokes[0]);
                    if((((CompositeExpr)den).Args[1].Annotations["Symbols"]) is Symbol)
                        sks.Add(((Symbol)((CompositeExpr)den).Args[1].Annotations["Symbols"]).r.strokes[0]);
                    else sks.Add(((Symbol[])((CompositeExpr)den).Args[1].Annotations["Symbols"])[0].r.strokes[0]);
                    MyParser.Charreco.FullClassify(s.r.strokes[0],
                        new Recognition(sks, "pi", sks.GetBoundingBox().Bottom, sks.GetBoundingBox().Top, false));
                    throw new Reparse1Exception("division reinterpreted as a 'π'");
                } else e = new CompositeExpr(WellKnownSym.times, num, new CompositeExpr(divhead, den));
            } else if(s is RootSym) {
                if(s.Super.Empty && _finalSubsts) {
                    s.r.addorsetalt('r', s.r.baseline, s.r.xheight);
                    s.r.levelsetby = 2;
                    e = new LetterSym(s.r.alt.Character);
                    e.Annotations["Symbols"] = s;
                    s.expr = e;
                } else {
                    Expr ex;
                    if(s.Super.Empty) ex = new MissingValueExpr();
                    else ex = Parse(s.Super);
                    WellKnownSym wksr = WellKnownSym.root;
                    Expr index = s.Sub.Empty ? new IntegerNumber(2) : Parse(s.Sub);
                    // not sure if annotation should be with wks or with whole expr...
                    //wksr.Annotations["Symbols"] = s;
                    //s.expr = wksr;
                    e = new CompositeExpr(wksr, index, ex);
                    e.Annotations["Symbols"] = s;
                    s.expr = e;
                }
            } else if(s is ParenSym && !((ParenSym)s).OneRow) {
                ParenSym ps = (ParenSym)s;
                int maxcols = 0;
                Expr[,] elts = null;
                if (ps.ArrowID != -1) {//Keep fixed format(no adjustment here) for matrix with arrow shorthand
                    foreach (List<Line> ll in ps.rows) maxcols = Math.Max(maxcols, ll.Count);
                    elts = new Expr[ps.rows.Count, maxcols];
                    for (int i = 0; i < ps.rows.Count; i++) {
                        List<Line> row = ps.rows[i];
                        for (int j = 0; j < row.Count; j++) elts[i, j] = Parse(row[j]);
                        for (int j = row.Count; j < maxcols; j++) elts[i, j] = new NullExpr();
                    }
                }
                else {// Adjust for non-well-formed matrices
                    List<System.Drawing.Rectangle> colBoxes = new List<System.Drawing.Rectangle>();
                    for (int i = 0; i < ps.rows[0].Count; i++)
                        colBoxes.Add(ps.rows[0][i].EntryBounds());
                    for (int i = 1; i < ps.rows.Count; i++) {
                        List<Line> row = ps.rows[i];
                        int m = 0;
                        for (int j = 0; j < row.Count; j++) {
                            bool done = false;
                            for (int k = m; k < colBoxes.Count; k++) {
                                if (row[j].EntryBounds().Right /*- row[j].EntryBounds().Width / 5 */< colBoxes[k].Left) {
                                    colBoxes.Insert(k, row[j].EntryBounds());
                                    m = k+1;
                                    done = true;
                                    break;
                                }
                                else if (row[j].EntryBounds().Left < colBoxes[k].Right && row[j].EntryBounds().Right > colBoxes[k].Left) {
                                    System.Drawing.Rectangle newBox = System.Drawing.Rectangle.Union(colBoxes[k], row[j].EntryBounds());
                                    colBoxes.RemoveAt(k);
                                    colBoxes.Insert(k, newBox);
                                    m = k+1;
                                    done = true;
                                    break;
                                }
                            }
                            if (!done)
                                colBoxes.Add(row[j].EntryBounds());
                        }
                    }
                    maxcols = colBoxes.Count;
                    elts = new Expr[ps.rows.Count, maxcols];
                    for (int i = 0; i < ps.rows.Count; i++) {
                        List<Line> row = ps.rows[i];
                        int m = 0;
                        for (int k = 0; k < colBoxes.Count; k++) {
                            bool done = false;
                            for (int j = m; j < row.Count; j++)
                                if (colBoxes[k].Contains(row[j].Bounds())) {
                                    elts[i, k] = Parse(row[j]);
                                    m++;
                                    done = true;
                                    break;
                                }
                            if (!done) elts[i, k] = new NullExpr();
                        }
                    }
                }
                e = new ArrayExpr(elts);
                e.Annotations["Force Parentheses"] = 1;

                List<Symbol> delims = new List<Symbol>();
                delims.Insert(0, s);
                delims.Add(ps.Parse2Closing);
                e.Annotations["delimiters"] = delims;

                s.expr = e;
                if(ps.Parse2Closing != null) {
                    ps.Parse2Closing.expr = e;

                    if(!ps.Parse2Closing.Sub.Empty) {
                        Expr sub = Parse(ps.Parse2Closing.Sub);
                        e = new CompositeExpr(WellKnownSym.subscript, e, sub);
                    }
                    if(!ps.Parse2Closing.Super.Empty) {
                        Expr super = Parse(ps.Parse2Closing.Super);
                        e = new CompositeExpr(WellKnownSym.power, e, super);
                    }
                }
            } else {
                switch(s.r.alt.Other) {
                    case Recognition.Result.Special.Imaginary:
                        e = WellKnownSym.i.Clone();
                        break;
                    case Recognition.Result.Special.NatLogBase:
                        e = WellKnownSym.e.Clone();
                        break;
                    default:
                        switch(s.r.alt.Character) {
                            case Unicode.S.SET_MINUS:
                                if(!s.Sub.Empty && s.Sub._syms[0].r.allograph.Contains("1")) {
                                    Strokes sks = s.r.strokes.Ink.CreateStrokes();
                                    sks.Add(s.r.strokes[0]);
                                    sks.Add(s.Sub._syms[0].r.strokes[0]);
                                    MyParser.Charreco.FullClassify(s.r.strokes[0],
                                        new Recognition(sks, "y", (sks.GetBoundingBox().Bottom+sks.GetBoundingBox().Top)/2, (sks.GetBoundingBox().Bottom+sks.GetBoundingBox().Top)/2, false));
                                    throw new Reparse1Exception("set minus reinterpreted as y");
                                }
                                e = new LetterSym(s.r.alt.Character);
                                break;
                            default:
                                e = Syntax.Fixes.Translate(s.r.alt.Character);
                                if(e == null) {
                                    e = Syntax.CharWKSMap[s.r.alt.Character];
                                    if(e == null) e = new LetterSym(s.r.alt.Character);
                                }
                                break;
                        }
                        break;
                }
                e.Annotations["Symbols"] = s;
                s.expr = e;
                if(!s.Sub.Empty) {
                    Expr sub = Parse(s.Sub);
                    if(e is LetterSym) ((LetterSym)e).Subscript = sub;
                    else e = new CompositeExpr(WellKnownSym.subscript, e, sub);
                }
                if(!s.Super.Empty) {
                    Expr super = Parse(s.Super);
                    e = new CompositeExpr(WellKnownSym.power, e, super);
                }
            }
            return e;
        }
        private static readonly WKSID[] _wkfns = new WKSID[] { WKSID.arccos, WKSID.acosh, WKSID.acot, WKSID.acoth, WKSID.acsc, WKSID.acsch, WKSID.arg,
            WKSID.asec, WKSID.asech, WKSID.arcsin, WKSID.asinh, WKSID.arctan, WKSID.atanh, WKSID.cos, WKSID.cosh, WKSID.cot, WKSID.coth,
            WKSID.csc, WKSID.csch, WKSID.im, WKSID.ln, WKSID.log, WKSID.mod, WKSID.re, WKSID.sec, WKSID.sech, WKSID.sin, WKSID.sinh, WKSID.tan,
            WKSID.tanh, WKSID.True, WKSID.False, WKSID.sum, WKSID.avg };
        public static WKSID[] WKFns { get { return _wkfns; } }

        private static readonly string[] _othertokens = new string[] {"while", "if", "else", "input", "Input", "INPUT"};
        private static string[] _funcNames = new string[] { "funcName1", "funcName2", "funcName3", "funcName4", "funcName5", "funcName6", "funcName7", "funcName8", "funcName9", "funcName10" };
        public static string funcNames {
            set {
                int i = 0;
                while (!_funcNames[i].StartsWith("funcName")) {
                    if (_funcNames[i] == value) return;
                    else i++; 
                }
                _funcNames[i] = value;
                List<string> wl = new List<string>();
                foreach (string tok in _funcNames) {
                    if (tok.StartsWith("funcName")) continue;
                    _tokenmap[tok] = new WordSym(tok);
                    wl.Add(tok);
                }
                FeaturePointDetector.AddWords(wl);
            }
        }

        private class StringLengthComparer : IComparer<string> {
            public int Compare(string x, string y) {
                if(x.Length == y.Length) return -x.CompareTo(y);
                else return -x.Length.CompareTo(y.Length);
            }
        }
        private static SortedDictionary<string, Expr> _tokenmap = new SortedDictionary<string, Expr>(new StringLengthComparer());
        private static Dictionary<string, Expr> _strictTokenmap = new Dictionary<string, Expr>();
        static Parse2() {
            Array.Sort(_wkfns);
            _expfns.Sort();
            List<string> wl = new List<string>();
            foreach(WKSID id in _wkfns) {
                string s = Enum.GetName(typeof(WKSID), id);
                _tokenmap[s] = new WellKnownSym(id);
                wl.Add(s);
                if(s != s.ToLower()) {
                    s = s.ToLower();
                    _tokenmap[s] = new WellKnownSym(id);
                    wl.Add(s);
                }
            }
            _tokenmap["lim"] = WellKnownSym.limit;
            wl.Add("lim");
            foreach(string tok in _othertokens) {
                _tokenmap[tok] = new WordSym(tok);
                wl.Add(tok);
            }
            FeaturePointDetector.AddWords(wl);
            _tokenmap["=>"] = new LetterSym(Unicode.R.RIGHTWARDS_DOUBLE_ARROW);
            _tokenmap["\\1"] = new LetterSym('y');
            // next line also takes all "i = " if put in _tokenmap!
            _strictTokenmap[":="] = WellKnownSym.definition;
            // next line would take 11 where both 1s are slanted if it were put in _tokenmap
            _strictTokenmap["//"] = new WordSym("//");
        }
    }
}
