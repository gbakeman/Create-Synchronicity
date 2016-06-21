namespace CreateSync.Forms
{
	partial class SynchronizeForm : System.Windows.Forms.Form
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SynchronizeForm));
			this.MainLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.Step3LayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.Step3_ProgressLayout = new System.Windows.Forms.TableLayoutPanel();
			this.Step3StatusLabel = new System.Windows.Forms.Label();
			this.Step3ProgressBar = new System.Windows.Forms.ProgressBar();
			this.Step3Label = new System.Windows.Forms.Label();
			this.Step2LayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.Step2ProgressLayout = new System.Windows.Forms.TableLayoutPanel();
			this.Step2StatusLabel = new System.Windows.Forms.Label();
			this.Step2ProgressBar = new System.Windows.Forms.ProgressBar();
			this.Step2Label = new System.Windows.Forms.Label();
			this.Step1LayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.Step1ProgressLayout = new System.Windows.Forms.TableLayoutPanel();
			this.Step1StatusLabel = new System.Windows.Forms.Label();
			this.Step1ProgressBar = new System.Windows.Forms.ProgressBar();
			this.Step1Label = new System.Windows.Forms.Label();
			this.ButtonsLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.SyncBtn = new System.Windows.Forms.Button();
			this.StopBtn = new System.Windows.Forms.Button();
			this.StatisticsPanel = new System.Windows.Forms.TableLayoutPanel();
			this.FilesCreatedLabel = new System.Windows.Forms.Label();
			this.FilesCreated = new System.Windows.Forms.Label();
			this.FilesDeletedLabel = new System.Windows.Forms.Label();
			this.FilesDeleted = new System.Windows.Forms.Label();
			this.FoldersCreatedLabel = new System.Windows.Forms.Label();
			this.FoldersCreated = new System.Windows.Forms.Label();
			this.FoldersDeletedLabel = new System.Windows.Forms.Label();
			this.FoldersDeleted = new System.Windows.Forms.Label();
			this.ElapsedTimeLabel = new System.Windows.Forms.Label();
			this.ElapsedTime = new System.Windows.Forms.Label();
			this.SpeedLabel = new System.Windows.Forms.Label();
			this.Speed = new System.Windows.Forms.Label();
			this.DoneLabel = new System.Windows.Forms.Label();
			this.Done = new System.Windows.Forms.Label();
			this.PreviewList = new System.Windows.Forms.ListView();
			this.TypeColumn = (System.Windows.Forms.ColumnHeader)new System.Windows.Forms.ColumnHeader();
			this.ActionColumn = (System.Windows.Forms.ColumnHeader)new System.Windows.Forms.ColumnHeader();
			this.DirectionColumn = (System.Windows.Forms.ColumnHeader)new System.Windows.Forms.ColumnHeader();
			this.PathColumn = (System.Windows.Forms.ColumnHeader)new System.Windows.Forms.ColumnHeader();
			this.SyncingIcons = new System.Windows.Forms.ImageList(this.components);
			this.SyncingTimer = new System.Windows.Forms.Timer(this.components);
			this.MainLayoutPanel.SuspendLayout();
			this.Step3LayoutPanel.SuspendLayout();
			this.Step3_ProgressLayout.SuspendLayout();
			this.Step2LayoutPanel.SuspendLayout();
			this.Step2ProgressLayout.SuspendLayout();
			this.Step1LayoutPanel.SuspendLayout();
			this.Step1ProgressLayout.SuspendLayout();
			this.ButtonsLayoutPanel.SuspendLayout();
			this.StatisticsPanel.SuspendLayout();
			this.SuspendLayout();
			//
			//MainLayoutPanel
			//
			this.MainLayoutPanel.Anchor = (System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right);
			this.MainLayoutPanel.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
			this.MainLayoutPanel.ColumnCount = 1;
			this.MainLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.MainLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 23f));
			this.MainLayoutPanel.Controls.Add(this.Step3LayoutPanel, 0, 2);
			this.MainLayoutPanel.Controls.Add(this.Step2LayoutPanel, 0, 1);
			this.MainLayoutPanel.Controls.Add(this.Step1LayoutPanel, 0, 0);
			this.MainLayoutPanel.Location = new System.Drawing.Point(12, 12);
			this.MainLayoutPanel.MaximumSize = new System.Drawing.Size(65536, 400);
			this.MainLayoutPanel.Name = "MainLayoutPanel";
			this.MainLayoutPanel.RowCount = 3;
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333f));
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333f));
			this.MainLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333f));
			this.MainLayoutPanel.Size = new System.Drawing.Size(601, 268);
			this.MainLayoutPanel.TabIndex = 0;
			//
			//Step3LayoutPanel
			//
			this.Step3LayoutPanel.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Inset;
			this.Step3LayoutPanel.ColumnCount = 1;
			this.Step3LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.Step3LayoutPanel.Controls.Add(this.Step3_ProgressLayout, 0, 1);
			this.Step3LayoutPanel.Controls.Add(this.Step3Label, 0, 0);
			this.Step3LayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Step3LayoutPanel.Location = new System.Drawing.Point(4, 182);
			this.Step3LayoutPanel.Name = "Step3LayoutPanel";
			this.Step3LayoutPanel.RowCount = 2;
			this.Step3LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.Step3LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.Step3LayoutPanel.Size = new System.Drawing.Size(593, 82);
			this.Step3LayoutPanel.TabIndex = 2;
			//
			//Step3_ProgressLayout
			//
			this.Step3_ProgressLayout.ColumnCount = 1;
			this.Step3_ProgressLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.Step3_ProgressLayout.Controls.Add(this.Step3StatusLabel, 0, 0);
			this.Step3_ProgressLayout.Controls.Add(this.Step3ProgressBar, 0, 1);
			this.Step3_ProgressLayout.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Step3_ProgressLayout.Location = new System.Drawing.Point(5, 30);
			this.Step3_ProgressLayout.Name = "Step3_ProgressLayout";
			this.Step3_ProgressLayout.RowCount = 2;
			this.Step3_ProgressLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.Step3_ProgressLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.Step3_ProgressLayout.Size = new System.Drawing.Size(583, 47);
			this.Step3_ProgressLayout.TabIndex = 0;
			//
			//Step3StatusLabel
			//
			this.Step3StatusLabel.AutoEllipsis = true;
			this.Step3StatusLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Step3StatusLabel.Location = new System.Drawing.Point(3, 0);
			this.Step3StatusLabel.Name = "Step3StatusLabel";
			this.Step3StatusLabel.Size = new System.Drawing.Size(577, 14);
			this.Step3StatusLabel.TabIndex = 2;
			this.Step3StatusLabel.Text = "\\WAITING";
			this.Step3StatusLabel.UseMnemonic = false;
			//
			//Step3ProgressBar
			//
			this.Step3ProgressBar.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Step3ProgressBar.Location = new System.Drawing.Point(3, 17);
			this.Step3ProgressBar.Name = "Step3ProgressBar";
			this.Step3ProgressBar.Size = new System.Drawing.Size(577, 27);
			this.Step3ProgressBar.TabIndex = 3;
			//
			//Step3Label
			//
			this.Step3Label.AutoSize = true;
			this.Step3Label.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Step3Label.Location = new System.Drawing.Point(5, 2);
			this.Step3Label.Name = "Step3Label";
			this.Step3Label.Padding = new System.Windows.Forms.Padding(0, 5, 0, 5);
			this.Step3Label.Size = new System.Drawing.Size(583, 23);
			this.Step3Label.TabIndex = 1;
			this.Step3Label.Text = "\\STEP_3";
			//
			//Step2LayoutPanel
			//
			this.Step2LayoutPanel.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Inset;
			this.Step2LayoutPanel.ColumnCount = 1;
			this.Step2LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.Step2LayoutPanel.Controls.Add(this.Step2ProgressLayout, 0, 1);
			this.Step2LayoutPanel.Controls.Add(this.Step2Label, 0, 0);
			this.Step2LayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Step2LayoutPanel.Location = new System.Drawing.Point(4, 93);
			this.Step2LayoutPanel.Name = "Step2LayoutPanel";
			this.Step2LayoutPanel.RowCount = 2;
			this.Step2LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.Step2LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.Step2LayoutPanel.Size = new System.Drawing.Size(593, 82);
			this.Step2LayoutPanel.TabIndex = 1;
			//
			//Step2ProgressLayout
			//
			this.Step2ProgressLayout.ColumnCount = 1;
			this.Step2ProgressLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.Step2ProgressLayout.Controls.Add(this.Step2StatusLabel, 0, 0);
			this.Step2ProgressLayout.Controls.Add(this.Step2ProgressBar, 0, 1);
			this.Step2ProgressLayout.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Step2ProgressLayout.Location = new System.Drawing.Point(5, 30);
			this.Step2ProgressLayout.Name = "Step2ProgressLayout";
			this.Step2ProgressLayout.RowCount = 2;
			this.Step2ProgressLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.Step2ProgressLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.Step2ProgressLayout.Size = new System.Drawing.Size(583, 47);
			this.Step2ProgressLayout.TabIndex = 0;
			//
			//Step2StatusLabel
			//
			this.Step2StatusLabel.AutoEllipsis = true;
			this.Step2StatusLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Step2StatusLabel.Location = new System.Drawing.Point(3, 0);
			this.Step2StatusLabel.Name = "Step2StatusLabel";
			this.Step2StatusLabel.Size = new System.Drawing.Size(577, 14);
			this.Step2StatusLabel.TabIndex = 2;
			this.Step2StatusLabel.Text = "\\WAITING";
			this.Step2StatusLabel.UseMnemonic = false;
			//
			//Step2ProgressBar
			//
			this.Step2ProgressBar.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Step2ProgressBar.Location = new System.Drawing.Point(3, 17);
			this.Step2ProgressBar.Name = "Step2ProgressBar";
			this.Step2ProgressBar.Size = new System.Drawing.Size(577, 27);
			this.Step2ProgressBar.TabIndex = 3;
			//
			//Step2Label
			//
			this.Step2Label.AutoSize = true;
			this.Step2Label.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Step2Label.Location = new System.Drawing.Point(5, 2);
			this.Step2Label.Name = "Step2Label";
			this.Step2Label.Padding = new System.Windows.Forms.Padding(0, 5, 0, 5);
			this.Step2Label.Size = new System.Drawing.Size(583, 23);
			this.Step2Label.TabIndex = 1;
			this.Step2Label.Text = "\\STEP_2";
			//
			//Step1LayoutPanel
			//
			this.Step1LayoutPanel.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Inset;
			this.Step1LayoutPanel.ColumnCount = 1;
			this.Step1LayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.Step1LayoutPanel.Controls.Add(this.Step1ProgressLayout, 0, 1);
			this.Step1LayoutPanel.Controls.Add(this.Step1Label, 0, 0);
			this.Step1LayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Step1LayoutPanel.Location = new System.Drawing.Point(4, 4);
			this.Step1LayoutPanel.Name = "Step1LayoutPanel";
			this.Step1LayoutPanel.RowCount = 2;
			this.Step1LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.Step1LayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.Step1LayoutPanel.Size = new System.Drawing.Size(593, 82);
			this.Step1LayoutPanel.TabIndex = 0;
			//
			//Step1ProgressLayout
			//
			this.Step1ProgressLayout.ColumnCount = 1;
			this.Step1ProgressLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.Step1ProgressLayout.Controls.Add(this.Step1StatusLabel, 0, 0);
			this.Step1ProgressLayout.Controls.Add(this.Step1ProgressBar, 0, 1);
			this.Step1ProgressLayout.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Step1ProgressLayout.Location = new System.Drawing.Point(5, 30);
			this.Step1ProgressLayout.Name = "Step1ProgressLayout";
			this.Step1ProgressLayout.RowCount = 2;
			this.Step1ProgressLayout.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.Step1ProgressLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.Step1ProgressLayout.Size = new System.Drawing.Size(583, 47);
			this.Step1ProgressLayout.TabIndex = 0;
			//
			//Step1StatusLabel
			//
			this.Step1StatusLabel.AutoEllipsis = true;
			this.Step1StatusLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Step1StatusLabel.Location = new System.Drawing.Point(3, 0);
			this.Step1StatusLabel.Name = "Step1StatusLabel";
			this.Step1StatusLabel.Size = new System.Drawing.Size(577, 14);
			this.Step1StatusLabel.TabIndex = 2;
			this.Step1StatusLabel.Text = "\\WAITING";
			this.Step1StatusLabel.UseMnemonic = false;
			//
			//Step1ProgressBar
			//
			this.Step1ProgressBar.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Step1ProgressBar.Location = new System.Drawing.Point(3, 17);
			this.Step1ProgressBar.MarqueeAnimationSpeed = 50;
			this.Step1ProgressBar.Name = "Step1ProgressBar";
			this.Step1ProgressBar.Size = new System.Drawing.Size(577, 27);
			this.Step1ProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
			this.Step1ProgressBar.TabIndex = 3;
			this.Step1ProgressBar.Value = 100;
			//
			//Step1Label
			//
			this.Step1Label.AutoSize = true;
			this.Step1Label.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Step1Label.Location = new System.Drawing.Point(5, 2);
			this.Step1Label.Name = "Step1Label";
			this.Step1Label.Padding = new System.Windows.Forms.Padding(0, 5, 0, 5);
			this.Step1Label.Size = new System.Drawing.Size(583, 23);
			this.Step1Label.TabIndex = 1;
			this.Step1Label.Text = "\\STEP_1";
			//
			//ButtonsLayoutPanel
			//
			this.ButtonsLayoutPanel.Anchor = (System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
			this.ButtonsLayoutPanel.ColumnCount = 1;
			this.ButtonsLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100f));
			this.ButtonsLayoutPanel.Controls.Add(this.SyncBtn, 0, 0);
			this.ButtonsLayoutPanel.Controls.Add(this.StopBtn, 0, 1);
			this.ButtonsLayoutPanel.Location = new System.Drawing.Point(489, 286);
			this.ButtonsLayoutPanel.Name = "ButtonsLayoutPanel";
			this.ButtonsLayoutPanel.RowCount = 2;
			this.ButtonsLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50f));
			this.ButtonsLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50f));
			this.ButtonsLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20f));
			this.ButtonsLayoutPanel.Size = new System.Drawing.Size(124, 69);
			this.ButtonsLayoutPanel.TabIndex = 1;
			//
			//SyncBtn
			//
			this.SyncBtn.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SyncBtn.Location = new System.Drawing.Point(3, 3);
			this.SyncBtn.Name = "SyncBtn";
			this.SyncBtn.Size = new System.Drawing.Size(118, 28);
			this.SyncBtn.TabIndex = 4;
			this.SyncBtn.Text = "\\SYNC";
			this.SyncBtn.UseVisualStyleBackColor = true;
			//
			//StopBtn
			//
			this.StopBtn.Dock = System.Windows.Forms.DockStyle.Fill;
			this.StopBtn.Location = new System.Drawing.Point(3, 37);
			this.StopBtn.Name = "StopBtn";
			this.StopBtn.Size = new System.Drawing.Size(118, 29);
			this.StopBtn.TabIndex = 1;
			this.StopBtn.Tag = "\\CANCEL_CLOSE";
			this.StopBtn.Text = "\\CANCEL";
			this.StopBtn.UseVisualStyleBackColor = true;
			//
			//StatisticsPanel
			//
			this.StatisticsPanel.Anchor = (System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right);
			this.StatisticsPanel.ColumnCount = 4;
			this.StatisticsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.StatisticsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50f));
			this.StatisticsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.StatisticsPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50f));
			this.StatisticsPanel.Controls.Add(this.FilesCreatedLabel, 0, 0);
			this.StatisticsPanel.Controls.Add(this.FilesCreated, 1, 0);
			this.StatisticsPanel.Controls.Add(this.FilesDeletedLabel, 0, 1);
			this.StatisticsPanel.Controls.Add(this.FilesDeleted, 1, 1);
			this.StatisticsPanel.Controls.Add(this.FoldersCreatedLabel, 0, 2);
			this.StatisticsPanel.Controls.Add(this.FoldersCreated, 1, 2);
			this.StatisticsPanel.Controls.Add(this.FoldersDeletedLabel, 0, 3);
			this.StatisticsPanel.Controls.Add(this.FoldersDeleted, 1, 3);
			this.StatisticsPanel.Controls.Add(this.ElapsedTimeLabel, 2, 0);
			this.StatisticsPanel.Controls.Add(this.ElapsedTime, 3, 0);
			this.StatisticsPanel.Controls.Add(this.SpeedLabel, 2, 1);
			this.StatisticsPanel.Controls.Add(this.Speed, 3, 1);
			this.StatisticsPanel.Controls.Add(this.DoneLabel, 2, 3);
			this.StatisticsPanel.Controls.Add(this.Done, 3, 3);
			this.StatisticsPanel.Location = new System.Drawing.Point(12, 286);
			this.StatisticsPanel.Name = "StatisticsPanel";
			this.StatisticsPanel.RowCount = 4;
			this.StatisticsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25f));
			this.StatisticsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25f));
			this.StatisticsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25f));
			this.StatisticsPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25f));
			this.StatisticsPanel.Size = new System.Drawing.Size(471, 69);
			this.StatisticsPanel.TabIndex = 2;
			//
			//FilesCreatedLabel
			//
			this.FilesCreatedLabel.AutoSize = true;
			this.FilesCreatedLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.FilesCreatedLabel.Location = new System.Drawing.Point(3, 0);
			this.FilesCreatedLabel.Name = "FilesCreatedLabel";
			this.FilesCreatedLabel.Size = new System.Drawing.Size(127, 17);
			this.FilesCreatedLabel.TabIndex = 10;
			this.FilesCreatedLabel.Text = "\\FILES_CREATED";
			this.FilesCreatedLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//FilesCreated
			//
			this.FilesCreated.AutoEllipsis = true;
			this.FilesCreated.Dock = System.Windows.Forms.DockStyle.Fill;
			this.FilesCreated.Location = new System.Drawing.Point(136, 0);
			this.FilesCreated.Name = "FilesCreated";
			this.FilesCreated.Size = new System.Drawing.Size(128, 17);
			this.FilesCreated.TabIndex = 9;
			this.FilesCreated.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//FilesDeletedLabel
			//
			this.FilesDeletedLabel.AutoSize = true;
			this.FilesDeletedLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.FilesDeletedLabel.Location = new System.Drawing.Point(3, 17);
			this.FilesDeletedLabel.Name = "FilesDeletedLabel";
			this.FilesDeletedLabel.Size = new System.Drawing.Size(127, 17);
			this.FilesDeletedLabel.TabIndex = 12;
			this.FilesDeletedLabel.Text = "\\FILES_DELETED";
			this.FilesDeletedLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//FilesDeleted
			//
			this.FilesDeleted.AutoEllipsis = true;
			this.FilesDeleted.Dock = System.Windows.Forms.DockStyle.Fill;
			this.FilesDeleted.Location = new System.Drawing.Point(136, 17);
			this.FilesDeleted.Name = "FilesDeleted";
			this.FilesDeleted.Size = new System.Drawing.Size(128, 17);
			this.FilesDeleted.TabIndex = 14;
			this.FilesDeleted.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//FoldersCreatedLabel
			//
			this.FoldersCreatedLabel.AutoSize = true;
			this.FoldersCreatedLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.FoldersCreatedLabel.Location = new System.Drawing.Point(3, 34);
			this.FoldersCreatedLabel.Name = "FoldersCreatedLabel";
			this.FoldersCreatedLabel.Size = new System.Drawing.Size(127, 17);
			this.FoldersCreatedLabel.TabIndex = 8;
			this.FoldersCreatedLabel.Text = "\\FOLDERS_CREATED";
			this.FoldersCreatedLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//FoldersCreated
			//
			this.FoldersCreated.AutoEllipsis = true;
			this.FoldersCreated.Dock = System.Windows.Forms.DockStyle.Fill;
			this.FoldersCreated.Location = new System.Drawing.Point(136, 34);
			this.FoldersCreated.Name = "FoldersCreated";
			this.FoldersCreated.Size = new System.Drawing.Size(128, 17);
			this.FoldersCreated.TabIndex = 11;
			this.FoldersCreated.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//FoldersDeletedLabel
			//
			this.FoldersDeletedLabel.AutoSize = true;
			this.FoldersDeletedLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.FoldersDeletedLabel.Location = new System.Drawing.Point(3, 51);
			this.FoldersDeletedLabel.Name = "FoldersDeletedLabel";
			this.FoldersDeletedLabel.Size = new System.Drawing.Size(127, 18);
			this.FoldersDeletedLabel.TabIndex = 13;
			this.FoldersDeletedLabel.Text = "\\FOLDERS_DELETED";
			this.FoldersDeletedLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//FoldersDeleted
			//
			this.FoldersDeleted.AutoEllipsis = true;
			this.FoldersDeleted.Dock = System.Windows.Forms.DockStyle.Fill;
			this.FoldersDeleted.Location = new System.Drawing.Point(136, 51);
			this.FoldersDeleted.Name = "FoldersDeleted";
			this.FoldersDeleted.Size = new System.Drawing.Size(128, 18);
			this.FoldersDeleted.TabIndex = 15;
			this.FoldersDeleted.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//ElapsedTimeLabel
			//
			this.ElapsedTimeLabel.AutoSize = true;
			this.ElapsedTimeLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ElapsedTimeLabel.Location = new System.Drawing.Point(270, 0);
			this.ElapsedTimeLabel.Name = "ElapsedTimeLabel";
			this.ElapsedTimeLabel.Size = new System.Drawing.Size(64, 17);
			this.ElapsedTimeLabel.TabIndex = 0;
			this.ElapsedTimeLabel.Text = "\\ELAPSED";
			this.ElapsedTimeLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//ElapsedTime
			//
			this.ElapsedTime.AutoEllipsis = true;
			this.ElapsedTime.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ElapsedTime.Location = new System.Drawing.Point(340, 0);
			this.ElapsedTime.Name = "ElapsedTime";
			this.ElapsedTime.Size = new System.Drawing.Size(128, 17);
			this.ElapsedTime.TabIndex = 1;
			this.ElapsedTime.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//SpeedLabel
			//
			this.SpeedLabel.AutoSize = true;
			this.SpeedLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SpeedLabel.Location = new System.Drawing.Point(270, 17);
			this.SpeedLabel.Name = "SpeedLabel";
			this.SpeedLabel.Size = new System.Drawing.Size(64, 17);
			this.SpeedLabel.TabIndex = 2;
			this.SpeedLabel.Text = "\\SPEED";
			this.SpeedLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//Speed
			//
			this.Speed.AutoEllipsis = true;
			this.Speed.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Speed.Location = new System.Drawing.Point(340, 17);
			this.Speed.Name = "Speed";
			this.Speed.Size = new System.Drawing.Size(128, 17);
			this.Speed.TabIndex = 3;
			this.Speed.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//DoneLabel
			//
			this.DoneLabel.AutoSize = true;
			this.DoneLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.DoneLabel.Location = new System.Drawing.Point(270, 51);
			this.DoneLabel.Name = "DoneLabel";
			this.DoneLabel.Size = new System.Drawing.Size(64, 18);
			this.DoneLabel.TabIndex = 4;
			this.DoneLabel.Text = "\\DONE";
			this.DoneLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//Done
			//
			this.Done.AutoEllipsis = true;
			this.Done.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Done.Location = new System.Drawing.Point(340, 51);
			this.Done.Name = "Done";
			this.Done.Size = new System.Drawing.Size(128, 18);
			this.Done.TabIndex = 5;
			this.Done.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//PreviewList
			//
			this.PreviewList.Anchor = (System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right);
			this.PreviewList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.TypeColumn,
			this.ActionColumn,
			this.DirectionColumn,
			this.PathColumn
		});
			this.PreviewList.FullRowSelect = true;
			this.PreviewList.Location = new System.Drawing.Point(12, 12);
			this.PreviewList.MultiSelect = false;
			this.PreviewList.Name = "PreviewList";
			this.PreviewList.Size = new System.Drawing.Size(601, 268);
			this.PreviewList.SmallImageList = this.SyncingIcons;
			this.PreviewList.TabIndex = 4;
			this.PreviewList.UseCompatibleStateImageBehavior = false;
			this.PreviewList.View = System.Windows.Forms.View.Details;
			this.PreviewList.Visible = false;
			//
			//TypeColumn
			//
			this.TypeColumn.Text = "\\TYPE";
			this.TypeColumn.Width = 80;
			//
			//ActionColumn
			//
			this.ActionColumn.Text = "\\ACTION";
			this.ActionColumn.Width = 80;
			//
			//DirectionColumn
			//
			this.DirectionColumn.Text = "\\DIRECTION";
			this.DirectionColumn.Width = 80;
			//
			//PathColumn
			//
			this.PathColumn.Text = "\\PATH";
			this.PathColumn.Width = 230;
			//
			//SyncingIcons
			//
			this.SyncingIcons.ImageStream = (System.Windows.Forms.ImageListStreamer)resources.GetObject("SyncingIcons.ImageStream");
			this.SyncingIcons.TransparentColor = System.Drawing.Color.Transparent;
			this.SyncingIcons.Images.SetKeyName(0, "go-next-upd.png");
			this.SyncingIcons.Images.SetKeyName(1, "go-next.png");
			this.SyncingIcons.Images.SetKeyName(2, "go-previous-upd.png");
			this.SyncingIcons.Images.SetKeyName(3, "go-previous.png");
			this.SyncingIcons.Images.SetKeyName(4, "list-remove.png");
			this.SyncingIcons.Images.SetKeyName(5, "folder-new.png");
			this.SyncingIcons.Images.SetKeyName(6, "folder.png");
			this.SyncingIcons.Images.SetKeyName(7, "delete-folder.png");
			this.SyncingIcons.Images.SetKeyName(8, "process-stop.png");
			//
			//SyncingTimer
			//
			this.SyncingTimer.Enabled = true;
			this.SyncingTimer.Interval = 50;
			//
			//SynchronizeForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(7f, 13f);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(625, 367);
			this.Controls.Add(this.PreviewList);
			this.Controls.Add(this.StatisticsPanel);
			this.Controls.Add(this.ButtonsLayoutPanel);
			this.Controls.Add(this.MainLayoutPanel);
			this.Font = new System.Drawing.Font("Verdana", 8.25f);
			this.KeyPreview = true;
			this.Name = "SynchronizeForm";
			this.Text = "\\SYNCHRONIZING";
			this.MainLayoutPanel.ResumeLayout(false);
			this.Step3LayoutPanel.ResumeLayout(false);
			this.Step3LayoutPanel.PerformLayout();
			this.Step3_ProgressLayout.ResumeLayout(false);
			this.Step2LayoutPanel.ResumeLayout(false);
			this.Step2LayoutPanel.PerformLayout();
			this.Step2ProgressLayout.ResumeLayout(false);
			this.Step1LayoutPanel.ResumeLayout(false);
			this.Step1LayoutPanel.PerformLayout();
			this.Step1ProgressLayout.ResumeLayout(false);
			this.ButtonsLayoutPanel.ResumeLayout(false);
			this.StatisticsPanel.ResumeLayout(false);
			this.StatisticsPanel.PerformLayout();
			this.ResumeLayout(false);

		}
		internal System.Windows.Forms.TableLayoutPanel MainLayoutPanel;
		internal System.Windows.Forms.TableLayoutPanel Step1LayoutPanel;
		internal System.Windows.Forms.TableLayoutPanel Step1ProgressLayout;
		internal System.Windows.Forms.TableLayoutPanel Step3LayoutPanel;
		internal System.Windows.Forms.TableLayoutPanel Step3_ProgressLayout;
		internal System.Windows.Forms.Label Step3StatusLabel;
		internal System.Windows.Forms.ProgressBar Step3ProgressBar;
		internal System.Windows.Forms.Label Step3Label;
		internal System.Windows.Forms.TableLayoutPanel Step2LayoutPanel;
		internal System.Windows.Forms.TableLayoutPanel Step2ProgressLayout;
		internal System.Windows.Forms.Label Step2StatusLabel;
		internal System.Windows.Forms.ProgressBar Step2ProgressBar;
		internal System.Windows.Forms.Label Step2Label;
		internal System.Windows.Forms.Label Step1StatusLabel;
		internal System.Windows.Forms.ProgressBar Step1ProgressBar;
		internal System.Windows.Forms.Label Step1Label;
		internal System.Windows.Forms.TableLayoutPanel ButtonsLayoutPanel;
		internal System.Windows.Forms.Button StopBtn;
		internal System.Windows.Forms.TableLayoutPanel StatisticsPanel;
		internal System.Windows.Forms.Label ElapsedTimeLabel;
		internal System.Windows.Forms.Label ElapsedTime;
		internal System.Windows.Forms.Button SyncBtn;
		internal System.Windows.Forms.ListView PreviewList;
		internal System.Windows.Forms.ColumnHeader PathColumn;
		internal System.Windows.Forms.ColumnHeader ActionColumn;
		internal System.Windows.Forms.ColumnHeader DirectionColumn;
		internal System.Windows.Forms.ColumnHeader TypeColumn;
		internal System.Windows.Forms.Label SpeedLabel;
		internal System.Windows.Forms.Label Done;
		internal System.Windows.Forms.Label DoneLabel;
		internal System.Windows.Forms.Label Speed;
		internal System.Windows.Forms.Label FoldersCreated;
		internal System.Windows.Forms.Label FilesCreatedLabel;
		internal System.Windows.Forms.Label FilesCreated;
		internal System.Windows.Forms.Label FoldersCreatedLabel;
		internal System.Windows.Forms.ImageList SyncingIcons;
		internal System.Windows.Forms.Timer SyncingTimer;
		internal System.Windows.Forms.Label FoldersDeleted;
		internal System.Windows.Forms.Label FoldersDeletedLabel;
		internal System.Windows.Forms.Label FilesDeletedLabel;
		internal System.Windows.Forms.Label FilesDeleted;
	}
}