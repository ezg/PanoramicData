using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using starPadSDK.MathExpr;
using starPadSDK.UnicodeNs;

namespace ExprScaffold {
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window {
        public Window1() {
            InitializeComponent();

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

            _exprLog.FontFamily = new FontFamily("Times New Roman,Arial Unicode MS,Lucida Sans Unicode");

            _exprEntry.Focus();
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
        void MenuItem_Click(object sender, RoutedEventArgs e) {
            Application.Current.Shutdown();
        }

        private void _exprEntry_KeyDown(object sender, KeyEventArgs e) {
            if(e.Key == Key.Enter) {
                if(e.KeyboardDevice.Modifiers == ModifierKeys.Control) {
                    /* control-return; echo TeX and MathML formf */
                    _exprLog.AppendText(_exprEntry.Text + "\n");
                    try {
                        Expr ex = Text.Convert(_exprEntry.Text);
                        string s = (new TeX()).Compose(ex);
                        _exprLog.AppendText(" -> " + s + "\n");
                        s = (new MathML()).Convert(ex).OuterXml;
                        _exprLog.AppendText(" -> " + s + "\n");
                    } catch(TextParseException exc) {
                        MessageBox.Show(((char)Unicode.W.WARNING_SIGN).ToString() + " " + exc.Message, "Parse error");
                    }
                    _exprEntry.Clear();
                    _exprLog.ScrollToEnd();

                    e.Handled = true;
                } else {
                    /* regular return; take as expression to be computed */
                    string s;
                    _exprLog.AppendText(_exprEntry.Text + "\n");
                    try {
                        Expr ex = Text.Convert(_exprEntry.Text);
                        try {
                            s = Text.Convert(ex);
                            _exprLog.AppendText(" =(plain) " + s + "\n");
                        } catch(Exception exn) {
                            _exprLog.AppendText(" =(plain) Text.Convert FAILED: " + exn.Message);
                        }
                        try {
                            s = Text.InputConvert(ex);
                            _exprLog.AppendText(" =(pretty) " + s + "\n");
                        } catch(Exception exn) {
                            _exprLog.AppendText(" =(pretty) Text.InputConvert FAILED: " + exn.Message);
                        }
                        try {
                            ex = Engine.Simplify(ex);
                            s = Text.InputConvert(ex);
                            _exprLog.AppendText(((char)Unicode.R.RIGHTWARDS_ARROW_FROM_BAR).ToString() + " " + s + "\n");
                        } catch(Exception exn) {
                            _exprLog.AppendText(((char)Unicode.R.RIGHTWARDS_ARROW_FROM_BAR).ToString() + " Simplify/IC FAILED: " + exn.Message);
                        }
                        try {
                            ex = Engine.Approximate(ex);
                            s = Text.InputConvert(ex);
                            _exprLog.AppendText(((char)Unicode.R.RIGHTWARDS_ARROW).ToString() + " " + s + "\n");
                        } catch(Exception exn) {
                            _exprLog.AppendText(((char)Unicode.R.RIGHTWARDS_ARROW).ToString() + " Approximate/IC FAILED: " + exn.Message);
                        }
                    } catch(TextParseException exc) {
                        _exprLog.AppendText(((char)Unicode.W.WARNING_SIGN).ToString() + " " + exc.Message + "\n");
                    }
                    _exprEntry.Clear();
                    _exprLog.ScrollToEnd();

                    e.Handled = true;
                }
            }
        }
    }
}
