/*
 * Copyright (c) 2008, TopCoder, Inc. All rights reserved.
 */
using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using Point = System.Windows.Point;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;



namespace starPadSDK.WPFHelp {
    /// <summary>
    /// <para>
    /// This class implements the <see cref="ISnapshot"/> interface to provide basic implementations of the
    /// required <see cref="ISnapshot"/> functionality. This class uses classes from both System.Windows.Forms
    /// as well as System.Windows (WinForms and WPF) to implement different pieces of the required
    /// functionality.
    /// </para>
    /// </summary>
    ///
    /// <remarks>
    /// <example>
    /// <code>
    /// // we could get an image of that exact window by the following method
    /// Image image = snapshot.TakeSnapshot(window);
    ///
    /// // to take an image of the entire primary screen contents, we could make this call
    /// Image screen = snapshot.TakeSnapshot();
    ///
    /// // or this method, that would give us output that was similar to this, only containing the entire screen
    /// screen = snapshot.TakeSnapshot(Screen.PrimaryScreen.DeviceName);
    ///
    /// // a secondary screen could be captured in the following call:
    /// Image secondaryScreen = snapshot.TakeSnapshot("\\deviceName");
    ///
    /// // to capture just the image in the window, we can make a call similar to this:
    /// Image internalControl = snapshot.TakeSnapshot(window.Image);
    ///
    /// // we can also capture just the button through this call:
    /// Image button = snapshot.TakeSnapshot(window.Button);
    ///
    /// // the user can also choose to just capture a portion of the screen as a rectangle.
    /// // To capture just the dew drop of the image, a potential call could look like this,
    /// // depending on the placement of the actual window on the screen:
    /// Image dewDrop = snapshot.TakeSnapshot(new Rectangle(100, 100, 100, 100));
    /// </code>
    /// </example>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This class itself is immutable and thread safe, although contents of windows and the
    /// screen could be changing when the snapshot is taken. This won't cause a
    /// problem the class itself, but it may cause the output to be slightly different than
    /// expected, if there is a lot of motion on the screen. XAML windows and controls are
    /// rendered directly to a bitmap, ensuring that the window is fully rendered before the image
    /// is created.
    /// </threadsafety>
    ///
    /// <author>Ghostar</author>
    /// <author>TCSDEVELOPER</author>
    /// <version>1.0</version>
    /// <copyright>Copyright (c) 2008, TopCoder, Inc. All rights reserved.</copyright>
    public class BasicSnapshot {
        /// <summary>
        /// <para>
        /// Represents the multi factor for DPI.
        /// </para>
        /// </summary>
        private const double FactorOfDPI = 96.0;

        /// <summary>
        /// <para>
        /// This method grabs a snapshot of the entire contents of the primary display, returning
        /// the result as an Image instance.
        /// </para>
        /// </summary>
        ///
        /// <returns>The image containing the entire contents of the primary display.</returns>
        ///
        /// <exception cref="SnapshotWindowTransparencyException">If any windows in current windows have
        /// a transparency less than 1.</exception>
        /// <exception cref="SnapshotException">If other errors occur while retrieving the snapshot.</exception>
        public static Bitmap TakeSnapshot()
        {
            return TakeSnapshot(Screen.PrimaryScreen.Bounds, Screen.PrimaryScreen);
        }

        /// <summary>
        /// <para>
        /// This method returns an image containing the entire contents of a secondary display,
        /// referenced by the device name given
        /// </para>
        /// </summary>
        ///
        /// <param name="deviceName">The name of the screen to get the snapshot from.</param>
        ///
        /// <returns>The image containing the entire contents of the secondary display.</returns>
        ///
        /// <exception cref="ArgumentNullException">If the given parameter is null.</exception>
        /// <exception cref="ArgumentException">If the device name doesn't match a screen name, or if the
        /// parameter is an empty string.</exception>
        /// <exception cref="SnapshotWindowTransparencyException">If any windows in current windows have
        /// a transparency less than 1.</exception>
        /// <exception cref="SnapshotException">If other errors occur while retrieving the snapshot.</exception>
        public static Bitmap TakeSnapshot(string deviceName)
        {
            ValidateNotNullOrEmpty(deviceName, "deviceName");

            // locate the screen
            Screen screen = FindScreenUsingDeviceName(deviceName);

            // to ensure the values of X, Y properties are 0, because the deviceName refers to the same screen here
            Rectangle bounds = screen.Bounds;
            bounds.X = 0;
            bounds.Y = 0;

            return TakeSnapshot(bounds, screen);
        }

