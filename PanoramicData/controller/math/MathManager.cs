using PanoramicData.utils.inq;
using PanoramicData.view.math;
using starPadSDK.AppLib;
using starPadSDK.CharRecognizer;
using starPadSDK.Geom;
using starPadSDK.Inq;
using starPadSDK.MathExpr;
using starPadSDK.MathRecognizer;
using starPadSDK.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using Label = PanoramicData.utils.inq.Label;
using starPadSDK.Inq.MSInkCompat;
using System.Windows;


namespace PanoramicData.controller.math
{
    public class MathManager
    {
        private List<Label> _consumedLabels = new List<Label>();
        private MathRecognition _mathRecognition;
        private StroqCollection _mathRecognizerStroqs = new StroqCollection();
        private StroqCollection _stroqs = new StroqCollection();
        private InkPresenter _inkPresenter = null;
        private RecognitionResultRenderer _recognitionResultRenderer = null;
        private InqScene _inqScene = null;
        private LabelConsumer _labelConsumer = null;
        private InqAnalyzer _inqAnalyzer = null;

        private Expr _renderableExpr = new WordSym("");
        private bool _isRenderableExprNumeric = false;

        private BatchLock _movingLock = null;
        private bool _isLockedForMoving = false;
        private List<KeyValuePair<StroqCollection, char>> _pendingRecognitionHints = new List<KeyValuePair<StroqCollection, char>>();

        public delegate void RecognitionChangedHandler(object sender, EventArgs e);
        public event RecognitionChangedHandler RecognitionChanged;

        public DisplayMode DisplayMode { get; set; }

        public double Width { get; set; }
        public double Height { get; set; }

        private bool _forceMath = false;
        public bool ForceMath
        {
            get
            {
                return _forceMath;
            }
            set
            {
                _forceMath = value;
            }
        }

        private bool _isRecognitionEnabled = true;
        public bool IsRecognitionEnabled
        {
            get
            {
                return _isRecognitionEnabled;
            }
            set
            {
                _isRecognitionEnabled = value;
            }
        }

        public bool IsLockedForMoving
        {
            get
            {
                return _isLockedForMoving;
            }
            set
            {
                if (value && _movingLock == null)
                {
                    _movingLock = _mathRecognition.BatchEdit();
                }
                _isLockedForMoving = value;
            }
        }

        public MathManager(InkPresenter inkPresenter, RecognitionResultRenderer recognitionResultRenderer, InqScene inqScene, LabelConsumer labelConsumer)
        {
            _inkPresenter = inkPresenter;
            _recognitionResultRenderer = recognitionResultRenderer;
            _inqScene = inqScene;
            _labelConsumer = labelConsumer;
            _mathRecognition = new MathRecognition(_mathRecognizerStroqs);
            _inqAnalyzer = new InqAnalyzer();
            _inqAnalyzer.ResultsUpdated += _inqAnalyzer_ResultsUpdated;
            DisplayMode = DisplayMode.Edit;
        }

        public void ConsumedLabelValueChanged()
        {
            if (DisplayMode == DisplayMode.Result)
            {
                displayResult(_mathRecognizerStroqs.GetBounds());
            }
        }

        public void CreateRecognitionHint(StroqCollection stroqs, char value)
        {
            _pendingRecognitionHints.Add(new KeyValuePair<StroqCollection, char>(stroqs, value));
        }

        private void checkPendingRecognitionHints()
        {
            bool found = false;
            foreach (var hint in _pendingRecognitionHints.ToArray())
            {
                StroqCollection stroqs = hint.Key;
                char value = hint.Value;
                foreach (var s in stroqs)
                {
                    if (!_mathRecognizerStroqs.Contains(s))
                    {
                        return;
                    }
                }
                found = true;

                Rct bounds = stroqs.GetBounds();
                var bbounds = _mathRecognition.Sim[stroqs].GetBoundingBox();

                char taggedValue = value; // put some char here to test if the tagging actually works
                Recognition r = new Recognition(_mathRecognition.Sim[stroqs], new Recognition.Result(taggedValue), (int)bbounds.Bottom, (int)(bbounds.Top + bbounds.Height / 2.0));
                r.levelsetby = 0;
                _mathRecognition.Charreco.FullClassify(_mathRecognizerStroqs.First().OldStroke(), r);
                _pendingRecognitionHints.Remove(hint);

                //_inqAnalyzer.CreateKnownCharHint(stroqs, value);
            }
            if (found)
            {
                _mathRecognition.ForceParse(false);
            }
        }

