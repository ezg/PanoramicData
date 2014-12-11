using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using starPadSDK.Inq.MSInkCompat;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Runtime.Serialization.Formatters.Binary;
using starPadSDK.Inq.BobsCusps;
using starPadSDK.WPFHelp;
using System.IO;
using starPadSDK.CharRecognizer;
using starPadSDK.DollarRecognizer;
using InputFramework.DeviceDriver;
using InputFramework.WPFDevices;

namespace starPadSDK.AppLib {
    /// <summary>
    /// CommandSets represent the interaction logic of a drawing surface.  This logic can be a combination of 
    /// Gestures and CommandEditors.  Gestures recognize and act upon Stroq input before that input is committed
    /// to the InqScene.  Editors, on the other hand react to Stroqs after they have been committed to the InqScene.
    /// So, when a Stroq is drawn, it is first processed by the Gesture recognition engine.  If no Gesture is recognized,
    /// then the Stroq triggers a NonGestureEvent.  If the application chooses to add the Stroq to the InqScene, then 
    /// all active CommandEditors will be notified in order to further update the InqScene or their recognition state.
    /// </summary>
    public class CommandSet
    {
        public abstract class MultitouchGesture
        {
            protected abstract void test(object sender, RoutedPointEventArgs e);
            public MultitouchGesture(InqScene scene) {
                scene.AddHandler(WPFPointDevice.PointDownEvent, new RoutedPointEventHandler(test));
            }
            public virtual  void Clear(InqScene page) { page.RemoveHandler(WPFPointDevice.PointDownEvent, new RoutedPointEventHandler(test)); }
        }
        public class PauseData
        {
            static Guid pauseData = new Guid("{25BB9734-E4C1-470d-B3A9-3F876B36B3AF}");
            public static Guid PauseDataGuid { get { return pauseData; } }
            public Pt    Touch { get; set; }
            public Stroq Stroq { get; set; }
            public PauseData(Stroq s, Pt touch) { Stroq = s; Touch = touch; }
        }
        /// <summary>
        /// CommandEditors are objects that process Stroqs that have already been added to the InqScene.
        /// For example, when a Stroq is added to the scene, an Enabled MathEditor would be notified and it could
        /// update its math recognition.  Alternatively, a curve editor might decide to join the new Stroq to an existing
        /// Stroq.
        /// Editors are designed to be combined with Gestures, to avoid reinventing the wheel. The math editor 
        /// need only be concerned with recognizing math, handling scribble deletion or lasso selection, for example,
        /// can be done by having those gestures active at the same time.  
        /// </summary>
        public class CommandEditor {
            protected InqScene _can;
            bool can_StroqAddedEvent(Stroq s) {
                if (!Enabled)
                    return false;
                 return stroqAdded(s);
             }
            void can_StroqsClearedEvent(Stroq[] s) {
                if (Enabled)
                    stroqsRemoved(s);
                stroqsRemoved(s);
             }
            void can_StroqRemovedEvent(Stroq s) {
                stroqRemoved(s);
            }
            virtual protected bool stroqAdded(Stroq s) { return false; }
            virtual protected void stroqRemoved(Stroq a) { }
            virtual protected void stroqsRemoved(Stroq[] s) { }

            public bool     Enabled { get; set; }
            public InqScene Scene   { get { return _can; } }
            public CommandEditor(InqScene can) {
                _can = can;
                Enabled = true;
                _can.StroqPreAddEvent += new InqScene.StroqFilterHandler(can_StroqAddedEvent);
                _can.StroqRemovedEvent += new InqScene.StroqHandler(can_StroqRemovedEvent);
                _can.StroqsClearedEvent += new InqScene.StroqsHandler(can_StroqsClearedEvent);
            }

        }
        protected InqScene            _can = null;
        protected Gesturizer          _gest = null;   // the gesture collection object
        protected Gesturizer          _right = null;
        protected Gesturizer          _pause = null;
        protected List<MultitouchGesture> _multitouchGestures = null;
        protected List<CommandEditor> _editors = new List<CommandEditor>();
        /// <summary>
        /// creates a new text box if the last stroke drawn was an insertion caret.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        virtual protected void keyDown(object sender, System.Windows.Input.KeyEventArgs e) {
            // See if this keyDown event follows a partial Text insertion gesture.  If so, then we wanto recognize the gesture 
            // and create a TextBox
            if (_gest.Pending.Count > 0 && _gest.Pending[0].IsInsertion()) {
                // force 'text' gesture to be recognized
                new TextCommand(_can).Fire(_gest.Pending.ToArray(), null);
                // cleanup the recognizer since we didn't process the gesture the "normal" way
                _gest.Pending.Clear();
                _gest.Reset();
                // re-enable all widgets that were disabled by the first part of the text box gesture in StroqCollected
                // this would normally have been done in StroqCollected by the second stroke of the gesture in StroqCollected.
                foreach (FrameworkElement fe in _can.Elements)
                    fe.IsHitTestVisible = true;
            }
        }

