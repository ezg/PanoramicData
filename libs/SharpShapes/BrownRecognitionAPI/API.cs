using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrownRecognitionCommon;
using System.Windows;
using starPadSDK.Inq;
using ShapeRecognizer;
using starPadSDK.Geom;

namespace BrownRecognitionAPI
{
    //  Doxygen main page comments:
    //  ---------------------------
    /// \mainpage Overview
    /// 
    /// \section a Types and Modes
    /// The BrownRecognition API supports different types and modes of recognition. 
    /// The following sections give an overview of how they can be used. 
    ///
    ///
    /// 
    /// \subsection aa Shape Recognition
    /// Shape recognition is intended to create a shape from one or more user input strokes. 
    /// The provided methods will return a BrownRecognitionCommon.BrownShape (or null) that has an attached shape type (see 
    /// BrownRecognitionCommon.ShapeType) and a list of points that make up the clean version of this shape. 
    /// \n 
    /// \n
    /// Example of how to use the one stroke shape recognition:
    /// \code
    /// // whatever the calling application uses as its stroke representation. 
    /// ApplicationStroke appStroke; 
    /// 
    /// // create a BrownInputStroke
    /// BrownInputStroke brownStroke = new BrownInputStroke(appStroke.ToPointArray(), appStroke);
    /// 
    /// // call the recognizer
    /// BrownShape brownShape = BrownRecognitionAPI.API.RecognizeBrownShape(brownStroke);
    /// if (brownShape != null) {
    ///     // once a shape has been recognized, the application probably
    ///     // wants to remove their stroke from the UI.
    ///     foreach (BrownInputStroke bis in brownShape.BrownInputStrokes) {
    ///         UI.remove((ApplicationStroke) bis.Data);
    ///     }
    ///     
    ///     // add a corresponding shape to the UI.
    ///     if (brownShape.ShapeType == ShapeType.Rect) {
    ///         UI.add(new Rectangle(brownShape.ShapePoints));
    ///     }
    ///     else if (brownShape.ShapeType == ShapeType.Circle) {
    ///         ...
    ///     }
    ///     ...
    /// }
    /// \endcode
    /// 
    /// \n
    /// Example of how to use the multi stroke shape recognition:
    /// \code
    /// // List of all lines that could be part of a new shape
    /// List<BrownShape> lineShapes = new List<BrownShape>();
    /// 
    /// // Add whatever makes sense to the lineShapes list, i.e. already recognized
    /// // shapes, new input strokes etc. 
    /// foreach (UIShape s in UI.Lines) {
    ///     BrownShape brownShape = s.ConvertToBrownShape();
    ///     // make sure that the underlying BrownInputStroke is set and that 
    ///     // the Data propery has a value that allows you to backtrack to the UIShape. 
    ///     brownShape.BrownInputStrokes[0].Data = s;
    ///     lineShapes.Add(brownShape);
    /// }
    /// foreach (ApplicationStroke s in UI.Strokes) {
    ///     // either convert an application stroke yourself or call RecognizeBrownShape()
    ///     // as in the example above.
    ///     BrownShape brownShape = s.ConvertToBrownShape();
    ///     lineShapes.Add(brownShape);
    /// }
    /// 
    /// // call the recognizer
    /// BrownShape brownShape = BrownRecognitionAPI.API.RecognizeBrownShape(lineShapes);
    /// if (brownShape != null) {
    ///     // once a shape has been recognized, the application probably
    ///     // wants to remove their stroke from the UI.
    ///     foreach(BrownInputStroke bis in brownShape.BrownInputStrokes) {
    ///         if (bis.Data is ApplicationStroke) {
    ///             UI.remove((ApplicationStroke) bis.Data);
    ///         }
    ///         else if (bis.Data is UIShape) {
    ///             UI.remove((UIShape) bis.Data);
    ///         }
    ///     }
    ///     
    ///     // add a corresponding shape to the UI.
    ///     if (brownShape.ShapeType == ShapeType.Rect) {
    ///         UI.add(new Rectangle(brownShape.ShapePoints));
    ///     }
    ///     else if (brownShape.ShapeType == ShapeType.Circle) {
    ///         ...
    ///     }
    ///     ...
    /// }
    /// \endcode
    /// 
    ///
    ///
    /// 
    /// \subsection ab Template Recognition with Feedback
    /// This mode can be used to give the user some intermediate feedback while he is drawing a template. In order
    /// to do that, the Application needs to trigger the template recognition after every new stroke and needs to 
    /// keep track of what previous strokes have been recognized as. 
    /// \n
    /// \n
    /// Example of how to use template recognition with feedback:
    /// \code
    /// // List of all shapes that could be part of a template
    /// List<BrownShape> brownShapes = new List<BrownShape>();
    /// 
    /// // Add whatever makes sense to the brownShapes list, i.e. already recognized
    /// // shapes, already cleaned up template shapes, new input strokes etc. 
    /// foreach (UIShape s in UI.Shapes) {
    ///     BrownShape brownShape = s.ConvertToBrownShape();
    ///     // make sure that the underlying BrownInputStroke is set and that 
    ///     // the Data propery has a value that allows you to backtrack to the UIShape. 
    ///     brownShape.BrownInputStrokes[0].Data = s;
    ///     brownShapes.Add(brownShape);
    /// }
    /// foreach (ApplicationStroke s in UI.Strokes) {
    ///     // either convert an application stroke yourself or call RecognizeBrownShape()
    ///     // as in the example above.
    ///     BrownShape brownShape = s.ConvertToBrownShape();
    ///     brownShapes.Add(brownShape);
    /// }
    /// 
    /// // call the recognizer
    /// List<BrownTemplate> recognizedTemplates = BrownRecognitionAPI.API.RecognizeBrownTemplate(brownShapes);
    /// 
    /// // for feedback: update your UI according to what templates have been recognized.
    /// // e.g. color shapes that are part of a template, etc. 
    /// UI.Feedback(recognizedTemplates);
    /// \endcode
    /// 
    ///
    ///
    /// 
    /// \subsection ac Template Recognition without Feedback
    /// This mode can be used if template recognition is triggered by the user (e.g. with a button). This 
    /// This method will be slower than the feedback one, since every input stroke will first be passed to the shape recognizer. It
    /// is not intended to be called on every new stroke.
    /// \n
    /// \n
    /// Example of how to use template recognition without feedback:
    /// \code
    /// // List of all input strokes that could be part of a template
    /// List<BrownInputStroke> brownInputStrokes = new List<BrownInputStroke>();
    /// 
    /// // Add whatever makes sense to the brownInputStrokes list, i.e. already recognized
    /// // shapes, already cleaned up template shapes, new input strokes etc. 
    /// foreach (UIShape s in UI.Shapes) {
    ///     BrownInputStroke brownStroke = s.ConvertToBrownInputStroke();
    ///     // make sure that the Data propery has a value that allows you to backtrack to the UIShape. 
    ///     brownStroke.Data = s;
    ///     brownInputStrokes.Add(brownShape);
    /// }
    /// foreach (ApplicationStroke s in UI.Strokes) {
    ///     BrownInputStroke brownStroke = new BrownInputStroke(s.ToPointArray(), s);
    ///     brownInputStrokes.Add(brownStroke);
    /// }
    /// 
    /// // call the recognizer
    /// List<BrownTemplate> recognizedTemplates = BrownRecognitionAPI.API.RecognizeBrownTemplate(brownInputStrokes);
    /// 
    /// \endcode
    ///    
    ///
    ///
    /// 
    /// \subsection ad Template Clean-up
    /// After a template got recognized, the API provides a method to get a set of clean shapes building the corresponding template.
    /// \n
    /// \n
    /// Example of how to use template clean-up:
    /// \code
    /// // the template you want to clean up
    /// BrownTemplate template; 
    /// 
    /// // call the API to get a list of clean shapes. 
    /// List<BrownShape> elements = BrownRecognitionAPI.API.GetCleanTemplateShapes(template);
    /// 
    /// foreach (BrownShape element in elements) {
    ///     // once we have clean shapes we want to remove everything that was used
    ///     // to create a clean shape from the UI. 
    ///     foreach (BrownInputStroke bis in element.BrownInputStrokes) {
    ///         if (bis.Data is ApplicationStroke) {
    ///             UI.remove((ApplicationStroke) bis.Data);
    ///         }
    ///         else if (bis.Data is UIShape) {
    ///             UI.remove((UIShape) bis.Data);
    ///         }
    ///     }
    ///     
    ///     // add a corresponding shape to the UI.
    ///     if (element.ShapeType == ShapeType.Rect) {
    ///         UI.add(new Rectangle(brownShape.ShapePoints));
    ///     }
    ///     else if (element.ShapeType == ShapeType.Circle) {
    ///         ...
    ///     }
    ///     ...
    /// }
    /// \endcode
    /// 
    ///
    ///
    /// 
    /// \section b Calling from C++
    /// To call the API from unmanged C++ code, a bridge C++ file must created and compiled as managed code. Other 
    /// unmanaged C++ code will then be able to call functions in that file. Inside the unmanged file, the same 
    /// logic as in the examples above can be applied with slightly differnet syntax.
    /// \n
    /// \n
    /// Example of how to call the API in C++:
    /// \code
    /// // Shape Recognition
	/// BrownRecognitionCommon::BrownInputStroke^ stroke = gcnew BrownRecognitionCommon::BrownInputStroke(pts, backtrackObject);
	/// BrownRecognitionCommon::BrownShape^ brownShape = BrownRecognitionAPI::API::RecognizeBrownShape(stroke);
	/// 
	/// // Template Recognition
	/// List<BrownRecognitionCommon::BrownInputStroke^>^ inputStrokes = gcnew List<BrownRecognitionCommon::BrownInputStroke^>();
	/// inputStrokes->Add(stroke);
    /// List<BrownRecognitionCommon::BrownTemplate^>^ templates = BrownRecognitionAPI::API::RecognizeBrownTemplate(inputStrokes);
    /// 
    /// // Settings
	/// System::String^ settings = BrownRecognitionAPI::API::GetBrownRecognitionSettings();
    /// BrownRecognitionAPI::API::SetBrownRecognitionSettings(settings);
    /// \endcode
    /// 
    ///    
    ///    
    /// \section c Settings
    /// The API overs methods to get and set the settings using XML. See BrownRecognitionCommon.BrownRecognitionSettings
    /// for further details on what the settings mean. A typical 
    /// XML looks like this:
    /// \code
    /// <?xml version="1.0" encoding="utf-16"?>
    /// <BrownRecognitionSettings xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
    ///   <OrgChartBlockConnectorDistanceThreshold>25</OrgChartBlockConnectorDistanceThreshold>
    ///   <OrgChartWidthPolicy>AverageOnChart</OrgChartWidthPolicy>
    ///   <OrgChartHeightPolicy>AverageOnChart</OrgChartHeightPolicy>
    ///   <OrgChartHorizontalSpacingPolicy>StickToConstant</OrgChartHorizontalSpacingPolicy>
    ///   <OrgChartVerticalSpacingPolicy>StickToConstant</OrgChartVerticalSpacingPolicy>
    ///   <OrgChartConstantHorizontalSpacing>30</OrgChartConstantHorizontalSpacing>
    ///   <OrgChartConstantVerticalSpacing>30</OrgChartConstantVerticalSpacing>
    ///   <PieChartMinimumLengthOfLinesInPercentOfCircleRadius>0.2</PieChartMinimumLengthOfLinesInPercentOfCircleRadius>
    ///   <TableDiagramContainsThreshold>25</TableDiagramContainsThreshold>
    ///   <TableDiagramAlignmentThreshold>10</TableDiagramAlignmentThreshold>
    ///   <TableDiagramVerticalLineToleranceInPercentOfLength>0.3</TableDiagramVerticalLineToleranceInPercentOfLength>
    ///   <TableDiagramHorizontalLineToleranceInPercentOfLength>0.3</TableDiagramHorizontalLineToleranceInPercentOfLength>
    /// </BrownRecognitionSettings>
    /// \endcode
    //  ---------------------------

