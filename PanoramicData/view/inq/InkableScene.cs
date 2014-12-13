using starPadSDK.Inq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PanoramicData.view.inq
{
    public class InkableScene : InkableCanvas
    {
        private StroqCollection _stroqs = new StroqCollection();
        private List<FrameworkElement> _elements = new List<FrameworkElement>();
        private Canvas _elementCanvas = new Canvas();

        public InkableScene()
        {
            InkCollectedEvent += InkableScene_InkCollectedEvent;
            Children.Add(_elementCanvas);
        }

        void InkableScene_InkCollectedEvent(object sender, InkCollectedEventArgs e)
        {
            if (!_stroqs.Contains(e.Stroq))
            {
                _elementCanvas.Children.Add(e.Stroq);
                _stroqs.Add(e.Stroq);
            }
        }

        public void Add(FrameworkElement elem)
        {
            if (!_elements.Contains(elem))
            {
                _elementCanvas.Children.Add(elem);
                _elements.Add(elem);
            }
        }

        public void Remove(FrameworkElement elem)
        {
            if (_elements.Contains(elem))
            {
                _elementCanvas.Children.Remove(elem);
                _elements.Remove(elem);
            }
        }

        public void Remove(Stroq s)
        {
            if (_stroqs.Contains(s))
            {
                _elementCanvas.Children.Remove(s);
                _stroqs.Remove(s);
            }
        }

    }
}
