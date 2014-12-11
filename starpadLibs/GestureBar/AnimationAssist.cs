using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Collections;
using starPadSDK.GestureBarLib.UICommon;
using starPadSDK.GestureBarLib;

namespace starPadSDK.GestureBarLib
{
    // This class helps to automate procedural animation-related tasks
    class AnimationAssist
    {
        FrameworkElement _Parent = null;

        public AnimationAssist(FrameworkElement parent)
        {
            _Parent = parent;
        }

        public static string MakeRandomName()
        {
            string name = Guid.NewGuid().ToString();
            name = name.Replace("-", "");

            name = "x_" + name;

            return name;
        }

        public ArrayList ExecuteStrokeAnimation(ArrayList input, Canvas outputContainer, Canvas penIcon, int msDelay, bool beginPlay)
        {
            double temp = 0;

            return ExecuteStrokeAnimation(input, outputContainer, penIcon, msDelay, beginPlay, ref temp, null, null);
        }

        public static Rect ComputeBoundingBox(System.Windows.Shapes.Path input)
        {
            System.Windows.Shapes.Path path = Regenerate(input);

            PathGeometry geom = (PathGeometry)path.Data;
            PathFigure figure = geom.Figures[0];

            Rect result = new Rect(figure.StartPoint.X, figure.StartPoint.Y, 0, 0);

            for (int i = 0; i < figure.Segments.Count; i++)
            {
                BezierSegment seg = (BezierSegment)figure.Segments[i];

                result.Union(seg.Point1);
            }

            return result;
        }

        public static Rect ComputeBoundingBox(ArrayList paths)
        {
            Rect rcResult = new Rect();

            foreach (System.Windows.Shapes.Path path in paths)
            {
                Rect rcTemp = ComputeBoundingBox(path); //Helpers.GetLocation(path);

                rcResult.Union(rcTemp.TopLeft);
                rcResult.Union(rcTemp.BottomRight);
            }

            return rcResult;
        }

        Storyboard _CurrentAnimation = null;
        ArrayList _CurrentPathStartTimes = null;
        ArrayList _CurrentPathEndTimes = null;

        ArrayList _StartDots = null;

        Storyboard _PenDownAnim = null;
        Storyboard _PenUpAnim = null;

        public ArrayList ExecuteStrokeAnimation(ArrayList input, Canvas outputContainer, Canvas penIcon, int msDelay,
                                                bool beginPlay, ref double animationDurationOut, Storyboard penDownAnim, Storyboard penUpAnim)
        {
            ArrayList output = new ArrayList();
            const double spacing = 400;
            double currentTime = 500;
            double animationDuration = 0;

            _PenDownAnim = penDownAnim;
            _PenUpAnim = penUpAnim;

            _StartDots = new ArrayList();

            DoubleAnimationUsingKeyFrames penLeftAnim = null;
            DoubleAnimationUsingKeyFrames penTopAnim = null;

            _CurrentPathStartTimes = new ArrayList();
            _CurrentPathEndTimes = new ArrayList();

            foreach (System.Windows.Shapes.Path path in input)
            {
                

                System.Windows.Shapes.Path animOutput = ExecuteStrokeAnimation_SinglePath(path, currentTime, 
                                                                                            outputContainer, penIcon, msDelay, ref animationDuration, 
                                                                                            ref penLeftAnim, ref penTopAnim, beginPlay);
                output.Add(animOutput);

                _CurrentPathStartTimes.Add(currentTime - 166);

                currentTime += animationDuration;

                _CurrentPathEndTimes.Add(currentTime);

                currentTime += spacing;

                // Add the background trace-over

                System.Windows.Shapes.Path backgroundTraceoverStroke = Regenerate(path);
                backgroundTraceoverStroke.Opacity = 0.25;

                outputContainer.Children.Add(backgroundTraceoverStroke);

                // Add a start dot

                PenDownDot dot = new PenDownDot();
                outputContainer.Children.Add(dot);

                PathGeometry geom = (PathGeometry)animOutput.Data;
                PathFigure figure = geom.Figures[0];

                dot.RenderTransform = new TranslateTransform(figure.StartPoint.X, figure.StartPoint.Y);
                dot.Hide();

                _StartDots.Add(dot);                
            }

            animationDurationOut = currentTime;

            if (penIcon != null)
            {
                Console.WriteLine("Start!");

                Storyboard penStoryboard = new Storyboard();
          /*      penIcon.RegisterName("PenIcon", penIcon);

                Storyboard.SetTargetName(penLeftAnim, "PenIcon");
                Storyboard.SetTargetProperty(penLeftAnim, new PropertyPath(Canvas.LeftProperty));
                penStoryboard.Children.Add(penLeftAnim);

                Storyboard.SetTargetName(penTopAnim, "PenIcon");
                Storyboard.SetTargetProperty(penTopAnim, new PropertyPath(Canvas.TopProperty));
                penStoryboard.Children.Add(penTopAnim);*/

                Helpers.AddAnimationToStoryboard(penLeftAnim, penIcon, Canvas.LeftProperty, penStoryboard);
                Helpers.AddAnimationToStoryboard(penTopAnim, penIcon, Canvas.TopProperty, penStoryboard);

                _CurrentAnimation = penStoryboard;

                penStoryboard.CurrentTimeInvalidated += new EventHandler(penLeftAnim_CurrentTimeInvalidated);
                penStoryboard.Completed += new EventHandler(penStoryboard_Completed);

                penStoryboard.Begin(_Parent, true); //outputContainer

                

//                penIcon.BeginAnimation(Canvas.LeftProperty, penLeftAnim);
//                penIcon.BeginAnimation(Canvas.TopProperty, penTopAnim);

                
            }

            return output;
        }

