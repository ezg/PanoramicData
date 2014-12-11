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
using starPadSDK.MathExpr;
using System.Windows.Input;
using System.Windows.Input.StylusPlugIns;
using starPadSDK.Inq.MSInkCompat;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using starPadSDK.Inq.BobsCusps;
using starPadSDK.WPFHelp;
using System.IO;
using starPadSDK.MathExpr.ExprWPF;
using starPadSDK.MathExpr.ExprWPF.Boxes;
using starPadSDK.MathRecognizer;
using starPadSDK.CharRecognizer;
using starPadSDK.UnicodeNs;
using Constant = starPadSDK.MathExpr.MathConstant;
using Microsoft.Research.DynamicDataDisplay.Charts;
using Microsoft.Research.DynamicDataDisplay;
using Microsoft.Research.DynamicDataDisplay.PointMarkers;
using Microsoft.Research.DynamicDataDisplay.DataSources;

namespace starPadSDK.AppLib
{
    public partial class MathEditor : CommandSet.CommandEditor
    {
        const int smallFontSize = 25;
        const int largeFontSize = 55;
        public enum TwoFingerOp {
            Pinching,
            Stretching,
            Graphing,
            Fixed
        };

        public struct Folds
        {
            public Image top;
            public Image bottom;
            public double yStart;
            public double yEnd;
            public double height3D;
            public double height2D;
            public List<ExprTags> expressionsStored;
            public bool isFinal;
            public Image final;
        }

        public LinkedList<Folds> foldedAreas = new LinkedList<Folds>();

        public TwoFingerOp TwoFingers = TwoFingerOp.Fixed;
        public ContainerVisualHost firstGhostTerm = new ContainerVisualHost();
        public ContainerVisualHost outputVisualExpr = null; // the expression that results from a draggin action
        public Rectangle hitExprBoundsVisual = null;
        public Rectangle hitExprBoundsVisual2 = null;

        public int SmallFontSize { get { return smallFontSize; } } 
        public int LargeFontSize { get { return largeFontSize; } }

        public void dragSingleTermTo(ContainerVisualHost frozenVisual, EWPF.HitBox hitExpr, ExprTags exprTags, Pt center, TwoFingerOp splitting)
        {
            if (hitExpr == null)
                return;
            TermColorizer.ClearFactorMarks(hitExpr.Path[0], exprTags.NumIterationsFromOriginal);
            TermColorizer.MarkFactor(hitExpr.HitExpr, exprTags.NumIterationsFromOriginal);
            EWPF.UpdateVisual(frozenVisual, exprTags.Expr, exprTags.FontSize, Colors.Black, Brushes.Black);
            outputVisualExpr = null;
            bool leadingTerm = center.X > (((Mat)frozenVisual.RenderTransform.Value)*hitExpr.Box.bbox.TopLeft).X && hitExpr.Path.Count > 1 && hitExpr.Path[hitExpr.Path.Count-2] is CompositeExpr && hitExpr.Box.Expr == hitExpr.Path[hitExpr.Path.Count - 2].Args()[0];
            bool allowReorder = TermDraggingMode == TermDragMode.SplitIntoFraction || 
                (TermDraggingMode == TermDragMode.Default && (leadingTerm || hitExpr.Box.ExprIx is int ||
                hitExpr.Path[hitExpr.Path.Count - 1].Head() == WellKnownSym.times ||
                (hitExpr.Path.Count > 1 && hitExpr.Path[hitExpr.Path.Count - 2].Head() == WellKnownSym.times)));
            Expr newExpr = dragOneContact(frozenVisual, firstGhostTerm, exprTags.Expr, hitExpr.Path[0], hitExpr.HitExpr, exprTags, center, splitting, allowReorder);
            Expr factorTerm = hitExpr.HitExpr;
            // display the result
            if (newExpr != null) {
                outputVisualExpr = displayModifiedExpr(newExpr.Clone(), new Pt(exprTags.Offset.X, exprTags.Offset.Y + frozenVisual.Height + 10), largeFontSize, exprTags.Id, exprTags.NumIterationsFromOriginal + 1, true);

                firstGhostTerm = updateGhostTerm(firstGhostTerm, exprTags.FontSize, center, factorTerm);
            }  
        }
        public void dragPairOfTermsTo(ContainerVisualHost currentExprVisual, EWPF.HitBox firstHitExpr, EWPF.HitBox secondHitExpr,
                                      ExprTags exprTags, Pt center, Pt ghostCenter, bool isChangeValue) {
            Expr factorTerm = null;
            Expr newExpr = dragTwoContacts(currentExprVisual, exprTags, firstHitExpr.Path[0], center,
                                           firstHitExpr.HitExpr, secondHitExpr.HitExpr, firstGhostTerm, TwoFingers);
            //if (TwoFingers == TwoFingerOp.Graphing) {
            //    if (StartGraphDraggingEvent != null)
            //        StartGraphDraggingEvent(currentExprVisual);
            //    MathUICanvas.Children.Remove(firstGhostTerm);
            //    MathUICanvas.Children.Remove(outputVisualExpr);
            //    MathUICanvas.Children.Remove(hitExprBoundsVisual);
            //    MathUICanvas.Children.Remove(hitExprBoundsVisual2);
            //    newExpr = null;
            //} else 
            if (TwoFingers != TwoFingerOp.Fixed && newExpr != null)  // the factorTerm may have changed when terms are pinched together
                factorTerm = TermColorizer.FindFactorTerm(newExpr);

            // display the result
            if (newExpr != null) {
                outputVisualExpr = displayModifiedExpr(newExpr, new Pt(exprTags.Offset.X, exprTags.Offset.Y + currentExprVisual.Height + 10), smallFontSize, exprTags.Id, exprTags.NumIterationsFromOriginal + 1, true);

                if (!isChangeValue)
                    firstGhostTerm = updateGhostTerm(firstGhostTerm, exprTags.FontSize, center, factorTerm);
                else
                    firstGhostTerm = updateGhostTerm(firstGhostTerm, exprTags.FontSize, ghostCenter, factorTerm);

            }
        }

