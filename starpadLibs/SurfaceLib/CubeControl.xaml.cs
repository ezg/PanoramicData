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

namespace WpfCubeControl
{
    /// <summary>
    /// Interaction logic for CubeControl.xaml
    /// </summary>
    public partial class CubeControl : UserControl
    {
        private Color _cubeColor = Colors.AliceBlue;

        public CubeControl()
        {
            InitializeComponent();
        }

        public Color CubeColor
        {
            get { return _cubeColor; }
            set { _cubeColor = value; }
        }

        public void RotateX(double angle)
        {
            rotX.Angle = angle;
        }

        public void RotateY(double angle)
        {
            rotY.Angle = angle;
        }

        public void RotateZ(double angle)
        {
            rotZ.Angle = angle;
        }

        public Viewport3D Viewport
        {
            get
            {
                return mainViewport;
            }
        }

        public double scaleY
        {
            get
            {
                return scale.ScaleY;
            }

            set
            {
                scale.ScaleY = value;
            }
        }

        public double scaleX
        {
            get
            {
                return scale.ScaleX;
            }

            set
            {
                scale.ScaleX = value;
            }
        }

        public double scaleZ
        {
            get
            {
                return scale.ScaleZ;
            }

            set
            {
                scale.ScaleZ = value;
            }
        }

        ModelVisual3D _Cube = null;

        public ModelVisual3D Model
        {
            get
            {
                return _Cube;
            }
        }

        public void Render()
        {
            
            CubeBuilder cubeBuilder = new CubeBuilder(_cubeColor);
            _Cube = cubeBuilder.Create();
            mainViewport.Children.Add(_Cube);

        }
    }
}
