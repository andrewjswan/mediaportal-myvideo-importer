// Type: MyVideoImporter.CongigForm
// Assembly: MyVideoImporter, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null

using System;
using System.Windows.Forms;

namespace MyVideoImporter
{
  public class ConfigForm : Form
  {
    private GroupBox grpFetcher;
    private CheckBox chkAutoApproveIfNearest;
    private CheckBox chkApproveIfOne;
    private Button btnOK;
    private NumericUpDown udFactor;
    private FolderBrowserDialog fldrDialog;

    public ConfigForm()
    {
      this.InitializeComponent();

      Translation.Init();

      this.chkApproveIfOne.Text = Translation.PrefsAutoIfOne;
      this.chkAutoApproveIfNearest.Text = Translation.PrefsAutoIfNearest;

      this.Text = this.Text + " v" + Utils.GetAllVersionNumber();
      this.LoadSettings();
    }

    private void btnOK_Click(object sender, EventArgs e)
    {
      this.SaveSettings();
      this.Close();
    }

    private void LoadSettings()
    {
      Utils.LoadSettings();

      this.chkApproveIfOne.Checked = Utils.ApproveIfOne;
      this.chkAutoApproveIfNearest.Checked = Utils.ApproveForNearest;
      this.udFactor.Value = Utils.NearestFactor;
    }

    private void SaveSettings()
    {
      Utils.ApproveIfOne = this.chkApproveIfOne.Checked;
      Utils.ApproveForNearest = this.chkAutoApproveIfNearest.Checked;
      Utils.NearestFactor = (int)this.udFactor.Value;

      Utils.SaveSettings();
    }

    protected override void Dispose(bool disposing)
    {
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigForm));
      this.grpFetcher = new System.Windows.Forms.GroupBox();
      this.udFactor = new System.Windows.Forms.NumericUpDown();
      this.chkAutoApproveIfNearest = new System.Windows.Forms.CheckBox();
      this.chkApproveIfOne = new System.Windows.Forms.CheckBox();
      this.btnOK = new System.Windows.Forms.Button();
      this.fldrDialog = new System.Windows.Forms.FolderBrowserDialog();
      this.grpFetcher.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.udFactor)).BeginInit();
      this.SuspendLayout();
      // 
      // grpFetcher
      // 
      this.grpFetcher.Controls.Add(this.udFactor);
      this.grpFetcher.Controls.Add(this.chkAutoApproveIfNearest);
      this.grpFetcher.Controls.Add(this.chkApproveIfOne);
      this.grpFetcher.Location = new System.Drawing.Point(24, 26);
      this.grpFetcher.Name = "grpFetcher";
      this.grpFetcher.Size = new System.Drawing.Size(419, 75);
      this.grpFetcher.TabIndex = 0;
      this.grpFetcher.TabStop = false;
      this.grpFetcher.Text = "Fetcher";
      // 
      // udFactor
      // 
      this.udFactor.Location = new System.Drawing.Point(344, 43);
      this.udFactor.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
      this.udFactor.Name = "udFactor";
      this.udFactor.Size = new System.Drawing.Size(68, 20);
      this.udFactor.TabIndex = 2;
      // 
      // chkAutoApproveIfNearest
      // 
      this.chkAutoApproveIfNearest.AutoSize = true;
      this.chkAutoApproveIfNearest.Checked = true;
      this.chkAutoApproveIfNearest.CheckState = System.Windows.Forms.CheckState.Checked;
      this.chkAutoApproveIfNearest.Location = new System.Drawing.Point(16, 44);
      this.chkAutoApproveIfNearest.Name = "chkAutoApproveIfNearest";
      this.chkAutoApproveIfNearest.Size = new System.Drawing.Size(255, 17);
      this.chkAutoApproveIfNearest.TabIndex = 1;
      this.chkAutoApproveIfNearest.Text = "Automatically select the nearest match, factor <=";
      this.chkAutoApproveIfNearest.UseVisualStyleBackColor = true;
      // 
      // chkApproveIfOne
      // 
      this.chkApproveIfOne.AutoSize = true;
      this.chkApproveIfOne.Checked = true;
      this.chkApproveIfOne.CheckState = System.Windows.Forms.CheckState.Checked;
      this.chkApproveIfOne.Location = new System.Drawing.Point(16, 20);
      this.chkApproveIfOne.Name = "chkApproveIfOne";
      this.chkApproveIfOne.Size = new System.Drawing.Size(178, 17);
      this.chkApproveIfOne.TabIndex = 0;
      this.chkApproveIfOne.Text = "Automatically select if one found";
      this.chkApproveIfOne.UseVisualStyleBackColor = true;
      // 
      // btnOK
      // 
      this.btnOK.Location = new System.Drawing.Point(368, 107);
      this.btnOK.Name = "btnOK";
      this.btnOK.Size = new System.Drawing.Size(75, 23);
      this.btnOK.TabIndex = 2;
      this.btnOK.Text = "OK";
      this.btnOK.UseVisualStyleBackColor = true;
      this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
      // 
      // ConfigForm
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(455, 138);
      this.Controls.Add(this.btnOK);
      this.Controls.Add(this.grpFetcher);
      this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
      this.Name = "ConfigForm";
      this.Text = "MyVideoImporter";
      this.grpFetcher.ResumeLayout(false);
      this.grpFetcher.PerformLayout();
      ((System.ComponentModel.ISupportInitialize)(this.udFactor)).EndInit();
      this.ResumeLayout(false);

    }
  }
}
