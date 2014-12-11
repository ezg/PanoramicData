using System;
using System.Collections.Generic;
using System.Collections;
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
using starPadSDK.Inq.BobsCusps;
using starPadSDK.WPFHelp;

namespace starPadSDK.AppLib {
    /// <summary> Undo/Redo functionality </summary>
    public class UndoRedo {
        private Stack<Action> _undoStack;
        private Stack<Action> _redoStack;

        public event EventHandler StartDelete;
        public event EventHandler FinishDelete;
        public event EventHandler UndoEnabledEvent, RedoEnabledEvent, ActionEvent;
        public void SendStartDelete() {
            if (StartDelete != null)
                StartDelete(null, null);
        }
        public void SendFinishDelete() {
            if (FinishDelete != null)
                FinishDelete(null, null);
        }
        /// <summary>Gets or sets a value indicating whether Undo is currently allowed/available</summary>
        public Boolean UndoEnabled {
            get { return this.CanUndo(); }
            set {
                bool canUndo = this.CanUndo();
                if (!value)
                    _undoStack.Clear();
                if (canUndo != this.CanUndo())
                    if (UndoEnabledEvent != null)
                        UndoEnabledEvent(this, new EventArgs());
            }
        }

        /// <summary>Gets or sets a value indicating whether Redo is currently allowed/available</summary>
        public Boolean RedoEnabled {
            get { return this.CanRedo(); }
            set {
                bool canUndo = this.CanRedo();
                if (!value)
                    _redoStack.Clear();
                if (canUndo != this.CanRedo())
                    if (RedoEnabledEvent != null)
                        RedoEnabledEvent(this, new EventArgs());
            }
        }

        public UndoRedo() {
            _undoStack    = new Stack<Action>();
            _redoStack    = new Stack<Action>();

            this.Flush();
        }

        /// <summary>Add an InkAction which is capable of being Undone/Redone</summary>
        /// <param name="newAction">The new action to push onto the undo stack (Ex: StrokeAddedAction)</param>
        public void Add(Action newAction) {
            if (newAction == null)
                return;

            // Disable redo upon adding new InkActions (pretty standard behavior in undo/redo systems)
            _redoStack.Clear();
            _undoStack.Push(newAction);
            this.UpdateEnabled(); 
        }

        /// <summary>Undoes the last InkAction in the undo stack</summary>
        /// <returns>True upon success; False if there's nothing to undo or if an error occurs</returns>
        public Boolean Undo() {
            if (!this.CanUndo())
                return false;

            Action last = _undoStack.Pop();
            _redoStack.Push(last);

            last.Undo();

            this.UpdateAfterChange();
            return true;
        }

        /// <summary>Should not need to call this method; See <see cref="PopStrokeAdded"/> instead.</summary>
        public Boolean Pop() {
            if (!this.CanUndo())
                return false;

            _undoStack.Pop();
            this.UpdateEnabled();
            return true;
        }

        /// <returns>True if there is an InkAction to undo; false otherwise</returns>
        public Boolean CanUndo() {
            return (_undoStack.Count > 0);
        }

        /// <summary>Redoes the last InkAction in the redo stack</summary>
        /// <returns>True upon success; False if there's nothing to redo or if an error occurs</returns>
        public Boolean Redo() {
            if (!this.CanRedo())
                return false;

            Action last = _redoStack.Pop();
            _undoStack.Push(last);

            last.Redo();

            this.UpdateAfterChange();
            return true;
        }

        private void UpdateAfterChange() {
            this.UpdateEnabled();
            if (ActionEvent != null)
                ActionEvent(this, new EventArgs());

        }

        /// <returns>True if there is an InkAction to redo; false otherwise</returns>
        public Boolean CanRedo() {
            return (_redoStack.Count > 0);
        }

        /// <summary>
        /// Ensures that the Undo/Redo menuItems in Form1 are Enabled only when available.
        /// Also synchronizes their text to coincide with the specific Undo/Redo actions available.
        /// </summary>
        private void UpdateEnabled() {
            this.UndoEnabled = this.CanUndo();

            this.RedoEnabled = this.CanRedo();
        }

        /// <summary>Clears the Undo/Redo stacks</summary>
        public void Flush() {
            this.UndoEnabled = false;
            this.RedoEnabled = false;
        }
    }

    /// <summary>
    /// Represents an undo/redo-able change to Form1's Ink.
    /// Takes a 'Snapshot' of Form1's Ink to restore later.
    /// </summary>
    public class Action {
        public String Description { get { return _description; } }
        public byte[] SavedInk { get { return _savedInk; } }

        protected String _description;
        protected byte[] _savedInk;

        public Action() : this("Action") { }
        public Action(String description) {
            _description = description;
        }

        public virtual Boolean Undo() {
            return false;
        }

        /// <returns>true upon success, false upon failure</returns>
        public virtual Boolean Redo() {
            return true;
        }
    }
    public class WiggleAddedAction : Action {
        Wiggle _added;
        InqScene _canvas;
        public WiggleAddedAction(Wiggle w, InqScene c)  : base("Wiggle added") {
            _added = w;
            _canvas = c;
        }

        public override bool Undo() {
            _canvas.Rem(_added);

            return true;
        }