        public ContainerVisualHost updateGhostTerm(ContainerVisualHost ghostTerm, double fontSize, Pt center, Expr factorTerm)
        {
            EWPF.HitBox curGhostTerm = ghostTerm.Tag as EWPF.HitBox;
            if (curGhostTerm.HitExpr != factorTerm && factorTerm != null) {
                EWPF.HitBox newGhostTerm = EWPF.FindTermBox(factorTerm, fontSize, factorTerm);
                if (newGhostTerm != null) {
                    if (ghostTerm != null)
                        undisplayExpr(ghostTerm);
                    ghostTerm = CreateGhostTerm(center, fontSize, newGhostTerm);
                }
            }
            return ghostTerm;
        }
        public void createNextExpr(ContainerVisualHost frozenVisual) {
            Boolean firstOFfset = false;
            ExprTags exprTags = frozenVisual.Tag as ExprTags;
            outputVisualExpr = null;

            undisplayExpr(firstGhostTerm);
            _mathUICanvas.Children.Remove(hitExprBoundsVisual);
            _mathUICanvas.Children.Remove(hitExprBoundsVisual2);
            undisplayExpr(frozenVisual);
            ContainerVisualHost largeExpr  = null;
            if (exprTags.NumIterationsFromOriginal > 1)
                TermColorizer.ClearFactorMarks(exprTags.Expr, exprTags.NumIterationsFromOriginal - 2);
            largeExpr = displayModifiedExpr(exprTags.Expr, new Pt(exprTags.Offset.X, exprTags.Offset.Y), largeFontSize, exprTags.Id, exprTags.NumIterationsFromOriginal, false);
           
            if (UnfocusExprEvent != null)
                UnfocusExprEvent(frozenVisual);

            Dictionary<Guid, ContainerVisualHost> oldExprs = new Dictionary<Guid, ContainerVisualHost>();
            SortedList<int, ContainerVisualHost> theExprs = new SortedList<int, ContainerVisualHost>();
            foreach (ContainerVisualHost visual in _mathUICanvas.Children.OfType<ContainerVisualHost>().ToArray())
                if (visual.Tag != null && visual.Tag is ExprTags && exprTags.Id == (visual.Tag as ExprTags).Id && (visual.Tag as ExprTags).Color != Colors.Blue) {
                    ExprTags currentExprTags = visual.Tag as ExprTags;
                    theExprs.Add(currentExprTags.NumIterationsFromOriginal, visual);
                }
            foreach (Canvas visual in _mathUICanvas.Children.OfType<Canvas>().ToArray())
                if (visual.Tag != (object)exprTags.Id) {
                    _mathUICanvas.Children.Remove(visual);
                }
            if (exprTags.NumIterationsFromOriginal == 0 && exprTags.FontSize == largeFontSize)
            {
                _mathUICanvas.Children.Remove(largeExpr);
                _mrec.ForceParse();
                return;
            }
            Pt offset = new Pt();
            int exprCt = theExprs.Values.Count;
            int counter = 1;
            int scale = 2;
            Pt oldOffset = new Pt();
            double oldHeight = 0;
            Expr prevExpr = null;
            ContainerVisualHost prevHost = null;
            double prevFontSize = 0;
            Mat prevXf = Mat.Identity;
            int colorInd = -1;
            foreach (ContainerVisualHost visual in theExprs.Values) {
                    ExprTags currentExprTags = visual.Tag as ExprTags;
                    if (foldedExpr.Contains(currentExprTags))
                        continue;
                    ContainerVisualHost oldExpr = oldExprs.ContainsKey(currentExprTags.Id) ? oldExprs[currentExprTags.Id] : null;
                    offset = offset == new Pt() ? (Pt)currentExprTags.Offset : offset;
                    if (counter != 1)
                        offset = new Pt(offset.X, oldOffset.Y + oldHeight + oldHeight*0.1);

                    foreach (Folds f in foldedAreas)
                    {
                        if ((offset.Y + oldHeight > f.yStart && offset.Y + oldHeight < f.yEnd && !firstOFfset)
                            ||(offset.Y > f.yStart && offset.Y < f.yEnd && !firstOFfset)
                            ||(offset.Y > f.yEnd && !firstOFfset))
                        {
                            offset.Y = f.yEnd;
                            firstOFfset = true;
                        }

                    }

                    double fontSize = currentExprTags != largeExpr.Tag ?  Math.Max(1,smallFontSize - scale * (exprCt - counter)) : largeFontSize;
                    ContainerVisualHost newerExpr = createNewExpr(currentExprTags.Id, currentExprTags.Expr, offset, fontSize, currentExprTags.NumIterationsFromOriginal);
                    FrameworkElement page = _mathUICanvas.Parent as FrameworkElement;
                    if (page == null)
                        continue;
                    if (offset.Y+newerExpr.Height > page.Height)
                        page.Height += (offset.Y + newerExpr.Height - page.Height) + 10;

                    if (offset.X + newerExpr.Width > page.Width)
                        page.Width += (offset.X + newerExpr.Width - page.Width) + 10;

                    _mathUICanvas.Children.Add(newerExpr);
                    undisplayExpr(visual);
                    oldOffset = offset;
                    oldHeight = newerExpr.Height;
                    offset = WPFUtil.GetBounds(newerExpr).BottomLeft + new Vec(0, 10);
                    counter++;
                    highlightDerivationChanges(exprTags,ref prevExpr, ref prevHost, ref prevFontSize, ref prevXf, colorInd++, fontSize, newerExpr);
            }
            UpdateComputations();
        }

        List<ExprTags> foldedExpr = new List<ExprTags>();
        List<ContainerVisualHost> hideList = new List<ContainerVisualHost>();

        DirectionalLight tempLight1 = null;
        DirectionalLight tempLight2 = null;
        Viewport2DVisual3D top3d = null;
        Viewport2DVisual3D bottom3d = null;
        Viewport3D tempViewPort = null;
        Folds newFold = new Folds();
        ContainerVisualHost lastFrozen = null;
        double midPoint = 0;

