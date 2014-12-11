using starPadSDK.CharRecognizer;
using starPadSDK.Inq;
using starPadSDK.MathRecognizer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using starPadSDK.Inq.MSInkCompat;
using starPadSDK.MathExpr;
using starPadSDK.Geom;

namespace PanoramicData.utils.inq
{

    public class Label
    {
        public static Guid LABEL_ID = Guid.NewGuid();

        public delegate void ValueChangedHandler(object sender, LabelEventArgs e);
        public event ValueChangedHandler ValueChanged;

        public delegate void LabelChangedHandler(object sender, LabelEventArgs e);
        public event LabelChangedHandler LabelChanged;

        public delegate void LabelDeletedHandler(object sender, LabelEventArgs e);
        public event LabelDeletedHandler LabelDeleted;

        public delegate void LabelProviderDeletedHandler(object sender, LabelEventArgs e);
        public event LabelProviderDeletedHandler LabelProviderDeleted;

        public Guid ID = Guid.NewGuid();

        private Color _labelColor = Color.FromArgb(255, 0, 141, 255);
        private Color _nonlabelColor = Colors.Black;
        private static List<Label> _valueChangedStack = new List<Label>();
        private InkTableContentCollection _inkTableContents = new InkTableContentCollection();

        public InkTableContentCollection InkTableContents
        {
            get
            {
                return _inkTableContents;
            }
            set
            {
                List<InkTableContentGroup> allGroups = new List<InkTableContentGroup>();
                foreach (var c in _inkTableContents)
                {
                    foreach (var g in c.InkTableContentGroups)
                    {
                        if (!allGroups.Contains(g))
                        {
                            allGroups.Add(g);
                            g.InkTableContents = _inkTableContents;
                        }
                    }
                }

                _inkTableContents = value;
                foreach (var c in _inkTableContents)
                {
                    c.InkTableContentGroups = allGroups;
                    c.Label = this;
                    c.ChangeColor(_labelColor);
                }
            }
        }

        public LabelProvider LabelProvider { get; set; }
        public LabelConsumer LabelConsumer { get; set; }

        public Label(LabelProvider labelProvider, LabelConsumer labelConsumer)
        {
            InkTableContents = new InkTableContentCollection();

            this.LabelProvider = labelProvider;
            this.LabelConsumer = labelConsumer;
            
            FormulaEvaluator.AddLabel(this);
        }

        public Label(LabelProvider labelProvider)
        {
            this.LabelProvider = labelProvider;

            FormulaEvaluator.AddLabel(this);
        }

        public Label Clone()
        {
            Label newLabel = new Label(this.LabelProvider, this.LabelConsumer);
            InkTableContentCollection contents = new InkTableContentCollection();
            foreach (var c in InkTableContents)
            {
                contents.Add(c.Clone());
            }
            newLabel.InkTableContents = contents;
            LabelProvider.CloneLabel(newLabel, this);
            return newLabel;
        }

        public void FireValueChanged()
        {
            if (ValueChanged != null)
            {
                // circular update checker
                if (!_valueChangedStack.Contains(this))
                {
                    _valueChangedStack.Add(this);
                    ValueChanged(this, new LabelEventArgs());
                    _valueChangedStack.Remove(this);
                }
            }
        }

        public void FireLabelChanged(InkTableContentCollection oldContents)
        {
            if (LabelChanged != null)
            {
                LabelChanged(this, new LabelEventArgs(oldContents));
            }
        }

        public void FireLabelDeleted()
        {
            foreach (var c in _inkTableContents)
            {
                c.Label = null;
                c.ChangeColor(_nonlabelColor);
            }
            FormulaEvaluator.RemoveLabel(this);
            LabelProvider.DeleteLabel(this);

            if (LabelDeleted != null)
            {
                LabelDeleted(this, new LabelEventArgs());
            }
        }
        
        public void FireLabelProviderDeleted()
        {
            foreach (var c in _inkTableContents)
            {
                c.Label = null;
                c.ChangeColor(_nonlabelColor);
            }
            FormulaEvaluator.RemoveLabel(this);

            if (LabelProviderDeleted != null)
            {
                LabelProviderDeleted(this, new LabelEventArgs());
            }
        }

        public void CreateRecognition(MathRecognition mathRecognition)
        {
            StroqCollection stroqs = _inkTableContents.GetStroqs();
            Rct bounds = stroqs.GetBounds();
            var bbounds = mathRecognition.Sim[stroqs].GetBoundingBox();

            Recognition r = new Recognition(mathRecognition.Sim[stroqs], new Recognition.Result(this.ID.ToString()), (int)bbounds.Bottom, (int)(bbounds.Top + bbounds.Height / 2.0));
            r.levelsetby = 0;
            mathRecognition.Charreco.FullClassify(stroqs.First().OldStroke(), r);
            mathRecognition.ForceParse(false);
        }
    }

        public class LabelEventArgs : EventArgs
    {
        public InkTableContentCollection OldContents { get; set; }
        public LabelEventArgs(InkTableContentCollection oldContents = null)
        {
            OldContents = oldContents;
        }
    }

    public interface LabelConsumer
    {
        bool DropLabelAllowed(Rct bounds, LabelProvider provider);
        void DropLabel(Label label, bool addStroqs = true);
    }

    public interface LabelProvider
    {
        Expr GetFunctionValue(WellKnownSym functionType, Label label, LabelConsumer target);
        Expr GetLabelValue(Label label, LabelConsumer target);
        void CloneLabel(Label newLabel, Label oldLabel);
        void DeleteLabel(Label label);
    }
}
