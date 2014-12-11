using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using System.Windows.Media;
using System.Windows.Shapes;
using starPadSDK.Geom;
using starPadSDK.Utils;
using starPadSDK.Inq.BobsCusps;

namespace starPadSDK.Inq {
    /// <summary>
    /// Adds a method to a Stroq
    ///     isDoubleLoop - is the Stroq a double lasso
    /// </summary>
    static public class DoubleLoopTester {
        static Guid DLOOP = new Guid("7DF80B6E-6805-4ee3-82C9-7E8BA4D37C29");
        static bool insertionCaret(Stroq caret) {
            double dist = (caret.Cusps()[1].dist / caret.Cusps().Distance - 0.5);
            bool isCaret = caret.Cusps().Length == 3 &&
                      Math.Abs(caret.Cusps()[1].dist / caret.Cusps().Distance - 0.5) < .1 &&
                      caret.Cusps().Straightness(0, 1) < 0.15 && caret.Cusps().Straightness(1, 2) < 0.15 &&
                      caret.Cusps().inSeg(1).Direction.UnsignedAngle(new Vec(0, -1)) < Math.PI / 4 &&
                      caret.Cusps().inSeg(1).Direction.UnsignedAngle(-caret.Cusps().outSeg(1).Direction) < Math.PI / 4;
            return isCaret;
        }
        static bool doubleLoop(Stroq s) {
            int    closeInd = 0;
            double nearest  = double.MaxValue;
            Pt     first = s[0];
            for (int i = s.Count/4; i < 3*s.Count/4; i++)
                if ((s[i]-first).Length < nearest) {
                    nearest = (s[i] -first).Length;
                    closeInd = i;
                }
            if (nearest < s.GetBounds().MaxDim *.4 && closeInd > 2 && closeInd < s.Count - 2) {
                List<Pt> clipped = new List<Pt>(s.Select<Pt,Pt>((Pt p) => p));
                List<Pt> clipped2 = new List<Pt>(s.Select<Pt, Pt>((Pt p) => p));
                clipped.RemoveRange(closeInd, s.Count-closeInd);
                clipped2.RemoveRange(0, closeInd);
                Rct r1 = Rct.Null;
                Rct r2 = Rct.Null;
                foreach (Pt p in clipped)
                    r1 = r1.Union(p);
                foreach (Pt p in clipped2)
                    r2 = r2.Union(p);
                Rct r3 = r1.Union(r2);
                Rct inter = r1.Intersection(r2);
                if (isCircular(clipped.ToArray()) && isCircular(clipped2.ToArray()) &&
                    inter.Width/(float)r1.Width > 0.5 && inter.Height/(float)r1.Height > 0.5 &&
                    inter.Width/(float)r2.Width > 0.5 && inter.Height/(float)r2.Height > 0.5 &&
                    inter.Width/(float)r3.Width > 0.5 && inter.Height/(float)r3.Height > 0.5) {
                    return true;
                }
            }
            return false;
        }

        static bool isCircular(Pt[] clippepts) {
            Stroq tempStroke = new Stroq(clippepts);
            Cusps set = tempStroke.Cusps();
            if (set.SelfIntersects.Count > 4)
                return false;
            if (set.Distance > 1.25*(tempStroke.GetBounds().Width*2+tempStroke.GetBounds().Height*2))
                return false;
            for (int i = 1; i < set.Length; i++)
                if (Math.Abs(set[i].curvature) > .75)
                    return false;
            double d = (tempStroke[-1]-tempStroke[0]).Length/tempStroke.GetBounds().MaxDim;
            return (d < 0.75);
        }
        static public bool IsDoubleLoop(this Stroq stroke) {
            if (!stroke.Property.Exists(DLOOP))
                stroke.Property[DLOOP] = doubleLoop(stroke);
            return (bool)stroke.Property[DLOOP];
        }
    }    
    static public class DoubleHitchTester {
        static Guid DLOOP = new Guid("59BA9DDF-44E9-46cd-A184-E9132CDD6219");
        static bool insertionCaret(Stroq caret) {
            double dist = (caret.Cusps()[1].dist / caret.Cusps().Distance - 0.5);
            bool isCaret = caret.Cusps().Length == 3 &&
                      Math.Abs(caret.Cusps()[1].dist / caret.Cusps().Distance - 0.5) < .1 &&
                      caret.Cusps().Straightness(0, 1) < 0.15 && caret.Cusps().Straightness(1, 2) < 0.15 &&
                      caret.Cusps().inSeg(1).Direction.UnsignedAngle(new Vec(0, -1)) < Math.PI / 4 &&
                      caret.Cusps().inSeg(1).Direction.UnsignedAngle(-caret.Cusps().outSeg(1).Direction) < Math.PI / 4;
            return isCaret;
        }

