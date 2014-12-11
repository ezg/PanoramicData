using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using starPadSDK.CharRecognizer;
using starPadSDK.UnicodeNs;
using starPadSDK.MathRecognizer;
using Microsoft.Ink;
using starPadSDK.MathExpr;
using starPadSDK;
using starPadSDK.Inq;
using starPadSDK.Utils;
using starPadSDK.Inq.MSInkCompat;
using System.Diagnostics;
using System.Drawing;

namespace starPadSDK.MathRecognizer {
    public class MathRecognition : IDisposable {
        private StroqCollection _stroqs;
        public StroqInkMapper Sim { get; private set; }
        public FeaturePointDetector Charreco { get; private set; }
        public Parser MyParser { get; private set; }
        /// <summary>
        /// increased by one every time a change is made by the lowest level changers--ie, ignoring BatchEditing
        /// </summary>
        [NonSerialized]
        private ulong _changeSeq = 0;
        private bool BatchEditing { get { return _batchLevel > 0; } }
        [NonSerialized]
        private int _batchLevel = 0;
        [NonSerialized]
        private Strokes _updatedStrokes = null, _updateStartStrokes = null;
        /// <summary>
        /// Allows multiple changes to the stroqs to be made without reparsing or rerecognition until the end.
        /// In the case of nested BatchEdits, there is only one reparse/reco made, at the end of the top level BatchEdit.
        /// The callback is made only if changes to the stroqs were made. If an exception is thrown from the delegate after a change has been made
        /// <em>the callback will still be made</em> on the way up the stack from the exception.
        /// </summary>
        /// <param name="fn">Lambda functions should work well here.</param>
        public void BatchEdit(Action fn) { using(var x = BatchEdit()) fn(); }
        /// <summary>
        /// Allows multiple changes to the stroqs to be made without reparsing or rerecognition until the end. You must call Dispose() on the value
        /// returned from this method to allow parsing again. In the case of nested BatchEdits,
        /// there is only one reparse/reco made, at the end of the top level BatchEdit. The reparse is made only if changes to the stroqs were made.
        /// You can either store the return value somewhere and
        /// call Dispose() on it directly, if your lock is over a period of time, or wrap your use in a using() block
        /// like so: using(var xxx = &lt;your mrecog&gt;.BatchEdit()) { &lt;your code goes here&gt; } (though you can name the variable anything instead of xxx).
        /// </summary>
        public BatchLock BatchEdit() { return BatchEdit(true, true); }
        /// <summary>
        /// Allows multiple changes to the stroqs to be made without reparsing or rerecognition until the end. This variant does not rerecognize even at the end.
        /// Therefore, you should only make changes that move existing characters around (by moving all their Stroqs with Move() or XformBy()).
        /// In the case of nested BatchEdits, there is only one reparse/reco made, at the end of the top level BatchEdit.
        /// The callback is made only if changes to the stroqs were made. If an exception is thrown from the delegate after a change has been made
        /// <em>the callback will still be made</em> on the way up the stack from the exception.
        /// </summary>
        /// <param name="fn">Lambda functions should work well here.</param>
        public void BatchEditNoRecog(Action fn, bool updatemath) { using(var x = BatchEditNoRecog(updatemath)) fn(); }
        /// <summary>
        /// Allows multiple changes to the stroqs to be made without reparsing or rerecognition until the end. This variant does not rerecognize even at the end.
        /// Therefore, you should only make changes that move existing characters around (by moving all their Stroqs with Move() or XformBy()).
        /// You must call Dispose() on the value
        /// returned from this method to allow parsing again. In the case of nested BatchEdits,
        /// there is only one reparse/reco made, at the end of the top level BatchEdit. The reparse is made only if changes to the stroqs were made.
        /// You can either store the return value somewhere and
        /// call Dispose() on it directly, if your lock is over a period of time, or wrap your use in a using() block
        /// like so: using(var xxx = &lt;your mrecog&gt;.BatchEdit()) { &lt;your code goes here&gt; } (though you can name the variable anything instead of xxx).
        /// </summary>
        public BatchLock BatchEditNoRecog(bool updatemath) { return BatchEdit(false, updatemath); }
        protected BatchLock BatchEdit(bool dorecog, bool updatemath) {
            if(_updatedStrokes != null && _updatedStrokes.Count > 0) {
                Trace.Assert(BatchEditing);
            } else {
                _updatedStrokes = Sim.Ink.CreateStrokes();
                _updateStartStrokes = Sim.Ink.CreateStrokes(Sim.Ink.Strokes.Cast<Stroke>().Select((Stroke s) => s.Id).ToArray());
            }
            // Parse should not be called in the case where we're in an enclosing batch edit
            return new BatchLock(new BatchEditLockProxy(this), () => BatchEditEnding(dorecog, updatemath));
        }
        private void BatchEditEnding(bool dorecog, bool updatemath) {
            Strokes del = Sim.Ink.CreateStrokes();
            Strokes addorchange = Sim.Ink.CreateStrokes();
            foreach(Stroke s in _updatedStrokes) {
                if(s.Deleted) {
                    if(_updateStartStrokes.Contains(s)) del.Add(s);
                } else {
                    addorchange.Add(s);
                }
            }

            if(dorecog) {
                if(del.Count > 0) DoDeleteGesture(del);
                if(addorchange.Count > 0) {
                    //MyParser.Ranges.Clear();
                    Charreco.Reset(addorchange);
                }
            } else {
                // Like the comment said, BatchEditNoRecog is really only for moving characters around, so go through and update the recog's notion
                // of where it is so parsing will work.
                HashSet<Recognition> recogs = new HashSet<Recognition>();
                Size orig = Size.Empty;
                foreach(Stroke s in addorchange) {
                    Recognition r = Charreco.Classification(s);
                    if(r != null && !recogs.Contains(r)) {
                        recogs.Add(r);
                        Point rl = r.bbox.Location;
                        Point sl = r.strokes.GetBoundingBox().Location;
                        Size delta = new Size(sl.X - rl.X, sl.Y - rl.Y);
                        if(delta != Size.Empty) {
                            r.Offset(delta.Width, delta.Height);
                            Trace.Assert(orig == Size.Empty || orig == delta);
                            orig = delta;
                        }
                    }
                }
            }
            if(del.Count > 0) Parse(del, addorchange.Count > 0 ? false : updatemath);
            if(addorchange.Count > 0) Parse(addorchange, updatemath);

            _updatedStrokes = null;
            _updateStartStrokes = null;
        }
        private class BatchEditLockProxy : IBatchLockable {
            public int BatchLevel { get { return _m._batchLevel; } set { _m._batchLevel = value; } }
            public ulong ChangeSeq { get { return _m._changeSeq; } }
            public string Name { get { return "MathRecognition"; } }
            private MathRecognition _m;
            public BatchEditLockProxy(MathRecognition m) { _m = m; }
        }
        private bool _disposed = false;
        public MathRecognition(StroqCollection stroqs) {
            _stroqs = stroqs;
            Sim = new StroqInkMapper(_stroqs);
            MyParser = new Parser((Charreco = new FeaturePointDetector()), Sim.Ink);
            Sim.StrokesAdded += sim_StrokesAdded;
            Sim.StrokesDeleting += sim_StrokesDeleting;
            Sim.StrokeTransformed += sim_StrokeTransformed;
            Charreco.Ignorable = Sim.Ink.CreateStrokes();
            if(_stroqs.Count > 0) ManyAdded(Sim.Ink.Strokes);
        }
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
                    MyParser.Dispose();
                    Sim.Dispose();
                }

                // Dispose unmanaged resources
                // But there are none for this class, so no code here.

