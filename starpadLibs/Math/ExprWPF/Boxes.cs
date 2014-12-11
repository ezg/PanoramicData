using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Runtime.InteropServices;
using starPadSDK.UnicodeNs;
using TType=starPadSDK.MathExpr.Syntax.TType;
using starPadSDK.MathExpr;
using starPadSDK.Geom;
using System.Windows.Media;

namespace starPadSDK.MathExpr.ExprWPF.Boxes {
    public abstract class Box {
        /// <summary>
        /// This is the tight bounding box of where there is actually black on the screen, relative to the starting reference point
        /// (whose Y value is at the midpt).
        /// </summary>
        protected Rct _bbox; public Rct bbox { get { return _bbox; } }
        /// <summary>
        /// This is the nominal bbox; its right is where you'd start drawing symbols that appear next to this box. It's
        /// essentially the same as _bbox vertically except that it's guaranteed to include the baseline and xheight.
        /// </summary>
        protected Rct _nombbox; public Rct nombbox { get { return _nombbox; } }
        private Expr _expr; public Expr Expr { get { return _expr; } }
        private object _exprix; public object ExprIx { get { return _exprix; } }
        protected Rct _bboxRefOrigin; public Rct BBoxRefOrigin { get { return _bboxRefOrigin; } }
        protected Rct _nombboxRefOrigin; public Rct NomBBoxRefOrigin { get { return _nombboxRefOrigin; } }
        public void Measure(EDrawingContext edc) { _Measure(edc); SaveMeasure(new Pt(0, 0)); }
        protected abstract void _Measure(EDrawingContext edc);
        /// <summary>
        /// this exists to allow us to keep non-Boxes from calling _Measure while still allowing subclasses to call it on other boxes
        /// </summary>
        protected void Call_Measure(Box b, EDrawingContext edc) { b._Measure(edc); }
        protected abstract void SaveMeasure(Pt refpt);
        /// <summary>
        /// this exists to allow us to keep non-Boxes from calling SaveMeasure while still allowing subclasses to call it on other boxes
        /// </summary>
        protected void CallSaveMeasure(Box b, Pt refpt) { b.SaveMeasure(refpt); }
        protected void SaveMeasureBBox(Pt refpt) { _bboxRefOrigin = _bbox + (Vec)refpt; _nombboxRefOrigin = _nombbox + (Vec)refpt; }
        public abstract void Draw(EDrawingContext edc, Pt refpt);
        /// <summary>
        /// assumes the starting math axis on the left side of the total expression is at the origin.
        /// </summary>
        public Geometry Geometry { get; protected set; }
        /// <summary>
        /// ComputeGeometry not only returns the computed geometry, but also sets the Geometry member on each Box
        /// in the tree of Boxes to be the sub-geometry corresponding to that box. The geometry assumes the starting math axis on the left side
        /// of the total expression is at the origin.
        /// </summary>
        public Geometry ComputeGeometry(EDrawingContext edc) { return _ComputeGeometry(edc, new Pt(0, 0)); }
        protected abstract Geometry _ComputeGeometry(EDrawingContext edc, Pt refpt);
        /// <summary>
        /// this exists to allow us to keep non-Boxes from calling _ComputeGeometry while still allowing subclasses to call it on other boxes
        /// </summary>
        protected Geometry Call_ComputeGeometry(Box b, EDrawingContext edc, Pt refpt) { return b._ComputeGeometry(edc, refpt); }
        protected Box(Expr expr) : this(expr, null) { }
        protected Box(Expr expr, object eix) { _expr = expr; _exprix = eix; }