        /// <summary>
        /// <para>
        /// This method returns an image containing the entire contents of the given XAML window
        /// instance.
        /// </para>
        /// </summary>
        ///
        /// <param name="window">The WPF Window to render to the image.</param>
        ///
        /// <returns>The image containing the entire contents of the Window given.</returns>
        ///
        /// <exception cref="ArgumentNullException">If the given parameter is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If none of the XAML window's area is inside the
        /// display area of the system.</exception>
        /// <exception cref="SnapshotWindowTransparencyException">If any windows in current windows have
        /// a transparency less than 1.</exception>
        /// <exception cref="SnapshotException">If other errors occur while retrieving the snapshot.</exception>
        public static Bitmap TakeSnapshot(Window window)
        {
            ValidateNotNull(window, "window");

            // get the topmost window
            Window topmost = null;

            WindowState oldState = window.WindowState;
            bool oldTopmost = window.Topmost;

            try {
                // move the window we want the snapshot of to the top
                if (oldState == WindowState.Minimized) {
                    window.WindowState = WindowState.Normal;
                }

                if (topmost != null) {
                    topmost.Topmost = false;
                }

                window.Topmost = true;

                // take the snapshot
                Rectangle bounds = new Rectangle((int)window.Left, (int)window.Top,
                                                 (int)window.RenderSize.Width, (int)window.RenderSize.Height);
                return TakeSnapshot(bounds);
            }
            finally {
                // move the original window back to the front
                if (topmost != null) {
                    topmost.Topmost = true;
                }

                // restore the property of the window
                window.Topmost = oldTopmost;
                window.WindowState = oldState;
            }
        }

        /// <summary>
        /// <para>
        /// This method returns an image containing the entire contents of the FrameworkElement. WPF controls
        /// extend from this element, so any control can be passed to this method to retrieve an image
        /// of its contents.
        /// </para>
        /// </summary>
        ///
        /// <param name="element">The element to render to an image.</param>
        ///
        /// <returns>The image containing the entire contents of the element given.</returns>
        ///
        /// <exception cref="ArgumentNullException">If the given parameter is null.</exception>
        /// <exception cref="SnapshotException">
        /// If Application.Current or Application.Current.Main has not been set.
        /// If other errors occur while retrieving the snapshot.
        /// </exception>
        public static Bitmap TakeSnapshot(FrameworkElement element)
        {
            ValidateNotNull(element, "element");

            if (Application.Current == null) {
                throw new Exception("Application.Current has not been set.");
            }
            if (Application.Current.MainWindow == null) {
                throw new Exception("Application.Current.MainWindow has not been set.");
            }

            try {
                // update the element to ensure it is rendered
                element.Measure(element.DesiredSize);
                Vector vector = VisualTreeHelper.GetOffset(element);
                element.Arrange(new Rect(new Point(vector.X - element.Margin.Left, vector.Y - element.Margin.Top),
                                         element.DesiredSize));

                // get the bounds of the element
                Rect bounds = VisualTreeHelper.GetDescendantBounds(element);

                // get the window DPI
                Matrix m =
                    PresentationSource.FromVisual(Application.Current.MainWindow).CompositionTarget.TransformToDevice;
                double dx = m.M11 * FactorOfDPI;
                double dy = m.M22 * FactorOfDPI;

                // create the bitmap to hold the rendered result
                RenderTargetBitmap renderBitmap =
                    new RenderTargetBitmap((int)(element.ActualWidth * m.M11), (int)(element.ActualHeight * m.M22),
                                           dx, dy, PixelFormats.Pbgra32);

                // create the rendered control image
                renderBitmap.Render(element);

                // create an encoder for the bitmap
                BitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

                // create a stream
                Stream stream = new MemoryStream();

                // save the bitmap to the stream
                encoder.Save(stream);

                // create the image from the bitmap in the stream
                return new Bitmap(stream);
            }
            catch (Exception e) {
                throw new Exception("Unexpected errors occur while retrieving the snapshot.", e);
            }
        }

