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
using System.Windows.Interop;
using System.Runtime.InteropServices;
using starPadSDK.Inq;
using starPadSDK.Geom;
using InputFramework;
using InputFramework.DeviceDriver;
using InputFramework.WPFDevices;

namespace starPadSDK.WPFHelp
{
    public class IOGearDeviceDriver : PointDeviceDriver
    {

        public override void Dispose() {   }

        private void window_PreviewMouseDown(Object sender, Point windowPoint) {
            PointEventArgs pointEventArgs = new PointEventArgs();

            initPointEvent(ref pointEventArgs, windowPoint);

            pointEventArgs.PointEventType = PointEventType.LeftDown;

            OnPointEvent(pointEventArgs);
        }

        private void window_PreviewMouseUp(Object sender, Point windowPoint) {
            PointEventArgs pointEventArgs = new PointEventArgs();

            initPointEvent(ref pointEventArgs, windowPoint);

            pointEventArgs.PointEventType = PointEventType.LeftUp;

            OnPointEvent(pointEventArgs);
        }


        private void window_PreviewMouseMove(Object sender, Point windowPoint, bool drag) {
            PointEventArgs pointEventArgs = new PointEventArgs();

            initPointEvent(ref pointEventArgs, windowPoint);

            if (drag) {
                pointEventArgs.PointEventType = PointEventType.Drag;
            }
            else {
                pointEventArgs.PointEventType = PointEventType.Move;
            }

            OnPointEvent(pointEventArgs);
        }

        private void initPointEvent(ref PointEventArgs pointEventArgs, Point windowPoint) {
            pointEventArgs.DeviceUID = new DeviceUID() { DeviceID = 0, DeviceDriverID = this.DeviceDriverID };
            pointEventArgs.DeviceType = DeviceType.Unknown;
            pointEventArgs.Handled = false;

            pointEventArgs.InBetweenPointsScreen = new PressurePoint[1];
            pointEventArgs.InBetweenPointsScreen[0] = new PressurePoint(mWindow.PointToScreen(windowPoint));
            // Mouse cursor position in screen coordinates
            pointEventArgs.PointScreen = new PressurePoint(mWindow.PointToScreen(windowPoint));
        }
        static Window mWindow;
        static HwndSourceHook calibHook = null;
        static Pt[] dots = null;
        static Canvas calibDots = new Canvas();
        static Window calibWindow = new Window();
        static bool penCalibState = false;
        static List<Pt> _calibPts = new List<Pt>();
        static List<IntPtr> iogears = new List<IntPtr>();
        static bool penState = false;
        static Window _eventWindow = null;
        static Point eventWindowScreenOrigin = new Point();
        static List<InqCanvas> _displays = new List<InqCanvas>();
        const string IOGearHdr = "\\\\?\\HID#VID_0E20";
        public delegate void MouseMovedHandler(Point where);

        static Point rawPtFromInput(byte[] input)
        {
            Point p = new Point((input[2 + 2] << 8) | input[1 + 2], (input[4 + 2] << 8) | input[3 + 2]);
            if (p.X > Math.Pow(2, 15))
                p.X = p.X - 65536;
            return p;
        }
        static void testForIOGearPen(Window window, ComSupport.RAWINPUTDEVICELIST r, string hidDeviceName, ComSupport.RID_DEVICE_INFO_HID rdih)
        {
            if (hidDeviceName.StartsWith(IOGearHdr))
            {//"\\\\?\\HID#WACF004"
                /*Trace.Assert(r.dwType == RIM_TYPEHID);
                Trace.Assert(rdih.usUsagePage == 13);
                Trace.Assert(rdih.usUsage == 2);*/
                // register for it
                ComSupport.RAWINPUTDEVICE rid = new ComSupport.RAWINPUTDEVICE();
                rid.usUsagePage = rdih.usUsagePage;
                rid.usUsage = rdih.usUsage;
                rid.dwFlags = 0;
                rid.hwndTarget = new WindowInteropHelper(window).Handle;
                bool result = ComSupport.RegisterRawInputDevices(new ComSupport.RAWINPUTDEVICE[] { rid }, 1, (uint)Marshal.SizeOf(rid));

                iogears.Add(r.hDevice);
            }
        }
        static void showCalibWindow()
        {
            calibDots.ClipToBounds = false;
            calibWindow.Content = calibDots;

            Ellipse m1 = new Ellipse();
            m1.Width = 3;
            m1.Height = 3;
            m1.Fill = Brushes.Green;
            m1.RenderTransform = new TranslateTransform(500, 200);
            calibDots.Children.Add(m1);
            m1 = new Ellipse();
            m1.Width = 3;
            m1.Height = 3;
            m1.Fill = Brushes.Green;
            m1.RenderTransform = new TranslateTransform(800, 200);
            calibDots.Children.Add(m1);
            m1 = new Ellipse();
            m1.Width = 3;
            m1.Height = 3;
            m1.Fill = Brushes.Green;
            m1.RenderTransform = new TranslateTransform(500, 500);
            calibDots.Children.Add(m1);


            calibDots.Cursor = Cursors.Cross;
            calibWindow.Loaded += new RoutedEventHandler(calibWindow_Loaded);
            calibWindow.ShowDialog();

        }
        static bool calibratePenUp(IntPtr hwnd)
        {
            if (penCalibState)
            {
                penCalibState = false;
                if (_calibPts.Count == 3)
                {
                    return true;
                }
            }
            return false;
        }

        static void calibratePenDown(Point p)
        {
            if (!penCalibState)
            {
                penCalibState = true;
                if (dots == null)
                    dots = new Pt[] { calibDots.Children[0].PointToScreen(new Point(0,0)), 
                                      calibDots.Children[1].PointToScreen(new Point(0,0)), 
                                      calibDots.Children[2].PointToScreen(new Point(0,0)) };
                _calibPts.Add(new Pt(p.X, p.Y));
                Console.WriteLine("Got Calib Pt #" + _calibPts.Count + " = " + _calibPts[_calibPts.Count - 1]);
                (calibDots.Children[_calibPts.Count - 1] as Ellipse).Fill = Brushes.Red;
            }
        }

