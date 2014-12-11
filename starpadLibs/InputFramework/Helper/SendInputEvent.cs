using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;

namespace mil.win32
{
    class SendInputEvent
    {
        [DllImport("User32.dll", SetLastError = true)]
        public static extern int SendInput(int nInputs, ref INPUT pInputs, int cbSize);

        [DllImport("user32.dll")]
        public static extern byte VkKeyScan(char ch);

        internal class INPUT
        {
            public const int MOUSE = 0;
            public const int KEYBOARD = 1;
            public const int HARDWARE = 2;
        }

        internal class KEYEYENTF
        {
            public const int KEY_EXTENDED = 0x0001;
            public const uint KEY_UP = 0x0002;
            public const uint KEY_SCANCODE = 0x0004;
        }

        internal class MOUSEEVENTF
        {
            public const int MOVE = 0x0001; /* mouse move */
            public const int LEFTDOWN = 0x0002; /* left button down */
            public const int LEFTUP = 0x0004; /* left button up */
            public const int RIGHTDOWN = 0x0008; /* right button down */
            public const int RIGHTUP = 0x0010; /* right button up */
            public const int MIDDLEDOWN = 0x0020; /* middle button down */
            public const int MIDDLEUP = 0x0040; /* middle button up */
            public const int XDOWN = 0x0080; /* x button down */
            public const int XUP = 0x0100; /* x button down */
            public const int WHEEL = 0x0800; /* wheel button rolled */
            public const int VIRTUALDESK = 0x4000; /* map to entire 
                                            virtual desktop */
            public const int ABSOLUTE = 0x8000; /* absolute move */
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct KEYBOARD_INPUT
        {
            public ushort vk;
            public ushort scanCode;
            public uint flags;
            public uint time;
            public uint extrainfo;
            public uint padding1;
            public uint padding2;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct Input
        {
            [FieldOffset(0)] public int type;
            [FieldOffset(4)] public MOUSEINPUT mi;
            [FieldOffset(4)] public KEYBOARD_INPUT ki;
            //[FieldOffset(4)] public HARDWAREINPUT hi;
        }

        [DllImport("User32.dll")]
        public static extern UInt32 SendInput
        (
            UInt32 nInputs,
            Input[] pInputs,
            Int32 cbSize
        );

        /// <summary>
        /// This is a method to send a mouse input.
        /// </summary>
        /// <param name="mouseEventType">mouseEventType: 0: leftdown, 1: leftup, 2: rightdown, 3: rightup, -1: no button down</param>
        /// <param name="x">screen coordinates</param>
        /// <param name="y">screen coordinates</param>
        /// <param name="force">not implemented yet</param>
        public static void SendMouseInput(int mouseEventType, int x, int y, Double force)
        {
            Rectangle screen = Screen.PrimaryScreen.Bounds;
            int x2 = (65535 * x) / screen.Width;
            int y2 = (65535 * y) / screen.Height;

            Input []input = new Input[1];

            input[0].type = INPUT.MOUSE;
            input[0].mi.dx = x2;
            input[0].mi.dy = y2;
            input[0].mi.dwFlags = MOUSEEVENTF.MOVE | MOUSEEVENTF.ABSOLUTE;
            switch (mouseEventType)
            {
                case 0:
                    input[0].mi.dwFlags |= MOUSEEVENTF.LEFTDOWN;
                    break;
                case 1:
                    input[0].mi.dwFlags |= MOUSEEVENTF.LEFTUP;
                    break;
                case 2:
                    input[0].mi.dwFlags |= MOUSEEVENTF.RIGHTDOWN;
                    break;
                case 3:
                    input[0].mi.dwFlags |= MOUSEEVENTF.RIGHTUP;
                    break;
            }
            SendInput(1, input, Marshal.SizeOf(input[0]));
        }

        public static void SetCursorPosition(int x, int y)
        {
            System.Windows.Forms.Cursor.Position = new System.Drawing.Point(x, y);
        }

        /// <summary>
        /// sends simple keyboard events, ---------------------------------------------------------------------  special keys not implemented yet
        /// </summary>
        /// <param name="key">key character to be sent</param>
        /// <param name="keydown">true = key down, false = key up</param>
        public static void sendKeySimple(char key, bool keydown)
        {
            byte scanCode = VkKeyScan(key);

            Input[] input = new Input[1];
            input[0].type = INPUT.KEYBOARD;
            input[0].ki.flags = KEYEYENTF.KEY_SCANCODE;

            // this is taken from an example with int as scancode
            //if ((scanCode & 0xFF00) == 0xE000)
            //{ // extended key?
            //    input[0].flags |= KEY_EXTENDED;
            //}

            if (keydown)
            { // down
                input[0].ki.scanCode = scanCode;
            }
            else
            { // key up
                input[0].ki.scanCode = scanCode;
                input[0].ki.flags |= KEYEYENTF.KEY_UP;
            }

            uint result = SendInput(1, input, Marshal.SizeOf(input[0]));

            if (result != 1)
            {
                throw new Exception("Could not send key: " + scanCode);
            }
        }
    }
}
