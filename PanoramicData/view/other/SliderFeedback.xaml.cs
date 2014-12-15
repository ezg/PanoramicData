using PanoramicDataModel;
using starPadSDK.AppLib;
using starPadSDK.Geom;
using starPadSDK.Inq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PixelLab.Common;
using starPadSDK.WPFHelp;
using PanoramicData.model.view;
using PanoramicData.view.table;
using PanoramicData.view.filter;
using PanoramicData.model.view_new;

namespace PanoramicData.view.other
{
    /// <summary>
    /// Interaction logic for SimpleDataGridDragFeedback.xaml
    /// </summary>
    public partial class SliderFeedback : UserControl, AttributeViewModelEventHandler
    {
        private InqScene _inqScene = null;
        private TableModel _tableModel = null;
        private PanoramicDataColumnDescriptor _columnDescriptor = null;

        public SliderFeedback(InqScene inqScene )
        {
            InitializeComponent();
            _inqScene = inqScene;
        }

        public void AddStroq(Stroq stroq)
        {
            inkPresenter.Strokes.Add(stroq.BackingStroke);
        }

        public void AttributeViewModelMoved(AttributeView sender, AttributeViewModelEventArgs e, bool overElement)
        {
            inkPresenter.Strokes[0].DrawingAttributes.Color = Color.FromArgb(0x88, 0x00, 0x8D, 0xff);

            InqScene inqScene = this.FindParent<InqScene>();
            if (overElement)
            {
                inkPresenter.Strokes[0].DrawingAttributes.Color = Colors.Black;
            }
        }

        public void AttributeViewModelDropped(AttributeView sender, AttributeViewModelEventArgs e)
        {
            /*inkPresenter.Strokes[0].DrawingAttributes.Color = Color.FromArgb(0x88, 0x00, 0x8D, 0xff);

            InqScene inqScene = this.FindParent<InqScene>();
            if (inqScene != null)
            {
                if (this.GetBounds(inqScene).IntersectsWith(e.Bounds))
                {
                    FilterHolder filter = new FilterHolder();
                    Rct bounds = inkPresenter.Strokes[0].GetBounds();
                    bounds = bounds.Inflate(60, 60);
                    Pt currentPos = this.TranslatePoint(new Point(0, 0), _inqScene);
                    currentPos = new Pt(currentPos.X - 30, currentPos.Y - 30);

                    FilterHolderViewModel filterHolderViewModel = new FilterHolderViewModel();
                    filterHolderViewModel.TableModel = e.TableModel != null ? e.TableModel : e.FilterModel.TableModel;
                    filterHolderViewModel.Label = e.ColumnDescriptor.Name;

                    PanoramicDataColumnDescriptor cdMax = (PanoramicDataColumnDescriptor) e.ColumnDescriptor.Clone();
                    cdMax.AggregateFunction = AggregateFunction.Max;
                    PanoramicDataColumnDescriptor cdMin = (PanoramicDataColumnDescriptor) e.ColumnDescriptor.Clone();
                    cdMin.AggregateFunction = AggregateFunction.Min;

                    filterHolderViewModel.AddColumnDescriptor(cdMin);
                    filterHolderViewModel.AddColumnDescriptor(cdMax);
                    filterHolderViewModel.AddColumnDescriptor((PanoramicDataColumnDescriptor)e.ColumnDescriptor.Clone());
                    filterHolderViewModel.FilterRendererType = FilterRendererType.Slider;
                    filterHolderViewModel.NoChrome = true;
                    inkPresenter.Strokes[0].DrawingAttributes.Color = Colors.LightGray;
                    filterHolderViewModel.Stroq = new Stroq(inkPresenter.Strokes[0]);
                    filter.FilterHolderViewModel = filterHolderViewModel;

                    filter.InitPostionAndDimension(currentPos, new Vec(bounds.Width, bounds.Height + 30));
                    
                    _inqScene.Rem(this);           
                } 
            }*/
        }

        protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
        {
            return new PointHitTestResult(this, hitTestParameters.HitPoint);
        }

        protected override GeometryHitTestResult HitTestCore(GeometryHitTestParameters hitTestParameters)
        {
            return new GeometryHitTestResult(this, IntersectionDetail.Intersects);
        }
    }
}
