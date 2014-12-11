using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;
using System.Windows.Shapes;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq.BobsCusps;
using System.Windows.Threading;
using InputFramework.WPFDevices;
using System.Windows.Data;

namespace starPadSDK.Inq
{
    // defines the basic interface for all Gestures
    public interface Gesture
    {
        Gesturizer.Result Process(Gesturizer g, object device, Stroq s, bool onlyPartial, List<Stroq> prev);
        void Reset(Gesturizer g);
        void Fire(Stroq[] strokes, object device);
        void Clear();
    }
    // defines an abstract one stroke Gesture
    public abstract class OneStrokeGesture : Gesture
    {
        public OneStrokeGesture() { }
        public abstract void Fire(Stroq[] strokes, object device);
        public abstract bool Test(Stroq s, object device);
        public virtual void Reset(Gesturizer g) { }
        public virtual void Clear() { }
        public Gesturizer.Result Process(Gesturizer g, object device, Stroq s, bool onlyPartial, List<Stroq> prev)
        {
            Reset(g);
            if (onlyPartial)
                return Gesturizer.Result.Unrecognized;
            if (Test(s, device))
                return Gesturizer.Result.Recognized;

            return Gesturizer.Result.Unrecognized;
        }
    }
    // defines an abstract two stroke crop G
    public abstract class TwoStrokeGesture : Gesture
    {
        TextBox _text = null;
        void AddText(Gesturizer g, double x, double y)
        {
            _text = new TextBox();
            _text.Text = Prompt;
            _text.IsHitTestVisible = false;
            _text.BorderBrush = Brushes.Transparent;
            _text.RenderTransform = new TranslateTransform(x, y);
            g.Children.Add(_text);
        }
        void ClearText(Gesturizer g)
        {
            if (_text != null)
                g.Children.Remove(_text);
            _text = null;
        }

        public delegate void FiredHandler(Gesture g, Stroq[] strokes);
        public TwoStrokeGesture() { }
        public bool OneStroke { get; set; }
        public virtual void Clear() { }
        public void Reset(Gesturizer g) { ClearText(g); }
        public abstract string Prompt { get; }
        public abstract void Fire(Stroq[] strokes, object device);
        public abstract bool Test1(Stroq s, object device);
        public abstract bool Test2(Stroq s, Stroq prev, object device);
        public Gesturizer.Result Process(Gesturizer g, object device, Stroq s, bool onlyPartial, List<Stroq> prev)
        {
            if (OneStroke)
            {
                if (onlyPartial)
                    return Gesturizer.Result.Unrecognized;
                if (Test1(s, device))
                    return Gesturizer.Result.Recognized;

                return Gesturizer.Result.Unrecognized;
            }
            else
            {
                Reset(g);
                if (onlyPartial && prev.Count == 1 && Test1(prev[0], device) && Test2(s, prev[0], device))
                    return Gesturizer.Result.Recognized;
                if (!onlyPartial && Test1(s, device))
                {
                    if (g.Canvas != null)
                        AddText(g, s.GetBounds().Left, s.GetBounds().Top);
                    return Gesturizer.Result.Partial;
                }
            }

            return Gesturizer.Result.Unrecognized;
        }
    }
    // defines an abstract button gesture 
    public abstract class ButtonGesture : Gesture
    {
        const double FADE_DURATION = 5; //in seconds
        const double TICKS_PER_SECOND = 20; //ticks per second
        const double TOTAL_TICKS = (int)(FADE_DURATION * TICKS_PER_SECOND);

        Gesturizer _gesturizer = null;
        Guid _gesturePartThatFiredGuid = Guid.NewGuid();
        List<ButtonGesturePart> _gestureParts = new List<ButtonGesturePart>();

        Rectangle _fireByTouchTapHitArea = null;
        ButtonGesturePart _fireByTouchTapGesturePart = null;

