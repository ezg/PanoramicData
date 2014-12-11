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
using System.Windows.Media.Media3D;
using starPadSDK.WPFHelp;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;

namespace starPadSDK.SurfaceLib
{
    /// <summary>
    /// Interaction logic for FingerPoseVisualizer.xaml
    /// </summary>
    public partial class FingerPoseVisualizer : SurfaceUserControl
    {
        public FingerPoseVisualizer()
        {
            InitializeComponent();

            Init();
        }

        
        void contactDown(object sender, ContactEventArgs e)
        {
        }

        WpfCubeControl.CubeControl _FingerModel = null;

        public WpfCubeControl.CubeControl FingerModel
        {
            get { return _FingerModel; }
            set { _FingerModel = value; }
        }
        public Ellipse Halo { get { return halo; } }
        public void Init()
        {
            //_FingerModel = CreateCube();
            //SetCamera();

            _FingerModel = new WpfCubeControl.CubeControl();
            LayoutRoot.Children.Add(_FingerModel);
            _FingerModel.Width = _FingerModel.Height = 200;


            _FingerModel.Render();


            _FingerModel.scaleY = 0.2;
            _FingerModel.scaleX = 2;
            _FingerModel.scaleZ = 4.0;

            ModelVisual3D lightHolder = new ModelVisual3D();
            PointLight light = new PointLight(Colors.Azure, new Point3D(20, 0, 40)); //40, 0, 40
            light.Range = 500;
            lightHolder.Content = light;

            _FingerModel.Viewport.Children.Add(lightHolder);

            //CreateSlice(0.5, 0, Colors.Red);

            target1.BringToFront();
            target2.BringToFront();
            target3.BringToFront();

            
            
        }

        /*
        protected ModelVisual3D CreateCube()
        {
            Model3DGroup cube = new Model3DGroup();
            Point3D p0 = new Point3D(0, 0, 0);
            Point3D p1 = new Point3D(5, 0, 0);
            Point3D p2 = new Point3D(5, 0, 5);
            Point3D p3 = new Point3D(0, 0, 5);
            Point3D p4 = new Point3D(0, 5, 0);
            Point3D p5 = new Point3D(5, 5, 0);
            Point3D p6 = new Point3D(5, 5, 5);
            Point3D p7 = new Point3D(0, 5, 5);
            //front side triangles
            cube.Children.Add(CreateTriangleModel(p3, p2, p6));
            cube.Children.Add(CreateTriangleModel(p3, p6, p7));
            //right side triangles
            cube.Children.Add(CreateTriangleModel(p2, p1, p5));
            cube.Children.Add(CreateTriangleModel(p2, p5, p6));
            //back side triangles
            cube.Children.Add(CreateTriangleModel(p1, p0, p4));
            cube.Children.Add(CreateTriangleModel(p1, p4, p5));
            //left side triangles
            cube.Children.Add(CreateTriangleModel(p0, p3, p7));
            cube.Children.Add(CreateTriangleModel(p0, p7, p4));
            //top side triangles
            cube.Children.Add(CreateTriangleModel(p7, p6, p5));
            cube.Children.Add(CreateTriangleModel(p7, p5, p4));
            //bottom side triangles
            cube.Children.Add(CreateTriangleModel(p2, p3, p0));
            cube.Children.Add(CreateTriangleModel(p2, p0, p1));

            ModelVisual3D model = new ModelVisual3D();
            model.Content = cube;
            this.FingerPoseView.Children.Add(model);

            return model;
        }

        private void SetCamera()
        {
            PerspectiveCamera camera = (PerspectiveCamera)FingerPoseView.Camera;
            Point3D position = new Point3D(
                Convert.ToDouble(20),
                Convert.ToDouble(0),
                Convert.ToDouble(0)
            );
            Vector3D lookDirection = new Vector3D(
                Convert.ToDouble(0),
                Convert.ToDouble(0),
                Convert.ToDouble(0)
            );
            camera.Position = position;
            camera.LookDirection = lookDirection;
        }

        private Model3DGroup CreateTriangleModel(Point3D p0, Point3D p1, Point3D p2)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            mesh.Positions.Add(p0);
            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);
            Vector3D normal = CalculateNormal(p0, p1, p2);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            Material material = new DiffuseMaterial(
                new SolidColorBrush(Colors.DarkKhaki));
            GeometryModel3D model = new GeometryModel3D(
                mesh, material);
            Model3DGroup group = new Model3DGroup();
            group.Children.Add(model);
            return group;
        }
        private Vector3D CalculateNormal(Point3D p0, Point3D p1, Point3D p2)
        {
            Vector3D v0 = new Vector3D(
                p1.X - p0.X, p1.Y - p0.Y, p1.Z - p0.Z);
            Vector3D v1 = new Vector3D(
                p2.X - p1.X, p2.Y - p1.Y, p2.Z - p1.Z);
            return Vector3D.CrossProduct(v0, v1);
        }
        */

