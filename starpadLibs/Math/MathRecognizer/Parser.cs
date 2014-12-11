using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Microsoft.Ink;
using System.Drawing;
using System.Collections;
using starPadSDK.MathExpr;
using System.Diagnostics;
using starPadSDK.UnicodeNs;
using starPadSDK.CharRecognizer;
using System.Runtime.Serialization.Formatters.Binary;

namespace starPadSDK.MathRecognizer {
    public class Reparse1Exception : ApplicationException {
        public string Expln;
        public Reparse1Exception(string expln)
            : base("A 1st-level symbol parse error occurred: " + expln) {
            Expln = expln;
        }
    }
    public class RerecogException : ApplicationException {
        public Strokes Stks;
        public RerecogException(Strokes stks)
            : base("A 1st-level symbol parse error occurred: ") {
            Stks = stks;
        }
    }
    public class Line {
        public List<Symbol> _syms = new List<Symbol>();
        public Symbol Last { get { return Empty ? null : _syms[_syms.Count-1]; } }
        public bool Empty { get { return _syms.Count == 0; } }
        public Line() { }
        public bool Uses(Symbol n, bool inside, bool allowSupers) {
            if (n.Sym == null)
                return false;
            if (_syms.Count == 0) {
                _syms.Add(n);
                return true;
            }
            for (int s = _syms.Count - 1; s >= 0; s--) {
                Symbol.Res r = _syms[s].Uses(n, s < _syms.Count - 1 ? _syms[s + 1] : null, Symbol.SymTest.TestAll, inside, allowSupers);
                if (r == Symbol.Res.Sib)
                    _syms.Add(n);
                if (r != Symbol.Res.Unused)
                    return true;
            }
            return false;
        }
        public Rectangle BaseLineBounds() {
            if (Empty)
                return Rectangle.Empty;
            Rectangle b = (_syms[0].Sym == Recognition.Result.Special.Division ? _syms[0].Bounds : _syms[0].StrokeBounds);
            for (int i = 1; i < _syms.Count; i++)
                b = Rectangle.Union((_syms[i].Sym == Recognition.Result.Special.Division ? _syms[i].Bounds : _syms[i].StrokeBounds), b);
            return b;
        }
        public Rectangle EntryBounds() {
            if (Empty)
                return Rectangle.Empty;
            if (_syms.Count == 1) {//including division line
                if(Bounds().Width > 0.8*Bounds().Height )
                    return Bounds();
                return new Rectangle((Bounds().X + (int)(Bounds().Width * 0.5 - Bounds().Height * 0.4)), Bounds().Y, (int)(Bounds().Height*0.8), Bounds().Height);
            }
            if ((Bounds().Width + 0.0) / Bounds().Height > 0.5)
                return Bounds();
            return new Rectangle((Bounds().X + (int)(Bounds().Width*0.5 - Bounds().Height * 0.25)), Bounds().Y, (int)(Bounds().Height * 0.5), Bounds().Height);
        }

        public Rectangle Bounds() {
            if (Empty)
                return Rectangle.Empty;
            Rectangle b = _syms[0].TotalBounds;
            for (int i = 1; i < _syms.Count; i++)
                b = Rectangle.Union(_syms[i].TotalBounds, b);
            return b;
        }
        public string Print() {
            string res = "";
            foreach (Symbol s in _syms)
                res += s.Print();
            return res;
        }
    }

    public class Symbol {
        public enum Res { Sib, Used, Unused };
        public static bool ParseParensToMatrices = true;

        static public Symbol From(FeaturePointDetector charreco, Recognition r) {
            if (ParseParensToMatrices && r.alt.Character == '(')
                return new ParenSym(charreco, r);
            if (r.alt == ',' || r.alt == '.' || r.alt == '~')
                return new NoModSym(charreco, r);
            if (r.alt == Recognition.Result.Special.Division)
                return new DivSym(charreco, r);
            if (r.alt == Unicode.M.MINUS_SIGN || r.alt == Unicode.R.RIGHTWARDS_ARROW || r.alt == Unicode.D.DIVISION_SIGN || r.alt == '(' || r.alt == '[' || r.alt == '{' || r.alt == Unicode.L.LEFT_FLOOR
                || r.alt == Unicode.L.LEFT_CEILING || r.alt == '=' || r.alt == '+' || r.alt == '/')
                return new NoModSym(charreco, r);
            if (r.alt == Unicode.S.SQUARE_ROOT)
                return new RootSym(charreco, r);
            if (r.alt == Unicode.I.INTEGRAL)
                return new IntSym(charreco, r);
            if (r.alt == Unicode.N.N_ARY_SUMMATION)
                return new IntSym(charreco, r);
            return new Symbol(charreco, r);
        }

        public Line Super = new Line();
        public Line Sub = new Line();
        public Recognition r;
        private int xht, bas;
        Rectangle bounds;
        public Expr expr = null;
        static public bool PrintTopAlternate = false;
        virtual public string Print() {
            string res;
            if (Sym.Character != (char)0) res = PrintTopAlternate ? r.alts[0].Character.ToString() : Sym.Character.ToString();
            else if (Sym.Word != null) res = Sym.Word;
            else res = Recognition.Result.ToChar(Sym.Other).ToString();
            if (!Super.Empty)
                res += " ^ ( " + Super.Print() + " ) ";
            if (!Sub.Empty)
                res += "_[" + Sub.Print() + "]";
            return res;
        }