        Canvas _buttonPanel = new Canvas();
        Dictionary<ButtonGestureDialog, ButtonGesturePart> _gestureDialogs = new Dictionary<ButtonGestureDialog, ButtonGesturePart>();
        DispatcherTimer _timer = new DispatcherTimer();
        int _ticksSoFar = 0;

        public ButtonGesture(Gesturizer gesturizer)
        {
            _gesturizer = gesturizer;
            _timer.Interval = TimeSpan.FromSeconds(1.0 / TICKS_PER_SECOND);
            _timer.Tick += new EventHandler(timerTick_Handler);
        }

        public abstract bool CommonTest(Stroq stroq, object device);

        public Gesturizer.Result Process(Gesturizer g, object device, Stroq s, bool onlyPartial, List<Stroq> prev)
        {
            // test for complete gesture 
            if (onlyPartial && prev.Count == 1 && CommonTest(prev[0], device))
            {
                bool isTap = s.IsTap();
                foreach (ButtonGestureDialog bgd in _gestureDialogs.Keys)
                {
                    Rct r = new Rct(0, 0, bgd.ActualWidth, bgd.ActualHeight);
                    if ((r.IntersectsWith(new Rct(g.TranslatePoint(s[-1], bgd), new Vec(1, 1))) && isTap) || // button got tapped.
                        _gestureDialogs[bgd].Test2(s, device)) // or second test fired 
                    {
                        ButtonGesturePart part = _gestureDialogs[bgd];
                        s.Property[_gesturePartThatFiredGuid] = part;
                        return Gesturizer.Result.Recognized;
                    }
                }
            }

            Reset(g);

            // test for partial / first part of the gesture. 
            if (!onlyPartial && CommonTest(s, device))
            {
                bool aPartRecognized = false;
                g.Children.Add(_buttonPanel);
                if (InvertScale)
                {
                    ConstantScreenSizeBinding.CreateBinding(g, _buttonPanel, new Point(s[-1].X, s[-1].Y), 0, FrameworkElement.RenderTransformProperty);
                }

                Point startingPoint = new Point(0, 0);
                foreach (ButtonGesturePart bgp in _gestureParts)
                {
                    if (bgp.Test(s, device))
                    {
                        _gestureDialogs.Add(addGestureDialog(g, bgp, startingPoint, bgp.Label), bgp);
                        aPartRecognized = true;
                        startingPoint.Y += ButtonYOffset;
                        if (bgp.FireByTouchTap && _fireByTouchTapGesturePart == null)
                        {
                            _fireByTouchTapGesturePart = bgp;
                        }
                    }
                }
                if (_fireByTouchTapGesturePart != null)
                {
                    Rct bounds = s.GetBounds();
                    _fireByTouchTapHitArea = new Rectangle();
                    _fireByTouchTapHitArea.Width = bounds.Width;
                    _fireByTouchTapHitArea.Height = bounds.Height;
                    _fireByTouchTapHitArea.Fill = new SolidColorBrush(Color.FromArgb(0, 255, 0, 0));
                    _fireByTouchTapHitArea.RenderTransform = new TranslateTransform(bounds.Left, bounds.Top);
                    _fireByTouchTapHitArea.AddHandler(WPFPointDevice.PointDownEvent, new RoutedPointEventHandler(fireByTouchTapHitArea_PointDownEvent));
                    g.Children.Insert(0, _fireByTouchTapHitArea);
                }
                if (aPartRecognized)
                {
                    _ticksSoFar = 0;
                    _timer.Start();
                    return Gesturizer.Result.Partial;
                }
            }

            return Gesturizer.Result.Unrecognized;
        }
        public void Fire(Stroq[] strokes, object device)
        {
            if (strokes.Count() > 0)
            {
                Stroq s = strokes.Last();
                if (s.Property.Exists(_gesturePartThatFiredGuid))
                {
                    ButtonGesturePart bgp = (ButtonGesturePart)s.Property[_gesturePartThatFiredGuid];
                    _timer.Stop();
                    bgp.Fire(strokes, device);
                }
            }
        }