        public abstract IEnumerable<Box> SubBoxes { get; }
    }
    public class BoxArg {
        private IEnumerable<Box> _boxes; public IEnumerable<Box> Boxes { get { return _boxes; } }
        public BoxArg(IEnumerable<Box> boxes) { _boxes = boxes; }
        public static implicit operator BoxArg(Box b) { return new BoxArg(new Box[] { b }); }
        public static implicit operator BoxArg(List<Box> boxes) { return new BoxArg(boxes); }
        public static implicit operator BoxArg(Box[] boxes) { return new BoxArg(boxes); }
    }
    public abstract class Multibox : Box {
        public Multibox(Expr e) : base(e) { }
        public Multibox(Expr e, object eix) : base(e, eix) { }
        protected override void SaveMeasure(Pt refpt) {
            SaveMeasureBBox(refpt);
            DoIt(refpt, (Box b, Pt p) => CallSaveMeasure(b, p));
        }
        public override void Draw(EDrawingContext edc, Pt refpt) {
            Color colr = edc.Colr;
            Expr refExpr = ExprIx is int && Expr is CompositeExpr ? Expr.Args()[(int)ExprIx] : Expr;
            if (refExpr != null && refExpr.Annotations.Contains("Factor"))
                edc.Colr = (Color)refExpr.Annotations["Factor"];
            DoIt(refpt, (Box b, Pt p) => b.Draw(edc, p));
            if (refExpr != null && refExpr.Annotations.Contains("Factor"))
                edc.Colr = colr;
        }
        protected override Geometry _ComputeGeometry(EDrawingContext edc, Pt refpt) {
            GeometryGroup gg = new GeometryGroup();
            DoIt(refpt, (Box b, Pt p) => gg.Children.Add(Call_ComputeGeometry(b, edc, p)));
            return Geometry = gg;
        }
        protected abstract void DoIt(Pt refpt, Action<Box, Pt> doer);
    }
    public class HBox : Multibox {
        private List<Box> _boxes; public List<Box> Boxes { get { return _boxes; } }
        public HBox(params BoxArg[] boxes) : this(null, boxes) { 
            _bbox = Rct.Null;
            _bboxRefOrigin = Rct.Null;
            _nombbox = Rct.Null;
            foreach (Box b in Boxes) {
                _bbox = _bbox.Union(b.bbox);
                _nombbox = _nombbox.Union(b.nombbox);
                _bboxRefOrigin = _bboxRefOrigin.Union(b.BBoxRefOrigin);
            }}
        public HBox(Expr expr,  object exprIx, params BoxArg[] boxes)
            : base(expr, exprIx) {
            _boxes = new List<Box>();
            foreach (BoxArg ba in boxes) _boxes.AddRange(ba.Boxes);
        }
        public HBox(Expr expr, params BoxArg[] boxes)
            : base(expr) {
            _boxes = new List<Box>();
            foreach(BoxArg ba in boxes) _boxes.AddRange(ba.Boxes);
        }
        protected override void _Measure(EDrawingContext edc) {
            _bbox = Rct.Null;
            _nombbox = new Rct(0, 0, 0, 0);
            foreach(Box b in Boxes) {
                Call_Measure(b, edc);
                _bbox = _bbox.Union(b.bbox + new Vec(_nombbox.Right, 0));
                _nombbox = _nombbox.Union(b.nombbox + new Vec(_nombbox.Right, 0));
            }
        }
        protected override void DoIt(Pt refpt, Action<Box, Pt> doer) {
            Pt pos = refpt;
            foreach(Box b in Boxes) {
                doer(b, pos);
                pos.X += b.nombbox.Right;
            }
        }
        public override IEnumerable<Box> SubBoxes { get { foreach(Box b in _boxes) yield return b; } }
    }
    public class VBox : Multibox {
        private List<Box> _boxes; public List<Box> Boxes { get { return _boxes; } }
        public VBox(Expr expr, params BoxArg[] boxes)
            : base(expr) {
            _boxes = new List<Box>();
            foreach(BoxArg ba in boxes) _boxes.AddRange(ba.Boxes);
        }
        protected override void _Measure(EDrawingContext edc) {
            _bbox = Rct.Null;
            _nombbox = new Rct(0, 0, 0, 0);
            foreach(Box b in Boxes) {
                Call_Measure(b, edc);
                _bbox = _bbox.Union(b.bbox + new Vec(0, _nombbox.Height - b.nombbox.Top));
                _nombbox = _nombbox.Union(b.nombbox + new Vec(0, _nombbox.Height - b.nombbox.Top));
            }
            _bbox -= new Vec(0, _nombbox.Height/2);
            _nombbox -= new Vec(0, _nombbox.Height/2);
        }
        protected override void DoIt(Pt refpt, Action<Box, Pt> doer) {
            Pt pos = refpt - new Vec(0, _nombbox.Height/2);
            foreach(Box b in Boxes) {
                doer(b, pos - new Vec(0, b.nombbox.Top));
                pos.Y += b.nombbox.Height;
            }            
        }
        public override IEnumerable<Box> SubBoxes { get { foreach(Box b in _boxes) yield return b; } }
    }
    public class AlignmentBox : Multibox {
        private Box[,] _boxes; public Box[,] Boxes { get { return _boxes; } }
        double[] _tops, _bottoms, _widths;
        Vec[,] _offsets;
        public AlignmentBox(Expr expr, Box[,] boxes)
            : base(expr) {
            _boxes = boxes;
        }
        protected override void _Measure(EDrawingContext edc) {
            _tops = new double[_boxes.GetLength(0)];
            _bottoms = new double[_boxes.GetLength(0)];
            _widths = new double[_boxes.GetLength(1)];
            _offsets = new Vec[_tops.Length, _widths.Length];

            Rct parenbbox;
            Rct parennombbox = edc.Measure(')', false, false, out parenbbox);
            double parentop = parennombbox.Top;
            double parenbot = parennombbox.Bottom;

            for(int i = 0; i < _tops.Length; i++) {
                _tops[i] = parentop;
                _bottoms[i] = parenbot;
            }
            for(int j = 0; j < _widths.Length; j++) _widths[j] = 0;
            for(int i = 0; i < _tops.Length; i++) {
                for(int j = 0; j < _widths.Length; j++) {
                    Box b = Boxes[i, j];
                    Call_Measure(b, edc);
                    _tops[i] = Math.Min(_tops[i], b.nombbox.Top);
                    _bottoms[i] = Math.Max(_bottoms[i], b.nombbox.Bottom);
                    _widths[j] = Math.Max(_widths[j], b.nombbox.Width);
                }
            }

            _bbox = Rct.Null;
            _nombbox = new Rct(0, 0, 0, 0);
            double xoffs, yoffs = 0;
            for(int i = 0; i < _tops.Length; i++) {
                xoffs = 0;
                _nombbox.Top = Math.Min(_nombbox.Top, parentop + yoffs - _tops[i]);
                _nombbox.Bottom = Math.Max(_nombbox.Top, parenbot + yoffs - _tops[i]);
                for(int j = 0; j < _widths.Length; j++) {
                    Box b = Boxes[i, j];
                    _offsets[i, j] = new Vec(xoffs + (_widths[j] - b.nombbox.Width)/2 - b.nombbox.Left, yoffs - _tops[i]);
                    _bbox = _bbox.Union(b.bbox + _offsets[i, j]);
                    _nombbox = _nombbox.Union(b.nombbox + _offsets[i, j]);

                    xoffs += _widths[j] + edc.Mu*18;/* quad of space */
                }
                yoffs += _bottoms[i] - _tops[i] + (edc.Display ? EDrawingContext.Metrics.MATHconstants.StackDisplayStyleGapMin : EDrawingContext.Metrics.MATHconstants.StackGapMin)*edc.FScaleFactor;
            }
            _bbox -= new Vec(0, _nombbox.Height/2);
            _nombbox -= new Vec(0, _nombbox.Height/2);
        }
        protected override void DoIt(Pt refpt, Action<Box, Pt> doer) {
            Pt pos = refpt - new Vec(0, _nombbox.Height/2);
            for(int i = 0; i < _tops.Length; i++) {
                for(int j = 0; j < _widths.Length; j++) {
                    Box b = Boxes[i, j];
                    doer(b, pos + _offsets[i, j]);
                }
            }
        }
        public override IEnumerable<Box> SubBoxes { get { foreach(Box b in _boxes) yield return b; } }
    }
    public class AtomBox : Box {
        private TType _TeXType; public TType TeXType { get { return _TeXType; } set { _TeXType = value; } }
        public enum LimitsType { NoLimits, DisplayLimits, Limits }
        private LimitsType _limits;
        /// <summary>
        /// Applies only if TeXType is LargeOp
        /// </summary>
        public LimitsType Limits { get { return _limits; } }
        private Box _nucleus;
        public Box Nucleus { get { return _nucleus; } set { _nucleus = value; } }
        private Box _sub;
        public Box Sub { get { return _sub; } set { _sub = value; } }
        private Box _sup;
        public Box Sup { get { return _sup; } set { _sup = value; } }
        private Vec _suboffs, _nucoffs, _supoffs;
        public AtomBox(Expr expr, Box nucleus, Box sub, Box sup, TType textype) : this(expr, nucleus, sub, sup, textype, LimitsType.DisplayLimits) { }
        public AtomBox(Expr expr, Box nucleus, Box sub, Box sup, TType textype, LimitsType lt)
            : base(expr) {
            _nucleus = nucleus;
            _sub = sub == null ? new NullBox() : sub;
            _sup = sup == null ? new NullBox() : sup;
            _TeXType = textype;
            _limits = lt;
        }
        private T Max<T>(T arg1, params T[] args) where T : IComparable {
            T max = arg1;
            foreach(T a in args) max = max.CompareTo(a) < 0 ? a : max;
            return max;
        }
        protected override void _Measure(EDrawingContext edc) {
            Call_Measure(_nucleus, edc);
            _bbox = _nucleus.bbox;
            _nombbox = _nucleus.nombbox;
            EDrawingContext edc2 = edc.Script();
            Call_Measure(_sub, edc2);
            Call_Measure(_sup, edc2);

            /* rule 13, 13a [from The TeXbook, appendix G] */
            if(_TeXType == TType.LargeOp && (_limits == LimitsType.Limits || (_limits == LimitsType.DisplayLimits && edc.Display))) {
                // FIXME could make symbols like sum and integral larger in display styles, per rule 13
                /* rule 13a */
                // FIXME ideally would use italic correction (delta) here
                // put sup above nuc above sub, each centered
                double wid = Max<double>(_nombbox.Width, _sup.nombbox.Width, _sub.nombbox.Width);
#if fonthasfullermathmetrics
                double xi9 = EDrawingContext.Metrics.MATHconstants.UpperLimitGapMin*edc.FScaleFactor;
                double xi11 = EDrawingContext.Metrics.MATHconstants.UpperLimitBaselineRiseMin*edc.FScaleFactor;
#else
                // stix beta fonts leave above parameters zero, so try to guess some decent values
                // FIXME are they still 0 for the other fonts beyond the base STIXGeneral.otf?
                double xi9 = EDrawingContext.Metrics.MATHconstants.StackGapMin*edc.FScaleFactor; // or should this use edc2 rather than edc?
                double xi11 = xi9;
#endif
                // remember TeX's computation is based on baselines, while our boxes are relative to the math axis.
                double gap = Max<double>(xi9, xi11 - (_sub.nombbox.Bottom - edc2.Midpt));
                _supoffs = new Vec((wid - _sup.nombbox.Width)/2, _nucleus.nombbox.Top - gap - _sup.nombbox.Bottom);
                _nucoffs = new Vec((wid - _nucleus.nombbox.Width)/2, 0);
#if fonthasfullermathmetrics
                double xi10 = EDrawingContext.Metrics.MATHconstants.LowerLimitGapMin*edc.FScaleFactor;
                double xi12 = EDrawingContext.Metrics.MATHconstants.LowerLimitBaselineDropMin*edc.FScaleFactor;
#else
                double xi10 = xi9, xi12 = xi11;
#endif
                gap = Max<double>(xi9, xi12 - (-_sub.nombbox.Top + edc2.Midpt));
                _suboffs = new Vec((wid - _sub.nombbox.Width)/2, _nucleus.nombbox.Bottom + gap - _sub.nombbox.Top);

                // ideally we would have also kerned due to delta (italic correction)--but don't have those metrics in our fonts yet(?)

                _bbox = _nucleus.bbox + _nucoffs;
                _bbox = _bbox.Union(_sup.bbox + _supoffs);
                _bbox = _bbox.Union(_sub.bbox + _suboffs);
                _nombbox = _nucleus.nombbox + _nucoffs;
                _nombbox = _nombbox.Union(_sup.nombbox + _supoffs);
                _nombbox = _nombbox.Union(_sub.nombbox + _suboffs);
                // Not sure what in the font metrics corresponds to this TeX parameter (xi13)
                _nombbox.Top -= EDrawingContext.Metrics.MATHconstants.StackGapMin*edc.FScaleFactor/2;
                _nombbox.Bottom += EDrawingContext.Metrics.MATHconstants.StackGapMin*edc.FScaleFactor/2;
            } else if(_sub is NullBox && _sup is NullBox) {
                /* rule 18 */
                _suboffs = _nucoffs = _supoffs = new Vec(0, 0);
            } else {
                double suboffs, supoffs;
                /* 18a */
                double u, v;
                if(_nucleus is CharBox || _nucleus is StringBox
                        || (_nucleus is DelimitedBox && ((DelimitedBox)_nucleus).DRight != null && !((DelimitedBox)_nucleus).DRight.Delim.IsScaled)) {
                    u = 0;
                    v = 0;
                } else {
                    // in tex this is computed as follows:
                    /* let q and r be the values of sigma18 and sigma19 in the scriptfont of us (so in p2)
                     * let h and d be the height and depth of the ("translated") nucleus
                     * then u <- h-q and v <- d+r */
                    // it says u and v are the minimum amounts to shift the the super and subscripts up and down; sigma18 & 19 are not otherwise used
                    // TeX computes this based on baselines
                    // FIXME should this be on edc2 analogous to the the TeXbook, or edc? right now doesn't matter cause stix sets to 0
                    double q = EDrawingContext.Metrics.MATHconstants.SuperscriptBaselineDropMax*edc2.FScaleFactor;
                    double r = EDrawingContext.Metrics.MATHconstants.SubscriptBaselineDropMin*edc2.FScaleFactor;
                    u = (-_nucleus.nombbox.Top + edc.Midpt) - q;
                    v = (_nucleus.nombbox.Bottom - edc.Midpt) + r;
                }
                /* 18b */
                // to handle adding scriptspace to subscript box, keep our own copies of nombbox here to be used in union computation later
                Rct subnombbox = _sub.nombbox;
                Rct supnombbox = _sup.nombbox;
                if(_sup is NullBox) {
                    supoffs = 0;
                    // add scriptspace to subscript box
                    subnombbox.Right += EDrawingContext.Metrics.MATHconstants.SpaceAfterScript*edc.FScaleFactor;
                    double sigma16 = EDrawingContext.Metrics.MATHconstants.SubscriptShiftDown*edc.FScaleFactor;
                    suboffs = Max<double>(v, sigma16, (-_sub.nombbox.Top + edc2.Midpt) - 4f/5f*edc.XHeight) + edc.Midpt - edc2.Midpt;
                } else {
                    /* 18c */
                    // add scriptspace to superscript box
                    supnombbox.Right += EDrawingContext.Metrics.MATHconstants.SpaceAfterScript*edc.FScaleFactor;
                    double p = EDrawingContext.Metrics.MATHconstants.SuperscriptShiftUp*edc.FScaleFactor; // FIXME use SuperscriptShiftUpCramped if cramped mode (not implemented yet)
                    u = Max<double>(u, p, (_sup.nombbox.Bottom - edc2.Midpt) + 1f/4f*edc.XHeight);
                    /* 18d */
                    if(_sub is NullBox) {
                        supoffs = -(u - edc.Midpt + edc2.Midpt);
                        suboffs = 0;
                    } else {
                        // add scripspace to subscript box
                        subnombbox.Right += EDrawingContext.Metrics.MATHconstants.SpaceAfterScript*edc.FScaleFactor;
                        double sigma17 = EDrawingContext.Metrics.MATHconstants.SubscriptShiftDown*edc.FScaleFactor;
                        v = Math.Max(v, sigma17);
                        /* 18e */
                        double theta = EDrawingContext.Metrics.MATHconstants.SubSuperscriptGapMin*edc.FScaleFactor/4;
                        if((u - (_sup.nombbox.Bottom - edc2.Midpt)) - ((-_sub.nombbox.Top + edc2.Midpt) - v) < 4*theta) {
                            v = 4*theta - ((u - (_sup.nombbox.Bottom - edc2.Midpt)) - (-_sub.nombbox.Top + edc2.Midpt));
                            double psi = 4f/5f*edc.XHeight - (u - (_sup.nombbox.Bottom - edc2.Midpt));
                            if(psi > 0) {
                                u += psi;
                                v -= psi;
                            }
                        }
                        /* 18f */
                        // FIXME: ideally would use kerning due to delta (italic correction) here--but we don't have those metrics in our fonts yet (?)
                        supoffs = -(u - edc.Midpt + edc2.Midpt);
                        suboffs = v + edc.Midpt - edc2.Midpt;
                    }
                }
                _suboffs = new Vec(_nucleus.nombbox.Right, suboffs);
                _nucoffs = new Vec(0, 0);
                _supoffs = new Vec(_nucleus.nombbox.Right, supoffs);
                _bbox = _bbox.Union(_sub.bbox + _suboffs);
                _nombbox = _nombbox.Union(subnombbox + _suboffs);
                _bbox = _bbox.Union(_sup.bbox + _supoffs);
                _nombbox = _nombbox.Union(supnombbox + _supoffs);
            }
        }
        protected override void SaveMeasure(Pt refpt) {
            CallSaveMeasure(_nucleus, refpt + _nucoffs);
            CallSaveMeasure(_sub, refpt + _suboffs);
            CallSaveMeasure(_sup, refpt + _supoffs);
            SaveMeasureBBox(refpt);
        }
        public override void Draw(EDrawingContext edc, Pt refpt) {
            _nucleus.Draw(edc, refpt + _nucoffs);
            EDrawingContext edc2 = edc.Script();
            _sub.Draw(edc2, refpt + _suboffs);
            _sup.Draw(edc2, refpt + _supoffs);
        }
        protected override Geometry _ComputeGeometry(EDrawingContext edc, Pt refpt) {
            GeometryGroup gg = new GeometryGroup();
            gg.Children.Add(Call_ComputeGeometry(_nucleus, edc, refpt + _nucoffs));
            EDrawingContext edc2 = edc.Script();
            gg.Children.Add(Call_ComputeGeometry(_sub, edc2, refpt + _suboffs));
            gg.Children.Add(Call_ComputeGeometry(_sup, edc2, refpt + _supoffs));
            return Geometry = gg;
        }
        public override IEnumerable<Box> SubBoxes {
            get {
                yield return _nucleus;
                yield return _sub;
                yield return _sup;
            }
        }
    }
    public class RootBox : Box {
        private Box _radicand; public Box Radicand { get { return _radicand; } }
        private Box _index; public Box Index { get { return _index; } }
        private Rct _radbb, _nomradbb, _linebb;
        private double _linewid, _textgap;
        private Vec _ioffs, _roffs, _rroffs;
        private EDrawingContext.BigC _bigsqrt;
        /* the four public accessors here are hacks for squishtypeset mode in Form1 to be able to squish square roots when they've been morphed */
        public EDrawingContext.BigC Bigsqrt { get { return _bigsqrt; } }
        public Vec Roffs { get { return _roffs; } }
        public Vec Rroffs { get { return _rroffs; } }
        public Rct Linebb { get { return _linebb; } }
        /* end of hack accessors */
        public RootBox(Expr expr, Box radicand) : this(expr, radicand, new NullBox()) { }
        public RootBox(Expr expr, Box radicand, Box index)
            : base(expr) {
            _radicand = radicand;
            _index = index;
        }
        protected override void _Measure(EDrawingContext edc) {
            EDrawingContext.OTFMetrics mt = EDrawingContext.Metrics;
            Call_Measure(_radicand, edc);
            EDrawingContext edc2 = edc.Script();
            Call_Measure(_index, edc2);
            // See TeXBook Appendix G rule 11 (scattered implementation in EDrawingContext.BigC too); modified due to having different metrics from our fonts
            if(edc.Display) _textgap = EDrawingContext.Metrics.MATHconstants.RadicalDisplayStyleVerticalGap*edc.FScaleFactor;
            else _textgap = mt.MATHconstants.RadicalVerticalGap*edc.FScaleFactor;
            _linewid = mt.MATHconstants.RadicalRuleThickness*edc.FScaleFactor;
            // but radicalrulethickness is the only rule thickness set to 0 in beta fonts?! From inspection of the top radical glyph piece (when composed)
            // it looks like the value should have been 66.
            if(_linewid == 0) _linewid = 66*edc.FScaleFactor;
            _textgap += _linewid;
            _bigsqrt = edc.Big(Unicode.S.SQUARE_ROOT, -_radicand.nombbox.Top + _textgap + _linewid, _radicand.nombbox.Bottom);
            _nomradbb = _bigsqrt.NomBBox;
            _radbb = _bigsqrt.BBox;

            // offset of index (root number, eg the 3 for cube root)
            _ioffs = new Vec(_index.nombbox.Right != 0 ? mt.MATHconstants.RadicalKernBeforeDegree*edc.FScaleFactor : 0,
                -mt.MATHconstants.RadicalDegreeBottomRaisePercent/100.0*_radbb.Height + _nomradbb.Bottom - _index.nombbox.Bottom);
            // offset of radical sign
            _roffs = new Vec(_ioffs.X + _index.nombbox.Right + (_index.nombbox.Right != 0 ? mt.MATHconstants.RadicalKernAfterDegree*edc.FScaleFactor : 0), 0);
            // offset of radicand (thing having its root taken) and _linebb (the line over the radicand)
            _rroffs = new Vec(_roffs.X + _nomradbb.Right, 0);
            double radsymtop = _radbb.Top;
            //_roffs = new Vec(_index.nombbox.Right, 0);
            //_ioffs = new Vec(0, radsymtop - _index.nombbox.Top);
            _linebb = new Rct(-mt.MATHvariants.MinConnectorOverlap*edc.FScaleFactor, radsymtop,
                _radicand.nombbox.Right + _linewid, radsymtop + _linewid); // LaTeX adds the extra horizontal bit of the radical overbar, but it's not in TeXBook as far as I can tell
            _bbox = (_index.bbox + _ioffs).Union(_radbb + _roffs).Union(_linebb + _rroffs).Union(_radicand.bbox + _rroffs);
            _nombbox = (_index.nombbox + _ioffs).Union(_nomradbb + _roffs).Union(_linebb + _rroffs).Union(_radicand.nombbox + _rroffs);
            _nombbox.Top = Math.Min(_nombbox.Top, radsymtop - mt.MATHconstants.RadicalExtraAscender*edc.FScaleFactor);
            _nombbox.Left = Math.Min(_nombbox.Left, 0);
        }
        protected override void SaveMeasure(Pt refpt) {
            CallSaveMeasure(_index, refpt + _ioffs);
            CallSaveMeasure(_radicand, refpt + _rroffs);
            SaveMeasureBBox(refpt);
        }
        public override void Draw(EDrawingContext edc, Pt refpt) {
            EDrawingContext edc2 = edc.Script();
            _index.Draw(edc2, refpt + _ioffs);
            edc.Draw(_bigsqrt, refpt + _roffs);
            edc.DC.DrawRectangle(edc.Brush, null, _linebb + (Vec)refpt + _rroffs);
            _radicand.Draw(edc, refpt + _rroffs);
        }
        protected override Geometry _ComputeGeometry(EDrawingContext edc, Pt refpt) {
            GeometryGroup gg = new GeometryGroup();
            EDrawingContext edc2 = edc.Script();
            gg.Children.Add(Call_ComputeGeometry(_index, edc2, refpt + _ioffs));
            Geometry g;
            edc.GetGeometry(_bigsqrt, refpt + _roffs, out g);
            gg.Children.Add(g);
            gg.Children.Add(new RectangleGeometry(_linebb + (Vec)refpt + _rroffs));
            gg.Children.Add(Call_ComputeGeometry(_radicand, edc, refpt + _rroffs));
            return Geometry = gg;
        }
        public override IEnumerable<Box> SubBoxes { get { yield return _radicand; yield return _index; } }
    }
    public class RuleBox : Box {
        private Rct _r; public Rct R { get { return _r; } set { _r = value; } }
        public RuleBox(Rct r) : this(null, r) { }
        public RuleBox(Expr expr, Rct r)
            : base(expr) {
            _r = r;
        }
        protected override void _Measure(EDrawingContext edc) {
            _nombbox = _bbox = _r;
        }
        protected override void SaveMeasure(Pt refpt) {
            SaveMeasureBBox(refpt);
        }
        public override void Draw(EDrawingContext edc, Pt refpt) {
            edc.DC.DrawRectangle(edc.Brush, null, _r + (Vec)refpt);
        }
        protected override Geometry _ComputeGeometry(EDrawingContext edc, Pt refpt) {
            return Geometry = new RectangleGeometry(_r + (Vec)refpt);
        }
        public override IEnumerable<Box> SubBoxes { get { yield break; } }
    }
    public class AtopBox : Box {
        private Expr _divlineexpr;
        private Box _top; public Box Top { get { return _top; } }
        private RuleBox _divline; public RuleBox DivLine { get { return _divline; } }
        private Box _bot; public Box Bot { get { return _bot; } }
        private bool _drawFract; public bool DrawFract { get { return _drawFract; } }
        private double _wid, _linehalf, _hoffs;
        private Rct _linerect;
        private Vec _topoffs, _lineoffs, _botoffs;
        public AtopBox(Expr expr, Box top, Box bot, bool drawfract) : this(expr, null, top, bot, drawfract) { }
        public AtopBox(Expr expr, Expr divlineexpr, Box top, Box bot, bool drawfract)
            : base(expr) {
            _divlineexpr = divlineexpr;
            _top = top;
            _divline = new RuleBox(_divlineexpr, Rct.Null);
            _bot = bot;
            _drawFract = drawfract;
        }
        protected override void _Measure(EDrawingContext edc) {
            // FIXME: this should really use TeXbook appendix G, p 444, rule 15 and 15a-e
            EDrawingContext edc2 = edc.Atop();
            Call_Measure(_top, edc2);
            Call_Measure(_bot, edc2);
            _wid = Math.Max(_top.nombbox.Width, _bot.nombbox.Width);
            _hoffs = 0/*edc.Thin*/;
            _lineoffs = new Vec(_hoffs/2, 0);
            _linehalf = _drawFract ? EDrawingContext.Metrics.MATHconstants.FractionRuleThickness*edc.FScaleFactor/2 : 0;
            _linerect = new Rct(0, -_linehalf, _wid + 2*_hoffs, _linehalf);
            _divline.R = _linerect;
            Call_Measure(_divline, edc);

            double numgap = (edc.Display ? EDrawingContext.Metrics.MATHconstants.FractionNumeratorDisplayStyleGapMin : EDrawingContext.Metrics.MATHconstants.FractionNumeratorGapMin)*edc.FScaleFactor;
            double dengap = (edc.Display ? EDrawingContext.Metrics.MATHconstants.FractionDenominatorDisplayStyleGapMin : EDrawingContext.Metrics.MATHconstants.FractionDenominatorGapMin)*edc.FScaleFactor;
            _topoffs = new Vec(3*_hoffs/2 + _wid/2 - _top.nombbox.Width/2, -_linehalf - numgap - _top.nombbox.Bottom);
            _botoffs = new Vec(3*_hoffs/2 + _wid/2 - _bot.nombbox.Width/2, _linehalf + dengap - _bot.nombbox.Top);
            _bbox = (_divline.bbox + _lineoffs).Union(_top.bbox + _topoffs).Union(_bot.bbox + _botoffs);
            _nombbox = (_divline.nombbox + _lineoffs).Union(_top.nombbox + _topoffs).Union(_bot.nombbox + _botoffs);
            _nombbox.Left = 0;
            _nombbox.Right += _hoffs/2;
        }
        protected override void SaveMeasure(Pt refpt) {
            CallSaveMeasure(_top, refpt + _topoffs);
            if(_drawFract) CallSaveMeasure(_divline, refpt + _lineoffs);
            CallSaveMeasure(_bot, refpt + _botoffs);
            SaveMeasureBBox(refpt);
        }
        public override void Draw(EDrawingContext edc, Pt refpt) {
            Color colr = edc.Colr;
            if (Expr != null && Expr.Annotations.Contains("Factor"))
                edc.Colr = (Color)Expr.Annotations["Factor"];
            EDrawingContext edc2 = edc.Atop();
            _top.Draw(edc2, refpt + _topoffs);
            if(_drawFract) _divline.Draw(edc, refpt + _lineoffs);
            _bot.Draw(edc2, refpt + _botoffs);
            if (Expr != null && Expr.Annotations.Contains("Factor"))
                edc.Colr = colr;
        }
        protected override Geometry _ComputeGeometry(EDrawingContext edc, Pt refpt) {
            GeometryGroup gg = new GeometryGroup();
            EDrawingContext edc2 = edc.Atop();
            gg.Children.Add(Call_ComputeGeometry(_top, edc2, refpt + _topoffs));
            if(_drawFract) gg.Children.Add(Call_ComputeGeometry(_divline, edc, refpt + _lineoffs));
            gg.Children.Add(Call_ComputeGeometry(_bot, edc2, refpt + _botoffs));
            return Geometry = gg;
        }
        public override IEnumerable<Box> SubBoxes { get { yield return _top; yield return _divline; yield return _bot; } }
    }
    public class DelimitedBox : Box {
        private DelimBox _dleft; public DelimBox DLeft { get { return _dleft; } set { _dleft = value; } }
        private Box _left; public Box Left { get { return _left; } set { _left = value; } }
        // HBox rather than Box so that the postprocess Fixup stage can add spacing without complexity
        private HBox _contents; public HBox Contents { get { return _contents; } set { _contents = value; } }
        private DelimBox _dright; public DelimBox DRight { get { return _dright; } set { _dright = value; } }
        private Box _right; public Box Right { get { return _right; } set { _right = value; } }

