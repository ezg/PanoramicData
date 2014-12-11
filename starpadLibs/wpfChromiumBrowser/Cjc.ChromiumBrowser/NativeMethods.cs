using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Cjc.ChromiumBrowser
{
	public enum Win32Message
	{
		WM_KEYDOWN = 0x100,
		WM_KEYUP = 0x101,
		WM_CHAR = 0x102,
		WM_SYSKEYDOWN = 0x104,
		WM_SYSKEYUP = 0x105,
		WM_SYSCHAR = 0x106,
		WM_SYSDEADCHAR = 0x107,
		WM_MOUSEDOWN = 0x201
	}

	[Flags]
	public enum ControlKeyStates
	{
		CapsLockOn = 0x80,
		EnhancedKey = 0x100,
		LeftAltPressed = 2,
		LeftCtrlPressed = 8,
		NumLockOn = 0x20,
		RightAltPressed = 1,
		RightCtrlPressed = 4,
		ScrollLockOn = 0x40,
		ShiftPressed = 0x10
	}

	public class NativeMethods
	{
		public enum MapType : uint
		{
			MAPVK_VK_TO_CHAR = 2,
			MAPVK_VK_TO_VSC = 0,
			MAPVK_VK_TO_VSC_EX = 4,
			MAPVK_VSC_TO_VK = 1,
			MAPVK_VSC_TO_VK_EX = 3
		}

		[DllImport( "user32.dll" )]
		public static extern int ToUnicode( uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs( UnmanagedType.LPWStr )] StringBuilder pwszBuff, int cchBuff, uint wFlags );

		[DllImport( "user32.dll" )]
		public static extern uint MapVirtualKey( uint uCode, MapType uMapType );

		[DllImport( "user32.dll" )]
		public static extern bool GetKeyboardState( byte[] lpKeyState );
	}
}