using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using P = DocumentFormat.OpenXml.Presentation;
using D = DocumentFormat.OpenXml.Drawing;
using P14 = DocumentFormat.OpenXml.Office2010.PowerPoint;
using A14 = DocumentFormat.OpenXml.Office2010.Drawing;
using DocumentFormat.OpenXml.Validation;
using System.IO;
using starPadSDK.Geom;
using System.Windows;
using C = DocumentFormat.OpenXml.Drawing.Charts;
using C14 = DocumentFormat.OpenXml.Office2010.Drawing.Charts;

namespace Serialization
{
    // Converts containerbubbles into powerpoint slides. Uses the openxml sdk.
    // Some useful links:
    // http://msdn.microsoft.com/en-us/library/office/cc850828
    // http://openxmldeveloper.org/blog/b/openxmldeveloper/archive/2009/09/06/7429.aspx
    public class PowerPointSerializer
    {
        public PowerPointSerializer()
        {
            
        }

        public void Serialize(string filename, string templateFilename, Rct viewPointRct, List<Model> models)
        {
            Console.WriteLine("=== Start Serialization to PowerPoint ===");

            // Save the template file with the new filename
            byte[] template = null;
            if (File.Exists(templateFilename))
            {
                template = File.ReadAllBytes(templateFilename);
            }
            else
            {
                template = Properties.Resources.PresentationTemplate;
            }
            try
            {
                FileStream fs = File.Open(filename, FileMode.Create);
                fs.Write(template, 0, template.Length);
                fs.Close();
            }
            catch (IOException e)
            {
                throw new SerializationException("Could not save file: " + filename + ". Is file in use?");
            }
            

            // create the slide deck 
            PresentationDocument presentationDoc = CreateSlides(filename, viewPointRct, models);

            // Validate the new presentation.
            OpenXmlValidator validator = new OpenXmlValidator();
            var errors = validator.Validate(presentationDoc);
            if (errors.Count() > 0)
            {
                Console.WriteLine("The deck creation process completed but the created presentation failed to validate.");
                Console.WriteLine("There are " + errors.Count() + " errors:\r\n");

                displayValidationErrors(errors);
            }
            else
            {
                Console.WriteLine("The deck creation process completed and the created presentation validated with 0 errors.");
            }

            //Close the presentation handle
            presentationDoc.Close();

            Console.WriteLine("=== End Serialization to PowerPoint ===");
        }

        private PresentationDocument CreateSlides(string newPresentation, Rct viewPointRct, List<Model> models)
        {
            string relId;
            SlideId slideId;

            // Slide identifiers have a minimum value of greater than or equal to 256
            // and a maximum value of less than 2147483648. Assume that the template
            // presentation being used has no slides.
            uint currentSlideId = 256;
            // Open the new presentation.
            PresentationDocument newDeck = PresentationDocument.Open(newPresentation, true);

            PresentationPart presentationPart = newDeck.PresentationPart;
            SlideSize slideSize = presentationPart.Presentation.SlideSize;

            // Reuse the slide master part. This code assumes that the template presentation
            // being used has at least one master slide.
            var slideMasterPart = presentationPart.SlideMasterParts.First();

            // Reuse the slide layout part. This code assumes that the template presentation
            // being used has at least one slide layout.
            var slideLayoutPart = slideMasterPart.SlideLayoutParts.First();

            // If the new presentation doesn't have a SlideIdList element yet then add it.
            if (presentationPart.Presentation.SlideIdList == null)
                presentationPart.Presentation.SlideIdList = new SlideIdList();

            // Create a unique relationship id based on the current slide id.
            relId = "rel" + currentSlideId;

            // calculate the export scale factor
            double exportScale = Math.Min((double)slideSize.Cx / viewPointRct.Width, (double)slideSize.Cy / viewPointRct.Height);

            // Create a slide part for the new slide.
            var slidePart = CreateSlidePart(presentationPart, relId, models, exportScale, viewPointRct);

            // Add the relationship between the slide and the slide layout.
            slidePart.AddPart<SlideLayoutPart>(slideLayoutPart);

            // Add the new slide to the slide list.
            slideId = new SlideId();
            slideId.RelationshipId = relId;
            slideId.Id = currentSlideId;
            presentationPart.Presentation.SlideIdList.Append(slideId);

            // Increment the slide id;
            currentSlideId++;

            // Save the changes to the slide master part.
            slideMasterPart.SlideMaster.Save();

            // Save the changes to the new deck.
            presentationPart.Presentation.Save();

            return newDeck;
        }

        private SlidePart CreateSlidePart(PresentationPart presentationPart, string slideRId, List<Model> model, double exportScale, Rct viewPointRct)
        {
            SlidePart slidePart1 = presentationPart.AddNewPart<SlidePart>(slideRId);
            Slide slide = new Slide();
            slidePart1.Slide = slide;
            CommonSlideData commonSlideData = new CommonSlideData();
            ShapeTree shapeTree = new ShapeTree();
            P.NonVisualGroupShapeProperties nonVisualGroupShapeProperties =
                new P.NonVisualGroupShapeProperties(
                                new P.NonVisualDrawingProperties() { Id = (UInt32Value)1U, Name = "" },
                                new P.NonVisualGroupShapeDrawingProperties(),
                                new ApplicationNonVisualDrawingProperties());

            GroupShapeProperties groupShapeProperties = new GroupShapeProperties(new TransformGroup());
            shapeTree.Append(nonVisualGroupShapeProperties);
            shapeTree.Append(groupShapeProperties);
            commonSlideData.Append(shapeTree);

            slide.Append(commonSlideData);
            slide.Append(new ColorMapOverride(new MasterColorMapping()));

            int partId = 1;
            foreach (var bubble in model)
            {
                string partRId1 = "rel" + partId;
                partId++;
                string partRId2 = "rel" + partId;
                partId++;
                CreateModelRepresentation(bubble, slidePart1, shapeTree, exportScale, viewPointRct, partRId1, partRId2);
            }

            return slidePart1;
        }

