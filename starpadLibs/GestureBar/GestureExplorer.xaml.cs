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
using starPadSDK.GestureBarLib.UICommon;
using System.Collections;

namespace starPadSDK.GestureBarLib
{
	public partial class GestureExplorer
	{
		public GestureExplorer()
		{
			this.InitializeComponent();
		}

        GestureButton _ParentButton = null;
        ArrayList m_TempPath = null;

        FrameworkElement DisplayIcon = null;
        FrameworkElement ContextIconContent = null;

        FrameworkElement DrawingCanvasScroller = null;
        FrameworkElement DrawingCanvas = null;

        bool TryItResultSpecified = false;

        public void NotifyStylusUp()
        {
            
        }

        public void ReportTryItResult(bool success, string failureText)
        {
            if (!TryItResultSpecified)
            {
                TryItResultSpecified = true;

                ShowReplayButton();

                if (success)
                {
                    
                    TryItResult.Success();
                }
                else
                {
                    
                    TryItResult.Failure();
                    //ConfigureTryIt();

                    //TryItResultSpecified = false;
                }
            }
        }

        public FrameworkElement TryItDrawingCanvas
        {
            get
            {
                return DrawingCanvas;
            }
        }

        public void InitIcon(GestureButton parentButton)
        {
            _ParentButton = parentButton;

            DemoUnit1.InitIcon(parentButton);
            //DemoUnit2.InitIcon(parentButton);
            //DemoUnit3.InitIcon(parentButton);
            //DemoUnit4.InitIcon(parentButton);


            



            //Helpers.ForceUpdateLayout(_ParentButton);

            //DisplayIcon = Helpers.CloneUsingXaml((FrameworkElement)_ParentButton.DisplayIcon);
            //CommandIcon.Children.Add(DisplayIcon);


            
        }

        public void InitParentBar()
        {
            DrawingCanvasScroller = _ParentButton.ParentBar.CanvasFactory.CreateDrawingCanvas();
            TryItParent.Children.Add(DrawingCanvasScroller);

            if (DrawingCanvasScroller is ScrollViewer)
            {
                ScrollViewer view = (ScrollViewer)DrawingCanvasScroller;
                DrawingCanvas = (FrameworkElement)view.Content;

                view.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
                view.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            }
            else
            {
                DrawingCanvas = (FrameworkElement)DrawingCanvasScroller;
                DrawingCanvasScroller = null;
            }

            
        }

        public void InitContext(GestureButton parentButton)
        {
            _ParentButton = parentButton;

            DemoUnit1.InitContext(parentButton);
            //DemoUnit2.InitContext(parentButton);
            //DemoUnit3.InitContext(parentButton);
            //DemoUnit4.InitContext(parentButton);

            //

            //if (_ParentButton.Context != null)
            //{
            //    Helpers.ForceUpdateLayout(_ParentButton);

            //    ContextIconContent = Helpers.CloneUsingXaml((FrameworkElement)_ParentButton.Context);
            //    ContextIcon.Children.Add(ContextIconContent);

            //    ContextIconContent.RenderTransform = new TranslateTransform(0, 0);
            //}
        }

        public GestureButton.ConfigureDrawingCanvasFunc GetConfigureDrawingCanvasFunction()
        {
            switch (DemoUnit1.StrokeNumber)
            {
                case 1:
                    return _ParentButton.ConfigureDrawingCanvasFunction1;

                case 2:
                    return _ParentButton.ConfigureDrawingCanvasFunction2;

                case 3:
                    return _ParentButton.ConfigureDrawingCanvasFunction3;

                case 4:
                    return _ParentButton.ConfigureDrawingCanvasFunction4;
            }

            return null;
        }

