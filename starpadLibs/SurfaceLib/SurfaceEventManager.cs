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
using System.IO;
using starPadSDK.MathExpr.ExprWPF;
using starPadSDK.MathExpr.ExprWPF.Boxes;
using starPadSDK.MathExpr;
using starPadSDK.MathRecognizer;
using starPadSDK.CharRecognizer;
using starPadSDK.AppLib;
using Constant = starPadSDK.MathExpr.Engine.Constant;
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
    public interface MultitouchPose
    {
        bool Test(InqScene page, ReadOnlyContactCollection numFingers, ContactEventArgs e);
    }
    public class SurfacePauseData : CommandSet.PauseData
    {
        public Contact ContactID { get; set; }
        public Contact PenID     { get; set; }
        public SurfacePauseData(Stroq s, Pt touch, Contact penID, Contact contactID) : base(s, touch) { 
            ContactID = contactID; 
            PenID = penID; 
        }
    }
    public class SurfaceEventManager
    {
        List<MultitouchPose>        multitouchPoses = new List<MultitouchPose>();
        public List<MultitouchPose> MultitouchPoses { get { return multitouchPoses; } }
        public SurfaceEventManager() { }
        /// <summary>
        /// tests if any multi-touch poses are recognized
        /// </summary>
        /// <param name="e"></param>
        /// <param name="page"></param>
        public void CheckForMultitouchPoses(ContactEventArgs e, InqScene page)
        {
            foreach (MultitouchPose mgest in MultitouchPoses)
            {
                if (mgest.Test(this, page, Contacts.GetContactsCaptured(page.Parent as FrameworkElement), e))
                {
                    //break;
                }
            }
        }


        /// <summary>
        /// checks if a contact occurs over an existing selection - if it is, the contact is grabbed to that selection
        /// </summary>
        /// <param name="e"></param>
        /// <param name="page"></param>
        /// <param name="area"></param>
        /// <returns></returns>
        public bool CheckIfSelectionGrabsInput(ContactEventArgs e, InqScene page, EWPF.ContactArea area)
        {
            bool overSelection = false;
            // check if the contact get grabbed by an existing selection
            foreach (KeyValuePair<object, SelectionFeedback> pair in page.Feedbacks)
                if (!pair.Value.Selection.Empty && pair.Value.InDragRgn(area.Bounds.Center))
                    if (pair.Value.Visibility == Visibility.Visible)
                    {
                        overSelection = true;
                        page.GrabSelection(page.ContactDriver.PointDevice(e.Contact.Id), pair.Value);
                        break;
                    }
            return overSelection;
        }
        /// <summary>
        /// checks for gestures that are initiated with a pen stroke and confirmed with a simultaneous, nearby non-pen contact.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="page"></param>
        /// <returns></returns>
        public bool CheckForContactPunctuatedPenGesture(ContactEventArgs e, InqScene page)
        {
            foreach (ContactDeviceDriver.ContactData cdata in page.ContactDriver.ActiveContacts)
                if (cdata.IsPen && page.GetStylusPoints(cdata.Device.DeviceUID).Count > 2)
                {
                    Contact penContact = Contacts.GetContactsCaptured(page).Single((Contact c) => c.Id == cdata.Device.DeviceUID.DeviceID);
                    StylusPointCollection spoints = page.GetStylusPoints(cdata.Device.DeviceUID);

                    //if ((((Pt)e.Contact.GetPosition(page)) - (Pt)spoints.Last()).Length < 50)
                    {
                        Stroq paused = new Stroq(spoints);
                        SurfacePauseData pdata = new SurfacePauseData(paused, e.Contact.GetPosition(page), penContact, e.Contact);
                        paused.Property[CommandSet.PauseData.PauseDataGuid] = pdata;
                        if (page.Commands.StroqPaused(paused, cdata.Device.DeviceUID))
                        {
                            e.Handled = true;
                            break;
                        }
                    }
                }
            return e.Handled;
        }
    }
}

#endif