        void penStoryboard_Completed(object sender, EventArgs e)
        {
            if (_Parent is GestureExplorerDemoUnit)
            {
                ((GestureExplorerDemoUnit)_Parent).ShowDetailsLayer();

                ((GestureExplorerDemoUnit)_Parent).NotifyAnimationCompleted();
            }
        }

        void penLeftAnim_CurrentTimeInvalidated(object sender, EventArgs e)
        {
            /*ClockGroup group = (ClockGroup)sender;
            Storyboard board = (Storyboard)group.Timeline;*/

            try
            {

                double time = _CurrentAnimation.GetCurrentTime(_Parent).Value.TotalMilliseconds;

                //Console.WriteLine(time.ToString());

                {
                    if (_CurrentPathStartTimes.Count > 0)
                    {
                        double timeOffset = (double)_CurrentPathStartTimes[0];

                        if (time >= timeOffset)
                        {
                            // Pen down animation 

                            _CurrentPathStartTimes.RemoveAt(0);

                            if (_PenDownAnim != null)
                            {
                                _PenDownAnim.Begin(_Parent);
                            }

                            Console.WriteLine("Down");

                            // Show start dot

                            if (_StartDots.Count > 0)
                            {
                                PenDownDot dot = (PenDownDot)_StartDots[0];

                                dot.Show();
                                _StartDots.RemoveAt(0);
                            }
                        }
                    }

                    if (_CurrentPathEndTimes.Count > 0)
                    {
                        double timeOffset = (double)_CurrentPathEndTimes[0];

                        if (time >= timeOffset)
                        {
                            _CurrentPathEndTimes.RemoveAt(0);

                            if (_PenUpAnim != null)
                            {
                                _PenUpAnim.Begin(_Parent);
                            }

                            Console.WriteLine("Up");
                        }
                    }
                }

            }
            catch (Exception ex)
            {
            }
        }

       

        

