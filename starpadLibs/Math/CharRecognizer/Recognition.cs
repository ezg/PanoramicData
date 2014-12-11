using System;
using System.IO;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.Ink;
using System.Diagnostics;
using starPadSDK.UnicodeNs;
using System.Runtime.Serialization;


namespace starPadSDK.CharRecognizer {
    [Serializable]
    public class Recognition {
        public static Guid SuperGuid         = new Guid("{DAC2F1B4-183F-49aa-8EC1-3D2964B84651}");
        public static Guid SubGuid           = new Guid("{455CF1D7-AEC6-4e68-9932-A2AF244CC78F}");
        public static Guid SuperIntegralGuid = new Guid("{77C2625E-B4DF-4fba-8D77-CD928C008AC7}");
        public static Guid SubIntegralGuid   = new Guid("{D96AB867-1D52-4cc8-B559-D70D35641AC1}");
        public delegate void AlternateChanging(Recognition r, int oldalt);
        public static event AlternateChanging AlternateChangingEvent;
        private int superId = -1;
        private int subId = -1;
        private int superIntegralId = -1;
        private int subIntegralId = -1;
        private int matrixId = -1;
        private int parenId = -1;
        private Rectangle _bbox = Rectangle.Empty;
        public Rectangle bbox { get { return _bbox; } }
        private void setVal(Guid guid, int val) {
            foreach (Stroke s in strokes)
                s.ExtendedProperties.Add(guid, val);
        }
        private void remVal(Guid guid) {
            foreach (Stroke s in strokes)
                s.ExtendedProperties.Remove(guid);
        }

        public int SubIntegralId {
            get { return subIntegralId; }
            set {
                if (subIntegralId == -1 && value != -1)
                    setVal(SubIntegralGuid, value);
                else if (subIntegralId != -1 && value == -1)
                    remVal(SubIntegralGuid);
                subIntegralId = value;
            }
        }
        public int SubId {
            get { return subId; }
            set {
                if (subId == -1 && value != -1)
                    setVal(SubGuid, value);
                else if (subId != -1&& value == -1)
                    remVal(SubGuid);
                subId = value;
            }
        }
        public int SuperIntegralId {
            get { return superIntegralId; }
            set {
                if (superIntegralId == -1 && value != -1)
                    setVal(SuperIntegralGuid, value);
                else if (superIntegralId != -1 && value == -1)
                    remVal(SuperIntegralGuid);
                superIntegralId = value;
            }
        }
        public int SuperId {
            get { return superId; }
            set {
                if (superId == -1 && value != -1)
                    setVal(SuperGuid, value);
                else if (superId != -1 && value == -1)
                    remVal(SuperGuid);
                superId = value;
            }
        }
        public int MatrixId {
            get { return matrixId; }
            set { matrixId = value; }
        }
        private Strokes _strokes;
        public Strokes strokes { get { return _strokes; } }
        /// <summary>
        /// allograph recognized
        /// </summary>
        public readonly string allograph;
        private int _curalt;
        /// <summary>
        /// index of current alternate in list
        /// </summary>
        public int curalt { get { return _curalt; }
            set {
                Trace.Assert(value >= 0 && value < alts.Length);
                //if(AlternateChangedEvent != null) AlternateChangedEvent(this, _curalt, value);
                _curalt = value;
            } 
        }
        /// <summary>
        /// list of alternates
        /// </summary>
        public Result[] alts;
        /// <summary>
        /// Current alternate chosen
        /// </summary>
        public Result alt { get { return alts[curalt]; } }
        /// <summary>
        /// baselines for each alternate
        /// </summary>
        public int[] baselinealts;
        /// <summary>
        /// baseline corresponding to current chosen alternate
        /// </summary>
        public int baseline { get { return baselinealts[curalt]; } }
        /// <summary>
        /// xheights for each alternate
        /// </summary>
        public int[] xheightalts;
        /// <summary>
        /// xheight corresponding to current chosen alternate
        /// </summary>
        public int xheight { get { return xheightalts[curalt]; } }
        private Hashtable data = new Hashtable();
        public Hashtable Data { get { return data; } }
        /// <summary>
        /// The alternate was set by this level of parsing and should not be changed by a later stage. Level 0 is the user, 1 is
        /// parse1 (Parse), 2 is parse2, maxint is unset by anyone. This value is negated when a reparse exception is thrown to 
        /// prevent it being cleared at the start of (re)parsing; its value is then the negative of the level calling the reparsing.
        /// </summary>
        public int levelsetby;
        public bool parseError = false;
        public bool msftRecoged; // true if we couldn't recognize the allograph at all and are relying on the MSFT recognizer
        public bool Different; // true if our recognizer returned a different symbol than MSFT's
        public Guid guid = Guid.NewGuid();
        public Recognition(Stroke s, string _allograph, int midpt, bool msftRecog)
            : this(s.Ink.CreateStrokes(new int[] { s.Id }), _allograph, midpt, msftRecog) { }
        public Recognition(Stroke s, string _allograph, int baseline, int midpt, bool msftRecog)
            : this(s.Ink.CreateStrokes(new int[] { s.Id }), _allograph, baseline, midpt, msftRecog) { }
        public Recognition(Strokes s, string _allograph, int midpt, bool msftRecog)
            : this(s, _allograph, s.GetBoundingBox().Bottom, midpt, msftRecog) { }
        public Recognition(Strokes s, string _allograph, int baseline, int midpt, bool msftRecog) {
            _strokes = s;
            _bbox = s.GetBoundingBox();
            updateIds();
            allograph = _allograph;
            _curalt = 0;
            Result[] tmpalts;

            if (_allographToAlternates.TryGetValue(allograph+(msftRecog?"MSFT":""), out tmpalts)) {
                alts = (Result[])tmpalts.Clone();
            } else if(_allograph.Length == 1) {
                alts = new Result[] { new Result(_allograph[0]) };
            } else {
                alts = new Result[] { new Result(_allograph) };
            }


            for (int i = 0; i < alts.Length; i++)
                alts[i] = new Result(alts[i]);
            baselinealts = new int[alts.Length];
            xheightalts = new int[alts.Length];
            levelsetby = int.MaxValue;
            msftRecoged = msftRecog;
            for (int i = 0; i < alts.Length; i++)
                SetMetrics(alts[i] == Result.Special.Imaginary ? "i" : alts[i], // bcz: Hack!  need to do something similar for 'e' etc?
                    baseline, midpt, ref baselinealts[i], ref xheightalts[i]);
        }
        public Recognition(Strokes s, Result r, int baseline, int midpt) {
            _strokes = s;
            _bbox = s.GetBoundingBox();
            updateIds();
            allograph = " "; // ez: this string can not be "", crashes in Parser.cs line 2965
            _curalt = 0;
            alts = new Result[] { r };
            baselinealts = new int[] { baseline };
            xheightalts = new int[] { midpt };
            levelsetby = int.MaxValue;
            msftRecoged = false;
        }
        // actually, this next one is just used for setting word recognition from WPF-using stuff, replacing variant after
        public Recognition(Strokes s, string allog, string word, int baseline, int midpt) {
            _strokes = s;
            _bbox = s.GetBoundingBox();
            updateIds();
            allograph = allog;
            _curalt = 0;
            alts = new Result[] { word.Length == 1 ? new Result(word[0]) : new Result(word) };
            baselinealts = new int[] { baseline };
            xheightalts = new int[] { midpt };
            levelsetby = int.MaxValue;
            msftRecoged = true;
            Console.WriteLine("Learner Hook: Here we will replace a result");
        }
        public Recognition(Strokes s, RecognitionAlternate ra) : this(s, "__MS word__", ra.ToString(), ra.Baseline.BeginPoint.Y, ra.Midline.BeginPoint.Y) { }

