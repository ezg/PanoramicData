using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using starPadSDK.Inq;
using starPadSDK.MathRecognizer;
using starPadSDK.CharRecognizer;
using starPadSDK.UnicodeNs;
using starPadSDK.Utils;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Ink;

namespace starPadSDK.MathUI {
    public class AlternatesMenuCreator {
        public AlternatesMenuCreator(ToolBar menushell, MathRecognition mrec) {
            _menuShell = menushell;
            _mrec = mrec;
        }
        private MathRecognition _mrec;
        private ToolBar _menuShell;
        public void Populate(ICollection<Recognition> recogs, StroqCollection stroqs) {
            alternateRec = recogs.Count == 1 ? recogs.Single() : null;
            if(alternateRec != null && !stroqs.All((s) => alternateRec.strokes.Contains(_mrec.Sim[s]))) alternateRec = null;
            altstroqsRec = stroqs;
            altrecogsRec = recogs;

            _menuShell.Items.Clear();

            MenuItem mi;
            bool needseparator = false;
            if(recogs.Count == 1) {
                /* regular alternates*/
                Recognition rr = recogs.Single();
                for(int i = 0; i < rr.alts.Length; i++) {
                    string label;
                    char c = rr.alts[i].Character;
                    if(c != 0)
                        label = c.ToString();
                    else {
                        label = rr.alts[i].Word;
                        if(label == null) {
                            label = rr.alts[i].ToString();
                            label = label.Substring(1, label.Length - 2);
                        }
                    }
                    mi = new MenuItem();
                    mi.Header = label;
                    if(c != 0) {
                        mi.ToolTip = Unicode.NameOf(c);
                        mi.FontFamily = new FontFamily(starPadSDK.MathExpr.ExprWPF.EDrawingContext.FontFamilyURIBase,
                            starPadSDK.MathExpr.ExprWPF.EDrawingContext.FontFamilyURIRel);
                        if((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= 'α' && c <= 'ω') || c == Unicode.G.GREEK_PHI_SYMBOL) mi.FontStyle = FontStyles.Italic;
                    } else {
                        mi.ToolTip = label;
                    }
                    mi.Tag = i;
                    mi.Click += ChooseAlternate;
                    mi.PreviewMouseDown += mi_MouseDown;
                    mi.PreviewMouseUp += mi_MouseUp;
                    if(i == 0) {
                        mi.AllowDrop = true;
                        mi.DragEnter += mi_DragCheck;
                        mi.DragOver += mi_DragCheck;
                        mi.Drop += mi_Drop;
                    }
                    if(rr.curalt == i) mi.IsChecked = true;
                    _menuShell.Items.Add(mi);
                }
                needseparator = true;
            }
            if(stroqs.Count > 1 && stroqs.Count != recogs.Count /* FIXME: if a stroke has no recog, this won't work */) {
                /* option to split apart and recognize each stroke separately */
                if(needseparator) {
                    _menuShell.Items.Add(new Separator());
                    needseparator = false;
                }
                string label = "";
                foreach(Stroq s1 in stroqs) {
                    Recognition r = _mrec.Charreco.Classify(_mrec.Sim[s1], true);
                    if(r == null)
                        continue;
                    string l;
                    char c = r.alt.Character;
                    if(c != 0)
                        l = c.ToString();
                    else {
                        l = r.alt.Word;
                        if(l == null) {
                            l = r.alt.ToString();
                            l = l.Substring(1, l.Length - 2);
                        }
                    }
                    label += l;
                }
                mi = new MenuItem();
                mi.Header = label;
                mi.ToolTip = "split combined symbol into separate symbols";
                mi.Tag = -1;
                mi.Click += ChooseAlternate;
                _menuShell.Items.Add(mi);
                needseparator = true;
            }
            if(stroqs.Count > 0) {
                /* Interpret everything as a single word */
                if(needseparator) {
                    _menuShell.Items.Add(new Separator());
                    needseparator = false;
                }
                InkAnalyzer ia = new InkAnalyzer();
                AnalysisHintNode ahn = ia.CreateAnalysisHint();
                ahn.WordMode = true;
                ahn.Location.MakeInfinite();
                foreach(Stroq s in stroqs) ia.AddStroke(s.BackingStroke);
                AnalysisStatus stat = ia.Analyze();
                if(stat.Successful) {
                    AnalysisAlternateCollection aac = ia.GetAlternates();
                    for(int i = 0; i < aac.Count; i++) {
                        if(aac[i].AlternateNodes.Count > 1 || !(aac[i].AlternateNodes[0] is InkWordNode)) continue;
                        mi = new MenuItem();
                        mi.Header = aac[i].RecognizedString;
                        mi.ToolTip = "interpret all selected strokes as a single character or word: alternate " + (i + 1);
                        mi.Tag = aac[i];
                        mi.Click += ChooseAlternate;
                        if(alternateRec != null) {
                            mi.PreviewMouseDown += mi_MouseDown;
                            mi.PreviewMouseUp += mi_MouseUp;
                        }
                        if(aac[i].RecognizedString.Length == 1) {
                            char c = aac[i].RecognizedString[0];
                            if((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= 'α' && c <= 'ω')) mi.FontStyle = FontStyles.Italic;
                            mi.ToolTip = (string)mi.ToolTip + " (" + Unicode.NameOf(c) + ")";
                        }
                        _menuShell.Items.Add(mi);
                    }
                }
            }
            _menuShell.InvalidateMeasure(); // odd that I need this
        }
        public void Clear() {
            alternateRec = null;
            altstroqsRec = null;
            altrecogsRec = null;
            _menuShell.Items.Clear();
            _menuShell.InvalidateMeasure(); // odd that I need this [is it needed even here?]
        }

