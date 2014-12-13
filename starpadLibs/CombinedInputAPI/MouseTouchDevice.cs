using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CombinedInputAPI
{
    public class MouseTouchDevice : TouchDevice
    {
        private static MouseTouchDevice _device;
        private static string _activeFrozenDevice = "";
        private static Dictionary<string, MouseTouchDevice> _frozenDevices = new Dictionary<string, MouseTouchDevice>();

        private DispatcherTimer _moveTimer;

        public Point Position { get; set; }
        public StylusPointCollection StylusPoints { get; set; }
        public bool IsStylus { get; set; }
        private bool froze = false;

        public MouseTouchDevice(int deviceId) :
            base(deviceId)
        {
            Position = new Point();
            _moveTimer = new DispatcherTimer();
            _moveTimer.Interval = new TimeSpan(0, 0, 0, 0, 20);
            _moveTimer.Tick += _moveTimer_Tick;
            _moveTimer.Start();
        }

        public static void RegisterEvents(FrameworkElement root)
        {
            root.PreviewMouseDown += MouseDown;
            root.PreviewMouseMove += MouseMove;
            root.PreviewMouseUp += MouseUp;
            root.LostMouseCapture += LostMouseCapture;
            root.MouseLeave += MouseLeave;

            root.KeyDown += root_KeyDown;
        }

        static void root_KeyDown(object sender, KeyEventArgs e)
        {
            if (Properties.Settings.Default.EnableKeyboardMultitouchSimulator)
            {
                if (_device != null && e.Key.ToString().ToCharArray().Count() > 1 && char.IsDigit(e.Key.ToString().ToCharArray()[1]))
                {
                    MouseTouchDevice clone = _device;
                    _device = null;
                    if (_frozenDevices.ContainsKey(e.Key.ToString()))
                    {
                        return;
                    }
                    _frozenDevices.Add(e.Key.ToString(), clone);
                    clone.froze = true;
                    _activeFrozenDevice = e.Key.ToString();
                }
                else if (e.Key.ToString().ToCharArray().Count() > 1 && char.IsDigit(e.Key.ToString().ToCharArray()[1]))
                {
                    _activeFrozenDevice = e.Key.ToString();
                }
                else if (_frozenDevices.ContainsKey(_activeFrozenDevice))
                {
                    MouseTouchDevice activeDevice = _frozenDevices[_activeFrozenDevice];
                    if (e.Key == Key.W)
                    {
                        activeDevice.Position = new Point(activeDevice.Position.X, activeDevice.Position.Y - 1);
                        activeDevice.ReportMove();
                    }
                    else if (e.Key == Key.S)
                    {
                        activeDevice.Position = new Point(activeDevice.Position.X, activeDevice.Position.Y + 1);
                        activeDevice.ReportMove();
                    }
                    else if (e.Key == Key.A)
                    {
                        activeDevice.Position = new Point(activeDevice.Position.X - 1, activeDevice.Position.Y);
                        activeDevice.ReportMove();
                    }
                    else if (e.Key == Key.D)
                    {
                        activeDevice.Position = new Point(activeDevice.Position.X + 1, activeDevice.Position.Y);
                        activeDevice.ReportMove();
                    }
                }
                if (e.Key == Key.Q)
                {
                    foreach (var dev in _frozenDevices.Values)
                    {
                        dev.ReportUp();
                        dev.Deactivate();
                    }
                    _frozenDevices.Clear();
                }
            }
        }

        private static void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null || e.RightButton == MouseButtonState.Pressed)
            {
                return;
            }
            if (_device != null &&
                _device.IsActive)
            {
                _device.ReportUp();
                _device.Deactivate();
                _device = null;
            }
            _device = new MouseTouchDevice(e.MouseDevice.GetHashCode());
            _device.IsStylus = ((Keyboard.GetKeyStates(Key.LeftCtrl) & KeyStates.Down) != 0) || e.StylusDevice != null;
            _device.SetActiveSource(e.MouseDevice.ActiveSource);
            _device.Position = e.GetPosition(null);
            if (e.StylusDevice != null)
            {
                StylusPointCollection spc = new StylusPointCollection();
                _device.StylusPoints = e.StylusDevice.GetStylusPoints(null, spc.Description);
            }
            _device.Activate();
            _device.ReportDown();
        }

        void _moveTimer_Tick(object sender, EventArgs e)
        {
            ReportMove();
        }

        private static void MouseMove(object sender, MouseEventArgs e)
        {
            if (e.StylusDevice != null)
            {
                return;
            }
            if (_device != null &&
                _device.IsActive)
            {
                _device.Position = e.GetPosition(null);
                if (e.StylusDevice != null)
                {
                    StylusPointCollection spc = new StylusPointCollection();
                    _device.StylusPoints = e.StylusDevice.GetStylusPoints(null, spc.Description);
                }
                _device.ReportMove();
            }
        }

        private static void MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.StylusDevice != null)
            {
                return;
            }
            LostMouseCapture(sender, e);
        }

        static void LostMouseCapture(object sender, MouseEventArgs e)
        {
            if (_device != null &&
                _device.IsActive)
            {
                //try
                {
                    _device.Position = e.GetPosition(null);
                    _device.ReportUp();
                    _device.Deactivate();
                    _device._moveTimer.Stop();
                    _device = null;
                }
                //catch (Exception ee)
                {

                }
            }
        }

        static void MouseLeave(object sender, MouseEventArgs e)
        {
            LostMouseCapture(sender, e);
        }

        public override TouchPointCollection GetIntermediateTouchPoints(IInputElement relativeTo)
        {
            return new TouchPointCollection();
        }

        public override TouchPoint GetTouchPoint(IInputElement relativeTo)
        {
            Point point = Position;
            if (relativeTo != null)
            {
                try
                {
                    point = this.ActiveSource.RootVisual.TransformToDescendant((Visual)relativeTo).Transform(Position);
                }
                catch (Exception e)
                {

                }
            }

            Rect rect = new Rect(point, new Size(1, 1));

            return new TouchPoint(this, point, rect, TouchAction.Move);
        }
    }
}
