﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Globalization;

namespace Cjc.ChromiumBrowser
{
	[StructLayout( LayoutKind.Sequential )]
	public struct KeyInfo
	{
		private int virtualKeyCode;
		private char character;
		private ControlKeyStates controlKeyState;
		private bool keyDown;
		public int VirtualKeyCode
		{
			get
			{
				return this.virtualKeyCode;
			}
			set
			{
				this.virtualKeyCode = value;
			}
		}
		public char Character
		{
			get
			{
				return this.character;
			}
			set
			{
				this.character = value;
			}
		}
		public ControlKeyStates ControlKeyState
		{
			get
			{
				return this.controlKeyState;
			}
			set
			{
				this.controlKeyState = value;
			}
		}
		public bool KeyDown
		{
			get
			{
				return this.keyDown;
			}
			set
			{
				this.keyDown = value;
			}
		}
		public KeyInfo( int virtualKeyCode, char ch, ControlKeyStates controlKeyState, bool keyDown )
		{
			this.virtualKeyCode = virtualKeyCode;
			this.character = ch;
			this.controlKeyState = controlKeyState;
			this.keyDown = keyDown;
		}

		public override string ToString()
		{
			return string.Format( CultureInfo.InvariantCulture, "{0},{1},{2},{3}", new object[] { this.VirtualKeyCode, this.Character, this.ControlKeyState, this.KeyDown } );
		}

		public override bool Equals( object obj )
		{
			bool flag = false;
			if ( obj is KeyInfo )
			{
				flag = this == ( (KeyInfo)obj );
			}
			return flag;
		}

		public override int GetHashCode()
		{
			uint num = this.KeyDown ? 0x10000000u : 0;
			num |= ( (uint)this.ControlKeyState ) << 0x10;
			num |= (uint)this.VirtualKeyCode;
			return num.GetHashCode();
		}

		public static bool operator ==( KeyInfo first, KeyInfo second )
		{
			return ( ( ( ( first.Character == second.Character ) && ( first.ControlKeyState == second.ControlKeyState ) ) && ( first.KeyDown == second.KeyDown ) ) && ( first.VirtualKeyCode == second.VirtualKeyCode ) );
		}

		public static bool operator !=( KeyInfo first, KeyInfo second )
		{
			return !( first == second );
		}
	}
}