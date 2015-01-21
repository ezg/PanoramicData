using System.Runtime.CompilerServices;
using InTheHand.Net.Mime;
using PanoramicDataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using PixelLab.Common;
using starPadSDK.AppLib;
using starPadSDK.Inq;
using System.Windows.Controls;
using PanoramicData.controller.data;
using Microsoft.Practices.Prism.Mvvm;

namespace PanoramicData.model.view
{
    public class FilterModel : BindableBase
    {
        private static int _nextColorId = 0;
        /*public static Color[] COLORS = new Color[] {
            Color.FromRgb(246, 83, 20),
            Color.FromRgb(124, 187, 0),
            Color.FromRgb(0, 161, 241),
            Color.FromRgb(255, 187, 0),
            Color.FromRgb(102, 102, 153),
            Color.FromRgb(0, 102, 102),
            Color.FromRgb(51, 153, 255),
            Color.FromRgb(153, 51, 0),
            Color.FromRgb(204, 204, 153),
            Color.FromRgb(102, 102, 102),
            Color.FromRgb(255, 204, 102),
            Color.FromRgb(102, 153, 204),
            Color.FromRgb(102, 51, 102),
            Color.FromRgb(153, 153, 204),
            Color.FromRgb(204, 204, 204),
            Color.FromRgb(1 , 153, 153),
            Color.FromRgb(204, 204, 102),
            Color.FromRgb(204, 102, 0),
            Color.FromRgb(153, 153, 255),
            Color.FromRgb(0, 102, 204),
            Color.FromRgb(153, 204, 204),
            Color.FromRgb(153, 153, 153),
            Color.FromRgb(255, 204, 0),
            Color.FromRgb(0, 153, 153),
            Color.FromRgb(153, 204, 51),
            Color.FromRgb(255, 153, 0),
            Color.FromRgb(153, 153, 102),
            Color.FromRgb(102, 204, 204),
            Color.FromRgb(51, 153, 102),
            Color.FromRgb(204, 204, 51)
        };*/
        public static Color[] COLORS = new Color[] {
            Color.FromRgb(26, 188, 156),
            Color.FromRgb(52, 152, 219),
            Color.FromRgb(52, 73, 94),
            Color.FromRgb(142, 68, 173),
            Color.FromRgb(241, 196, 15),
            Color.FromRgb(231, 76, 60),
            Color.FromRgb(149, 165, 166),
            Color.FromRgb(211, 84, 0),
            Color.FromRgb(189, 195, 199),
            Color.FromRgb(46, 204, 113),
            Color.FromRgb(155, 89, 182),
            Color.FromRgb(22, 160, 133),
            Color.FromRgb(41, 128, 185),
            Color.FromRgb(44, 62, 80),
            Color.FromRgb(230, 126, 34),
            Color.FromRgb(39, 174, 96),
            Color.FromRgb(243, 156, 18),
            Color.FromRgb(192, 57, 43),
            Color.FromRgb(127, 140, 141)
        };

        public delegate void FilterModelUpdatedHandler(object sender, FilterModelUpdatedEventArgs e);
        public event FilterModelUpdatedHandler FilterModelUpdated;

        public bool IsCombinedFilter = false;
        public List<FilterModel> CombinedIncomingFilterModels = new List<FilterModel>();

        public bool ShowSettings = true;

        public FilterModel()
        {
            selectColor();
            _incomingFilterModels.Add(FilteringType.Brush, new List<FilterModel>());
            _incomingFilterModels.Add(FilteringType.Filter, new List<FilterModel>());

            _invertedIncomingFilterModels.Add(FilteringType.Brush, new List<FilterModel>());
            _invertedIncomingFilterModels.Add(FilteringType.Filter, new List<FilterModel>());

            _outgoingFilterModels.Add(FilteringType.Brush, new List<FilterModel>());
            _outgoingFilterModels.Add(FilteringType.Filter, new List<FilterModel>());

            _filterModelLinkType.Add(FilteringType.Brush, FilterModelLinkType.AND);
            _filterModelLinkType.Add(FilteringType.Filter, FilterModelLinkType.AND);  
        }

        private void selectColor()
        {
            if (_nextColorId >= COLORS.Count() - 1)
            {
                _nextColorId = 0;
            }
            Color = COLORS[_nextColorId++];
        }

        public void SwitchToNewColor()
        {
            this.selectColor();
            List<FilterModel> touched = new List<FilterModel>();
            touched.Add(this);
            updateColorForward(this, this.Color, touched);
        }

        private void updateColorForward(FilterModel model, Color color, List<FilterModel> touched)
        {
            touched.Add(model);
            model.Color = color;

            foreach (var outgoingFilterModel in model._outgoingFilterModels[FilteringType.Filter])
            {
                if (!touched.Contains(outgoingFilterModel))
                    updateColorForward(outgoingFilterModel, color, touched);
            }
            foreach (var incomingFilterModel in model._incomingFilterModels[FilteringType.Filter])
            {
                if (!touched.Contains(incomingFilterModel))
                    updateColorForward(incomingFilterModel, color, touched);
            }
        }


        private int _rowCount = 0;

        public int RowCount
        {
            get
            {
                return _rowCount;
            }
            set
            {
                this.SetProperty(ref _rowCount, value);
            }
        }

