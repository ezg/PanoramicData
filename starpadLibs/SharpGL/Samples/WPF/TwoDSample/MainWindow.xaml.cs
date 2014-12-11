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
using SharpGL;
using SharpGL.Enumerations;
using SharpGL.SceneGraph;
using SharpGL.SceneGraph.Primitives;
using SharpGL.WPF;

namespace TwoDSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OpenGLControl glControl; 
        private int _windowWidth = -1;
        private int _windowHeight = -1;

        public MainWindow()
        {
            InitializeComponent();

            glControl = new OpenGLControl();
            glControl.RenderContextType = RenderContextType.FBO;
            glControl.DrawFPS = true;
            glControl.OpenGLDraw += openGLControl1_OpenGLDraw;
            glControl.Resized += openGLControl1_Resized;
            glControl.OpenGLInitialized += glControl_OpenGLInitialized;
            this.KeyDown += MainWindow_KeyDown;

            grid1.Children.Add(glControl);
        }

        void CheckFramebufferStatus(OpenGL gl)
        {
            uint status;
            status = gl.CheckFramebufferStatusEXT(OpenGL.GL_FRAMEBUFFER_EXT);
            switch (status)
            {
                case OpenGL.GL_FRAMEBUFFER_COMPLETE_EXT:
                    Console.WriteLine("GOOOOOOODDDD!\n");
                    break;
                case OpenGL.GL_FRAMEBUFFER_UNSUPPORTED_EXT:
                    Console.WriteLine("Unsupported framebuffer format\n");
                    break;
                case OpenGL.GL_FRAMEBUFFER_INCOMPLETE_MISSING_ATTACHMENT_EXT:
                    Console.WriteLine("Framebuffer incomplete, missing attachment\n");
                    break;
                case OpenGL.GL_FRAMEBUFFER_INCOMPLETE_DIMENSIONS_EXT:
                    Console.WriteLine("Framebuffer incomplete, attached images must have same dimensions\n");
                    break;
                case OpenGL.GL_FRAMEBUFFER_INCOMPLETE_FORMATS_EXT:
                    Console.WriteLine("Framebuffer incomplete, attached images must have same format\n");
                    break;
                case OpenGL.GL_FRAMEBUFFER_INCOMPLETE_DRAW_BUFFER_EXT:
                    Console.WriteLine("Framebuffer incomplete, missing draw buffer\n");
                    break;
                case OpenGL.GL_FRAMEBUFFER_INCOMPLETE_READ_BUFFER_EXT:
                    Console.WriteLine("Framebuffer incomplete, missing read buffer\n");
                    break;
                default:
                    Console.WriteLine("!!!!!!!!!!!!!!\n");
                    break;
            }
        }

        uint[] colorBufID = new uint[1];
        uint[] mframeBufID = new uint[1];
        uint[] frameBufID = new uint[1];

        uint[] textureId = new uint[1];

        bool rendered = false;
        
        void glControl_OpenGLInitialized(object sender, OpenGLEventArgs args)
        {
            var gl = args.OpenGL;
            
        }

        private void createFBO(OpenGL gl)
        {
            gl.DeleteTextures(1, textureId);
            gl.DeleteRenderbuffersEXT(1, colorBufID);
            gl.DeleteFramebuffersEXT(1, mframeBufID);
            gl.DeleteFramebuffersEXT(1, frameBufID);

            gl.Enable(OpenGL.GL_DEPTH_TEST);
            gl.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);

            // color texture
            gl.GenTextures(1, textureId);
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, textureId[0]);
            gl.TexImage2D(OpenGL.GL_TEXTURE_2D, 0, OpenGL.GL_RGBA8, _windowWidth, _windowHeight, 0,
                         OpenGL.GL_RGBA, OpenGL.GL_FLOAT, null);
            gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MIN_FILTER, OpenGL.GL_LINEAR);
            gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_MAG_FILTER, OpenGL.GL_LINEAR);
            gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_S, OpenGL.GL_CLAMP_TO_EDGE);
            gl.TexParameter(OpenGL.GL_TEXTURE_2D, OpenGL.GL_TEXTURE_WRAP_T, OpenGL.GL_CLAMP_TO_EDGE);

            
            // multi sampled color buffer
            gl.GenRenderbuffersEXT(1, colorBufID);
            gl.BindRenderbufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, colorBufID[0]);
            if (enable)
            {
                gl.RenderbufferStorageMultisampleEXT(OpenGL.GL_RENDERBUFFER_EXT, 4, OpenGL.GL_RGBA, _windowWidth,
                    _windowHeight);
            }
            else
            {
                gl.RenderbufferStorageEXT(OpenGL.GL_RENDERBUFFER_EXT, OpenGL.GL_RGBA, _windowWidth, _windowHeight);
            }

            // unbind
            gl.BindRenderbufferEXT(OpenGL.GL_RENDERBUFFER_EXT, 0);

            // create fbo for multi sampled content and attach buffers
            gl.GenFramebuffersEXT(1, mframeBufID);
            gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, mframeBufID[0]);
            gl.FramebufferRenderbufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_COLOR_ATTACHMENT0_EXT, OpenGL.GL_RENDERBUFFER_EXT, colorBufID[0]);
            //gl.FramebufferTexture2DEXT(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_COLOR_ATTACHMENT0_EXT, OpenGL.GL_TEXTURE_2D, textureId[0], 0);

            // create final fbo and attach textures
            gl.GenFramebuffersEXT(1, frameBufID);
            gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, frameBufID[0]);
            gl.FramebufferTexture2DEXT(OpenGL.GL_FRAMEBUFFER_EXT, OpenGL.GL_COLOR_ATTACHMENT0_EXT, OpenGL.GL_TEXTURE_2D, textureId[0], 0);

            //CheckFramebufferStatus(gl);

            // unbind
            gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, 0);
        }

        private void openGLControl1_OpenGLDraw(object sender, SharpGL.SceneGraph.OpenGLEventArgs args)
        {
            if (glControl == null)
            {
                return;
            }
            var gl = args.OpenGL;
            double radius = 100;
            double precision = 32;

            // render to the render target texture first
            if (!rendered)
            {
                createFBO(gl);
                gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, mframeBufID[0]);
                {
                    gl.PushAttrib(OpenGL.GL_VIEWPORT_BIT);
                    gl.Viewport(0, 0, _windowWidth, _windowHeight);

                    gl.MatrixMode(OpenGL.GL_MODELVIEW);
                    gl.PushMatrix();
                    gl.LoadIdentity();
                    //gl.Translate(0.0f, 0.0f, -1.5);
                    //gl.Rotatef(teapot_rot, 0.0, 1.0, 0.0);

                    /*gl.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
                    gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
                    gl.Color(0.0, 1.0, 0.0);

                    gl.PointSize(3);
                    gl.Begin(BeginMode.Points);
                    Random r = new Random(0);
                    for (int i = 0; i < 20000; i++)
                    {
                        gl.Vertex(r.NextDouble() * 1000, r.NextDouble() * 1000);
                    }
                    gl.End();
                    gl.PointSize(30);
                    gl.Color(1.0, 0.0, 0.0);
                    gl.Begin(BeginMode.Points);
                    gl.Vertex(0,0);
                    gl.End();*/

                    gl.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
                    gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
                   

                    gl.PushMatrix();
                    gl.Translate(100, 100, 0);
                    gl.Color(0f, 0f, 1f);

                    gl.Begin(OpenGL.GL_POLYGON);
                    for (float i = 0; i < precision; i++)
                    {
                        gl.Vertex(
                            (float)Math.Sin(i * Math.PI * 2f / (float)precision) * radius,
                            (float)Math.Cos(i * Math.PI * 2f / (float)precision) * radius, 0);
                    }
                    gl.End();
                    gl.PopMatrix();

                    gl.PopMatrix();
                    gl.PopAttrib();
                }

                // blit from multisample FBO to final FBO
                gl.BindFramebufferEXT(OpenGL.GL_FRAMEBUFFER_EXT, 0);
                gl.BindFramebufferEXT(OpenGL.GL_READ_FRAMEBUFFER_EXT, mframeBufID[0]);
                gl.BindFramebufferEXT(OpenGL.GL_DRAW_FRAMEBUFFER_EXT, frameBufID[0]);
                gl.BlitFramebufferEXT(0, 0, _windowWidth, _windowHeight, 0, 0, _windowWidth, _windowHeight,
                    OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT, OpenGL.GL_NEAREST);
                gl.BindFramebufferEXT(OpenGL.GL_READ_FRAMEBUFFER_EXT, 0);
                gl.BindFramebufferEXT(OpenGL.GL_DRAW_FRAMEBUFFER_EXT, 0);

                rendered = true;
            }

            // now render to the screen using the texture...
            gl.ClearColor(1f, 1f, 1f, 1.0f);
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);
            gl.MatrixMode(OpenGL.GL_MODELVIEW);
            gl.PushMatrix();
            gl.LoadIdentity();

            // draw textured quad
            gl.BindTexture(OpenGL.GL_TEXTURE_2D, textureId[0]);
            gl.Enable(OpenGL.GL_TEXTURE_2D);
            
            gl.Translate(20f, 20f, 0f);
            
            gl.Color(1.0, 1.0, 1.0);
            gl.Begin(OpenGL.GL_QUADS);
            {
                gl.TexCoord(0, 0); 
                gl.Vertex(0, 0);

                gl.TexCoord(1.0f, 0); 
                gl.Vertex(_windowWidth, 0);

                gl.TexCoord(1.0f, 1.0f);
                gl.Vertex(_windowWidth, _windowHeight);

                gl.TexCoord(0, 1.0f);
                gl.Vertex(0, _windowHeight);
            }
            gl.End();
            gl.Disable(OpenGL.GL_TEXTURE_2D);
            gl.PopMatrix();

            if (enable)
            {
                gl.Enable(OpenGL.GL_MULTISAMPLE);
                gl.Enable(OpenGL.GL_BLEND);
                gl.BlendFunc(OpenGL.GL_SRC_ALPHA, OpenGL.GL_ONE_MINUS_SRC_ALPHA);
                gl.Enable(OpenGL.GL_LINE_SMOOTH);
                gl.Enable(OpenGL.GL_POLYGON_SMOOTH);
                gl.Hint(OpenGL.GL_LINE_SMOOTH_HINT, OpenGL.GL_NICEST);
                gl.Hint(OpenGL.GL_POLYGON_SMOOTH_HINT, OpenGL.GL_NICEST);
            }
           
            gl.PushMatrix();
            gl.Translate(300, 100, 0);
            gl.Color(1f,0f,0f);

            gl.Begin(OpenGL.GL_POLYGON);
            for (float i = 0; i < precision; i++)
            {
                gl.Vertex(
                    (float)Math.Sin(i * Math.PI * 2f / (float)precision) * radius,
                    (float)Math.Cos(i * Math.PI * 2f / (float)precision) * radius, 0);
            }
            gl.End();
            gl.PopMatrix();
        }

        private bool enable = true;

        void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                enable = !enable;
                Console.WriteLine("IsEnabled : " + enable);
            }
        }


        private void openGLControl1_Resized(object sender, SharpGL.SceneGraph.OpenGLEventArgs args)
        {
            var gl = args.OpenGL;

            gl.MatrixMode(OpenGL.GL_PROJECTION);
            gl.LoadIdentity();
            _windowWidth = gl.RenderContextProvider.Width;
            _windowHeight = gl.RenderContextProvider.Height;
            gl.Ortho(0, (float)gl.RenderContextProvider.Width, (float)gl.RenderContextProvider.Height, 0, 0, 1);

            //  Load the modelview.
            gl.MatrixMode(OpenGL.GL_MODELVIEW);
            rendered = false;
        }
    }
}