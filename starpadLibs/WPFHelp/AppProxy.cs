using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Timers;
using Image = System.Windows.Controls.Image;
using Timer = System.Timers.Timer;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using starPadSDK.Utils.Gma.UserActivityMonitor;
using starPadSDK.Geom;
using starPadSDK.WPFHelp;
using System.Drawing;

namespace starPadSDK.WPFHelp {
    public class AppProxy {
        Bitmap      _appBitmap = null;
        Timer        _grabTimer = new Timer();
        double       _screenUpdateInterval = 10;
        double       _idleUpdateInterval = 500;
        bool           _hasExited = false;
        Process      _appProcess = null;
        Canvas       _crop = null;
        Image        _appImage = null;
        IntPtr       _mainWind = (IntPtr)0;
        Rectangle  _apprect = Rectangle.Empty;
        Rct           _proxy;
        int           _focusInApp = 2;
        bool         _suspended = false;
        double     _initialWidth = 0;
        MouseButtons _downButtons = MouseButtons.None;

        bool findProcess() {
            foreach (Process pa in System.Diagnostics.Process.GetProcesses())
                if (pa.Id == _appProcess.Id && pa.MainWindowHandle != (IntPtr)0) {
                    _appProcess = pa;
                    _mainWind = _appProcess.MainWindowHandle;
                    return true;
                }
            return false;
        }

        void sendAppExit() {
            if (!_hasExited) {
                if (AppExited != null)
                    AppExited(this, null);
                if (_focusInApp == 2)
                    warpCursorToProxy(Cursor.Position);
                _hasExited = true;
            }
        }
        bool checkProcessActive() {
            if (_appProcess.HasExited)
                sendAppExit();
            return !_hasExited;
        }

        void pollForProcessElapsed(object sender, ElapsedEventArgs e) {
            (sender as Timer).Enabled = false;
            if (CreateProxyCanvas(_initialWidth) == null)
                (sender as Timer).Enabled = true; // restart the timer if the process wasn't ready
        }

        // takes a snapshot of the application window
        Bitmap snapshotApp()  {
            if (_appBitmap != null)
                _appBitmap.Dispose();

            if (_apprect.IsEmpty)
                _appBitmap = ScreenCapture.CaptureWindow(_mainWind);
            else 
                _appBitmap = BasicSnapshot.TakeSnapshot(_apprect);
                    
            return _appBitmap;
        }

        // switch the proxy bitmap of the application window
        void changeAppBitmap(Image appImage) {
            ImageChangedEventArgs ie = new ImageChangedEventArgs();
            ie.OldImage = _appImage;
            ie.OldSize = new System.Windows.Size(_crop.Width, _crop.Height);
            if (_appImage != null) {
                _crop.Children.Remove(_appImage);
                appImage.RenderTransform = _appImage.RenderTransform;
            }
            Rct appRect = new Rct(0, 0, appImage.Width, appImage.Height);
            Rct scaledRct = ((Mat)_crop.RenderTransform.Value) * appRect;
            _crop.Height = appImage.Height / appImage.Width * _crop.Width;
            _crop.Children.Insert(0, appImage);
            _crop.ClipToBounds = true;
            _appImage = appImage;
            ie.NewImage = appImage;
            ie.NewSize = new System.Windows.Size(_crop.Width, _crop.Height);
            if (ImageChanged != null)
                ImageChanged(this, ie);
        }

        // grabs the bitmap of the application window to display in the proxy window
        void grabAppWindow(object sender, System.Timers.ElapsedEventArgs e) {
            if (_suspended)
                return;
            if ((sender as Timer).Enabled) {
                (sender as Timer).Enabled = false;
                (sender as Timer).Interval = _focusInApp > 0 ? ScreenUpdateInterval : IdleUpdateInterval;
                _crop.Parent.Dispatcher.Invoke(DispatcherPriority.Normal, new MethodInvoker(grabAppWindowDelegate));
                (sender as Timer).Enabled = !_appProcess.HasExited;
            }
        }

