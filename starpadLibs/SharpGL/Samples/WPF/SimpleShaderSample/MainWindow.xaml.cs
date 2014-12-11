using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
using SharpGL;
using SharpGL.Enumerations;
using SharpGL.SceneGraph.Primitives;
using SharpGL.SceneGraph.Shaders;
using SharpGL.SceneGraph;

namespace SimpleShaderSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenGLControl_Resized(object sender, OpenGLEventArgs args)
        {
            //  Get the OpenGL instance.
            var gl = args.OpenGL;

            //  Create an orthographic projection.
            gl.MatrixMode(MatrixMode.Projection);
            gl.LoadIdentity();
            gl.Ortho(0, glControl.ActualWidth, glControl.ActualHeight, 0, -10, 10);

            //  Back to the modelview.
            gl.MatrixMode(MatrixMode.Modelview);
        }

        private void OpenGLControl_OpenGLDraw(object sender, OpenGLEventArgs args)
        {
            OpenGL gl = args.OpenGL;	
            
            // Clear The Screen And The Depth Buffer
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
            program.Push(gl, null);
            gl.Color(1.0,1.0,0.0);
            gl.PointSize(3);
            gl.Begin(BeginMode.Points);
            Random r = new Random();
            gl.Vertex(r.NextDouble()* 300 ,r.NextDouble()* 300);
            gl.End();


            
            gl.PointSize(3.0f);
            var vertexHandle = GCHandle.Alloc(vertexArrayValues, GCHandleType.Pinned);
            gl.VertexAttribPointer(0, 3, OpenGL.GL_FLOAT, false, 0, IntPtr.Add(vertexHandle.AddrOfPinnedObject(), 0));
            gl.EnableVertexAttribArray(0);

            gl.DrawArrays(OpenGL.GL_POINTS, 0, 3);


            program.Pop(gl, null);
        }

        float rotation = 0;

        private float[] vertexArrayValues;
        private void OpenGLControl_OpenGLInitialized(object sender, OpenGLEventArgs args)
        {
            OpenGL gl = args.OpenGL;
         
        
            //  Create a vertex shader.
            VertexShader vertexShader = new VertexShader();
            vertexShader.CreateInContext(gl);
            vertexShader.SetSource(
                "void main()" + Environment.NewLine +
                "{" + Environment.NewLine +
                "gl_Position = ftransform();" + Environment.NewLine +
                "}" + Environment.NewLine);

            //  Create a fragment shader.
            FragmentShader fragmentShader = new FragmentShader();
            fragmentShader.CreateInContext(gl);
            fragmentShader.SetSource(
                "void main()" + Environment.NewLine +
                "{" + Environment.NewLine +
                "gl_FragColor = vec4(1.0,0.0,0.0,1.0);" + Environment.NewLine +
                "}" + Environment.NewLine);

            //  Compile them both.
            vertexShader.Compile();
            fragmentShader.Compile();

            //  Build a program.
            program.CreateInContext(gl);

            //  Attach the shaders.
            program.AttachShader(vertexShader);
            program.AttachShader(fragmentShader);
            program.Link();

             Random r = new Random();
            int n = 100;
            vertexArrayValues = new float[n * 3];
            uint counter = 0;
            for (uint i = 0; i < n; i++)
            {
                vertexArrayValues[counter] = (float) r.NextDouble() * 100.0f;
                vertexArrayValues[counter++] = (float) r.NextDouble() * 100.0f;
                vertexArrayValues[counter++] = 0.0f;
            }
        }

        ShaderProgram program = new ShaderProgram();
    }
}
