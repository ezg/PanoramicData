using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using starPadSDK.AppLib;
using starPadSDK.Inq;
using starPadSDK.Geom;
using System.Windows;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using InputFramework.WPFDevices;
using InputFramework.DeviceDriver;

namespace starPadSDK.SurfaceLib
{
    public class SurfacePauseData : CommandSet.PauseData
    {
        public WPFPointDevice ContactDev   { get; set; }
        public WPFPointDevice PenDev       { get; set; }
        public SurfacePauseData(Stroq s, Pt touch, WPFPointDevice pen,
                                WPFPointDevice contact) : base(s, touch)
        {
            ContactDev = contact;
            PenDev     = pen;
        }
    }
    public class TimestampedHover
    {
        public DateTime when;
        public Rect where;
        public TimestampedHover(DateTime When, Rect Where) { when = When; where = Where; }
    }
    /// <summary>
    /// Identifies if a Contact came from a stylus and stores that with the Contact.
    /// Creates a virtual PointDevice for the Contact.
    /// Provides an API for generating Point events for Contact events.
    /// 
    /// Due to various system lags, the first few movement locations after a contact is made are not reported.
    /// So we "recover" these lost movements by tracking movement from the raw camera display and saving that 
    /// movement with a time index.  Then when we find out that a pen contact has occurred, we make all the missing
    /// events between the start of the contact and the time it is processed available as LostHovers
    /// </summary>
    public class SurfaceContactDeviceDriver : PointDeviceDriver
    {
        Window                         mWindow = null;
        Dictionary<long, ContactData> _data = new Dictionary<long, ContactData>();

        void initPointEvent(ref PointEventArgs pointEventArgs, Contact contact, Point windowPoint)
        {
            pointEventArgs.DeviceUID = GetUID(contact);
            pointEventArgs.DeviceType = DeviceType.Unknown;
            pointEventArgs.Handled = false;

            pointEventArgs.InBetweenPointsScreen = new PressurePoint[1];
            pointEventArgs.InBetweenPointsScreen[0] = new PressurePoint(mWindow.PointToScreen(windowPoint));
            // Mouse cursor position in screen coordinates
            pointEventArgs.PointScreen = new PressurePoint(mWindow.PointToScreen(windowPoint));
        }

        public class ContactDeviceUID : DeviceUID
        {
            public Contact Contact;
        }

        ContactDeviceUID GetUID(Contact contact)
        {
            return new ContactDeviceUID()
            {
                Contact = contact,
                DeviceID = contact.Id,
                DeviceDriverID = this.DeviceDriverID
            };
        }
        void testForPen(object sender, ContactEventArgs e)
        {
            List<TimestampedHover> lost;
            bool penInput = Hover.HoverInTimeSpaceRnage(e.GetPosition(MWindow), DateTime.Now, 15, 500, out lost);
            if (System.Windows.Input.Keyboard.Modifiers != System.Windows.Input.ModifierKeys.None)
                penInput = true;
            RaiseDeviceAdded(e.Contact);
            SetLostHovers(e.Contact.Id, lost);
            SetDeviceType(e.Contact, penInput ? DeviceType.Stylus : DeviceType.MultiTouch);
        }

        public class ContactData
        {
            public WPFPointDevice         Device     { get; set; }
            public List<TimestampedHover> LostHovers { get; set; }
            public DeviceType             DeviceType { get; set; }
            public ContactData(WPFPointDevice dev) { Device = dev; DeviceType = DeviceType.Unknown; LostHovers = new List<TimestampedHover>(); }
        }

        public void RaiseDeviceAdded(Contact contact) { OnAddDeviceEvent(GetUID(contact)); }
        public void RaiseDeviceRemoved(Contact contact) { OnRemoveDeviceEvent(GetUID(contact)); }

        public bool RaisePointDown(Object sender, Contact contact)
        {
            var lost = contact.LostHovers();
            Point windowPoint = contact.DevType() == DeviceType.Stylus && lost.Count > 0 ? ((Rct)lost[0].where).Center : (Pt)contact.GetPosition(MWindow);
            PointEventArgs pointEventArgs = new PointEventArgs();

            initPointEvent(ref pointEventArgs, contact, windowPoint);

            pointEventArgs.DeviceType = contact.DevType();
            pointEventArgs.PointEventType = PointEventType.LeftDown;

            OnPointEvent(pointEventArgs);

            if (pointEventArgs.Handled) {
                if (contact.Device().Captured != null)
                    Contacts.CaptureContact(contact, sender as FrameworkElement);
            } 

            return pointEventArgs.Handled;
        }