        /// <summary>
        /// <para>
        /// This method returns an image containing the contents of the primary display in the
        /// rectangle given, including the rectangle's X, Y, Width and Height properties.
        /// </para>
        /// </summary>
        ///
        /// <param name="bounds">The bounds of the rectangle in the screen to render to an image</param>
        ///
        /// <returns>The image containing the contents of the primary window that fit into the
        /// rectangle given.</returns>
        ///
        /// <exception cref="ArgumentOutOfRangeException">If none of the rectangle's area is inside the
        /// display area of the system.</exception>
        /// <exception cref="SnapshotWindowTransparencyException">If any windows in current windows have
        /// a transparency less than 1.</exception>
        /// <exception cref="SnapshotException">If other errors occur while retrieving the snapshot.</exception>
        public static Bitmap TakeSnapshot(Rectangle bounds)
        {
            foreach (Screen s in Screen.AllScreens)
                if (s.Bounds.Contains(bounds.Location))
                    return TakeSnapshot(new Rectangle(bounds.X-s.Bounds.X,bounds.Y-s.Bounds.Y, bounds.Width, bounds.Height), s);
            return null;
        }

        /// <summary>
        /// <para>
        /// This method returns an image containing the contents of a secondary display in the
        /// rectangle given, including the rectangle's X, Y, Width and Height properties.
        /// </para>
        /// <para>
        /// The secondary display to retrieve the rectangle image from is specified by the device
        /// name given.
        /// </para>
        /// </summary>
        ///
        /// <param name="bounds">The bounds of the rectangle in the screen to render to an image.</param>
        /// <param name="deviceName">The device name of the display to get the rectangle from.</param>
        ///
        /// <returns>The image containing the contents of the primary window that fit into the
        /// rectangle given.</returns>
        ///
        /// <exception cref="ArgumentNullException">If the given parameter is null.</exception>
        /// <exception cref="ArgumentException">If the device name doesn't match a screen name, or if the
        /// parameter is an empty string.</exception>
        /// <exception cref="ArgumentOutOfRangeException">If none of the rectangle's area is inside the
        /// display area of the display name given.</exception>
        /// <exception cref="SnapshotWindowTransparencyException">If any windows in current windows have
        /// a transparency less than 1.</exception>
        /// <exception cref="SnapshotException">If other errors occur while retrieving the snapshot.</exception>
        public static Bitmap TakeSnapshot(Rectangle bounds, string deviceName)
        {
            ValidateNotNullOrEmpty(deviceName, "deviceName");

            // locate the screen
            Screen screen = FindScreenUsingDeviceName(deviceName);

            // take the snapshot
            return TakeSnapshot(bounds, screen);
        }

        /// <summary>
        /// <para>
        /// This method returns an image containing the contents of a secondary display in the
        /// rectangle given, including the rectangle's X, Y, Width and Height properties.
        /// </para>
        /// <para>
        /// The secondary display to retrieve the rectangle image from is specified by the screen given.
        /// </para>
        /// </summary>
        ///
        /// <param name="bounds">The bounds of the rectangle in the screen to render to an image.</param>
        /// <param name="screen">The display screen to get the rectangle from.</param>
        ///
        /// <returns>The image containing the contents of the primary window that fit into the
        /// rectangle given.</returns>
        ///
        /// <exception cref="ArgumentOutOfRangeException">If none of the rectangle's area is inside the
        /// display area of the display name given.</exception>
        /// <exception cref="SnapshotWindowTransparencyException">If any windows in current windows have
        /// a transparency less than 1.</exception>
        /// <exception cref="SnapshotException">If other errors occur while retrieving the snapshot.</exception>
        public static Bitmap TakeSnapshot(Rectangle bounds, Screen screen) {
            // add the X and Y properties of the screen's Bounds property, properly orienting the bounds
            // rectangle to the proper display
            if (bounds.Width == 0)
                bounds = screen.Bounds;
            else {
                bounds.X += screen.Bounds.X;
                bounds.Y += screen.Bounds.Y;
            }

            if (bounds.X > screen.Bounds.Right || bounds.Y > screen.Bounds.Bottom) {
                throw new ArgumentOutOfRangeException("bounds", screen.Bounds,
                                                      "None of the rectangle's area is inside the display area.");
            }

            if (bounds.X + bounds.Width < screen.Bounds.Left || bounds.Y + bounds.Height < screen.Bounds.Top) {
                throw new ArgumentOutOfRangeException("bounds", screen.Bounds,
                                                      "None of the rectangle's area is inside the display area.");
            }


            try {
                // create a Bitmap the same size as the rectangle given
                Bitmap image = new Bitmap(bounds.Width, bounds.Height);

                // create a graphics context for the bitmap
                using (Graphics graphics = Graphics.FromImage(image)) {
                    // fill the bitmap
                    graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
                }

                // return the result
                return image;
            }
            catch (Exception e) {
                throw new Exception("Unexpected errors occur while retrieving the snapshot.", e);
            }
        }

