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
using PanoramicData.controller.view;

namespace PanoramicData.view.filter
{
    public abstract class MovableElement : UserControl
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
            this.SetSize(dim);

            PhysicsController.Instance.AddPhysicalObject(this);
            MainViewController.Instance.InkableScene.Add(this);
        }
        public abstract Pt GetPosition();

        public abstract void SetPosition(Pt pos);

        public abstract void SetSize(Vec dim);

        public abstract Vec GetSize();

        public abstract Vec GetMinSize();

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

        public virtual void FlipSides()
        {
        }
        
    }


    public enum MovableElementType
    {
        Rect, Circle
    }
}