        public void Reset(Gesturizer g)
        {
            foreach (ButtonGestureDialog bgd in _gestureDialogs.Keys)
            {
                removeGestureDialog(g, bgd);
            }
            g.Children.Remove(_buttonPanel);
            if (_fireByTouchTapHitArea != null)
            {
                g.Children.Remove(_fireByTouchTapHitArea);
                _fireByTouchTapGesturePart = null;
                _fireByTouchTapHitArea = null;
            }
            foreach (ButtonGesturePart bgp in _gestureParts)
            {
                bgp.Reset();
            }
            _gestureDialogs.Clear();
        }
        public void AddButtonGestureParts(ButtonGesturePart gesturePart)
        {
            _gestureParts.Add(gesturePart);
        }
        public virtual void Clear() { }
        public virtual double ButtonYOffset { get { return 31; } }
        public virtual bool InvertScale { get { return true; } }
        private ButtonGestureDialog addGestureDialog(Gesturizer gesturizer, ButtonGesturePart buttonGesturePart, Point sourcePoint, String gestureName)
        {
            ButtonGestureDialog bdg = new ButtonGestureDialog(gestureName, buttonGesturePart.Draggable);
            bdg.RenderTransform = new TranslateTransform(sourcePoint.X, sourcePoint.Y);
            bdg.InteractionStart += new ButtonGestureDialog.InteractionStartHandler(buttonGestureDialog_InteractionStart);
            bdg.InteractionEnd += new ButtonGestureDialog.InteractionEndHandler(buttonGestureDialog_InteractionEnd);
            bdg.InteractionMove += new ButtonGestureDialog.InteractionMoveHandler(buttonGestureDialog_InteractionMove);
            bdg.Tapped += new ButtonGestureDialog.TappedHandler(buttonGestureDialog_Tapped);

            _buttonPanel.Children.Add(bdg);
            return bdg;
        }

        public void NotifyDeviceMoved(Pt point)
        {
            bool over = false;
            foreach (ButtonGestureDialog bgd in _gestureDialogs.Keys)
            {
                Rct r = new Rct(0, 0, bgd.ActualWidth, bgd.ActualHeight);
                if (r.IntersectsWith(new Rct(_gesturizer.TranslatePoint(point, bgd), new Vec(1, 1))))
                {
                    over = true;
                    break;
                }
            }

            if (over)
            {
                _timer.Stop();
                _ticksSoFar = 0;
                foreach (ButtonGestureDialog bgd2 in _gestureDialogs.Keys)
                {
                    bgd2.Opacity = 1.0;
                }
            }
            else
            {
                _timer.Start();
            }
        }

