using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Cjc.ChromiumBrowser
{
	public static class Extensions
	{
		// Methods
		private static char GetChar( int vk )
		{
			byte[] ks = new byte[ 0x100 ];
			NativeMethods.GetKeyboardState( ks );
			uint sc = NativeMethods.MapVirtualKey( (uint)vk, NativeMethods.MapType.MAPVK_VK_TO_VSC );
			StringBuilder sb = new StringBuilder( 2 );
			char ch = '\0';
			switch ( NativeMethods.ToUnicode( (uint)vk, sc, ks, sb, sb.Capacity, 0 ) )
			{
				case -1:
				case 0:
					return ch;

				case 1:
					return sb[ 0 ];
			}
			return sb[ 0 ];
		}

		private static ControlKeyStates GetControlKeyStates( this KeyboardDevice kb )
		{
			ControlKeyStates controlStates = 0;
			if ( kb.IsKeyDown( Key.LeftCtrl ) )
			{
				controlStates |= ControlKeyStates.LeftCtrlPressed;
			}
			if ( kb.IsKeyDown( Key.LeftAlt ) )
			{
				controlStates |= ControlKeyStates.LeftAltPressed;
			}
			if ( kb.IsKeyDown( Key.RightAlt ) )
			{
				controlStates |= ControlKeyStates.RightAltPressed;
			}
			if ( kb.IsKeyDown( Key.RightCtrl ) )
			{
				controlStates |= ControlKeyStates.RightCtrlPressed;
			}
			if ( kb.IsKeyToggled( Key.Scroll ) )
			{
				controlStates |= ControlKeyStates.ScrollLockOn;
			}
			if ( kb.IsKeyToggled( Key.Capital ) )
			{
				controlStates |= ControlKeyStates.CapsLockOn;
			}
			if ( kb.IsKeyToggled( Key.NumLock ) )
			{
				controlStates |= ControlKeyStates.NumLockOn;
			}
			if ( !kb.IsKeyDown( Key.LeftShift ) && !kb.IsKeyDown( Key.RightShift ) )
			{
				return controlStates;
			}
			return ( controlStates | ControlKeyStates.ShiftPressed );
		}

		public static KeyInfo ToKeyInfo( this KeyEventArgs e )
		{
			int vk = KeyInterop.VirtualKeyFromKey( e.Key );
			return new KeyInfo( vk, GetChar( vk ), e.KeyboardDevice.GetControlKeyStates(), e.IsDown );
		}
	}
}