        private FilterRendererType _filterRendererType = FilterRendererType.Table;
        public FilterRendererType FilterRendererType
        {
            get
            {
                return _filterRendererType;
            }
            set
            {
                this.SetProperty(ref _filterRendererType, value);
                /*if (_filterRendererType != value)
                {
                    _filterRendererType = value;
                    fireFilterUpdated(UpdatedMode.UI, SubUpdatedMode.RenderStyle);
                }*/
            }
        }

        public bool Removed { get; set; }

        public List<OptionCardinalityMapping> OptionCardinalityMappings
        {
            get
            {
                List<OptionCardinalityMapping> mappings = new List<OptionCardinalityMapping>();
                if (FilterRendererType == FilterRendererType.Histogram)
                {
                    mappings.Add(new OptionCardinalityMapping
                    {
                        Option = Option.Label,
                        OptionCardinality = OptionCardinality.Many
                    });
                    mappings.Add(new OptionCardinalityMapping
                    {
                        Option = Option.Y,
                        OptionCardinality = OptionCardinality.One
                    });
                    mappings.Add(new OptionCardinalityMapping
                    {
                        Option = Option.ColorBy,
                        OptionCardinality = OptionCardinality.One
                    });
                }
                else if (FilterRendererType == FilterRendererType.Pie)
                {
                    mappings.Add(new OptionCardinalityMapping
                    {
                        Option = Option.Label,
                        OptionCardinality = OptionCardinality.Many
                    });
                    mappings.Add(new OptionCardinalityMapping
                    {
                        Option = Option.SegmentSize,
                        OptionCardinality = OptionCardinality.One
                    });
                }
                else if (FilterRendererType == FilterRendererType.Map)
                {
                    mappings.Add(new OptionCardinalityMapping
                    {
                        Option = Option.Label,
                        OptionCardinality = OptionCardinality.Many
                    });
                    mappings.Add(new OptionCardinalityMapping
                    {
                        Option = Option.Location,
                        OptionCardinality = OptionCardinality.One
                    }); 
                    mappings.Add(new OptionCardinalityMapping
                    {
                        Option = Option.ColorBy,
                        OptionCardinality = OptionCardinality.One
                    });
                }
                else if (FilterRendererType == FilterRendererType.Plot)
                {
                    mappings.Add(new OptionCardinalityMapping
                    {
                        Option = Option.X,
                        OptionCardinality = OptionCardinality.One
                    });
                    mappings.Add(new OptionCardinalityMapping
                    {
                        Option = Option.Y,
                        OptionCardinality = OptionCardinality.One
                    });
                    mappings.Add(new OptionCardinalityMapping
                    {
                        Option = Option.ColorBy,
                        OptionCardinality = OptionCardinality.One
                    });
                }

                return mappings;
            }
        }

        private bool _temporary = false;
        public bool Temporary
        {
            get
            {
                return _temporary;
            }
            set
            {
                this.SetProperty(ref _temporary, value);
                /*if (_temporary != value)
                {
                    _temporary = value;
                    fireFilterUpdated(UpdatedMode.UI);
                }*/
            }
        }

        private string _label = "";
        public string Label
        {
            get
            {
                return _label;
            }
            set
            {
                this.SetProperty(ref _label, value);
                /*if (_label != value)
                {
                    _label = value;
                    fireFilterUpdated(UpdatedMode.UI);
                }*/
            }
        }


        private bool _hasFilteredItems = false;
        public bool HasFilteredItems
        {
            get
            {
                return _hasFilteredItems;
            }
            set
            {
                this.SetProperty(ref _hasFilteredItems, value);
                /*if (_hasFilteredItems != value)
                {
                    _hasFilteredItems = value;
                    fireFilterUpdated(UpdatedMode.FilteredItemsStatus);
                }*/
            }
        }

