/**
 * The $1 Unistroke Recognizer (C# version)
 *
 *		Jacob O. Wobbrock, Ph.D.
 * 		The Information School
 *		University of Washington
 *		Mary Gates Hall, Box 352840
 *		Seattle, WA 98195-2840
 *		wobbrock@u.washington.edu
 *
 *		Andrew D. Wilson, Ph.D.
 *		Microsoft Research
 *		One Microsoft Way
 *		Redmond, WA 98052
 *		awilson@microsoft.com
 *
 *		Yang Li, Ph.D.
 *		Department of Computer Science and Engineering
 * 		University of Washington
 *		The Allen Center, Box 352350
 *		Seattle, WA 98195-2840
 * 		yangli@cs.washington.edu
 *
 * The Protractor enhancement was published by Yang Li and programmed here by 
 * Jacob O. Wobbrock.
 *
 *	Li, Y. (2010). Protractor: A fast and accurate gesture 
 *	  recognizer. Proceedings of the ACM Conference on Human 
 *	  Factors in Computing Systems (CHI '10). Atlanta, Georgia
 *	  (April 10-15, 2010). New York: ACM Press, pp. 2169-2172.
 * 
 * This software is distributed under the "New BSD License" agreement:
 * 
 * Copyright (c) 2007-2011, Jacob O. Wobbrock, Andrew D. Wilson and Yang Li.
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *    * Redistributions of source code must retain the above copyright
 *      notice, this list of conditions and the following disclaimer.
 *    * Redistributions in binary form must reproduce the above copyright
 *      notice, this list of conditions and the following disclaimer in the
 *      documentation and/or other materials provided with the distribution.
 *    * Neither the names of the University of Washington nor Microsoft,
 *      nor the names of its contributors may be used to endorse or promote 
 *      products derived from this software without specific prior written
 *      permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS
 * IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL Jacob O. Wobbrock OR Andrew D. Wilson
 * OR Yang Li BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, 
 * OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, 
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
**/
using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace starPadSDK.DollarRecognizer
{
    public class GeometricRecognizer
    {
        #region Members

        public const int NumResamplePoints = 64;
        private const double DX = 250.0;
        public static readonly SizeR ResampleScale = new SizeR(DX, DX);
        public static readonly double Diagonal = Math.Sqrt(DX * DX + DX * DX);
        public static readonly double HalfDiagonal = 0.5 * Diagonal;
        public static readonly PointR ResampleOrigin = new PointR(0, 0);
        private static readonly double Phi = 0.5 * (-1 + Math.Sqrt(5)); // Golden Ratio

        private Hashtable _gestures;

        #endregion

        #region Constructor

        public GeometricRecognizer()
        {
            _gestures = new Hashtable(256);
        }

        #endregion

        #region Recognition

        public NBestList Recognize(List<PointR> points) // candidate points
        {
            // resample to a common number of points
            points = Utils.Resample(points, NumResamplePoints);

            // rotate so that the centroid-to-1st-point is at zero degrees
            double radians = Utils.AngleInRadians(Utils.Centroid(points), points[0], false); // indicative angle
            points = Utils.RotateByRadians(points, -radians); // undo angle

            // scale to a common (square) dimension
            points = Utils.ScaleTo(points, ResampleScale);

            // translate to a common origin
            points = Utils.TranslateCentroidTo(points, ResampleOrigin);

            NBestList nbest = new NBestList();
            foreach (DGesture p in _gestures.Values)
            {
                double[] best = GoldenSectionSearch(
                    points,                 // to rotate
                    p.Points,               // to match
                    Utils.Deg2Rad(-45.0),   // lbound
                    Utils.Deg2Rad(+45.0),   // ubound6
                    Utils.Deg2Rad(2.0));    // threshold

                double score = 1d - best[0] / HalfDiagonal;
                nbest.AddResult(p.Name, score, best[0], best[1]); // name, score, distance, angle
            }
            nbest.SortDescending(); // sort so that nbest[0] is best result
            return nbest;
        }

        // From http://www.math.uic.edu/~jan/mcs471/Lec9/gss.pdf
        private double[] GoldenSectionSearch(List<PointR> pts1, List<PointR> pts2, double a, double b, double threshold)
        {
            double x1 = Phi * a + (1 - Phi) * b;
            List<PointR> newPoints = Utils.RotateByRadians(pts1, x1);
            double fx1 = Utils.PathDistance(newPoints, pts2);

            double x2 = (1 - Phi) * a + Phi * b;
            newPoints = Utils.RotateByRadians(pts1, x2);
            double fx2 = Utils.PathDistance(newPoints, pts2);

            double i = 2.0; // calls
            while (Math.Abs(b - a) > threshold)
            {
                if (fx1 < fx2)
                {
                    b = x2;
                    x2 = x1;
                    fx2 = fx1;
                    x1 = Phi * a + (1 - Phi) * b;
                    newPoints = Utils.RotateByRadians(pts1, x1);
                    fx1 = Utils.PathDistance(newPoints, pts2);
                }
                else
                {
                    a = x1;
                    x1 = x2;
                    fx1 = fx2;
                    x2 = (1 - Phi) * a + Phi * b;
                    newPoints = Utils.RotateByRadians(pts1, x2);
                    fx2 = Utils.PathDistance(newPoints, pts2);
                }
                i++;
            }
            return new double[3] { Math.Min(fx1, fx2), Utils.Rad2Deg((b + a) / 2.0), i }; // distance, angle, calls to pathdist
        }

        // continues to rotate 'pts1' by 'step' degrees as long as points become ever-closer 
        // in path-distance to pts2. the initial distance is given by D. the best distance
        // is returned in array[0], while the angle at which it was achieved is in array[1].
        // array[3] contains the number of calls to PathDistance.
        private double[] HillClimbSearch(List<PointR> pts1, List<PointR> pts2, double D, double step)
        {
            double i = 0.0;
            double theta = 0.0;
            double d = D;
            do
            {
                D = d; // the last angle tried was better still
                theta += step;
                List<PointR> newPoints = Utils.RotateByDegrees(pts1, theta);
                d = Utils.PathDistance(newPoints, pts2);
                i++;
            }
            while (d <= D);
            return new double[3] { D, theta - step, i }; // distance, angle, calls to pathdist
        }

        private double[] FullSearch(List<PointR> pts1, List<PointR> pts2, StreamWriter writer)
        {
            double bestA = 0d;
            double bestD = Utils.PathDistance(pts1, pts2);

            for (int i = -180; i <= +180; i++)
            {
                List<PointR> newPoints = Utils.RotateByDegrees(pts1, i);
                double d = Utils.PathDistance(newPoints, pts2);
                if (writer != null)
                {
                    writer.WriteLine("{0}\t{1:F3}", i, Math.Round(d, 3));
                }
                if (d < bestD)
                {
                    bestD = d;
                    bestA = i;
                }
            }
            writer.WriteLine("\nFull Search (360 rotations)\n{0:F2}{1}\t{2:F3} px", Math.Round(bestA, 2), (char)176, Math.Round(bestD, 3)); // calls, angle, distance
            return new double[3] { bestD, bestA, 360.0 }; // distance, angle, calls to pathdist
        }

        #endregion

        #region Gestures & Xml

        public int NumGestures
        {
            get
            {
                return _gestures.Count;
            }
        }

        public ArrayList Gestures
        {
            get
            {
                ArrayList list = new ArrayList(_gestures.Values);
                list.Sort();
                return list;
            }
        }

        public void ClearGestures()
        {
            _gestures.Clear();
        }

        public bool SaveGesture(string filename, List<PointR> points)
        {
            // add the new prototype with the name extracted from the filename.
            string name = DGesture.ParseName(filename);
            if (_gestures.ContainsKey(name))
                _gestures.Remove(name);
            DGesture newPrototype = new DGesture(name, points);
            _gestures.Add(name, newPrototype);

            // figure out the duration of the gesture
            PointR p0 = (PointR)points[0];
            PointR pn = (PointR)points[points.Count - 1];

            // do the xml writing
            bool success = true;
            XmlTextWriter writer = null;
            try
            {
                // save the prototype as an Xml file
                writer = new XmlTextWriter(filename, Encoding.UTF8);
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument(true);
                writer.WriteStartElement("Gesture");
                writer.WriteAttributeString("Name", name);
                writer.WriteAttributeString("NumPts", XmlConvert.ToString(points.Count));
                writer.WriteAttributeString("Millseconds", XmlConvert.ToString(pn.T - p0.T));
                writer.WriteAttributeString("AppName", Assembly.GetExecutingAssembly().GetName().Name);
                writer.WriteAttributeString("AppVer", Assembly.GetExecutingAssembly().GetName().Version.ToString());
                writer.WriteAttributeString("Date", DateTime.Now.ToLongDateString());
                writer.WriteAttributeString("TimeOfDay", DateTime.Now.ToLongTimeString());

                // write out the raw individual points
                foreach (PointR p in points)
                {
                    writer.WriteStartElement("Point");
                    writer.WriteAttributeString("X", XmlConvert.ToString(p.X));
                    writer.WriteAttributeString("Y", XmlConvert.ToString(p.Y));
                    writer.WriteAttributeString("T", XmlConvert.ToString(p.T));
                    writer.WriteEndElement(); // <Point />
                }

                writer.WriteEndDocument(); // </Gesture>
            }
            catch (XmlException xex)
            {
                Console.Write(xex.Message);
                success = false;
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                success = false;
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
            return success; // Xml file successfully written (or not)
        }

        public bool LoadGesture(string filename, bool addReverse)
        {
            bool success = true;
            XmlTextReader reader = null;
            try
            {
                reader = new XmlTextReader(filename);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();

                var gestures = ReadGesture(reader, addReverse);

                // remove any with the same name and add the prototype gesture
                foreach (var p in gestures)
                {
                    if (_gestures.ContainsKey(p.Name))
                        _gestures.Remove(p.Name);
                    _gestures.Add(p.Name, p);
                }
            }
            catch (XmlException xex)
            {
                Console.Write(xex.Message);
                success = false;
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                success = false;
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
            return success;
        }

        public bool LoadGesture(Stream input, bool addReverse)
        {
            bool success = true;
            XmlTextReader reader = null;
            try
            {
                reader = new XmlTextReader(input);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();

                var gestures = ReadGesture(reader, addReverse);

                // remove any with the same name and add the prototype gesture
                foreach (var p in gestures)
                {
                    if (_gestures.ContainsKey(p.Name))
                        _gestures.Remove(p.Name);
                    _gestures.Add(p.Name, p);
                }
            }
            catch (XmlException xex)
            {
                Console.Write(xex.Message);
                success = false;
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                success = false;
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
            return success;
        }

        // assumes the reader has been just moved to the head of the content.
        private DGesture[] ReadGesture(XmlTextReader reader, bool addReverse)
        {
            Debug.Assert(reader.LocalName == "Gesture");
            string name = reader.GetAttribute("Name");

            List<PointR> points = new List<PointR>(XmlConvert.ToInt32(reader.GetAttribute("NumPts")));

            reader.Read(); // advance to the first Point
            Debug.Assert(reader.LocalName == "Point");

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                PointR p = PointR.Empty;
                p.X = XmlConvert.ToDouble(reader.GetAttribute("X"));
                p.Y = XmlConvert.ToDouble(reader.GetAttribute("Y"));
                p.T = XmlConvert.ToInt32(reader.GetAttribute("T"));
                points.Add(p);
                reader.ReadStartElement("Point");
            }

            if (addReverse)
            {
                var pointsR = new List<PointR>(points.ToArray());
                pointsR.Reverse();
                return new DGesture[] { new DGesture(name, points), new DGesture(name + "_R", pointsR) };
            }
            return new DGesture[] { new DGesture(name, points) };
        }

        #endregion

        #region Rotation Graph

        public bool CreateRotationGraph(string file1, string file2, string dir, bool similar)
        {
            bool success = true;
            StreamWriter writer = null;
            XmlTextReader reader = null;
            try
            {
                // read gesture file #1
                reader = new XmlTextReader(file1);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();
                DGesture g1 = ReadGesture(reader, false)[0];
                reader.Close();

                // read gesture file #2
                reader = new XmlTextReader(file2);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();
                DGesture g2 = ReadGesture(reader, false)[0];

                // create output file for results
                string outfile = String.Format("{0}\\{1}({2}, {3})_{4}.txt", dir, similar ? "o" : "x", g1.Name, g2.Name, Environment.TickCount);
                writer = new StreamWriter(outfile, false, Encoding.UTF8);
                writer.WriteLine("Rotated: {0} --> {1}. {2}, {3}\n", g1.Name, g2.Name, DateTime.Now.ToLongDateString(), DateTime.Now.ToLongTimeString());

                // do the full 360 degree rotations
                double[] full = FullSearch(g1.Points, g2.Points, writer);

                // use bidirectional hill climbing to do it again
                double init = Utils.PathDistance(g1.Points, g2.Points); // initial distance
                double[] pos = HillClimbSearch(g1.Points, g2.Points, init, 1d);
                double[] neg = HillClimbSearch(g1.Points, g2.Points, init, -1d);
                double[] best = new double[3];
                best = (neg[0] < pos[0]) ? neg : pos; // min distance
                writer.WriteLine("\nHill Climb Search ({0} rotations)\n{1:F2}{2}\t{3:F3} px", pos[2] + neg[2] + 1, Math.Round(best[1], 2), (char)176, Math.Round(best[0], 3)); // calls, angle, distance

                // use golden section search to do it yet again
                double[] gold = GoldenSectionSearch(
                    g1.Points,              // to rotate
                    g2.Points,              // to match
                    Utils.Deg2Rad(-45.0),   // lbound
                    Utils.Deg2Rad(+45.0),   // ubound
                    Utils.Deg2Rad(2.0));    // threshold
                writer.WriteLine("\nGolden Section Search ({0} rotations)\n{1:F2}{2}\t{3:F3} px", gold[2], Math.Round(gold[1], 2), (char)176, Math.Round(gold[0], 3)); // calls, angle, distance

                // for pasting into Excel
                writer.WriteLine("\n{0} {1} {2:F2} {3:F2} {4:F3} {5:F3} {6} {7:F2} {8:F2} {9:F3} {10} {11:F2} {12:F2} {13:F3} {14}",
                    g1.Name,                    // rotated
                    g2.Name,                    // into
                    Math.Abs(Math.Round(full[1], 2)), // |angle|
                    Math.Round(full[1], 2),     // Full Search angle
                    Math.Round(full[0], 3),     // Full Search distance
                    Math.Round(init, 3),        // Initial distance w/o any search
                    full[2],                    // Full Search iterations
                    Math.Abs(Math.Round(best[1], 2)), // |angle|
                    Math.Round(best[1], 2),     // Bidirectional Hill Climb Search angle
                    Math.Round(best[0], 3),     // Bidirectional Hill Climb Search distance
                    pos[2] + neg[2] + 1,        // Bidirectional Hill Climb Search iterations
                    Math.Abs(Math.Round(gold[1], 2)), // |angle|
                    Math.Round(gold[1], 2),     // Golden Section Search angle
                    Math.Round(gold[0], 3),     // Golden Section Search distance
                    gold[2]);                   // Golden Section Search iterations
            }
            catch (XmlException xml)
            {
                Console.Write(xml.Message);
                success = false;
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                success = false;
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                if (writer != null)
                    writer.Close();
            }
            return success;
        }

        #endregion
    }
}