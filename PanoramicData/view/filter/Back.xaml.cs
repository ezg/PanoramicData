using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Windows;
using System.Windows.Controls;
using PanoramicDataModel;
using starPadSDK.Inq;
using PanoramicData.model.view;
using PanoramicData.utils.inq;
using PanoramicData.model.view_new;

namespace PanoramicData.view.filter
{
    /// <summary>
    /// Interaction logic for Back.xaml
    /// </summary>
    public partial class Back : FilterRenderer
    {
        private InqAnalyzer _inqAnalyser = new InqAnalyzer();

        public string Name { get; set; }

        public StroqCollection Stroqs
        {
            get
            {
                if (aPage.Stroqs != null && aPage.Stroqs.Count > 0)
                {
                    return new StroqCollection(aPage.Stroqs);
                }
                else
                {
                    return null;
                }
            }
        }

        public Back()
        {
            InitializeComponent(); 
            this.SizeChanged += Back_SizeChanged;
            aPage.Background = new SolidColorBrush(Color.FromArgb(1, 0, 0, 0));
            aPage.StroqAddedEvent += new starPadSDK.AppLib.InqScene.StroqHandler(stroqAddedEvent);
            aPage.StroqRemovedEvent += new starPadSDK.AppLib.InqScene.StroqHandler(stroqRemovedEvent);
            aPage.StroqsAddedEvent += new starPadSDK.AppLib.InqScene.StroqsHandler(stroqsAddedEvent);
            aPage.StroqsRemovedEvent += new starPadSDK.AppLib.InqScene.StroqsHandler(stroqsRemovedEvent);
        }

        void Back_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            aPage.Width = this.ActualWidth;
            aPage.Height = this.ActualHeight;
        }


        protected override void UpdateRendering()
        {
            base.UpdateRendering();
        }

        public void SetStroqs(StroqCollection stroqs)
        {
            aPage.Clear();
            _inqAnalyser = new InqAnalyzer();
            _inqAnalyser.ResultsUpdated += _inqAnalyser_ResultsUpdated;

            Name = "";
            if (stroqs != null && stroqs.Count > 0)
            {
                aPage.AddNoUndo(stroqs);
            }
            else
            {
                resultLabel.Content = "Handwrite a Name for this Filter";
            }
        }

        void _inqAnalyser_ResultsUpdated(object sender, System.Windows.Ink.ResultsUpdatedEventArgs e)
        {
            string recognizedString = _inqAnalyser.GetRecognizedString().Trim().Replace("\r\n", " ");
            Name = recognizedString;
            resultLabel.Content = recognizedString;

            if (recognizedString == "")
            {
                resultLabel.Content = "Handwrite a Name for this Filter";
            }
        }

        void stroqAddedEvent(Stroq s)
        {
            _inqAnalyser.AddStroke(s);
            _inqAnalyser.BackgroundAnalyze();
        }

        void stroqsAddedEvent(Stroq[] stroqs)
        {
            foreach (var s in stroqs)
            {
                _inqAnalyser.AddStroke(s);
            }
            _inqAnalyser.BackgroundAnalyze();
        }

        void stroqRemovedEvent(Stroq s)
        {
            _inqAnalyser.RemoveStroke(s);
            _inqAnalyser.BackgroundAnalyze();
        }

        void stroqsRemovedEvent(Stroq[] stroqs)
        {
            foreach (var s in stroqs)
            {
                _inqAnalyser.RemoveStroke(s);
            }
            _inqAnalyser.BackgroundAnalyze();
        }
    }
}
