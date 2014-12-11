using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using starPadSDK.Geom;
using starPadSDK.Utils;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.ComponentModel;
using System.Windows.Shapes;
using InputFramework;
using InputFramework.DeviceDriver;
using InputFramework.WPFDevices;

namespace starPadSDK.Inq
{

    public class InqCanvas : Canvas
    {
        private static Guid REMOVE_RENDERING_FOR_ACTIVE_STROQ = Guid.NewGuid();
        private static Guid DONT_UPDATE_RENDERING_FOR_ACTIVE_STROQ = Guid.NewGuid();
        private static Guid DONT_RAISE_STROQ_COLLECTED_FOR_ACTIVE_STROQ = Guid.NewGuid();

        Dictionary<object, Stroq> _activeStroqs = new Dictionary<object, Stroq>();
        Dictionary<object, bool> _inkEnabledForDevice = new Dictionary<object, bool>();
        StroqCollection _stroqs = new StroqCollection();
        protected bool _drawStroqs = true;
        DrawingAttributes _drawingAttributes;
        StylusPointDescription spd = null;

        protected virtual void PointDownEvent(object sender, RoutedPointEventArgs e)
        {
            if (e.PointEventArgs.DeviceType == DeviceType.Stylus)
            {
                if (e.PointEventArgs.PointEventType == PointEventType.LeftDown)
                {
                    if (_activeStroqs.ContainsKey(e.WPFPointDevice))
                        PointUpEvent(sender, e); // bcz: Argh! somebody stole the Up event from the previous stroke -- so we need to terminate the last stroke before beginning a new one

                    Stroq drawing = null;
                    if (e.PointEventArgs.StylusEventArgs != null)
                    {
                        StylusPointCollection spc = new StylusPointCollection();
                        spc.Add(e.PointEventArgs.StylusEventArgs.GetStylusPoints(this, spd = spc.Description));
                        drawing = new Stroq(spc);
                    }
                    else
                    {
                        StylusPointCollection spc = new StylusPointCollection();
                        foreach (var pt in e.GetStylusPoints(this))
                            spc.Add(new StylusPoint(pt.X, pt.Y, (float)0.5));
                        drawing = new Stroq(spc);
                    }

                    drawing.BackingStroke.DrawingAttributes = _drawingAttributes.Clone();
                    Children.Add(drawing);

                    _activeStroqs.Add(e.WPFPointDevice, drawing);
                    e.Handled = true;

                    if (ActiveStroqDown != null)
                    {
                        ActiveStroqDown(this, new ActiveStroqEventArgs(drawing, e.WPFPointDevice));
                    }
                }
            }
        }

        protected virtual void PointMoveEvent(object sender, RoutedPointEventArgs e) { }

        protected virtual void PointDragEvent(object sender, RoutedPointEventArgs e)
        {
            if (_activeStroqs.ContainsKey(e.WPFPointDevice))
            {
                Stroq drawing = _activeStroqs[e.WPFPointDevice];
                if (e.PointEventArgs.StylusEventArgs != null)
                {
                    foreach (var pt in e.PointEventArgs.StylusEventArgs.GetStylusPoints(this, spd))
                        drawing.Add(pt);
                }
                else
                {
                    foreach (var pt in e.GetStylusPoints(this))
                        drawing.Add(new StylusPoint(pt.X, pt.Y, (float)0.5));
                }

                e.Handled = true;
                if (ActiveStroqMove != null)
                {
                    ActiveStroqMove(this, new ActiveStroqEventArgs(drawing, e.WPFPointDevice));
                }
            }
        }

