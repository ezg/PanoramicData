using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Runtime.Serialization;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq;
using starPadSDK.WPFHelp;

/// Defines a generic inking canvas that can be used to display strokes interleaved with FrameworkElements.  This canvas also 
/// supports a Selection, Undo/Redo, Grouping, and Wiggles (which are resizable hand-drawn lines)
namespace starPadSDK.AppLib {
    [Serializable()]
    public class InqScene : InqCanvas, ISerializable {
        [NonSerialized()] Dictionary<object, SelectionObj> _selections = new Dictionary<object, SelectionObj>();
        [NonSerialized()] Dictionary<object,SelectionFeedback>   _feedback  = new Dictionary<object,SelectionFeedback>();
        [NonSerialized()] GroupMgr               _groups     = new GroupMgr();
        [NonSerialized()] List<Wiggle>           _wiggles = new List<Wiggle>();
        [NonSerialized()] List<FrameworkElement> _elements = new List<FrameworkElement>();
        [NonSerialized()] Canvas                 _sceneLayer = new Canvas();
        [NonSerialized()] UndoRedo               _undoRedo   = new UndoRedo();
        [NonSerialized()] CommandSet             _cmds = null;  // gesture commands active on this page
        [NonSerialized()] bool                    _immediateDrag = false;
        #region ISerializable Members
        [NonSerialized()]
        List<Stroq> _pending = new List<Stroq>(); // a list of Stroq's that need to be added to the Page after deserialization is completed.

        virtual protected void getObjectData(SerializationInfo info, StreamingContext context) {
            List<AnImage.ImageProxy> _images = new List<AnImage.ImageProxy>();
            List<ATextBox.ATextBoxProxy> _texts = new List<ATextBox.ATextBoxProxy>();
            foreach (FrameworkElement e in Elements)
                if (e is Image) _images.Add((e as Image).Proxy());
                else if (e is Canvas && (e as Canvas).Children[0] is Image) _images.Add(((e as Canvas).Children[0] as Image).Proxy(e as Canvas));
                else if (e is TextBox) _texts.Add((e as TextBox).Proxy());
            info.AddValue("Stroqs", new List<Stroq>(Stroqs));
            info.AddValue("Images", _images);
            info.AddValue("Texts", _texts);
        }
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
            getObjectData(info, context);
        }

        [OnDeserialized()]
        void onDeserializedFix(StreamingContext sc) {
            foreach (Stroq s in _pending) {
                s.onDeserializedFix(sc);
                AddWithUndo(s);
            }
            _pending.Clear();
        }
        #endregion

