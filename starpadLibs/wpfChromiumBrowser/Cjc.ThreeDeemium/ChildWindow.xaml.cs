using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Effects;
using Cjc.ThreeDeemium.Effects;

namespace Cjc.ThreeDeemium
{
	/// <summary>
	/// Interaction logic for ChildWindow.xaml
	/// </summary>
	public partial class ChildWindow : Window
	{
		private BindingBase addressBinding;

		public ChildWindow() : this( null )
		{
		}

		public ChildWindow( string url )
		{
			Effects = EffectsHelper.GetEffects();

			InitializeComponent();

			address.GotFocus += delegate { address.SelectAll(); };
			Loaded += delegate { address.Focus(); };

			// Preserve binding; we'll need to set it again after input
			addressBinding = BindingOperations.GetBinding( address, TextBox.TextProperty );
			browser.Source = url ?? "http://chriscavanagh.wordpress.com";
		}

		public IDictionary<string, ShaderEffect> Effects { get; private set; }

		private void Button_Click( object sender, RoutedEventArgs e )
		{
			var source = address.Text.Trim();

			// Replace binding (since Mode=OneWay, text input will clear binding)
			address.SetBinding( TextBox.TextProperty, addressBinding );

			browser.Navigate( source.Contains( ":" ) ? source : "http://" + source );
			browser.Focus();
		}

		private void back_Click( object sender, RoutedEventArgs e )
		{
			browser.GotoHistoryOffset( -1 );
		}

		private void forward_Click( object sender, RoutedEventArgs e )
		{
			browser.GotoHistoryOffset( 1 );
		}

		private void close_Click( object sender, RoutedEventArgs e )
		{
			Close();
			browser.Dispose();
		}

		private void Border_MouseLeftButtonDown( object sender, System.Windows.Input.MouseButtonEventArgs e )
		{
			DragMove();
		}
	}
}
