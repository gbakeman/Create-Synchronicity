namespace CreateSync.Forms
{
	partial class LanguageForm : System.Windows.Forms.Form
	{

		//Form overrides dispose to clean up the component list.
		[System.Diagnostics.DebuggerNonUserCode()]
		protected override void Dispose(bool disposing)
		{
			try
			{
				if (disposing && components != null)
				{
					components.Dispose();
				}
			}
			finally
			{
				base.Dispose(disposing);
			}
		}

		//Required by the Windows Form Designer

		private System.ComponentModel.IContainer components;
		//NOTE: The following procedure is required by the Windows Form Designer
		//It can be modified using the Windows Form Designer.  
		//Do not modify it using the code editor.
		[System.Diagnostics.DebuggerStepThrough()]
		private void InitializeComponent()
		{
			this.OkBtn = new System.Windows.Forms.Button();
			this.LanguagesList = new System.Windows.Forms.ComboBox();
			this.SuspendLayout();
			//
			//OkBtn
			//
			this.OkBtn.Anchor = (System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Right);
			this.OkBtn.Location = new System.Drawing.Point(232, 12);
			this.OkBtn.Name = "OkBtn";
			this.OkBtn.Size = new System.Drawing.Size(87, 23);
			this.OkBtn.TabIndex = 0;
			this.OkBtn.Text = "Ok";
			this.OkBtn.UseVisualStyleBackColor = true;
			//
			//LanguagesList
			//
			this.LanguagesList.Anchor = (System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right);
			this.LanguagesList.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.LanguagesList.Location = new System.Drawing.Point(12, 13);
			this.LanguagesList.Name = "LanguagesList";
			this.LanguagesList.Size = new System.Drawing.Size(214, 21);
			this.LanguagesList.TabIndex = 4;
			//
			//LanguageForm
			//
			this.AcceptButton = this.OkBtn;
			this.AutoScaleDimensions = new System.Drawing.SizeF(7f, 13f);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(331, 47);
			this.Controls.Add(this.LanguagesList);
			this.Controls.Add(this.OkBtn);
			this.Font = new System.Drawing.Font("Verdana", 8.25f);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "LanguageForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Please select your language";
			this.ResumeLayout(false);

		}
		internal System.Windows.Forms.Button OkBtn;
		internal System.Windows.Forms.ComboBox LanguagesList;
	}
}