using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CjcAwesomiumWrapper;
using Microsoft.Win32.SafeHandles;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using System.Collections.Generic;
using Microsoft.Surface.Presentation.Controls;


namespace Cjc.ChromiumBrowser
{
	/// <summary>
	///     <Cjc.ChromiumBrowser:WebBrowser/>
	/// </summary>
	[TemplatePart( Name = "PART_Browser", Type = typeof( Image ) )]
	public class WebBrowser : ContentControl, IDisposable
	{
		public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
			"Source",
			typeof( string ),
			typeof( WebBrowser ),
			new PropertyMetadata( OnSourceChanged ) );

		public string Source
		{
			get { return (string)GetValue( SourceProperty ); }
			set { SetValue( SourceProperty, value ); }
		}

		public static readonly DependencyProperty RenderPriorityProperty = DependencyProperty.Register(
			"RenderPriority",
			typeof( DispatcherPriority ),
			typeof( WebBrowser ),
			new PropertyMetadata( DispatcherPriority.Render ) );

		public DispatcherPriority RenderPriority
		{
			get { return (DispatcherPriority)GetValue( RenderPriorityProperty ); }
			set { SetValue( RenderPriorityProperty, value ); }
		}

		public static readonly DependencyProperty IsTransparentProperty = DependencyProperty.Register(
			"IsTransparent",
			typeof( bool ),
			typeof( WebBrowser ),
			new PropertyMetadata( false, OnIsTransparentChanged ) );

		public bool IsTransparent
		{
			get { return (bool)GetValue( IsTransparentProperty ); }
			set { SetValue( IsTransparentProperty, value ); }
		}

