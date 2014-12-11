using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Microsoft.Ink;
using starPadSDK.MathRecognizer;
using starPadSDK.CharRecognizer;
using starPadSDK.UnicodeNs;
using starPadSDK.Utils;

namespace starPadSDK.MathUI {
    public class InkColorizer {
        /* colorize ink. Ideally we would have kept track of which ink strokes had changes and only update colorization in those ranges affected
         * by the changes. */
        /// <summary>
        /// This colorizes the ink from scratch, but state is maintained across all of the calls.
        /// </summary>
        public void Colorize(MathRecognizer.Line math, Strokes strokes, MathRecognition mrec) {
            HashSet<Recognition> recs = new HashSet<Recognition>(new RecogGuidEqual());
            foreach(var s in strokes) {
                Recognition r = mrec.Charreco.Classification(s);
                if(r != null) recs.Add(r);
            }
            foreach(var r in recs) {
                Symbol sym = FindSymbol(math, r);
                if(sym == null) continue;
                string npath = FindPath(math, sym.r);
                string opath;
                if(!_oldPaths.TryGetValue(sym.r.strokes[0].Id, out opath) || opath != npath) {
                    ComputePaintColors(math, sym.r);
                }
                _oldPaths[sym.r.strokes[0].Id] = npath;
            }
            // paint expression using current color keys
            recs.Clear();
            foreach(var s in strokes) {
                if(!s.Deleted) {
                    Recognition r = mrec.Charreco.Classification(s);
                    if(r != null) recs.Add(r);
                }
            }
            foreach(var r in recs) {
                string path = FindPath(math, r);
                foreach(var s in r.strokes) {
                    if(s.Deleted) continue;
                    colTime ct;
                    if(path == "") {
                        mrec.Sim[s].BackingStroke.DrawingAttributes.Color = Colors.Yellow;
                    } else if(_recent.TryGetValue(path, out ct)) {
                        mrec.Sim[s].BackingStroke.DrawingAttributes.Color = ct.col;
                    } else {
                        mrec.Sim[s].BackingStroke.DrawingAttributes.Color = Colors.Black;
                    }
                }
            }
        }
        
        private class colTime {
            public static int timestamp = 0;
            public Color col;
            public int time;
            public colTime(Color c, int t) { col = c; time = t; }
        }
        private Dictionary<string, colTime> _recent = new Dictionary<string, colTime>();
        private List<Color> _colors = new List<Color>(new[] { Colors.Orange, Colors.Green, Colors.Brown, Colors.Red, Colors.Blue });
        private Dictionary<int, string> _oldPaths = new Dictionary<int, string>();
        private void ComputePaintColors(starPadSDK.MathRecognizer.Line root, Recognition update) {
            string path = FindPath(root, update);
            if(path == "" || path == "X") return;
            colTime ct;
            if(_recent.TryGetValue(path, out ct)) { // refresh the time for this path
                _recent[path] = new colTime(ct.col, ++colTime.timestamp);
                return;
            }
            // if there are no colors left, recycle the color assigned to the LRU path
            if(_colors.Count == 0) {
                //var oldest = _recent.Aggregate(new Pair<int, string>(colTime.timestamp, null),
                //    (min, old) => old.Value.time <= min.First ? new Pair<int, string>(old.Value.time, old.Key) : min);
                var oldest = _recent.Min(colTime.timestamp, (kvp) => kvp.Value.time);
                if(oldest.Key != null) {
                    _colors.Add(oldest.Value.col);
                    _recent.Remove(oldest.Key);
                }
            }
            // choose a color for this path
            _recent.Add(path, new colTime(_colors[_colors.Count - 1], ++colTime.timestamp));
            _colors.RemoveAt(_colors.Count - 1);
        }

        private string FindPath(starPadSDK.MathRecognizer.Line l, Recognition r) {
            Symbol parent = null, lineParent = null;
            return FindPath(l, r, ref parent, ref lineParent, new List<Recognition>());
        }