        void grabAppWindowDelegate() {
            try {
                updateProxyWindowFromAppWindowChanges();
                if (snapshotApp() != null) {
                    var cursorInTargetBMP = new Point(Cursor.Position.X - _apprect.Left, Cursor.Position.Y - _apprect.Top);
                    var appCursBitmap = CaptureScreen.AddCursorToBitmap(cursorInTargetBMP, _appBitmap);
                    changeAppBitmap(appCursBitmap.ConvertBitmapToWPFImage(_crop.Width));
                    appCursBitmap.Dispose();
                }
            }
            catch (Exception) {
                checkProcessActive();
            }
        }

        void updateProxyRectangle() {
            Window rootWin = System.Windows.Application.Current.MainWindow;
            Pt tlApp = rootWin.PointToScreen(new System.Windows.Point());
            Pt brApp = rootWin.PointToScreen(new System.Windows.Point(rootWin.Width, rootWin.Height));
            Pt tl = new Pt().TransformFromAtoB(_crop, rootWin) + new Vec(tlApp.X, tlApp.Y);
            Pt br = new Pt(_crop.Width,_crop.Height).TransformFromAtoB(_crop, rootWin)+ new Vec(tlApp.X, tlApp.Y);
            _proxy = new Rct(tl, br);
        }

        void updateAppRectangle() {
            RECT foo = new RECT();
            ComSupport.GetWindowRect(_mainWind, foo);
            if (ComSupport.GetLastError() == ComSupport.ERROR_INVALID_WINDOW_HANDLE)
                sendAppExit();
            _apprect = new Rectangle(foo.Left, foo.Top, foo.Right-foo.Left, foo.Bottom-foo.Top);
            ComSupport.GetWindowRect(_appProcess.MainWindowHandle, foo);
            if (ComSupport.GetLastError() == ComSupport.ERROR_INVALID_WINDOW_HANDLE)
                sendAppExit();
            _apprect = Rectangle.Union(_apprect, new Rectangle(foo.Left, foo.Top, foo.Right-foo.Left, foo.Bottom-foo.Top));
            List<IntPtr> children = Win32Stuff.GetTopLevelWindows();
            foreach (IntPtr child in children) {
                IntPtr par = Win32Stuff.GetParent(child);
                if (par == _appProcess.MainWindowHandle || par == _mainWind) {
                    ComSupport.GetWindowRect(child, foo);
                    if (foo.AsRectangle.Width > 0)
                        _apprect = Rectangle.Union(_apprect, new Rectangle(foo.Left, foo.Top, foo.Right - foo.Left, foo.Bottom - foo.Top));
                }
            }
        }

        void updateAppResized(Rectangle oldApprect) {
            _proxy.BottomRight = _proxy.TopLeft + new Vec((_apprect.Width / (double)oldApprect.Width * _proxy.Width),
                                                                                       (_apprect.Height / (double)oldApprect.Height * _proxy.Height));
            Window rootWin = System.Windows.Application.Current.MainWindow;
            Pt tl = _proxy.TopLeft.TransformFromAtoB(rootWin, _crop);
            Pt br = _proxy.BottomRight.TransformFromAtoB(rootWin, _crop);
            _crop.Width = br.X - tl.X;
            _crop.Height = br.Y - tl.Y;
        }

        void updateAppTranslated(Rectangle oldApprect) {
            double ratioX = _proxy.Width / oldApprect.Width;
            double ratioY = _proxy.Height / oldApprect.Height;
            Vec xlate = new Vec((_apprect.Left - oldApprect.Left) * ratioX, (_apprect.Top - oldApprect.Top) * ratioY);
            _proxy = _proxy.Translated(xlate);
            Window rootWin = System.Windows.Application.Current.MainWindow;
            Pt tl = new Pt().TransformFromAtoB(rootWin, _crop);
            Pt br = ((Pt)xlate).TransformFromAtoB(rootWin, _crop);
            _crop.RenderTransform = new MatrixTransform(((Mat)_crop.RenderTransform.Value) * Mat.Translate(br - tl));
        }