        Visibility feedbackVisibility(object obj) {
            if (_feedback.ContainsKey(obj))
                return _feedback[obj].Visibility;
            return Visibility.Hidden;
        }
        /// <summary>
        /// Adds a handler for generating Undo entries when the TextBox is edited
        /// </summary>
        /// <param name="tb"></param>
        protected void set(TextBox tb) {
           tb.PreviewMouseLeftButtonDown += (((object s, MouseButtonEventArgs e) => UndoRedo.Add(new TextEnteredAction(s as TextBox, (s as TextBox).Text, this))));
        }
        /// <summary> 
        /// Prevents Ink from being started over TextBox's
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released &&
                e.MiddleButton == MouseButtonState.Released &&
                e.RightButton == MouseButtonState.Released &&
                feedbackVisibility(e.Device) != Visibility.Visible)
            {
                SetInkEnabledForDevice(e.Device, true);
                foreach (FrameworkElement fe in Elements)
                    if (!(fe is Image) && fe.InputHitTest(e.GetPosition(fe)) != null)
                    //    if (!(fe is Image) && !(fe is PolygonBase) && fe.InputHitTest(WPFUtil.TransformFromAtoB(e.GetPosition(this), this, fe)) != null)
                        SetInkEnabledForDevice(e.Device, false);
            }
            base.OnMouseMove(e);
        }
        protected override void OnStylusInAirMove(StylusEventArgs e) {
            if (e.InAir && feedbackVisibility(e.Device) != Visibility.Visible) {
                SetInkEnabledForDevice(e.Device, true);
                foreach (FrameworkElement fe in Elements)
                    if (!(fe is Image) && fe.InputHitTest(e.GetPosition(fe)) != null)
                        //     if (!(fe is Image) && !(fe is PolygonBase) && fe.InputHitTest(WPFUtil.TransformFromAtoB(e.GetPosition(this), this, fe)) != null)
                        ;    
                        //SetInkEnabledForDevice(e.Device, false);
            }
            else SetInkEnabledForDevice(e.Device, false);
            base.OnStylusInAirMove(e);
        }
        protected override void PointMoveEvent(object sender, InputFramework.WPFDevices.RoutedPointEventArgs e) {
            if (feedbackVisibility(e.WPFPointDevice) != Visibility.Visible) {
                SetInkEnabledForDevice(e.WPFPointDevice, true);
                foreach (FrameworkElement fe in Elements)
                    if (!(fe is Image) && fe.InputHitTest(e.GetPosition(fe)) != null)
                   //     if (!(fe is Image) && !(fe is PolygonBase) && fe.InputHitTest(WPFUtil.TransformFromAtoB(e.GetPosition(this), this, fe)) != null)
                            SetInkEnabledForDevice(e.WPFPointDevice, true);
            }
            base.PointMoveEvent(sender, e);
        }
        protected virtual  void init() { Commands = initCommands(); }
        protected virtual CommandSet initCommands() { return null; }

        public delegate void ElementHandler(FrameworkElement e);
        public delegate void ElementsHandler(FrameworkElement[] elems);
        public delegate void StroqHandler(Stroq s);
        public delegate void StroqsHandler(Stroq[] s);
        public delegate bool StroqFilterHandler(Stroq s);
        public delegate bool StroqsFilterHandler(Stroq[] s);
        public delegate bool StroqReplaceFilterHandler(Stroq[] orig, Stroq[] replace);
        public delegate void SelectedChangedHandler(object device, InqScene canvas);
        public delegate void SelectionTransformHandler(SelectionObj selection);
        public delegate void SelectionMovedHandler(SelectionObj sel);
        public delegate void DeviceMovedHandler(Pt point);
        public event StroqFilterHandler        StroqPreAddEvent;
        public event StroqsFilterHandler       StroqsPreAddEvent;
        public event StroqReplaceFilterHandler StroqPreReplaceEvent;
        public event StroqsHandler             StroqsClearedEvent;
        public event StroqHandler              StroqAddedEvent;
        public event StroqsHandler             StroqsAddedEvent;
        public event StroqHandler              StroqRemovedEvent;
        public event StroqsHandler             StroqsRemovedEvent;
        public event ElementHandler            ElementAddedEvent;
        public event ElementsHandler           ElementsAddedEvent;
        public event ElementHandler            ElementRemovedEvent;
        public event ElementsHandler           ElementsRemovedEvent;
        public event ElementsHandler           ElementsClearedEvent;
        public event SelectedChangedHandler    SelectedChangedEvent;
        public event SelectionTransformHandler SelectionPreTransformEvent;
        public event SelectionTransformHandler SelectionPostTransformEvent;
        public event SelectionTransformHandler SelectionStartTransformingEvent;
        public event SelectionTransformHandler SelectionStopTransformingEvent;
        public event SelectionMovedHandler     SelectionMovedEvent;
        public event SelectionMovedHandler     SelectionDroppedEvent;
        public event DeviceMovedHandler        DeviceMovedEvent;
        
        public InqScene(SerializationInfo info, StreamingContext context):this() {
            List<Stroq>                   stroqs = (List<Stroq>)info.GetValue("Stroqs", typeof(List<Stroq>));
            List<ATextBox.ATextBoxProxy> _texts = (List<ATextBox.ATextBoxProxy>)info.GetValue("Texts", typeof(List<ATextBox.ATextBoxProxy>));
            List<AnImage.ImageProxy>     _images = (List<AnImage.ImageProxy>)info.GetValue("Images", typeof(List<AnImage.ImageProxy>));
            foreach (ATextBox.ATextBoxProxy t in _texts) AddNoUndo(t.Create());
            foreach (AnImage.ImageProxy ip in _images) AddNoUndo(ip.Create());
            _pending = stroqs;
        }

        public InqScene() : base(false) {
            _drawStroqs = false; // don't want the base InqCanvas to draw anything.  We'll do that ourself
            Children.Add(_sceneLayer);
            init();
        }
        public void                                  RemoveSelection(object index)
        {
            if (Feedback(index) != null)
            {
                Children.Remove(_feedback[index]);
                Feedback(index).Dispose();
            }
            _feedback.Remove(index);
            _selections.Remove(index);
        }
        public SelectionObj                          Selection(object index) {
            if (!_selections.ContainsKey(index)) {
                _selections.Add(index, new SelectionObj());
                _selections[index].Device = index;
            }
            return _selections[index];
        }
        public SelectionObj                          SetSelection(object index, SelectionObj val) {
            if (!_feedback.ContainsKey(index))
                Feedback(index);
            if (Selection(index) != null)
                _selections[index] = val;
            val.Device = index;
            val.SelectionMovedEvent -= new SelectionObj.SelectionMovedHandler(RaiseSelectionMovedEvent);
            val.SelectionMovedEvent += new SelectionObj.SelectionMovedHandler(RaiseSelectionMovedEvent);
            if (SelectedChangedEvent != null)
            {
                SelectedChangedEvent(index, this);

                Console.WriteLine("SetSelection");
            }
            return val;
        }
        public SelectionFeedback                     Feedback(object obj) {
            if (!_feedback.ContainsKey(obj)) {
                _feedback.Add(obj, new SelectionFeedback(this, obj));
                Children.Add(_feedback[obj]);
            }
            _feedback[obj].ImmediateDrag = _immediateDrag;
            _feedback[obj].AllowScaleRotate = !_immediateDrag;
            return _feedback[obj];
        }
        public Dictionary<object, SelectionObj>      Selections { get { return _selections; } }
        public Dictionary<object, SelectionFeedback> Feedbacks  { get { return _feedback; } }
        public CommandSet          Commands          { get { return _cmds; } set { _cmds = value; } }
        public UndoRedo            UndoRedo          { get { return _undoRedo; } }
        public Canvas              SceneLayer        { get { return _sceneLayer; } set { _sceneLayer = value; } }
        public GroupMgr            Groups            { get { return _groups; } }
        public Wiggle[]            Wiggles           { get { return _wiggles.ToArray(); } }
        public FrameworkElement[]  Elements          { get { return _elements.ToArray(); } }

        public void GrabSelection(object device, SelectionFeedback feedback)
        {
            if (device == feedback.Device)
                return;
            SelectionObj grabbedSelection = feedback.Selection;
            object oldDevice = grabbedSelection.Device;
            _selections[oldDevice] = new SelectionObj(); // give old device an empty selection to turn it off
            _selections[oldDevice].Device = oldDevice;
            _selections.Remove(device);
            _selections.Add(device, grabbedSelection);
            grabbedSelection.Device = device;
            Feedback(device);
            if (_feedback.ContainsKey(oldDevice))
            {
                var oldFeedback = _feedback[oldDevice];
                var newFeedback = _feedback.ContainsKey(device) ? _feedback[device] : null;
                _feedback[oldDevice] = newFeedback;
                if (_feedback.ContainsKey(device))
                    _feedback.Remove(device);
                _feedback.Add(device, oldFeedback);
                oldFeedback.Device = device;
                newFeedback.Device = oldDevice;
            }
            if (SelectedChangedEvent != null) {
                SelectedChangedEvent(oldDevice, this);
                SelectedChangedEvent(device, this);
            }
            RemoveSelection(oldDevice);
        }
        public void RaiseSelectionMovedEvent(SelectionObj selection) {
            if (SelectionMovedEvent != null)
                SelectionMovedEvent(selection);
        }
        public void RaisePreTransformEvent(SelectionObj selection) {
            if (SelectionPreTransformEvent != null)
                SelectionPreTransformEvent(selection);
        }
        public void RaisePostTransformEvent(SelectionObj selection) {
            if (SelectionPostTransformEvent != null)
                SelectionPostTransformEvent(selection);
        }
        public void RaiseStartTransformingEvent(SelectionObj selection) {
            if (SelectionStartTransformingEvent != null)
                SelectionStartTransformingEvent(selection);
        }
        public void RaiseStopTransformingEvent(SelectionObj selection) {
            if (SelectionStopTransformingEvent != null)
                SelectionStopTransformingEvent(selection);
        }
        public void RaiseSelectionDroppedEvent(SelectionObj selection) {
            if (SelectionDroppedEvent != null)
                SelectionDroppedEvent(selection);
        }
        public void ReplaceNoUndo(Stroq[] orig, Stroq[] replace) {
            if (StroqPreReplaceEvent != null) 
                if (StroqPreReplaceEvent(orig, replace)) 
                    return;
            foreach (Stroq s in orig)
                if (Stroqs.Contains(s))
                    Rem(s);
            foreach (Stroq s in replace)
                if (!Stroqs.Contains(s)) {
                    AddNoUndo(s);
                }
        }
        public void AddWithUndo(TextBox t) { 
            AddNoUndo(t as FrameworkElement); 
            UndoRedo.Add(new TextEnteredAction(t, t.Text, this));
        }
        public void AddWithUndo(FrameworkElement c)  { 
            if (!_elements.Contains(c)) { 
                _elements.Add(c); 
                _sceneLayer.Children.Add(c);
                UndoRedo.Add(new ElementAddedAction(c, this));
                if (ElementAddedEvent != null)
                    ElementAddedEvent(c); 
                                                                 
                if (c is TextBox) set(c as TextBox);   
            } 
        }
        public void AddWithUndo(List<FrameworkElement> elems)
        {
            foreach (var c in elems)
            {
                if (!_elements.Contains(c))
                {
                    _elements.Add(c);
                    _sceneLayer.Children.Add(c);

                    if (c is TextBox) set(c as TextBox);
                }
            }
            if (elems.Count > 0)
            {
                UndoRedo.Add(new ElementAddedAction(elems.ToArray(), this));
                if (ElementsAddedEvent != null)
                    ElementsAddedEvent(elems.ToArray());
            }
        }
        public void AddNoUndoBack(FrameworkElement c)
        {
            if (!_elements.Contains(c))
            {
                _elements.Add(c);
                _sceneLayer.Children.Insert(0, c);
                if (ElementAddedEvent != null)
                    ElementAddedEvent(c);
                if (c is TextBox)
                    set(c as TextBox);
            }
        }
        public void AddWithUndoBack(FrameworkElement c)
        {
            if (!_elements.Contains(c))
            {
                _elements.Add(c);
                _sceneLayer.Children.Insert(0, c);
                UndoRedo.Add(new ElementAddedAction(c, this));
                if (ElementAddedEvent != null)
                    ElementAddedEvent(c);

                if (c is TextBox) set(c as TextBox);
            }
        }
        public void AddWithUndo(Stroq s)  {
            if (StroqPreAddEvent != null) 
                if (StroqPreAddEvent(s)) 
                    return; 
            if (!Stroqs.Contains(s)) {
                Stroqs.Add(s);
                //if (s.BackingStroke.DrawingAttributes.IsHighlighter) {
                //    s.BackingStroke.DrawingAttributes.IsHighlighter = false;
                //    _sceneLayer.Children.Insert(0, s);
                //}
                //else 
                    _sceneLayer.Children.Add(s);
                UndoRedo.Add(new InkAddedAction(s, this));
                if (StroqAddedEvent != null) 
                    StroqAddedEvent(s); 
            }
        }
        public void AddWithUndo(StroqCollection sc)  {
            if (StroqsPreAddEvent != null) 
                if (StroqsPreAddEvent(sc.ToArray())) 
                    return;
            foreach (var stroq in sc)
            {
                if (!Stroqs.Contains(stroq))
                {
                    Stroqs.Add(stroq);
                    _sceneLayer.Children.Add(stroq);
                }
            }
            if (sc.Count > 0)
            {
                UndoRedo.Add(new InkAddedAction(sc.ToArray(), this));
                if (StroqsAddedEvent != null)
                    StroqsAddedEvent(sc.ToArray()); 
            }
        }
        public void AddWithUndo(Wiggle w) {
            if (!_wiggles.Contains(w)) {
                _wiggles.Add(w); 
                AddNoUndo(w.Line[0]); 
                AddNoUndo(w.A); 
                AddNoUndo(w.B);
                UndoRedo.Add(new WiggleAddedAction(w, this));
            }
        }
        public void Rem(Wiggle w)  { _wiggles.Remove(w); Rem(w.Line[0]); Rem(w.A); Rem(w.B); }
        public void Rem(FrameworkElement c, bool fireEvent = true)
        { 
            _elements.Remove(c);
            _sceneLayer.Children.Remove(c);
            if (ElementRemovedEvent != null && fireEvent)
                ElementRemovedEvent(c);
        }
        public void Rem(List<FrameworkElement> elems)
        {
            foreach (var c in elems)
            {
                if (_elements.Contains(c))
                {
                    _elements.Remove(c);
                    _sceneLayer.Children.Remove(c);
                }
            }
            if (ElementsRemovedEvent != null)
                ElementsRemovedEvent(elems.ToArray());
        }
        public void Rem(Stroq s, bool fireEvent = true)  {
            Stroqs.Remove(s);
            foreach (Wiggle w in _wiggles)
                if (w.Line[0] == s) {
                    Rem(w.A);
                    Rem(w.B);
                    _wiggles.Remove(w);
                    break;
                }
            foreach (var e in _sceneLayer.Children)
                if (e is StroqElement && (e as StroqElement).Stroq == s) {
                    _sceneLayer.Children.Remove(e as StroqElement);
                    break;
                }
            if (StroqRemovedEvent != null && fireEvent)
                StroqRemovedEvent(s);
        }
        public void Rem(StroqCollection sc)
        {
            foreach (var s in sc)
            {
                Stroqs.Remove(s);
                foreach (Wiggle w in _wiggles)
                    if (w.Line[0] == s)
                    {
                        Rem(w.A);
                        Rem(w.B);
                        _wiggles.Remove(w);
                        break;
                    }
                foreach (var e in _sceneLayer.Children)
                    if (e is StroqElement && (e as StroqElement).Stroq == s)
                    {
                        _sceneLayer.Children.Remove(e as StroqElement);
                        break;
                    }
            }
            if (StroqsRemovedEvent != null)
                StroqsRemovedEvent(sc.ToArray());
        }
        public void RemWithUndo(Wiggle w)           { UndoRedo.Add(new DeleteAction(new SelectionObj(w.Line), this)); }
        public void RemWithUndo(StroqCollection sc) { UndoRedo.Add(new DeleteAction(new SelectionObj(sc.ToArray()), this)); }
        public void RemWithUndo(Stroq s)            { UndoRedo.Add(new DeleteAction(new SelectionObj(s), this)); }
        public void RemWithUndo(FrameworkElement f) { UndoRedo.Add(new DeleteAction(new SelectionObj(f), this)); }
        public void AddNoUndo(FrameworkElement c) {
            if (!_elements.Contains(c)) {
                _elements.Add(c);
                _sceneLayer.Children.Add(c);
                if (ElementAddedEvent != null)
                    ElementAddedEvent(c); 
                if (c is TextBox) 
                    set(c as TextBox);
            }
        }
        public void AddNoUndo(Stroq s, bool fireEvent = true)  {
            if (StroqPreAddEvent != null && fireEvent)
                if (StroqPreAddEvent(s))
                    return; 
            if (!Stroqs.Contains(s)) { 
                Stroqs.Add(s);
                _sceneLayer.Children.Add(s);
                if (StroqAddedEvent != null && fireEvent) StroqAddedEvent(s); 
            } 
        }
        public void AddNoUndo(StroqCollection sc)
        {
            if (StroqsPreAddEvent != null)
                if (StroqsPreAddEvent(sc.ToArray()))
                    return;

            foreach (var stroq in sc)
            {
                if (!Stroqs.Contains(stroq))
                {
                    Stroqs.Add(stroq);
                    _sceneLayer.Children.Add(stroq);
                }
            }
            if (sc.Count > 0)
            {
                if (StroqsAddedEvent != null)
                    StroqsAddedEvent(sc.ToArray());
            }
        }
        public void AddNoUndo(Wiggle w) { 
            if (!_wiggles.Contains(w)) { 
                _wiggles.Add(w); 
                AddNoUndo(w.Line[0]); 
                AddNoUndo(w.A); 
                AddNoUndo(w.B); 
            } 
        }

        public void Hide(Stroq s)
        {
            foreach (var e in _sceneLayer.Children)
                if (e is StroqElement && (e as StroqElement).Stroq == s)
                {
                    (e as StroqElement).Visibility = System.Windows.Visibility.Hidden;
                    break;
                }
        }

        public void Show(Stroq s)
        {
            foreach (var e in _sceneLayer.Children)
                if (e is StroqElement && (e as StroqElement).Stroq == s)
                {
                    (e as StroqElement).Visibility = System.Windows.Visibility.Visible;
                    break;
                }
        }

        /// <summary>
        /// The inqscene can be notified if there has been some movement of a pointer device. 
        /// This is used to update the fade out timer of the GestureButtons (see Gesturizer)
        /// </summary>
        /// <param name="point"></param>
        public void NotifyDeviceMoved(Pt point)
        {
            if (DeviceMovedEvent != null)
                DeviceMovedEvent(point);
        }

        public virtual void Clear()     {
            Stroq[] stroqs = Stroqs.ToArray();
            Stroqs.Clear();
            if (StroqsClearedEvent != null)
                StroqsClearedEvent(stroqs);
            foreach (KeyValuePair<object, SelectionObj> pair in _selections)
                _selections[pair.Key] = new SelectionObj();
            FrameworkElement[] elements = _elements.ToArray();
            _elements.Clear();
            if (ElementsClearedEvent != null)
                ElementsClearedEvent(elements);

            _sceneLayer.Children.Clear(); 
        }
        /// <summary>
        /// determines whether selections start moving on contact, or whether the contact must pass through
        /// the border of the selection to initiate movement.  In the latter case, additional widgets are
        /// displayed to allow scaling and rotation.
        /// </summary>
        /// <param name="flag"></param>
        public void         SetImmediateDrag(bool flag) {
            _immediateDrag = flag;
            foreach (KeyValuePair<object, SelectionFeedback> pair in _feedback) {
                pair.Value.ImmediateDrag =  flag;
                pair.Value.AllowScaleRotate = !flag;
            }
        }
        public Wiggle       Wiggle(Stroq s)     {
            foreach (Wiggle w in _wiggles)
                if (w.Line[0] == s)
                    return w;
            return null;
        }
        public SelectionObj PasteSelection()    {
            SelectionObj sel = null;
            IDataObject obj = Clipboard.GetDataObject();
            if (obj != null && obj.GetDataPresent(typeof(BitmapSource))) {
                BitmapSource o = (BitmapSource)obj.GetData(typeof(BitmapSource));
                FormatConvertedBitmap fb = new FormatConvertedBitmap(o, PixelFormats.Bgr32, null, 0); 
                Image img = new Image();
                img.Width = o.Width;
                img.Height = o.Height;
                if (img.Width > ActualWidth / 2) {
                    img.Height = ActualWidth / 2 / img.Width * img.Height;
                    img.Width = ActualWidth / 2;
                }
                if (img.Height > ActualHeight / 2) {
                    img.Width = ActualHeight / 2 / img.Height * img.Width;
                    img.Height = ActualHeight / 2;
                }
                img.Source = fb;
                img.VerticalAlignment = VerticalAlignment.Top;
                AddWithUndo(img);
                sel = new SelectionObj(null, new FrameworkElement[] { img }, null);
            }
            if (obj != null && obj.GetDataPresent("Ink Serialized Format")) {
                InkCanvas temp = new InkCanvas();
                temp.Paste();
                StroqCollection sq = new StroqCollection();
                foreach (Stroke s in temp.Strokes)
                    sq.Add(new Stroq(s));
                foreach (Stroq s in sq)
                    AddWithUndo(s);
            }
            if (obj != null && obj.GetDataPresent(typeof(SelectionObj))) {
                sel = obj.GetData(typeof(SelectionObj)) as SelectionObj;
                if (sel != null) {
                    foreach (Stroq s in sel.Strokes)
                        AddNoUndo(s);
                    foreach (FrameworkElement i in sel.Elements)
                        AddNoUndo(i);
                    UndoRedo.Add(new SelectionAddedAction(sel, this));
                }
            }
            if (obj != null && obj.GetDataPresent(typeof(string))) {
                if (obj.GetDataPresent(DataFormats.Rtf)) {
                    RichTextBox rtb = new RichTextBox();
                    rtb.Paste();
                    AddWithUndo(rtb);
                    rtb.BorderThickness = new Thickness(0);
                    rtb.Focus();
                    rtb.Width = 500; // bcz: ARghh!! why doesn't the RichTextBox come up a the "right" size automatically??
                    sel = new SelectionObj(null, new FrameworkElement[] { rtb }, null);
                }
                else {
                    TextBox tb = WPFUtil.MakeText("", new Rct(100, 100, 200, 150));
                    tb.Paste();
                    AddWithUndo(tb);
                    tb.Focus();
                    sel = new SelectionObj(null, new FrameworkElement[] { tb }, null);
                }
            }
            return sel;
        }
        /// <summary>
        /// selects Stroqs (or Wiggle EndPt's) that are within the touch Rectangle.
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="fingerTip"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        public SelectionFeedback AreaSelect(Rct bounds, bool selectAll, object device)
        {
            List<Stroq> contained = new List<Stroq>();
            List<FrameworkElement> elements = new List<FrameworkElement>();

            // first test if a Wiggle's EndPt is selected- if so, don't select anything else
            foreach (FrameworkElement c in Elements)
                if (c is EndPt)  {
                    Rct crect = WPFUtil.GetBounds(c);
                    if (bounds.IntersectsWith(crect))
                        elements.Add(c);
                }

            if (elements.Count == 0 || selectAll) {
                // if nothing was selected, then see if any Stroqs were touched and select them
                foreach (Stroq s in Stroqs)
                    if (bounds.IntersectsWith(s.GetBounds())) {
                        foreach (Wiggle w in Wiggles)
                            if (w.Line[0] == s)
                                elements.AddRange(new FrameworkElement[] { w.A, w.B });
                        foreach (StylusPoint p in s.StylusPoints)
                            if (bounds.Contains(p))  {
                                contained.Add(s);
                                break;
                            }
                    }
                if (elements.Count == 0 || selectAll) {
                    if (SelectionTestEvent != null)
                        SelectionTestEvent(bounds, contained, elements);
                    foreach (FrameworkElement c in Elements) {
                        if (c.GetBounds().IntersectsWith(bounds) || c.InputHitTest(bounds.Center) != null ||
                            c.InputHitTest(bounds.TopLeft) != null || c.InputHitTest(bounds.TopRight) != null ||
                            c.InputHitTest(bounds.BottomRight) != null || c.InputHitTest(bounds.BottomLeft) != null)
                            elements.Add(c);
                    }
                }
            }
            List<Stroq> uniqueStroqs = new List<Stroq>();
            foreach (Stroq s in contained)
                if (!uniqueStroqs.Contains(s))
                    uniqueStroqs.Add(s);
            bool selected = !(SetSelection(device, new SelectionObj(uniqueStroqs, elements, GeomUtils.ToPointList(bounds)))).Empty;
            if (selected)
            {
                Feedback(device).Visibility = Visibility.Visible;
                Feedback(device).UpdateLayout();
                return Feedback(device);
            }
            return null;
        }

        public delegate void SelectionTestHandler(Rct bounds, List<Stroq> contained, List<FrameworkElement> elements);
        public event SelectionTestHandler SelectionTestEvent;

        public void ProcessInkEvent(Window parWin, Pt p, Pt eventWindowScreenOrigin, bool sendDown, bool sendUp, bool draw)
        {
            double wherex = p.X - eventWindowScreenOrigin.X;
            double wherey = p.Y - eventWindowScreenOrigin.Y;
            int where = ComSupport.MakeLParam((int)wherex, (int)wherey);
            IntPtr hwnd = parWin == null ? ComSupport.GetDesktopWindow() : new WindowInteropHelper(parWin).Handle;
            if (sendDown)
                ComSupport.SendMessage(hwnd, ComSupport.WM_LBUTTONDOWN, ComSupport.MK_LBUTTON, where);
            ComSupport.SendMessage(hwnd, ComSupport.WM_MOUSEMOVE, ComSupport.MK_LBUTTON, where);
            if (sendUp)
                ComSupport.SendMessage(hwnd, ComSupport.WM_LBUTTONUP, 0, where);
            if (draw)
                //DynamicRenderer.Move(p);
            System.Windows.Forms.Cursor.Position = new System.Drawing.Point((int)p.X, (int)(p.Y));
        }
    }
}