using CombinedInputAPI;
using PanoramicDataModel;
using PixelLab.Common;
using starPadSDK.AppLib;
using starPadSDK.Geom;
using starPadSDK.Inq;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace PanoramicData.view.other
{
    /// <summary>
    /// Interaction logic for RadialControl.xaml
    /// </summary>
    public partial class RadialControl : UserControl
    {
        public static double SIZE = 200;
        private static double cx = SIZE / 2;
        private static double cy = SIZE / 2;

        private static double innerRadius = 70;
        private static double activeRadius = 30;
        private static double labelRadius = 50;
        private static double highlightInnerRadius = 63;
        private static double highlightOuterRadius = 70;
        public static double OUTER_RADIUS = SIZE / 2;
        private double _x = 0;
        private double _y = 0;
        private PanoramicData.utils.inq.InqAnalyzer _inqAnalyser = new PanoramicData.utils.inq.InqAnalyzer();
        private SimpleAPage _aPage = new SimpleAPage();
        private Label _resultLabel = new Label();

        private bool _moveLower = false;
        private bool _moveMiddle = false;
        private bool _moveUpper = false;

        private List<RadialSegement> _radialSegments = new List<RadialSegement>();
        private RadialSegement _currentRadialSegment = null; 
        private RadialMenuCommand _currentCommand = null;

        private Border _draggingShadow = null;
        private Point _startDrag = new Point(0, 0);

        private RadialControlExecution _execution = null;
        private Delegate _outsidePointDelegate = null;

        public RadialControl(RadialMenuCommand root, RadialControlExecution execution, object outsideOf = null)
        {
            InitializeComponent();
            this.Width = SIZE;
            this.Height = SIZE;

            this.TouchDown += RadialControl_TouchDown;

            _outsidePointDelegate = new EventHandler<TouchEventArgs>(TouchOutsideDownEvent);
            Application.Current.MainWindow.AddHandler(Window.TouchDownEvent, _outsidePointDelegate, true);

            _currentCommand = root;
            _execution = execution;

            _inqAnalyser.ResultsUpdated += _inqAnalyser_ResultsUpdated;
            _aPage.StroqAddedEvent += new starPadSDK.AppLib.InqScene.StroqHandler(stroqAddedEvent);
            _aPage.StroqRemovedEvent += new starPadSDK.AppLib.InqScene.StroqHandler(stroqRemovedEvent);
            _aPage.StroqsAddedEvent += new starPadSDK.AppLib.InqScene.StroqsHandler(stroqsAddedEvent);
            _aPage.StroqsRemovedEvent += new starPadSDK.AppLib.InqScene.StroqsHandler(stroqsRemovedEvent);
            
            setup(_currentCommand);
        }

        void _inqAnalyser_ResultsUpdated(object sender, System.Windows.Ink.ResultsUpdatedEventArgs e)
        {
            string recognizedString = _inqAnalyser.GetRecognizedString().Trim().Replace("\r\n", " ");
            _resultLabel.Content = recognizedString;
            _execution.ExecuteCommand(this, _currentCommand, recognizedString, _aPage.Stroqs);
        }

        void stroqAddedEvent(Stroq s)
        {
            //_mathManager.UpdateMathRecognition(aPage.Stroqs);
            _inqAnalyser.AddStroke(s);
            _inqAnalyser.BackgroundAnalyze();
        }

        void stroqsAddedEvent(Stroq[] stroqs)
        {
            foreach (var s in stroqs)
            {
                _inqAnalyser.AddStroke(s);
            }
            _inqAnalyser.BackgroundAnalyze();
            //_mathManager.UpdateMathRecognition(aPage.Stroqs);
        }

        void stroqRemovedEvent(Stroq s)
        {
            //_mathManager.UpdateMathRecognition(aPage.Stroqs);
            _inqAnalyser.RemoveStroke(s);
            _inqAnalyser.BackgroundAnalyze();
        }

        void stroqsRemovedEvent(Stroq[] stroqs)
        {
            //_mathManager.UpdateMathRecognition(aPage.Stroqs);
            foreach (var s in stroqs)
            {
                _inqAnalyser.RemoveStroke(s);
            }
            _inqAnalyser.BackgroundAnalyze();
        }

        static DependencyObject VisualUpwardSearch<T>(DependencyObject source)
        {
            while (source != null && source.GetType() != typeof(T))
                source = VisualTreeHelper.GetParent(source);

            return source;
        }

        void TouchOutsideDownEvent(object sender, TouchEventArgs e)
        {
            if (e.Source != this)
            {
                var radialControl =
                    VisualUpwardSearch<RadialControl>(e.OriginalSource as DependencyObject) as RadialControl;

                if (radialControl != this)
                {
                    Application.Current.MainWindow.RemoveHandler(Window.TouchDownEvent, _outsidePointDelegate);
                    dispose();
                }
            }
        }

        void goBackOrDispose()
        {
            if (_currentCommand.Parent != null)
            {
                _currentCommand = _currentCommand.Parent;
                setup(_currentCommand);
            }
            else
            {
                dispose();
            }
        }


        void RadialControl_TouchDown(object sender, TouchEventArgs e)
        {
            Point mousePoint = e.GetTouchPoint(this).Position;

            // special case the handwritten input command
            if (_currentCommand.AllowsStroqInput)
            {
                if (mousePoint.X < 0 || mousePoint.Y < 0 || mousePoint.X > OUTER_RADIUS * 4 || mousePoint.Y > OUTER_RADIUS * 2)
                {
                    dispose();
                    return;
                }

                // back;
                if (mousePoint.X < 25 && mousePoint.Y < 20)
                {
                    goBackOrDispose();
                    return;
                }
            }
            else
            {
                e.Handled = true;
                mousePoint.X -= cx;
                mousePoint.Y -= cy;

                // outside of radial menu => dispose
                double distGlobal = Math.Sqrt(Math.Pow(mousePoint.X, 2) + Math.Pow(mousePoint.Y, 2));
                if (distGlobal > OUTER_RADIUS)
                {
                    dispose();
                    return;
                }

                // hit the back button or the exit button
                if (distGlobal < activeRadius)
                {
                    goBackOrDispose();
                    return;
                }

                // find the closest segement
                RadialSegement closestSegment = null;
                double closestDist = double.MaxValue;
                foreach (var rs in _radialSegments)
                {
                    double dist = Math.Sqrt(Math.Pow(rs.CenterPoint.X - mousePoint.X, 2) + Math.Pow(rs.CenterPoint.Y - mousePoint.Y, 2));
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestSegment = rs;
                    }
                }

                // extract the corresponding command
                RadialMenuCommand cmd = closestSegment.RadialMenuCommand;

                // we are within the label area. 
                if (distGlobal < innerRadius)
                {
                    // execute command if it can be selected
                    if (cmd.IsSelectable)
                    {
                        executeCommand(cmd);
                    }
                    else
                    {
                        // move one down in the cmd hierachy 
                        if (cmd.InnerCommands.Count > 0 && cmd != _currentCommand)
                        {
                            _currentCommand = cmd;
                            setup(_currentCommand);
                        }
                    }
                }
                else
                {
                    // move one down in the cmd hierachy 
                    if (cmd.InnerCommands.Count > 0 && cmd != _currentCommand)
                    {
                        _currentCommand = cmd;
                        setup(_currentCommand);
                    }
                    else if (cmd.AllowsNumericInput)
                    {
                        e.TouchDevice.Capture(this);
                        _currentRadialSegment = closestSegment;

                        this.TouchMove += RadialControl_TouchMove;
                        this.TouchUp += RadialControl_TouchUp;

                        handleNumericInput(mousePoint, _currentRadialSegment, false);
                    }
                    // execute command
                    else
                    {
                        executeCommand(cmd);
                    }
                }
                
                if (cmd.AllowsDragging)
                {
                    e.TouchDevice.Capture(this);
                    _currentRadialSegment = closestSegment;

                    this.TouchMove += RadialControl_TouchMove;
                    this.TouchUp += RadialControl_TouchUp;
                    
                    handleDragging(e, _currentRadialSegment);
                }
            }
        }

        void executeCommand(RadialMenuCommand cmd)
        {
            cmd.IsActive = !cmd.IsActive;
            try
            {
                if (cmd.IsRemove)
                {
                    _execution.Remove(this, cmd);
                    _execution.Dispose(this);
                }
                else
                {
                    _execution.ExecuteCommand(this, cmd);
                }
            }
            catch (Exception e)
            {
                
            }
            setup(_currentCommand, false);
        }

        void handleNumericInput(Point mousePoint, RadialSegement radialSegement, bool isInMotion)
        {
            RadialMenuCommand cmd = radialSegement.RadialMenuCommand;

            Vec v = new Vec(mousePoint.X, mousePoint.Y);
            v.Normalize();
            Vec axis = new Vec(1, 0);
            double mouseAngle = Math.Acos(v.Dot(axis));
            if (v.Y < 0)
            {
                mouseAngle = Math.PI * 2 - mouseAngle;
            }

            if (cmd.IsRangeNumericInput)
            {
                if (!isInMotion)
                {
                    _moveLower = false;
                    _moveMiddle = false;
                    _moveUpper = false;
                }

                double angleUpper = ((radialSegement.Angle2 - radialSegement.Angle1) / (double)(cmd.MaxNumericValue - cmd.MinNumericValue)) * (double)(cmd.UpperNumericValue - cmd.MinNumericValue) + radialSegement.Angle1;
                double angleLower = ((radialSegement.Angle2 - radialSegement.Angle1) / (double)(cmd.MaxNumericValue - cmd.MinNumericValue)) * (double)(cmd.LowerNumericValue - cmd.MinNumericValue) + radialSegement.Angle1;
                double angleMiddle = (angleUpper + angleLower) / 2.0;

                double distToUpper = Math.Pow(mouseAngle - angleUpper, 2);
                double distToLower = Math.Pow(mouseAngle - angleLower, 2);

                //Console.WriteLine(angleLower + " " + angleMiddle + " " + angleUpper);

                if ((isInMotion && _moveLower) ||
                    (!isInMotion && /*distToLower <= distToMiddle &&*/ distToLower <= distToUpper))
                {
                    cmd.LowerNumericValue = calculateValue(
                        mouseAngle, radialSegement.Angle1, angleUpper,
                        cmd.MinNumericValue, cmd.UpperNumericValue, radialSegement.Angle1, radialSegement.Angle2,
                        cmd.MinNumericValue, cmd.MaxNumericValue);
                    _moveLower = true;
                }
                /*else if ((isInMotion && _moveMiddle) ||
                         (!isInMotion && distToMiddle < distToLower && distToMiddle < distToUpper))
                {
                    Console.WriteLine("move middle");
                    _moveMiddle = true;
                }*/
                else if ((isInMotion && _moveUpper) ||
                         (!isInMotion && /*distToUpper < distToMiddle &&*/ distToUpper < distToLower))
                {
                    cmd.UpperNumericValue = calculateValue(
                        mouseAngle, angleLower, radialSegement.Angle2,
                        cmd.LowerNumericValue, cmd.MaxNumericValue, radialSegement.Angle1, radialSegement.Angle2,
                        cmd.MinNumericValue, cmd.MaxNumericValue);
                    _moveUpper = true;
                }
            }
            else
            {
                cmd.UpperNumericValue = calculateValue(mouseAngle, radialSegement.Angle1, radialSegement.Angle2,
                    cmd.MinNumericValue, cmd.MaxNumericValue, radialSegement.Angle1, radialSegement.Angle2, 
                    cmd.MinNumericValue, cmd.MaxNumericValue);
            }

            if (cmd.IsSelectable)
            {
                cmd.IsActive = true;
            }
            setup(cmd.Parent, false);
        }

        double calculateValue(
            double mouseAngle, double lowerBoundAngle, double upperBoundAngle,
            double lowerBoundCap, double upperBoundCap,
            double startAngle, double endAngle, double minValue, double maxValue)
        {
            if (mouseAngle < lowerBoundAngle)
            {
                return lowerBoundCap;
            }
            else if (mouseAngle > upperBoundAngle)
            {
                return upperBoundCap;
            }
            else
            {
                double c = (mouseAngle - startAngle);
                double e = (endAngle - startAngle);

                return (int)Math.Ceiling((c / e) * (double)(maxValue - minValue)) + minValue;
            }
        }

        private void handleDragging(TouchEventArgs e, RadialSegement radialSegement)
        {
            RadialMenuCommand cmd = radialSegement.RadialMenuCommand;
            InqScene inqScene = this.FindParent<InqScene>();
            Point fromInqScene = e.GetTouchPoint(inqScene).Position;

            if (inqScene != null && _draggingShadow == null)
            {
                _startDrag = fromInqScene;
                _draggingShadow = new Border();
                _draggingShadow.Width = 120;
                _draggingShadow.Height = 40;
                _draggingShadow.Background = new SolidColorBrush(Color.FromArgb(70, 125, 125, 125));
                _draggingShadow.BorderBrush = Brushes.Black;
                Label l = new Label();
                l.HorizontalAlignment = HorizontalAlignment.Center;
                l.VerticalAlignment = VerticalAlignment.Center;
                l.FontWeight = FontWeights.Bold;
                l.Content = radialSegement.RadialMenuCommand.Name.Replace("\n", " ");
                _draggingShadow.Child = l;
                inqScene.AddNoUndo(_draggingShadow);
            }

            if (_draggingShadow != null)
            {
                _startDrag = fromInqScene;
                _draggingShadow.RenderTransform = new TranslateTransform(
                    fromInqScene.X - _draggingShadow.Width / 2.0,
                    fromInqScene.Y - _draggingShadow.Height);
                
            }
        }


        void RadialControl_TouchMove(object sender, TouchEventArgs e)
        {
            Point mousePoint = e.GetTouchPoint(this).Position;
            mousePoint.X -= cx;
            mousePoint.Y -= cy;

            if (_currentRadialSegment.RadialMenuCommand.AllowsNumericInput)
            {
                handleNumericInput(mousePoint, _currentRadialSegment, true);
            }
            else if (_currentRadialSegment.RadialMenuCommand.AllowsDragging)
            {
                handleDragging(e, _currentRadialSegment);
            }
            e.Handled = true;
        }
        
        void RadialControl_TouchUp(object sender, TouchEventArgs e)
        {
            this.TouchMove -= RadialControl_TouchMove;
            this.TouchUp -= RadialControl_TouchUp;
            e.TouchDevice.Capture(null);
            Point mousePoint = e.GetTouchPoint(this).Position;
            mousePoint.X -= cx;
            mousePoint.Y -= cy;

            if (_currentRadialSegment.RadialMenuCommand.AllowsNumericInput)
            {
                handleNumericInput(mousePoint, _currentRadialSegment, true);
                _execution.ExecuteCommand(this, _currentRadialSegment.RadialMenuCommand);
            }
            else if (_currentRadialSegment.RadialMenuCommand.AllowsDragging)
            {
                if (_draggingShadow != null)
                {
                    InqScene inqScene = this.FindParent<InqScene>();
                    Point fromInqScene = e.GetTouchPoint(inqScene).Position;
                    inqScene.Rem(_draggingShadow);
                    _draggingShadow = null;

                    // outside of radial menu => drop
                    double distGlobal = Math.Sqrt(Math.Pow(mousePoint.X, 2) + Math.Pow(mousePoint.Y, 2));
                    if (distGlobal > OUTER_RADIUS)
                    {
                        drop(_currentRadialSegment.RadialMenuCommand, fromInqScene);
                    }
                }
            }
            setup(_currentCommand, false);
            e.Handled = true;
        }

        void dispose()
        {
            Application.Current.MainWindow.RemoveHandler(Window.TouchDownEvent, _outsidePointDelegate);
            _execution.Dispose(this);
        }

        void drop(RadialMenuCommand cmd, Point fromInqScene)
        {
            Application.Current.MainWindow.RemoveHandler(Window.TouchDownEvent, _outsidePointDelegate);
            _execution.Drop(this, cmd, fromInqScene);
        }

        private void setup(RadialMenuCommand command, bool animate = true)
        {
            canvasMain.Children.Clear();
            canvasLabel.Children.Clear();

            _radialSegments.Clear();

            Path p = null;
            PathGeometry pg = null;
            PathFigure pf = null;

            if (command.AllowsStroqInput)
            {
                canvasMain.IsHitTestVisible = true;
                canvasLabel.IsHitTestVisible = true;

                RadialMenuCommand innerCommand = command;

                this.RenderTransform = new TranslateTransform(_x - OUTER_RADIUS, _y);
                this.Width = OUTER_RADIUS * 4;
                this.Height = OUTER_RADIUS * 2;

                Rectangle r1 = null;
                for (int i = 0; i < 10; i++)
                {
                    r1 = new Rectangle();
                    r1.RadiusX = 6;
                    r1.RadiusY = 6;
                    r1.RenderTransform = new TranslateTransform(i, i);
                    r1.Width = OUTER_RADIUS * 4;
                    r1.Height = OUTER_RADIUS * 2;
                    r1.Fill = new SolidColorBrush(Color.FromArgb(25, 30, 30, 30));
                    canvasMain.Children.Add(r1);
                }

                r1 = new Rectangle();
                r1.RadiusX = 6;
                r1.RadiusY = 6;
                r1.Width = OUTER_RADIUS * 4;
                r1.Height = OUTER_RADIUS * 2;
                r1.Fill = Brushes.White;
                r1.Stroke = Brushes.DarkGray;
                r1.StrokeThickness = 3;
                canvasMain.Children.Add(r1);

                _aPage.Width = OUTER_RADIUS * 4;
                _aPage.Height = OUTER_RADIUS * 2;
                _aPage.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));

                _resultLabel.Width = _aPage.Width;
                _resultLabel.Height = 35;
                _resultLabel.HorizontalAlignment = HorizontalAlignment.Right;
                _resultLabel.VerticalAlignment = VerticalAlignment.Bottom;
                _resultLabel.HorizontalContentAlignment = HorizontalAlignment.Right;

                if (innerCommand.Data is PanoramicDataColumnDescriptor &&
                    (innerCommand.Data as PanoramicDataColumnDescriptor).FilterStroqs != null)
                {
                    _aPage.Clear();
                    _aPage.AddNoUndo((innerCommand.Data as PanoramicDataColumnDescriptor).FilterStroqs);
                } 
                else if (innerCommand.Stroqs != null)
                {
                    _aPage.Clear();
                    _aPage.AddNoUndo(innerCommand.Stroqs);
                }
                canvasMain.Children.Add(_aPage);
                canvasMain.Children.Add(_resultLabel);

                // back button
                if (innerCommand.Parent != null)
                {
                    p = new Path();
                    p.RenderTransform = new TranslateTransform(15, 15);
                    pg = new PathGeometry();
                    pg.Figures = new PathFigureCollection();
                    p.Data = pg;

                    p.Fill = Brushes.DarkGray;

                    pf = new PathFigure();
                    pg.Figures.Add(pf);
                    pf.StartPoint = new Point(-8, 0);
                    pf.Segments.Add(new LineSegment(new Point(8, -8), true));
                    pf.Segments.Add(new LineSegment(new Point(8, 8), true));

                    canvasMain.Children.Add(p);
                }
            }
            else
            {
                canvasMain.IsHitTestVisible = false;
                canvasLabel.IsHitTestVisible = false;

                this.Width = SIZE;
                this.Height = SIZE;
                this.RenderTransform = new TranslateTransform(_x, _y);
                Ellipse e1 = null;
                for (int i = 0; i < 10; i++)
                {
                    e1 = new Ellipse();
                    e1.RenderTransform = new TranslateTransform(cx - OUTER_RADIUS + i, cy - OUTER_RADIUS + i);
                    e1.Width = OUTER_RADIUS*2;
                    e1.Height = OUTER_RADIUS*2;
                    e1.Fill = new SolidColorBrush(Color.FromArgb(25, 30, 30, 30));
                    canvasMain.Children.Add(e1);
                }

                e1 = new Ellipse();
                e1.RenderTransform = new TranslateTransform(cx - OUTER_RADIUS, cy - OUTER_RADIUS);
                e1.Width = OUTER_RADIUS * 2;
                e1.Height = OUTER_RADIUS * 2;
                e1.Fill = Brushes.White;
                // e1.StrokeThickness = 2;
                canvasMain.Children.Add(e1);

                double segements = command.InnerCommands.Count;

                for (int i = 0; i < segements; i++)
                {
                    RadialMenuCommand innerCommand = command.InnerCommands[i];

                    double a1 = ((Math.PI*2)/segements)*i + (segements > 1 ? 0.01 : 0);
                    double a2 = ((Math.PI*2)/segements)*(i + 1) - (segements > 1 ? 0.01 : 0);
                    double aM = (a1 + a2) / 2;

                    p = generatePath(a1, a2, innerRadius, OUTER_RADIUS - 2);
                    p.Fill = Brushes.DarkGray;

                    Path activePath = generatePath(a1, a2, highlightInnerRadius, highlightOuterRadius);
                    activePath.Fill = Brushes.LightBlue;
                    activePath.Visibility = (innerCommand.IsActive && innerCommand.IsActivatable) ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;

                    canvasMain.Children.Add(p);
                    canvasMain.Children.Add(activePath);

                    // arrow if there are sub commands
                    if (innerCommand.InnerCommands.Count > 0 || innerCommand.AllowsStroqInput)
                    {
                        Path arrow = generateArrow(aM, innerRadius, OUTER_RADIUS);
                        arrow.Fill = Brushes.Black;
                        canvasMain.Children.Add(arrow);
                    }

                    // if this is a numeric "wheel"
                    if (innerCommand.AllowsNumericInput)
                    {
                        double angleUpper = ((a2 - a1) / (double)(innerCommand.MaxNumericValue - innerCommand.MinNumericValue)) * (double)(innerCommand.UpperNumericValue - innerCommand.MinNumericValue);
                        double angleLower = 0;

                        if (!innerCommand.IsRangeNumericInput)
                        {
                            Label lNum1 = createNumericLabel(aM, innerCommand.UpperNumericValue + "", innerCommand.IsActive);
                            canvasLabel.Children.Add(lNum1);
                        }
                        else
                        {
                            angleLower = ((a2 - a1) / (double)(innerCommand.MaxNumericValue - innerCommand.MinNumericValue)) * (double)(innerCommand.LowerNumericValue - innerCommand.MinNumericValue);
                            Label lNum1 = createNumericLabel(a1 + angleLower, innerCommand.LowerNumericValue + "", innerCommand.IsActive);
                            canvasLabel.Children.Add(lNum1);
                            Label lNum2 = createNumericLabel(a1 + angleUpper, innerCommand.UpperNumericValue + "", innerCommand.IsActive);
                            canvasLabel.Children.Add(lNum2);
                        
                        }
                        Path pNum = generatePath(a1 + angleLower, a1 + angleUpper, innerRadius, OUTER_RADIUS);
                        pNum.Fill = Brushes.LightBlue;

                        canvasMain.Children.Add(pNum);
                    }

                    if (innerCommand.IsRemove)
                    {
                        p = new Path();
                        p.RenderTransformOrigin = new Point(0.5, 0.5);
                        p.RenderTransform = new TranslateTransform(
                            Math.Cos(aM) * labelRadius + cx,
                            Math.Sin(aM) * labelRadius + cy);
                        pg = new PathGeometry();
                        pg.Figures = new PathFigureCollection();
                        p.Data = pg;

                        p.Stroke = Brushes.DarkGray;
                        p.StrokeThickness = 3;
                        const double length = 6;

                        pf = new PathFigure();
                        pg.Figures.Add(pf);
                        pf.StartPoint = new Point(-length, -length);
                        pf.Segments.Add(new LineSegment(new Point(length, length), true));

                        pf = new PathFigure();
                        pg.Figures.Add(pf);
                        pf.StartPoint = new Point(length, -length);
                        pf.Segments.Add(new LineSegment(new Point(-length, length), true));

                        canvasMain.Children.Add(p);
                    }
                    else
                    {
                        Border b = new Border()
                        {
                            Width = 100,
                            Height = 60
                        };
                        TextBlock l = new TextBlock()
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            TextAlignment = TextAlignment.Center,
                            TextWrapping = TextWrapping.Wrap,
                            Text = innerCommand.Name,
                            FontWeight = (innerCommand.IsActive && innerCommand.IsActivatable) ? FontWeights.Bold : FontWeights.Normal
                        };
                        b.Child = l;
                        b.RenderTransform = new TranslateTransform(
                            Math.Cos(aM) * labelRadius + cx - b.Width / 2,
                            Math.Sin(aM) * labelRadius + cy - b.Height / 2);
                        canvasLabel.Children.Add(b);
                    }

                    RadialSegement rs = new RadialSegement();
                    rs.Angle1 = a1;
                    rs.Angle2 = a2;
                    rs.RadialMenuCommand = innerCommand;
                    rs.CenterPoint = new Point(Math.Cos(aM) * OUTER_RADIUS, Math.Sin(aM) * OUTER_RADIUS);
                    rs.Path = p;
                    rs.ActivePath = activePath;
                    _radialSegments.Add(rs);
                }
                p = new Path();
                p.RenderTransform = new TranslateTransform(cx, cy);
                pg = new PathGeometry();
                pg.Figures = new PathFigureCollection();
                p.Data = pg;

                p.Fill = Brushes.DarkGray;

                pf = new PathFigure();
                pg.Figures.Add(pf);
                pf.StartPoint = new Point(-8, 0);
                pf.Segments.Add(new LineSegment(new Point(8, -8), true));
                pf.Segments.Add(new LineSegment(new Point(8, 8), true));

                canvasMain.Children.Add(p);
                InvalidateVisual();
            }

            if (animate)
            {
                Storyboard fadeIn = (Storyboard)TryFindResource("fadeIn");
                fadeIn.Begin(canvasMain);
                fadeIn.Begin(canvasLabel);
                fadeIn.Begin(canvasHighlights);
            }
        }

        private Label createNumericLabel(double angle, string content, bool isBold)
        {
            Label lNum = new Label();
            lNum.Width = 30;
            lNum.Height = 30;
            lNum.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center;
            lNum.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
            lNum.Content = content;
            lNum.FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal;

            double labelRadius = (OUTER_RADIUS + innerRadius) / 2.0;
            lNum.RenderTransform = new TranslateTransform(
                Math.Cos(angle) * labelRadius + cx - lNum.Width / 2,
                Math.Sin(angle) * labelRadius + cy - lNum.Height / 2);

            return lNum;
        }


        private Path generatePath(double a1, double a2, double innerRadius, double outerRadius)
        {
            Path p = new Path();
            p.RenderTransform = new TranslateTransform(cx, cy);
            PathGeometry pg = new PathGeometry();
            pg.Figures = new PathFigureCollection();
            p.Data = pg;

            for (double i = 0; i < 2; i++)
            {
                double a1i = a1 + ((a2 - a1) / 2.0) * i;
                double a2i = a1 + ((a2 - a1) / 2.0) * (i + 1);
                Point p1 = new Point(Math.Cos(a1i) * innerRadius, Math.Sin(a1i) * innerRadius);
                Point p2 = new Point(Math.Cos(a1i) * outerRadius, Math.Sin(a1i) * outerRadius);
                Point p3 = new Point(Math.Cos(a2i) * outerRadius, Math.Sin(a2i) * outerRadius);
                Point p4 = new Point(Math.Cos(a2i) * innerRadius, Math.Sin(a2i) * innerRadius);

                PathFigure pf = new PathFigure();
                pg.Figures.Add(pf);

                p.Stroke = Brushes.White;
                p.StrokeThickness = 0;

                pf.StartPoint = p1;
                pf.Segments.Add(new LineSegment(p2, true));
                pf.Segments.Add(new ArcSegment(p3, new Size(outerRadius, outerRadius), 0, false,
                    SweepDirection.Clockwise, true));
                pf.Segments.Add(new LineSegment(p4, true));
                pf.Segments.Add(new ArcSegment(p1, new Size(innerRadius, innerRadius), 0, false,
                    SweepDirection.Counterclockwise, true));
            }
            return p;
        }

        private Path generateArrow(double a, double innerRadius, double outerRadius)
        {
            double midRadius = (innerRadius + outerRadius) / 2;
            double a1 = a - 0.06;
            double a2 = a + 0.06;

            Point p1 = new Point(Math.Cos(a1) * (midRadius - 3), Math.Sin(a1) * (midRadius - 3));
            Point p2 = new Point(Math.Cos(a) * (midRadius + 3), Math.Sin(a) * (midRadius + 3));
            Point p3 = new Point(Math.Cos(a2) * (midRadius - 3), Math.Sin(a2) * (midRadius - 3));

            Path p = new Path();
            p.RenderTransform = new TranslateTransform(cx, cy);
            PathGeometry pg = new PathGeometry();
            pg.Figures = new PathFigureCollection();
            p.Data = pg;
            PathFigure pf = new PathFigure();
            pg.Figures.Add(pf);

            pf.StartPoint = p1;
            pf.Segments.Add(new LineSegment(p2, true));
            pf.Segments.Add(new LineSegment(p3, true));

            return p;
        }

        private Canvas generateHighligthPath(double angle1, double angle2, double innerRadius, double outerRadius)
        {
            Canvas c = new Canvas();

            double seg = 7;
            for (int i = 0; i < seg; i = i + 2)
            {
                double a1 = angle1 + ((angle2 - angle1) / seg) * i;
                double a2 = angle1 + ((angle2 - angle1) / seg) * (i + 1);

                Path p = generatePath(a1, a2, innerRadius, outerRadius);
                p.Fill = Brushes.DarkGray;
                c.Children.Add(p);
            }
            return c;
        }

        public void SetPosition(double x, double y)
        {
            _x = x;
            _y = y;
            this.RenderTransform = new TranslateTransform(x, y);
        }
    }

    public class RadialControlExecution
    {
        public virtual void Remove(RadialControl sender, RadialMenuCommand cmd) { }
        public virtual void Drop(RadialControl sender, RadialMenuCommand cmd, Point fromInqScene) { }
        public virtual void Dispose(RadialControl sender) { }
        public virtual void ExecuteCommand(
            RadialControl sender, RadialMenuCommand cmd, 
            string needle = null, StroqCollection stroqs = null) { }
    }

    public class RadialSegement
    {
        public double Angle1 { get; set; }
        public double Angle2 { get; set; }
        public Point CenterPoint { get; set; }
        public Path Path { get; set; }
        public Path ActivePath { get; set; }
        public RadialMenuCommand RadialMenuCommand { get; set; }
    }

    public enum RadialMenuCommandComandGroupPolicy { MultiActive, DeactivateOthers, DeactivateAndTriggerOthers }

    public enum RadialMenuCommandParentPolicy { None, ActivateParentWhenActive }

    public class RadialMenuCommandGroup
    {
        public RadialMenuCommandComandGroupPolicy GroupPolicy { get; set; }
        public string GroupName { get; set; }

        public RadialMenuCommandGroup(string groupName, RadialMenuCommandComandGroupPolicy groupPolicy)
        {
            GroupName = groupName;
            GroupPolicy = groupPolicy;
        }
    }

    public class RadialMenuCommand
    {
        public RadialMenuCommand Parent { get; set; }
        public string Command { get; set; }
        public string Name { get; set; }
        public bool AllowsStroqInput { get; set; }
        public StroqCollection Stroqs { get; set; }

        public bool AllowsNumericInput { get; set; }
        public bool AllowsDragging { get; set; }
        public bool IsRangeNumericInput { get; set; }
        public double UpperNumericValue { get; set; }
        public double LowerNumericValue { get; set; }
        public double MaxNumericValue { get; set; }
        public double MinNumericValue { get; set; }

        public RadialMenuCommandGroup CommandGroup { get; set; }
        public RadialMenuCommandParentPolicy ParentPolicy { get; set; }
        public Action<RadialMenuCommand> ActiveTriggered { get; set; }

        public bool IsRemove { get; set; }
        public bool IsSelectable { get; set; }
        public bool IsActivatable { get; set; }
        public bool IsActive { get; set; }
        public object Data { get; set; }
        public string ShortCut { get; set; }
        public System.Drawing.Bitmap Image { get; set; }
        public List<RadialMenuCommand> InnerCommands { get; private set; }

        public RadialMenuCommand()
        {
            InnerCommands = new List<RadialMenuCommand>();
            CommandGroup = null;
            IsActivatable = true;
            ParentPolicy = RadialMenuCommandParentPolicy.None;
        }

        public void AddSubCommand(RadialMenuCommand cmd)
        {
            this.InnerCommands.Add(cmd);
            cmd.Parent = this;
        }
    }
}