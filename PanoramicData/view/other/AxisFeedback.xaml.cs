using System.Linq;
using PanoramicDataModel;
using starPadSDK.AppLib;
using starPadSDK.Geom;
using starPadSDK.Inq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PixelLab.Common;
using starPadSDK.WPFHelp;
using PanoramicData.view.table;
using PanoramicData.model.view;
using PanoramicData.model.view_new;

namespace PanoramicData.view.other
{
    /// <summary>
    /// Interaction logic for SimpleDataGridDragFeedback.xaml
    /// </summary>
    public partial class AxisFeedback : UserControl, AttributeViewModelEventHandler
    {
        private InqScene _inqScene = null;
        private TableModel xTableModel = null;
        private TableModel yTableModel = null;
        private FilterModel xFilterModel = null;
        private FilterModel yFilterModel = null;
        private PanoramicDataColumnDescriptor xColumnDescriptor = null;
        private PanoramicDataColumnDescriptor yColumnDescriptor = null;
        private  FilterRendererType _type = FilterRendererType.Table;

        public AxisFeedback(InqScene inqScene, FilterRendererType type)
        {
            InitializeComponent();
            _inqScene = inqScene;
            _type = type;
            if (_type == FilterRendererType.Pie)
            {
                xBorder.Visibility = Visibility.Collapsed;
            }  
        }

        public void AddStroq(Stroq stroq)
        {
            inkPresenter.Strokes.Add(stroq.BackingStroke);
        }

        public void AttributeViewModelMoved(AttributeViewModel sender, AttributeViewModelEventArgs e, bool overElement)
        {
            xBorder.BorderThickness = new Thickness(1);
            yBorder.BorderThickness = new Thickness(1);

            InqScene inqScene = this.FindParent<InqScene>();
            if (overElement)
            {
                if (xBorder.GetBounds(inqScene).IntersectsWith(e.Bounds))
                {
                    xBorder.BorderThickness = new Thickness(3);
                }
                else if (yBorder.GetBounds(inqScene).IntersectsWith(e.Bounds))
                {
                    yBorder.BorderThickness = new Thickness(3);
                }
            }
        }

        public void AttributeViewModelDropped(AttributeViewModel sender, AttributeViewModelEventArgs e)
        {
           /* xBorder.BorderThickness = new Thickness(1);
            yBorder.BorderThickness = new Thickness(1);

            InqScene inqScene = this.FindParent<InqScene>();
            if (inqScene != null)
            {
                if (xBorder.GetBounds(inqScene).IntersectsWith(e.Bounds))
                {
                    xAxis.Text = e.ColumnDescriptor.GetSimpleLabel();
                    xTableModel = e.TableModel != null ? e.TableModel : e.FilterModel.TableModel;
                    xFilterModel = e.FilterModel;
                    xColumnDescriptor = e.ColumnDescriptor;
                } 
                else if (yBorder.GetBounds(inqScene).IntersectsWith(e.Bounds))
                {
                    yAxis.Text = e.ColumnDescriptor.GetSimpleLabel();
                    yTableModel = e.TableModel != null ? e.TableModel : e.FilterModel.TableModel;
                    yFilterModel = e.FilterModel;
                    yColumnDescriptor = e.ColumnDescriptor;
                }

                if (yColumnDescriptor != null && xColumnDescriptor != null && xTableModel == yTableModel)
                {
                    Pt currentPos = this.TranslatePoint(new Point(0, 0), _inqScene);

                    FilterHolder filter = new FilterHolder();
                    FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateDefault(
                        (PanoramicDataColumnDescriptor)xColumnDescriptor,
                        (PanoramicDataColumnDescriptor)yColumnDescriptor,
                        xTableModel, _type);
                    filterHolderViewModel.Center = new Point();

                    if (yFilterModel == xFilterModel)
                    {
                        yFilterModel.GetColumnDescriptorsForOption(Option.GroupBy)
                            .Select(cd => (PanoramicDataColumnDescriptor) cd.Clone())
                            .ForEach(cd => filterHolderViewModel.AddOptionColumnDescriptor(Option.GroupBy, cd));

                        xFilterModel.GetColumnDescriptorsForOption(Option.ColorBy)
                            .Select(cd => (PanoramicDataColumnDescriptor)cd.Clone())
                            .ForEach(cd => filterHolderViewModel.AddOptionColumnDescriptor(Option.ColorBy, cd));
                    }
                    filter.FilterHolderViewModel = filterHolderViewModel;

                    filter.InitPostionAndDimension(currentPos, new Vec(this.Width, this.Height));
                    
                    _inqScene.Rem(this);
                }
                if (yColumnDescriptor != null && _type == FilterRendererType.Pie)
                {
                    Pt currentPos = this.TranslatePoint(new Point(0, 0), _inqScene);

                    FilterHolder filter = new FilterHolder();
                    FilterHolderViewModel filterHolderViewModel = FilterHolderViewModel.CreateDefaultPie(
                        (PanoramicDataColumnDescriptor)yColumnDescriptor.Clone(),
                        yTableModel);
                    filterHolderViewModel.Center = new Point();
                    filter.FilterHolderViewModel = filterHolderViewModel;

                    filter.InitPostionAndDimension(currentPos, new Vec(this.Width, this.Height));

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