        public System.Windows.Shapes.Path ExecuteStrokeAnimation_SinglePath(System.Windows.Shapes.Path input, double initialDelay,
                                                                                        Canvas outputContainer, Canvas penIcon, int msDelay, 
                                                                                        ref double animationDuration, ref DoubleAnimationUsingKeyFrames penLeftAnim,
                                                                                        ref DoubleAnimationUsingKeyFrames penTopAnim, bool beginPlay)
        {
            System.Windows.Shapes.Path path = Regenerate(input);

            PathGeometry geom = (PathGeometry)path.Data;
            PathFigure figure = geom.Figures[0];

            Storyboard board = new Storyboard();

            PathSegment[] orig = Copy(figure.Segments);

            

            

            // Animations for each segment and property
            PointAnimationUsingKeyFrames[] anim1 = new PointAnimationUsingKeyFrames[figure.Segments.Count];
            PointAnimationUsingKeyFrames[] anim2 = new PointAnimationUsingKeyFrames[figure.Segments.Count];
            PointAnimationUsingKeyFrames[] anim3 = new PointAnimationUsingKeyFrames[figure.Segments.Count];

            if (penLeftAnim == null)
            {
                penLeftAnim = new DoubleAnimationUsingKeyFrames();
                penTopAnim = new DoubleAnimationUsingKeyFrames();

                Rect rcPen = Helpers.GetLocation(penIcon);

                penLeftAnim.KeyFrames.Add(new LinearDoubleKeyFrame(rcPen.Left, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
                penTopAnim.KeyFrames.Add(new LinearDoubleKeyFrame(rcPen.Top, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            }

            double InitialDelay = 0 + initialDelay;

            penLeftAnim.KeyFrames.Add(new LinearDoubleKeyFrame(figure.StartPoint.X, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(InitialDelay))));
            penTopAnim.KeyFrames.Add(new LinearDoubleKeyFrame(figure.StartPoint.Y, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(InitialDelay))));

            // Init
            for (int k = 0; k < anim1.Length; k++)
            {
                anim1[k] = new PointAnimationUsingKeyFrames();
                anim2[k] = new PointAnimationUsingKeyFrames();
                anim3[k] = new PointAnimationUsingKeyFrames();

                KeyTime time = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0));
                LinearPointKeyFrame key1 = new LinearPointKeyFrame(figure.StartPoint, time);
                anim1[k].KeyFrames.Add(key1);

                LinearPointKeyFrame key2 = new LinearPointKeyFrame(figure.StartPoint, time);
                anim2[k].KeyFrames.Add(key2);

                LinearPointKeyFrame key3 = new LinearPointKeyFrame(figure.StartPoint, time);
                anim3[k].KeyFrames.Add(key3);



                time = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(InitialDelay));
                key1 = new LinearPointKeyFrame(figure.StartPoint, time);
                anim1[k].KeyFrames.Add(key1);

                key2 = new LinearPointKeyFrame(figure.StartPoint, time);
                anim2[k].KeyFrames.Add(key2);

                key3 = new LinearPointKeyFrame(figure.StartPoint, time);
                anim3[k].KeyFrames.Add(key3);
            }

            

            double calcTime = InitialDelay;
            

            // For each segment
            for (int i = 0; i < figure.Segments.Count; i++)
            {
                // Move each subsequent segment
                for (int j = i; j < figure.Segments.Count; j++)
                {
                    Point pt1 = ((BezierSegment)orig[i]).Point1;
                    Point pt2 = ((BezierSegment)orig[i]).Point2;
                    Point pt3 = ((BezierSegment)orig[i]).Point3;

                    //((BezierSegment)figure.Segments[j]).Point1 = pt1;
                    //((BezierSegment)figure.Segments[j]).Point2 = pt2;
                    //((BezierSegment)figure.Segments[j]).Point3 = pt3;

                    calcTime = InitialDelay + (msDelay * (i + 1));
                    animationDuration = calcTime - InitialDelay;

                    KeyTime time = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(calcTime));

                    LinearPointKeyFrame key1 = new LinearPointKeyFrame(pt1, time);
                    anim1[j].KeyFrames.Add(key1);

                    LinearPointKeyFrame key2 = new LinearPointKeyFrame(pt2, time);
                    anim2[j].KeyFrames.Add(key2);

                    LinearPointKeyFrame key3 = new LinearPointKeyFrame(pt3, time);
                    anim3[j].KeyFrames.Add(key3);


                    LinearDoubleKeyFrame keyLeft = new LinearDoubleKeyFrame(pt3.X, time);
                    penLeftAnim.KeyFrames.Add(keyLeft);

                    LinearDoubleKeyFrame keyRight = new LinearDoubleKeyFrame(pt3.Y, time);
                    penTopAnim.KeyFrames.Add(keyRight);




                    //board.BeginAnimation(

                    //AddPointAnimationToStoryboard((BezierSegment)figure.Segments[j], this, pt1, 1000, BezierSegment.Point1Property, board);
                    //AddPointAnimationToStoryboard((BezierSegment)figure.Segments[j], this, pt2, 1000, BezierSegment.Point2Property, board);
                    //AddPointAnimationToStoryboard((BezierSegment)figure.Segments[j], this, pt3, 1000, BezierSegment.Point3Property, board);
                }
            }

            

            //((System.Windows.Controls.Grid) outputContainer.Content).Children.Add(path);
            outputContainer.Children.Add(path);
//            outputContainer.Children.Insert(0, path);

            if (beginPlay)
            {
                for (int k = 0; k < anim1.Length; k++)
                {
                    figure.Segments[k].BeginAnimation(BezierSegment.Point1Property, anim1[k]);
                    figure.Segments[k].BeginAnimation(BezierSegment.Point2Property, anim2[k]);
                    figure.Segments[k].BeginAnimation(BezierSegment.Point3Property, anim3[k]);
                }

                //if (penIcon != null)
                //{
                //    penIcon.BeginAnimation(Canvas.LeftProperty, penLeftAnim);
                //    penIcon.BeginAnimation(Canvas.TopProperty, penTopAnim);
                //}
            }