        private string FindPath(starPadSDK.MathRecognizer.Line l, Recognition r, ref Symbol refParent, ref Symbol lineParent, List<Recognition> pending) {
            foreach(Symbol s in l._syms) {
                if(s.r.guid == r.guid) {
                    bool foundit = false;
                    for(int i = l._syms.IndexOf(s) - 1; i >= 0; i--)
                        if("+=><()".IndexOf(l._syms[i].Sym.Character) == -1 && l._syms[i].Sym.Character != Unicode.M.MINUS_SIGN) {
                            lineParent = l._syms[i];
                            foundit = true;
                            break;
                        }
                    if(!foundit) {
                        for(int i = l._syms.IndexOf(s) + 1; i < l._syms.Count; i++)
                            if(!pending.Contains(l._syms[i].r) && "+=><()".IndexOf(l._syms[i].Sym.Character) == -1 && l._syms[i].Sym.Character != Unicode.M.MINUS_SIGN) {
                                lineParent = l._syms[i];
                                foundit = true;
                                break;
                            }
                    }
                    return "X";
                } else {
                    Symbol tmpParent = s, tmpLineParent = null;
                    string path1 = FindPath(s.Super, r, ref tmpParent, ref tmpLineParent, pending);
                    if(path1 != "") {
                        refParent = tmpParent;
                        lineParent = tmpLineParent;
                        return (s is RootSym ? "R" : (s is DivSym ? "D" : "P")) + path1;
                    }
                    string path2 = FindPath(s.Sub, r, ref tmpParent, ref tmpLineParent, pending);
                    if(path2 != "") {
                        refParent = tmpParent;
                        lineParent = tmpLineParent;
                        return (s is DivSym ? "V" : (s is RootSym ? "" : "B")) + path2;
                    }
                    if(s is IntSym) {
                        tmpParent = null;

                        string path3 = FindPath(((IntSym)s).Integrand, r, ref tmpParent, ref tmpLineParent, pending);
                        if(path3 != "") {
                            refParent = (s.r.alt == Unicode.I.INTEGRAL ? tmpParent : refParent);
                            lineParent = path3 == "X" && tmpLineParent == null ? s : tmpLineParent;
                            if(path3[0] == 'P' || path3[0] == 'B') return "I" + path3;
                            else return path3;
                        }
                    }
                    if(s is ParenSym) {
                        tmpParent = null;
                        string path3 = "";
                        foreach(starPadSDK.MathRecognizer.Line ll in ((ParenSym)s).lines) {
                            path3 = FindPath(ll, r, ref tmpParent, ref tmpLineParent, pending);
                            if(path3 != "") {
                                lineParent = path3 == "X" && tmpLineParent == null ? s : tmpLineParent;
                                return path3;
                            }
                        }
                        if(((ParenSym)s).Closing != null && ((ParenSym)s).Closing.r.guid == r.guid) {
                            lineParent = s;
                            return "X";
                        }
                    }
                }
            }
            return "";
        }

        private Symbol FindSymbol(starPadSDK.MathRecognizer.Line l, Recognition r) {
            foreach(Symbol s in l._syms) {
                if(s.r.guid == r.guid) {
                    return s;
                } else {
                    Symbol l1 = FindSymbol(s.Super, r);
                    if(l1 != null)
                        return l1;
                    Symbol l2 = FindSymbol(s.Sub, r);
                    if(l2 != null)
                        return l2;
                    if(s is IntSym) {
                        Symbol l3 = FindSymbol(((IntSym)s).Integrand, r);
                        if(l3 != null)
                            return l3;
                    }
                    if(s is ParenSym) {
                        foreach(starPadSDK.MathRecognizer.Line ll in ((ParenSym)s).lines) {
                            Symbol l3 = FindSymbol(ll, r);
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

        public void Reset() {
            _recent.Clear();
            _oldPaths.Clear();
            _colors = new List<Color>(new[] { Colors.Orange, Colors.Green, Colors.Brown, Colors.Red, Colors.Blue });
        }
    }
}