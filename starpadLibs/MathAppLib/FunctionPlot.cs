using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using starPadSDK.Inq.MSInkCompat;
using System.Windows.Media;
using System.Windows.Shapes;
using starPadSDK.Inq.BobsCusps;
using starPadSDK.WPFHelp;
using System.IO;
using starPadSDK.MathExpr.ExprWPF;
using starPadSDK.MathExpr.ExprWPF.Boxes;
using starPadSDK.MathExpr;
using starPadSDK.MathRecognizer;
using starPadSDK.CharRecognizer;
using starPadSDK.UnicodeNs;
using Constant = starPadSDK.MathExpr.MathConstant;
using Line = System.Windows.Shapes.Line;
using Microsoft.Research.DynamicDataDisplay.Charts;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.PointMarkers;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using System.Windows.Documents;

namespace starPadSDK.AppLib
{
    public class FunctionPlot : ChartPlotter {
        protected List<CanonicalFuncPlot> _funcs = new List<CanonicalFuncPlot>();
        protected int                      penIndex = 0;
        protected Pen[]                    pens = new Pen[] { 
            new Pen(Brushes.Red,    2), new Pen(Brushes.Blue,  2), new Pen(Brushes.Green,  2), new Pen(Brushes.Cyan, 2),
            new Pen(Brushes.Yellow, 2), new Pen(Brushes.Black, 2), new Pen(Brushes.Orange, 2), new Pen(Brushes.Gray, 2) };
        public NumberInputBox   MaxRangeBox = new NumberInputBox();
        public NumberInputBox   MinRangeBox = new NumberInputBox();

        /// <summary>Data source that extracts sequence of points and their attributes from DataTable</summary>
        public class SortedDataTableSource : EnumerableDataSource<DataRow>
        {
            class ViewHelper : IEnumerable
            {
                DataTable _tab;
                public ViewHelper(DataTable tab) { _tab = tab; }
                IEnumerator IEnumerable.GetEnumerator() {
                    foreach (DataRowView row in _tab.DefaultView)
                        yield return row.Row;
                }
            }
            bool _suspendNotifications = false;

            void NewRowInsertedHandler(object sender, DataTableNewRowEventArgs e) {
                // Raise DataChanged event. ChartPlotter should redraw graph.
                // This will be done automatically when rows are added to table.
                if (!SuspendNotifications)
                    RaiseDataChanged();
            }
            void RowChangedHandler(object sender, DataRowChangeEventArgs e) {
                if (!SuspendNotifications)
                    RaiseDataChanged();
            }

            public SortedDataTableSource(DataTable table)
                : base(new ViewHelper(table)) {
                // Subscribe to DataTable events
                table.TableNewRow += NewRowInsertedHandler;
                table.RowChanged += RowChangedHandler;
                table.RowDeleted += RowChangedHandler;
            }
            public bool SuspendNotifications {
                get { return _suspendNotifications; }
                set {
                    _suspendNotifications = value;
                    if (!value)
                        RaiseDataChanged();
                }
            }
        }
        /// <summary>
        /// This creates and manages all of the LineGraphs that are associated with a single expression
        /// e.g., y = x  produces one LineGraph, but x^2+y^2 = 1 produces two LineGraphs
        /// </summary>
        public class CanonicalFuncPlot {