    /// <summary>
    /// Entry point for all calls to the BrownRecognition API. 
    /// </summary>
    public class API
    {
        /// <summary>
        /// Returns all the Brown Recognition settings as an XML string 
        /// (see BrownRecognitionCommon.BrownRecognitionSettings).
        /// </summary>
        /// <returns>XML string containing all the current setting values.</returns>
        public static string GetBrownRecognitionSettings()
        {
            return BrownRecognitionSettings.SerializeToXML();
        }

        /// <summary>
        /// Sets all the Brown Recognition settings 
        /// (see BrownRecognitionCommon.BrownRecognitionSettings).
        /// </summary>
        /// <param name="xml">XML string containing all the setting values.</param>
        public static void SetBrownRecognitionSettings(string xml)
        {
            BrownRecognitionSettings.DeserializeFromXML(xml);
        }

        /// <summary>
        /// Takes a single stroke and tries to recognized it as a primitive shape.
        /// </summary>
        /// <param name="brownInputStroke">The input stroke. The Data property can be filled with an object that allows users
        /// to backtrack the original stroke object of their application. </param>
        /// <returns>The BrownRecognitionCommon.BrownShape containing all the information about the recognized shape or null if 
        /// no shape was recognized. The returned shape will contain the BrownRecognitionCommon.BrownInputStroke which was used
        /// to build this shape (application strokes can be backtracked with the Data property).</returns>
        public static BrownShape RecognizeBrownShape(BrownInputStroke brownInputStroke)
        {
            ShapeType stype;
            Stroq stroq = new Stroq(brownInputStroke.StrokePoints);

            ShapeRecognizer.Recognizer.SelectionObj res;
            if (Recognizer.RecognizeScribbleDelete(stroq, null, out res))
            {
                Rct r = stroq.GetBounds();
                return new BrownShape(ShapeType.Scribble, 
                    new Point[5] { new Point(r.TopLeft.X, r.TopLeft.Y),
                                   new Point(r.TopRight.X, r.TopRight.Y),
                                   new Point(r.BottomRight.X, r.BottomRight.Y),
                                   new Point(r.BottomLeft.X, r.BottomLeft.Y),
                                   new Point(r.TopLeft.X, r.TopLeft.Y)}, brownInputStroke); 
            }

            List<Point> pts = Recognizer.Recognize(out stype, stroq, Recognizer.RecognizeShapeHint(stroq, false));
            if (stype == ShapeType.None)
            {
                return null;
            }
            else
            {
                return new BrownShape(stype, pts.ToArray(), brownInputStroke);
            }
        }