        virtual public void Offset(int x, int y) {
            foreach (Symbol sm in Sub._syms)
                sm.Offset(x, y);
            foreach (Symbol sm in Super._syms)
                sm.Offset(x, y);
            r.Offset(0, y);
            foreach (Stroke m in r.strokes)
                m.Move(0, y);
        }
        public int Xht { get { return xht; } set { xht = value; } }
        public int Base { get { return bas; } set { bas = value; } }
        public Recognition.Result Sym { get { return r.alt; } }
        public Rectangle StrokeBounds { get { return r.strokes != null ? r.bbox : Rectangle.Empty; } }
        virtual public Rectangle Bounds { get { return bounds; } set { bounds = value; } }
        virtual public Rectangle TotalBounds {
            get {
                Rectangle totalBounds = StrokeBounds;
                Rectangle sp = Super.Bounds();
                Rectangle sb = Sub.Bounds();
                if (sp != Rectangle.Empty)
                    totalBounds = Rectangle.Union(totalBounds, sp);
                if (sb != Rectangle.Empty)
                    totalBounds = Rectangle.Union(totalBounds, sb);
                return totalBounds;
            }
        }
        protected FeaturePointDetector _charreco;
        public FeaturePointDetector Charreco { get { return _charreco; } }
        public Symbol(FeaturePointDetector charreco, Recognition rec) {
            _charreco = charreco;
            r = rec;
            Xht = r.xheight;
            bas = r.baseline;
            bounds = new Rectangle(StrokeBounds.Left, Xht, StrokeBounds.Width, Base - Xht);
        }
        public enum SymTest {
            TestAll,   // test sibling, not super/subscripts (not super/subscripts ??)
            TestMods,  // only test for super/subscripts, not siblings
            TestStrict // strict test for sub/super that are smaller than base
        }
        virtual public Res Uses(Symbol n, Symbol sibling, SymTest test, bool inside, bool allowSupers) {
            if (sibling != null) {
                if (".,".Contains(n.r.alt.Character.ToString()) && r.strokes.Count == 1 && ("\\()uL1l/" + Unicode.D.DIVISION_SLASH).Contains(r.alt.Character.ToString())) {
                    if (n.Bounds.Bottom < (Bounds.Top + Bounds.Bottom) / 2) {
                        Strokes sks = this.r.strokes.Ink.CreateStrokes();
                        sks.Add(r.strokes);
                        sks.Add(n.r.strokes);
                        _charreco.FullClassify(n.r.strokes[0],
                            new Recognition(sks, "i", sks.GetBoundingBox().Bottom, (sks.GetBoundingBox().Top + sks.GetBoundingBox().Bottom) / 2, false));
                        throw new Reparse1Exception("reinterpret as i");
                    }
                }
                if (n.Bounds.Left > sibling.Bounds.Right || sibling.Sym != ',')
                    return Res.Unused;
            }
            if ((StrokeBounds.Right - n.Bounds.Right + 0.0) / n.Bounds.Width > 1.5 && Sym.Character != Unicode.G.GREEK_SMALL_LETTER_PI && Sym.Character != Unicode.P.PERPENDICULAR && Sym.Character != 'T' && Sym.Character != '↘')
                return Res.Unused;
            string small = "acemnorsuvwxz.,=+><^-~αεικνοπστυω°()" + Unicode.M.MINUS_SIGN + Unicode.D.DOT_OPERATOR + Unicode.P.PLUS_MINUS_SIGN;
            int spacing = !small.Contains(Sym.Character.ToString()) ? Math.Max(Bounds.Width, Bounds.Height) : Math.Max(Bounds.Width, Math.Max(n.Bounds.Height, n.Bounds.Width));
            if (small.Contains(Sym.Character.ToString()) && small.Contains(n.Sym.Character.ToString()))
                spacing = (int)(_charreco.InkPixel * 250);
            Rectangle ntotbounds = n.TotalBounds;
            Rectangle totbounds = TotalBounds;
            double nearRatio = Math.Max(totbounds.Top - ntotbounds.Bottom + 0.0,
                                  Math.Max(ntotbounds.Top - totbounds.Bottom + 0.0, (ntotbounds.Left - totbounds.Right + 0.0)/2)) / spacing;
            if ((n.Sym == '.' || n.Sym == Unicode.D.DOT_OPERATOR) &&
                (n.StrokeBounds.Bottom - StrokeBounds.Top + 0.0) / StrokeBounds.Height > 0.2 &&
                (n.StrokeBounds.Bottom - StrokeBounds.Top + 0.0) / StrokeBounds.Height < 0.69) {
                n.r.addorsetalt(new Recognition.Result(Unicode.D.DOT_OPERATOR), n.r.baseline, n.r.baseline);
                n.r.levelsetby = 0;
                return Res.Sib;
            }
            if (n.Sym == ',' && n.StrokeBounds.Height > StrokeBounds.Height)
                return Res.Unused;
            if ((n.Sym == ',' || (n.r.alts.Length > 1 && n.r.alts[1] == ',')) && Sub.Empty)
                if ((Bounds.Bottom - n.StrokeBounds.Top + 0.0) / Bounds.Height > -.25 && n.StrokeBounds.Bottom > StrokeBounds.Bottom &&
                     (n.StrokeBounds.Top - Bounds.Top + 0.0) / Bounds.Height > 0.4) {
                    if (n.Sym != ',') {
                        n.r.curalt = 1;
                        n.r.levelsetby = 2;
                    }
                    return Res.Sib;
                }

            if (Sym.Character == ',')
                return Res.Sib;
            if (!inside && Math.Abs(nearRatio) > 1.5)
                return Res.Unused;
            bool heightRatio = (n.Bounds.Height + 0.0) / Bounds.Height > (n.Sym == '+' || n.Sym == '<' || n.Sym == '>' ? .35 : (n.StrokeBounds.Height > n.Bounds.Height * 1.5 ? .625 : .5)); // above .625 is siblike, else super/sub
            Rectangle nbounds = n.StrokeBounds;
            if (n.Sym == ')')
                nbounds = new Rectangle(nbounds.Left, nbounds.Top < Bounds.Top ? Bounds.Top : nbounds.Top,
                                        nbounds.Width, (nbounds.Bottom > Bounds.Bottom ? Bounds.Bottom : nbounds.Bottom) -
                                        (nbounds.Top < Bounds.Top ? Bounds.Top : nbounds.Top));
            else if (n.Sym != '0')
                nbounds = n.Bounds;
            double hgtAdjust = ((0.0 + n.StrokeBounds.Height) / Bounds.Height);
            if (small.Contains(n.Sym.Character.ToString()))
                hgtAdjust = hgtAdjust * hgtAdjust;
            bool useStrokeBounds = char.IsDigit(Sym.Character) || Sym.Character == '|';//&& char.IsDigit(n.Sym.Character);
            Rectangle refBounds = useStrokeBounds ? StrokeBounds : Bounds;
            double botRise = (nbounds.Bottom - refBounds.Top + 0.0) / (Sym.Character == '4' ? Bounds.Height : refBounds.Height);
            double topDrop = (refBounds.Bottom - nbounds.Top + 0.0) / refBounds.Height;
            bool above = botRise < 0.4 || (botRise < .55 && (n.Bounds.Height + 0.0) / refBounds.Height < 1);
            bool below = topDrop < 0.3 || topDrop < .7 && (refBounds.Bottom - n.StrokeBounds.Top) * topDrop * hgtAdjust < n.Bounds.Bottom - refBounds.Bottom;
            bool sibFirst = botRise > .4 && heightRatio && ((!above && !below) ||
                (StrokeBounds.Top >= nbounds.Top && StrokeBounds.Bottom <= nbounds.Bottom) ||
                ((3 * StrokeBounds.Top + 2 * StrokeBounds.Bottom) / 5 > nbounds.Top && (2 * StrokeBounds.Top + 3 * StrokeBounds.Bottom) / 5 < nbounds.Bottom));
            //            if (Sym == ')' && topDrop >0 && below)
            //                return Res.Sib;
            if (n is DivSym) {
                if (n.TotalBounds.Bottom < Bounds.Bottom && n.TotalBounds.Top < Bounds.Top) {
                    above = true;
                    sibFirst = false;
                }
                if (n.TotalBounds.Top < Bounds.Bottom && n.TotalBounds.Bottom > Bounds.Bottom) {
                    below = true;
                    sibFirst = false;
                }
            }
            if ((Sym.Character == Unicode.G.GREEK_SMALL_LETTER_PI || Sym.Character == 'T' || Sym.Character == 'J' || Sym.Character == '5') && above && n.Bounds.Right < Bounds.Right) {
                throw new RerecogException(this.r.strokes);
            }
            if (r.allograph == "perp" && below && n.Bounds.Right < Bounds.Right) {
                throw new RerecogException(this.r.strokes);
            }
            if (n.r.allograph == "perp" && above && Bounds.Right < n.Bounds.Right && (Bounds.Right- n.Bounds.Left+0.0)/Bounds.Width > 0.1) {
                throw new RerecogException(n.r.strokes);
            }
            if (Sym.Character == 'I' && below && n.Bounds.Right < Bounds.Right) {
                throw new RerecogException(this.r.strokes);
            }
            if (above && Super.Empty && (n.Sym == ',' || (n.r.alts.Length > 1 && n.r.alts[1] == ',')))
                return Res.Unused;
            if (!below && botRise > 0 && nbounds.Top > n.StrokeBounds.Top && 
                ((n.Sym == Unicode.I.INTEGRAL && (n.StrokeBounds.Bottom-StrokeBounds.Top+0.0)/StrokeBounds.Height >= 0.6) ||
                n.StrokeBounds.Top > StrokeBounds.Top) &&
                (n.Bounds.Height + 0.0) / Bounds.Height > (small.Contains(n.Sym.Character.ToString()) ? 0.45 : 0.65)) {
                above = false;
                sibFirst = true;
            }
            if (!allowSupers)
                above = false;
            if (test == SymTest.TestStrict && (above || below || botRise < .65))
                sibFirst = false;
            if (!above && !below && Sym.Character == ')')
                sibFirst = true;
            if (n.StrokeBounds.Top < StrokeBounds.Bottom && n.StrokeBounds.Bottom > StrokeBounds.Top && n.Sym.Character == '=') {
                sibFirst = true;
                above = below = false;
            }
            if (sibFirst && test != SymTest.TestMods && sib(n)) {
                if ((n.Sym == ',' || (n.r.alts.Length > 1 && n.r.alts[1] == ',')))
                    if ((Bounds.Bottom - n.StrokeBounds.Top + 0.0) / Bounds.Height > -.25 && n.StrokeBounds.Bottom > StrokeBounds.Bottom &&
                     (n.StrokeBounds.Top - Bounds.Top + 0.0) / Bounds.Height > 0.4) {
                        n.r.curalt = 1;
                        n.r.levelsetby = 2;
                    }
                if (n.Sym == ')' && (n.StrokeBounds.Top - StrokeBounds.Top + 0.0) / StrokeBounds.Height > 0.3)
                    return Res.Unused;
                return Res.Sib;
            }
            if (allowSupers && !sibFirst && (botRise < 0.6 || (botRise < 0.8 && (n.Bounds.Height + 0.0) / Bounds.Height < 0.5)) && topDrop > botRise &&
                (n.StrokeBounds.Top - StrokeBounds.Top + 0.0) / StrokeBounds.Height < 0.2)
                above = true;
            if (above && n.Bounds.Bottom > (Bounds.Top + 2 * Bounds.Bottom) / 3)
                above = false;
            if (!sibFirst) {
                Rectangle ntotbnds = n.TotalBounds;
                float aboveCover = (Math.Min(ntotbnds.Bottom, Bounds.Bottom) - Math.Max(ntotbnds.Top, Bounds.Top)) / (float)ntotbnds.Height;
                float belowCover = (Math.Max(ntotbnds.Bottom, Bounds.Bottom) - Bounds.Bottom) / (float)ntotbnds.Height;
                if (aboveCover / belowCover < 3)
                    below = true;
                else if (below && Sub.Empty && n.Bounds.Height / (float)Bounds.Height > 0.5) {
                    below = false;
                    sibFirst = true;
                } else if (hgtAdjust < 0.4 && !Sub.Empty && (Sub.Bounds().Top - n.StrokeBounds.Bottom + 0.0) / Bounds.Height < hgtAdjust)
                    below = true;
                else if (hgtAdjust < 0.5 && (n.StrokeBounds.Top - (StrokeBounds.Top + StrokeBounds.Bottom) / 2 + 0.0) / n.StrokeBounds.Height > 0.2)
                    below = true;
            }
            if (test == SymTest.TestStrict && heightRatio && n.StrokeBounds.Height > StrokeBounds.Height)
                above = below = false;
            double aboveang = FeaturePointDetector.angle(Bounds.Location, nbounds.Location, new PointF(-1,0));
            if (test == SymTest.TestMods && aboveang < 20)
                above = false;
            if (sibFirst && below && Bounds.Height / (float)n.Bounds.Height < 1) {
                below = false;
            }
            nearRatio = Math.Max(totbounds.Top - ntotbounds.Bottom + 0.0,
                                  Math.Max(ntotbounds.Top - totbounds.Bottom + 0.0, (ntotbounds.Left - totbounds.Right + 0.0))) / spacing;
            if (Math.Abs(nearRatio) < 1.5 && above && super(n))
                return Res.Used;
            if (below) {
                Rectangle nbox = n.r.strokes[0].GetBoundingBox();
                Point nearpt = FeaturePointDetector.getPt(r.strokes[0].NearestPoint(new Point(nbox.Right, nbox.Top)), r.strokes[0].GetPoints());
                double subang = FeaturePointDetector.angle(new Point(nbox.Right, nbox.Top), nearpt, new PointF(1, 0));
                if ((subang >45 && nearRatio > 1) || subang > 75)
                    return Res.Unused;
            }
            if (below && sub(n)) {
                if (Sub._syms.Count == 1 && ("\\,".Contains(Sub._syms[0].Sym.Character.ToString()) ||
                                           (Sub._syms[0].r.alts.Length > 1 && "\\,".Contains(Sub._syms[0].r.alts[1].Character.ToString())))) {
                    if (n.Sym != ',' && n.Sym != '\\' && (n.StrokeBounds.Top - Bounds.Top + 0.0) / Bounds.Height > 0.4) {
                        n.r.curalt = 1;
                        n.r.levelsetby = 2;
                    }
                    Sub._syms.Clear();
                    return Res.Sib;
                }
                return Res.Used;
            }
            if (Sym != '.' && n.Sym != '.' && (StrokeBounds.Top > n.StrokeBounds.Bottom || StrokeBounds.Bottom < n.StrokeBounds.Top))
                return Res.Unused;
            if (((n.Sym == ',' || (n.r.alts.Length > 1 && n.r.alts[1] == ',')) && !above) ||
                (!(n.Sym == ',' || (n.r.alts.Length > 1 && n.r.alts[1] == ',')) && !sibFirst && test == SymTest.TestAll && sib(n))) {
                if(n.r.allograph.Contains("1") && hgtAdjust < .6 && n.StrokeBounds.Height < Charreco.InkPixel * 25 && !below &&
                    (n.StrokeBounds.Bottom - StrokeBounds.Top + 0.0) / StrokeBounds.Height > 0.2 &&
                        (n.StrokeBounds.Bottom - StrokeBounds.Top + 0.0) / StrokeBounds.Height < 0.69) {
                    if(n.r.levelsetby >= 1) {
                        n.r.addorsetalt(new Recognition.Result(Unicode.D.DOT_OPERATOR), n.r.baseline, n.r.baseline);
                        n.r.levelsetby = 1;
                    }
                } else {
                    if(n.r.alts.Length > 1 && n.r.alts[1] == ',')
                        if((Bounds.Bottom - n.StrokeBounds.Top + 0.0) / Bounds.Height > -.25 &&
                            (n.StrokeBounds.Top - Bounds.Top + 0.0) / Bounds.Height > 0.4) {
                            if(n.r.levelsetby >= 2) {
                                n.r.curalt = 1;
                                n.r.levelsetby = 2;
                            }
                        }
                }
                return Res.Sib;
            }
            if (test == SymTest.TestAll && below && char.IsDigit(r.alt.Character) && n.StrokeBounds.Top > StrokeBounds.Top &&
                (StrokeBounds.Bottom-n.StrokeBounds.Top+0.0)/n.StrokeBounds.Height > 0.2)
                if (!char.IsLetter(n.r.alt.Character))
                    return Res.Unused;
                else return Res.Sib;
            if (((n.StrokeBounds.Top > StrokeBounds.Top && n.StrokeBounds.Bottom < StrokeBounds.Bottom) || (!above && !below)) &&
                test == SymTest.TestAll)
                return Res.Sib;
            return Res.Unused;
        }
        protected bool sib(Symbol n) {
            Rectangle totalBounds = TotalBounds;
            if (Sym != '.' && Sym != Unicode.D.DOT_OPERATOR && (n.StrokeBounds.Left - totalBounds.Right + 0.0) / Math.Max(Bounds.Height, Bounds.Width) > 2)
                return false;
            if (Sym == '.') {
                if (StrokeBounds.Bottom > n.TotalBounds.Top && StrokeBounds.Top < (n.TotalBounds.Bottom + n.TotalBounds.Height * .2))
                    return true;
                else return false;
            }
            if (n.Sym == '.') {
                if (StrokeBounds.Bottom + StrokeBounds.Height * .4 > n.Bounds.Top && Bounds.Top < n.Bounds.Bottom)
                    return true;
                else return false;
            }
            if (n.Sym == ',') {
                if (Bounds.Bottom > n.StrokeBounds.Top && n.StrokeBounds.Bottom > StrokeBounds.Bottom)
                    return true;
            }
            if (n.Sym == Unicode.M.MINUS_SIGN || n.Sym == Unicode.R.RIGHTWARDS_ARROW || n.Sym == '+' || n.Sym == '=' || n.Sym == '<' || n.Sym == '>') {
                if ((n.Bounds.Bottom - StrokeBounds.Top + 0.0) / Bounds.Height < 0.3 ||
                    (n.Bounds.Bottom - StrokeBounds.Bottom + 0.0) / n.Bounds.Height > 0.5 || 
                    (StrokeBounds.Bottom - n.Bounds.Top + 0.0) / Bounds.Height < 0.15)
                    return false;
                return true;
            }
            if (r.alt != Recognition.Result.Special.Division && n.Sym != ')' && Sym != '.' &&
                (Bounds.Bottom - n.StrokeBounds.Bottom + 0.0) / Bounds.Height > .3 &&
                (Bounds.Top + 0.0) / ((n.Bounds.Bottom + n.Bounds.Top) / 2) > 1.2)
                return false;
            if (n.StrokeBounds.Top > StrokeBounds.Top || n.StrokeBounds.Bottom < StrokeBounds.Bottom) {
                if (r.alt != Recognition.Result.Special.Division && n.r.alt != Recognition.Result.Special.Division && n.Sym != ')' && Sym != '.' &&
                (Bounds.Bottom - n.StrokeBounds.Top + 0.0) / Bounds.Height < (n.Bounds.Bottom - Bounds.Bottom + 0.0) / Bounds.Height)
                    return false;
                double below = (n.StrokeBounds.Bottom - StrokeBounds.Bottom + 0.0) / Bounds.Height;
                double above = (StrokeBounds.Bottom - n.StrokeBounds.Top + 0.0) / Bounds.Height;
                double midline = (n.Bounds.Top - (Bounds.Bottom + Bounds.Top) / 2 + 0.0) / Bounds.Height;
                if (midline > 0 && below + midline > 0.35)
                    return false;
                if (n.Bounds.Top - Bounds.Bottom > 0)
                    return false;
                if (n.Sym != ')' && Bounds.Bottom > n.Bounds.Bottom && StrokeBounds.Top > (n.Bounds.Bottom + n.Bounds.Top) / 2)
                    return false;
            }
            double threshold = StrokeBounds.Height / (float)StrokeBounds.Width < 3 ? 0.3 : 0.15;
            if(n.r.levelsetby >= 1) {
                if(n.Sym == 't' && char.IsLetterOrDigit(Sym.Character) &&
                V2D.Straightness(n.r.strokes[0].GetPoints()) < threshold && V2D.Straightness(n.r.strokes[1].GetPoints()) < threshold)
                    if(n.StrokeBounds.Height / (float)Bounds.Height < 1.1) {
                        n.r.addorsetalt('+', n.r.baseline, (n.r.bbox.Height / 2 + n.r.bbox.Top));
                        n.r.levelsetby = 1;
                    }
                /*if(n.Sym == '1' && (char.IsLetter(Sym.Character) || "αεικνοπστυωΓΔΘΛΞΠΣΦΨΩδϕθλγημρςφχψ".Contains(Sym.Character.ToString()))) {
                    n.r.addorsetalt('|', n.r.baseline, (n.r.bbox.Height / 2 + n.r.bbox.Top));
                    n.r.levelsetby = 1;
                } */
            }
            return true;
        }
        protected bool super(Symbol n) {
            if (Super._syms.Count > 0)
                return Super.Uses(n, false, true);
            if (n.Sym == '<' || n.Sym == '>' || n.Sym == '=')
                return false;
            if (n.Sym == Unicode.I.INTEGRAL && (n.StrokeBounds.Bottom-StrokeBounds.Top+0.0)/StrokeBounds.Height > 0.6)
                return false;
            if (n.Sym == ')' && n.r.alts.Length > 1 && n.r.alts[1] == ',')
                return false;
            if (n.Sym == Unicode.M.MINUS_SIGN || n.Sym == '+')
                if (n.StrokeBounds.Bottom > (Bounds.Bottom + 2 * Bounds.Top) / 3)
                    return false;
            if (n.Sym == Unicode.P.PLUS_MINUS_SIGN)
                if (n.StrokeBounds.Bottom > ((Bounds.Top > StrokeBounds.Top) ? Bounds.Top : (Bounds.Bottom + 2 * Bounds.Top) / 3))
                    return false;
            if (Sym != Unicode.I.INTEGRAL && Sym != Unicode.N.N_ARY_SUMMATION && n.StrokeBounds.Right < (2 * StrokeBounds.Right + StrokeBounds.Left) / 3)
                return false;
            double above_bottom = (StrokeBounds.Bottom - n.StrokeBounds.Bottom + 0.0) / StrokeBounds.Height;
            if (Sym == ')') {
                double below_top = (n.StrokeBounds.Top - StrokeBounds.Top + 0.0) / StrokeBounds.Height;
                if (below_top < 0.2 && above_bottom > 0.4 && above_bottom > below_top) {
                    Super._syms.Add(n);
                    return true;
                }
            }
            double above_baseline = (n.Bounds.Bottom - Bounds.Top + 0.0) / Bounds.Height;
            double above_xheight = (Bounds.Top - n.Bounds.Top + 0.0) / Bounds.Height;
            bool above_top = (StrokeBounds.Top > n.StrokeBounds.Top);
            // if (n.Sym == '0' && above_baseline < 0.5 && (n.StrokeBounds.Height+0.0)/StrokeBounds.Height > 0.5)
            //     return false;
            if (n.StrokeBounds.Bottom > StrokeBounds.Bottom)
                return false;
            if (Bounds.Top < n.StrokeBounds.Top && above_bottom < 0.4 && Sym != ')' && n.Sym != '.')
                return false;
            if ((StrokeBounds.Top - n.StrokeBounds.Bottom + 0.0) / StrokeBounds.Height > 1.25)
                return false;
            if (!above_top && above_xheight / above_baseline < 1.5 && above_baseline > (bounds.Height + 0.0) / n.Bounds.Height * 0.35 && above_bottom < 0.4)
                return false;
            if (above_xheight < 0) {
                if (above_baseline > Math.Max(.6, .6 * (3 * Bounds.Height / 4.0) / n.Bounds.Height))
                    return false;
            } else if (above_xheight < 1 && !(n is DivSym)) {
                if (above_baseline * (1 - above_xheight) > Math.Min(.6, .6 * (2 * Bounds.Height / 3.0) / n.Bounds.Height))
                    return false;
            } else if ((above_baseline > 0.8 && !(n is DivSym)) || above_xheight < .8 * above_baseline)
                return false;
            if (Sym != Unicode.I.INTEGRAL && Sym != Unicode.N.N_ARY_SUMMATION && (n.Bounds.Left - Bounds.Right + 0.0) / Bounds.Width <
                (n.Sym == Unicode.M.MINUS_SIGN ? -.85 : -0.55) &&
                (n.Bounds.Left - Bounds.Right + 0.0) / Bounds.Width * ((StrokeBounds.Top - n.StrokeBounds.Bottom + 0.0) / n.Bounds.Height) * 2 < -0.5)
                return false;
            if (above_baseline > 1)
                return false;
            double dist = Math.Abs(n.Bounds.Left - Bounds.Right);
            if (n.r.alt == Recognition.Result.Special.Division) {
                if (n.TotalBounds.Bottom < Bounds.Top)
                    dist = V2D.Dist(new Point(n.TotalBounds.Left, n.TotalBounds.Bottom), new Point(Bounds.Right, Bounds.Top));
            } else if (n.StrokeBounds.Bottom < Bounds.Top)
                dist = V2D.Dist(new Point(n.StrokeBounds.Left, n.StrokeBounds.Bottom), new Point(Bounds.Right, Bounds.Top));
            double dratio = dist / Math.Max(StrokeBounds.Width, Math.Max(StrokeBounds.Height, Math.Max(Bounds.Width, Bounds.Height)));
            if (n.StrokeBounds.Right > StrokeBounds.Right && n.Sym.Character != Unicode.M.MINUS_SIGN && n.Sym.Character != Unicode.I.INTEGRAL && n.Sym.Character != Unicode.S.SQUARE_ROOT && Sym.Character != Unicode.N.N_ARY_SUMMATION && Sym.Character != Unicode.I.INTEGRAL)
                if (dratio > 1 && dratio > 2 * (StrokeBounds.Height + 0.0) / Math.Max(n.Bounds.Width, n.Bounds.Height))
                    return false;
            if (Super.Empty && n.r.allograph == "z2" && n.r.levelsetby > 0) {
                _charreco.Recogs.Remove(n.r.strokes[0].Id);
                n.r = new Recognition(n.r.strokes, "2z", (n.r.bbox.Top+n.r.bbox.Bottom)/2, false);
                _charreco.Recogs.Add(n.r.strokes[0].Id, n.r);
            }
            Super._syms.Add(n);
            return true;
        }
        protected bool sub(Symbol n) {
            if (Sub._syms.Count > 0) {
                return Sub.Uses(n, false, true); // bcz: true to allow superscripts of subscripts
            }
            if (r.alt == "sin" || r.alt == "cos" || r.alt == "tan" || r.alt == '+' || r.alt == '<' || r.alt == '>' || r.alt == '=' || r.alt == Unicode.R.RIGHTWARDS_ARROW/* || Sym == ')'*/)
                return false;
            if (char.IsDigit(Sym.Character) && n.Sym != ',' && n.Sym != ')' && n.Sym != Unicode.D.DIVISION_SLASH && n.Sym != '1')
                return false;
            double below = (Bounds.Bottom - n.StrokeBounds.Bottom + 0.0) / Bounds.Height;
            double above = (Bounds.Bottom - (n.StrokeBounds.Top + n.Bounds.Top) / 2 + 0.0) / Bounds.Height;
            double abovexht = (Bounds.Bottom - n.Bounds.Top + 0.0) / Bounds.Height;
            double sizeratio = (Bounds.Height + 0.0) / n.Bounds.Height;
            if ((Math.Max(n.StrokeBounds.Height, n.Bounds.Height) + 0.0) / Bounds.Height > 0.5) {
                if (below > 0)
                    return false;
                if (abovexht > -below * 1.25 && above > 0.2 && below > -.4 && (abovexht > .7 || sizeratio < 1.2))
                    return false;
            } else if (below > 0.25)
                return false;
            if (n.StrokeBounds.Top < Bounds.Top)
                return false;
            if (n.Bounds.Left - Bounds.Right > 2 * Math.Max(n.Bounds.Width, Math.Max(Bounds.Height, Bounds.Width)))
                return false;
            if ((n.Bounds.Height > StrokeBounds.Height || n.Bounds.Height > 1.5 * Bounds.Height) && above > -below)
                return false;
            double relsize = (Math.Max(StrokeBounds.Height, Bounds.Height) + 0.0) / n.Bounds.Height;
            if (relsize < 0.5)
                return false;
            double dist = V2D.Dist(n.StrokeBounds.Location, new Point(Bounds.Right, Bounds.Bottom));
            if (n.StrokeBounds.Top < Bounds.Bottom || n.StrokeBounds.Top < StrokeBounds.Bottom)
                dist = n.StrokeBounds.Left - Bounds.Right;
            double dratio = dist / Math.Max(StrokeBounds.Height, Bounds.Height);// *Math.Max(n.Bounds.Width, n.Bounds.Height)/Math.Max(Bounds.Width, Bounds.Height);
            if (dratio > 1 && Bounds.Bottom > n.Bounds.Top)
                return false;
            if (dratio > 0.4 && (n.Sym == Unicode.M.MINUS_SIGN || n.Sym == '+' || n.Sym == '<' || n.Sym == '>') && n.Bounds.Top < Bounds.Bottom)
                return false;
            //if(relsize < 1.3 && dist/Math.Max(Bounds.Width, Bounds.Height)>.5)
            //    return false;
            if (/*n.Sym.Character == ')' || */n.Sym.Character == Unicode.D.DIVISION_SLASH || (n.Sym.Character == '1' && char.IsDigit(Sym.Character))) {
                if(n.r.levelsetby >= 1) {
                    n.r.addorsetalt(',', n.r.baseline, (n.bounds.Top + n.bounds.Bottom) / 2);
                    n.r.levelsetby = 1;
                    n = Symbol.From(Charreco, n.r);
                }
            }
            Sub._syms.Add(n);
            return true;
        }
        public override string ToString() {
            return GetType().Name + " " + Sym.Label();
        }
        /***** NOTE: this must be overridden in descendents if they store children Symbols anywhere else... */
        public virtual IEnumerable<Symbol> ChildSyms {
            get {
                foreach (Symbol s in Super._syms) yield return s;
                foreach (Symbol s in Sub._syms) yield return s;
            }
        }
    }
    public class DivSym : NoModSym {
        public DivSym(FeaturePointDetector charreco, Recognition rec) : base(charreco, rec) { }
        override public string Print() {
            string res = "";
            if (!Super.Empty)
                res = "[" + Super.Print() + "]";
            res += "DIV";
            if (!Sub.Empty)
                res += "[" + Sub.Print() + "]";
            return res;
        }
        override public Res Uses(Symbol n, Symbol sibling, SymTest test, bool inside, bool allowSupers) {
            if (sibling != null)
                return Res.Unused;
            Rectangle nbounds = n.Sym == Recognition.Result.Special.Division ? n.TotalBounds : n.r.bbox;
            bool notslashdivision = FeaturePointDetector.angle(StrokeBounds.Location, new Point(StrokeBounds.Right, StrokeBounds.Bottom), new PointF(0, -1)) > 60;
            bool num = nbounds.Bottom < StrokeBounds.Bottom && notslashdivision;
            bool den = nbounds.Top + n.StrokeBounds.Height * .3 > StrokeBounds.Top;
            if (den && !notslashdivision && !Sub.Empty && n.Bounds.Height / (float)Sub._syms[0].Bounds.Height > 1.5) {
                if (n.StrokeBounds.Top < StrokeBounds.Bottom)
                    return Res.Sib;
                return Res.Unused;
            }
            if (n.Sym == '=' || n.Sym == Unicode.R.RIGHTWARDS_ARROW || n.Sym == Unicode.R.RIGHTWARDS_DOUBLE_ARROW)
                num = den = false; // XXX - this assumes we're only using these symbols to evaluate full expressions
            // bool inside = nbounds.Left < StrokeBounds.Right;
            double nearRatio = (n.Bounds.Left - TotalBounds.Right + 0.0) / Math.Max(n.TotalBounds.Width, n.TotalBounds.Height);
            Rectangle nrealbounds = (n.Sym == '.' || n.Sym == Unicode.M.MINUS_SIGN || n.Sym == '+' || n.Sym == '>' || n.Sym == '<') ? n.Bounds : n.StrokeBounds;
            //if (test != SymTest.TestMods && n.Bounds.Right > StrokeBounds.Right) {
                if (n.Sym == ')') {
                    double amountAbove = (StrokeBounds.Bottom - n.StrokeBounds.Top + 0.0) / n.StrokeBounds.Height;
                    double amountBelow = (n.StrokeBounds.Bottom - StrokeBounds.Top + 0.0) / n.StrokeBounds.Height;
                    if (amountAbove > 0.20 && amountBelow > 0.20)
                        return Res.Sib;
                    if (amountBelow > 0.5 && Sub.Uses(n, false, true))
                        return Res.Used;
                    if (amountAbove > 0.5 && Super.Uses(n, false, true))
                        return Res.Used;
                    if (StrokeBounds.Right > n.StrokeBounds.Right) {
                        if (amountAbove > amountBelow)//not reachable
                            Super._syms.Add(n);
                        else Sub._syms.Add(n);
                        return Res.Used;
                    }
                    if (amountAbove <= 0) { // force it onto divisor
                        Sub._syms.Add(n);
                        return Res.Used;
                    } else if (amountBelow <= 0) { // force it onto dividend
                        Super._syms.Add(n);
                        return Res.Used;
                    }
                    return Res.Sib;
                }
                if ((n.Sym == ')' || (n.TotalBounds.Height + 0.0) / TotalBounds.Height > 0.7) &&
                    (n.StrokeBounds.Bottom > StrokeBounds.Top + .3 * n.StrokeBounds.Height && n.StrokeBounds.Top + .3 * n.StrokeBounds.Height < StrokeBounds.Bottom) ||
                    (nrealbounds.Bottom > StrokeBounds.Top + .3 * n.StrokeBounds.Height && nrealbounds.Top + .3 * n.StrokeBounds.Height < StrokeBounds.Bottom)) {
                    if (r.alts[0] == Unicode.D.DIVISION_SLASH && !Sub.Empty && char.IsDigit(Sub.Last.Sym.Character)) {
                        Res re = Sub.Last.Uses(n, null, SymTest.TestStrict, false, true);
                        if (re == Res.Sib) {
                            Sub._syms.Add(n);
                            return Res.Used;
                        } else if (re == Res.Used)
                            return Res.Used;
                    } else if (r.alts[0] == Unicode.D.DIVISION_SLASH && Sub.Empty) {
                        Sub._syms.Add(n);
                        return Res.Used;
                    }
                    return Res.Sib;
                }
            //}
            Res res;
            if (num && nearRatio < 0.9 && Super.Empty) {
                Super._syms.Add(n);
                return Res.Used;
            } else if (num && !Super.Empty && ((StrokeBounds.Bottom - n.Bounds.Bottom + 0.0) / n.Bounds.Height > 2 ||
                   (n.Bounds.Left - StrokeBounds.Right + 0.0) / n.Bounds.Width < -0.3 ||
                  (n.Bounds.Left - Super.Last.Bounds.Right + 0.0) / n.Bounds.Width < 0.3)) {
                if (!Super.Empty && (res = Super.Last.Uses(n, null, nbounds.Left < StrokeBounds.Right ? SymTest.TestAll : SymTest.TestMods, false, true)) != Res.Unused) {
                    if (res == Res.Sib) {
                        Super._syms.Add(n);
                        return Res.Used;
                    } else if (res == Res.Used)
                        return Res.Used;
                }
            }
            if (den && (nearRatio < 0 || (!Sub.Empty && !Super.Empty))) {
                if (Sub.Empty) {
                    Sub._syms.Add(n);
                    return Res.Used;
                }
                Res re = Sub.Last.Uses(n, null, nbounds.Top > StrokeBounds.Bottom ? SymTest.TestAll : SymTest.TestMods, false, true);
                if (re == Res.Sib) {
                    Sub._syms.Add(n);
                    return Res.Used;
                } else if (re == Res.Used)
                    return Res.Used;
            }
            if (test != SymTest.TestMods)
                if (((n.StrokeBounds.Bottom > Bounds.Top && n.StrokeBounds.Top < Bounds.Bottom) ||
                    (n.Bounds.Bottom > Bounds.Top && n.Bounds.Top < Bounds.Bottom)))
                    return Res.Sib;
             //*/
            return Res.Unused;
        }
        public override Rectangle Bounds { get { return TotalBounds; } set { } }
    }
    public class RootSym : NoModSym {
        override public string Print() {
            string res = "sqrt[";
            if (!Super.Empty)
                res += Super.Print();
            res += "]";
            return res;
        }
        override public Res Uses(Symbol n, Symbol sibling, SymTest test, bool inside, bool allowSupers) {
            if (sibling != null)
                return Res.Unused;
            bool allowSibs = n.StrokeBounds.IntersectsWith(StrokeBounds);
            if (!Super.Empty) {
                //if (Super.Bounds().Right > n.Bounds.Right)
                //    return Res.Unused;
                Res rs = Super.Last.Uses(n, null, allowSibs ? SymTest.TestAll : SymTest.TestMods, false, true);
                if (rs == Res.Sib && (n.StrokeBounds.Left < StrokeBounds.Right/*||
                    !("+-/><(="+Unicode.R.RIGHTWARDS_ARROW+Unicode.R.RIGHTWARDS_DOUBLE_ARROW).Contains(n.r.alt.Character.ToString())*/)) {
                    Super._syms.Add(n);
                    return Res.Used;
                }
                if (rs == Res.Used)
                    return Res.Used;
            }
            if (test == SymTest.TestAll && ((n.StrokeBounds.Bottom > StrokeBounds.Top && (StrokeBounds.Bottom - n.StrokeBounds.Top + 0.0) / StrokeBounds.Height > 0.25) ||
                (n.Bounds.Bottom > StrokeBounds.Top && n.Bounds.Top < StrokeBounds.Bottom))) {
                if (Super.Empty && (n.StrokeBounds.Right - StrokeBounds.Right + 0.0) / n.StrokeBounds.Width < 0.4) {
                    Super._syms.Add(n);
                    return Res.Used;
                }
                return Res.Sib;
            }
            return Res.Unused;
        }
        public RootSym(FeaturePointDetector charreco, Recognition rec) : base(charreco, rec) { }
    }
    public class NoModSym : Symbol { // =, (, +, -, 
        public NoModSym(FeaturePointDetector charreco, Recognition rec) : base(charreco, rec) { }
        override public Res Uses(Symbol n, Symbol sibling, SymTest test, bool inside, bool allowSupers) {
            if (sibling != null || test == SymTest.TestMods)
                return Res.Unused;
            if (((n.StrokeBounds.Bottom - StrokeBounds.Top + 0.0) / Math.Max(n.StrokeBounds.Height, StrokeBounds.Height) > -0.5 &&
                 (StrokeBounds.Bottom - n.StrokeBounds.Top + 0.0) / Math.Max(n.StrokeBounds.Height, StrokeBounds.Height) > -0.5) ||
                (n.Bounds.Bottom > StrokeBounds.Top && n.Bounds.Top < StrokeBounds.Bottom)) {
                if (n.Bounds.Left < Bounds.Right && ((Sym.Character == Unicode.M.MINUS_SIGN && n.Sym.Character == '+') ||
                                                    (n.Sym.Character == Unicode.M.MINUS_SIGN && Sym.Character == '+'))) {
                    Strokes sks = this.r.strokes.Ink.CreateStrokes();
                    sks.Add(r.strokes);
                    sks.Add(n.r.strokes);
                    _charreco.FullClassify(n.r.strokes[0],
                        new Recognition(sks, "+/-", sks.GetBoundingBox().Bottom, (sks.GetBoundingBox().Top + sks.GetBoundingBox().Bottom) / 2, false));
                    throw new Reparse1Exception("reinterpret + - as +/-");
                }
                return Res.Sib;
            }
            return Res.Unused;
        }
    }
    public class ParenSym : Symbol {
        public List<Line> lines = new List<Line>();
        public Line curLine = new Line();
        public List<List<Line>> rows = new List<List<Line>>();
        private bool closed = false;
        public int ArrowID = -1; // -1 for not having an arrow
        public String matrixOp = null; // to hold matrix operations

        public bool Closed {
            get { return closed; }
            set { closed = value; }
        }
        private Symbol closing = null;
        public Symbol Closing { get { return closing; } set { closing = value; } }
        private Symbol parse2closing;
        public Symbol Parse2Closing { get { return parse2closing; } set { parse2closing = value; } }

