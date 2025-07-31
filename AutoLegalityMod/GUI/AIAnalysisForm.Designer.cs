namespace AutoModPlugins.GUI
{
    partial class AIAnalysisForm
    {
        private System.Windows.Forms.TextBox TB_Input;
        private System.Windows.Forms.RichTextBox TB_Output;
        private System.Windows.Forms.Button B_Analyze;
        private System.Windows.Forms.Button B_Clear;
        private System.Windows.Forms.Button B_Copy;
        private System.Windows.Forms.Label L_Input;
        private System.Windows.Forms.Label L_Output;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.Button B_ImportFix;

        private void InitializeComponent()
        {
            this.TB_Input = new System.Windows.Forms.TextBox();
            this.TB_Output = new System.Windows.Forms.RichTextBox();
            this.B_Analyze = new System.Windows.Forms.Button();
            this.B_Clear = new System.Windows.Forms.Button();
            this.B_Copy = new System.Windows.Forms.Button();
            this.L_Input = new System.Windows.Forms.Label();
            this.L_Output = new System.Windows.Forms.Label();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.B_ImportFix = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.SuspendLayout();
            // 
            // TB_Input
            // 
            this.TB_Input.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TB_Input.Font = new System.Drawing.Font("Courier New", 9F);
            this.TB_Input.Location = new System.Drawing.Point(3, 23);
            this.TB_Input.Multiline = true;
            this.TB_Input.Name = "TB_Input";
            this.TB_Input.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.TB_Input.Size = new System.Drawing.Size(376, 367);
            this.TB_Input.TabIndex = 0;
            // 
            // TB_Output
            // 
            this.TB_Output.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TB_Output.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.TB_Output.Location = new System.Drawing.Point(3, 23);
            this.TB_Output.Name = "TB_Output";
            this.TB_Output.ReadOnly = true;
            this.TB_Output.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            this.TB_Output.Size = new System.Drawing.Size(376, 367);
            this.TB_Output.TabIndex = 1;
            this.TB_Output.Text = "";
            // 
            // B_Analyze
            // 
            this.B_Analyze.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.B_Analyze.Location = new System.Drawing.Point(12, 415);
            this.B_Analyze.Name = "B_Analyze";
            this.B_Analyze.Size = new System.Drawing.Size(100, 30);
            this.B_Analyze.TabIndex = 2;
            this.B_Analyze.Text = "Analyze";
            this.B_Analyze.UseVisualStyleBackColor = true;
            this.B_Analyze.Click += new System.EventHandler(this.B_Analyze_Click);
            // 
            // B_Clear
            // 
            this.B_Clear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.B_Clear.Location = new System.Drawing.Point(118, 415);
            this.B_Clear.Name = "B_Clear";
            this.B_Clear.Size = new System.Drawing.Size(75, 30);
            this.B_Clear.TabIndex = 3;
            this.B_Clear.Text = "Clear";
            this.B_Clear.UseVisualStyleBackColor = true;
            this.B_Clear.Click += new System.EventHandler(this.B_Clear_Click);
            // 
            // B_Copy
            // 
            this.B_Copy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.B_Copy.Location = new System.Drawing.Point(697, 415);
            this.B_Copy.Name = "B_Copy";
            this.B_Copy.Size = new System.Drawing.Size(75, 30);
            this.B_Copy.TabIndex = 4;
            this.B_Copy.Text = "Copy";
            this.B_Copy.UseVisualStyleBackColor = true;
            this.B_Copy.Click += new System.EventHandler(this.B_Copy_Click);
            // 
            // L_Input
            // 
            this.L_Input.AutoSize = true;
            this.L_Input.Location = new System.Drawing.Point(3, 5);
            this.L_Input.Name = "L_Input";
            this.L_Input.Size = new System.Drawing.Size(77, 15);
            this.L_Input.TabIndex = 5;
            this.L_Input.Text = "Showdown Set:";
            // 
            // L_Output
            // 
            this.L_Output.AutoSize = true;
            this.L_Output.Location = new System.Drawing.Point(3, 5);
            this.L_Output.Name = "L_Output";
            this.L_Output.Size = new System.Drawing.Size(69, 15);
            this.L_Output.TabIndex = 6;
            this.L_Output.Text = "AI Analysis:";
            // 
            // splitContainer
            // 
            this.splitContainer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer.Location = new System.Drawing.Point(12, 12);
            this.splitContainer.Name = "splitContainer";
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.L_Input);
            this.splitContainer.Panel1.Controls.Add(this.TB_Input);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.L_Output);
            this.splitContainer.Panel2.Controls.Add(this.TB_Output);
            this.splitContainer.Size = new System.Drawing.Size(760, 393);
            this.splitContainer.SplitterDistance = 380;
            this.splitContainer.TabIndex = 7;
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.Location = new System.Drawing.Point(199, 420);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(492, 20);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.progressBar.TabIndex = 8;
            this.progressBar.Visible = false;
            // 
            // B_ImportFix
            // 
            this.B_ImportFix.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.B_ImportFix.Location = new System.Drawing.Point(616, 415);
            this.B_ImportFix.Name = "B_ImportFix";
            this.B_ImportFix.Size = new System.Drawing.Size(75, 30);
            this.B_ImportFix.TabIndex = 9;
            this.B_ImportFix.Text = "Import Fix";
            this.B_ImportFix.UseVisualStyleBackColor = true;
            this.B_ImportFix.Visible = false;
            this.B_ImportFix.Click += new System.EventHandler(this.B_ImportFix_Click);
            // 
            // AIAnalysisForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 461);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.B_ImportFix);
            this.Controls.Add(this.B_Copy);
            this.Controls.Add(this.B_Clear);
            this.Controls.Add(this.B_Analyze);
            this.MinimumSize = new System.Drawing.Size(800, 500);
            this.Name = "AIAnalysisForm";
            this.Text = "AI Analysis";
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel1.PerformLayout();
            this.splitContainer.Panel2.ResumeLayout(false);
            this.splitContainer.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}