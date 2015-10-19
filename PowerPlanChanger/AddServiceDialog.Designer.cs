namespace PowerPlanChanger
{
    partial class AddServiceDialog
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
            this.okButton = new System.Windows.Forms.Button();
            this.serviceListBox = new System.Windows.Forms.CheckedListBox();
            this.cancelButton = new System.Windows.Forms.Button();
            this.serviceInfoTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // okButton
            // 
            this.okButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.okButton.Location = new System.Drawing.Point(142, 315);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(120, 35);
            this.okButton.TabIndex = 2;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // serviceListBox
            // 
            this.serviceListBox.CheckOnClick = true;
            this.serviceListBox.FormattingEnabled = true;
            this.serviceListBox.Location = new System.Drawing.Point(12, 12);
            this.serviceListBox.Name = "serviceListBox";
            this.serviceListBox.Size = new System.Drawing.Size(250, 289);
            this.serviceListBox.Sorted = true;
            this.serviceListBox.TabIndex = 3;
            this.serviceListBox.SelectedIndexChanged += new System.EventHandler(this.serviceListBox_SelectedIndexChanged);
            // 
            // cancelButton
            // 
            this.cancelButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cancelButton.Location = new System.Drawing.Point(268, 315);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(120, 35);
            this.cancelButton.TabIndex = 4;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // serviceInfoTextBox
            // 
            this.serviceInfoTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.serviceInfoTextBox.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.serviceInfoTextBox.Location = new System.Drawing.Point(268, 12);
            this.serviceInfoTextBox.Multiline = true;
            this.serviceInfoTextBox.Name = "serviceInfoTextBox";
            this.serviceInfoTextBox.ReadOnly = true;
            this.serviceInfoTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.serviceInfoTextBox.Size = new System.Drawing.Size(304, 289);
            this.serviceInfoTextBox.TabIndex = 5;
            // 
            // AddServiceDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 362);
            this.Controls.Add(this.serviceInfoTextBox);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.Controls.Add(this.serviceListBox);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddServiceDialog";
            this.Text = "Add a service...";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.CheckedListBox serviceListBox;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.TextBox serviceInfoTextBox;
    }
}