        static void setupDevices(Window window)
        {
            foreach (ComSupport.RAWINPUTDEVICELIST r in ComSupport.GetRawInputDeviceList())
            {
                IntPtr devinfo;
                ComSupport.RID_DEVICE_INFOheader rdi = ComSupport.GetDeviceInfo(r, out devinfo);

                switch (rdi.dwType)
                {
                    case ComSupport.RIM_TYPEHID:
                        ComSupport.RID_DEVICE_INFO_HID rdih = (ComSupport.RID_DEVICE_INFO_HID)
                            Marshal.PtrToStructure(new IntPtr(devinfo.ToInt32() + Marshal.SizeOf(typeof(ComSupport.RID_DEVICE_INFOheader))), typeof(ComSupport.RID_DEVICE_INFO_HID));
                        //textsb.AppendFormat("HID: vendor {0:X}, product {1:X}, version {2}, usage page {3}, usage {4}\r\n", rdih.dwVendorID, rdih.dwProductID, rdih.dwVersionNumber, rdih.usUsagePage, rdih.usUsage);

                        testForIOGearPen(window,r, ComSupport.GetDeviceName(r), rdih);

                        break;
                    #region otherdevices
                    case ComSupport.RIM_TYPEKEYBOARD:
                        ComSupport.RID_DEVICE_INFO_KEYBOARD rdik = (ComSupport.RID_DEVICE_INFO_KEYBOARD)
                            Marshal.PtrToStructure(new IntPtr(devinfo.ToInt32() + Marshal.SizeOf(typeof(ComSupport.RID_DEVICE_INFOheader))), typeof(ComSupport.RID_DEVICE_INFO_KEYBOARD));
                        //textsb.AppendFormat("Kbd: type/sub {0}/{1}, scan code mode {2}, {3} fn(s), {4} indicator(s), {5} key(s)\r\n",  rdik.dwType, rdik.dwSubType, rdik.dwKeyboardMode, rdik.dwNumberOfFunctionKeys, rdik.dwNumberOfIndicators, rdik.dwNumberOfKeysTotal);
                        break;
                    case ComSupport.RIM_TYPEMOUSE:
                        ComSupport.RID_DEVICE_INFO_MOUSE rdim = (ComSupport.RID_DEVICE_INFO_MOUSE)
                            Marshal.PtrToStructure(new IntPtr(devinfo.ToInt32() + Marshal.SizeOf(typeof(ComSupport.RID_DEVICE_INFOheader))), typeof(ComSupport.RID_DEVICE_INFO_MOUSE));
                        //textsb.AppendFormat("Mouse: id {0}, {1} button(s), {2} samples/second\r\n", rdim.dwId, rdim.dwNumberOfButtons, rdim.dwSampleRate);
                        break;
                    default:
                        throw new ApplicationException("Hey!");
                    #endregion
                }
                Marshal.FreeHGlobal(devinfo);
            }
        }

        static void calibWindow_Loaded(object sender, RoutedEventArgs e)
        {
            setupDevices(calibWindow);

            foreach (ComSupport.RAWINPUTDEVICELIST r in ComSupport.GetRawInputDeviceList())
            {
                IntPtr devinfo;
                ComSupport.RID_DEVICE_INFOheader rdi = ComSupport.GetDeviceInfo(r, out devinfo);

                switch (rdi.dwType)
                {
                    case ComSupport.RIM_TYPEHID:
                        ComSupport.RID_DEVICE_INFO_HID rdih = (ComSupport.RID_DEVICE_INFO_HID)
                            Marshal.PtrToStructure(new IntPtr(devinfo.ToInt32() + Marshal.SizeOf(typeof(ComSupport.RID_DEVICE_INFOheader))), typeof(ComSupport.RID_DEVICE_INFO_HID));
                        //textsb.AppendFormat("HID: vendor {0:X}, product {1:X}, version {2}, usage page {3}, usage {4}\r\n", rdih.dwVendorID, rdih.dwProductID, rdih.dwVersionNumber, rdih.usUsagePage, rdih.usUsage);

                        testForIOGearPen(calibWindow, r, ComSupport.GetDeviceName(r), rdih);

                        break;
                    #region otherdevices
                    case ComSupport.RIM_TYPEKEYBOARD:
                        ComSupport.RID_DEVICE_INFO_KEYBOARD rdik = (ComSupport.RID_DEVICE_INFO_KEYBOARD)
                            Marshal.PtrToStructure(new IntPtr(devinfo.ToInt32() + Marshal.SizeOf(typeof(ComSupport.RID_DEVICE_INFOheader))), typeof(ComSupport.RID_DEVICE_INFO_KEYBOARD));
                        //textsb.AppendFormat("Kbd: type/sub {0}/{1}, scan code mode {2}, {3} fn(s), {4} indicator(s), {5} key(s)\r\n",  rdik.dwType, rdik.dwSubType, rdik.dwKeyboardMode, rdik.dwNumberOfFunctionKeys, rdik.dwNumberOfIndicators, rdik.dwNumberOfKeysTotal);
                        break;
                    case ComSupport.RIM_TYPEMOUSE:
                        ComSupport.RID_DEVICE_INFO_MOUSE rdim = (ComSupport.RID_DEVICE_INFO_MOUSE)
                            Marshal.PtrToStructure(new IntPtr(devinfo.ToInt32() + Marshal.SizeOf(typeof(ComSupport.RID_DEVICE_INFOheader))), typeof(ComSupport.RID_DEVICE_INFO_MOUSE));
                        //textsb.AppendFormat("Mouse: id {0}, {1} button(s), {2} samples/second\r\n", rdim.dwId, rdim.dwNumberOfButtons, rdim.dwSampleRate);
                        break;
                    default:
                        throw new ApplicationException("Hey!");
                    #endregion
                }
                Marshal.FreeHGlobal(devinfo);
            }
            calibHook = new HwndSourceHook(CalibProc);
            HwndSource.FromHwnd(new WindowInteropHelper(calibWindow).Handle).AddHook(calibHook);
        }