        public bool OneRow { get { return (rows.Count == 1 && rows[0].Count == 1) || rows.Count == 0; } }
        override public void Offset(int x, int y) {
            foreach (Line l in lines)
                foreach (Symbol sm in l._syms)
                    sm.Offset(x, y);
            base.Offset(x, y);
        }
        public ParenSym(FeaturePointDetector charreco, Recognition rec) : base(charreco, rec) { }
        override public Rectangle TotalBounds {
            get {
                Rectangle bds = base.TotalBounds;
                if (lines.Count != 0) {
                    foreach (Line l in lines)
                        bds = Rectangle.Union(l.Bounds(), bds);
                }
                return bds;
            }
        }
        override public string Print() {
            string res = Sym.Character.ToString();
            for (int r = 0; r < rows.Count; r++)
                for (int c = 0; c < rows[r].Count; c++)
                    res += "[" + r + "," + c + "]:" + rows[r][c].Print() + " ";
            return res;
        }
        public override Res Uses(Symbol n, Symbol sibling, SymTest test, bool inside, bool allowSupers) {
            if (n.Sym != ')')
                return Res.Unused;
            if (test == SymTest.TestMods)
                return Res.Unused;
            if (closed)
                return base.Uses(n, sibling, test, inside, true);
            Rectangle region = Bounds;//bounding box of left paren
            region = Rectangle.Union(region, this.TotalBounds);
            if ((region.Bottom - n.StrokeBounds.Top + 0.0) / region.Height < -0.2 ||
                (n.StrokeBounds.Bottom - region.Top + 0.0) / region.Height < -0.2)
                return Res.Unused;
            if (n.Sym.Character == ')' || (n.Sym.Character == ',' && ((n.Bounds.Height + 0.0) / Bounds.Height > 0.6))) {
                closed = true;
                if (n.Sym.Character == ',' && n.r.levelsetby >= 1) {
                    n.r.addorsetalt(')', n.r.baseline, (n.Bounds.Top + n.Bounds.Bottom) / 2);
                    n.r.levelsetby = 1;
                }
                parse2closing = null;
                return Res.Sib;
            }
            foreach (Line l in lines)
                if (l.Uses(n, inside, true))
                    return Res.Used;
            Line nline = new Line();//CJ: n is not used in lines, hence a new line is needed for it
            lines.Add(nline);
            nline._syms.Add(n);
            List<Line> newRow = new List<Line>();
            for (int r = 0; r < rows.Count; r++) {
                Rectangle rbounds = rows[r][0].Bounds();
                for (int i = 1; i < rows[r].Count; i++)
                    rbounds = Rectangle.Union(rbounds, rows[r][i].Bounds());
                if (rbounds.Top > n.StrokeBounds.Bottom) {// what if outside paren range?
                    newRow.Add(nline);
                    rows.Insert(r, newRow);
                    return Res.Used;
                }
                if (n.Bounds.Top < rbounds.Bottom && n.Bounds.Bottom > rbounds.Top) {
                    for (int i = rows[r].Count - 1; i >= 0; i--)
                        if (n.Bounds.Left < rows[r][i].Bounds().Right) {
                            rows[r].Insert(i + 1, nline);
                            return Res.Used;
                        }
                    rows[r].Add(nline);
                    return Res.Used;
                }
            }
            newRow.Add(nline);
            rows.Add(newRow);
            return Res.Used;
        }
        override public IEnumerable<Symbol> ChildSyms {
            get {
                foreach (Line l in lines) foreach (Symbol s in l._syms) yield return s;
                if (closing != null) yield return closing;
            }
        }
    }
    public class IntSym : Symbol {
        public Line Integrand = new Line();
        override public void Offset(int x, int y) {
            foreach (Symbol sm in Integrand._syms)
                sm.Offset(x, y);
            base.Offset(x, y);
        }
        public IntSym(FeaturePointDetector charreco, Recognition rec) : base(charreco, rec) { }
        override public Rectangle TotalBounds {
            get {
                if (!Integrand.Empty)
                    return Rectangle.Union(Integrand.Bounds(), base.TotalBounds);
                return base.TotalBounds;
            }
        }
        override public string Print() {
            string res = Sym.Character.ToString();
            if (!Super.Empty)
                res += "^[" + Super.Print() + "]";
            if (!Sub.Empty)
                res += "_[" + Sub.Print() + "]";
            if (!Integrand.Empty)
                res += Integrand.Print();
            return res;
        }
        public override Res Uses(Symbol n, Symbol sibling, SymTest test, bool inside, bool allowSupers) {
            if (sibling != null && !(n.Bounds.Left < sibling.Bounds.Right && sibling.Sym == ','))
                return Res.Unused;
            if (n.Sym.Character == Sym.Character)
                return Res.Sib;
            Rectangle sbounds = StrokeBounds;
            if (sibling == null && n.Bounds.Left - TotalBounds.Right > 2 * Math.Max(sbounds.Width, Math.Max(n.Bounds.Height, n.Bounds.Width)))
                return Res.Unused;
            int subTop = (int)(sbounds.Bottom - sbounds.Height * .2);
            int subRight = sbounds.Right;
            if (!Sub.Empty) {
                subTop = Sub.Bounds().Top;
                subRight = Sub.Bounds().Right;
            }
            int supBot = (int)(sbounds.Top + sbounds.Height * .2);
            int supRight = sbounds.Right;
            if (!Super.Empty) {
                supBot = Super.Bounds().Bottom;
                supRight = Super.Bounds().Right;
            }
            /*
             bool sibFirst = ((n.Bounds.Bottom - subTop + 0.0) / n.Bounds.Height < 0.35 && (supBot - n.Bounds.Top + 0.0) / n.Bounds.Height < 0.20) ||
                 (n.Bounds.Left - Math.Max(subRight, supRight) + 0.0) / Math.Max(n.Bounds.Width, sbounds.Width) > 1;
             if (Integrand.Empty && (n.TotalBounds.Top > (2 * Bounds.Bottom + Bounds.Top) / 3 || n.TotalBounds.Bottom < (Bounds.Bottom + 2 * Bounds.Top) / 3) &&
                 sibFirst && (n.Bounds.Bottom < (3 * Bounds.Top + Bounds.Bottom) / 4 || n.Bounds.Top > (Bounds.Top + 3 * Bounds.Bottom) / 4))
                 sibFirst = false;
             int superTag = n.r.SuperId;
             int subTag = n.r.SubId;
             int superIntTag = n.r.SuperIntegralId;
             int subIntTag = n.r.SubIntegralId;
             if (((superTag == -1 && subTag == -1 && superIntTag == -1 && subIntTag == -1 && !Integrand.Empty) || sibFirst) && Integrand.Uses(n, false, true))
                 return Res.Used;
             else if (((Integrand.Empty || Integrand.Bounds().Top > n.Bounds.Bottom) && n.TotalBounds.Bottom < (Bounds.Bottom + Bounds.Top) / 2) && super(n)) return Res.Used;
             else if (((Integrand.Empty || Integrand.Bounds().Bottom < n.Bounds.Top) && n.TotalBounds.Top > (Bounds.Bottom + Bounds.Top) / 2) && sub(n)) return Res.Used;
             else if (!sibFirst && Integrand.Uses(n, false, true)) return Res.Used;
             return Res.Unused;
             //*/
             ///*

            // n.Bounds of '.' has been changed in checkLookahead
            // n.r.SuperID, etc == -1 if this method is called
            bool sibFirst = ((n.Bounds.Bottom - subTop + 0.0) / n.Bounds.Height < 0.35 && (supBot - n.Bounds.Top + 0.0) / n.Bounds.Height < 0.20) || 
                n.Bounds.Left - Math.Max(subRight, supRight) < 2*Math.Max(n.Bounds.Width, sbounds.Width);
            //even if Integrand is not empty, Integrand.Empty is true when some super/subscript (such as a mis-recognized dot) on the left of the first integrand is tested here:
            if (Integrand.Empty && sibFirst &&
                (n.TotalBounds.Top > (2 * sbounds.Bottom + sbounds.Top) / 3 || n.TotalBounds.Bottom < (sbounds.Bottom + 2 * sbounds.Top) / 3))
                sibFirst = false;
            if ((!Integrand.Empty || sibFirst) && Integrand.Uses(n, false, true)) return Res.Used;
            else if (((Integrand.Empty || n.Bounds.Bottom < Integrand.Bounds().Top) && n.TotalBounds.Bottom < (sbounds.Bottom + sbounds.Top) / 2) && super(n)) return Res.Used;
            else if (((Integrand.Empty || n.Bounds.Top > Integrand.Bounds().Bottom) && n.TotalBounds.Top > (sbounds.Bottom + sbounds.Top) / 2) && sub(n)) return Res.Used;
            
            return Res.Unused;
          //*/    
        }
        override public IEnumerable<Symbol> ChildSyms {
            get {
                foreach (Symbol s in Super._syms) yield return s;
                foreach (Symbol s in Sub._syms) yield return s;
                foreach (Symbol s in Integrand._syms) yield return s;
            }
        }
    }
    public class Parser : IDisposable {
        public FeaturePointDetector Charreco { get; private set; }
        private Ink _ink;
        private void AlternateChangingHandler(Recognition r, int oldalt) {
            /* **NB: the recognition passed in might be for a different Parser etc, so check if Ink is the same */
            if(r.strokes.Ink != _ink) return;
            if (r.alts[oldalt] == Unicode.I.INTEGRAL && integralList.Count > 0) {
                foreach (Recognition ir in integralList)
                    if (ir.strokes[0].Id == r.strokes[0].Id) {
                        if (ir.strokes[0].ExtendedProperties.Contains(TempGuid))
                            ir.strokes[0].ExtendedProperties.Remove(TempGuid);
                        integralList.Remove(ir);
                        break;
                    }
            } else if (r.alts[oldalt] == Unicode.N.N_ARY_SUMMATION && sumList.Count > 0) {
                foreach (Recognition sr in sumList)
                    if (sr.strokes[0].Id == r.strokes[0].Id) {
                        if (sr.strokes[0].ExtendedProperties.Contains(TempGuid))
                            sr.strokes[0].ExtendedProperties.Remove(TempGuid);
                        sumList.Remove(sr);
                        break;
                    }

            } else if (r.alts[oldalt] == '(' && Symbol.ParseParensToMatrices && opSyms.Count > 0) {
                foreach (ParenSym os in opSyms)
                    if (os.r.strokes[0].Id == r.strokes[0].Id) {
                        opSyms.Remove(os);
                        break;
                    }
            } 
        }
        public Parser(FeaturePointDetector charreco, Ink ink) {
            Charreco = charreco;
            _ink = ink;
            Recognition.AlternateChangingEvent += AlternateChangingHandler;
        }
        private bool _disposed = false;
        public void Dispose() {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SuppressFinalize to
            // take this object off the finalization queue
            // and prevent finalization (destructor) code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        private void Dispose(bool disposing) {
            if(!_disposed) {
                if(disposing) {
                    // Dispose managed resources.
                    Recognition.AlternateChangingEvent -= AlternateChangingHandler;
                }

                // Dispose unmanaged resources
                // But there are none for this class, so no code here.

                _disposed = true;
            }
        }
        // This destructor will run only if the Dispose method does not get called.
        // Do not provide destructors in types derived from this class.
        ~Parser() {
            Dispose(false);
        }
        [Serializable()]
        public class ParseResult {
            public Rectangle bounds;
            [NonSerialized()]
            public Line root;
            public Expr expr;
            public bool parseError;
            public Expr finalSimp = null;
            public Expr matrixOperationResult = null;
            public bool isNum = false;
            [NonSerialized()]
            public Strokes Strokes;
            private Hashtable _data = new Hashtable(); public Hashtable Data { get { return _data; } }
            public ParseResult(Strokes s, Line n) { bounds = s.GetBoundingBox(); Strokes = s; root = n; expr = null; }
            public ParseResult(Line n) { bounds = Rectangle.Empty; Strokes = null; root = n; expr = null; }
            public ParseResult(Expr e, Rectangle b) { bounds = b; Strokes = null; root = null; expr = e; }

           // public Rectangle Bounds { set {bounds = value;} }
        };
        public class Range {
            private Rectangle _bounds;
            Guid _id = Guid.NewGuid();
            static public Guid RangeId = new Guid("{26B4EA44-4653-4b41-A56F-4C2B754A2064}");

            public void resetID() {
                Hashtable guids = new Hashtable();
                List<Stroke> toDelete = new List<Stroke>();
                foreach (Stroke s in Strokes)
                    if (s.Deleted)
                        toDelete.Add(s);
                foreach (Stroke s in toDelete)
                    Strokes.Remove(s);
                foreach (Stroke s in Strokes) {
                    if (s.ExtendedProperties.Contains(RangeId)) {
                        Guid strokeRangeId = new Guid((byte[])(s.ExtendedProperties[RangeId].Data));
                        if (guids.Contains(strokeRangeId))
                            guids[strokeRangeId] = (int)guids[strokeRangeId] + 1;
                        else guids.Add(strokeRangeId, 1);
                    }
                }
                int maxguids = 0;
                Guid theGuid = Guid.NewGuid();
                foreach (DictionaryEntry pair in guids) {
                    if ((int)pair.Value > maxguids) {
                        maxguids = (int)pair.Value;
                        theGuid = (Guid)pair.Key;
                    }
                }
                foreach (Stroke s in Strokes)
                    s.ExtendedProperties.Add(RangeId, theGuid.ToByteArray());
                ID = theGuid;
            }
            public Guid ID { get { return _id; } set { _id = value; } } 
            public Rectangle Bounds { get { return _bounds; } }
            public starPadSDK.Geom.Rct RBounds { get { return new starPadSDK.Geom.Rct(_bounds.Left / 100, _bounds.Top / 100, _bounds.Right / 100, _bounds.Bottom / 100); } }
            internal void Add(Range r) {
                Strokes.Add(r.Strokes);
                Divisions.Add(r.Divisions);
                Grabby.Add(r.Grabby);
                _bounds = Rectangle.Union(_bounds, r._bounds);
            }
            internal void Add(Recognition r, bool grabby, bool div) {
                if (grabby) {
                    Grabby.Add(r.strokes);
                    updateGrabby(ref _bounds, r, r.bbox);
                }
                if (div) {
                    Divisions.Add(r.strokes);
                    updateDivision(ref _bounds, r, r.bbox);
                }
                Strokes.Add(r.strokes);
                _bounds = Rectangle.Union(_bounds, r.bbox);
            }
            internal void UpdateBounds() {
                Rectangle bounds = Strokes.GetBoundingBox();
                foreach (Stroke d in Divisions)
                    if (!d.Deleted) {
                        Recognition r = _charreco.Classification(d);
                        if (r != null)
                            updateDivision(ref bounds, r, r.bbox);
                    }
                foreach (Stroke g in Grabby)
                    if (!g.Deleted) {
                        Recognition r = _charreco.Classification(g);
                        if (r != null)
                            updateGrabby(ref bounds, r, r.bbox);
                    }
                _bounds = bounds;
            }

            private static void updateGrabby(ref Rectangle bounds, Recognition r, Rectangle gbox) {
                if (r.alt == Unicode.N.N_ARY_SUMMATION) {
                    gbox.Offset(gbox.Width/5, 0);
                    gbox.Inflate(gbox.Width/5, 0);
                } else {
                    bool horiz = gbox.Width/gbox.Height > 3;
                    int dim = (horiz ? gbox.Width : Math.Max(gbox.Width, gbox.Height));
                    gbox.Offset(2 * dim / 3, 0);
                    gbox.Inflate(dim * 5 / 3, 0);
                }
                bounds = Rectangle.Union(bounds, gbox);
            }

            private void updateDivision(ref Rectangle bounds, Recognition r, Rectangle dbox) {
                bool vertical = (r != null && (r.alt == Unicode.I.INTEGRAL || r.alt == Unicode.N.N_ARY_SUMMATION));
                int padding = vertical ? (int)(dbox.Height * (3 / (2 + dbox.Height / _charreco.InkPixel / 200))) : 
                                                dbox.Width;
                double dratio = 0.3;
                double uratio = 0.3;
                if (dbox.Width > 26 * 100) {
                    Point[] hull = new Point[] { new Point(dbox.Left, dbox.Bottom+1), new Point(dbox.Right,dbox.Bottom+1),
                                                             new Point(dbox.Right,(int)(dbox.Bottom+dbox.Width*.1+1)),new Point(dbox.Left,(int)(dbox.Bottom+dbox.Width*.1+1))};
                    Strokes hit = r.strokes.Ink.HitTest(hull, 1);
                    if (hit.Count > 0)
                        dratio = 0.1;
                    hull = new Point[] { new Point(dbox.Left, (int)(dbox.Top-dbox.Width*.1-1)), new Point(dbox.Right,(int)(dbox.Top-dbox.Width*.1-1)),
                                                             new Point(dbox.Right,dbox.Top-1),new Point(dbox.Left,dbox.Top-1)};
                    hit = r.strokes.Ink.HitTest(hull, 1);
                    if (hit.Count > 0)
                        uratio = 0.1;
                } else if (dbox.Width < _charreco.InkPixel * 20 && dbox.Height < _charreco.InkPixel * 20) {
                    int size = (int)(_charreco.InkPixel * 20);
                    dbox = new Rectangle(dbox.Left - size / 2, dbox.Top - size / 2, dbox.Width + size, dbox.Height + size);
                }
                bounds = Rectangle.Union(bounds,
                    new Rectangle(dbox.Left, dbox.Top - (int)(uratio * padding),
                                  dbox.Width + (vertical ? dbox.Height/2:0), dbox.Height + (int)((uratio + dratio) * padding)));
            }
            public Strokes Divisions { get; private set; }
            public Strokes Strokes { get; private set; }
            public Strokes Grabby { get; private set; }
            private FeaturePointDetector _charreco;
            internal Range(FeaturePointDetector charreco, Strokes ss, Strokes divisions, Strokes grabby) {
                _charreco = charreco;
                Strokes = ss; Divisions = divisions; Grabby = grabby;
                hasArrowInMatrixEntry = false;
                UpdateBounds();
            }
            public Range(ParseResult result) { Parse = result; }
            public bool hasArrowInMatrixEntry { get; internal set; }
            public ParseResult Parse { get; internal set; }
        }
        //for (orginally for NIST) logging
        /// <summary>
        /// This is non-null whenever logging should be done.
        /// </summary>
        private StreamWriter log0 = null;
        public bool Logging { get { return log0 != null; } }

        public void StartNISTLog() {
            if(log0 == null) {
                DateTime startTime = DateTime.Now;
                string dirname = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AlgoSketch\\Logs");
                if(!Directory.Exists(dirname)) Directory.CreateDirectory(dirname);
                string filename = Path.Combine(dirname,
                    "Log_" + startTime.Month + "_" + startTime.Day + "_" + startTime.Year +
                    "_" + startTime.Hour + "_" + startTime.Minute + "_" + startTime.Second);

                string logfile0 = filename + ".log";
                string logfile1 = filename + ".inklog";

                log0 = File.AppendText(logfile0);
                LogTimeLn(startTime, " -- MathPaper starts");
                /*byte[] isf = loggingInk.Save(PersistenceFormat.InkSerializedFormat);
                using (Parser.log1 = new FileStream(logfile1, FileMode.Append, FileAccess.Write)) {
                    Parser.log1.Write(isf, 0, isf.Length);
                }*/
                log1 = new FileStream(logfile1, FileMode.Create, FileAccess.Write);
            }
        }
        public void CloseNISTLog() {
            if(log0 != null) {
                log0.Close();
                log0 = null;
                log1.Close();
                log1 = null;
            }
        }
        public void LogTime(DateTime dt) {
            if(log0 != null) log0.Write(dt.ToShortDateString() + " " + dt.TimeOfDay);
        }
        public void LogTime(DateTime dt, string s) {
            if(log0 == null) return;
            LogTime(dt);
            log0.Write(s);
        }
        public void LogTimeLn(DateTime dt, string s) {
            if (log0 == null) return;
            LogTime(dt, s);
            LogStringLn();
        }
        public void LogTimeLn(DateTime dt) {
            if(log0 == null) return;
            LogTime(dt);
            LogStringLn();
        }
        public void LogString(string s) {
            if(log0 == null) return;
            log0.Write(s);
        }
        public void LogStringLn() {
            log0.WriteLine();
            log0.Flush();
        }
        public void LogStringLn(string s) {
            if(log0 == null) return;
            LogString(s);
            LogStringLn();
        }

        private FileStream log1 = null;
        [Serializable]
        public struct StrokeLogEntry {
            public DateTime dt;
            public int id;
            public byte[] isf;
            public StrokeLogEntry(DateTime _dt, int _id, byte[] _isf) {
                dt = _dt;
                id = _id;
                isf = _isf;
            }
        }
        private BinaryFormatter strokeLogBF = new BinaryFormatter();
        public void LogStroke(DateTime dt, Stroke s) {
            if(log1 == null) return;
            Ink loggingInk = new Ink();
            loggingInk.AddStrokesAtRectangle(s.Ink.CreateStrokes(new int[] { s.Id }), s.GetBoundingBox());
            byte[] isf = loggingInk.Save(PersistenceFormat.InkSerializedFormat);
            StrokeLogEntry sle = new StrokeLogEntry(dt, s.Id, isf);
            strokeLogBF.Serialize(log1, sle);
        }

        public SortedList sortedRecs = null;
        public Point traceFrom = new Point(-1, -1);
        public int funcDefLine = -1;        
        public List<Range> Ranges = new List<Range>();
        public Dictionary<Recognition, Rectangle> RangeGrabHack = new Dictionary<Recognition, Rectangle>();
        private bool RemoveStrokes(Strokes toRemove, Range rg, ref List<Range> emptyRanges) {
            int icount = rg.Strokes.Count;
            Rectangle orect = rg.Bounds;
            rg.Strokes.Remove(toRemove);
            rg.Divisions.Remove(toRemove);
            rg.Grabby.Remove(toRemove);
            rg.UpdateBounds();
            if (icount != rg.Strokes.Count) {
                rg.Parse = null;
            }
            if (rg.Strokes.Count == 0)
                emptyRanges.Add(rg);
            return (icount != rg.Strokes.Count);
        }

        private int UpdateRanges(Strokes update, Strokes strokes) {
            return UpdateRanges(update, strokes, -1, false);
        }
        
        private int UpdateRanges(Strokes update, Strokes strokes, int parenID, bool matrixParsing) {
            int inkPixel = (int)Charreco.InkPixel;
            if (update == null || Ranges == null) {
                Ranges = new List<Range>();
                RangeGrabHack = new Dictionary<Recognition, Rectangle>();
            }
            foreach (Stroke s in strokes) { // bcz: needed to make sure that opSyms was complete to allow range merging when writing ')'
                Recognition r = Charreco.Classification(s);
                if (r != null && r.alt == '←') funcDefLine = 0; //Assuming at least one of function definition and function return is completed. will be replaced with real funcDefLine value in ParseAlgorithm()
                // Pressing the Parse button, which calls Parse(null, false, true) in Form1, and loading 
                // inks from files both call UpdateRanges, which needs to update left and right bounds for open
                // and closing parentheses. Hence capture opSyms here. It is updated in inParse() for alternates.
                // OpSyms.Closing is updated in collectMatrixStrokes(..)
                if (r != null && r.alt == '(' && Symbol.ParseParensToMatrices /*&& !Parser.opSyms.Contains((ParenSym)Symbol.From(r))*/)
                    if (opSyms.Count == 0) { 
                        opSyms.Add((ParenSym)Symbol.From(Charreco, r));
                    } else {
                        bool exist = false;
                        foreach (ParenSym os in opSyms)
                            if (os.r.strokes[0].Id == r.strokes[0].Id) {
                                exist = true;
                                break;
                            }
                        if (!exist) {
                            opSyms.Add((ParenSym)Symbol.From(Charreco, r));
                        }
                    }
            }
            List<Range> theRanges = null;
            if (parenID == -1) theRanges = Ranges;
            else theRanges = matrixRanges[parenID];//update of theRanges will update matrixRanges[parenID] too

            Dictionary<Recognition, bool> recogs = new Dictionary<Recognition, bool>();
            int rangeCount = 0;
            List<Range> emptyRanges = new List<Range>();
            int emptyrangecount = emptyRanges.Count;
            
            //Set traceFrom for parseAlgorithm()
            foreach (Stroke s in (update == null ? strokes : update)) {
                Recognition r = Charreco.Classification(s);        
                if (r != null && !s.Deleted) {                    
                    if (funcDefLine !=-1 && "↗↘→↓↑".Contains(r.alt.Character.ToString())){
                        if(!s.ExtendedProperties.Contains(Charreco.IgnoreGuid)) {
                            Charreco.Ignorable.Add(s);
                            s.ExtendedProperties.Add(Charreco.IgnoreGuid, s.Id);
                        }
                        traceFrom = s.GetPoint(0);
                    }
                    else recogs[r] = true;
                }
                else if (r == null && !s.Deleted) {  //for a loaded file
                    r = Charreco.FullClassify(s);
                    if (funcDefLine != -1 && "↗↘→↓↑".Contains(r.alt.Character.ToString())) {
                        if(!s.ExtendedProperties.Contains(Charreco.IgnoreGuid)) {
                            Charreco.Ignorable.Add(s);
                            s.ExtendedProperties.Add(Charreco.IgnoreGuid, s.Id);
                        }
                        traceFrom = s.GetPoint(0);
                    }               
                }
            }

            foreach (Recognition r in recogs.Keys) {
                if (r.alt == '↘') continue;
                foreach (Range rg in theRanges) {
                    RemoveStrokes(r.strokes, rg, ref emptyRanges); // a multi-stroke character "removes" the previous stroke
                }
                // but we don't have to "move" this range because we know
                // that the range will get "moved" when the stroke is added
            }
            if (emptyRanges.Count > emptyrangecount) {
                for (int i = emptyRanges.Count - 1; i >= emptyrangecount; i--)
                    theRanges.Remove(emptyRanges[i]);
                emptyRanges.RemoveRange(emptyrangecount, emptyRanges.Count - emptyrangecount);
            }
            if (parenID == -1)
                foreach (Range ne in theRanges)
                    if (ne.Parse == null) {
                        emptyRanges.Add(ne);
                        foreach (Stroke s in ne.Strokes) {
                            Recognition r = Charreco.Classification(s);
                            if (r != null)
                                recogs[r] = true;
                        }
                    }
            if (update != null && update.Count > 0 && update[0].Deleted)
                foreach (Range rg in theRanges)
                    if (RemoveStrokes(update, rg, ref emptyRanges)) {
                        if (!emptyRanges.Contains(rg)) {
                            emptyRanges.Add(rg);
                            foreach (Stroke s in rg.Strokes) {
                                Recognition r = Charreco.Classification(s);
                                if (r != null)
                                    recogs[r] = true;
                            }
                        }
                    }

            int minRange = 0;
            foreach (Range r in emptyRanges)
                theRanges.Remove(r);
            if (emptyRanges.Count > 0)
                minRange = theRanges.Count;
            int startRangeCount = theRanges.Count; 
            List<Recognition> recs = new List<Recognition>();
            foreach (Recognition r in recogs.Keys)
                recs.Add(r);

            //for ellipses in vectors, this needs to be skipped
            foreach (Recognition r in recs)
                if (parenID != -1 && (r.alt == '⋯' || r.alt == '⋮' || r.alt == '⋰' || r.alt == '⋱' /*|| r.alt == '↘'*/)) {
                    recs.Remove(r);
                    strokes.Remove(r.strokes);
                    Strokes ss = r.strokes.Ink.CreateStrokes();
                    ss.Add(r.strokes);
                    Range newRange = new Range(Charreco, ss, // a new strokes collection is needed here
                                            r.strokes.Ink.CreateStrokes(), r.strokes.Ink.CreateStrokes());
                    //if (r.alt == '↘') newRange.hasArrowInMatrixEntry = true;
                    theRanges.Add(newRange);
                    rangeCount = 1;
                    break;
                }
            foreach (Recognition r in recs) {
                bool grabby;
                bool divs;
                Rectangle b = ComputeStrokeRangeData(parenID, matrixParsing, inkPixel, r, out divs, out grabby);

                List<Range> rangesHit = new List<Range>();
                Strokes divsincluded = strokes.Ink.CreateStrokes();
                Strokes grabbyincluded = strokes.Ink.CreateStrokes();
                Strokes strokesincluded = strokes.Ink.CreateStrokes();
                strokesincluded.Add(r.strokes);
                //if (grabby) grabbyincluded.Add(r.strokes);
                if (grabby && (!matrixParsing || r.alt.Character != '+')) grabbyincluded.Add(r.strokes);
                if (divs) divsincluded.Add(r.strokes);
                //find ranges which intersect directly with b
                b = FindIntersectedRanges(parenID, matrixParsing, theRanges, minRange, r, b, rangesHit, strokesincluded, divsincluded, grabbyincluded);

                Range newRange = rangesHit.Count == 0 ? new Range(Charreco, strokesincluded, divsincluded, grabbyincluded) : null;
                // put together all hit ranges for a new range
                foreach (Range rg in rangesHit) {
                    //if (theRanges.IndexOf(rg) < startRangeCount)
                    //    startRangeCount = theRanges.IndexOf(rg);
                    if (newRange == null) {
                        newRange = rg;
                        newRange.Add(r, grabby, divs);
                    } else {
                        newRange.Add(rg);
                    }
                    if (rg.hasArrowInMatrixEntry) newRange.hasArrowInMatrixEntry = true;
                    theRanges.Remove(rg);
                }
                rangesHit.Clear();
                if (newRange!=null && r.alt == '↘') newRange.hasArrowInMatrixEntry = true;

                // if the remaining unhit ranges intersect with the new range, collect them too
                //if(!newRange.hasArrowInMatrixEntry) // the Arrow entry would not have more than 5 characters, and subsequent hit is not supposed to happen.
                if (!matrixParsing && !newRange.hasArrowInMatrixEntry)
                foreach (Range rg in theRanges) {
                    if (rg.hasArrowInMatrixEntry) continue;
                    Recognition rr = Charreco.Classification(rg.Strokes[0]);
                    if (matrixParsing && (rr.alt == '⋯' || rr.alt == '⋮' || rr.alt == '⋰' || rr.alt == '⋱')) continue;

                    Rectangle hitrect = newRange.Bounds;
                    if(!matrixParsing) hitrect.Inflate(new Size((int)(Charreco.InkPixel * 8), (int)(Charreco.InkPixel * 8)));
                    else hitrect.Inflate(new Size((int)(Charreco.InkPixel * 4), (int)(Charreco.InkPixel * 4))); // is it good to inflate it?
                    if (rg.Bounds.IntersectsWith(hitrect)) {
                        rangesHit.Add(rg);
                        newRange.Add(rg);
                    }
                }
                foreach (Range rg in rangesHit) {
                    theRanges.Remove(rg);
                }               
                theRanges.Add(newRange);
                
                if (startRangeCount==0) rangeCount = theRanges.Count;
                else if (theRanges.Count == startRangeCount) rangeCount = 1;
                else rangeCount = theRanges.Count - startRangeCount;
            }// end of foreach(Recognition r in recs) 
            return rangeCount;
        }


