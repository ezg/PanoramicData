using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using DiagramDesigner;
using starPadSDK.Geom;
using starPadSDK.WPFHelp;
using PanoramicData.controller.physics;

namespace PanoramicData.view.filter
{
    public class MovableElement : UserControl
    {
        private bool _isUnderInteraction = false;

        public MovableElementType Type { get; set; }

        public bool HasEnclosedAnchor { get; set; }

        public void PreTransformation()
        {
            _isUnderInteraction = true;
        }

        public void PostTransformation()
        {
            _isUnderInteraction = false;

            //PhysicsManager.Instance.UpdatePhysicalObject(this, _isUnderInteraction);
        }

        public virtual void InitPostionAndDimension(Pt pos, Vec dim)
        {
            this.SetPosition(pos);
            this.SetDimension(dim);

            PhysicsController.Instance.AddPhysicalObject(this);
            PhysicsController.InqScene.AddNoUndo(this);
        }

        public virtual void SetPosition(Pt pos)
        {
            Canvas.SetLeft(this, pos.X);
            Canvas.SetTop(this, pos.Y);
        }

        public virtual void SetCenter(Pt pos)
        {
            Canvas.SetLeft(this, pos.X - this.Width / 2.0);
            Canvas.SetTop(this, pos.Y - this.Height / 2.0);
        }

        public virtual Pt GetPosition()
        {
            return new Pt(Canvas.GetLeft(this), Canvas.GetTop(this));
        }

        public virtual Pt GetCenter()
        {
            return new Pt(Canvas.GetLeft(this) + this.Width / 2.0, Canvas.GetTop(this) + this.Height / 2.0);
        }

        public virtual void SetDimension(Vec dim)
        {
            this.Width = dim.X;
            this.Height = dim.Y;
        }

        public virtual void NotifyDragStart(Pt currentPos)
        {
            PhysicsController.Instance.DragStart(this, currentPos);
        }

        public virtual void NotifyDragMove(Pt currentPos)
        {
            PhysicsController.Instance.DragMove(this, currentPos);
        }

        public virtual void NotifyDragEnd(Pt currentPos)
        {
            PhysicsController.Instance.DragEnd(this, currentPos);
        }

        public virtual void NotifyMove(Pt delta)
        {
            //Canvas.SetLeft(this, Canvas.GetLeft(this) + delta.X);
            //Canvas.SetTop(this, Canvas.GetTop(this) + delta.Y);
            //PhysicsManager.Instance.UpdatePhysicalObject(this, _isUnderInteraction);
        }


        public virtual void NotifyScale(Vec delta, Vec offset)
        {
            this.Width = this.Width*delta.X;
            this.Height = this.Height*delta.Y;
            PhysicsController.Instance.UpdatePhysicalObject(this, _isUnderInteraction);
        }

        public virtual void NotifyRotate(double delta)
        {
        }

        public virtual void NotifyInteraction()
        {
        }

        public virtual Vec GetSize()
        {
            return new Vec(this.Width, this.Height);
        }

        public virtual Vec GetMinSize()
        {
            return new Vec(this.MinWidth, this.MinHeight);
        }

        public virtual void FlipSides()
        {
        }
        
    }


    public enum MovableElementType
    {
        Rect, Circle
    }
}