        void buttonGestureDialog_InteractionStart(object sender, RoutedPointEventArgs e)
        {
            ButtonGestureDialog bdg = (ButtonGestureDialog)sender;

            // if there is a dragfeedback, attach it. 
            FrameworkElement dragFeedback = _gestureDialogs[bdg].GetDragFeedback(_gesturizer.Pending.First());
            if (dragFeedback != null)
            {
                bdg.SetDragFeedback(dragFeedback);
                _gesturizer.Children.Add(dragFeedback);
                dragFeedback.RenderTransform = new MatrixTransform(((Mat)bdg.RenderTransform.Value) * Mat.Translate(new Vec(0, -60)));
                bdg.Visibility = Visibility.Hidden;
            }
            _timer.Stop();

            // remove other dialogs
            List<ButtonGestureDialog> toRemove = new List<ButtonGestureDialog>();
            foreach (ButtonGestureDialog d in _gestureDialogs.Keys)
            {
                if (d != bdg)
                {
                    toRemove.Add(d);
                }
            }
            foreach (ButtonGestureDialog d in toRemove)
            {
                removeGestureDialog(_gesturizer, d);
                _gestureDialogs.Remove(d);
            }
        }
        void buttonGestureDialog_Tapped(object sender, RoutedPointEventArgs e)
        {
            List<Stroq> stroqs = new List<Stroq>();
            stroqs.AddRange(_gesturizer.Pending);
            Point p = e.GetPosition((FrameworkElement)_gesturizer);
            stroqs.Add(new Stroq(p));

            ButtonGestureDialog bdg = (ButtonGestureDialog)sender;
            ButtonGesturePart bgp = _gestureDialogs[sender as ButtonGestureDialog];

            bgp.Fire(stroqs.ToArray(), e.WPFPointDevice);
            _gesturizer.Reset(false);
        }
        void buttonGestureDialog_InteractionMove(object sender, RoutedPointEventArgs e)
        {
            ButtonGestureDialog bdg = (ButtonGestureDialog)sender;
            ButtonGesturePart bgp = _gestureDialogs[sender as ButtonGestureDialog];
            bgp.CheckDropAllowed();
        }
        void buttonGestureDialog_InteractionEnd(object sender, RoutedPointEventArgs e)
        {
            ButtonGesturePart bgp = _gestureDialogs[sender as ButtonGestureDialog];
            List<Stroq> stroqs = new List<Stroq>();
            stroqs.AddRange(_gesturizer.Pending);
            Point p = e.GetPosition((FrameworkElement)_gesturizer);
            stroqs.Add(new Stroq(p));

            bgp.Fire(stroqs.ToArray(), e.WPFPointDevice);
            _gesturizer.Reset(false);
        }

        void fireByTouchTapHitArea_PointDownEvent(Object sender, RoutedPointEventArgs e)
        {
            //if (e.DeviceType != InputFramework.DeviceType.MultiTouch)
            //    return;

            List<Stroq> stroqs = new List<Stroq>();
            stroqs.AddRange(_gesturizer.Pending);
            Point p = e.GetPosition((FrameworkElement)_gesturizer);
            stroqs.Add(new Stroq(p));

            _fireByTouchTapGesturePart.Fire(stroqs.ToArray(), e.WPFPointDevice);
            _gesturizer.Reset(false);
        }

        private void removeGestureDialog(Gesturizer gesturizer, ButtonGestureDialog buttonGestureDialog)
        {
            _buttonPanel.Children.Remove(buttonGestureDialog);
        }
        private void timerTick_Handler(object sender, EventArgs e)
        {
            _ticksSoFar++;
            double fraction = 1.0 - (_ticksSoFar / TOTAL_TICKS);
            foreach (ButtonGestureDialog bgd in _gestureDialogs.Keys)
            {
                bgd.Opacity = fraction;
            }
            if (_ticksSoFar == TOTAL_TICKS)
            {
                _timer.Stop();
                _gesturizer.Reset();
            }
        }
    }

    [ValueConversion(typeof(Transform), typeof(double))]
    public class ConstantScreenSizeBinding : IMultiValueConverter
    {
        Point _offset;
        double _scrOffset;
        ConstantScreenSizeBinding(Point offset, double scrOffset) { _offset = offset; _scrOffset = scrOffset; }
        #region IValueConverter Members
        public object Convert(object[] transforms, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var tt = (transforms.Last() as Transform);
            TransformGroup tg = new TransformGroup();
            Vector scale = new Vector(1 / tt.Value.M11, 1 / tt.Value.M22);
            for (int i = 0; i < transforms.Length - 1; i++)
            {
                var t2 = (transforms[i] as Transform);
                scale = new Vector(scale.X / t2.Value.M11, scale.Y / t2.Value.M22);
            }
            tg.Children.Add(new ScaleTransform(scale.X, scale.Y));
            tg.Children.Add(new TranslateTransform(_offset.X, _offset.Y));
            //tg.Children.Add(new TranslateTransform(0, _scrOffset / tt.Value.M11));
            return tg;
        }
        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
        #endregion
        public static void CreateBinding(FrameworkElement bubble, FrameworkElement target, Point offset, double scrOffset, DependencyProperty property)
        {
            var mbind = new MultiBinding();
            var ele = bubble;
            while (ele != null)
            {
                Binding b = new Binding();
                b.Source = ele;
                b.Path = new PropertyPath(FrameworkElement.RenderTransformProperty);
                mbind.Bindings.Add(b);
                ele = ele.Parent as FrameworkElement;
            }
            mbind.Converter = new ConstantScreenSizeBinding(offset, scrOffset);
            BindingOperations.SetBinding(target, property, mbind);
        }
    }

