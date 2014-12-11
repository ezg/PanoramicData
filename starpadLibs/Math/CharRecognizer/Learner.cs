using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Ink;
using System.IO;
/*
 * TODO
 * Better statistics: rather than simple distance to means, make use of deviations there?
 * More features
 * More recognizers {'2z', '1()', ?'2Y'}
 * Three way learner
 * 
 * 1. DONE Multilearner must persist enablement
 * 2. autotester must permute
 * 3. autotester must test serveral learners
 * */

namespace CharRecognizer {
    public class UnrecognizedAllographException : Exception { }
    public class BadAllographFileException : Exception { }

    public class Learners {
        // This is just a simple holder for accuracy results in the regression tester.
        public class RegressionAccuracy {
            private int _ok;
            private int _total;
            public RegressionAccuracy() { _ok = 0; _total = 0; }
            public void MarkSuccess() { _ok++; _total++; }
            public void MarkFailure() { _total++; }
            public int Successes { get { return _ok; } }
            public int Failures { get { return _total - _ok; } }
            public int Total { get { return _total; } }
            public double Accuracy { get { return ((double)_ok) / _total; } }
        }

        private System.Collections.Generic.Dictionary<int, Recognition.Result> _whereStrokesAre;
        private System.Collections.Generic.List<MultiLearner> _learners;
        private int _occurencesToConsider;
        private bool _enabled = true;
        private bool _regressionMode = false;
        private System.Collections.Generic.Dictionary<string, RegressionAccuracy> _regressionResults = null;
        private string _activeLearnersFile = null;
        public Learners() {
            _whereStrokesAre = new Dictionary<int, Recognition.Result>();
            _learners = new List<MultiLearner>();
            _activeLearnersFile = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                "SegmentationTester"), "active_learners");

            // Add new learners here
            _learners.Add(new Learner_5S());
            //_learners.Add(new Learner_Generic(new string[] { "b6" }, new Recognition.Result('b'), new Recognition.Result('6')));
            //_learners.Add(new Learner_2z());

