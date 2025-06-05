namespace AutoModPlugins.GUI
{
    partial class ALMStatusBar
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
            pb_status = new System.Windows.Forms.ProgressBar();
            L_status = new System.Windows.Forms.Label();
            SuspendLayout();
            // 
            // pb_status
            // 
            pb_status.Location = new System.Drawing.Point(12, 12);
            pb_status.Name = "pb_status";
            pb_status.Size = new System.Drawing.Size(301, 31);
            pb_status.TabIndex = 0;
            // 
            // L_status
            // 
            L_status.AutoSize = true;
            L_status.Location = new System.Drawing.Point(126, 60);
            L_status.Name = "L_status";
            L_status.Size = new System.Drawing.Size(38, 15);
            L_status.TabIndex = 1;
            L_status.Text = "label1";
            // 
            // ALMStatusBar
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(325, 84);
            Controls.Add(L_status);
            Controls.Add(pb_status);
            Name = "ALMStatusBar";
            Text = "form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ProgressBar pb_status;
        private System.Windows.Forms.Label L_status;
    }
}