        void updateIds() {
            if (strokes[0].ExtendedProperties.Contains(SuperGuid))
                SuperId = (int)strokes[0].ExtendedProperties[SuperGuid].Data;
            if (strokes[0].ExtendedProperties.Contains(SuperIntegralGuid))
                SuperIntegralId = (int)strokes[0].ExtendedProperties[SuperIntegralGuid].Data;
            if (strokes[0].ExtendedProperties.Contains(SubGuid))
                SubId = (int)strokes[0].ExtendedProperties[SubGuid].Data;
            if (strokes[0].ExtendedProperties.Contains(SubIntegralGuid))
                SubIntegralId = (int)strokes[0].ExtendedProperties[SubIntegralGuid].Data;
        }

        public void addorsetalt(Result r, int baseline, int midpt) {
            if (AlternateChangingEvent != null) AlternateChangingEvent(this, _curalt);
            for(int i = 0; i < alts.Length; i++) {
                if(alts[i] == r) {
                    curalt = i;
                    break;
                }
            }
            if(alt != r) {
                Result[] nalts = new Result[alts.Length+1];
                Array.Copy(alts, nalts, alts.Length);
                nalts[alts.Length] = r;

                int[] nbaselinealts = new int[baselinealts.Length+1];
                Array.Copy(baselinealts, nbaselinealts, baselinealts.Length);

                int[] nxheightalts = new int[xheightalts.Length+1];
                Array.Copy(xheightalts, nxheightalts, xheightalts.Length);

                SetMetrics(r, baseline, midpt, ref nbaselinealts[alts.Length], ref nxheightalts[alts.Length]);

                alts = nalts;
                baselinealts = nbaselinealts;
                xheightalts = nxheightalts;
                curalt = alts.Length-1;
            }
        }
        public void ScaleToRectangle(Rectangle oldR, Rectangle r) {
            for (int i = 0; i < baselinealts.Length; i++) {
                baselinealts[i] = (int)((baselinealts[i]- oldR.Top+0.0)/oldR.Height * r.Height + r.Top);
            }
            for (int i = 0; i < xheightalts.Length; i++)
                xheightalts[i] = (int)((xheightalts[i]- oldR.Top+0.0)/oldR.Height * r.Height + r.Top);
            _bbox.Offset(V2D.Sub(r.Location, oldR.Location));
        }
        public void Offset(int offsetX, int  offsetY) {
            for (int i = 0; i < baselinealts.Length; i++)
                baselinealts[i] += offsetY;
            for (int i = 0; i < xheightalts.Length; i++)
                xheightalts[i] += offsetY;
            _bbox.Offset(offsetX, offsetY);
        }

