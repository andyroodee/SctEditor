namespace SctEditor
{
    partial class SctEditorForm
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
            this.openSctButton = new System.Windows.Forms.Button();
            this.saveSctButton = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // openSctButton
            // 
            this.openSctButton.Location = new System.Drawing.Point(130, 73);
            this.openSctButton.Name = "openSctButton";
            this.openSctButton.Size = new System.Drawing.Size(75, 23);
            this.openSctButton.TabIndex = 0;
            this.openSctButton.Text = "Open";
            this.openSctButton.UseVisualStyleBackColor = true;
            this.openSctButton.Click += new System.EventHandler(this.openSctButton_Click);
            // 
            // saveSctButton
            // 
            this.saveSctButton.Location = new System.Drawing.Point(130, 140);
            this.saveSctButton.Name = "saveSctButton";
            this.saveSctButton.Size = new System.Drawing.Size(75, 23);
            this.saveSctButton.TabIndex = 1;
            this.saveSctButton.Text = "Save";
            this.saveSctButton.UseVisualStyleBackColor = true;
            this.saveSctButton.Click += new System.EventHandler(this.saveSctButton_Click);
            // 
            // SctEditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.saveSctButton);
            this.Controls.Add(this.openSctButton);
            this.Name = "SctEditorForm";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button openSctButton;
        private System.Windows.Forms.Button saveSctButton;
    }
}

