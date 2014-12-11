#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
#endregion

namespace Taloware.Starlight.Controls
{
	public class DialogPresenter : ContentControl
	{
		#region Constructors

		static DialogPresenter()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(DialogPresenter), new FrameworkPropertyMetadata(typeof(DialogPresenter)));
		}

		public DialogPresenter()
		{
			this.CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, CloseCommandExecute));
		}

		#endregion

		#region Properties

		#region Dialog

		//public Control Dialog
		//{
		//    get { return (Control)GetValue(DialogProperty); }
		//    set { SetValue(DialogProperty, value); }
		//}

		//public static readonly DependencyProperty DialogProperty =
		//    DependencyProperty.Register("Dialog", typeof(Control), typeof(DialogPresenter), new UIPropertyMetadata(null));

		#endregion

		#region IsDialogVisible

		public bool IsDialogVisible
		{
			get { return (bool)GetValue(IsDialogVisibleProperty); }
			set { SetValue(IsDialogVisibleProperty, value); }
		}

		public static readonly DependencyProperty IsDialogVisibleProperty =
			DependencyProperty.Register("IsDialogVisible", typeof(bool), typeof(DialogPresenter), new UIPropertyMetadata(false));

		#endregion

		#region DialogTitle

		public string DialogTitle
		{
			get { return (string)GetValue(DialogTitleProperty); }
			set { SetValue(DialogTitleProperty, value); }
		}

		public static readonly DependencyProperty DialogTitleProperty =
			DependencyProperty.Register("DialogTitle", typeof(string), typeof(DialogPresenter), new UIPropertyMetadata(string.Empty));

		#endregion

		#endregion

		#region Public Methods

		public void Show(Control view)
		{
			var presenter = Template.FindName("PART_DialogView", this) as ContentPresenter;

			if (presenter == null)
				return;

			IsDialogVisible = true;
			presenter.Content = view;

		}

		public void Show(Control view, string title)
		{
			DialogTitle = title;
			Show(view);
		}

        public void Hide()
        {
            var presenter = Template.FindName("PART_DialogView", this) as ContentPresenter;

            IsDialogVisible = false;

            // Free the control for garbage collection
            if (presenter != null)
                presenter.Content = null;
        }

		#endregion

		#region Event Handlers

		private void CloseCommandExecute(object sender, ExecutedRoutedEventArgs e)
		{
            Hide();
		}

		#endregion
	}
}