        private Vec _loffs, _coffs, _roffs;
        public DelimitedBox(Expr e, char l, Box contents) : this(e, new DelimBox(l), contents, null) { }
        public DelimitedBox(Expr e, Box contents, char r) : this(e, null, contents, new DelimBox(r)) { }
        public DelimitedBox(Expr e, char l, Box contents, char r) : this(e, new DelimBox(l), contents, new DelimBox(r)) { }
        public DelimitedBox(Expr e, object lix, char l, Box contents) : this(e, new DelimBox(e, lix, l), contents, null) { }
        public DelimitedBox(Expr e, Box contents, object rix, char r) : this(e, null, contents, new DelimBox(e, rix, r)) { }
        public DelimitedBox(Expr e, object lix, char l, Box contents, object rix, char r) : this(e, new DelimBox(e, lix, l), contents, new DelimBox(e, rix, r)) { }
        public DelimitedBox(Expr e, bool emph, object lix, char l, Box contents, object rix, char r) : this(e, new DelimBox(e, lix, l, emph), contents, new DelimBox(e, rix, r, emph)) { }
        private DelimitedBox(Expr e, DelimBox l, Box contents, DelimBox r) : this(e, l, new HBox(contents), r) { }
        private DelimitedBox(Expr e, DelimBox l, HBox contents, DelimBox r)
            : base(e) {
            _dleft = l;
            _left = l == null ? (Box)new NullDelimBox() : new AtomBox(null, l, null, null, TType.Open);
            _contents = contents;
            _dright = r;
            _right = r == null ? (Box)new NullDelimBox() : new AtomBox(null, r, null, null, TType.Close);
        }
        public DelimitedBox(Expr e, DelimBox dl, Box l, Box contents) : this(e, dl, l, contents, null, new NullDelimBox()) { }
        public DelimitedBox(Expr e, Box contents, DelimBox dr, Box r) : this(e, null, new NullDelimBox(), contents, dr, r) { }
        public DelimitedBox(Expr e, DelimBox dl, Box l, Box contents, DelimBox dr, Box r) : this(e, dl, l, new HBox(contents), dr, r) { }
        public DelimitedBox(Expr e, DelimBox dl, Box l, HBox contents, DelimBox dr, Box r)
            : base(e) {
            _dleft = dl;
            _left = l;
            _contents = contents;
            _dright = dr;
            _right = r;
        }
        protected override void _Measure(EDrawingContext edc) {
            Call_Measure(_contents, edc);
            if(_dleft != null) {
                _dleft.Ascent = -_contents.nombbox.Top;
                _dleft.Descent = _contents.nombbox.Bottom;
            }
            if(_dright != null) {
                _dright.Ascent = -_contents.nombbox.Top;
                _dright.Descent = _contents.nombbox.Bottom;
            }
            Call_Measure(_left, edc);
            Call_Measure(_right, edc);
            _loffs = new Vec(0, 0);
            _coffs = new Vec(_left.nombbox.Right, 0);
            _roffs = _coffs + new Vec(_contents.nombbox.Right, 0);
            _bbox = _left.bbox + _loffs;
            _bbox = _bbox.Union(_contents.bbox + _coffs);
            _bbox = _bbox.Union(_right.bbox + _roffs);
            _nombbox = _left.nombbox + _loffs;
            _nombbox = _nombbox.Union(_contents.nombbox + _coffs);
            _nombbox = _nombbox.Union(_right.nombbox + _roffs);
        }
        protected override void SaveMeasure(Pt refpt) {
            CallSaveMeasure(_left, refpt + _loffs);
            CallSaveMeasure(_contents, refpt + _coffs);
            CallSaveMeasure(_right, refpt + _roffs);
            SaveMeasureBBox(refpt);
        }
        public override void Draw(EDrawingContext edc, Pt refpt) {
            _left.Draw(edc, refpt + _loffs);
            _contents.Draw(edc, refpt + _coffs);
            _right.Draw(edc, refpt + _roffs);
        }
        protected override Geometry _ComputeGeometry(EDrawingContext edc, Pt refpt) {
            GeometryGroup gg = new GeometryGroup();
            gg.Children.Add(Call_ComputeGeometry(_left, edc, refpt + _loffs));
            gg.Children.Add(Call_ComputeGeometry(_contents, edc, refpt + _coffs));
            gg.Children.Add(Call_ComputeGeometry(_right, edc, refpt + _roffs));
            return Geometry = gg;
        }
        public override IEnumerable<Box> SubBoxes { get { yield return _left; yield return _contents; yield return _right; } }
    }
    public class NullBox : Box {
        public NullBox()
            : base(null) {
        }
        protected override void _Measure(EDrawingContext edc) {
            _bbox = Rct.Null;
            _nombbox = new Rct(0, 0, 0, 0);
        }
        protected override void SaveMeasure(Pt refpt) {
            _bboxRefOrigin = Rct.Null;
            _nombboxRefOrigin = _nombbox + (Vec)refpt;
        }
        public override void Draw(EDrawingContext edc, Pt refpt) { }
        protected override Geometry _ComputeGeometry(EDrawingContext edc, Pt refpt) { return Geometry = new GeometryGroup(); }
        public override IEnumerable<Box> SubBoxes { get { yield break; } }
    }
    public class MuSkipBox : Box {
        private int _skip;
        public MuSkipBox(int skip)
            : base(null) {
            _skip = skip;
        }
        protected override void _Measure(EDrawingContext edc) {
            _bbox = Rct.Null;
            _nombbox = new Rct(0, 0, edc.Mu*_skip, 0);
        }
        protected override void SaveMeasure(Pt refpt) {
            _bboxRefOrigin = Rct.Null;
            _nombboxRefOrigin = _nombbox + (Vec)refpt;
        }
        public override void Draw(EDrawingContext edc, Pt refpt) { }
        protected override Geometry _ComputeGeometry(EDrawingContext edc, Pt refpt) { return Geometry = new GeometryGroup(); }
        public override IEnumerable<Box> SubBoxes { get { yield break; } }
    }
    /// <summary>
    /// Like MuSkipBox, but only skips if in display or text style (not script or scriptscript)
    /// </summary>
    public class DTMuSkipBox : Box {
        private int _skip;
        public DTMuSkipBox(int skip)
            : base(null) {
            _skip = skip;
        }
        protected override void _Measure(EDrawingContext edc) {
            _bbox = Rct.Null;
            if(edc.Nonscript) _nombbox = new Rct(0, 0, edc.Mu*_skip, 0);
            else _nombbox = new Rct(0, 0, 0, 0);
        }
        protected override void SaveMeasure(Pt refpt) {
            _bboxRefOrigin = Rct.Null;
            _nombboxRefOrigin = _nombbox + (Vec)refpt;
        }
        public override void Draw(EDrawingContext edc, Pt refpt) { }
        protected override Geometry _ComputeGeometry(EDrawingContext edc, Pt refpt) { return Geometry = new GeometryGroup(); }
        public override IEnumerable<Box> SubBoxes { get { yield break; } }
    }
    public class NullDelimBox : Box {
        public NullDelimBox() : base(null) { }
        protected override void _Measure(EDrawingContext edc) {
            // "Plain TeX sets \nulldelimiterspace=1.2pt" (for a 10 pt default font)
            _bbox = Rct.Null;
            _nombbox = new Rct(0, 0, (edc.Ascent + edc.Descent)*0.12f, 0);
        }
        protected override void SaveMeasure(Pt refpt) {
            _bboxRefOrigin = Rct.Null;
            _nombboxRefOrigin = _nombbox + (Vec)refpt;
        }
        public override void Draw(EDrawingContext edc, Pt refpt) { }
        protected override Geometry _ComputeGeometry(EDrawingContext edc, Pt refpt) { return Geometry = new GeometryGroup(); }
        public override IEnumerable<Box> SubBoxes { get { yield break; } }
    }
    public class CharBox : Box {
        private TType _TeXType; public TType TeXType { get { return _TeXType; } set { _TeXType = value; } }
        private char _c; public char C { get { return _c; } }
        public bool Bold { get; private set; }
        public bool Italic { get; private set; }
        public CharBox(char c, TType textype) : this(null, null, c, false, false, textype) { }
        public CharBox(Expr expr, char c, TType textype) : this(expr, null, c, false, false, textype) { }
        public CharBox(Expr expr, object exprix, char c, TType textype) : this(expr, exprix, c, false, false, textype) { }
        public CharBox(Expr expr, char c, bool bold, bool italic, TType textype) : this(expr, null, c, bold, italic, textype) { }
        public CharBox(Expr expr, object exprix, char c, bool bold, bool italic, TType textype)
            : base(expr, exprix) {
            _c = c;
            Bold = bold;
            Italic = italic;
            _TeXType = textype;
        }
        protected override void _Measure(EDrawingContext edc) {
            _nombbox = edc.Measure(_c, Bold, Italic, out _bbox);
        }
        protected override void SaveMeasure(Pt refpt) {
            SaveMeasureBBox(refpt);
        }
        public override void Draw(EDrawingContext edc, Pt refpt) {
            Color colr = edc.Colr;
            if (Expr != null && Expr.Annotations.Contains("Factor"))
                edc.Colr = (Color)Expr.Annotations["Factor"];
            edc.Draw(_c, refpt, Bold, Italic);
            if (Expr != null && Expr.Annotations.Contains("Factor"))
                edc.Colr = colr;
        }
        protected override Geometry _ComputeGeometry(EDrawingContext edc, Pt refpt) {
            Geometry g;
            edc.GetGeometry(_c, refpt, Bold, Italic, out g);
            return Geometry = g;
        }
        public override IEnumerable<Box> SubBoxes { get { yield break; } }
    }
    public class IntegralCharBox : CharBox {
        public IntegralCharBox(Expr e, char c) : base(e, c, TType.LargeOp) { }
        private EDrawingContext.BigC _bigc; public EDrawingContext.BigC BigC { get { return BigC; } }
        protected override void _Measure(EDrawingContext edc) {
            Rct cbbox;
            Rct ncbbox = edc.Measure(C, Bold, Italic, out cbbox);
            double ascent = Math.Max(-cbbox.Top, edc.Ascent - edc.Midpt);
            double descent = Math.Max(cbbox.Bottom, edc.Descent + edc.Midpt);
            _bigc = edc.Big(C, ascent, descent);
            _bbox = _bigc.BBox;
            _nombbox = _bigc.NomBBox;
        }
        protected override void SaveMeasure(Pt refpt) {
            SaveMeasureBBox(refpt);
        }
        public override void Draw(EDrawingContext edc, Pt refpt) {
            edc.Draw(_bigc, refpt);
        }
        protected override Geometry _ComputeGeometry(EDrawingContext edc, Pt refpt) {
            Geometry g;
            edc.GetGeometry(_bigc, refpt, out g);
            return Geometry = g;
        }
        public override IEnumerable<Box> SubBoxes { get { yield break; } }
    }
    public class DelimBox : Box {
        private char _c;
        private bool _emph;
        private double _ascent;
        public double Ascent { get { return _ascent; } set { _ascent = value; } }
        private double _descent;
        public double Descent { get { return _descent; } set { _descent = value; } }
        private EDrawingContext.BigC _delim; public EDrawingContext.BigC Delim { get { return _delim; } }
        public DelimBox(char c) : this(null, null, c) { }
        public DelimBox(Expr e, char c) : this(e, null, c) { }
        public DelimBox(Expr e, object exprix, char c) : this(e, exprix, c, false) { }
        public DelimBox(Expr e, object exprix, char c, bool emph)
            : base(e, exprix) {
            _c = c;
            _emph = emph;
            _ascent = _descent = 0;
        }
        protected override void _Measure(EDrawingContext edc) {
            _delim = edc.Big(_c, _emph, _ascent, _descent);
            _bbox = _delim.BBox;
            _nombbox = _delim.NomBBox;
        }
        protected override void SaveMeasure(Pt refpt) {
            SaveMeasureBBox(refpt);
        }
        public override void Draw(EDrawingContext edc, Pt refpt) {
            edc.Draw(_delim, refpt);
        }
        protected override Geometry _ComputeGeometry(EDrawingContext edc, Pt refpt) {
            Geometry g;
            edc.GetGeometry(_delim, refpt, out g);
            return Geometry = g;
        }
        public override IEnumerable<Box> SubBoxes { get { yield break; } }
    }
    public class IntegralDelimBox : DelimBox {
        public IntegralDelimBox(Expr e, char c) : base(e, c) { }
        protected override void _Measure(EDrawingContext edc) {
            double savedascent = Ascent;
            double saveddescent = Descent;
            Ascent = Math.Max(Ascent, edc.Ascent - edc.Midpt);
            Descent = Math.Max(Descent, edc.Descent + edc.Midpt);
            base._Measure(edc);
            Ascent = savedascent;
            Descent = saveddescent;
        }
    }
    public class StringBox : Multibox {
        private TType _TexType; public TType TeXType { get { return _TexType; } set { _TexType = value; } }
        private string _s; public string S { get { return _s; } }
        public bool Bold { get; private set; }
        public bool Italic { get; private set; }
        private CharBox[] _cboxes; public CharBox[] CBoxes { get { return _cboxes; } }
        public StringBox(string s, TType textype) : this(null, s, false, false, textype) { }
        public StringBox(Expr expr, string s, TType textype) : this(expr, s, false, false, textype) { }
        public StringBox(Expr expr, string s, bool bold, bool italic, TType textype)
            : base(expr) {
            _s = s;
            Bold = bold;
            Italic = italic;
            _cboxes = new CharBox[s.Length];
            _TexType = textype;
            for(int i = 0; i < s.Length; i++) _cboxes[i] = new CharBox(expr, i, s[i], bold, italic, _TexType);
        }
        protected override void _Measure(EDrawingContext edc) {
            _bbox = Rct.Null;
            _nombbox = new Rct(0, 0, 0, 0);
            foreach(CharBox cb in _cboxes) {
                Call_Measure(cb, edc);
                _bbox = _bbox.Union(cb.bbox + new Vec(_nombbox.Right, 0));
                _nombbox = _nombbox.Union(cb.nombbox + new Vec(_nombbox.Right, 0));
            }
        }
       protected override void DoIt(Pt refpt, Action<Box, Pt> doer) {
            Pt pos = refpt;
            foreach(CharBox cb in _cboxes) {
                doer(cb, pos);
                pos.X += cb.nombbox.Right;
            }
        }
        public override IEnumerable<Box> SubBoxes { get { foreach(CharBox cb in _cboxes) yield return cb; } }
    }
}