        private Rectangle FindIntersectedRanges(int parenID, bool matrixParsing, List<Range> theRanges, int minRange, Recognition r, Rectangle b, List<Range> rangesHit, Strokes strokesincluded, Strokes divsincluded, Strokes grabbyincluded) {
            if (parenID != -1 /*&& r.alt != '↘'*/)                 
                for (int k = minRange; k < theRanges.Count; k++) {
                    if (!theRanges[k].hasArrowInMatrixEntry) continue;
                    Rectangle arrowBox = Rectangle.Empty;
                    for (int kk = 0; kk < theRanges[k].Strokes.Count; kk++) {
                        Recognition rr = Charreco.Classification(theRanges[k].Strokes[kk]);
                        if (rr.alt == '↘') {
                            arrowBox = rr.strokes.GetBoundingBox();
                            break;
                        }
                    }           //Search inkPanel_Paint for handling arrow range         
                    /*Point[] pts = new Point[] { arrowBox.Location, new Point((arrowBox.Left+arrowBox.Right)/2, arrowBox.Top),
                            new Point(arrowBox.Right, (arrowBox.Top+arrowBox.Bottom)/2), new Point(arrowBox.Right, arrowBox.Bottom), 
                            new Point((arrowBox.Left+arrowBox.Right)/2, arrowBox.Bottom),new Point(arrowBox.Left, (arrowBox.Top+arrowBox.Bottom)/2),
                            arrowBox.Location };
                         * */
                    Point[] pts = new Point[] { arrowBox.Location, new Point(arrowBox.Right, arrowBox.Top),
                            new Point(arrowBox.Right, arrowBox.Bottom), new Point(arrowBox.Left, arrowBox.Bottom),
                            arrowBox.Location };
                    if (r.strokes.Ink.HitTest(pts, 1).Contains(r.strokes[0])) {// further testing to remove intersecting strokes at ends of arrow might be needed here
                        rangesHit.Add(theRanges[k]);
                        strokesincluded.Add(theRanges[k].Strokes);
                        divsincluded.Add(theRanges[k].Divisions);
                        grabbyincluded.Add(theRanges[k].Grabby);
                    }
                    else { break; }
                    return b;
                }
                        
            Rectangle bexpand = r.strokes.GetBoundingBox();
            if(!matrixParsing && r.alt != '↘') bexpand.Inflate((int)(Charreco.InkPixel * 8), (int)(Charreco.InkPixel * 8));
            else if(r.alt == Recognition.Result.Special.Division) bexpand.Inflate((int)(Charreco.InkPixel * 4), (int)(Charreco.InkPixel * 8));
            else if(r.alt != '↘') bexpand.Inflate((int)(Charreco.InkPixel * 4), (int)(Charreco.InkPixel * 4));
            bexpand = Rectangle.Union(bexpand, b);
            for (int k = minRange; k < theRanges.Count; k++) {
                if (parenID != -1 && theRanges[k].hasArrowInMatrixEntry) continue;
                if (parenID != -1 && theRanges[k].Strokes.Count != 0) {
                    Recognition rr = Charreco.Classification(theRanges[k].Strokes[0]);
                    if (rr.alt == '⋯' || rr.alt == '⋮' || rr.alt == '⋰' || rr.alt == '⋱') continue;
                }
                /*Rectangle rangebounds = theRanges[k].Bounds;
                if ( r.allograph == ".") {
                    int maxwidth = 0;
                    int maxheight = 0;
                    foreach (Stroke s in theRanges[k].Strokes) {
                        if (!theRanges[k].Divisions.Contains(s)) {
                            maxwidth = Math.Max(maxwidth, s.GetBoundingBox().Width);
                            maxheight = Math.Max(maxheight, s.GetBoundingBox().Height);
                        }
                    }
                    rangebounds.Inflate((int)Math.Min(50 * FeaturePointDetector.InkPixel, maxwidth * 2),
                        (int)Math.Min(50 * FeaturePointDetector.InkPixel, Math.Min(maxheight, maxwidth) * 0.5));
                }*/
                bool divs = false; bool grabby = false;
                bool intersected = Intersect(theRanges[k], bexpand, r, parenID, matrixParsing, out divs, out grabby);
                if (parenID !=-1) {
                    if (r.alt == '↘') {
                        int hitCounts = 0;
                        for (int kk = 0; kk < theRanges[k].Strokes.Count; kk++) 
                            if (r.strokes.Ink.HitTest(r.strokes.GetBoundingBox(), 1).Contains(theRanges[k].Strokes[kk])) 
                                hitCounts ++;
                        if (hitCounts > 0){
                            if (hitCounts == theRanges[k].Strokes.Count) {
                                rangesHit.Add(theRanges[k]);
                                strokesincluded.Add(theRanges[k].Strokes);
                                divsincluded.Add(theRanges[k].Divisions);
                                grabbyincluded.Add(theRanges[k].Grabby);
                            }
                            else
                                UpdateRanges(theRanges[k].Strokes, theRanges[k].Strokes, parenID, matrixParsing);//placeholder only
                        }                           
                    }
                    else if (intersected) {
                        rangesHit.Add(theRanges[k]);
                        strokesincluded.Add(theRanges[k].Strokes);
                        divsincluded.Add(theRanges[k].Divisions);
                        grabbyincluded.Add(theRanges[k].Grabby);
                    }
                }
                else if (intersected) {
                    rangesHit.Add(theRanges[k]);
                    strokesincluded.Add(theRanges[k].Strokes);
                    divsincluded.Add(theRanges[k].Divisions);
                    grabbyincluded.Add(theRanges[k].Grabby);
                }
            }
            return b;
        }

        //two-way intersection test
        private bool Intersect(Range range, Rectangle bexpand, Recognition r, int parenID, bool matrixParsing, out bool divs, out bool grabby) {
            divs = false;
            grabby = false;
            int inkPixel = (int)Charreco.InkPixel;
            Strokes ss = range.Strokes;
            Stroke ns = ss[0];
            int dist = 0;
            Rectangle bbox = r.strokes.GetBoundingBox();
            Rectangle newRect = range.Bounds;

            if (range.Bounds.IntersectsWith(bexpand)) return true;
            if (r.allograph == ".") {
                int maxwidth = 0;
                int maxheight = 0;
                foreach (Stroke s in ss) {
                    if (!range.Divisions.Contains(s)) {
                        maxwidth = Math.Max(maxwidth, s.GetBoundingBox().Width);
                        maxheight = Math.Max(maxheight, s.GetBoundingBox().Height);
                    }
                }
                newRect.Inflate((int)Math.Min(50 * Charreco.InkPixel, maxwidth * 2),
                     (int)Math.Min(50 * Charreco.InkPixel, Math.Min(maxheight, maxwidth) * 0.5));
                return newRect.IntersectsWith(bbox);
            }
            Rectangle aInflate = range.Bounds;
            aInflate.Inflate(bexpand.Width,bexpand.Height);
            if (!aInflate.IntersectsWith(bbox)) return false;

            
            int min = (ss[0].GetPoint(0).X-bbox.X)*(ss[0].GetPoint(0).X-bbox.X) + (ss[0].GetPoint(0).Y-bbox.Y)*(ss[0].GetPoint(0).Y-bbox.Y);
            foreach (Stroke s in ss) { 
                dist = (s.GetPoint(0).X-bbox.X)*(s.GetPoint(0).X-bbox.X) + (s.GetPoint(0).Y-bbox.Y)*(s.GetPoint(0).Y-bbox.Y);
                if (dist < min) {
                    min = dist;
                    ns = s;
                }
            }
            Recognition rr = Charreco.Classification(ns);
            if (rr.allograph == ".") {
                int maxwidth = 0;
                int maxheight = 0;
                foreach (Stroke s in ss) {
                    if (!range.Divisions.Contains(s)) {
                        maxwidth = Math.Max(maxwidth, s.GetBoundingBox().Width);
                        maxheight = Math.Max(maxheight, s.GetBoundingBox().Height);
                    }
                }
                newRect.Inflate((int)Math.Min(50 * Charreco.InkPixel, maxwidth * 2),
                     (int)Math.Min(50 * Charreco.InkPixel, Math.Min(maxheight, maxwidth) * 0.5));
                return newRect.IntersectsWith(bbox);
            }
            newRect = ComputeStrokeRangeData(parenID, matrixParsing, inkPixel, rr, out divs, out grabby);
            return newRect.IntersectsWith(bbox);
        }
        
        private Rectangle ComputeStrokeRangeData(int parenID, bool matrixParsing, int inkPixel, Recognition r, out bool divs, out bool grabby) {
            grabby = divs = false;
            if (!matrixParsing && ("+/=><" + Unicode.A.ALMOST_EQUAL_TO + Unicode.N.N_ARY_SUMMATION + Unicode.L.LESS_THAN_OR_EQUAL_TO + Unicode.A.APPROXIMATELY_EQUAL_TO +
                    Unicode.G.GREATER_THAN_OR_EQUAL_TO + Unicode.S.SUBSET_OF_OR_EQUAL_TO + Unicode.S.SUPERSET_OF_OR_EQUAL_TO).IndexOf(r.alt.Character) != -1)
                grabby = true;
            Rectangle b = r.bbox;
            if (parenID !=-1 && r.alt == '↘') 
                return b;
            int top = b.Top;
            int left = b.Left;
            int bottom = b.Bottom;
            int right = b.Right;
            int height = bottom - top;
            int width = right - left;
            int xhgt = r.baseline - r.xheight;
            bool r_as_rSQUARE_ROOT = false;
            
            if (r.alt == 'r') { 
                Point[] hull = new Point[] { new Point(left,top), new Point(right,top),new Point (right, bottom), new Point(left , bottom)};
                Strokes hit = r.strokes.Ink.HitTest(hull, 1);
                if (hit.Count > 3) r_as_rSQUARE_ROOT = true;
            }
          
            if (r_as_rSQUARE_ROOT ||r.alt == Unicode.N.N_ARY_SUMMATION || r.alt == Unicode.S.SQUARE_ROOT || r.alt == Unicode.I.INTEGRAL) {
                top = top - (int)(0.2 * height);
                bottom = bottom + (int)(0.2 * height);
                left -= Math.Max((int)(Charreco.InkPixel*15), Math.Min(height, (int)(Charreco.InkPixel*75)));
                right += Math.Max((int)(Charreco.InkPixel*15), Math.Min(height, (int)(Charreco.InkPixel*75)));
            }else if (r.alt == ',' && matrixParsing) {//for function call with more than one arguments
                int i = 0;
                for (i = 0; i < opSyms.Count; i++)
                    if (matrixLparenInds.ContainsKey(opSyms[i].r.strokes[0].Id) && 
                        (int)matrixLparenInds[opSyms[i].r.strokes[0].Id] == parenID) break;
                left = opSyms[i].Bounds.Left;
                if (opSyms[i].Closing != null)
                    right = Math.Max(right + (int)(Charreco.InkPixel * 100), opSyms[i].Closing.Bounds.Left);
                else right += (int)(Charreco.InkPixel * 100);
                top = top - (int)(Charreco.InkPixel * 20);
            }
            else if(r.alt != Unicode.M.MINUS_SIGN && width < Charreco.InkPixel * 15 && height < Charreco.InkPixel * 15 &&
              (width < Charreco.InkPixel * 10 || height < Charreco.InkPixel * 10)) {
//            else if (r.alt != Unicode.M.MINUS_SIGN && width < FeaturePointDetector.InkPixel * 10 && height < FeaturePointDetector.InkPixel * 10 &&
//              (width < FeaturePointDetector.InkPixel * 7 || height < FeaturePointDetector.InkPixel * 7)) {
                if (matrixParsing) {
                    int hd = (int)(Charreco.InkPixel * 20);
                    int vd = (int)(Charreco.InkPixel * 5);
                    Point[] hull = new Point[] { new Point(b.Left, b.Top - vd), new Point(b.Left - hd, b.Top - vd), 
                        new Point(b.Left - hd, b.Bottom + vd), new Point(b.Left, b.Bottom + vd) };
                    Strokes hit = r.strokes.Ink.HitTest(hull, 1);
                    if (hit.Count > 0) {
                        top = top - vd;
                        bottom = bottom + vd;
                        left -= hd;
                        right += hd;
                    }
                    else {
                        int size = (int)(Charreco.InkPixel * 10);
                        top = top - size / 2;
                        bottom = bottom + 2 * size;
                        left -= 2 * size;
                        right += 2 * size;
                    }     
                }
                else {
                    int size = (int)(Charreco.InkPixel * 20);
                    top = top - size / 2;
                    bottom = bottom + 2 * size;
                    left -= 2 * size;
                    right += 2 * size;
                }
            }
            else if (r.alt == Recognition.Result.Special.Division || r.alt == Unicode.M.MINUS_SIGN || r.allograph == "+" || r.allograph == "perp") {
                bool horiz = !(r.allograph == "+" || r.allograph == "perp");
                if (r.alt == Recognition.Result.Special.Division) {
                    //new top
                    int max = (int)(Charreco.InkPixel * 75);
                    int min = top - max;
                    Point[] hull = new Point[] { new Point(b.Left, b.Top), new Point(b.Right,b.Top),
                    new Point(b.Right, b.Top - max), new Point(b.Left, b.Top - max)};
                    Strokes hit = r.strokes.Ink.HitTest(hull, 1);
                    int k = 0, k0 = 0;
                    if (hit.Count > 0) {
                        for (k = 0; k < hit.Count; k++)
                            if (hit[k].GetBoundingBox().Bottom > min) {
                                min = hit[k].GetBoundingBox().Bottom;
                                k0 = k;
                            }
                        top = hit[k0].GetBoundingBox().Top;
                    }
                    //new bottom
                    min = bottom + max;
                    k0 = 0;
                    hull = new Point[] { new Point(b.Left, b.Bottom), new Point(b.Right,b.Bottom),
                    new Point(b.Right, b.Bottom + max),new Point(b.Left, b.Bottom + max)};
                    hit = r.strokes.Ink.HitTest(hull, 1);
                    if (hit.Count > 0) {
                        for (k = 0; k < hit.Count; k++)
                            if (hit[k].GetBoundingBox().Top < min) {
                                min = hit[k].GetBoundingBox().Top;
                                k0 = k;
                            }
                        bottom = hit[k0].GetBoundingBox().Bottom;
                    }
                }

                left -= Math.Min(horiz ? b.Width * 2 : Math.Max(b.Width * 2, b.Height * 2), (int)(Charreco.InkPixel * 50));
                right += Math.Min(horiz ? b.Width * 2 : Math.Max(b.Width * 2, b.Height * 2), (int)(Charreco.InkPixel * 50));                 
            }          
            else {
                if (!matrixParsing) {
                    double grabFactor = 0.75;
                    double grabDist = (grabFactor / (1 + b.Height * b.Height / 6400.0 / inkPixel / inkPixel) * Math.Min(b.Height, 500));
                    top = top - (int)Math.Max(inkPixel, grabDist);
                    bottom = bottom + (int)Math.Max(inkPixel, grabDist);
                    if ((0.0 + width) / height > 0.35) {
                        if (r.alt.Word != null) {//sin, etc
                            left -= (int)(xhgt * (0.5 + Math.Max(0, (inkPixel * 50 - xhgt) / 25.0 / inkPixel)));
                            right += (int)(xhgt * (0.5 + Math.Max(0, (inkPixel * 50 - xhgt) / 25.0 / inkPixel)));
                        }
                        else {
                            int expand = (int)(Math.Max(b.Width, xhgt) * (grabby ? 1.2 : 1));
                            double eratio = Math.Max(0, 1.75 - Math.Max(b.Width, xhgt) /30.0 / inkPixel) + 0.75;
                            left -= (int)(expand * eratio);
                            right += (int)(expand * eratio);
                        }
                    }
                    else {
                        left -= Math.Min(1, (int)(height + 0.0) / (2 * width)) * height;
                        right += Math.Min(1, (int)(height + 0.0) / (2 * width)) * height;
 
                    }
                    // the leftmost and rightmost parentheses are not included in the matrix
                    bool done = false;
                    if (r.alt == '(') {//reset right
                        foreach (ParenSym ps in opSyms)
                            if (ps.r.strokes[0].Id == r.strokes[0].Id && ps.Closing != null && !ps.Closing.r.strokes[0].Deleted) {
                                right = ps.Closing.r.strokes[0].GetBoundingBox().Right;
                                done = true;
                                break;
                            }
                        if (!done && rpStrokes != null)
                            foreach (Stroke s in rpStrokes)
                                if (!s.Deleted && r.strokes.GetBoundingBox().Left < s.GetBoundingBox().Right &&
                                    r.strokes.GetBoundingBox().Bottom > (s.GetBoundingBox().Top + 2 * s.GetBoundingBox().Bottom) / 3 &&//size and vertical location test
                                    r.strokes.GetBoundingBox().Top < (2 * s.GetBoundingBox().Top + s.GetBoundingBox().Bottom) / 3 &&//size and vertical location test
                                    s.GetBoundingBox().Bottom > (r.strokes[0].GetBoundingBox().Top + 2 * r.strokes[0].GetBoundingBox().Bottom) / 3 &&//size and vertical location test
                                    s.GetBoundingBox().Top < (2 * r.strokes[0].GetBoundingBox().Top + r.strokes[0].GetBoundingBox().Bottom) / 3)//size and vertical location test 
                                {
                                    right = s.GetBoundingBox().Right;
                                    break;
                                }
                    }
                    else if (r.alt == ')' && opSyms != null) {//reset left                      
                        foreach (ParenSym ps in opSyms)
                            if (ps.Closing != null && ps.Closing.r.strokes[0].Id == r.strokes[0].Id) {
                                left = ps.r.strokes[0].GetBoundingBox().Left;
                                done = true;
                                break;
                            }
                        if (!done)
                            foreach (ParenSym ps in opSyms)
                                if (ps.Closing == null &&
                                        r.strokes[0].GetBoundingBox().Right > ps.r.strokes[0].GetBoundingBox().Left &&
                                        r.strokes[0].GetBoundingBox().Bottom > (ps.r.strokes[0].GetBoundingBox().Top + 2 * ps.r.strokes[0].GetBoundingBox().Bottom) / 3 &&//size and vertical location test
                                        r.strokes[0].GetBoundingBox().Top < (2 * ps.r.strokes[0].GetBoundingBox().Top + ps.r.strokes[0].GetBoundingBox().Bottom) / 3 &&//size and vertical location test
                                        ps.r.strokes[0].GetBoundingBox().Bottom > (r.strokes[0].GetBoundingBox().Top + 2 * r.strokes[0].GetBoundingBox().Bottom) / 3 &&//size and vertical location test
                                        ps.r.strokes[0].GetBoundingBox().Top < (2 * r.strokes[0].GetBoundingBox().Top + r.strokes[0].GetBoundingBox().Bottom) / 3)//size and vertical location test 
                                    {
                                    ps.Closing = Symbol.From(Charreco, r);
                                    left = ps.r.strokes[0].GetBoundingBox().Left;
                                    break;
                                }
                    }
                }
                else {//for matrix
                    /*
                    int grabDist = (int)(b.Width + (Char.IsDigit(r.alt.Character) ? b.Height : Math.Max(b.Width, xhgt)) + 0.0) / 3;
                    top -= grabDist / 2;
                    bottom += grabDist / 2;
                    left -= grabDist;
                    right += grabDist;
                    */
                    double grabFactor = 0.75;
                    double grabDist = (grabFactor / (1 + b.Height * b.Height / 2500.0 / inkPixel / inkPixel) * Math.Min(b.Height, 460));
                    top = top - (int)Math.Max(inkPixel, grabDist);
                    bottom = bottom + (int)Math.Max(inkPixel, grabDist);
                    if ((0.0 + width) / height > 0.35) {
                        if (r.alt.Word != null) {//sin, etc
                            left -= (int)(xhgt * (0.5 + Math.Max(0, (inkPixel * 50 - xhgt) / 25.0 / inkPixel)));
                            right += (int)(xhgt * (0.5 + Math.Max(0, (inkPixel * 50 - xhgt) / 25.0 / inkPixel)));
                        }
                        else {
                            int expand = (int)(Math.Max(b.Width, xhgt) * (grabby ? 1.2 : 1));
                            double eratio = Math.Max(0, 1.5 - Math.Max(b.Width, xhgt) / 20.0 / inkPixel) + 0.5;
                            left -= (int)(expand * eratio);
                            right += (int)(expand * eratio);
                        }
                    }
                    else {
                        int delta = (int)(Math.Max(height, 20.0 * inkPixel) / (1 + height * height / 50.0 / 50.0 / inkPixel / inkPixel));
                        left -= delta;
                        right += delta;
                    }

                    bool done = false;
                    if (r.alt == '(') {// reset right
                        foreach (ParenSym ps in opSyms)
                            if (ps.r.strokes[0].Id == r.strokes[0].Id && ps.Closing != null && !ps.Closing.r.strokes[0].Deleted) {
                                right = ps.Closing.r.strokes[0].GetBoundingBox().Right;
                                done = true;
                                break;
                            }
                        if (!done && rpStrokes != null)
                            foreach (Stroke s in rpStrokes)
                                if (!s.Deleted && r.strokes[0].GetBoundingBox().Left < s.GetBoundingBox().Right &&
                                    r.strokes[0].GetBoundingBox().Bottom > (s.GetBoundingBox().Top + 2 * s.GetBoundingBox().Bottom) / 3 &&//size and vertical location test
                                    r.strokes[0].GetBoundingBox().Top < (2 * s.GetBoundingBox().Top + s.GetBoundingBox().Bottom) / 3 &&//size and vertical location test
                                    s.GetBoundingBox().Bottom > (r.strokes[0].GetBoundingBox().Top + 2 * r.strokes[0].GetBoundingBox().Bottom) / 3 &&//size and vertical location test
                                    s.GetBoundingBox().Top < (2 * r.strokes[0].GetBoundingBox().Top + r.strokes[0].GetBoundingBox().Bottom) / 3)//size and vertical location test 
                                {
                                    right = s.GetBoundingBox().Right;
                                    break;
                                }

                    }
                    else if (r.alt == ')' && opSyms != null) {// reset left
                        foreach (ParenSym ps in opSyms)
                            if (ps.Closing != null && ps.Closing.r.strokes[0].Id == r.strokes[0].Id) {
                                left = ps.r.strokes[0].GetBoundingBox().Left;
                                done = true;
                                break;
                            }
                        if (!done)
                            foreach (ParenSym ps in opSyms)
                                if (ps.Closing == null &&
                                        r.strokes[0].GetBoundingBox().Right > ps.r.strokes[0].GetBoundingBox().Left &&
                                        r.strokes[0].GetBoundingBox().Bottom > (ps.r.strokes[0].GetBoundingBox().Top + 2 * ps.r.strokes[0].GetBoundingBox().Bottom) / 3 &&//size and vertical location test
                                        r.strokes[0].GetBoundingBox().Top < (2 * ps.r.strokes[0].GetBoundingBox().Top + ps.r.strokes[0].GetBoundingBox().Bottom) / 3 &&//size and vertical location test
                                        ps.r.strokes[0].GetBoundingBox().Bottom > (r.strokes[0].GetBoundingBox().Top + 2 * r.strokes[0].GetBoundingBox().Bottom) / 3 &&//size and vertical location test
                                        ps.r.strokes[0].GetBoundingBox().Top < (2 * r.strokes[0].GetBoundingBox().Top + r.strokes[0].GetBoundingBox().Bottom) / 3)//size and vertical location test 
                                    {
                                    ps.Closing = Symbol.From(Charreco, r);
                                    left = ps.r.strokes[0].GetBoundingBox().Left;
                                    break;
                                }
                    }
                }
            }
            if (r.allograph.Contains("larrow"))  left = 0; 
            b = new Rectangle(left, top, right - left, bottom - top);
            //if (r.allograph == "+" && parenID == -1) grabby = true;
            //else if (r.allograph == "+" && parenID != -1) divs = true;
            if (r.allograph == "-" || r.allograph == "perp" || r.alt == Unicode.N.N_ARY_SUMMATION || r.alt == Unicode.I.INTEGRAL) // || r.alt == Unicode.S.SQUARE_ROOT)
                divs  = true;
            // we don't aggregate division lines with anything.  Instead, things have to aggregate to a division line
            //if ((!(r.alt == Recognition.Result.Special.Division && r.allograph[0] != Unicode.D.DIVISION_SLASH)  && r.allograph != "-") || 
            //     (r.strokes[0].Id == r.strokes.Ink.Strokes[r.strokes.Ink.Strokes.Count-1].Id) ||
            //     r.strokes[0].Ink.HitTest(new Rectangle(r.bbox.Left, r.bbox.Top-r.bbox.Width/2, r.bbox.Width,r.bbox.Width),1).Count < 2) {
            return b;
        }