        protected virtual void PointUpEvent(object sender, RoutedPointEventArgs e)
        {
            if (_activeStroqs.ContainsKey(e.WPFPointDevice))
            {
                Stroq drawing = _activeStroqs[e.WPFPointDevice];

                // remove the stroq rendering
                removeStroqRendering(drawing);

                if (KeepStroqs) _stroqs.Add(drawing);
                _activeStroqs.Remove(e.WPFPointDevice);

                if (!(drawing.Property.Exists(DONT_RAISE_STROQ_COLLECTED_FOR_ACTIVE_STROQ) && (bool)drawing.Property[DONT_RAISE_STROQ_COLLECTED_FOR_ACTIVE_STROQ] == true))
                {
                    RaiseStroqCollectedEvent(drawing, e.WPFPointDevice, false);
                }

                e.Handled = true;

                if (ActiveStroqUp != null)
                {
                    ActiveStroqUp(this, new ActiveStroqEventArgs(drawing, e.WPFPointDevice));
                }
            }
        }

        /*protected override void OnStylusDown(StylusDownEventArgs e) {
            ForceStylusDown(e);
            base.OnStylusDown(e);
        }
        public void ForceStylusDown(StylusDownEventArgs e) {
            //Console.WriteLine("Stylus Down " + e.StylusDevice.Id + " " + this.ActualHeight);
            if (InkEnabled && InkEnabledForDevice(e.StylusDevice) && SharpWhiteBoardDeviceDriver.enabledDevices.Contains(e.Device) &&
                (!SharpWhiteBoardDeviceDriver.Windows8Slate || e.StylusDevice.TabletDevice.Type == TabletDeviceType.Stylus))
            {
                if (_activeStroqs.ContainsKey(e.StylusDevice))
                    StylusUpHandler(e.StylusDevice, e.StylusDevice.TabletDevice.Type, e.Device);

                StylusPointCollection spc = new StylusPointCollection();
                spc.Add(e.GetStylusPoints(this, spd = spc.Description));
                Stroq drawing = new Stroq(spc);

                drawing.BackingStroke.DrawingAttributes = _drawingAttributes.Clone();

                Children.Add(drawing);
                _activeStroqs.Add(e.StylusDevice, drawing);
                Stylus.Capture(this);
                this.LostStylusCapture -=  InqCanvas_LostStylusCapture;
                this.LostStylusCapture += InqCanvas_LostStylusCapture;
                e.Handled = true;

                if (ActiveStroqDown != null)
                {
                    ActiveStroqDown(this, new ActiveStroqEventArgs(drawing, e.Device));
                }
            }
        }

        void InqCanvas_LostStylusCapture(object sender, StylusEventArgs e)
        {
            this.LostStylusCapture -=InqCanvas_LostStylusCapture;
            Console.WriteLine("LostStylusCapture");
        }

        protected override void OnStylusMove(StylusEventArgs e)
        {
            var x = _activeStroqs.ContainsKey(e.StylusDevice);
            var y = InkEnabledForDevice(e.StylusDevice);
            if (InkEnabled && InkEnabledForDevice(e.StylusDevice) && _activeStroqs.ContainsKey(e.StylusDevice) && 
                SharpWhiteBoardDeviceDriver.enabledDevices.Contains(e.Device) &&
                (!SharpWhiteBoardDeviceDriver.Windows8Slate || e.StylusDevice.TabletDevice.Type == TabletDeviceType.Stylus))
            {
                Stroq drawing = _activeStroqs[e.StylusDevice];
                foreach (var pt in e.GetStylusPoints(this, spd))
                    drawing.Add(pt);
                e.Handled = true;

                if (ActiveStroqMove != null)
                {
                    ActiveStroqMove(this, new ActiveStroqEventArgs(drawing, e.Device));
                }
             }

            base.OnStylusMove(e);
        }
        protected override void OnStylusUp(StylusEventArgs e)
        {
            StylusUpHandler(e.StylusDevice, e.StylusDevice.TabletDevice.Type, e.Device);
            base.OnStylusUp(e);
        }

        private void StylusUpHandler(InputDevice dev, TabletDeviceType deviceType, object device)
        {
            if (InkEnabled && InkEnabledForDevice(dev) && _activeStroqs.ContainsKey(dev) && SharpWhiteBoardDeviceDriver.enabledDevices.Contains(dev) &&
                (!SharpWhiteBoardDeviceDriver.Windows8Slate || deviceType == TabletDeviceType.Stylus))
            {
                Stroq drawing = _activeStroqs[dev];

                // remove the stroq rendering
                removeStroqRendering(drawing);

                if (KeepStroqs) _stroqs.Add(drawing);
                _activeStroqs.Remove(dev);

                if (!(drawing.Property.Exists(DONT_RAISE_STROQ_COLLECTED_FOR_ACTIVE_STROQ) && (bool)drawing.Property[DONT_RAISE_STROQ_COLLECTED_FOR_ACTIVE_STROQ] == true))
                {
                    RaiseStroqCollectedEvent(drawing, dev, false);
                }

                Stylus.Capture(null);

                if (ActiveStroqUp != null)
                {
                    ActiveStroqUp(this, new ActiveStroqEventArgs(drawing, device));
                }
            }
        }*/