        private void CreateModelRepresentation(Model model, SlidePart slidePart, ShapeTree shapeTree, double exportScale, Rct viewPointRct, string partRId1, string partRId2)
        {
            if (model is ImageModel)
            {
                CreateImageRepresentation(model as ImageModel, slidePart, shapeTree, exportScale, viewPointRct, partRId1, partRId2);
            }
            else if (model is ShapeModel)
            {
                CreateShapeRepresentation(model as ShapeModel, slidePart, shapeTree, exportScale, viewPointRct, partRId1, partRId2);
            }
            else if (model is TableModel)
            {
                CreateTableRepresentation(model as TableModel, slidePart, shapeTree, exportScale, viewPointRct, partRId1, partRId2);
            }
            else if (model is TextModel)
            {
                CreateTextRepresentation(model as TextModel, slidePart, shapeTree, exportScale, viewPointRct, partRId1, partRId2);
            }
            else if (model is ChartModel)
            {
                CreateChartRepresentation(model as ChartModel, slidePart, shapeTree, exportScale, viewPointRct, partRId1, partRId2);
                ChartPart chartPart1 = slidePart.AddNewPart<ChartPart>(partRId2);
                GenerateChartPart1Content(chartPart1, model as ChartModel);
            }
        }

        private void CreateShapeRepresentation(ShapeModel model, SlidePart slidePart, ShapeTree shapeTree, double exportScale, Rct viewPointRct, string partRId1, string partRId2)
        {
            P.Shape shape1 = new P.Shape();

            P.NonVisualShapeProperties nonVisualShapeProperties1 = new P.NonVisualShapeProperties();
            P.NonVisualDrawingProperties nonVisualDrawingProperties2 = new P.NonVisualDrawingProperties() { Id = (UInt32Value)4U, Name = "PanoramicData" };
            P.NonVisualShapeDrawingProperties nonVisualShapeDrawingProperties1 = new P.NonVisualShapeDrawingProperties();
            ApplicationNonVisualDrawingProperties applicationNonVisualDrawingProperties2 = new ApplicationNonVisualDrawingProperties();

            nonVisualShapeProperties1.Append(nonVisualDrawingProperties2);
            nonVisualShapeProperties1.Append(nonVisualShapeDrawingProperties1);
            nonVisualShapeProperties1.Append(applicationNonVisualDrawingProperties2);

            P.ShapeProperties shapeProperties1 = new P.ShapeProperties();

            D.Transform2D transform2D1 = new D.Transform2D() { Rotation = new Int32Value((int)(model.Angle * 60000)) };

            D.Offset offset2 = new D.Offset() { X = new Int64Value((long)((model.Position.X - viewPointRct.Left) * exportScale)), Y = new Int64Value((long)((model.Position.Y - viewPointRct.Top) * exportScale)) };
            D.Extents extents2 = new D.Extents() { Cx = new Int64Value((long)(model.Size.W * exportScale)), Cy = new Int64Value((long)(model.Size.H * exportScale)) };

            transform2D1.Append(offset2);
            transform2D1.Append(extents2);

            D.CustomGeometry customGeometry1 = new D.CustomGeometry();

            D.Rectangle rectangle1 = new D.Rectangle() { Left = "l", Top = "t", Right = "r", Bottom = "b" };

            D.PathList pathList1 = new D.PathList();

            D.Path path1 = new D.Path() { Width = new Int64Value((long)(model.Size.W * exportScale)), Height = new Int64Value((long)(model.Size.H * exportScale)) };

            // calculate the shape points
            List<PointModel> points = model.Points;
            if (points.Count > 0)
            {
                // move to first point
                Pt pt = points[0];

                D.MoveTo moveTo1 = new D.MoveTo();
                D.Point point1 = new D.Point()
                {
                    X = new Int64Value((long)(pt.X * exportScale)).ToString(),
                    Y = new Int64Value((long)(pt.Y * exportScale)).ToString()
                };
                moveTo1.Append(point1);
                path1.Append(moveTo1);

                for (int i = 1; i < points.Count; i++)
                {
                    pt = points[i];

                    D.LineTo lineTo1 = new D.LineTo();
                    D.Point point2 = new D.Point()
                    {
                        X = new Int64Value((long)(pt.X * exportScale)).ToString(),
                        Y = new Int64Value((long)(pt.Y * exportScale)).ToString()
                    };

                    lineTo1.Append(point2);
                    path1.Append(lineTo1);
                }

                // close shape
                D.CloseShapePath closeShapePath1 = new D.CloseShapePath();
                path1.Append(closeShapePath1);
            }
            pathList1.Append(path1);

            //customGeometry1.Append(adjustValueList1);
            //customGeometry1.Append(shapeGuideList1);
            //customGeometry1.Append(adjustHandleList1);
            //customGeometry1.Append(connectionSiteList1);
            customGeometry1.Append(rectangle1);
            customGeometry1.Append(pathList1);

            D.SolidFill solidFill10 = new D.SolidFill();
            D.RgbColorModelHex rgbColorModelHex1 = new D.RgbColorModelHex() { Val = convertColor(model.Color) };

            solidFill10.Append(rgbColorModelHex1);

            D.Outline outline1 = new D.Outline();

            D.SolidFill solidFill11 = new D.SolidFill();
            D.SchemeColor schemeColor10 = new D.SchemeColor() { Val = D.SchemeColorValues.Text1 };

            solidFill11.Append(schemeColor10);

            outline1.Append(solidFill11);

            shapeProperties1.Append(transform2D1);
            shapeProperties1.Append(customGeometry1);
            shapeProperties1.Append(solidFill10);
            shapeProperties1.Append(outline1);

            P.ShapeStyle shapeStyle1 = new P.ShapeStyle();

            D.LineReference lineReference1 = new D.LineReference() { Index = (UInt32Value)2U };

            D.SchemeColor schemeColor11 = new D.SchemeColor() { Val = D.SchemeColorValues.Accent1 };
            D.Shade shade1 = new D.Shade() { Val = 50000 };

            schemeColor11.Append(shade1);

            lineReference1.Append(schemeColor11);

            D.FillReference fillReference1 = new D.FillReference() { Index = (UInt32Value)1U };
            D.SchemeColor schemeColor12 = new D.SchemeColor() { Val = D.SchemeColorValues.Accent1 };

            fillReference1.Append(schemeColor12);

            D.EffectReference effectReference1 = new D.EffectReference() { Index = (UInt32Value)0U };
            D.SchemeColor schemeColor13 = new D.SchemeColor() { Val = D.SchemeColorValues.Accent1 };

            effectReference1.Append(schemeColor13);

            D.FontReference fontReference1 = new D.FontReference() { Index = D.FontCollectionIndexValues.Minor };
            D.SchemeColor schemeColor14 = new D.SchemeColor() { Val = D.SchemeColorValues.Light1 };

            fontReference1.Append(schemeColor14);

            shapeStyle1.Append(lineReference1);
            shapeStyle1.Append(fillReference1);
            shapeStyle1.Append(effectReference1);
            shapeStyle1.Append(fontReference1);


            P.TextBody textBody1 = new P.TextBody();
            D.BodyProperties bodyProperties1 = new D.BodyProperties() { RightToLeftColumns = false, Anchor = D.TextAnchoringTypeValues.Center };
            D.NormalAutoFit normalAutoFit1 = new D.NormalAutoFit();
            bodyProperties1.Append(normalAutoFit1);
            D.ListStyle listStyle1 = new D.ListStyle();

            D.Paragraph paragraph1 = new D.Paragraph();
            D.ParagraphProperties paragraphProperties1 = new D.ParagraphProperties() { Alignment = D.TextAlignmentTypeValues.Center };

            D.Run run1 = new D.Run();

            D.RunProperties runProperties1 = new D.RunProperties() { Language = "en-US", Dirty = false, SpellingError = true, SmartTagClean = false };

            D.SolidFill solidFill12 = new D.SolidFill();
            D.SchemeColor schemeColor15 = new D.SchemeColor() { Val = D.SchemeColorValues.Text1 };

            solidFill12.Append(schemeColor15);

            runProperties1.Append(solidFill12);
            D.Text text1 = new D.Text();
            text1.Text = model.Text;

            run1.Append(runProperties1);
            run1.Append(text1);

            D.EndParagraphRunProperties endParagraphRunProperties1 = new D.EndParagraphRunProperties() { Language = "en-US", Dirty = false };

            D.SolidFill solidFill13 = new D.SolidFill();
            D.SchemeColor schemeColor16 = new D.SchemeColor() { Val = D.SchemeColorValues.Text1 };

            solidFill13.Append(schemeColor16);

            endParagraphRunProperties1.Append(solidFill13);

            paragraph1.Append(paragraphProperties1);
            paragraph1.Append(run1);
            paragraph1.Append(endParagraphRunProperties1);

            textBody1.Append(bodyProperties1);
            textBody1.Append(listStyle1);
            textBody1.Append(paragraph1);

            shape1.Append(nonVisualShapeProperties1);
            shape1.Append(shapeProperties1);
            shape1.Append(shapeStyle1);
            shape1.Append(textBody1);

            shapeTree.Append(shape1);
        }