        /// <summary>
        /// Attempts to recognize a primitive shape from a set of lines.
        /// </summary>
        /// <param name="brownShapes">The set of BrownRecognitionCommon.BrownShape that will be used to try to build a
        /// shape out of it. Only BrownRecognitionCommon.BrownShape of type StraightLine or Polyline are considered. </param>
        /// <returns>The BrownRecognitionCommon.BrownShape containing all the information about the recognized shape or null if 
        /// no shape was recognized. The returned shape will contain all BrownRecognitionCommon.BrownInputStrokes that were used
        /// to build this shape (application strokes can be backtracked with the Data property).</returns>
        public static BrownShape RecognizeBrownShape(List<BrownShape> brownShapes)
        {
            ShapeType stype;
            List<BrownShape> usedBrownShapes;
            List<Point> pts = Recognizer.Recognize(out stype, out usedBrownShapes, brownShapes);
            if (stype == ShapeType.None)
            {
                return null;
            }
            else
            {
                BrownShape ret = new BrownShape(stype, pts.ToArray(), null);
                brownShapes.Clear();
                foreach (BrownShape bs in usedBrownShapes)
                {
                    foreach (BrownInputStroke bis in bs.BrownInputStrokes)
                    {
                        ret.BrownInputStrokes.Add(bis);
                    }
                    brownShapes.Add(bs);
                }
                
                return ret;
            }
        }