        private void stroqs_Changed(object sender, StroqCollection.ChangedEventArgs e)
        {
            if (_drawStroqs)
            {
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        foreach (Stroq s in e.NewItems)
                        {
                            Children.Add(s);
                        }
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        foreach (Stroq s in e.OldItems)
                        {
                            removeStroqRendering(s);
                        }
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                        List<StroqElement> toRemove = new List<StroqElement>();
                        foreach (var c in Children)
                        {
                            if (c is StroqElement)
                            {
                                toRemove.Add(c as StroqElement);
                            }
                        }

                        foreach (var se in toRemove)
                        {
                            Children.Remove(se);
                        }

                        if (e.NewItems != null)
                        {
                            foreach (Stroq s in e.NewItems)
                            {
                                Children.Add(s);
                            }
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private void removeStroqRendering(Stroq s)
        {
            foreach (var c in Children)
            {
                if (c is StroqElement && (c as StroqElement).Stroq == s)
                {
                    Children.Remove(c as StroqElement);
                    break;
                }
            }

            if (s.Property.Exists(DONT_UPDATE_RENDERING_FOR_ACTIVE_STROQ) && s.Property[DONT_UPDATE_RENDERING_FOR_ACTIVE_STROQ] != null)
            {
                removeStroqRendering((Stroq)s.Property[DONT_UPDATE_RENDERING_FOR_ACTIVE_STROQ]);
            }
        }

        static InqCanvas()
        {
            // Allow ink to be drawn only within the bounds of the control.
            Type owner = typeof(InqCanvas);
            ClipToBoundsProperty.OverrideMetadata(owner, new FrameworkPropertyMetadata(true));
        }

        public InqCanvas()
            : this(true)
        {
        }

        public InqCanvas(bool keepstroqs)
            : base()
        {
            KeepStroqs = keepstroqs;
            InkEnabled = true;
            _drawingAttributes = new DrawingAttributes();
            _stroqs.Changed += new EventHandler<StroqCollection.ChangedEventArgs>(stroqs_Changed);
            AddHandler(WPFPointDevice.PointDownEvent, new RoutedPointEventHandler(PointDownEvent));
            AddHandler(WPFPointDevice.PointDragEvent, new RoutedPointEventHandler(PointDragEvent));
            AddHandler(WPFPointDevice.PointMoveEvent, new RoutedPointEventHandler(PointMoveEvent));
            AddHandler(WPFPointDevice.PointUpEvent, new RoutedPointEventHandler(PointUpEvent));
        }

        public bool KeepStroqs { get; private set; }

        public bool InkEnabled { get; set; }

        public bool InkEnabledForDevice(object obj)
        {
            if (!_inkEnabledForDevice.ContainsKey(obj))
            {
                _inkEnabledForDevice.Add(obj, true);
            }

            return _inkEnabledForDevice[obj];
        }

        public void SetInkEnabledForDevice(object obj, bool flag)
        {
            if (InkEnabledForDevice(obj) != flag)
            {
                _inkEnabledForDevice[obj] = flag;
            }
        }

        public DrawingAttributes DefaultDrawingAttributes
        {
            get
            {
                return _drawingAttributes;
            }
            set
            {
                _drawingAttributes = value;
            }
        }

        public StroqCollection Stroqs { get { return _stroqs; } }

        /// <summary>
        /// Removes the active Stroq rendering. 
        /// </summary>
        /// <param name="s"></param>
        public void RemoveRenderingForActiveStroq(Stroq s)
        {
            s.Property[REMOVE_RENDERING_FOR_ACTIVE_STROQ] = true;
            removeStroqRendering(s);
        }

        /// <summary>
        /// Does not update the rendering of an active stroq anymore, but keeeps the partial rendering. 
        /// </summary>
        /// <param name="s"></param>
        public void DontUpdateRenderingForActiveStroq(Stroq s)
        {
            foreach (var c in Children)
            {
                if (c is StroqElement && (c as StroqElement).Stroq == s)
                {
                    Children.Remove(c as StroqElement);
                    Stroq clone = s.Clone();
                    s.Property[DONT_UPDATE_RENDERING_FOR_ACTIVE_STROQ] = clone;
                    Children.Add(clone);
                    break;
                }
            }
        }

        /// <summary>
        /// No StroqCollected event will be raised for the current active stroq if this method is called. 
        /// </summary>
        /// <param name="s"></param>
        public void DontRaiseStroqCollectedForActiveStroq(Stroq s)
        {
            s.Property[DONT_RAISE_STROQ_COLLECTED_FOR_ACTIVE_STROQ] = true;
        }

        public class StroqCollectedEventArgs : RoutedEventArgs
        {
            public Stroq Stroq { get; private set; }
            public bool RightButton { get; private set; }
            public object Device { get; private set; }
            public StroqCollectedEventArgs(RoutedEvent revt, Stroq s, object device, bool rightbutton)
                : base(revt)
            {
                Stroq = s;
                RightButton = rightbutton;
                Device = device;
            }
            protected override void InvokeEventHandler(Delegate genericHandler, object genericTarget)
            {
                StroqCollectedEventHandler hdlr = (StroqCollectedEventHandler)genericHandler;
                hdlr(genericTarget, this);
            }
        }

        public delegate void StroqCollectedEventHandler(object sender, StroqCollectedEventArgs e);

        public static readonly RoutedEvent StroqCollectedEvent = EventManager.RegisterRoutedEvent("StroqCollected", RoutingStrategy.Bubble,
            typeof(StroqCollectedEventHandler), typeof(InqCanvas));

        public event StroqCollectedEventHandler StroqCollected
        {
            add { AddHandler(StroqCollectedEvent, value); }
            remove { RemoveHandler(StroqCollectedEvent, value); }
        }

        public void RaiseStroqCollectedEvent(Stroq s, object device, bool rightButton)
        {
            StroqCollectedEventArgs evtargs = new StroqCollectedEventArgs(StroqCollectedEvent, s, device, rightButton);
            RaiseEvent(evtargs);
        }

        public class ActiveStroqEventArgs : RoutedEventArgs
        {
            public Stroq Stroq { get; private set; }
            public object Device { get; private set; }
            public ActiveStroqEventArgs(Stroq s, object device)
            {
                Stroq = s;
                Device = device;
            }
        }

        public delegate void ActiveStroqDownHandler(object sender, ActiveStroqEventArgs e);
        public event ActiveStroqDownHandler ActiveStroqDown;

        public delegate void ActiveStroqUpHandler(object sender, ActiveStroqEventArgs e);
        public event ActiveStroqUpHandler ActiveStroqUp;

        public delegate void ActiveStroqMoveHandler(object sender, ActiveStroqEventArgs e);
        public event ActiveStroqMoveHandler ActiveStroqMove;
    }
}
