using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using FarseerPhysics.Collision;
using FarseerPhysics.Common;
using FarseerPhysics.Dynamics;
using FarseerPhysics.Dynamics.Joints;
using FarseerPhysics.Factories;
using Microsoft.Xna.Framework;
using Poly2Tri.Triangulation.Delaunay.Sweep;
using starPadSDK.AppLib;
using starPadSDK.Inq;
using starPadSDK.WPFHelp;
using Matrix = System.Windows.Media.Matrix;
using PanoramicData.view.physics;
using PanoramicData.controller.view;
using PanoramicData.model.view_new;
using PanoramicData.view.vis;

namespace PanoramicData.controller.physics
{
    public class PhysicsController
    {
        private static PhysicsController _instance;
        private static Grid _root = null;
        private bool _renderDebugView = false;

        private World _world;
        private WPFDebugView _wpfDebugView;
        private Canvas _worldDebugCanvas;
        private readonly DispatcherTimer _physicsTimer = new DispatcherTimer();
        private Matrix _real2PhysicsMatrixScale = new Matrix();
        private Matrix _real2PhysicsMatrixTranslate = new Matrix();
        private Matrix _physics2RealMatrixScale = new Matrix();
        private Matrix _physics2RealMatrixTranslate = new Matrix();

        private Dictionary<MovableElement, PhysicalObject> _physicalObjects = new Dictionary<MovableElement, PhysicalObject>();
        private Dictionary<MovableElement, FixedMouseJoint> _mouseJoints = new Dictionary<MovableElement, FixedMouseJoint>();

        private PhysicsController()
        {
            setupPhysicsSystem();
        }

        private void setupPhysicsSystem()
        {
            if (_world != null)
            {
                _world.Clear();
            }
            _world = new World(new Microsoft.Xna.Framework.Vector2(0, 0));

            if (_renderDebugView)
            {
                if (_worldDebugCanvas != null)
                {
                    _root.Children.Remove(_worldDebugCanvas);
                }
                _worldDebugCanvas = new Canvas();
                _worldDebugCanvas.HorizontalAlignment = HorizontalAlignment.Stretch;
                _worldDebugCanvas.VerticalAlignment = VerticalAlignment.Stretch;
                _worldDebugCanvas.IsHitTestVisible = false;

                _worldDebugCanvas.Background = new SolidColorBrush(Color.FromArgb(10, 255, 0, 0));
                _root.Children.Add(_worldDebugCanvas);
                _wpfDebugView = new WPFDebugView(_worldDebugCanvas, _world);
            }

            double scaleFactor = Math.Max(_root.ActualWidth / WPFDebugView.WIDTH, _root.ActualHeight / WPFDebugView.HEIGHT);

            Matrix matrix = new Matrix();
            matrix.SetIdentity();
            matrix.Scale(scaleFactor, scaleFactor);
            matrix.Translate(0, 0);
            if (_renderDebugView)
            {
                _wpfDebugView.Transform = matrix;
            }

            _real2PhysicsMatrixScale.SetIdentity();
            _real2PhysicsMatrixScale.Scale(1.0 / scaleFactor, 1.0 / scaleFactor);

            _real2PhysicsMatrixTranslate.SetIdentity();
            _real2PhysicsMatrixTranslate.Translate(-25000, -25000);

            _physics2RealMatrixScale.SetIdentity();
            _physics2RealMatrixScale.Scale(scaleFactor, scaleFactor);

            _physics2RealMatrixTranslate.SetIdentity();
            _physics2RealMatrixTranslate.Translate(+25000, +25000);

            // setup physics update timer
            _physicsTimer.Interval = TimeSpan.FromSeconds(1.0 / 60.0);
            _physicsTimer.Tick += new EventHandler(physicsTimerTick_Handler);
            _physicsTimer.Start();
        }