        static public double zStraightnessTolerance = 0.08;
        public enum Dir {
            NE,
            NW,
            SE,
            SW,
            E,
            W,
            None
        }
        static Dir doubleHitch(Stroq s) {
            if (s.Cusps().Length == 4 && s.Cusps().Straightness() < zStraightnessTolerance) {
                Vec dir = (s[-1] - s[0]).Normal();
                double dx = dir.SignedAngle(Vec.Xaxis);
                double ndx = dir.SignedAngle(-Vec.Xaxis);
                if (dir.X > 0.3)
                    if (dx > .3)
                        return Dir.NE;
                    else if (dx < -.3)
                        return Dir.SE;
                    else return Dir.E;
                else if (dir.X < 0.3)
                    if (ndx < -.3)
                        return Dir.NW;
                    else if (ndx > .3)
                        return Dir.SW;
                    else return Dir.W;
            }
            return Dir.None;
        }

        static public Dir IsDoubleHitch(this Stroq stroke) {
            if (!stroke.Property.Exists(DLOOP))
                stroke.Property[DLOOP] = doubleHitch(stroke);
            return (Dir)stroke.Property[DLOOP];
        }
    }
    /// <summary>
    /// Adds two methods to a stroq:
    ///   IsLasso(test) - Does the stroke match a lasso geometry
    ///   Lassoed(test)  - the Selection returned by calling test(stroke)
    /// </summary>
    static public class LassoTester {
        class DictLookup : Dictionary<LassoTest, object> { }
        static Guid LASSO = new Guid("b3af0cd0-845e-42cf-87ac-b5465909964d");
        public delegate object LassoTest(Stroq stroke);
        static public bool IsLasso(this Stroq stroke) {
            double threshold = stroke.GetBounds().MaxDim * .2;
            bool canBeLasso = false;
            for (int i = stroke.Count - 1; !canBeLasso && i > Math.Max(stroke.Count / 2, stroke.Cusps()[-2].index); i--)
                if ((stroke[i] - stroke[0]).Length < threshold)
                    canBeLasso = true;  // the lasso has returned close enough to the starting point to make it a lasso
            return canBeLasso;
        }
        static public object Lassoed(this Stroq stroke, LassoTest test) {
            object sel = null;
            DictLookup dict = null;
            if (!stroke.Property.Exists(LASSO)) {
                dict = new DictLookup();
                stroke.Property[LASSO] = dict;
            }
            else
                dict = (DictLookup)stroke.Property[LASSO];
            if (dict.ContainsKey(test))
                sel = dict[test];
            else {
                double threshold = stroke.GetBounds().MaxDim * .2;
                Stroq lasso = stroke;
                // truncate the lasso where it retraces closest to its starting point.
                for (int i = stroke.Count - 1;  i > Math.Max(stroke.Count / 2, stroke.Cusps()[-2].index); i--)
                    if ((stroke[i] - stroke[0]).Length < threshold) {
                        List<Pt> lassoPts = new List<Pt>();
                        for (int j = 0; j < i; j++)
                            lassoPts.Add(stroke[j]);
                        lasso = new Stroq(lassoPts.ToArray());
                    }
                sel = test(lasso);
                dict.Add(test, sel);
            }
            return sel;
        }
    }
    /// <summary>
    /// Adds a method to a Stroq:
    ///     ScribbledOver(test) - the selection that has been Scribbled over if the Stroq is a Scribble
    /// </summary>
    static public class ScribbleTester {
        class DictLookup : Dictionary<ScribbleTest, object> { }
        static Guid SCRIBBLE = new Guid("afd6b74e-667a-4ab3-9cc9-2c1775c5e1b7");
        public delegate object ScribbleTest(Stroq stroke);
        static public object ScribbledOver(this Stroq stroke, ScribbleTest test) {
            object sel = null;
            DictLookup dict = null;
            if (!stroke.Property.Exists(SCRIBBLE)) {
                dict = new DictLookup();
                stroke.Property[SCRIBBLE] = dict;
            }
            else
                dict = (DictLookup)stroke.Property[SCRIBBLE];
            if (dict.ContainsKey(test))
                sel = dict[test];
            else {
                sel = test(stroke);
                dict.Add(test, sel);
            }
            return sel;
        }
    }
    /// <summary>
    /// Adds a method to a Stroq
    ///     IsInsertion - is the Stroq an upward insertion caret
    /// </summary>
    static public class InsertionTester {
        static Guid TEXT = new Guid("93187dae-4e0a-4ea2-9d41-e2b89d16a7c2");
        static bool insertionCaret(Stroq caret) {
            double dist = (caret.Cusps()[1].dist / caret.Cusps().Distance - 0.5);
            bool isCaret = caret.Cusps().Length == 3 &&
                      Math.Abs(caret.Cusps()[1].dist / caret.Cusps().Distance - 0.5) < .1 &&
                      caret.Cusps().Straightness(0, 1) < 0.15 && caret.Cusps().Straightness(1, 2) < 0.15 &&
                      caret.Cusps().inSeg(1).Direction.UnsignedAngle(new Vec(0, -1)) < Math.PI / 4 &&
                      caret.Cusps().inSeg(1).Direction.UnsignedAngle(-caret.Cusps().outSeg(1).Direction) < Math.PI / 4;
            return isCaret;
        }
        static public bool IsInsertion(this Stroq stroke) {
            if (!stroke.Property.Exists(TEXT))
                stroke.Property[TEXT] = insertionCaret(stroke);
            return (bool)stroke.Property[TEXT];
        }
    }
    /// <summary>
    /// Adds a method to a Stroq:
    ///     IsUndoRedo - is the Stroq a left-right hook or a right-left hook
    /// </summary>
    static public class LeftRightHookTester {
        static Guid UNDO = new Guid("1519751e-ab12-4dfd-bb1c-658ed32aefa4");
        static int leftRightHook(Stroq caret) {
            Pt endpt = Pt.Avg(caret[0], caret[-1]);
            int dir = (caret.Cusps().Length == 3 &&
                      Math.Abs(caret.Cusps()[1].dist / caret.Cusps().Distance - 0.5) < .1 &&
                      caret.Cusps().Straightness(0, 1) < 0.15 && caret.Cusps().Straightness(1, 2) < 0.15 &&
                      caret.Cusps().inSeg(1).Direction.UnsignedAngle(-caret.Cusps().outSeg(1).Direction) < Math.PI / 8) ?
                          (caret.Cusps()[1].pt - endpt).UnsignedAngle(new Vec(1, 0)) < Math.PI / 6 ? 1 :
                          ((caret.Cusps()[1].pt - endpt).UnsignedAngle(new Vec(-1, 0)) < Math.PI / 6 ? -1 : 0) : 0;
            return dir;
        }
        static public int IsLeftRight(this Stroq stroke) {
            if (!stroke.Property.Exists(UNDO))
                stroke.Property[UNDO] = leftRightHook(stroke);
            return (int)stroke.Property[UNDO];
        }
    }
    /// <summary>
    /// Adds a method to a Stroq:
    ///     IsUndoRedo - is the Stroq a left-right hook or a right-left hook
    /// </summary>