        public bool RaisePointUp(Object sender, Contact contact)
        {
            PointEventArgs pointEventArgs = new PointEventArgs();

            initPointEvent(ref pointEventArgs, contact, contact.GetPosition(MWindow));

            pointEventArgs.DeviceType = contact.DevType();
            pointEventArgs.PointEventType = PointEventType.LeftUp;

            OnPointEvent(pointEventArgs);

            RaiseDeviceRemoved(contact);
            return pointEventArgs.Handled;
        }

        public bool RaisePointMove(Object sender, Contact contact)
        {
            if (!contact.Valid()) {
                RaiseDeviceAdded(contact);
                RaisePointDown(sender, contact);
            }
            bool handled = false;
            if (contact.LostHovers().Count == 0) {
                Pt p = contact.GetPosition(MWindow); ;
                handled = RaisePointMove(this, contact, p, true);
            } else if (contact.DevType() == DeviceType.Stylus) {
                foreach (TimestampedHover thover in contact.LostHovers())  {
                    Pt p = ((Rct)thover.where).Center;
                    handled = RaisePointMove(this, contact, p, true);
                }
            }
            contact.LostHovers().Clear();
            return handled;
        }
        public bool RaisePointMove(Object sender, Contact contact, Point windowPoint, bool drag)
        {
            PointEventArgs pointEventArgs = new PointEventArgs();

            initPointEvent(ref pointEventArgs, contact, windowPoint);

            pointEventArgs.DeviceType = contact.DevType();
            if (drag)
            {
                pointEventArgs.PointEventType = PointEventType.Drag;
            }
            else
            {
                pointEventArgs.PointEventType = PointEventType.Move;
            }

            OnPointEvent(pointEventArgs);

            return pointEventArgs.Handled;
        }

        public bool                   ActiveContact(int contactID) { return _data.ContainsKey(contactID); }
        public void                   SetDeviceType(Contact contact, DeviceType dtype) { _data[contact.Id].DeviceType = dtype; }
        public DeviceType             DevType(Contact contact)  { return _data[contact.Id].DeviceType; }
        public DeviceType             DevType(WPFPointDevice d) {
            foreach (KeyValuePair<long, ContactData> pair in _data)
                if (pair.Value.Device == d)
                    return pair.Value.DeviceType;
            return DeviceType.Unknown;
        }
        public WPFPointDevice         PointDevice(long contactID)
        {
            return _data.ContainsKey(contactID) ? _data[contactID].Device : null;
        }
        public bool                   Valid(int contactID) { return _data.ContainsKey(contactID);  }
        public List<TimestampedHover> LostHovers(int contactID)
        {
            return _data[contactID].LostHovers;
        }
        public void                   SetLostHovers(int contactID, List<TimestampedHover> lost)
        {
            _data[contactID].LostHovers = lost;
        }
        public SurfaceContactDeviceDriver(SurfaceWindow window, WPFDeviceManager manager)
        {
            mWindow = window;
            manager.WPFPointDeviceAdded += new WPFDeviceManager.WPFPointDeviceAddedHandler((object sender, WPFPointDevice newDev) =>
            {
                _data.Add(newDev.DeviceUID.DeviceID, new ContactData(newDev));
            });
            manager.WPFPointDeviceRemoved += new WPFDeviceManager.WPFPointDeviceRemovedHandler((object sender, WPFPointDevice newDev) =>
            {
                if (_data.ContainsKey(newDev.DeviceUID.DeviceID))
                    _data.Remove(newDev.DeviceUID.DeviceID);
            });
            SurfaceContactExt.Driver = this;
            Hover = new HoverManager(window);
            Hover.StartHover(null);
            Hover.Window = window;
            Contacts.AddPreviewContactDownHandler(mWindow, (object sender, ContactEventArgs e) => { testForPen(sender, e);  if (RaisePointDown(mWindow, e.Contact)) e.Handled = true; });
            Contacts.AddPreviewContactChangedHandler(mWindow, (object sender, ContactEventArgs e) => { if (RaisePointMove(mWindow, e.Contact)) e.Handled = true; });
            Contacts.AddPreviewContactUpHandler(mWindow, (object sender, ContactEventArgs e) => { if (RaisePointUp(mWindow, e.Contact))   e.Handled = true; });
        }