        private void physicsTimerTick_Handler(object sender, EventArgs e)
        {
            _world.Step((float) (1.0/60.0));

            Matrix matrix = new Matrix();
            Matrix inqSceneMatrix = (MainViewController.Instance.InkableScene.RenderTransform as MatrixTransform).Matrix;
            matrix.SetIdentity();
            double scaleFactor = Math.Max(_root.ActualWidth/WPFDebugView.WIDTH, _root.ActualHeight/WPFDebugView.HEIGHT);
            matrix.Scale(scaleFactor, scaleFactor);
            matrix.Translate(0, 0);

            if (inqSceneMatrix.M11 == 1.0)
            {
                matrix.OffsetX = inqSceneMatrix.OffsetX;
                matrix.OffsetY = inqSceneMatrix.OffsetY;
            }
            else
            {
                var pp = inqSceneMatrix.Transform(new Point(25000, 25000));
                matrix.OffsetX = -(25000 - pp.X);
                matrix.OffsetY = -(25000 - pp.Y);
                matrix.M11 = matrix.M11 * inqSceneMatrix.M11;
                matrix.M22 = matrix.M22 * inqSceneMatrix.M22;
            }

            if (_renderDebugView)
            {
                _wpfDebugView.Transform = matrix;
                _worldDebugCanvas.Children.Clear();
                _wpfDebugView.DrawDebugData();
            }

            foreach (var element in _physicalObjects.Keys)
            {
                if (element is VisualizationContainerView && !((element as VisualizationContainerView).DataContext as VisualizationViewModel).IsTemporary)
                {
                    Vector2 vec2 = _physicalObjects[element].OuterBody.Position;
                    Vector newPos = _physics2RealMatrixScale.Transform(new Vector(vec2.X, vec2.Y));
                    element.SetPosition(new Point(newPos.X + 25000 - element.GetSize().X / 2.0, newPos.Y + 25000 - element.GetSize().Y / 2.0));
                }
            }

            if (_mouseJoints.Keys.Count == 2)
            {
                if (_mouseJoints.Keys.ToList()[0].IsDescendantOf(MainViewController.Instance.InkableScene) &&
                    _mouseJoints.Keys.ToList()[1].IsDescendantOf(MainViewController.Instance.InkableScene))
                {
                    var b1 = _mouseJoints.Keys.ToList()[0].GetBounds(MainViewController.Instance.InkableScene).GetPolygon();
                    var b2 = _mouseJoints.Keys.ToList()[1].GetBounds(MainViewController.Instance.InkableScene).GetPolygon();

                    var dist = b1.Distance(b2);
                    Console.WriteLine(dist);
                }
            }
        }

        public static void SetRootCanvas(Grid root)
        {
            _root = root;
        }