        /// <summary>
        /// tests the input stroke to see if it's part of a gesture.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        virtual protected void stroqCollected(object sender, InqCanvas.StroqCollectedEventArgs e) {
            Gesturizer.Result res = Gesturizer.Result.Unrecognized;
            if (e.RightButton)
                res = _right.Process(e.Stroq, e.Device);
            else
                res = _gest.Process(e.Stroq, e.Device);

            bool enableWidgets = _gest.Pending.Count == 0;
            // disable all widgets (ie, TextBox's) if we have a partial gesture so that the next gesture stroke can be drawn
            // over the widget if necessary.
            // The next stroke will re-enable these widgets as will a keyDown that creates a text gesture
            
            // EZ: I commented the next 3 lines out, had problems with my button gestures.
            //foreach (FrameworkElement fe in _can.Elements)
            //    fe.IsHitTestVisible = enableWidgets;
            //_can.Feedback(e.Device).IsHitTestVisible = enableWidgets;

            if (!enableWidgets)
                _can.SetInkEnabledForDevice(e.Device,  true);

            //e.Handled = res != Gesturizer.Result.Unrecognized;
            e.Handled = true;

        }
        /// <summary>
        /// processes an input Stroq that is definitively not part of a Gesture
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="s"></param>
        virtual protected bool gestureUnrecognizedEvent(object sender, object device, Stroq s) {
            //if (_can.Selection != null && !_can.Selection.Empty)  // Policy: ignore unrecognized gesture strokes when there is a selection
            //    return true;

            s.BackingStroke.DrawingAttributes.Color = _can.DefaultDrawingAttributes.Color;  // restore Color of Stroq

            bool handled = false;
            if (NonGestureEvent != null)
                handled |= NonGestureEvent(this, device, s);
            if (!handled)
                _can.AddWithUndo(s);
            return true;
        }

        void can_DeviceMovedEvent(Pt point)
        {
            _gest.NotifyDeviceMoved(point);
        }

        /// <summary>
        /// Initialize Gesture sets for application modes
        /// </summary>
        /// <param name="curveGestures"></param>
        virtual protected void InitGestures()           { foreach (Gesture g in _gest.Gestures)  g.Clear(); _gest.Clear(); }
        virtual protected void InitRightGestures()      { foreach (Gesture g in _right.Gestures) g.Clear(); _right.Clear(); }
        virtual protected void InitPauseGestures()      { foreach (Gesture g in _pause.Gestures) g.Clear(); _pause.Clear(); }
        virtual protected void InitMultitouchGestures() { foreach (MultitouchGesture g in _multitouchGestures) g.Clear(_can); _multitouchGestures.Clear(); }

        public event Gesturizer.StrokeUnrecognizedHandler NonGestureEvent;
        /// <summary>
        /// A default command set to associate with a canvas that includes generically useful and representative gestures.
        /// </summary>
        /// <param name="can"></param>
        public CommandSet(InqScene can) { Scene = can;  }

        public virtual void InitGestureSets()
        {
            InitGestures();
            InitPauseGestures();
            InitRightGestures();
            InitMultitouchGestures();
        }
        /// <summary>
        /// Tests if a Stroq, tagged w/ pause data (like a touch contact), matches a pause gesture (ie, Touch-activated pen gestures).
        /// </summary>
        /// <param name="s"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public bool StroqPaused(Stroq s, object device)
        {
            Gesturizer.Result result = _pause.Process(s, device);
            bool enableWidgets = _pause.Pending.Count == 0;
            // disable all widgets (ie, TextBox's) if we have a partial gesture so that the next gesture stroke can be drawn
            // over the widget if necessary.
            // The next stroke will re-enable these widgets as will a keyDown that creates a text gesture
            foreach (FrameworkElement fe in _can.Elements)
                fe.IsHitTestVisible = enableWidgets;
            _can.Feedback(device).IsHitTestVisible = enableWidgets;

            if (!enableWidgets)
                _can.SetInkEnabledForDevice(device, true);
            return result == Gesturizer.Result.Recognized;
        }
        public List<CommandEditor> Editors           { get { return _editors; }
                                                       set { _editors = value; InitGestureSets(); }
        }
        public Gesturizer          GestureRecognizer { get { return _gest;  } }
        public InqScene            Scene {
            get { return _can; }
            set {
                _can = value;
                _right = new Gesturizer(_can);
                _pause = new Gesturizer(_can);
                _gest = new Gesturizer(_can);
                _multitouchGestures = new List<MultitouchGesture>();
                _can.Children.Insert(1, _pause);
                _can.Children.Insert(1, _right);
                _can.Children.Insert(1, _gest); // add _gest to Children list so that its feedback can be displayed

                _can.Loaded += new RoutedEventHandler((object sender, RoutedEventArgs e)=> InitGestureSets());

                _can.KeyDown += new System.Windows.Input.KeyEventHandler(keyDown);
                _can.StroqCollected += new InqScene.StroqCollectedEventHandler(stroqCollected);
                _gest.StrokeUnrecognizedEvent += new Gesturizer.StrokeUnrecognizedHandler(gestureUnrecognizedEvent);
                _can.DeviceMovedEvent += new InqScene.DeviceMovedHandler(can_DeviceMovedEvent);
            }
        }

        public virtual void Dispose()
        {
        }
    }
}