        /// <summary>
        /// <para>
        /// This method finds the screen specified by the given device name.
        /// </para>
        /// </summary>
        ///
        /// <param name="deviceName">The device name of the display to get the rectangle from.</param>
        ///
        /// <returns>The screen specified by the given device name.</returns>
        ///
        /// <exception cref="ArgumentException">If the device name doesn't match a screen name.</exception>
        private static Screen FindScreenUsingDeviceName(string deviceName) {
            foreach (Screen screen in Screen.AllScreens) {
                if (screen.DeviceName == deviceName) {
                    return screen;
                }
            }

            throw new ArgumentException(
                string.Format("There is no screen matching the given deviceName '{0}'.", deviceName), "deviceName");
        }

        /// <summary>
        /// <para>
        /// Validates the value of a variable. The value cannot be <c>null</c>.
        /// </para>
        /// </summary>
        ///
        /// <param name="value">The value of the variable to be validated.</param>
        /// <param name="name">The name of the variable to be validated.</param>
        ///
        /// <exception cref="ArgumentNullException">
        /// The value of the variable is <c>null</c>.
        /// </exception>
        private static void ValidateNotNull(object value, string name) {
            if (value == null) {
                throw new ArgumentNullException(name, name + " cannot be null.");
            }
        }

        /// <summary>
        /// <para>
        /// Validates the value of a string variable. The value cannot be empty string after
        /// trimming.
        /// </para>
        /// </summary>
        ///
        /// <param name="value">The value of the variable to be validated.</param>
        /// <param name="name">The name of the variable to be validated.</param>
        ///
        /// <exception cref="ArgumentException">
        /// The value of the variable is empty string.
        /// </exception>
        private static void ValidateNotEmpty(string value, string name) {
            if (value != null && value.Trim().Length == 0) {
                throw new ArgumentException(name + " cannot be empty string.", name);
            }
        }

        /// <summary>
        /// <para>
        /// Validates the value of a string variable. The value cannot be <c>null</c> or empty string after
        /// trimming.
        /// </para>
        /// </summary>
        ///
        /// <param name="value">The value of the variable to be validated.</param>
        /// <param name="name">The name of the variable to be validated.</param>
        ///
        /// <exception cref="ArgumentNullException">
        /// The value of the variable is <c>null</c>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The value of the variable is empty string.
        /// </exception>
        private static void ValidateNotNullOrEmpty(string value, string name) {
            ValidateNotNull(value, name);
            ValidateNotEmpty(value, name);
        }
    }

