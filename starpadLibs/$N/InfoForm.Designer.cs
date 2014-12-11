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
namespace Recognizer.NDollar
{
    partial class InfoForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblSubject = new System.Windows.Forms.Label();
            this.numSubject = new System.Windows.Forms.NumericUpDown();
            this.lblSpeed = new System.Windows.Forms.Label();
            this.cboSpeed = new System.Windows.Forms.ComboBox();
            this.cmdOK = new System.Windows.Forms.Button();
            this.cmdCancel = new System.Windows.Forms.Button();
            this.radioLabel = new System.Windows.Forms.Label();
            this.multiUserButton = new System.Windows.Forms.RadioButton();
            this.singleUserPanel = new System.Windows.Forms.Panel();
            this.singleUserButton = new System.Windows.Forms.RadioButton();
            this.twoD = new System.Windows.Forms.CheckBox();
            this.oneD = new System.Windows.Forms.CheckBox();
            this.checkboxLbl = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.numSubject)).BeginInit();
            this.singleUserPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblSubject
            // 
            this.lblSubject.AutoSize = true;
            this.lblSubject.Enabled = false;
            this.lblSubject.Location = new System.Drawing.Point(8, 15);
            this.lblSubject.Name = "lblSubject";
            this.lblSubject.Size = new System.Drawing.Size(83, 13);
            this.lblSubject.TabIndex = 1;
            this.lblSubject.Text = "Subject Number";
            // 
            // numSubject
            // 
            this.numSubject.Enabled = false;
            this.numSubject.Location = new System.Drawing.Point(97, 13);
            this.numSubject.Maximum = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.numSubject.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numSubject.Name = "numSubject";
            this.numSubject.Size = new System.Drawing.Size(82, 20);
            this.numSubject.TabIndex = 2;
            this.numSubject.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblSpeed
            // 
            this.lblSpeed.AutoSize = true;
            this.lblSpeed.Enabled = false;
            this.lblSpeed.Location = new System.Drawing.Point(8, 48);
            this.lblSpeed.Name = "lblSpeed";
            this.lblSpeed.Size = new System.Drawing.Size(78, 13);
            this.lblSpeed.TabIndex = 3;
            this.lblSpeed.Text = "Gesture Speed";
            // 
            // cboSpeed
            // 
            this.cboSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSpeed.Enabled = false;
            this.cboSpeed.FormattingEnabled = true;
            this.cboSpeed.Items.AddRange(new object[] {
            "slow",
            "medium",
            "fast"});
            this.cboSpeed.Location = new System.Drawing.Point(97, 45);
            this.cboSpeed.Name = "cboSpeed";
            this.cboSpeed.Size = new System.Drawing.Size(82, 21);
            this.cboSpeed.TabIndex = 4;
            // 
            // cmdOK
            // 
            this.cmdOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.cmdOK.Location = new System.Drawing.Point(24, 235);
            this.cmdOK.Name = "cmdOK";
            this.cmdOK.Size = new System.Drawing.Size(75, 23);
            this.cmdOK.TabIndex = 5;
            this.cmdOK.Text = "OK";
            this.cmdOK.UseVisualStyleBackColor = true;
            // 
            // cmdCancel
            // 
            this.cmdCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cmdCancel.Location = new System.Drawing.Point(105, 235);
            this.cmdCancel.Name = "cmdCancel";
            this.cmdCancel.Size = new System.Drawing.Size(75, 23);
            this.cmdCancel.TabIndex = 6;
            this.cmdCancel.Text = "Cancel";
            this.cmdCancel.UseVisualStyleBackColor = true;
            // 
            // radioLabel
            // 
            this.radioLabel.AutoSize = true;
            this.radioLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioLabel.Location = new System.Drawing.Point(9, 9);
            this.radioLabel.Name = "radioLabel";
            this.radioLabel.Size = new System.Drawing.Size(140, 13);
            this.radioLabel.TabIndex = 9;
            this.radioLabel.Text = "Type of test to perform:";
            // 
            // multiUserButton
            // 
            this.multiUserButton.AutoSize = true;
            this.multiUserButton.Checked = true;
            this.multiUserButton.Location = new System.Drawing.Point(23, 30);
            this.multiUserButton.Name = "multiUserButton";
            this.multiUserButton.Size = new System.Drawing.Size(86, 17);
            this.multiUserButton.TabIndex = 7;
            this.multiUserButton.TabStop = true;
            this.multiUserButton.Text = "Multiple User";
            this.multiUserButton.UseVisualStyleBackColor = true;
            this.multiUserButton.CheckedChanged += new System.EventHandler(this.multiUserButton_CheckedChanged);
            // 
            // singleUserPanel
            // 
            this.singleUserPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.singleUserPanel.Controls.Add(this.cboSpeed);
            this.singleUserPanel.Controls.Add(this.lblSpeed);
            this.singleUserPanel.Controls.Add(this.numSubject);
            this.singleUserPanel.Controls.Add(this.lblSubject);
            this.singleUserPanel.Enabled = false;
            this.singleUserPanel.Location = new System.Drawing.Point(8, 84);
            this.singleUserPanel.Name = "singleUserPanel";
            this.singleUserPanel.Size = new System.Drawing.Size(186, 80);
            this.singleUserPanel.TabIndex = 10;
            // 
            // singleUserButton
            // 
            this.singleUserButton.AutoSize = true;
            this.singleUserButton.Location = new System.Drawing.Point(24, 53);
            this.singleUserButton.Name = "singleUserButton";
            this.singleUserButton.Size = new System.Drawing.Size(79, 17);
            this.singleUserButton.TabIndex = 11;
            this.singleUserButton.Text = "Single User";
            this.singleUserButton.UseVisualStyleBackColor = true;
            this.singleUserButton.CheckedChanged += new System.EventHandler(this.singleUserButton_CheckedChanged);
            // 
            // twoD
            // 
            this.twoD.AutoSize = true;
            this.twoD.Checked = true;
            this.twoD.CheckState = System.Windows.Forms.CheckState.Checked;
            this.twoD.Location = new System.Drawing.Point(72, 204);
            this.twoD.Name = "twoD";
            this.twoD.Size = new System.Drawing.Size(40, 17);
            this.twoD.TabIndex = 12;
            this.twoD.Text = "2D";
            this.twoD.UseVisualStyleBackColor = true;
            // 
            // oneD
            // 
            this.oneD.AutoSize = true;
            this.oneD.Location = new System.Drawing.Point(26, 204);
            this.oneD.Name = "oneD";
            this.oneD.Size = new System.Drawing.Size(40, 17);
            this.oneD.TabIndex = 13;
            this.oneD.Text = "1D";
            this.oneD.UseVisualStyleBackColor = true;
            // 
            // checkboxLbl
            // 
            this.checkboxLbl.AutoSize = true;
            this.checkboxLbl.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkboxLbl.Location = new System.Drawing.Point(9, 180);
            this.checkboxLbl.Name = "checkboxLbl";
            this.checkboxLbl.Size = new System.Drawing.Size(166, 13);
            this.checkboxLbl.TabIndex = 14;
            this.checkboxLbl.Text = "Type of gestures to include:";
            // 
            // InfoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(202, 268);
            this.Controls.Add(this.checkboxLbl);
            this.Controls.Add(this.oneD);
            this.Controls.Add(this.twoD);
            this.Controls.Add(this.multiUserButton);
            this.Controls.Add(this.singleUserButton);
            this.Controls.Add(this.singleUserPanel);
            this.Controls.Add(this.radioLabel);
            this.Controls.Add(this.cmdCancel);
            this.Controls.Add(this.cmdOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "InfoForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Information";
            ((System.ComponentModel.ISupportInitialize)(this.numSubject)).EndInit();
            this.singleUserPanel.ResumeLayout(false);
            this.singleUserPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblSubject;
        private System.Windows.Forms.NumericUpDown numSubject;
        private System.Windows.Forms.Label lblSpeed;
        private System.Windows.Forms.ComboBox cboSpeed;
        private System.Windows.Forms.Button cmdOK;
        private System.Windows.Forms.Button cmdCancel;
        private System.Windows.Forms.Label radioLabel;
        private System.Windows.Forms.RadioButton multiUserButton;
        private System.Windows.Forms.Panel singleUserPanel;
        private System.Windows.Forms.RadioButton singleUserButton;
        private System.Windows.Forms.CheckBox twoD;
        private System.Windows.Forms.CheckBox oneD;
        private System.Windows.Forms.Label checkboxLbl;
    }
}