        static Pt calibratedPt(Pt p)
        {
            Pt[] cals = _calibPts.ToArray();
            if (cals.Length < 3)
                return p;
                                   
            Vec data = p - cals[0];
            Vec x = (cals[1] - cals[0]).Normal() ;
            Vec y = (cals[2] - cals[0]).Normal() ;

            Pt cal = data.Dot(x) * x/(cals[1]-cals[0]).Length * (dots[1]-dots[0]).Length  + 
                     data.Dot(y) * y/(cals[2]-cals[0]).Length * (dots[2]-dots[0]).Length  + dots[0];


            return cal;
        }
        static IntPtr CalibProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case ComSupport.WM_INPUT:
                    uint dwSize = 0;
                    ComSupport.GetRawInputData(lParam, ComSupport.RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(ComSupport.RAWINPUTHEADER)));
                    IntPtr lpb = Marshal.AllocHGlobal((int)dwSize);

                    ComSupport.RAWINPUTHEADER rih = ComSupport.GetRawInputHeader(lParam, dwSize, lpb);
                    ComSupport.RAWHIDheader rhh = (ComSupport.RAWHIDheader)Marshal.PtrToStructure(new IntPtr(lpb.ToInt32() + Marshal.SizeOf(rih)), typeof(ComSupport.RAWHIDheader));

                    byte[] input = new byte[rhh.dwSizeHid];
                    int ix = lpb.ToInt32() + Marshal.SizeOf(rih) + Marshal.SizeOf(rhh);
                    for (int i = 0; i < rhh.dwCount; i++, ix += rhh.dwSizeHid)
                    {
                        Marshal.Copy(new IntPtr(ix), input, 0, rhh.dwSizeHid);

                        if (rhh.dwSizeHid == 65)
                        {
                            Point p = rawPtFromInput(input);

                            if (input[2] == 0x08) // IOGear Pen in Pen Mode, Button Up
                                if (calibratePenUp(hwnd))
                                {
                                    handled = true;
                                    //HwndSource.FromHwnd(hwnd).RemoveHook(calibHook);
                                    calibWindow.Close();
                                    return IntPtr.Zero;
                                }

                            if (input[2] == 0x01)  // IOGear Pen in Pen Mode, Button Down
                                calibratePenDown(p);
                        }

                        if (rhh.dwSizeHid == 6 && input[0] == 0x08) { // IOGear pen in Mouse Mode, Button ??
                            handled = true;
                            Point p = new Point((input[2 + 0] << 8) | input[1 + 0], (input[4 + 0] << 8) | input[3 + 0]);
                        }
                    }

