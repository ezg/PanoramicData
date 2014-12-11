using TemplateRecognizer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using starPadSDK.Inq;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using ShapeRecognizer;
using System.Windows;
using BrownRecognitionCommon;
using starPadSDK.Geom;
using System.Linq;
using BrownRecognitionAPI;

namespace BrownRecognitionAPITestSuite
{
    
    // How to setup tests:
    // Go to Test -> Edit Test Settings -> Local
    //  - Go to Deployment Tab and add "BrownRecognitionAPITestSuite\stroqs" directory.
    //  - Go to Data and Diagnostic Tab
    //    - Enable Code Coverage
    //    - Go to Configure Code Coverage
    //      - add BrownRecognitionAPI.dll
    //      - add BrownRecognitionCommon.dll
    //      - add ShapeRecognizer.dll
    //      - add TemplateRecognizer.dll

    /// <summary>
    ///This is a test class for BrownRecognitionAPI and is intended
    ///to contain all BrownRecognitionAPI Unit Tests
    ///</summary>
    [TestClass()]
    public class BrownRecognitionAPITest
    {
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        private StroqCollection loadStroqsFromFile(String fileName)
        {
            Stream stream = File.Open(fileName, FileMode.Open);
            BinaryFormatter bFormatter = new BinaryFormatter();
            StroqCollection sc = (StroqCollection)bFormatter.Deserialize(stream);
            stream.Close();
            return sc;
        }

        /// <summary>
        ///A test for save and load BrownRecognitionSettings
        ///</summary>
        [TestMethod()]
        public void SaveAndLoadBrownRecognitionSettings()
        {
            BrownRecognitionSettings.Instance.OrgChartBlockConnectorDistanceThreshold = 25;
            BrownRecognitionSettings.Instance.OrgChartWidthPolicy = OrgChartWidthPolicies.AverageOnChart;
            BrownRecognitionSettings.Instance.OrgChartHeightPolicy = OrgChartHeightPolicies.AverageOnChart;
            BrownRecognitionSettings.Instance.OrgChartHorizontalSpacingPolicy = OrgChartHorizontalSpacingPolicies.StickToConstant;
            BrownRecognitionSettings.Instance.OrgChartVerticalSpacingPolicy = OrgChartVerticalSpacingPolicies.StickToConstant;
            BrownRecognitionSettings.Instance.OrgChartConstantHorizontalSpacing = 30;
            BrownRecognitionSettings.Instance.OrgChartConstantVerticalSpacing = 30;
            
            BrownRecognitionSettings.Instance.PieChartMinimumLengthOfLinesInPercentOfCircleRadius = 0.2;
            
            BrownRecognitionSettings.Instance.TableDiagramContainsThreshold = 25;
            BrownRecognitionSettings.Instance.TableDiagramAlignmentThreshold = 10;
            BrownRecognitionSettings.Instance.TableDiagramVerticalLineToleranceInPercentOfLength = 35;
            BrownRecognitionSettings.Instance.TableDiagramHorizontalLineToleranceInPercentOfLength = 35;

            string initial = API.GetBrownRecognitionSettings();

            BrownRecognitionSettings.Instance.OrgChartBlockConnectorDistanceThreshold = 1000;
            BrownRecognitionSettings.Instance.OrgChartWidthPolicy = OrgChartWidthPolicies.KeepOriginal;
            BrownRecognitionSettings.Instance.OrgChartHeightPolicy = OrgChartHeightPolicies.KeepOriginal;
            BrownRecognitionSettings.Instance.OrgChartHorizontalSpacingPolicy = OrgChartHorizontalSpacingPolicies.GreatesOnSameLevel;
            BrownRecognitionSettings.Instance.OrgChartVerticalSpacingPolicy = OrgChartVerticalSpacingPolicies.GreatesOnSameLevel;
            BrownRecognitionSettings.Instance.OrgChartConstantHorizontalSpacing = 1000;
            BrownRecognitionSettings.Instance.OrgChartConstantVerticalSpacing = 1000;

            BrownRecognitionSettings.Instance.PieChartMinimumLengthOfLinesInPercentOfCircleRadius = 1.0;

            BrownRecognitionSettings.Instance.TableDiagramContainsThreshold = 1000;
            BrownRecognitionSettings.Instance.TableDiagramAlignmentThreshold = 1000;
            BrownRecognitionSettings.Instance.TableDiagramVerticalLineToleranceInPercentOfLength = 1000;
            BrownRecognitionSettings.Instance.TableDiagramHorizontalLineToleranceInPercentOfLength = 1000;

            API.SetBrownRecognitionSettings(initial);

            Assert.IsTrue(BrownRecognitionSettings.Instance.OrgChartBlockConnectorDistanceThreshold == 25);
            Assert.IsTrue(BrownRecognitionSettings.Instance.OrgChartWidthPolicy == OrgChartWidthPolicies.AverageOnChart);
            Assert.IsTrue(BrownRecognitionSettings.Instance.OrgChartHeightPolicy == OrgChartHeightPolicies.AverageOnChart);
            Assert.IsTrue(BrownRecognitionSettings.Instance.OrgChartHorizontalSpacingPolicy == OrgChartHorizontalSpacingPolicies.StickToConstant);
            Assert.IsTrue(BrownRecognitionSettings.Instance.OrgChartVerticalSpacingPolicy == OrgChartVerticalSpacingPolicies.StickToConstant);
            Assert.IsTrue(BrownRecognitionSettings.Instance.OrgChartConstantHorizontalSpacing == 30);
            Assert.IsTrue(BrownRecognitionSettings.Instance.OrgChartConstantVerticalSpacing == 30);

            Assert.IsTrue(BrownRecognitionSettings.Instance.PieChartMinimumLengthOfLinesInPercentOfCircleRadius == 0.2);

            Assert.IsTrue(BrownRecognitionSettings.Instance.TableDiagramContainsThreshold == 25);
            Assert.IsTrue(BrownRecognitionSettings.Instance.TableDiagramAlignmentThreshold == 10);
            Assert.IsTrue(BrownRecognitionSettings.Instance.TableDiagramVerticalLineToleranceInPercentOfLength == 35);
            Assert.IsTrue(BrownRecognitionSettings.Instance.TableDiagramHorizontalLineToleranceInPercentOfLength == 35);
        }