        public Folds collapseExpressions(Pt start, Pt end)
        {
            newFold = new Folds();
            newFold.yStart = start.Y;
            newFold.yEnd = end.Y;
            newFold.expressionsStored = new List<ExprTags>();
            newFold.isFinal = false;
 
            FrameworkElement page = _mathUICanvas.Parent as FrameworkElement;         

            Dictionary<Guid, ContainerVisualHost> oldExprs = new Dictionary<Guid, ContainerVisualHost>();
            SortedList<int, ContainerVisualHost> theExprs = new SortedList<int, ContainerVisualHost>();
            foreach (ContainerVisualHost visual in _mathUICanvas.Children.OfType<ContainerVisualHost>().ToArray())
                if (visual.Tag != null && visual.Tag is ExprTags )
                {
                    ExprTags currentExprTags = visual.Tag as ExprTags;
                    theExprs.Add(currentExprTags.NumIterationsFromOriginal, visual);
                }
            if (theExprs.Values.Count == 0)
                return new Folds();

            ContainerVisualHost largeExpr = theExprs.Values.Last();
            Pt offset = new Pt();
            int exprCt = theExprs.Values.Count;
            int counter = 1;
            int scale = 2;
            Pt oldOffset = new Pt();
            double oldHeight = 0;
            foreach (ContainerVisualHost visual in theExprs.Values)
            {
                ExprTags currentExprTags = visual.Tag as ExprTags;
                ContainerVisualHost oldExpr = oldExprs.ContainsKey(currentExprTags.Id) ? oldExprs[currentExprTags.Id] : null;
                offset = offset == new Pt() ? (Pt)currentExprTags.Offset : offset;
                if (counter != 1)
                    offset = new Pt(offset.X, oldOffset.Y + oldHeight + oldHeight * 0.1);

                ContainerVisualHost newerExpr = createNewExpr(currentExprTags.Id, currentExprTags.Expr, offset, currentExprTags != largeExpr.Tag ?
                    Math.Max(1, smallFontSize - scale * (exprCt - counter)) : largeFontSize, currentExprTags.NumIterationsFromOriginal);

                if (offset.Y + newerExpr.Height > page.Height)
                    page.Height += (offset.Y + newerExpr.Height - page.Height) + 10;

                if (offset.X + newerExpr.Width > page.Width)
                    page.Width += (offset.X + newerExpr.Width - page.Width) + 10;

                _mathUICanvas.Children.Add(newerExpr);
                undisplayExpr(visual);
                oldHeight = newerExpr.Height;
                oldExpr = newerExpr;
                oldOffset = offset;
                offset = WPFUtil.GetBounds(newerExpr).BottomLeft + new Vec(0, 10);
                counter++;

                lastFrozen = newerExpr;

                if ((offset.Y >= start.Y && offset.Y <= end.Y)||(offset.Y + newerExpr.Height >= start.Y && offset.Y + newerExpr.Height <= end.Y))
                {
                    if (offset.Y + newerExpr.Height > end.Y)
                    {
                        end.Y = offset.Y + newerExpr.Height;
                        newFold.yEnd = end.Y;
                    }
                    if (offset.Y < start.Y)
                    {
                        start.Y = offset.Y;
                        newFold.yStart = start.Y;
                    }
                    hideList.Add(newerExpr);
                    foldedExpr.Add(newerExpr.Tag as ExprTags);
                    newFold.expressionsStored.Add(newerExpr.Tag as ExprTags);
                }
            }

            //below here all 3d stuff
            const double angle = 0;

            Vector3D normal = new Vector3D(0, 0, 1);

            tempViewPort = new Viewport3D();
            tempViewPort.ClipToBounds = false;
            tempViewPort.Height = page.Height;
            tempViewPort.Width = page.Width;

            Model3DGroup tempModel3D = new Model3DGroup();
            tempViewPort.Camera = (new PerspectiveCamera(new Point3D(0, 0, 1), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0), 90));

