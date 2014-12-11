using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Runtime.Serialization;
using starPadSDK.Inq.BobsCusps;
using starPadSDK.WPFHelp;
using System.IO;

namespace starPadSDK.AppLib {
    [Serializable()]
    public class SelectionObj : ISerializable {
        List<Stroq>            _strokes = new List<Stroq>();
        List<FrameworkElement> _elements = new List<FrameworkElement>();
        Pt[]                   _outline = null;  // selection boundary relative to selection
        Rct                    _outlineRect;
        Mat                    _outlineXform = Mat.Identity;

        // updates the outline stroke -- needed after deserializing since serializing saves the selection contents only
        protected void updateOutline() {
            Rct bounds = ActualBounds;
            if (bounds.Width < 100)
                bounds = bounds.Inflate((200 - bounds.Width) / 2, 0);
            if (bounds.Height < 100)
                bounds =  bounds.Inflate(0, (200 - bounds.Height) / 2);
            _outlineXform = Mat.Translate(bounds.TopLeft);
            _outline = GeomUtils.ToPointList(bounds.Translated(-(Vec)bounds.TopLeft));
            _outlineRect = GeomUtils.Bounds(_outline.Select((p)=>(Pt)p));
        }

        public delegate void SelectionMovedHandler(SelectionObj sel);
        public event SelectionMovedHandler SelectionMovedEvent;
        public SelectionObj() { }
        public SelectionObj(SerializationInfo info, StreamingContext context) {
           _strokes = (List<Stroq>)info.GetValue("Stroqs", typeof(List<Stroq>));
           List<ATextBox.ATextBoxProxy>   _texts  = (List<ATextBox.ATextBoxProxy>) info.GetValue("Texts",  typeof(List<ATextBox.ATextBoxProxy>));
           List<AnImage.ImageProxy>       _images = (List<AnImage.ImageProxy>)     info.GetValue("Images", typeof(List<AnImage.ImageProxy>));
           foreach (ATextBox.ATextBoxProxy t in _texts) _elements.Add(t.Create());
           foreach (AnImage.ImageProxy ip in _images)   _elements.Add(ip.Create());
        }
        public SelectionObj(params FrameworkElement[] elements) : this(new List<Stroq>(), elements, null) { }
        public SelectionObj(params Stroq[] strokes) : this(strokes, new List<FrameworkElement>(), null) { }
        public SelectionObj(IEnumerable<Stroq> strokes, IEnumerable<FrameworkElement> images, Pt[] outline) {
            _strokes  = strokes != null?  new List<Stroq>(strokes):_strokes;
            _elements = images != null?   new List<FrameworkElement>(images):_elements;
            _outline  = null;
            Rct bounds = ActualBounds;
            if (outline != null) {
                _outlineXform = Mat.Translate(bounds.TopLeft);
                _outline = new Pt[outline.Length];
                // normalize the outline so that it can be offset to wherever the selection is placed
                for (int i = 0; i < _outline.Length; i++)
                    _outline[i] = (Pt)(outline[i] - bounds.TopLeft);

                outline  = null;
                bounds   = bounds.Union(GeomUtils.Bounds(_outline.Select((p)=>(Pt)p)).Translated(bounds.TopLeft - new Pt()));

                // convert freeform outline to rectangle shape
                _outlineXform = Mat.Translate(bounds.TopLeft);
                _outline = GeomUtils.ToPointList(bounds.Translated(-(Vec)bounds.TopLeft));

                _outlineRect = GeomUtils.Bounds(_outline.Select((p) => (Pt)p));
            } else
              updateOutline();
        }
        public void Add(SelectionObj sel) {
            foreach (Stroq s in sel.Strokes)
                if (!Strokes.Contains(s))
                    _strokes.Add(s);
            foreach (FrameworkElement f in sel.Elements)
                if (!Elements.Contains(f))
                    _elements.Add(f);
            updateOutline();
        }
        /// <summary>
        /// Outline of selection in the coordinates of the Canvas that the Selection is part of
        /// </summary>
        public Polygon            Outline      { get { if (_outline == null) updateOutline();
                                                      Polygon p         = new Polygon();
                                                      p.Points          = new PointCollection(_outline.Select((Pt pp)=>(Point)pp));
                                                      p.RenderTransform = new MatrixTransform(_outlineXform);
                                                      return p;
            }
        }
        /// <summary>
        /// The Rectangle bounds of the contents of the selection in the coordinates of the Canvas that contains the selection.
        /// </summary>
        public Rct                ActualBounds {
            get {
                Rct bounds = Rct.Null;
                foreach (Stroq s in _strokes)
                    bounds = bounds.Union(s.GetBounds());
                foreach (FrameworkElement i in _elements)
                    bounds = bounds.Union(WPFUtil.GetBounds(i));
                return bounds;
            }
        }
        /// <summary>
        /// the bounds of the selection, including padding, in the coordinates of the Canvas that contains the selection
        /// </summary>
        public Rct                Bounds      { get { return ((Mat)Outline.RenderTransform.Value)*GeomUtils.Bounds(Outline.Points.Select((p)=>(Pt)p)); } }
        /// <summary>
        /// whether the selection contains anything or not
        /// </summary>
        public bool               Empty       { get { return _strokes.Count == 0 && _elements.Count == 0; } }
        /// <summary>
        /// the selected strokes.
        /// </summary>
        public Stroq[]            Strokes     { get { return _strokes.ToArray(); } }
        /// <summary>
        /// the selected FrameworkElements
        /// </summary>
        public FrameworkElement[] Elements    { get { return _elements.ToArray(); } }
        /// <summary>
        /// Moves the selection
        /// </summary>
        /// <param name="delta">the vector to translate the selection by</param>
        public void               MoveBy(Vec delta) { XformBy(Mat.Translate(delta)); }
        /// <summary>
        /// Applies a transformation to the selection
        /// </summary>
        /// <param name="m">the matrix to apply to the selection</param>
        public void               XformBy(Mat m) {
            foreach (Stroq s in _strokes) 
                s.XformBy(m);
            foreach (FrameworkElement i in _elements)
                i.RenderTransform = new MatrixTransform(((Mat)i.RenderTransform.Value) * m);
            _outlineXform = _outlineXform * m;

            if (SelectionMovedEvent != null)
                SelectionMovedEvent(this);
        }
        /// <summary>
        /// Device that created/controls this selection
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        public object             Device { get; set; }

