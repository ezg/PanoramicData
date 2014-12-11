using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq;
using System.Windows.Media;
using System.Windows.Shapes;
using System.IO;
using starPadSDK.AppLib;
using starPadSDK.WPFHelp;
using starPadSDK.SurfaceLib;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using System.Windows.Media.Imaging;

namespace starPadSDK.SurfaceLib
{
    public class PageBezel
    {
        Panel _panel = null;
        FrameworkElement _eventElement;
        bool          _panning = false;
        Pt            _lastPan = new Pt();
        System.Timers.Timer waitTimer = new Timer();
        Contact bezelFinger = null;
        Contact panBarFinger = null;
        List<Contact> seen = new List<Contact>();
        public delegate void PageDragInHdlr(Contact c, Contact c2);
        public event PageDragInHdlr PageDragInEvent;
        public event PageDragInHdlr PagePanBarEvent;
        /// <summary>
        /// creates a palmPrint object that monitors Contact events on 'eventElement' and displays feedback in 'displayPanel'
        /// when a finger press is detected, a FingerPressEvent is generated
        /// </summary>
        /// <param name="eventElement"></param>
        /// <param name="displayPanel"></param>
        public PageBezel(FrameworkElement eventElement, Panel displayPanel)
        {
            _eventElement = eventElement;
            _panel = displayPanel;
            Contacts.AddPreviewContactDownHandler(eventElement, windowContact);
            Contacts.AddPreviewContactUpHandler(eventElement, windowUpContact);
            Contacts.AddPreviewContactChangedHandler(eventElement, windowMoveContact);
            waitTimer.Interval = 200;
            waitTimer.Elapsed += new ElapsedEventHandler(waitTimer_Elapsed);
        }
        void waitTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            (sender as Timer).Stop();
            _panning = true;
            _lastPan = new Pt();
        }
        void windowUpContact(object sender, ContactEventArgs e)
        {
            if (e.Contact == bezelFinger)
            {
                _panning = false;
                bezelFinger = null;
            }
            if (e.Contact == panBarFinger)
                panBarFinger = null;
        }
        public delegate void PageBezelPanMoveHandler(Vec PanBy);
        public event PageBezelPanMoveHandler PageBezelPanMoveEvent;
        void windowMoveContact(object sender, ContactEventArgs e)
        {
            if (!seen.Contains(e.Contact))
                windowContact(sender, e);
            if (_panning && e.Contact == bezelFinger)
            {
                if (_lastPan != new Pt())
                    if (PageBezelPanMoveEvent != null)
                        PageBezelPanMoveEvent(-((Pt)e.GetPosition(_eventElement) - _lastPan));
                _lastPan = e.GetPosition(_eventElement);
            }
        }

        void windowContact(object sender, ContactEventArgs e)
        {
            try { Point x = panBarFinger == null ? new Point() :panBarFinger.GetCenterPosition(_eventElement); } catch (Exception) {  panBarFinger = null; }
            try { Point x = bezelFinger == null ? new Point() : bezelFinger.GetCenterPosition(_eventElement); }
            catch (Exception) { bezelFinger = null; }
            try
            {
                seen.Add(e.Contact);
                if (e.GetPosition(_eventElement).X > 980 || e.GetPosition(_eventElement).X < 24 ||
                    e.GetPosition(_eventElement).Y > 744)
                    if (panBarFinger != null &&
                        Math.Abs(panBarFinger.GetPosition(_eventElement).Y - e.GetPosition(_eventElement).Y) < 100)
                    {
                        waitTimer.Stop();
                        panBarFinger = null;
                        bezelFinger = null;
                        if (PagePanBarEvent != null)
                        {
                            PagePanBarEvent(e.Contact, bezelFinger);
                            e.Handled = true;
                        }
                    } else if (bezelFinger != null &&
                        Math.Abs(bezelFinger.GetPosition(_eventElement).X - e.GetPosition(_eventElement).X) < 100)
                    {
                        waitTimer.Stop();
                        _panning = false;
                        if (PageDragInEvent != null)
                        {
                            PageDragInEvent(e.Contact, bezelFinger);
                            e.Handled = true;
                        }
                        bezelFinger = null;
                        panBarFinger = null;
                    }
                    else
                    {
                        waitTimer.Start();
                        if (e.GetPosition(_eventElement).Y > 744)
                            panBarFinger = e.Contact;
                        if (e.GetPosition(_eventElement).X > 980 || e.GetPosition(_eventElement).X < 24)
                            bezelFinger = e.Contact;
                        e.Handled = true;
                    }
            }
            catch (Exception)
            {
                panBarFinger = null;
                bezelFinger = null;
            }
        }
    }
}