                    Marshal.FreeHGlobal(lpb);
                    break;
            }
            return IntPtr.Zero;

        }

        public IOGearDeviceDriver(Window window, InqCanvas display)
        {
            if (calibHook == null)
            {
                showCalibWindow();
                _displays.Add(display);
                _eventWindow = mWindow = window;
                setupDevices(_eventWindow);
                HwndSource.FromHwnd(new WindowInteropHelper(_eventWindow).Handle).AddHook(new HwndSourceHook(WndProc));
                eventWindowScreenOrigin = _eventWindow.PointToScreen(new Point());
                _eventWindow.Focus();
            }
            else
            {
                _displays.Add(display);
                _eventWindow = mWindow = window;
            }
        }
        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
               case ComSupport.WM_INPUT:
                    uint dwSize = 0;
                    ComSupport.GetRawInputData(lParam, ComSupport.RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(ComSupport.RAWINPUTHEADER)));
                    IntPtr lpb = Marshal.AllocHGlobal((int)dwSize);

                    ComSupport.RAWINPUTHEADER rih = ComSupport.GetRawInputHeader(lParam, dwSize, lpb);
                    ComSupport.RAWHIDheader rhh = (ComSupport.RAWHIDheader)Marshal.PtrToStructure(new IntPtr(lpb.ToInt32() + Marshal.SizeOf(rih)), typeof(ComSupport.RAWHIDheader));

                    byte[] input = new byte[rhh.dwSizeHid];
                    int ix = lpb.ToInt32() + Marshal.SizeOf(rih) + Marshal.SizeOf(rhh);
                    for (int i = 0; i < rhh.dwCount; i++, ix += rhh.dwSizeHid)
                    {
                        Marshal.Copy(new IntPtr(ix), input, 0, rhh.dwSizeHid);

                        if (rhh.dwSizeHid == 65)
                        {
                            Point p = calibratedPt(rawPtFromInput(input));

                            double wherex = p.X - eventWindowScreenOrigin.X;
                            double wherey = p.Y - eventWindowScreenOrigin.Y;

                            if (input[2] == 0x08) { // IOGear Pen in Pen Mode, Button Up
                                window_PreviewMouseUp(hwnd, new Point(wherex, wherey));
                                penState = false;
                            }
                            if (input[2] == 0x01)
                            { // IOGear Pen in Pen Mode, Button Down
                                if (!penState)
                                {
                                    window_PreviewMouseDown(hwnd, new Point(wherex, wherey));
                                    penState = true;
                                }

                                InqCanvas display = null;
                                foreach (InqCanvas iq in _displays)
                                    if (iq.IsMouseCaptured)
                                    {
                                        iq.Cursor = new Cursor(new System.IO.MemoryStream(WPFHelp.Resource1.Cursor1));  //Cursors.Pen;
                                        display = iq;
                                        break;
                                    }
                                window_PreviewMouseMove(hwnd, new Point(wherex, wherey), penState);
                            }
                            //System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)p.X, (int)(p.Y));
                        }
                    }

                    Marshal.FreeHGlobal(lpb);
                    break;
            }
            return IntPtr.Zero;

        }

    }
    public class Iogear2
    {
        static HwndSourceHook calibHook = null;
        static Pt[] dots = null;
        static Canvas calibDots = new Canvas();
        static Window calibWindow = new Window();
        static bool penCalibState = false;
        static List<Pt> _calibPts = new List<Pt>();
        static List<IntPtr> iogears = new List<IntPtr>();
        static bool penState = false;
        static Window _eventWindow = null;
        static Point eventWindowScreenOrigin = new Point();
        static List<InqCanvas> _displays = new List<InqCanvas>();
        const string IOGearHdr = "\\\\?\\HID#VID_0E20";
        public delegate void MouseMovedHandler(Point where);

        static Point rawPtFromInput(byte[] input)
        {
            Point p = new Point((input[2 + 2] << 8) | input[1 + 2], (input[4 + 2] << 8) | input[3 + 2]);
            if (p.X > Math.Pow(2, 15))
                p.X = p.X - 65536;
            return p;
        }
        static void testForIOGearPen(Window window, ComSupport.RAWINPUTDEVICELIST r, string hidDeviceName, ComSupport.RID_DEVICE_INFO_HID rdih)
        {
            if (hidDeviceName.StartsWith(IOGearHdr))
            {//"\\\\?\\HID#WACF004"
                /*Trace.Assert(r.dwType == RIM_TYPEHID);
                Trace.Assert(rdih.usUsagePage == 13);
                Trace.Assert(rdih.usUsage == 2);*/
                // register for it
                ComSupport.RAWINPUTDEVICE rid = new ComSupport.RAWINPUTDEVICE();
                rid.usUsagePage = rdih.usUsagePage;
                rid.usUsage = rdih.usUsage;
                rid.dwFlags = 0;
                rid.hwndTarget = new WindowInteropHelper(window).Handle;
                bool result = ComSupport.RegisterRawInputDevices(new ComSupport.RAWINPUTDEVICE[] { rid }, 1, (uint)Marshal.SizeOf(rid));

                iogears.Add(r.hDevice);
            }
        }
        static void showCalibWindow()
        {
            calibDots.ClipToBounds = false;
            calibWindow.Content = calibDots;

            Ellipse m1 = new Ellipse();
            m1.Width = 3;
            m1.Height = 3;
            m1.Fill = Brushes.Green;
            m1.RenderTransform = new TranslateTransform(500, 200);
            calibDots.Children.Add(m1);
            m1 = new Ellipse();
            m1.Width = 3;
            m1.Height = 3;
            m1.Fill = Brushes.Green;
            m1.RenderTransform = new TranslateTransform(800, 200);
            calibDots.Children.Add(m1);
            m1 = new Ellipse();
            m1.Width = 3;
            m1.Height = 3;
            m1.Fill = Brushes.Green;
            m1.RenderTransform = new TranslateTransform(500, 500);
            calibDots.Children.Add(m1);


            calibDots.Cursor = Cursors.Cross;
            calibWindow.Loaded += new RoutedEventHandler(calibWindow_Loaded);
            calibWindow.ShowDialog();

        }
        static bool calibratePenUp(IntPtr hwnd)
        {
            if (penCalibState)
            {
                penCalibState = false;
                if (_calibPts.Count == 3)
                {
                    return true;
                }
            }
            return false;
        }

        static void calibratePenDown(Point p)
        {
            if (!penCalibState)
            {
                penCalibState = true;
                if (dots == null)
                    dots = new Pt[] { calibDots.Children[0].PointToScreen(new Point(0,0)), 
                                      calibDots.Children[1].PointToScreen(new Point(0,0)), 
                                      calibDots.Children[2].PointToScreen(new Point(0,0)) };
                _calibPts.Add(new Pt(p.X, p.Y));
                Console.WriteLine("Got Calib Pt #" + _calibPts.Count + " = " + _calibPts[_calibPts.Count - 1]);
                (calibDots.Children[_calibPts.Count - 1] as Ellipse).Fill = Brushes.Red;
            }
        }

        static void setupDevices(Window window)
        {
            foreach (ComSupport.RAWINPUTDEVICELIST r in ComSupport.GetRawInputDeviceList())
            {
                IntPtr devinfo;
                ComSupport.RID_DEVICE_INFOheader rdi = ComSupport.GetDeviceInfo(r, out devinfo);

                switch (rdi.dwType)
                {
                    case ComSupport.RIM_TYPEHID:
                        ComSupport.RID_DEVICE_INFO_HID rdih = (ComSupport.RID_DEVICE_INFO_HID)
                            Marshal.PtrToStructure(new IntPtr(devinfo.ToInt32() + Marshal.SizeOf(typeof(ComSupport.RID_DEVICE_INFOheader))), typeof(ComSupport.RID_DEVICE_INFO_HID));
                        //textsb.AppendFormat("HID: vendor {0:X}, product {1:X}, version {2}, usage page {3}, usage {4}\r\n", rdih.dwVendorID, rdih.dwProductID, rdih.dwVersionNumber, rdih.usUsagePage, rdih.usUsage);

                        testForIOGearPen(window,r, ComSupport.GetDeviceName(r), rdih);

                        break;
                    #region otherdevices
                    case ComSupport.RIM_TYPEKEYBOARD:
                        ComSupport.RID_DEVICE_INFO_KEYBOARD rdik = (ComSupport.RID_DEVICE_INFO_KEYBOARD)
                            Marshal.PtrToStructure(new IntPtr(devinfo.ToInt32() + Marshal.SizeOf(typeof(ComSupport.RID_DEVICE_INFOheader))), typeof(ComSupport.RID_DEVICE_INFO_KEYBOARD));
                        //textsb.AppendFormat("Kbd: type/sub {0}/{1}, scan code mode {2}, {3} fn(s), {4} indicator(s), {5} key(s)\r\n",  rdik.dwType, rdik.dwSubType, rdik.dwKeyboardMode, rdik.dwNumberOfFunctionKeys, rdik.dwNumberOfIndicators, rdik.dwNumberOfKeysTotal);
                        break;
                    case ComSupport.RIM_TYPEMOUSE:
                        ComSupport.RID_DEVICE_INFO_MOUSE rdim = (ComSupport.RID_DEVICE_INFO_MOUSE)
                            Marshal.PtrToStructure(new IntPtr(devinfo.ToInt32() + Marshal.SizeOf(typeof(ComSupport.RID_DEVICE_INFOheader))), typeof(ComSupport.RID_DEVICE_INFO_MOUSE));
                        //textsb.AppendFormat("Mouse: id {0}, {1} button(s), {2} samples/second\r\n", rdim.dwId, rdim.dwNumberOfButtons, rdim.dwSampleRate);
                        break;
                    default:
                        throw new ApplicationException("Hey!");
                    #endregion
                }
                Marshal.FreeHGlobal(devinfo);
            }
        }

        static void calibWindow_Loaded(object sender, RoutedEventArgs e)
        {
            setupDevices(calibWindow);

            foreach (ComSupport.RAWINPUTDEVICELIST r in ComSupport.GetRawInputDeviceList())
            {
                IntPtr devinfo;
                ComSupport.RID_DEVICE_INFOheader rdi = ComSupport.GetDeviceInfo(r, out devinfo);

                switch (rdi.dwType)
                {
                    case ComSupport.RIM_TYPEHID:
                        ComSupport.RID_DEVICE_INFO_HID rdih = (ComSupport.RID_DEVICE_INFO_HID)
                            Marshal.PtrToStructure(new IntPtr(devinfo.ToInt32() + Marshal.SizeOf(typeof(ComSupport.RID_DEVICE_INFOheader))), typeof(ComSupport.RID_DEVICE_INFO_HID));
                        //textsb.AppendFormat("HID: vendor {0:X}, product {1:X}, version {2}, usage page {3}, usage {4}\r\n", rdih.dwVendorID, rdih.dwProductID, rdih.dwVersionNumber, rdih.usUsagePage, rdih.usUsage);

                        testForIOGearPen(calibWindow, r, ComSupport.GetDeviceName(r), rdih);

                        break;
                    #region otherdevices
                    case ComSupport.RIM_TYPEKEYBOARD:
                        ComSupport.RID_DEVICE_INFO_KEYBOARD rdik = (ComSupport.RID_DEVICE_INFO_KEYBOARD)
                            Marshal.PtrToStructure(new IntPtr(devinfo.ToInt32() + Marshal.SizeOf(typeof(ComSupport.RID_DEVICE_INFOheader))), typeof(ComSupport.RID_DEVICE_INFO_KEYBOARD));
                        //textsb.AppendFormat("Kbd: type/sub {0}/{1}, scan code mode {2}, {3} fn(s), {4} indicator(s), {5} key(s)\r\n",  rdik.dwType, rdik.dwSubType, rdik.dwKeyboardMode, rdik.dwNumberOfFunctionKeys, rdik.dwNumberOfIndicators, rdik.dwNumberOfKeysTotal);
                        break;
                    case ComSupport.RIM_TYPEMOUSE:
                        ComSupport.RID_DEVICE_INFO_MOUSE rdim = (ComSupport.RID_DEVICE_INFO_MOUSE)
                            Marshal.PtrToStructure(new IntPtr(devinfo.ToInt32() + Marshal.SizeOf(typeof(ComSupport.RID_DEVICE_INFOheader))), typeof(ComSupport.RID_DEVICE_INFO_MOUSE));
                        //textsb.AppendFormat("Mouse: id {0}, {1} button(s), {2} samples/second\r\n", rdim.dwId, rdim.dwNumberOfButtons, rdim.dwSampleRate);
                        break;
                    default:
                        throw new ApplicationException("Hey!");
                    #endregion
                }
                Marshal.FreeHGlobal(devinfo);
            }
            calibHook = new HwndSourceHook(CalibProc);
            HwndSource.FromHwnd(new WindowInteropHelper(calibWindow).Handle).AddHook(calibHook);
        }

        static Pt calibratedPt(Pt p)
        {
            Pt[] cals = _calibPts.ToArray();
            if (cals.Length < 3)
                return p;
                                   
            Vec data = p - cals[0];
            Vec x = (cals[1] - cals[0]).Normal() ;
            Vec y = (cals[2] - cals[0]).Normal() ;

            Pt cal = data.Dot(x) * x/(cals[1]-cals[0]).Length * (dots[1]-dots[0]).Length  + 
                     data.Dot(y) * y/(cals[2]-cals[0]).Length * (dots[2]-dots[0]).Length  + dots[0];


            return cal;
        }
        static IntPtr CalibProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case ComSupport.WM_INPUT:
                    uint dwSize = 0;
                    ComSupport.GetRawInputData(lParam, ComSupport.RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(ComSupport.RAWINPUTHEADER)));
                    IntPtr lpb = Marshal.AllocHGlobal((int)dwSize);

                    ComSupport.RAWINPUTHEADER rih = ComSupport.GetRawInputHeader(lParam, dwSize, lpb);
                    ComSupport.RAWHIDheader rhh = (ComSupport.RAWHIDheader)Marshal.PtrToStructure(new IntPtr(lpb.ToInt32() + Marshal.SizeOf(rih)), typeof(ComSupport.RAWHIDheader));

                    byte[] input = new byte[rhh.dwSizeHid];
                    int ix = lpb.ToInt32() + Marshal.SizeOf(rih) + Marshal.SizeOf(rhh);
                    for (int i = 0; i < rhh.dwCount; i++, ix += rhh.dwSizeHid)
                    {
                        Marshal.Copy(new IntPtr(ix), input, 0, rhh.dwSizeHid);

                        if (rhh.dwSizeHid == 65)
                        {
                            Point p = rawPtFromInput(input);

                            if (input[2] == 0x08) // IOGear Pen in Pen Mode, Button Up
                                if (calibratePenUp(hwnd))
                                {
                                    handled = true;
                                    //HwndSource.FromHwnd(hwnd).RemoveHook(calibHook);
                                    calibWindow.Close();
                                    return IntPtr.Zero;
                                }

                            if (input[2] == 0x01)  // IOGear Pen in Pen Mode, Button Down
                                calibratePenDown(p);
                        }

                        if (rhh.dwSizeHid == 6 && input[0] == 0x08) { // IOGear pen in Mouse Mode, Button ??
                            handled = true;
                            Point p = new Point((input[2 + 0] << 8) | input[1 + 0], (input[4 + 0] << 8) | input[3 + 0]);
                        }
                    }

                    Marshal.FreeHGlobal(lpb);
                    break;
            }
            return IntPtr.Zero;

        }

        public Iogear2(Window window, InqCanvas display)
        {
            if (calibHook == null)
            {
                showCalibWindow();
                _displays.Add(display);
                _eventWindow = window;
                setupDevices(_eventWindow);
                HwndSource.FromHwnd(new WindowInteropHelper(_eventWindow).Handle).AddHook(new HwndSourceHook(WndProc));
                eventWindowScreenOrigin = _eventWindow.PointToScreen(new Point());
                _eventWindow.Focus();
            }
            else
            {
                _displays.Add(display);
                _eventWindow = window;
            }
        }
        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
               case ComSupport.WM_INPUT:
                    uint dwSize = 0;
                    ComSupport.GetRawInputData(lParam, ComSupport.RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(ComSupport.RAWINPUTHEADER)));
                    IntPtr lpb = Marshal.AllocHGlobal((int)dwSize);

                    ComSupport.RAWINPUTHEADER rih = ComSupport.GetRawInputHeader(lParam, dwSize, lpb);
                    ComSupport.RAWHIDheader rhh = (ComSupport.RAWHIDheader)Marshal.PtrToStructure(new IntPtr(lpb.ToInt32() + Marshal.SizeOf(rih)), typeof(ComSupport.RAWHIDheader));

                    byte[] input = new byte[rhh.dwSizeHid];
                    int ix = lpb.ToInt32() + Marshal.SizeOf(rih) + Marshal.SizeOf(rhh);
                    for (int i = 0; i < rhh.dwCount; i++, ix += rhh.dwSizeHid)
                    {
                        Marshal.Copy(new IntPtr(ix), input, 0, rhh.dwSizeHid);

                        if (rhh.dwSizeHid == 65)
                        {
                            Point p = calibratedPt(rawPtFromInput(input));

                            double wherex = p.X - eventWindowScreenOrigin.X;
                            double wherey = p.Y - eventWindowScreenOrigin.Y;

                            int where = ComSupport.MakeLParam((int)wherex, (int)wherey);

                            if (input[2] == 0x08) { // IOGear Pen in Pen Mode, Button Up
                                ComSupport.SendMessage(hwnd, ComSupport.WM_MOUSEMOVE, 0, where);
                                if (penState) {
                                    penState = false;
                                    ComSupport.SendMessage(hwnd, ComSupport.WM_LBUTTONUP, 0, where);
                                }
                            }
                            if (input[2] == 0x01)
                            { // IOGear Pen in Pen Mode, Button Down
                                if (!penState)
                                {
                                    penState = true;
                                    ComSupport.SendMessage(hwnd, ComSupport.WM_LBUTTONDOWN, ComSupport.MK_LBUTTON, where);
                                }

                                InqCanvas display = null;
                                foreach (InqCanvas iq in _displays)
                                    if (iq.IsMouseCaptured)
                                    {
                                        iq.Cursor = new Cursor(new System.IO.MemoryStream(WPFHelp.Resource1.Cursor1));  //Cursors.Pen;
                                        display = iq;
                                        break;
                                    }
                                ComSupport.SendMessage(hwnd, ComSupport.WM_MOUSEMOVE, ComSupport.MK_LBUTTON, where);

                                if(display != null)
                                    display.DynamicRenderer.Move(p);
                            }
                            System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)p.X, (int)(p.Y));
                        }
                    }

                    Marshal.FreeHGlobal(lpb);
                    break;
            }
            return IntPtr.Zero;

        }

    }
    public class Iogear
    {
        IntPtr _leftDevice = IntPtr.Zero;
        Dictionary<IntPtr, Brush> penColor = new Dictionary<IntPtr, Brush>();
        Dictionary<IntPtr, List<Point>> penCals = new Dictionary<IntPtr, List<Point>>();
        bool penDown = false;
        bool prepareForDividingLine = false;
        bool CalibrationComplete = false;
        SortedList<double, Vector> divideDeltas = new SortedList<double, Vector>();
        Dictionary<IntPtr, List<Point>> dividers = new Dictionary<IntPtr, List<Point>>();
        Dictionary<IntPtr, SortedList<double, double>> dividingLine = new Dictionary<IntPtr, SortedList<double, double>>();
        Dictionary<IntPtr, bool> penState = new Dictionary<IntPtr, bool>();
        Brush[] penBrushes = new Brush[] { Brushes.Red, Brushes.Green, Brushes.Black, Brushes.Blue };
        List<IntPtr> iogears = new List<IntPtr>();
        int            nextBrush = 0;
        Window _eventWindow = null;
        InqCanvas   _display = null;
        const string IOGearHdr = "\\??\\HID#Vid_0e20";

        void showInputPt(Brush drawColor, Point p) {
            Rectangle rct = new Rectangle();
            rct.Fill = drawColor;
            rct.Width = rct.Height = 3;
            rct.RenderTransform = new TranslateTransform(p.X, p.Y);
            _trails.Children.Add(rct);
            if (_trails.Children.Count > 15)
                _trails.Children.RemoveAt(0);
        }

        void testForIOGearPen(ComSupport.RAWINPUTDEVICELIST r, string hidDeviceName, ComSupport.RID_DEVICE_INFO_HID rdih) {
            if (hidDeviceName.StartsWith(IOGearHdr)) {//"\\\\?\\HID#WACF004"
                /*Trace.Assert(r.dwType == RIM_TYPEHID);
                Trace.Assert(rdih.usUsagePage == 13);
                Trace.Assert(rdih.usUsage == 2);*/
                // register for it
                ComSupport.RAWINPUTDEVICE rid = new ComSupport.RAWINPUTDEVICE();
                rid.usUsagePage = rdih.usUsagePage;
                rid.usUsage = rdih.usUsage;
                rid.dwFlags = 0;
                rid.hwndTarget = new WindowInteropHelper(_eventWindow).Handle;
                bool result = ComSupport.RegisterRawInputDevices(new ComSupport.RAWINPUTDEVICE[] { rid }, 1, (uint)Marshal.SizeOf(rid));

                if (!penState.ContainsKey(r.hDevice))
                    penState.Add(r.hDevice, false);
                if (!penColor.ContainsKey(r.hDevice)) {
                    penColor.Add(r.hDevice, penBrushes[nextBrush++]);
                    penCals.Add(r.hDevice, new List<Point>());
                    iogears.Add(r.hDevice);
                }
            }
        }

        Canvas _trails = new Canvas();
        public void InitializeWindow(Window eventWindow, InqCanvas display) {
            _eventWindow = eventWindow;
            _display = display;
            Canvas calibDots = new Canvas();
            calibDots.ClipToBounds = false;
            _display.Children.Add(calibDots);
            _trails.ClipToBounds = false;
            _display.Children.Add(_trails);


            Ellipse m1 = new Ellipse();
            m1.Width = 3;
            m1.Height = 3;
            m1.Fill = Brushes.Green;
            m1.RenderTransform = new TranslateTransform(200, 200);
            calibDots.Children.Add(m1);
            m1 = new Ellipse();
            m1.Width = 3;
            m1.Height = 3;
            m1.Fill = Brushes.Green;
            m1.RenderTransform = new TranslateTransform(700, 200);
            calibDots.Children.Add(m1);
            m1 = new Ellipse();
            m1.Width = 3;
            m1.Height = 3;
            m1.Fill = Brushes.Green;
            m1.RenderTransform = new TranslateTransform(200, 700);
            calibDots.Children.Add(m1);

            m1 = new Ellipse();
            m1.Width = 3;
            m1.Height = 3;
            m1.Fill = Brushes.Green;
            m1.RenderTransform = new TranslateTransform(1200, 200);
            calibDots.Children.Add(m1);
            m1 = new Ellipse();
            m1.Width = 3;
            m1.Height = 3;
            m1.Fill = Brushes.Green;
            m1.RenderTransform = new TranslateTransform(1700, 200);
            calibDots.Children.Add(m1);
            m1 = new Ellipse();
            m1.Width = 3;
            m1.Height = 3;
            m1.Fill = Brushes.Green;
            m1.RenderTransform = new TranslateTransform(1200, 700);
            calibDots.Children.Add(m1);

            foreach (FrameworkElement obj in _display.Children)
                obj.Cursor = Cursors.Cross;
            _display.Cursor = Cursors.Cross;

            HwndSource.FromHwnd(new WindowInteropHelper(_eventWindow).Handle).AddHook(new HwndSourceHook(WndProc));

            foreach (ComSupport.RAWINPUTDEVICELIST r in ComSupport.GetRawInputDeviceList()) {
                IntPtr devinfo;
                ComSupport.RID_DEVICE_INFOheader rdi = ComSupport.GetDeviceInfo(r, out devinfo);
                
                switch (rdi.dwType) {
                    case ComSupport.RIM_TYPEHID:
                           ComSupport.RID_DEVICE_INFO_HID rdih = (ComSupport.RID_DEVICE_INFO_HID)
                               Marshal.PtrToStructure(new IntPtr(devinfo.ToInt32() + Marshal.SizeOf(typeof(ComSupport.RID_DEVICE_INFOheader))), typeof(ComSupport.RID_DEVICE_INFO_HID));
                        //textsb.AppendFormat("HID: vendor {0:X}, product {1:X}, version {2}, usage page {3}, usage {4}\r\n", rdih.dwVendorID, rdih.dwProductID, rdih.dwVersionNumber, rdih.usUsagePage, rdih.usUsage);

                        testForIOGearPen(r, ComSupport.GetDeviceName(r), rdih);

                        break;
                    #region otherdevices
                    case ComSupport.RIM_TYPEKEYBOARD:
                        ComSupport.RID_DEVICE_INFO_KEYBOARD rdik = (ComSupport.RID_DEVICE_INFO_KEYBOARD)
                            Marshal.PtrToStructure(new IntPtr(devinfo.ToInt32() + Marshal.SizeOf(typeof(ComSupport.RID_DEVICE_INFOheader))), typeof(ComSupport.RID_DEVICE_INFO_KEYBOARD));
                        //textsb.AppendFormat("Kbd: type/sub {0}/{1}, scan code mode {2}, {3} fn(s), {4} indicator(s), {5} key(s)\r\n",  rdik.dwType, rdik.dwSubType, rdik.dwKeyboardMode, rdik.dwNumberOfFunctionKeys, rdik.dwNumberOfIndicators, rdik.dwNumberOfKeysTotal);
                        break;
                    case ComSupport.RIM_TYPEMOUSE:
                        ComSupport.RID_DEVICE_INFO_MOUSE rdim = (ComSupport.RID_DEVICE_INFO_MOUSE)
                            Marshal.PtrToStructure(new IntPtr(devinfo.ToInt32() + Marshal.SizeOf(typeof(ComSupport.RID_DEVICE_INFOheader))), typeof(ComSupport.RID_DEVICE_INFO_MOUSE));
                        //textsb.AppendFormat("Mouse: id {0}, {1} button(s), {2} samples/second\r\n", rdim.dwId, rdim.dwNumberOfButtons, rdim.dwSampleRate);
                        break;
                    default:
                        throw new ApplicationException("Hey!");
                    #endregion
                }
                Marshal.FreeHGlobal(devinfo);
            }
        }


        Point normalizedPt(Point p, IntPtr device) {
            Point[] cals = penCals[device].ToArray();
            if (p.X > Math.Pow(2, 15))
                p.X = p.X - 65536;
            p = new Point(p.X / 10, p.Y / 10);
            if (cals.Length < 3)
                return p;
            Vector data = p - cals[0];
            Vector x = cals[1] - cals[0];
            Vector y = cals[2] - cals[0];

            double projX = (data.X * x.X + data.Y * x.Y) / (x.X * x.X + x.Y * x.Y) * 500;
            double projY = (data.X * y.X + data.Y * y.Y) / (y.X * y.X + y.Y * y.Y) * 500;
            Point cal = new Point(projX , projY);

            return cal;
        }
        public delegate void MouseMovedHandler(Point where);
        IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            _display.Cursor = Cursors.Cross;
            switch (msg) {
                case ComSupport.WM_INPUT:
                    // m.wParam is RIM_INPUT or RIM_INPUTSINK
                    // m.lParam is *handle* to RAWINPUT, which is a RAWINPUTHEADER followed by RAWHID, RAWMOUSE, or RAWKEYBOARD, depending.
                    //Console.WriteLine("WM_INPUT " + GET_RAWINPUT_CODE_WPARAM(m.WParam));
                    uint dwSize = 0;
                    ComSupport.GetRawInputData(lParam, ComSupport.RID_INPUT, IntPtr.Zero, ref dwSize, (uint)Marshal.SizeOf(typeof(ComSupport.RAWINPUTHEADER)));
                    IntPtr lpb = Marshal.AllocHGlobal((int)dwSize);

                    ComSupport.RAWINPUTHEADER rih = ComSupport.GetRawInputHeader(lParam, dwSize, lpb);
                    ComSupport.RAWHIDheader        rhh = (ComSupport.RAWHIDheader)Marshal.PtrToStructure(new IntPtr(lpb.ToInt32() + Marshal.SizeOf(rih)), typeof(ComSupport.RAWHIDheader));

                    Brush drawColor = penColor[rih.hDevice];
                    //Console.WriteLine("{0} inputs of {1} bytes each", rhh.dwCount, rhh.dwSizeHid);
                    byte[] input = new byte[rhh.dwSizeHid];
                    int       ix = lpb.ToInt32() + Marshal.SizeOf(rih) + Marshal.SizeOf(rhh);
                    for (int i = 0; i < rhh.dwCount; i++, ix += rhh.dwSizeHid) {
                        Point pp = _eventWindow.PointToScreen(new Point());
                        Point pp2 = _display.PointToScreen(new Point());
                        Marshal.Copy(new IntPtr(ix), input, 0, rhh.dwSizeHid);

                        if (rhh.dwSizeHid == 65) {
                            Point p =  normalizedPt(new Point((input[2 + 2] << 8) | input[1 + 2], (input[4 + 2] << 8) | input[3 + 2]), rih.hDevice);
                            if (CalibrationComplete) {
                                double xdivider;
                                Vector deviceShift = CalibratedPt(rih, p, out xdivider);

                                if ((rih.hDevice == _leftDevice && xdivider <= p.X) || (rih.hDevice != _leftDevice && xdivider >= p.X))
                                    continue;
                                p = new Point(p.X + 200, p.Y + 200);
                                if (rih.hDevice != _leftDevice)
                                    p = new Point(p.X + deviceShift.X, p.Y + deviceShift.Y);
                            }
                            int where = ComSupport.MakeLParam((int)(p.X), (int)(p.Y - pp.Y));

                            if (input[2] == 0x08) { // IOGear Pen in Pen Mode, Button Up
                                if (CalibrationComplete) 
                                    ComSupport.SendMessage(hwnd, ComSupport.WM_MOUSEMOVE, 0, where);
                                if (penState[rih.hDevice]) {
                                    penState[rih.hDevice] = false;
                                    if (CalibrationComplete) 
                                        ComSupport.SendMessage(hwnd, ComSupport.WM_LBUTTONUP, 0, where);
                                    CompleteCalibration(rih);
                                }
                               // showInputPt(Brushes.Red, new Point(p.X, p.Y - pp2.Y));
                            }

                            if (input[2] == 0x01) { // IOGear Pen in Pen Mode, Button Down
                                if (!penState[rih.hDevice]) {
                                    penState[rih.hDevice] = true;
                                    if (CalibrationComplete)
                                        ComSupport.SendMessage(hwnd, ComSupport.WM_LBUTTONDOWN, ComSupport.MK_LBUTTON, where);
                                    else {
                                        if (prepareForDividingLine)
                                            startDividingLine(rih);
                                        else 
                                            prepareForDividingLine = AddCalibrationPoint(rih, p);
                                    }
                                }

                                if (CalibrationComplete)
                                    ComSupport.SendMessage(hwnd, ComSupport.WM_MOUSEMOVE, ComSupport.MK_LBUTTON, where);
                                else if (dividingLine.ContainsKey(rih.hDevice)) 
                                    accumulateDividingLine(rih, p);

                                (_display as InqCanvas).DynamicRenderer.Move(p);
                                //showInputPt(drawColor, p);

                            }
                            System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)p.X, (int)(p.Y));
                        }

                        if (rhh.dwSizeHid == 6 && input[0] == 0x08) { // IOGear pen in Mouse Mode, Button ??
                            handled = true;
                            Point p = new Point((input[2 + 0] << 8) | input[1 + 0], (input[4 + 0] << 8) | input[3 + 0]);

                            //showInputPt(drawColor, p);
                        }
                    }
                   
                    Marshal.FreeHGlobal(lpb);
                    break;
            }
            return IntPtr.Zero;

        }

        void CompleteCalibration(ComSupport.RAWINPUTHEADER rih) {
            if (dividingLine.ContainsKey(rih.hDevice)) {
                Console.WriteLine("Calibration completed ");
                CalibrationComplete = true;
            }
        }

        void startDividingLine(ComSupport.RAWINPUTHEADER rih) {
            dividingLine.Add(rih.hDevice, new SortedList<double, double>());
            dividers.Add(rih.hDevice, new List<Point>());
        }

        private bool AddCalibrationPoint(ComSupport.RAWINPUTHEADER rih, Point p) {
            if (penCals[rih.hDevice].Count < 3) {
                if (_leftDevice == IntPtr.Zero)
                    _leftDevice = rih.hDevice;
                penCals[rih.hDevice].Add(p);

                if (penCals[rih.hDevice].Count == 3 && rih.hDevice != _leftDevice) {
                    Console.WriteLine("Next stroke will be dividing line");
                    return true;
                }
            }
            return false;
        }

        private void accumulateDividingLine(ComSupport.RAWINPUTHEADER rih, Point p) {
            if (!dividingLine[rih.hDevice].ContainsKey(p.Y)) {
                dividingLine[rih.hDevice].Add(p.Y, p.X);
                dividers[rih.hDevice].Add(p);
                if (rih.hDevice != _leftDevice && dividers.ContainsKey(_leftDevice) && dividers[_leftDevice].Count > 0)
                    divideDeltas.Add(p.Y, new Vector(dividers[_leftDevice][dividers[_leftDevice].Count - 1].X - p.X,
                                                                         dividers[_leftDevice][dividers[_leftDevice].Count - 1].Y - p.Y));
            }
        }

        Vector CalibratedPt( ComSupport.RAWINPUTHEADER rih, Point p,out double xdivider) {
            Vector deviceShift = new Vector();
            xdivider = 0;
            int index = 0;
            if (!dividingLine[rih.hDevice].ContainsKey(p.Y)) {
                dividingLine[rih.hDevice].Add(p.Y, p.X);
                index = dividingLine[rih.hDevice].IndexOfKey(p.Y);
                xdivider = dividingLine[rih.hDevice].Values[Math.Max(0, index - 1)];
                dividingLine[rih.hDevice].RemoveAt(index);
            }
            else {
                index = dividingLine[rih.hDevice].IndexOfKey(p.Y);
                xdivider = dividingLine[rih.hDevice].Values[index];
            }
            if (rih.hDevice != _leftDevice) {

                if (!divideDeltas.ContainsKey(p.Y)) {
                    divideDeltas.Add(p.Y, new Vector());
                    int ind2 = divideDeltas.IndexOfKey(p.Y);
                    deviceShift = divideDeltas.Values[Math.Max(0, ind2 - 1)];
                    divideDeltas.RemoveAt(ind2);
                }
                else
                    deviceShift = divideDeltas[p.Y];
            }
            return deviceShift;
        }

    }
}