        private bool _selected = true;
        public bool Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                this.SetProperty(ref _selected, value);
                /*if (_selected != value)
                {
                    _selected = value;
                    fireFilterUpdated(UpdatedMode.UI);
                }*/
            }
        }

        private int _margin = 0;
        public int Margin
        {
            get
            {
                return _margin;
            }
            set
            {
                this.SetProperty(ref _margin, value);
                /*if (_margin != value)
                {
                    _margin = value;
                    OnPropertyChanged("Margin");
                }*/
            }
        }

        public Image FrozenImage { get; set; }

        private SolidColorBrush _brush = null;
        public SolidColorBrush Brush
        {
            get
            {
                return _brush;
            }
            set
            {
                this.SetProperty(ref _brush, value);
                /*if (_brush != value)
                {
                    _brush = value;
                    OnPropertyChanged("Brush");
                }*/
            }
        }

        private Color _color = Color.FromArgb(0xff, 0x00, 0x00, 0x00);
        public Color Color
        {
            get
            {
                return _color;
            }
            set
            {
                this.SetProperty(ref _color, value);
                Brush = new SolidColorBrush(_color);
                FaintBrush = new SolidColorBrush(Color.FromArgb(90, Color.R, Color.G, Color.B));
                /*if (_color != value)
                {
                    _color = value;
                    Brush = new SolidColorBrush(_color);
                    FaintBrush = new SolidColorBrush(Color.FromArgb(90, Color.R, Color.G, Color.B));
                    OnPropertyChanged("Color");
                    fireFilterUpdated(UpdatedMode.UI, SubUpdatedMode.Color);
                }*/
            }
        }

        private SolidColorBrush _faintBrush = null;
        public SolidColorBrush FaintBrush
        {
            get
            {
                return _faintBrush;
            }
            set
            {
                this.SetProperty(ref _faintBrush, value);
                /*if (_faintBrush != value)
                {
                    _faintBrush = value;
                    OnPropertyChanged("FaintBrush");
                }*/
            }
        }

        protected StroqCollection _nameStroqs = null;
        public StroqCollection NameStroqs
        {
            get
            {
                return _nameStroqs;
            }
            set
            {
                this.SetProperty(ref _nameStroqs, value);
                /*if (_nameStroqs != value)
                {
                    _nameStroqs = value;
                    OnPropertyChanged("NameStroqs");
                }*/
            }
        }

        protected string _name = "";
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                if (_name != value)
                {
                    TableModel.UpdateNamedFilterModel(this);
                }
                this.SetProperty(ref _name, value);
                /*if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged("Name");
                    TableModel.UpdateNamedFilterModel(this);
                    fireFilterUpdated(UpdatedMode.FilteredItemsChange);
                }*/
            }
        }

        private Dictionary<FilteringType, FilterModelLinkType> _filterModelLinkType = new Dictionary<FilteringType, FilterModelLinkType>();

        public FilterModelLinkType GetFilterModelLinkType(FilteringType filteringType)
        {
            return _filterModelLinkType[filteringType];
        }
        
        public void SetFilterModelLinkType(FilteringType filteringType, FilterModelLinkType linkType)
        {
            if (_filterModelLinkType[filteringType] != linkType)
            {
                _filterModelLinkType[filteringType] = linkType;
                fireFilterUpdated(UpdatedMode.Structure);
            }
        }

        private List<Pivot> _pivots = new List<Pivot>();
        public List<Pivot> Pivots
        {
            get
            {
                return _pivots;
            }
        }

        public void AddPivot(Pivot pivot, object sender)
        {
            _pivots.Add(pivot);
            pivot.PropertyChanged += pivot_PropertyChanged;
            fireFilterUpdated(UpdatedMode.FilteredItemsChange, SubUpdatedMode.None, sender);
        }

        void pivot_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            PanoramicDataColumnDescriptor cd = (sender as Pivot).ColumnDescriptor;
            if ((sender as Pivot).Selected)
            {
                if (!this.ColumnDescriptors.Contains(cd))
                {
                    this.AddColumnDescriptor(cd);
                }
            }
            else
            {
                if (this.ColumnDescriptors.Contains(cd))
                {
                    this.RemoveColumnDescriptor(cd);
                }
            }
            fireFilterUpdated(UpdatedMode.FilteredItemsChange);
        }

        private FilterModel _diffSourceFilterModel = null;
        public FilterModel DiffSourceFilterModel
        {
            get
            {
                return _diffSourceFilterModel;
            }
            set
            {
                if (_diffSourceFilterModel != value)
                {
                    _diffSourceFilterModel = value; 
                    fireFilterUpdated(UpdatedMode.Structure);
                }
            }
        }

        private TableModel _tableModel = null;
        public TableModel TableModel
        {
            get
            {
                return _tableModel;
            }
            set
            {
                if (_tableModel != value)
                {
                    _tableModel = value;
                }
            }
        }
        
        private Dictionary<Option, List<PanoramicDataColumnDescriptor>> _optionColumnDescriptors = new Dictionary<Option, List<PanoramicDataColumnDescriptor>>();
        public void AddOptionColumnDescriptor(Option option, PanoramicDataColumnDescriptor columnDescriptor)
        {
            if (!_optionColumnDescriptors.ContainsKey(option))
            {
                _optionColumnDescriptors.Add(option, new List<PanoramicDataColumnDescriptor>());
            }

            if (!_optionColumnDescriptors[option].ContainsByReference(columnDescriptor))
            {
                _optionColumnDescriptors[option].Add(columnDescriptor);
                if (!ColumnDescriptors.ContainsByReference(columnDescriptor))
                {
                    AddColumnDescriptor(columnDescriptor);
                }
                fireFilterUpdated(UpdatedMode.Structure);
            }
        }

        public List<PanoramicDataColumnDescriptor> GetColumnDescriptorsForOption(Option option)
        {
            if (!_optionColumnDescriptors.ContainsKey(option))
            {
                return new List<PanoramicDataColumnDescriptor>();
            }
            else
            {
                return _optionColumnDescriptors[option];
            }
        }

        public List<Option> GetOptionsForColumnDescriptors(PanoramicDataColumnDescriptor columnDescriptor)
        {
            List<Option> options = new List<Option>();
            foreach (var option in _optionColumnDescriptors.Keys)
            {
                if (_optionColumnDescriptors[option].Contains(columnDescriptor))
                {
                    options.Add(option);
                }
            }
            return options;
        }

        public void RemoveOptionColumnDescriptor(Option option, PanoramicDataColumnDescriptor columnDescriptor)
        {
            if (_optionColumnDescriptors.ContainsKey(option))
            {
                if (_optionColumnDescriptors[option].ContainsByReference(columnDescriptor))
                {
                    _optionColumnDescriptors[option].RemoveByReference(columnDescriptor);
                    fireFilterUpdated(UpdatedMode.Structure);
                }
                RemoveColumnDescriptor(columnDescriptor, false);
            }
        }

        private List<PanoramicDataColumnDescriptor> _columnDescriptors = new List<PanoramicDataColumnDescriptor>();
        public List<PanoramicDataColumnDescriptor> ColumnDescriptors
        {
            get
            {
                return _columnDescriptors;
            }
        }

        public void AddColumnDescriptor(PanoramicDataColumnDescriptor columnDescriptor)
        {
            _tableModel.AddColumnDescriptor((PanoramicDataColumnDescriptor)columnDescriptor.SimpleClone());
            _columnDescriptors.Add(columnDescriptor);

            columnDescriptor.PropertyChanged += ColumnDescriptor_PropertyChanged;
            columnDescriptor.PanoramicDataColumnDescriptorUpdated += ColumnDescriptor_PanoramicDataColumnDescriptorUpdated;
            fireFilterUpdated(UpdatedMode.Structure);
        }

        public void AddColumnDescriptors(List<PanoramicDataColumnDescriptor> columnDescriptors)
        {
            foreach (var columnDescriptor in columnDescriptors)
            {
                _tableModel.AddColumnDescriptor((PanoramicDataColumnDescriptor)columnDescriptor.Clone());
                _columnDescriptors.Add(columnDescriptor);
                columnDescriptor.PropertyChanged += ColumnDescriptor_PropertyChanged;
            }
            fireFilterUpdated(UpdatedMode.Structure);
        }

        public void ClearAndAddColumnDescriptors(List<PanoramicDataColumnDescriptor> columnDescriptors)
        {
            foreach (var columnDescriptor in _columnDescriptors)
            {
                columnDescriptor.PropertyChanged -= ColumnDescriptor_PropertyChanged;
                columnDescriptor.PanoramicDataColumnDescriptorUpdated -= ColumnDescriptor_PanoramicDataColumnDescriptorUpdated;
            }
            _columnDescriptors.Clear();
            _optionColumnDescriptors.Clear();
            foreach (var columnDescriptor in columnDescriptors)
            {
                _tableModel.AddColumnDescriptor(columnDescriptor);
                _columnDescriptors.Add(columnDescriptor);
                columnDescriptor.PropertyChanged += ColumnDescriptor_PropertyChanged;
                columnDescriptor.PanoramicDataColumnDescriptorUpdated += ColumnDescriptor_PanoramicDataColumnDescriptorUpdated;
            }
            fireFilterUpdated(UpdatedMode.Structure);
        }

        public void RemoveColumnDescriptor(PanoramicDataColumnDescriptor columnDescriptor, bool alsoRemoveFromOptions = true)
        {
            if (alsoRemoveFromOptions)
            {
                foreach (var option in GetOptionsForColumnDescriptors(columnDescriptor))
                {
                    RemoveOptionColumnDescriptor(option, columnDescriptor);
                }
            }
            if (columnDescriptor.IsBinned &&
                _columnDescriptors.Count(cd => cd.MatchSimple(columnDescriptor) && cd.IsBinned) == 1)
            {
                _columnDescriptors.Where(cd => cd.MatchSimple(columnDescriptor) && cd.AggregateFunction == AggregateFunction.Bin).ForEach(cd => cd.AggregateFunction = AggregateFunction.None);
            }
            _columnDescriptors.RemoveByReference(columnDescriptor);

            columnDescriptor.PropertyChanged -= ColumnDescriptor_PropertyChanged;
            columnDescriptor.PanoramicDataColumnDescriptorUpdated -= ColumnDescriptor_PanoramicDataColumnDescriptorUpdated;
            fireFilterUpdated(UpdatedMode.Structure);
        }

        public void RemoveColumnDescriptors(List<PanoramicDataColumnDescriptor> columnDescriptors)
        {
            foreach (var columnDescriptor in columnDescriptors)
            {
                _columnDescriptors.RemoveByReference(columnDescriptor);
                foreach (var option in GetOptionsForColumnDescriptors(columnDescriptor))
                {
                    RemoveOptionColumnDescriptor(option, columnDescriptor);
                }

                columnDescriptor.PropertyChanged -= ColumnDescriptor_PropertyChanged;
                columnDescriptor.PanoramicDataColumnDescriptorUpdated -= ColumnDescriptor_PanoramicDataColumnDescriptorUpdated;
            }
            fireFilterUpdated(UpdatedMode.Structure);
        }

        void ColumnDescriptor_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (GetColumnDescriptorsForOption(Option.X).Count > 0 &&
                GetColumnDescriptorsForOption(Option.Y).Count > 0 &&
                GetColumnDescriptorsForOption(Option.GroupBy).Count == 0)
            {
                PanoramicDataColumnDescriptor columnDescriptor = (PanoramicDataColumnDescriptor) sender;
                if (columnDescriptor.AggregateFunction != AggregateFunction.None &&
                    columnDescriptor.AggregateFunctionSetByUser)
                {
                    if (GetColumnDescriptorsForOption(Option.X).ContainsByReference(columnDescriptor))
                    {
                        PanoramicDataColumnDescriptor clone = GetColumnDescriptorsForOption(Option.Y)[0].SimpleClone();
                        clone.IsGrouped = true;
                        clone.IsHidden = true;
                        this.AddOptionColumnDescriptor(Option.GroupBy, clone);
                    }
                    else if (GetColumnDescriptorsForOption(Option.Y).ContainsByReference(columnDescriptor))
                    {
                        PanoramicDataColumnDescriptor clone = GetColumnDescriptorsForOption(Option.X)[0].SimpleClone();
                        clone.IsGrouped = true;
                        clone.IsHidden = true;
                        this.AddOptionColumnDescriptor(Option.GroupBy, clone);
                    }
                }
            }
            if (GetColumnDescriptorsForOption(Option.X).Count > 0 &&
                GetColumnDescriptorsForOption(Option.Y).Count > 0 &&
                GetColumnDescriptorsForOption(Option.GroupBy).Count(cd => cd.IsHidden) > 0)
            {
                if (GetColumnDescriptorsForOption(Option.X)[0].AggregateFunction == AggregateFunction.None &&
                    GetColumnDescriptorsForOption(Option.Y)[0].AggregateFunction == AggregateFunction.None)
                {
                    this.GetColumnDescriptorsForOption(Option.GroupBy).Where(cd => cd.IsHidden).ToList().ForEach(cd => this.RemoveOptionColumnDescriptor(Option.GroupBy, cd));
                }
                if (GetColumnDescriptorsForOption(Option.X)[0].AggregateFunction != AggregateFunction.None &&
                    GetColumnDescriptorsForOption(Option.Y)[0].AggregateFunction != AggregateFunction.None)
                {
                    this.GetColumnDescriptorsForOption(Option.GroupBy).Where(cd => cd.IsHidden).ToList().ForEach(cd => this.RemoveOptionColumnDescriptor(Option.GroupBy, cd));
                }
            }

            fireFilterUpdated(UpdatedMode.Structure);
        }

        void ColumnDescriptor_PanoramicDataColumnDescriptorUpdated(object sender, PanoramicDataColumnDescriptorUpdatedEventArgs e)
        {
            fireFilterUpdated(UpdatedMode.FilteredItemsChange);
        }

        private Dictionary<FilteringType, List<FilterModel>> _outgoingFilterModels = new Dictionary<FilteringType, List<FilterModel>>();
        public List<FilterModel> GetOutgoingFilterModels(FilteringType filteringType)
        {
            return _outgoingFilterModels[filteringType];
        }

        public void AddOutgoingFilter(FilterModel filterModel, FilteringType filteringType)
        {
            if (!_outgoingFilterModels[filteringType].Contains((filterModel)))
            {
                _outgoingFilterModels[filteringType].Add(filterModel);
            }
            if (filteringType == FilteringType.Filter)
            {
                List<FilterModel> touched = new List<FilterModel>();
                touched.Add(this);
                updateColorForward(filterModel, this.Color, touched);
            }
            fireFilterUpdated(UpdatedMode.Incoming);
        }

        public void RemoveOutgoingFilter(FilterModel filterModel, FilteringType filteringType)
        {
            _outgoingFilterModels[filteringType].Remove(filterModel);
            fireFilterUpdated(UpdatedMode.Incoming);

            if (!Removed)
            {
                this.selectColor();
                List<FilterModel> touched = new List<FilterModel>();
                touched.Add(this);
                updateColorForward(this, this.Color, touched);
            }
        }

        private Dictionary<FilteringType, List<FilterModel>> _incomingFilterModels = new Dictionary<FilteringType, List<FilterModel>>();
        public List<FilterModel> GetIncomingFilterModels(params FilteringType[] filteringTypes)
        {
            List<FilterModel> filterModels = new List<FilterModel>();
            foreach (var filteringType in _incomingFilterModels.Keys)
            {
                if (filteringTypes.Contains(filteringType))
                {
                    filterModels = filterModels.Union(_incomingFilterModels[filteringType]).ToList();
                }
            }
            return filterModels;
        }

        public void AddIncomingFilter(FilterModel filterModel, FilteringType filteringType, bool addOutgoingFilterModel = true)
        {
            // check if we need to flip the link
            if (this.GetOutgoingFilterModels(FilteringType.Brush).Contains(filterModel))
            {
                filterModel.RemoveIncomingFilter(this, FilteringType.Brush); ;
                this.AddIncomingFilter(filterModel, FilteringType.Brush, true);
                return;
            }
            if (this.GetOutgoingFilterModels(FilteringType.Filter).Contains(filterModel))
            {
                filterModel.RemoveIncomingFilter(this, FilteringType.Filter); ;
                this.AddIncomingFilter(filterModel, FilteringType.Filter, true);
                return;
            }

            List<FilterModel> recModels = new List<FilterModel>();
            findAllFilterModelsRecursive(filterModel, recModels);
            if (recModels.Contains(this) || this._incomingFilterModels[filteringType].Contains(filterModel))
            {
                return;
            }

            if (!_incomingFilterModels[filteringType].Contains((filterModel)))
            {
                _incomingFilterModels[filteringType].Add(filterModel);
            }

            if (filteringType == FilteringType.Filter)
            {
                // add a series column descriptor if we link filtersa
                if (_incomingFilterModels.Count == 1 && this.GetColumnDescriptorsForOption(Option.ColorBy).Count == 0 &&
                    _incomingFilterModels[filteringType][0].ColumnDescriptors.Count(cd => cd.IsAnyGroupingOperationApplied()) == 1 &&
                    this.FilterRendererType != FilterRendererType.Pie &&
                    this.FilterRendererType != FilterRendererType.Table &&
                    this.FilterRendererType != FilterRendererType.Slider &&
                    addOutgoingFilterModel &&
                    filterModel.FilterRendererType == FilterRendererType.Pie)
                {
                    var grouppedCd =
                        _incomingFilterModels[filteringType][0].ColumnDescriptors.First(cd => cd.IsAnyGroupingOperationApplied());
                    this.AddOptionColumnDescriptor(Option.ColorBy, (PanoramicDataColumnDescriptor) grouppedCd.Clone());
                }
            }

            filterModel.FilterModelUpdated += IncomingFilter_FilterUpdated;
            if (addOutgoingFilterModel)
            {
                filterModel.AddOutgoingFilter(this, filteringType);
            }
            fireFilterUpdated(UpdatedMode.Incoming);
        }

        public void RemoveIncomingFilter(FilterModel filterModel, FilteringType filteringType)
        {
            _incomingFilterModels[filteringType].Remove(filterModel);

            filterModel.FilterModelUpdated -= IncomingFilter_FilterUpdated;
            filterModel.RemoveOutgoingFilter(this, filteringType);
            fireFilterUpdated(UpdatedMode.Incoming);
        }

        private void findAllFilterModelsRecursive(FilterModel filterModel, List<FilterModel> chain)
        {
            chain.Add(filterModel);
            foreach (var fm in filterModel.GetIncomingFilterModels(FilteringType.Brush, FilteringType.Filter))
            {
                findAllFilterModelsRecursive(fm, chain);
            }
        }

        void IncomingFilter_FilterUpdated(object sender, FilterModelUpdatedEventArgs e)
        {
            if (e.Mode != UpdatedMode.UI || 
                (GetIncomingFilterModels(FilteringType.Brush).Contains(sender as FilterModel) && e.SubMode == SubUpdatedMode.Color))
            {
                fireFilterUpdated(UpdatedMode.Incoming);
            }
        }

        private Dictionary<FilteringType, List<FilterModel>> _invertedIncomingFilterModels = new Dictionary<FilteringType, List<FilterModel>>();
        public List<FilterModel> GetInvertedIncomingFilterModels(FilteringType filteringType)
        {
            return _invertedIncomingFilterModels[filteringType];
        }

        public void InvertIncomingFilterModel(FilterModel filterModel, FilteringType filteringType)
        {
            if (_invertedIncomingFilterModels[filteringType].Contains(filterModel))
            {
                _invertedIncomingFilterModels[filteringType].Remove(filterModel);
            }
            else
            {
                _invertedIncomingFilterModels[filteringType].Add(filterModel);
            }

            fireFilterUpdated(UpdatedMode.Structure);
        }

        private List<FilteredItem> _embeddedFilterItems = new List<FilteredItem>();
        public List<FilteredItem> EmbeddedFilteredItems
        {
            get
            {
                return _embeddedFilterItems;
            }
        }

        public void ClearEmbeddedFilteredItems()
        {
            _embeddedFilterItems.Clear();
            fireFilterUpdated(UpdatedMode.Incoming);
        }

        public void AddEmbeddedFilteredItems(List<FilteredItem> filteredItems)
        {
            _embeddedFilterItems.AddRange(filteredItems);
            fireFilterUpdated(UpdatedMode.Incoming);
        }

        public void AddEmbeddedFilteredItem(FilteredItem filteredItem)
        {
            _embeddedFilterItems.Add(filteredItem);
            fireFilterUpdated(UpdatedMode.Incoming);
        }

        public void RemoveEmbeddedFilteredItem(FilteredItem filteredItem)
        {
            _embeddedFilterItems.Remove(filteredItem);
            fireFilterUpdated(UpdatedMode.Incoming);
        }

        private List<FilteredItem> _filteredItems = new List<FilteredItem>();
        public List<FilteredItem> FilteredItems
        {
            get
            {
                return _filteredItems;
            }
        }

        public void ClearFilteredItems()
        {
            _filteredItems.Clear();
            fireFilterUpdated(UpdatedMode.FilteredItemsChange);
        }

        public void AddFilteredItems(List<FilteredItem> filteredItems, object sender)
        {
            _filteredItems.AddRange(filteredItems);
            fireFilterUpdated(UpdatedMode.FilteredItemsChange, SubUpdatedMode.None, sender);
        }

        public void AddFilteredItem(FilteredItem filteredItem, object sender)
        {
            _filteredItems.Add(filteredItem);
            fireFilterUpdated(UpdatedMode.FilteredItemsChange, SubUpdatedMode.None, sender);
        }

        public void RemoveFilteredItem(FilteredItem filteredItem, object sender)
        {
            _filteredItems.Remove(filteredItem);
            fireFilterUpdated(UpdatedMode.FilteredItemsChange, SubUpdatedMode.None, sender);
        }

        public void RemoveFilteredItems(List<FilteredItem> filteredItems, object sender)
        {
            foreach (var filteredItem in filteredItems)
            {
                _filteredItems.Remove(filteredItem);
            }
            if (filteredItems.Count > 0)
            {
                fireFilterUpdated(UpdatedMode.FilteredItemsChange, SubUpdatedMode.None, sender);
            }
        }
    
        public void FireFilterUpdated(UpdatedMode mode, SubUpdatedMode subMode = SubUpdatedMode.None, object sender = null)
        {
            fireFilterUpdated(mode, subMode, sender);
        }

        protected void fireFilterUpdated(UpdatedMode mode, SubUpdatedMode subMode = SubUpdatedMode.None, object sender = null)
        {
            if (mode != UpdatedMode.UI)
            {
                if (FilterRendererType == FilterRendererType.Pivot)
                {
                    Label = "Pivot Operator";
                }
                else if (FilterRendererType == FilterRendererType.Frozen)
                {
                    Label = "Snapshot";
                }
                else 
                {
                    List<string> l = new List<string>();
                    foreach (var cd in _columnDescriptors.ToArray())
                    {
                        string mainLabel = "";
                        string subLabel = "";
                        cd.GetLabels(out mainLabel, out subLabel, false);
                        l.Add(mainLabel);
                    }
                    Label = (Name != "" ? (Name + " : ") : "") + string.Join(", ", l.Distinct());
                }
            }
            if (FilterModelUpdated != null)
            {
                FilterModelUpdated(this, new FilterModelUpdatedEventArgs(new List<FilterModel>(new FilterModel[] { this }), mode, subMode, sender));
            }
        }

        public bool IsDataTypeOfPanoramicDataColumnDescriptorNumeric(PanoramicDataColumnDescriptor columnDescriptor, bool considerGrouping)
        {
            string dt = GetDataTypeOfPanoramicDataColumnDescriptor(columnDescriptor, considerGrouping);

            if (dt == AttributeDataTypeConstants.FLOAT ||
                dt == AttributeDataTypeConstants.INT)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public string GetDataTypeOfPanoramicDataColumnDescriptor(PanoramicDataColumnDescriptor columnDescriptor, bool considerGrouping)
        {
            if (!considerGrouping)
            {
                return columnDescriptor.DataType;
            }
            else
            {
                if (columnDescriptor.IsBinned)
                {
                    return AttributeDataTypeConstants.NVARCHAR;
                }

                bool isGroupingApplied = this.ColumnDescriptors.Where(cd => cd.IsAnyGroupingOperationApplied()).Count() > 0;

                if (!isGroupingApplied)
                {
                    if (this.ColumnDescriptors.Count(cd => cd.AggregateFunction != AggregateFunction.None) > 0)
                    {
                        if (columnDescriptor.AggregateFunction == AggregateFunction.Avg ||
                            columnDescriptor.AggregateFunction == AggregateFunction.Sum ||
                            columnDescriptor.AggregateFunction == AggregateFunction.Max ||
                            columnDescriptor.AggregateFunction == AggregateFunction.Min)
                        {
                            if (columnDescriptor.DataType == AttributeDataTypeConstants.TIME)
                            {
                                return AttributeDataTypeConstants.TIME;
                            }
                            if (columnDescriptor.DataType == AttributeDataTypeConstants.NVARCHAR)
                            {
                                return AttributeDataTypeConstants.NVARCHAR;
                            }
                            if (columnDescriptor.DataType == AttributeDataTypeConstants.BIT)
                            {
                                return AttributeDataTypeConstants.BIT;
                            }
                            return AttributeDataTypeConstants.FLOAT;
                        }
                        else if (columnDescriptor.AggregateFunction == AggregateFunction.Count)
                        {
                            return AttributeDataTypeConstants.INT;
                        }
                        return AttributeDataTypeConstants.NVARCHAR;
                    }
                    else
                    {
                        return columnDescriptor.DataType;
                    }
                }
                else
                {
                    if (columnDescriptor.AggregateFunction == AggregateFunction.None &&
                        this.ColumnDescriptors.Count(
                            cd => cd.IsAnyGroupingOperationApplied() && cd.MatchSimple(columnDescriptor)) > 0)
                    {
                        return columnDescriptor.DataType;
                    }

                    if (columnDescriptor.AggregateFunction == AggregateFunction.Avg ||
                        columnDescriptor.AggregateFunction == AggregateFunction.Sum ||
                        columnDescriptor.AggregateFunction == AggregateFunction.Max ||
                        columnDescriptor.AggregateFunction == AggregateFunction.Min)
                    {
                        if (columnDescriptor.DataType == AttributeDataTypeConstants.NVARCHAR)
                        {
                            return AttributeDataTypeConstants.NVARCHAR;
                        }
                        if (columnDescriptor.DataType == AttributeDataTypeConstants.TIME)
                        {
                            return AttributeDataTypeConstants.TIME;
                        } 
                        if (columnDescriptor.DataType == AttributeDataTypeConstants.BIT)
                        {
                            return AttributeDataTypeConstants.BIT;
                        }
                        return AttributeDataTypeConstants.FLOAT;
                    }
                    else if (columnDescriptor.AggregateFunction == AggregateFunction.Count)
                    {
                        return AttributeDataTypeConstants.INT;
                    }
                    else
                    {
                        return AttributeDataTypeConstants.NVARCHAR;
                    }
                }
            }
        }

        public void AddGrouping(PanoramicDataColumnDescriptor columnDescriptor)
        {
            this.AddOptionColumnDescriptor(Option.GroupBy, columnDescriptor);
            this.GetColumnDescriptorsForOption(Option.GroupBy).Where(cd => cd.IsHidden).ToList().ForEach(cd => this.RemoveOptionColumnDescriptor(Option.GroupBy, cd));
            this.GetColumnDescriptorsForOption(Option.ColorBy).ForEach(cd => cd.IsGrouped = true);

            this.GetColumnDescriptorsForOption(Option.X).Where(cd =>
                (cd.DataType == AttributeDataTypeConstants.INT ||
                cd.DataType == AttributeDataTypeConstants.FLOAT ||
                cd.DataType == AttributeDataTypeConstants.BIT ||
                cd.DataType == AttributeDataTypeConstants.TIME) &&
                cd.AggregateFunctionSetByUser == false).
                ForEach(cd =>
                {
                    if (cd.MatchSimple(columnDescriptor) && columnDescriptor.IsBinned)
                    {
                        cd.AggregateFunction = AggregateFunction.Bin;
                        cd.AggregateFunctionSetByUser = false;
                    }
                    else
                    {
                        cd.AggregateFunction = AggregateFunction.Avg;
                        cd.AggregateFunctionSetByUser = false;
                    }
                });

            this.GetColumnDescriptorsForOption(Option.Y).Where(cd =>
                (cd.DataType == AttributeDataTypeConstants.INT ||
                cd.DataType == AttributeDataTypeConstants.FLOAT ||
                cd.DataType == AttributeDataTypeConstants.BIT ||
                cd.DataType == AttributeDataTypeConstants.TIME) &&
                cd.AggregateFunctionSetByUser == false).
                ForEach(cd =>
                {
                    if (cd.MatchSimple(columnDescriptor) && columnDescriptor.IsBinned)
                    {
                        cd.AggregateFunction = AggregateFunction.Bin;
                        cd.AggregateFunctionSetByUser = false;
                    }
                    else
                    {
                        cd.AggregateFunction = AggregateFunction.Avg;
                        cd.AggregateFunctionSetByUser = false;
                    }
                });
        }

        public void RemoveGrouping(PanoramicDataColumnDescriptor columnDescriptor)
        {
            this.RemoveOptionColumnDescriptor(Option.GroupBy, columnDescriptor);

            if (this.GetColumnDescriptorsForOption(Option.GroupBy).Count == 0)
            {
                this.GetColumnDescriptorsForOption(Option.X).Where(cd =>
                    (cd.DataType == AttributeDataTypeConstants.INT ||
                        cd.DataType == AttributeDataTypeConstants.FLOAT ||
                        cd.DataType == AttributeDataTypeConstants.BIT ||
                        cd.DataType == AttributeDataTypeConstants.TIME) &&
                    cd.AggregateFunctionSetByUser == false).
                    ForEach(cd =>
                    {
                        cd.AggregateFunction = AggregateFunction.None;
                        cd.AggregateFunctionSetByUser = false;
                    });


                this.GetColumnDescriptorsForOption(Option.Y).Where(cd =>
                    (cd.DataType == AttributeDataTypeConstants.INT ||
                        cd.DataType == AttributeDataTypeConstants.FLOAT ||
                        cd.DataType == AttributeDataTypeConstants.BIT ||
                        cd.DataType == AttributeDataTypeConstants.TIME) &&
                    cd.AggregateFunctionSetByUser == false).
                    ForEach(cd =>
                    {
                        cd.AggregateFunction = AggregateFunction.None;
                        cd.AggregateFunctionSetByUser = false;
                    });

                this.GetColumnDescriptorsForOption(Option.ColorBy).ForEach(cd => cd.IsGrouped = false);
            }
        }
    }

    public class Pivot : BindableBase
    {
        private string _label = "";
        public string Label
        {
            get
            {
                return _label;
            }
            set
            {
                this.SetProperty(ref _label, value);
            }
        }

        private bool _selected = false;
        public bool Selected
        {
            get
            {
                return _selected;
            }
            set
            {
                this.SetProperty(ref _selected, value);
            }
        }

        private PanoramicDataColumnDescriptor _columnDescriptor = null;
        public PanoramicDataColumnDescriptor ColumnDescriptor
        {
            get
            {
                return _columnDescriptor;
            }
            set
            {
                this.SetProperty(ref _columnDescriptor, value);
            }
        }
    }

    public class FilterModelUpdatedEventArgs : EventArgs
    {
        public object Sender { get; set; }
        public UpdatedMode Mode { get; set; }
        public SubUpdatedMode SubMode { get; set; }
        public List<FilterModel> Chain { get; set; }

        public FilterModelUpdatedEventArgs(List<FilterModel> chain, UpdatedMode mode, SubUpdatedMode subMode = SubUpdatedMode.None, object sender = null)
        {
            Mode = mode;
            SubMode = subMode;
            Chain = chain;
            Sender = sender;
        }
    }

    public class OptionCardinalityMapping
    {
        public Option Option { get; set; }
        public OptionCardinality OptionCardinality { get; set; }
    }

    public enum OptionCardinality
    {
        One, 
        Zero,
        Many
    }

    public enum Option
    {
        ColorBy,
        GroupBy,
        X,
        Y,
        Location,
        Label,
        SegmentSize
    }

    public enum FilterModelLinkType { AND, OR }
}
