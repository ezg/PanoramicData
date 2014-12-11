using starPadSDK.Inq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PanoramicData.view.inq
{
    public class InkableScene : InkableCanvas
    {
        private StroqCollection _stroqs = new StroqCollection();
        private Canvas _elementCanvas = new Canvas();

        public InkableScene()
        {
            InkCollectedEvent += InkableScene_InkCollectedEvent;
            Children.Add(_elementCanvas);
        }

        void InkableScene_InkCollectedEvent(object sender, InkCollectedEventArgs e)
        {
            _stroqs.Add(e.Stroq);
            _elementCanvas.Children.Add(e.Stroq);
        }
    }
}
