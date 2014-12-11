using starPadSDK.AppLib;
using starPadSDK.Geom;
using starPadSDK.Inq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PanoramicData.utils.inq
{
    public class InkTableSegmenter
    {
        //public static double MIN_SEGMENTATION_DISTANCE = 15;
        public static double Y_SEGMENTATION_THRESHOLD = Properties.Settings.Default.InkTableSegmenterYThreshold;
        public static double Y_SEGMENTATION_THRESHOLD_2 = Y_SEGMENTATION_THRESHOLD * 2;

        public static double X_SEGMENTATION_THRESHOLD = Properties.Settings.Default.InkTableSegmenterXThreshold;
        public static double X_SEGMENTATION_THRESHOLD_2 = X_SEGMENTATION_THRESHOLD * 2;

        private InkTableContentCollection _tableContent = new InkTableContentCollection();

        public InkTableContentCollection GetInkTableContent()
        {
            return _tableContent;
        }

        public List<GeoAPI.Geometries.IPolygon> removeInput(InkTableContent content, out int numberOfRows, out int numberOfColumns)
        {
            _tableContent.Remove(content);
            return runSegmentation(_tableContent, out numberOfRows, out numberOfColumns);
        }

        public List<GeoAPI.Geometries.IPolygon> removeInput(InkTableContentCollection contents, out int numberOfRows, out int numberOfColumns)
        {
            foreach (var c in contents)
            {
                _tableContent.Remove(c);
            }
            return runSegmentation(_tableContent, out numberOfRows, out numberOfColumns);
        }

        Polyline pl = null;
        Polyline pl1 = null;
        Polyline pl2 = null;
        public List<GeoAPI.Geometries.IPolygon> addInput(InkTableContent content, InqScene inqScene, out int numberOfRows, out int numberOfColumns)
        {
            List<GeoAPI.Geometries.IPolygon> segmentation = runSegmentation(_tableContent, out numberOfRows, out numberOfColumns);
            Rct contentBounds = content.SegmentationBound.InflatedBounds;
            GeoAPI.Geometries.IGeometry geom = contentBounds.GetPolygon();

            List<GeoAPI.Geometries.IGeometry> intersectedCells = new List<GeoAPI.Geometries.IGeometry>();
            foreach (var p in segmentation)
            {
                if (p.Intersects(geom))
                {
                    GeoAPI.Geometries.IGeometry inter = p.Intersection(geom);
                    intersectedCells.Add(p);
                }
            }

            if (intersectedCells.Count == 2 && content.Object is Stroq)
            {
                GeoAPI.Geometries.IGeometry first = intersectedCells[0].GetBounds().Union(geom.GetBounds()).GetPolygon();
                GeoAPI.Geometries.IGeometry second = intersectedCells[1].GetBounds().Union(geom.GetBounds()).GetPolygon();

                /*if (pl1 != null)
                {
                    inqScene.Rem(pl1);
                }
                pl1 = new Polyline();
                pl1.Points = new PointCollection(first.Coordinates.Select((corrd) => new Point(corrd.X, corrd.Y)).ToArray());
                pl1.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0));
                inqScene.AddNoUndo(pl1);*/

                /*if (pl2 != null)
                {
                    inqScene.Rem(pl2);
                }
                pl2 = new Polyline();
                pl2.Points = new PointCollection(second.Coordinates.Select((corrd) => new Point(corrd.X, corrd.Y)).ToArray());
                pl2.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 255));
                inqScene.AddNoUndo(pl2);*/


                GeoAPI.Geometries.IGeometry inter = first.Intersection(second);

                GeoAPI.Geometries.IGeometry t1 = inter.Intersection(intersectedCells[0]);

                /*if (pl1 != null)
                {
                    inqScene.Rem(pl1);
                }
                pl1 = new Polyline();
                pl1.Points = new PointCollection(t1.Coordinates.Select((corrd) => new Point(corrd.X, corrd.Y)).ToArray());
                pl1.Fill = new SolidColorBrush(Color.FromArgb(100, 255, 0, 0));
                inqScene.AddNoUndo(pl1);*/

                GeoAPI.Geometries.IGeometry t2 = inter.Intersection(intersectedCells[1]);

                /*if (pl2 != null)
                {
                    inqScene.Rem(pl1);
                }
                pl2 = new Polyline();
                pl2.Points = new PointCollection(t2.Coordinates.Select((corrd) => new Point(corrd.X, corrd.Y)).ToArray());
                pl2.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 255, 0));
                inqScene.AddNoUndo(pl2);*/


                Rct areaToExclude = new Rct();
                Rct cellToBeExludedFrom;
                Rct cellToBeIncludedTo;
                if (t1.Area > t2.Area)
                {
                    cellToBeExludedFrom = intersectedCells[1].GetBounds();
                    cellToBeIncludedTo = intersectedCells[0].GetBounds();
                }
                else
                {
                    cellToBeExludedFrom = intersectedCells[0].GetBounds();
                    cellToBeIncludedTo = intersectedCells[1].GetBounds();
                }
                if (!cellToBeExludedFrom.Inflate(3000, 0).IntersectsWith(cellToBeIncludedTo))
                {
                    areaToExclude = cellToBeExludedFrom.Inflate(3000, 0).Intersection(contentBounds);
                }
                if (!cellToBeExludedFrom.Inflate(0, 3000).IntersectsWith(cellToBeIncludedTo))
                {
                    areaToExclude = cellToBeExludedFrom.Inflate(0, 3000).Intersection(contentBounds);
                }
                areaToExclude = areaToExclude.Inflate(X_SEGMENTATION_THRESHOLD_2, Y_SEGMENTATION_THRESHOLD_2);

                /*if (pl != null)
                {
                    inqScene.Rem(pl);
                }
                pl = new Polyline();
                pl.Points = new PointCollection(areaToExclude.GetPolygon().Coordinates.Select((corrd) => new Point(corrd.X, corrd.Y)).ToArray());
                pl.Fill = new SolidColorBrush(Color.FromArgb(100, 0, 0, 255));
                inqScene.AddNoUndo(pl);*/

                List<float> pressureFactors = new List<float>();
                Stroq stroq = content.Object as Stroq;
                for (int i = 0; i < stroq.Count; i++)
                {
                    if (areaToExclude.IntersectsWith(new Rct(stroq[i], new Vec(1, 1))))
                    {
                        pressureFactors.Add(stroq.StylusPoints[i].PressureFactor);
                    }
                    else
                    {
                        pressureFactors.Add(0);
                    }
                }

                stroq.SecondRenderPass = true;
                stroq.SecondRenderPassPressureFactors = pressureFactors;
                stroq.SecondRenderPassColor = Colors.DarkGray;
                StroqCache.UpdateCache(stroq);

            }

            //Console.WriteLine("YYY : " + intersections.Count() + " " + totalIntersectionArea +  " " + geom.Area);

            _tableContent.Add(content);
            return runSegmentation(_tableContent, out numberOfRows, out numberOfColumns);
        }

        public List<GeoAPI.Geometries.IPolygon> addInput(InkTableContentCollection contents, out int numberOfRows, out int numberOfColumns)
        {
            _tableContent.Add(contents);
            return runSegmentation(_tableContent, out numberOfRows, out numberOfColumns);
        }

        public List<GeoAPI.Geometries.IPolygon> runSegmentation(InkTableContentCollection contents, out int numberOfRows, out int numberOfColumns)
        {
            List<GeoAPI.Geometries.IPolygon> segmentation = new List<GeoAPI.Geometries.IPolygon>();
            if (contents.Count == 0)
            {
                numberOfColumns = 0;
                numberOfRows = 0;
                return segmentation;
            }
            var histResult = getHistogram(contents);
            bool[][] hists = histResult.Key;
            Rct allInputBounds = histResult.Value;

            List<Range> cols = getRanges(hists[0]);
            List<Range> rows = getRanges(hists[1]);

            double yOffset = 0.0;
            int nrOfRows = 0;
            int nrOfCols = 0;
            foreach (var row in rows)
            {
                if (row.Ink)
                {
                    nrOfRows++;
                    nrOfCols = 0;
                    double xOffset = 0.0;
                    foreach (var col in cols)
                    {
                        if (col.Ink)
                        {
                            nrOfCols++;
                            Rect r = new Rect(allInputBounds.TopLeft.X + xOffset,
                                              allInputBounds.TopLeft.Y + yOffset,
                                              col.Size, row.Size);

                            segmentation.Add(r.GetPolygon());
                        }
                        xOffset += col.Size;
                    }
                }
                yOffset += row.Size;
            }

            Console.WriteLine("ROWS : " + nrOfRows + " / COLS : " + nrOfCols);
            numberOfColumns = nrOfCols;
            numberOfRows = nrOfRows;
            return segmentation;
        }

        private KeyValuePair<bool[][], Rct> getHistogram(InkTableContentCollection contents)
        {
            List<Rct> bounds = new List<Rct>();

            // calculate the group boundaries
            List<InkTableContentGroup> allGroups = GetAllInkTableContentGroups(contents);
            foreach (var group in allGroups)
            {
                bounds.Add(GetSegmentationBoundsUnion(group.InkTableContents, true));
                foreach (var c in group.InkTableContents)
                {
                    if (!_tableContent.Contains(c))
                    {
                        _tableContent.Add(c);
                    }
                }
            }

            // calculate the label boundaries
            List<Label> allLabels = GetAllLabels(contents);
            foreach (var label in allLabels)
            {
                bounds.Add(GetSegmentationBoundsUnion(label.InkTableContents, true));
                foreach (var c in label.InkTableContents)
                {
                    if (!_tableContent.Contains(c))
                    {
                        _tableContent.Add(c);
                    }
                }
            }

            // calculate stroq boundaries
            foreach (var content in contents)
            {
                bounds.Add(content.SegmentationBound.InflatedBounds);
            }

            Rct allInputBounds = GetSegmentationBoundsUnion(contents, true);

            bool[] yHist2 = new bool[(int)Math.Ceiling(allInputBounds.Height)];
            bool[] xHist2 = new bool[(int)Math.Ceiling(allInputBounds.Width)];

            foreach (var b in bounds)
            {
                double yFrom = b.Top - allInputBounds.Top;
                double yTo = yFrom + b.Height;
                for (int y = (int)Math.Floor(yFrom); y < (int)Math.Ceiling(yTo); y++)
                {
                    yHist2[y] = true;
                }

                double xFrom = b.Left - allInputBounds.Left;
                double xTo = xFrom + b.Width;
                for (int x = (int)Math.Floor(xFrom); x < (int)Math.Ceiling(xTo); x++)
                {
                    xHist2[x] = true;
                }
            }

            return new KeyValuePair<bool[][], Rct>(new bool[][] { xHist2, yHist2 }, allInputBounds);
        }

        private List<Range> getRanges(bool[] hist)
        {
            List<Range> ret = new List<Range>();
            Range r = new Range();
            bool ink = hist[0];
            double size = 0.0;
            for (int i = 0; i < hist.Length; i++)
            {
                bool flipped = false;
                if (hist[i] && !ink)
                {
                    flipped = true;
                }
                else if (!hist[i] && ink)
                {
                    flipped = true;
                }
                if (flipped)
                {
                    r.Size = size;
                    r.Ink = ink;
                    ret.Add(r);
                    r = new Range();

                    ink = !ink;
                    size = 1.0;
                }
                else
                {
                    size += 1;
                }
            }
            r.Size = size;
            r.Ink = ink;
            ret.Add(r);
            return ret;
        }

        public static Rct GetSegmentationBoundsUnion(InkTableContentCollection contents, bool inflated)
        {
            Rct r = Rct.Null;
            foreach (var c in contents)
            {
                if (inflated)
                {
                    r = r.Union(c.SegmentationBound.InflatedBounds);
                }
                else
                {
                    r = r.Union(c.SegmentationBound.Bounds);
                }
            }
            return r;
        }

        public static List<InkTableContentGroup> GetAllInkTableContentGroups(InkTableContentCollection contents)
        {
            // figure out which contents are grouped
            List<InkTableContentGroup> allGroups = new List<InkTableContentGroup>();
            foreach (var c in contents)
            {
                foreach (var group in c.InkTableContentGroups)
                {
                    allGroups.Add(group);
                }
            }
            return allGroups;
        }

        public static List<Label> GetAllLabels(InkTableContentCollection contents)
        {
            // figure out which stroqs are grouped
            List<Label> allLabels = new List<Label>();
            foreach (var c in contents)
            {
                if (c.Label != null)
                {
                    allLabels.Add(c.Label);
                }
            }
            return allLabels;
        }

        public class Range
        {
            public double Size { get; set; }
            public bool Ink { get; set; }
        }
    }
}
