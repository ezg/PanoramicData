using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using Cjc.ThreeDeemium.Effects;
using Cjc.ThreeDeemium.Properties;
using System.Windows.Media.Animation;

namespace Cjc.ThreeDeemium
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
		private string baseTitle;
		private BindingBase addressBinding;

		public Window1()
		{
			Effects = EffectsHelper.GetEffects();

			InitializeComponent();

			address.GotFocus += delegate { address.SelectAll(); };
			Loaded += delegate { address.Focus(); };
			Closing += delegate { Settings.Default.Save(); };

			// Preserve binding; we'll need to set it again after input
			addressBinding = BindingOperations.GetBinding( address, TextBox.TextProperty );
			baseTitle = Title;

			browser.Ready += delegate
			{
				browser.BeginLoading += delegate { loading.Visibility = Visibility.Visible; };
				browser.FinishLoading += delegate { loading.Visibility = Visibility.Collapsed; };
			};

			browser.Source = Settings.Default.DefaultUrl;
		}

		public IDictionary<string, ShaderEffect> Effects { get; private set; }

        private void Button_Click( object sender, RoutedEventArgs e )
        {
			var url = GetUrl();

			if ( url.StartsWith( "javascript:" ) )
			{
				browser.ExecuteJavascript( url.Replace( "javascript:", "" ), "" );
			}
			else
			{
				browser.Navigate( GetUrl() );
				browser.Focus();
			}

			// Replace binding (since Mode=OneWay, text input will clear binding)
			address.SetBinding( TextBox.TextProperty, addressBinding );
		}

		private void browser_ReceiveTitle( object sender, Cjc.ChromiumBrowser.WebBrowser.ContentEventArgs e )
		{
			if ( string.IsNullOrEmpty( e.FrameName ) ) this.Title = e.Content + " - " + baseTitle;
		}

		private void browser_Status( object sender, Cjc.ChromiumBrowser.WebBrowser.StatusEventArgs e )
		{
			status.Items.Insert( 0, e.Message );
		}

		private void openTransparent_Click( object sender, RoutedEventArgs e )
		{
			var childWindow = new ChildWindow( GetUrl() );
			childWindow.Show();
		}

		private void back_Click( object sender, RoutedEventArgs e )
		{
			browser.GotoHistoryOffset( -1 );
		}

		private void forward_Click( object sender, RoutedEventArgs e )
		{
			browser.GotoHistoryOffset( 1 );
		}

		private void Default_Click( object sender, RoutedEventArgs e )
		{
			Settings.Default.DefaultUrl = GetUrl();
		}

		private string GetUrl()
		{
			var source = address.Text.Trim();

			return source.Contains( ":" ) ? source : "http://" + source;
		}
	}
}