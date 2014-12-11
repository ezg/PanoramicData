using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;
using System.Windows.Shapes;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq;
using starPadSDK.WPFHelp;
using starPadSDK.MathExpr.ExprWPF;
using starPadSDK.MathExpr.ExprWPF.Boxes;
using starPadSDK.MathExpr;
using starPadSDK.MathRecognizer;
using starPadSDK.CharRecognizer;
using starPadSDK.UnicodeNs;

namespace starPadSDK.AppLib {
    /// <summary>
    /// This is Control that recognizes mathematical input
    /// </summary>
    public class NumberInputBox : InqScene {
        double _number     = double.NaN;
        bool   _autoUpdate = false;
        void updateNumber() {
            foreach (Parser.Range range in (this.Commands.Editors[0] as MathEditor).RecognizedMath.Ranges) {
                bool negate = range.Parse.expr.Head() == WellKnownSym.minus;
                Expr expr = range.Parse.expr;
                if (negate)
                    expr = expr.Args()[0];
                if (expr is DoubleNumber || expr is IntegerNumber)
                    Number = negate ? -(double)expr : (double)expr;
            }
        }

        public double Number { get { return _number; } set { _number = value; if (NumberEnteredEvent != null) NumberEnteredEvent(null, null); } }
        public event EventHandler NumberEnteredEvent;
        public class ACommandSet : CommandSet {
            /// <summary>
            /// initializes the gestures that are active in the Control
            /// </summary>
            protected override void InitGestures() {
                base.InitGestures();  // cleans out all current Gestures
                _gest.Add(new ScribbleTapCommand(_can, true, true, false));
            }
            public ACommandSet(InqScene scene) : base(scene) {
                MathEditor = new MathEditor(_can, false);
                Editors    = new List<CommandEditor>(new CommandEditor[] { MathEditor });
            }
            public MathEditor MathEditor { get; set; }
        }

        public NumberInputBox(bool autoUpdate) {
            _autoUpdate = autoUpdate;
            if (!_autoUpdate) {
                Button okay = new Button();
                okay.Content = "OK";
                okay.PreviewStylusDown += new StylusDownEventHandler((object sender, StylusDownEventArgs e) => e.Handled = true);
                okay.Click += new RoutedEventHandler((object sender, RoutedEventArgs e) => {
                    e.Handled = true;
                    updateNumber();
                });
                this.SceneLayer.Children.Add(okay);
            }
            else
                (this.Commands as ACommandSet).MathEditor.MathChangedEvent += new MathEditor.MathChangedHandler((object obj, MathRecognition mrec, Recognition rec) => updateNumber());
        }
        public NumberInputBox():this(false) { }
        override protected void init() {
            // initializes the default feedback widget for when objects are selected.
            base.init();

            // install the gestures for this APage
            Commands = new ACommandSet(this);
        }
    }
}