        public void UpdateMathRecognition(StroqCollection stroqs)
        {
            // calculate the set difference and only trigger recognition if something changed.    
            bool sameAs = stroqs.HasSameElementsAs(_stroqs);
            _stroqs = new StroqCollection(stroqs.ToArray());

            if (IsRecognitionEnabled && !sameAs)
            {
                bool changed = false;
                StroqCollection used = new StroqCollection();
                foreach (var s in stroqs)
                {
                    if (!_mathRecognizerStroqs.Contains(s))
                    {
                        if (!_isLockedForMoving && _movingLock != null)
                        {
                            _movingLock.Dispose();
                            _movingLock = null;
                        }
                        _mathRecognizerStroqs.Add(s);
                        _inqAnalyzer.AddStroke(s);
                        changed = true;
                    }
                    used.Add(s);
                }
                List<Stroq> toRemoveStroq = new List<Stroq>();
                foreach (var s in _mathRecognizerStroqs)
                {
                    if (!used.Contains(s))
                    {
                        toRemoveStroq.Add(s);
                    }
                }
                foreach (var s in toRemoveStroq)
                {
                    if (!_isLockedForMoving && _movingLock != null)
                    {
                        _movingLock.Dispose();
                        _movingLock = null;
                    }
                    _mathRecognizerStroqs.Remove(s);
                    _inqAnalyzer.RemoveStroke(s);
                    changed = true;
                }
                if (IsRecognitionEnabled && changed)
                {
                    checkPendingRecognitionHints(); // check if there are any hints that we should add
                    _inqAnalyzer.BackgroundAnalyze();
                }
            }

            List<Label> labelsInStroqs = new List<Label>();
            foreach (var s in stroqs)
            {
                Label l = InkTableContent.GetInkTableContent(s).Label;
                if (l != null && !labelsInStroqs.Contains(l))
                {
                    labelsInStroqs.Add(l);
                }
            }
            List<Label> usedLabels = new List<Label>();
            foreach (var l in labelsInStroqs)
            {
                // don't create recognition if recognition is disabled
                if (IsRecognitionEnabled && !sameAs)
                {
                    if (!_isLockedForMoving && _movingLock != null)
                    {
                        _movingLock.Dispose();
                        _movingLock = null;
                    }
                    l.CreateRecognition(_mathRecognition);
                }
                if (!_consumedLabels.Contains(l))
                {

                    _consumedLabels.Add(l);
                }
                usedLabels.Add(l);
            }
            List<Label> toRemoveLabel = new List<Label>();
            foreach (var l in _consumedLabels)
            {
                if (!usedLabels.Contains(l))
                {
                    toRemoveLabel.Add(l);
                }
            }
            foreach (var l in toRemoveLabel)
            {
                _consumedLabels.Remove(l);
            }

            if (IsFormula() && DisplayMode == DisplayMode.Result)
            {
                displayResult(_mathRecognizerStroqs.GetBounds());
            }

        }

        public bool IsFormula()
        {
            if (_consumedLabels.Count > 0 || _renderableExpr is CompositeExpr)
            {
                return true;
            }
            return false;
        }

        public void ToggleDisplayMode()
        {
            if (DisplayMode == DisplayMode.Edit)
            {
                displayResult(_mathRecognizerStroqs.GetBounds());
                _inkPresenter.Visibility = Visibility.Visible;
                DisplayMode = DisplayMode.Result;
                foreach (var s in _stroqs)
                {
                    s.Visible = false;
                }
                _recognitionResultRenderer.Visibility = Visibility.Collapsed;
            }
            else if (DisplayMode == DisplayMode.Result)
            {
                _inkPresenter.Visibility = Visibility.Collapsed;
                DisplayMode = DisplayMode.Edit;
                foreach (var s in _stroqs)
                {
                    s.Visible = true;
                }
                _recognitionResultRenderer.Visibility = Visibility.Visible;
            }
        }

