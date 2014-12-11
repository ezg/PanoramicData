using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using starPadSDK.Inq;
using starPadSDK.CharRecognizer;
using starPadSDK.Utils;
using starPadSDK.Inq.MSInkCompat;
using System.Diagnostics;
using starPadSDK.UnicodeNs;
using starPadSDK.MathRecognizer;
using Microsoft.Ink;
using starPadSDK.MathExpr;
using starPadSDK.Geom;
using CuspDetector = starPadSDK.Inq.BobsCusps.FeaturePointDetector;
using starPadSDK.MathUI;

namespace MathRecoScaffold {
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window {
        private StroqCollection _mathStroqs = new StroqCollection();
        private MathRecognition _mrec;
        public Window1() {
            InitializeComponent();

            _mrec = new MathRecognition(_mathStroqs);
            _mrec.EnsureLoaded(); // this is optional, and should only be called once per program run
            _mrec.ParseUpdated += _mrec_ParseUpdated;

            _altsMenuCrea = new AlternatesMenuCreator(alternatesMenu, _mrec);

            inqCanvas.StroqCollected += inqCanvas_StroqCollected;
            inqCanvas.PreviewStylusDown += inqCanvas_PreviewStylusDown;
            inqCanvas.PreviewMouseLeftButtonDown += inqCanvas_PreviewMouseLeftButtonDown;
            inqCanvas.PreviewMouseMove += inqCanvas_PreviewMouseMove;
            inqCanvas.PreviewMouseLeftButtonUp += inqCanvas_PreviewMouseLeftButtonUp;
            inqCanvas.DefaultDrawingAttributes.Width = 1;

            foreach(Engine eng in Engine.Engines) {
                MenuItem mi = new MenuItem();
                mi.Header = eng.Name;
                if(eng.Names == null) {
                    mi.Tag = eng;
                    mi.Click += new RoutedEventHandler(ChangeEngine);
                    if(eng == Engine.Current) mi.IsChecked = true;
                } else {
                    for(int i = 0; i < eng.Names.Length; i++) {
                        MenuItem mi2 = new MenuItem();
                        mi2.Header = eng.Names[i];
                        mi2.Tag = new KeyValuePair<Engine, int>(eng, i);
                        mi2.Click += new RoutedEventHandler(ChangeEngine);
                        if(eng == Engine.Current && i == eng.Variant) mi2.IsChecked = true;
                        mi.Items.Add(mi2);
                    }
                }
                _engineMenu.Items.Add(mi);
            }

            /* for the rest of this method, try to ensure more stuff is loaded at startup to avoid a long pause after first stroke */
            // load unicode stuff (may not be that long?)
            Console.WriteLine(Unicode.NameOf('a'));
            
            // load drawing wpf stuff and create initial math font stuff
            DrawingVisual dv = new DrawingVisual();
            var dc = dv.RenderOpen();
            Rct nombb = starPadSDK.MathExpr.ExprWPF.EWPF.DrawTop(new LetterSym('1'), 22, dc, Colors.Blue, new Pt(0, 0), true);
            dc.Close();
            
            // load wpf ink analysis (as opposed to old ink analysis loaded in _mrec.EnsureLoaded() above)
            InkAnalyzer ia = new InkAnalyzer();
            AnalysisHintNode ahn = ia.CreateAnalysisHint();
            ahn.WordMode = true;
            ahn.Location.MakeInfinite();
            ia.AddStroke(new System.Windows.Ink.Stroke(new StylusPointCollection(new[] { new Point(100, 100), new Point(100, 200) })));
            AnalysisStatus stat = ia.Analyze();
            AnalysisAlternateCollection aac = ia.GetAlternates();
            Console.WriteLine(aac[0].RecognizedString);
        }

        private void ChangeEngine(object sender, RoutedEventArgs e) {
            MenuItem mi = (MenuItem)sender;
            if(mi.IsChecked) return;
            foreach(MenuItem old in _engineMenu.Items) {
                old.IsChecked = false;
                foreach(MenuItem oo in old.Items) {
                    oo.IsChecked = false;
                }
            }
            if(mi.Tag is Engine) {
                Engine.Current = (Engine)mi.Tag;
                mi.IsChecked = true;
            } else {
                KeyValuePair<Engine, int> pair = (KeyValuePair<Engine, int>)mi.Tag;
                pair.Key.Variant = pair.Value;
                Engine.Current = pair.Key;
                mi.IsChecked = true;
            }
        }

        private bool _moving = false;
        void inqCanvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if(_moving) {
                _moving = false;
                using(_mrec.BatchEditNoRecog(true)) {
                    Selected.Contents.MoveTo(e.GetPosition(inqCanvas));
                }
                Selected.Contents.EndMove();
                if(_movingLock != null) {
                    _movingLock.Dispose(); // this will call Parse itself
                    _movingLock = null;
                } else Selected.Contents.Reparse(_mrec); // make sure math is updated and do a full rerecog just in case; we have only been doing non-re-recognition parses for speed
                Deselect();
                Mouse.Capture(null);
                e.Handled = true;
                inqCanvas.InkEnabled = true;
            }
        }