		public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
			"IsActive",
			typeof( bool ),
			typeof( WebBrowser ),
			new PropertyMetadata( true ) );

		public bool IsActive
		{
			get { return (bool)GetValue( IsActiveProperty ); }
			set { SetValue( IsActiveProperty, value ); }
		}

		public static readonly DependencyProperty EnableAsyncRenderingProperty = DependencyProperty.Register(
			"EnableAsyncRendering",
			typeof( bool ),
			typeof( WebBrowser ),
			new PropertyMetadata( false ) );

		public bool EnableAsyncRendering
		{
			get { return (bool)GetValue( EnableAsyncRenderingProperty ); }
			set { SetValue( EnableAsyncRenderingProperty, value ); }
		}

		public class StatusEventArgs : EventArgs
		{
			public string Message { get; private set; }

			public StatusEventArgs( string message )
			{
				this.Message = message;
			}
		}

		public class UrlEventArgs : EventArgs
		{
			public string Url { get; private set; }
			public string FrameName { get; private set; }

			public UrlEventArgs( string url, string frameName )
			{
				this.Url = url;
				this.FrameName = frameName;
			}
		}

		public class LoadingEventArgs : UrlEventArgs
		{
			public int StatusCode { get; private set; }
			public string MimeType { get; private set; }

			public LoadingEventArgs( string url, string frameName, int statusCode, string mimeType )
				: base( url, frameName )
			{
				this.StatusCode = statusCode;
				this.MimeType = mimeType;
			}
		}

		public class ContentEventArgs : EventArgs
		{
			public string Content { get; private set; }
			public string FrameName { get; private set; }

			public ContentEventArgs( string content, string frameName )
			{
				this.Content = content;
				this.FrameName = frameName;
			}
		}

		public class CallbackEventArgs : EventArgs
		{
			public string Name { get; private set; }
			public object[] Arguments { get; private set; }

			public CallbackEventArgs( string name, object[] arguments )
			{
				this.Name = name;
				this.Arguments = arguments;
			}
		}

		public event EventHandler Ready;
		public event EventHandler<StatusEventArgs> Status;
		public event EventHandler<UrlEventArgs> BeginNavigation;
		public event EventHandler<LoadingEventArgs> BeginLoading;
		public event EventHandler FinishLoading;
		public event EventHandler<ContentEventArgs> ReceiveTitle;
		public event EventHandler<CallbackEventArgs> Callback;

		private static WebCore webCore;

		private bool disposed;
		private WebView webView;
		private WebViewListener webViewListener;

		private Image image;
		private WriteableBitmap bitmap;
		private ToolTip tooltip;
		private byte[] buffer;
		private PixelFormat pixelFormat = PixelFormats.Bgr32;

		private bool isBrowserFocused;
		private string loadedUrl;

        private UIElement ContainedIn = null;

        private Dictionary<Contact, Point> contactDict = new Dictionary<Contact, Point>();
        private Point contactCentroid;
        private Point newCentroid;
        private bool gestured;

		/// <summary>
		/// Initializes the <see cref="WebBrowser"/> class.
		/// </summary>
		static WebBrowser()
		{
			DefaultStyleKeyProperty.OverrideMetadata( typeof( WebBrowser ), new FrameworkPropertyMetadata( typeof( WebBrowser ) ) );

			webCore = new WebCore();

			CompositionTarget.Rendering += delegate { webCore.Update(); };
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="WebBrowser"/> class.
		/// </summary>
		public WebBrowser(UIElement container)
		{
            ContainedIn = container;
			KeyboardNavigation.SetAcceptsReturn( this, true );

			tooltip = new ToolTip
			{
				HasDropShadow = true,
				IsOpen = false,
				StaysOpen = true
			};

			CompositionTarget.Rendering += CompositionTarget_Rendering;
            Contacts.AddPreviewContactDownHandler(this, Surface_ContactDown);
            Contacts.AddPreviewContactUpHandler(this, Surface_ContactUp);
            Contacts.AddPreviewContactChangedHandler(this, Surface_ContactChanged);
        }

        public WebView getView()
        {
            return webView;
        }

        public WebCore getCore()
        {
            return webCore;
        }

        void Surface_ContactDown(object sender, ContactEventArgs e)
        {
            List<Contact> stale = new List<Contact>();
            double c_X = 0, c_Y = 0;

            if (contactDict.Count > 1)
            {
                foreach(Contact c in contactDict.Keys)
                    try
                    {
                        Contacts.CaptureContact(c, this);
                        Point p = c.GetPosition(this);
                        c_X += p.X;
                        c_Y += p.Y;

                    }
                    catch (Exception ex)
                    {
                        stale.Add(c);
                    }

                foreach (Contact c in stale)
                    contactDict.Remove(c);
            }
            contactDict.Add(e.Contact, e.Contact.GetPosition(this));
            c_X += e.Contact.GetPosition(this).X;
            c_X += e.Contact.GetPosition(this).Y;
            contactCentroid = new Point(c_X / contactDict.Count, c_Y / contactDict.Count);

            if (contactDict.Count > 2)// Change to > 2
            {
                Contacts.CaptureContact(e.Contact, this);

                gestured = false;
                e.Handled = true;
            }

        }

        void Surface_ContactUp(object sender, ContactEventArgs e)
        {
            try
            {
                contactDict.Remove(e.Contact);
                if ((contactDict.Count <= 2) && (ContainedIn != null))
                {
                    foreach (Contact c in contactDict.Keys)
                        Contacts.CaptureContact(c, ContainedIn);
                }
            }
            catch (Exception ex)
            {
            }
        }

        void Surface_ContactChanged(object sender, ContactEventArgs e)
        {
            if (contactDict.Count > 1)
            {
                // Eat multifinger motions so they don't propigate through
                e.Handled = true;

                if (!gestured)
                {
                    double c_X = 0, c_Y = 0;
                    int cnt = 0;
                    foreach (Contact c in contactDict.Keys)
                    {
                        try
                        {
                            c_X += c.GetPosition(this).X;
                            c_Y += c.GetPosition(this).Y;
                            cnt++;
                        }
                        catch (Exception ex)
                        {
                        }
                    }

                    if (cnt == 3)
                    {
                        newCentroid = new Point(c_X / cnt, c_Y / cnt);
                        if (newCentroid.X - contactCentroid.X < -100)
                        {
                            // Left
                            gestured = true;
                            Console.Out.WriteLine("Left!");
                            GotoHistoryOffset(-1);
                        }
                        else if (newCentroid.X - contactCentroid.X > 100)
                        {
                            // Right
                            gestured = true;
                            Console.Out.WriteLine("Right!");
                            GotoHistoryOffset(1);
                        }
                    }
                }
            }
            else
            {
                if (ContainedIn != null)
                    ContainedIn.RaiseEvent(e);
            }
        }

		/// <summary>
		/// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate"/>.
		/// </summary>
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			image = (Image)GetTemplateChild( "PART_Image" );

			if ( image == null )
			{
				Content = image = new Image
				{
					Focusable = false,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					Stretch = Stretch.None
				};
			}
		}

		/// <summary>
		/// Handles the CompositionTarget.Rendering event.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void CompositionTarget_Rendering( object sender, EventArgs e )
		{
			if ( buffer != null && bitmap != null && webView != null && webView.IsDirty() && ( IsActive || IsMouseOver ) )
			{
				var stride = bitmap.BackBufferStride;
				var r = webView.Render( buffer, stride, 4 );

				var rendered = EnableAsyncRendering
					? new Int32Rect( 0, 0, bitmap.PixelWidth, bitmap.PixelHeight )
					: new Int32Rect( r.X, r.Y, r.Width, r.Height );

				if ( rendered.Width > 0 && rendered.Height > 0 )
				{
					bitmap.WritePixels(
						rendered,
						buffer,
						stride,
						rendered.X,
						rendered.Y );
				}

				if ( image.Source != bitmap )
				{
					Dispatcher.BeginInvoke(
						(Action)delegate { image.Source = bitmap; },
						( IsFocused || IsMouseOver ) ? DispatcherPriority.Render : RenderPriority );
				}
			}
		}

		/// <summary>
		/// Navigates the specified URL.
		/// </summary>
		/// <param name="url">The URL.</param>
		public void Navigate( string url )
		{
			if ( webView != null ) webView.LoadURL( url );
		}

		/// <summary>
		/// Loads HTML from a string.
		/// </summary>
		/// <param name="html">The HTML.</param>
		public void LoadHtml( string html )
		{
			if ( webView != null ) webView.LoadHTML( html );
		}

		/// <summary>
		/// Loads a local HTML file.
		/// </summary>
		/// <param name="path">The path.</param>
		public void LoadFile( string path )
		{
			if ( webView != null ) webView.LoadFile( path );
		}

		/// <summary>
		/// Goto the history offset.
		/// </summary>
		/// <param name="offset">The offset.</param>
		public void GotoHistoryOffset( int offset )
		{
			if ( webView != null ) webView.GotoHistoryOffset( offset );
		}

		/// <summary>
		/// Executes the javascript.
		/// </summary>
		/// <param name="javascript">The javascript.</param>
		/// <param name="frameName">Name of the frame.</param>
		public void ExecuteJavascript( string javascript, string frameName )
		{
			if ( webView != null ) webView.ExecuteJavascript( javascript, frameName );
		}

		/// <summary>
		/// Executes the javascript with result.
		/// </summary>
		/// <param name="javascript">The javascript.</param>
		/// <param name="frameName">Name of the frame.</param>
		/// <returns></returns>
		public JSValue ExecuteJavascriptWithResult( string javascript, string frameName )
		{
			return ( webView != null )
				? webView.ExecuteJavascriptWithResult( javascript, frameName )
				: null;
		}

		/// <summary>
		/// Sets the property.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="value">The value.</param>
		public void SetProperty( string name, JSValue value )
		{
			if ( webView != null ) webView.SetProperty( name, value );
		}

		/// <summary>
		/// Sets the callback.
		/// </summary>
		/// <param name="name">The name.</param>
		public void SetCallback( string name )
		{
			if ( webView != null ) webView.SetCallback( name );
		}

		/// <summary>
		/// Called to arrange and size the content of a <see cref="T:System.Windows.Controls.Control"/> object.
		/// </summary>
		/// <param name="arrangeBounds">The computed size that is used to arrange the content.</param>
		/// <returns>The size of the control.</returns>
		protected override Size ArrangeOverride( Size arrangeBounds )
		{
			var size = base.ArrangeOverride( arrangeBounds );

			if ( image != null )
			{
				var width = (int)size.Width;
				var height = (int)size.Height;

				if ( width > 0 && height > 0 )
				{
					try
					{
						if ( webView == null )
						{
							InitializeWebView( width, height );
							AttachEventHandlers();

							webView.LoadURL( Source ?? "about:blank" );
						}
						else webView.Resize( width, height );

						var bufferSize = width * height * 4;
						if ( buffer == null || buffer.Length != bufferSize ) buffer = new byte[ bufferSize ];

						if ( ( bitmap == null || bitmap.PixelWidth != width || bitmap.PixelHeight != height ) )
						{
							bitmap = new WriteableBitmap( width, height, 96, 96, pixelFormat, null );
						}
					}
					catch { }
				}
			}

			return size;
		}

		/// <summary>
		/// Initializes the web view.
		/// </summary>
		/// <param name="width">The width.</param>
		/// <param name="height">The height.</param>
		private void InitializeWebView( int width, int height )
		{
			try
			{
				RaiseStatus( "Creating WebView" );

				webView = webCore.CreateWebView( width, height, IsTransparent, EnableAsyncRendering, 70 );

				RaiseStatus( "Initializign WebViewListener" );

				webViewListener = new WebViewListener();

				webViewListener.BeginNavigation += delegate( string url, string frameName )
				{
					RaiseStatus( string.Format( "BeginNavigation: {0}", url ) );

					if ( string.IsNullOrEmpty( frameName ) && url != loadedUrl )
					{
						loadedUrl = url;

						Dispatcher.BeginInvoke( (Action)delegate
						{
							Source = url;
						},
						DispatcherPriority.Render );
					}

					if ( BeginNavigation != null )
					{
						Dispatcher.BeginInvoke(
							(Action)delegate { BeginNavigation( this, new UrlEventArgs( url, frameName ) ); },
							DispatcherPriority.Normal );
					}
				};

				webViewListener.BeginLoading += delegate( string url, string frameName, int statusCode, string mimeType )
				{
					RaiseStatus( string.Format( "BeginLoading: {0}", url ) );

					if ( BeginLoading != null )
					{
						Dispatcher.BeginInvoke(
							(Action)delegate { BeginLoading( this, new LoadingEventArgs( url, frameName, statusCode, mimeType ) ); },
							DispatcherPriority.Normal );
					}
				};

				webViewListener.FinishLoading += delegate
				{
					RaiseStatus( string.Format( "FinishLoading" ) );

					if ( FinishLoading != null )
					{
						Dispatcher.BeginInvoke(
							(Action)delegate { FinishLoading( this, EventArgs.Empty ); },
							DispatcherPriority.Normal );
					}
				};

				webViewListener.ReceiveTitle += delegate( string title, string frameName )
				{
					RaiseStatus( string.Format( "ReceiveTitle: {0}", title ) );

					if ( ReceiveTitle != null )
					{
						Dispatcher.BeginInvoke(
							(Action)delegate { ReceiveTitle( this, new ContentEventArgs( title, frameName ) ); },
							DispatcherPriority.Render );
					}
				};

				webViewListener.ChangeCursor += delegate( ValueType cursorHandle )
				{
					var safeHandle = new SafeFileHandle( (IntPtr)cursorHandle, false );

					Dispatcher.BeginInvoke( (Action)delegate
					{
						Cursor = CursorInteropHelper.Create( safeHandle );
					},
					DispatcherPriority.Render );
				};

				webViewListener.ChangeTooltip += delegate( string text )
				{
					Dispatcher.BeginInvoke( (Action)delegate
					{
						try
						{
							if ( text != null && text.Trim().Length > 0 && IsFocused )
							{
								tooltip.Content = text;
								tooltip.IsOpen = true;
							}
							else tooltip.IsOpen = false;
						}
						catch
						{
						}
					},
					DispatcherPriority.Render );
				};

				webViewListener.ChangeKeyboardFocus += delegate( bool isFocused )
				{
					isBrowserFocused = isFocused;
				};

				webViewListener.Callback += delegate( string name, JSValue[] args )
				{
					var argValues = args.Select( a => a.Value() ).ToArray();

					RaiseStatus( string.Format( "Callback" ) );

					if ( Callback != null )
					{
						Dispatcher.BeginInvoke(
							(Action)delegate { Callback( this, new CallbackEventArgs( name, argValues ) ); },
							DispatcherPriority.Normal );
					}
				};

				webView.SetListener( webViewListener );
				webView.Focus();

				if ( Ready != null ) Dispatcher.BeginInvoke( (Action)delegate { Ready( this, EventArgs.Empty ); }, DispatcherPriority.Normal );
			}
			catch ( Exception ex )
			{
				RaiseStatus( ex.Message + ex.StackTrace );
			}
		}

		/// <summary>
		/// Attaches the event handlers.
		/// </summary>
		private void AttachEventHandlers()
		{
			RaiseStatus( "Attaching event handlers" );

			GotFocus += delegate
			{
				RaiseStatus( "Got focus" );
				if ( webView != null ) webView.Focus();
				isBrowserFocused = true;
			};

			LostFocus += delegate
			{
				RaiseStatus( "Lost focus" );
				if ( webView != null ) webView.Unfocus();
				tooltip.IsOpen = false;
			};

			KeyDown += delegate( object sender, KeyEventArgs e )
			{
				if ( e.Key == Key.Tab && !isBrowserFocused )
				{
					RaiseStatus( "Allowed tab KeyDown for navigation" );
					return;
				}

				var k = e.ToKeyInfo();
				var isExtended = ( k.ControlKeyState & ControlKeyStates.EnhancedKey ) == ControlKeyStates.EnhancedKey;

				InjectKeyboardEvent( Win32Message.WM_KEYDOWN, (byte)k.VirtualKeyCode, isExtended, false, e.IsRepeat, false );

				if ( k.Character != 0 )
				{
					var leftAlt = ( k.ControlKeyState & ControlKeyStates.LeftAltPressed ) == ControlKeyStates.LeftAltPressed;
					var rightAlt = ( k.ControlKeyState & ControlKeyStates.LeftAltPressed ) == ControlKeyStates.RightAltPressed;

					InjectKeyboardEvent( Win32Message.WM_CHAR, (byte)k.Character, isExtended, leftAlt | rightAlt, e.IsRepeat, false );
				}

				RaiseStatus( "Handled KeyDown: " + e.Key );
				e.Handled = true;
			};

			KeyUp += delegate( object sender, KeyEventArgs e )
			{
				if ( e.Key == Key.Tab && !isBrowserFocused )
				{
					RaiseStatus( "Allowed tab KeyUp for navigation" );
					return;
				}

				var k = e.ToKeyInfo();
				var isExtended = ( k.ControlKeyState & ControlKeyStates.EnhancedKey ) == ControlKeyStates.EnhancedKey;

				InjectKeyboardEvent( Win32Message.WM_KEYUP, (byte)k.VirtualKeyCode, isExtended, true, !e.IsRepeat, true );

				RaiseStatus( "Handled KeyUp: " + e.Key );
				e.Handled = true;
			};

			PreviewMouseMove += delegate( object sender, MouseEventArgs e )
			{
				var pos = e.GetPosition( this );
				webView.InjectMouseMove( (int)pos.X, (int)pos.Y );

				tooltip.PlacementTarget = this;
				tooltip.Placement = PlacementMode.Top;
				tooltip.HorizontalOffset = pos.X;
				tooltip.VerticalOffset = pos.Y;
			};

			MouseLeave += delegate
			{
				tooltip.IsOpen = false;
			};

			PreviewMouseDown += delegate( object sender, MouseButtonEventArgs e )
			{
				CaptureMouse();
				Keyboard.Focus( this );
				if ( webView != null ) webView.InjectMouseDown( GetMouseButton( e.ChangedButton ) );
                e.Handled = true;
            };

			PreviewMouseUp += delegate( object sender, MouseButtonEventArgs e )
			{
				if ( webView != null ) webView.InjectMouseUp( GetMouseButton( e.ChangedButton ) );
				ReleaseMouseCapture();
			};

			MouseWheel += delegate( object sender, MouseWheelEventArgs e )
			{
				if ( webView != null ) webView.InjectMouseWheel( e.Delta );
			};
		}

		/// <summary>
		/// Raises the status.
		/// </summary>
		/// <param name="message">The message.</param>
		private void RaiseStatus( string message )
		{
			if ( Status != null ) Status( this, new StatusEventArgs( message ) );
		}

		/// <summary>
		/// Injects the keyboard event.
		/// </summary>
		/// <param name="message">The message.</param>
		/// <param name="virtualKeyCode">The virtual key code.</param>
		/// <param name="isExtended">if set to <c>true</c> [is extended].</param>
		/// <param name="contextCode">if set to <c>true</c> [context code].</param>
		/// <param name="previousState">if set to <c>true</c> [previous state].</param>
		/// <param name="transitionState">if set to <c>true</c> [transition state].</param>
		private void InjectKeyboardEvent( Win32Message message, byte virtualKeyCode, bool isExtended, bool contextCode, bool previousState, bool transitionState )
		{
			if ( webView != null )
			{
				var lParam = new KeyLParam( 1, virtualKeyCode, isExtended, contextCode, previousState, transitionState );

				webView.InjectKeyboardEvent( (IntPtr)0, (int)message, virtualKeyCode, lParam );
			}
		}

		/// <summary>
		/// Gets the mouse button.
		/// </summary>
		/// <param name="button">The button.</param>
		/// <returns></returns>
		private CjcAwesomiumWrapper.MouseButton GetMouseButton( System.Windows.Input.MouseButton button )
		{
			switch ( button )
			{
				case System.Windows.Input.MouseButton.Middle: return CjcAwesomiumWrapper.MouseButton.Middle;
				case System.Windows.Input.MouseButton.Right: return CjcAwesomiumWrapper.MouseButton.Right;
				default: return CjcAwesomiumWrapper.MouseButton.Left;
			}
		}

		/// <summary>
		/// Releases the web view.
		/// </summary>
		private void ReleaseWebView()
		{
			CompositionTarget.Rendering -= CompositionTarget_Rendering;

			if ( webView != null )
			{
				webView.Dispose();
				webView = null;
			}

			if ( webViewListener != null )
			{
				webViewListener.Dispose();
				webViewListener = null;
			}
		}

		/// <summary>
		/// Called when [source changed].
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
		private static void OnSourceChanged( DependencyObject obj, DependencyPropertyChangedEventArgs args )
		{
			if ( args.NewValue != args.OldValue )
			{
				var webBrowser = obj as WebBrowser;
				var url = (string)args.NewValue;

				if ( webBrowser != null && url != webBrowser.loadedUrl ) webBrowser.Navigate( url );
			}
		}

		/// <summary>
		/// Called when [is transparent changed].
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <param name="args">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void OnIsTransparentChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var webBrowser = obj as WebBrowser;
            var isTransparent = (bool)args.NewValue;
            if (webBrowser.webView != null) webBrowser.webView.SetTransparent(isTransparent);
            webBrowser.pixelFormat = isTransparent ? PixelFormats.Bgra32 : PixelFormats.Bgr32;
        }

		#region IDisposable Members

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		/// <summary>
		/// Releases unmanaged and - optionally - managed resources
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
		protected virtual void Dispose( bool disposing )
		{
			if ( !disposed )
			{
				if ( disposing ) ReleaseWebView();

				disposed = true;
			}
		}

		#endregion
	}
}