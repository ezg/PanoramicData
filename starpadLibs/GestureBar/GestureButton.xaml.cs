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
using System.Windows.Ink;
using System.Collections;
using System.Windows.Documents;
using System.Windows.Markup;

namespace starPadSDK.GestureBarLib
{
    public partial class GestureButton : IGestureButton
	{
		public GestureButton()
		{
			this.InitializeComponent();

            InitNotify.OnTrueInit += new EventHandler(InitNotify_OnTrueInit);
		}

        public FrameworkElement ToFrameworkElement()
        {
            return this;
        }

        void InitNotify_OnTrueInit(object sender, EventArgs e)
        {
            if (Stroke1 != null)
            {
                DropArrowIcon.Visibility = Visibility.Visible;

                Rect rc = Helpers.GetSubstringRect(ButtonTitle, ButtonTitle.ContentEnd.GetPositionAtOffset(-1), ButtonTitle.ContentEnd);
                Rect rc2 = Helpers.GetLocation(ButtonTitle);
                DropArrowIcon.RenderTransform = new TranslateTransform(rc2.Left + rc.Left, rc2.Top + rc.Top + 2);

                _GestureExplorer.TrueInit();
            }
        }

        AnimationAssist _AnimationAssist = null;

        private void UserControl_Initialized(object sender, System.EventArgs e)
        {
            UpdateTransform();

            _AnimationAssist = new AnimationAssist(this);
        }

        public event EventHandler OnClick;

        GestureTab _Parent = null;

        public void SetParent(GestureTab parent)
        {
            _Parent = parent;

            if (_GestureExplorer.Parent != null)
            {
                ((Panel)_GestureExplorer.Parent).Children.Remove(_GestureExplorer);
            }

            //GestureBar.DefaultToolbarScroller.Children.Add(_GestureExplorer);
            _Parent.TabPage.Children.Add(_GestureExplorer);
            _GestureExplorer.Visibility = Visibility.Hidden;
            _GestureExplorer.IsHitTestVisible = false;
        }

        protected FrameworkElement _Stroke1Icon = null;

        public FrameworkElement Stroke1Icon
        {
            get
            {
                return _Stroke1Icon;
            }

            set
            {
                _Stroke1Icon = value;
            }
        }

        protected FrameworkElement _Stroke2Icon = null;

        public FrameworkElement Stroke2Icon
        {
            get
            {
                return _Stroke2Icon;
            }

            set
            {
                _Stroke2Icon = value;
            }
        }

        protected FrameworkElement _Stroke3Icon = null;

        public FrameworkElement Stroke3Icon
        {
            get
            {
                return _Stroke3Icon;
            }

            set
            {
                _Stroke3Icon = value;
            }
        }

        protected FrameworkElement _Stroke4Icon = null;

        public FrameworkElement Stroke4Icon
        {
            get
            {
                return _Stroke4Icon;
            }

            set
            {
                _Stroke4Icon = value;
            }
        }

        protected UIElement _FinalResultIcon = null;

        public UIElement FinalResultIcon
        {
            get
            {
                return _FinalResultIcon;
            }

            set
            {
                _FinalResultIcon = value;
            }
        }

        protected AuxiliaryAnimation _AuxAnimation = null;

        public UIElement AuxAnimation
        {
            get
            {
                return (UIElement) _AuxAnimation;
            }

            set
            {
                _AuxAnimation = (AuxiliaryAnimation)value;
            }
        }

        protected void UpdateTransform()
        {
            TransformGroup set = new TransformGroup();
            set.Children.Add(new TranslateTransform(m_TranslateAnimX, m_TranslateAnimY));
            set.Children.Add(new ScaleTransform(m_ScaleAnimX, m_ScaleAnimY));
            AnimationCanvas.RenderTransform = set;

            TransformGroup penTrans = new TransformGroup();
            penTrans.Children.Add(new TranslateTransform(m_TranslatePenX, m_TranslatePenY));
            penTrans.Children.Add(new ScaleTransform(m_ScaleAnimX, m_ScaleAnimY));
            PenIcon.RenderTransform = penTrans;
        }

