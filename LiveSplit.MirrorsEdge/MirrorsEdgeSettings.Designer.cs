namespace LiveSplit.MirrorsEdge
{
    partial class MirrorsEdgeSettings
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
            this.chkResetStart = new System.Windows.Forms.CheckBox();
            this.gbAutoSplit = new System.Windows.Forms.GroupBox();
            this.chkStormdrainSplit = new System.Windows.Forms.CheckBox();
            this.chkEndingSplit = new System.Windows.Forms.CheckBox();
            this.chkChapterSplit = new System.Windows.Forms.CheckBox();
            this.gbLoadRemoval = new System.Windows.Forms.GroupBox();
            this.rbGameTime = new System.Windows.Forms.RadioButton();
            this.rbRealTime = new System.Windows.Forms.RadioButton();
            this.gbAutoSplit.SuspendLayout();
            this.gbLoadRemoval.SuspendLayout();
            this.SuspendLayout();
            // 
            // chkResetStart
            // 
            this.chkResetStart.AutoSize = true;
            this.chkResetStart.Location = new System.Drawing.Point(6, 19);
            this.chkResetStart.Name = "chkResetStart";
            this.chkResetStart.Size = new System.Drawing.Size(165, 17);
            this.chkResetStart.TabIndex = 0;
            this.chkResetStart.Text = "Reset and start on new game";
            this.chkResetStart.UseVisualStyleBackColor = true;
            // 
            // gbAutoSplit
            // 
            this.gbAutoSplit.Controls.Add(this.chkStormdrainSplit);
            this.gbAutoSplit.Controls.Add(this.chkEndingSplit);
            this.gbAutoSplit.Controls.Add(this.chkChapterSplit);
            this.gbAutoSplit.Controls.Add(this.chkResetStart);
            this.gbAutoSplit.Location = new System.Drawing.Point(12, 12);
            this.gbAutoSplit.Name = "gbAutoSplit";
            this.gbAutoSplit.Size = new System.Drawing.Size(235, 118);
            this.gbAutoSplit.TabIndex = 1;
            this.gbAutoSplit.TabStop = false;
            this.gbAutoSplit.Text = "Auto-Split";
            // 
            // chkStormdrainSplit
            // 
            this.chkStormdrainSplit.AutoSize = true;
            this.chkStormdrainSplit.Location = new System.Drawing.Point(6, 88);
            this.chkStormdrainSplit.Name = "chkStormdrainSplit";
            this.chkStormdrainSplit.Size = new System.Drawing.Size(208, 17);
            this.chkStormdrainSplit.TabIndex = 3;
            this.chkStormdrainSplit.Text = "Additional split at stormdrain exit button";
            this.chkStormdrainSplit.UseVisualStyleBackColor = true;
            // 
            // chkEndingSplit
            // 
            this.chkEndingSplit.AutoSize = true;
            this.chkEndingSplit.Location = new System.Drawing.Point(6, 65);
            this.chkEndingSplit.Name = "chkEndingSplit";
            this.chkEndingSplit.Size = new System.Drawing.Size(80, 17);
            this.chkEndingSplit.TabIndex = 2;
            this.chkEndingSplit.Text = "Ending split";
            this.chkEndingSplit.UseVisualStyleBackColor = true;
            // 
            // chkChapterSplit
            // 
            this.chkChapterSplit.AutoSize = true;
            this.chkChapterSplit.Location = new System.Drawing.Point(6, 42);
            this.chkChapterSplit.Name = "chkChapterSplit";
            this.chkChapterSplit.Size = new System.Drawing.Size(105, 17);
            this.chkChapterSplit.TabIndex = 1;
            this.chkChapterSplit.Text = "Chapter end split";
            this.chkChapterSplit.UseVisualStyleBackColor = true;
            // 
            // gbLoadRemoval
            // 
            this.gbLoadRemoval.Controls.Add(this.rbGameTime);
            this.gbLoadRemoval.Controls.Add(this.rbRealTime);
            this.gbLoadRemoval.Location = new System.Drawing.Point(18, 136);
            this.gbLoadRemoval.Name = "gbLoadRemoval";
            this.gbLoadRemoval.Size = new System.Drawing.Size(229, 49);
            this.gbLoadRemoval.TabIndex = 2;
            this.gbLoadRemoval.TabStop = false;
            this.gbLoadRemoval.Text = "Load Removal - Main timer timing method";
            // 
            // rbGameTime
            // 
            this.rbGameTime.AutoSize = true;
            this.rbGameTime.Location = new System.Drawing.Point(89, 19);
            this.rbGameTime.Name = "rbGameTime";
            this.rbGameTime.Size = new System.Drawing.Size(79, 17);
            this.rbGameTime.TabIndex = 1;
            this.rbGameTime.TabStop = true;
            this.rbGameTime.Text = "Game Time";
            this.rbGameTime.UseVisualStyleBackColor = true;
            this.rbGameTime.CheckedChanged += new System.EventHandler(this.TimingMethodsCheckedChanged);
            // 
            // rbRealTime
            // 
            this.rbRealTime.AutoSize = true;
            this.rbRealTime.Location = new System.Drawing.Point(6, 19);
            this.rbRealTime.Name = "rbRealTime";
            this.rbRealTime.Size = new System.Drawing.Size(73, 17);
            this.rbRealTime.TabIndex = 0;
            this.rbRealTime.TabStop = true;
            this.rbRealTime.Text = "Real Time";
            this.rbRealTime.UseVisualStyleBackColor = true;
            this.rbRealTime.CheckedChanged += new System.EventHandler(this.TimingMethodsCheckedChanged);
            // 
            // MirrorsEdgeSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gbLoadRemoval);
            this.Controls.Add(this.gbAutoSplit);
            this.Name = "MirrorsEdgeSettings";
            this.Size = new System.Drawing.Size(259, 194);
            this.gbAutoSplit.ResumeLayout(false);
            this.gbAutoSplit.PerformLayout();
            this.gbLoadRemoval.ResumeLayout(false);
            this.gbLoadRemoval.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.CheckBox chkResetStart;
        private System.Windows.Forms.GroupBox gbAutoSplit;
        private System.Windows.Forms.CheckBox chkStormdrainSplit;
        private System.Windows.Forms.CheckBox chkEndingSplit;
        private System.Windows.Forms.CheckBox chkChapterSplit;
        private System.Windows.Forms.GroupBox gbLoadRemoval;
        private System.Windows.Forms.RadioButton rbGameTime;
        private System.Windows.Forms.RadioButton rbRealTime;
    }
}