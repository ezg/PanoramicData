using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace starPadSDK.GestureBarLib
{
	public partial class SelectAnimation
	{
		public SelectAnimation()
		{
			this.InitializeComponent();
		}

        //public System.Windows.Shapes.Path ConvertPath()
        //{
        //    System.Windows.Shapes.Path result = new System.Windows.Shapes.Path();
        //   // result.Data = new 
        //}

        public BezierSegment[] Segmentize(PolyBezierSegment input)
        {
            BezierSegment [] segments = new BezierSegment[input.Points.Count / 3];
            int j = 0;

            for (int i = 0; i < input.Points.Count; i += 3)
            {
                segments[j] = new BezierSegment(input.Points[i], input.Points[i + 1], input.Points[i + 2], true);
                j++;
            }

            return segments;
        }

        public LineSegment[] Segmentize(PolyLineSegment input)
        {
            LineSegment[] segments = new LineSegment[input.Points.Count];

            for (int i = 0; i < input.Points.Count; i++)
            {
                segments[i] = new LineSegment(input.Points[i], true);
            }

            return segments;
        }

        private void SecondButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            PathFigure figure;
            PathGeometry path;

            if (SecondPath.Data is StreamGeometry)
            {
                // Clean up the mess that Expression Blend creates

                StreamGeometry stream = (StreamGeometry)SecondPath.Data;

                PathGeometry p = PathGeometry.CreateFromGeometry(stream);
                SecondPath.Data = p;

                path = (PathGeometry)SecondPath.Data;
                figure = path.Figures[0];

                PolyLineSegment segment = (PolyLineSegment)figure.Segments[0];
                LineSegment[] segmented = Segmentize(segment);

                
                //segmented = new LineSegment[1];
                //segmented[0] = new LineSegment(new Point(85.713899, 86.660236), true);
                //figure.StartPoint = new Point(79.135076, 26.128051);

                figure.Segments = new PathSegmentCollection(segmented);



                // Try making another one and see if maybe it's just that expression is fucked

                System.Windows.Shapes.Path test = new System.Windows.Shapes.Path();

                PathGeometry geom = new PathGeometry();
                test.Data = geom;

                PathFigure[] figures = new PathFigure[1];
                figures[0] = new PathFigure();

                geom.Figures = new PathFigureCollection(figures);


                

                LineSegment[] segs = new LineSegment[1];
                segs[0] = new LineSegment(new Point(5, 5), true);

                segs = segmented;
                geom.Figures[0].Segments = new PathSegmentCollection(segs);

                geom.Figures[0].StartPoint = segs[0].Point;//79.135076, 26.128051);

                test.Stroke = new SolidColorBrush(Colors.AliceBlue);
                test.StrokeThickness = 4.0;

                this.LayoutRoot.Children.Add(test);

                for (int i = 0; i < geom.Figures[0].Segments.Count; i++)
                {
                    if (i > 2)
                    {
                        Point pt = ((LineSegment)figure.Segments[1]).Point;

                        PointAnimation anim = new PointAnimation(pt, new Duration(TimeSpan.FromSeconds(2)));
                        geom.Figures[0].Segments[i].BeginAnimation(LineSegment.PointProperty, anim);
                    }
                }
            }


            //path = (PathGeometry)SecondPath.Data;
            //figure = path.Figures[0];

            ////Point origStart = path.Figures[0].StartPoint;

            //for (int i = 0; i < figure.Segments.Count; i++)
            //{
            //    if (i > 0)
            //    {
            //        Point pt = ((LineSegment) figure.Segments[0]).Point;

            //        PointAnimation anim = new PointAnimation(pt, new Duration(TimeSpan.FromSeconds(1)));
            //        figure.Segments[i].BeginAnimation(LineSegment.PointProperty, anim);
            //    }
            //}
        }

        private void execButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            PathFigure figure;
            PathGeometry path;

            if (CoverPath.Data is StreamGeometry)
            {
                // Clean up the mess that Expression Blend creates

                StreamGeometry stream = (StreamGeometry)CoverPath.Data;

                PathGeometry p = PathGeometry.CreateFromGeometry(stream);
                CoverPath.Data = p;

                path = (PathGeometry)CoverPath.Data;
                figure = path.Figures[0];

                PolyBezierSegment segment = (PolyBezierSegment)figure.Segments[0];
                BezierSegment [] segmented = Segmentize(segment);

                segmented = new BezierSegment[1];//Test
                segmented[0] = new BezierSegment(new Point(1, 1), new Point(2, 2), new Point(3, 3), true);

                
                figure.Segments = new PathSegmentCollection(segmented);

                
            }

            //path = (PathGeometry)CoverPath.Data;
            //figure = path.Figures[0];

            //Point origStart = path.Figures[0].StartPoint;

            //for (int i = 0; i < figure.Segments.Count; i++)
            //{
            //    if (i > 0)
            //    {
            //        Point pt = new Point(30, 30);//((BezierSegment) figure.Segments[0]).Point3;

            //        BezierSegment seg = (BezierSegment)figure.Segments[i];
            //        seg.Point1 = pt;
            //        seg.Point2 = pt;
            //        seg.Point3 = pt;

            //        //PointAnimation anim = new PointAnimation(pt, new Duration(TimeSpan.FromSeconds(1)));
            //        //figure.Segments[i].BeginAnimation(BezierSegment.Point1Property, anim);
            //        //figure.Segments[i].BeginAnimation(BezierSegment.Point2Property, anim);
            //        //figure.Segments[i].BeginAnimation(BezierSegment.Point3Property, anim);
            //    }
            //}

            //path.Figures[0].StartPoint = origStart;

            

            


            //Storyboard board = new Storyboard();

            //for (int i = 0; i < segment.Points.Count; i++)
            //{
                
            //    for (int j = i; j < segment.Points.Count; j++)
            //    {
            //    }
            //}
        }


        private void TestBtn_Click(object sender, RoutedEventArgs e)
        {
            
            

            //StreamGeometry stream = (StreamGeometry)CoverPath.Data;
            //CoverPath.Data = stream.GetFlattenedPathGeometry();

            //PathGeometry path = (PathGeometry)CoverPath.Data;
            //PathFigure figure = path.Figures[0];

            //Point prevPoint = figure.StartPoint;
            //Storyboard board = new Storyboard();

            //for (int i = 0; i < figure.Segments.Count; i++)
            //{
            //    PolyLineSegment curSegment = (PolyLineSegment)figure.Segments[i];

            //    Point destPoint = ((PolyLineSegment)figure.Segments[i]).Points[0];

            //    for (int j = i; j < figure.Segments.Count; j++)
            //    {
            //        PolyLineSegment segment = (PolyLineSegment)figure.Segments[j];

            //        PointAnimation animation = new PointAnimation(destPoint, curSegment.Points,
            //            new Duration(new TimeSpan(0, 0, 1)));

            //        string name = Guid.NewGuid().ToString();
            //        RegisterName(name, segment);
            //        Storyboard.SetTargetName(animation, name);
            //        Storyboard.SetTargetProperty(animation, new PropertyPath(PolyLineSegment.PointsProperty));

            //        //board.Children.Add(animation);
            //    }

                

            //    // Continue

            //    //prevPoint = curSegment.Point;
            //}
        }
    }
}