        public Canvas ParentScroller = null;

        protected void DestroyTempPath()
        {
            if (m_TempPath != null)
            {
                foreach (System.Windows.Shapes.Path path in m_TempPath)
                {
                    AnimationCanvas.Children.Remove(path);
                }

                m_TempPath = null;
            }
        }

        public void PlayAnimation()
        {
            if (m_Stroke1 != null)
            {
                DestroyTempPath();
                m_TempPath = _AnimationAssist.ExecuteStrokeAnimation(m_Stroke1, AnimationCanvas, PenIcon, m_AnimationSpeed, true);
            }
        }

        GestureExplorer _GestureExplorer = new GestureExplorer();

        public GestureExplorer GestureExplorerControl
        {
            get
            {
                return _GestureExplorer; 
            }
        } 

        public void Click(object sender, System.Windows.Input.MouseButtonEventArgs e) 
        {
            if (m_Stroke1 == null)
            {
                GestureExplorer.DismissLastExplorer();

                Storyboard board = (Storyboard)Resources["ButtonClicked"];
                board.Begin(this);

                if (OnClick != null)
                {
                    OnClick(this, e);
                }

                if (ParentBar != null)
                {
                    ParentBar.InvokeClick(this, e);
                }
            }
            else if (!EatClick)
            {
                

                ButtonToolTip.Hide();

                //Storyboard board = (Storyboard)Resources["ButtonClicked"];
                //board.Begin(this);


                _GestureExplorer.Visibility = Visibility.Visible;
                _GestureExplorer.IsHitTestVisible = true;


                Rect rc = Helpers.GetLocationTrans(this, (FrameworkElement)_GestureExplorer.Parent);

                if (_ParentBar != null)
                {

                    if ((rc.Left + _GestureExplorer.ActualWidth) > Helpers.GetLocationTrans(_ParentBar, (FrameworkElement)_GestureExplorer.Parent).Right)
                    {
                        rc = new Rect((Helpers.GetLocationTrans(_ParentBar, (FrameworkElement)_GestureExplorer.Parent).Right - (_GestureExplorer.ActualWidth)) - 8, rc.Top, rc.Width, rc.Height);
                    }

                }

                _GestureExplorer.RenderTransform = new TranslateTransform(rc.Left, rc.Bottom); //Math.Max(0, 

                _GestureExplorer.Show();

            }

            EatClick = false;
        }

        private void ButtonCanvas_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            
        }

        protected GestureBar _ParentBar = null;

        public GestureBar ParentBar
        {
            get
            {
                return _ParentBar;
            }

            set
            {
                _ParentBar = value;

                _GestureExplorer.InitParentBar();
            }
        }


        private void ButtonCanvas_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            

            if (ParentScroller == null)
            {
                ParentScroller = GestureBar.DefaultToolbarScroller;
            }

            ButtonToolTip.Display(this);

            if (m_Stroke1 == null)
            {
                ReplayCanvas.Visibility = Visibility.Hidden;
                PenIcon.Visibility = Visibility.Hidden;
            }

            if (ParentScroller != null)
            {
                //double w = ParentScroller.Width - 50;

                //GeneralTransform transform = this.TransformToAncestor((Visual)this.Parent);
                //Point rootPoint = transform.Transform(new Point(0, 0));

                //double realX = rootPoint.X - ParentScroller.HorizontalOffset;

                //if ((realX + ButtonToolTip.Width) > w)
                //{
                //    ButtonToolTip.RenderTransform = new TranslateTransform(w - (realX + ButtonToolTip.Width), 0);
                //}
                //else
                {
              //      ButtonToolTip.RenderTransform = new TranslateTransform(0, 0);
                }
            }