        private void CreateImageRepresentation(ImageModel model, SlidePart slidePart, ShapeTree shapeTree, double exportScale, Rct viewPointRct, string partRId1, string partRId2)
        {
            // add the image data to the slide
            byte[] imageFileBytes;
            using (FileStream fsImageFile = File.OpenRead(model.FileName))
            {
                imageFileBytes = new byte[fsImageFile.Length];
                fsImageFile.Read(imageFileBytes, 0, imageFileBytes.Length);
            }

            ImagePart imagePart1 = slidePart.AddNewPart<ImagePart>("image/png", partRId1);
            using (BinaryWriter writer = new BinaryWriter(imagePart1.GetStream()))
            {
                writer.Write(imageFileBytes);
                writer.Flush();
            }

            // create all the neccesary xml tags for the image. 
            P.Picture picture1 = new P.Picture();

            P.NonVisualPictureProperties nonVisualPictureProperties1 = new P.NonVisualPictureProperties();
            P.NonVisualDrawingProperties nonVisualDrawingProperties2 = new P.NonVisualDrawingProperties() { Id = (UInt32Value)1026U, Name = "Picture", Description = "Exported WorkTop Content" };

            // create a hyperlink from the image
            if (model.Link != null)
            {
                slidePart.AddHyperlinkRelationship(new System.Uri(model.Link, System.UriKind.Absolute), true, partRId2);
                D.HyperlinkOnClick hyperlinkOnClick1 = new D.HyperlinkOnClick() { Id = partRId2 };
                nonVisualDrawingProperties2.Append(hyperlinkOnClick1);
            }

            P.NonVisualPictureDrawingProperties nonVisualPictureDrawingProperties1 = new P.NonVisualPictureDrawingProperties();
            D.PictureLocks pictureLocks1 = new D.PictureLocks() { NoChangeAspect = true, NoChangeArrowheads = true };

            nonVisualPictureDrawingProperties1.Append(pictureLocks1);
            ApplicationNonVisualDrawingProperties applicationNonVisualDrawingProperties2 = new ApplicationNonVisualDrawingProperties();

            nonVisualPictureProperties1.Append(nonVisualDrawingProperties2);
            nonVisualPictureProperties1.Append(nonVisualPictureDrawingProperties1);
            nonVisualPictureProperties1.Append(applicationNonVisualDrawingProperties2);

            P.BlipFill blipFill1 = new P.BlipFill();

            D.Blip blip1 = new D.Blip() { Embed = partRId1, CompressionState = D.BlipCompressionValues.Print };

            D.BlipExtensionList blipExtensionList1 = new D.BlipExtensionList();

            D.BlipExtension blipExtension1 = new D.BlipExtension() { Uri = "{28A0092B-C50C-407E-A947-70E740481C1C}" };

            A14.UseLocalDpi useLocalDpi1 = new A14.UseLocalDpi() { Val = false };
            useLocalDpi1.AddNamespaceDeclaration("a14", "http://schemas.microsoft.com/office/drawing/2010/main");

            blipExtension1.Append(useLocalDpi1);

            blipExtensionList1.Append(blipExtension1);

            blip1.Append(blipExtensionList1);
            D.SourceRectangle sourceRectangle1 = new D.SourceRectangle();

            D.Stretch stretch1 = new D.Stretch();
            D.FillRectangle fillRectangle1 = new D.FillRectangle();

            stretch1.Append(fillRectangle1);

            blipFill1.Append(blip1);
            blipFill1.Append(sourceRectangle1);
            blipFill1.Append(stretch1);

            P.ShapeProperties shapeProperties1 = new P.ShapeProperties() { BlackWhiteMode = D.BlackWhiteModeValues.Auto };

            D.Transform2D transform2D1 = new D.Transform2D() { Rotation = new Int32Value((int)(model.Angle * 60000)) };

            D.Offset offset2 = new D.Offset() { X = new Int64Value((long)((model.Position.X - viewPointRct.Left) * exportScale)), Y = new Int64Value((long)((model.Position.Y - viewPointRct.Top) * exportScale)) };
            D.Extents extents2 = new D.Extents() { Cx = new Int64Value((long)(model.Size.W * exportScale)), Cy = new Int64Value((long)(model.Size.H * exportScale)) };

            transform2D1.Append(offset2);
            transform2D1.Append(extents2);

            D.PresetGeometry presetGeometry1 = new D.PresetGeometry() { Preset = D.ShapeTypeValues.Rectangle };
            D.AdjustValueList adjustValueList1 = new D.AdjustValueList();

            presetGeometry1.Append(adjustValueList1);
            D.NoFill noFill1 = new D.NoFill();

            D.ShapePropertiesExtensionList shapePropertiesExtensionList1 = new D.ShapePropertiesExtensionList();

            D.ShapePropertiesExtension shapePropertiesExtension1 = new D.ShapePropertiesExtension() { Uri = "{909E8E84-426E-40DD-AFC4-6F175D3DCCD1}" };

            A14.HiddenFillProperties hiddenFillProperties1 = new A14.HiddenFillProperties();
            hiddenFillProperties1.AddNamespaceDeclaration("a14", "http://schemas.microsoft.com/office/drawing/2010/main");

            D.SolidFill solidFill1 = new D.SolidFill();
            D.RgbColorModelHex rgbColorModelHex1 = new D.RgbColorModelHex() { Val = "FFFFFF" };

            solidFill1.Append(rgbColorModelHex1);

            hiddenFillProperties1.Append(solidFill1);

            shapePropertiesExtension1.Append(hiddenFillProperties1);

            shapePropertiesExtensionList1.Append(shapePropertiesExtension1);

            shapeProperties1.Append(transform2D1);
            shapeProperties1.Append(presetGeometry1);
            shapeProperties1.Append(noFill1);
            shapeProperties1.Append(shapePropertiesExtensionList1);

            picture1.Append(nonVisualPictureProperties1);
            picture1.Append(blipFill1);
            picture1.Append(shapeProperties1);

            shapeTree.Append(picture1);

        }

