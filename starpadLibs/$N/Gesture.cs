/**
 * The $N Multistroke Recognizer (C# version)
 *
 *	    Lisa Anthony, Ph.D.
 *		UMBC
 *		Information Systems Department
 * 		1000 Hilltop Circle
 *		Baltimore, MD 21250
 * 		lanthony@umbc.edu
 * 
 *      Jacob O. Wobbrock, Ph.D.
 * 		The Information School
 *		University of Washington
 *		Mary Gates Hall, Box 352840
 *		Seattle, WA 98195-2840
 *		wobbrock@u.washington.edu
 *
 * The Protractor enhancement was published by Yang Li and programmed
 * here by Lisa Anthony and Jacob O. Wobbrock.
 *
 *	Li, Y. (2010). Protractor: A fast and accurate gesture 
 *	  recognizer. Proceedings of the ACM Conference on Human 
 *	  Factors in Computing Systems (CHI '10). Atlanta, Georgia
 *	  (April 10-15, 2010). New York: ACM Press, pp. 2169-2172.
 *
 * This software is distributed under the "New BSD License" agreement:
 * 
 * Copyright (c) 2007-2011, Lisa Anthony and Jacob O. Wobbrock
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *    * Redistributions of source code must retain the above copyright
 *      notice, this list of conditions and the following disclaimer.
 *    * Redistributions in binary form must reproduce the above copyright
 *      notice, this list of conditions and the following disclaimer in the
 *      documentation and/or other materials provided with the distribution.
 *    * Neither the name of the University of Washington nor UMBC,
 *      nor the names of its contributors may be used to endorse or promote 
 *      products derived from this software without specific prior written
 *      permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS
 * IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL Jacob O. Wobbrock OR Lisa Anthony 
 * BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
 * GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) 
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
 * LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY
 * OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
 * SUCH DAMAGE.
**/
using System;
using System.Collections;
using System.Collections.Generic;

namespace Recognizer.NDollar
{
    public class Gesture : IComparable
    {
        public string Name;
        public List<PointR> RawPoints; // raw points (for drawing) -- read in from XML
        public List<PointR> Points;    // resampled points (for matching) -- done when loaded
        public bool Is1D; // flag indicating if this is a 1D or a 2D gesture, Lisa 1/2/2008; 8/9/2009 boolean now
        public PointR StartUnitVector; // Lisa 8/8/2009
        public List<Double> VectorVersion; // Lisa 3/7/2011 -- added to help with Protractor testing

        // added for debugging purpose, Lisa 5/10/2008
        // setting this flag to true will draw all phases of matching templates to
        // candidates to a pop-up window on-screen (DON'T USE WITH TEST BATCH!)
        private DebugForm _debugFrm = null;

        public Gesture()
        {
            this.Name = String.Empty;
            this.RawPoints = null;
            this.Points = null;
            this.Is1D = true; // Lisa 1/2/2008
        }

        // when a new prototype is made, its raw points are resampled into n equidistantly spaced
        // points, then it is scaled to a preset size and translated to a preset origin. this is
        // the same treatment applied to each candidate stroke, and it allows us to thereafter
        // simply step through each point in each stroke and compare those points' distances.
        // in other words, it removes the challenge of determining corresponding points in each gesture.
        // after resampling, scaling, and translating, we compute the "indicative angle" of the 
        // stroke as defined by the angle between its centroid point and first point.
        public Gesture(string name, List<PointR> points)
            : this(points)
        {
            this.Name = name;
        }