            OccurenceToConsider = 3;
            LoadActiveLearners();
        }
        ~Learners() {
            SaveActiveLearners();
        }
        public void SaveActiveLearners() {
            if (_enabled && _activeLearnersFile != null) {
                if (!Directory.Exists(Path.GetDirectoryName(_activeLearnersFile)))
                    Directory.CreateDirectory(Path.GetDirectoryName(_activeLearnersFile));
                using (FileStream myStream = new FileStream(_activeLearnersFile, FileMode.Create, FileAccess.Write)) {
                    StreamWriter sw = new StreamWriter(myStream);
                    foreach (MultiLearner l in _learners)
                        if (l.Activated)
                            foreach (string a in l.Allographs)
                                sw.WriteLine(a);
                    sw.Flush();
                }
            }
        }
        public void LoadActiveLearners() {
            if (_enabled && _activeLearnersFile != null && File.Exists(_activeLearnersFile)) {
                using (FileStream myStream = new FileStream(_activeLearnersFile, FileMode.Open, FileAccess.Read)) {
                    StreamReader sr = new StreamReader(myStream);
                    string line;
                    while (!sr.EndOfStream) {
                        line = sr.ReadLine();
                        foreach (MultiLearner l in _learners)
                            if (l.IsRecognizerForAllograph(line.Trim()))
                                l.Activated = true;
                    }
                }
            }
        }
        public bool Recognize(string allograph, Strokes ss, out Recognition.Result res) {
            if (_enabled) {
                foreach (MultiLearner l in _learners)
                    if (l.IsRecognizerForAllograph(allograph) && l.HasEnoughSamples) {
                        res = l.Recognize(allograph, ss);
                        // If we are in regression mode, we mearly check to see if we would have been right, but don't change the results.
                        if (!_regressionMode) {
                            //Console.WriteLine("Learners: Recognize {0} as {1}.", allograph, res);
                            return true;
                        } else {
                            if (!_regressionResults.ContainsKey(allograph))
                                _regressionResults.Add(allograph, new RegressionAccuracy());
                            if (res.Equals(l.Default))
                                _regressionResults[allograph].MarkSuccess();
                            else
                                _regressionResults[allograph].MarkFailure();
                            return false;
                        }
                    }
            }
            res = new Recognition.Result();
            return false;
        }
        // This untrains the strokes if and only if they were trained recently
        public void UserDeletedStrokes(Strokes ss) {
            if (_enabled) {
                foreach (Stroke s in ss) {
                    Recognition.Result r;
                    if (_whereStrokesAre.TryGetValue(s.Id, out r)) {
                        //Console.WriteLine("Untraining stroke {0} from {1}", s.Id, r);
                        foreach (MultiLearner l in _learners)
                            if (l.IsRecognizerForResult(r))
                                l.RemoveTrainingStrokeIfRecent(r, s);
                        //if (r.Equals(l.One) || r.Equals(l.Two)) l.RemoveTrainingStrokeIfRecent(r, s);
                    }
                }
            }
        }
        public void DeleteStrokes(Strokes ss) {
            if (_enabled) {
                foreach (Stroke s in ss) {
                    Recognition.Result r;
                    if (_whereStrokesAre.TryGetValue(s.Id, out r)) {
                        //Console.WriteLine("Untraining stroke {0} from {1}", s.Id, r);
                        foreach (MultiLearner l in _learners)
                            if (l.IsRecognizerForResult(r))
                                l.RemoveTrainingStroke(r, s);
                        //if (r.Equals(l.One) || r.Equals(l.Two)) l.RemoveTrainingStroke(r, s);
                    }
                }
            }
        }
        public void TrainStrokes(Strokes ss, string allograph, Recognition.Result expect) {
            if (_enabled) {
                DeleteStrokes(ss);
                if (ss.Count == 1) {
                    //Console.WriteLine("Training stroke {0} to be {2}->{1}", ss[0].Id, expect, allograph);
                    _whereStrokesAre[ss[0].Id] = expect;
                    foreach (MultiLearner l in _learners)
                        if (l.IsRecognizerForResult(expect))
                            l.AddTrainingStroke(expect, allograph, ss[0]);
                }
            }
        }
        public int OccurenceToConsider {
            set {
                _occurencesToConsider = value;
                foreach (MultiLearner l in _learners)
                    l.OccurencesToConsider = _occurencesToConsider;
            }
            get { return _occurencesToConsider; }
        }
        public void SetDefaults(string allograph, Recognition.Result def) {
            foreach (MultiLearner l in _learners)
                if (l.IsRecognizerForAllograph(allograph))
                    l.Default = def;
        }
        public void Reset() {
            //Console.WriteLine("Learner: Reset.");
            foreach (MultiLearner l in _learners)
                l.Reset();
        }
        public void FixStrokes() {
            foreach (MultiLearner l in _learners)
                l.FixStrokes();
        }
        public bool Enabled { get { return _enabled; } set { _enabled = value; } }
        public void RegressionStart() {
            _regressionMode = true;
            _regressionResults = new Dictionary<string, RegressionAccuracy>();
            foreach (MultiLearner l in _learners)
                l.SaveToFiles();
            this.Reset();
        }
        public System.Collections.Generic.Dictionary<string, RegressionAccuracy> RegressionEnd() {
            _regressionMode = false;
            System.Collections.Generic.Dictionary<string, RegressionAccuracy> temp = _regressionResults;
            _regressionResults = null;
            this.Reset();
            foreach (MultiLearner l in _learners)
                l.LoadFormFiles();
            return temp;
        }
        public bool RegressionMode {
            get { return _regressionMode; }
            set {
                if (value)
                    RegressionStart();
                else
                    RegressionEnd();
            }
        }
    }

    // This class allows us to abstract learners who differentiate more than two results
    public interface MultiLearner {
        //public MultiLearner() { }
        Recognition.Result Recognize(string allograph, Strokes ss);
        Recognition.Result Recognize(string allograph, Stroke s);
        bool IsRecognizerForAllograph(string allograph);
        bool IsRecognizerForResult(Recognition.Result r);
        void RemoveTrainingStrokeIfRecent(Recognition.Result r, Stroke s);
        void RemoveTrainingStroke(Recognition.Result r, Stroke s);
        void RemoveTrainingStrokeIfRecent(Recognition.Result r, Strokes ss);
        void RemoveTrainingStroke(Recognition.Result r, Strokes ss);
        void AddTrainingStroke(Recognition.Result r, string allograph, Stroke s);
        int OccurencesToConsider { get; set; }
        Recognition.Result Default { get; set; }
        void Reset();
        void FixStrokes();
        bool HasEnoughSamples { get; }
        List<string> Allographs { get; }
        bool Activated { get; set; }
        void LoadFormFiles();
        void SaveToFiles();
    }

    public class Learner_Generic : MultiLearner {
        protected Learner _learner;
        protected List<string> _allographs;
        protected Dictionary<string, bool> _seenOne;
        protected Dictionary<string, bool> _seenTwo;

        public Learner_Generic(string[] allographs, Recognition.Result one, Recognition.Result two)
            : this(allographs, one, two, new FeatureAnalyzer()) { }
        internal Learner_Generic(string[] allographs, Recognition.Result one, Recognition.Result two, FeatureAnalyzer f) {
            _learner = new Learner(one, two, f);
            _allographs = new List<string>();
            _seenOne = new Dictionary<string, bool>();
            _seenTwo = new Dictionary<string, bool>();
            foreach (string a in allographs) {
                _allographs.Add(a);
                _seenOne.Add(a, false);
                _seenTwo.Add(a, false);
            }
        }
        public Recognition.Result Recognize(string allograph, Stroke s) {
            if (_seenOne[allograph] && _seenTwo[allograph])
                return _learner.Recognize(s);
            else
                return _learner.Default;
        }
        public Recognition.Result Recognize(string allograph, Strokes ss) {
            if (ss.Count != 1) return _learner.Default;
            return Recognize(allograph, ss[0]);
        }
        public bool IsRecognizerForAllograph(string allograph) {
            return _allographs.Contains(allograph);
        }
        public bool IsRecognizerForResult(Recognition.Result r) { return r.Equals(_learner.One) || r.Equals(_learner.Two); }
        public void RemoveTrainingStrokeIfRecent(Recognition.Result r, Stroke s) { _learner.RemoveTrainingStrokeIfRecent(r, s); }
        public void RemoveTrainingStroke(Recognition.Result r, Stroke s) { _learner.RemoveTrainingStroke(r, s); }
        public void RemoveTrainingStrokeIfRecent(Recognition.Result r, Strokes ss) {
            foreach (Stroke s in ss)
                _learner.RemoveTrainingStrokeIfRecent(r, s);
        }
        public void RemoveTrainingStroke(Recognition.Result r, Strokes ss) {
            foreach (Stroke s in ss)
                _learner.RemoveTrainingStroke(r, s);
        }
        public void AddTrainingStroke(Recognition.Result r, string allograph, Stroke s) {
            if (IsRecognizerForAllograph(allograph)) {
                if (r.Equals(_learner.One))
                    _seenOne[allograph] = true;
                if (r.Equals(_learner.Two))
                    _seenTwo[allograph] = true;
                if (Activated)
                    Console.WriteLine("Learner activated!");
            }
            _learner.AddTrainingStroke(r, s);
        }
        public List<string> Allographs { get { return _allographs; } }
        public bool Activated {
            get {
                foreach (string a in _allographs) {
                    if (!_seenOne[a] || !_seenTwo[a])
                        return false;
                }
                return true;
            }
            set {
                foreach (string a in _allographs) {
                    _seenOne[a] = value;
                    _seenTwo[a] = value;
                }
            }
        }
        public int OccurencesToConsider { get { return _learner.OccurencesToConsider; } set { _learner.OccurencesToConsider = value; } }
        public Recognition.Result Default { get { return _learner.Default; } set { _learner.Default = value; } }
        public void Reset() {
            _learner.Reset();
            Activated = false;
        }
        public void FixStrokes() { _learner.FixStrokes(); }
        public bool HasEnoughSamples { get { return _learner.TrainingStokeCount(_learner.One) > 0 && _learner.TrainingStokeCount(_learner.Two) > 0; } }
        public void LoadFormFiles() { _learner.LoadTrainingStrokes(); }
        public void SaveToFiles() { _learner.SaveTrainingStrokesToFile(); }
    }

    public class Learner_2z : Learner_Generic {
        public Learner_2z()
            : base(new string[] { "2z", "z2", "z" }, new Recognition.Result('z'), new Recognition.Result('2'), new FeatureAnalyzer_2z()) {
            _learner.LoadTrainingStrokesFromFile(new Recognition.Result('2'), Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                "SegmentationTester"), "allograph_2.feat"));
            _learner.LoadTrainingStrokesFromFile(new Recognition.Result('z'), Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                "SegmentationTester"), "allograph_Z.feat"));
        }
    }

    public class Learner_5S : Learner_Generic {
        public Learner_5S()
            : base(new string[] { "5s" }, new Recognition.Result('5'), new Recognition.Result('s'), new FeatureAnalyzer_5S()) {
            _learner.LoadTrainingStrokesFromFile(new Recognition.Result('5'), Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                "SegmentationTester"), "allograph_5.feat"));
            _learner.LoadTrainingStrokesFromFile(new Recognition.Result('s'), Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                "SegmentationTester"), "allograph_S.feat"));
        }
    }

    public class TestLearner : Learner {
        public TestLearner(FeatureAnalyzer f) : base(new Recognition.Result('5'), new Recognition.Result('s'), f) { }
    }

    public class Learner {
        internal enum ResultLabel { ONE = 1, TWO = -1 };

        //private bool _enabled = false;
        private Recognition.Result _resultOne;
        private Recognition.Result _resultTwo;
        private TrainingSet _trainingExamplesOne;
        private TrainingSet _trainingExamplesTwo;
        private int _numToConsider;
        private Recognition.Result _default;
        private int _cycles;
        private FeatureAnalyzer _analizer;
        protected string _fileOne = null;
        protected string _fileTwo = null;
        private FeatureSet _mins;
        private FeatureSet _maxs;

        private List<WeakLearner> _weakLearners;

        public Learner(Recognition.Result one, Recognition.Result two)
            : this(one, two, new FeatureAnalyzer()) { }
        internal Learner(Recognition.Result one, Recognition.Result two, FeatureAnalyzer f) {
            _default = one;
            _resultOne = one;
            _resultTwo = two;
            _cycles = 10;
            _numToConsider = 3;
            _trainingExamplesOne = new TrainingSet(_numToConsider, f);
            _trainingExamplesTwo = new TrainingSet(_numToConsider, f);
            _analizer = f;
            _weakLearners = null;
        }
        ~Learner() {
            SaveTrainingStrokesToFile();
        }
        public int OccurencesToConsider {
            get { return _numToConsider; }
            set {
                _numToConsider = value;
                _trainingExamplesOne.Maximum = _numToConsider;
                _trainingExamplesTwo.Maximum = _numToConsider;
            }
        }
        public int Cycles {
            get { return _cycles; }
            set { _cycles = value; }
        }
        public Recognition.Result Default {
            get { return _default; }
            set { _default = value; }
        }
        public void Reset() {
            //Console.WriteLine("Reseting Training");
            _trainingExamplesOne.Clear();
            _trainingExamplesTwo.Clear();
            //SaveTrainingStrokesToFile();
            _weakLearners = null;
            //_enabled = false;
        }
        public void FixStrokes() {
            _trainingExamplesOne.Fix();
            _trainingExamplesTwo.Fix();
        }
        public int TrainingStokeCount(Recognition.Result p) {
            if (p == _resultOne)
                return _trainingExamplesOne.Count;
            else if (p == _resultTwo)
                return _trainingExamplesTwo.Count;
            else
                throw new UnrecognizedAllographException();
        }
        public void AddTrainingStroke(Recognition.Result p, Stroke s) {
            if (p == _resultOne) {
                if (!_trainingExamplesOne.Contains(s))
                    _trainingExamplesOne.Add(s);
            } else if (p == _resultTwo) {
                if (!_trainingExamplesTwo.Contains(s))
                    _trainingExamplesTwo.Add(s);
            } else
                throw new UnrecognizedAllographException();
            //SaveTrainingStrokesToFile();
            _weakLearners = null;
            //Console.WriteLine("Add " + p.Character + " " + s.Id + " 5: " + TrainingStokeCount('5') + " s: " + TrainingStokeCount('s'));
        }
        public void RemoveTrainingStroke(Recognition.Result p, Stroke s) {
            if (p == _resultOne)
                _trainingExamplesOne.Remove(s);
            else if (p == _resultTwo)
                _trainingExamplesTwo.Remove(s);
            else
                throw new UnrecognizedAllographException();
            //SaveTrainingStrokesToFile();
            _weakLearners = null;
        }
        public void RemoveTrainingStroke(Recognition.Result p, Strokes ss) {
            foreach (Stroke s in ss) {
                if (p == _resultOne)
                    _trainingExamplesOne.Remove(s);
                else if (p == _resultTwo)
                    _trainingExamplesTwo.Remove(s);
                else
                    throw new UnrecognizedAllographException();
            }
            //SaveTrainingStrokesToFile();
            _weakLearners = null;
        }
        public void RemoveTrainingStrokeIfRecent(Recognition.Result p, Stroke s) {
            if (p == _resultOne)
                _trainingExamplesOne.RemoveIfRecent(s);
            else if (p == _resultTwo)
                _trainingExamplesTwo.RemoveIfRecent(s);
            else
                throw new UnrecognizedAllographException();
            //SaveTrainingStrokesToFile();
            _weakLearners = null;
        }
        public void RemoveTrainingStrokeIfRecent(Recognition.Result p, Strokes ss) {
            foreach (Stroke s in ss) {
                if (p == _resultOne)
                    _trainingExamplesOne.RemoveIfRecent(s);
                else if (p == _resultTwo)
                    _trainingExamplesTwo.RemoveIfRecent(s);
                else
                    throw new UnrecognizedAllographException();
            }
            //SaveTrainingStrokesToFile();
            _weakLearners = null;
        }
        public void LoadTrainingStrokes() {
            if (_fileOne != null)
                LoadTrainingStrokesFromFile(_resultOne, _fileOne);
            if (_fileTwo != null)
                LoadTrainingStrokesFromFile(_resultTwo, _fileTwo);
        }
        public void LoadTrainingStrokesFromFile(Recognition.Result p, string file) {
            if (p == _resultOne)
                _fileOne = file;
            else if (p == _resultTwo)
                _fileTwo = file;
            else
                throw new UnrecognizedAllographException();
            if (!File.Exists(file)) return;
            using (FileStream myStream = new FileStream(file, FileMode.Open, FileAccess.Read)) {
                StreamReader sr = new StreamReader(myStream);
                string line;
                //if (!sr.EndOfStream) {
                //    line = sr.ReadLine();
                //    if (line.StartsWith("enabled"))
                //        _enabled = true;
                //}
                while (!sr.EndOfStream) {
                    line = sr.ReadLine();
                    // We don't load incompatible allographs!
                    if (!line.StartsWith(_analizer.Version))
                        continue;

                    if (p == _resultOne)
                        _trainingExamplesOne.Add(line);
                    else if (p == _resultTwo)
                        _trainingExamplesTwo.Add(line);
                    else
                        throw new UnrecognizedAllographException();
                }
            }
            _weakLearners = null;
        }
        public void SaveTrainingStrokesToFile() {
            if (_fileOne != null) {
                if (!Directory.Exists(Path.GetDirectoryName(_fileOne)))
                    Directory.CreateDirectory(Path.GetDirectoryName(_fileOne));
                using (FileStream myStream = new FileStream(_fileOne, FileMode.Create, FileAccess.Write)) {
                    StreamWriter sw = new StreamWriter(myStream);
                    //if (_enabled)
                    //    sw.WriteLine("enabled");
                    //else
                    //    sw.WriteLine("disabled");
                    for (int i = 0; i < _trainingExamplesOne.Count; i++)
                        sw.WriteLine(_trainingExamplesOne[i].FormatForSave());
                    sw.Flush();
                }
            }
            if (_fileTwo != null) {
                if (!Directory.Exists(Path.GetDirectoryName(_fileOne)))
                    Directory.CreateDirectory(Path.GetDirectoryName(_fileOne));
                using (FileStream myStream = new FileStream(_fileTwo, FileMode.Create, FileAccess.Write)) {
                    StreamWriter sw = new StreamWriter(myStream);
                    //if (_enabled)
                    //    sw.WriteLine("enabled");
                    //else
                    //    sw.WriteLine("disabled");
                    for (int i = 0; i < _trainingExamplesTwo.Count; i++)
                        sw.WriteLine(_trainingExamplesTwo[i].FormatForSave());
                    sw.Flush();
                }
            }
        }
        private void Train() {
            _weakLearners = new List<WeakLearner>(); // On a retrain, toss the old ones...

            // As Sherlock Holmes once said, it is a capital mistake to theorizee in advance of data...
            if (_trainingExamplesOne.Count < 1 || _trainingExamplesTwo.Count < 1)
                return;

            _trainingExamplesOne.MakeMinMax(_trainingExamplesTwo);
            _trainingExamplesTwo.MakeMinMax(_trainingExamplesOne);
            _mins = _trainingExamplesOne.Mins;
            _maxs = _trainingExamplesOne.Maxs;

            //Console.Write("Training Examples, top: ");
            //for (int i = 0; i < _trainingExamplesOne.Count; i++)
            //    Console.Write("{0} ", _trainingExamplesOne[i].Id);
            //Console.Write("; bottom: ");
            //for (int i = 0; i < _trainingExamplesTwo.Count; i++)
            //    Console.Write("{0} ", _trainingExamplesTwo[i].Id);
            //Console.WriteLine();

            int totalLen = _trainingExamplesOne.Count + _trainingExamplesTwo.Count;
            FeatureSet[] featureset = new FeatureSet[totalLen];
            ResultLabel[] expects = new ResultLabel[totalLen];
            for (int i = 0; i < _trainingExamplesOne.Count; i++) {
                featureset[i] = _trainingExamplesOne[i];
                expects[i] = ResultLabel.ONE;
            }
            int startTwo = _trainingExamplesOne.Count;
            for (int i = 0; i < _trainingExamplesTwo.Count; i++) {
                featureset[i + startTwo] = _trainingExamplesTwo[i];
                expects[i + startTwo] = ResultLabel.TWO;
            }

            double[] weights = new double[totalLen];
            for (int i = 0; i < totalLen; i++)
                weights[i] = 1.0 / totalLen;

            for (int t = 0; t < _cycles; t++) {
                // Heavy lifting done in this constructor:
                WeakLearner wl = new WeakLearner(featureset, expects, weights);
                _weakLearners.Add(wl);
                double sumWeight = 0.0;
                // Calc weights for next round
                for (int i = 0; i < weights.Length; i++) {
                    weights[i] = weights[i] * Math.Exp(-wl.Importance * (double)expects[i] * wl.Recognize(featureset[i]));
                    sumWeight += weights[i];
                }
                // Normalize
                for (int i = 0; i < weights.Length; i++)
                    weights[i] /= sumWeight;
            }
        }
        public Recognition.Result Recognize(Stroke s) {
            if (/*!_enabled ||*/ _trainingExamplesOne.Count < 1 || _trainingExamplesTwo.Count < 1)
                return _default;

            if (_weakLearners == null)
                Train();

            FeatureSet fs = (new FeatureSet(s, _analizer)).Normalize(_mins, _maxs);

            double hypothesis = 0.0;
            foreach (WeakLearner wl in _weakLearners) {
                //Console.WriteLine("WeakLeaner says: {0})", wl.Discuss(fs));
                hypothesis += wl.Recognize(fs) * wl.Importance;
            }
            //Console.WriteLine("Learner says {0}: {1}", hypothesis>0?_resultOne:_resultTwo, hypothesis);
            _lastconf = Math.Abs(hypothesis);
            if (hypothesis > 0)
                return _resultOne;
            else
                return _resultTwo;
        }
        private double _lastconf;
        public double LastConfidence { get { return _lastconf; } }
        public Recognition.Result One { get { return _resultOne; } }
        public Recognition.Result Two { get { return _resultTwo; } }
        //public bool Enabled { get { return _enabled; } set { _enabled = value; } }
    }
    internal class TrainingSet {
        //private System.Collections.ArrayList _stks;
        private System.Collections.ArrayList _features;
        private int _max;
        private FeatureAnalyzer _analizer;
        private FeatureSet _mins = new FeatureSet();
        private FeatureSet _maxs = new FeatureSet();
        public FeatureSet Mins { get { return _mins; } }
        public FeatureSet Maxs { get { return _maxs; } }
        public void MakeMinMax(TrainingSet other) {
            int size = 0;
            if (_features.Count > 0) size = ((FeatureSet)_features[0]).Length;
            _mins.Clear(size);
            _maxs.Clear(size);
            foreach (FeatureSet s in _features) {
                for (int i = 0; i < s.Length; ++i) {
                    if (double.IsNaN(_mins[i])) _mins[i] = s[i];
                    if (s[i] < _mins[i]) _mins[i] = s[i];
                    if (double.IsNaN(_maxs[i])) _maxs[i] = s[i];
                    if (s[i] > _maxs[i]) _maxs[i] = s[i];
                }
            }
            foreach (FeatureSet s in other._features) {
                for (int i = 0; i < s.Length; ++i) {
                    if (double.IsNaN(_mins[i])) _mins[i] = s[i];
                    if (s[i] < _mins[i]) _mins[i] = s[i];
                    if (double.IsNaN(_maxs[i])) _maxs[i] = s[i];
                    if (s[i] > _maxs[i]) _maxs[i] = s[i];
                }
            }
        }
        public TrainingSet() : this(3, new FeatureAnalyzer()) { }
        public TrainingSet(int max) : this(max, new FeatureAnalyzer()) { }
        public TrainingSet(FeatureAnalyzer f) : this(3, f) { }
        public TrainingSet(int max, FeatureAnalyzer f) {
            _max = max;
            //_stks = new System.Collections.ArrayList();
            _features = new System.Collections.ArrayList();
            _analizer = f;
        }
        public void Add(Stroke s) {
            //_stks.Insert(0, s);
            _features.Insert(0, new FeatureSet(s, _analizer));
        }
        public void Add(string featurelist) {
            _features.Insert(0, new FeatureSet(featurelist, _analizer));
        }
        public void Fix() {
            for (int i = 0; i < _features.Count; i++)
                ((FeatureSet)_features[i]).Fix();
        }
        public void RemoveIfRecent(Stroke s) {
            for (int i = 0; i < _features.Count; i++)
                if (((FeatureSet)_features[i]).Equals(s) && ((FeatureSet)_features[i]).IsRecent) {
                    _features.RemoveAt(i);
                }
        }
        public void Remove(Stroke s) {
            for (int i = 0; i < _features.Count; i++)
                //if (s.Id == ((Stroke)_stks[i]).Id) {
                if (((FeatureSet)_features[i]).Equals(s)) {
                    //_stks.RemoveAt(i);
                    _features.RemoveAt(i);
                }
        }
        public bool Contains(Stroke s) {
            for (int i = 0; i < _features.Count; i++)
                if (((FeatureSet)_features[i]).Equals(s)) {
                    return true;
                }
            return false;
        }
        public void Clear() {
            _features = new System.Collections.ArrayList();
        }
        public FeatureSet this[int i] {
            get {
                if (i >= 0 && i < Count) {
                    //Console.WriteLine(((FeatureSet)_features[i]).Normalize(_mins, _maxs).FormatForSave());
                    return ((FeatureSet)_features[i]).Normalize(_mins, _maxs);
                } else
                    return null;
            }
        }
        public int Count {
            get {
                if (_features.Count > _max)
                    return _max;
                else
                    return _features.Count;
            }
        }
        public int Maximum {
            get { return _max; }
            set { _max = value; }
        }
    }

    // This weak learner does all of it's learning when it is constructed and is pretty much test-only after that.
    // It represents one instance of h_t in the AdaBoost algo; Figure 1 in:
    //   Schapire,Robert. A Brief Introduction to Boosting. In Procedings of the 16th International Joint Conference on Artificial Intellegence, 1401-1406, 1999.
    internal class WeakLearner {
        private double _importance;
        private bool[] _reverse;
        private double[] _error;
        private double _totalError;
        private double[] _averagesOne;
        private double[] _averagesTwo;
        private bool[] _hypothesis;
        private double[] _deviationsOne;
        private double[] _deviationsTwo;
        private double[] _featureWeights;
        public WeakLearner(FeatureSet[] strokes, Learner.ResultLabel[] expected, double[] weights) {
            if (expected.Length != strokes.Length || weights.Length != strokes.Length)
                throw new System.ArgumentException("Array input to WeakLearner constructor must all be same length!");
            if (expected.Length == 0)
                throw new System.ArgumentException("Arguments must have at least some length...");
            double totalWeight = 0.0;
            for (int i = 0; i < weights.Length; i++)
                totalWeight += weights[i];
            if (totalWeight < 0.9 || totalWeight > 1.1) // FIXME, should use a smaller epsilon...
                throw new System.ArgumentException("Weights not balanced!");

            int len = strokes[0].Length;
            _averagesOne = new double[len];
            _averagesTwo = new double[len];
            _error = new double[len];
            _reverse = new bool[len];
            _hypothesis = new bool[len];
            _deviationsOne = new double[len];
            _deviationsTwo = new double[len];
            _featureWeights = new double[len];
            //Console.WriteLine("Creating WeakLearner:");
            CalcAverages(strokes, expected, weights);
            CalcErrors(strokes, expected, weights);
            ChooseHyppothesis();
            CalcTotalError(strokes, expected, weights);
            CalcImportance();
        }
        private void CalcTotalError(FeatureSet[] strokes, Learner.ResultLabel[] expected, double[] weights) {
            _totalError = 0.0;

            for (int i = 0; i < strokes.Length; i++) {
                //Console.WriteLine("WeakLearnerError {0} = {1}", Recognize(strokes[i]), expected[i]);
                if (this.Recognize(strokes[i]) != (int)expected[i])
                    _totalError += weights[i];
            }
        }
        private void CalcErrors(FeatureSet[] strokes, Learner.ResultLabel[] expected, double[] weights) {
            for (int i = 0; i < strokes.Length; i++) {
                for (int f = 0; f < _error.Length; f++)
                    if (Math.Sign(this.Recognize(strokes[i], f)) != (int)expected[i])
                        _error[f] += weights[i];
            }
            for (int f = 0; f < _error.Length; f++)
                if (_error[f] > 0.5) {
                    _reverse[f] = true;
                    _error[f] = 1 - _error[f];
                }
        }
        private void CalcImportance() {
            if (_totalError < 1e-12)
                _importance = 30.0;
            else if (_totalError > 0.999999999)
                _importance = -30.0;
            else
                _importance = Math.Log((1 - _totalError) / _totalError) / 2;
        }
        private void ChooseHyppothesis() {
            //bool foundOne = false;
            //// I hope that there will be several tests that are perfect; we'll average them
            //for (int i = 0; i < _error.Length; i++) {
            //    if (_error[i] < 1e-5) {
            //        _hypothesis[i] = true;
            //        foundOne = true;
            //    }
            //}
            //// If there are no tests that give minimal error then choose the best test we've found:
            //if (!foundOne) {
            //    int hypothesis = 0;
            //    for (int i = 1; i < _error.Length; i++)
            //        if (_error[i] < _error[hypothesis])
            //            hypothesis = i;
            //    _hypothesis[hypothesis] = true;
            //}

            // Just for a moment, let's let deviations take care of this...
            for (int i = 0; i < _error.Length; i++)
                _hypothesis[i] = true;
        }
        private void CalcAverages(FeatureSet[] strokes, Learner.ResultLabel[] expected, double[] weights) {
            for (int feat = 0; feat < strokes[0].Length; feat++) {
                double weightSumOne = 0.0;
                double weightSumTwo = 0.0;
                double maxOne = 0.0;
                double maxTwo = 0.0;

                for (int i = 0; i < strokes.Length; i++) {
                    if (!Double.IsNaN(strokes[i][feat])) {
                        if (expected[i] == Learner.ResultLabel.ONE) {
                            _averagesOne[feat] += strokes[i][feat] * weights[i];
                            weightSumOne += weights[i];
                            maxOne = Math.Max(maxOne, Math.Abs(strokes[i][feat] * weights[i]));
                        } else {
                            _averagesTwo[feat] += strokes[i][feat] * weights[i];
                            weightSumTwo += weights[i];
                            maxTwo = Math.Max(maxTwo, Math.Abs(strokes[i][feat] * weights[i]));
                        }
                    }
                }
                _averagesOne[feat] /= weightSumOne;
                _averagesTwo[feat] /= weightSumTwo;
                if (maxOne == 0.0) maxOne = 1.0;
                if (maxTwo == 0.0) maxTwo = 1.0;
                double d1 = 0.0; double d2 = 0.0;
                for (int i = 0; i < strokes.Length; i++) {
                    if (!Double.IsNaN(strokes[i][feat])) {
                        if (expected[i] == Learner.ResultLabel.ONE) {
                            d1 += Math.Pow((strokes[i][feat] * weights[i] - _averagesOne[feat]), 2);
                        } else {
                            d2 += Math.Pow((strokes[i][feat] * weights[i] - _averagesTwo[feat]), 2);
                        }
                    }
                }
                _deviationsOne[feat] = Math.Sqrt(d1 / strokes.Length) / maxOne;
                _deviationsTwo[feat] = Math.Sqrt(d2 / strokes.Length) / maxTwo;
                if (_deviationsOne[feat] < 1e-10 || _deviationsTwo[feat] < 1e-10)
                    _featureWeights[feat] = 0;
                else
                    _featureWeights[feat] = (_averagesOne[feat] - _averagesTwo[feat]) / (_deviationsOne[feat] * _deviationsTwo[feat]);
            }
        }
        private double Recognize(FeatureSet s, int feature) {
            if (Double.IsNaN(s[feature])) return 0;
            double dist1 = Math.Abs(s[feature] - _averagesOne[feature]);
            double dist2 = Math.Abs(s[feature] - _averagesTwo[feature]);
            if (Math.Abs(dist1 - dist2) < 1e-7)
                return 0;
            if (dist1 < dist2)
                return _featureWeights[feature];
            else
                return -_featureWeights[feature];
        }
        public int Recognize(FeatureSet s) {
            double hypothesis = 0;
            for (int feat = 0; feat < s.Length; feat++) {
                if (_hypothesis[feat]) {
                    if (_reverse[feat])
                        hypothesis -= Recognize(s, feat);
                    else
                        hypothesis += Recognize(s, feat);
                }
            }
            return Math.Sign(hypothesis);
        }
        // Importance is the alpha_t factor in the AdaBoost algorithm
        public double Importance { get { return _importance; } }
        // Error is (naturally) the epsilon_t factor int the AdaBoost algorithm
        public double Error { get { return _totalError; } }
        public string Discuss(FeatureSet s) {
            StringBuilder sb = new StringBuilder();
            sb.Append("hypos:");
            for (int i = 0; i < _hypothesis.Length; i++)
                if (_hypothesis[i]) {
                    sb.AppendFormat(" {0}={1:F3}", s.Names[i], Recognize(s, i));
                }
            sb.AppendFormat("; {0}*{1} (error {2})", Recognize(s), Importance, Error);
            return sb.ToString();
        }
    }

    internal class FeatureSet {
        private bool _editable = false;
        private string _version;
        private int _id = -1;
        private double[] _features = { };
        private List<string> _names;
        private DateTime _stored;
        public double this[int i] {
            get { return _features[i]; }
            set { if (_editable) _features[i] = value; }
        }
        public void Clear(int size) {
            if (_editable) {
                _features = new double[size];
                for (int i = 0; i < Length; ++i)
                    _features[i] = double.NaN;
            }
        }
        public FeatureSet Normalize(FeatureSet mins, FeatureSet maxs) {
            FeatureSet s = new FeatureSet();
            s.Clear(Length);
            for (int i = 0; i < Length; ++i)
                s[i] = (_features[i] - mins[i]) / (maxs[i] - mins[i]);
            return s;
        }
        public int Length { get { return _features.Length; } }
        public FeatureSet() { _editable = true; }
        public FeatureSet(string featurelist) : this(featurelist, new FeatureAnalyzer()) { }
        public FeatureSet(string featurelist, FeatureAnalyzer f) {
            _version = f.Version;
            if (!featurelist.StartsWith(_version))
                throw new BadAllographFileException();
            string[] features = featurelist.Split(new char[] { ' ' });
            int count = Int32.Parse(features[1]);
            _features = new double[count];
            for (int i = 0; i < count; i++)
                _features[i] = Double.Parse(features[i + 2]);
            Console.WriteLine("Loading traing strokes: {0}", featurelist);
        }
        public FeatureSet(Stroke s) : this(s, new FeatureAnalyzer()) { }
        public FeatureSet(FeaturePointDetector.CuspSet cs) : this(cs, new FeatureAnalyzer()) { }
        public FeatureSet(Stroke s, FeatureAnalyzer f) {
            System.Collections.ArrayList cuspies;
            _id = s.Id;

            CreateFeatureSet(FeaturePointDetector.FeaturePoints(s), f);
        }
        public FeatureSet(FeaturePointDetector.CuspSet cs, FeatureAnalyzer f) { CreateFeatureSet(cs, f); }
        private void CreateFeatureSet(FeaturePointDetector.CuspSet cs, FeatureAnalyzer f) {
            _version = f.Version;
            List<double> features = new List<double>();
            _names = new List<string>();
            // In 45 seconds, we assume that the stroke is correct; before that erasure means not an example.
            _stored = DateTime.Now.AddSeconds(45);

            double[] temp = { };
            for (int i = 0; i < f.Count; i++) {
                temp = f[i](cs);
                string name = f.Names(i);
                for (int j = 0; j < temp.Length; j++) {
                    features.Add(temp[j]);
                    _names.Add(name + "[" + j + "]");
                }
            }

            _features = new double[features.Count];
            //Console.Write("Createing Feature Set: [");
            for (int i = 0; i < _features.Length; i++) {
                _features[i] = features[i];
                //Console.Write("{0:F3} ", _features[i]);
            }
            //Console.WriteLine("]");
        }
        public List<string> Names { get { return _names; } }
        public int Id { get { return _id; } }
        public void Fix() { _id = -1; }
        public bool IsRecent { get { return _id != -1 && _stored > DateTime.Now; } }
        public override bool Equals(Object o) {
            if (o == null)
                return false;
            if (o is FeatureSet && this.Id != -1)
                return ((FeatureSet)o).Id == this.Id;
            if (o is Stroke)
                return ((Stroke)o).Id == this.Id;
            return false;
        }
        public override int GetHashCode() {
            int l = _id;
            for (int i = 0; i < _features.Length; i++)
                l *= 37 * ((int)(_features[i] * 1000));
            return l;
        }
        public string FormatForSave() {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("{0} {1}", _version, _features.Length);
            for (int i = 0; i < _features.Length; i++)
                sb.AppendFormat(" {0}", _features[i]);

            return sb.ToString();
        }
    }

    public class FeatureAnalyzer_2z : FeatureAnalyzer {
        public override string Version { get { return base.Version + "_2z_0.1"; } }
        public FeatureAnalyzer_2z() {

        }
    }

    public class FeatureAnalyzer_5S_Only : FeatureAnalyzer {
        public override string Version { get { return base.Version + "_5s_0.4"; } }
        public FeatureAnalyzer_5S_Only() {
            //classifiers.Add(UpperLeftSharpness);
            classifiers.Add(MiscFeatures);
            classifiers.Add(FeatureAnalyzer_Generic.AspectRatio);
            classifiers.Add(FeatureAnalyzer_Generic.NumberOfCusps);
            //names.Add("UpperLeftSharpness");
            //Console.WriteLine("5S Specific Analizer created!");
        }

        private static int FirstTangentPoint(FeaturePointDetector.CuspSet s, System.Drawing.Point axis) {
            return FirstTangentPoint(s, axis, 0);
        }
        private static int FirstTangentPoint(FeaturePointDetector.CuspSet s, System.Drawing.Point axis, int start) {
            if (s.pts.Length < start + 3) return -1;

            int index = start + 2;
            int lastSign = Math.Sign(V2D.Angle(V2D.Sub(s.pts[1], s.pts[0]), axis));
            while (index < s.pts.Length) {
                double ang = V2D.Angle(V2D.Sub(s.pts[index], s.pts[index - 1]), axis);
                if (lastSign != Math.Sign(ang))
                    break;
                if (Math.Abs(ang) < 1e-7)
                    break;
                index++;
            }
            if (index >= s.pts.Length) return -1;
            return index;
        }
        public static double[] UpperLeftSharpness(FeaturePointDetector.CuspSet s) {
            double[] result = new double[5];
            System.Drawing.Point xAxis = new System.Drawing.Point(1, 0);
            System.Drawing.Point yAxis = new System.Drawing.Point(0, 1);
            System.Drawing.Point topPivotAxis = new System.Drawing.Point(-1, 1);
            System.Drawing.Point bottomPivotAxis = new System.Drawing.Point(1, 1);

            int initial = s.pts.Length / 10;

            int ul = FirstTangentPoint(s, topPivotAxis, initial);
            int ll = FirstTangentPoint(s, bottomPivotAxis, ul + initial);

            if (ul < 0 || ll < 0) return result;

            result[0] = V2D.Straightness(s.pts, initial, ul);
            result[1] = s.avgCurveSeg(initial, ul);
            result[2] = V2D.Straightness(s.pts, ul, ll);
            result[3] = s.avgCurveSeg(ul, ll);
            result[4] = V2D.Angle(V2D.Sub(s.pts[initial], s.pts[ul]), V2D.Sub(s.pts[ll], s.pts[ul])) / (Math.PI);

            return result;
        }
        public static double[] AltULSharpness(FeaturePointDetector.CuspSet s) {
            double[] result = new double[5];

            // posit top bar at least crosses midline of the bb
            // start, follow until cross midline
            double midline = s.bbox.Width / 2 + s.bbox.Left;
            int indexOfMidlineCross = 0;
            while (indexOfMidlineCross < s.pts.Length && s.pts[indexOfMidlineCross].X > midline)
                indexOfMidlineCross++;

            // TODO now follow left and right to find straightness area.

            return result;
        }
        // Some misc features from FeaturePointDetector's 5, s, and 5s routines:
        public static double[] MiscFeatures(FeaturePointDetector.CuspSet s) {
            double[] result = new double[58];
            for (int i = 0; i < result.Length; i++)
                result[i] = Double.NaN; // This way, if a test can't be completed, it will be ignored...

            int min_ind, max_ind;
            result[0] = FeaturePointDetector.minx(0, s.pts.Length / 2, s.pts, out min_ind);
            result[1] = min_ind;
            result[2] = FeaturePointDetector.maxx(s.pts.Length / 2, s.pts.Length, s.pts, out max_ind);
            result[3] = max_ind;

            int midCusp = 2;
            if (s.cusps.Length > 2) {
                if (V2D.Straightness(s.pts, s.cusps[1].index, s.cusps[2].index) < .06 && s.cusps[2].curvature > 0)
                    midCusp = 3;
                result[4] = s.cusps[2].index - max_ind - 5;
                result[5] = s.cusps[2].index - max_ind - 2;
            }

            if (s.cusps.Length > 0) {
                if (s.pts.Length > s.cusps[0].index + 5)
                    result[6] = FeaturePointDetector.angle(s.pts[s.cusps[0].index + 5], s.cusps[0].pt, new System.Drawing.PointF(-1, 0));
                result[7] = s.cusps[0].pt.X - s.bbox.Left;
                result[8] = s.cusps[0].pt.Y - s.bbox.Top;
                result[9] = s.cusps[0].curvature;
                result[10] = s.cusps[0].pt.X - s.pts[min_ind].X;
                result[11] = s.cusps[0].pt.Y - s.pts[max_ind].Y;
            }
            if (s.cusps.Length > 1) {
                result[12] = V2D.Straightness(s.pts, 0, s.cusps[1].index);
                result[13] = Math.Abs(s.cusps[1].curvature);
                result[15] = s.cusps[1].pt.X - s.bbox.Left;
                result[16] = s.cusps[1].pt.Y - s.bbox.Top;
                result[17] = Math.Acos(V2D.Dot(V2D.Normalize(V2D.Sub(s.cusps[1].pt, s.cusps[0].pt)),
                    new System.Drawing.PointF(-1, 0))) * 180 / Math.PI;
                result[18] = s.cusps[1].curvature;
                result[19] = s.cusps[0].pt.X - s.cusps[1].pt.X;
            }
            if (s.cusps.Length > 2) {
                result[14] = V2D.Straightness(s.pts, s.cusps[1].index, s.cusps[2].index);
            }
            if (s.cusps.Length > midCusp) {
                result[20] = Math.Abs(s.cusps[midCusp - 1].curvature);
                result[21] = Math.Abs(s.cusps[midCusp].curvature);
                result[22] = s.cusps[midCusp].pt.X - s.bbox.Left;
                result[23] = s.avgCurve(midCusp, s.l);
                result[24] = s.cusps[midCusp].pt.Y - s.bbox.Top;
                result[25] = s.cusps[midCusp].curvature;
                result[26] = FeaturePointDetector.angle(s.cusps[midCusp].pt, s.cusps[1].pt, new System.Drawing.Point(-1, 2));
                result[27] = s.cusps[0].pt.X - s.cusps[midCusp].pt.X;
                result[28] = s.cusps[midCusp].pt.Y - s.cusps[s.l].pt.Y;
            }
            if (s.cusps.Length > 3) {
                result[29] = s.cusps[3].pt.X - s.bbox.Left;
                result[30] = s.cusps[3].pt.Y - s.bbox.Top;
                result[31] = s.cusps[3].curvature;
                result[32] = s.cusps[3].pt.X - s.cusps[s.l].pt.X;
                result[33] = s.cusps[0].pt.Y - s.cusps[3].pt.Y;
                result[34] = s.cusps[1].pt.Y - s.cusps[3].pt.Y;
            }
            if (s.cusps.Length > 4) {
                result[35] = s.cusps[4].pt.Y - s.bbox.Top;
                result[36] = s.cusps[4].curvature;
                result[37] = s.cusps[1].pt.Y - s.cusps[4].pt.Y;
            }
            result[38] = s.cusps[s.l].pt.X - s.bbox.Left;
            result[39] = s.cusps[s.l].pt.Y - s.bbox.Top;
            result[40] = s.cusps[s.l].curvature;

            result[41] = FeaturePointDetector.angle(s.pts[min_ind], s.pts[0], new System.Drawing.PointF(-1, 0));
            result[42] = (s.pts[(max_ind - min_ind) / 2 + min_ind].Y - s.pts[min_ind / 2].Y + 0.0) / s.bbox.Height;
            bool gotPos = false;
            result[43] = 0;
            for (int i = 1; i < s.cusps.Length - 1; i++) {
                if (s.cusps[i].curvature > 0.1)
                    gotPos = true;
                else if (s.cusps[i].curvature < -0.1 && gotPos)
                    result[43] = 1;
            }
            result[44] = FeaturePointDetector.angle(s.last, s.pts[max_ind], new System.Drawing.PointF(-1, 0));
            result[45] = FeaturePointDetector.angle(s.pts[min_ind], s.pts[max_ind], new System.Drawing.PointF(0, -1));
            result[46] = V2D.Dist(s.pts[max_ind], s.last) / s.bbox.Width;
            result[47] = s.pts[min_ind].X - s.bbox.Left;
            result[48] = s.pts[max_ind].X - s.bbox.Left;
            result[49] = s.pts[max_ind].Y - s.bbox.Top;
            result[50] = s.pts[min_ind].Y - s.bbox.Top;
            result[51] = s.curvatures[min_ind];
            result[52] = s.curvatures[max_ind];
            result[53] = (s.pts[0].X - s.bbox.Left + 0.0) / s.bbox.Width;
            result[54] = s.pts[min_ind].X - s.pts[max_ind].X;
            result[55] = s.pts[max_ind].X - s.cusps[s.l].pt.X;
            result[56] = s.pts[min_ind].Y - s.pts[max_ind].Y;
            result[57] = s.pts[min_ind].Y - s.cusps[s.l].pt.Y;

            return result;
        }
    }
    public class FeatureAnalyzer_5S : FeatureAnalyzer_Generic {
        public override string Version { get { return base.Version + "_5s_0.4"; } }
        public FeatureAnalyzer_5S() {
            //classifiers.Add(UpperLeftSharpness);
            classifiers.Add(MiscFeatures);
            classifiers.Add(FeatureAnalyzer_Generic.AspectRatio);
            classifiers.Add(FeatureAnalyzer_Generic.NumberOfCusps);
            //names.Add("UpperLeftSharpness");
            //Console.WriteLine("5S Specific Analizer created!");
        }

        private static int FirstTangentPoint(FeaturePointDetector.CuspSet s, System.Drawing.Point axis) {
            return FirstTangentPoint(s, axis, 0);
        }
        private static int FirstTangentPoint(FeaturePointDetector.CuspSet s, System.Drawing.Point axis, int start) {
            if (s.pts.Length < start + 3) return -1;

            int index = start + 2;
            int lastSign = Math.Sign(V2D.Angle(V2D.Sub(s.pts[1], s.pts[0]), axis));
            while (index < s.pts.Length) {
                double ang = V2D.Angle(V2D.Sub(s.pts[index], s.pts[index - 1]), axis);
                if (lastSign != Math.Sign(ang))
                    break;
                if (Math.Abs(ang) < 1e-7)
                    break;
                index++;
            }
            if (index >= s.pts.Length) return -1;
            return index;
        }
        public static double[] UpperLeftSharpness(FeaturePointDetector.CuspSet s) {
            double[] result = new double[5];
            System.Drawing.Point xAxis = new System.Drawing.Point(1, 0);
            System.Drawing.Point yAxis = new System.Drawing.Point(0, 1);
            System.Drawing.Point topPivotAxis = new System.Drawing.Point(-1, 1);
            System.Drawing.Point bottomPivotAxis = new System.Drawing.Point(1, 1);

            int initial = s.pts.Length / 10;

            int ul = FirstTangentPoint(s, topPivotAxis, initial);
            int ll = FirstTangentPoint(s, bottomPivotAxis, ul + initial);

            if (ul < 0 || ll < 0) return result;

            result[0] = V2D.Straightness(s.pts, initial, ul);
            result[1] = s.avgCurveSeg(initial, ul);
            result[2] = V2D.Straightness(s.pts, ul, ll);
            result[3] = s.avgCurveSeg(ul, ll);
            result[4] = V2D.Angle(V2D.Sub(s.pts[initial], s.pts[ul]), V2D.Sub(s.pts[ll], s.pts[ul])) / (Math.PI);

            return result;
        }
        public static double[] AltULSharpness(FeaturePointDetector.CuspSet s) {
            double[] result = new double[5];

            // posit top bar at least crosses midline of the bb
            // start, follow until cross midline
            double midline = s.bbox.Width / 2 + s.bbox.Left;
            int indexOfMidlineCross = 0;
            while (indexOfMidlineCross < s.pts.Length && s.pts[indexOfMidlineCross].X > midline)
                indexOfMidlineCross++;

            // TODO now follow left and right to find straightness area.

            return result;
        }
        // Some misc features from FeaturePointDetector's 5, s, and 5s routines:
        public static double[] MiscFeatures(FeaturePointDetector.CuspSet s) {
            double[] result = new double[58];
            for (int i = 0; i < result.Length; i++)
                result[i] = Double.NaN; // This way, if a test can't be completed, it will be ignored...

            int min_ind, max_ind;
            result[0] = FeaturePointDetector.minx(0, s.pts.Length / 2, s.pts, out min_ind);
            result[1] = min_ind;
            result[2] = FeaturePointDetector.maxx(s.pts.Length / 2, s.pts.Length, s.pts, out max_ind);
            result[3] = max_ind;

            int midCusp = 2;
            if (s.cusps.Length > 2) {
                if (V2D.Straightness(s.pts, s.cusps[1].index, s.cusps[2].index) < .06 && s.cusps[2].curvature > 0)
                    midCusp = 3;
                result[4] = s.cusps[2].index - max_ind - 5;
                result[5] = s.cusps[2].index - max_ind - 2;
            }

            if (s.cusps.Length > 0) {
                result[6] = FeaturePointDetector.angle(s.pts[s.cusps[0].index + 5], s.cusps[0].pt, new System.Drawing.PointF(-1, 0));
                result[7] = s.cusps[0].pt.X - s.bbox.Left;
                result[8] = s.cusps[0].pt.Y - s.bbox.Top;
                result[9] = s.cusps[0].curvature;
                result[10] = s.cusps[0].pt.X - s.pts[min_ind].X;
                result[11] = s.cusps[0].pt.Y - s.pts[max_ind].Y;
            }
            if (s.cusps.Length > 1) {
                result[12] = V2D.Straightness(s.pts, 0, s.cusps[1].index);
                result[13] = Math.Abs(s.cusps[1].curvature);
                result[15] = s.cusps[1].pt.X - s.bbox.Left;
                result[16] = s.cusps[1].pt.Y - s.bbox.Top;
                result[17] = Math.Acos(V2D.Dot(V2D.Normalize(V2D.Sub(s.cusps[1].pt, s.cusps[0].pt)),
                    new System.Drawing.PointF(-1, 0))) * 180 / Math.PI;
                result[18] = s.cusps[1].curvature;
                result[19] = s.cusps[0].pt.X - s.cusps[1].pt.X;
            }
            if (s.cusps.Length > midCusp) {
                result[20] = Math.Abs(s.cusps[midCusp - 1].curvature);
                result[21] = Math.Abs(s.cusps[midCusp].curvature);
                result[22] = s.cusps[midCusp].pt.X - s.bbox.Left;
                result[23] = s.avgCurve(midCusp, s.l);
                result[24] = s.cusps[midCusp].pt.Y - s.bbox.Top;
                result[25] = s.cusps[midCusp].curvature;
                result[26] = FeaturePointDetector.angle(s.cusps[midCusp].pt, s.cusps[1].pt, new System.Drawing.Point(-1, 2));
                result[27] = s.cusps[0].pt.X - s.cusps[midCusp].pt.X;
                result[28] = s.cusps[midCusp].pt.Y - s.cusps[s.l].pt.Y;
            }
            if (s.cusps.Length > 2) {
                result[14] = V2D.Straightness(s.pts, s.cusps[1].index, s.cusps[2].index);
            }
            if (s.cusps.Length > 3) {
                result[29] = s.cusps[3].pt.X - s.bbox.Left;
                result[30] = s.cusps[3].pt.Y - s.bbox.Top;
                result[31] = s.cusps[3].curvature;
                result[32] = s.cusps[3].pt.X - s.cusps[s.l].pt.X;
                result[33] = s.cusps[0].pt.Y - s.cusps[3].pt.Y;
                result[34] = s.cusps[1].pt.Y - s.cusps[3].pt.Y;
            }
            if (s.cusps.Length > 4) {
                result[35] = s.cusps[4].pt.Y - s.bbox.Top;
                result[36] = s.cusps[4].curvature;
                result[37] = s.cusps[1].pt.Y - s.cusps[4].pt.Y;
            }
            result[38] = s.cusps[s.l].pt.X - s.bbox.Left;
            result[39] = s.cusps[s.l].pt.Y - s.bbox.Top;
            result[40] = s.cusps[s.l].curvature;

            result[41] = FeaturePointDetector.angle(s.pts[min_ind], s.pts[0], new System.Drawing.PointF(-1, 0));
            result[42] = (s.pts[(max_ind - min_ind) / 2 + min_ind].Y - s.pts[min_ind / 2].Y + 0.0) / s.bbox.Height;
            bool gotPos = false;
            result[43] = 0;
            for (int i = 1; i < s.cusps.Length - 1; i++) {
                if (s.cusps[i].curvature > 0.1)
                    gotPos = true;
                else if (s.cusps[i].curvature < -0.1 && gotPos)
                    result[43] = 1;
            }
            result[44] = FeaturePointDetector.angle(s.last, s.pts[max_ind], new System.Drawing.PointF(-1, 0));
            result[45] = FeaturePointDetector.angle(s.pts[min_ind], s.pts[max_ind], new System.Drawing.PointF(0, -1));
            result[46] = V2D.Dist(s.pts[max_ind], s.last) / s.bbox.Width;
            result[47] = s.pts[min_ind].X - s.bbox.Left;
            result[48] = s.pts[max_ind].X - s.bbox.Left;
            result[49] = s.pts[max_ind].Y - s.bbox.Top;
            result[50] = s.pts[min_ind].Y - s.bbox.Top;
            result[51] = s.curvatures[min_ind];
            result[52] = s.curvatures[max_ind];
            result[53] = (s.pts[0].X - s.bbox.Left + 0.0) / s.bbox.Width;
            result[54] = s.pts[min_ind].X - s.pts[max_ind].X;
            result[55] = s.pts[max_ind].X - s.cusps[s.l].pt.X;
            result[56] = s.pts[min_ind].Y - s.pts[max_ind].Y;
            result[57] = s.pts[min_ind].Y - s.cusps[s.l].pt.Y;

            return result;
        }
    }

    public class FeatureAnalyzer_Generic : FeatureAnalyzer {
        public override string Version { get { return base.Version + "_generic_0.1"; } }
        public FeatureAnalyzer_Generic() {
            classifiers.Add(ArcLength);
            classifiers.Add(StartEndDistance);
            classifiers.Add(AspectRatio);
            //            classifiers.Add(NumberOfCusps);
            classifiers.Add(InterCuspDistance);
            classifiers.Add(NumberOfSelfIntersects);
            classifiers.Add(InterSelfIntersectDistance);
            classifiers.Add(CoarsePointHistogram);
            classifiers.Add(AngleHistogram);
            classifiers.Add(StrokeArea);
            classifiers.Add(StartRatio);
            classifiers.Add(AreasOfCurvature);
            //classifiers.Add(CuspCurvatures);
            classifiers.Add(CuspLeftRightTopBottom);
        }
        // These are feature classification functions that return the average of a feature of a Stroke

        public static double[] CuspLeftRightTopBottom(FeaturePointDetector.CuspSet s) {
            double[] result = new double[12];
            result[0] = s.cusps[0].left ? 1 : 0;
            result[1] = s.cusps[0].right ? 1 : 0;
            result[2] = s.cusps[0].top ? 1 : 0;
            result[3] = s.cusps[0].bot ? 1 : 0;
            result[4] = s.cusps[s.l].left ? 1 : 0;
            result[5] = s.cusps[s.l].right ? 1 : 0;
            result[6] = s.cusps[s.l].top ? 1 : 0;
            result[7] = s.cusps[s.l].bot ? 1 : 0;
            for (int i = 0; i <= s.l; i++) {
                if (s.cusps[i].top) result[8]++;
                if (s.cusps[i].bot) result[9]++;
                if (s.cusps[i].left) result[10]++;
                if (s.cusps[i].right) result[11]++;
            }
            result[8] /= (s.l + 1);
            result[9] /= (s.l + 1);
            result[10] /= (s.l + 1);
            result[11] /= (s.l + 1);
            return result;
        }
        //public static double[] CuspCurvatures(FeaturePointDetector.CuspSet s) {
        //    double[] result = new double[s.cusps.Length];
        //    for (int i = 0; i < s.cusps.Length; i++)
        //        result[i] = s.cusps[i].curvature;
        //    return result;
        //}
        public static double[] StartEndDistance(FeaturePointDetector.CuspSet s) {
            // Ratio of distance between start and end points to overall size of character
            double[] result = new double[1];
            double size = Math.Max(s.bbox.Height, s.bbox.Width);
            result[0] = V2D.Dist(s.pts[0], s.pts[s.pts.Length - 1]) / size;
            return result;
        }
        public static double[] ArcLength(FeaturePointDetector.CuspSet s) {
            // Ratio of distance between start and end points to overall size of character
            double[] result = new double[1];
            double size = Math.Max(s.bbox.Height, s.bbox.Width);
            result[0] = s.dist / size;
            return result;
        }
        public static double[] AspectRatio(FeaturePointDetector.CuspSet s) {
            double[] result = new double[1];
            result[0] = ((double)s.bbox.Height) / ((double)s.bbox.Width);
            return result;
        }
        public static double[] NumberOfCusps(FeaturePointDetector.CuspSet s) {
            double[] result = new double[1];
            result[0] = (double)s.cusps.Length;
            return result;
        }
        public static double[] InterCuspDistance(FeaturePointDetector.CuspSet s) {
            double size = Math.Max(s.bbox.Height, s.bbox.Width);
            double[] result = new double[3]; //min, max, average
            result[0] = result[1] = s.distances[s.cusps[1].index];    // There will always be at least 2 cusps...
            result[2] = s.distances[s.cusps[1].index];
            for (int i = 2; i < s.cusps.Length; i++) {
                double d = s.distances[s.cusps[i].index] - s.distances[s.cusps[i - 1].index];
                result[0] = Math.Min(result[0], d);
                result[1] = Math.Max(result[1], d);
                result[2] += d;
            }
            result[2] /= (s.cusps.Length - 1);

            result[0] /= size;
            result[1] /= size;
            result[2] /= size;
            return result;
        }
        public static double[] NumberOfSelfIntersects(FeaturePointDetector.CuspSet s) {
            double[] result = new double[1];
            if (s.intersects != null)
                result[0] = (double)s.intersects.Length;
            return result;
        }
        public static double[] InterSelfIntersectDistance(FeaturePointDetector.CuspSet s) {
            double size = Math.Max(s.bbox.Height, s.bbox.Width);
            double[] result = new double[3]; // min, max, average
            if (s.intersects == null || s.intersects.Length < 2)
                return result;
            result[0] = result[1] = result[2] = s.distances[s.intersects[1]];
            for (int i = 2; i < s.intersects.Length; i++) {
                double d = s.distances[s.intersects[i]] - s.distances[s.intersects[i - 1]];
                result[0] = Math.Min(result[0], d);
                result[1] = Math.Max(result[1], d);
                result[2] += d;
            }
            result[2] /= (s.intersects.Length);

            result[0] /= size;
            result[1] /= size;
            result[2] /= size;
            return result;
        }
        public static double[] CoarsePointHistogram(FeaturePointDetector.CuspSet s) {
            double[] h = new double[9]; // 3x3 histogram
            int top = s.bbox.Top;
            int left = s.bbox.Left;

            int xwidth = s.bbox.Width / 3;
            int ywidth = s.bbox.Height / 3;

            if (xwidth == 0 || ywidth == 0) return h;

            //Console.WriteLine("{0} {1}", xwidth,ywidth);

            for (int i = 0; i < s.pts.Length; i++) {
                int x = s.pts[i].X - left;
                int y = s.pts[i].Y - top;

                int xindex = x / xwidth;
                if (xindex > 2) xindex = 2;
                int yindex = y / ywidth;
                if (yindex > 2) yindex = 2;

                h[xindex + 3 * yindex]++;
                //Console.WriteLine("({0},{1}) in {2} {3}", x, y, xindex, yindex);
            }

            for (int i = 0; i < h.Length; i++)
                h[i] /= s.pts.Length;

            return h;
        }
        // This seems to cause too much overfitting
        public static double[] FinePointHistogram(FeaturePointDetector.CuspSet s) {
            double[] h = new double[49]; // 7x7 histogram
            int top = s.bbox.Top;
            int left = s.bbox.Left;

            int xwidth = s.bbox.Width / 7;
            int ywidth = s.bbox.Height / 7;

            if (xwidth == 0 || ywidth == 0) return h;

            //Console.WriteLine("{0} {1}", xwidth,ywidth);

            for (int i = 0; i < s.pts.Length; i++) {
                int x = s.pts[i].X - left;
                int y = s.pts[i].Y - top;

                int xindex = x / xwidth;
                if (xindex > 2) xindex = 6;
                int yindex = y / ywidth;
                if (yindex > 2) yindex = 6;

                h[xindex + 7 * yindex]++;
                //Console.WriteLine("({0},{1}) in {2} {3}", x, y, xindex, yindex);
            }
            for (int i = 0; i < h.Length; i++)
                h[i] /= s.pts.Length;

            return h;
        }
        public static double[] AngleHistogram(FeaturePointDetector.CuspSet s) {
            double[] h = new double[8];

            System.Drawing.Point xAxis = new System.Drawing.Point(1, 0);
            double partition = Math.PI / 4;

            for (int i = 1; i < s.pts.Length; i++) {
                double ang = V2D.Angle(V2D.Sub(s.pts[i], s.pts[i - 1]), xAxis) + Math.PI;
                h[(int)(ang / partition)]++;
            }

            for (int i = 0; i < h.Length; i++)
                h[i] /= (s.pts.Length - 1);

            return h;
        }
        public static double[] StrokeArea(FeaturePointDetector.CuspSet s) {
            double[] result = new double[1];
            double area = s.bbox.Height * s.bbox.Width;

            for (int i = 2; i < s.pts.Length; i++) {
                double ang = V2D.Angle(V2D.Sub(s.pts[i], s.pts[i - 2]), V2D.Sub(s.pts[i - 1], s.pts[i - 2]));
                result[0] += (V2D.Dist(s.pts[i], s.pts[i - 2]) * V2D.Dist(s.pts[i - 1], s.pts[i - 2]) * Math.Abs(Math.Sin(ang))) / 2;
            }

            result[0] /= area;
            return result;
        }
        public static double[] StartRatio(FeaturePointDetector.CuspSet s) {
            double[] result = new double[4];

            result[0] = (s.pts[0].X - s.bbox.Left) / s.bbox.Width;
            result[1] = (s.pts[s.pts.Length - 1].X - s.bbox.Left) / s.bbox.Width;
            result[2] = (s.pts[0].Y - s.bbox.Top) / s.bbox.Height;
            result[3] = (s.pts[s.pts.Length - 1].Y - s.bbox.Top) / s.bbox.Height;

            return result;
        }
        public static double[] AreasOfCurvature(FeaturePointDetector.CuspSet s) {
            double[] result = new double[10]; // numeber, min/max/av length, min/max/average distbetween, min/max/av straightness between
            double size = s.dist;
            double THRESHOLD = 0.1275; // Same as used in FeaturePointDetector for "areas of high curvature"

            bool startInAreaOfHighCurvature = Math.Abs(s.curvatures[0]) > THRESHOLD;
            List<int> transitions = new List<int>();
            transitions.Add(0);
            bool inAreaHighCurve = startInAreaOfHighCurvature;

            for (int i = 1; i < s.curvatures.Length; i++)
                if (Math.Abs(s.curvatures[i]) > THRESHOLD) {
                    if (!inAreaHighCurve) { // Just entered
                        transitions.Add(i);
                        inAreaHighCurve = true;
                    }
                } else {
                    if (inAreaHighCurve) { // Just left
                        transitions.Add(i);
                        inAreaHighCurve = false;
                    }
                }
            if (inAreaHighCurve)
                transitions.Add(s.curvatures.Length - 1);

            bool first = true;
            int nSpaces = 0;
            double maxDist = 0, minDist = 0, totalDist = 0;
            double minStraght = 0, maxStraght = 0, totalStraght = 0;
            // iterate over spaces between high curvatures
            int j = 0;
            if (startInAreaOfHighCurvature)
                j = 1;
            while ((j + 1) < transitions.Count) {
                nSpaces++;
                double d = s.distances[transitions[j + 1]] - s.distances[transitions[j]];
                double st = V2D.Straightness(s.pts, transitions[j], transitions[j + 1]);
                if (first) {
                    first = false;
                    maxDist = minDist = totalDist = d;
                    minStraght = maxStraght = totalStraght = st;
                } else {
                    maxDist = Math.Max(maxDist, d);
                    minDist = Math.Min(minDist, d);
                    totalDist += d;
                    maxStraght = Math.Max(maxStraght, st);
                    minStraght = Math.Min(minStraght, st);
                    totalStraght += st;
                }
                j += 2;
            }
            int nAreas = 0;
            first = true;
            double maxLength = 0, minLength = 0, totalLength = 0;
            j = 1;
            if (startInAreaOfHighCurvature)
                j = 0;
            while ((j + 1) < transitions.Count) {
                nAreas++;
                double d = s.distances[transitions[j + 1]] - s.distances[transitions[j]];
                if (first) {
                    first = false;
                    maxLength = minLength = totalLength = d;
                } else {
                    maxLength = Math.Max(maxLength, d);
                    minLength = Math.Min(minLength, d);
                    totalLength += d;
                }
                j += 2;
            }

            //numeber, min/max/av length, min/max/average distbetween, min/max/av straightness between
            //result[0] = nAreas;
            result[1] = minLength /= size;
            result[2] = maxLength /= size;
            result[3] = totalLength / (nAreas * size);
            result[4] = minDist /= size;
            result[5] = maxDist /= size;
            result[6] = totalDist / (nSpaces * size);
            result[7] = minStraght;
            result[8] = maxStraght;
            result[9] = totalStraght / nSpaces;

            //for (int i = 0; i < 10; i++)
            //    Console.Write("{0} ", result[i]);
            //Console.WriteLine("");

            return result;
        }
    }

    public class FeatureAnalyzer {
        public virtual string Version { get { return "FeatureAnalyzer_0.1"; } }
        public delegate double[] Classifier(FeaturePointDetector.CuspSet cs);
        protected List<Classifier> classifiers;
        //protected List<string> names;
        public FeatureAnalyzer() {
            classifiers = new List<Classifier>();
            //names = new List<string>();
            //foreach (Classifier c in classifiers) {
            //    names.Add(c.Method.Name);
            //}
        }
        public Classifier this[int i] { get { return classifiers[i]; } }
        public int Count { get { return classifiers.Count; } }
        public string Names(int i) {
            return classifiers[i].Method.Name;
        }
        public List<Classifier> Classifiers { get { return classifiers; } }
    }
}