            ModelVisual3D light1 = new ModelVisual3D();
            tempLight1 = new DirectionalLight(Colors.White, new Vector3D(0, -1, -1));
            tempLight1.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(-1, 0, 0), angle));
            light1.Content = tempLight1;
            tempViewPort.Children.Add(light1);

            ModelVisual3D light2 = new ModelVisual3D();
            tempLight2 = new DirectionalLight(Colors.White, new Vector3D(0, 3, -1));
            tempLight2.Transform = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), angle));
            light2.Content = tempLight2;
            tempViewPort.Children.Add(light2);

            //update the fold areas
            System.Drawing.Bitmap foldImg = BasicSnapshot.TakeSnapshot(page);
            newFold.top = foldImg.ConvertBitmapToWPFImage(page.Width);
            newFold.top.Clip = new RectangleGeometry(new Rect(new Point(0, start.Y), new Point(page.Width, (start.Y + end.Y) / 2)));
            double ratio = Math.Abs(start.Y - end.Y) /  page.Width;

            Point3D topLeft = new Point3D(-1,1*ratio,0);
            Point3D topRight = new Point3D(1, 1 * ratio, 0);
            Point3D midLeft = new Point3D(-1, 0, 0);
            Point3D midRight = new Point3D(1, 0, 0);
            Point3D bottomLeft = new Point3D(-1, -1 * ratio, 0);
            Point3D bottomRight = new Point3D(1, -1 * ratio, 0);
            newFold.height3D = midLeft.Y - topLeft.Y;
            newFold.height2D = Math.Abs(end.Y - start.Y);
            
            top3d = Create3DImage(newFold.top, topLeft, midLeft, midRight, topRight);

            ModelVisual3D container1 = new ModelVisual3D();
            container1.Children.Add(top3d);
            tempViewPort.Children.Add(container1);

              
            newFold.bottom = foldImg.ConvertBitmapToWPFImage(page.Width);
            newFold.bottom.Clip = new RectangleGeometry(new Rect(new Point(0, (start.Y + end.Y) / 2), new Point(page.Width, end.Y)));
            
            bottom3d = Create3DImage(newFold.bottom, midLeft,bottomLeft,bottomRight, midRight);
            ModelVisual3D container2 = new ModelVisual3D();
            container2.Children.Add(bottom3d);
            tempViewPort.Children.Add(container2);

            tempViewPort.RenderTransform = new TranslateTransform(0,(start.Y + end.Y)/2 - page.ActualHeight/2 );
            midPoint = (start.Y + end.Y) / 2 - page.ActualHeight / 2;

            _mathUICanvas.Children.Add(tempViewPort);

            foreach (ContainerVisualHost i in hideList)
            {
                i.Visibility = Visibility.Hidden;
            }
            UpdateComputations();
            foldedAreas.AddLast(newFold);
            return newFold;
        }


        public void RotatePage(double angle, double moveUp)
        {
            if (foldedAreas.Count == 0)
                return;
            Folds currentFold = foldedAreas.Last();
            tempViewPort.RenderTransform = new TranslateTransform(0, midPoint + (-currentFold.height2D + Math.Sin((90 - angle) / 180 * Math.PI) * currentFold.height2D)/2);// -200);//(Math.Sin((90 - angle) / 180 * Math.PI) * moveUp) - moveUp);
            Console.WriteLine((-currentFold.height2D + Math.Sin((90 - angle) / 180 * Math.PI) * currentFold.height2D) / 2);
            currentFold.yEnd = currentFold.yStart + Math.Sin((90 - angle) / 180 * Math.PI) * currentFold.height2D;
            Transform3DGroup tg = new Transform3DGroup();
            tg.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(-1, 0, 0), angle)));
            tg.Children.Add(new TranslateTransform3D(0, 0, -currentFold.height3D * Math.Sin(angle / 180 * Math.PI)));
            Transform3DGroup tg2 = new Transform3DGroup();
            tg2.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(-1, 0, 0), -angle)));
            tg2.Children.Add(new TranslateTransform3D(0, 0, -currentFold.height3D * Math.Sin(angle / 180 * Math.PI)));
            top3d.Transform = tg;
         
            bottom3d.Transform = tg2;
            foldedAreas.RemoveLast();
            foldedAreas.AddLast(currentFold);
            createNextExpr(lastFrozen);
        }

        public void CreateFinalFoldImage()
        {
            if (foldedAreas.Count == 0)
                return;
            if (foldedAreas.Last().isFinal)
                return;
            Folds currentFold = foldedAreas.Last();
            currentFold.isFinal = true;
            FrameworkElement page = _mathUICanvas.Parent as FrameworkElement;
            System.Drawing.Bitmap foldImg = BasicSnapshot.TakeSnapshot(page);
            Image im = foldImg.ConvertBitmapToWPFImage(page.Width);
            im.Clip = new RectangleGeometry(new Rect(new Point(-100, currentFold.yStart), new Point(page.Width+100, currentFold.yEnd)));
            currentFold.final = im;
            //Contacts.AddPreviewContactDownHandler(im, downFold);
            //Contacts.AddPreviewContactUpHandler(im, unfoldTap);
            foldedAreas.RemoveLast();
            foldedAreas.AddLast(currentFold);
            _mathUICanvas.Children.Remove(tempViewPort);
            _mathUICanvas.Children.Add(im);

            tempViewPort = null;
            tempLight1 = null;
            tempLight2 = null;
            
        }

        //void downFold(Object sender, ContactEventArgs e)
        //{
        //    Contacts.CaptureContact(e.Contact, sender as IInputElement);
        //    e.Handled = true;
        //}

        //void unfoldTap(Object sender, ContactEventArgs e)
        //{
        //    Image currentIm = sender as Image;
        //    List<Folds> removeList = new List<Folds>();
        //    foreach (Folds i in foldedAreas)
        //    {
        //        if (i.final == currentIm)
        //        {
        //            foreach (ExprTags expr in i.expressionsStored)
        //            {
        //                foldedExpr.Remove(expr);
        //            }
        //            _mathUICanvas.Children.Remove(currentIm);
        //            removeList.Add(i);
        //        }
        //    }
        //    foreach (Folds i in removeList)
        //        foldedAreas.Remove(i);

        //    createNextExpr(lastFrozen);
        //}

        private Viewport2DVisual3D Create3DImage(Image im, Point3D first, Point3D second, Point3D third, Point3D fourth)
        {
            Viewport2DVisual3D currentView = new Viewport2DVisual3D();
            DiffuseMaterial topMaterial = new DiffuseMaterial(Brushes.White);
            topMaterial.SetValue(Viewport2DVisual3D.IsVisualHostMaterialProperty, true);
            currentView.Material = topMaterial;
            currentView.Visual = im;
            currentView.Material.SetValue(Viewport2DVisual3D.IsVisualHostMaterialProperty, true);

            MeshGeometry3D topMesh = new MeshGeometry3D();
            topMesh.Positions.Add(first);
            topMesh.Positions.Add(second);
            topMesh.Positions.Add(third);
            topMesh.Positions.Add(fourth);

            topMesh.TriangleIndices.Add(0);
            topMesh.TriangleIndices.Add(1);
            topMesh.TriangleIndices.Add(2);

            topMesh.TriangleIndices.Add(0);
            topMesh.TriangleIndices.Add(2);
            topMesh.TriangleIndices.Add(3);

            topMesh.TextureCoordinates.Add(new Point(0, 0));
            topMesh.TextureCoordinates.Add(new Point(0, 1));
            topMesh.TextureCoordinates.Add(new Point(1, 1));
            topMesh.TextureCoordinates.Add(new Point(1, 0));

            currentView.Geometry = topMesh;
            return currentView;
        }

        private void highlightDerivationChanges(ExprTags exprTags, ref Expr prevExpr, ref ContainerVisualHost prevHost, ref double prevFontSize, ref Mat prevXf, int colorInd, double fontSize, ContainerVisualHost newerExpr) {
            Expr curExpr = (newerExpr.Tag as ExprTags).Expr;
            List<FrameworkElement> highlights;
            if (prevExpr != null) {
                highlights = associateExprs(prevExpr, prevXf, prevFontSize, curExpr, (Mat)newerExpr.RenderTransform.Value, fontSize, colorInd);
                if (highlights.Count > 1) {
                    Canvas cvh1 = new Canvas();
                    cvh1.IsHitTestVisible = false;
                    cvh1.Opacity = 0.1;
                    cvh1.Tag = exprTags.Id;
                    cvh1.RenderTransform = prevHost.RenderTransform;
                    cvh1.Children.Add(highlights[0]);
                    _mathUICanvas.Children.Add(cvh1);
                    Vec p1 = ((Mat)cvh1.RenderTransform.Value) * ((Mat)highlights[0].RenderTransform.Value * new Pt(highlights[0].Width / 2, highlights[0].Height / 2)) - ((Mat)cvh1.RenderTransform.Value * new Pt());

                    for (int i = 1; i < highlights.Count; i++) {
                        Canvas cvh2 = new Canvas();
                        cvh2.IsHitTestVisible = false;
                        cvh2.Opacity = 0.1;
                        cvh2.Tag = exprTags.Id;
                        FrameworkElement h = highlights[i];
                        cvh2.Children.Add(h);
                        cvh2.Tag = exprTags.Id;
                        cvh2.RenderTransform = newerExpr.RenderTransform;
                        System.Windows.Shapes.Line connector = new System.Windows.Shapes.Line();
                        Vec p2 = ((Mat)cvh2.RenderTransform.Value) * ((Mat)h.RenderTransform.Value * new Pt(h.Width / 2, h.Height / 2)) - ((Mat)cvh1.RenderTransform.Value * new Pt());
                        connector.X1 = p1.X;
                        connector.Y1 = p1.Y;
                        connector.X2 = p2.X;
                        connector.Y2 = p2.Y;
                        connector.Stroke = new SolidColorBrush(TermColorizer.FactorCol(colorInd));
                        connector.StrokeThickness = 3;
                        cvh1.Children.Add(connector);
                        _mathUICanvas.Children.Add(cvh2);
                    }
                }
            }
            prevHost = newerExpr;
            prevExpr = curExpr;
            prevXf = (Mat)newerExpr.RenderTransform.Value;
            prevFontSize = fontSize;
        }
        void findColoredBoxes(Box box, int colorInd, List<Box> found) {
            Expr expr = box.ExprIx == null || !(box.ExprIx is int) || !(box.Expr is CompositeExpr)? box.Expr : box.Expr.Args()[(int)box.ExprIx];
            if (box.Expr.Head() != WellKnownSym.equals && expr != null && expr.Annotations.Contains("FactorList")) {
                Dictionary<int, Color> factorList = (Dictionary<int, Color>)expr.Annotations["FactorList"];
                if (factorList.ContainsKey(colorInd))
                    found.Add(box);
                else foreach (Box b in box.SubBoxes)
                    findColoredBoxes(b, colorInd, found);
            }  else foreach (Box b in box.SubBoxes)
                    findColoredBoxes(b, colorInd, found);
        }
        List<FrameworkElement> associateExprs(Expr expr1, Mat xf1, double font1, Expr expr2, Mat xf2, double font2, int colorInd) {
            Box box1, box2;
            List<FrameworkElement> highlights = new List<FrameworkElement>();
            EWPF.MeasureTopLeft(expr1, font1, out box1);  // compute the Box for the frozen expression's visual
            EWPF.MeasureTopLeft(expr2, font2, out box2);  // compute the Box for the frozen expression's visual
            List<Box> found1 = new List<Box>();
            List<Box> found2 = new List<Box>();
            findColoredBoxes(box1, colorInd, found1);
            findColoredBoxes(box2, colorInd, found2);
            if (found1.Count == 0 || found2.Count == 0)
                return highlights;
            Rct r = Rct.Null;
            foreach (Box b in found1)
                r = r.Union(b.BBoxRefOrigin);
            Rectangle rr = new Rectangle();
            rr.Width = r.Width;
            rr.Height = r.Height;
            rr.Fill = new SolidColorBrush(TermColorizer.FactorCol(colorInd));
            rr.RenderTransform = new MatrixTransform(Mat.Translate(r.TopLeft - new Pt(0, box1.BBoxRefOrigin.Top)));
            highlights.Add(rr);
            foreach (Box b in found2) {
                rr = new Rectangle();
                rr.Width = b.bbox.Width;
                rr.Height = b.bbox.Height;
                rr.Fill = new SolidColorBrush(TermColorizer.FactorCol(colorInd));
                rr.RenderTransform = new MatrixTransform(Mat.Translate(b.BBoxRefOrigin.TopLeft - new Pt(0,box2.BBoxRefOrigin.Top)));
                highlights.Add(rr);
            }
            return highlights;
        }

        public delegate void UnfocusExprHandler(ContainerVisualHost cvh);
        public event UnfocusExprHandler UnfocusExprEvent;

        public ContainerVisualHost CreateGhostTerm(Pt where, double fontSize, EWPF.HitBox selectedExpr)
        {

            ContainerVisualHost ghostTerm = selectedExpr.ToVisual(fontSize, Colors.Black, null);
            ghostTerm.RenderTransform = new TranslateTransform(where.X - ghostTerm.Width / 2, where.Y - ghostTerm.Height / 2);
            ghostTerm.Opacity = 0.5;
            ghostTerm.Tag = selectedExpr;
            _mathUICanvas.Children.Add(ghostTerm);
            return ghostTerm;
        }

        /// <summary>
        /// create a visual for the factored expression and add it to the Canvas (after removing any previous visual)
        /// </summary>
        /// <param name="factoredTerm"></param>

        public ContainerVisualHost createNewExpr(Guid id, Expr factoredTerm, Pt where, double fontSize,
                                                    int numIterationsFromOriginal) {
            ContainerVisualHost displayedExpr = EWPF.ToVisual(factoredTerm, fontSize, Colors.Black, Brushes.White, EWPF.DrawTop);

            // position the factored expression
            displayedExpr.RenderTransform = new TranslateTransform(where.X, where.Y);

            // this starts up the factoring interaction
            displayedExpr.Tag = new ExprTags(id, factoredTerm, fontSize, where, numIterationsFromOriginal);
            addFactorHandlers(displayedExpr, true);
            return displayedExpr;
        }

        public void undisplayExpr(ContainerVisualHost cvh)
        {
            _mathUICanvas.Children.Remove(cvh);
            if (cvh.Tag != null && cvh.Tag is ExprTags && (cvh.Tag as ExprTags).Output != null) {
                _mathUICanvas.Children.Remove((cvh.Tag as ExprTags).Output);
                (cvh.Tag as ExprTags).Output = null;
            }
        }
        public ContainerVisualHost displayModifiedExpr(Expr factoredTerm, Pt where, double fontSize, Guid id,
                                                          int numIterationsFromOriginal, Boolean isTappable) {
            for (int i = 0; i < _mathUICanvas.Children.OfType<ContainerVisualHost>().Count(); i++) {
                ExprTags tag = _mathUICanvas.Children.OfType<ContainerVisualHost>().ToList()[i].Tag as ExprTags;
                if (tag != null && tag.Id == id && tag.NumIterationsFromOriginal >= numIterationsFromOriginal) {
                    undisplayExpr(_mathUICanvas.Children.OfType<ContainerVisualHost>().ToList()[i]);
                    i--;
                }
            }
            ContainerVisualHost displayedExpr = EWPF.ToVisual(factoredTerm, fontSize, Colors.Black, Brushes.White, EWPF.DrawTop);
            displayedExpr.IsHitTestVisible = false; // bcz: this allows us to have simultaneous contacts
            _mathUICanvas.Children.Add(displayedExpr);

            // position the factored expression
            displayedExpr.RenderTransform = new TranslateTransform(where.X, where.Y);

            // this starts up the factoring interaction
            displayedExpr.Tag = new ExprTags(id, factoredTerm, fontSize, where, numIterationsFromOriginal);
            addFactorHandlers(displayedExpr, isTappable);
            return displayedExpr;
        }

        /// <summary>
        /// display a rectangle around the part of the expression that was selected (after first removing any previous rectangle)
        /// </summary>
        /// <param name="hitExpr"></param>
        public Rectangle highlightSelectedExprTerm(ContainerVisualHost frozenVisual, EWPF.HitBox hitExpr, Rectangle highlight) {
            ExprTags exprTags = frozenVisual.Tag as ExprTags;

            Expr target = hitExpr.HitExpr;
            Rct hitBounds = hitExpr.Box.BBoxRefOrigin;  // bounds of symbol we actually hit

            if (highlight != null)
                _mathUICanvas.Children.Remove(highlight);
            highlight = new Rectangle();
            highlight.IsHitTestVisible = false;
            highlight.Width = hitBounds.Width;
            highlight.Height = hitBounds.Height;
            highlight.Stroke = Brushes.Gray;

            Box box = EWPF.Measure(exprTags.Expr, exprTags.FontSize);  // compute the Box for the frozen expression's visual
            Vec offsetToTopLeftOfFrozenVisual = box.bbox.TopLeft - new Pt(); // the Expr code positions text relative to the text mid-line.  Thus, the actual topLeft of the text may be higher on the screen

            Canvas.SetLeft(highlight, frozenVisual.RenderTransform.Value.OffsetX + hitBounds.Left);
            Canvas.SetTop(highlight, frozenVisual.RenderTransform.Value.OffsetY + hitBounds.Top - offsetToTopLeftOfFrozenVisual.Y);
            _mathUICanvas.Children.Add(highlight);

            return highlight;
        }
        static public Expr dragFactor(Pt dragPt, Expr topLevel, Box exprBox, Pt topLeftOfExpr, Expr target, List<Rct> boxes, int colorInd, bool allowReorder)
        {
            // find the path in the expression tree from the root to the selected factor term
            List<Expr> selPath = Expr.FindPath(topLevel, target);
            if (selPath.Count == 0)
                return topLevel;
            // setup the values needed to do some factoring
            Expr unfactoredExpr = selPath[0];   // the expression to be factored
            Expr factoredExpr = selPath[0];       // the resulting factored expression
            Expr factorTerm = selPath[0]; // the term to factor out
            TermColorizer.MarkFactor(factorTerm, colorInd);  // mark the term that should be factored -- there's no other way to know what to factor since the same term might be used elsewhere
            
            // iterate up the expression tree, factoring out the factorTerm at each step and then substituting the factored expression in the
            // next higher level expression in the tree.  If no factoring has yet been done, then we will try to reorder the term being dragged.
            int pathCount = 0;
            double dragX = dragPt.X;
            bool justReordered = false;
            bool wasJustReordered = false;
            bool wasEverReordered = false;
            foreach (Expr curTerm in selPath) {
                if (curTerm != null) {
                    Rct box = boxes[pathCount];
                    bool exprChanged = unfactoredExpr != factoredExpr;
                   // Expr lastFactoredExpr = factoredExpr;
                    Expr factored = exprChanged ? Engine.Replace(curTerm, unfactoredExpr, factoredExpr) : curTerm;
                    factoredExpr = factored;

                    // first try local movement within the expression (ie, reordering terms and partial factoring)
                    if ((curTerm.Head() == WellKnownSym.plus || curTerm.Head() == WellKnownSym.times)) {
                        if (allowReorder && !exprChanged && Expr.ContainsExact(curTerm.Args(), factorTerm)) {
                            factoredExpr = reorderTerms(exprBox, dragX, topLeftOfExpr, curTerm as CompositeExpr, factorTerm);
                            if (factoredExpr != curTerm)
                            {
                                wasEverReordered = curTerm.Head() == WellKnownSym.plus;
                                justReordered = true;
                            }
                        }
                        else if (curTerm.Head() == WellKnownSym.plus) {
                            factoredExpr = partiallyFactorTerms(exprBox, dragX, topLeftOfExpr, curTerm, factorTerm, unfactoredExpr);
                            if (factoredExpr == curTerm)
                                factoredExpr = factored;
                            else
                                dragX += dragX < boxes[0].Center.X ?  10 : -10;
                        }
                    }


                    // then, depending on how far the user has dragged, try to factor out of the expression completely
                    // NOTE: these steps override any local movements.
                    if ( (box.Left > dragX || box.Right < dragX) && !allowReorder)    // there may be multiple levels of factoring that can be done at each step
                        factoredExpr = ExprOps.FactorIntoProduct(box, colorInd, wasJustReordered ? curTerm : factored, ref factorTerm, ref dragX, allowReorder);

                    if (curTerm != target && (curTerm.Head() == WellKnownSym.plus || curTerm.Head() == WellKnownSym.times) && !wasEverReordered)
                        allowReorder = false;
                    wasJustReordered = justReordered;
                    justReordered = false;
                    unfactoredExpr = curTerm; // save original version of term for next iteration
                }
                pathCount++;
            }
            return ExprTransform.FlattenMults(ExprTransform.FlattenSums(factoredExpr));
        }
        static public Expr dragSplitFractionalSum(Pt dragPt, Expr topLevel, Box exprBox, Pt topLeftOfExpr, Expr target, List<Rct> boxes, int colorInd) {
            // find the path in the expression tree from the root to the selected factor term
            List<Expr> selPath = Expr.FindPath(topLevel, target);
            if (selPath.Count == 0)
                return topLevel;
            if (selPath[0] is LetterSym && selPath.Count > 1) {
                selPath.RemoveAt(0);
                boxes.RemoveAt(0);
            }
            // setup the values needed to do some factoring
            foreach (Expr e in selPath) {
                Expr newFactorTerm = ExprTransform.SplitFractionalSum(e, target);
                if (newFactorTerm != e)
                    return ExprTransform.FlattenSums(Engine.Replace(topLevel, e, newFactorTerm));
            }
            return topLevel;
        }
        static public Expr dragFactorIntoSum(Pt dragPt, Expr topLevel, Box exprBox, Pt topLeftOfExpr, Expr target, List<Rct> boxes, int colorInd, TwoFingerOp mode) {
            double dragX = dragPt.X;
            double dragY = dragPt.Y;
            // find the path in the expression tree from the root to the selected factor term
            List<Expr> selPath = Expr.FindPath(topLevel, target);
            if (selPath.Count == 0)
                return topLevel;
            if (selPath[0] is LetterSym && selPath.Count > 1) {
                selPath.RemoveAt(0);
            }
            // setup the values needed to do some factoring
            Expr unfactoredExpr = selPath[0];   // the expression to be factored
            Expr factoredExpr = selPath[0];       // the resulting factored expression
            Expr factorTerm = selPath[0]; // the term to factor out
            Expr newFactorTerm = mode == TwoFingerOp.Stretching ? splitProductIntoSum(boxes[0], target, ref dragX, factorTerm, factoredExpr):
                                        splitProductIntoFraction(boxes[0], target, ref dragY, factorTerm, factoredExpr);
            if (selPath.Count > 1 && selPath[ 1].Head() == WellKnownSym.minus) {
                newFactorTerm = new CompositeExpr(WellKnownSym.minus, newFactorTerm);
                newFactorTerm = ExprOps.Join(newFactorTerm, new Expr[] { newFactorTerm, newFactorTerm }, colorInd,false);
                return ExprTransform.FlattenSums(Engine.Replace(topLevel, selPath[1], newFactorTerm));
            }
            Expr replaced = Engine.Replace(topLevel, factorTerm, newFactorTerm);
            TermColorizer.MarkFactor(factorTerm, colorInd);
            TermColorizer.MarkFactor(newFactorTerm, colorInd);
            foreach (Expr e in newFactorTerm.Args())
                TermColorizer.MarkFactor(e, colorInd);
            replaced = ExprTransform.FlattenSums(replaced);
            return replaced;
        }

        static Expr splitProductIntoFraction(Rct box, Expr factorTerm, ref double dragY, Expr factored, Expr preFactoredExpr)
        {
            Expr factoredExpr = factored;
            bool first = true;
            if (box.Bottom < dragY)
                for (int numLevels = 1; numLevels < 100 && box.Bottom < dragY; numLevels++)
                {
                    factoredExpr = ExprTransform.SplitProductIntoFraction(factored, factorTerm, -numLevels); //bcz: sign of numLevels should be linked to drawX being < or > box.Left & box.Right
                    if (factored == factoredExpr)
                        break;
                    else dragY -= 15;
                    first = false;
                }
            else
                for (int numLevels = 1; numLevels < 100 && box.Top > dragY; numLevels++)
                {
                    factoredExpr = ExprTransform.SplitProductIntoFraction(factored, factorTerm, numLevels); //bcz: sign of numLevels should be linked to drawX being < or > box.Left & box.Right
                    if (factored == factoredExpr)
                        break;
                    else dragY += 15;
                    first = false;
                }
            return first ? preFactoredExpr : factoredExpr;
        }

        static Expr splitProductIntoSum(Rct box, Expr factorTerm, ref double dragX, Expr factored, Expr preFactoredExpr) {
            Expr factoredExpr = factored;
            bool first = true;
            if (box.Right < dragX) 
            for (int numLevels = 1; numLevels < 100 && box.Right < dragX; numLevels++) {
                factoredExpr = ExprTransform.SplitProductIntoSum(factored, factorTerm, -numLevels); //bcz: sign of numLevels should be linked to drawX being < or > box.Left & box.Right
                if (factored == factoredExpr)
                    break;
                else dragX -= 10;
                first = false;
            }
            else
            for (int numLevels = 1; numLevels < 100 && box.Right > dragX; numLevels++) {
                factoredExpr = ExprTransform.SplitProductIntoSum(factored, factorTerm, numLevels); //bcz: sign of numLevels should be linked to drawX being < or > box.Left & box.Right
                if (factored == factoredExpr)
                    break;
                else dragX += 10;
                first = false;
            }
            return first ? preFactoredExpr : factoredExpr;
        }

        static public Expr partiallyFactorTerms(Box exprBox, double dragX, Pt topLeftOfExpr, Expr curTerm, Expr factorTerm, Expr lastFactoredExpr) {
            List<Expr> lhs = new List<Expr>();
            List<Expr> terms = new List<Expr>();
            List<Expr> rhs = new List<Expr>();
            bool foundStart = false;
            bool foundEnd = false;
            bool passed = false;
            bool movingLeft = true;
            foreach (Expr term in curTerm.Args()) {
                Box hbox = EWPF.HitBox.FindBox(term, exprBox);
                if (hbox == null)
                    return curTerm;
                Rct box = hbox.BBoxRefOrigin.Translated((Vec)topLeftOfExpr);
                if (Expr.FindPath(term, factorTerm).Count == 0) {
                    if (foundEnd)
                        rhs.Add(term);
                    else if (foundStart && passed) {
                        if (!foundEnd && ((movingLeft && dragX < box.Center.X) || (!movingLeft && dragX < box.Center.X))) {
                            foundEnd = true;
                            if (dragX > box.Center.X) {
                                terms.Add(term);
                            }
                            else {
                                rhs.Add(term);
                            }
                        }
                        else if (passed)
                            terms.Add(term);
                        else
                            rhs.Add(term);
                    }
                    else {
                        if (passed && dragX > box.Center.X) {
                            foundEnd = true;
                            terms.Add(term);
                        } else  if (passed)
                            terms.Add(term);
                        else if (!passed && dragX < box.Center.X) {
                            foundStart = true;
                            terms.Add(term);
                        }
                        else
                            lhs.Add(term);
                    }
                }
                else { // we found the term that's being dragged
                    movingLeft = dragX < box.Center.X;
                    if ((movingLeft && dragX < box.Left) || (!movingLeft && dragX > box.Right))
                        terms.Add(lastFactoredExpr);
                    else lhs.Add(lastFactoredExpr);
                    if (!foundStart)
                        foundStart = true;
                    else foundEnd = true;
                    passed = true;
                }
            }
            if (terms.Count == 0)
                return curTerm;
            Expr factors = terms.Count == 1 ? terms[0] : new CompositeExpr(WellKnownSym.plus, terms.ToArray()); 
            Expr refactored = terms.Count > 1 || movingLeft ? ExprTransform.FactorOut(factors, factorTerm, true) : factors;
            if (lhs.Count > 0 || rhs.Count > 0) {
                if (refactored.Head() == WellKnownSym.plus)
                    lhs.AddRange(refactored.Args());
                else lhs.Add(refactored);
                lhs.AddRange(rhs.ToArray());
                return new CompositeExpr(curTerm.Head(), lhs.ToArray());
            }
            return refactored;
        }

        static public Expr reorderTerms(Box exprBox, double dragX, Pt topLeftOfExpr, CompositeExpr curTerm, Expr factorTerm) {
            List<Expr> lhs = new List<Expr>();
            bool found = false;
            bool passed = false;
            foreach (Expr term in curTerm.Args())  {
                Box hbox = EWPF.HitBox.FindBox(term, exprBox);
                if (hbox == null)
                    return curTerm;
                Rct box = hbox.BBoxRefOrigin.Translated((Vec)topLeftOfExpr);
                if (!Object.ReferenceEquals(term,factorTerm)) {
                    if (passed) {
                        if (!found && dragX < box.Right ) {
                            found = true;
                            if (dragX > box.Center.X) {
                                lhs.Add(term);
                                lhs.Add(factorTerm);
                            }
                            else {
                                lhs.Add(factorTerm);
                                lhs.Add(term);
                            }
                        }
                        else
                            lhs.Add(term);
                    }
                    else {
                        if (!found && dragX < box.Center.X) {
                            found = true;
                            passed = true;
                            lhs.Add(factorTerm);
                            lhs.Add(term);
                        }
                        else
                            lhs.Add(term);
                    }
                }
                else {
                    if (dragX < box.Right) {
                        if (!found)
                            lhs.Add(term);
                        found = true;
                    }
                    passed = true;
                }
            }
            if (!found)
                lhs.Add(factorTerm);
            return new CompositeExpr(curTerm.Head(), lhs.ToArray());
        }

        static public Expr dragTwoContacts(ContainerVisualHost currentExprVisual, ExprTags exprTags, Expr topLevel, Pt center,
                                    Expr firstTerm, Expr secondTerm, ContainerVisualHost firstGhostTerm, TwoFingerOp twoFingers) {
            int i1, i2;
            Expr factorTerm = Expr.FindCommonAncestor(topLevel, firstTerm, secondTerm, out i1, out i2);
            Expr newExpr = topLevel;
            if (factorTerm != null) {
                if (twoFingers == TwoFingerOp.Pinching || factorTerm.Head() != WellKnownSym.plus)
                {
                    Expr origFirstTerm = firstTerm;
                    if (firstTerm is CompositeExpr && Expr.ContainsExact(firstTerm.Args(), secondTerm))
                        secondTerm = firstTerm;
                    if (secondTerm is CompositeExpr && Expr.ContainsExact(secondTerm.Args(), firstTerm))
                        firstTerm = secondTerm;
                    newExpr = ExprOps.JoinRange(topLevel, firstTerm, secondTerm, exprTags.NumIterationsFromOriginal, twoFingers == TwoFingerOp.Stretching);
                    factorTerm = TermColorizer.FindFactorTerm(newExpr);
                    return newExpr;
                }
                else if (twoFingers == TwoFingerOp.Stretching)
                {
                    if (factorTerm.Head() == WellKnownSym.power && factorTerm.Args()[1] is IntegerNumber && (int)factorTerm.Args()[1] < 5 && (int)factorTerm.Args()[1] > 0) {
                        Expr[] args = new Expr[(int)factorTerm.Args()[1]];
                        for (int i = 0; i < args.Length; i++)
                            args[i] = factorTerm.Args()[0].Clone();
                        Expr expanded = new CompositeExpr(WellKnownSym.times, args);
                        newExpr = ExprTransform.FlattenMults(Engine.Replace(newExpr, factorTerm, ExprOps.DistributeTimes(expanded, true)));
                    }
                    List<Expr> path = Expr.FindPath(topLevel, factorTerm);
                    if (path.Count > 1) {
                        TermColorizer.MarkFactor(path[0].Args()[i2 < i1 ? i1 : i2], exprTags.NumIterationsFromOriginal);
                        Expr parent = ExprTransform.SplitFractionalSum(path[1], path[0].Args()[i2 < i1 ? i1 : i2]);
                        TermColorizer.MarkFactor(parent, exprTags.NumIterationsFromOriginal);
                        foreach (Expr e in parent.Args())
                            TermColorizer.MarkFactor(e, exprTags.NumIterationsFromOriginal);
                        newExpr = ExprTransform.FlattenSums(Engine.Replace(topLevel, path[1], parent));
                        if (newExpr != topLevel)
                            return newExpr;
                        else {
                            newExpr = topLevel;
                            factorTerm = secondTerm;
                        }
                    }
                }
            }
            else factorTerm = firstTerm;
            return dragOneContact(currentExprVisual, firstGhostTerm, topLevel, newExpr, factorTerm, exprTags, center, twoFingers, false);
        }

        static public Expr dragOneContact(ContainerVisualHost currentExprVisual, ContainerVisualHost ghostTerm, Expr origTopLevel,
                                   Expr topLevel, Expr term, ExprTags exprTags, Pt contactPt, TwoFingerOp splitting, bool allowReorder) {
            Box exprBox = EWPF.Measure(origTopLevel, exprTags.FontSize);
            Pt topLeftOfExpr = ((Mat)currentExprVisual.RenderTransform.Value) * new Pt();

            // get the bounding Rct for the '=' if it's there
            Box equalsBox = EWPF.HitBox.FindEqualsBox(topLevel, exprBox);
            Rct equalsRct = equalsBox == null ? Rct.Null : equalsBox.BBoxRefOrigin.Translated((Vec)topLeftOfExpr - (Vec)exprBox.BBoxRefOrigin.TopLeft);

            bool lhs = false;
            if (equalsBox != null) {
                lhs = Expr.FindPath(topLevel.Args()[0], term).Count > 0;
                if (!lhs && term.Head() == WellKnownSym.times)
                    lhs = Expr.FindPath(topLevel.Args()[0], term.Args()[0]).Count > 0;
            }

            EWPF.MeasureTopLeft(topLevel, exprTags.FontSize, out exprBox);
            topLeftOfExpr = ((Mat)currentExprVisual.RenderTransform.Value) * new Pt();

            Expr factoredExpr = null;
            // if the user crossed over the equal sign, then factor the term out completely and move it across
            if (equalsBox != null && ((lhs && contactPt.X > equalsRct.Right) || (!lhs && contactPt.X < equalsRct.Left))) {
                factoredExpr = ExprOps.DragTermAcrossEquality(term, topLevel, contactPt.Y > equalsRct.Bottom, lhs, exprTags.NumIterationsFromOriginal);
                if (factoredExpr == topLevel)
                    factoredExpr = factorTermInteractively(contactPt, term, exprBox, topLeftOfExpr, topLevel, exprTags.NumIterationsFromOriginal, splitting, allowReorder);
            }
            else // otherwise, factor out just as far as the user has dragged
            {
                factoredExpr = factorTermInteractively(contactPt, term, exprBox, topLeftOfExpr, topLevel, exprTags.NumIterationsFromOriginal, splitting, allowReorder);
            }

            // display the ghost
            ghostTerm.RenderTransform = new TranslateTransform(contactPt.X - ghostTerm.Width / 2,
                                                                    contactPt.Y - ghostTerm.Height / 2 + 25);

            return factoredExpr;
        }

        static public Expr factorTermInteractively(Pt dragPt, Expr dragExpr, Box exprBox, Pt topLeftOfExpr, Expr topLevelExpr, int colorInd, TwoFingerOp splitting, bool allowReorder)
        {
            // get all the expression nodes on the way down to the selected term
            List<Expr> targetPath = Expr.FindPath(topLevelExpr, dragExpr);

            // compute the bounding Rct foreach expression node
            List<Rct> targetBoxes = new List<Rct>();
            foreach (Expr pathExpr in targetPath) {
                Box hbox = EWPF.HitBox.FindBox(pathExpr, exprBox);
                targetBoxes.Add(hbox == null ? new Rct() : hbox.BBoxRefOrigin.Translated((Vec)topLeftOfExpr - (Vec)exprBox.BBoxRefOrigin.TopLeft));
            }
            if (splitting == TwoFingerOp.Stretching || splitting == TwoFingerOp.Graphing)
                return dragFactorIntoSum(dragPt, topLevelExpr, exprBox, topLeftOfExpr, dragExpr, targetBoxes, colorInd, splitting);
            if (Keyboard.IsKeyDown(Key.LeftCtrl))
                return dragSplitFractionalSum(dragPt, topLevelExpr, exprBox, topLeftOfExpr, dragExpr, targetBoxes, colorInd);

            // do some mathematical factoring type stuff
            return dragFactor(dragPt, topLevelExpr, exprBox, topLeftOfExpr, dragExpr, targetBoxes,  colorInd, allowReorder);
        }
    }
}
