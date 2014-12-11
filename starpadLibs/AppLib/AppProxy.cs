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
using Timer = System.Timers.Timer;
using StarPadSDK.Points;
using WPFHelp;

namespace AnApp {
    public class AppProxy {
        System.Drawing.Bitmap _appBitmap = null;
        Timer      _grabTimer = new Timer();
        Timer      _warpTimer = new Timer(1);
        Process    _appProcess = null;
        Canvas     _crop = new Canvas();
        Image      _appImage = null;
        IntPtr     _mainWind = (IntPtr)0;
        Rct         _apprect = Rct.Null, _proxy;

        delegate void updateDelegate();
        delegate void warpDelegate(object sender);

        IntPtr findProcessWindow() {
            RECT foo = new RECT();
            RECT foo2 = new RECT();
            IntPtr window = (IntPtr)0;
            Process[] pp = System.Diagnostics.Process.GetProcesses();
            List<Process> foundProc = new List<Process>();
            foreach (Process pa in pp)
                if (pa.MainWindowHandle != (IntPtr)0) {
                    if (pa.Id == _appProcess.Id) {
                        foundProc.Add(pa);
                    }
                }
            if (foundProc.Count > 0)
                _appProcess = foundProc[0];

            if (window == (IntPtr)0)
                window = _appProcess.MainWindowHandle;
            ComSupport.GetWindowRect(_appProcess.MainWindowHandle, foo);
            ComSupport.GetWindowRect(window, foo2);
            if (foo.Right - foo.Left >= foo2.Right - foo2.Left)
                window = _appProcess.MainWindowHandle;
            StringBuilder lpString = new StringBuilder(1000);
            if (window != (IntPtr)0)
                ComSupport.GetWindowText(window, lpString, 1000);
            if (lpString.ToString() == "")
                return (IntPtr)0;
            return window;
        }
        void pollForProcessElapsed(object sender, ElapsedEventArgs e) {
            double initialWidth = 100;
            Image appImage = snapshotApp(initialWidth); // scale the snapshot to be 100 pixels wide
            if (appImage != null && !double.IsNaN(appImage.Width)) {
                _crop.Width = initialWidth;
                _crop.Height = appImage.Height / appImage.Width * initialWidth;
                changeAppBitmap(appImage);
                if (AppStartedEvent != null)
                    AppStartedEvent(this, _crop);
                _crop.MouseMove += new System.Windows.Input.MouseEventHandler(cropMouseMove);
                _grabTimer.Interval = 200;
                _grabTimer.Elapsed += new System.Timers.ElapsedEventHandler(grabAppWindow);
                _grabTimer.Start();
            }
            else
                (sender as Timer).Start();
        }

        // takes a snapshot of the application window
        Image snapshotApp(double initialWidth) {
            _mainWind = findProcessWindow();
            if (_mainWind == (IntPtr)0)
                _appBitmap = BasicSnapshot.TakeSnapshot(new System.Drawing.Rectangle((int)_apprect.Left, (int)_apprect.Top, (int)_apprect.Width, (int)_apprect.Height));
            else _appBitmap = ScreenCapture.CaptureWindow(_mainWind);
            
            return WPFUtil.ConvertBitmapToWPFImage(initialWidth, _appBitmap);
        }

        // switch the proxy bitmap of the application window
        void changeAppBitmap(Image appImage) {
            if (_appImage != null)
                _crop.Children.Remove(_appImage);
            Rct appRect = new Rct(0, 0, appImage.Width, appImage.Height);
            Rct scaledRct = ((Mat)_crop.RenderTransform.Value) * appRect;
            _crop.Height = appImage.Height / appImage.Width * _crop.Width;
            _crop.Children.Insert(0, appImage);
            _crop.ClipToBounds = false;
            _appImage = appImage;
        }

        // grabs the bitmap of the application window to display in the proxy window
        void grabAppWindow(object sender, System.Timers.ElapsedEventArgs e) {
            _crop.Parent.Dispatcher.Invoke(DispatcherPriority.Normal, new updateDelegate(grabAppWindowDelegate));
        }
        void grabAppWindowDelegate() {
            _grabTimer.Enabled = false;
            Image appImage = snapshotApp(_crop.Width);
            if (appImage != null)
                changeAppBitmap(appImage);
            updateProxyCursor(_warpTimer.Interval > 1 ? _warpTimer : _grabTimer);
            _grabTimer.Enabled = true;
        }

        // warps the cursor to the real application window and starts a timer for grabbing the cursor image
        void startCursorWarpTimer() {
            _warpTimer.Interval = 20;
            _warpTimer.Elapsed -= new ElapsedEventHandler(warper_Elapsed);
            _warpTimer.Elapsed += new ElapsedEventHandler(warper_Elapsed);
            _warpTimer.Start();
        }
        void updateProxyRectangle() {
            Window rootWin = System.Windows.Application.Current.MainWindow;
            Pt tlApp = rootWin.PointToScreen(new Point());
            Pt brApp = rootWin.PointToScreen(new Point(rootWin.Width, rootWin.Height));
            Pt tl = WPFUtil.TransformFromAtoB(new Pt(), _crop, rootWin);
            Pt br = WPFUtil.TransformFromAtoB(new Pt(_crop.Width, _crop.Height), _crop, rootWin);
            _proxy = new Rct(tl + new Vec(tlApp.X, tlApp.Y), br + new Vec(tlApp.X, tlApp.Y));
        }
        void updateAppRectangle() {
            IntPtr window = findProcessWindow();
            if (window != (IntPtr)0) {
                RECT _winBounds = new RECT();
                ComSupport.GetWindowRect(window, _winBounds);
                _apprect = new Rct(_winBounds.Left, _winBounds.Top, _winBounds.Right, _winBounds.Bottom);
            }
        }
        void cropMouseMove(object sender, System.Windows.Input.MouseEventArgs e) {
            updateAppRectangle();
            updateProxyRectangle();

            System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)(_apprect.Left + e.GetPosition(_crop).X / _crop.Width * _apprect.Width),
                                                                                                                   (int)(_apprect.Top + e.GetPosition(_crop).Y / _crop.Height * _apprect.Height));

