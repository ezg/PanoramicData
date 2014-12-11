using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using starPadSDK.MathExpr;
using starPadSDK.MathRecognizer;
using System.Diagnostics;
using starPadSDK.CharRecognizer;
using starPadSDK.Inq;
using starPadSDK.Utils;
using starPadSDK.Geom;

namespace MathRecoScaffold {
    public class Selection {
        private SelectionContents _contents;

        public SelectionContents Contents {
            get { return _contents; }
            set {
                if(_contents != null) Deselect();
                _contents = value;
                if(_contents != null) Select();
            }
        }

        public delegate void SelDeSel();
        public static event SelDeSel SelectionEvent;
        public static event SelDeSel DeselectionEvent;
        private void Deselect() {
            if(_contents != null) _contents.DeSelect();
            if(DeselectionEvent != null) DeselectionEvent();
        }
        public void Select() {
            if(SelectionEvent != null) SelectionEvent();
            if(_contents != null) _contents.Select();
        }
    }
    public abstract class SelectionContents {
        public Stroq Outline { get; protected set; }
        abstract public bool Empty { get; }
        abstract public void Select();
        abstract public void DeSelect();
        abstract public Rct ScrBounds { get; }
        abstract public void MoveTo(Pt where);
        abstract public void StartMove(Pt hit);
        abstract public void EndMove();
        abstract public void Reparse(MathRecognition mrec);
        public SelectionContents() {
        }
    }
    class StroqSel : SelectionContents {
        Pt moveStrokeStart = new Pt();
        Pt hitPt = new Pt();
        public StroqCollection AllStroqs { get; protected set; }
        public StroqCollection ReRecStroqs { get; private set; }
        private StroqCollection _displayedStrokes;
        /// <summary>
        /// Create a selection of inq strokes
        /// </summary>
        /// <param name="contents">The set of stroqs to be selected.</param>
        /// <param name="outline">The outline to be used for the selection; can be null.</param>
        /// <param name="stroq2char">Delegate taking a Stroq as input and returns the (character) Recognition record the stroke is a part of.</param>
        /// <param name="char2stroqs">Given a Recognition, return the Stroqs that compose it.</param>
        /// <param name="dispstrs">A StroqCollection controlling what Stroqs are displayed; the outline is automatically removed from here on deselection.
        /// Can be null.</param>
        public StroqSel(IEnumerable<Stroq> contents, Stroq outline, Func<Stroq,Recognition> stroq2char, Func<Recognition,IEnumerable<Stroq>> char2stroqs,
            StroqCollection dispstrs) { // sc would be Charreco.Classification(s); sqs the mapping of r.strokes
            AllStroqs = new StroqCollection(contents); Outline = outline;
            ReRecStroqs = new StroqCollection();
            HashSet<Stroq> additional = new HashSet<Stroq>();
            foreach(Stroq ss in AllStroqs) {
                Recognition r = stroq2char(ss);
                if(r == null || r.levelsetby != 0)
                    ReRecStroqs.Add(ss);
                else additional.UnionWith(char2stroqs(r));
            }
            AllStroqs.Add(additional);
            _displayedStrokes = dispstrs;
        }
        public override bool Empty { get { return AllStroqs == null || AllStroqs.Count == 0; } }
        public override void Select() {
        }
        public override void DeSelect() {
            AllStroqs = null;
            ReRecStroqs = null;
            if(Outline != null && _displayedStrokes != null) _displayedStrokes.Remove(Outline);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inkPt">where the cursor is at the start of the move</param>
        public override void StartMove(Pt inkPt) {
            moveStrokeStart = inkPt;
            hitPt = AllStroqs.First()[0];
        }
        public override void EndMove() {
            //XXX Parser.Ranges.Clear();
            //XXX FeaturePointDetector.Reset(rerecstrokes);
            // parse(null, false, true);
        }
        public override void MoveTo(Pt lastInkPt) {
            Vec delta = hitPt - AllStroqs.First()[0] + lastInkPt - moveStrokeStart;
            Rct r = AllStroqs.GetBounds();
            AllStroqs.Move(delta);
        }
        public override Rct ScrBounds {
            get {
                return AllStroqs.GetBounds();
            }
        }
        public override void Reparse(MathRecognition mrec) {
            mrec.ReRecogParse(mrec.Sim[AllStroqs], true);
        }
    }
}