        private void CreateTableRepresentation(TableModel model, SlidePart slidePart, ShapeTree shapeTree, double exportScale, Rct viewPointRct, string partRId1, string partRId2)
        {
            P.GraphicFrame graphicFrame1 = new P.GraphicFrame();

            P.NonVisualGraphicFrameProperties nonVisualGraphicFrameProperties1 = new P.NonVisualGraphicFrameProperties();
            P.NonVisualDrawingProperties nonVisualDrawingProperties2 = new P.NonVisualDrawingProperties() { Id = (UInt32Value)3U, Name = "Table 2" };

            P.NonVisualGraphicFrameDrawingProperties nonVisualGraphicFrameDrawingProperties1 = new P.NonVisualGraphicFrameDrawingProperties();
            D.GraphicFrameLocks graphicFrameLocks1 = new D.GraphicFrameLocks() { NoGrouping = true };

            nonVisualGraphicFrameDrawingProperties1.Append(graphicFrameLocks1);

            ApplicationNonVisualDrawingProperties applicationNonVisualDrawingProperties2 = new ApplicationNonVisualDrawingProperties();

            ApplicationNonVisualDrawingPropertiesExtensionList applicationNonVisualDrawingPropertiesExtensionList1 = new ApplicationNonVisualDrawingPropertiesExtensionList();

            ApplicationNonVisualDrawingPropertiesExtension applicationNonVisualDrawingPropertiesExtension1 = new ApplicationNonVisualDrawingPropertiesExtension() { Uri = "{D42A27DB-BD31-4B8C-83A1-F6EECF244321}" };

            P14.ModificationId modificationId1 = new P14.ModificationId() { Val = (UInt32Value)3734533096U };
            modificationId1.AddNamespaceDeclaration("p14", "http://schemas.microsoft.com/office/powerpoint/2010/main");

            applicationNonVisualDrawingPropertiesExtension1.Append(modificationId1);

            applicationNonVisualDrawingPropertiesExtensionList1.Append(applicationNonVisualDrawingPropertiesExtension1);

            applicationNonVisualDrawingProperties2.Append(applicationNonVisualDrawingPropertiesExtensionList1);

            nonVisualGraphicFrameProperties1.Append(nonVisualDrawingProperties2);
            nonVisualGraphicFrameProperties1.Append(nonVisualGraphicFrameDrawingProperties1);
            nonVisualGraphicFrameProperties1.Append(applicationNonVisualDrawingProperties2);

            Transform transform1 = new Transform();

            D.Offset offset2 = new D.Offset() { X = new Int64Value((long)((model.Position.X - viewPointRct.Left) * exportScale)), Y = new Int64Value((long)((model.Position.Y - viewPointRct.Top) * exportScale)) };
            D.Extents extents2 = new D.Extents() { Cx = new Int64Value((long)(model.Size.W * exportScale)), Cy = new Int64Value((long)(model.Size.H * exportScale)) };

            transform1.Append(offset2);
            transform1.Append(extents2);

            D.Graphic graphic1 = new D.Graphic();

            D.GraphicData graphicData1 = new D.GraphicData() { Uri = "http://schemas.openxmlformats.org/drawingml/2006/table" };

            D.Table table1 = new D.Table();

            D.TableProperties tableProperties1 = new D.TableProperties() { FirstRow = true, BandRow = true };
            D.TableStyleId tableStyleId1 = new D.TableStyleId();
            tableStyleId1.Text = "{5C22544A-7EE6-4342-B048-85BDC9FD1C3A}";


            //cD.InkTableCell = _cells[row * _numberOfCols + col];

            tableProperties1.Append(tableStyleId1);

            D.TableGrid tableGrid1 = new D.TableGrid();
            table1.Append(tableProperties1);
            table1.Append(tableGrid1);

            for (int i = 0; i < model.NumberOfCols; i++)
            {
                D.GridColumn gridColumn1 = new D.GridColumn() { Width = (long)(model.CellModels[i].Size.W * exportScale ) };
                tableGrid1.Append(gridColumn1);
            }

            for (int i = 0; i < model.NumberOfRows; i++)
            {
                D.TableRow tableRow1 = new D.TableRow() { Height = (long)(model.CellModels[i * model.NumberOfCols].Size.W * exportScale) };

                for (int j = 0; j < model.NumberOfCols; j++)
                {
                    D.TableCell tableCell1 = new D.TableCell();

                    D.TextBody textBody1 = new D.TextBody();
                    D.BodyProperties bodyProperties1 = new D.BodyProperties();
                    D.ListStyle listStyle1 = new D.ListStyle();

                    D.Paragraph paragraph1 = new D.Paragraph();

                    D.Run run1 = new D.Run();
                    D.RunProperties runProperties1 = new D.RunProperties() { Language = "en-US", Dirty = false, SmartTagClean = false };
                    D.Text text1 = new D.Text();
                    text1.Text = model.CellModels[i * model.NumberOfCols + j].Text;

                    run1.Append(runProperties1);
                    run1.Append(text1);
                    D.EndParagraphRunProperties endParagraphRunProperties1 = new D.EndParagraphRunProperties() { Language = "en-US", Dirty = false };

                    paragraph1.Append(run1);
                    paragraph1.Append(endParagraphRunProperties1);

                    textBody1.Append(bodyProperties1);
                    textBody1.Append(listStyle1);
                    textBody1.Append(paragraph1);
                    D.TableCellProperties tableCellProperties1 = new D.TableCellProperties();

                    tableCell1.Append(textBody1);
                    tableCell1.Append(tableCellProperties1);
                    tableRow1.Append(tableCell1);
                }
                table1.Append(tableRow1);
            }

            graphicData1.Append(table1);

            graphic1.Append(graphicData1);

            graphicFrame1.Append(nonVisualGraphicFrameProperties1);
            graphicFrame1.Append(transform1);
            graphicFrame1.Append(graphic1);

            shapeTree.Append(graphicFrame1);
        }