        /// <summary>
        /// The update strokes collection is used to update ranges in stmt
        ///         int rangeId = UpdateRanges(update, strokes, inkPixel);
        /// rangeID is always one, and only the last added or updated range is parsed further.
        /// All strokes in the added or updated range are eventually sorted and passed to the parseLine(... , ref SortedList sl, ...) method
        /// </summary>
        /// <param name="update"></param>
        /// <param name="strokes"></param>
        /// <param name="inkPixel"></param>
        /// <returns></returns>
        public List<Range> UpdateParse(Strokes update, Strokes strokes, int inkPixel) {
            foreach (Stroke s in strokes) {
                Recognition r = Charreco.Classification(s);
                if (r == null) continue;

                if (r.levelsetby > 0) {
                    r.levelsetby = int.MaxValue;
                    r.curalt = 0;
                } else if (r.levelsetby < 0)
                    r.levelsetby = -r.levelsetby;
                r.parseError = false;
            }
            if (update == null) {
                Parse(Charreco.filter(strokes), inkPixel);
                return Ranges;
            }
            int rangeCount = -1;
            List<ParseResult> results = new List<ParseResult>();
            rangeCount = UpdateRanges(update, strokes);
            inkPixel = Math.Abs(inkPixel);
            //parse all affected ranges, including those which do not contains update
            for (int i = 0; i < (rangeCount < 0 ? Ranges.Count : Math.Min(Ranges.Count, rangeCount)); i++) {
                int okay = 0;
                while (okay < 5) {
                    try {
                        Ranges[Ranges.Count - i - 1].Parse = InParse(update, Ranges[Ranges.Count - i - 1].Strokes, inkPixel);
                        Parse2.Parse(this, Ranges[Ranges.Count - i - 1].Parse);
                        okay = 5;
                    } catch (Reparse1Exception) {
                        //InParse(Ranges[Ranges.Count - i - 1].Strokes, inkPixel);
                        okay++;
                    }
                }
            }
            return Ranges;
        }
        public void Parse(Strokes strokes, int inkPixel) {
            UpdateRanges(null, strokes);
            foreach (Stroke s in strokes) {
                Recognition r = Charreco.Classification(s);
                if (r != null) {
                    if (r.levelsetby > 0) {
                        r.levelsetby = int.MaxValue;
                        r.curalt = 0;
                    } else
                        r.levelsetby = -r.levelsetby;
                    r.parseError = false;
                }
            }
            foreach (Range rg in Ranges) {
                int okay = 0;
                while (okay < 10) {
                    try {
                        rg.Parse = InParse(null, rg.Strokes, inkPixel);
                        Parse2.Parse(this, rg.Parse);
                        okay = 10;
                    } catch (Reparse1Exception) {
                        okay++;
                    }
                }
            }
        }

        private ParseResult InParse(Strokes update, Strokes strokes, int inkPixel) {
            int okay = 0;
            while (okay < 10) {
                try {
                    return inParse(update, strokes, inkPixel);
                } catch (Reparse1Exception) {
                    okay++;
                }
            }
            return null;
        }

        private ParseResult inParse(Strokes update, Strokes strokes, int inkPixel) {
            /* really first, reset all alternates */
            foreach (Stroke s in strokes) {
                Recognition r = Charreco.Classification(s);
                if (r == null) continue;

                if (r.levelsetby > 0) {
                    r.levelsetby = int.MaxValue;
                    r.curalt = 0;
                } else if (r.levelsetby == -1)
                    r.levelsetby = -r.levelsetby;
                r.parseError = false;


                //Added here, not in HandleStrokeCollected() to avoid being bypassed by the Parse button, which calls Parse(null, false, true) in Form1
                //Loading inks from files also requires to capture these symbols here.
                bool exist = false;
                if (r.alt == Unicode.N.N_ARY_SUMMATION) {
                    if (sumList == null)
                        sumList = new List<Recognition>();
                    if (sumList.Count > 0)
                        foreach (Recognition sr in sumList) {
                            if (sr.strokes[0].Id == r.strokes[0].Id) {
                                exist = true;
                                break;
                            }
                        }
                    if (!exist)
                        sumList.Add(r);
                } else if (r.alt == Unicode.I.INTEGRAL) {
                    if (integralList == null)
                        integralList = new List<Recognition>();
                    if (integralList.Count > 0)
                        foreach (Recognition ir in integralList) {
                            if (ir.strokes[0].Id == r.strokes[0].Id) {
                                exist = true;
                                break;
                            }
                        }
                    if (!exist) integralList.Add(r);
                } else if (r.alt == '(' && Symbol.ParseParensToMatrices) { //capture current alternate for matrix parsing. 
                    if (opSyms.Count > 0)
                        foreach (ParenSym os in opSyms)
                            if (os.r.strokes[0].Id == r.strokes[0].Id) {
                                exist = true;
                                break;
                            }
                    if (!exist) opSyms.Add((ParenSym)Symbol.From(Charreco, r));
                }else if (r.alt.Character == ')') {
                    if (rpStrokes == null || rpStrokes.Count == 0) rpStrokes = s.Ink.CreateStrokes();
                    rpStrokes.Add(s);
                }


            }

            ArrayList divs;
            SortedList sl;
            sortStrokes(strokes, inkPixel, out divs, out sl);
            sortedRecs = sl;

            Hashtable used = new Hashtable();
            Line root2 = new Line();
            superPreParsed = new Hashtable(); // clean out the superscript preparsed hashtable before calling parseLine for the first time
            subPreParsed = new Hashtable();
            superIntegralPreParsed = new Hashtable();
            subIntegralPreParsed = new Hashtable();
            matrixStrokes = new List<Strokes>();
            leftParen = new List<Recognition>();
            rightParen = new List<Recognition>();
            dots = new List<Strokes>();
            matrixLparenInds = new Hashtable();
            Recognition rr = null;

            if(update != null && !strokes.Contains(update[0])) update = null;
            if(update != null) rr = Charreco.Classification(update[0]);
            if(update == null || rr == null)
                matrixRanges.Clear();
            if (rr != null && (rr.alt == '(' || rr.alt == ')')) {
                oldMatrixRanges = new List<List<Range>>();
                for (int k1 = 0; k1 < matrixRanges.Count; k1++) {
                    List<Range> newRanges = new List<Range>();
                    for (int k2 = 0; k2 < matrixRanges[k1].Count; k2++) {
                        Strokes ss = rr.strokes.Ink.CreateStrokes();
                        ss.Add(matrixRanges[k1][k2].Strokes);
                        Range newRange = new Range(Charreco, ss, rr.strokes.Ink.CreateStrokes(), rr.strokes.Ink.CreateStrokes());
                        newRanges.Add(newRange);
                    }
                    oldMatrixRanges.Add(newRanges);                    
                }
                if (matrixRanges.Count == 0) {
                    List<Range> newRanges = new List<Range>();
                    oldMatrixRanges.Add(newRanges);   
                }
                matrixRanges.Clear(); //???
            }
            if (rr != null && rr.curalt > 0 && (rr.alts[rr.curalt - 1] == '(' || rr.alts[rr.curalt - 1] == ')'))
                 matrixRanges = oldMatrixRanges;
 
            if (Symbol.ParseParensToMatrices)//this can be done multiple times due to parse 1 reparse, just like the above more time consuming stroke sorting
                collectMatrixStrokes(update, sl);//collect matrix strokes for preprocessing
            parseLine(update, strokes, ref used, ref sl, ref root2, divs, Rectangle.Empty, 0, null, false);

            return new ParseResult(strokes, root2);

        }

        private void sortStrokes(Strokes strokes, int inkPixel, out ArrayList divs, out SortedList sl) {
            List<Recognition> recogs = new List<Recognition>();

            foreach (Stroke s in strokes) {
                Recognition r = Charreco.Classification(s);
                if (r != null && !recogs.Contains(r))
                    recogs.Add(r);
            }
            Strokes ignored = strokes.Ink.CreateStrokes();
            ignored.Add(strokes.Ink.Strokes);
            ignored.Remove(strokes);
            SortedList divisions = new SortedList();
            Hashtable divStarts = new Hashtable();
            List<Recognition> divcandidates = new List<Recognition>();
            // first find all the divisions and sort them by size.  The largest divisions will take hierarchical
            // precedence over the smaller ones
            // also remove the Super/Sub guid flags from any strokes that are not in the same range as their referent Summation/Integral
            foreach (Recognition r in recogs) {
                Rectangle bbox = r.bbox;
                if (r.alt == 'r' || r.alt == 'n' || r.alt == 'v' || r.alt == "INTbot") {
                    Point[] hpts = new Point[] { bbox.Location, new Point(bbox.Right, bbox.Top),
                                                 new Point(bbox.Right, (bbox.Top+bbox.Bottom)/2), new Point(bbox.Left, bbox.Bottom), bbox.Location };
                    Strokes contained = r.strokes.Ink.HitTest(hpts, 20);
                    contained.Remove(r.strokes);
                    if (contained.Count == 0)
                        continue;
                    for (int a = 0; a < r.alts.Length; a++)
                        if (r.alts[a] == Unicode.S.SQUARE_ROOT) {
                            r.levelsetby = 1;
                            r.curalt = a;
                            break;
                        }
                }
                if ((r.levelsetby < 2 && r.alt != Recognition.Result.Special.Division) ||
                     (r.allograph != "-" && r.allograph != Unicode.M.MINUS_SIGN.ToString() && r.alt != Unicode.D.DIVISION_SLASH && r.alt != Unicode.I.INTEGRAL && r.alt != Unicode.S.SQUARE_ROOT && r.alt != Unicode.N.N_ARY_SUMMATION && r.alt != '↘'))
                    continue;
                int xtend = inkPixel * 5;
                int extender = Math.Max(bbox.Height, bbox.Width);
                Point[] hull = (r.alt == Unicode.D.DIVISION_SLASH || (r.alt == Recognition.Result.Special.Division && r.alts[0] == Unicode.D.DIVISION_SLASH)) ?
                        new Point[] { new Point(bbox.Left-xtend,bbox.Top-extender), new Point(bbox.Right+xtend,bbox.Top-extender),
                                      new Point(bbox.Right+xtend, bbox.Bottom+extender), new Point(bbox.Left-xtend, bbox.Bottom+extender) } :
                        new Point[] { new Point(bbox.Left-xtend,bbox.Top-extender), new Point(bbox.Right-xtend,bbox.Top-extender),
                                      new Point(bbox.Right-xtend, bbox.Bottom+extender), new Point(bbox.Left-xtend, bbox.Bottom+extender) };
                Strokes stks = r.strokes.Ink.HitTest(hull, 1);
                foreach (Recognition root in divcandidates)
                    if (root != r && root.alt == Unicode.S.SQUARE_ROOT) {
                        Point ctr = new Point((bbox.Left + bbox.Right) / 2, bbox.Bottom);
                        if (bbox.Bottom < bbox.Top && ctr.X > bbox.Left && ctr.X < bbox.Right)
                            stks.Add(root.strokes);
                    }
                stks.Remove(r.strokes);
                stks.Remove(ignored);
                foreach (Stroke x in stks) {
                    Recognition rr = Charreco.Classification(x);
                    if (rr == null || x.DrawingAttributes.Transparency == 255) continue;
                    Rectangle xbounds = rr.bbox;
                    foreach (Recognition rk in divisions.Values) {
                        if (!(rk.strokes.GetBoundingBox().Left > xbounds.Right || rk.strokes.GetBoundingBox().Right < xbounds.Left))
                            if ((rk.strokes.GetBoundingBox().Top < bbox.Top && xbounds.Bottom < rk.strokes.GetBoundingBox().Bottom) ||
                                (rk.strokes.GetBoundingBox().Bottom > bbox.Bottom || xbounds.Top > rk.strokes.GetBoundingBox().Top)) {
                                stks.Remove(x);
                                break;
                            }
                    }
                    if (r.alt == Unicode.M.MINUS_SIGN && bbox.Width / (float)xbounds.Width < (rr.alt == Unicode.S.SQUARE_ROOT ? 0.6 : 0.9)) // this isn't just a minus sign
                        stks.Remove(x);
                    if ((r.alt == Unicode.I.INTEGRAL || r.alt == Unicode.N.N_ARY_SUMMATION) && xbounds.Top > bbox.Top && xbounds.Bottom < bbox.Bottom)
                        stks.Remove(x);
                    else if (rr.alt == '=' || rr.alt == Unicode.R.RIGHTWARDS_ARROW || rr.alt == Recognition.Result.Special.Division || rr.alt == '+' || rr.alt == '>' || rr.alt == '<' || rr.alt == Unicode.M.MINUS_SIGN)
                        stks.Remove(x);
                    else if (rr.alt == '(' || rr.alt == Unicode.I.INTEGRAL || rr.alt == Unicode.N.N_ARY_SUMMATION || rr.alt == Unicode.S.SQUARE_ROOT) {
                        if (xbounds.Top < bbox.Top && xbounds.Bottom > bbox.Bottom)
                            stks.Remove(x);
                    } else {
                        int span = Math.Min(xbounds.Right, bbox.Right) - Math.Max(xbounds.Left, bbox.Left);
                        if (r.alt == Unicode.I.INTEGRAL || r.alt == Unicode.N.N_ARY_SUMMATION) {
                            if (span < 0)
                                stks.Remove(x);
                        } else if ((span + 0.0) / xbounds.Width < ((xbounds.Width / (float)xbounds.Height) > 1 ? 0.4 : .3))
                            stks.Remove(x);
                    }
                }
                if (stks.Count > 0 || r.alt == Recognition.Result.Special.Division || r.alt == Unicode.I.INTEGRAL ||
                    r.alt == Unicode.N.N_ARY_SUMMATION || r.alt == Unicode.S.SQUARE_ROOT)
                    divcandidates.Add(r);
            }
            divs = new ArrayList();
            foreach (Recognition r in divcandidates) {
                int xtend = inkPixel * 5;
                Rectangle bbox = r.bbox;
                int extender = Math.Max(bbox.Height, bbox.Width);
                Point[] hull = (r.alt == Unicode.D.DIVISION_SLASH || (r.alt == Recognition.Result.Special.Division && r.alts[0] == Unicode.D.DIVISION_SLASH)) ?
                    new Point[] { new Point(bbox.Left-xtend,  bbox.Top-extender),    new Point(bbox.Right+xtend,bbox.Top-extender),
                                  new Point(bbox.Right+xtend, bbox.Bottom+extender), new Point(bbox.Left-xtend, bbox.Bottom+extender) } :
                    new Point[] { new Point(bbox.Left-xtend,  bbox.Top-extender),    new Point(bbox.Right-xtend,bbox.Top-extender),
                                  new Point(bbox.Right-xtend, bbox.Bottom+extender), new Point(bbox.Left-xtend, bbox.Bottom+extender) };
                Strokes stks = r.strokes.Ink.HitTest(hull, 1);
                foreach (Recognition root in divcandidates)
                    if (root != r && root.alt == Unicode.S.SQUARE_ROOT) {
                        Point ctr = new Point((root.strokes.GetBoundingBox().Left + root.strokes.GetBoundingBox().Right) / 2,
                            root.strokes.GetBoundingBox().Bottom);
                        if (root.strokes.GetBoundingBox().Bottom < r.bbox.Top && ctr.X > bbox.Left && ctr.X < bbox.Right)
                            stks.Add(root.strokes);
                    }
                stks.Remove(r.strokes);
                List<Recognition> testRecs = new List<Recognition>();
                stks.Remove(ignored);
                foreach (Stroke x in stks) {
                    Recognition rr = Charreco.Classification(x);
                    if (rr != null && x.DrawingAttributes.Transparency != 255)
                        testRecs.Add(rr);
                }
                List<Recognition> hitRecs = new List<Recognition>();
                foreach (Recognition testr in testRecs) {
                    Recognition rr = testr;
                    Rectangle xbounds = rr.bbox;
                    foreach (Recognition rk in divisions.Values) if (rk.alt != 'r' && rk.alt != 'n' && rk.alt != Unicode.S.SQUARE_ROOT) {
                            if (!(rk.strokes.GetBoundingBox().Left > xbounds.Right ||
                                  rk.strokes.GetBoundingBox().Right < xbounds.Left))
                                if ((rk.strokes.GetBoundingBox().Top < bbox.Top && xbounds.Bottom < rk.strokes.GetBoundingBox().Bottom) ||
                                    (rk.strokes.GetBoundingBox().Bottom > bbox.Bottom && xbounds.Top > rk.strokes.GetBoundingBox().Top)) {
                                    rr = null;
                                    break;
                                }
                        }
                    if (rr ==null)
                        continue;
                    if ((r.alt == Unicode.I.INTEGRAL || r.alt == Unicode.N.N_ARY_SUMMATION) && xbounds.Top > bbox.Top && xbounds.Bottom < bbox.Bottom)
                        rr = null;
                    else if (rr.alt == '=' || rr.alt == Unicode.R.RIGHTWARDS_ARROW || rr.alt == Recognition.Result.Special.Division || rr.alt == '+' || rr.alt == '>' || rr.alt == '<' || rr.alt == Unicode.M.MINUS_SIGN)
                        rr = null;
                    else if (rr.alt == '(' || rr.alt == Unicode.N.N_ARY_SUMMATION || rr.alt == Unicode.I.INTEGRAL || rr.alt == Unicode.S.SQUARE_ROOT) {
                        if ((r.alt == Unicode.I.INTEGRAL || r.alt == Unicode.N.N_ARY_SUMMATION || r.alt == Unicode.S.SQUARE_ROOT) &&
                            (xbounds.Top < bbox.Top || xbounds.Bottom > bbox.Bottom))
                            rr = null;
                    }
                    if (xbounds.Top < bbox.Top && xbounds.Bottom > bbox.Bottom)
                        rr = null;
                    else if (rr != null) {
                        int span = Math.Min(xbounds.Right, bbox.Right) - Math.Max(xbounds.Left, bbox.Left);
                        if (r.alt == Unicode.S.SQUARE_ROOT && xbounds.Left < bbox.Left &&
                            xbounds.Bottom > (bbox.Top + 2 * bbox.Bottom) / 3)
                            rr = null;
                        else if (r.alt == Unicode.I.INTEGRAL) {
                            if (bbox.Left > rr.bbox.Left && rr.bbox.Height / (float)bbox.Height > 0.6)
                                rr = null;
                        } else if (rr.alt == ')' && rr.bbox.Left < bbox.Left) {
                            rr = null;
                        } else if (r.alt == Unicode.N.N_ARY_SUMMATION || r.alt == Unicode.S.SQUARE_ROOT) {
                            if (span < 0)
                                rr = null;
                        } else if ((span + 0.0) / xbounds.Width < ((xbounds.Width / (float)xbounds.Height) > 1 ? 0.4 : .3))
                            rr = null;
                        else if (bbox.Width / (float)rr.bbox.Width < 0.8 &&
                            (r.alt != Unicode.M.MINUS_SIGN || rr.alt != Unicode.S.SQUARE_ROOT))
                            rr = null;
                    }
                    if (rr != null)
                        hitRecs.Add(rr);
                }
                if (hitRecs.Count > 0 || r.alt == Recognition.Result.Special.Division || r.alt == Unicode.I.INTEGRAL ||
                                         r.alt == Unicode.S.SQUARE_ROOT || r.alt == Unicode.N.N_ARY_SUMMATION) {
                    Rectangle bounds = r.bbox;
                    foreach (Recognition rr in hitRecs) {
                        Rectangle rbounds = rr.bbox;
                        rbounds.Inflate(new Size(1, 1)); // so that the division will come before this stroke
                        bounds = Rectangle.Union(bounds, rbounds);
                    }
                    if (divStarts.Contains(r))
                        continue;
                    divStarts.Add(r, bounds.Left);
                    int key = -bbox.Width;
                    while (divisions.ContainsKey(key))
                        key -= 1;
                    divisions.Add(key, r);
                    if (r.alt != Recognition.Result.Special.Division && r.alt != Unicode.I.INTEGRAL && r.alt != Unicode.N.N_ARY_SUMMATION && r.alt != Unicode.S.SQUARE_ROOT && r.alt != 'n' && r.alt != 'r' && r.alt != '↘') {
                        Trace.Assert(r.levelsetby > 1);
                        r.addorsetalt(Recognition.Result.Special.Division, r.baseline, r.xheight);
                        r.levelsetby = 1;
                    }
                }
            }
            sl = new SortedList();
            // first sort all the non-division strokes by leftmost point
            // Division lines are sorted by extending them to come before any
            // strokes that they intersect on their left edge, unless those strokes are an '=',
            // or an already processed division (which was sorted to be larger and take precedent)
            foreach (Recognition r in recogs) {
                Rectangle bbox = r.bbox;
                int key = bbox.Left;
                if (r != null && divStarts.Contains(r) && (r.alt == Recognition.Result.Special.Division || r.alt == Unicode.I.INTEGRAL ||
                        r.alt == Unicode.S.SQUARE_ROOT || r.alt == Unicode.N.N_ARY_SUMMATION)) { // handle division lines: make them come before anything above or below them that isn't a longer division line
                    key = (int)divStarts[r];
                    Point loc = bbox.Location;
                    Point[] hull = new Point[] { new Point(loc.X, 0), new Point(loc.X + inkPixel*10, 0), 
                                                     new Point(loc.X + inkPixel*10, inkPixel*1000), new Point(loc.X, inkPixel*1000) };
                    Strokes stks = r.strokes.Ink.HitTest(hull, 1);
                    divs.Add(r);
                    foreach (Recognition sk in divisions.GetValueList())
                        if (sk.strokes[0] == r.strokes[0]) {
                            break;
                        } else
                            stks.Remove(sk.strokes[0]);
                    stks.Remove(ignored);
                    foreach (Stroke x in stks)
                        if (!x.Deleted && x.DrawingAttributes.Transparency != 255) {
                            bool cont = true;
                            foreach (Recognition rk in divisions.Values) {
                                if (!(rk.strokes.GetBoundingBox().Left > x.GetBoundingBox().Right ||
                                          rk.strokes.GetBoundingBox().Right < x.GetBoundingBox().Left))
                                    if (rk.alt == Unicode.S.SQUARE_ROOT || rk.alt == 'r' || rk.alt == 'n') {
                                    } else if ((rk.strokes.GetBoundingBox().Top < bbox.Top &&
                                            x.GetBoundingBox().Bottom < rk.strokes.GetBoundingBox().Bottom) ||
                                           (rk.strokes.GetBoundingBox().Bottom > bbox.Bottom &&
                                            x.GetBoundingBox().Top > rk.strokes.GetBoundingBox().Top)) {
                                        stks.Remove(x);
                                        cont = false;
                                        break;
                                    }
                            }
                            if (!cont)
                                continue;
                            if ((r.alt == Unicode.I.INTEGRAL || r.alt == Unicode.N.N_ARY_SUMMATION) && x.GetBoundingBox().Top > bbox.Top && x.GetBoundingBox().Bottom < bbox.Bottom)
                                stks.Remove(x);
                            else if (r.alt == Unicode.S.SQUARE_ROOT && x.GetBoundingBox().Left < bbox.Left)
                                stks.Remove(x);
                            else {
                                Recognition rr = Charreco.Classification(x);
                                if (rr == null)
                                    continue;
                                if (rr.alt == '=' || rr.alt == '+' || rr.alt == '>' || rr.alt == '<' || rr.alt == Unicode.R.RIGHTWARDS_ARROW || rr.alt == Unicode.M.MINUS_SIGN)
                                    stks.Remove(x);
                                if (rr.alt == ')' && rr.bbox.Left < bbox.Left)
                                    stks.Remove(x);
                                if (rr.alt == '(' || rr.alt == Unicode.I.INTEGRAL || rr.alt == Unicode.S.SQUARE_ROOT) {
                                    if (r.alt == Unicode.I.INTEGRAL && (
                                        rr.bbox.Top < bbox.Top ||
                                            rr.bbox.Bottom > bbox.Bottom))
                                        stks.Remove(x);
                                    else if (rr.bbox.Top <bbox.Top &&
                                            rr.bbox.Bottom > bbox.Bottom)
                                        stks.Remove(x);
                                } else {
                                    int span = Math.Min(rr.bbox.Right, bbox.Right) -
                                                   Math.Max(rr.bbox.Left, bbox.Left);
                                    if ((span + 0.0) / rr.bbox.Width < 0.4)
                                        stks.Remove(x);
                                }

                            }
                        }
                    foreach (Stroke h in stks) {
                        Recognition rr = Charreco.Classification(h);
                        if (h != null) {
                            if ((key == -1 || h.GetBoundingBox().Left < key) &&
                                    (h.GetBoundingBox().Top > loc.Y || h.GetBoundingBox().Bottom < loc.Y))
                                key = h.GetBoundingBox().Left - 1;
                        }
                    }
                }
                while (sl.ContainsKey(key))
                    key -= 1;
                sl.Add(key, r);
            }

            foreach (Stroke prev in strokes) if (!prev.Deleted) {  // handle overlapping strokes
                    Recognition pr = Charreco.Classification(prev);
                    if (pr == null || pr.alt == Recognition.Result.Special.Division || pr.alt.Character == Unicode.S.SQUARE_ROOT || pr.alt.Character == 'r' || pr.alt.Character == 'n')
                        continue;
                    Strokes hitraw = prev.Ink.HitTest(prev.GetBoundingBox(), 25);
                    hitraw.Remove(pr.strokes);
                    Strokes hit = hitraw.Ink.CreateStrokes();
                    foreach (Stroke h in hitraw) {
                        Recognition rr = Charreco.Classification(h);
                        if (rr != null && rr.alt != Recognition.Result.Special.Division)
                            hit.Add(h);
                    }
                    if (hitraw.Count > 0) {
                        int minshift = 0;
                        List<Stroke> slist = new List<Stroke>();
                        int pid = 0;
                        foreach (Recognition psort in sl.GetValueList())
                            if (psort.strokes.Contains(prev))
                                break;
                            else pid++;
                        foreach (Stroke h in hit) {
                            int hid = 0;
                            foreach (Recognition hsort in sl.GetValueList())
                                if (hsort.strokes.Contains(h))
                                    break;
                                else hid++;
                            if (hid > pid) {
                                Ink tmpink = new Ink();
                                int midh = Math.Max(Math.Min(prev.GetBoundingBox().Bottom-(int)2*inkPixel, h.GetBoundingBox().Top + h.GetBoundingBox().Height / 2), prev.GetBoundingBox().Top+(int)2*inkPixel);
                                Stroke tprev = tmpink.CreateStroke(prev.GetPoints());
                                Stroke th    = tmpink.CreateStroke(h.GetPoints());
                                Stroke tray  = tmpink.CreateStroke(new Point[] { new Point(Math.Min(h.GetBoundingBox().Left,prev.GetBoundingBox().Left),midh),
                                                                                 new Point(Math.Max(h.GetBoundingBox().Right,prev.GetBoundingBox().Right), midh) });
                                float[] ints1 = tprev.FindIntersections(tmpink.CreateStrokes(new int[] { tray.Id }));
                                float[] ints2 = th.FindIntersections(tmpink.CreateStrokes(new int[] { tray.Id }));
                                // this is the normal case when something like an integral contains a character inside its bounding box
                                // but the character is actually to the left of the stem of the integral
                                // Summations should be handled differently--need to know if sym is between the two left pointing cusps or not
                                if (pr.alt != ')' && (pr.alt != Unicode.N.N_ARY_SUMMATION ||  (th.GetBoundingBox().Top > tprev.GetBoundingBox().Top && 
                                    th.GetBoundingBox().Bottom< tprev.GetBoundingBox().Bottom))&& ints2.Length != 0 && (ints1.Length == 0 || getPt(ints1[0], tprev.GetPoints()).X > getPt(ints2[0], th.GetPoints()).X)) {
                                    minshift = Math.Max(minshift, getPt(ints2[0], th.GetPoints()).X - prev.GetBoundingBox().Left);
                                    slist.Add(h);
                                }
                                if (pr.alt == '(' && ints1.Length != 0 && ints2.Length != 0 && getPt(ints1[0], tprev.GetPoints()).X < getPt(ints2[0], th.GetPoints()).X &&
                                    prev.GetBoundingBox().Left > h.GetBoundingBox().Left) {
                                    //  in this case the previous stroke needs to come after the thing it contains
                                    //  this handles the case (x where one of the x strokes comes before the (
                                    int shiftright = prev.GetBoundingBox().Left-h.GetBoundingBox().Left;
                                    int mid = 0;
                                    foreach (Recognition msort in sl.GetValueList())
                                        if (msort.strokes.Contains(prev)) {
                                            sl.RemoveAt(mid);
                                            break;
                                        } else mid++;
                                    int key = prev.GetBoundingBox().Left + shiftright;
                                    while (sl.ContainsKey(key))
                                        key++;
                                    sl.Add(key, pr);
                                }
                            }
                        }
                        foreach (Stroke m in slist) {
                            int mid = 0;
                            foreach (Recognition msort in sl.GetValueList())
                                if (msort.strokes.Contains(m)) {
                                    sl.RemoveAt(mid);
                                    break;
                                } else mid++;
                            int key = m.GetBoundingBox().Left - minshift;
                            while (sl.ContainsKey(key))
                                key--;
                            Recognition mrec = Charreco.Classification(m);
                            if (mrec != null)
                                sl.Add(key, mrec);
                        }
                    }
                }
        }

