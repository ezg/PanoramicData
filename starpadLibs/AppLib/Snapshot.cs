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

namespace AJournal {
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
    public class BasicSnapshot  {
        /// <summary>
        /// <para>
        /// Represents the multi factor for DPI.
        /// </para>
        /// </summary>
        private const double FactorOfDPI = 96.0;

        /// <summary>
        /// <para>
        /// Creates a new instance of <see cref="BasicSnapshot"/>. This is a do nothing,
        /// default constructor.
        /// </para>
        /// </summary>
        public BasicSnapshot() {
        }

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
        public Image TakeSnapshot() {
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
        public Image TakeSnapshot(string deviceName) {
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
        public Image TakeSnapshot(Window window) {
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
        public Image TakeSnapshot(FrameworkElement element) {
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
        public Image TakeSnapshot(Rectangle bounds) {
            return TakeSnapshot(bounds, Screen.PrimaryScreen);
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
        public Image TakeSnapshot(Rectangle bounds, string deviceName) {
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
        private Image TakeSnapshot(Rectangle bounds, Screen screen) {
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
                    graphics.CopyFromScreen(bounds.X, bounds.Y, 0, 0, bounds.Size);
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
}