        public void ConfigureTryIt()
        {
            TryItResultSpecified = false;

            if (GetConfigureDrawingCanvasFunction() == null)
            {
             /*   // No gesture explorer support defined for this gesture

                Storyboard anim = (Storyboard)Resources["ShowDisplayOnlyMode"];
                anim.Begin(this);*/

                TryItBackground.Visibility = Visibility.Hidden;
                TryItRect.Visibility = Visibility.Hidden;
                TryItLabels.Visibility = Visibility.Hidden;
                TryItParent.Visibility = Visibility.Hidden;

                TryItBackground.IsHitTestVisible = TryItRect.IsHitTestVisible = TryItLabels.IsHitTestVisible = TryItParent.IsHitTestVisible = false;
            }
            else
            {
                TryItBackground.Visibility = Visibility.Visible;
                TryItRect.Visibility = Visibility.Visible;
                TryItLabels.Visibility = Visibility.Visible;
                TryItParent.Visibility = Visibility.Visible;

                TryItBackground.IsHitTestVisible = TryItRect.IsHitTestVisible = TryItLabels.IsHitTestVisible = TryItParent.IsHitTestVisible = true;
            }

            TryItResult.Init();

            HideReplayButton();

            if (GetConfigureDrawingCanvasFunction() != null)
            {
                GetConfigureDrawingCanvasFunction()(this, DrawingCanvas);
            }
        }

        public double GetWidth()
        {
            //if (_ParentButton.GetStroke4() != null)
            //{
            //    return Helpers.GetLocation(DemoUnit4).Right;
            //}
            //else if (_ParentButton.GetStroke3() != null)
            //{
            //    return Helpers.GetLocation(DemoUnit3).Right;
            //}
            //else if (_ParentButton.GetStroke2() != null)
            //{
            //    return Helpers.GetLocation(DemoUnit2).Right;
            //}
            //else
            {
                return Helpers.GetLocation(DemoUnit1).Right;
            }
        }

        public void Resize()
        {
            //DemoUnit2.Visibility = (_ParentButton.GetStroke2() != null ? Visibility.Visible : Visibility.Hidden);
            //DemoUnit3.Visibility = (_ParentButton.GetStroke3() != null ? Visibility.Visible : Visibility.Hidden);
            //DemoUnit4.Visibility = (_ParentButton.GetStroke4() != null ? Visibility.Visible : Visibility.Hidden);

            //BeveledRectangle.Width = GetWidth();
            //DropShadowRectangle.Width = GetWidth();

            //double borderOffset = Helpers.GetLocation(TryItRect).Left;
            //TryItRect.Width = GetWidth() - (borderOffset * 2);
            //TryItParent.Width = GetWidth() - (borderOffset * 2);

            //foreach (FrameworkElement elt in TryItParent.Children)
            //{
            //    elt.Width = GetWidth() - (borderOffset * 2);
            //}
        }

        protected static GestureExplorer LastExplorer = null;

        public double GetDemoUnitMaxHeight()
        {
            return DemoUnit1.GetActualHeight();// Helpers.Max(DemoUnit1.GetActualHeight(), DemoUnit2.GetActualHeight(), DemoUnit3.GetActualHeight(), DemoUnit4.GetActualHeight());
        }

        ArrayList Tabs = new ArrayList();

        public void AddTab(GestureExplorerTab tab)
        {
            Tabs.Add(tab);
        }

        public GestureExplorerTab[] GetTabs()
        {
            GestureExplorerTab[] tabs = new GestureExplorerTab[4];
            
            tabs[0] = Tab1;
            tabs[1] = Tab2;
            tabs[2] = Tab3;
            tabs[3] = Tab4;

            return tabs;
        }

        public void Init()
        {
            Tab1.ParentExplorer = Tab2.ParentExplorer = Tab3.ParentExplorer = Tab4.ParentExplorer = this;

            Tab1.Title = _ParentButton.Stroke1Title;
            Tab1.Visibility = (_ParentButton.Stroke1 != null ? Visibility.Visible : Visibility.Collapsed);

            Tab2.Title = _ParentButton.Stroke2Title;
            Tab2.Visibility = (_ParentButton.Stroke2 != null ? Visibility.Visible : Visibility.Collapsed);

            Tab3.Title = _ParentButton.Stroke3Title;
            Tab3.Visibility = (_ParentButton.Stroke3 != null ? Visibility.Visible : Visibility.Collapsed);

            Tab4.Title = _ParentButton.Stroke4Title;
            Tab4.Visibility = (_ParentButton.Stroke4 != null ? Visibility.Visible : Visibility.Collapsed);

            Tab1.Icon = _ParentButton.Stroke1Icon;
            Tab2.Icon = _ParentButton.Stroke2Icon;
            Tab3.Icon = _ParentButton.Stroke3Icon;
            Tab4.Icon = _ParentButton.Stroke4Icon;
        }

