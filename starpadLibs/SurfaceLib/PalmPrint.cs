using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Ink;
using System.Windows.Input;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq;
using starPadSDK.AppLib;
using starPadSDK.WPFHelp;
using starPadSDK.SurfaceLib;
using Microsoft.Surface;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using InputFramework.WPFDevices;


namespace starPadSDK.SurfaceLib
{
    public class PalmPrint
    {
        const double PalmShrink = .75;
        const double PalmEnlarge = 1.25; 
  
        Panel               _panel = null;
        SideToolbar         _sideTools = new SideToolbar();
        FrameworkElement    _eventElement = null;
        DateTime  _palmDownTime = DateTime.MinValue;
        Pt        _palmCenter = new Pt(); 
        Vec[]     _fingerLocations = new Vec[5];
        List<Contact>               _palmContacts = new List<Contact>();
        SortedList<double, Contact> _sortedCurrentFingerContacts = new SortedList<double, Contact>();
        Contact[]                   _currentFingerContacts = new Contact[5]; 
        List<Canvas>    fingerImages = new List<Canvas>();
        List<Canvas>    fingerTargets = new List<Canvas>();
        Canvas[]        _activeFingerDisplays = new Canvas[5];
        BitmapImage[]   _chordingIcons;    
        string[]        _captions = new string[5];   
        bool[]      _flagActivated = new bool[5]; 
        bool[]      _flagUp = new bool[5]; 
        bool        _palmShown = false;
        bool        _palmActive = false;
        bool        _fingersRegistered = false;
        int         _numUp = 0; 
        public LinkedList<PalmFinger>   Fingers = new LinkedList<PalmFinger>();
        public static PalmPrint         CurrentInstance = null;

        /** Constructor: Instantiates variables, adds contact handlers, displays palm and finger graphics.
         * */
        public PalmPrint(FrameworkElement eventElement, Panel displayPanel)
        {
            CurrentInstance = this;
            _sideTools = new SideToolbar();
            _sideTools.Visibility = Visibility.Collapsed;
            displayPanel.Children.Add(_sideTools);
            _eventElement = eventElement;
            _panel = displayPanel;
            eventElement.AddHandler(WPFPointDevice.PreviewPointDownEvent, new RoutedPointEventHandler(windowContact));
            eventElement.AddHandler(WPFPointDevice.PreviewPointUpEvent, new RoutedPointEventHandler(windowUpContact));
            eventElement.AddHandler(WPFPointDevice.PreviewPointDragEvent, new RoutedPointEventHandler(windowMoveContact));
            InstantiateIcons();

            // Display icons above each finger corresponding to finger's functionality.
            for (int i = 0; i < 5; i++)
            {
                PalmFinger pf = createPalmFinger(i, true);
                PalmFinger pf2 = createPalmFinger(i, false);
                pf2.Tag = i;
                switch (i)
                {
                    case 0: _sideTools.finger1.Children.Add(pf2.Parent as FrameworkElement); break;
                    case 1: _sideTools.finger2.Children.Add(pf2.Parent as FrameworkElement); break;
                    case 2: _sideTools.finger3.Children.Add(pf2.Parent as FrameworkElement); break;
                    case 3: _sideTools.finger4.Children.Add(pf2.Parent as FrameworkElement); break;
                    case 4: _sideTools.finger5.Children.Add(pf2.Parent as FrameworkElement); break;
                }
                Contacts.AddContactDownHandler(pf2, fingerToolbarClicked);
                Fingers.AddLast(pf);
                // Initialize flags as false
                _flagActivated[i] = false;
                _flagUp[i] = false;
                //_isFunctionActive[i] = false;
            }

            // Display arrows under user's fingertips for all _activeFingers
            for (int i = 0; i < _activeFingerDisplays.Length; i++)
            {
                PalmFinger pf = new PalmFinger();
                pf.Icon.Source = global::SurfaceLib.Properties.Resources.arrow3D.LoadImage();
                Canvas container = new Canvas();
                container.Children.Add(pf);
                container.Tag = i;
                Grid.SetColumn(container, 0);
                Grid.SetRow(container, 0);
                _panel.Children.Add(container);
                container.Visibility = Visibility.Hidden;
                _activeFingerDisplays[i] = container;

                pf.RenderTransform = new ScaleTransform(0.5, 0.5);
            }

            // Display palm image (a star for now)
            Image palm = new Image();
            palm.Source = global::SurfaceLib.Properties.Resources.palmStar.LoadImage();
            palm.Tag = 6;
            Canvas p_container = new Canvas();
            p_container.Children.Add(palm);
            Grid.SetColumn(p_container, 0);
            Grid.SetRow(p_container, 0);
            p_container.Visibility = Visibility.Collapsed;
            p_container.Tag = 6;
            fingerImages.Add(p_container);
            _panel.Children.Add(p_container);

            // Add customize button to palm image

            PalmPrintCustomize _CustomizeUI = new PalmPrintCustomize();
            p_container.Children.Add(_CustomizeUI);

        }

