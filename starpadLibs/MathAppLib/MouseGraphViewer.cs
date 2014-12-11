using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using starPadSDK.WPFHelp;
using starPadSDK.MathExpr;
using starPadSDK.MathRecognizer;


namespace starPadSDK.AppLib
{
    public class MouseGraphViewer : MathEditor.GraphViewerUI
    {
        public MouseGraphViewer(MathEditor medit):base(medit) { }

        protected override void mathDropped(Parser.Range range, FrameworkElement ele) {
            if (ele is FunctionPlot ) {
                if (range.Parse != null)
                    (ele as FunctionPlot).AddFunc(range);
                _medit.Scene.UndoRedo.Undo();
            }
        }
        override protected void displayFunctionPlot(IEnumerable<Parser.Range> funcs, Rct where) {
            FunctionPlot plotter = new FunctionPlot(funcs);
            plotter.RenderTransform = new TranslateTransform(where.TopLeft.X, where.TopLeft.Y);
            plotter.Width = where.Width;
            plotter.Height = where.Height;
            _medit.Scene.AddNoUndo(plotter);
        }

        override protected void displayFunctionPlot(IEnumerable<Parser.Range> funcs, Pt where)
        {
            FunctionPlot plotter = new FunctionPlot(funcs);
            _medit.Scene.AddNoUndo(plotter.MaxRangeBox);
            _medit.Scene.AddNoUndo(plotter.MinRangeBox);
            plotter.RenderTransform = new TranslateTransform(where.X, where.Y);
            _medit.Scene.AddNoUndo(plotter);
        }
    }
}