        #region HashListGuid
        // CJ: declare the superscript preparsed hashtable
        public Hashtable superPreParsed = null;
        public Hashtable subPreParsed = null;
        public Hashtable superIntegralPreParsed = null;
        public Hashtable subIntegralPreParsed = null;
        public List<Recognition> leftParen = null;
        public List<Recognition> rightParen = null;
        public List<Strokes> matrixStrokes = null;
        public Hashtable matrixLparenInds = null;
        public List<List<Range>> matrixRanges = new List<List<Range>>(); //updated in UpdateRanges()
        public List<List<Range>> oldMatrixRanges = new List<List<Range>>();
        public List<ParenSym> opSyms = new List<ParenSym>();
        Ink tmpink = new Ink();
        public Strokes rpStrokes;

        public List<Strokes> dots = null;
        public List<Recognition> sumList = new List<Recognition>();
        public List<Recognition> integralList = new List<Recognition>();
        public static Guid TempGuid = new Guid("{2B73D681-15AA-47d6-8679-B2192634139C}");

        #endregion


        private void parseLine(Strokes allRangeStrokes, ref Hashtable used, ref SortedList sl, ref Line line, ArrayList divs, Rectangle bounds, int upDown, Symbol div2, bool inMatrix) { 
           parseLine(null, allRangeStrokes, ref used, ref sl, ref line, divs, bounds, upDown, div2, inMatrix);
        }

        private void parseLine(Strokes update, Strokes allRangeStrokes, ref Hashtable used, ref SortedList sl, ref Line line, ArrayList divs, Rectangle bounds, int upDown, Symbol div2, bool inMatrix) {
            int leftbounds = bounds.Left;
            if (div2 != null)
                foreach (Recognition xxx in sl.GetValueList())
                    if (xxx.strokes== div2.r.strokes) {
                        leftbounds = (int)sl.GetKey(sl.IndexOfValue(xxx));
                        break;
                    }
            bool allInside = used.Count == 0;
            List<Symbol> lookahead2 = new List<Symbol>();
           
            if (inMatrix) {
                bool isArrowEntry = false;
                foreach (Recognition r in sl.GetValueList()) 
                    if (r.allograph.Contains("drarrow")) {
                        isArrowEntry = true;
                        break;
                    }
                if (isArrowEntry){
                    foreach (Recognition r in sl.GetValueList())
                        line._syms.Add(Symbol.From(Charreco, r));
                    return;
                }
            }

            foreach (Recognition r in sl.GetValueList()) {                
                if (used.Contains(r.guid))
                    continue;
                if (bounds.IsEmpty &&
                        (r.allograph == "rdoublearrow" || r.allograph == "rarrow-1" ||r.allograph == "rarrow-2"||r.allograph.Contains("larrow"))) // special-case terminal arrows -- always siblings of root line
                    //if (sl.GetValueList().IndexOf(r) == sl.GetValueList().Count - (r.allograph == "rdoublearrow" ? 3 : r.allograph == "rarrow-1" ? 1 : 2)) {
                    if (sl.GetValueList().IndexOf(r) == sl.GetValueList().Count - 1) {
                        line._syms.Add(Symbol.From(Charreco, r));
                        used.Add(r.guid, 0);
                        continue;
                    }
                if (r.MatrixId != -1) {
                    int id = r.MatrixId;// reference paren Id
                    bool foundReference = false;

                    foreach (ParenSym ps in opSyms)
                        if (ps.r.strokes[0].Id == id) {// if the open paren is not deleted
                            if (ps.Closing == null) { // if not matched
                                foundReference = true;
                                break;
                            } else if (!ps.Closing.r.strokes[0].Deleted) { // matched and closing paren is on the right of current stroke
                                if (ps.Closing.r.strokes[0].GetBoundingBox().Right > r.strokes[0].GetBoundingBox().Right) {
                                    foundReference = true;
                                    break;
                                }
                            }
                        }
                    if (foundReference) //if the reference parenthesis is not deleted, skip its grabbed strokes during the normal parse
                        continue;
                    else//  otherwise parse them as non-matrix strokes
                        r.MatrixId = -1;
                }
                Line ForcedLineAssociation;
                if (CheckIfSymbolIsPreParsed(allRangeStrokes, used, r, upDown != 0 ? div2 : null, out ForcedLineAssociation))
                    continue; // pre-parsed symbol must wait for symbol that it refers to
                if (bounds != Rectangle.Empty && !bounds.IntersectsWith(r.bbox) && ForcedLineAssociation != null)
                    continue;

                bool inside = true;
                if (ForcedLineAssociation == null) {
                    int keepParsing = CheckIfSymbolIsInParseLineBounds(used, line, ref bounds, upDown, div2, leftbounds, allInside, lookahead2, r, out inside);
                    if (keepParsing == -1) return;   // symbol is horizontally after the range, no future symbols can be in range
                    if (keepParsing == 0) continue; // symbol is before above or below range, check next symbol
                }

                Symbol nn = Symbol.From(Charreco, r);

                ProcessPreParsedSymbols(allRangeStrokes, ref used, divs, inMatrix, r, nn);


                if (("><.+"+ Unicode.M.MINUS_SIGN + Unicode.D.DOT_OPERATOR).Contains(r.alt.Character.ToString())) {
                    if (sl.GetValueList().IndexOf(r) != sl.GetValueList().Count - 1 && (line.Empty || (
                        line.Last.TotalBounds.Top < nn.StrokeBounds.Bottom &&
                            line.Last.TotalBounds.Bottom > nn.StrokeBounds.Top))) {
                        lookahead2.Add(nn);
                        used.Add(nn.r.guid, 0);
                        continue;
                    }
                }
                Hashtable substruct = null;
                if (r.alt == Unicode.S.SQUARE_ROOT) {
                    used.Add(r.guid, 0);
                    substruct = (Hashtable)used.Clone();
                    Rectangle power = new Rectangle(r.bbox.Left - (int)(Charreco.InkPixel * 30),
                        r.bbox.Top - (int)(Charreco.InkPixel * 30), 10, r.strokes[0].GetBoundingBox().Height);
                    int botind, cornerInd;
                    FeaturePointDetector.maxy(0, r.strokes[0].GetPoints().Length, r.strokes[0].GetPoints(), out botind);
                    bool left;
                    V2D.MaxDist(r.strokes[0].GetPoints(),
                        V2D.Normalize(V2D.Sub(r.strokes[0].GetPoint(r.strokes[0].GetPoints().Length - 1), r.strokes[0].GetPoint(botind))), out left,
                        out cornerInd, botind, r.strokes[0].GetPoints().Length);
                    power.Size = new Size(r.strokes[0].GetPoint(cornerInd).X - power.Left,
                        power.Height + (int)(Charreco.InkPixel * 30));
                    Rectangle rootBnds = r.bbox;
                    rootBnds.Size = new Size(rootBnds.Width, bounds == Rectangle.Empty ? rootBnds.Bottom * 2 : Math.Max(rootBnds.Bottom, bounds.Bottom) - rootBnds.Top);
                    if (r.alt == Unicode.S.SQUARE_ROOT)  // parse power of root
                        parseLine(allRangeStrokes, ref substruct, ref sl, ref nn.Sub, divs, power, 0, nn, inMatrix);
                    used.Remove(r.guid);
                } else if (!(r.alt == Unicode.I.INTEGRAL || r.alt == Unicode.N.N_ARY_SUMMATION ) && divs.Contains(r)) {
                    used.Add(r.guid, 0);
                    substruct = (Hashtable)used.Clone();
                    Rectangle rbox = r.bbox;
                    if (upDown == 0 && bounds != Rectangle.Empty) {
                        Point tl = new Point(Math.Max(bounds.Left, rbox.Left), Math.Min(bounds.Top, rbox.Top));
                        Point br = new Point(Math.Min(bounds.Right, rbox.Right), rbox.Bottom);
                        rbox = new Rectangle(tl, new Size(br.X - tl.X, br.Y - tl.Y));
                    }
                    parseLine(allRangeStrokes, ref substruct, ref sl, ref (nn as DivSym).Super, divs, upDown == -1 ? new Rectangle(new Point(rbox.Left, bounds.Top), new Size(rbox.Width, rbox.Top - bounds.Top)) : rbox, upDown == -1 || (upDown == 0 && !bounds.IsEmpty) ? 0 : 1, nn, inMatrix);
                    rbox = r.bbox;
                    if (upDown == 0 && bounds != Rectangle.Empty) {
                        Point tl = new Point(Math.Max(bounds.Left, rbox.Left), Math.Min(bounds.Bottom, rbox.Bottom));
                        Point br = new Point(Math.Min(bounds.Right, rbox.Right), bounds.Bottom);
                        rbox = new Rectangle(tl, new Size(br.X - tl.X, br.Y - tl.Y));
                    }
                    parseLine(allRangeStrokes, ref substruct, ref sl, ref (nn as DivSym).Sub, divs, upDown == 1 ? new Rectangle(rbox.Location, new Size(rbox.Width, bounds.Bottom - rbox.Top)) : rbox, upDown == 1 || (upDown == 0 && !bounds.IsEmpty) ? 0 : -1, nn, inMatrix);
                    used.Remove(r.guid);
                }
                ArrayList toRemove = new ArrayList();
                bool theInside = inside;
                if (inMatrix) inside = true;
                foreach (Symbol sim in lookahead2) {
                    if (checkLookahead(ref used, sim, nn, inside, ref line))
                        toRemove.Add(sim);
                }
                foreach (Symbol rem in toRemove)
                    lookahead2.Remove(rem);
                
                if (r.alt == '(' && Symbol.ParseParensToMatrices) 
                    parseMatrixEntires(update, allRangeStrokes, ref used, sl, divs, r, ref nn, ref inMatrix, ref inside, theInside);

                bool lineUses;
                if (ForcedLineAssociation != null && div2 == null && !inMatrix)  // upDown != 0 means its more tightly bound to a division/square root than to ForcedLine
                    lineUses = ForcedLineAssociation.Uses(nn, true, true);
                else lineUses = line.Uses(nn, inside, true);
                if (lineUses) {
                    used.Add(r.guid, 0);
                    if (substruct != null)
                        used = substruct;
                }
            }
            if (lookahead2.Count > 0)
                foreach (Symbol sim in lookahead2)
                    checkLookahead(ref used, sim, null, false, ref line);
        }

        private int CheckIfSymbolIsInParseLineBounds(Hashtable used, Line line, ref Rectangle bounds, int upDown, Symbol div2, int leftbounds, bool allInside, List<Symbol> lookahead2, Recognition r, out bool inside) {
            inside = !bounds.IsEmpty || allInside;
            Rectangle nbounds = r.bbox;
            if (upDown != 0) {
                if (nbounds.Right < leftbounds)
                    return 0;  // this symbol comes before the line
                if (nbounds.Left > bounds.Right) {
                    foreach (Symbol l in lookahead2)
                        used.Remove(l.r.guid);
                    return -1; // no more l-r sorted symbols can fall in this range, quit parsing this line
                }
                inside = true;
                bool num = nbounds.Top < bounds.Bottom;
                bool den = nbounds.Bottom > bounds.Top;
                if (num && den) {
                    if ((r.alt == ')' || (num && den && (nbounds.Right - bounds.Right + 0.0) / nbounds.Width > 0.5)) && nbounds.Right > bounds.Right && div2.r.strokes[0].FindIntersections(r.strokes).Length < 1)
                        return 0; // this symbol is not in bounds
                    Rectangle rbounds = r.strokes[0].GetBoundingBox();
                    Point rcenter = new Point(rbounds.Left + rbounds.Width / 2, rbounds.Top + rbounds.Height / 2);
                    Point center = new Point(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);
                    Point off = V2D.Sub(rcenter, center);
                    PointF tangent = V2D.Normalize(V2D.Sub(div2.r.strokes[0].GetPoint(0), div2.r.strokes[0].GetPoint(div2.r.strokes[0].GetPoints().Length - 1)));
                    PointF normal = new PointF(-tangent.Y, tangent.X);
                    if (normal.Y > 0)
                        normal = new PointF(-normal.X, -normal.Y);
                    if (FeaturePointDetector.angle(V2D.Normalize(off), normal) < FeaturePointDetector.angle(V2D.Normalize(off), new PointF(-normal.X, -normal.Y)))
                        den = false;
                    else num = false;
                } else if (!num && !den) {
                    if (nbounds.Right > bounds.Right)
                        return 0; // this symbol is not in bounds
                    float[] ints = div2.r.strokes[0].FindIntersections(r.strokes);
                    if (ints.Length > 0) {
                        int yloc = getPt(ints[0], div2.r.strokes[0].GetPoints()).Y;
                        if (yloc - r.xheight > r.baseline - yloc)
                            num = true;
                        else den = true;
                    } else {
                        if (getPt(div2.r.strokes[0].NearestPoint(nbounds.Location), div2.r.strokes[0].GetPoints()).Y - r.xheight >
                                    r.baseline - getPt(div2.r.strokes[0].NearestPoint(new Point(nbounds.Left, nbounds.Bottom)), div2.r.strokes[0].GetPoints()).Y)
                            num = true;
                        else den = true;
                    }
                }
                if ((num && upDown == -1) || (den && upDown == 1))
                    return 0; // this symbol is not in bounds
                // don't grab numerators that are separated from the division line but have something in between.
                if (num && line.Empty && r.alt.Other != Recognition.Result.Special.Division && r.alt.Character != Unicode.N.N_ARY_SUMMATION &&
                            r.alt != Unicode.I.INTEGRAL && r.alt != Unicode.S.SQUARE_ROOT) {
                    Point botleft = new Point(Math.Max(div2.Bounds.Left, r.bbox.Left),
                        r.bbox.Bottom);
                    Rectangle between = new Rectangle(botleft, new Size(r.bbox.Right - botleft.X, div2.Bounds.Top - r.bbox.Bottom));
                    if (between.Width > 0 && between.Height > 0) {
                        Strokes inBetween = r.strokes.Ink.HitTest(between, 5f);
                        inBetween.Remove(div2.r.strokes);
                        inBetween.Remove(r.strokes);
                        if (inBetween.Count > 0) {
                            int xoverlap = r.bbox.Right - inBetween.GetBoundingBox().Left;
                            int yoverlap = inBetween.GetBoundingBox().Top - r.bbox.Bottom;
                            if (xoverlap / (float)r.bbox.Width > 0.2 && yoverlap/(float)inBetween.GetBoundingBox().Height > 0.5)
                                return 0; // this symbol is not in bounds
                        }
                    }
                }
            } else if (bounds != Rectangle.Empty) {
                if (!line.Empty)
                    bounds = Rectangle.Union(bounds, line.Bounds());
                if (!bounds.IntersectsWith(r.bbox))
                    return 0; // this symbol is not in bounds
                if (div2 is DivSym) {
                    Rectangle inter = Rectangle.Intersect(bounds, r.bbox);
                    if (inter.Height / (r.bbox.Height + 0.0) < (r.bbox.Height / (float)r.bbox.Width > 0.5 ? 0.25 : 0.5))
                        return 0; // this symbol is not in bounds
                }
                if ((r.bbox.Right - bounds.Right + 0.0) / r.bbox.Width > 0.5)
                    return 0; // this symbol is not in bounds
                if (div2 is RootSym) {
                    Ink tmpink = new Ink();
                    tmpink.AddStrokesAtRectangle(div2.r.strokes, div2.r.bbox);
                    Point ctr = new Point((r.bbox.Left + r.bbox.Right) / 2, (r.bbox.Top + r.bbox.Bottom) / 2);
                    tmpink.CreateStroke(new Point[] { ctr, new Point(ctr.X, ctr.Y - 1000 * 26) });
                    //int mid = (leftParen[i].strokes.GetBoundingBox().Top + leftParen[i].strokes.GetBoundingBox().Bottom) / 2;
                    int intersects = tmpink.Strokes[tmpink.Strokes.Count - 1].FindIntersections(tmpink.CreateStrokes(new int[] { tmpink.Strokes[0].Id })).Length;
                    //int botind, topind;
                    //FeaturePointDetector.maxy(0, pr.strokes[0].GetPoints().Length, pr.strokes[0].GetPoints(), out botind);
                    //FeaturePointDetector.miny(0, pr.strokes[0].GetPoints().Length, pr.strokes[0].GetPoints(), out topind);
                    if (intersects == 1)
                        return 0; // this symbol is not in bounds
                }
            }
            return 1;
        }