        bool _Init = false;

        public void Show()
        {
            DismissLastExplorer();

            

            if (!_Init)
            {
                _Init = true;
                Init();
            }

            Tab1.Expand();
            Tab2.Contract();
            Tab3.Contract();
            Tab4.Contract();

            

            LastExplorer = this;

            DismissCatcher.RenderTransform = new ScaleTransform(1000, 1.0);
            DismissCatcher.Height = 5000.0;

            

            Rect rc = Helpers.GetLocation(TryItRect);

            if (DrawingCanvasScroller != null)
            {
                DrawingCanvasScroller.RenderTransform = new TranslateTransform(0, 0);
                DrawingCanvasScroller.Width = rc.Width;
                DrawingCanvasScroller.Height = rc.Height;
            }

            if (DrawingCanvas != null)
            {
                DrawingCanvas.RenderTransform = new TranslateTransform(0, 0);
                DrawingCanvas.Width = rc.Width;
                DrawingCanvas.Height = rc.Height;
            }

            ConfigureTryIt();
            TryItResult.Init();

            DemoUnit1.Show();
            



            Resize();




            this.Visibility = Visibility.Visible;
//            Storyboard board = (Storyboard)Resources["ShowExplorer"];
//            board.Begin(this);


//            Tab1_OnSelect(null, null);

            DemoUnit1.StrokeNumber = 1;
            DemoUnit1.Show();
            PlayAnimation(true);

            //PlayAnimation(true);
        }

        //protected void DestroyTempPath()
        //{
        //    if (m_TempPath != null)
        //    {
        //        foreach (System.Windows.Shapes.Path path in m_TempPath)
        //        {
        //            AnimationCanvas.Children.Remove(path);
        //        }

        //        m_TempPath = null;
        //    }
        //}

        public FrameworkElement GetTryItOverlay()
        {
            switch ( DemoUnit1.StrokeNumber )
            {
                case 1:
                    return _ParentButton.Stroke1TryItOverlay;

                case 2:
                    return _ParentButton.Stroke2TryItOverlay;

                case 3:
                    return _ParentButton.Stroke3TryItOverlay;

                case 4:
                    return _ParentButton.Stroke4TryItOverlay;

                default:
                    return null;
            }
        }

        public void PlayAnimation(bool introAnimation)
        {
            StopPracticeAttentionAttract();

            ClearAnimationScreen();
            DemoUnit1.PrepareContext();
            DemoUnit1.PrepareScale();

            if (introAnimation)
            {
                Storyboard board = (Storyboard)Resources["IntroAnimation"];
                board.Completed += new EventHandler(board_Completed);

                board.Begin(this, true);
            }
            else
            {
                PlayAnimation_Main();
            }
        }

        public void NotifyAnimationCompleted()
        {
            StartPracticeAttentionAttract();
        }

        Storyboard _practiceAttentionAttract = null;

        public void StopPracticeAttentionAttract()
        {
            if (_practiceAttentionAttract != null)
            {
                _practiceAttentionAttract.Stop(this);
            }
        }

        public void StartPracticeAttentionAttract()
        {
            if (_practiceAttentionAttract == null)
            {
                _practiceAttentionAttract = (Storyboard)Resources["PracticeAttentionAttract"];
            }

            _practiceAttentionAttract.RepeatBehavior = RepeatBehavior.Forever;
            _practiceAttentionAttract.Begin(this, true);
        }

        void board_Completed(object sender, EventArgs e)
        {
            PlayAnimation_Main();
        }