        public Contact PoseFromContact
        {
            set
            {
                if (_FingerModel != null)
                {
                    double minaxis, maxaxis, ang;
                    value.GetEllipse(this, out maxaxis, out minaxis, out ang);
                    double slider = Math.Max(-100 * value.PhysicalArea, maxaxis);
                    _FingerModel.RotateX(Math.Max(-100, -3 * (slider - 13)));
                    _FingerModel.RotateY(0);
                    _FingerModel.RotateZ(0);

                }
            }
        }


        private ModelUIElement3D CreateSlice(double percent, double currentAngle, Color clr)
        {
            int popOutDistance = 4; // distance slices are from origin

            double size = 5;
            double textpos = size * .25;

            Point popOutPoint = new Point(((popOutDistance - percent * 10) * Math.Sin((currentAngle + percent / 2) * 2 * Math.PI)),
                                            (-(popOutDistance - percent * 10) * Math.Cos((currentAngle + percent / 2) * 2 * Math.PI)));
            Point midwayPoint = new Point((size * Math.Sin((currentAngle + percent / 2) * 2 * Math.PI)), (-size * Math.Cos((currentAngle + percent / 2) * 2 * Math.PI)));

            // ModelUIElement3D is used so we can listen to events
            ModelUIElement3D pieSlice = new ModelUIElement3D();

            GeometryModel3D gm = new GeometryModel3D();
            DiffuseMaterial dm = new DiffuseMaterial(new SolidColorBrush(clr));
            gm.Material = dm;

            MeshGeometry3D mg = new MeshGeometry3D();
            gm.Geometry = mg;

            int max = (int)(percent * 200);
            mg.Positions.Add(new Point3D(0, 0, 0));
            for (int i = 0; i <= max; i++)
            {
                Point ArcPoint = new Point((size * Math.Sin(currentAngle * 2 * Math.PI)), (-size * Math.Cos(currentAngle * 2 * Math.PI)));
                currentAngle += 0.005;
                mg.Positions.Add(new Point3D(ArcPoint.X, ArcPoint.Y, 0));
            }

            // draw top layer
            max = mg.Positions.Count;
            for (int i = 0; i < max; i++)
                CreateTriangle(mg, 0, i + 1, i + 2);

            // add bottom layer
            for (int i = 0; i < max; i++)
                mg.Positions.Add(new Point3D(mg.Positions[i].X, mg.Positions[i].Y, -3));

            #region draw sides
            // draw straight sides
            CreateTriangle(mg, 0, max + 1, 1);
            CreateTriangle(mg, max, max + 1, 0);
            CreateTriangle(mg, max, max - 1, mg.Positions.Count - 1);
            CreateTriangle(mg, max, 0, max - 1);

            // draw curved sides
            for (int i = 1; i < max; i++)
            {
                CreateTriangle(mg, i, i + max, i + 1);
                CreateTriangle(mg, i + 1, i + max, i + max + 1);
            }
            #endregion

            pieSlice.Model = gm;
            //pieSlice.MouseDown += new MouseButtonEventHandler(pieSlice_MouseDown);

            pieSlice.Transform = new TranslateTransform3D(popOutPoint.X, popOutPoint.Y, 0);
            this._FingerModel.Viewport.Children.Add(pieSlice);

            #region Draw text for each slice
            double left = 0;
            double top = 0;

            TextBlock tb = new TextBlock();
            tb.Text = percent.ToString();
            tb.Measure(new Size(100, 30));
            if (midwayPoint.X < 0)
                left = midwayPoint.X - (tb.DesiredSize.Width / 3.4);
            else
                left = midwayPoint.X + 1;
            if (midwayPoint.Y < 0)
                top = midwayPoint.Y - (tb.DesiredSize.Height / 3.4);
            else
                top = midwayPoint.Y + 1;
            //DrawTextHorizontal((int)left, (int)top, tb.Text, ref height, popOutPoint);
            #endregion

            return pieSlice;
        }

        private static void CreateTriangle(MeshGeometry3D mg, int index0, int index1, int index2)
        {
            mg.TriangleIndices.Add(index0);
            mg.TriangleIndices.Add(index1);
            mg.TriangleIndices.Add(index2);
        }
    }
}
