namespace WakaTime.Forms
{
    partial class SolutionsSettingsForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SolutionsSettingsForm));
            this.radioButtonSolutionAsOne = new System.Windows.Forms.RadioButton();
            this.radioButtonIndividualProjects = new System.Windows.Forms.RadioButton();
            this.label1 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // radioButtonSolutionAsOne
            // 
            this.radioButtonSolutionAsOne.AutoSize = true;
            this.radioButtonSolutionAsOne.Location = new System.Drawing.Point(12, 49);
            this.radioButtonSolutionAsOne.Name = "radioButtonSolutionAsOne";
            this.radioButtonSolutionAsOne.Size = new System.Drawing.Size(141, 17);
            this.radioButtonSolutionAsOne.TabIndex = 0;
            this.radioButtonSolutionAsOne.TabStop = true;
            this.radioButtonSolutionAsOne.Text = "Time solution as a whole";
            this.radioButtonSolutionAsOne.UseVisualStyleBackColor = true;
            this.radioButtonSolutionAsOne.CheckedChanged += new System.EventHandler(this.radioButton1_CheckedChanged);
            // 
            // radioButtonIndividualProjects
            // 
            this.radioButtonIndividualProjects.AutoSize = true;
            this.radioButtonIndividualProjects.Location = new System.Drawing.Point(12, 72);
            this.radioButtonIndividualProjects.Name = "radioButtonIndividualProjects";
            this.radioButtonIndividualProjects.Size = new System.Drawing.Size(164, 17);
            this.radioButtonIndividualProjects.TabIndex = 1;
            this.radioButtonIndividualProjects.TabStop = true;
            this.radioButtonIndividualProjects.Text = "Time each project individually";
            this.radioButtonIndividualProjects.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(358, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "You can choose how WakaTime will measure your activity for this solution.";
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.button1.Location = new System.Drawing.Point(314, 115);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "Save";
            this.button1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button2.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button2.Location = new System.Drawing.Point(233, 115);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 4;
            this.button2.Text = "Cancel";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // SolutionsSettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(401, 150);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.radioButtonIndividualProjects);
            this.Controls.Add(this.radioButtonSolutionAsOne);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SolutionsSettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "WakaTime Solution Settings";
            this.Load += new System.EventHandler(this.SolutionsSettingsForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RadioButton radioButtonSolutionAsOne;
        private System.Windows.Forms.RadioButton radioButtonIndividualProjects;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
    }
}