        void updateProxyWindowFromAppWindowChanges() {
            var oldApprect = _apprect;
            updateAppRectangle();
            updateProxyRectangle();
            if (checkProcessActive()) {
                if (!oldApprect.IsEmpty && oldApprect.Location != _apprect.Location)
                    updateAppTranslated(oldApprect);
                if (!oldApprect.IsEmpty && oldApprect.Size != _apprect.Size)
                    updateAppResized(oldApprect);
            }
        }

        void cropMouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
            Cursor.Position = new Point((int)(_apprect.Left + e.GetPosition(_crop).X / _crop.Width * _apprect.Width),
                                                      (int)(_apprect.Top + e.GetPosition(_crop).Y / _crop.Height * _apprect.Height));
            _focusInApp = 1;
        }

        void HookManager_MouseMove(object sender, MouseEventArgs e)  {
            if (_focusInApp > 0 && _downButtons == MouseButtons.None && _apprect.Width > 0 && _apprect.Height > 0) {
                if (_apprect.Contains(e.Location)) {
                    _focusInApp = 2;
                }
                else if (_focusInApp == 2) {
                    warpCursorToProxy(e.Location) ;
                    _focusInApp = 0;
                    (e as MouseEventExtArgs).Handled = true;
                }
            }
        }

        void warpCursorToProxy(Point where) {
            var warpPt = new Point((int)(_proxy.Left + (where.X - _apprect.Left) * _proxy.Width / _apprect.Width),
                                                 (int)(_proxy.Top + (where.Y - _apprect.Top) * _proxy.Height / _apprect.Height));
            if (warpPt.X > _proxy.Right - 1)
                warpPt = new Point((int)_proxy.Right + 1, warpPt.Y);
            if (warpPt.Y > _proxy.Bottom - 1)
                warpPt = new Point(warpPt.X, (int)_proxy.Bottom + 1);
            Cursor.Position = warpPt;
        }

        public delegate void AppStartedHandler(AppProxy sender, Canvas proxy);
        public event AppStartedHandler AppStartedEvent;
        public event EventHandler AppExited;
        public class  ImageChangedEventArgs {
            public Image OldImage;
            public Image NewImage;
            public System.Windows.Size OldSize;
            public System.Windows.Size   NewSize;
        }
        public delegate void ImageChangedHandler(object sender, ImageChangedEventArgs e);
        public event ImageChangedHandler ImageChanged;
        /// <summary>
        /// Constructs an AppProxy - however, you must keep calling CreateProxyCanvas() until a non-null Canvas is returned
        /// </summary>
        /// <param name="proc"></param>
        public AppProxy(Process proc)
        {
            _appProcess = proc;
        }
        /// <summary>
        /// Constructs an AppProxy for the Process and triggers the AppStartedEvent when the Process' window appears
        /// </summary>
        /// <param name="proc"></param>
        /// <param name="caller"></param>
        /// <param name="appStartedHdlr"></param>
        public AppProxy(Process proc, FrameworkElement caller, double initialWidth, AppStartedHandler appStartedHdlr)
        {
            _initialWidth = initialWidth;
            _appProcess = proc;

            Timer pollForProcess = new Timer(100);
            pollForProcess.AutoReset = false;
            pollForProcess.Elapsed += new ElapsedEventHandler((object sender, ElapsedEventArgs e) => caller.Dispatcher.Invoke(DispatcherPriority.Normal, new ElapsedEventHandler(pollForProcessElapsed), sender, e));
            pollForProcess.Start();

            this.AppStartedEvent += new AppStartedHandler(appStartedHdlr);
        }
        /// <summary>
        /// Starts the specified application and triggers the AppStartedEvent when the application's window appears
        /// </summary>
        /// <param name="appToStart"></param>
        /// <param name="caller"></param>
        /// <param name="appStartedHdlr"></param>o
        public AppProxy(string appToStart, FrameworkElement caller, double initialWidth, AppStartedHandler appStartedHdlr) {
            _initialWidth = initialWidth;
            _appProcess = new Process();
            _appProcess.StartInfo.FileName = appToStart;
            _appProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            _appProcess.Start();
            _appProcess.Exited += new EventHandler((object sender, EventArgs e) => {if (AppExited != null) AppExited(this, null);});

            Timer pollForProcess = new Timer(100);
            pollForProcess.AutoReset = false;
            pollForProcess.Elapsed += new ElapsedEventHandler((object sender, ElapsedEventArgs e) => caller.Dispatcher.Invoke(DispatcherPriority.Normal, new ElapsedEventHandler(pollForProcessElapsed), sender, e));
            pollForProcess.Start();

            this.AppStartedEvent += new AppStartedHandler(appStartedHdlr);
        }