        bool between(double num, double min, double max)
        {
            return num < max && num > min;
        }

        /* Returns true if the finger was (a) close enough to palm to be considered a palm finger, 
         * (b)not already in collection of current finger contacts, and (c) was properly added to current 
         * finger collections.
         * Returns false if the contact was already in _sortedCurrentFingerContacts
         * */
        bool addFingerContact(Contact c)
        {
            //If it's close enough to the palm to be a finger from the same hand:
            Pt CurPalm = Pt.Avg(_palmContacts.Select((Contact i) => (Pt)i.GetCenterPosition(_panel)).ToArray());
            if (_sortedCurrentFingerContacts.Count < 5 && (between((((Pt)c.GetPosition(_panel)) - _palmCenter).Length, 50, 275) || between((((Pt)c.GetPosition(_panel)) - CurPalm).Length, 50, 275)))
            {
                if (!addToSorted(c))
                    return false;

                bool needToFire = (_palmActive || _fingersRegistered) && _palmShown;
                int index = -1;

                // This is a first-time registration of a user's PalmPrint, or a user has tapped fingers
                if (_sortedCurrentFingerContacts.Count == 5 || needToFire) {
                    if (_palmContacts.Count > 0)
                        _palmCenter = Pt.Avg(_palmContacts.Select((Contact i) => (Pt)i.GetCenterPosition(_panel)).ToArray());
                    _fingersRegistered = true;

                    index = currentIndexAdded(c);
                    if (index < 0)
                        return false;

                    if (_palmContacts.Count == 0)
                        return false;

                    /* If the palm and some fingers have already been registered, the user has tapped this 
                     * finger to trigger a function.*/
                    if (needToFire)
                        chord(index);
                }
                return _palmActive;
            }
            return false;
        }

        /** Returns true if contact c was added (i.e. not already in) _sortedCurrentFingerContacts.
         * Returns false if it was already in that hashmap.
         * */
        public bool addToSorted(Contact c)
        {
            if (_sortedCurrentFingerContacts.ContainsValue(c))
                return false;

            Pt CurPalm = Pt.Avg(_palmContacts.Select((Contact i) => (Pt)i.GetCenterPosition(_panel)).ToArray());
            LnSeg l = new LnSeg(CurPalm + new Vec(0, -10), CurPalm);
            Rad ang = l.SignedAngle(new LnSeg(c.GetPosition(_panel), CurPalm));

            if (_sortedCurrentFingerContacts.ContainsKey(ang))
                _sortedCurrentFingerContacts[ang] = c;
            else _sortedCurrentFingerContacts.Add(ang, c);

            return true;
        }

        /** Adds the finger contact c to _currentFingerContacts. 
         * Note: If the contact is found to be closer to the palm than to any other finger, then this returns -1.
         * Returns: The index of the finger (0 for leftmost thru 4 for rightmost).
         * */
        public int currentIndexAdded(Contact c)
        {
            int whichFinger = -1;
            int f = 0;

            if (_sortedCurrentFingerContacts.Count == 5)
            {
                for (int i = 0; i < 5; i++)
                    _currentFingerContacts[i] = null;
                for (int i = 0; i < 5; i++)
                {
                    if (_sortedCurrentFingerContacts.Values[i] == c)
                        whichFinger = i;
                    _currentFingerContacts[f++] = _sortedCurrentFingerContacts.Values[i];
                    // Update _fingerLocations with new contact point.
                    _fingerLocations[i] = (Pt)_currentFingerContacts[i].GetPosition(_panel) - _palmCenter;
                }
            }
            else
            {
                double nearest = double.MaxValue;
                for (int i = 0; i < 5; i++)
                    if (_currentFingerContacts[i] == null && (_fingerLocations[i] + _palmCenter - (Pt)c.GetPosition(_panel)).Length < nearest)
                    {
                        nearest = (_fingerLocations[i] + _palmCenter - (Pt)c.GetPosition(_panel)).Length;
                        whichFinger = i;
                    }
                _currentFingerContacts[whichFinger] = c;

                // closer to palm than to any finger - no good.
                if (_palmContacts.Count > 0 && (_palmCenter - (Pt)c.GetPosition(_panel)).Length < nearest)
                    whichFinger = -1;
            }

            return whichFinger;
        }