        public void ClearAnimationScreen()
        {
            FrameworkElement elt = GetTryItOverlay();
            TryItOverlayParent.Children.Clear();

            if (elt != null)
            {
                elt.IsHitTestVisible = false;

                if (elt.Parent != null)
                {
                    ((Panel)elt.Parent).Children.Remove(elt);
                }

                TryItOverlayParent.Children.Add(elt);
            }

            DemoUnit1.PrepareForAnimation();
        }

        public void PlayAnimation_Main()
        {
            

            DemoUnit1.PlayAnimation(true);
            //DemoUnit2.PlayAnimation(false);
            //DemoUnit3.PlayAnimation(false);
            //DemoUnit4.PlayAnimation(false);
        }

        public void TrueInit()
        {
            DemoUnit1.TrueInit();
        }

        //public void PlayAnimation()
        //{
        //    if (_ParentButton.GetStroke1() != null)
        //    {
        //        DestroyTempPath();
        //        m_TempPath = AnimationAssist.ExecuteStrokeAnimation((ArrayList)(object)_ParentButton.GetStroke1(),
        //            AnimationCanvas, GesturePenIcon, _ParentButton.AnimationSpeed);

        //        foreach (System.Windows.Shapes.Path path in m_TempPath)
        //        {
        //            path.StrokeThickness = 1.5;
        //        }
        //    }
        //}

        private void ReplayAnimation_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            

            PlayAnimation(false);
        }

        private void DismissCatcher_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {

        }

        private void DismissCatcher_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Dismiss();
        }

        public static bool IsExplorerOpen()
        {
            return LastExplorer != null;
        }

        public static void DismissLastExplorer()
        {
            if (LastExplorer != null)
            {
                LastExplorer.Dismiss();
            }

            LastExplorer = null;
        }

        public void Dismiss()
        {
            if (_ParentButton != null)
            {
                
            }

            //Storyboard board = (Storyboard)Resources["DismissExplorer"];
            //board.Begin(this);

            Visibility = Visibility.Collapsed;

            LastExplorer = null;
        }

        private void TryItInkCanvas_StrokeCollected(object sender, InkCanvasStrokeCollectedEventArgs e)
        {
            if (_ParentButton.RecoFunction1 != null)
            {
                if (_ParentButton.RecoFunction1(e.Stroke))
                {
                    e.Stroke.DrawingAttributes.Color = Colors.Green;
                }
                else
                {
                    e.Stroke.DrawingAttributes.Color = Colors.Red;
                }
            }
        }

        private void ReplayButton_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ConfigureTryIt();
        }

        bool ReplayButtonVisible = false;

        public void ShowReplayButton()
        {
            return;

            Storyboard board = (Storyboard)Resources["ShowReplayButton"];
            board.Begin(this);

            ReplayButtonVisible = true;
        }

        public void HideReplayButton()
        {
            return;

            if (ReplayButtonVisible)
            {
                ReplayButtonVisible = false;

                Storyboard board = (Storyboard)Resources["HideReplayButton"];
                board.Begin(this);
            }
        }

        private void TryItResult_OnClick(object sender, EventArgs e)
        {
            ConfigureTryIt();
        }

        private void Tab1_OnSelect(object sender, EventArgs e)
        {
            

            DemoUnit1.StrokeNumber = 1;
            DemoUnit1.Show();
            PlayAnimation(false);

            ConfigureTryIt();
        }

        private void Tab2_OnSelect(object sender, EventArgs e)
        {
            

            DemoUnit1.StrokeNumber = 2;
            DemoUnit1.Show();
            PlayAnimation(false);

            ConfigureTryIt();
        }

        private void Tab3_OnSelect(object sender, EventArgs e)
        {
            

            DemoUnit1.StrokeNumber = 3;
            DemoUnit1.Show();
            PlayAnimation(false);

            ConfigureTryIt();
        }

        private void Tab4_OnSelect(object sender, EventArgs e)
        {
            

            DemoUnit1.StrokeNumber = 4;
            DemoUnit1.Show();
            PlayAnimation(false);

            ConfigureTryIt();
        }

        private void CloseExplorer_OnClick(object sender, EventArgs e)
        {
            Dismiss();
        }

        
	}
}