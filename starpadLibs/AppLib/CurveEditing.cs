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
using starPadSDK.CharRecognizer;

namespace starPadSDK.AppLib {
    public class CurveEditing : CommandSet.CommandEditor {
        public CurveEditing(InqScene c):base(c) {  }
        override protected bool  stroqAdded(Stroq s) {
            if (_can.Stroqs.Count == 0)
                return false;
            double startCDist = 10;
            Stroq startCandidate = null, endCandidate = null;
            StroqCollection sq = new StroqCollection(_can.Stroqs);
            float x1 = 0;
            float x2 = 0;
            using(StroqInkMapper sm = new StroqInkMapper(sq)) {
                Microsoft.Ink.Stroke os = s.OldStroke();
                Stroq sk = sm[sm.Ink.NearestPoint(os.GetPoint(0))];
                Stroq sk2 = sm[sm.Ink.NearestPoint(os.GetPoint(os.GetPoints().Length - 1))];
                if(sk != null) {
                    x1 = sm[sk].NearestPoint(os.GetPoint(0));
                    if((sk[x1] - s[0]).Length < startCDist) {
                        startCandidate = sk;
                        ClipStroq(s, startCandidate, sm, sm[sk], ref x1, false);
                        startCandidate[(int)x1] = s[0] = startCandidate[x1];
                    }
                    if(x1 < 1)
                        startCandidate = null;
                }
                if(sk2 != null) {
                    x2 = sm[sk2].NearestPoint(os.GetPoint(os.GetPoints().Length - 1));
                    if((sk2[x2] - s[-1]).Length < startCDist) {
                        endCandidate = sk2;
                        ClipStroq(s, endCandidate, sm, sm[sk2], ref x2, true);
                        endCandidate[(int)x2] = s[-1] = endCandidate[x2];
                    }
                }
            }
            if (startCandidate == endCandidate && startCandidate != null) {
                SpliceStroq(startCandidate, (int)x1, (int)x2, s);
                return true;
            }
            if (startCandidate != null && endCandidate == null) {
                SpliceStroq(startCandidate, (int)x1, -1, s);
                return true;
            }
            if (endCandidate != null && startCandidate == null) {
                List<Pt> reversed = new List<Pt>(s.Reverse());
               SpliceStroq(endCandidate, (int)x2, -1, reversed);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clips Stroq 's' against 'refStroke'  and updates 'x1' to be the index along 'refStroke' of the intersection
        /// </summary>
        /// If 's' and 'refStroke' don't intersect, or if the point of intersection is closer to the end than the start of 's'
        /// (or vice-versa if 'end' is true), then no clipping takes place.
        /// <param name="s"></param>
        /// <param name="startCandidate"></param>
        /// <param name="sm"></param>
        /// <param name="sk"></param>
        /// <param name="x1"></param>
        /// <param name="end"></param>
        private static void ClipStroq(Stroq s, Stroq refStroke, StroqInkMapper sm, Microsoft.Ink.Stroke sk, ref float x1, bool end) {
            Microsoft.Ink.Stroke os = s.OldStroke();
            Microsoft.Ink.Stroke tmp = sm.Ink.CreateStroke(os.GetPoints());
            float[] ints1 = sk.FindIntersections(sm.Ink.CreateStrokes(new int[] { tmp.Id }));
            if (ints1.Length > 0) {
                double closest = double.MaxValue;
                bool clipped = false;
                // see if there's an intersection point between the editing stroke and the edited stroke that is closer to the
                // start than the end of the editing stroke (or vice-versa if 'end' is true).
                foreach (float inter in ints1)
                    if ((refStroke[inter] - s[end ? -1 : 0]).Length <
                        (refStroke[inter] - s[end ? 0 : -1]).Length &&
                        (refStroke[inter] - s[end ? -1 : 0]).Length < closest) {
                        closest = (refStroke[inter] - s[0]).Length;
                        x1 = inter;
                        clipped = true;
                    }
                // clip the editing stroke at the point of intersection with the edited stroke
                if (clipped) {
                    float[] trunc = tmp.FindIntersections(sm.Ink.CreateStrokes(new int[] { sk.Id }));
                    int rem = end ? s.Count - (int)trunc[trunc.Length - 1] : (int)trunc[0];
                    for (int i = 0; i < rem; i++)
                        s.RemoveAt(end ? s.Count-1 : 0);
                }
            }
            Console.WriteLine("clip stroke");
            sm.Ink.DeleteStroke(tmp);
        }

        /// <summary>
        /// Splices points into a Stroq starting at index x1 (and optionally ending at x2)
        /// </summary>
        /// if x2 is passed as -1, then the original stroke will be split at x1 and the new points inserted.  Note that
        /// the decision to keep the beginning of the original stroke or the end will depend on the Dot product of the tangent
        /// of the original stroke at x1 with the starting tangetn ot the points to be inserted.
        /// 
        /// if x2 is not -1, then the middle of the original stroke will be removed from x1 to x2.  In this case, it doesn't
        /// matter whether x1 is bigger or not than x2.
        /// <param name="startCandidate"></param>
        /// <param name="x1"></param>
        /// <param name="x2"></param>
        /// <param name="toSplice"></param>
        private void SpliceStroq(Stroq startCandidateOrig, int x1, int x2, IList<Pt> toSplice) {
            Stroq startCandidate = startCandidateOrig.Clone();
            if (x2 == -1) {
                bool fromEnd = ((startCandidate[x1] - startCandidate[x1 - 1]).Dot(toSplice[Math.Min(toSplice.Count-1,5)] - toSplice[0]) > 0);
                if (!fromEnd) {
                   toSplice = toSplice.Reverse1();
                    x2 = x1;
                    x1 = 0;
                }
                else
                    x2 = startCandidate.Count;
            }
            else {
                if (x2 < x1) {
                    int tmp = x2;
                    x2 = x1;
                    x1 = tmp;
                    toSplice = new List<Pt>(toSplice.Reverse1());
                }
            }
            int remCount = x2 - x1;
            for (int i = 0; i < remCount; i++)
                try {
                    startCandidate.RemoveAt(x1);
                }
                catch (Exception) {
                    // bcz: ack! the strokeCollection is not allowed to be of length 0
                }
            foreach (Pt p in toSplice)
                startCandidate.Insert(x1++, p);
            SmoothStroke(startCandidate);

            _can.UndoRedo.Add(new ReplaceAction(new SelectionObj(startCandidate), new SelectionObj(startCandidateOrig), _can));
        }

        /// <summary>
        /// Smooths the the stroq
        /// </summary>
        /// <param name="smooth"></param>
        private static void SmoothStroke(Stroq smooth) {
            Microsoft.Ink.Ink tmpInk = new Microsoft.Ink.Ink();
            Microsoft.Ink.Stroke smoothStroke = tmpInk.CreateStroke(smooth.OldStroke().GetPoints());

            // smoooth the Stroq in the old Microsoft.Ink.Stroke coordinate space
            for (int k = 0; k < 3; k++)
                smoothStroke = tmpInk.CreateStroke(tmpInk.Strokes[tmpInk.Strokes.Count - 1].GetFlattenedBezierPoints());

            // bcz; hack for knowing the transform between Stroq and Microsoft.Ink.Stroke coordinate spaces
            double scale = smooth.GetBounds().Width / (double)smoothStroke.GetBoundingBox().Width;

            int rem = (smooth.Count - smoothStroke.GetPoints().Length);
            for (int i = 0; i < rem; i++)
                smooth.RemoveAt(smooth.Count - 1);
            while (smooth.Count < smoothStroke.GetPoints().Length)
                smooth.Add(new Pt());
            for (int i = 0; i < smoothStroke.GetPoints().Length; i++)
                smooth[i] = new Pt(scale * smoothStroke.GetPoint(i).X, scale * smoothStroke.GetPoint(i).Y);
            tmpInk.Dispose();
        }

        public class CurveWidg {
            Pt _grabRelPt = new Pt();
            InqScene _can = null;
            Ellipse _e = null;
            public FrameworkElement Visual { get { return _e; } }
            public CurveWidg(InqScene can, Pt loc, double radius) {
                _can = can;

                _e = new Ellipse();
                _e.Height = _e.Width = radius;
                _e.RenderTransform = new MatrixTransform(Mat.Translate(loc));
                _e.Stroke = Brushes.Red;
                _e.Fill = new SolidColorBrush(Color.FromArgb(125, 125, 125, 125));
                _e.PreviewStylusDown += new StylusDownEventHandler(e_StylusDown);
            }
            void e_StylusDown(object sender, StylusDownEventArgs ea) {
                Mouse.Capture(_e);
                _e.MouseMove += new MouseEventHandler(e_MouseMove);
                _e.MouseUp += new MouseButtonEventHandler(e_MouseUp);
                _grabRelPt = ea.GetPosition(_e);
                ea.Handled = true;
                _can.SetInkEnabledForDevice(ea.StylusDevice, false);
            }


            void e_MouseUp(object sender, MouseButtonEventArgs ea) {
                _e.MouseMove -= new MouseEventHandler(e_MouseMove);
                _e.MouseUp -= new MouseButtonEventHandler(e_MouseUp);
                Mouse.Capture(null);
                _can.SetInkEnabledForDevice(ea.StylusDevice, false);
            }

            void e_MouseMove(object sender, MouseEventArgs ea) {
                Mat cur = (Mat)_e.RenderTransform.Value;
                Mat next = cur * Mat.Translate((Pt)ea.GetPosition(_can) - cur * _grabRelPt);
                Pt objCenter = new Pt(_e.ActualWidth / 2, _e.ActualHeight / 2);
                double rad = _e.ActualHeight/2;
                if (cur*objCenter != next*objCenter)
                    ReshapeWithCircle(_can.Stroqs.ToArray(), cur * objCenter, next*objCenter, rad);
                _e.RenderTransform = new MatrixTransform(next);
            }

            static void ReshapeWithCircle(Stroq[] stroqs, Pt oldCenter, Pt center, double rad) {
                foreach (Stroq s in stroqs) {
                    float dist;
                    s.OldNearestPoint(center, out dist);
                    if (dist < rad + 10) {
                        ReSample(center, rad, s);
                        Push(s, rad, oldCenter, center);
                    }
                }
            }

            static void ReSample(Pt center, double rad, Stroq s) {
                Stroq tmp = s.Clone();
                int offset = 0;
                for (int i = 1; i < tmp.Count; i++)
                    if ((tmp[i] - tmp[i - 1]).Length > rad / 4 && (Pt.Avg(tmp[i], tmp[i - 1]) - center).Length < rad * 1.5) {
                        s.Insert(i + offset, Pt.Avg(tmp[i], tmp[i - 1]), (tmp.Pressure[i] + tmp.Pressure[i - 1]) / 2);
                        offset++;
                    }
            }

            static void Push(Stroq s, double rad, Pt cold, Pt cnew) {
                Vec delta = (cnew - cold).Normal();
                s.BatchEdit(() => s.ForEach((RWIter<Pt> p) => {
                        Vec offDir = (p - cnew).Normal() * ((p-cnew).Length-rad);
                        if (offDir.Dot(p-cnew) <= 0)
                            p.Value = cnew + (p-cnew).Normal() * rad;
                        else p.Value += offDir / (1+offDir.Length*offDir.Length) ;
                }));
            }
        }
    }
}
