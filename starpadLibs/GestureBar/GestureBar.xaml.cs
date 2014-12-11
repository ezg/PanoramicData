using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using starPadSDK.GestureBarLib;
using System.Collections;

namespace starPadSDK.GestureBarLib
{
	public partial class GestureBar
	{
        public static Canvas DefaultToolbarScroller = null;

		public GestureBar()
		{
            DefaultToolbarScroller = ToolbarScroller;

			this.InitializeComponent();

			
		}

        public event EventHandler OnButtonClick;

        public void InvokeClick(object obj, EventArgs e)
        {
            if (OnButtonClick != null)
            {
                OnButtonClick(obj, e);
            }
        }

        protected IDrawingCanvasFactory _CanvasFactory = null;

        public IDrawingCanvasFactory CanvasFactory
        {
            get
            {
                return _CanvasFactory;
            }

            set
            {
                _CanvasFactory = value;

                InitButtons();
            }
        }

        public Canvas GetToolbarScroller()
        {
            return ToolbarScroller;
        }

        protected ArrayList _Tabs = new ArrayList();

        public void AddTab(GestureTab tab)
        {
            _Tabs.Add(tab);
        }

        public GestureTab FindTabByTitle(string title)
        {
            foreach (GestureTab tab in _Tabs)
            {
                if (tab.Title.CompareTo(title) == 0)
                    return tab;
            }

            return null;
        }

        public GestureButton FindButtonByTitle(string title)
        {
            foreach (GestureTab tab in _Tabs)
            {
                GestureButton button = tab.FindButtonByTitle(title);

                if (button != null)
                    return button;
            }

            return null;
        }

        public void InitButtons()
        {
            foreach (GestureTab tab in _Tabs)
            {
                tab.InitButtons(this);
            }
        }

        private void UserControl_Initialized(object sender, System.EventArgs e)
        {
            DefaultToolbarScroller = ToolbarScroller;

            foreach (FrameworkElement tab in LayoutRoot.Children)
            {
                if (tab is GestureTab)
                {
                    _Tabs.Add( (GestureTab) tab);
                }
            }

            
        }

        double CurrentSize = 1.0;

        private void Ellipse_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CurrentSize > 0.6)
            {
                double PrevSize = CurrentSize;
                CurrentSize -= 0.2;

                ScaleTransform trans = new ScaleTransform(PrevSize, PrevSize);
                LayoutRoot.RenderTransform = trans;

                DoubleAnimation anim = new DoubleAnimation(CurrentSize, new Duration(TimeSpan.FromSeconds(1.0)));
                trans.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                trans.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
            }
        }

        private void IncreaseSize_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (CurrentSize < 1.0)
            {
                double PrevSize = CurrentSize;
                CurrentSize += 0.2;

                ScaleTransform trans = new ScaleTransform(PrevSize, PrevSize);
                LayoutRoot.RenderTransform = trans;

                DoubleAnimation anim = new DoubleAnimation(CurrentSize, new Duration(TimeSpan.FromSeconds(1.0)));
                trans.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
                trans.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
            }
        }

        bool Expanded = false;

        private void SizerRect_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!Expanded)
            {
                Storyboard board = (Storyboard)Resources["ExpandSizer"];
                board.Begin(this);

                Expanded = true;
            }
            else
            {
                Storyboard board = (Storyboard)Resources["ContractSizer"];
                board.Begin(this);

                Expanded = false;
            }
        }

        private void BackgroundRectangle_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                GestureExplorer.DismissLastExplorer();
            }
        }

	}
}