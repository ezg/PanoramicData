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
	public partial class GestureExplorerDemoUnit
	{
		public GestureExplorerDemoUnit()
		{
			this.InitializeComponent();
		}

        IGestureButton _ParentButton = null;
        ArrayList m_TempPath = null;

        FrameworkElement DisplayIcon = null;
        FrameworkElement ContextIconContent = null;

        public void Init(IGestureButton parentButton)
        {
            _ParentButton = parentButton;

            CommandName.Text = _ParentButton.Title;
        }

        public void InitContext(IGestureButton parentButton)
        {
            Init(parentButton);

            if (_ParentButton.Context != null)
            {
                Helpers.ForceUpdateLayout(_ParentButton.ToFrameworkElement());

                ContextIconContent = Helpers.CloneUsingXaml((FrameworkElement)_ParentButton.Context);
                ContextIcon.Children.Add(ContextIconContent);

                ContextIconContent.RenderTransform = new TranslateTransform(0, 0);

                
            }

            
        }

        Hashtable _StoredDetailLayers = new Hashtable();

        public void SetStoredDetailsLayer(Panel panel)
        {
            if (_StoredDetailLayers.ContainsKey(StrokeNumber))
            {
                _StoredDetailLayers.Remove(StrokeNumber);
            }
            
            _StoredDetailLayers.Add(StrokeNumber, panel);
        }

        public Panel GetStoredDetailsLayer()
        {
            return (Panel) _StoredDetailLayers[StrokeNumber];
        }

        //Panel _DetailsLayer = null;

        Panel _ContextLayer = null;

        public void InitIcon(IGestureButton parentButton)
        {
            Init(parentButton);

            Helpers.ForceUpdateLayout(_ParentButton.ToFrameworkElement());

            if (_ParentButton.DisplayIcon != null)
            {
                DisplayIcon = Helpers.CloneUsingXaml((FrameworkElement)_ParentButton.DisplayIcon);
                CommandIcon.Children.Add(DisplayIcon);
            }
            

            
        }

        protected void DestroyTempPath()
        {
            if (m_TempPath != null)
            {
                // Clear

                GesturePenIcon.BeginAnimation(Canvas.LeftProperty, null);
                GesturePenIcon.BeginAnimation(Canvas.TopProperty, null);

                foreach (FrameworkElement elt in Helpers.GetAllChildren(AnimationCanvas))
                {
                    if (elt is System.Windows.Shapes.Path)
                    {
                        AnimationCanvas.Children.Remove(elt);
                    }
                }

/*                foreach (System.Windows.Shapes.Path path in m_TempPath)
                {
                    AnimationCanvas.Children.Remove(path);
                }*/

                m_TempPath = null;

                // Remove start dots

                ArrayList toDelete = new ArrayList();

                foreach (FrameworkElement elt in AnimationCanvas.Children)
                {
                    if (elt is PenDownDot)
                    {
                        toDelete.Add(elt);
                    }
                }

                foreach (PenDownDot dot in toDelete)
                {
                    AnimationCanvas.Children.Remove(dot);
                }
            }
        }

        int _StrokeNumber = 1;

        public int StrokeNumber
        {
            get
            {
                return _StrokeNumber;
            }

            set
            {
                _StrokeNumber = value;
            }
        }

        public ArrayList GetStroke()
        {
            switch (_StrokeNumber)
            {
                case 1:
                    return _ParentButton.GetStroke1();

                case 2:
                    return _ParentButton.GetStroke2();

                case 3:
                    return _ParentButton.GetStroke3();

                case 4:
                    return _ParentButton.GetStroke4();

                default:
                    return null;
            }
        }

        

        public Panel GetDetailsLayer()
        {
            switch (_StrokeNumber)
            {
                case 1:
                    return _ParentButton.Stroke1DetailsLayer;

                case 2:
                    return _ParentButton.Stroke2DetailsLayer;

                case 3:
                    return _ParentButton.Stroke3DetailsLayer;

                case 4:
                    return _ParentButton.Stroke4DetailsLayer;

                default:
                    return null;
            }
        }

        public Panel GetContextLayer()
        {
            switch (_StrokeNumber)
            {
                case 1:
                    return _ParentButton.Stroke1Context;

                case 2:
                    return _ParentButton.Stroke2Context;

                case 3:
                    return _ParentButton.Stroke3Context;

                case 4:
                    return _ParentButton.Stroke4Context;

                default:
                    return null;
            }
        }

        public int GetAnimationSpeed()
        {
            switch (_StrokeNumber)
            {
                case 1:
                    return _ParentButton.AnimationSpeed;

                case 2:
                    return _ParentButton.AnimationSpeed2;

                case 3:
                    return _ParentButton.AnimationSpeed3;

                case 4:
                    return _ParentButton.AnimationSpeed4;

                default:
                    return 100;
            }
        }

        protected DateTime AnimStartTimestamp = DateTime.MinValue;
        protected double AnimDuration = 0;

        Transform OriginalAnimationCanvasRenderTransform = null;

        public void ShowDetailsLayer()
        {
            Storyboard board = (Storyboard)Resources["ShowDetailsLayer"];
            board.Begin(this);
        }

        public void HideDetailsLayer()
        {
            Storyboard board = (Storyboard)Resources["HideDetailsLayer"];
            board.Begin(this);
        }

        public void PrepareForAnimation()
        {
            HideDetailsLayer();
            DestroyTempPath();

            _ContextLayer = null;
            ContextLayerParent.Children.Clear();
        }

        public void PrepareContext()
        {
            if (_ContextLayer != GetContextLayer())
            {
                ContextLayerParent.Children.Clear();
                ContextLayerParent.Opacity = 0.5;

                if (GetContextLayer() != null)
                {
                    Panel contextParent = (Panel)GetContextLayer().Parent;

                    if (contextParent != DetailsLayerParent)
                    {
                        if (contextParent != null)
                        {
                            contextParent.Children.Remove(GetContextLayer());
                        }

                        ContextLayerParent.Children.Add(GetContextLayer());
                    }

                    Helpers.SendToBack2(ContextLayerParent);
                }

                _ContextLayer = GetContextLayer();
            }
        }

        public void PrepareScale()
        {
            if (OriginalAnimationCanvasRenderTransform == null)
            {
                OriginalAnimationCanvasRenderTransform = AnimationCanvas.RenderTransform;
            }
            else
            {
                AnimationCanvas.RenderTransform = OriginalAnimationCanvasRenderTransform;
            }

            double scaleFactor = 1.0;

            Rect rc = AnimationAssist.ComputeBoundingBox((ArrayList)(object)GetStroke());


            double targetWidth = 100;
            double targetHeight = 100;

            if (rc.Width > rc.Height)
            {
                scaleFactor = targetWidth / rc.Width;
                scaleFactor = targetHeight / rc.Height;
            }
            else
            {
                scaleFactor = targetHeight / rc.Height;
            }

            scaleFactor = Math.Min(targetWidth / rc.Width, targetHeight / rc.Height);

            scaleFactor = Math.Max(1.0, scaleFactor); //
            //Helpers.ApplyScaleTransform2(AnimationCanvas, scaleFactor, scaleFactor);
            //AnimationCanvas.RenderTransform = new ScaleTransform(scaleFactor, scaleFactor);
            AnimationCanvasParent.RenderTransform = new ScaleTransform(scaleFactor, scaleFactor);
            Helpers.ApplyTranslateTransform(AnimationCanvasParent, 8, 8);
        }

        public void InitDetailsLayer()
        {
            if (GetStoredDetailsLayer() != GetDetailsLayer())
            {
                //DetailsLayerParent.Children.Clear();

                if ((GetDetailsLayer() != null) && (GetStoredDetailsLayer() == null))
                {
                    Panel detailsParent = (Panel)GetDetailsLayer().Parent;

                    if (detailsParent != DetailsLayerParent)
                    {
                        if (detailsParent != null)
                        {
                            detailsParent.Children.Remove(GetDetailsLayer());
                        }

                        DetailsLayerParent.Children.Add(GetDetailsLayer());
                    }

                    GetDetailsLayer().MouseLeftButtonUp += new System.Windows.Input.MouseButtonEventHandler(_DetailsLayer_MouseLeftButtonUp);

                    SetStoredDetailsLayer(GetDetailsLayer());
                }
            }
        }

        public void PlayAnimation(bool beginAnim)
        {
            if (GetStroke() != null)
            {
                // Init details layer if available

                HideDetailsLayer();             

                // Update details layer visibility

                foreach (FrameworkElement elt in DetailsLayerParent.Children)
                {
                    elt.Visibility = Visibility.Hidden;
                }

                if (GetStoredDetailsLayer() != null)
                {
                    GetStoredDetailsLayer().Visibility = Visibility.Visible;
                }
                

                // Apply scale

                PrepareScale();

                // Execute animation

                Storyboard penDownAnim = (Storyboard)Resources["PutDownPen"];
                Storyboard penUpAnim = (Storyboard)Resources["PickUpPen"];

                DestroyTempPath();
                m_TempPath = _AnimationAssist.ExecuteStrokeAnimation((ArrayList)(object)GetStroke(),
                    AnimationCanvas, GesturePenIcon, GetAnimationSpeed(), beginAnim, ref AnimDuration, 
                    penDownAnim, penUpAnim);

                foreach (System.Windows.Shapes.Path path in m_TempPath)
                {
                    path.StrokeThickness = 1.5;
                }

                AnimStartTimestamp = DateTime.Now;

                // Show context layer if available

                PrepareContext();

                Helpers.BringToFront(DetailsLayerParent);
            }
        }

        public void TrueInit()
        {
            int strokeNum = StrokeNumber;

            for (int i = 1; i <= 4; i++)
            {
                StrokeNumber = i;

                if (GetStroke() != null)
                {
                    InitDetailsLayer();
                }                
            }

            StrokeNumber = strokeNum;
        }

        void _DetailsLayer_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PlayAnimation(true);
        }

        public bool IsAnimPlaying()
        {
            TimeSpan span = DateTime.Now.Subtract(AnimStartTimestamp);
            return span.TotalMilliseconds < AnimDuration;
        }

        public void Show()
        {
            HideDetailsLayer();

            if (DisplayIcon != null)
            {
                Helpers.SizeToFit(DisplayIcon, 20, 20);
            }

            switch (StrokeNumber)
            {
                case 1:
                    CaptionBlock.Text = _ParentButton.Stroke1FeatureDescription;
                    break;

                case 2:
                    CaptionBlock.Text = _ParentButton.Stroke2FeatureDescription;
                    break;

                case 3:
                    CaptionBlock.Text = _ParentButton.Stroke3FeatureDescription;
                    break;

                case 4:
                    CaptionBlock.Text = _ParentButton.Stroke4FeatureDescription;
                    break;
            }

        }

        public double GetActualHeight()
        {
            Helpers.ForceUpdateLayout(CaptionBlock);

            return Helpers.GetLocation(CaptionBlock).Bottom + Helpers.GetLocation(CaptionBlock).Left;
        }

        public void SetHeight(double height)
        {
            //this.Height = height;
        }

        private void ReplayRect_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            
        }

        private void UserControl_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
     //       if (!IsAnimPlaying())
            {
                //PlayAnimation(true);
            }
        }

        private void UserControl_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //PlayAnimation(false);
        }

        AnimationAssist _AnimationAssist = null;

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            _AnimationAssist = new AnimationAssist(this);
        }

        public void NotifyAnimationCompleted()
        {
            if ((_ParentButton != null) && (_ParentButton is GestureButton))
            {
                if (((GestureButton) _ParentButton).GestureExplorerControl != null)
                {
                    ((GestureButton) _ParentButton).GestureExplorerControl.NotifyAnimationCompleted();
                }
            }
        }

        private void ReplayAnimation_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            

            PlayAnimation(true);

            
        }
	}
}