    /// <summary>
    /// Provides functions to capture the entire screen, or a particular window, and save it to a file.
    /// </summary>
    public class ScreenCapture {
        /// <summary>
        /// Creates an Image object containing a screen shot of the entire desktop
        /// </summary>
        /// <returns></returns>
        static public Image CaptureScreen() {
            return CaptureWindow(ComSupport.GetDesktopWindow());
        }
        /// <summary>
        /// Creates an Image object containing a screen shot of a specific window
        /// </summary>
        /// <param name="handle">The handle to the window. (In windows forms, this is obtained by the Handle property)</param>
        /// <returns></returns>
        static public Bitmap CaptureWindow(IntPtr handle) {
            // get te hDC of the target window
            IntPtr hdcSrc = ComSupport.GetWindowDC(handle);
            // get the size
            RECT windowRect = new RECT();
            ComSupport.GetWindowRect(handle, windowRect);
            int width = windowRect.Right - windowRect.Left;
            int height = windowRect.Bottom - windowRect.Top;
            // create a device context we can copy to
            IntPtr hdcDest = GDI32.CreateCompatibleDC((IntPtr)0);
            // create a bitmap we can copy it to,
            // using GetDeviceCaps to get the width/height
            IntPtr hBitmap = GDI32.CreateCompatibleBitmap(hdcSrc, width, height);
            // select the bitmap object
            IntPtr hOld = GDI32.SelectObject(hdcDest, hBitmap);
            // bitblt over
            GDI32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, GDI32.SRCCOPY);
            // restore selection
            GDI32.SelectObject(hdcDest, hOld);
            // clean up 
            GDI32.DeleteDC(hdcDest);
            ComSupport.ReleaseDC(handle, hdcSrc);
            // get a .NET image object for it
            Bitmap img = Bitmap.FromHbitmap(hBitmap);
            Bitmap newImg = new Bitmap((int)img.Width, (int)img.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            Graphics.FromImage(newImg).DrawImage(img, new PointF(0, 0));
            // free up the Bitmap object

            GDI32.DeleteObject(hBitmap);

            return newImg;
        }
        static Bitmap CaptureCursor(ref int x, ref int y) {
            Bitmap bmp;
            IntPtr hicon;
            Win32Stuff.CURSORINFO ci = new Win32Stuff.CURSORINFO();
            Win32Stuff.ICONINFO icInfo;
            ci.cbSize = Marshal.SizeOf(ci);
            if (Win32Stuff.GetCursorInfo(ref ci)) {
                if (ci.flags == Win32Stuff.CURSOR_SHOWING) {
                    hicon = Win32Stuff.CopyIcon(ci.hCursor);
                    if (Win32Stuff.GetIconInfo(hicon, out icInfo)) {
                        x = ci.ptScreenPos.x - ((int)icInfo.xHotspot);
                        y = ci.ptScreenPos.y - ((int)icInfo.yHotspot);
                        Icon ic = Icon.FromHandle(hicon);
                        bmp = ic.ToBitmap();

                        return bmp;
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// Captures a screen shot of a specific window, and saves it to a file
        /// </summary>
        /// <param name="handle"></param>
        /// <param name="filename"></param>
        /// <param name="format"></param>
        static public void CaptureWindowToFile(IntPtr handle, string filename, ImageFormat format) {
            Image img = CaptureWindow(handle);
            img.Save(filename, format);
        }
        /// <summary>
        /// Captures a screen shot of the entire desktop, and saves it to a file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="format"></param>
        static public void CaptureScreenToFile(string filename, ImageFormat format) {
            Image img = CaptureScreen();
            img.Save(filename, format);
        }

        /// <summary>
        /// Helper class containing Gdi32 API functions
        /// </summary>
        public class GDI32 {

            public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter
            [DllImport("gdi32.dll")]
            public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
                int nWidth, int nHeight, IntPtr hObjectSource,
                int nXSrc, int nYSrc, int dwRop);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth,
                int nHeight);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteObject(IntPtr hObject);
            [DllImport("gdi32.dll")]
            public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        }
    }

    /*
    public static class Direct3DCapture {
        private static SlimDX.Direct3D9.Direct3D _direct3D9 = new SlimDX.Direct3D9.Direct3D();
        private static Dictionary<IntPtr, SlimDX.Direct3D9.Device> _direct3DDeviceCache = new Dictionary<IntPtr, SlimDX.Direct3D9.Device>();

        /// <summary>
        /// Capture the entire client area of a window
        /// </summary>
        /// <param name="hWnd"></param>
        /// <returns></returns>
        public static Bitmap CaptureWindow(IntPtr hWnd) {
            return CaptureRegionDirect3D(hWnd, NativeMethods.GetAbsoluteClientRect(hWnd));
        }

        /// <summary>
        /// Capture a region of the screen using Direct3D
        /// </summary>
        /// <param name="handle">The handle of a window</param>
        /// <param name="region">The region to capture (in screen coordinates)</param>
        /// <returns>A bitmap containing the captured region, this should be disposed of appropriately when finished with it</returns>
        public static Bitmap CaptureRegionDirect3D(IntPtr handle, Rectangle region) {
            IntPtr hWnd = handle;
            Bitmap bitmap = null;

            // We are only supporting the primary display adapter for Direct3D mode
            SlimDX.Direct3D9.AdapterInformation adapterInfo = _direct3D9.Adapters.DefaultAdapter;
            SlimDX.Direct3D9.Device device;

            #region Get Direct3D Device
            // Retrieve the existing Direct3D device if we already created one for the given handle
            if (_direct3DDeviceCache.ContainsKey(hWnd)) {
                device = _direct3DDeviceCache[hWnd];
            }
            // We need to create a new device
            else {
                // Setup the device creation parameters
                SlimDX.Direct3D9.PresentParameters parameters = new SlimDX.Direct3D9.PresentParameters();
                parameters.BackBufferFormat = adapterInfo.CurrentDisplayMode.Format;
                Rectangle clientRect = NativeMethods.GetAbsoluteClientRect(hWnd);
                parameters.BackBufferHeight = clientRect.Height;
                parameters.BackBufferWidth = clientRect.Width;
                parameters.Multisample = SlimDX.Direct3D9.MultisampleType.None;
                parameters.SwapEffect = SlimDX.Direct3D9.SwapEffect.Discard;
                parameters.DeviceWindowHandle = hWnd;
                parameters.PresentationInterval = SlimDX.Direct3D9.PresentInterval.Default;
                parameters.FullScreenRefreshRateInHertz = 0;

                // Create the Direct3D device
                device = new SlimDX.Direct3D9.Device(_direct3D9, adapterInfo.Adapter, SlimDX.Direct3D9.DeviceType.Hardware, hWnd, SlimDX.Direct3D9.CreateFlags.SoftwareVertexProcessing, parameters);
                _direct3DDeviceCache.Add(hWnd, device);
            }
            #endregion

            // Capture the screen and copy the region into a Bitmap
            using (SlimDX.Direct3D9.Surface surface = SlimDX.Direct3D9.Surface.CreateOffscreenPlain(device, adapterInfo.CurrentDisplayMode.Width, adapterInfo.CurrentDisplayMode.Height, SlimDX.Direct3D9.Format.A8R8G8B8, SlimDX.Direct3D9.Pool.SystemMemory)) {
                device.GetFrontBufferData(0, surface);

                bitmap = new Bitmap(SlimDX.Direct3D9.Surface.ToStream(surface, SlimDX.Direct3D9.ImageFileFormat.Bmp, new Rectangle(region.Left, region.Top, region.Right, region.Bottom)));
            }

            return bitmap;
        }
    }
    */
    #region Native Win32 Interop
    [System.Security.SuppressUnmanagedCodeSecurity()]
    internal sealed class NativeMethods {
        [DllImport("user32.dll")]
        internal static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        /// <summary>
        /// Get a windows client rectangle in a .NET structure
        /// </summary>
        /// <param name="hwnd">The window handle to look up</param>
        /// <returns>The rectangle</returns>
        internal static Rectangle GetClientRect(IntPtr hwnd) {
            WPFHelp.RECT crect = new WPFHelp.RECT();
            ComSupport.GetClientRect(hwnd, crect);
            return crect.AsRectangle;
            // RECT rect = new RECT();
            //GetClientRect(hwnd, out rect);
            //return rect.AsRectangle;
        }

        /// <summary>
        /// Get a windows rectangle in a .NET structure
        /// </summary>
        /// <param name="hwnd">The window handle to look up</param>
        /// <returns>The rectangle</returns>
        internal static Rectangle GetWindowRect(IntPtr hwnd) {
            WPFHelp.RECT wrect = new WPFHelp.RECT();
            ComSupport.GetWindowRect(hwnd, wrect);
            return wrect.AsRectangle;
            //RECT rect = new RECT();
            // GetWindowRect(hwnd, out rect);
            //return rect.AsRectangle;
        }

        internal static Rectangle GetAbsoluteClientRect(IntPtr hWnd) {
            Rectangle windowRect = NativeMethods.GetWindowRect(hWnd);
            Rectangle clientRect = NativeMethods.GetClientRect(hWnd);

            // This gives us the width of the left, right and bottom chrome - we can then determine the top height
            int chromeWidth = (int)((windowRect.Width - clientRect.Width) / 2);

            return new Rectangle(new System.Drawing.Point(windowRect.X + chromeWidth, windowRect.Y + (windowRect.Height - clientRect.Height - chromeWidth)), clientRect.Size);
        }
    }
    #endregion
}