        /// <summary>
        ///A test for RecognizeBrownShape
        ///</summary>
        [TestMethod()]
        public void RecognizeSingleStroqShapes()
        {
            String[] files = Directory.GetFiles("singleStroqShapes");
            foreach (string fileName in files)
            {
                StroqCollection stroqs = loadStroqsFromFile(fileName);
                Stroq s = stroqs.First();
                BrownInputStroke bis = new BrownInputStroke(s.Select((pt) => new Point(pt.X, pt.Y)).ToArray(), s);
                BrownShape bs = API.RecognizeBrownShape(bis);

                FileInfo fileInfo = new FileInfo(fileName);
                string type = fileInfo.Name.Substring(0, fileInfo.Name.Length - (fileInfo.Extension.Length)).ToUpper();

                Console.WriteLine("Test " + fileName + ": " + type + " == " + bs.ShapeType.ToString().ToUpper());
                Assert.IsTrue(type.Equals(bs.ShapeType.ToString().ToUpper()), "Test case: '" + fileName + "' failed.");
                
            }
        }

        /// <summary>
        ///A test for RecognizeBrownShape
        ///</summary>
        [TestMethod()]
        public void RecognizeMultiStroqShapes()
        {
            String[] files = Directory.GetFiles("multiStroqShapes");
            foreach (string fileName in files)
            {
                List<BrownShape> brownShapes = new List<BrownShape>();
                StroqCollection stroqs = loadStroqsFromFile(fileName);
                foreach (Stroq s in stroqs)
                {
                    BrownInputStroke bis = new BrownInputStroke(s.Select((pt) => new Point(pt.X, pt.Y)).ToArray(), s);
                    brownShapes.Add(API.RecognizeBrownShape(bis));
                }

                BrownShape recognizedShape = API.RecognizeBrownShape(brownShapes);
                
                FileInfo fileInfo = new FileInfo(fileName);
                string type = fileInfo.Name.Substring(0, fileInfo.Name.Length - (fileInfo.Extension.Length + 3)).ToUpper();

                Console.WriteLine("Test " + fileName + ": " + type + " == " + recognizedShape.ShapeType.ToString().ToUpper());
                Assert.IsTrue(type.Equals(recognizedShape.ShapeType.ToString().ToUpper()), "Test case: '" + fileName + "' failed.");
            }
        }

