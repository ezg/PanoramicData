using starPadSDK.Inq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using System.Windows.Media;

namespace PanoramicData.utils.inq
{
    public class InqAnalyzer
    {
        InkAnalyzer _inkAnalyzer = new InkAnalyzer();
        List<KeyValuePair<StroqCollection, char>> _knownCharHints = new List<KeyValuePair<StroqCollection, char>>();

        public event ResultsUpdatedEventHandler ResultsUpdated;

        public InqAnalyzer()
        {
            _inkAnalyzer.ResultsUpdated += new ResultsUpdatedEventHandler(_inkAnalyzer_ResultsUpdated);
        }

        public AnalysisRegion DirtyRegion { get { return _inkAnalyzer.DirtyRegion; } }
        public ContextNode RootNode { get { return _inkAnalyzer.RootNode; } }

        public void AddStroke(Stroq stroq)
        {
            try
            {
                _inkAnalyzer.AddStroke(stroq.BackingStroke);
                _inkAnalyzer.SetStrokeType(stroq.BackingStroke, StrokeType.Writing);
            }
            catch (Exception e)
            {
            }
        }

        public void CreateKnownCharHint(Stroq s, char value)
        {
            this.CreateKnownCharHint(new StroqCollection(new Stroq[] { s }), value);
        }

        public void CreateKnownCharHint(StroqCollection sc, char value)
        {
            /*_knownCharHints.Add(new KeyValuePair<StroqCollection, char>(sc, value));

            foreach (var s in sc)
            {
                _inkAnalyzer.RemoveStroke(s.BackingStroke);
                _inkAnalyzer.AddStrokeToCustomRecognizer(s.BackingStroke, _inkAnalyzer.CreateCustomRecognizer(Guid.NewGuid()));
            }*/
        }

        public void Analyze()
        {
            _inkAnalyzer.Analyze();
        }

        public void BackgroundAnalyze()
        {
            _inkAnalyzer.BackgroundAnalyze();
        }

        public string GetRecognizedString()
        {
            /*string text = "";
            ContextNodeCollection nodes = _inkAnalyzer.FindLeafNodes();
            foreach (var contextNode in nodes)
            {
                if (contextNode is InkWordNode)
                {
                    text += (contextNode as InkWordNode).GetRecognizedString() + " ";
                }
                else if (contextNode is LineNode)
                {
                    text += "\n";
                }
                else if (contextNode is UnclassifiedInkNode)
                {
                    UnclassifiedInkNode uin = (UnclassifiedInkNode) contextNode;
                    foreach (var hint in _knownCharHints)
                    {
                        bool found = false;
                        foreach(var s in hint.Key) 
                        {
                            if (uin.Strokes.Contains(s.BackingStroke))
                            {
                                text += hint.Value;
                                found = true;
                                break;
                            }
                        }
                        if (found)
                        {
                            break;
                        }
                    }
                }
            }*/


            return _inkAnalyzer.GetRecognizedString();
            //return text;// _inkAnalyzer.GetRecognizedString();
        }

        public void RemoveStroke(Stroq stroq)
        {
            _inkAnalyzer.RemoveStroke(stroq.BackingStroke);
        }

        public ContextNodeCollection FindIntersections(out List<Stroq> selectedStroqes, Rect rect, StroqCollection stroqs, Guid type)
        {
            selectedStroqes = new List<Stroq>();
            StrokeCollection selectedStrokes = new StrokeCollection();
            ContextNodeCollection nodes = _inkAnalyzer.FindNodesOfType(type);
            foreach (ContextNode node in nodes)
            {
                RotatedBoundingBoxContexNodeWrapper n = new RotatedBoundingBoxContexNodeWrapper(node);
                if (rect.IntersectsWith(n.getBoundingBox()) || rect.IsEmpty)
                {
                    foreach (Stroke s in n.Strokes)
                    {
                        foreach (Stroq ss in stroqs)
                        {
                            if (ss.BackingStroke == s)
                            {
                                selectedStroqes.Add(ss);
                                selectedStrokes.Add(s);
                            }
                        }
                    }
                }
            }

            ContextNodeCollection lines = _inkAnalyzer.FindNodesOfType(type, selectedStrokes);
            return lines;
        }

        private void _inkAnalyzer_ResultsUpdated(object sender, ResultsUpdatedEventArgs e)
        {
            ResultsUpdated(this, e);
        }
    }

    public class RotatedBoundingBoxContexNodeWrapper
    {
        public ContextNode _contextNode;
        public RotatedBoundingBoxContexNodeWrapper(ContextNode contextNode)
        {
            this._contextNode = contextNode;
        }
        public StrokeCollection Strokes { get { return _contextNode.Strokes; } }
        public PointCollection getPointCollection()
        {
            PointCollection pc = new PointCollection();
            if (_contextNode is LineNode)
            {
                pc = ((LineNode)_contextNode).GetRotatedBoundingBox();
            }
            else if (_contextNode is ParagraphNode)
            {
                pc = ((ParagraphNode)_contextNode).GetRotatedBoundingBox();
            }
            else if (_contextNode is InkWordNode)
            {
                pc = ((InkWordNode)_contextNode).GetRotatedBoundingBox();
            }
            else
            {
                throw new Exception("ContextNode does not have a RotatedBoundingBox.");
            }

            return pc;
        }
        public Rect getBoundingBox()
        {
            PointCollection pc = getPointCollection();
            return new Rect(new Point(pc.Min(p => p.X),
                                      pc.Min(p => p.Y)),
                            new Point(pc.Max(p => p.X),
                                      pc.Max(p => p.Y)));

        }
        public Point getPosition()
        {
            PointCollection pc = getPointCollection();
            return new Point(pc.Min(p => p.X),
                             pc.Min(p => p.Y));
        }
        public double getWidth()
        {
            return this.getBoundingBox().Width;
        }
        public double getHeight()
        {
            return this.getBoundingBox().Height;
        }
    }
}
