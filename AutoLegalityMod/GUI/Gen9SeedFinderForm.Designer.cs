using System;

namespace AutoModPlugins.GUI
{
    partial class Gen9SeedFinderForm
    {
        private System.ComponentModel.IContainer components = null;

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.mainPanel = new System.Windows.Forms.Panel();
            this.splitContainer = new System.Windows.Forms.SplitContainer();

            // Search Panel
            this.searchPanel = new System.Windows.Forms.Panel();
            this.speciesGroup = new System.Windows.Forms.GroupBox();
            this.speciesLabel = new System.Windows.Forms.Label();
            this.speciesCombo = new System.Windows.Forms.ComboBox();
            this.formLabel = new System.Windows.Forms.Label();
            this.formCombo = new System.Windows.Forms.ComboBox();
            this.encounterLabel = new System.Windows.Forms.Label();
            this.encounterCombo = new System.Windows.Forms.ComboBox();

            this.criteriaGroup = new System.Windows.Forms.GroupBox();
            this.genderLabel = new System.Windows.Forms.Label();
            this.genderCombo = new System.Windows.Forms.ComboBox();
            this.abilityLabel = new System.Windows.Forms.Label();
            this.abilityCombo = new System.Windows.Forms.ComboBox();
            this.natureLabel = new System.Windows.Forms.Label();
            this.natureCombo = new System.Windows.Forms.ComboBox();
            this.shinyLabel = new System.Windows.Forms.Label();
            this.shinyCombo = new System.Windows.Forms.ComboBox();

            this.ivGroup = new System.Windows.Forms.GroupBox();
            this.ivHpLabel = new System.Windows.Forms.Label();
            this.ivHpMin = new System.Windows.Forms.NumericUpDown();
            this.ivAtkLabel = new System.Windows.Forms.Label();
            this.ivAtkMin = new System.Windows.Forms.NumericUpDown();
            this.ivDefLabel = new System.Windows.Forms.Label();
            this.ivDefMin = new System.Windows.Forms.NumericUpDown();
            this.ivSpaLabel = new System.Windows.Forms.Label();
            this.ivSpaMin = new System.Windows.Forms.NumericUpDown();
            this.ivSpdLabel = new System.Windows.Forms.Label();
            this.ivSpdMin = new System.Windows.Forms.NumericUpDown();
            this.ivSpeLabel = new System.Windows.Forms.Label();
            this.ivSpeMin = new System.Windows.Forms.NumericUpDown();

            this.searchOptionsGroup = new System.Windows.Forms.GroupBox();
            this.maxSeedsLabel = new System.Windows.Forms.Label();
            this.maxSeedsNum = new System.Windows.Forms.NumericUpDown();
            this.searchButton = new System.Windows.Forms.Button();
            this.exportButton = new System.Windows.Forms.Button();

            // Results Panel
            this.resultsPanel = new System.Windows.Forms.Panel();
            this.resultsGrid = new System.Windows.Forms.DataGridView();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.progressBar = new System.Windows.Forms.ToolStripProgressBar();

