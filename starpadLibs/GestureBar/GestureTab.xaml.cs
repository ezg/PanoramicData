using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Collections;
using starPadSDK.GestureBarLib;

namespace starPadSDK.GestureBarLib
{
	public partial class GestureTab
	{
		public GestureTab()
		{
			this.InitializeComponent();
		}

        public void UpdateState()
        {
            Panel element = (Panel)this.Parent;

            foreach (UIElement elt in element.Children)
            {
                if (elt is GestureTab)
                {
                    GestureTab tab = (GestureTab)elt;

                    if ((tab != this) && (tab.Expanded))
                    {
                        tab.Contract();
                    }
                }
            }
        }

        public bool Expanded = false;
        Canvas m_TabPage = null;
        Canvas m_ScrollViewer = null;

        public Canvas ToolbarScroller
        {
            get
            {
                return m_ScrollViewer;
            }

            set
            {
                m_ScrollViewer = value;
            }
        }

        public Canvas TabPage
        {
            get
            {
                return m_TabPage;
            }

            set
            {
                m_TabPage = value;
            }
        }

        public void Contract()
        {
            Storyboard contract = (Storyboard)this.Resources["Contract"];

            contract.Begin(this);
            Expanded = false;

            m_TabPage.Visibility = Visibility.Hidden;
            m_TabPage.IsHitTestVisible = false;
        }

        public void Expand()
        {
            Storyboard expand = (Storyboard)this.Resources["Expand"];

            expand.Begin(this);
            Expanded = true;

            if ((m_ScrollViewer != null) && (m_TabPage != null))
            {
                //m_ScrollViewer.Children.Clear();
                //m_ScrollViewer.Children.Add(m_TabPage);

                //foreach (FrameworkElement elt in m_ScrollViewer.Children)
                //{
                //    if (elt != m_TabPage)
                //    {
                //        elt.Visibility = Visibility.Hidden;
                //        elt.IsHitTestVisible = false;
                //    }
                //}

                m_TabPage.Visibility = Visibility.Visible;
                m_TabPage.IsHitTestVisible = true;

                //m_ScrollViewer.Width = m_TabPage.Width;


                //m_ScrollViewer.ScrollToHome();
                m_TabPage.RenderTransform = new TranslateTransform(0, 0);
            }
        }

        private void UserControl_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            starPadSDK.GestureBarLib.GestureExplorer.DismissLastExplorer();

            UpdateState();

            Expand();
        }

        protected Canvas ExtractCanvas(UserControl control)
        {

            Panel panel = (Panel)control.Content;

            /*if (panel is Canvas)
            {
                return (Canvas) panel;
            }*/

            if (panel != null)
            {
                foreach (UIElement elt in panel.Children)
                {
                    if (elt is Canvas)
                    {
                        return (Canvas)elt;
                    }
                }
            }

            return null;
        }

        public ArrayList GetChildButtons()
        {
            ArrayList result = new ArrayList();

            if (TabPage != null)
            {
                foreach (UIElement elt in TabPage.Children)
                {
                    if (elt is GestureButton)
                    {
                        result.Add(elt);
                    }
                }
            }

            return result;
        }

        public void InitButtons(GestureBar parent)
        {
            ArrayList childButtons = GetChildButtons();

            foreach (GestureButton button in childButtons)
            {
                button.ParentBar = parent;
                button.SetParent(this);
            }
        }

        public GestureButton FindButtonByTitle(string title)
        {
            ArrayList childButtons = GetChildButtons();

            foreach (GestureButton button in childButtons)
            {
                if (button.Title.CompareTo(title) == 0)
                    return button;
            }

            return null;
        }

        public void Create(UserControl page, GestureBar parent)
        {
            Canvas canvas = ExtractCanvas(page);

            if (canvas.Parent != null)
            {
                Panel panel = (Panel)canvas.Parent;
                panel.Children.Remove(canvas);
            }

            TabPage = canvas;
            ToolbarScroller = parent.GetToolbarScroller();

            parent.AddTab(this);

            m_ScrollViewer.Children.Add(m_TabPage);
            m_TabPage.Visibility = Visibility.Hidden;
            m_TabPage.IsHitTestVisible = false;

            //UpdateState();
        }

        public string Title
        {
            get
            {
                return TabLabel.Text;
            }

            set
            {
                TabLabel.Text = value;
            }
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            
        }

        

	}
}