        /// <summary>
        ///Furukawa-San Test Case 1
        ///</summary>
        [TestMethod()]
        public void FurukawaSanTest1()
        {
            // Button 1
            BrownInputStroke bis = new BrownInputStroke(
                new Point[] {
                    new Point(0, 0),
                    new Point(0, 100),
                    new Point(100, 100),
                    new Point(0, 0),
                    }, null);

            BrownShape bs = API.RecognizeBrownShape(bis);


            // Button 2
            List<BrownShape> shapes = new List<BrownShape>();
            bis = new BrownInputStroke(
                new Point[] {
                    new Point(0, 0),
                    new Point(0, 100),
                    }, null);
            bs = API.RecognizeBrownShape(bis);
            shapes.Add(bs);

            bis = new BrownInputStroke(
                new Point[] {
                    new Point(0, 100),
                    new Point(100, 100),
                    }, null);
            bs = API.RecognizeBrownShape(bis);
            shapes.Add(bs);

            bis = new BrownInputStroke(
                new Point[] {
                    new Point(100, 100),
                    new Point(0, 0),
                    }, null);
            bs = API.RecognizeBrownShape(bis);
            shapes.Add(bs);

            bs = API.RecognizeBrownShape(shapes);


            // Button 3
            shapes = new List<BrownShape>();
            bis = new BrownInputStroke(
                new Point[] {
                    new Point(0, 0),
                    new Point(0, 100),
                    }, null);
            bs = API.RecognizeBrownShape(bis);
            shapes.Add(bs);

            bis = new BrownInputStroke(
                new Point[] {
                    new Point(0, 100),
                    new Point(50, 100),
                    new Point(100, 100),
                    new Point(50, 50),
                    new Point(0, 0),
                    }, null);
            bs = API.RecognizeBrownShape(bis);
            shapes.Add(bs);
            
            bs = API.RecognizeBrownShape(shapes);
        }

        /// <summary>
        ///Furukawa-San Test Case 1
        ///</summary>
        [TestMethod()]
        public void FurukawaSanTest2()
        {
            StreamReader streamReader = new StreamReader(@"C:\temp\furukawa-san test\line_NG.txt");
            string text = streamReader.ReadToEnd();
            streamReader.Close();

            List<BrownShape> shapes = new List<BrownShape>();
            string[] lines = text.Split('\n');
            foreach (var line in lines)
            {
                string[] coords = line.Split(',');
                Point[] pts = new Point[] {
                                            new Point(float.Parse(coords[0]), float.Parse(coords[1])),
                                            new Point(float.Parse(coords[2]), float.Parse(coords[3]))
                                          };
                BrownInputStroke bis = new BrownInputStroke(pts, null);
                BrownShape bs = new BrownShape(ShapeType.StraightLine, pts, bis);
                shapes.Add(bs);
            }

            BrownShape output = API.RecognizeBrownShape(shapes);
        }


        /// <summary>
        ///A test for PieChart Template
        ///</summary>
        [TestMethod()]
        public void RecognizePieChart1()
        {
            StroqCollection stroqs = loadStroqsFromFile("pieChart//1.stroqs");
            List<BrownInputStroke> strokes = new List<BrownInputStroke>();
            foreach (Stroq s in stroqs)
            {
                strokes.Add(new BrownInputStroke(s.Select((pt) => new Point(pt.X, pt.Y)).ToArray(), s));
            }

            List<BrownTemplate> brownTemplates = API.RecognizeBrownTemplate(strokes);
            Assert.IsTrue(brownTemplates.Count == 1, "Test case: 'RecognizePieChart1' failed.");
            Assert.IsTrue(brownTemplates[0].TemplateType == TemplateType.PieChart, "Test case: 'RecognizePieChart1' failed.");
        }
    }
}
