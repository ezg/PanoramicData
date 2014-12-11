using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Windows.Media.Imaging;
using System.Windows;
using CAW = CjcAwesomiumWrapper;
using System.Windows.Media;

namespace Cjc.WebSnapshot
{
	class Program
	{
		static void Main( string[] args )
		{
			Console.WriteLine( "\nWeb Snapshot by Chris Cavanagh\n" );

			if ( args.Length < 1 )
			{
				Console.Error.WriteLine( "Usage: WebSnapshot <url> [target] [width] [height] [transparent?]" );
				return;
			}

			var url = args[ 0 ];
			var target = ( args.Length > 1 ) ? args[ 1 ] : Path.GetFileName( url ) + ".png";
			var width = ( args.Length > 2 ) ? int.Parse( args[ 2 ] ) : 1024;
			var height = ( args.Length > 3 ) ? int.Parse( args[ 3 ] ) : 2048;
			var transparent = ( args.Length > 4 ) ? bool.Parse( args[ 4 ] ) : false;

			var stride = width * 4;
			var buffer = new byte[ stride * height ];

			Console.WriteLine( "Waiting for response..." );

			if ( RenderSnapshot( url, buffer, width, height, transparent ) == null )
			{
				throw new TimeoutException( "Failed to render page within timeout" );
			}

			Console.WriteLine( "Saving " + target );

			var encoder = new PngBitmapEncoder();
			var pixelFormat = transparent ? PixelFormats.Bgra32 : PixelFormats.Bgr32;
			encoder.Frames.Add( BitmapFrame.Create( BitmapSource.Create( width, height, 96, 96, pixelFormat, null, buffer, stride ) ) );

			using ( var fileStream = File.Create( target ) ) encoder.Save( fileStream );

			Console.WriteLine( "Done.\n" );
		}

		public static Rect? RenderSnapshot( string url, byte[] buffer, int width, int height, bool transparent )
		{
			using ( var webCore = new CAW.WebCore() )
			{
				using ( var webView = webCore.CreateWebView( width, height ) )
				{
					webView.SetTransparent( transparent );

					var finished = new ManualResetEvent( false );
					var listener = new CAW.WebViewListener();
					listener.FinishLoading += delegate { finished.Set(); };
					webView.SetListener( listener );

					if ( url.StartsWith( "file:" ) ) webView.LoadFile( url );
					else webView.LoadURL( url );

					var timeout = DateTime.Now.AddSeconds( 30 );

					while ( !finished.WaitOne( 100 ) )
					{
						webCore.Update();

						if ( DateTime.Now > timeout ) return null;
					}

					var r = webView.Render( buffer, width * 4, 4 );

					return new Rect( r.X, r.Y, r.Width, r.Height );
				}
			}
		}
	}
}