        /** Activates FingerPressEvents based on chording logic (up flags and activated flags).
         * 
         * Process:
         * If one finger contact came down, but there was more than one finger lifted, this method
         * assumes that the rest of the lifted fingers will come down eventually. Activates down 
         * contact function for index finger, as well as all other up contact functions. 
         * If the index corresponds to a function that was already activated (not the first finger 
         * to make a down contact), treats it as a null action.
         * 
         * Note: Another approach might be to use a timer, if users tend to lift fingers they don't
         * mean to activate.
         * */
        public void chord(int index)
        {
            int numActivated = countNumActivated();
            // Case 1: Single finger chord */
            if (_numUp == 1)
            {
                if (FingerPressEvent != null)
                    FingerPressEvent(index, _currentFingerContacts, true);
                for (int i = 0; i < 5; i++)
                {
                    _flagActivated[i] = false;
                    _flagUp[i] = false;
                }
                _numUp = 0;
            }

            // Case 2: First of a multi-finger chord
            if (_numUp > 1 && numActivated == 0)
            {
                _flagActivated[index] = false;
                for (int i = 0; i < 5; i++)
                    if (_currentFingerContacts[i] == null && _flagUp[i])
                        _flagActivated[i] = true;
                if (FingerPressEvent != null)
                    FingerPressEvent(index, _currentFingerContacts, true);
            }

            // Case 3: Not first of a multi-finger chord (corresponding function already activated)
            if (_numUp > 1 && numActivated > 0)
            {
                _flagActivated[index] = false;
                if (numActivated == 0 && _sortedCurrentFingerContacts.Count == 5)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        _flagActivated[i] = false;
                        _flagUp[i] = false;
                    }
                    _numUp = 0;
                }
            }
        }

        /** Iterates through _flagActivated, incrementing counter if flag is true.
         * Returns the number of finger functions that were activated by a chording gesture.
         * */
        public int countNumActivated()
        {
            int numActivated = 0;
            for (int i = 0; i < 5; i++)
                if (_flagActivated[i])
                    numActivated += 1;
            return numActivated;
        }

        // Refactored 4/20/10
        public delegate void FingerPressHdlr(int finger, Contact[] fingers, bool isChording);

        public event FingerPressHdlr FingerPressEvent;
        /// <summary>
        /// creates a palmPrint object that monitors Contact events on 'eventElement' and displays feedback in 'displayPanel'
        /// when a finger press is detected, a FingerPressEvent is generated
        /// </summary>
        /// <param name="eventElement"></param>
        /// <param name="displayPanel"></param>
        /// 

        public PalmFinger GetFingerByName(string name)
        {
            foreach (PalmFinger pf in Fingers)
                if ((string)pf.Caption == name)
                    return pf;
            return null;
        }

        void fingerToolbarClicked(object sender, ContactEventArgs e)
        {
            if (FingerPressEvent != null)
            {
                ((_sideTools.finger1.Children[0] as Canvas).Children[0] as PalmFinger).ModeActive = (int)(sender as PalmFinger).Tag == 0;
                ((_sideTools.finger2.Children[0] as Canvas).Children[0] as PalmFinger).ModeActive = (int)(sender as PalmFinger).Tag == 1;
                ((_sideTools.finger3.Children[0] as Canvas).Children[0] as PalmFinger).ModeActive = (int)(sender as PalmFinger).Tag == 2;
                ((_sideTools.finger4.Children[0] as Canvas).Children[0] as PalmFinger).ModeActive = (int)(sender as PalmFinger).Tag == 3;
                ((_sideTools.finger5.Children[0] as Canvas).Children[0] as PalmFinger).ModeActive = (int)(sender as PalmFinger).Tag == 4;
                FingerPressEvent((int)(sender as PalmFinger).Tag, new Contact[] { }, false);
            }
        }

        PalmFinger createPalmFinger(int i, bool add)
        {
            PalmFinger pf = new PalmFinger();
            pf.Icon.Source = _chordingIcons[i];
            Canvas container = new Canvas();
            container.Children.Add(pf);
            container.Tag = -1;
            Grid.SetColumn(container, 0);
            Grid.SetRow(container, 0);
            //container.Width = cvh.Width;
            //container.Height = cvh.Height;
            if (add)
            {
                container.Visibility = Visibility.Collapsed;
                fingerImages.Add(container);
                _panel.Children.Add(container);
            }

            pf.Caption = _captions[i];
            return pf;
        }

        public void InstantiateIcons()
        {
            _chordingIcons = new BitmapImage[5]; //can be changed later when actual chording is supported
            _chordingIcons[0] = global::SurfaceLib.Properties.Resources.redPen.LoadImage();
            _captions[0] = "Red Pen";

            _chordingIcons[1] = global::SurfaceLib.Properties.Resources.greenPen.LoadImage();
            _captions[1] = "Green Pen";

            _chordingIcons[2] = global::SurfaceLib.Properties.Resources.bluePen.LoadImage();
            _captions[2] = "Blue Pen";

            _chordingIcons[3] = global::SurfaceLib.Properties.Resources.mathMode.LoadImage();
            _captions[3] = "Math Mode";

            _chordingIcons[4] = global::SurfaceLib.Properties.Resources.writeMode.LoadImage();
            _captions[4] = "Lookup Mode";
        }

        // Refactored 4/20/10 ~ alex
        void windowUpContact(object sender, RoutedPointEventArgs e)
        {
            try
            {
                Contact c = e.WPFPointDevice.Contact();
                //if (DateTime.Now.Subtract(_palmDownTime).TotalMilliseconds < 300)
                //    if (_palmContacts.Count == 0 || ((Pt)_palmContacts[0].GetCenterPosition(_panel) - _palmCenter).Length > 200)
                //        return;

                // Case 1: palm contact
                if (_palmContacts.Contains(c)) {
                    _palmContacts.Remove(c);
                    palmUpContact();
                }
                // Case 2: Palm-finger contact
                else if (_sortedCurrentFingerContacts.ContainsValue(c))
                {
                    _sortedCurrentFingerContacts.RemoveAt(_sortedCurrentFingerContacts.IndexOfValue(c));
                    for (int i = 0; i < 5; i++)
                    {
                        if (_currentFingerContacts[i] != null && _currentFingerContacts[i].Id == c.Id)
                            setFingerUpFlags(i);
                    }
                }
            }
            catch (Exception) { return; }
        }

        /** Called from windowUpContact when contact was from palms collection.
         * Displays/undisplays palm graphics as needed. Sets active flags for palm.
         * */
        public void palmUpContact()
        {
            // No more palm contacts
            if (_palmContacts.Count == 0)
            {
                // Graphics
                foreach (Canvas finger in fingerImages)
                    finger.Visibility = Visibility.Collapsed;
                if (_palmShown)
                {
                    FrameworkElement palmImage = fingerImages[5].Children[0] as FrameworkElement;
                    //_sideTools.Visibility = Visibility.Visible;
                    _sideTools.RenderTransform = new MatrixTransform(Mat.Translate(_palmCenter - new Vec(500, 400)));
                }
                // Logic
                for (int i = 0; i < 5; i++)
                {
                    _currentFingerContacts[i] = null;
                    _flagUp[i] = false;
                    _flagActivated[i] = false;
                }
                _sortedCurrentFingerContacts.Clear();
                _numUp = 0;
                _palmShown = false;
                _palmActive = false;
                _fingersRegistered = false;
            }
            // Still have some palm contacts
            else if (_palmShown)
                ShowFeedback();
        }

        /** Called when finger i has been lifted. Flagged for chording.
         * */
        public void setFingerUpFlags(int i)
        {
            _flagUp[i] = true;
            _numUp += 1;
            _currentFingerContacts[i] = null;
        }

        /** Changes graphics for fingers and palms based on contact movement.
         * */
        void windowMoveContact(object sender, RoutedEventArgs e)
        {
            if (_palmContacts.Count == 0 || ((Pt)_palmContacts[0].GetCenterPosition(_panel) - _palmCenter).Length > 200)
                return;

            foreach (FrameworkElement fe in _panel.Children)
                if (fe is Canvas && (fe as Canvas).Children.Count == 1 &&(fe as Canvas).Tag is int && (int)(fe as Canvas).Tag != -1 && (int)(fe as Canvas).Tag < 6)
                    fe.Visibility = Visibility.Hidden; 
            for (int i = 0; i < 5; i++)
                if (_currentFingerContacts[i] != null) // I think this is what we need to change... _fingers[i] should be null if the finger event is not triggered?
                    ShowActiveFingerContact(i);

            // move the palm if the palm contact
            _palmCenter = Pt.Avg(_palmContacts.Select((Contact i) => (Pt)i.GetCenterPosition(_panel)).ToArray());
            ShowFeedback();
        }

        /** Graphics for an active palmFinger contact.
         * */
        void ShowActiveFingerContact(int activeFinger)
        {
            Canvas container = _activeFingerDisplays[activeFinger];
            PalmFinger pf = container.Children[0] as PalmFinger;
            container.Visibility = Visibility.Visible;
            container.RenderTransform = new MatrixTransform(
                Mat.Rotate((Deg)_currentFingerContacts[activeFinger].GetOrientation(_panel))*
                Mat.Translate(_currentFingerContacts[activeFinger].GetPosition(_panel) - pf.Size/2));
        }

        /** Displays graphics for all FrameworkElements (i.e. palm and finger graphics) in PalmPrint.
         * */
        void ShowFeedback()
        {
            foreach (Canvas finger in fingerImages)
                finger.Visibility = Visibility.Collapsed;
            foreach (Canvas disp in this._activeFingerDisplays)
                disp.Visibility = Visibility.Collapsed;

            double offset = _palmContacts.Count > 0 ? PalmEnlarge : PalmShrink; // offset display of feedback when palm is down since the fingers will block the feedback if its placed right below the fingers.
            for (int i = 0; i < 5; i++)
                try
                {
                    FrameworkElement fingerImage = fingerImages[i].Children[0] as FrameworkElement;
                    fingerImages[i].RenderTransform = new MatrixTransform(Mat.Translate(_fingerLocations[i] * offset + _palmCenter  - new Vec(fingerImage.ActualWidth / 2, fingerImage.ActualHeight / 2)));
                    fingerImages[i].Visibility = Visibility.Visible;
                }
                catch (Exception) { }
            try
            {
                FrameworkElement palmImage = fingerImages[5].Children[0] as FrameworkElement;
                fingerImages[5].RenderTransform = new MatrixTransform(Mat.Translate(_palmCenter - new Vec(palmImage.ActualWidth / 2, palmImage.ActualHeight / 2)));
                fingerImages[5].Visibility = Visibility.Visible;
                _sideTools.Visibility = Visibility.Collapsed;
                _palmShown = true;
            }
            catch (Exception) { }
        }

        /** tests if a window contact is a finger tip, palm or pen and processes accordingly
         * */
        void windowContact(object sender, RoutedPointEventArgs e)
        {
            // Case 1: Pen
            if (e.DeviceType == InputFramework.DeviceDriver.DeviceType.Stylus)
                return;

            Contact c = e.WPFPointDevice.Contact();
            if (c == null)
                return;
            // Case 2: Finger
            if (c.IsFingerRecognized && addFingerContact(c)) {
                e.Handled = true;
                windowMoveContact(sender, e);
            }

            // Case 3: Palm
            if ((!c.IsFingerRecognized && c.PhysicalArea > .2) || 
                (_palmShown && (_palmCenter - (Pt)e.GetPosition(_panel)).Length < 50 && !e.Handled))
            {
                if (_palmContacts.Count == 0) {
                    _palmDownTime = DateTime.Now;
                    _palmContacts.Add(c);
                    _palmActive = true;
                    foreach (Contact cc in Contacts.GetContactsOver(_eventElement))
                        if (cc.IsFingerRecognized)
                            addFingerContact(cc);
                    e.Handled = true;
                    windowMoveContact(sender, e);
                }
            }
        }

    }
}
