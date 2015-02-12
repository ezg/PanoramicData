using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using starPadSDK.WPFHelp;
using PanoramicData.controller.physics;
using PanoramicData.controller.view;
using PanoramicData.utils;

namespace PanoramicData.view.vis
{
    public abstract class MovableElement : UserControl
    {
        private bool _isUnderInteraction = false;

        public MovableElementType Type { get; set; }

        public void PreTransformation()
        {
            _isUnderInteraction = true;
        }

        public void PostTransformation()
        {
            _isUnderInteraction = false;

            //PhysicsManager.Instance.UpdatePhysicalObject(this, _isUnderInteraction);
        }

        public virtual void InitPostionAndDimension(Point pos, Vector2 dim)
        {
            this.SetPosition(pos);
            this.SetSize(dim);

            PhysicsController.Instance.AddPhysicalObject(this);
            MainViewController.Instance.InkableScene.Add(this);
        }
        public abstract Point GetPosition();

        public abstract void SetPosition(Point pos);

        public abstract void SetSize(Vector2 dim);

        public abstract Vector2 GetSize();

        public abstract Vector2 GetMinSize();

        public virtual void NotifyDragStart(Point currentPos)
        {
            PhysicsController.Instance.DragStart(this, currentPos);
        }

        public virtual void NotifyDragMove(Point currentPos)
        {
            PhysicsController.Instance.DragMove(this, currentPos);
        }

        public virtual void NotifyDragEnd(Point currentPos)
        {
            PhysicsController.Instance.DragEnd(this, currentPos);
        }

        public virtual void NotifyMove(Point delta)
        {
            //Canvas.SetLeft(this, Canvas.GetLeft(this) + delta.X);
            //Canvas.SetTop(this, Canvas.GetTop(this) + delta.Y);
            //PhysicsManager.Instance.UpdatePhysicalObject(this, _isUnderInteraction);
        }


        public virtual void NotifyScale(Vector2 delta, Vector2 offset)
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