            this.mainPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.searchPanel.SuspendLayout();
            this.speciesGroup.SuspendLayout();
            this.criteriaGroup.SuspendLayout();
            this.ivGroup.SuspendLayout();
            this.searchOptionsGroup.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ivHpMin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ivAtkMin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ivDefMin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ivSpaMin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ivSpdMin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.ivSpeMin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.maxSeedsNum)).BeginInit();
            this.resultsPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.resultsGrid)).BeginInit();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();

            // Form
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(900, 600);
            this.Controls.Add(this.mainPanel);
            this.Controls.Add(this.statusStrip);
            this.MinimumSize = new System.Drawing.Size(900, 600);
            this.Name = "Gen9SeedFinderForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Gen 9 Seed Finder";

            // mainPanel
            this.mainPanel.Controls.Add(this.splitContainer);
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.Location = new System.Drawing.Point(0, 0);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.Size = new System.Drawing.Size(900, 578);
            this.mainPanel.TabIndex = 0;

            // splitContainer
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 0);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.Panel1.Controls.Add(this.searchPanel);
            this.splitContainer.Panel2.Controls.Add(this.resultsPanel);
            this.splitContainer.Size = new System.Drawing.Size(900, 578);
            this.splitContainer.SplitterDistance = 350;
            this.splitContainer.TabIndex = 0;

            // searchPanel
            this.searchPanel.AutoScroll = true;
            this.searchPanel.Controls.Add(this.searchOptionsGroup);
            this.searchPanel.Controls.Add(this.ivGroup);
            this.searchPanel.Controls.Add(this.criteriaGroup);
            this.searchPanel.Controls.Add(this.speciesGroup);
            this.searchPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.searchPanel.Location = new System.Drawing.Point(0, 0);
            this.searchPanel.Name = "searchPanel";
            this.searchPanel.Padding = new System.Windows.Forms.Padding(5);
            this.searchPanel.Size = new System.Drawing.Size(350, 578);
            this.searchPanel.TabIndex = 0;

            // speciesGroup
            this.speciesGroup.Controls.Add(this.encounterCombo);
            this.speciesGroup.Controls.Add(this.encounterLabel);
            this.speciesGroup.Controls.Add(this.formCombo);
            this.speciesGroup.Controls.Add(this.formLabel);
            this.speciesGroup.Controls.Add(this.speciesCombo);
            this.speciesGroup.Controls.Add(this.speciesLabel);
            this.speciesGroup.Location = new System.Drawing.Point(8, 8);
            this.speciesGroup.Name = "speciesGroup";
            this.speciesGroup.Size = new System.Drawing.Size(330, 140);
            this.speciesGroup.TabIndex = 0;
            this.speciesGroup.TabStop = false;
            this.speciesGroup.Text = "Target Pokémon";

            // speciesLabel
            this.speciesLabel.AutoSize = true;
            this.speciesLabel.Location = new System.Drawing.Point(10, 25);
            this.speciesLabel.Name = "speciesLabel";
            this.speciesLabel.Size = new System.Drawing.Size(49, 15);
            this.speciesLabel.TabIndex = 0;
            this.speciesLabel.Text = "Species:";

            // speciesCombo
            this.speciesCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.speciesCombo.FormattingEnabled = true;
            this.speciesCombo.Location = new System.Drawing.Point(80, 22);
            this.speciesCombo.Name = "speciesCombo";
            this.speciesCombo.Size = new System.Drawing.Size(240, 23);
            this.speciesCombo.TabIndex = 1;
            this.speciesCombo.SelectedIndexChanged += new System.EventHandler(this.SpeciesCombo_SelectedIndexChanged);

            // formLabel
            this.formLabel.AutoSize = true;
            this.formLabel.Location = new System.Drawing.Point(10, 55);
            this.formLabel.Name = "formLabel";
            this.formLabel.Size = new System.Drawing.Size(38, 15);
            this.formLabel.TabIndex = 2;
            this.formLabel.Text = "Form:";

            // formCombo
            this.formCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.formCombo.FormattingEnabled = true;
            this.formCombo.Location = new System.Drawing.Point(80, 52);
            this.formCombo.Name = "formCombo";
            this.formCombo.Size = new System.Drawing.Size(240, 23);
            this.formCombo.TabIndex = 3;

            // encounterLabel
            this.encounterLabel.AutoSize = true;
            this.encounterLabel.Location = new System.Drawing.Point(10, 85);
            this.encounterLabel.Name = "encounterLabel";
            this.encounterLabel.Size = new System.Drawing.Size(64, 15);
            this.encounterLabel.TabIndex = 4;
            this.encounterLabel.Text = "Encounter:";

            // encounterCombo
            this.encounterCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.encounterCombo.FormattingEnabled = true;
            this.encounterCombo.Location = new System.Drawing.Point(80, 82);
            this.encounterCombo.Name = "encounterCombo";
            this.encounterCombo.Size = new System.Drawing.Size(240, 23);
            this.encounterCombo.TabIndex = 5;

            // criteriaGroup
            this.criteriaGroup.Controls.Add(this.shinyCombo);
            this.criteriaGroup.Controls.Add(this.shinyLabel);
            this.criteriaGroup.Controls.Add(this.natureCombo);
            this.criteriaGroup.Controls.Add(this.natureLabel);
            this.criteriaGroup.Controls.Add(this.abilityCombo);
            this.criteriaGroup.Controls.Add(this.abilityLabel);
            this.criteriaGroup.Controls.Add(this.genderCombo);
            this.criteriaGroup.Controls.Add(this.genderLabel);
            this.criteriaGroup.Location = new System.Drawing.Point(8, 154);
            this.criteriaGroup.Name = "criteriaGroup";
            this.criteriaGroup.Size = new System.Drawing.Size(330, 140);
            this.criteriaGroup.TabIndex = 1;
            this.criteriaGroup.TabStop = false;
            this.criteriaGroup.Text = "Search Criteria";

            // genderLabel
            this.genderLabel.AutoSize = true;
            this.genderLabel.Location = new System.Drawing.Point(10, 25);
            this.genderLabel.Name = "genderLabel";
            this.genderLabel.Size = new System.Drawing.Size(48, 15);
            this.genderLabel.TabIndex = 0;
            this.genderLabel.Text = "Gender:";

            // genderCombo
            this.genderCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.genderCombo.FormattingEnabled = true;
            this.genderCombo.Items.AddRange(new object[] {
                "Random",
                "Male",
                "Female",
                "Genderless"});
            this.genderCombo.Location = new System.Drawing.Point(80, 22);
            this.genderCombo.Name = "genderCombo";
            this.genderCombo.Size = new System.Drawing.Size(100, 23);
            this.genderCombo.TabIndex = 1;
            this.genderCombo.SelectedIndex = 0;

            // abilityLabel
            this.abilityLabel.AutoSize = true;
            this.abilityLabel.Location = new System.Drawing.Point(10, 55);
            this.abilityLabel.Name = "abilityLabel";
            this.abilityLabel.Size = new System.Drawing.Size(44, 15);
            this.abilityLabel.TabIndex = 2;
            this.abilityLabel.Text = "Ability:";

            // abilityCombo
            this.abilityCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.abilityCombo.FormattingEnabled = true;
            this.abilityCombo.Items.AddRange(new object[] {
                "Any",
                "1",
                "2",
                "Hidden",
                "1/2"});
            this.abilityCombo.Location = new System.Drawing.Point(80, 52);
            this.abilityCombo.Name = "abilityCombo";
            this.abilityCombo.Size = new System.Drawing.Size(100, 23);
            this.abilityCombo.TabIndex = 3;
            this.abilityCombo.SelectedIndex = 0;

            // natureLabel
            this.natureLabel.AutoSize = true;
            this.natureLabel.Location = new System.Drawing.Point(190, 25);
            this.natureLabel.Name = "natureLabel";
            this.natureLabel.Size = new System.Drawing.Size(46, 15);
            this.natureLabel.TabIndex = 4;
            this.natureLabel.Text = "Nature:";

            // natureCombo
            this.natureCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.natureCombo.FormattingEnabled = true;
            this.natureCombo.Location = new System.Drawing.Point(240, 22);
            this.natureCombo.Name = "natureCombo";
            this.natureCombo.Size = new System.Drawing.Size(80, 23);
            this.natureCombo.TabIndex = 5;

            // shinyLabel
            this.shinyLabel.AutoSize = true;
            this.shinyLabel.Location = new System.Drawing.Point(190, 55);
            this.shinyLabel.Name = "shinyLabel";
            this.shinyLabel.Size = new System.Drawing.Size(39, 15);
            this.shinyLabel.TabIndex = 6;
            this.shinyLabel.Text = "Shiny:";

            // shinyCombo
            this.shinyCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.shinyCombo.FormattingEnabled = true;
            this.shinyCombo.Items.AddRange(new object[] {
                "Random",
                "Never",
                "Always"});
            this.shinyCombo.Location = new System.Drawing.Point(240, 52);
            this.shinyCombo.Name = "shinyCombo";
            this.shinyCombo.Size = new System.Drawing.Size(80, 23);
            this.shinyCombo.TabIndex = 7;
            this.shinyCombo.SelectedIndex = 0;

            // ivGroup
            this.ivGroup.Controls.Add(this.ivSpeMin);
            this.ivGroup.Controls.Add(this.ivSpeLabel);
            this.ivGroup.Controls.Add(this.ivSpdMin);
            this.ivGroup.Controls.Add(this.ivSpdLabel);
            this.ivGroup.Controls.Add(this.ivSpaMin);
            this.ivGroup.Controls.Add(this.ivSpaLabel);
            this.ivGroup.Controls.Add(this.ivDefMin);
            this.ivGroup.Controls.Add(this.ivDefLabel);
            this.ivGroup.Controls.Add(this.ivAtkMin);
            this.ivGroup.Controls.Add(this.ivAtkLabel);
            this.ivGroup.Controls.Add(this.ivHpMin);
            this.ivGroup.Controls.Add(this.ivHpLabel);
            this.ivGroup.Location = new System.Drawing.Point(8, 300);
            this.ivGroup.Name = "ivGroup";
            this.ivGroup.Size = new System.Drawing.Size(330, 100);
            this.ivGroup.TabIndex = 2;
            this.ivGroup.TabStop = false;
            this.ivGroup.Text = "Minimum IVs (0 = Any)";

            // ivHpLabel
            this.ivHpLabel.AutoSize = true;
            this.ivHpLabel.Location = new System.Drawing.Point(10, 30);
            this.ivHpLabel.Name = "ivHpLabel";
            this.ivHpLabel.Size = new System.Drawing.Size(26, 15);
            this.ivHpLabel.TabIndex = 0;
            this.ivHpLabel.Text = "HP:";

            // ivHpMin
            this.ivHpMin.Location = new System.Drawing.Point(40, 28);
            this.ivHpMin.Maximum = new decimal(new int[] { 31, 0, 0, 0 });
            this.ivHpMin.Name = "ivHpMin";
            this.ivHpMin.Size = new System.Drawing.Size(40, 23);
            this.ivHpMin.TabIndex = 1;

            // ivAtkLabel
            this.ivAtkLabel.AutoSize = true;
            this.ivAtkLabel.Location = new System.Drawing.Point(90, 30);
            this.ivAtkLabel.Name = "ivAtkLabel";
            this.ivAtkLabel.Size = new System.Drawing.Size(30, 15);
            this.ivAtkLabel.TabIndex = 2;
            this.ivAtkLabel.Text = "ATK:";

            // ivAtkMin
            this.ivAtkMin.Location = new System.Drawing.Point(125, 28);
            this.ivAtkMin.Maximum = new decimal(new int[] { 31, 0, 0, 0 });
            this.ivAtkMin.Name = "ivAtkMin";
            this.ivAtkMin.Size = new System.Drawing.Size(40, 23);
            this.ivAtkMin.TabIndex = 3;

            // ivDefLabel
            this.ivDefLabel.AutoSize = true;
            this.ivDefLabel.Location = new System.Drawing.Point(175, 30);
            this.ivDefLabel.Name = "ivDefLabel";
            this.ivDefLabel.Size = new System.Drawing.Size(30, 15);
            this.ivDefLabel.TabIndex = 4;
            this.ivDefLabel.Text = "DEF:";

            // ivDefMin
            this.ivDefMin.Location = new System.Drawing.Point(210, 28);
            this.ivDefMin.Maximum = new decimal(new int[] { 31, 0, 0, 0 });
            this.ivDefMin.Name = "ivDefMin";
            this.ivDefMin.Size = new System.Drawing.Size(40, 23);
            this.ivDefMin.TabIndex = 5;

            // ivSpaLabel
            this.ivSpaLabel.AutoSize = true;
            this.ivSpaLabel.Location = new System.Drawing.Point(10, 60);
            this.ivSpaLabel.Name = "ivSpaLabel";
            this.ivSpaLabel.Size = new System.Drawing.Size(29, 15);
            this.ivSpaLabel.TabIndex = 6;
            this.ivSpaLabel.Text = "SPA:";

            // ivSpaMin
            this.ivSpaMin.Location = new System.Drawing.Point(40, 58);
            this.ivSpaMin.Maximum = new decimal(new int[] { 31, 0, 0, 0 });
            this.ivSpaMin.Name = "ivSpaMin";
            this.ivSpaMin.Size = new System.Drawing.Size(40, 23);
            this.ivSpaMin.TabIndex = 7;

            // ivSpdLabel
            this.ivSpdLabel.AutoSize = true;
            this.ivSpdLabel.Location = new System.Drawing.Point(90, 60);
            this.ivSpdLabel.Name = "ivSpdLabel";
            this.ivSpdLabel.Size = new System.Drawing.Size(30, 15);
            this.ivSpdLabel.TabIndex = 8;
            this.ivSpdLabel.Text = "SPD:";

            // ivSpdMin
            this.ivSpdMin.Location = new System.Drawing.Point(125, 58);
            this.ivSpdMin.Maximum = new decimal(new int[] { 31, 0, 0, 0 });
            this.ivSpdMin.Name = "ivSpdMin";
            this.ivSpdMin.Size = new System.Drawing.Size(40, 23);
            this.ivSpdMin.TabIndex = 9;

            // ivSpeLabel
            this.ivSpeLabel.AutoSize = true;
            this.ivSpeLabel.Location = new System.Drawing.Point(175, 60);
            this.ivSpeLabel.Name = "ivSpeLabel";
            this.ivSpeLabel.Size = new System.Drawing.Size(28, 15);
            this.ivSpeLabel.TabIndex = 10;
            this.ivSpeLabel.Text = "SPE:";

            // ivSpeMin
            this.ivSpeMin.Location = new System.Drawing.Point(210, 58);
            this.ivSpeMin.Maximum = new decimal(new int[] { 31, 0, 0, 0 });
            this.ivSpeMin.Name = "ivSpeMin";
            this.ivSpeMin.Size = new System.Drawing.Size(40, 23);
            this.ivSpeMin.TabIndex = 11;

            // searchOptionsGroup
            this.searchOptionsGroup.Controls.Add(this.exportButton);
            this.searchOptionsGroup.Controls.Add(this.searchButton);
            this.searchOptionsGroup.Controls.Add(this.maxSeedsNum);
            this.searchOptionsGroup.Controls.Add(this.maxSeedsLabel);
            this.searchOptionsGroup.Location = new System.Drawing.Point(8, 406);
            this.searchOptionsGroup.Name = "searchOptionsGroup";
            this.searchOptionsGroup.Size = new System.Drawing.Size(330, 100);
            this.searchOptionsGroup.TabIndex = 3;
            this.searchOptionsGroup.TabStop = false;
            this.searchOptionsGroup.Text = "Search Options";

            // maxSeedsLabel
            this.maxSeedsLabel.AutoSize = true;
            this.maxSeedsLabel.Location = new System.Drawing.Point(10, 30);
            this.maxSeedsLabel.Name = "maxSeedsLabel";
            this.maxSeedsLabel.Size = new System.Drawing.Size(72, 15);
            this.maxSeedsLabel.TabIndex = 0;
            this.maxSeedsLabel.Text = "Max Results:";

            // maxSeedsNum
            this.maxSeedsNum.Location = new System.Drawing.Point(90, 28);
            this.maxSeedsNum.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            this.maxSeedsNum.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.maxSeedsNum.Name = "maxSeedsNum";
            this.maxSeedsNum.Size = new System.Drawing.Size(80, 23);
            this.maxSeedsNum.TabIndex = 1;
            this.maxSeedsNum.Value = new decimal(new int[] { 100, 0, 0, 0 });

            // searchButton
            this.searchButton.Location = new System.Drawing.Point(10, 60);
            this.searchButton.Name = "searchButton";
            this.searchButton.Size = new System.Drawing.Size(150, 30);
            this.searchButton.TabIndex = 2;
            this.searchButton.Text = "Search";
            this.searchButton.UseVisualStyleBackColor = true;
            this.searchButton.Click += new System.EventHandler(this.SearchButton_Click);

            // exportButton
            this.exportButton.Location = new System.Drawing.Point(170, 60);
            this.exportButton.Name = "exportButton";
            this.exportButton.Size = new System.Drawing.Size(150, 30);
            this.exportButton.TabIndex = 3;
            this.exportButton.Text = "Export Results";
            this.exportButton.UseVisualStyleBackColor = true;
            this.exportButton.Click += new System.EventHandler(this.ExportButton_Click);

            // resultsPanel
            this.resultsPanel.Controls.Add(this.resultsGrid);
            this.resultsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resultsPanel.Location = new System.Drawing.Point(0, 0);
            this.resultsPanel.Name = "resultsPanel";
            this.resultsPanel.Size = new System.Drawing.Size(546, 578);
            this.resultsPanel.TabIndex = 0;

            // resultsGrid
            this.resultsGrid.AllowUserToAddRows = false;
            this.resultsGrid.AllowUserToDeleteRows = false;
            this.resultsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.resultsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                new System.Windows.Forms.DataGridViewTextBoxColumn { HeaderText = "Seed", Width = 80 },
                new System.Windows.Forms.DataGridViewTextBoxColumn { HeaderText = "Stars", Width = 50 },
                new System.Windows.Forms.DataGridViewTextBoxColumn { HeaderText = "Shiny", Width = 50 },
                new System.Windows.Forms.DataGridViewTextBoxColumn { HeaderText = "Nature", Width = 80 },
                new System.Windows.Forms.DataGridViewTextBoxColumn { HeaderText = "Ability", Width = 100 },
                new System.Windows.Forms.DataGridViewTextBoxColumn { HeaderText = "IVs", Width = 120 },
                new System.Windows.Forms.DataGridViewTextBoxColumn { HeaderText = "Tera", Width = 80 }
            });
            this.resultsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resultsGrid.Location = new System.Drawing.Point(0, 0);
            this.resultsGrid.Name = "resultsGrid";
            this.resultsGrid.ReadOnly = true;
            this.resultsGrid.RowTemplate.Height = 25;
            this.resultsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.resultsGrid.Size = new System.Drawing.Size(546, 578);
            this.resultsGrid.TabIndex = 0;
            this.resultsGrid.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.ResultsGrid_CellDoubleClick);

            // statusStrip
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.statusLabel,
                this.progressBar});
            this.statusStrip.Location = new System.Drawing.Point(0, 578);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(900, 22);
            this.statusStrip.TabIndex = 1;
            this.statusStrip.Text = "statusStrip";

            // statusLabel
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(39, 17);
            this.statusLabel.Text = "Ready";

            // progressBar
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(100, 16);
            this.progressBar.Visible = false;

            // Load nature names
            var natures = Enum.GetNames(typeof(PKHeX.Core.Nature));
            var natureItems = new string[natures.Length + 1];
            natureItems[0] = "Any";
            Array.Copy(natures, 0, natureItems, 1, natures.Length);
            this.natureCombo.Items.AddRange(natureItems);
            this.natureCombo.SelectedIndex = 0;

            this.mainPanel.ResumeLayout(false);
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.searchPanel.ResumeLayout(false);
            this.speciesGroup.ResumeLayout(false);
            this.speciesGroup.PerformLayout();
            this.criteriaGroup.ResumeLayout(false);
            this.criteriaGroup.PerformLayout();
            this.ivGroup.ResumeLayout(false);
            this.ivGroup.PerformLayout();
            this.searchOptionsGroup.ResumeLayout(false);
            this.searchOptionsGroup.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ivHpMin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ivAtkMin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ivDefMin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ivSpaMin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ivSpdMin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.ivSpeMin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.maxSeedsNum)).EndInit();
            this.resultsPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.resultsGrid)).EndInit();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Panel mainPanel;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.Panel searchPanel;
        private System.Windows.Forms.Panel resultsPanel;

        private System.Windows.Forms.GroupBox speciesGroup;
        private System.Windows.Forms.Label speciesLabel;
        private System.Windows.Forms.ComboBox speciesCombo;
        private System.Windows.Forms.Label formLabel;
        private System.Windows.Forms.ComboBox formCombo;
        private System.Windows.Forms.Label encounterLabel;
        private System.Windows.Forms.ComboBox encounterCombo;

        private System.Windows.Forms.GroupBox criteriaGroup;
        private System.Windows.Forms.Label genderLabel;
        private System.Windows.Forms.ComboBox genderCombo;
        private System.Windows.Forms.Label abilityLabel;
        private System.Windows.Forms.ComboBox abilityCombo;
        private System.Windows.Forms.Label natureLabel;
        private System.Windows.Forms.ComboBox natureCombo;
        private System.Windows.Forms.Label shinyLabel;
        private System.Windows.Forms.ComboBox shinyCombo;

        private System.Windows.Forms.GroupBox ivGroup;
        private System.Windows.Forms.Label ivHpLabel;
        private System.Windows.Forms.NumericUpDown ivHpMin;
        private System.Windows.Forms.Label ivAtkLabel;
        private System.Windows.Forms.NumericUpDown ivAtkMin;
        private System.Windows.Forms.Label ivDefLabel;
        private System.Windows.Forms.NumericUpDown ivDefMin;
        private System.Windows.Forms.Label ivSpaLabel;
        private System.Windows.Forms.NumericUpDown ivSpaMin;
        private System.Windows.Forms.Label ivSpdLabel;
        private System.Windows.Forms.NumericUpDown ivSpdMin;
        private System.Windows.Forms.Label ivSpeLabel;
        private System.Windows.Forms.NumericUpDown ivSpeMin;

        private System.Windows.Forms.GroupBox searchOptionsGroup;
        private System.Windows.Forms.Label maxSeedsLabel;
        private System.Windows.Forms.NumericUpDown maxSeedsNum;
        private System.Windows.Forms.Button searchButton;
        private System.Windows.Forms.Button exportButton;

        private System.Windows.Forms.DataGridView resultsGrid;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.ToolStripProgressBar progressBar;
    }
}