        private void CreateTextRepresentation(TextModel model, SlidePart slidePart, ShapeTree shapeTree, double exportScale, Rct viewPointRct, string partRId1, string partRId2)
        {
            P.Shape shape1 = new P.Shape();

            P.NonVisualShapeProperties nonVisualShapeProperties1 = new P.NonVisualShapeProperties();
            P.NonVisualDrawingProperties nonVisualDrawingProperties2 = new P.NonVisualDrawingProperties() { Id = (UInt32Value)3U, Name = "Content Placeholder" };

            P.NonVisualShapeDrawingProperties nonVisualShapeDrawingProperties1 = new P.NonVisualShapeDrawingProperties();
            D.ShapeLocks shapeLocks1 = new D.ShapeLocks() { NoGrouping = true };

            nonVisualShapeDrawingProperties1.Append(shapeLocks1);

            ApplicationNonVisualDrawingProperties applicationNonVisualDrawingProperties2 = new ApplicationNonVisualDrawingProperties();
            PlaceholderShape placeholderShape1 = new PlaceholderShape() { Index = (UInt32Value)1U };

            applicationNonVisualDrawingProperties2.Append(placeholderShape1);

            nonVisualShapeProperties1.Append(nonVisualDrawingProperties2);
            nonVisualShapeProperties1.Append(nonVisualShapeDrawingProperties1);
            nonVisualShapeProperties1.Append(applicationNonVisualDrawingProperties2);

            P.ShapeProperties shapeProperties1 = new P.ShapeProperties();

            D.Transform2D transform2D1 = new D.Transform2D() { Rotation = new Int32Value((int)(model.Angle * 60000)) };

            D.Offset offset2 = new D.Offset() { X = new Int64Value((long)((model.Position.X - viewPointRct.Left) * exportScale)), Y = new Int64Value((long)((model.Position.Y - viewPointRct.Top) * exportScale)) };
            D.Extents extents2 = new D.Extents() { Cx = new Int64Value((long)(model.Size.W * exportScale)), Cy = new Int64Value((long)(model.Size.H * exportScale)) };

            transform2D1.Append(offset2);
            transform2D1.Append(extents2);

            shapeProperties1.Append(transform2D1);

            P.TextBody textBody1 = new P.TextBody();

            D.BodyProperties bodyProperties1 = new D.BodyProperties();
            D.NormalAutoFit normalAutoFit1 = new D.NormalAutoFit();

            bodyProperties1.Append(normalAutoFit1);
            D.ListStyle listStyle1 = new D.ListStyle();

            textBody1.Append(bodyProperties1);
            textBody1.Append(listStyle1);

            int count = 0;
            foreach (var textLine in model.LineModels)
            {
                D.Paragraph paragraph1 = new D.Paragraph();
                D.ParagraphProperties paragraphProperties1 = new D.ParagraphProperties() { Level = textLine.IndentLevel };
                if (!textLine.Bullet)
                {
                    D.NoBullet noBullet2 = new D.NoBullet();
                    paragraphProperties1.Append(noBullet2);
                }

                D.Run run1 = new D.Run();
                D.RunProperties runProperties1 = new D.RunProperties() { Language = "en-US", Dirty = false, SmartTagClean = false };
                D.Text text1 = new D.Text();
                text1.Text = textLine.Text;

                run1.Append(runProperties1);
                run1.Append(text1);

                paragraph1.Append(paragraphProperties1);
                paragraph1.Append(run1);

                if (count == model.LineModels.Count - 1)
                {
                    D.EndParagraphRunProperties endParagraphRunProperties1 = new D.EndParagraphRunProperties() { Language = "en-US", Dirty = false };
                    paragraph1.Append(endParagraphRunProperties1);
                }

                textBody1.Append(paragraph1);
                count++;
            }

            shape1.Append(nonVisualShapeProperties1);
            shape1.Append(shapeProperties1);
            shape1.Append(textBody1);

            shapeTree.Append(shape1);
        }