        public List<Label> GetConsumedLabels()
        {
            return _consumedLabels;
        }

        public void Dispose()
        {
            this._mathRecognition.Dispose();
            _stroqs.Clear();
            _mathRecognizerStroqs.Clear();
        }

        public void SetRenderableExpr(Expr expr, bool numeric)
        {
            _isRenderableExprNumeric = numeric;
            _renderableExpr = expr;
            _recognitionResultRenderer.Display(_renderableExpr, _isRenderableExprNumeric, Width, Height);
        }

        public Expr GetRenderableExpr(out bool numeric)
        {
            numeric = _isRenderableExprNumeric;
            return _renderableExpr;
        }

        private void UpdateRenderableExpr()
        {
            _renderableExpr = null;
            string recString = _inqAnalyzer.GetRecognizedString();
            Expr recExpr = null;
            if (_mathRecognition.Ranges != null && _mathRecognition.Ranges.Count() > 0 && _mathRecognition.Ranges[0].Parse != null)
            {
                recExpr = _mathRecognition.Ranges[0].Parse.expr;
            }
            if (recExpr == null)
            {
                _isRenderableExprNumeric = false;
                if (ForceMath)
                {
                    _renderableExpr = new WordSym("error");
                }
                else
                {
                    _renderableExpr = new WordSym(recString);
                }
            }
            else
            {
                List<LetterSym> letterSym = new List<LetterSym>();
                findLetterSyms(recExpr, letterSym);
                if (letterSym.Count > 0)
                {
                    double parseResult = 0.0;
                    if (double.TryParse(recString, out parseResult))
                    {
                        _renderableExpr = new DoubleNumber(parseResult);
                        _isRenderableExprNumeric = true;
                    }
                    else
                    {
                        if (ForceMath)
                        {
                            _renderableExpr = new WordSym("error");
                        }
                        else
                        {
                            _renderableExpr = new WordSym(recString);
                        }
                        _isRenderableExprNumeric = false;
                    }
                }
                else
                {
                    _renderableExpr = recExpr;
                    _isRenderableExprNumeric = true;
                }
            }
        }

        void _inqAnalyzer_ResultsUpdated(object sender, System.Windows.Ink.ResultsUpdatedEventArgs e)
        {
            if (!_inqAnalyzer.DirtyRegion.IsEmpty)
            {
                _inqAnalyzer.BackgroundAnalyze();
                return;
            }

            UpdateRenderableExpr();
            _recognitionResultRenderer.Display(_renderableExpr, _isRenderableExprNumeric, Width, Height);

            if (RecognitionChanged != null)
            {
                RecognitionChanged(this, new EventArgs());
            }
        }

        private void findLetterSyms(Expr e, List<LetterSym> found)
        {
            if (e is LetterSym)
            {
                found.Add(e as LetterSym);
            }
            else
            {
                if (e.Args() != null)
                {
                    foreach (Expr ee in e.Args())
                    {
                        findLetterSyms(ee, found);
                    }
                }
            }
        }

        private void displayResult(Rect bounds)
        {
            if (_renderableExpr != null)
            {
                double number;
                string text;
                Expr calculated = FormulaEvaluator.Calculate(_renderableExpr);
                FormulaEvaluator.GetValue(calculated, out text, out number);

                string resultString = "= " + text;
                List<KeyValuePair<char, StroqCollection>> stroqsPerCharacter = null;
                
                /*StroqCollection sc = SimpleInqFont.StroqsFromString(resultString, 0.8 * Width, 0.8 * Height, out stroqsPerCharacter);
                Point pt = _inkPresenter.TranslatePoint(new Point(), _inqScene);
                sc.Move(new Vec(0.1 * Width, 0.1 * Height));
                _inkPresenter.Strokes.Clear();
                foreach (Stroq s in sc)
                {
                    s.BackingStroke.DrawingAttributes.Width = 4;
                    s.BackingStroke.DrawingAttributes.Height = 4;
                    _inkPresenter.Strokes.Add(s.BackingStroke);
                }*/
            }
        }

    }
}
