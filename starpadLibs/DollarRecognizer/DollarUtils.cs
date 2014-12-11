using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Input;
using starPadSDK.Inq;
using System.IO;

namespace starPadSDK.DollarRecognizer
{
    public class DollarUtil
    {
        GeometricRecognizer _rec = new GeometricRecognizer();

        public GeometricRecognizer Rec
        {
            get { return _rec; }
            set { _rec = value; }
        }

        public DollarUtil(string dir, List<string> filterIn, List<string> filterOut, string repository, List<string> gestures, bool addReverse)
        {
            try
            {
                if (repository != "")
                    foreach (string name in System.IO.Directory.GetFiles(dir + "\\" + repository, "*.xml"))
                    {
                        string[] splits = System.IO.Path.GetFileName(name).Split(new char[] { '_', '.' });
                        if (filterIn.Contains(splits[0]) && !filterOut.Contains(splits[0]))
                            _rec.LoadGesture(name, addReverse);
                    }
                else if (gestures != null)
                {
                    foreach (var g in gestures)
                        _rec.LoadGesture(new MemoryStream(ASCIIEncoding.Default.GetBytes(g)), addReverse);
                }
            }
            catch (Exception) { }
        }
        public DollarUtil(string dir, List<string> gestures, string repository)
        {
            try
            {
            }
            catch (Exception Exception) { }
        }

        public string Recognize(Stroke s, double thresh, out double ang, out bool reversed)
        {
            int t = 0;
            ang = 0;
            reversed = false;
            List<PointR> points = new List<PointR>();
            foreach (StylusPoint pp in s.StylusPoints)
            {
                points.Add(new PointR(pp.X, pp.Y, t));
                t += 10;
            }
            if (points.Count >= 5) // require 5 points for a valid gesture
                if (_rec.NumGestures > 0) // not recording, so testing
                {
                    NBestList result = _rec.Recognize(points); // where all the action is!!
                    if (result.Score > thresh)
                    {
                        ang = result.Angle;
                        reversed = result.Name.EndsWith("_R");
                        return result.Name.Split('_')[0];
                    }
                }
            return "";
        }
    }
    /// <summary>
    /// Adds two methods to a Stroq:
    ///  Dollar() - the name of the Dollar recognizer matched
    /// </summary>
    static public class DollarTester
    {
        static Guid DOLLAR = new Guid("0900773F-9EDC-43fd-862B-F33EA01D3403");
        static public string Directory = System.IO.Directory.GetCurrentDirectory();
        static public List<string> FilterOut = new List<string>();
        public class DollarRec
        {
            public object _gestures;
            public string _rec;
            public double _ang;
            public bool _reversed;
            public DollarRec(object gestures, string recog, double angle, bool reversed) { _gestures = gestures; _rec = recog; _ang = angle; _reversed = reversed; }
        }
        static public string Dollar(this Stroq stroke, string[] filterIn, double threshold = 0.8, string repository = "DollareGestures", bool addReverse = false)
        {
            string rec = "";
            if (!stroke.Property.Exists(DOLLAR) || ((DollarRec)stroke.Property[DOLLAR])._gestures != filterIn)
            {
                DollarUtil du = new DollarUtil(Directory, new List<string>(filterIn), FilterOut, repository, null, addReverse);
                double ang; bool reversed;
                rec = du.Recognize(stroke.BackingStroke, threshold, out ang, out reversed);
                stroke.Property[DOLLAR] = new DollarRec(filterIn, rec, ang, reversed);
            }
            else
                rec = ((DollarRec)stroke.Property[DOLLAR])._rec;
            return rec;
        }
        static public DollarRec Dollar(this Stroq stroke, string gestureSet, List<string> gestures, double threshold = 0.8, bool addReverse = false)
        {
            DollarRec rec;
            if (!stroke.Property.Exists(DOLLAR) || ((string)((DollarRec)stroke.Property[DOLLAR])._gestures) != gestureSet)
            {
                DollarUtil du = new DollarUtil(Directory, null, FilterOut, "", gestures, addReverse);
                double ang; bool reversed;
                string recog = du.Recognize(stroke.BackingStroke, threshold, out ang, out reversed);
                stroke.Property[DOLLAR] = rec = new DollarRec(gestureSet, recog, ang, reversed);
            }
            else
                rec = ((DollarRec)stroke.Property[DOLLAR]);
            return rec;
        }
    }
}
