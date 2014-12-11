using System;
using System.Collections.Generic;
using System.Linq;
using PanoramicDataModel;
using starPadSDK.Geom;
using starPadSDK.Inq;
using starPadSDK.MathExpr;
using PanoramicData.utils.inq;

namespace PanoramicData.model.view
{
    public class CalculatedColumnDescriptorInfo : ViewModelBase, LabelProvider, LabelConsumer
    {
        private Dictionary<Label, PanoramicDataColumnDescriptor> _providedLabels = null;

        public CalculatedColumnDescriptorInfo()
        {
            _providedLabels = new Dictionary<Label, PanoramicDataColumnDescriptor>();
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
                    OnPropertyChanged("TableModel");
                }
            }
        }

        private Guid _guid = Guid.NewGuid();
        public Guid Guid
        {
            get
            {
                return _guid;
            }
            set
            {
                if (_guid != value)
                {
                    _guid = value;
                    OnPropertyChanged("Guid");
                }
            }
        }

        private Expr _expr = null;
        public Expr Expr
        {
            get
            {
                return _expr;
            }
            set
            {
                if (_expr != value)
                {
                    _expr = value;
                    OnPropertyChanged("Expr");
                }
            }
        }

        private StroqCollection _stroqs = new StroqCollection();
        public StroqCollection Stroqs
        {
            get
            {
                return _stroqs;
            }
            set
            {
                if (_stroqs != value)
                {
                    _stroqs = value;
                    OnPropertyChanged("Stroqs");
                }
            }
        }

        private string _name = null;
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
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }
        }

        public Dictionary<Label, PanoramicDataColumnDescriptor> ProvidedLabels
        {
            get
            {
                return _providedLabels;
            }
        }

        private List<PanoramicDataColumnDescriptor> _columnDescriptors = new List<PanoramicDataColumnDescriptor>();
        public IList<PanoramicDataColumnDescriptor> ColumnDescriptors
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
            OnPropertyChanged("ColumnDescriptors");
        }

        public void AddColumnDescriptors(List<PanoramicDataColumnDescriptor> columnDescriptors)
        {
            foreach (var columnDescriptor in columnDescriptors)
            {
                _tableModel.AddColumnDescriptor((PanoramicDataColumnDescriptor)columnDescriptor.Clone());
                _columnDescriptors.Add(columnDescriptor);
                columnDescriptor.PropertyChanged += ColumnDescriptor_PropertyChanged;
            }
            OnPropertyChanged("ColumnDescriptors");
        }
        public void RemoveColumnDescriptor(PanoramicDataColumnDescriptor columnDescriptor)
        {
            _columnDescriptors.Remove(columnDescriptor);

            columnDescriptor.PropertyChanged -= ColumnDescriptor_PropertyChanged;
            columnDescriptor.PanoramicDataColumnDescriptorUpdated -= ColumnDescriptor_PanoramicDataColumnDescriptorUpdated;
            OnPropertyChanged("ColumnDescriptors");
        }

        void ColumnDescriptor_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged("ColumnDescriptors");
        }

        void ColumnDescriptor_PanoramicDataColumnDescriptorUpdated(object sender, PanoramicDataColumnDescriptorUpdatedEventArgs e)
        {
            OnPropertyChanged("ColumnDescriptors");
        }

        public PanoramicDataColumnDescriptor GetColumnDescriptorForLabelGuid(Guid id)
        {
            if (_providedLabels.Keys.Any(l => l.ID == id))
            {
                return _providedLabels[_providedLabels.Keys.First(l => l.ID == id)];
            }
            else
            {
                return null;
            }
        }

        public Expr GetLabelValue(Label label, LabelConsumer target)
        {
            bool numeric = true;
            Expr toRender = new DoubleNumber(3);//_mathManager.GetRenderableExpr(out numeric);
            return toRender;
        }

        public Expr GetFunctionValue(WellKnownSym functionType, Label label, LabelConsumer target)
        {
            bool numeric = true;
            Expr toRender = new DoubleNumber(3);//_mathManager.GetRenderableExpr(out numeric);
            return toRender;
        }

        public void CloneLabel(Label newLabel, Label oldLabel)
        {
            //ProvidedLabels.Add(newLabel);
        }

        public void DeleteLabel(Label label)
        {
            if (_providedLabels.ContainsKey(label))
            {
                PanoramicDataColumnDescriptor cd = _providedLabels[label];
                RemoveColumnDescriptor(cd);
                _providedLabels.Remove(label);
            }
        }

        public bool DropLabelAllowed(Rct bounds, LabelProvider provider)
        {
            return true;
        }

        public void DropLabel(Label label, bool addStroqs = true)
        {
        }

        public void CreateNewLabel(StroqCollection sc, PanoramicDataColumnDescriptor panoramicDataColumnDescriptor)
        {
            Label label = new Label(this, this);
            AddColumnDescriptor(panoramicDataColumnDescriptor);
            label.InkTableContents = InkTableContentCollection.GetInkTableContentCollection(sc);
            _providedLabels.Add(label, panoramicDataColumnDescriptor);
        }
    }
}