    public abstract class ButtonGesturePart
    {
        protected Guid _guid = Guid.NewGuid();
        public abstract bool Test(Stroq stroq, object device);
        public virtual bool Test2(Stroq stroq, object device) { return false; }
        public abstract void Fire(Stroq[] strokes, object device);
        public abstract string Label { get; }
        public virtual bool Draggable { get { return false; } }
        public virtual bool FireByTouchTap { get { return false; } }
        public virtual FrameworkElement GetDragFeedback(Stroq stroq) { return null; }
        public virtual void Reset() { }
        public virtual void CheckDropAllowed()
        {
        }
        protected void SetStroqProperty(Stroq stroq, object obj)
        {
            stroq.Property[_guid] = obj;
        }
        protected object GetStroqProperty(Stroq stroq)
        {
            if (stroq.Property.Exists(_guid))
            {
                return stroq.Property[_guid];
            }
            else
            {
                return false;
            }
        }

    }

    // defines an abstract parameterized flick command
    public abstract class FlickCommand : TwoStrokeGesture
    {
        string _chars;
        public FlickCommand(string chars) { _chars = chars; }
        public override bool Test1(Stroq s, object device) { return s.IsFlick(); }
        public override bool Test2(Stroq s, Stroq prev, object device) { return s.IsChar(_chars) && prev.BackingStroke.HitTest(s.Select((Pt p) => (Point)p), new RectangleStylusShape(1, 1)); }
        public override string Prompt { get { return "Write Mnemonic"; } }
    }
    public abstract class SymbolCommand : OneStrokeGesture
    {
        string _chars;
        public SymbolCommand(string chars) { _chars = chars; }
        public override bool Test(Stroq s, object device) { return s.IsChar(_chars); }
    }

    public class Gesturizer : Canvas
    {
        public enum Result
        {
            Recognized,
            Partial,
            Unrecognized
        };

        List<Gesture> _gestures = new List<Gesture>();
        List<Stroq> _pending = new List<Stroq>();
        Canvas _canvas = null;
        void fireGesture(Gesture g, object device)
        {
            if (GestureRecognizedEvent != null)
                GestureRecognizedEvent(this, device, g, _pending.ToArray());
            Console.WriteLine("Fired " + g.ToString());
            g.Fire(_pending.ToArray(), device);
            _pending.Clear();
            Children.Clear();
        }

        /// <summary>
        /// </summary>
        /// <param name="sender">the Gesturizer recognizer that generate this event</param>
        /// <param name="g">the recognized Gesture</param>
        /// <param name="strokes">the strokes that comprise the Gesture</param>
        public delegate void GestureRecognizedHandler(object sender, object device, Gesture g, Stroq[] strokes);
        /// <summary>
        /// </summary>
        /// <param name="sender">the Gesturizer recognizer that generate this event</param>
        /// <param name="s">the Stroq that is not a gesture</param>
        /// <returns>whether the callback function has handled the event</returns>
        public delegate bool StrokeUnrecognizedHandler(object sender, object device, Stroq s);
        /// <summary>
        /// Event called when any Gesture has been recognized
        /// </summary>
        public event GestureRecognizedHandler GestureRecognizedEvent;
        /// <summary>
        /// Event called when a stroke has been determined not to be a gesture stroke
        /// </summary>
        public event StrokeUnrecognizedHandler StrokeUnrecognizedEvent;
        public Gesturizer() { }
        public Gesturizer(Canvas c) { _canvas = c; }
        /// <summary>
        /// Canvas that gestures are being collected on and where feedback is displayed
        /// </summary>
        public Canvas Canvas
        {
            get { return _canvas; }
            set { _canvas = value; }
        }
        public Gesture[] Gestures { get { return _gestures.ToArray(); } }
        public List<Stroq> Pending
        {
            get { return _pending; }
            set { _pending = value; }
        }