        public static PhysicsController Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new PhysicsController();
                }
                return _instance;
            }
        }

        public void DragStart(MovableElement element, Point currentPos)
        {
            if (_mouseJoints.ContainsKey(element))
            {
                _world.RemoveJoint(_mouseJoints[element]);
                _mouseJoints.Remove(element);
            }
            if (!_physicalObjects.ContainsKey(element))
            {
                return;
            }

            currentPos = _real2PhysicsMatrixTranslate.Transform(currentPos);
            currentPos = _real2PhysicsMatrixScale.Transform(currentPos);
            Vector2 p = new Vector2((float) currentPos.X, (float) currentPos.Y);

            // Make a small box.
            AABB aabb;
            Vector2 d = new Vector2(0.001f, 0.001f);
            aabb.LowerBound = p - d;
            aabb.UpperBound = p + d;

            Fixture savedFixture = null;

            // Query the world for overlapping shapes.
            _world.QueryAABB(
                fixture =>
                {
                    Body body = fixture.Body;
                    if (body.BodyType == BodyType.Dynamic)
                    {
                        bool inside = fixture.TestPoint(ref p);
                        if (inside)
                        {
                            savedFixture = fixture;

                            // We are done, terminate the query.
                            return false;
                        }
                    }

                    // Continue the query.
                    return true;
                }, ref aabb);

            if (savedFixture != null)
            {
                Body body = savedFixture.Body;
                FixedMouseJoint mouseJoint = new FixedMouseJoint(body, p);
                mouseJoint.MaxForce = 100000.0f * body.Mass;
                _world.AddJoint(mouseJoint);
                body.Awake = true;

                _mouseJoints.Add(element, mouseJoint);
            }
        }

        public void DragMove(MovableElement element, Point currentPos)
        {
            FixedMouseJoint joint = null;
            _mouseJoints.TryGetValue(element, out joint);

            if (joint != null)
            {
                currentPos = _real2PhysicsMatrixTranslate.Transform(currentPos);
                currentPos = _real2PhysicsMatrixScale.Transform(currentPos);
                joint.WorldAnchorB = new Vector2((float)currentPos.X, (float)currentPos.Y);
            }
        }

        public void DragEnd(MovableElement element, Point currentPos)
        {
            if (_mouseJoints.ContainsKey(element))
            {
                _world.RemoveJoint(_mouseJoints[element]);
                _mouseJoints.Remove(element);
            }
        }

        public void AddPhysicalObject(MovableElement element, bool isUnderInteraction = false)
        {
            if (!_physicalObjects.ContainsKey(element))
            {
                _physicalObjects.Add(element, createPhysicalObject(element, isUnderInteraction));
            }
        }

        public void RemovePhysicalObject(MovableElement element)
        {
            if (_physicalObjects.ContainsKey(element))
            {
                PhysicalObject po = _physicalObjects[element];
                if (po.AnchorBody != null)
                {
                    _world.RemoveBody(po.AnchorBody);
                }
                if (po.OuterBody != null && po.OuterBody != po.AnchorBody)
                {
                    _world.RemoveBody(po.OuterBody);
                }
                _physicalObjects.Remove(element);
            }
        }

        public void UpdatePhysicalObject(MovableElement element, bool isUnderInteraction)
        {
            RemovePhysicalObject(element);
            AddPhysicalObject(element, isUnderInteraction);
        }

        public List<MovableElement> GetAllMovableElements()
        {
            return _physicalObjects.Keys.ToList();
        }

        private Body createSmallAnchorBody(PhysicalObject po)
        {
            Body anchorBody = BodyFactory.CreateCircle(_world, (float)_real2PhysicsMatrixScale.M11 * 5.0f, 1f);
            anchorBody.BodyType = BodyType.Dynamic;
            anchorBody.FixedRotation = false;
            anchorBody.Restitution = .0f;
            anchorBody.LinearDamping = 5f;
            anchorBody.Friction = .2f;
            anchorBody.Position = po.AnchorBody.Position;

            // glue to physical object's anchor
            JointFactory.CreateRevoluteJoint(_world, po.AnchorBody, anchorBody, new Vector2(0, 0));
            po.AnchorBodies.Add(anchorBody);

            return anchorBody;
        }

        private PhysicalObject createPhysicalObject(MovableElement element, bool isUnderInteraction = false)
        {
            Point center = element.GetPosition() + element.GetSize() / 2.0;
            center = _real2PhysicsMatrixTranslate.Transform(center);
            center = _real2PhysicsMatrixScale.Transform(center);
            float borderThickness = (float) _real2PhysicsMatrixScale.M11*2.0f;
            PanoramicData.utils.Vector2 dim = element.GetSize();

            float width = (float) _real2PhysicsMatrixScale.M11*(float) dim.X;
            float height = (float) _real2PhysicsMatrixScale.M11*(float) dim.Y;

            PhysicalObject po = new PhysicalObject();
            if (element.Type == MovableElementType.Rect)
            {
                /*
                List<Vertices> borders = new List<Vertices>(4);

                borders.Add(PolygonTools.CreateRectangle(width/2, borderThickness, new Vector2(0, (height/2)), 0));
                borders.Add(PolygonTools.CreateRectangle(borderThickness, height/2, new Vector2(-(width/2), 0), 0));
                borders.Add(PolygonTools.CreateRectangle(width/2, borderThickness, new Vector2(0, -height/2), 0));
                borders.Add(PolygonTools.CreateRectangle(borderThickness, height/2, new Vector2((width/2), 0), 0));

                Body border = BodyFactory.CreateCompoundPolygon(_world, borders, 1);
                border.Position = new Vector2((float) center.X, (float) center.Y);
                foreach (Fixture t in border.FixtureList)
                {
                    t.CollisionCategories = Category.Cat2;
                    t.CollidesWith = Category.Cat2;
                }

                Fixture fix = FixtureFactory.AttachRectangle(width, height, 1, new Vector2(), border);
                fix.CollisionCategories = Category.Cat1;
                fix.CollidesWith = Category.Cat1;
                */
                Body border = BodyFactory.CreateRectangle(_world, width, height, 1);
                border.Position = new Vector2((float)center.X, (float)center.Y);
                border.BodyType = BodyType.Dynamic;//isUnderInteraction ? BodyType.Kinematic : BodyType.Dynamic;
                border.FixedRotation = true;
                border.Restitution = .0f;
                border.LinearDamping = 5f;
                border.Friction = .2f;

                po.OuterBody = border;
            }
            else if (element.Type == MovableElementType.Circle)
            {
                Body circle = BodyFactory.CreateCircle(_world, (float)Math.Max(width, height) / 2.0f, 1f);
                circle.BodyType = BodyType.Dynamic;//isUnderInteraction ? BodyType.Kinematic : BodyType.Dynamic;
                circle.FixedRotation = true;
                circle.Restitution = .0f;
                circle.LinearDamping = 5f;
                circle.Friction = .2f;
                circle.Position = new Vector2((float)center.X, (float)center.Y);

                po.OuterBody = circle;
            }

            if (element.HasEnclosedAnchor)
            {
                Body innerBody = BodyFactory.CreateCircle(_world,
                    Math.Min(width, height)/4.0f, 1);
                innerBody.FixedRotation = false;
                innerBody.Position = new Vector2((float) center.X, (float) center.Y);
                innerBody.BodyType = BodyType.Dynamic;//isUnderInteraction ? BodyType.Kinematic : BodyType.Dynamic;
                innerBody.Restitution = .0f;
                innerBody.LinearDamping = 5f;
                innerBody.Friction = .2f;
                innerBody.CollisionCategories = Category.Cat2;
                innerBody.CollidesWith = Category.Cat2;
                po.AnchorBody = innerBody;
            }
            else
            {
                //po.AnchorBody = po.OuterBody;
            }

            return po;
        }
    }

    public class PhysicalObject
    {
        public PhysicalObject()
        {
            AnchorBodies = new List<Body>();
        }

        public Body OuterBody { get; set; }
        public Body AnchorBody { get; set; }

        public List<Body> AnchorBodies { get; set; } 
    }

    public class PhysicalJointObject
    {
        public MovableElement Source { get; set; }
        public MovableElement Destination { get; set; }
        public Body SourceAnchor { get; set; }
        public Body DestinationAnchor { get; set; }
        public Joint Joint { get; set; }
    }
}