                _disposed = true;
            }
        }
        // This destructor will run only if the Dispose method does not get called.
        // Do not provide destructors in types derived from this class.
        ~MathRecognition() {
            Dispose(false);
        }
        /// <summary>
        /// This function is optional, and should only be called once in your program in any case. It tries to load the libraries MathRecognition
        /// needs, which makes the program pause, so that you can control where the pause happens. For instance, if you call this at the beginning
        /// of your program, when the user later first draws a stroke the system will respond quicker.
        /// </summary>
        public void EnsureLoaded() {
            /* draw, classify, parse, and delete a dummy stroke in order to try to load libraries to eliminate the pause right after drawing a new stroke
             * that we would otherwise get.
             * *NOTE ALSO*: this code must ensure something in Parse2 is referenced or called or whatever to be sure it loads and its static constructor
             * runs so that the wordlist in the character recognizer is filled in properly.
             */
            Stroke dummy = Sim.Ink.CreateStroke(new Point[] { new Point(0, 10), new Point(100, 10), new Point(200, 200), new Point(200, 100), new Point(100, 200), new Point(0, 0) });
            Charreco.FullClassify(dummy);
            Strokes dummyss = Sim.Ink.CreateStrokes(new int[] { dummy.Id });
            Parse(dummyss, false, true, null);
            DoDeleteGesture(dummyss);
            Parse(dummyss, true, true, null);
        }
        private Recognition curSum = null;  // CJ: declare the current temporal summation
        private Recognition prevSum = null;
        private Recognition curIntegral = null;
        private Recognition prevIntegral = null;
        public List<Parser.Range> Ranges { get; private set; }
        public IEnumerable<Stroq> RangeStrokes(Parser.Range r)
        {
            return Sim[r.Strokes];
        }
        private Parser.ParseResult curParse = null;

        private void ManyAdded(Strokes ss) {
            Charreco.Reset(ss);
            Parse(ss, false, true, null);
        }
        public void ForceParse(bool rerecog=true) {
            if (rerecog)
                ResetInk();
            Parse(null, false, true, null);
        }
        public void ReRecogParse(Strokes updated, bool updatemath) {
            //MyParser.Ranges.Clear();
            Charreco.Reset(updated);
            Parse(updated, updatemath);
        }
        private void Parse(Strokes updated, bool updatemath) {
            Parse(updated, updated == null || updated.Count == 0 ? false : updated[0].Deleted, updatemath, null);
        }
        public void ResetInk()
        {
            MyParser.Ranges.Clear();
            MyParser.sumList.Clear();
            MyParser.integralList.Clear();
            MyParser.opSyms = new List<ParenSym>();
            if(MyParser.rpStrokes != null) MyParser.rpStrokes.Clear();
            Charreco.Ignorable = Sim.Ink.CreateStrokes();
            Charreco.Reset(Sim.Ink.Strokes);
        }
        private void sim_StrokesAdded(Strokes ss) {
            _changeSeq++;
            if(BatchEditing) {
                if(_updatedStrokes != null) _updatedStrokes.Add(ss);
                return;
            }
            if(ss.Count != 1) {
                ManyAdded(ss);
            } else {
                Stroke mis = ss[0];
                Recognition r = Charreco.FullClassify(mis);

                //CJ: for temporal parsing
                if(curSum != null && !curSum.strokes[0].Deleted) {
                    double overlap = (Math.Min(curSum.strokes.GetBoundingBox().Bottom, r.strokes.GetBoundingBox().Bottom)-
                        Math.Max(curSum.strokes.GetBoundingBox().Top, r.strokes.GetBoundingBox().Top)+0.0)/r.strokes.GetBoundingBox().Height;
                    if(overlap < 0.7 && r.strokes.GetBoundingBox().Top < curSum.strokes[0].GetBoundingBox().Top)
                        r.SuperId = curSum.strokes[0].Id;
                    else if(overlap < 0.7 && r.strokes.GetBoundingBox().Bottom > curSum.strokes[0].GetBoundingBox().Bottom)
                        r.SubId = curSum.strokes[0].Id;
                    else {
                        if(curSum.strokes[0].ExtendedProperties.Contains(Parser.TempGuid))
                            curSum.strokes[0].ExtendedProperties.Remove(Parser.TempGuid);
                        curSum = null;
                    }
                } else if(curIntegral != null && !curIntegral.strokes[0].Deleted) {
                    double overlap = (Math.Min(curIntegral.strokes.GetBoundingBox().Bottom, r.strokes.GetBoundingBox().Bottom)-
                        Math.Max(curIntegral.strokes.GetBoundingBox().Top, r.strokes.GetBoundingBox().Top)+0.0)/r.strokes.GetBoundingBox().Height;
                    if(overlap < 0.7 && r.strokes.GetBoundingBox().Top < curIntegral.strokes[0].GetBoundingBox().Top &&
                            (curIntegral.strokes[0].GetBoundingBox().Bottom- r.strokes.GetBoundingBox().Bottom+0.0)/curIntegral.strokes.GetBoundingBox().Height > .5)
                        r.SuperIntegralId = curIntegral.strokes[0].Id;
                    else if(overlap < 0.7 && r.strokes.GetBoundingBox().Bottom > curIntegral.strokes[0].GetBoundingBox().Bottom)
                        r.SubIntegralId = curIntegral.strokes[0].Id;
                    else {
                        prevIntegral = curIntegral;
                        if(curIntegral.strokes[0].ExtendedProperties.Contains(Parser.TempGuid))
                            curIntegral.strokes[0].ExtendedProperties.Remove(Parser.TempGuid);
                        curIntegral = null;
                    }
                }

                if(MyParser.integralList != null && MyParser.integralList.Count > 0) {//\int is part of a multi-stroke symbol, like 'f'
                    foreach(Recognition ir in MyParser.integralList)// allowing any integral to be part of an f symbol
                        if(ir != null && !ir.strokes[0].Deleted && r.strokes.Contains(ir.strokes[0])) {
                            if(curIntegral != null && curIntegral == ir) {
                                curIntegral.strokes[0].ExtendedProperties.Remove(Parser.TempGuid);
                                MyParser.integralList.Remove(curIntegral);
                                if(curIntegral != prevIntegral) {
                                    curIntegral = prevIntegral;
                                    if(curIntegral != null) curIntegral.strokes[0].ExtendedProperties.Add(Parser.TempGuid, 0);
                                } else curIntegral = null;
                            } else {
                                if(ir == prevIntegral) prevIntegral = null;
                                MyParser.integralList.Remove(ir);
                            }
                            break;
                        }
                }

                if(r != null && r.alt == Unicode.N.N_ARY_SUMMATION) {
                    if(curSum != null) { curSum.strokes[0].ExtendedProperties.Remove(Parser.TempGuid); curSum = null; }
                    if(curIntegral != null) { curIntegral.strokes[0].ExtendedProperties.Remove(Parser.TempGuid); curIntegral = null; }
                    prevSum = curSum;
                    curSum = r;
                    r.strokes[0].ExtendedProperties.Add(Parser.TempGuid, 0);
                    MyParser.sumList.Add(r);
                } else if(r != null && r.alt == Unicode.I.INTEGRAL) {
                    if(curSum != null) { curSum.strokes[0].ExtendedProperties.Remove(Parser.TempGuid); curSum = null; }
                    if(curIntegral != null) { curIntegral.strokes[0].ExtendedProperties.Remove(Parser.TempGuid); curIntegral = null; }
                    prevIntegral = curIntegral;
                    curIntegral = r;
                    r.strokes[0].ExtendedProperties.Add(Parser.TempGuid, 0);
                    MyParser.integralList.Add(r);
                }

                if(r != null) {
                    Strokes updateStrokes = Sim.Ink.CreateStrokes(new int[] { mis.Id });
                    Parse(updateStrokes, false, true, r);
                }
            }
        }
        private MathRecognizer.Symbol findSymbol(MathRecognizer.Line l, Recognition r) {
            foreach(MathRecognizer.Symbol s in l._syms) {
                if(s.r.guid == r.guid) {
                    return s;
                } else {
                    MathRecognizer.Symbol l1 = findSymbol(s.Super, r);
                    if(l1 != null)
                        return l1;
                    MathRecognizer.Symbol l2 = findSymbol(s.Sub, r);
                    if(l2 != null)
                        return l2;
                    if(s is IntSym) {
                        MathRecognizer.Symbol l3 = findSymbol(((IntSym)s).Integrand, r);
                        if(l3 != null)
                            return l3;
                    }
                    if(s is ParenSym) {
                        foreach(MathRecognizer.Line ll in ((ParenSym)s).lines) {
                            MathRecognizer.Symbol l3 = findSymbol(ll, r);
                            if(l3 != null)
                                return l3;
                        }
                        if(((ParenSym)s).Closing != null && ((ParenSym)s).Closing.r.guid == r.guid)
                            return ((ParenSym)s).Closing;
                    }
                }
            }
            return null;
        }
        private void Parse(Strokes update, bool delete, bool updateMath, Recognition chchanged) {
            Strokes mystrokes = Sim.Ink.Strokes;
            if(delete && update != null)
                mystrokes.Remove(update);
            foreach(Stroke s in mystrokes) {// make sure all automatic alternates are reset
                Recognition r = Charreco.Classification(s);
                if(r != null) {
                    if(r.levelsetby != 0 && r.MatrixId == -1) {
                        r.levelsetby = int.MaxValue;
                        r.curalt = 0;
                    }
                    r.parseError = false;
                }
            }
            int okay = 0;
            while(okay < 10) {
                try {
                    Ranges = MyParser.UpdateParse(update, mystrokes, (int)(Charreco.InkPixel));
                    okay = 10;
                } catch(RerecogException e) {
                    Charreco.Reset(e.Stks);
                    okay++;
                }
            }

            foreach(Parser.Range r in Ranges) {
                Parser.ParseResult pr = r.Parse;
                if(pr == null|| pr.expr == null) continue;
                if(pr.parseError) continue;
                ProcessMatrices(pr.expr);
            }

            if(MyParser.opSyms != null && MyParser.opSyms.Count > 0)
                foreach(Parser.Range r in Ranges) {
                    Parser.ParseResult pr = r.Parse;
                    if(pr == null || pr.root == null) continue;
                    foreach(ParenSym ps in MyParser.opSyms) {
                        foreach(Symbol sym in pr.root._syms)
                            if(sym.Sym.Character == '(' && sym.expr is ArrayExpr && sym.r.strokes[0].Id == ps.r.strokes[0].Id) {
                                updateExpr(ref pr.expr, sym.expr as ArrayExpr, ps.matrixOp);
                                break;
                            }
                    }
                }
            foreach (Parser.Range range in this.Ranges)
                range.resetID();
            if(ParseUpdated != null) ParseUpdated(this, chchanged, updateMath);
        }
        public delegate void ParseUpdatedHandler(MathRecognition source, Recognition chchanged, bool updateMath);
        public event ParseUpdatedHandler ParseUpdated;
        private Expr ProcessMatrices(Expr expr) {
            if(expr is ArrayExpr)
                return instantiateMatrix(expr as ArrayExpr);
            if(!(expr is CompositeExpr)) return expr;
            CompositeExpr ce = expr as CompositeExpr;
            int len = ce.Args.Length;
            Expr[] exprElms = new Expr[len];
            for(int i = 0; i < len; i++)
                exprElms[i] = ProcessMatrices(ce.Args[i]);
            return new CompositeExpr(ce.Head, exprElms);
        }
        private Expr updateExpr(ref Expr ep0, ArrayExpr ep1, String op) {
            Expr[] newExprElts = new Expr[2];
            newExprElts[0] = ep1;
            newExprElts[1] = new WordSym(op);

            Expr newExpr = new CompositeExpr(WellKnownSym.power, newExprElts);
            if(ep0 is ArrayExpr && (ep0 as ArrayExpr).Equals(ep1)) {
                ep0 = (op == "No Matrix Operation" || op == null)? ep0 : newExpr;
            } else if(ep0 is CompositeExpr) {
                CompositeExpr ce = ep0 as CompositeExpr;
                int c = ce.Args.Length;
                if(ce.Head == WellKnownSym.power && ce.Args[0].Equals(ep1)) {
                    if(!(ce.Args[1] is WordSym)) { // written power overwrites menu choice
                        if(ce.Args[1] is LetterSym && (ce.Args[1] as LetterSym).Letter == 'T')
                            return new CompositeExpr(WellKnownSym.power, ep1, new WordSym("Transpose"));
                        return ep0;
                    }
                    ep0 = op == "No Matrix Operation" ? ep1 : newExpr;
                    return ep0;
                }
                for(int i = 0; i < c; i++)
                    ce.Args[i] = updateExpr(ref ce.Args[i], ep1, op);
                ep0 = new CompositeExpr(ce.Head, ce.Args);
                return ep0;
            }
            return ep0;
        }

        private Expr instantiateMatrix(ArrayExpr ae) {
            int nd = 7;//default new dimensions of matrix
            Char lt;
            bool existEllipsis = false;
            bool existArrow = false;
            int rows = ae.Dims[0];
            int cols = ae.Dims[1];
            if(rows < 3) return ae;
            MathExpr.Expr[,] dummyArray = ((MathExpr.Expr[,])(ae.Elts));

            int arrowEntryLen = 0;
            int arrowLoc = 0;
            CompositeExpr ce = null;
            if(dummyArray[rows - 1, 0] is LetterSym && (dummyArray[rows - 1, 0] as LetterSym).Letter == '↘') existArrow = true;
            else if(dummyArray[rows - 1, 0] is CompositeExpr) {
                ce = (dummyArray[rows - 1, 0] as CompositeExpr);
                arrowEntryLen = ce.Args.Length;
                for(int i = 0; i < arrowEntryLen; i++)
                    if((ce.Args[i] is LetterSym &&(ce.Args[i] as LetterSym).Letter == '↘')) {
                        arrowLoc = i;
                        existArrow = true;
                        break;
                    }
            }
            if(existArrow) {
                if(cols > 2 || rows > 3) return ae;
                int newRowCount = nd;
                int newColCount = nd;
                Char colID = 'i';
                Char rowID = 'j';

                for(int i = 0; i < arrowLoc; i++)
                    if(ce.Args[i] is IntegerNumber)
                        newRowCount = (int)(ce.Args[i] as IntegerNumber).Num;
                    else if(ce.Args[i] is LetterSym)
                        rowID = (ce.Args[i] as LetterSym).Letter;
                for(int i = arrowLoc + 1; i < arrowEntryLen; i++)
                    if(ce.Args[i] is IntegerNumber)
                        newColCount = (int)(ce.Args[i] as IntegerNumber).Num;
                    else if(ce.Args[i] is LetterSym)
                        colID = (ce.Args[i] as LetterSym).Letter;

                Array arrowArray = Array.CreateInstance(typeof(MathExpr.Expr), newRowCount, newColCount);
                for(int m = arrowArray.GetLowerBound(0); m <= arrowArray.GetUpperBound(0); m++)
                    for(int n = arrowArray.GetLowerBound(1); n <= arrowArray.GetUpperBound(1); n++)
                        if(dummyArray.Length == 3) {
                            arrowArray.SetValue(simplify(newEntry(dummyArray[1, 0], m, n, rowID, colID)), m, n); //
                        } else if(dummyArray.Length == 6) {
                            if(m > n && !(dummyArray[1, 1] is NullExpr)) arrowArray.SetValue(dummyArray[1, 0], m, n); //dummyArray[1,0] is constant
                            else if(m < n && !(dummyArray[0, 1] is NullExpr)) arrowArray.SetValue(dummyArray[0, 1], m, n); //dummyArray[0, 1] is constant
                            else {
                                if(dummyArray[1, 1] is NullExpr)
                                    arrowArray.SetValue(simplify(newEntry(dummyArray[1, 0], m, n, rowID, colID)), m, n); //
                                else
                                    arrowArray.SetValue(simplify(newEntry(dummyArray[1, 1], m, n, rowID, colID)), m, n); //                     
                            }
                        }
                ae.Elts = (arrowArray as MathExpr.Expr[,]);
                return ae;
            }

            for(int i = 0; i < rows; i++) {
                for(int j = 0; j < cols; j++) {
                    if(dummyArray[i, j] is MathExpr.NullExpr) {
                        existEllipsis = false;
                        break;
                    }
                    if(dummyArray[i, j] is MathExpr.LetterSym) {
                        lt = (dummyArray[i, j] as MathExpr.LetterSym).Letter;
                        if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') {
                            existEllipsis = true;
                            break;
                        }
                    } else if(dummyArray[i, j] is MathExpr.CompositeExpr) {
                        MathExpr.CompositeExpr dummyEntry = (MathExpr.CompositeExpr)dummyArray[i, j];
                        if(!(dummyEntry.Args[0] is MathExpr.LetterSym))
                            continue;
                        lt = ((MathExpr.LetterSym)dummyEntry.Args[0]).Letter;
                        if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') {
                            existEllipsis = true;
                            break;
                        }
                    }
                }
                if(existEllipsis) break;
            }//end of for loop
            if(!existEllipsis) return ae;

            bool ready = true;
            for(int i = 0; i < rows; i++) {
                for(int j = 0; j < cols; j++) {
                    if(dummyArray[i, j] is NullExpr) {
                        ready = false;
                        break;
                    }
                }
                if(!ready) break;
            }
            if(!ready) return ae;

            Array newArray = Array.CreateInstance(typeof(MathExpr.Expr), nd, nd);

            //Toeplits matrix
            bool isToeplitz = true;
            if(rows != cols) isToeplitz = false;
            if(isToeplitz)
                for(int i = 0; i < rows; i++) {
                    Expr e = null;
                    for(int j = 0; j < cols - i; j++)
                        if(e == null)
                            if(dummyArray[i + j, j] is MathExpr.LetterSym) {
                                lt = (dummyArray[i + j, j] as MathExpr.LetterSym).Letter;
                                if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                                else e = dummyArray[i + j, j];
                            } else if(dummyArray[i + j, j] is MathExpr.CompositeExpr && (dummyArray[i + j, j] as MathExpr.CompositeExpr).Args[0] is MathExpr.LetterSym) {
                                lt = ((MathExpr.LetterSym)(dummyArray[i + j, j] as MathExpr.CompositeExpr).Args[0]).Letter;
                                if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                                else e = dummyArray[i + j, j];
                            } else e = dummyArray[i + j, j];
                        else if(!e.Equals(dummyArray[i + j, j]))
                            if(dummyArray[i + j, j] is MathExpr.LetterSym) {
                                lt = (dummyArray[i + j, j] as MathExpr.LetterSym).Letter;
                                if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                                else {
                                    isToeplitz = false;
                                    break;
                                }
                            } else if(dummyArray[i + j, j] is MathExpr.CompositeExpr && (dummyArray[i + j, j] as MathExpr.CompositeExpr).Args[0] is MathExpr.LetterSym) {
                                lt = ((MathExpr.LetterSym)(dummyArray[i + j, j] as MathExpr.CompositeExpr).Args[0]).Letter;
                                if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                                else {
                                    isToeplitz = false;
                                    break;
                                }
                            } else {
                                isToeplitz = false;
                                break;
                            }
                    if(!isToeplitz) break;
                }
            if(isToeplitz) {
                for(int i = 1; i < cols; i++) {
                    Expr e = null;
                    for(int j = 0; j < rows - i; j++)
                        if(e == null)
                            if(dummyArray[j, i + j] is MathExpr.LetterSym) {
                                lt = (dummyArray[j, i + j] as MathExpr.LetterSym).Letter;
                                if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                                else e = dummyArray[j, i + j];
                            } else if(dummyArray[j, i + j] is MathExpr.CompositeExpr && (dummyArray[j, i + j] as MathExpr.CompositeExpr).Args[0] is MathExpr.LetterSym) {
                                lt = ((MathExpr.LetterSym)(dummyArray[j, i + j] as MathExpr.CompositeExpr).Args[0]).Letter;
                                if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                                else e = dummyArray[j, i + j];
                            } else e = dummyArray[j, i + j];
                        else if(!e.Equals(dummyArray[j, i + j]))
                            if(dummyArray[j, i + j] is MathExpr.LetterSym) {
                                lt = (dummyArray[j, i + j] as MathExpr.LetterSym).Letter;
                                if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                                else {
                                    isToeplitz = false;
                                    break;
                                }
                            } else if(dummyArray[j, i + j] is MathExpr.CompositeExpr && (dummyArray[j, i + j] as MathExpr.CompositeExpr).Args[0] is MathExpr.LetterSym) {
                                lt = ((MathExpr.LetterSym)(dummyArray[j, i + j] as MathExpr.CompositeExpr).Args[0]).Letter;
                                if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                                else {
                                    isToeplitz = false;
                                    break;
                                }
                            } else {
                                isToeplitz = false;
                                break;
                            }
                    if(!isToeplitz) break;
                }
            }
            if(isToeplitz) {
                //setValue for the first column
                for(int m = newArray.GetLowerBound(0); m <= newArray.GetUpperBound(0); m++) {
                    if(m >= ae.Dims[0]) {
                        newArray.SetValue(new LetterSym('⋱'), m, 0);
                        continue;
                    }
                    for(int n = dummyArray.GetLowerBound(1); n <= dummyArray.GetUpperBound(1) - m; n++) {
                        if(dummyArray[m + n, n] is MathExpr.LetterSym) {
                            lt = (dummyArray[m + n, n] as MathExpr.LetterSym).Letter;
                            if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                        }
                        if(dummyArray[m + n, n] is MathExpr.CompositeExpr && (dummyArray[m + n, n] as MathExpr.CompositeExpr).Args[0] is MathExpr.LetterSym) {
                            lt = ((MathExpr.LetterSym)(dummyArray[m + n, n] as MathExpr.CompositeExpr).Args[0]).Letter;
                            if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                        }
                        newArray.SetValue(dummyArray[m + n, n], m, 0);
                        break;
                    }
                    if(newArray.GetValue(m, 0) == null) newArray.SetValue(new LetterSym('⋱'), m, 0);
                }
                //setValue for the first row
                for(int n = newArray.GetLowerBound(1) + 1; n <= newArray.GetUpperBound(1); n++) {
                    if(n >= ae.Dims[1]) {
                        newArray.SetValue(new LetterSym('⋱'), 0, n);
                        continue;
                    }
                    for(int m = dummyArray.GetLowerBound(1); m <= dummyArray.GetUpperBound(1) - n; m++) {
                        if(dummyArray[m, m + n] is MathExpr.LetterSym) {
                            lt = (dummyArray[m, m + n] as MathExpr.LetterSym).Letter;
                            if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                        }
                        if(dummyArray[m, m + n] is MathExpr.CompositeExpr && (dummyArray[m, m + n] as MathExpr.CompositeExpr).Args[0] is MathExpr.LetterSym) {
                            lt = ((MathExpr.LetterSym)(dummyArray[m, m + n] as MathExpr.CompositeExpr).Args[0]).Letter;
                            if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                        }
                        newArray.SetValue(dummyArray[m, m + n], 0, n);
                        break;
                    }
                    if(newArray.GetValue(0, n) == null) newArray.SetValue(new LetterSym('⋱'), 0, n);
                }
                for(int m = newArray.GetLowerBound(0) + 1; m <= newArray.GetUpperBound(0); m++)
                    for(int n = newArray.GetLowerBound(1) + 1; n <= newArray.GetUpperBound(1); n++)
                        newArray.SetValue((newArray as MathExpr.Expr[,])[m - 1, n - 1], m, n);
                ae.Elts = (newArray as MathExpr.Expr[,]);
                return ae;
            }
            //Hankel matrix
            bool isHankel = true;
            if(rows != cols) isHankel = false;
            if(isHankel)
                for(int i = 0; i < rows; i++) {
                    Expr e = null;
                    for(int j = 0; j < i; j++)
                        if(e == null)
                            if(dummyArray[i - j, j] is MathExpr.LetterSym) {
                                lt = (dummyArray[i - j, j] as MathExpr.LetterSym).Letter;
                                if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                                else e = dummyArray[i - j, j];
                            } else if(dummyArray[i - j, j] is MathExpr.CompositeExpr && (dummyArray[i - j, j] as MathExpr.CompositeExpr).Args[0] is MathExpr.LetterSym) {
                                lt = ((MathExpr.LetterSym)(dummyArray[i - j, j] as MathExpr.CompositeExpr).Args[0]).Letter;
                                if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                                else e = dummyArray[i - j, j];
                            } else e = dummyArray[i - j, j];
                        else if(!e.Equals(dummyArray[i - j, j]))
                            if(dummyArray[i - j, j] is MathExpr.LetterSym) {
                                lt = (dummyArray[i - j, j] as MathExpr.LetterSym).Letter;
                                if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                                else {
                                    isHankel = false;
                                    break;
                                }
                            } else if(dummyArray[i - j, j] is MathExpr.CompositeExpr && (dummyArray[i - j, j] as MathExpr.CompositeExpr).Args[0] is MathExpr.LetterSym) {
                                lt = ((MathExpr.LetterSym)(dummyArray[i - j, j] as MathExpr.CompositeExpr).Args[0]).Letter;
                                if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                                else {
                                    isHankel = false;
                                    break;
                                }
                            } else {
                                isHankel = false;
                                break;
                            }
                    if(!isHankel) break;
                }
            if(isHankel) {
                for(int i = 1; i < cols; i++) {
                    Expr e = null;
                    for(int j = rows - 1; j > i - 1; j--)
                        if(e == null)
                            if(dummyArray[j, cols - 1 + i - j] is MathExpr.LetterSym) {
                                lt = (dummyArray[j, cols - 1 + i - j] as MathExpr.LetterSym).Letter;
                                if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                                else e = dummyArray[j, cols - 1 + i - j];
                            } else if(dummyArray[j, cols - 1 + i - j] is MathExpr.CompositeExpr && (dummyArray[j, cols - 1 + i - j] as MathExpr.CompositeExpr).Args[0] is MathExpr.LetterSym) {
                                lt = ((MathExpr.LetterSym)(dummyArray[j, cols - 1 + i - j] as MathExpr.CompositeExpr).Args[0]).Letter;
                                if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                                else e = dummyArray[j, cols - 1 + i - j];
                            } else e = dummyArray[j, cols - 1 + i - j];
                        else if(!e.Equals(dummyArray[j, cols - 1 + i - j]))
                            if(dummyArray[j, cols - 1 + i - j] is MathExpr.LetterSym) {
                                lt = (dummyArray[j, cols - 1 + i - j] as MathExpr.LetterSym).Letter;
                                if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                                else {
                                    isHankel = false;
                                    break;
                                }
                            } else if(dummyArray[j, cols - 1 + i - j] is MathExpr.CompositeExpr && (dummyArray[j, cols - 1 + i - j] as MathExpr.CompositeExpr).Args[0] is MathExpr.LetterSym) {
                                lt = ((MathExpr.LetterSym)(dummyArray[j, cols - 1 + i - j] as MathExpr.CompositeExpr).Args[0]).Letter;
                                if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                                else {
                                    isHankel = false;
                                    break;
                                }
                            } else {
                                isHankel = false;
                                break;
                            }
                    if(!isHankel) break;
                }
            }

            if(isHankel) {
                //setValue for the first column(starting from the second row), and the last row if necessary
                for(int m = 1; m <= rows + cols - 2; m++) {
                    bool hasVal = false;
                    for(int n = dummyArray.GetLowerBound(1) + Math.Max(0, m - dummyArray.GetUpperBound(0)); n <= Math.Min(m, dummyArray.GetUpperBound(1)); n++) {
                        if(dummyArray[m - n, n] is MathExpr.LetterSym) {
                            lt = (dummyArray[m - n, n] as MathExpr.LetterSym).Letter;
                            if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                        }
                        if(dummyArray[m - n, n] is MathExpr.CompositeExpr && (dummyArray[m - n, n] as MathExpr.CompositeExpr).Args[0] is MathExpr.LetterSym) {
                            lt = ((MathExpr.LetterSym)(dummyArray[m - n, n] as MathExpr.CompositeExpr).Args[0]).Letter;
                            if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                        }
                        hasVal = true;
                        if(m < rows) dummyArray.SetValue(dummyArray[m - n, n], m, 0);
                        else dummyArray.SetValue(dummyArray[m - n, n], rows - 1, m - rows + 1);
                        break;
                    }
                    if(!hasVal) { //make ellipses consistent
                        if(m < rows) dummyArray.SetValue(new LetterSym('⋰'), m, 0);
                        else dummyArray.SetValue(new LetterSym('⋰'), rows - 1, m - rows + 1);
                    }
                }
                for(int n = newArray.GetLowerBound(1); n <= newArray.GetUpperBound(1); n++)
                    for(int m = newArray.GetLowerBound(0); m <= newArray.GetUpperBound(0); m++) {
                        if(m + n > rows + cols - 2) {
                            newArray.SetValue(new LetterSym('⋰'), m, n);
                            continue;
                        }
                        if(m + n < rows) newArray.SetValue(dummyArray[m + n, 0], m, n);
                        else newArray.SetValue(dummyArray[rows - 1, m + n - rows + 1], m, n);
                    }
                ae.Elts = (newArray as MathExpr.Expr[,]);
                return ae;
            }

            //Other matrices, including Vandermonde matrix
            bool found = false;
            int ii = 0, jj = 0, mm = 0, nn = 0;
            Expr pEntry0, pEntry1, nEntry;
            for(jj = cols - 1; jj >= 0; jj--) {
                for(ii = rows - 1; ii >= 0; ii--) {
                    if(dummyArray[ii, jj] is MathExpr.LetterSym) {
                        lt = (dummyArray[ii, jj] as MathExpr.LetterSym).Letter;
                        if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                    }
                    if(dummyArray[ii, jj] is MathExpr.CompositeExpr && (dummyArray[ii, jj] as MathExpr.CompositeExpr).Args[0] is MathExpr.LetterSym) {
                        lt = ((MathExpr.LetterSym)(dummyArray[ii, jj] as MathExpr.CompositeExpr).Args[0]).Letter;
                        if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') continue;
                    }

                    for(mm = 0; mm < rows; mm++) {
                        if(dummyArray[mm, jj] is MathExpr.LetterSym) {
                            lt = (dummyArray[mm, jj] as MathExpr.LetterSym).Letter;
                            if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') break;
                        }
                        if(dummyArray[mm, jj] is MathExpr.CompositeExpr && (dummyArray[mm, jj] as MathExpr.CompositeExpr).Args[0] is MathExpr.LetterSym) {
                            lt = ((MathExpr.LetterSym)(dummyArray[mm, jj] as MathExpr.CompositeExpr).Args[0]).Letter;
                            if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') break;
                        }
                    }
                    for(nn = 0; nn < cols; nn++) {
                        if(dummyArray[ii, nn] is MathExpr.LetterSym) {
                            lt = (dummyArray[ii, nn] as MathExpr.LetterSym).Letter;
                            if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') break;
                        }
                        if(dummyArray[ii, nn] is MathExpr.CompositeExpr && (dummyArray[ii, nn] as MathExpr.CompositeExpr).Args[0] is MathExpr.LetterSym) {
                            lt = ((MathExpr.LetterSym)(dummyArray[ii, nn] as MathExpr.CompositeExpr).Args[0]).Letter;
                            if(lt == '⋯' || lt == '⋮' || lt == '⋰' || lt == '⋱') break;
                        }
                    }
                    if(mm > 0 && mm < ii && nn > 0 && nn < jj) {
                        found = true;
                        break;
                    }
                }
                if(found) break;
            }

            if(found) {
                pEntry0 = patternEntry(mm - 1, '~', dummyArray[mm - 1, jj], dummyArray[ii, jj]); //Entries at different rows 
                pEntry1 = patternEntry(nn - 1, '@', dummyArray[ii, nn - 1], dummyArray[ii, jj]); //Entries at different columns
                if(isNull(pEntry0) || isNull(pEntry1)) found = false;
                if(found) {
                    for(ii = 0; ii < nd; ii++) {
                        for(jj = 0; jj < nd; jj++) {
                            nEntry = newEntry(ii, jj, pEntry0, pEntry1);
                            newArray.SetValue(simplify(nEntry), ii, jj);
                        }
                    }
                    ae.Elts = (newArray as MathExpr.Expr[,]);
                }
            }
            return ae;
        }
        private bool isNull(MathExpr.Expr e) {
            if(e == null) return true;
            if(e is IntegerNumber) return false;
            if(e is LetterSym && (e as LetterSym).Subscript == null) return true;
            if(e is CompositeExpr) {
                int len = (e as CompositeExpr).Args.Length;
                for(int i = 0; i < len; i++) {
                    if(isNull((e as CompositeExpr).Args[i])) return true;
                }
            }
            return false;
        }
        private MathExpr.Expr simplify(MathExpr.Expr e) {
            if(e is IntegerNumber || (e is LetterSym && (e as LetterSym).Subscript is NullExpr) || (e is LetterSym && (e as LetterSym).Subscript is IntegerNumber)) return e;
            if((e is LetterSym && (e as LetterSym).Subscript is LetterSym) || (e is LetterSym && (e as LetterSym).Subscript is CompositeExpr))
                return new LetterSym((e as LetterSym).Letter, new NoAccent(), simplify((e as LetterSym).Subscript), Format.Normal, null);
            if(e is CompositeExpr && (e as CompositeExpr).Head == WellKnownSym.power && (e as CompositeExpr).Args[1] == 0) return new IntegerNumber("1");
            if(e is CompositeExpr && (e as CompositeExpr).Head == WellKnownSym.power && (e as CompositeExpr).Args[1] == 1) return simplify((e as CompositeExpr).Args[0]);
            if(e is CompositeExpr && (e as CompositeExpr).Head == WellKnownSym.power && ((e as CompositeExpr).Args[0] == 0 || (e as CompositeExpr).Args[0] == 1)) return (e as CompositeExpr).Args[0];
            if(e is CompositeExpr && (e as CompositeExpr).Head == WellKnownSym.minus) return new CompositeExpr(WellKnownSym.minus, simplify((e as CompositeExpr).Args[0]));
            if(e is CompositeExpr && (e as CompositeExpr).Head == WellKnownSym.divide) return new CompositeExpr(WellKnownSym.divide, simplify((e as CompositeExpr).Args[0]));
            if(e is CompositeExpr) {
                Expr head = (e as CompositeExpr).Head;
                int len = (e as CompositeExpr).Args.Length;
                Expr[] args = new Expr[len];
                for(int i = 0; i < len; i++) {
                    args[i] = simplify((e as CompositeExpr).Args[i]);
                }
                if(head == new WordSym("comma")) return new CompositeExpr(head, args);
                int k = 0;
                for(int i = 1; i < len; i++) {
                    if((args[i - 1] is IntegerNumber || (args[i - 1] is CompositeExpr && (args[i - 1] as CompositeExpr).Head == WellKnownSym.minus && (args[i - 1] as CompositeExpr).Args[0] is IntegerNumber)) &&
                        (args[i] is IntegerNumber || (args[i] is CompositeExpr && (args[i] as CompositeExpr).Head == WellKnownSym.minus && (args[i] as CompositeExpr).Args[0] is IntegerNumber))) {
                        int a0, a1;
                        a0 = args[i - 1] is IntegerNumber ? (int)(args[i - 1] as IntegerNumber).Num : -(int)((args[i - 1] as CompositeExpr).Args[0] as IntegerNumber).Num;
                        a1 = args[i] is IntegerNumber ? (int)(args[i] as IntegerNumber).Num : -(int)((args[i] as CompositeExpr).Args[0] as IntegerNumber).Num;
                        if(head == WellKnownSym.power) args[i] = Math.Pow(a0, a1);
                        else if(head == WellKnownSym.plus) args[i] = a0 + a1;
                        else if(head == WellKnownSym.times) args[i] = a0 * a1;
                        args[i - 1] = null;
                        k++;
                    }
                }
                if(k == 0)
                    if(args[1] is IntegerNumber && (args[1] == 0 || args[1] == 1) && head == WellKnownSym.power) return simplify(new CompositeExpr(head, args));
                    else return new CompositeExpr(head, args);
                if(k == 1) return args[len - 1];
                Expr[] newArgs = new Expr[k];
                k = 0;
                for(int i = 0; i < len; i++) {
                    if(args[i] != null) {
                        newArgs[k] = args[i];
                        k++;
                    }
                }
                return new CompositeExpr(head, newArgs);
            }
            return e;
        }

        //replace rowId with m, colID with n for the newEntry
        private Expr newEntry(MathExpr.Expr e, int m, int n, Char rowId, Char colID) {
            return newEntry(e, m, n, rowId, colID, false);
        }

        private Expr newEntry(MathExpr.Expr e, int m, int n, Char rowId, Char colID, bool isSubscript) {
            if(e == null) return null;
            if(e is IntegerNumber) return e;
            if(e is LetterSym && (e as LetterSym).Letter == rowId && (e as LetterSym).Subscript is NullExpr) return new IntegerNumber(m);
            if(e is LetterSym && (e as LetterSym).Letter == colID && (e as LetterSym).Subscript is NullExpr) return new IntegerNumber(n);
            if(e is LetterSym && (e as LetterSym).Letter == colID && !((e as LetterSym).Subscript is NullExpr)) return e;
            if(e is LetterSym && (e as LetterSym).Letter != rowId && (e as LetterSym).Letter != colID && (e as LetterSym).Subscript is NullExpr) return e;
            if(e is LetterSym && (e as LetterSym).Letter != rowId && (e as LetterSym).Letter != colID && !((e as LetterSym).Subscript is NullExpr))
                return new LetterSym((e as LetterSym).Letter, new NoAccent(), newEntry((e as LetterSym).Subscript, m, n, rowId, colID, true), Format.Normal, null);
            if(e is CompositeExpr) {
                CompositeExpr ce = e as CompositeExpr;
                Expr head = ce.Head;
                int len = ce.Args.Length;
                if(isSubscript && head == WellKnownSym.times) {
                    head = new WordSym("comma");
                    for(int i = 0; i < ce.Args.Length; i++)
                        if(ce.Args[i] is LetterSym && ((ce.Args[i] as LetterSym).Letter == ')'||(ce.Args[i] as LetterSym).Letter == '.')) len--;
                }

                Expr[] args = new Expr[len];
                int k = 0;
                for(int i = 0; i < ce.Args.Length; i++)
                    if(!(ce.Args[i] is LetterSym) || ((ce.Args[i] as LetterSym).Letter != ')' && (ce.Args[i] as LetterSym).Letter != '.')) {
                        args[k] = newEntry(ce.Args[i], m, n, rowId, colID, isSubscript);
                        k++;
                    }
                return new CompositeExpr(head, args);
            }
            return e;
        }

        //replace ~ with m, @ with n for the newEntry
        private MathExpr.Expr newEntry(int m, int n, MathExpr.Expr e0, MathExpr.Expr e1) {
            if(e1 == null && e0 == null) return null;
            if(e0 != null && e1 != null && e0.Equals(e1)) return e0;
            if(e0 is LetterSym && (e0 as LetterSym).Letter.Equals('~')) return new IntegerNumber(m);//no subscript for integers
            if(e1 is LetterSym && (e1 as LetterSym).Letter.Equals('@')) return new IntegerNumber(n);//no subscript for integers
            if(e0 is LetterSym && !((e0 as LetterSym).Subscript is NullExpr)) {
                if((e0 as LetterSym).Subscript is LetterSym && ((e0 as LetterSym).Subscript as LetterSym).Letter == '~')
                    return new LetterSym((e0 as LetterSym).Letter, new NoAccent(), new IntegerNumber(m), Format.Normal, null);//subscript
                else return new LetterSym((e0 as LetterSym).Letter, new NoAccent(), newEntry(m, n, (e0 as LetterSym).Subscript, (e1 as LetterSym).Subscript), Format.Normal, null);//subscript
            }
            if(e0 is LetterSym && (e0 as LetterSym).Subscript is NullExpr && e1 != null) {
                return newEntry(m, n, null, e1);
            }


            if(e1 is LetterSym && !((e1 as LetterSym).Subscript is NullExpr)) {
                if((e1 as LetterSym).Subscript is LetterSym && ((e1 as LetterSym).Subscript as LetterSym).Letter == '@')
                    return new LetterSym((e1 as LetterSym).Letter, new NoAccent(), new IntegerNumber(n), Format.Normal, null);//subscript
                else return new LetterSym((e1 as LetterSym).Letter, new NoAccent(), newEntry(m, n, (e0 as LetterSym).Subscript, (e1 as LetterSym).Subscript), Format.Normal, null);//subscript
            }
            if(e1 is LetterSym && (e1 as LetterSym).Subscript is NullExpr && e0 != null) {
                return newEntry(m, n, e0, null);
            }

            if(e0 is CompositeExpr && e1 is CompositeExpr && (e0 as CompositeExpr).Head == (e1 as CompositeExpr).Head && (e0 as CompositeExpr).Args.Length == (e1 as CompositeExpr).Args.Length) {
                Expr head = (e0 as CompositeExpr).Head;
                int len = (e0 as CompositeExpr).Args.Length;
                Expr[] args = new Expr[len];
                for(int i = 0; i < len; i++) {
                    args[i] = newEntry(m, n, (e0 as CompositeExpr).Args[i], (e1 as CompositeExpr).Args[i]);
                }
                return new CompositeExpr(head, args);
            }
            if(e0 is IntegerNumber && e1 != null) return newEntry(m, n, null, e1);
            if(e1 is IntegerNumber && e0 != null) return newEntry(m, n, e0, null);

            if(e0 == null) {
                if(e1 is IntegerNumber) return e1;
                Expr head = (e1 as CompositeExpr).Head;
                int len = (e1 as CompositeExpr).Args.Length;
                Expr[] args = new Expr[len];
                for(int i = 0; i < len; i++) {
                    args[i] = newEntry(m, n, null, (e1 as CompositeExpr).Args[i]);
                }
                return new CompositeExpr(head, args);
            }
            if(e1 == null) {
                if(e0 is IntegerNumber) return e0;
                Expr head = (e0 as CompositeExpr).Head;
                int len = (e0 as CompositeExpr).Args.Length;
                Expr[] args = new Expr[len];
                for(int i = 0; i < len; i++) {
                    args[i] = newEntry(m, n, (e0 as CompositeExpr).Args[i], null);
                }
                return new CompositeExpr(head, args);
            }
            return null;
        }

        // Use ~ for row variable, @ for column variable. Assuming them not to appear in a matrix.
        // first arg k is the row or column number of e0 which is ahead of any ellipses, starting from 0.
        private MathExpr.Expr patternEntry(int k, char c, Expr e0, Expr e1) {
            if(e0.Equals(e1)) return e0;
            if(e0 is IntegerNumber) {
                if((e0 as IntegerNumber).Num == k) return new LetterSym(c);//let's assume it is not that complicated at the moment
                if(e1 is IntegerNumber) return e1;
                if(e1 is LetterSym) {
                    if(k == -1) return new LetterSym(c);
                    Expr head = WellKnownSym.plus;
                    Expr[] args = new Expr[2];
                    args[0] = new LetterSym(c);
                    args[1] = new IntegerNumber((e0 as IntegerNumber).Num - k);
                    return new CompositeExpr(head, args);
                }
                if(e1 is CompositeExpr) {
                    Expr head = (e1 as CompositeExpr).Head;
                    int len = (e1 as CompositeExpr).Args.Length;
                    Expr[] args = new Expr[len];
                    for(int i = 0; i < len; i++) {
                        //args[i] = ( ?  : (e1 as CompositeExpr).Args[i]; //simplified here
                        if((e1 as CompositeExpr).Args[i] is LetterSym) args[i] = new LetterSym(c);
                        else if((e1 as CompositeExpr).Args[i] is IntegerNumber) args[i] = (e1 as CompositeExpr).Args[i];
                        else args[i] = patternEntry(-1, c, new IntegerNumber("1"), (e1 as CompositeExpr).Args[i]);
                    }
                    return new CompositeExpr(head, args);
                }
            }
            if(e0 is LetterSym && e1 is LetterSym) {
                if((e0 as LetterSym).Subscript != null && (e0 as LetterSym).Subscript is IntegerNumber) {
                    if(((e0 as LetterSym).Subscript as IntegerNumber).Equals(k))
                        return new LetterSym((e0 as LetterSym).Letter, new NoAccent(), new LetterSym(c), Format.Normal, null);
                    else if((e1 as LetterSym).Subscript != null && (e1 as LetterSym).Subscript is LetterSym) {//linear for subscripts
                        Expr head = WellKnownSym.plus;
                        Expr[] args = new Expr[2];
                        args[0] = new LetterSym(c);
                        if(((e0 as LetterSym).Subscript as IntegerNumber).Num > k) {
                            args[1] = new IntegerNumber(((e0 as LetterSym).Subscript as IntegerNumber).Num - k);
                        } else {
                            Expr[] arg = new Expr[2];
                            arg[0] = new IntegerNumber(k - ((e0 as LetterSym).Subscript as IntegerNumber).Num);
                            args[1] = new CompositeExpr(WellKnownSym.minus, arg);
                        }
                        return new LetterSym((e0 as LetterSym).Letter, new NoAccent(), new CompositeExpr(head, args), Format.Normal, null);
                    }
                } else 
                    return new LetterSym((e0 as LetterSym).Letter, new NoAccent(), 
                                         patternEntry(k, c, (e0 as LetterSym).Subscript, (e1 as LetterSym).Subscript), 
                                         Format.Normal, null);
            }

            if(e0.Head() != null && e0.Head() == e1.Head() && e0.Args().Length == e1.Args().Length) {
                int    len  = e0.Args().Length;
                Expr[] args = new Expr[len];
                for(int i = 0; i < len; i++) {
                    args[i] = patternEntry(k, c, e0.Args()[i], e1.Args()[i]);
                }
                return new CompositeExpr(e0.Head(), args);
            }
            if(e0 is LetterSym && e1 is CompositeExpr && (e1 as CompositeExpr).Args[0].Equals(e0)) {
                Expr head = (e1 as CompositeExpr).Head;
                Expr[] args = new Expr[2];
                args[0] = e0;
                args[1] = patternEntry(-1, c, new IntegerNumber("1"), (e1 as CompositeExpr).Args[1]);
                return new CompositeExpr(head, args);
            }
            return null;
        }
        private void sim_StrokesDeleting(Strokes stks) {
            _changeSeq++;
            if(BatchEditing) {
                if(_updatedStrokes != null) _updatedStrokes.Add(stks);
                return;
            }
            DoDeleteGesture(stks);
            Parse(stks, true, true, null);
        }
        private void DoDeleteGesture(Strokes stks) {
            foreach(Stroke s in stks)
                if(curSum != null && curSum.strokes.Contains(s)) {
                    curSum = null;
                    break;
                }
            foreach(Stroke s in stks)
                if(curIntegral != null && curIntegral.strokes.Contains(s)) {
                    curIntegral = null;
                    break;
                }

            // CJ: for temporal parsing, allows for superscripts/subscripts to be recognized after their summations are deleted.            
            foreach(Stroke s in stks) {
                if(MyParser.sumList != null)
                    foreach(Recognition r in MyParser.sumList) {
                        if(s.Id == r.strokes[0].Id) {
                            if(r.SuperId == s.Id)
                                r.SuperId = -1;
                            if(r.SubId == s.Id)
                                r.SubId = -1;
                            MyParser.sumList.Remove(r);
                            break;
                        }
                    }
                if(MyParser.integralList != null)
                    foreach(Recognition r in MyParser.integralList) {
                        if(s.Id == r.strokes[0].Id) {
                            if(r.SuperIntegralId == s.Id)
                                r.SuperIntegralId = -1;
                            if(r.SubIntegralId == s.Id)
                                r.SubIntegralId = -1;
                            MyParser.integralList.Remove(r);
                            break;
                        }
                    }
                if(MyParser.opSyms != null)
                    foreach(ParenSym ps in MyParser.opSyms) {
                        if(s.Id == ps.r.strokes[0].Id) {
                            MyParser.opSyms.Remove(ps);
                            foreach(Recognition r in Charreco.Recogs.Values) {
                                if(r.MatrixId == s.Id)
                                    r.MatrixId = -1;
                            }
                            break;
                        }
                    }
                Recognition rec = Charreco.Classification(s);
                /*if (rec != null && (rec.alt == '(' || rec.alt == 'c' || rec.alt == '1'))
                    Parser.matrixRanges.Clear();
                else */
                // Classification BUG? The above condition does not work: 
                // rec.alt can be none of '(', 'c', '1', etc while it was meant to be for '(', etc
                MyParser.matrixRanges.Clear();
                if(rec != null && rec.alt == ')') {
                    MyParser.rpStrokes.Remove(s);
                    if(MyParser.opSyms != null)
                        foreach(ParenSym ps in MyParser.opSyms) {
                            if(ps.Closing != null && s.Id == ps.Closing.r.strokes[0].Id) {
                                ps.Closing = null;
                                ps.matrixOp = null;
                                break;
                            }
                        }
                }
                else if(rec != null && MyParser.funcDefLine!=-1 && "↗↘→↓↑".Contains(rec.alt.Character.ToString()))
                    MyParser.traceFrom.Y = -1;
                else if(rec != null && MyParser.funcDefLine != -1 && rec.alt == '←') {
                    MyParser.funcDefLine = -1;
                }
                else if(rec != null && rec.alt == '↘' && MyParser.opSyms != null) {
                    foreach(ParenSym ps in MyParser.opSyms) {
                        if(s.Id == ps.ArrowID) {
                            ps.ArrowID = -1;
                            break;
                        }
                    }
                }

            }
            string allograph = "";
            if(DoDeleteGesture(stks, ref allograph))
                /*textBox1.Text = allograph*/
                ;
        }
        private bool DoDeleteGesture(Strokes stks, ref string allograph) {
            bool didallgraph = false;
            System.Collections.SortedList reclassify = Charreco.ClearRecogs(stks);
            stks.Ink.DeleteStrokes(stks);
            foreach(Stroke s in reclassify.GetValueList())
                if(!s.Deleted) {
                    didallgraph = true;
                    Recognition r = Charreco.FullClassify(s);
                    allograph = r == null ? "" : r.allograph;
                }
            return didallgraph;
        }
        private void sim_StrokeTransformed(Stroke s, System.Drawing.Drawing2D.Matrix m) {
            _changeSeq++;
            if(BatchEditing) {
                if(_updatedStrokes != null) _updatedStrokes.Add(s);
                return;
            }
            Strokes ss = Sim.Ink.CreateStrokes(new[] { s.Id });
            Charreco.Reset(ss);
            Parse(ss, false, true, null);
        }

        private Recognition MakeWord(IEnumerable<Stroq> stroqs, System.Windows.Ink.AnalysisAlternate aa) {
            HashSet<Recognition> recogs = new HashSet<Recognition>(stroqs.Select((Stroq s) => Charreco.Classification(Sim[s])).
                Where((Recognition r) => r != null));
            foreach(Recognition r in recogs)
                foreach(Stroke s in r.strokes) {
                    Charreco.UnClassify(s);
                }
            Strokes stks = Sim[stroqs];
            System.Windows.Ink.InkWordNode iwn = (System.Windows.Ink.InkWordNode)aa.AlternateNodes[0];
            Recognition rr = new Recognition(stks, "__MS word__", aa.RecognizedString,
                (int)(iwn.GetBaseline().Average((System.Windows.Point p) => p.Y)*OldStrokeStuff.Scale),
                (int)(iwn.GetMidline().Average((System.Windows.Point p) => p.Y)*OldStrokeStuff.Scale));
            return rr;
        }
        private void AddOrSetWord(Recognition rr, System.Windows.Ink.AnalysisAlternate aa) {
            System.Windows.Ink.InkWordNode iwn = (System.Windows.Ink.InkWordNode)aa.AlternateNodes[0];
            rr.addorsetalt(aa.RecognizedString.Length == 1 ? new Recognition.Result(aa.RecognizedString[0]) : new Recognition.Result(aa.RecognizedString),
                (int)(iwn.GetBaseline().Average((System.Windows.Point p) => p.Y)*OldStrokeStuff.Scale),
                (int)(iwn.GetMidline().Average((System.Windows.Point p) => p.Y)*OldStrokeStuff.Scale));
        }
        private Recognition GetWord(Recognition rr, IEnumerable<Stroq> stroqs, System.Windows.Ink.AnalysisAlternate aa) {
            if(rr == null) return MakeWord(stroqs, aa);
            else AddOrSetWord(rr, aa);
            return rr;
        }

        public void ChooseWord(Recognition rr, IEnumerable<Stroq> stroqs, System.Windows.Ink.AnalysisAlternate aa) {
            rr = GetWord(rr, stroqs, aa);
            rr.levelsetby = 0;
            Charreco.ClearRecogs(rr.strokes);
            Charreco.FullClassify(rr.strokes[0], rr);
            AlternateChanged(stroqs);
        }
        public void AlternateChanged(IEnumerable<Stroq> stroqs) {
            Parse(Sim[stroqs], false, true, null);
        }

        private void MakeTopAlt(Recognition alternateRec, int newtop) {
            Recognition.Result[] alts = alternateRec.alts;
            Recognition.Result tmp = alternateRec.alts[newtop];
            Array.Copy(alts, 0, alts, 1, newtop);
            alts[0] = tmp;
            alternateRec.curalt = 0;
            alternateRec.levelsetby = 0;
        }
        public void ChangeAlternatePersistently(IEnumerable<Stroq> stroqs, Recognition alternateRec, int newalt) {
            MakeTopAlt(alternateRec, newalt);
            Recognition.SetAlternates(alternateRec.allograph + (alternateRec.msftRecoged ? "MSFT" : ""), alternateRec.alts);
            AlternateChanged(stroqs);
        }

        public void ChooseWordPersistently(IEnumerable<Stroq> stroqs, Recognition alternateRec, System.Windows.Ink.AnalysisAlternate aa) {
            Recognition rr = GetWord(alternateRec, stroqs, aa);
            ChangeAlternatePersistently(stroqs, alternateRec, alternateRec.curalt);
        }

        /// <summary>
        /// Return what the stroke would be classified as if it were added. *Note:* the current implementation only returns what
        /// the clasification would be if the stroke were isolated, as if there weren't any other strokes added previously.
        /// </summary>
        public Recognition ClassifyOneTemp(Stroq s) {
            Ink i = new Ink();
            Stroke ss = s.OldStroke(i);
            Strokes saved = Charreco.Ignorable;
            Charreco.Ignorable = i.CreateStrokes();
            Recognition r = Charreco.Classify(ss);
            Charreco.Ignorable = saved;
            return r;
        }
        // bcz: sigh ... we want to extend the Expr object w/ a ToString() method so that it will appear
        // nicely formatted in the Debugger.  However, extension methods for ToString() are not recognized
        // by the debugger, so we register the Text.InputConvert method as the Expr->string converter that
        // CompositeExpr's should use in their ToString() method.
        // This belongs in ExprText, but that's F# and I don't know how to do the equivalent there.
        class ExprTextConverter
        {
            public ExprTextConverter() { Expr.Converter = Text.InputConvert; }
        }
        static ExprTextConverter converter = new ExprTextConverter();
    }
}