        #region ISerializable Members

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
            List<AnImage.ImageProxy>       _images = new List<AnImage.ImageProxy>();
            List<ATextBox.ATextBoxProxy>   _texts  = new List<ATextBox.ATextBoxProxy>();
            foreach (FrameworkElement e in _elements)
                if (e is Image)         _images.Add((e as Image).Proxy());
                else if (e is TextBox) _texts.Add((e as TextBox).Proxy());
            info.AddValue("Stroqs", _strokes);
            info.AddValue("Texts",  _texts);
            info.AddValue("Images", _images);
         }

        #endregion
    }
    static public class AnImage {
        [Serializable]
        public class ImageProxy {
            Size   _size = new Size();
            byte[] _pixels = null;
            Mat    _xform = new Mat();
            Mat    _canXform = Mat.Identity;
            Size   _canSize  = new Size();
            public FrameworkElement Create() {
                Image img = new Image();
                BitmapImage bimg = new BitmapImage();
                bimg.BeginInit();
                bimg.StreamSource = new MemoryStream(_pixels);
                bimg.EndInit();
                img.Source = bimg;
                img.Height = _size.Height;
                img.Width = _size.Width;
                img.RenderTransform = new MatrixTransform(_xform);
                if (_canXform != Mat.Identity) {
                    Canvas canvas = new Canvas();
                    canvas.RenderTransform = new MatrixTransform(_canXform);
                    canvas.Children.Add(img);
                    canvas.Width = _canSize.Width;
                    canvas.Height = _canSize.Height;
                    canvas.ClipToBounds = true;
                    return canvas;
                }
                return img;
            }
            public ImageProxy(Image i, Canvas can) {
                using (MemoryStream outStream = new MemoryStream()) {
                    BitmapEncoder enc = new BmpBitmapEncoder();
                    enc.Frames.Add(BitmapFrame.Create(i.Source as BitmapSource));
                    enc.Save(outStream);
                    _pixels = outStream.ToArray();
                }
                _xform = ((Mat)i.RenderTransform.Value);
                _size = new Size(i.ActualWidth, i.ActualHeight);
                if (can != null) {
                    _canXform = (Mat)can.RenderTransform.Value;
                    _canSize = new Size(can.Width, can.Height);
                }
            }
        }
        static public ImageProxy Proxy(this Image img) { return new ImageProxy(img, null); }
        static public ImageProxy Proxy(this Image img, Canvas can) { return new ImageProxy(img, can); }
    }
    static public class ATextBox  {
        [Serializable]
        public struct ATextBoxProxy {
            public string Text;
            public string FontFamily;
            public double Size;
            public string Style;
            public Mat Xform;

            public ATextBoxProxy(TextBox f) {
                Text = f.Text;
                FontFamilyConverter fc = new FontFamilyConverter();
                FontStyleConverter sc = new FontStyleConverter();
                FontFamily = fc.ConvertToString(f.FontFamily);
                Size = f.FontSize;
                Style = sc.ConvertToString(f.FontStyle);
                Xform = (Mat)f.RenderTransform.Value;
            }
            public TextBox Create() {
                FontFamilyConverter fc = new FontFamilyConverter();
                FontStyleConverter sc = new FontStyleConverter();
                Vec Where = Xform * new Pt() - new Pt(0, -2 * Size);
                TextBox f = WPFUtil.MakeText(Text, new Rct(new Pt() + Where, new Pt(0, Size * 2) + Where));
                f.FontFamily = (FontFamily)fc.ConvertFromString(FontFamily);
                f.FontStyle = (FontStyle)sc.ConvertFromString(Style);
                f.RenderTransform = new MatrixTransform(Xform);
                return f;
            }
        }
        static public ATextBoxProxy Proxy(this TextBox tb) { return new ATextBoxProxy(tb); }
    }
}
