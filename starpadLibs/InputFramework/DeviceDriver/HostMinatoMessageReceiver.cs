/*
Notes:  Data structures and class to host win32 window to receive messages from Sharp's pen driver for IWB products PN-702B and PN-602B

Version: .1 
Author:  Richard J. Campbell, Ph.D.   rcampbell@sharplabs.com
Date: 5/4/2012
Changes:
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using InputFramework;
using InputFramework.WPFDevices;
using InputFramework.DeviceDriver;



namespace InputFramework.DeviceDriver
{
    #region event delegates and arguments
    public delegate void TouchPanelModeChangedEventHandler(object sender, TouchPanelModeChangedArgs e);
    public delegate void TouchPanelEraserOnEventHandler(object sender,TouchPanelEraserOnArgs e);
    public delegate void TouchPanelEraserOffEventHandler(object sender,EventArgs e);

    public class TouchPanelModeChangedArgs : EventArgs
    {
        private readonly bool _autoMode;
        private readonly bool _penMode;
        public TouchPanelModeChangedArgs(bool auto, bool pen)
        {
            _autoMode = auto;
            _penMode = pen;
        }
        public bool PenMode
        {
            get { return _penMode; }
        }
        public bool AutoMode
        {
            get { return _autoMode; }
        }
    }

    public class TouchPanelEraserOnArgs : EventArgs
    {
        private readonly double _width;
        private readonly double _height;

        public TouchPanelEraserOnArgs(double width, double height)
        {
            _width = width;
            _height = height;
        }
        public double Width
        {
            get {
                return _width; }
        }
        public double Height
        {
            get { return _height; }
        }
       
    }
    #endregion

    public class HostMinatoMessageReceiver 
    {
        IntPtr hwndHost;
        IntPtr hEraserSize;
        WndProcDelegate persistentWndProc; // keep a reference of our WndProc to keep the GC from collecting it since it will be passed to unmanaged callback

        public event TouchPanelModeChangedEventHandler TouchPanelModeChangedEvent;
        public event TouchPanelEraserOnEventHandler TouchPanelEraserOnEvent;
        public event TouchPanelEraserOffEventHandler TouchPanelEraserOffEvent;

        private double _width;
        private double _height;
        public double EraserWidth
        {
            get {

                return _width; }
        }
        public double EraserHeight
        {
            get {
                return _height; }
        }

        public HostMinatoMessageReceiver()
        {
            hEraserSize=IntPtr.Zero;
            persistentWndProc = myWndProcDelegate;
            // register the class
            try
            {
              WNDCLASSEX wndClassEx = new WNDCLASSEX()
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(WNDCLASSEX)),
                    style = ClassStyles.DoubleClicks,
                    cbClsExtra = 0,
                    cbWndExtra = 0,
                    hbrBackground = IntPtr.Zero,
                    hCursor = IntPtr.Zero,
                    hIcon = IntPtr.Zero,
                    hIconSm = IntPtr.Zero,
                    lpszClassName = MINATO_MESSAGERECEIVER_CLASSNAME,
                    lpszMenuName = null,
                    hInstance = Marshal.GetHINSTANCE(this.GetType().Module),
                    lpfnWndProc = persistentWndProc
                };




                ushort atom = RegisterClassEx(ref wndClassEx);
                int lasterror;
                if(atom==0)
                    lasterror = Marshal.GetLastWin32Error();
                hwndHost = CreateWindowEx(0, atom, MINATO_MESSAGERECEIVER_WINDOWNAME,
                                           WS_POPUP | WS_MINIMIZEBOX | WS_SYSMENU | WS_MAXIMIZEBOX, // WS_CHILD is important for HwndHost WinProc and message hooks to work (WS_POPUP enabled FindWindow to work correctly)
                                           0, 0,
                                           0, 0,
                                           IntPtr.Zero,
                                           IntPtr.Zero,
                                           Marshal.GetHINSTANCE(this.GetType().Module),
                                           0); 
                if (hwndHost == IntPtr.Zero)
                    lasterror = Marshal.GetLastWin32Error();
                /* debug info */
                WNDCLASSEX wndClassEx2 = new WNDCLASSEX();
                bool check = GetClassInfoEx(Marshal.GetHINSTANCE(this.GetType().Module), MINATO_MESSAGERECEIVER_CLASSNAME, ref wndClassEx2);
                IntPtr hwndTest2 = FindWindow(MINATO_MESSAGERECEIVER_CLASSNAME, MINATO_MESSAGERECEIVER_WINDOWNAME);
            }
            catch
            {
                int error = Marshal.GetLastWin32Error();
            }

        }

        ~HostMinatoMessageReceiver()
        {
            DestroyWindow(hwndHost);
        }


        private IntPtr myWndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_MINATO_TOUCHPANEL_ERASER_OFF:
                    EventArgs eOff = new EventArgs();
                    OnTouchPanelEraserOffEvent(eOff);
                    lock (this)
                    {
                        hEraserSize = IntPtr.Zero;
                    }
                    break;
                case WM_MINATO_TOUCHPANEL_ERASER_ON:
                    hEraserSize=wParam;
                    UpdateEraserDim();
                    TouchPanelEraserOnArgs eOn = new TouchPanelEraserOnArgs(_width,_height);
                    OnTouchPanelEraserOnEvent(eOn);
                    break;
                case WM_MINATO_TOUCHPANEL_MODE_CHANGED:
                    TouchPanelModeChangedArgs e = new TouchPanelModeChangedArgs((bool)(wParam.ToInt32()==1), (bool)(lParam.ToInt32()==1));
                    OnTouchPanelModeChangedEvent(e);
                    break;
                default:
                    return DefWindowProc(hWnd, msg, wParam, lParam);  // call the default window's procedure to handle messages if we don't directly handle them
            }
            return IntPtr.Zero;
        }

        public void UpdateEraserDim()  
        {
            _width = 0.0; _height = 0.0;
            StringBuilder eTxt = new StringBuilder();
            lock (this)
            {
                if (hEraserSize != IntPtr.Zero)
                {
                    if (IsWindow(hEraserSize))
                    {
                        int len = GetWindowText(hEraserSize, eTxt, 20);
                        if (len > 0)
                        {
                            string seTxt = eTxt.ToString();
                            int idxSep = seTxt.IndexOf(",");
                            string wstring = seTxt.Substring(0, idxSep - 1);
                            Double.TryParse(wstring, out _width);
                            string hstring = seTxt.Substring(idxSep + 1);
                            Double.TryParse(hstring, out _height);
                        }
                    }
                }
            }
        }

        
        #region raise event methods
        protected  void OnTouchPanelModeChangedEvent(TouchPanelModeChangedArgs e)
        {
            if (TouchPanelModeChangedEvent != null)
            {
                TouchPanelModeChangedEvent(this, e);
            }
        }

        protected void OnTouchPanelEraserOnEvent(TouchPanelEraserOnArgs e)
        {
            if (TouchPanelEraserOnEvent != null)
            {
                TouchPanelEraserOnEvent(this, e);
            }
        }

        protected  void OnTouchPanelEraserOffEvent(EventArgs e)
        {
            if (TouchPanelEraserOffEvent != null)
            {
                TouchPanelEraserOffEvent(this, e);
            }
        }
        #endregion

        #region PInvoke win32 declarations
        [DllImport("user32.dll", SetLastError=true, EntryPoint = "CreateWindowEx", CharSet = CharSet.Unicode)]
        static extern IntPtr CreateWindowEx(int dwExStyle,
                                                      ushort lpszClassName,
                                                      string lpszWindowName,
                                                      uint style,
                                                      int x, int y,
                                                      int width, int height,
                                                      IntPtr hwndParent,
                                                      IntPtr hMenu,
                                                      IntPtr hInst,
                                                      [MarshalAs(UnmanagedType.AsAny)] object pvParam);
        [DllImport("user32.dll", SetLastError = true, EntryPoint = "CreateWindowEx", CharSet = CharSet.Unicode)]
        static extern IntPtr CreateWindowEx(int dwExStyle,
                                                      string lpszClassName,
                                                      string lpszWindowName,
                                                      uint style,
                                                      int x, int y,
                                                      int width, int height,
                                                      IntPtr hwndParent,
                                                      IntPtr hMenu,
                                                      IntPtr hInst,
                                                      [MarshalAs(UnmanagedType.AsAny)] object pvParam);


        [DllImport("user32.dll", EntryPoint = "RegisterClassEx", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.U2)]
        static extern ushort RegisterClassEx([In] ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Unicode)]
        internal static extern System.IntPtr FindWindow(String lpClassName, String lpWindowName);


        [DllImport("user32.dll", EntryPoint = "GetClassInfoEx", CharSet = CharSet.Unicode)]
        static extern bool GetClassInfoEx(IntPtr hInst, string lpszClassName, [In, Out] ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", EntryPoint = "DestroyWindow", CharSet = CharSet.Unicode)]
        static extern bool DestroyWindow(IntPtr hwnd);

        [DllImport("user32.dll", EntryPoint = "DefDlgProc", CharSet = CharSet.Unicode)]
        static extern IntPtr DefDlgProc(IntPtr hDlg, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "DefWindowProc", CharSet = CharSet.Unicode)]
        static extern IntPtr DefWindowProc(IntPtr hDlg, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "GetWindowText", CharSet = CharSet.Unicode)]
        static extern int GetWindowText(IntPtr hwnd, StringBuilder lpWindowText, int nMaxCount);

        [DllImport("user32.dll", EntryPoint = "IsWindow", CharSet = CharSet.Unicode)]
        static extern bool IsWindow(IntPtr hwnd);

        [DllImport("user32.dll", EntryPoint = "LoadCursor", CharSet = CharSet.Unicode)]
        static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);



        internal const uint
        WS_MINIMIZEBOX = 0x00020000,
        WS_MAXIMIZEBOX = 0x00010000,
        WS_SYSMENU = 0x00080000,
        WS_POPUP = 0x80000000,
        WS_CHILD = 0x40000000,
        WS_VISIBLE = 0x10000000,
        LBS_NOTIFY = 0x00000001,
        HOST_ID = 0x00000002,
        CONTROL_ID = 0x00000001,
        WS_VSCROLL = 0x00200000,
        WS_BORDER = 0x00800000;
        internal const uint
        WM_MOUSEMOVE = 0x0200,
        WM_USER = 0x0400,
        WM_MINATO_TOUCHPANEL_ERASER_ON = WM_USER + 300,
        WM_MINATO_TOUCHPANEL_ERASER_OFF = WM_USER + 301,
        WM_MINATO_TOUCHPANEL_MODE_CHANGED = WM_USER + 302;

        internal const string MINATO_MESSAGERECEIVER_WINDOWNAME	="SHARP_PENSOFTWARE";
        internal const string MINATO_MESSAGERECEIVER_CLASSNAME = "SHARP_PENSOFTWARE";

        [Flags]
        private enum ClassStyles : uint
        {
            /// <summary>Aligns the window's client area on a byte boundary (in the x direction). This style affects the width of the window and its horizontal placement on the display.</summary>
            ByteAlignClient = 0x1000,

            /// <summary>Aligns the window on a byte boundary (in the x direction). This style affects the width of the window and its horizontal placement on the display.</summary>
            ByteAlignWindow = 0x2000,

            /// <summary>
            /// Allocates one device context to be shared by all windows in the class.
            /// Because window classes are process specific, it is possible for multiple threads of an application to create a window of the same class.
            /// It is also possible for the threads to attempt to use the device context simultaneously. When this happens, the system allows only one thread to successfully finish its drawing operation.
            /// </summary>
            ClassDC = 0x40,

            /// <summary>Sends a double-click message to the window procedure when the user double-clicks the mouse while the cursor is within a window belonging to the class.</summary>
            DoubleClicks = 0x8,

            /// <summary>
            /// Enables the drop shadow effect on a window. The effect is turned on and off through SPI_SETDROPSHADOW.
            /// Typically, this is enabled for small, short-lived windows such as menus to emphasize their Z order relationship to other windows.
            /// </summary>
            DropShadow = 0x20000,

            /// <summary>Indicates that the window class is an application global class. For more information, see the "Application Global Classes" section of About Window Classes.</summary>
            GlobalClass = 0x4000,

            /// <summary>Redraws the entire window if a movement or size adjustment changes the width of the client area.</summary>
            HorizontalRedraw = 0x2,

            /// <summary>Disables Close on the window menu.</summary>
            NoClose = 0x200,

            /// <summary>Allocates a unique device context for each window in the class.</summary>
            OwnDC = 0x20,

            /// <summary>
            /// Sets the clipping rectangle of the child window to that of the parent window so that the child can draw on the parent.
            /// A window with the CS_PARENTDC style bit receives a regular device context from the system's cache of device contexts.
            /// It does not give the child the parent's device context or device context settings. Specifying CS_PARENTDC enhances an application's performance.
            /// </summary>
            ParentDC = 0x80,

            /// <summary>
            /// Saves, as a bitmap, the portion of the screen image obscured by a window of this class.
            /// When the window is removed, the system uses the saved bitmap to restore the screen image, including other windows that were obscured.
            /// Therefore, the system does not send WM_PAINT messages to windows that were obscured if the memory used by the bitmap has not been discarded and if other screen actions have not invalidated the stored image.
            /// This style is useful for small windows (for example, menus or dialog boxes) that are displayed briefly and then removed before other screen activity takes place.
            /// This style increases the time required to display the window, because the system must first allocate memory to store the bitmap.
            /// </summary>
            SaveBits = 0x800,

            /// <summary>Redraws the entire window if a movement or size adjustment changes the height of the client area.</summary>
            VerticalRedraw = 0x1
        }

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct WNDCLASSEX
        {
            public uint cbSize;
            public ClassStyles style;
            [MarshalAs(UnmanagedType.FunctionPtr)]
            public WndProcDelegate lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }
        #endregion
    }

}