            return path;
        }

        public static void AddPointAnimationToStoryboard(DependencyObject appliesTo, FrameworkElement parent, Point destPoint, int milliseconds,
                                                            object property, Storyboard board)
        {
            PointAnimation anim = new PointAnimation(destPoint, new Duration(TimeSpan.FromMilliseconds(milliseconds)));

            string name = MakeRandomName();

            if (Storyboard.GetTargetName(appliesTo) != null)
            {
                name = Storyboard.GetTargetName(appliesTo);
            }
            else
            {
                parent.RegisterName(name, appliesTo);
            }

            Storyboard.SetTargetName(anim, name);
            Storyboard.SetTargetProperty(anim, new PropertyPath(property));

            board.Children.Add(anim);
        }

        public static PathSegment[] Copy(PathSegmentCollection input)
        {
            PathSegment[] output = new PathSegment[input.Count];

            for (int i = 0; i < input.Count; i++)
            {
                output[i] = input[i].Clone();
            }

            return output;
        }

        public static BezierSegment[] Segmentize(PolyBezierSegment input)
        {
            BezierSegment[] segments = new BezierSegment[input.Points.Count / 3];
            int j = 0;

            for (int i = 0; i < input.Points.Count; i += 3)
            {
                segments[j] = new BezierSegment(input.Points[i], input.Points[i + 1], input.Points[i + 2], true);
                j++;
            }

            return segments;
        }

        public static LineSegment[] Segmentize(PolyLineSegment input)
        {
            LineSegment[] segments = new LineSegment[input.Points.Count];

            for (int i = 0; i < input.Points.Count; i++)
            {
                segments[i] = new LineSegment(input.Points[i], true);
            }

            return segments;
        }

        public static void Clean(System.Windows.Shapes.Path input)
        {
            PathFigure figure;
            PathGeometry path;

            if (input.Data is StreamGeometry)
            {
                // Clean up the mess that Expression Blend creates

                StreamGeometry stream = (StreamGeometry)input.Data;

                PathGeometry p = PathGeometry.CreateFromGeometry(stream);
                input.Data = p;

                path = (PathGeometry)input.Data;
                figure = path.Figures[0];

                if (figure.Segments.Count <= 0)
                {
                    figure.Segments = new PathSegmentCollection(new LineSegment[0]);
                }
                else if (figure.Segments[0] is PolyLineSegment)
                {
                    PolyLineSegment segment = (PolyLineSegment)figure.Segments[0];
                    LineSegment[] segmented = Segmentize(segment);

                    figure.Segments = new PathSegmentCollection(segmented);
                }
                else if (figure.Segments[0] is PolyBezierSegment)
                {
                    PolyBezierSegment segment = (PolyBezierSegment)figure.Segments[0];
                    BezierSegment[] segmented = Segmentize(segment);

                    figure.Segments = new PathSegmentCollection(segmented);
                }
                else if (figure.Segments[0] is BezierSegment)
                {
                    //PolyBezierSegment segment = new PolyBezierSegment(); // (PolyBezierSegment)figure.Segments[0];
                    BezierSegment[] segmented = new BezierSegment[1];
                    segmented[0] = (BezierSegment)figure.Segments[0];

                    figure.Segments = new PathSegmentCollection(segmented);
                }
                else
                    throw new Exception();
            }
        }

        public static System.Windows.Shapes.Path Regenerate(System.Windows.Shapes.Path input)
        {
            Clean(input);

            System.Windows.Shapes.Path output = new System.Windows.Shapes.Path();

            PathGeometry geom = new PathGeometry();
            output.Data = geom;

            PathFigure[] figures = new PathFigure[1];
            figures[0] = new PathFigure();

            geom.Figures = new PathFigureCollection(figures);



            PathGeometry inputGeom = (PathGeometry)input.Data;
            geom.Figures[0].Segments = inputGeom.Figures[0].Segments.Clone();

            geom.Figures[0].StartPoint = inputGeom.Figures[0].StartPoint;//79.135076, 26.128051);

            output.Stroke = new SolidColorBrush( ((SolidColorBrush)input.Stroke).Color );
            output.StrokeThickness = 1.0;


            return output;
        }
    }
}