        private StroqCollection altstroqsRec;
        private Recognition alternateRec;
        private ICollection<Recognition> altrecogsRec;

        private void ChooseAlternate(object sender, RoutedEventArgs e) {
            MenuItem mi = (MenuItem)e.Source;
            if(mi.Tag is AnalysisAlternate) {
                /* join into word */
                _mrec.ChooseWord(alternateRec, altstroqsRec, (AnalysisAlternate)mi.Tag);
            } else if((int)mi.Tag == -1) {
                /* split apart */
                //XXX FIXME sometimes does not work--wrote 4, break apart from menu, write -3 after, vert stroke missing from everything
                foreach(Recognition r in altrecogsRec) _mrec.Charreco.ClearRecogs(r.strokes);
                foreach(Stroq s1 in altstroqsRec) {
                    Recognition r = _mrec.Charreco.FullClassify(_mrec.Sim[s1], true);
                    if(r != null)
                        r.levelsetby = int.MaxValue;
                }
                _mrec.AlternateChanged(altstroqsRec);
            } else {
                /* just pick a regular alternate */
                alternateRec.curalt = (int)mi.Tag;
                alternateRec.levelsetby = 0;
                _mrec.AlternateChanged(altstroqsRec);
            }
            foreach(var i in _menuShell.Items) {
                MenuItem imi = i as MenuItem;
                if(imi != null) imi.IsChecked = false;
            }
            mi.IsChecked = true;
        }

        private void mi_MouseUp(object sender, MouseButtonEventArgs e) {
            ((MenuItem)sender).MouseLeave -= mi_MouseLeave;
        }
        private void mi_MouseDown(object sender, MouseButtonEventArgs e) {
            ((MenuItem)sender).MouseLeave += mi_MouseLeave;
        }
        private void mi_MouseLeave(object sender, MouseEventArgs e) {
            MenuItem mi = (MenuItem)sender;
            mi.MouseLeave -= mi_MouseLeave;
            DragDrop.DoDragDrop(mi, mi.Tag, DragDropEffects.Move);
        }
        private void mi_DragCheck(object sender, DragEventArgs e) {
            if(alternateRec != null && (e.Data.GetDataPresent(typeof(AnalysisAlternate)) || e.Data.GetDataPresent(typeof(int)))) {
                e.Effects = DragDropEffects.Move & e.AllowedEffects;
            } else {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }
        private void mi_Drop(object sender, DragEventArgs e) {
            if(e.Data.GetDataPresent(typeof(AnalysisAlternate))) {
                AnalysisAlternate aa = (AnalysisAlternate)e.Data.GetData(typeof(AnalysisAlternate));
                /* join into word */
                _mrec.ChooseWordPersistently(altstroqsRec, alternateRec, aa);
            } else {
                /* just pick a regular alternate */
                _mrec.ChangeAlternatePersistently(altstroqsRec, alternateRec, (int)e.Data.GetData(typeof(int)));
            }
            Populate(altrecogsRec, altstroqsRec);
        }
    }
}