        public double GetHoverIntensity(Rect area, Rect skip) { return Hover.GetHoverIntensity(area, skip); }
        public HoverManager             Hover   { get; set; }
        public Window                   MWindow { get { return mWindow; } }
        public IEnumerable<ContactData> ActiveContacts
        {
            get
            {
                return _data.Select<KeyValuePair<long, ContactData>, ContactData>(
                           (KeyValuePair<long, ContactData> pair) => pair.Value);
            }
        }

        public override void Dispose() { }
    }
    static public class SurfaceContactExt {
        static public SurfaceContactDeviceDriver Driver = null;
        static public Contact                Contact(this WPFPointDevice d) {
            if (d.DeviceUID is SurfaceContactDeviceDriver.ContactDeviceUID)
                return (d.DeviceUID as SurfaceContactDeviceDriver.ContactDeviceUID).Contact;
            return null;      
        }
        static public WPFPointDevice         Device(this Contact c)     { return Driver.PointDevice(c.Id); }
        static public DeviceType             DevType(this Contact c)    { return Driver.DevType(c); }
        static public List<TimestampedHover> LostHovers(this Contact c) { return Driver.LostHovers(c.Id); }
        static public bool                   Valid(this Contact c)      { return Driver.Valid(c.Id); }
        public static ContactArea            Area(this Contact e, FrameworkElement frozenVisual)
        {
            double majorAxis, minorAxis, orientation;
            e.GetEllipse(frozenVisual, out majorAxis, out minorAxis, out orientation);
            Rct ellipseRct = new Rct(new Pt(), new Vec(majorAxis, minorAxis));
            Vec offset = new Vec(majorAxis / 2, minorAxis / 2);
            Pt center = e.GetCenterPosition(frozenVisual);
            Pt[] pts = new Pt[4];
            pts[0] = (ellipseRct.TopLeft - offset);
            pts[1] = (ellipseRct.TopRight - offset);
            pts[2] = (ellipseRct.BottomLeft - offset);
            pts[3] = (ellipseRct.BottomRight - offset);

            Mat matrix = Mat.Rotate((Deg)orientation) * Mat.Translate(center);
            return new ContactArea(new List<Pt>(pts), matrix);
        }

        public static ContactArea            Area(this Contact e1, Contact e2, FrameworkElement frozenVisual)
        {
            Pt center1 = e1.GetCenterPosition(frozenVisual);
            Pt center2 = e2.GetCenterPosition(frozenVisual);
            //create the big rct
            double majorAxis1, minorAxis1, orientation1, majorAxis2, minorAxis2, orientation2;
            e1.GetEllipse(frozenVisual, out majorAxis1, out minorAxis1, out orientation1);
            e2.GetEllipse(frozenVisual, out majorAxis2, out minorAxis2, out orientation2);
            Rct ellipseRct1 = new Rct(center1, new Vec(majorAxis1, minorAxis1));
            Rct ellipseRct2 = new Rct(center2, new Vec(majorAxis2, minorAxis2));
            Rct ellipseRct = ellipseRct1.Union(ellipseRct2);

            //new center position
            Pt center = ellipseRct.Center;
            ellipseRct = ellipseRct.Translated(new Pt() - center);

            //find out new major and minor axis
            double majorAxis, minorAxis;
            minorAxis = Math.Min(Math.Abs(center1.X - center2.X), Math.Abs(center1.Y - center2.Y));
            majorAxis = Math.Max(Math.Abs(center1.X - center2.X), Math.Abs(center1.Y - center2.Y));

            //offset the center position
            Vec offset = new Vec();// new Vec(majorAxis / 2, minorAxis / 2);
            Pt[] pts = new Pt[4];
            pts[0] = (ellipseRct.TopLeft - offset);
            pts[1] = (ellipseRct.TopRight - offset);
            pts[2] = (ellipseRct.BottomLeft - offset);
            pts[3] = (ellipseRct.BottomRight - offset);

            //get average orientation
            double orientation = (orientation1 + orientation2) / 2;

            //transform to the correct place
            Mat matrix = Mat.Rotate((Deg)0) * Mat.Translate(center);
            return new ContactArea(new List<Pt>(pts), matrix);
        }
    }
}