        public Gesture(List<PointR> points)
        {
            this.Name = String.Empty;
            this.RawPoints = new List<PointR>(points); // copy (saved for drawing)

            Points = RawPoints;

            // added for debugging, Lisa 1/2/2008
            if (GeometricRecognizer._debug)
            {
                _debugFrm = new DebugForm();
                _debugFrm.Owner = null;
                _debugFrm.Show();

                _debugFrm.SendPoints(Points, "Raw Points");
            }

            // reflects new order of pre-processing steps as of 8/31/2009
            // old order (from 8/8/2009) was 
            // 1. check for 1D
            // 2. scale
            // 3. resample
            // 4. translate
            // 5. rotate (if rot-invariant)
            // 6. calculate start angle

            // new order (as of 8/31/2009) is
            // 1. resample
            // 2. rotate (all); save amount
            // 3. check for 1D
            // 4. scale
            // 5. rotate back (if NOT rot-invariant)
            // 6. translate
            // 7. calculate start angle

            // first, resample (influences calculation of centroid)
            Points = Utils.Resample(Points, NDollarParameters.Instance.NumResamplePoints);
            if (GeometricRecognizer._debug)
            {
                _debugFrm.SendPoints(Points, "Resampled Points");
            }

            // then, if we are rotation-invariant, rotate to a common reference angle
            // otherwise skip that step
            // Lisa 8/8/2009: this is now set by a flag in the NDollarRecognizer.cs file with all the other flags
            // rotate so that the centroid-to-1st-point is at zero degrees
            double radians = Utils.AngleInRadians(Utils.Centroid(Points), (PointR) Points[0], false);
            Points = Utils.RotateByRadians(Points, -radians);
            if (GeometricRecognizer._debug)
            {
                _debugFrm.SendPoints(Points, "Rotated Points");
            }

            // then, resize to a square
            // check for 1D vs 2D (because we resize differently)
            // Lisa 1/2/2008
            // replace with boolean, Lisa 8/9/2009
            this.Is1D = Utils.Is1DGesture(RawPoints);

            // scale to a common (square) dimension
            // moved determination of scale method to within the scale() method for less branching here
            // Lisa 8/9/2009
            Points = Utils.Scale(Points, GeometricRecognizer._1DThreshold, GeometricRecognizer.ResampleScale);
            if (GeometricRecognizer._debug)
            {
                _debugFrm.SendPoints(Points, "Scaled Points");
            }

            // next, if NOT rotation-invariant, rotate back
            if (!NDollarParameters.Instance.RotationInvariant)
            {
                Points = Utils.RotateByRadians(Points, +radians); // undo angle
                if (GeometricRecognizer._debug)
                {
                    _debugFrm.SendPoints(Points, "Un-rotated Points");
                }
            }

            // next, translate to a common origin
            Points = Utils.TranslateCentroidTo(Points, GeometricRecognizer.ResampleOrigin);
            if (GeometricRecognizer._debug)
            {
                _debugFrm.SendPoints(Points, "Translated Points");
            }

            // finally, save the start angle
            // Lisa 8/8/2009
            // store the start unit vector after post-processing steps
            this.StartUnitVector = Utils.CalcStartUnitVector(Points, NDollarParameters.Instance.StartAngleIndex);

            // Lisa 3/7/2011
            // make the simple vector-based version for Protractor testing
            this.VectorVersion = Vectorize(this.Points);
        }

        public int Duration
        {
            get
            {
                if (RawPoints.Count >= 2)
                {
                    PointR p0 = (PointR) RawPoints[0];
                    PointR pn = (PointR) RawPoints[RawPoints.Count - 1];
                    return pn.T - p0.T;
                }
                else
                {
                    return 0;
                }
            }
        }

        // sorts in descending order of Score
        public int CompareTo(object obj)
        {
            if (obj is Gesture)
            {
                Gesture g = (Gesture) obj;
                return Name.CompareTo(g.Name);
            }
            else throw new ArgumentException("object is not a Gesture");
        }

        /// <summary>
        /// Pulls the gesture name from the file name, e.g., "circle03" from "C:\gestures\circles\circle03.xml".
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ParseName(string filename)
        {
            int start = filename.LastIndexOf('\\');
            int end = filename.LastIndexOf('.');
            return filename.Substring(start + 1, end - start - 1);
        }


        /// <summary>
        /// Pulls the user name from the name of the gesture, e.g., "14" from "minus_14_30_0".
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string ParseUser(string name)
        {
            int start = name.IndexOf("_");
            int end = name.IndexOf("_", start + 1);
            return name.Substring(start + 1, end - start - 1);
        }

        // From http://yangl.org/protractor/Protractor%20Gesture%20Recognizer.pdf
        // Given a list of PointR's this can translate them into a flat list of X,Y coordinates,
        // a Vector, which is needed by Protractor's OptimalCosineDistance().
        private List<Double> Vectorize(List<PointR> pts)
        {
            // skip the resampling, translation because $N already did this in pre-processing
            // re-do the rotation though
            // (note: doing rotation  on the pre-processed points is ok because $N rotates it back to the
            // original orientation if !RotationInvariant, e.g., it is rotation sensitive)
            //
            // 04/04/2011 [JOW]: If RotationInvariant, no reason to do this stuff so optimize by skipping.
            //
            double cos = 1.0;
            double sin = 0.0;

            if (!NDollarParameters.Instance.RotationInvariant) // rotation sensitive
            {
                double iAngle = Math.Atan2(pts[0].Y, pts[0].X);
                double baseOrientation = (Math.PI / 4.0) * Math.Floor((iAngle + Math.PI / 8.0) / (Math.PI / 4.0));
                cos = Math.Cos(baseOrientation - iAngle);
                sin = Math.Sin(baseOrientation - iAngle);
            }
            double sum = 0.0;
            List<double> vector = new List<double>();
            foreach (PointR p in pts)
            {
                double newX = p.X * cos - p.Y * sin;
                double newY = p.Y * cos + p.X * sin;
                vector.Add(newX);
                vector.Add(newY);
                sum += newX * newX + newY * newY;
            }
            double magnitude = Math.Sqrt(sum);
            for (int i = 0; i < vector.Count; i++)
            {
                vector[i] /= magnitude;
            }
            return vector;
        }
    }
}
