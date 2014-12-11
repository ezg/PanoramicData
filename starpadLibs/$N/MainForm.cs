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
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using System.IO;
using System.Collections.Generic;

namespace Recognizer.NDollar.Geometric
{
	public class MainForm : System.Windows.Forms.Form
	{
		#region Fields

        private GeometricRecognizer	_rec;
		private bool				_recording;
		private bool				_isDown;
		private List<PointR>    	_points;
        private List<List<PointR>>  _strokes; // Lisa 8/8/2009 
        private ViewForm            _viewFrm;
        private bool                _similar;
        private bool                _recentlyRecognized = false; // Lisa 12/22/2007
        private List<int>           _numPtsInStroke; // Lisa 1/2/2008
        
		#endregion

		#region Form Elements

		private System.Windows.Forms.Label lblRecord;
		private System.Windows.Forms.MainMenu MainMenu;
        private System.Windows.Forms.MenuItem Exit;
		private System.Windows.Forms.MenuItem LoadGesture;
		private System.Windows.Forms.MenuItem ViewGesture;
		private System.Windows.Forms.MenuItem RecordGesture;
		private System.Windows.Forms.MenuItem GestureMenu;
        private System.Windows.Forms.MenuItem ClearGestures;
		private System.Windows.Forms.Label lblResult;
		private System.Windows.Forms.MenuItem HelpMenu;
        private System.Windows.Forms.MenuItem About;
        private Label lblRecognizing;
        private MenuItem FileMenu;
        private MenuItem TestBatch;
        private MenuItem Separator0;
        private ProgressBar prgTesting;
        private MenuItem RotationGraph;
        private MenuItem RotateSimilar;
        private MenuItem GraphMenu;
        private Button recognizeButton;
        private Button clearCanvas;
        private Button recordButton;
        private IContainer components;

		#endregion

        #region Main Form for GUI