        private void CreateChartRepresentation(ChartModel model, SlidePart slidePart, ShapeTree shapeTree, double exportScale, Rct viewPointRct, string partRId1, string partRId2)
        {
            P.GraphicFrame graphicFrame1 = new P.GraphicFrame();

            P.NonVisualGraphicFrameProperties nonVisualGraphicFrameProperties1 = new P.NonVisualGraphicFrameProperties();
            P.NonVisualDrawingProperties nonVisualDrawingProperties2 = new P.NonVisualDrawingProperties() { Id = (UInt32Value)2U, Name = "Chart 1" };
            P.NonVisualGraphicFrameDrawingProperties nonVisualGraphicFrameDrawingProperties1 = new P.NonVisualGraphicFrameDrawingProperties();

            ApplicationNonVisualDrawingProperties applicationNonVisualDrawingProperties2 = new ApplicationNonVisualDrawingProperties();

            ApplicationNonVisualDrawingPropertiesExtensionList applicationNonVisualDrawingPropertiesExtensionList1 = new ApplicationNonVisualDrawingPropertiesExtensionList();

            ApplicationNonVisualDrawingPropertiesExtension applicationNonVisualDrawingPropertiesExtension1 = new ApplicationNonVisualDrawingPropertiesExtension() { Uri = "{D42A27DB-BD31-4B8C-83A1-F6EECF244321}" };

            P14.ModificationId modificationId1 = new P14.ModificationId() { Val = (UInt32Value)3228825033U };
            modificationId1.AddNamespaceDeclaration("p14", "http://schemas.microsoft.com/office/powerpoint/2010/main");

            applicationNonVisualDrawingPropertiesExtension1.Append(modificationId1);

            applicationNonVisualDrawingPropertiesExtensionList1.Append(applicationNonVisualDrawingPropertiesExtension1);

            applicationNonVisualDrawingProperties2.Append(applicationNonVisualDrawingPropertiesExtensionList1);

            nonVisualGraphicFrameProperties1.Append(nonVisualDrawingProperties2);
            nonVisualGraphicFrameProperties1.Append(nonVisualGraphicFrameDrawingProperties1);
            nonVisualGraphicFrameProperties1.Append(applicationNonVisualDrawingProperties2);

            Transform transform1 = new Transform();
            D.Offset offset2 = new D.Offset() { X = new Int64Value((long)((model.Position.X - viewPointRct.Left) * exportScale)), Y = new Int64Value((long)((model.Position.Y - viewPointRct.Top) * exportScale)) };
            D.Extents extents2 = new D.Extents() { Cx = new Int64Value((long)(model.Size.W * exportScale)), Cy = new Int64Value((long)(model.Size.H * exportScale)) };

            transform1.Append(offset2);
            transform1.Append(extents2);

            D.Graphic graphic1 = new D.Graphic();

            D.GraphicData graphicData1 = new D.GraphicData() { Uri = "http://schemas.openxmlformats.org/drawingml/2006/chart" };

            C.ChartReference chartReference1 = new C.ChartReference() { Id = partRId2 };
            chartReference1.AddNamespaceDeclaration("c", "http://schemas.openxmlformats.org/drawingml/2006/chart");
            chartReference1.AddNamespaceDeclaration("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");

            graphicData1.Append(chartReference1);

            graphic1.Append(graphicData1);

            graphicFrame1.Append(nonVisualGraphicFrameProperties1);
            graphicFrame1.Append(transform1);
            graphicFrame1.Append(graphic1);

            shapeTree.Append(graphicFrame1);
        }