        /// <summary>
        /// Attempts to recognize templates from a set of shapes. This method can be used to provide some feedback to the 
        /// user. Every new stroke that a user draws should first be processed with one of the RecognizeBrownShape() methods.
        /// All pre-recognized shapes (plus the new one) can then be passed into this method. Editing templates can be achieved by 
        /// converting allready reognized template blocks back into a BrownRecognitionCommon.BrownShape (set Data property of the 
        /// underlying BrownRecognitionCommon.BrownInputStroke to backtrack).
        /// </summary>
        /// <param name="brownShapes">The set of BrownRecognitionCommon.BrownShape that will be used to try to recognize
        /// a template out of it. To get a BrownRecognitionCommon.BrownShape, one of the RecognizeBrownShape() methods should
        /// be called.</param>
        /// <returns>List of BrownRecognitionCommon.BrownTemplate that were recognized. The templates
        /// contain all the shapes that were used to reocognize it.
        /// </returns>
        public static List<BrownTemplate> RecognizeBrownTemplate(List<BrownShape> brownShapes)
        {
            List<BrownTemplate> recognizedTemplates = TemplateRecognizer.TemplateRecognizer.RecognizeTemplate(brownShapes);
            return recognizedTemplates;
        }

        /// <summary>
        /// Attempts to recognize templates from a set of strokes. It can be used to do direct template recognition (without 
        /// feedback). This method will be slower, since every input stroke will first be passed to the shape recognizer. It
        /// is not intended to be called on every new stroke.
        /// Editing templates can be achieved by 
        /// converting allready reognized template blocks back into a BrownRecognitionCommon.BrownShape (set Data property of the 
        /// underlying BrownRecognitionCommon.BrownInputStroke to backtrack).
        /// </summary>
        /// <param name="strokes">The input strokes. The Data property can be filled with an object that allows users
        /// to backtrack the original stroke object of their application. </param>
        /// <returns></returns>
        public static List<BrownTemplate> RecognizeBrownTemplate(List<BrownInputStroke> strokes) 
        {
            List<BrownShape> brownShapes = new List<BrownShape>();

            foreach (BrownInputStroke stroke in strokes)
            {
                BrownShape brownShape = RecognizeBrownShape(stroke);

                if (brownShape != null)
                {
                    brownShapes.Add(brownShape);

                    if (brownShape.ShapeType == ShapeType.StraightLine || brownShape.ShapeType == ShapeType.Polyline)
                    {
                        List<BrownShape> lineShapes = new List<BrownShape>();
                        foreach (BrownShape bs in brownShapes)
                        {
                            if (bs.ShapeType == ShapeType.StraightLine || bs.ShapeType == ShapeType.Polyline)
                            {
                                lineShapes.Add(bs);
                            }
                        }

                        brownShape = RecognizeBrownShape(lineShapes);

                        if (brownShape != null && (brownShape.ShapeType != ShapeType.None))
                        {
                            brownShapes.Add(brownShape);

                            foreach (BrownShape bs in lineShapes)
                            {
                                brownShapes.Remove(bs);
                            }
                        }
                    }
                }
            }
            List<BrownTemplate> recognizedTemplates = TemplateRecognizer.TemplateRecognizer.RecognizeTemplate(brownShapes);
            return recognizedTemplates;
        }
        
        /// <summary>
        /// Creates a list of clean BrownRecognitionCommon.BrownShape according to the template that got passed in. 
        /// Every returned shape has a list of BrownRecognitionCommon.BrownInputStroke. Their Data property
        /// can be used to backtrack the original stroke object of the application.
        /// </summary>
        /// <param name="brownTemplate">The template that needs to be cleaned up.</param>
        /// <returns>List of clean BrownRecognitionCommon.BrownShape.</returns>
        public static List<BrownShape> GetCleanTemplateShapes(BrownTemplate brownTemplate) 
        {
            return TemplateRecognizer.TemplateRecognizer.CleanUpDiagram(brownTemplate);
        }
       
    }
}