        public void Clear() { _gestures.Clear(); }
        public void Add(Gesture g) { _gestures.Add(g); }
        public void Rem(Gesture g) { _gestures.Remove(g); }
        public void Reset(bool fireUnrecognizedEvent = true)
        {
            foreach (Gesture g in _gestures)
                g.Reset(this);
            // the current policy is that we can have no more than two-stroke gestures.
            // so if we have a partial match or no match at all, we need to flush out
            // all the pending strokes (which for now can be at most 1 stroke).
            foreach (Stroq ps in _pending)
                if (StrokeUnrecognizedEvent != null && fireUnrecognizedEvent)
                    StrokeUnrecognizedEvent(this, null /* bcz: is this right??? */ , ps);
            _pending.Clear();
            Children.Clear();
        }

        /// <summary>
        /// The temporary color of strokes that may turn into multi-stroke Gestures
        /// </summary>
        public Color GestureStrokeIntermediateColor = Colors.Red;

        /// <summary>
        /// The gesturizer can be notified if there has been some movement of a pointer device. 
        /// This is used to update the fade out timer of the GestureButtons. 
        /// </summary>
        /// <param name="point"></param>
        public void NotifyDeviceMoved(Pt point)
        {
            foreach (var g in Gestures)
            {
                if (g is ButtonGesture)
                {
                    ((ButtonGesture)g).NotifyDeviceMoved(point);
                }
            }
        }

        /// <summary>
        /// Process a stroke sequentially through each gesture recognizer, terminating only
        /// if a gesture is completely recognized.  An event is fired when a gesture is matched.
        /// The method returns whether the stroke completed a gesture, is potentially part of a
        /// multi-stroke gesture, or if it is not part of any gesture.
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public Result Process(Stroq s, object device)
        {
            //  try to find a completed gesture
            foreach (Gesture g in Gestures)
            {
                Result r = g.Process(this, device, s, true, _pending);
                if (r == Result.Recognized)
                {
                    _pending.Add(s);
                    fireGesture(g, device);
                    return Result.Recognized;
                }
            }

            // pending gesture strokes -> real strokes if they weren't completed
            // the current policy is that we can have no more than two-stroke gestures.
            // so if we have a partial match or no match at all, we need to flush out
            // all the pending strokes (which for now can be at most 1 stroke).
            foreach (Stroq ps in _pending)
                if (StrokeUnrecognizedEvent != null)
                    StrokeUnrecognizedEvent(this, device, ps);
            _pending.Clear();

            // try to find a new (or new partial)gesture
            foreach (Gesture g in Gestures)
            {
                Result r = g.Process(this, device, s, false, _pending);
                if (r != Result.Unrecognized)
                {
                    // save the stroke if a multi-stroke gesture is pending.
                    s.BackingStroke.DrawingAttributes.Color = GestureStrokeIntermediateColor;
                    _pending.Add(s);
                    if (r == Result.Recognized)
                        fireGesture(g, device);
                    else
                        Children.Add(s);
                    return r;
                }
            }

            // gesture stroke -> real stroke if nothing matched it
            Children.Clear();
            if (StrokeUnrecognizedEvent != null)
                StrokeUnrecognizedEvent(this, device, s);
            return Result.Unrecognized;
        }

    }
}
