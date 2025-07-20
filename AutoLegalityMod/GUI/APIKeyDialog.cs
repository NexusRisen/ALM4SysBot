using System.Windows.Forms;

namespace AutoModPlugins.GUI;

public class APIKeyDialog : Form
{
    private TextBox TB_ApiKey;
    private Button B_OK;
    private Button B_Cancel;
    private Label L_Instructions;
    private LinkLabel LL_GetKey;

    public string ApiKey => TB_ApiKey.Text;

    public APIKeyDialog() => InitializeComponent();

    private void InitializeComponent()
    {
        this.TB_ApiKey = new TextBox();
        this.B_OK = new Button();
        this.B_Cancel = new Button();
        this.L_Instructions = new Label();
        this.LL_GetKey = new LinkLabel();
        this.SuspendLayout();

        // L_Instructions
        this.L_Instructions.AutoSize = true;
        this.L_Instructions.Location = new System.Drawing.Point(12, 9);
        this.L_Instructions.MaximumSize = new System.Drawing.Size(360, 0);
        this.L_Instructions.Name = "L_Instructions";
        this.L_Instructions.Size = new System.Drawing.Size(350, 30);
        this.L_Instructions.TabIndex = 0;
        this.L_Instructions.Text = "Please enter your OpenAI API key. You can obtain one from the OpenAI platform.";

        // LL_GetKey
        this.LL_GetKey.AutoSize = true;
        this.LL_GetKey.Location = new System.Drawing.Point(12, 45);
        this.LL_GetKey.Name = "LL_GetKey";
        this.LL_GetKey.Size = new System.Drawing.Size(150, 15);
        this.LL_GetKey.TabIndex = 1;
        this.LL_GetKey.TabStop = true;
        this.LL_GetKey.Text = "Get API Key from OpenAI";
        this.LL_GetKey.LinkClicked += (s, e) => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "https://platform.openai.com/api-keys",
            UseShellExecute = true
        });

        // TB_ApiKey
        this.TB_ApiKey.Location = new System.Drawing.Point(12, 70);
        this.TB_ApiKey.Name = "TB_ApiKey";
        this.TB_ApiKey.PasswordChar = '*';
        this.TB_ApiKey.Size = new System.Drawing.Size(360, 23);
        this.TB_ApiKey.TabIndex = 2;
        this.TB_ApiKey.PlaceholderText = "sk-...";

        // B_OK
        this.B_OK.Location = new System.Drawing.Point(216, 110);
        this.B_OK.Name = "B_OK";
        this.B_OK.Size = new System.Drawing.Size(75, 30);
        this.B_OK.TabIndex = 3;
        this.B_OK.Text = "OK";
        this.B_OK.UseVisualStyleBackColor = true;
        this.B_OK.Click += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(TB_ApiKey.Text))
            {
                MessageBox.Show("Please enter an API key.", "API Key Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            this.DialogResult = DialogResult.OK;
            this.Close();
        };

        // B_Cancel
        this.B_Cancel.Location = new System.Drawing.Point(297, 110);
        this.B_Cancel.Name = "B_Cancel";
        this.B_Cancel.Size = new System.Drawing.Size(75, 30);
        this.B_Cancel.TabIndex = 4;
        this.B_Cancel.Text = "Cancel";
        this.B_Cancel.UseVisualStyleBackColor = true;
        this.B_Cancel.Click += (s, e) =>
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        };

        // APIKeyDialog
        this.AcceptButton = this.B_OK;
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.CancelButton = this.B_Cancel;
        this.ClientSize = new System.Drawing.Size(384, 152);
        this.Controls.Add(this.B_Cancel);
        this.Controls.Add(this.B_OK);
        this.Controls.Add(this.TB_ApiKey);
        this.Controls.Add(this.LL_GetKey);
        this.Controls.Add(this.L_Instructions);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "APIKeyDialog";
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "Enter OpenAI API Key";
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}