        /// <summary>
        ///  Creates the proxy canvas for the application. 
        /// </summary>
        /// <param name="initialWidth"></param> 
        /// The width of the proxy Canvas.  If 0 is specified, then the proxy's width will match the mirrored app's width.
        /// <returns></returns>
        public Canvas CreateProxyCanvas(double initialWidth) {
            if (findProcess()) {
                updateAppRectangle();
                if (initialWidth == 0)
                    initialWidth = _apprect.Width;
                Image appImage = snapshotApp().ConvertBitmapToWPFImage(initialWidth); // scale the snapshot to be initialWidth
                if (appImage != null && !double.IsNaN(appImage.Width)) {
                    _crop = new Canvas();
                    _crop.Width = initialWidth;
                    _crop.Height = appImage.Height / appImage.Width * initialWidth;
                    changeAppBitmap(appImage);
                    if (AppStartedEvent != null)
                        AppStartedEvent(this, _crop);
                    HookManager.MouseMove += new MouseEventHandler(HookManager_MouseMove);
                    HookManager.MouseDown += new MouseEventHandler((object s, MouseEventArgs e) => _downButtons = e.Button);
                    HookManager.MouseUp += new MouseEventHandler((object s, MouseEventArgs e) => _downButtons = MouseButtons.None);
                    _crop.MouseMove += new System.Windows.Input.MouseEventHandler(cropMouseMove);
                    _grabTimer.Interval = ScreenUpdateInterval;
                    _grabTimer.Elapsed += new System.Timers.ElapsedEventHandler(grabAppWindow);
                    _grabTimer.Start();
                    return _crop;
                }
            }
            return null;
        }
        /// <summary>
        /// whether the proxy application has exited yet
        /// </summary>
        public bool HasExited { get { return _hasExited; } }
        /// <summary>
        /// Suspends all bitmap updates to the Proxy from the Application
        /// </summary>
        public void Suspend() { _suspended = true; }
        /// <summary>
        /// Resumes  updates to the Proxy from the Application
        /// </summary>
        public void   Resume()   { _suspended = false; }
        /// <summary>
        /// The Canvas that contains the mirrored image of the Application being proxied
        /// </summary>
        public Canvas ProxyCanvas { get { return _crop; } }
        /// <summary>
        /// sets the update rate for the proxy when the cursor is inside the application being proxied.
        /// </summary>
        public double ScreenUpdateInterval { 
            get { return _screenUpdateInterval; } 
            set { _screenUpdateInterval = value;}
        }
        /// <summary>
        /// sets the update rate for the proxy when the cursor is inside the application being proxied.
        /// </summary>
        public double IdleUpdateInterval {
            get { return _idleUpdateInterval; }
            set { _idleUpdateInterval = value; }
        }
        /// <summary>
        /// The Process that this AppProxy is mirroring.
        /// email bcz if you need a set'ter for this field
        /// </summary>
        public Process AppProcess { get { return _appProcess; } }
        /// <summary>
        ///  Terminates the proxied application
        /// </summary>
        public void   Terminate()
        {
            _grabTimer.Stop();
            HookManager.MouseMove -= new MouseEventHandler(HookManager_MouseMove);
            if (!_appProcess.HasExited)
                _appProcess.Kill();
        }
    }
}