        private void GenerateChartPart1Content(ChartPart chartPart1, ChartModel model)
        {

            {
                C.ChartSpace chartSpace1 = new C.ChartSpace();
                chartSpace1.AddNamespaceDeclaration("c", "http://schemas.openxmlformats.org/drawingml/2006/chart");
                chartSpace1.AddNamespaceDeclaration("a", "http://schemas.openxmlformats.org/drawingml/2006/main");
                chartSpace1.AddNamespaceDeclaration("r", "http://schemas.openxmlformats.org/officeDocument/2006/relationships");
                C.Date1904 date19041 = new C.Date1904() { Val = false };
                C.EditingLanguage editingLanguage1 = new C.EditingLanguage() { Val = "en-US" };
                C.RoundedCorners roundedCorners1 = new C.RoundedCorners() { Val = false };

                AlternateContent alternateContent1 = new AlternateContent();
                alternateContent1.AddNamespaceDeclaration("mc", "http://schemas.openxmlformats.org/markup-compatibility/2006");

                AlternateContentChoice alternateContentChoice1 = new AlternateContentChoice() { Requires = "c14" };
                alternateContentChoice1.AddNamespaceDeclaration("c14", "http://schemas.microsoft.com/office/drawing/2007/8/2/chart");
                C14.Style style1 = new C14.Style() { Val = 102 };

                alternateContentChoice1.Append(style1);

                AlternateContentFallback alternateContentFallback1 = new AlternateContentFallback();
                C.Style style2 = new C.Style() { Val = 2 };

                alternateContentFallback1.Append(style2);

                alternateContent1.Append(alternateContentChoice1);
                alternateContent1.Append(alternateContentFallback1);

                C.Chart chart1 = new C.Chart();
                C.AutoTitleDeleted autoTitleDeleted1 = new C.AutoTitleDeleted() { Val = false };

                C.PlotArea plotArea1 = new C.PlotArea();
                C.Layout layout1 = new C.Layout();

                C.LineChart lineChart1 = new C.LineChart();
                C.Grouping grouping1 = new C.Grouping() { Val = C.GroupingValues.Standard };
                C.VaryColors varyColors1 = new C.VaryColors() { Val = false };

                lineChart1.Append(grouping1);
                lineChart1.Append(varyColors1);



                for (int s = 0; s < model.Series.Count; s++)
                {
                    ////
                    C.LineChartSeries lineChartSeries1 = new C.LineChartSeries();
                    C.Index index1 = new C.Index() { Val = new UInt32Value((uint)s) };
                    C.Order order1 = new C.Order() { Val = new UInt32Value((uint)s) };

                    C.SeriesText seriesText1 = new C.SeriesText();

                    C.StringReference stringReference1 = new C.StringReference();
                    C.Formula formula1 = new C.Formula();
                    formula1.Text = "Sheet1!$B$1";

                    C.StringCache stringCache1 = new C.StringCache();
                    C.PointCount pointCount1 = new C.PointCount() { Val = (UInt32Value)1U };

                    C.StringPoint stringPoint1 = new C.StringPoint() { Index = (UInt32Value)0U };
                    C.NumericValue numericValue1 = new C.NumericValue();
                    numericValue1.Text = model.SerieNames[s];

                    stringPoint1.Append(numericValue1);

                    stringCache1.Append(pointCount1);
                    stringCache1.Append(stringPoint1);

                    stringReference1.Append(formula1);
                    stringReference1.Append(stringCache1);

                    seriesText1.Append(stringReference1);

                    C.CategoryAxisData categoryAxisData1 = new C.CategoryAxisData();

                    C.StringReference stringReference2 = new C.StringReference();
                    C.Formula formula2 = new C.Formula();
                    formula2.Text = "Sheet1!$A$2:$A$3";

                    C.StringCache stringCache2 = new C.StringCache();
                    C.PointCount pointCount2 = new C.PointCount() { Val = new UInt32Value((uint)model.Series[s].Count) };
                    stringCache2.Append(pointCount2);

                    for (int p = 0; p < model.Series[s].Count; p++)
                    {
                        C.StringPoint stringPoint2 = new C.StringPoint() { Index = new UInt32Value((uint)p) };
                        C.NumericValue numericValue2 = new C.NumericValue();
                        numericValue2.Text = model.Series[s][p].X.ToString();

                        stringPoint2.Append(numericValue2);
                        stringCache2.Append(stringPoint2);
                    }

                    stringReference2.Append(formula2);
                    stringReference2.Append(stringCache2);

                    categoryAxisData1.Append(stringReference2);

                    C.Values values1 = new C.Values();

                    C.NumberReference numberReference1 = new C.NumberReference();
                    C.Formula formula3 = new C.Formula();
                    formula3.Text = "Sheet1!$B$2:$B$3";

                    C.NumberingCache numberingCache1 = new C.NumberingCache();
                    C.FormatCode formatCode1 = new C.FormatCode();
                    formatCode1.Text = "General";
                    C.PointCount pointCount3 = new C.PointCount() { Val = new UInt32Value((uint)model.Series[s].Count) };
                    numberingCache1.Append(formatCode1);
                    numberingCache1.Append(pointCount3);

                    for (int p = 0; p < model.Series[s].Count; p++)
                    {
                        C.NumericPoint numericPoint1 = new C.NumericPoint() { Index = new UInt32Value((uint)p) };
                        C.NumericValue numericValue4 = new C.NumericValue();
                        numericValue4.Text = model.Series[s][p].Y.ToString();

                        numericPoint1.Append(numericValue4);

                        numberingCache1.Append(numericPoint1);
                    }

                    numberReference1.Append(formula3);
                    numberReference1.Append(numberingCache1);

                    values1.Append(numberReference1);
                    C.Smooth smooth1 = new C.Smooth() { Val = false };

                    lineChartSeries1.Append(index1);
                    lineChartSeries1.Append(order1);
                    lineChartSeries1.Append(seriesText1);
                    lineChartSeries1.Append(categoryAxisData1);
                    lineChartSeries1.Append(values1);
                    lineChartSeries1.Append(smooth1);

                    lineChart1.Append(lineChartSeries1);

                    ////
                }
            

                C.DataLabels dataLabels1 = new C.DataLabels();
                C.ShowLegendKey showLegendKey1 = new C.ShowLegendKey() { Val = false };
                C.ShowValue showValue1 = new C.ShowValue() { Val = false };
                C.ShowCategoryName showCategoryName1 = new C.ShowCategoryName() { Val = false };
                C.ShowSeriesName showSeriesName1 = new C.ShowSeriesName() { Val = false };
                C.ShowPercent showPercent1 = new C.ShowPercent() { Val = false };
                C.ShowBubbleSize showBubbleSize1 = new C.ShowBubbleSize() { Val = false };

                dataLabels1.Append(showLegendKey1);
                dataLabels1.Append(showValue1);
                dataLabels1.Append(showCategoryName1);
                dataLabels1.Append(showSeriesName1);
                dataLabels1.Append(showPercent1);
                dataLabels1.Append(showBubbleSize1);
                C.ShowMarker showMarker1 = new C.ShowMarker() { Val = true };
                C.Smooth smooth3 = new C.Smooth() { Val = false };
                C.AxisId axisId1 = new C.AxisId() { Val = (UInt32Value)83759104U };
                C.AxisId axisId2 = new C.AxisId() { Val = (UInt32Value)83760640U };
                
                lineChart1.Append(dataLabels1);
                lineChart1.Append(showMarker1);
                lineChart1.Append(smooth3);
                lineChart1.Append(axisId1);
                lineChart1.Append(axisId2);

                C.CategoryAxis categoryAxis1 = new C.CategoryAxis();
                C.AxisId axisId3 = new C.AxisId() { Val = (UInt32Value)83759104U };

                C.Scaling scaling1 = new C.Scaling();
                C.Orientation orientation1 = new C.Orientation() { Val = C.OrientationValues.MinMax };

                scaling1.Append(orientation1);
                C.Delete delete1 = new C.Delete() { Val = false };
                C.AxisPosition axisPosition1 = new C.AxisPosition() { Val = C.AxisPositionValues.Bottom };
                C.NumberingFormat numberingFormat1 = new C.NumberingFormat() { FormatCode = "General", SourceLinked = true };
                C.MajorTickMark majorTickMark1 = new C.MajorTickMark() { Val = C.TickMarkValues.Outside };
                C.MinorTickMark minorTickMark1 = new C.MinorTickMark() { Val = C.TickMarkValues.None };
                C.TickLabelPosition tickLabelPosition1 = new C.TickLabelPosition() { Val = C.TickLabelPositionValues.NextTo };
                C.CrossingAxis crossingAxis1 = new C.CrossingAxis() { Val = (UInt32Value)83760640U };
                C.Crosses crosses1 = new C.Crosses() { Val = C.CrossesValues.AutoZero };
                C.AutoLabeled autoLabeled1 = new C.AutoLabeled() { Val = true };
                C.LabelAlignment labelAlignment1 = new C.LabelAlignment() { Val = C.LabelAlignmentValues.Center };
                C.LabelOffset labelOffset1 = new C.LabelOffset() { Val = (UInt16Value)100U };
                C.NoMultiLevelLabels noMultiLevelLabels1 = new C.NoMultiLevelLabels() { Val = false };

                categoryAxis1.Append(axisId3);
                categoryAxis1.Append(scaling1);
                categoryAxis1.Append(delete1);
                categoryAxis1.Append(axisPosition1);
                categoryAxis1.Append(numberingFormat1);
                categoryAxis1.Append(majorTickMark1);
                categoryAxis1.Append(minorTickMark1);
                categoryAxis1.Append(tickLabelPosition1);
                categoryAxis1.Append(crossingAxis1);
                categoryAxis1.Append(crosses1);
                categoryAxis1.Append(autoLabeled1);
                categoryAxis1.Append(labelAlignment1);
                categoryAxis1.Append(labelOffset1);
                categoryAxis1.Append(noMultiLevelLabels1);

                C.ValueAxis valueAxis1 = new C.ValueAxis();
                C.AxisId axisId4 = new C.AxisId() { Val = (UInt32Value)83760640U };

                C.Scaling scaling2 = new C.Scaling();
                C.Orientation orientation2 = new C.Orientation() { Val = C.OrientationValues.MinMax };

                scaling2.Append(orientation2);
                C.Delete delete2 = new C.Delete() { Val = false };
                C.AxisPosition axisPosition2 = new C.AxisPosition() { Val = C.AxisPositionValues.Left };
                C.MajorGridlines majorGridlines1 = new C.MajorGridlines();
                C.NumberingFormat numberingFormat2 = new C.NumberingFormat() { FormatCode = "General", SourceLinked = true };
                C.MajorTickMark majorTickMark2 = new C.MajorTickMark() { Val = C.TickMarkValues.Outside };
                C.MinorTickMark minorTickMark2 = new C.MinorTickMark() { Val = C.TickMarkValues.None };
                C.TickLabelPosition tickLabelPosition2 = new C.TickLabelPosition() { Val = C.TickLabelPositionValues.NextTo };
                C.CrossingAxis crossingAxis2 = new C.CrossingAxis() { Val = (UInt32Value)83759104U };
                C.Crosses crosses2 = new C.Crosses() { Val = C.CrossesValues.AutoZero };
                C.CrossBetween crossBetween1 = new C.CrossBetween() { Val = C.CrossBetweenValues.Between };

                valueAxis1.Append(axisId4);
                valueAxis1.Append(scaling2);
                valueAxis1.Append(delete2);
                valueAxis1.Append(axisPosition2);
                valueAxis1.Append(majorGridlines1);
                valueAxis1.Append(numberingFormat2);
                valueAxis1.Append(majorTickMark2);
                valueAxis1.Append(minorTickMark2);
                valueAxis1.Append(tickLabelPosition2);
                valueAxis1.Append(crossingAxis2);
                valueAxis1.Append(crosses2);
                valueAxis1.Append(crossBetween1);

                plotArea1.Append(layout1);
                plotArea1.Append(lineChart1);
                plotArea1.Append(categoryAxis1);
                plotArea1.Append(valueAxis1);

                C.Legend legend1 = new C.Legend();
                C.LegendPosition legendPosition1 = new C.LegendPosition() { Val = C.LegendPositionValues.Right };
                C.Layout layout2 = new C.Layout();
                C.Overlay overlay1 = new C.Overlay() { Val = false };

                legend1.Append(legendPosition1);
                legend1.Append(layout2);
                legend1.Append(overlay1);
                C.PlotVisibleOnly plotVisibleOnly1 = new C.PlotVisibleOnly() { Val = true };
                C.DisplayBlanksAs displayBlanksAs1 = new C.DisplayBlanksAs() { Val = C.DisplayBlanksAsValues.Gap };
                C.ShowDataLabelsOverMaximum showDataLabelsOverMaximum1 = new C.ShowDataLabelsOverMaximum() { Val = false };

                chart1.Append(autoTitleDeleted1);
                chart1.Append(plotArea1);
                chart1.Append(legend1);
                chart1.Append(plotVisibleOnly1);
                chart1.Append(displayBlanksAs1);
                chart1.Append(showDataLabelsOverMaximum1);

                C.TextProperties textProperties1 = new C.TextProperties();
                D.BodyProperties bodyProperties1 = new D.BodyProperties();
                D.ListStyle listStyle1 = new D.ListStyle();

                D.Paragraph paragraph1 = new D.Paragraph();

                D.ParagraphProperties paragraphProperties1 = new D.ParagraphProperties();
                D.DefaultRunProperties defaultRunProperties11 = new D.DefaultRunProperties() { FontSize = 1800 };

                paragraphProperties1.Append(defaultRunProperties11);
                D.EndParagraphRunProperties endParagraphRunProperties1 = new D.EndParagraphRunProperties() { Language = "en-US" };

                paragraph1.Append(paragraphProperties1);
                paragraph1.Append(endParagraphRunProperties1);

                textProperties1.Append(bodyProperties1);
                textProperties1.Append(listStyle1);
                textProperties1.Append(paragraph1);

                //C.ExternalData externalData1 = new C.ExternalData() { Id = "rId1" };
                //C.AutoUpdate autoUpdate1 = new C.AutoUpdate() { Val = false };

                //externalData1.Append(autoUpdate1);

                chartSpace1.Append(date19041);
                chartSpace1.Append(editingLanguage1);
                chartSpace1.Append(roundedCorners1);
                chartSpace1.Append(alternateContent1);
                chartSpace1.Append(chart1);
                chartSpace1.Append(textProperties1);
                //chartSpace1.Append(externalData1);

                chartPart1.ChartSpace = chartSpace1;
            }
        }

        private string convertColor(System.Windows.Media.Color color)
        {
            var c = color.ToString();
            c = c.Substring(3);
            return c;
        }

        private void displayValidationErrors(IEnumerable<ValidationErrorInfo> errors)
        {
            int errorIndex = 1;

            foreach (ValidationErrorInfo errorInfo in errors)
            {
                Console.WriteLine(errorInfo.Description);
                Console.WriteLine(errorInfo.Path.XPath);

                if (++errorIndex <= errors.Count())
                    Console.WriteLine("================");
            }
        }

    }
}
