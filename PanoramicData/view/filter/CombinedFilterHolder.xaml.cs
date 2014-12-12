using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PanoramicDataModel;
using PixelLab.Common;
using starPadSDK.AppLib;
using starPadSDK.Geom;
using starPadSDK.WPFHelp;
using PanoramicData.model.view;

namespace PanoramicData.view.filter
{
    /// <summary>
    /// Interaction logic for CombinedFilterHolder.xaml
    /// </summary>
    public partial class CombinedFilterHolder : MovableElement
    {
        private const int FILTER_SMALL_WIDTH = 60;
        private const int FILTER_SMALL_HEIGHT = 60;
        private InqScene _inqScene = null;
        private FilterHolder _centerFilterHolder = null;

        public static readonly DependencyProperty LeftFilterHolderViewModelProperty = DependencyProperty.Register("LeftFilterHolderViewModel", typeof(FilterHolderViewModel), typeof(CombinedFilterHolder), new PropertyMetadata(OnLeftFilterHolderViewModelChanged));

        public FilterHolderViewModel LeftFilterHolderViewModel
        {
            get
            {
                return (FilterHolderViewModel)GetValue(LeftFilterHolderViewModelProperty);
            }
            set
            {
                SetValue(LeftFilterHolderViewModelProperty, value);
            }
        }

        static void OnLeftFilterHolderViewModelChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ((CombinedFilterHolder) obj).OnLeftFilterHolderViewModelChanged(args);
        }

        private void OnLeftFilterHolderViewModelChanged(DependencyPropertyChangedEventArgs args)
        {
            left.Children.Clear();
            FilterHolder filter = new FilterHolder();
            filter.Width = FILTER_SMALL_WIDTH;
            filter.Height = FILTER_SMALL_HEIGHT;
            FilterHolderViewModel model = args.NewValue as FilterHolderViewModel;
            model.Margin = 0;
            model.ShowSettings = false;
            filter.FilterHolderViewModel = model;
            
            left.Children.Add(filter);

            init();
        }

        public static readonly DependencyProperty RightFilterHolderViewModelProperty = DependencyProperty.Register("RightFilterHolderViewModel", typeof(FilterHolderViewModel), typeof(CombinedFilterHolder), new PropertyMetadata(OnRightFilterHolderViewModelChanged));

        public FilterHolderViewModel RightFilterHolderViewModel
        {
            get
            {
                return (FilterHolderViewModel)GetValue(RightFilterHolderViewModelProperty);
            }
            set
            {
                SetValue(RightFilterHolderViewModelProperty, value);
            }
        }

        static void OnRightFilterHolderViewModelChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ((CombinedFilterHolder)obj).OnRightFilterHolderViewModelChanged(args);
        }

        private void OnRightFilterHolderViewModelChanged(DependencyPropertyChangedEventArgs args)
        {
            right.Children.Clear();
            FilterHolder filter = new FilterHolder();
            filter.Width = FILTER_SMALL_WIDTH;
            filter.Height = FILTER_SMALL_HEIGHT;
            FilterHolderViewModel model = args.NewValue as FilterHolderViewModel;
            model.Margin = 0;
            model.ShowSettings = false;
            filter.FilterHolderViewModel = model;

            right.Children.Add(filter);

            init();
        }

        private FilterHolderViewModel _filterHolderViewModel = null;

        public FilterHolderViewModel FilterHolderViewModel
        {
            get
            {
                return _filterHolderViewModel;
            }
            set
            {
                _filterHolderViewModel = value;
            }
        }

        public CombinedFilterHolder(InqScene inqScene)
        {
            InitializeComponent();
            _inqScene = inqScene;
        }

        private void init()
        {
            if (LeftFilterHolderViewModel != null &&
                RightFilterHolderViewModel != null)
            {
                _filterHolderViewModel = new FilterHolderViewModel();
                //filterHolderViewModel.Center = new Point(currentPos.X + this.Width / 2.0, currentPos.Y + this.Height / 2.0);
                _filterHolderViewModel.TableModel = LeftFilterHolderViewModel.TableModel;

                _filterHolderViewModel.FilterRendererType = FilterRendererType.Plot;

                foreach (var cd in LeftFilterHolderViewModel.ColumnDescriptors)
                {
                    if (!_filterHolderViewModel.ColumnDescriptors.Contains(cd))
                        _filterHolderViewModel.AddColumnDescriptor(cd);
                } 
                foreach (var cd in RightFilterHolderViewModel.ColumnDescriptors)
                {
                    if (!_filterHolderViewModel.ColumnDescriptors.Contains(cd))
                        _filterHolderViewModel.AddColumnDescriptor(cd);
                }

                // TODO
                //_filterHolderViewModel.AddXColumnDescriptor(LeftFilterHolderViewModel.YColumnDescriptors[0]);
                //_filterHolderViewModel.AddYColumnDescriptor(RightFilterHolderViewModel.YColumnDescriptors[0]);

                _centerFilterHolder = new FilterHolder();
                Vec pos = (LeftFilterHolderViewModel.Center.GetVec() + RightFilterHolderViewModel.Center.GetVec()) / 2.0;
                Vec size = (LeftFilterHolderViewModel.Dimension + RightFilterHolderViewModel.Dimension) / 2.0;

                _centerFilterHolder.Width = size.X;
                _centerFilterHolder.Height = size.Y;
                
                _filterHolderViewModel.Center = new Point(pos.X, pos.Y);
                this.SetValue(Canvas.LeftProperty, pos.X - _centerFilterHolder.Width / 2.0 - FILTER_SMALL_WIDTH);
                this.SetValue(Canvas.TopProperty, pos.Y - _centerFilterHolder.Height / 2.0);

                _centerFilterHolder.FilterHolderViewModel = _filterHolderViewModel;

                // set incoming and outgoing filter models
                _filterHolderViewModel.AddIncomingFilter(LeftFilterHolderViewModel, FilteringType.Filter);
                _filterHolderViewModel.AddIncomingFilter(RightFilterHolderViewModel, FilteringType.Filter);
                _filterHolderViewModel.CombinedIncomingFilterModels.Add(LeftFilterHolderViewModel);
                _filterHolderViewModel.CombinedIncomingFilterModels.Add(RightFilterHolderViewModel);
                _filterHolderViewModel.IsCombinedFilter = true;

                foreach (var incomingFilterModel in LeftFilterHolderViewModel.GetIncomingFilterModels(FilteringType.Filter).ToArray())
                {
                    _filterHolderViewModel.AddIncomingFilter(incomingFilterModel, FilteringType.Filter);
                    LeftFilterHolderViewModel.RemoveIncomingFilter(incomingFilterModel, FilteringType.Filter);
                }
                foreach (var incomingFilterModel in RightFilterHolderViewModel.GetIncomingFilterModels(FilteringType.Filter).ToArray())
                {
                    _filterHolderViewModel.AddIncomingFilter(incomingFilterModel, FilteringType.Filter);
                    RightFilterHolderViewModel.RemoveIncomingFilter(incomingFilterModel, FilteringType.Filter);
                }

                center.Children.Clear();
                center.Children.Add(_centerFilterHolder);
            }
        }

        public override Vec GetSize()
        {
            return new Vec(_centerFilterHolder.Width, _centerFilterHolder.Height);
        }

        public override Vec GetMinSize()
        {
            return new Vec(_centerFilterHolder.MinWidth, _centerFilterHolder.MinHeight);
        }

        public override void NotifyMove(Pt delta)
        {
            Canvas.SetLeft(this, Canvas.GetLeft(this) + delta.X);
            Canvas.SetTop(this, Canvas.GetTop(this) + delta.Y);

            FilterHolderViewModel.Center = _centerFilterHolder.GetBounds(_inqScene).Center.GetVec().GetWindowsPoint();
            FilterHolderViewModel.Dimension = new Vec(_centerFilterHolder.Width, _centerFilterHolder.Height);
        }

        public override void NotifyScale(Vec delta, Vec offset)
        {
            _centerFilterHolder.Width = _centerFilterHolder.Width * delta.X;
            _centerFilterHolder.Height = _centerFilterHolder.Height * delta.Y;

            FilterHolderViewModel.Center = _centerFilterHolder.GetBounds(_inqScene).Center.GetVec().GetWindowsPoint();
            FilterHolderViewModel.Dimension = new Vec(_centerFilterHolder.Width, _centerFilterHolder.Height);
        }
    }
}