            bool isMarkerGraphShown = false;
            bool isLineGraphShown   = false;
            Pen  plotPen            = new Pen(Brushes.Blue, 1);
            DataTable            createNewDataTable(bool sorted) {
                DataTable table = new DataTable();
                Tables.Add(table);
                table.Columns.Add(Func.IndepVar.ToString(), typeof(double));
                if (sorted)
                    table.DefaultView.Sort = Func.IndepVar.ToString() + " asc";
                table.Columns.Add(Func.DepVar.ToString(), typeof(double));
                return table;
            }
            SortedDataTableSource createNewDataSource(DataTable table) {
                SortedDataTableSource data = new SortedDataTableSource(table);
                Datas.Add(data);
                bool yMajorAxis = Func.IndepVar == new LetterSym('y');
                Major.Add(!yMajorAxis);
                string indepVarName = (yMajorAxis ? Func.DepVar : Func.IndepVar).ToString();
                string depVarName = (!yMajorAxis ? Func.DepVar : Func.IndepVar).ToString();
                data.SetXMapping(row => (double)row[indepVarName]);
                data.SetYMapping(row => (double)row[depVarName]);
                return data;
            }
            void                 createPlots(FunctionPlot plot) {
                for (int i = 0; i < Func.Funcs.Count; i++) {
                    Graphs.Add(plot.AddLineGraph(createNewDataSource(createNewDataTable(!IsPolar && (!Func.IsInequality || i > 0))),
                        PlotPen, new CirclePointMarker(), null));
                    // sigh ... the new graph is placed on top of everything else (like annotations & axis widgets)
                    // so we have to remove it's contents and put them at the bottom of the stack ... this behavior should
                    // be a flag in AddLineGraph()
                    var child = plot.Children[plot.Children.Count - 1];
                    var child2 = plot.Children[plot.Children.Count - 2];
                    plot.Children.Remove(child);
                    plot.Children.Remove(child2);
                    plot.Children.Insert(0, child);
                    plot.Children.Insert(0, child2);
                }
                IsMarkerGraphShown = Func.IsInequality;
                IsLineGraphShown   = !Func.IsInequality;
                PlotPen            = new Pen(PlotPen.Brush, Func.IsInequality ? 1:3);
            }
            public class PolarParams {
                public double Start { get; set; }
                public double Stop { get; set; }
                public double Step { get; set; }
                public PolarParams(double start, double stop) {
                    Start = start;
                    Stop = stop;
                    Step = Math.PI / 180;
                }
            }
            public List<LineAndMarker<MarkerPointsGraph>> Graphs { get; set; }
            public bool                  IsPolar { get { return Func != null && 
                                                   Func.IndepVar == new LetterSym(Unicode.G.GREEK_SMALL_LETTER_THETA); }}
            public PolarParams           Polar  { get; set; }
            public CanonicalFunc         Func   { get; set; }
            public Expr                  RawExpr{ get; set; }
            public Guid                  ExprId { get; set; }
            public List<DataTable>       Tables { get; set; }
            public List<SortedDataTableSource> Datas  { get; set; }
            public List<bool>            Major  { get; set; }
            public CanonicalFuncPlot(Expr rawExpr, Guid exprId, CanonicalFunc func, FunctionPlot plotter, Pen pen) {
                func.CacheChangedEvent += new CanonicalFunc.CacheChangedHandler((CanonicalFunc f) => {
                    foreach (DataTable table in Tables)
                        table.Rows.Clear();
                });
                RawExpr = rawExpr;
                ExprId = exprId;
                Graphs = new List<LineAndMarker<MarkerPointsGraph>>();
                Tables = new List<DataTable>();
                Datas  = new List<SortedDataTableSource>();
                Major  = new List<bool>();
                Func   = func;
                plotPen = pen;
                Polar  = new PolarParams(0, 4 * Math.PI);
                createPlots(plotter);
            }
            public bool IsMarkerGraphShown {
                get { return isMarkerGraphShown; }
                set {
                    isMarkerGraphShown = value;
                    if (Func.IsInequality) {
                        Graphs[0].MarkerGraph.Visibility = Visibility.Visible;
                        for (int i = 1; i < Graphs.Count; i++)
                            Graphs[i].MarkerGraph.Visibility = Visibility.Collapsed;
                    } else
                        foreach (LineAndMarker<MarkerPointsGraph> graph in Graphs)
                            graph.MarkerGraph.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            public bool IsLineGraphShown   {
                get { return isLineGraphShown; }
                set {
                    isLineGraphShown = value;
                    if (Func.IsInequality) {
                        Graphs[0].LineGraph.Visibility = Visibility.Hidden;
                        for (int i = 1; i < Graphs.Count; i++)
                            Graphs[i].LineGraph.Visibility = Visibility.Visible;
                    }
                    else
                        foreach (LineAndMarker<MarkerPointsGraph> graph in Graphs)
                            graph.LineGraph.Visibility = value ? Visibility.Visible : Visibility.Visible;
                }
            }
            public Pen  PlotPen            { 
                get { return plotPen; } 
                set { 
                    plotPen = value;
                    foreach (SortedDataTableSource data in Datas) {
                        data.AddMapping(ShapePointMarker.FillProperty, row => plotPen.Brush);
                        data.AddMapping(ShapePointMarker.PenProperty, row => plotPen);
                        data.AddMapping(ShapePointMarker.SizeProperty, row => plotPen.Thickness) ;
                    }
                    Pen dashedPen = new Pen(plotPen.Brush, plotPen.Thickness);
                    dashedPen.DashStyle = new DashStyle(new double[] { 1, 5}, 10);
                    for (int i = 0; i < Graphs.Count; i++)
                        Graphs[i].LineGraph.LinePen = (i == 0 || (RawExpr.Head() == WellKnownSym.equals || RawExpr.Head() == WellKnownSym.greaterequals || RawExpr.Head() == WellKnownSym.lessequals)) ? plotPen : dashedPen;
                }
            }

            void fillInFunction(Rct area, Vec step2D, int j) {
                Tables[j].Rows.Clear();
                double start = Major[j] ? area.Left : area.Top;
                double stop = Major[j] ? area.Right + step2D.X : area.Bottom + step2D.Y; // need a few extra points or graph doesn't reach border
                double step = step2D.X / 4;
                if (IsPolar) {
                    start = Polar.Start;
                    stop = Polar.Stop;
                    step = Polar.Step;
                }
                foreach (CanonicalFunc.Result r in Func.SampleRange(Func.CachedFuncs[j], start, stop, step)) {
                    DataRow newRow = Tables[j].NewRow();
                    newRow.ItemArray = new object[] { r.index, r.value };
                    Tables[j].Rows.Add(newRow);
                }
            }

            public void FillIn(Rct area, Vec step2D) {
                if (!Func.IsInequality) { // fill-in function graph
                    for (int j = 0; j < Func.CachedFuncs.Count; j++)
                        fillInFunction(area, step2D, j);
                } 
                if (Func.IsInequality) {  // fill-in inequality graph
                    Tables[0].Rows.Clear();
                    foreach (Pt r in Func.SampleArea(Func.CachedFuncs[0], area, step2D))
                        Tables[0].Rows.Add(r.X, r.Y);
                    for (int i = 1; i < Func.CachedFuncs.Count; i++)
                        fillInFunction(area, step2D, i);
                }
            }

        }
        void   maxRange_Click(object sender, RoutedEventArgs e) {
            e.Handled = true;
            Pt topLeft = (Parent as FrameworkElement).PointFromScreen(LeftPanel.PointToScreen(new Pt()));
            Pt botLeft = (Parent as FrameworkElement).PointFromScreen(LeftPanel.PointToScreen(new Pt(0, LeftPanel.ActualHeight)));
            MaxRangeBox.RenderTransform = new TranslateTransform(topLeft.X - MaxRangeBox.Width, topLeft.Y);
            MinRangeBox.RenderTransform = new TranslateTransform(botLeft.X - MinRangeBox.Width, botLeft.Y);

            MaxRangeBox.Visibility = MaxRangeBox.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            MinRangeBox.Visibility = MinRangeBox.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
        }
        void   graphPropertyChanged(object sender, ExtendedPropertyChangedEventArgs e) {
            if (e.PropertyName == Viewport2D.VisibleProperty.Name)
                UpdatePlot();
        }

        void   plot(IEnumerable<Parser.Range> funcs) {
            foreach (Parser.Range func in funcs) {
                CanonicalFunc cfunc = CanonicalFunc.Convert(func.Parse.expr, func.ID);
                if (cfunc != null)
                    _funcs.Add(new CanonicalFuncPlot(func.Parse.expr, func.ID, cfunc, this, pens[penIndex++ % pens.Length]));
            }

            Width = Height = 400;
            Viewport.Visible = new Rect(0, 0, 4, 10);
            MouseNavigation.IsEnabled = true;
            HorizontalAxisNavigation.AxisScaling = true;
            VerticalAxisNavigation.AxisScaling = true;
            HorizontalAxisNavigation.AxisTiedToOrigin = true;
            VerticalAxisNavigation.AxisTiedToOrigin = true;
            (VerticalAxis as AxisBase<double>).AxisControl.AxisTiedToOrigin = true;
            (HorizontalAxis as AxisBase<double>).AxisControl.AxisTiedToOrigin = true;
            (Viewport as Viewport2D).PropertyChanged += new EventHandler<ExtendedPropertyChangedEventArgs>(graphPropertyChanged);

            Loaded += new RoutedEventHandler((object sender, RoutedEventArgs e) =>
                Visible = new Rct(Visible.TopLeft, new Vec(Visible.Width+1e-7,Visible.Height)));
        }

        public event EventHandler PlotRemovedEvent;
        public event EventHandler PlotAddedEvent;
        public FunctionPlot() { }
        public FunctionPlot(IEnumerable<Parser.Range> funcs) { plot(funcs); }
        public Pen[]                   Palette { get { return pens; } set { pens = value; } }
        public List<CanonicalFuncPlot> Funcs { get { return _funcs; } }
        public CanonicalFuncPlot       AddFunc(Parser.Range func) {
            Pen plotPen = null;
            CanonicalFunc cfunc = CanonicalFunc.Convert(func.Parse.expr, func.ID);
            if (cfunc != null) {
                foreach (CanonicalFuncPlot plot in Funcs)
                    if (plot.ExprId == func.ID) {
                        plotPen = plot.PlotPen;
                        RemovePlot(plot);
                        break;
                    }
                CanonicalFuncPlot cplot = new CanonicalFuncPlot(func.Parse.expr, func.ID, cfunc, this, 
                                                                plotPen != null ? plotPen : pens[penIndex++ % pens.Length]);
                _funcs.Add(cplot);
                if (PlotAddedEvent != null)
                    PlotAddedEvent(cplot, null);

                Viewport2D view2D = this.Viewport;
                foreach (SortedDataTableSource data in cplot.Datas)
                    data.SuspendNotifications = true;

                cplot.FillIn(view2D.Visible,
                    new Vec(view2D.Visible.Width / Viewport.Output.Width * 10, view2D.Visible.Height / Viewport.Output.Height * 10));

                foreach (SortedDataTableSource data in cplot.Datas)
                    data.SuspendNotifications = false;

                return cplot;
            }
            return null;
        }
        public void                    RemovePlot(CanonicalFuncPlot plot) {
            foreach (LineAndMarker<MarkerPointsGraph> graph in plot.Graphs)
                this.RemoveLineGraph(graph);
            Funcs.Remove(plot);
            if (PlotRemovedEvent != null)
                PlotRemovedEvent(plot, null);
        }
        public void                    UpdatePlot() {
            foreach (CanonicalFuncPlot funcPlot in _funcs)
                foreach (SortedDataTableSource data in funcPlot.Datas)
                    data.SuspendNotifications = true;

            foreach (CanonicalFuncPlot funcPlot in _funcs)
                funcPlot.FillIn(Viewport.Visible,
                    new Vec(Viewport.Visible.Width / Viewport.Output.Width * 30,
                            Viewport.Visible.Height / Viewport.Output.Height * 30));

            foreach (CanonicalFuncPlot funcPlot in _funcs) {
                foreach (SortedDataTableSource data in funcPlot.Datas)
                    data.SuspendNotifications = false;
            }
        }
    }
}