    /// <summary>
    /// Adds 3 methods to a Stroq:
    ///  IsCrop() - is the Stroq any of the 4 corner crop marks
    ///  BalancingCrops(crop1, crop2) -  crop1 and crop2 opposite marks, 
    ///  Cropped(crop1, crop2, test) - the selection contained between two crop marks
    /// </summary>
    static public class CropTester {
        class DictLookup : Dictionary<CropTest, object> { }
        static Guid CROP = new Guid("17bc4c2d-4b6c-46e4-9019-05923ffceed6");
        public delegate object CropTest(Stroq stroke, Stroq balance);
        static public object Cropped(this Stroq stroke, Stroq balance, CropTest test) {
            object sel = null;
            DictLookup dict = null;
            if (!stroke.Property.Exists(CROP)) {
                dict = new DictLookup();
                stroke.Property[CROP] = dict;
            }
            else
                dict = (DictLookup)stroke.Property[CROP];
            if (dict.ContainsKey(test))
                sel = dict[test];
            else {
                sel = test(stroke, balance);
                dict.Add(test, sel);
            }
            return sel;
        }
        static public bool IsCrop(this Stroq stroke) {
            if (stroke.Cusps().Length == 3 && stroke.Cusps().Straightness(0, 1) < 0.15 & stroke.Cusps().Straightness(1, 2) < 0.15 &&
                Math.Abs(stroke.Cusps().inSeg(1).Direction.UnsignedAngle(stroke.Cusps().outSeg(1).Direction) - Math.PI / 2) < Math.PI / 4)
                return true;
            return false;
        }
        static public bool BalancedCrops(this Stroq crop1, Stroq crop2) {
            if (crop2 != null &&
                (Math.Sign(crop1.Cusps()[1].curvature) == Math.Sign(crop2.Cusps()[1].curvature) ||
                ((crop1.Cusps().outSeg(1).Direction.Normal().Dot(crop2.Cusps().inSeg(1).Direction.Normal()) < -0.5) &&
                (crop1.Cusps().inSeg(1).Direction.Normal().Dot(crop2.Cusps().outSeg(1).Direction.Normal()) < -0.5))))
                return false;
            return true;
        }
    }
    /// <summary>
    /// Adds a method to a stroq:
    ///   IsTap() - is the stroke a tap
    /// </summary>
    static public class TapTester {
        static Guid IS_TAP = new Guid("cdec3f8b-b8d1-4c79-a808-418d20ccb0c9");
        static public bool IsTap(this Stroq stroke) {
            if (!stroke.Property.Exists(IS_TAP)) {
                bool tap = stroke.GetBounds().MaxDim < 15;
                stroke.Property[IS_TAP] = tap;
            }
            return (bool)stroke.Property[IS_TAP];
        }
    }
    /// <summary>
    /// Adds a method to a stroq:
    ///   IsFlick() - is the stroke a flick
    /// </summary>
    static public class FlickTester {
        static Guid IS_FLICK = new Guid("0EEA6653-E32B-4edc-B01F-24E3977D9F30");
        static public bool IsFlick(this Stroq stroke) {
            if (!stroke.Property.Exists(IS_FLICK)) {
                bool flick = stroke.Cusps().Length == 2 && stroke.Cusps().Straightness(0, 1) < 0.15 &&
                    stroke.Cusps().Distance > 75 &&
                    stroke.Count * 16 < 353 &&
                    (stroke.Cusps()[1].pt - stroke.Cusps()[0].pt).Normal().Dot(new Vec(1, -1).Normal()) > 0.95;
                stroke.Property[IS_FLICK] = flick;
            }
            return (bool)stroke.Property[IS_FLICK];
        }
    }
    /// <summary>
    /// Adds a method to a stroq:
    ///   IsCircle(stroke) - is the stroke a circle
    /// </summary>
    static public class CircleTester {
        static Guid IS_CIRCLE = new Guid("21204783-F8B8-4c2d-9FBF-2C2313D7FCD1");
        static public bool IsCircle(this Stroq stroke) {
            if (!stroke.Property.Exists(IS_CIRCLE)) {
                bool circle = (stroke[0] - stroke[-1]).Length / stroke.GetBounds().MaxDim < .4;
                var csps = stroke.Cusps();
                //for (int i = 2; i < stroke.Cusps().Length - 1 &&  circle; i++) {
                //    if (Math.Abs(stroke.Cusps()[i].curvature) > 0.6)
                //        circle = false;
                //    if (Math.Sign(stroke.Cusps()[i].curvature) != Math.Sign(stroke.Cusps()[i - 1].curvature))
                //        circle = false;
                //}

                stroke.Property[IS_CIRCLE] = circle;
            }
            return (bool)stroke.Property[IS_CIRCLE];
        }
    }
    /// <summary>
    /// Adds a method to a stroq:
    ///   IsChar(string charset) - is the stroke when recognized in the charset
    /// </summary>
    static public class CharTester {
        static public bool IsChar(this Stroq stroke, string charset) {
            System.Windows.Ink.InkAnalyzer ia = new InkAnalyzer();
            ia.AddStroke(stroke.BackingStroke);
            //AnalysisHintNode node = ia.CreateAnalysisHint();
            //node.Factoid = "IS_ONECHAR";
            //node.CoerceToFactoid = true;
            //node.Location.MakeInfinite();
            AnalysisStatus astat = ia.Analyze();
            string reco = ia.GetRecognizedString();
            Console.WriteLine("Recognized:<" + reco + ">");
            if (astat.Successful && reco != "Other")
                return charset.Contains(reco[0]);
            return false;
        }
    }
}