            startCursorWarpTimer();

            _crop.MouseMove -= new System.Windows.Input.MouseEventHandler(cropMouseMove);
        }

        // updates the cursor in the proxy window
        void warper_Elapsed(object sender, ElapsedEventArgs e) {
            (sender as Timer).Enabled = false;
           if ((sender as Timer).Interval != 1)
                _crop.Parent.Dispatcher.Invoke(DispatcherPriority.Send, new warpDelegate(warperDelegate),sender);
        }
        void warperDelegate(object sender) {
            if (updateProxyCursor(sender as Timer)) {
                (sender as Timer).Interval = 10;
                (sender as Timer).Enabled = true;
            }
            else {
                (sender as Timer).Interval = 1;
            }
        }

        bool updateProxyWindowFromAppWindowChanges(Timer sender) {
            bool changed =false;
            Rct oldApprect = _apprect;
            updateAppRectangle();
            updateProxyRectangle();
            if (oldApprect != Rct.Null && oldApprect.TopLeft != _apprect.TopLeft) {
                _proxy = _proxy.Translated(_apprect.TopLeft - oldApprect.TopLeft);
                // this will drag the proxy window -- we only want to do that if the user thinks they are clicking the title bar of the proxy window.
                // if they are interacting with the real window, then we don't update the proxy.
                // We identify what is happening by looking at the sender -- if the sender is the warpTimer, then we know the user was interacting
                // with the proxy, and so we can move the proxy accordingly.
                if (sender == _warpTimer)
                    _crop.RenderTransform = new MatrixTransform(((Mat)_crop.RenderTransform.Value) * Mat.Translate(_apprect.TopLeft - oldApprect.TopLeft));
                changed = true;
            }
            if (oldApprect != Rct.Null && oldApprect.Size != _apprect.Size) {
                _proxy.Right = _proxy.Left + _apprect.Width / (double)oldApprect.Width * _proxy.Width;
                _proxy.Bottom = _proxy.Top + _apprect.Height / (double)oldApprect.Height * _proxy.Height;
                _crop.Width = _proxy.Width / _crop.RenderTransform.Value.M11;
                _crop.Height = _proxy.Height / _crop.RenderTransform.Value.M22;
                changed = true;
            }
            return changed;
        }

        bool updateProxyCursor(Timer sender) {
            lock (this) {
                if (sender.Interval > 1)
                    return updateProxyCursorLocked(sender);
            }
            return false;
        }
        bool updateProxyCursorLocked(Timer sender) {
            bool keepWarping = true;
            bool windowChanged = updateProxyWindowFromAppWindowChanges(sender);

            ComSupport.POINT cursorPt;
            ComSupport.GetCursorPos(out cursorPt);
            System.Drawing.Point cursorInTargetBMP = new System.Drawing.Point((int)(cursorPt.x - _apprect.Left), (int)(cursorPt.y - _apprect.Top));
            if (!_apprect.Contains(new Pt(cursorPt.x, cursorPt.y)) && !windowChanged) {
                if (sender != _warpTimer) {
                    return false;
                }
                (sender as Timer).Interval = 1;
                (sender as Timer).Enabled = false;
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)(_proxy.Left + (cursorPt.x - _apprect.Left) / _apprect.Width * _proxy.Width),
                                                                                                                       (int)(_proxy.Top + (cursorPt.y - _apprect.Top) / _apprect.Height * _proxy.Height));
                _crop.MouseMove -= new System.Windows.Input.MouseEventHandler(cropMouseMove);
                _crop.MouseMove += new System.Windows.Input.MouseEventHandler(cropMouseMove);
                return false;
            }
            System.Drawing.Bitmap appCursBitmap = CaptureScreen.AddCursorToBitmap(cursorInTargetBMP, _appBitmap);
            changeAppBitmap(WPFUtil.ConvertBitmapToWPFImage(_crop.Width, appCursBitmap));
            return keepWarping;
        }

        public delegate void AppStartedHandler(AppProxy sender, Canvas proxy);
        public event AppStartedHandler AppStartedEvent;
        public AppProxy(string appToStart, FrameworkElement caller, AppStartedHandler appStartedHdlr)  {
            _appProcess = new Process();
            _appProcess.StartInfo.FileName = appToStart;
            _appProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            _appProcess.Start();

            Timer pollForProcess = new Timer(100);
            pollForProcess.AutoReset = false;
            pollForProcess.Elapsed += new ElapsedEventHandler((object sender, ElapsedEventArgs e) => caller.Dispatcher.Invoke(DispatcherPriority.Normal, new ElapsedEventHandler(pollForProcessElapsed), sender, e));
            pollForProcess.Start();

            this.AppStartedEvent += new AppStartedHandler(appStartedHdlr);
        }
        public void Terminate()
        {
            _warpTimer.Stop();
            _grabTimer.Stop();
            _appProcess.Kill();
        }
    }
}