        public override bool Redo() {
            _canvas.AddNoUndo(_added);

            return true;
        }
    }
    public class InkAddedAction : Action {
        Stroq[]               _added;
        InqScene _canvas;
        public InkAddedAction(Stroq s, InqScene c) : this(new Stroq[] { s }, c) { }
        public InkAddedAction(Stroq[] s, InqScene c):base("Ink added") {
            _added = s;
            _canvas = c;
        }

        public override bool Undo() {
            foreach (Stroq s in _added)
                _canvas.Rem(s);

            return true;
        }

        public override bool Redo() {
            foreach (Stroq s in _added)
                _canvas.AddNoUndo(s);

            return true;
        }
    }
    public class ElementAddedAction : Action {
        FrameworkElement[] _added;
        InqScene _canvas;
        public ElementAddedAction(FrameworkElement e, InqScene c) : this(new FrameworkElement[] { e }, c) { }
        public ElementAddedAction(FrameworkElement[] e, InqScene c)
            : base("Element added") {
            _added = e;
            _canvas = c;
        }

        public override bool Undo() {
            foreach (FrameworkElement e in _added)
                _canvas.Rem(e);

            return true;
        }

        public override bool Redo() {
            foreach (FrameworkElement e in _added)
                _canvas.AddNoUndo(e);

            return true;
        }
    }
    public class SelectionAddedAction : Action {
        InqScene _canvas;

        InkAddedAction         _stksAdded;
        ElementAddedAction _elesAdded;

        public SelectionAddedAction(SelectionObj sel, InqScene c) : base("Selection added") {
            _canvas = c;
            _stksAdded = new InkAddedAction(sel.Strokes, c);
            _elesAdded = new ElementAddedAction(sel.Elements, c);
        }

        public override bool Undo() {
            _stksAdded.Undo();
            _elesAdded.Undo();

            return true;
        }

        public override bool Redo() {
            _elesAdded.Redo();
            _stksAdded.Redo();

            return true;
        }
    }
    public class XformAction : Action {
        SelectionObj _xformed;
        Mat          _xform;
        public XformAction(SelectionObj xformed, Mat xform, InqScene c):base("Sel transformed") {
            _xformed = xformed;
            _xform = xform;
        }
        public override bool Undo() {
            _xformed.XformBy(_xform.Inverse());
            return true;
        }
        public override bool Redo() {
            _xformed.XformBy(_xform);
            return true;
        }
    }
    public class ReplaceAction : Action {
        SelectionAddedAction _added;
        DeleteAction _deleted;
        public ReplaceAction(SelectionObj additions, SelectionObj deletions, InqScene c) : base("Replace") {
            _deleted = new DeleteAction(deletions, c);
            c.ReplaceNoUndo(deletions.Strokes, additions.Strokes);
            _added = new SelectionAddedAction(additions,c);
        }

        public override bool Undo() {
            _deleted.Undo();
            _added.Undo();

            return true;
        }

        public override bool Redo() {
            _deleted.Redo();
            _added.Redo();

            return true;
        }
    }
    public class DeleteAction : Action {
        SelectionObj        _deleted;
        InqScene _canvas;
        List<Wiggle>        _deleteWiggles = new List<Wiggle>();
        List<FrameworkElement> _elements = new List<FrameworkElement>();
        public DeleteAction(SelectionObj sel, InqScene c):base("Delete") {
            _deleted = sel;
            _canvas = c;
            _canvas.UndoRedo.SendStartDelete();
            foreach (FrameworkElement i in _canvas.SceneLayer.Children)
                _elements.Add(i);
            foreach (Stroq s in sel.Strokes) {
                Wiggle w = _canvas.Wiggle(s);
                if (w != null)
                    _deleteWiggles.Add(w);
                _canvas.Rem(s);
            }
            foreach (FrameworkElement i in sel.Elements)
                _canvas.Rem(i);
            _canvas.UndoRedo.SendFinishDelete();
        }

        public override bool Undo() {
            _canvas.UndoRedo.SendStartDelete();
            foreach (Stroq s in _deleted.Strokes)
                _canvas.AddNoUndo(s);
            foreach (FrameworkElement e in _deleted.Elements)
                _canvas.AddNoUndo(e);
            foreach (Wiggle w in _deleteWiggles)
                _canvas.AddNoUndo(w);
            _canvas.SceneLayer.Children.Clear();
            foreach (FrameworkElement i in _elements)
                _canvas.SceneLayer.Children.Add(i);
            _canvas.UndoRedo.SendFinishDelete();

            return true;
        }

        public override bool Redo() {
            _canvas.UndoRedo.SendStartDelete();
            foreach (Stroq s in _deleted.Strokes)
                _canvas.Rem(s);
            foreach (FrameworkElement e in _deleted.Elements)
                _canvas.Rem(e);
            _canvas.UndoRedo.SendFinishDelete();

            return true;
        }
    }
    public class TextEnteredAction : Action {
        string    _alternateText;
        TextBox   _box;
        InqScene _canvas;
        public TextEnteredAction(TextBox box, string original, InqScene c):base("Text") {
            _alternateText = original;
            _box = box;
            _canvas  = c;
        }

        public override bool Undo() {
            string tmp = _box.Text;
            _box.Text = _alternateText;
            _alternateText = tmp;
            if (_box.Text == "")
                _canvas.Rem(_box);
            else if (!_canvas.SceneLayer.Children.Contains(_box))
                _canvas.AddNoUndo(_box as FrameworkElement);

            return true;
        }

        public override bool Redo() {
            return Undo();
        }
    }
}
