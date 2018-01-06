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
            this.SuspendLayout();
            // 
            // openSctButton
            // 
            this.openSctButton.Location = new System.Drawing.Point(130, 73);
            this.openSctButton.Name = "openSctButton";
            this.openSctButton.Size = new System.Drawing.Size(75, 23);
            this.openSctButton.TabIndex = 0;
            this.openSctButton.Text = "button1";
            this.openSctButton.UseVisualStyleBackColor = true;
            this.openSctButton.Click += new System.EventHandler(this.openSctButton_Click);
            // 
            // SctEditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.openSctButton);
            this.Name = "SctEditorForm";
            this.Text = "Form1";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button openSctButton;
    }
}