        private static Dictionary<string, Result[]> _userAllographAlternates;
        private static Dictionary<string, Result[]> _allographToAlternates;
        private static string _pathToUserAllographAlternateStorage = "";
        private static string UserAllographFileName() {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), 
                                "Brown University\\starPad SDK\\myallographs.alpf");
        }
        public static void UseAllographAlternatesFromSettings() {
            _pathToUserAllographAlternateStorage = UserAllographFileName();
            InitializeAllographAlternates();
            LoadAllographAlternates();
        }
        public static void UseAllographAlternatesFromFile(string path) {
            _pathToUserAllographAlternateStorage = path;
            InitializeAllographAlternates();
            LoadAllographAlternates();
        }
        private static void LoadAllographAlternates() {
            string userPrefs = "";
            if (_pathToUserAllographAlternateStorage == "") {
                userPrefs = Properties.Settings.Default.AllographAlternateOverride;
            } else if (File.Exists(_pathToUserAllographAlternateStorage)) {
                using (FileStream myStream = new FileStream(_pathToUserAllographAlternateStorage, FileMode.Open, FileAccess.Read)) {
                    StreamReader sr = new StreamReader(myStream);
                    userPrefs = sr.ReadToEnd();
                }
            }
            if (userPrefs != null) {
                System.IO.StringReader sr = new System.IO.StringReader(userPrefs);
                string line;
                while ((line = sr.ReadLine()) != null) {
                    if (line.Length > 0) {
                        string[] allographAndResults = line.Split(new char[] { ',' });
                        if (line[0] == ',') { //special case, comma is allograph:
                            string[] temp = new string[allographAndResults.Length - 1];
                            temp[0] = ",";
                            for (int i = 2; i < allographAndResults.Length; i++)
                                temp[i - 1] = allographAndResults[i];
                            allographAndResults = temp;
                        }
                        if (allographAndResults.Length > 1) {
                            Console.Write("Your Preferences for " + allographAndResults[0] + ": ");
                            Result[] results = new Result[allographAndResults.Length - 1];
                            for (int i = 0; i < results.Length; i++) {
                                results[i] = new Result();
                                results[i].SetFromResultCode(allographAndResults[i + 1]);
                                Console.Write(results[i].ToString() + " ");
                            }
                            Console.WriteLine();
                            _allographToAlternates[allographAndResults[0]] = results;
                            _userAllographAlternates[allographAndResults[0]] = results;
                        }
                    }
                }
            }
        }
        private static void InitializeAllographAlternates() {
            _allographToAlternates = new Dictionary<string, Result[]>();
            _userAllographAlternates = new Dictionary<string, Result[]>();
            //single-character-named allographs, following identities string order above, then adding other chars
            //FIXME: should go through and make all cursive allographs be different from corresponding noncurisves. Eg, for 'j', 't', ...
            //       for cursive make u have mu and micro sign as alternate, etc.
            InitialSetAlternates("a", 'a', '9', 'u');
            InitialSetAlternates("ad", 'a', 'd');
            InitialSetAlternates("b", 'b', '3');
            InitialSetAlternates("da", 'd', 'a');
            InitialSetAlternates("c", 'c', 'C', Unicode.S.SUBSET_OF, '[');
            InitialSetAlternates("cd", 'c', 'd', 'C');
            InitialSetAlternates("e", 'e', Result.Special.NatLogBase, 'l', 'c');
            InitialSetAlternates("Escript", 'ε', 'E');
            InitialSetAlternates("f", 'f', '+');
            InitialSetAlternates("F", 'F', 'f');
            //InitialSetAlternates("g9", '9', 'g', 'y' , 's'); ez: use this line for euro-handwriting ;-)
            InitialSetAlternates("g9", 'g', 'y', '9', 's');
            InitialSetAlternates("g", 'g', 'y');
            InitialSetAlternates("h", 'h', 'n');
            InitialSetAlternates("hu", 'h', 'u', 'n');
            InitialSetAlternates("i", 'i', Result.Special.Imaginary, ':', ';');
            InitialSetAlternates("iz", 'i', "dz");
            InitialSetAlternates("j", 'j', ';');
            InitialSetAlternates("jscript", 'j', "dy");
            InitialSetAlternates("k", 'k', 'K');
            InitialSetAlternates("K", 'K', 'k', 'x', 'X');
            InitialSetAlternates("k2", 'k', 'K', 'x', 'X');
            InitialSetAlternates("L", 'L', Unicode.L.LEFT_FLOOR, 'c', '2', '(', '<');
            InitialSetAlternates("l", 'l', 'e', Result.Special.NatLogBase);
            InitialSetAlternates("I", 'I', '±', Unicode.M.MINUS_OR_PLUS_SIGN);
            InitialSetAlternates("m", 'm', 'M', 'μ');
            InitialSetAlternates("mn", 'm', 'M', 'n');
            InitialSetAlternates("n", 'n', 'h', Unicode.I.INTERSECTION, Unicode.N.N_ARY_INTERSECTION);
            InitialSetAlternates("p", 'p', 'P');
            InitialSetAlternates("P", 'P', 'p');
            InitialSetAlternates("q", 'q');
            InitialSetAlternates("q9", 'q', '9');
            InitialSetAlternates("s", 's', Unicode.I.INTEGRAL, 'S', '5', 'g');
            InitialSetAlternates("T", 'T', '+', 't');
            InitialSetAlternates("u", 'u', 'n', 'v');
            InitialSetAlternates("un", 'n', 'u');
            InitialSetAlternates("um", 'u', 'm');
            InitialSetAlternates("v", 'v', 'r', 'ν', 'γ', Unicode.L.LOGICAL_OR, Unicode.N.N_ARY_LOGICAL_OR);
            InitialSetAlternates("vr", 'v', Unicode.S.SQUARE_ROOT, 'r',  'σ');
            InitialSetAlternates("w", 'w', 'ω', 'W');
            InitialSetAlternates("x", 'x', 'X', 't', Unicode.M.MULTIPLICATION_SIGN, '+');
            InitialSetAlternates("xx", 'x', 'k', 'X');
            InitialSetAlternates("y", 'y', 'Y', '4', '2','P');
            InitialSetAlternates("z", 'z', '2');
            InitialSetAlternates(")1MSFT", ')', '1');
            InitialSetAlternates("(1MSFT", '(', '1');
            InitialSetAlternates("zMSFT", 'z', 'L');
            InitialSetAlternates("ZMSFT", 'z', 'L');
            InitialSetAlternates("AMSFT", 'A', 's');
            InitialSetAlternates("2MSFT", '2', 'L');
            InitialSetAlternates("uMSFT", 'u', 'n', 'v', 'h');
            InitialSetAlternates("0MSFT", '0', 'o', 'O');
            InitialSetAlternates("zed", 'z', 'x', 'Z', 't');
            InitialSetAlternates("0", '0', 'o', 'O');
            InitialSetAlternates("1", '1', '|', Unicode.D.DIVISION_SLASH, Unicode.D.DIVIDES, 'l');
            InitialSetAlternates("||", Unicode.D.DOUBLE_VERTICAL_LINE, Unicode.P.PARALLEL_TO);
            InitialSetAlternates("17", '1', '7', 'n');
            InitialSetAlternates("1^", '1', '^', 'Λ', Unicode.L.LOGICAL_AND, Unicode.N.N_ARY_LOGICAL_AND);
            InitialSetAlternates("^1", '^', 'Λ', Unicode.L.LOGICAL_AND, Unicode.N.N_ARY_LOGICAL_AND, '1');
            InitialSetAlternates("2", '2', 'α', '∂');
            InitialSetAlternates("2y", '2', 'y');
            InitialSetAlternates("3", '3', '}');
            InitialSetAlternates("4", '4', 'y', 'Y');
            InitialSetAlternates("4y", '4', 'y');
            InitialSetAlternates("6", '6', 'b');
            InitialSetAlternates("7", '7', '>', Unicode.N.NOT_SIGN, Unicode.R.RIGHT_CEILING);
            InitialSetAlternates("8", '8', 'σ', 'g');
            InitialSetAlternates("9", '9', 'g', 'q');
            InitialSetAlternates("+", '+', 't', 'x','f', Unicode.A.ASSERTION, Unicode.R.RIGHT_TACK);
            InitialSetAlternates(">", '>', '7', ')');
            InitialSetAlternates("<", '<', 'L', 'c');
            InitialSetAlternates(",", ',', '.', '1', ')');
            InitialSetAlternates(")", ')', ',', '>', Unicode.S.SUPERSET_OF, ']');
            InitialSetAlternates("^", '1', '^', 'Λ', Unicode.L.LOGICAL_AND, Unicode.N.N_ARY_LOGICAL_AND);
            InitialSetAlternates("INTtop s", Unicode.I.INTEGRAL, 's');
            InitialSetAlternates("/", '/', Unicode.D.DIVISION_SLASH, '1', ',', Result.Special.Division);
            // InitialSetAlternates("~", '~',Unicode.M.MINUS_SIGN,  Unicode.T.TILDE_OPERATOR); // bcz: For TABLETS!!
            InitialSetAlternates("~", Unicode.M.MINUS_SIGN, '~', Unicode.T.TILDE_OPERATOR); // bcz: FOR SURFACE!!
            InitialSetAlternates("}", '}', '3');
            InitialSetAlternates("\\", Unicode.S.SET_MINUS, '1', '\\');
            InitialSetAlternates("-", Unicode.M.MINUS_SIGN, '-', Result.Special.Division);
            InitialSetAlternates("c(", 'c', '(', 'C');
            InitialSetAlternates("(c", '(', 'c', 'C');
            InitialSetAlternates("(1", '(', '1', '|', Unicode.D.DIVIDES);
            InitialSetAlternates(")1", ')', '1', ',', '|', Unicode.D.DIVIDES);
            InitialSetAlternates("1(", '1', '(', '|', Unicode.D.DIVIDES);
            InitialSetAlternates("1)", '1', ')', ',', '|', Unicode.D.DIVIDES);
            InitialSetAlternates(")S", ')', Unicode.S.SUPERSET_OF);
            InitialSetAlternates("1()", '1', '(', ')', Unicode.D.DIVISION_SLASH, '|', Unicode.D.DIVIDES);
            InitialSetAlternates("1)(", '1', ')', '(', ',', Unicode.D.DIVISION_SLASH, '|', Unicode.D.DIVIDES);
            //allographs named as variants of single characters
            InitialSetAlternates("script-t", 't');
            InitialSetAlternates("iscript", Result.Special.Imaginary, 'i', ':', ';');
            InitialSetAlternates("uv", 'u', 'v', Unicode.U.UNION, Unicode.N.N_ARY_UNION);
            InitialSetAlternates("2z", '2', 'z');
            InitialSetAlternates("z2", 'z', '2');
            InitialSetAlternates("d2", '2', '∂', 'd', 'α');
            InitialSetAlternates("2partial", '∂', '2', '0');
            InitialSetAlternates("r", 'r', 'v', Unicode.S.SQUARE_ROOT, 'γ', 'n', 'Γ', Unicode.L.LEFT_CEILING);
            InitialSetAlternates("5s", '5', 's');
            InitialSetAlternates("y4", 'y', '4', 'Y', 'Ψ', 'P');
            InitialSetAlternates("y44", 'y', '4', 'Ψ','P');
            InitialSetAlternates("b6", '6', 'b');
            InitialSetAlternates("2p", '∂', '2');
            InitialSetAlternates("t", 't', '+', 'x', Unicode.E.ELEMENT_OF);
            InitialSetAlternates("mM", 'M', 'm', 'μ');
            //allographs named by words and other
            InitialSetAlternates("beta", 'β', 'B');
            InitialSetAlternates("alpha", 'α', '2', 'n', 'h');
            InitialSetAlternates("alphax", 'x', 'α', '2', 'n', 'h');
            InitialSetAlternates("gamma", 'γ', 'r');
            InitialSetAlternates("omega", 'ω', 'w');
            InitialSetAlternates("Omega", Unicode.G.GREEK_CAPITAL_LETTER_OMEGA);
            InitialSetAlternates("not", Unicode.N.NOT_SIGN, '7');
            InitialSetAlternates("vectorArrow", Unicode.R.RIGHTWARDS_HARPOON_WITH_BARB_UPWARDS);
            InitialSetAlternates("aproxEqTo", Unicode.A.APPROXIMATELY_EQUAL_TO);
            InitialSetAlternates("asympEqTo", Unicode.A.ASYMPTOTICALLY_EQUAL_TO);
            InitialSetAlternates("sigma", 'σ', 'θ', 'o', '0');
            InitialSetAlternates("partial", '∂', 'd');
            InitialSetAlternates("superset", Unicode.S.SUPERSET_OF, ')');
            InitialSetAlternates("fbase", '∫', 's', 'r');
            InitialSetAlternates("DIV", Result.Special.Division);
            InitialSetAlternates("INTtop", '∫', 's', 'S');
            InitialSetAlternates("INTbot", '∫');
            InitialSetAlternates("sqrt", Unicode.S.SQUARE_ROOT, 'r');
            InitialSetAlternates("Estart", 'E');
            InitialSetAlternates("Sigma", Unicode.N.N_ARY_SUMMATION, Unicode.G.GREEK_CAPITAL_LETTER_SIGMA);
            InitialSetAlternates("θ", 'θ', 'σ', 'Θ', Unicode.C.CIRCLED_MINUS);
            InitialSetAlternates("nu", 'ν', 'v');
            InitialSetAlternates("mu", 'μ', 'm', 'u');
            InitialSetAlternates("pi", 'π');
            InitialSetAlternates("Psi", 'Ψ', 'ψ', '4', 'y');
            InitialSetAlternates("varphi", /* this is the loopy phi, not GREEK_PHI_SYMBOL */Unicode.G.GREEK_SMALL_LETTER_PHI, '4', 'e', 'y', 'Ψ', 'ψ');
            InitialSetAlternates("+/-", '±', 'I', Unicode.M.MINUS_OR_PLUS_SIGN);
            // InitialSetAlternates("lambda", 'λ', 'x'); // bcz: FOR TABLETS!!
            InitialSetAlternates("lambda", 'x', 'λ'); // bcz: FOR SURFACE !!
            InitialSetAlternates("phi", Unicode.G.GREEK_PHI_SYMBOL, 'Φ', Unicode.E.EMPTY_SET, '0');
            InitialSetAlternates("rdoublearrow", Unicode.R.RIGHTWARDS_DOUBLE_ARROW);
            InitialSetAlternates("rarrow-1", Unicode.R.RIGHTWARDS_ARROW);
            InitialSetAlternates("larrow-1", Unicode.L.LEFTWARDS_ARROW);
            InitialSetAlternates("uarrow-1", Unicode.U.UPWARDS_ARROW);
            InitialSetAlternates("darrow-1", Unicode.D.DOWNWARDS_ARROW);
            InitialSetAlternates("rarrow-2", Unicode.R.RIGHTWARDS_ARROW);
            InitialSetAlternates("larrow-2", Unicode.L.LEFTWARDS_ARROW);
            InitialSetAlternates("uarrow-2", Unicode.U.UPWARDS_ARROW);
            InitialSetAlternates("darrow-2", Unicode.D.DOWNWARDS_ARROW);
            InitialSetAlternates("urarrow-1", Unicode.N.NORTH_EAST_ARROW);
            InitialSetAlternates("ularrow-1", Unicode.N.NORTH_WEST_ARROW);
            InitialSetAlternates("drarrow-1", Unicode.S.SOUTH_EAST_ARROW);
            InitialSetAlternates("dlarrow-1", Unicode.S.SOUTH_WEST_ARROW);
            InitialSetAlternates("-L", ','/*, ')'*/, Unicode.R.RIGHT_FLOOR);
            InitialSetAlternates("Delta", 'Δ', Unicode.I.INCREMENT, Unicode.W.WHITE_UP_POINTING_TRIANGLE, Unicode.W.WHITE_UP_POINTING_SMALL_TRIANGLE);
            InitialSetAlternates("nabla", Unicode.N.NABLA, Unicode.W.WHITE_DOWN_POINTING_TRIANGLE, Unicode.W.WHITE_DOWN_POINTING_SMALL_TRIANGLE);
            InitialSetAlternates("ltri", Unicode.W.WHITE_LEFT_POINTING_TRIANGLE, Unicode.W.WHITE_LEFT_POINTING_SMALL_TRIANGLE);
            InitialSetAlternates("rtri", Unicode.W.WHITE_RIGHT_POINTING_TRIANGLE, Unicode.W.WHITE_RIGHT_POINTING_SMALL_TRIANGLE);
            InitialSetAlternates("exists", Unicode.T.THERE_EXISTS);
            InitialSetAlternates("forall", Unicode.F.FOR_ALL);
            InitialSetAlternates("perp", Unicode.P.PERPENDICULAR, '+', Unicode.U.UP_TACK);
            InitialSetAlternates("memberof", Unicode.E.ELEMENT_OF, 't');
            InitialSetAlternates("bbN", Unicode.D.DOUBLE_STRUCK_CAPITAL_N);
            InitialSetAlternates("bbZ", Unicode.D.DOUBLE_STRUCK_CAPITAL_Z);
            InitialSetAlternates("bbQ", Unicode.D.DOUBLE_STRUCK_CAPITAL_Q);
            InitialSetAlternates("bbR", Unicode.D.DOUBLE_STRUCK_CAPITAL_R);
            InitialSetAlternates("bbC", Unicode.D.DOUBLE_STRUCK_CAPITAL_C);
            InitialSetAlternates("greatOrEq", Unicode.G.GREATER_THAN_OR_EQUAL_TO, Unicode.S.SUPERSET_OF_OR_EQUAL_TO);
            InitialSetAlternates("lessOrEq", Unicode.L.LESS_THAN_OR_EQUAL_TO, Unicode.S.SUBSET_OF_OR_EQUAL_TO);
            InitialSetAlternates("supersetOrEq", Unicode.S.SUPERSET_OF_OR_EQUAL_TO, Unicode.G.GREATER_THAN_OR_EQUAL_TO);
            InitialSetAlternates("subsetOrEq", Unicode.S.SUBSET_OF_OR_EQUAL_TO, Unicode.L.LESS_THAN_OR_EQUAL_TO);
            InitialSetAlternates("circledPlus", Unicode.C.CIRCLED_PLUS);
            InitialSetAlternates("circledMinus", Unicode.C.CIRCLED_MINUS, 'θ', 'Θ');
            InitialSetAlternates("circledTimes", Unicode.C.CIRCLED_TIMES);
            InitialSetAlternates("circledSlash", Unicode.C.CIRCLED_DIVISION_SLASH, Unicode.E.EMPTY_SET, Unicode.G.GREEK_PHI_SYMBOL, '0');
            InitialSetAlternates("circledBackslash", Unicode.C.CIRCLED_REVERSE_SOLIDUS, 'Q');
            InitialSetAlternates("pathIntegral", Unicode.C.CONTOUR_INTEGRAL);
            InitialSetAlternates("surfaceIntegral", Unicode.S.SURFACE_INTEGRAL);
            InitialSetAlternates("circledDot", Unicode.C.CIRCLED_DOT_OPERATOR);
            InitialSetAlternates("circledVertBar", Unicode.C.CIRCLED_VERTICAL_BAR);
            InitialSetAlternates("measuredAngle", Unicode.M.MEASURED_ANGLE, Unicode.S.SPHERICAL_ANGLE);
            InitialSetAlternates("sphericalAngle", Unicode.S.SPHERICAL_ANGLE, Unicode.M.MEASURED_ANGLE);
            InitialSetAlternates("Xi", Unicode.G.GREEK_CAPITAL_LETTER_XI, '≡');
            InitialSetAlternates("Gamma", Unicode.G.GREEK_CAPITAL_LETTER_GAMMA);
            InitialSetAlternates("not7", Unicode.N.NOT_SIGN, '7');
            InitialSetAlternates("mapsTo", Unicode.R.RIGHTWARDS_ARROW_FROM_BAR, Unicode.R.RIGHTWARDS_ARROW);
            InitialSetAlternates("assertion", Unicode.A.ASSERTION, Unicode.R.RIGHT_TACK, '+');
            InitialSetAlternates("≠", '≠');
            InitialSetAlternates("trapez=", '=');
            InitialSetAlternates("=", '=', ':', Unicode.T.TWO_DOT_LEADER, '⋰', '⋱', '⋮', '⋯');
            InitialSetAlternates(":", ':', Unicode.T.TWO_DOT_LEADER, '⋰', '⋱', '⋮', '⋯');
            InitialSetAlternates("..", Unicode.T.TWO_DOT_LEADER, ':', '⋰', '⋱', '⋯', '⋮');
            InitialSetAlternates("⋯", '⋯', '⋰', '⋱', '⋮');
            InitialSetAlternates("⋮", '⋮', '⋰', '⋱', '⋯');
            InitialSetAlternates("⋰", '⋰', '⋱', '⋮', '⋯');
            InitialSetAlternates("⋱", '⋱', '⋰', '⋮', '⋯');
            InitialSetAlternates(".", '.',':','⋱', '⋰', '⋮', '⋯');

            foreach (string w in FeaturePointDetector.Words)
                InitialSetAlternates(w, w);
        }

        public static void ExportAlternates(string path) {
            StringBuilder sb = new StringBuilder();
            System.IO.StringWriter sw = new System.IO.StringWriter(sb);

            foreach (string key in _userAllographAlternates.Keys) {
                sw.Write(key);
                foreach (Result r in _userAllographAlternates[key]) {
                    sw.Write("," + r.ToResultCode());
                }
                sw.WriteLine();
            }
            if (!Directory.Exists(Path.GetDirectoryName(path)))
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (FileStream myStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write)) {
                StreamWriter sr = new StreamWriter(myStream);
                sr.WriteLine(sb.ToString());
                sr.Flush(); sr.Close();
            }
        }

        public static void SetAlternates(string allograph, params Result[] alts) {
            if (alts.Length > 0) {
                _allographToAlternates[allograph] = alts;
                _userAllographAlternates[allograph] = alts;

                ExportAlternates(_pathToUserAllographAlternateStorage);
            }


//            // First we add/replace in the in memory preferences, then dump that to a string and property it
//            StringBuilder sb = new StringBuilder();
//            System.IO.StringWriter sw = new System.IO.StringWriter(sb);

//            foreach (string key in _userAllographAlternates.Keys) {
//                sw.Write(key);
//                foreach (Result r in _userAllographAlternates[key]) {
//                    sw.Write("," + r.ToResultCode());
//                }
//                sw.WriteLine();
//            }
//            if (_pathToUserAllographAlternateStorage == "") {
//                Properties.Settings.Default.AllographAlternateOverride = sb.ToString();
//                Properties.Settings.Default.Save();
//            } else {
////                if (!File.Exists(_pathToUserAllographAlternateStorage))
////                    File.Create(_pathToUserAllographAlternateStorage);
//                using (FileStream myStream = new FileStream(_pathToUserAllographAlternateStorage, 
//                                                            FileMode.OpenOrCreate, FileAccess.Write)) {
//                    StreamWriter sr = new StreamWriter(myStream);
//                    sr.WriteLine(sb.ToString());
//                    sr.Flush(); sr.Close();
//                }
//            }
        }
        private static void InitialSetAlternates(string allograph, params Result[] alts) {
            if (alts.Length > 0) {
                _allographToAlternates[allograph] = alts;
            }
        }
        public static void ResetAlternates() {
            InitializeAllographAlternates();
            _pathToUserAllographAlternateStorage = UserAllographFileName();
            if (File.Exists(_pathToUserAllographAlternateStorage))
                File.Delete(_pathToUserAllographAlternateStorage);
            //Properties.Settings.Default.AllographAlternateOverride = null;
            //Properties.Settings.Default.Save();
            InitializeAllographAlternates();
        }

        
        private static Dictionary<char, int> _charToBaselineIx;
        private static Dictionary<char, int> _charToXHeightIx;
        private static void SetBaseXHgtIx(char c, int bix, int xix) { _charToBaselineIx[c] = bix; _charToXHeightIx[c] = xix; }
        static Recognition() {
            //learners = new Learners();
            //learners.OccurenceToConsider = 20;

            _pathToUserAllographAlternateStorage = UserAllographFileName();
            InitializeAllographAlternates();
            
            // load from Properties.Settings.Defaults.AllographAlternateOverride and replace as needed...
            LoadAllographAlternates();

            _charToBaselineIx = new Dictionary<char, int>();
            _charToXHeightIx = new Dictionary<char, int>();
            string small = "acemnorsuvwxz.,-~αεικνοπστυω°()∞"+Unicode.D.DOT_OPERATOR;
            string low = "gpqyγημρςφχψϕ";
            string high = "bdfhikltABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789+:;<=≠>≤≥{}[]|/\\-^ΓΔΘΛΞΠΣΦΨΩδθλ≡∂"+Unicode.P.PERPENDICULAR;
            string tall = "βζj";
            foreach(char c in small)SetBaseXHgtIx(c, 0, 3);
            foreach (char c in low) SetBaseXHgtIx(c, 2, 3);
            foreach(char c in high) SetBaseXHgtIx(c, 0, 2);
            foreach(char c in tall) SetBaseXHgtIx(c, 1, 2);
            SetBaseXHgtIx(Unicode.I.INTEGRAL, -1, -1);
            SetBaseXHgtIx(Unicode.C.CONTOUR_INTEGRAL, -1, -1);
            SetBaseXHgtIx(Unicode.S.SQUARE_ROOT, 0, 3);
            SetBaseXHgtIx(Unicode.S.SET_MINUS, 0, 3);
            SetBaseXHgtIx(Unicode.N.N_ARY_SUMMATION, 0, 2);
            SetBaseXHgtIx(Unicode.N.N_ARY_PRODUCT, 0, 2);
            SetBaseXHgtIx(Unicode.P.PLUS_MINUS_SIGN, 0, 3);
            SetBaseXHgtIx(Unicode.M.MINUS_OR_PLUS_SIGN, 0, 3);
            SetBaseXHgtIx(Unicode.D.DIVISION_SLASH, 0, 3);
            SetBaseXHgtIx(Unicode.M.MINUS_SIGN, 0, 3);
            SetBaseXHgtIx(Unicode.S.SUPERSET_OF, 0, 3);
            SetBaseXHgtIx(Unicode.S.SUBSET_OF, 0, 3);
            SetBaseXHgtIx(Unicode.I.INTERSECTION, 0, 2);
            SetBaseXHgtIx(Unicode.N.N_ARY_INTERSECTION, 0, 2);
            SetBaseXHgtIx(Unicode.U.UNION, 0, 2);
            SetBaseXHgtIx(Unicode.N.N_ARY_UNION, 0, 2);
            SetBaseXHgtIx(Unicode.R.RIGHTWARDS_DOUBLE_ARROW, 0, 3);
            SetBaseXHgtIx(Unicode.R.RIGHTWARDS_ARROW, 0, 2);
            SetBaseXHgtIx(Unicode.L.LEFTWARDS_ARROW, 0, 2);
            SetBaseXHgtIx(Unicode.U.UPWARDS_ARROW, 0, 2);
            SetBaseXHgtIx(Unicode.D.DOWNWARDS_ARROW, 0, 2);
            SetBaseXHgtIx(Unicode.N.NORTH_EAST_ARROW, 0, 2);
            SetBaseXHgtIx(Unicode.N.NORTH_WEST_ARROW, 0, 2);
            SetBaseXHgtIx(Unicode.S.SOUTH_EAST_ARROW, 0, 2);
            SetBaseXHgtIx(Unicode.S.SOUTH_WEST_ARROW, 0, 2);
            SetBaseXHgtIx(Unicode.D.DIVIDES, 0, 3);
            SetBaseXHgtIx(Unicode.T.TILDE_OPERATOR, 0, 3);
            SetBaseXHgtIx(Unicode.I.INCREMENT, 0, 2);
            SetBaseXHgtIx(Unicode.W.WHITE_RIGHT_POINTING_TRIANGLE, 0, 2);
            SetBaseXHgtIx(Unicode.W.WHITE_RIGHT_POINTING_SMALL_TRIANGLE, 0, 2);
            SetBaseXHgtIx(Unicode.W.WHITE_LEFT_POINTING_TRIANGLE, 0, 2);
            SetBaseXHgtIx(Unicode.W.WHITE_LEFT_POINTING_SMALL_TRIANGLE, 0, 2);
            SetBaseXHgtIx(Unicode.W.WHITE_UP_POINTING_TRIANGLE, 0, 2);
            SetBaseXHgtIx(Unicode.W.WHITE_UP_POINTING_SMALL_TRIANGLE, 0, 2);
            SetBaseXHgtIx(Unicode.W.WHITE_DOWN_POINTING_TRIANGLE, 0, 2);
            SetBaseXHgtIx(Unicode.W.WHITE_DOWN_POINTING_SMALL_TRIANGLE, 0, 2);
            SetBaseXHgtIx(Unicode.T.THERE_EXISTS, 0, 2);
            SetBaseXHgtIx(Unicode.F.FOR_ALL, 0, 2);
            SetBaseXHgtIx(Unicode.E.ELEMENT_OF, 0, 3);
            SetBaseXHgtIx(Unicode.L.LOGICAL_AND, 0, 3);
            SetBaseXHgtIx(Unicode.L.LOGICAL_OR, 0, 3);
            SetBaseXHgtIx(Unicode.N.NOT_SIGN, 0, 3);
            SetBaseXHgtIx(Unicode.D.DOUBLE_STRUCK_CAPITAL_N, 0, 2);
            SetBaseXHgtIx(Unicode.D.DOUBLE_STRUCK_CAPITAL_Z, 0, 2);
            SetBaseXHgtIx(Unicode.D.DOUBLE_STRUCK_CAPITAL_Q, 0, 2);
            SetBaseXHgtIx(Unicode.D.DOUBLE_STRUCK_CAPITAL_R, 0, 2);
            SetBaseXHgtIx(Unicode.D.DOUBLE_STRUCK_CAPITAL_C, 0, 2);
            SetBaseXHgtIx(Unicode.R.RIGHTWARDS_HARPOON_WITH_BARB_UPWARDS, 0, 3);
            SetBaseXHgtIx(Unicode.A.ASYMPTOTICALLY_EQUAL_TO, 0, 2);
            SetBaseXHgtIx(Unicode.A.APPROXIMATELY_EQUAL_TO, 0, 2);
            SetBaseXHgtIx(Unicode.A.ALMOST_EQUAL_TO, 0, 2);
            SetBaseXHgtIx(Unicode.S.SUPERSET_OF_OR_EQUAL_TO, 0, 2);
            SetBaseXHgtIx(Unicode.S.SUBSET_OF_OR_EQUAL_TO, 0, 2);
            SetBaseXHgtIx(Unicode.C.CIRCLED_MINUS, 0, 2);
            SetBaseXHgtIx(Unicode.C.CIRCLED_TIMES, 0, 2);
            SetBaseXHgtIx(Unicode.C.CIRCLED_DIVISION_SLASH, 0, 2);
            SetBaseXHgtIx(Unicode.C.CIRCLED_REVERSE_SOLIDUS, 0, 2);
            SetBaseXHgtIx(Unicode.C.CIRCLED_DOT_OPERATOR, 0, 2);
            SetBaseXHgtIx(Unicode.C.CIRCLED_VERTICAL_BAR, 0, 2);
            SetBaseXHgtIx(Unicode.C.CIRCLED_PLUS, 0, 2);
            SetBaseXHgtIx(Unicode.L.LEFT_FLOOR, 0, 2);
            SetBaseXHgtIx(Unicode.R.RIGHT_FLOOR, 0, 2);
            SetBaseXHgtIx(Unicode.L.LEFT_CEILING, 0, 2);
            SetBaseXHgtIx(Unicode.R.RIGHT_CEILING, 0, 2);
            SetBaseXHgtIx(Unicode.D.DOUBLE_VERTICAL_LINE, 0, 2);
            SetBaseXHgtIx(Unicode.P.PARALLEL_TO, 0, 2);
        }

        private void SetMetrics(Result r, int baseline, int midpt, ref int baselineout, ref int xheightout) {
            if(r == alts[0] && msftRecoged) {
                // Believe microsoft. This is needed for someone drawing a script f which isn't recognized by us.
                // Eventually both the roman and (new) script f recognizers should set baseline and midpt so f can be
                // treated as a tall letter uniformly.
                baselineout = baseline;
                xheightout = midpt;
                return;
            }
            if(r.Word != null) {
                baselineout = baseline;
                xheightout = midpt;
            } else if(r.Other != Result.Special.None) {
                if (strokes != null) {
                    baselineout = strokes.GetBoundingBox().Bottom;
                    xheightout = strokes.GetBoundingBox().Top;
                }
            } else if (strokes != null){
                int[] metrics = new int[] { strokes.GetBoundingBox().Bottom, baseline, midpt, strokes.GetBoundingBox().Top };
                if(!_charToBaselineIx.ContainsKey(r.Character) || _charToBaselineIx[r.Character] == -1) {
                    int xht = (strokes.GetBoundingBox().Bottom - strokes.GetBoundingBox().Top)/3;
                    baselineout = strokes.GetBoundingBox().Bottom - xht;
                    xheightout = strokes.GetBoundingBox().Top + xht;
                } else {
                    baselineout = metrics[_charToBaselineIx[r.Character]];
                    xheightout = metrics[_charToXHeightIx[r.Character]];
                }
            }
        }

        public class Result {
            public enum Special {
                None, Division, Imaginary, NatLogBase
            }
            static public char ToChar(Special s) {
                switch (s) {
                    case Special.Imaginary:
                        return 'i';// Unicode.G.GREEK_SMALL_LETTER_IOTA_WITH_TONOS;
                    case Special.NatLogBase :
                        return 'e';// Unicode.S.SCRIPT_SMALL_E;
                    case Special.Division :
                        return Unicode.S.SOLIDUS;
                }
                return (char)0;
            }
            private char         _character; public char         Character { get { return _character; } }
            private string       _word;      public string       Word      { get { return _word; } }
            private Special      _other;     public Special      Other     { get { return _other; } }
            private object _tag = null; public object Tag { get { return _tag; } }

            internal Result() {
                _character = (char)0;
                _word = null;
                _other = Special.None;
            }
            public Result(char c) {
                Trace.Assert(c != (char)0);
                _character = c;
                _word = null;
                _other = Special.None;
            }
            public Result(string w) {
                Trace.Assert(w != null);
                _character = (char)0;
                _word = w;
                _other = Special.None;
            }
            /// <summary>
            /// tag should not be internally modified after constructing a Result! I really want C++ "const" semantics here.
            /// </summary>
            public Result(string w, object tag)
                : this(w) {
                _tag = tag;
            }
            public Result(Special o) {
                Trace.Assert(o != Special.None);
                _character = (char)0;
                _word = null;
                _other = o;
            }
            public Result(Result r) {
                _character = r._character;
                _word = r._word;
                _other = r._other;
            }
            public static implicit operator Result(char c) { return new Result(c); }
            public static implicit operator Result(string w) { return new Result(w); }
            public static implicit operator Result(Special o) { return new Result(o); }

            public static bool operator==(Result r, char c) {
                return c == (char)0 ? (r.Character == (char)0 && r.Word == null && r.Other == Special.None)
                    : (r.Character == c);
            }
            public static bool operator!=(Result r, char c) { return !(r == c); }
            public static bool operator==(Result r, string w) {
                return w == null ? (r.Character == (char)0 && r.Word == null && r.Other == Special.None)
                    : (r.Word == w);
            }
            public static bool operator!=(Result r, string w) { return !(r == w); }
            public static bool operator==(Result r, Special o) {
                return o == Special.None ? (r.Character == (char)0 && r.Word == null && r.Other == Special.None)
                    : (r.Other == o);
            }
            public static bool operator!=(Result r, Special o) { return !(r == o); }

            public static bool operator==(Result r, Result r2) {
                return (r == null && r2 == null) ||
                    (r != null && r2 != null && r.Character == r2.Character && r.Word == r2.Word && r.Other == r2.Other && r.Tag == r2.Tag);
            }
            public static bool operator!=(Result r, Result r2) { return !(r == r2); }

            public override bool Equals(object obj) {
                Result r2 = obj as Result;
                if(r2 == null) return false;
                return this == r2;
            }
            public override int GetHashCode() {
                return Character.GetHashCode() 
                    ^ (Word == null ? 1 : Word.GetHashCode()) 
                    ^ Other.GetHashCode() 
                    ^ (Tag == null ? 0 : Tag.GetHashCode());
                //return Character.GetHashCode() ^ Word.GetHashCode() ^ Other.GetHashCode() ^ (Tag == null ? 0 : Tag.GetHashCode());
            }
            public override string ToString() {
                return "{" + Label() + "}";
            }

            public string Label() {
                string label;
                if (Character != (char)0) label = Character.ToString() + " (" + Unicode.NameOf(Character) + ")";
                else if (Word != null) label = Word;
                else if (Other == Special.Division) label = ToChar(Other).ToString() + " Division line";
                else if (Other == Special.Imaginary) label = ToChar(Other).ToString() + " Imaginary Number";
                else if (Other == Special.NatLogBase) label = ToChar(Other).ToString() + " Natural Log Base";
                else label = "null?";
                if (Tag != null) label += " " + Tag.ToString();
                return label;
            }
            public string ToResultCode() {
                string label;
                if (Character == ',') label = "s:c"; // because we'll be using , to delimit later...
                else if (Character != (char)0) label = "c:" + Character.ToString();
                else if (Word != null) label = "w:" + Word;
                else if (Other == Special.Division) label = "s:d";
                else if (Other == Special.Imaginary) label = "s:i";
                else if (Other == Special.NatLogBase) label = "s:e";
                else label = "null?";
                if (Tag != null) label += " " + Tag.ToString();
                return label;
            }
            public void SetFromResultCode(string res) {
                if (res.Length < 3) return;
                else if (res[0] == 'c') _character = res[2];
                else if (res[0] == 'w') _word = res.Substring(2);
                else if (res[0] == 's') {
                    if (res[2] == 'd') _other = Special.Division;
                    else if (res[2] == 'i') _other = Special.Imaginary;
                    else if (res[2] == 'e') _other = Special.NatLogBase;
                    else if (res[2] == 'c') _character = ',';
                }
            }
        }
    }
    public class RecogGuidEqual : IEqualityComparer<Recognition> {
        public bool Equals(Recognition x, Recognition y) {
            return x.guid.Equals(y.guid);
        }

        public int GetHashCode(Recognition obj) {
            return obj.guid.GetHashCode();
        }
    }

}