        public MainForm()
		{
			SetStyle(ControlStyles.DoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
			InitializeComponent();
			_rec = new GeometricRecognizer();
            _rec.ProgressChangedEvent += new ProgressEventHandler(OnProgressChanged);
			_points = new List<PointR>(256);
            _strokes = new List<List<PointR>>(5); // Lisa 8/8/2009; we don't expect more than 5 strokes
            _numPtsInStroke = new List<int>(); // Lisa 1/2/2008
            _viewFrm = null;
            _similar = true;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose(disposing);
        }

        #endregion

        #region Windows Form Designer generated code
        /// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            this.lblRecord = new System.Windows.Forms.Label();
            this.MainMenu = new System.Windows.Forms.MainMenu(this.components);
            this.FileMenu = new System.Windows.Forms.MenuItem();
            this.Exit = new System.Windows.Forms.MenuItem();
            this.GestureMenu = new System.Windows.Forms.MenuItem();
            this.RecordGesture = new System.Windows.Forms.MenuItem();
            this.LoadGesture = new System.Windows.Forms.MenuItem();
            this.ViewGesture = new System.Windows.Forms.MenuItem();
            this.ClearGestures = new System.Windows.Forms.MenuItem();
            this.Separator0 = new System.Windows.Forms.MenuItem();
            this.TestBatch = new System.Windows.Forms.MenuItem();
            this.GraphMenu = new System.Windows.Forms.MenuItem();
            this.RotationGraph = new System.Windows.Forms.MenuItem();
            this.RotateSimilar = new System.Windows.Forms.MenuItem();
            this.HelpMenu = new System.Windows.Forms.MenuItem();
            this.About = new System.Windows.Forms.MenuItem();
            this.lblResult = new System.Windows.Forms.Label();
            this.lblRecognizing = new System.Windows.Forms.Label();
            this.prgTesting = new System.Windows.Forms.ProgressBar();
            this.recognizeButton = new System.Windows.Forms.Button();
            this.clearCanvas = new System.Windows.Forms.Button();
            this.recordButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblRecord
            // 
            this.lblRecord.BackColor = System.Drawing.Color.Transparent;
            this.lblRecord.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecord.Font = new System.Drawing.Font("Courier New", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblRecord.ForeColor = System.Drawing.Color.Firebrick;
            this.lblRecord.Location = new System.Drawing.Point(0, 0);
            this.lblRecord.Name = "lblRecord";
            this.lblRecord.Size = new System.Drawing.Size(352, 24);
            this.lblRecord.TabIndex = 2;
            this.lblRecord.Text = "[Recording]";
            this.lblRecord.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.lblRecord.Visible = false;
            // 
            // MainMenu
            // 
            this.MainMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.FileMenu,
            this.GestureMenu,
            this.GraphMenu,
            this.HelpMenu});
            // 
            // FileMenu
            // 
            this.FileMenu.Index = 0;
            this.FileMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.Exit});
            this.FileMenu.Text = "&File";
            // 
            // Exit
            // 
            this.Exit.Index = 0;
            this.Exit.Text = "E&xit";
            this.Exit.Click += new System.EventHandler(this.Exit_Click);
            // 
            // GestureMenu
            // 
            this.GestureMenu.Index = 1;
            this.GestureMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.RecordGesture,
            this.LoadGesture,
            this.ViewGesture,
            this.ClearGestures,
            this.Separator0,
            this.TestBatch});
            this.GestureMenu.Text = "&Gestures";
            this.GestureMenu.Popup += new System.EventHandler(this.GestureMenu_Popup);
            // 
            // RecordGesture
            // 
            this.RecordGesture.Index = 0;
            this.RecordGesture.Shortcut = System.Windows.Forms.Shortcut.CtrlR;
            this.RecordGesture.Text = "&Record";
            this.RecordGesture.Click += new System.EventHandler(this.RecordGesture_Click);
            // 
            // LoadGesture
            // 
            this.LoadGesture.Index = 1;
            this.LoadGesture.Shortcut = System.Windows.Forms.Shortcut.CtrlO;
            this.LoadGesture.Text = "&Load...";
            this.LoadGesture.Click += new System.EventHandler(this.LoadGesture_Click);
            // 
            // ViewGesture
            // 
            this.ViewGesture.Index = 2;
            this.ViewGesture.Shortcut = System.Windows.Forms.Shortcut.CtrlV;
            this.ViewGesture.Text = "&View";
            this.ViewGesture.Click += new System.EventHandler(this.ViewGesture_Click);
            // 
            // ClearGestures
            // 
            this.ClearGestures.Index = 3;
            this.ClearGestures.Text = "&Clear";
            this.ClearGestures.Click += new System.EventHandler(this.ClearGestures_Click);
            // 
            // Separator0
            // 
            this.Separator0.Index = 4;
            this.Separator0.Text = "-";
            // 
            // TestBatch
            // 
            this.TestBatch.Index = 5;
            this.TestBatch.Shortcut = System.Windows.Forms.Shortcut.CtrlT;
            this.TestBatch.Text = "&Test Batch...";
            this.TestBatch.Click += new System.EventHandler(this.TestBatch_Click);
            // 
            // GraphMenu
            // 
            this.GraphMenu.Index = 2;
            this.GraphMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.RotationGraph,
            this.RotateSimilar});
            this.GraphMenu.Text = "G&raph";
            this.GraphMenu.Popup += new System.EventHandler(this.GraphMenu_Popup);
            // 
            // RotationGraph
            // 
            this.RotationGraph.Index = 0;
            this.RotationGraph.Shortcut = System.Windows.Forms.Shortcut.CtrlG;
            this.RotationGraph.Text = "Rotation &Graph...";
            this.RotationGraph.Click += new System.EventHandler(this.RotationGraph_Click);
            // 
            // RotateSimilar
            // 
            this.RotateSimilar.Checked = true;
            this.RotateSimilar.Index = 1;
            this.RotateSimilar.Text = "&Pair Similar";
            this.RotateSimilar.Click += new System.EventHandler(this.RotateSimilar_Click);
            // 
            // HelpMenu
            // 
            this.HelpMenu.Index = 3;
            this.HelpMenu.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
            this.About});
            this.HelpMenu.Text = "&Help";
            // 
            // About
            // 
            this.About.Index = 0;
            this.About.Text = "&About...";
            this.About.Click += new System.EventHandler(this.About_Click);
            // 
            // lblResult
            // 
            this.lblResult.AutoSize = true;
            this.lblResult.Dock = System.Windows.Forms.DockStyle.Right;
            this.lblResult.Location = new System.Drawing.Point(324, 24);
            this.lblResult.Name = "lblResult";
            this.lblResult.Size = new System.Drawing.Size(28, 13);
            this.lblResult.TabIndex = 1;
            this.lblResult.Text = "Test";
            this.lblResult.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblRecognizing
            // 
            this.lblRecognizing.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblRecognizing.ForeColor = System.Drawing.Color.Firebrick;
            this.lblRecognizing.Location = new System.Drawing.Point(0, 24);
            this.lblRecognizing.Name = "lblRecognizing";
            this.lblRecognizing.Size = new System.Drawing.Size(324, 23);
            this.lblRecognizing.TabIndex = 0;
            this.lblRecognizing.Text = "Recognizing...";
            this.lblRecognizing.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.lblRecognizing.Visible = false;
            // 
            // prgTesting
            // 
            this.prgTesting.Location = new System.Drawing.Point(0, 141);
            this.prgTesting.Name = "prgTesting";
            this.prgTesting.Size = new System.Drawing.Size(352, 23);
            this.prgTesting.TabIndex = 3;
            this.prgTesting.Visible = false;
            // 
            // recognizeButton
            // 
            this.recognizeButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.recognizeButton.Location = new System.Drawing.Point(132, 283);
            this.recognizeButton.Name = "recognizeButton";
            this.recognizeButton.Size = new System.Drawing.Size(88, 23);
            this.recognizeButton.TabIndex = 4;
            this.recognizeButton.Text = "Recognize";
            this.recognizeButton.UseVisualStyleBackColor = true;
            this.recognizeButton.Click += new System.EventHandler(this.Recognize_Click);
            // 
            // clearCanvas
            // 
            this.clearCanvas.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.clearCanvas.Location = new System.Drawing.Point(221, 283);
            this.clearCanvas.Name = "clearCanvas";
            this.clearCanvas.Size = new System.Drawing.Size(88, 23);
            this.clearCanvas.TabIndex = 5;
            this.clearCanvas.Text = "Clear Canvas";
            this.clearCanvas.UseVisualStyleBackColor = true;
            this.clearCanvas.Click += new System.EventHandler(this.ClearCanvas_Click);
            // 
            // recordButton
            // 
            this.recordButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.recordButton.Location = new System.Drawing.Point(43, 283);
            this.recordButton.Name = "recordButton";
            this.recordButton.Size = new System.Drawing.Size(88, 23);
            this.recordButton.TabIndex = 6;
            this.recordButton.Text = "Save Gesture";
            this.recordButton.UseVisualStyleBackColor = true;
            this.recordButton.Click += new System.EventHandler(this.RecordButton_Click);
            // 
            // MainForm
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.BackColor = System.Drawing.SystemColors.Window;
            this.ClientSize = new System.Drawing.Size(352, 307);
            this.Controls.Add(this.recordButton);
            this.Controls.Add(this.recognizeButton);
            this.Controls.Add(this.clearCanvas);
            this.Controls.Add(this.prgTesting);
            this.Controls.Add(this.lblRecognizing);
            this.Controls.Add(this.lblResult);
            this.Controls.Add(this.lblRecord);
            this.Menu = this.MainMenu;
            this.Name = "MainForm";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "$N Recognizer";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.MainForm_Paint);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MainForm_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.MainForm_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.MainForm_MouseUp);
            this.ResumeLayout(false);
            this.PerformLayout();

		}
		#endregion

        #region File Menu

        private void Exit_Click(object sender, System.EventArgs e)
        {
            Close();
        }

        #endregion

        #region Gestures Menu

        private void GestureMenu_Popup(object sender, System.EventArgs e)
        {
            RecordGesture.Checked = _recording;
            ViewGesture.Checked = (_viewFrm != null && !_viewFrm.IsDisposed);
            ClearGestures.Enabled = (_rec.NumGestures > 0);
        }

        private void LoadGesture_Click(object sender, System.EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Gestures (*.xml)|*.xml";
            dlg.Title = "Load Gestures";
            dlg.Multiselect = true;
            dlg.RestoreDirectory = true; // Lisa 8/16/2009; have to un-allow this because of accessing our parameters config file

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                Cursor.Current = Cursors.WaitCursor;
                for (int i = 0; i < dlg.FileNames.Length; i++)
                {
                   string name = dlg.FileNames[i];
                    _rec.LoadGesture(name);
                }
                ReloadViewForm();
                Cursor.Current = Cursors.Default;
            }
        }

        private void ViewGesture_Click(object sender, System.EventArgs e)
        {
            if (_viewFrm != null && !_viewFrm.IsDisposed)
            {
                _viewFrm.Close();
                _viewFrm = null;
            }
            else
            {
                Cursor.Current = Cursors.WaitCursor;
                _viewFrm = new ViewForm(_rec.Gestures);
                _viewFrm.Owner = this;
                _viewFrm.Show();
                Cursor.Current = Cursors.Default;
            }
        }

        // helper fn
        private void ReloadViewForm()
        {
            if (_viewFrm != null && !_viewFrm.IsDisposed)
            {
                _viewFrm.Close();
                _viewFrm = new ViewForm(_rec.Gestures);
                _viewFrm.Owner = this;
                _viewFrm.Show();
            }
        }

        private void RecordGesture_Click(object sender, System.EventArgs e)
        {
            _points.Clear();
            _numPtsInStroke.Clear(); // Lisa 1/2/2008
            recognizeButton.Enabled = !recognizeButton.Enabled; // disable recognition if we're saving the gesture
            Invalidate();
            _recording = !_recording; // recording will happen on mouse-up -- changed to button activated, Lisa 1/2/2008
            lblRecord.Visible = _recording;
        }

        private void ClearGestures_Click(object sender, System.EventArgs e)
        {
            if (MessageBox.Show(this, "This will clear all loaded gestures. (It will not delete any XML files.)", "Confirm", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
            {
                _rec.ClearGestures();
                ReloadViewForm();
            }
        }

        /// <summary>
        /// This menu command allows the user to multi-select a handful of
        /// gesture XML files from a directory, and to produce an output
        /// file containing the recognition results for everything in the
        /// directory.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>
        /// The gestures loaded must conform to a naming convention
        /// where each example of a particular gesture is named with
        /// the same string, followed by a numerical identifier for
        /// that example. As in:
        ///
        ///     circle01.xml        // circle gestures
        ///     circle02.xml
        ///     circle03.xml
        ///     square01.xml        // square gestures
        ///     square02.xml
        ///     square03.xml
        ///     triangle01.xml      // triangle gestures
        ///     triangle02.xml
        ///     triangle03.xml
        /// 
        /// This naming convention is not followed in the multistroke algebra 
        /// gestures dataset.  It uses names such as:
        /// 
        ///     minus_1900_13_0
        ///     minus_1900_13_1
        ///     horizontal-line_42_45_0
        /// 
        /// The name parsing has been updated to allow both types.
        /// (Lisa 1/5/2008)
        /// 
        /// The same number of examples should be read in for each gesture
        /// category. The testing procedure will load a random subset of
        /// each gesture and test on the remaining gestures.
        /// 
        /// <b>Warning.</b> This process will throw an exception if the number
        /// of gesture examples for each gesture is unbalanced.
        /// 
        /// This constraint has been relaxed -- no longer needed for $N.
        /// (Lisa 1/5/2008)
        /// 
        /// </remarks>
        private void TestBatch_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Gestures (*.xml)|*.xml";
            dlg.Title = "Load Gesture Batch";
            dlg.Multiselect = true;
            dlg.RestoreDirectory = true; // Lisa 8/16/2009; have to un-allow this because of accessing our parameters config file

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                InfoForm ifrm = new InfoForm();
                if (ifrm.ShowDialog(this) == DialogResult.OK)
                {
                    prgTesting.Visible = true;
                    lblRecognizing.Visible = true;
                    Application.DoEvents();

                    // each slot in the ArrayList contains a gesture Category, which 
                    // contains an ArrayList of gesture prototypes.
                    
                    // this now gets all files located in a particular directory because
                    // there is a limit to the number of files that can be opened at once;
                    // you can still explicitly select a subset to limit the test to, the
                    // automatic getting of all files only happens if you just pick one.
                    // Lisa 1/5/2008
                    SamplesCollection categoriesByUser;
                    if (dlg.FileNames.Length > 1)
                    {
                        Console.WriteLine("Number of files: " + dlg.FileNames.Length);
                        categoriesByUser = _rec.AssembleBatch(dlg.FileNames, ifrm.OneD, ifrm.TwoD);
                    }
                    else
                    {
                        // get all files in the same dir as the selected one
                        String selectedFilename = dlg.FileNames[0];
                        DirectoryInfo dir = new DirectoryInfo(selectedFilename.Substring(0, selectedFilename.LastIndexOf('\\')));
                        FileInfo[] allXMLFiles = dir.GetFiles("*.xml");
                        string[] allXMLFilenames = new string[allXMLFiles.Length];

                        int count = 0;
                        Console.Write("Counting Gesture files");
                        foreach (FileInfo fi in allXMLFiles)
                        {
                            allXMLFilenames[count] = dir.FullName + "/" + fi.Name;
                            count++;
                            Console.Write(".");
                        }
                        Console.WriteLine();
                        Console.WriteLine("Number of files: " + count);

                        categoriesByUser = _rec.AssembleBatch(allXMLFilenames, ifrm.OneD, ifrm.TwoD);
                    }

                    if (categoriesByUser != null)
                    {
                        if (_rec.TestBatch(categoriesByUser, dlg.FileName.Substring(0, dlg.FileName.LastIndexOf('\\'))))
                        {
                            MessageBox.Show(this, "Testing complete.", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show(this, "There was an error writing the output file during testing.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else // error assembling batch
                    {
                        MessageBox.Show(this, "Unreadable files, or unbalanced number of gestures in categories.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    lblRecognizing.Visible = false;
                    prgTesting.Visible = false;
                } // end of InfoFrm check
            }
        }

        // update the progress bar upon receiving this event (callback)
        public void OnProgressChanged(object source, ProgressEventArgs e)
        {
            prgTesting.Value = (int) (e.Percent * 100d);
            Application.DoEvents();
        }

        #endregion

        #region Graph Menu

        private void GraphMenu_Popup(object sender, EventArgs e)
        {
            RotateSimilar.Checked = _similar;
        }

        private void RotationGraph_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Gestures (*.xml)|*.xml";
            dlg.Title = "Load Gesture Pairs";
            dlg.Multiselect = true;
            dlg.RestoreDirectory = true; // Lisa 8/16/2009; have to un-allow this because of accessing our parameters config file

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                if (dlg.FileNames.Length % 2 != 0)
                {
                    MessageBox.Show(this, "Pairs of two gestures must be selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    FolderBrowserDialog fld = new FolderBrowserDialog();
                    fld.Description = "Select a folder where the results file will be written.";
                    fld.SelectedPath = dlg.FileName.Substring(0, dlg.FileName.LastIndexOf('\\'));
                    if (fld.ShowDialog() == DialogResult.OK)
                    {
                        string[] filenames = new string[dlg.FileNames.Length];
                        Array.Copy(dlg.FileNames, filenames, dlg.FileNames.Length);

                        Array.Sort(filenames); // sorts alphabetically
                        if (!_similar) // doing rotation graphs for dissimilar gestures
                        {
                            bool dissimilar = false;
                            while (!dissimilar)
                            {
                                for (int j = 0; j < filenames.Length * 2; j++) // random shuffle
                                {
                                    int pos1 = Utils.Random(0, filenames.Length - 1);
                                    int pos2 = Utils.Random(0, filenames.Length - 1);
                                    string tmp = filenames[pos1];
                                    filenames[pos1] = filenames[pos2];
                                    filenames[pos2] = tmp;
                                }
                                for (int j = 0; j < filenames.Length; j += 2) // ensure no pairs are same category
                                {
                                    string cat1 = Category.ParseName(Gesture.ParseName(filenames[j + 1]));
                                    string cat2 = Category.ParseName(Gesture.ParseName(filenames[j]));
                                    dissimilar = (cat1 != cat2); // set the flag
                                    if (!dissimilar)
                                        break;
                                }
                            }
                        }

                        // now do the rotating and declare victory
                        bool failed = false;
                        for (int i = 0; i < filenames.Length; i += 2)
                        {
                            if (!_rec.CreateRotationGraph(filenames[i + 1], filenames[i], fld.SelectedPath, _similar))
                            {
                                MessageBox.Show(this, "There was an error reading or writing files.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                failed = true;
                            }
                        }
                        if (!failed)
                        {
                            MessageBox.Show(this, "Finished rotations of gesture pair(s).", "Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
        }

        private void RotateSimilar_Click(object sender, EventArgs e)
        {
            _similar = !_similar;
        }

        #endregion

        #region About Menu

        private void About_Click(object sender, System.EventArgs e)
        {
            AboutForm frm = new AboutForm();
            frm.ShowDialog(this);
        }

        #endregion

        #region Window Form Events

        private void MainForm_Paint(object sender, System.Windows.Forms.PaintEventArgs e)
        {
            foreach (List<PointR> pts in _strokes) // Lisa 8/8/2009; drawing strokes instead of points
            {
                if (pts.Count > 0)
                {
                    PointF p0 = (PointF) (PointR) pts[0]; // draw the first point of each stroke bigger
                    e.Graphics.FillEllipse(_recording ? Brushes.Firebrick : Brushes.DarkBlue, p0.X - 5f, p0.Y - 5f, 10f, 10f);
                }
                foreach (PointR r in pts)
                {
                    PointF p = (PointF) r; // cast
                    e.Graphics.FillEllipse(_recording ? Brushes.Firebrick : Brushes.DarkBlue, p.X - 2f, p.Y - 2f, 4f, 4f);
                }
            }
            for (int i = 0; i < _points.Count; i++) // Jake 3/19/2011; still need to draw points in current stroke
            {
                if (i == 0)
                {
                    PointF p0 = (PointF) _points[0]; // draw the first point of each stroke bigger
                    e.Graphics.FillEllipse(_recording ? Brushes.Firebrick : Brushes.DarkBlue, p0.X - 5f, p0.Y - 5f, 10f, 10f);
                }
                else
                {
                    PointF p = (PointF) _points[i]; // cast
                    e.Graphics.FillEllipse(_recording ? Brushes.Firebrick : Brushes.DarkBlue, p.X - 2f, p.Y - 2f, 4f, 4f);
                }
            }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            lblResult.Text = String.Empty;
        }

        #endregion

        #region Mouse Events

        // We are allowing multistroke gestures now, so don't clear points just because the pen
        // lifts or makes contact.
        // Lisa 12/22/2007
        private void MainForm_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
		{
            _isDown = true;
            // always clear points @ every new stroke, Lisa 8/8/2009
            _points.Clear();
            // only clear strokes if we clicked recognize, Lisa 8/8/2009
            if (_recentlyRecognized)
            {
                _strokes.Clear(); // Lisa 8/8/2009
                _numPtsInStroke.Clear(); // Lisa 1/2/2008
                _recentlyRecognized = false;
            }
            _points.Add(new PointR(e.X, e.Y, Environment.TickCount));
            Invalidate();
		}

		private void MainForm_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (_isDown)
			{
				_points.Add(new PointR(e.X, e.Y, Environment.TickCount));
				Invalidate(new Rectangle(e.X - 2, e.Y - 2, 4, 4));
			}
		}

        // We are allowing multistroke gestures now, so don't clear points just because the pen
        // lifts or makes contact.
        // Lisa 12/22/2007
        private void MainForm_MouseUp(object sender, System.Windows.Forms.MouseEventArgs e)
		{
			if (_isDown)
			{
				_isDown = false;

                // moved the recognize handling code to the Recognize_Click() method
                // Lisa 12/22/2007
                // but when we pick the mouse up we want to store the stroke boundaries
                // Lisa 1/2/2008
                // still need this for recording from the canvas, Lisa 8/8/2009
                if (_numPtsInStroke.Count == 0)
                    _numPtsInStroke.Add(_points.Count);
                else _numPtsInStroke.Add(_points.Count - (int)(_numPtsInStroke[_numPtsInStroke.Count - 1]));

                // revised to save to an ArrayList of Strokes too, Lisa 8/8/2009
                _strokes.Add (new List<PointR>(_points)); // need to copy so they don't get cleared
                Invalidate();
			}
		}

		#endregion

        #region Form Button Events

        // event handler for the Recognize button
        // Lisa 12/22/2007
        private void Recognize_Click(object sender, EventArgs e)
        {
            _recentlyRecognized = true;
            if (_points.Count >= 5) // require 5 points for a valid gesture
            {
                if (_rec.NumGestures > 0) // not recording, so testing
                {
                    lblRecognizing.Visible = true;
                    Application.DoEvents(); // forces label to display

                    // combine the strokes into one unistroke, Lisa 8/8/2009
                    List<PointR> points = new List<PointR>();
                    foreach (List<PointR> pts in _strokes)
                    {
                        points.AddRange(pts);
                    }            
                    NBestList result = _rec.Recognize(points, _strokes.Count); // where all the action is!!
                    if (result.Score == -1)
                    {
                        lblResult.Text = String.Format("No Match!\n[{0} out of {1} comparisons made]",
                            result.getActualComparisons(),
                            result.getTotalComparisons());
                    }
                    else
                    {
                        lblResult.Text = String.Format("{0}: {1} ({2}px, {3}{4})\n[{5} out of {6} comparisons made]",
                        result.Name,
                        Math.Round(result.Score, 2),
                        Math.Round(result.Distance, 2),
                        Math.Round(result.Angle, 2), (char)176,
                        result.getActualComparisons(), 
                        result.getTotalComparisons());
                    }

                    lblRecognizing.Visible = false;
                }
            }
        }

        // event handler for the Record button
        // Lisa 1/2/2008
        private void RecordButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Gestures (*.xml)|*.xml";
            dlg.Title = "Save Gesture As";
            dlg.AddExtension = true;
            dlg.RestoreDirectory = true; // Lisa 8/16/2009; have to un-allow this because of accessing our parameters config file

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                // resample, scale, translate to origin
                _rec.SaveGesture(dlg.FileName, _strokes, _numPtsInStroke);  // strokes, not points; Lisa 8/8/2009 
                ReloadViewForm();
            }

            dlg.Dispose();
            _recording = false;
            lblRecord.Visible = false;
            recognizeButton.Enabled = true; // this wasn't enabled while we were recording
            Invalidate();
        }

        // event handler for the Clear Canvas button
        // Lisa 12/22/2007
        private void ClearCanvas_Click(object sender, EventArgs e)
        {
            _points.Clear();
            _strokes.Clear(); // Lisa 8/8/2009
            _numPtsInStroke.Clear(); // Lisa 1/2/2008
            Invalidate();
        }

        #endregion
    }
}