        private void ProcessPreParsedSymbols(Strokes allRangeStrokes, ref Hashtable used, ArrayList divs, bool inMatrix, Recognition r, Symbol nn) {
            // CJ: this section checks to see if we have any pre-parsed superscripts waiting for this 
            //     symbol (e.g., we now have a summation and we want to add all the superscript symbols
            //     that preceeded the summation)
            if (superPreParsed[r.guid] != null && superPreParsed[r.guid] is List<Recognition>) {
                List<Recognition> preRecList = (List<Recognition>)superPreParsed[r.guid];
                SortedList sl2 = new SortedList();
                foreach (Recognition pr in preRecList) {
                    sl2.Add(pr.bbox.Left, pr);
                    pr.SuperId = -1;
                    used.Remove(pr.guid);
                }
                superPreParsed[r.guid] = nn;
                parseLine(allRangeStrokes, ref used, ref sl2, ref nn.Super, divs, Rectangle.Empty, 0, null, inMatrix);
                foreach (Recognition pr in preRecList)
                    pr.SuperId = r.strokes[0].Id;
            }
            if (subPreParsed[r.guid] != null && subPreParsed[r.guid] is List<Recognition>) {//has waiting unparsed subscripts
                List<Recognition> preRecList = (List<Recognition>)subPreParsed[r.guid];// retrieve subscript recognitions
                SortedList sl2 = new SortedList();
                foreach (Recognition pr in preRecList) {
                    sl2.Add(pr.bbox.Left, pr); // sort them
                    pr.SubId = -1; // remove guid for regular parsing by parseLine()
                    used.Remove(pr.guid);
                }
                subPreParsed[r.guid] = nn;
                parseLine(allRangeStrokes, ref used, ref sl2, ref nn.Sub, divs, Rectangle.Empty, 0, null, inMatrix);
                foreach (Recognition pr in preRecList) {
                    pr.SubId = r.strokes[0].Id;  // Associate them with the summation
                }
            }
            if (superIntegralPreParsed[r.guid] != null && superIntegralPreParsed[r.guid] is List<Recognition>) {
                List<Recognition> preIntegralRecList = (List<Recognition>)superIntegralPreParsed[r.guid];
                SortedList sl2 = new SortedList();
                foreach (Recognition pr in preIntegralRecList) {
                    sl2.Add(pr.bbox.Left, pr);
                    pr.SuperIntegralId = -1;
                    used.Remove(pr.guid);
                }
                superIntegralPreParsed[r.guid] = nn;
                parseLine(allRangeStrokes, ref used, ref sl2, ref nn.Super, divs, Rectangle.Empty, 0, null, inMatrix);
                foreach (Recognition pr in preIntegralRecList)
                    pr.SuperIntegralId = r.strokes[0].Id;
            }
            if (subIntegralPreParsed[r.guid] != null && subIntegralPreParsed[r.guid] is List<Recognition>) {
                List<Recognition> preIntegralRecList = (List<Recognition>)subIntegralPreParsed[r.guid];// retrieve subscript recognitions
                SortedList sl2 = new SortedList();
                foreach (Recognition pr in preIntegralRecList) {
                    sl2.Add(pr.bbox.Left, pr); // sort them
                    pr.SubIntegralId = -1; // remove guid for regular parsing by parseLine()
                    used.Remove(pr.guid);
                }
                subIntegralPreParsed[r.guid] = nn;
                parseLine(allRangeStrokes, ref used, ref sl2, ref nn.Sub, divs, Rectangle.Empty, 0, null, inMatrix);
                foreach (Recognition pr in preIntegralRecList)
                    pr.SubIntegralId = r.strokes[0].Id;  // Associate them with the summation
            }
            superPreParsed[r.guid]         = nn;// store Symbol, not List<Recognition> to indicate that its super/subscripts have been parsed
            subPreParsed[r.guid]           = nn;
            superIntegralPreParsed[r.guid] = nn;
            subIntegralPreParsed[r.guid]   = nn;
        }

        private bool CheckIfSymbolIsPreParsed(Strokes allRangeStrokes, Hashtable used, Recognition r, Symbol div2, out Line ForcedLineAssociation) {
            ForcedLineAssociation = null;
            //CJ: This section checks the first stroke of a Recognition to see if it is a pre-parsed Superscript
            if (r.SuperId != -1) {
                int id = r.SuperId;

                bool foundReference = false;
                foreach (Stroke fs in allRangeStrokes)
                    if (fs.Id == id) {
                        foundReference = true;
                        break;
                    }
                if (foundReference) {

                    Guid refRecGuid = Guid.Empty;
                    foreach (Stroke fs in r.strokes[0].Ink.Strokes)
                        if (fs.Id == id) {
                            refRecGuid = Charreco.Classification(fs).guid;
                            break;
                        }
                    // if the reference symbol (ie the summation) is hashed to a symbol, then we've already
                    // processed it and so we just need to send the superscript to the reference symbol's
                    // superscript Line
                    if (!(superPreParsed[refRecGuid] is Symbol)) { // otherwise, the reference symbol hasn't been processed so we need to
                        // add the preparsed superscript to a "waiting" list for when the symbol is parsed
                        List<Recognition> preRecList = (List<Recognition>)superPreParsed[refRecGuid];
                        if (preRecList == null)
                            preRecList = new List<Recognition>();
                        preRecList.Add(r);
                        if (!superPreParsed.Contains(refRecGuid))//CJ: no need to update after addition of r
                            superPreParsed.Add(refRecGuid, preRecList);
                        used.Add(r.guid, 0);
                        return true;
                    } else if (superPreParsed[refRecGuid] is IntSym && (div2 == null || div2.r.SuperId != id)) {
                        ForcedLineAssociation = (superPreParsed[refRecGuid] as IntSym).Super;
                    }
                }
            }
            if (r.SubId != -1) { // subscript detected
                int id = r.SubId;// retrieve Id of the associated summation

                bool foundReference = false;
                foreach (Stroke fs in allRangeStrokes)
                    if (fs.Id == id) {
                        foundReference = true;
                        break;
                    }
                if (foundReference) {
                    Guid refRecGuid = Guid.Empty;
                    foreach (Stroke fs in r.strokes[0].Ink.Strokes) // search all strokes
                        if (fs.Id == id) {
                            refRecGuid = Charreco.Classification(fs).guid;  // find out the guid of the summation Recognition
                            break;
                        }
                    if (!(subPreParsed[refRecGuid] is Symbol)) {// current stroke precedes its reference (summation).
                        List<Recognition> preRecList = (List<Recognition>)subPreParsed[refRecGuid]; // retrieve Recognitions of subscripts
                        if (preRecList == null)
                            preRecList = new List<Recognition>();
                        preRecList.Add(r); // add current subscript to other subscripts
                        if (!subPreParsed.Contains(refRecGuid))
                            subPreParsed.Add(refRecGuid, preRecList); //CJ: add the first Recognition, no updates? 
                        used.Add(r.guid, 0);
                        return true;    // this Recognition is done.
                    } else if (subPreParsed[refRecGuid] is IntSym && (div2 == null || div2.r.SubId != id)) {
                        ForcedLineAssociation = (subPreParsed[refRecGuid] as IntSym).Sub;
                    }
                }
            }
            // for integral temporal parsing
            if (r.SuperIntegralId != -1) {
                int id = r.SuperIntegralId; // retrieve Id of the associated summation

                bool foundReference = false;
                foreach (Stroke fs in allRangeStrokes)
                    if (fs.Id == id) {
                        foundReference = true;
                        break;
                    }
                if (foundReference) {
                    Guid refIntegralRecGuid = Guid.Empty;
                    foreach (Stroke fs in r.strokes[0].Ink.Strokes)
                        if (fs.Id == id) {
                            refIntegralRecGuid = Charreco.Classification(fs).guid;
                            break;
                        }

                    if (!(superIntegralPreParsed[refIntegralRecGuid] is Symbol)) {
                        List<Recognition> preIntegralRecList = (List<Recognition>)superIntegralPreParsed[refIntegralRecGuid];
                        if (preIntegralRecList == null)
                            preIntegralRecList = new List<Recognition>();
                        preIntegralRecList.Add(r);
                        if (!superIntegralPreParsed.Contains(refIntegralRecGuid))
                            superIntegralPreParsed.Add(refIntegralRecGuid, preIntegralRecList);
                        used.Add(r.guid, 0);
                        return true;
                    } else if (superIntegralPreParsed[refIntegralRecGuid] is IntSym && (div2 == null || div2.r.SuperIntegralId != id)) {
                        ForcedLineAssociation = (superIntegralPreParsed[refIntegralRecGuid] as IntSym).Super;
                    }
                }
            }
            if (r.SubIntegralId != -1) {
                int id = r.SubIntegralId;// retrieve Id of the associated summation

                bool foundReference = false;
                foreach (Stroke fs in allRangeStrokes)
                    if (fs.Id == id) {
                        foundReference = true;
                        break;
                    }
                if (foundReference) {
                    Guid refIntegralRecGuid = Guid.Empty;
                    foreach (Stroke fs in r.strokes[0].Ink.Strokes)
                        if (fs.Id == id) {
                            refIntegralRecGuid = Charreco.Classification(fs).guid;
                            break;
                        }
                    if (!(subIntegralPreParsed[refIntegralRecGuid] is Symbol)) {
                        List<Recognition> preIntegralRecList = (List<Recognition>)subIntegralPreParsed[refIntegralRecGuid];
                        if (preIntegralRecList == null)
                            preIntegralRecList = new List<Recognition>();
                        preIntegralRecList.Add(r);
                        if (!subIntegralPreParsed.Contains(refIntegralRecGuid))
                            subIntegralPreParsed.Add(refIntegralRecGuid, preIntegralRecList);
                        used.Add(r.guid, 0);
                        return true;
                    } else if (subIntegralPreParsed[refIntegralRecGuid] is IntSym && (div2 == null || div2.r.SubIntegralId != id)) {
                        ForcedLineAssociation = (subIntegralPreParsed[refIntegralRecGuid] as IntSym).Sub;
                    }
                }
            }
            return false;
        }

        private void collectMatrixStrokes(Strokes update, SortedList sl) {
            if (opSyms == null||opSyms.Count == 0) return;

            Recognition rrr = null;
            Rectangle lbox = new Rectangle();
            Rectangle curbox = new Rectangle();
            Rectangle pbox = new Rectangle();
            List<Recognition> lparenMatch = new List<Recognition>();
            List<Stroke> lparenMatchRparen = new List<Stroke>();
            List<int> lparenListIndex = new List<int>();
            List<Rectangle> lparenBox = new List<Rectangle>();
            List<Rectangle> lparenMatchRparenBox = new List<Rectangle>();
            int lparenIndex = -1;
            foreach (Recognition r in sl.GetValueList()) {
                lparenIndex++;
                curbox = r.bbox;
                r.MatrixId = -1; // clear off any associated MatrixID ... things could be different this time around

                if (r.alt == '(') {
                    leftParen.Add(r); // leftParen is sorted since sl is sorted
                    lparenMatch.Add(null);
                    lparenMatchRparen.Add(null);
                    lparenMatchRparenBox.Add(Rectangle.Empty);
                    lparenBox.Add(curbox);
                    lparenListIndex.Add(lparenIndex);
                    Strokes myStrokes = r.strokes.Ink.CreateStrokes();//container of matrix strokes for this paren
                    matrixStrokes.Add(myStrokes);
                    Strokes mydots = r.strokes.Ink.CreateStrokes();//container of dots strokes for this paren
                    dots.Add(mydots);
                    
                    matrixLparenInds.Add(r.strokes[0].Id, matrixStrokes.Count-1);
                    if(update != null) rrr = Charreco.Classification(update[0]);
                    if(update == null || rrr == null||rrr.alt == '('||rrr.alt == ')'){ // rrr == null for stroke deletion
                        List<Range> newMatrixRanges = new List<Range>();
                        matrixRanges.Add(newMatrixRanges); // updated in UpdateRanges(...)
                    } else if (matrixRanges.Count < leftParen.Count) {
                        List<Range> newMatrixRanges = new List<Range>();
                        matrixRanges.Add(newMatrixRanges);              
                    }
                } else if (r.alt == ')') {
                    if (leftParen.Count > 0)
                        for (int i = leftParen.Count - 1; i >= 0; i--) {// to find matching left paren, starting from the rightmost one
                            lbox = lparenBox[i];
                            if (curbox.Right > lbox.Left &&                                                               // right paren at the right of left paren
                                lparenMatch[i] == null &&                                                                 //left paren not matched yet
                                curbox.Bottom > (lbox.Top + lbox.Bottom) / 2 &&                                           //size and vertical location test
                                curbox.Top < (lbox.Top + lbox.Bottom) / 2 &&                                              //size and vertical location test
                                lbox.Bottom > (curbox.Top + curbox.Bottom) / 2 &&                                           //size and vertical location test
                                lbox.Top < (curbox.Top + curbox.Bottom) / 2) {                                              //size and vertical location test
                                // the above criteria are too loose and can miss the best one

                                lparenMatch[i] = r;
                                lparenMatchRparen[i] = r.strokes[0];
                                lparenMatchRparenBox[i] = curbox;
                                if (opSyms != null)
                                    foreach (ParenSym ps in opSyms)
                                        if (leftParen[i].strokes[0].Id == ps.r.strokes[0].Id) {
                                            ps.Closing = Symbol.From(Charreco, r);
                                            break;
                                        }
                                break;
                            }
                        }
                    rightParen.Add(r);
                }
            }

            List<int> unclosedParens = new List<int>();  // running list of unclosed '(' that have a nearby symbol not contained in parenthesis boundary
            int slindex = -1;
            if (leftParen.Count > 0) foreach (Recognition r in sl.GetValueList()) {
                slindex++;
                curbox = r.bbox;
                int k = -1;
                int toLP = Int32.MaxValue;//distance to the nearest leftParen
                bool rightmost = false;
                for (int i = 0; i < leftParen.Count; i++) if (!unclosedParens.Contains(i)) {
                        lparenIndex = lparenListIndex[i];
                        if (lparenIndex > slindex)
                            continue;
                        lbox = lparenBox[i];
                        double containRatio = (Math.Min(lbox.Bottom, curbox.Bottom) - Math.Max(lbox.Top, curbox.Top)) / (double)curbox.Height;
                        bool closed = lparenMatch[i] != null;
                        Stroke rp = null;
                        if (closed) {
                            rp = lparenMatchRparen[i];
                            Rectangle rpbox = lparenMatchRparenBox[i];
                            lbox = Rectangle.Union(lbox, new Rectangle(new Point(lbox.Left, rpbox.Top),
                                new Size(1, rpbox.Height)));
                        }
                        if (slindex > lparenIndex && curbox.Bottom > lbox.Top && curbox.Top < lbox.Bottom && // collect even minimally contained strokes
                                (containRatio > 0.25 || (closed && r.alt.Other != Recognition.Result.Special.Division && r.alt != Unicode.S.SQUARE_ROOT))) { // unless the parenthesis isn't closed -- then apply stricter bounds since it might not really be an '('
                            if (curbox.Left - lbox.Left <= toLP) {  //find the closest left paren
                                if (closed) { //if closed
                                    Recognition pr = lparenMatch[i];
                                    pbox = lparenMatchRparenBox[i];
                                    if (pbox.Right == curbox.Right || (rp != null && rp == r.strokes[0])) {
                                        if (leftParen[i].MatrixId == -1)
                                            rightmost = true; //the rightmost closing paren is not tagged with a MatrixId, and not included in matrixStrokes
                                    } else if (pbox.Right > curbox.Right) {  //in the right range
                                        if (!curbox.IntersectsWith(pbox)) {
                                            k = i;
                                            toLP = curbox.Left - lbox.Left;
                                        } else {
                                            Ink tmpink = new Ink();
                                            tmpink.AddStrokesAtRectangle(pr.strokes, pbox);
                                            Point ctr = new Point((curbox.Left + curbox.Right) / 2, (curbox.Top + curbox.Bottom) / 2);
                                            tmpink.CreateStroke(new Point[] { ctr, V2D.Add(ctr, new Point(1000 * 26, 0)) });
                                            int mid = (leftParen[i].strokes.GetBoundingBox().Top + leftParen[i].strokes.GetBoundingBox().Bottom) / 2;
                                            int intersects = tmpink.Strokes[tmpink.Strokes.Count - 1].FindIntersections(tmpink.CreateStrokes(new int[] { tmpink.Strokes[0].Id })).Length;
                                            int botind, topind;
                                            FeaturePointDetector.maxy(0, pr.strokes[0].GetPoints().Length, pr.strokes[0].GetPoints(), out botind);
                                            FeaturePointDetector.miny(0, pr.strokes[0].GetPoints().Length, pr.strokes[0].GetPoints(), out topind);
                                            if (intersects > 0 || (ctr.Y > mid && pr.strokes[0].GetPoint(botind).X > ctr.X) ||
                                                    (ctr.Y < mid && pr.strokes[0].GetPoint(topind).X > ctr.X)) {
                                                k = i;
                                                toLP = curbox.Left - lbox.Left;
                                            }
                                        }
                                    }
                                } else {//if not closed
                                    bool firstEntry = true;
                                    if (firstEntry) {//since sl is sorted, the first entry of each row should have been collected before strokes for the second entry are to be collected.
                                        k = i;
                                        toLP = curbox.Left - lbox.Left;
                                    }
                                }
                            }
                        } else if (!closed && curbox.Left > lbox.Left && (curbox.Top > lbox.Top && curbox.Top < lbox.Bottom + lbox.Height))
                            unclosedParens.Add(i); // we've found a stroke that isn't part of an unclosed paren -- so stop accumulating contained entries since it might be a symbol rec error
                        if (rightmost) break;
                    }
                if (k != -1 && !rightmost) {
                    r.MatrixId = leftParen[k].strokes[0].Id;// update leftParen reference. for a closing paren, it's the reference left paren, not the matching open paren
                    foreach (Stroke rs in r.strokes) {
                        if (!matrixStrokes[k].Contains(rs))
                            matrixStrokes[k].Add(rs);
                        if (".:⋰⋱⋯⋮".IndexOf(r.allograph[0]) != -1) 
                            dots[k].Add(rs);                       
                    }
                }
            }
        }