        void inqCanvas_PreviewMouseMove(object sender, MouseEventArgs e) {
            if(_moving) {
                using(_mrec.BatchEditNoRecog(false)) {
                    Selected.Contents.MoveTo(e.GetPosition(inqCanvas));
                }
                e.Handled = true;
            }
        }

        void inqCanvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if(_moving == true) { // could be set by stylus going down
                Mouse.Capture(inqCanvas); // stylus doesn't capture mouse
                e.Handled = true;
                return;
            }
            if(Selected.Contents != null && Selected.Contents.Outline != null && Selected.Contents.Outline.GetBounds().Contains(e.GetPosition(inqCanvas))) {
                Mouse.Capture(inqCanvas);
                StartMove(e.GetPosition(inqCanvas));
                e.Handled = true;
            }
        }

        void inqCanvas_PreviewStylusDown(object sender, StylusDownEventArgs e) {
            if(Selected.Contents != null && Selected.Contents.Outline != null && Selected.Contents.Outline.GetBounds().Contains(e.GetPosition(inqCanvas))) {
                StartMove(e.GetPosition(inqCanvas));
                inqCanvas.InkEnabled = false;
                e.Handled = true;
            }
        }

        private BatchLock _movingLock = null;
        void StartMove(Pt p) {
            _moving = true;
            StroqSel ss = Selected.Contents as StroqSel;
            if(ss != null && ss.AllStroqs.Count > 10) _movingLock = _mrec.BatchEdit();
            Selected.Contents.StartMove(p);
            inqCanvas.Stroqs.Remove(Selected.Contents.Outline);
        }

        Deg fpdangle(Pt a, Pt b, Vec v) {
            return (a-b).Normalized().UnsignedAngle(v.Normalized());
        }

        void inqCanvas_StroqCollected(object sender, InqCanvas.StroqCollectedEventArgs e) {
            /* filter out gestures before taking everything else as math */

            /* If we get here, it's a real stroke (not movement), so deselect any selection */
            Deselect();

            /* check for scribble delete */
            if(ScribbleDelete(e)) return;

            /* check for lassos/circles around stuff */
            if(LassoSelect(e)) return;

            _mathStroqs.Add(e.Stroq);
        }

        private bool ScribbleDelete(InqCanvas.StroqCollectedEventArgs e) {
            bool canBeScribble = e.Stroq.OldPolylineCusps().Length > 4;
            if(e.Stroq.OldPolylineCusps().Length == 4) {
                int[] pcusps = e.Stroq.OldPolylineCusps();
                Deg a1 = fpdangle(e.Stroq[0], e.Stroq[pcusps[1]], e.Stroq[pcusps[2]] - e.Stroq[pcusps[1]]);
                Deg a2 = fpdangle(e.Stroq[pcusps[1]], e.Stroq[pcusps[1]], e.Stroq[pcusps[3]] - e.Stroq[pcusps[1]]);
                if(a1 < 35 && a2 < 35)
                    canBeScribble = e.Stroq.BackingStroke.HitTest(e.Stroq.ConvexHull().First(), 1);
            }
            if(canBeScribble) {
                IEnumerable<Pt> hull = e.Stroq.ConvexHull();
                StroqCollection stqs = inqCanvas.Stroqs.HitTest(hull, 1);
                if(stqs.Count > 1) {
                    inqCanvas.Stroqs.Remove(stqs);
                    _mathStroqs.Remove(stqs);

                    inqCanvas.Stroqs.Remove(e.Stroq);
                    return true;
                }
            }
            return false;
        }

        private bool LassoSelect(InqCanvas.StroqCollectedEventArgs e) {
            if(e.Stroq.OldPolylineCusps().Length <= 4 && e.Stroq.Count > 4) {
                Stroq estroq = e.Stroq;
                CuspDetector.CuspSet cs = CuspDetector.FeaturePoints(estroq);

                Pt[] first = new Pt[cs.pts.Count / 2];
                for(int i = 0; i < first.Length; i++)
                    if(cs.distances[i] > cs.dist/2)
                        break;
                    else first[i] = cs.pts[i];
                Pt[] second = new Pt[cs.pts.Count - first.Length];
                for(int j = 0; j < second.Length; j++) second[j] = cs.pts[first.Length + j];
                Stroq s1 = new Stroq(first);
                Stroq s2 = new Stroq(second);
                float d1, d2;
                s1.OldNearestPoint(s2[-1], out d1);
                s2.OldNearestPoint(s1[0], out d2);
                if(Math.Min(d1, d2) / Math.Max(estroq.GetBounds().Width, estroq.GetBounds().Height) < 0.3f) {
                    StroqCollection stqs = _mathStroqs.HitTest(estroq, 50);
                    StroqCollection stqs2 = _mathStroqs.HitTest(estroq.Reverse1(), 50);
                    if(stqs2.Count > stqs.Count)
                        stqs = stqs2;
                    stqs.Remove(estroq);
                    StroqCollection stqs3 = new StroqCollection(stqs.Where((Stroq s) => _mrec.Charreco.Classification(_mrec.Sim[s]) != null));
                    stqs =stqs3;
                    Recognition rtemp = _mrec.ClassifyOneTemp(estroq);
                    if(stqs.Count > 0 && (rtemp == null || !rtemp.alts.Contains(new Recognition.Result(Unicode.S.SQUARE_ROOT)))) {
                        if(rtemp != null) Console.WriteLine("select recognized for " + rtemp.allograph);
                        Deselect();

                        estroq.BackingStroke.DrawingAttributes.Color = Colors.Purple;
                        Selected.Contents = new StroqSel(stqs, estroq, (Stroq s) => _mrec.Charreco.Classification(_mrec.Sim[s]),
                            (Recognition r) => _mrec.Sim[r.strokes], inqCanvas.Stroqs);
                        StroqSel Sel = (StroqSel)Selected.Contents;
                        HashSet<Recognition> recogs = new HashSet<Recognition>(Sel.AllStroqs.Select((Stroq s) => _mrec.Charreco.Classification(_mrec.Sim[s]))
                            .Where((Recognition r) => r != null));
                        if(recogs.Count != 0) showSidebarAlts(recogs, Sel.AllStroqs);
                        return true;
                    } else {
                        // Generic additional selections would be called here.
                        return false;
                    }
                }
            }
            return false;
        }

        AlternatesMenuCreator _altsMenuCrea;

        private void hideSidebarAlts() {
            _altsMenuCrea.Clear();
        }

        private void showSidebarAlts(ICollection<Recognition> recogs, StroqCollection stroqs) {
            _altsMenuCrea.Populate(recogs, stroqs);
        }

        public Selection Selected = new Selection();
        public void Deselect() {
            Selected.Contents = null;
            hideSidebarAlts();
        }

        private void clearMenu_Click(object sender, RoutedEventArgs e) {
            _mathStroqs.Clear();
            inqCanvas.Stroqs.Clear();
            inqCanvas.Children.Clear();
            underlay.Children.Clear();
            _colorizer.Reset();
        }

        private void quitMenu_Click(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
        }

        private void newMenu_Click(object sender, RoutedEventArgs e) {
            (new Window1()).Show();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            _mrec.Charreco.InkPixel = (float)OldStrokeStuff.Scale;
        }

        public Rct bbox(Strokes stks) {
            return _mrec.Sim[stks].Aggregate(Rct.Null, (Rct r, Stroq s) => r.Union(s.GetBounds()));
        }
        private void _mrec_ParseUpdated(MathRecognition source, Recognition chchanged, bool updateMath) {
            /* Evaluate math if necessary */
            if(updateMath)
                try {
                    Evaluator.UpdateMath(_mrec.Ranges.Select((Parser.Range r) => r.Parse));
                } catch { }

            /* reset geometry displayed: range displays, etc */
            underlay.Children.Clear();
            inqCanvas.Children.Clear();

            /* set up to draw background yellow thing for range displays */
            Brush fill3 = new SolidColorBrush(Color.FromArgb(50, 255, 255, 180));
            Brush fill2 = new SolidColorBrush(Color.FromArgb(75, 255, 255, 180));
            Brush fill1 = new SolidColorBrush(Color.FromArgb(100, 255, 255, 180));
            Brush sqr3 = new SolidColorBrush(Color.FromArgb(50, 0, 255, 0));
            Brush sqr2 = new SolidColorBrush(Color.FromArgb(75, 0, 255, 0));
            Brush sqr1 = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0));
            foreach(Parser.Range rrr in _mrec.Ranges) {
                Rct rangebbox = bbox(rrr.Strokes);
                Rct box = rangebbox.Inflate(8, 8);

                /* draw yellow box */
                DrawingVisual dv = new DrawingVisual();
                DrawingContext dc = dv.RenderOpen();
                dc.DrawRoundedRectangle(fill3, null, box, 4, 4);
                dc.DrawRoundedRectangle(fill2, null, box.Inflate(-4, -4), 4, 4);
                dc.DrawRoundedRectangle(fill1, null, box.Inflate(-8, -8), 4, 4);
                dc.Close();
                underlay.Children.Add(dv);

                if(rrr.Parse != null) {
                    /* draw interpretation of entry */
                    if(rrr.Parse.expr != null) {
                        dv = new DrawingVisual();
                        dc = dv.RenderOpen();
                        // this is an example of normal drawing of an expr
                        Rct nombb = starPadSDK.MathExpr.ExprWPF.EWPF.DrawTop(rrr.Parse.expr, 22, dc, Colors.Blue, new Pt(box.Left, box.Bottom+24), true);
                        dc.Close();
                        underlay.Children.Add(dv);
                    }

                    /* draw result of computation, if any */
                    if(rrr.Parse.finalSimp != null) {
                        Rct nombb;
                        Expr result = rrr.Parse.matrixOperationResult == null ? rrr.Parse.finalSimp : rrr.Parse.matrixOperationResult;
                        // this is an example of drawing an expr by getting a geometry of it first, so can be used for special effects, etc.
                        Geometry g = starPadSDK.MathExpr.ExprWPF.EWPF.ComputeGeometry(result, 22, out nombb);
                        Path p = new Path();
                        p.Data = g;
                        p.Stroke = Brushes.Red;
                        p.Fill = Brushes.Transparent;
                        p.StrokeThickness = 1;
                        p.RenderTransform = new TranslateTransform(box.Right + 10, box.Center.Y);
                        inqCanvas.Children.Add(p);
                    }

                    /* colorize ink. Ideally we would have kept track of which ink strokes had changes and only update colorization in those ranges affected
                     * by the changes. */
                    if(rrr.Parse.root != null) _colorizer.Colorize(rrr.Parse.root, rrr.Strokes, _mrec);
                }
            }

            /* Update alternates menu if user wrote a char */
            if(chchanged != null) {
                showSidebarAlts(new[] { chchanged }, new StroqCollection(_mrec.Sim[chchanged.strokes]));
            }
#if false
            /* print out log of current 1st-level parse, for debugging */
            List<string> resstrs = new List<string>();
            foreach(Parser.Range r in _mrec.Ranges) {
                Parser.ParseResult p = r.Parse;
                if(p != null && p.root != null)
                    resstrs.Add(p.root.Print());
            }
            if(resstrs.Count > 0) Console.WriteLine(resstrs.Aggregate((string a, string b) => a + " // " + b));
            foreach(Parser.Range r in _mrec.Ranges) {
                Parser.ParseResult pr = r.Parse;
                if(pr != null && pr.expr != null) Console.WriteLine(Text.Convert(pr.expr));
            }
#endif
        }
        private starPadSDK.MathUI.InkColorizer _colorizer = new starPadSDK.MathUI.InkColorizer();

        private void reparseMenu_Click(object sender, RoutedEventArgs e) {
            _mrec.ForceParse();
        }
    }
}