            //PlayAnimation();
        }

        bool EatClick = false;

        private void ReplayCanvas_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //EatClick = true;
            //PlayAnimation();
        }

        private void ButtonCanvas_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            ButtonToolTip.Hide();
            DestroyTempPath();
        }

        ArrayList m_Stroke1 = null;
        ArrayList m_Stroke2 = null;
        ArrayList m_Stroke3 = null;
        ArrayList m_Stroke4 = null;
        ArrayList m_TempPath = null;
        UIElement m_Icon = null;
        Panel m_Context = null;
        double m_ContextTranslateX = 0;
        double m_ContextTranslateY = 0;

        double m_ScaleAnimX = 1.0;
        double m_ScaleAnimY = 1.0;
        double m_TranslateAnimX = 0;
        double m_TranslateAnimY = 0;
        double m_TranslatePenX = 0;
        double m_TranslatePenY = 0;
        int m_AnimationSpeed = 100;
        int m_AnimationSpeed2 = 100;
        int m_AnimationSpeed3 = 100;
        int m_AnimationSpeed4 = 100;

        public UIElement Context
        {
            get
            {
                return m_Context;
            }

            set
            {
                if (value is Panel)
                {
                    m_Context = (Panel) value;
                }
                else if (value is UserControl)
                {
                    m_Context = Helpers.ExtractPanel((UserControl)value);
                }
                else
                {
                    return;
                }

                _GestureExplorer.InitContext(this);

                ContextCanvas.Children.Clear();

                while (m_Context.Children.Count > 0)
                {
                    UIElement elt = (UIElement)m_Context.Children[0];

                    m_Context.Children.Remove(elt);
                    ContextCanvas.Children.Add(elt);
                }

                
            }
        }

        protected void UpdateContextTransform()
        {
            ContextCanvas.RenderTransform = new TranslateTransform(m_ContextTranslateX, m_ContextTranslateY);
        }

        public double ContextTranslateX
        {
            get
            {
                return m_ContextTranslateX;
            }

            set
            {
                m_ContextTranslateX = value;
                UpdateContextTransform();
            }
        }

        public double ContextTranslateY
        {
            get
            {
                return m_ContextTranslateY;
            }

            set
            {
                m_ContextTranslateY = value;
                UpdateContextTransform();
            }
        }

        public int AnimationSpeed
        {
            get
            {
                return m_AnimationSpeed;
            }

            set
            {
                m_AnimationSpeed = value;
            }
        }

        public int AnimationSpeed2
        {
            get
            {
                return m_AnimationSpeed2;
            }

            set
            {
                m_AnimationSpeed2 = value;
            }
        }

        public int AnimationSpeed3
        {
            get
            {
                return m_AnimationSpeed3;
            }

            set
            {
                m_AnimationSpeed3 = value;
            }
        }

        public int AnimationSpeed4
        {
            get
            {
                return m_AnimationSpeed4;
            }

            set
            {
                m_AnimationSpeed4 = value;
            }
        }

        public FrameworkElement TryItDrawingCanvas
        {
            get
            {
                return _GestureExplorer.TryItDrawingCanvas;
            }
        }



        public delegate bool RecoFunction(Stroke stroke);

        public delegate void ConfigureDrawingCanvasFunc(GestureExplorer sender, FrameworkElement drawingCanvas);




        ConfigureDrawingCanvasFunc _ConfigureDrawingCanvasFunc1 = null;

        public ConfigureDrawingCanvasFunc ConfigureDrawingCanvasFunction1
        {
            get
            {
                return _ConfigureDrawingCanvasFunc1;
            }

            set
            {
                _ConfigureDrawingCanvasFunc1 = value;
            }
        }

        ConfigureDrawingCanvasFunc _ConfigureDrawingCanvasFunc2 = null;

        public ConfigureDrawingCanvasFunc ConfigureDrawingCanvasFunction2
        {
            get
            {
                return _ConfigureDrawingCanvasFunc2;
            }

            set
            {
                _ConfigureDrawingCanvasFunc2 = value;
            }
        }

        ConfigureDrawingCanvasFunc _ConfigureDrawingCanvasFunc3 = null;

        public ConfigureDrawingCanvasFunc ConfigureDrawingCanvasFunction3
        {
            get
            {
                return _ConfigureDrawingCanvasFunc3;
            }

            set
            {
                _ConfigureDrawingCanvasFunc3 = value;
            }
        }

        ConfigureDrawingCanvasFunc _ConfigureDrawingCanvasFunc4 = null;

        public ConfigureDrawingCanvasFunc ConfigureDrawingCanvasFunction4
        {
            get
            {
                return _ConfigureDrawingCanvasFunc4;
            }

            set
            {
                _ConfigureDrawingCanvasFunc4 = value;
            }
        }


        


        RecoFunction _RecoFunction1 = null;

        public RecoFunction RecoFunction1
        {
            get
            {
                return _RecoFunction1;
            }

            set
            {
                _RecoFunction1 = value;
            }
        }

        public double ScaleAnimX
        {
            get
            {
                return m_ScaleAnimX;
            }

            set
            {
                m_ScaleAnimX = value;
                UpdateTransform();
            }
        }

        public double ScaleAnimY
        {
            get
            {
                return m_ScaleAnimY;
            }

            set
            {
                m_ScaleAnimY = value;
                UpdateTransform();
            }
        }

        public double TranslateAnimX
        {
            get
            {
                return m_TranslateAnimX;
            }

            set
            {
                m_TranslateAnimX = value;
                UpdateTransform();
            }
        }

        public double TranslateAnimY
        {
            get
            {
                return m_TranslateAnimY;
            }

            set
            {
                m_TranslateAnimY = value;
                UpdateTransform();
            }
        }

        public double TranslatePenX
        {
            get
            {
                return m_TranslatePenX;
            }

            set
            {
                m_TranslatePenX = value;
                UpdateTransform();
            }
        }

        public double TranslatePenY
        {
            get
            {
                return m_TranslatePenY;
            }

            set
            {
                m_TranslatePenY = value;
                UpdateTransform();
            }
        }

        public UIElement DisplayIcon
        {
            get
            {
                return m_Icon;
            }

            set
            {
                

                m_Icon = value;

                if (m_Icon != null)
                {
                    IconCanvas.Children.Clear();
                    IconCanvas.Children.Add(m_Icon);
                    if (m_Icon is UserControl) {
                        UserControl micon = m_Icon as UserControl;
                        micon.Margin = new Thickness((IconCanvas.Width - micon.Width) / 2, (IconCanvas.Height-micon.Height)/2, 0, 0);
                    }
                }

                _GestureExplorer.InitIcon(this);
            }
        }

        

        protected FrameworkElement _Stroke1Value = null;
        protected FrameworkElement _Stroke2Value = null;
        protected FrameworkElement _Stroke3Value = null;
        protected FrameworkElement _Stroke4Value = null;

        public ArrayList GetStroke1()
        {
            return m_Stroke1;
        }

        public ArrayList GetStroke2()
        {
            return m_Stroke2;
        }

        public ArrayList GetStroke3()
        {
            return m_Stroke3;
        }

        public ArrayList GetStroke4()
        {
            return m_Stroke4;
        }

        protected Panel m_Stroke1DetailsLayer = null;

        public Panel Stroke1DetailsLayer
        {
            get
            {
                return m_Stroke1DetailsLayer;
            }
        }

        protected Panel m_Stroke1Context = null;

        public Panel Stroke1Context
        {
            get
            {
                return m_Stroke1Context;
            }
        }

        public FrameworkElement Stroke1
        {
            get
            {
                return (FrameworkElement)_Stroke1Value;
            }

            set
            {
                _Stroke1Value = value;

                if (value is System.Windows.Shapes.Path)
                {
                    m_Stroke1 = new ArrayList();
                    m_Stroke1.Add((System.Windows.Shapes.Path) value);
                }
                else if (value is UserControl)
                {
                    m_Stroke1 = Helpers.ExtractPaths((UserControl)value);
                    m_Stroke1DetailsLayer = Helpers.ExtractPanelOfName((UserControl)value, "DetailsLayer");
                    m_Stroke1Context = Helpers.ExtractPanelOfName((UserControl)value, "ContextLayer");
                }
                else
                {
                    throw new Exception("Invalid value - must be of type path or a user control containing a single path.");
                }
            }
        }

        FrameworkElement _Stroke1TryItOverlay = null;

        public FrameworkElement Stroke1TryItOverlay
        {
            get
            {
                return _Stroke1TryItOverlay;
            }

            set
            {
                _Stroke1TryItOverlay = value;
            }
        }

        FrameworkElement _Stroke2TryItOverlay = null;

        public FrameworkElement Stroke2TryItOverlay
        {
            get
            {
                return _Stroke2TryItOverlay;
            }

            set
            {
                _Stroke2TryItOverlay = value;
            }
        }

        FrameworkElement _Stroke3TryItOverlay = null;

        public FrameworkElement Stroke3TryItOverlay
        {
            get
            {
                return _Stroke3TryItOverlay;
            }

            set
            {
                _Stroke3TryItOverlay = value;
            }
        }

        FrameworkElement _Stroke4TryItOverlay = null;

        public FrameworkElement Stroke4TryItOverlay
        {
            get
            {
                return _Stroke4TryItOverlay;
            }

            set
            {
                _Stroke4TryItOverlay = value;
            }
        }

        protected Panel m_Stroke2DetailsLayer = null;

        public Panel Stroke2DetailsLayer
        {
            get
            {
                return m_Stroke2DetailsLayer;
            }
        }

        protected Panel m_Stroke2Context = null;

        public Panel Stroke2Context
        {
            get
            {
                return m_Stroke2Context;
            }
        }

        public FrameworkElement Stroke2
        {
            get
            {
                return (FrameworkElement)_Stroke2Value;
            }

            set
            {
                _Stroke2Value = value;

                if (value is System.Windows.Shapes.Path)
                {
                    m_Stroke2 = new ArrayList();
                    m_Stroke2.Add((System.Windows.Shapes.Path)value);
                }
                else if (value is UserControl)
                {
                    m_Stroke2 = Helpers.ExtractPaths((UserControl)value);
                    m_Stroke2DetailsLayer = Helpers.ExtractPanelOfName((UserControl)value, "DetailsLayer");
                    m_Stroke2Context = Helpers.ExtractPanelOfName((UserControl)value, "ContextLayer");
                }
                else
                {
                    throw new Exception("Invalid value - must be of type path or a user control containing a single path.");
                }
            }
        }

        protected Panel m_Stroke3DetailsLayer = null;

        public Panel Stroke3DetailsLayer
        {
            get
            {
                return m_Stroke3DetailsLayer;
            }
        }

        protected Panel m_Stroke3Context = null;

        public Panel Stroke3Context
        {
            get
            {
                return m_Stroke3Context;
            }
        }

        public FrameworkElement Stroke3
        {
            get
            {
                return (FrameworkElement)_Stroke3Value;
            }

            set
            {
                _Stroke3Value = value;

                if (value is System.Windows.Shapes.Path)
                {
                    m_Stroke3 = new ArrayList();
                    m_Stroke3.Add((System.Windows.Shapes.Path)value);
                }
                else if (value is UserControl)
                {
                    m_Stroke3 = Helpers.ExtractPaths((UserControl)value);
                    m_Stroke3DetailsLayer = Helpers.ExtractPanelOfName((UserControl)value, "DetailsLayer");
                    m_Stroke3Context = Helpers.ExtractPanelOfName((UserControl)value, "ContextLayer");
                }
                else
                {
                    throw new Exception("Invalid value - must be of type path or a user control containing a single path.");
                }
            }
        }

        protected Panel m_Stroke4DetailsLayer = null;

        public Panel Stroke4DetailsLayer
        {
            get
            {
                return m_Stroke4DetailsLayer;
            }
        }

        protected Panel m_Stroke4Context = null;

        public Panel Stroke4Context
        {
            get
            {
                return m_Stroke4Context;
            }
        }

        public FrameworkElement Stroke4
        {
            get
            {
                return (FrameworkElement)_Stroke4Value;
            }

            set
            {
                _Stroke4Value = value;

                if (value is System.Windows.Shapes.Path)
                {
                    m_Stroke4 = new ArrayList();
                    m_Stroke4.Add((System.Windows.Shapes.Path)value);
                }
                else if (value is UserControl)
                {
                    m_Stroke4 = Helpers.ExtractPaths((UserControl)value);
                    m_Stroke4DetailsLayer = Helpers.ExtractPanelOfName((UserControl)value, "DetailsLayer");
                    m_Stroke4Context = Helpers.ExtractPanelOfName((UserControl)value, "ContextLayer");
                }
                else
                {
                    throw new Exception("Invalid value - must be of type path or a user control containing a single path.");
                }
            }
        }

        public string Title
        {
            get
            {
                return ButtonTitle.Text;
            }

            set
            {
                ButtonTitle.Text = value;
                ButtonTitle.LineHeight = 12.0;
                ButtonTitle.LineStackingStrategy = LineStackingStrategy.BlockLineHeight;

                Helpers.ForceUpdateLayout(ButtonTitle);

                

                //Helpers.MakeBold(ButtonTitle, ButtonTitle.ContentStart, ButtonTitle.ContentEnd, true);
                //Helpers.MakeVerticalAlignment(ButtonTitle, BaselineAlignment.Bottom);
            }
        }

        public string TooltipTitle
        {
            get
            {
                return ButtonToolTip.Title;
            }

            set
            {
                ButtonToolTip.Title = value;
            }
        }

        public string TooltipDescription
        {
            get
            {
                return ButtonToolTip.Description;
            }

            set
            {
                ButtonToolTip.Description = value;
            }
        }




        protected string _Stroke1Title = "";

        public string Stroke1Title
        {
            get
            {
                return _Stroke1Title;
            }

            set
            {
                _Stroke1Title = value;
            }
        }

        protected string _Stroke2Title = "";

        public string Stroke2Title
        {
            get
            {
                return _Stroke2Title;
            }

            set
            {
                _Stroke2Title = value;
            }
        }

        protected string _Stroke3Title = "";

        public string Stroke3Title
        {
            get
            {
                return _Stroke3Title;
            }

            set
            {
                _Stroke3Title = value;
            }
        }

        protected string _Stroke4Title = "";

        public string Stroke4Title
        {
            get
            {
                return _Stroke4Title;
            }

            set
            {
                _Stroke4Title = value;
            }
        }








        protected string _Stroke1FeatureDescription = "";

        public string Stroke1FeatureDescription
        {
            get
            {
                return _Stroke1FeatureDescription;
            }

            set
            {
                _Stroke1FeatureDescription = value;
            }
        }

        protected string _Stroke2FeatureDescription = "";

        public string Stroke2FeatureDescription
        {
            get
            {
                return _Stroke2FeatureDescription;
            }

            set
            {
                _Stroke2FeatureDescription = value;
            }
        }

        protected string _Stroke3FeatureDescription = "";

        public string Stroke3FeatureDescription
        {
            get
            {
                return _Stroke3FeatureDescription;
            }

            set
            {
                _Stroke3FeatureDescription = value;
            }
        }

        protected string _Stroke4FeatureDescription = "";

        public string Stroke4FeatureDescription
        {
            get
            {
                return _Stroke4FeatureDescription;
            }

            set
            {
                _Stroke4FeatureDescription = value;
            }
        }

        private void UserControl_LayoutUpdated(object sender, EventArgs e)
        {
            
        }

        private void ButtonCanvas_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Click(sender, e);
        }
	}
}