        private void parseMatrixEntires(Strokes update, Strokes allRangeStrokes, ref Hashtable used, SortedList sl, ArrayList divs, Recognition r, ref Symbol nn, ref bool inMatrix, ref bool inside, bool theInside) {
 
            ParenSym parenSym = (ParenSym)nn;
            inMatrix = true;
            bool dotsOnly = true;
            int k = (int)matrixLparenInds[r.strokes[0].Id];
            //copying ink and then pasting it back to the panel will resulting in update == all strokes, a superset of matrixStrokes[k]!
            if (update != null && update.GetBoundingBox().Contains(matrixStrokes[k].GetBoundingBox())) update = matrixStrokes[k];
            //update closing paren for parenSym if closed as cached in opSyms
            int opInd = 0;
            for (opInd = 0; opInd < opSyms.Count; opInd++)
                if (opSyms[opInd].r.strokes[0].Id == r.strokes[0].Id){
                    if (opSyms[opInd].Closing != null)   {
                        parenSym.Closing = opSyms[opInd].Closing;
                        parenSym.Closed = true;
                    }
                    break;
                }
            //if update is outside the matrix
            double inflateFactor = 0.1;
            if (update != null && !update.GetBoundingBox().IsEmpty &&
                ((!parenSym.Closed && (update.GetBoundingBox().Left < parenSym.Bounds.Left ||
                update.GetBoundingBox().Bottom < (parenSym.Bounds.Top - inflateFactor * parenSym.Bounds.Height) ||
                update.GetBoundingBox().Top > (parenSym.Bounds.Bottom + inflateFactor * parenSym.Bounds.Height))) ||
                (parenSym.Closed && (update.GetBoundingBox().Left < parenSym.Bounds.Left ||
                update.GetBoundingBox().Bottom < (parenSym.Bounds.Top - inflateFactor * (parenSym.Bounds.Height + parenSym.Closing.TotalBounds.Height) / 2) ||
                update.GetBoundingBox().Top > (parenSym.Bounds.Bottom + inflateFactor * (parenSym.Bounds.Height + parenSym.Closing.TotalBounds.Height) / 2) ||
                update.GetBoundingBox().Left > parenSym.Closing.TotalBounds.Left)))) {
                if (update != null && !(matrixStrokes[k].Contains(update[0]))) {
                   /* foreach (Stroke s in matrixStrokes[k]) {
                        Recognition rr = FeaturePointDetector.Classification(s);
                        if (rr.alt == '.') dots[k].Remove(s);
                        FeaturePointDetector.Classification(s).MatrixId = parenSym.r.strokes[0].Id;
                        if (!used.Contains(rr.guid)) used.Add(rr.guid, 0);
                    }*/
                    nn = opSyms[opInd]; //reference to a different object!
                    //if (dots[k].Count > 0) nn = handleDots((ParenSym)nn, dots[k], k, ref used);
                    inside = theInside;
                    return;
                }
            }
            if (matrixStrokes[k].Count == 0){
                opSyms[opInd].rows.Clear();
                opSyms[opInd].lines.Clear();
                return;
            }

            Strokes updateStrokes = null;
            List<Range> updatedRanges = new List<Range>();
            /*
            // remove decimal points from dots[k]
            foreach (Stroke dot in dots[k])
                for (int i = 0; i < matrixRanges[k].Count; i++) {
                    if (!(dot.GetBoundingBox().IntersectsWith(matrixRanges[k][i].Bounds))) continue;
                    if (matrixRanges[k][i].Strokes.Count > 3) { dots[k].Remove(dot); break; }
                    if (matrixRanges[k][i].Strokes.Count == 1) break;
                    for (int j = 0; j < matrixRanges[k][i].Strokes.Count; j++) {
                        Recognition rr = FeaturePointDetector.Classification(matrixRanges[k][i].Strokes[j]);
                        if (".:⋰⋱⋯⋮".IndexOf(rr.allograph[0]) == -1) { dots[k].Remove(dot); break; }
                    }
                }
            // if the strokes are added to updatedStrokes, selecting and moving outside strokes into a matrix would be in trouble: 
            // "Selected" strokes become the total, and "moving" matrix entries to outside when outside strokes begin to join the matrix.
            Strokes outsiders = (update==null)?null:update.Ink.CreateStrokes(); 
            
            Recognition rrr = null;
            if (update != null) rrr = FeaturePointDetector.Classification(update[0]);
            if (update == null || rrr == null || rrr.alt == '(' || rrr.alt == ')') {
                updateStrokes = matrixStrokes[k];
                opSyms[opInd].lines.Clear();
                opSyms[opInd].rows.Clear();
            }
            else {
                int k1 = 0;
                for (int i = 0; i < matrixRanges[k].Count; i++)
                    if (matrixRanges[k].Count > 0) k1 += matrixRanges[k][i].Strokes.Count;
                if (k1 + update.Count == matrixStrokes[k].Count)
                    updateStrokes = update;
                else {   //there are outsiders
                    updateStrokes = update;
                    foreach (Stroke s in matrixStrokes[k]) {
                        bool isFound = false;
                        if (updateStrokes.Contains(s)) continue;
                        for (int i = 0; i < matrixRanges[k].Count; i++)
                            if (matrixRanges[k].Count > 0 && matrixRanges[k][i].Strokes.Contains(s)) {
                                isFound = true;
                                break;
                            }
                        if (!isFound) outsiders.Add(s);
                    }
                }
            }
            */
            updateStrokes = matrixStrokes[k];
            int rangeCount = 0;
/*
            if (opSyms[opInd].lines.Count < 2) {
                //matrixRanges[k].Clear();
                rangeCount = UpdateRanges(updateStrokes, matrixStrokes[k], k, false);                             
            }
            
            if (matrixRanges[k].Count > 1 || opSyms[opInd].lines.Count > 1) { 
                //if (opSyms[opInd].lines.Count == 1) {//there are too many issues associated with caching entries for subsequent use, just parsing all entries for each update might be slow for large matrix, yet is less error prone.
                    matrixRanges[k].Clear();
                    opSyms[opInd].lines.Clear();
                    opSyms[opInd].rows.Clear();
               // }
                rangeCount = UpdateRanges(updateStrokes, matrixStrokes[k], k, true);                
            }
            else if (matrixRanges[k].Count == 1) {
                updatedRanges = null; 
                matrixRanges[k].Clear();
                opSyms[opInd].lines.Clear();
                opSyms[opInd].rows.Clear();
                rangeCount = UpdateRanges(updateStrokes, matrixStrokes[k], k, true);
                if (matrixRanges[k].Count > 1) {
                    int top = matrixRanges[k][0].Bounds.Top;
                    int bot = matrixRanges[k][0].Bounds.Bottom;
                    for (int i = 1; i < matrixRanges[k].Count; i++) 
                        if (matrixRanges[k][i].Bounds.Top > (top + bot) / 2 || matrixRanges[k][i].Bounds.Bottom < (top + bot) / 2) {
                            updatedRanges = matrixRanges[k];
                            break;
                        }                    
                }
            } 
*/
            
            bool hasOneRow = true;
            matrixRanges[k].Clear();
            opSyms[opInd].lines.Clear();
            opSyms[opInd].rows.Clear();
            rangeCount = UpdateRanges(updateStrokes, matrixStrokes[k], k, true);
            
            if (matrixRanges[k].Count > 1) {              
                int top = matrixRanges[k][0].Bounds.Top;
                int bot = matrixRanges[k][0].Bounds.Bottom;
                for (int i = 1; i < matrixRanges[k].Count; i++)
                    if (matrixRanges[k][i].Bounds.Top > (top + 2*bot) / 3 || matrixRanges[k][i].Bounds.Bottom < (2*top + bot) / 3) {
                        updatedRanges = matrixRanges[k];
                        hasOneRow = false;
                        break;
                    }
            } 
            if (hasOneRow) {
                for (int i = 0; i < matrixRanges[k].Count; i++) {
                    updatedRanges.Add(new Range(Charreco, matrixRanges[k][i].Strokes, r.strokes.Ink.CreateStrokes(), r.strokes.Ink.CreateStrokes()));
                    updatedRanges[i].hasArrowInMatrixEntry = matrixRanges[k][i].hasArrowInMatrixEntry;
                }
                matrixRanges[k].Clear();
                opSyms[opInd].lines.Clear();
                opSyms[opInd].rows.Clear();
                rangeCount = UpdateRanges(updateStrokes, matrixStrokes[k], k, false);
                if (matrixRanges[k].Count == 1)
                    updatedRanges = matrixRanges[k];
            }


            //if (outsiders != null && outsiders.Count > 0)
            //    rangeCount = Math.Min(rangeCount + UpdateRanges(outsiders, matrixStrokes[k], k, true), matrixRanges[k].Count); // this is a shortcut. room for improvement if program is too slow for large matrices
            
            //if (update == null || rrr == null || rrr.alt == '(' || rrr.alt == ')' || matrixRanges[k].Count < opSyms[opInd].lines.Count /*for entry merge*/){
                //updatedRanges = matrixRanges[k];
            /*} else {
                int totalCount = matrixRanges[k].Count;
                for (int i =  0; i < rangeCount; i++)
                    updatedRanges.Add(matrixRanges[k][totalCount - 1 - i]);
                parenSym = opSyms[opInd];
            }*/
            //parsing non-dots entries
            for ( int i = 0; i < updatedRanges.Count; i++) {
                foreach (Stroke s in updatedRanges[i].Strokes) {
                    Recognition rr = Charreco.Classification(s);
                    if (rr == null) continue;

                    if (rr.levelsetby > 1) {
                        rr.levelsetby = int.MaxValue;
                        rr.curalt = 0;
                    } else if (rr.levelsetby < 0)
                        rr.levelsetby = -rr.levelsetby;
                    rr.parseError = false;
                } 
                // collect isolated dots for postprocess
                dotsOnly = true;
                foreach (Stroke sss in updatedRanges[i].Strokes) {
                    Recognition rr = Charreco.Classification(sss);
                    if (rr == null || ".:⋰⋱⋯⋮".IndexOf(rr.allograph[0]) == -1)
                        dotsOnly = false;
                }
                if (dotsOnly) continue;                
                
                SortedList sl3 = new SortedList();
                foreach (Stroke sss in updatedRanges[i].Strokes) {
                    Recognition rr = Charreco.Classification(sss);
                    if(rr == null) continue;
                    //int key = sss.GetBoundingBox().Left;
                    //while (sl3.Contains(key))
                    //    key--;
                    //sl3.Add(key, sss);
                    rr.MatrixId = -1;
                    if (used.Contains(rr.guid))
                        used.Remove(rr.guid);
                }
                IList skeys = sortedRecs.GetKeyList(); // use sort information from sl to determine sort order for this matrix range
                bool isArrowEntry = updatedRanges[i].hasArrowInMatrixEntry;
                int id = 0;
                foreach (Stroke ms in updatedRanges[i].Strokes) {
                    Recognition mr = Charreco.Classification(ms);
                    id = sortedRecs.IndexOfValue(mr);
                    if (id < 0) {
                        updatedRanges.RemoveAt(i);
                        break;
                    }
                    if (!sl3.ContainsKey(skeys[id]))
                        sl3.Add(skeys[id], mr);
                }
                if (id < 0) continue;
                Line entryLine = new Line();
                parseLine(allRangeStrokes, ref used, ref sl3, ref entryLine, divs, Rectangle.Empty, 0, null, inMatrix);// parse sorted range and store result at rangeLine
                insertNewEntry(parenSym, entryLine, isArrowEntry);

                foreach (Stroke sss in updatedRanges[i].Strokes)// tag them back
                    Charreco.Classification(sss).MatrixId = parenSym.r.strokes[0].Id;
            }
            //If ParenSym had a Clone() method, it would be easier to obtain a deep copy.
            if (dots[k].Count > 0) nn = handleDots(parenSym, dots[k], k, ref used);
            else nn = parenSym;
            String tmp = opSyms[opInd].matrixOp;
            opSyms[opInd] = new ParenSym(Charreco, parenSym.r);
            opSyms[opInd].matrixOp = tmp;
            for (int i = 0; i < parenSym.rows.Count; i++) {
                List<Line> newRow = new List<Line>();
                for (int j = 0; j < parenSym.rows[i].Count; j++)
                    newRow.Add(parenSym.rows[i][j]);
                opSyms[opInd].rows.Add(newRow);
            }
            for (int i = 0; i < parenSym.lines.Count; i++)
                opSyms[opInd].lines.Add(parenSym.lines[i]);
            opSyms[opInd].Closing = parenSym.Closing;
            if (opSyms[opInd].Closing != null) opSyms[opInd].Closed = true;
            opSyms[opInd].ArrowID = parenSym.ArrowID;

            inside = theInside;
        }

        private ParenSym handleDots(ParenSym theParenSym, Strokes dots, int k, ref Hashtable used) {

            // remove decimal points from dots[k]
            foreach (Stroke dot in dots)
                for (int i = 0; i < matrixRanges[k].Count; i++) {
                    if (!(dot.GetBoundingBox().IntersectsWith(matrixRanges[k][i].Bounds))) continue;
                    if (matrixRanges[k][i].Strokes.Count > 3) { dots.Remove(dot); break; }
                    if (matrixRanges[k][i].Strokes.Count == 1) break;
                    for (int j = 0; j < matrixRanges[k][i].Strokes.Count; j++) {
                        Recognition rr = Charreco.Classification(matrixRanges[k][i].Strokes[j]);
                        if (".:⋰⋱⋯⋮".IndexOf(rr.allograph[0]) == -1) { dots.Remove(dot); break; }
                    }
                }
            if (dots.Count == 0) return theParenSym;


            Line ellipsis = new Line();
            //if (dots.Count < 3) return; // no isolated dots are supported
            double[,] dist = new double[dots.Count, dots.Count];
            double maxdist = 0;
            double temp0 = 0, temp1 = 0;
            int Min0 = 0, Min1 = 0;
            bool[] isUsed = new bool[dots.Count];
            for (int i = 0; i < dots.Count; i++) {
                for (int j = 0; j < dots.Count; j++) {
                    dist[i, j] = dist2(dots[i], dots[j]);
                    maxdist = Math.Max(maxdist, dist[i, j]);
                }
                isUsed[i] = false;
            }

            // Note: the code in FeaturePointDetector.cs added for ellipsis is bypassed by handling of dots below.
            // assuming dots.Count is a multiple of 3, and dots for an ellipsis are closer to one another than to other dots.
            // if not a multiple of 3, one dot will be "borrowed" temporarily???
            for (int i = 0; i < dots.Count; i++) {

                if (isUsed[i]) continue;
                temp0 = maxdist;
                temp1 = maxdist;
                for (int j = 0; j < dots.Count; j++) {
                    if (dist[i, j] > 0 && dist[i, j] <= temp0) {
                        temp0 = dist[i, j];
                        Min0 = j;
                    }
                }
                for (int j = 0; j < dots.Count; j++) {
                    if (dist[i, j] > temp0 && dist[i, j] <= temp1) {
                        temp1 = dist[i, j];
                        Min1 = j;
                    }
                }
                //if we can assume that a dot stroke is closer to another dot stroke than to a non-dot stroke, then
                //Ink.NearestPoint(Pt) can be used to find the nearest dots
                Point[] hull = new Point[] { dots[i].GetPoint(0), dots[Min0].GetPoint(0), dots[Min1].GetPoint(0) };
                if (dots.Ink.HitTest(hull, 1).Count > 3) continue;

                double ang1 = theAngle(dots[i], dots[Min0]);
                double ang2 = theAngle(dots[i], dots[Min1]);
                double ang3 = theAngle(dots[Min1], dots[Min0]);
                double best = (ang1 + ang2 + ang3) / 3;
                String _allograph = "";

                if ((ang1 > 67.5 && ang1 <= 112.5) && (ang2 > 67.5 && ang2 <= 112.5) && (ang3 > 67.5 && ang3 <= 112.5))
                    _allograph = "⋯";
                else if ((ang1 <= 22.5 || ang1 > 157.5) && (ang2 <= 22.5 || ang2 > 157.5) && (ang3 <= 22.5 || ang3 > 157.5))
                    _allograph = "⋮";
                else if ((ang1 > 22.5 && ang1 <= 67.5) && (ang2 > 22.5 && ang2 <= 67.5) && (ang3 > 22.5 && ang3 <= 67.5))
                    _allograph = "⋰";
                else if ((ang1 > 112.5 && ang1 <= 157.5) && (ang2 > 112.5 && ang2 <= 157.5) && (ang3 > 112.5 && ang3 <= 157.5))
                    _allograph = "⋱";
                else if (best > 67.5 && best <= 112.5)
                    _allograph = "⋯";
                else if (best <= 22.5 || best > 157.5)
                    _allograph = "⋮";
                else if (best > 22.5 && best <= 67.5)
                    _allograph = "⋰";
                else
                    _allograph = "⋱";

                // add two "empty" dots so that the ellipsis has a proper bounding box
                // parseLine(...) does not work if the dots are far away from one another
                ellipsis._syms.Add(new Symbol(Charreco, new Recognition(dots[i], _allograph, dots[i].GetBoundingBox().Bottom, false)));
                ellipsis._syms.Add(new Symbol(Charreco, new Recognition(dots[Min0], "", dots[Min0].GetBoundingBox().Bottom, false)));
                ellipsis._syms.Add(new Symbol(Charreco, new Recognition(dots[Min1], "", dots[Min1].GetBoundingBox().Bottom, false)));
                

                insertNewEntry(theParenSym, ellipsis, false);
                ellipsis = new Line();              
                isUsed[i] = true;
                isUsed[Min0] = true;
                isUsed[Min1] = true;
                Recognition r1 = Charreco.Classification(dots[i]);
                Recognition r2 = Charreco.Classification(dots[Min0]);
                Recognition r3 = Charreco.Classification(dots[Min1]);
                Charreco.Classification(dots[i]).MatrixId = theParenSym.r.strokes[0].Id;
                Charreco.Classification(dots[Min0]).MatrixId = theParenSym.r.strokes[0].Id;
                Charreco.Classification(dots[Min1]).MatrixId = theParenSym.r.strokes[0].Id;
                if (!used.Contains(r1.guid)) used.Add(r1.guid, 0);
                if (!used.Contains(r2.guid)) used.Add(r2.guid, 0);
                if (!used.Contains(r3.guid)) used.Add(r3.guid, 0);
            }
            return theParenSym;
        }

        private static double theAngle(Stroke s1, Stroke s2) {
            Point ctr1 = s1.GetPoint(0);
            Point ctr2 = s2.GetPoint(0);
            Point vec;
            if (ctr1.X < ctr2.X)
                vec = V2D.Sub(ctr1, ctr2);
            else
                vec = V2D.Sub(ctr2, ctr1);
            return FeaturePointDetector.angle(new Point(0, 1), vec);
        }

        private static double dist2(Stroke s1, Stroke s2) {
            return Math.Pow((s1.GetPoint(0).X - s2.GetPoint(0).X), 2.0) + Math.Pow((s1.GetPoint(0).Y - s2.GetPoint(0).Y), 2.0);
        }


        // elements in non-well-formed matrices are adjusted in Expr ConvertSymToExpr(Symbol) of Parse2
        // Search for "colBoxes" or "non-well-formed matrices" to locate the code
        private void insertNewEntry(ParenSym parenSym, Line entryLine, bool isArrowEntry) {
            // remove cached entries being hit
/*            for (int rr = 0; rr < (parenSym.ArrowID != -1 ? parenSym.rows.Count - 1 : parenSym.rows.Count); rr++) {
                for (int k = 0; k < parenSym.rows[rr].Count; k++) {
                    //if (entryLine.Bounds().Contains(parenSym.rows[rr][k].Bounds())) {
                    if (entryLine.Bounds().IntersectsWith(parenSym.rows[rr][k].Bounds())) {
                        parenSym.lines.Remove(parenSym.rows[rr][k]);
                        parenSym.rows[rr].RemoveAt(k);
                        k--;
                    }
                    if (parenSym.rows[rr].Count == 0) {
                        parenSym.rows.RemoveAt(rr);
                        rr--;
                        break;
                    }
                }
            }
  */          if (isArrowEntry) {
                int arrowID = 0;
                bool added = false;
                for(int i = 0; i< entryLine._syms.Count; i++)
                    if (entryLine._syms[i].r.alt == '↘') {
                        arrowID = entryLine._syms[i].r.strokes[0].Id;
                        parenSym.ArrowID = arrowID;
                        break;
                    }
                int rowCount = parenSym.rows.Count;
                int colCount = rowCount == 0? 0:parenSym.rows[rowCount - 1].Count;
                for (int i = 0; i < colCount; i++) 
                    for (int j = 0; j < parenSym.rows[rowCount - 1][i]._syms.Count; j++) 
                        if (parenSym.rows[rowCount - 1][i]._syms[j].r.alt == '↘') {
                            parenSym.lines.Remove(parenSym.rows[rowCount - 1][i]);
                            parenSym.rows[rowCount - 1].Remove(parenSym.rows[rowCount - 1][i]);
                            if (parenSym.rows[rowCount - 1].Count == 0)
                                parenSym.rows.RemoveAt(rowCount - 1);
                            List<Line> newRow = new List<Line>();
                            newRow.Add(entryLine);
                            parenSym.lines.Insert(0,entryLine);
                            parenSym.rows.Add(newRow);
                            added = true;                            
                            break;
                        } 
                if (!added) {
                    List<Line> newRow = new List<Line>();
                    newRow.Add(entryLine);
                    parenSym.lines.Insert(0,entryLine);
                    parenSym.rows.Add(newRow);
                }
                adjustEntries(parenSym);
                return;
            }

            bool isNewRow = true;
            //Check existing rows:
            for (int rr = 0; rr < (parenSym.ArrowID != -1 ? parenSym.rows.Count - 1 : parenSym.rows.Count); rr++) {
                if (!isNewRow) break;
                Rectangle rowBounds = parenSym.rows[rr][0].BaseLineBounds();// row bounds
                for (int k = 1; k < parenSym.rows[rr].Count; k++)
                    rowBounds = Rectangle.Union(rowBounds, parenSym.rows[rr][k].BaseLineBounds());
                int upperPtEntry = (entryLine.BaseLineBounds().Top * 3 + entryLine.BaseLineBounds().Bottom) / 4;
                int lowerPtEntry = (entryLine.BaseLineBounds().Top + entryLine.BaseLineBounds().Bottom * 3) / 4;
                int upperPtRow = (rowBounds.Top * 3 + rowBounds.Bottom) / 4;
                int lowerPtRow = (rowBounds.Top + rowBounds.Bottom * 3) / 4;

                if ((("⋯".IndexOf(entryLine._syms[0].r.allograph[0]) == -1) ? upperPtEntry : entryLine.BaseLineBounds().Top - entryLine.BaseLineBounds().Width / 2) < rowBounds.Bottom &&
                    (("⋯".IndexOf(entryLine._syms[0].r.allograph[0]) == -1) ? lowerPtEntry : entryLine.BaseLineBounds().Top + entryLine.BaseLineBounds().Width / 2) > rowBounds.Top ||
                    (("⋯".IndexOf(entryLine._syms[0].r.allograph[0]) == -1) ? entryLine.BaseLineBounds().Top : entryLine.BaseLineBounds().Top - entryLine.BaseLineBounds().Width / 2) < lowerPtRow &&
                    (("⋯".IndexOf(entryLine._syms[0].r.allograph[0]) == -1) ? entryLine.BaseLineBounds().Bottom : entryLine.BaseLineBounds().Top + entryLine.BaseLineBounds().Width / 2) > upperPtRow
                    ){// if in this row
                    for (int ii = 0; ii < parenSym.rows[rr].Count; ii++) {
                       /* if (entryLine.Bounds().IntersectsWith( parenSym.rows[rr][ii].Bounds())) {
                            parenSym.lines.Remove(parenSym.rows[rr][ii]);
                            parenSym.rows[rr].Remove(parenSym.rows[rr][ii]);
                            parenSym.rows[rr].Insert(ii, entryLine);
                            isNewRow = false;
                            break;
                        }
                        else*/
                        if (entryLine.Bounds().Right < parenSym.rows[rr][ii].Bounds().Left) {//BaseLineBounds not suitable here
                            parenSym.rows[rr].Insert(ii, entryLine);
                            isNewRow = false;
                            break;
                        }
                    }
                    if (isNewRow) {// last element of this row
                        parenSym.rows[rr].Add(entryLine);
                        isNewRow = false;
                    }
                    parenSym.lines.Add(entryLine);
                }
            }
            // a new row
            if (isNewRow) {
                List<Line> newRow = new List<Line>();                
                newRow.Add(entryLine);
                parenSym.lines.Add(entryLine);
                if (parenSym.rows.Count == 0) { //first row
                    parenSym.rows.Add(newRow);
                } else {
                    for (int rr = 0; rr < (parenSym.ArrowID != -1 ? parenSym.rows.Count - 1 : parenSym.rows.Count); rr++) {
                        Rectangle rowBounds = parenSym.rows[rr][0].BaseLineBounds();
                        for (int k = 1; k < parenSym.rows[rr].Count; k++)
                            rowBounds = Rectangle.Union(rowBounds, parenSym.rows[rr][k].BaseLineBounds());
                        if (entryLine.BaseLineBounds().Bottom < (rowBounds.Top * 3 + rowBounds.Bottom) / 4)
                        {//insert a new row, allowing < 1/4*height overlap
                            parenSym.rows.Insert(rr, newRow);
                            isNewRow = false;
                            break;
                        }
                    }
                    if (isNewRow) { // new row at the end
                        if (parenSym.ArrowID != -1)
                            parenSym.rows.Insert(parenSym.rows.Count - 1, newRow);
                        else 
                            parenSym.rows.Add(newRow);                        
                    }
                }
            }
            adjustEntries(parenSym);
        }
        
        //re-arrange up to 4 non-arrow entries in more than 2 rows, allowing more flexible/non-aligned entry original arrangement in a matrix with an arrow
        private void adjustEntries(ParenSym parenSym) {
            if (parenSym.ArrowID == -1 || parenSym.lines.Count > 5 || parenSym.rows.Count < 3) return;
            List<Line> newRow = new List<Line>();
            newRow = parenSym.rows[parenSym.rows.Count-1];   
            Rectangle arrowbox = new Rectangle ();
            for (int i = 0; i < parenSym.lines[0]._syms.Count; i++)
                if (parenSym.lines[0]._syms[i].r.alt == '↘'){
                    arrowbox = parenSym.lines[0]._syms[i].Bounds;                   
                    break;
                }
            int m = -1, n = -1;//indexes of entries at two ends of arrow, with arrow being the 0th line of parenSym.lines.
            int min = arrowbox.Y; 
 
            for (int i = 1; i < parenSym.lines.Count; i++)
                if (parenSym.lines[i]._syms[0].Bounds.X < arrowbox.X && parenSym.lines[i]._syms[0].Bounds.Y < arrowbox.Y){
                    m = i;
                    break;
                }
            double slope = (arrowbox.Bottom - arrowbox.Y + 0.0) / (arrowbox.Right - arrowbox.X);
            for (int i = 1; i < parenSym.lines.Count; i++) {
                if (i == m) continue;
                if (parenSym.lines[i]._syms[0].Bounds.Right < arrowbox.Right && parenSym.lines[i]._syms[0].Bounds.Bottom < arrowbox.Bottom) continue;
                int delta = parenSym.lines[i]._syms[0].Bounds.Y - (int)(arrowbox.Y + slope * (parenSym.lines[i]._syms[0].Bounds.X - arrowbox.X));
                if(delta < 0) delta = -delta;
                if(min > delta){                
                    min = delta;
                    n = i;
                } 
            } 
            if (n == -1) return;   


            parenSym.rows.Clear();
            parenSym.rows.Add(newRow);//arrow entry
            newRow = new List<Line>();
            newRow.Add(parenSym.lines[n]);//arrow head entry
            parenSym.rows.Insert(0, newRow);
            newRow = new List<Line>();
            newRow.Add(m<0? parenSym.lines[n]:parenSym.lines[m]);
            parenSym.rows.Insert(0, newRow);//arrow tail entry
           
            for (int i = 1; i < parenSym.lines.Count; i++) {
                if (i == m || i == n) continue;
                if (parenSym.lines[i]._syms[0].Bounds.Y < (int)(arrowbox.Y + slope * (parenSym.lines[i]._syms[0].Bounds.X - arrowbox.X))){
                    if (parenSym.rows[0].Count > 1) return;
                    parenSym.rows[0].Add(parenSym.lines[i]);
                }
                else {
                    if (parenSym.rows[1].Count > 1) return;
                    parenSym.rows[1].Insert(0, parenSym.lines[i]);                
                }
            }
            return;
        }

        private bool checkLookahead(ref Hashtable used, Symbol sim, Symbol nn, bool inside, ref Line line) {
            if (nn == null) {
                used.Remove(sim.r.guid);
                if (line.Uses(sim, inside, true))
                    used.Add(sim.r.guid, 0);
                return true;
            }

            Rectangle nextSymRect = nn.TotalBounds;
            if (((sim.Sym == '.') || sim.Bounds.Top < nextSymRect.Bottom) &&
                  (nn.Sym == '.' || nn.Sym == '-' || sim.Bounds.Bottom > nextSymRect.Top)) {
                sim.Bounds = new Rectangle(sim.Bounds.Left, Math.Min(sim.Bounds.Top, nn.Bounds.Top),
                                           sim.Bounds.Width, Math.Max(sim.Bounds.Bottom, nn.Bounds.Bottom) - Math.Min(sim.Bounds.Top, nn.Bounds.Top));
                if (sim.Sym == '.') {
                    //sim.Bounds = new Rectangle(sim.Bounds.Left, nn.StrokeBounds.Top, sim.Bounds.Width, nn.StrokeBounds.Height);
                    sim.Bounds = new Rectangle(sim.Bounds.Left, sim.Bounds.Top, nn.StrokeBounds.Width, sim.Bounds.Height);
                }
                used.Remove(sim.r.guid);
                if (line.Uses(sim, inside, true))
                    used.Add(sim.r.guid, 0);
                return true;
            }
            return false;
        }
        static private Point getPt(float i, Point[] pts) {
            if (i != (float)(int)i) {
                Point p1 = pts[(int)i];
                Point p2 = pts[(int)i + 1];
                return (Point)(V2D.Add(p1, V2D.Mul(V2D.Sub(p2, p1), (i - (int)i))));
            }
            return pts[(int)i];
        }
    }
}
