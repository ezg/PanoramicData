#if SURFACE
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
using starPadSDK.Inq.MSInkCompat;
using System.Windows.Media;
using System.Windows.Shapes;
using starPadSDK.Inq.BobsCusps;
using starPadSDK.WPFHelp;
using System.Runtime.InteropServices;
using System.IO;
using starPadSDK.MathExpr.ExprWPF;
using starPadSDK.MathExpr.ExprWPF.Boxes;
using starPadSDK.MathExpr;
using starPadSDK.MathRecognizer;
using starPadSDK.CharRecognizer;
using starPadSDK.AppLib;
using Constant = starPadSDK.MathExpr.Engine.Constant;
using Line = System.Windows.Shapes.Line;
using Microsoft.Research.DynamicDataDisplay.Charts;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.PointMarkers;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using InputFramework.DeviceDriver;
using InputFramework.WPFDevices;
namespace starPadSDK.SurfaceLib
{
    public class SpaceMaker : PenWidget, MultitouchPose
    {
        Line resizeLine = null;
        Image resizeIcon = null;
        Contact resizeContact = null;
        Pt lastResize = new Pt();
        StroqCollection resizeSet = new StroqCollection();
        List<Contact> contacts = null;
        bool active = false;
        InqScene _page = null;

        static public BitmapSource Icon = null;
        void activate(InqScene page, Pt where1, Pt where2)
        {
            active = true;
            _page = page;
            if (Math.Abs(where1.X - where2.X) > Math.Abs(where1.Y - where2.Y))
                return;

            Pt Where = Pt.Avg(where1, where2);
            Line resizeLineWidget = new Line();
            resizeLineWidget.StrokeThickness = 2;
            resizeLineWidget.StrokeDashArray = new DoubleCollection(new double[] { 3, 3 });
            resizeLineWidget.X1 = Where.X - 25;
            resizeLineWidget.X2 = Where.X + 50;
            resizeLineWidget.Y1 = Where.Y;
            resizeLineWidget.Visibility = Visibility.Hidden;

            Image insertSpaceIcon = new Image();
            insertSpaceIcon.Stretch = Stretch.Uniform;
            insertSpaceIcon.Source = Icon;
            insertSpaceIcon.RenderTransform = new MatrixTransform(Mat.Translate(Pt.Avg(where1, where2) - new Vec(0, Icon.Height / 2)));

            resizeLineWidget.Y2 = Where.Y;
            resizeLineWidget.Stroke = Brushes.Black;
            page.Children.Add(resizeLineWidget);
            page.Children.Add(insertSpaceIcon);
            resizeLine = resizeLineWidget;
            resizeIcon = insertSpaceIcon;
            Contacts.RemovePreviewContactChangedHandler(page, trackPageContact);
            Contacts.RemovePreviewContactUpHandler(page.Parent as ScatterViewItem, upContact);
            Contacts.AddPreviewContactChangedHandler(page, trackPageContact);
            Contacts.AddPreviewContactUpHandler(page.Parent as ScatterViewItem, upContact);
        }
        void upContact(object sender, ContactEventArgs e)
        {
            if (resizeIcon == null)
                return;
            int numFingers = Contacts.GetContactsCaptured(resizeIcon).Count;
            if (numFingers < 2 && resizeLine != null)
            {
                active = false;
                _page.Children.Remove(resizeLine);
                _page.Children.Remove(resizeIcon);
                resizeLine = null;
                resizeIcon = null;
            }
        }
        void trackLineContact(object sender, ContactEventArgs e)
        {
            int numFingers = Contacts.GetContactsCaptured(resizeIcon).Count;
            if (numFingers == 2)
            {
                Pt where1 = Contacts.GetContactsCaptured(resizeIcon)[0].GetPosition(_page);
                Pt where2 = Contacts.GetContactsCaptured(resizeIcon)[1].GetPosition(_page);
                resizeIcon.RenderTransform = new MatrixTransform(Mat.Translate(Pt.Avg(where1, where2) - new Vec(0, resizeIcon.ActualHeight / 2)));
                resizeLine.Y1 = resizeLine.Y2 = (where1.Y + where2.Y) / 2;
            }
            e.Handled = true;
        }
        void trackPageContact(object sender, ContactEventArgs e)
        {
            if (resizeContact != null && e.Contact.Id == resizeContact.Id)
            {
                InqScene p = sender as InqScene;
                if (resizeLine != null)
                {
                    if (resizeLine.Visibility == Visibility.Hidden)
                    {
                        Contacts.AddPreviewContactChangedHandler(resizeIcon, trackLineContact);
                        Contacts.CaptureContact(contacts[0], resizeIcon);
                        Contacts.CaptureContact(contacts[1], resizeIcon);
                        resizeLine.Visibility = Visibility.Visible;
                        resizeLine.X2 = p.Width - 10;
                    }

                    BatchLock block = null;
                    foreach (CommandSet.CommandEditor editor in p.Commands.Editors)
                        if (editor is MathEditor)
                        {
                            block = (editor as MathEditor).RecognizedMath.BatchEdit();
                            break;
                        }
                    foreach (Stroq s in resizeSet)
                        s.Move(new Vec(0, e.GetPosition(p).Y - lastResize.Y));
                    if (block != null)
                        block.Dispose();
                    lastResize = e.GetPosition(p);
                }
                e.Handled = true;
            }
        }

        public SpaceMaker()
        {
            active = false;
        }
        public bool Grabs(Pt where, Contact contact)
        {
            resizeSet.Clear();
            if (resizeLine != null && where.X < resizeLine.X2)
            {
                foreach (Stroq s in _page.Stroqs)
                    if (s.GetBounds().Center.Y > where.Y)
                        resizeSet.Add(s);
                lastResize = where;
                resizeContact = contact;
                return true;
            }
            return false;
        }
        public bool Test(SurfaceEventManager hmath, InqScene page, ReadOnlyContactCollection numFingers, ContactEventArgs e)
        {
            hmath.ActivePenWidgets.Remove(this);
            active = false;

            if (numFingers.Count != 0 && !page.ContactDriver.IsPen(e.Contact.Id))
            {
                Pt where1 = numFingers[0].GetPosition(page);
                Pt where2 = e.Contact.GetPosition(page);
                foreach (Contact c in numFingers)
                    if (page.ContactDriver.ActiveContact(c.Id) && page.ContactDriver.IsPen(c.Id))
                        return active;
                contacts = new List<Contact>(new Contact[] { numFingers[0], e.Contact });
                if (numFingers.Count == 1 && !active)
                {
                    activate(page, where1, where2);
                    if (!hmath.ActivePenWidgets.Contains(this))
                        hmath.ActivePenWidgets.Add(this);
                }
            }
            return active;
        }
    }
}
#endif