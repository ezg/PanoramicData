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

// If debugging is enabled, this form will pop up during 
// loading templates and recognizing candidates, to show 
// the stages of pre-processing and recognition matching.
// Multiple instances will pop up, one for each step.
// DO NOT enable debugging during recognition of a batch!!

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;

namespace Recognizer.NDollar
{
    public partial class DebugForm : Form
    {
        private List<List<PointR>> _debugPts = new List<List<PointR>>();
        private int _index = 0;
        private List<String> _label = new List<String>();
        private bool _displayTogether = false; // Lisa 8/9/2009 (if false, do one at a time; if true, all on same page)
        private Brush[] _brushes = { Brushes.Firebrick, Brushes.DodgerBlue };

        public DebugForm()
        {
            InitializeComponent();
        }

        // used to display points during pre-processing
        public void SendPoints(List<PointR> pts, String label)
        {
            _displayTogether = false;
            List<PointR> newPts = Utils.TranslateCentroidTo(pts, new PointR(200, 200));
            _debugPts.Add(new List<PointR>(newPts));
            _label.Add(label);
        }

        // used to display template and candidate being matched
        public void SendPoints(List<PointR> candidate, List<PointR> template, String label)
        {
            _displayTogether = true;
            displayButton.Enabled = false;
            List<PointR> cpts = Utils.TranslateCentroidTo(candidate, new PointR(200, 200));
            _debugPts.Add(new List<PointR>(cpts));
            List<PointR> tpts = Utils.TranslateCentroidTo(template, new PointR(200, 200));
            _debugPts.Add(new List<PointR>(tpts));
            _label.Add(label);
        }

        private void OnPaintPage (object sender, PaintEventArgs e) //ArrayList points)
        {
            if (_debugPts.Count > 0)
            {
                if (_displayTogether)
                {
                    for (int i = 0; i < _debugPts.Count; i++)
                    {
//                        PointF p0 = (PointF)(PointR)((ArrayList)(_debugPts[i]))[0];
                        PointF p0 = (PointF)_debugPts[i][0];
                        e.Graphics.FillEllipse(_brushes[i], p0.X - 5f, p0.Y - 5f, 10f, 10f);

                        foreach (PointR r in _debugPts[i])
                        {
                            PointF p = (PointF)r; // cast
                            e.Graphics.FillEllipse(_brushes[i], p.X - 2f, p.Y - 2f, 4f, 4f);
                        }
                    }
                    debugLabel.Text = (String)_label[0];
                }
                else
                {
//                    PointF p0 = (PointF)(PointR)((ArrayList)(_debugPts[_index]))[0];
                    PointF p0 = (PointF)_debugPts[_index][0];
                    e.Graphics.FillEllipse(Brushes.Firebrick, p0.X - 5f, p0.Y - 5f, 10f, 10f);

                    foreach (PointR r in _debugPts[_index])
                    {
                        PointF p = (PointF)r; // cast
                        e.Graphics.FillEllipse(Brushes.Firebrick, p.X - 2f, p.Y - 2f, 4f, 4f);
                    }
                    debugLabel.Text = _label[_index];
                }
            }
        }

        private void DisplayButton_Click(object sender, EventArgs e)
        {
            if (_index + 1 < _debugPts.Count)
                _index++;
            else displayButton.Enabled = false;
            Invalidate();
        }

    }
}
