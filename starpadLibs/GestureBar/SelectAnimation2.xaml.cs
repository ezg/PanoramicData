using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;

namespace starPadSDK.GestureBarLib
{
	public partial class SelectAnimation2
	{
		public SelectAnimation2()
		{
			this.InitializeComponent();

			
		}

        private void UserControl_Initialized(object sender, System.EventArgs e)
        {
          
        }

        private void UserControl_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            
        }

        public static string MakeRandomName()
        {
            string name = Guid.NewGuid().ToString();
            name = name.Replace("-", "");

            name = "x_" + name;

            return name;
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

        private void ExecTest_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            System.Windows.Shapes.Path path = Regenerate(StrokePath);

            PathGeometry geom = (PathGeometry) path.Data;
            PathFigure figure = geom.Figures[0];

            Storyboard board = new Storyboard();

            PathSegment[] orig = Copy(figure.Segments);

            // Animations for each segment and property
            PointAnimationUsingKeyFrames[] anim1 = new PointAnimationUsingKeyFrames[figure.Segments.Count];
            PointAnimationUsingKeyFrames[] anim2 = new PointAnimationUsingKeyFrames[figure.Segments.Count];
            PointAnimationUsingKeyFrames[] anim3 = new PointAnimationUsingKeyFrames[figure.Segments.Count];

            // Init
            for (int k = 0; k < anim1.Length; k++)
            {
                anim1[k] = new PointAnimationUsingKeyFrames();
                anim2[k] = new PointAnimationUsingKeyFrames();
                anim3[k] = new PointAnimationUsingKeyFrames();
            }

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

                    LinearPointKeyFrame key1 = new LinearPointKeyFrame(pt1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100 * i)));
                    anim1[j].KeyFrames.Add(key1);

                    LinearPointKeyFrame key2 = new LinearPointKeyFrame(pt2, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100 * i)));
                    anim2[j].KeyFrames.Add(key2);

                    LinearPointKeyFrame key3 = new LinearPointKeyFrame(pt3, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(100 * i)));
                    anim3[j].KeyFrames.Add(key3);







                    //board.BeginAnimation(

                    //AddPointAnimationToStoryboard((BezierSegment)figure.Segments[j], this, pt1, 1000, BezierSegment.Point1Property, board);
                    //AddPointAnimationToStoryboard((BezierSegment)figure.Segments[j], this, pt2, 1000, BezierSegment.Point2Property, board);
                    //AddPointAnimationToStoryboard((BezierSegment)figure.Segments[j], this, pt3, 1000, BezierSegment.Point3Property, board);
                }
            }

            this.LayoutRoot.Children.Add(path);

            for (int k = 0; k < anim1.Length; k++)
            {
                figure.Segments[k].BeginAnimation(BezierSegment.Point1Property, anim1[k]);
                figure.Segments[k].BeginAnimation(BezierSegment.Point2Property, anim2[k]);
                figure.Segments[k].BeginAnimation(BezierSegment.Point3Property, anim3[k]);
            }

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

                if (figure.Segments[0] is PolyLineSegment)
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

            output.Stroke = new SolidColorBrush(Colors.Black);
            output.StrokeThickness = 1.0;

            return output;
        }

	}
}