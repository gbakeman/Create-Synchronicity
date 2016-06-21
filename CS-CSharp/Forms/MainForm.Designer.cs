namespace CreateSync.Forms
{
	partial class MainForm : System.Windows.Forms.Form
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
			System.Windows.Forms.ListViewGroup ListViewGroup1 = new System.Windows.Forms.ListViewGroup("\\ACTIONS", System.Windows.Forms.HorizontalAlignment.Left);
			System.Windows.Forms.ListViewGroup ListViewGroup2 = new System.Windows.Forms.ListViewGroup("\\PROFILES", System.Windows.Forms.HorizontalAlignment.Left);
			System.Windows.Forms.ListViewItem ListViewItem1 = new System.Windows.Forms.ListViewItem(new string[] {
			"\\NEW_PROFILE_LABEL",
			"\\NEW_PROFILE"
		}, 3);
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.Actions = new System.Windows.Forms.ListView();
			this.NameColumn = (System.Windows.Forms.ColumnHeader)new System.Windows.Forms.ColumnHeader();
			this.MethodsColumn = (System.Windows.Forms.ColumnHeader)new System.Windows.Forms.ColumnHeader();
			this.LastRunColumn = (System.Windows.Forms.ColumnHeader)new System.Windows.Forms.ColumnHeader();
			this.SyncIcons = new System.Windows.Forms.ImageList(this.components);
			this.InfoPanel = new System.Windows.Forms.Panel();
			this.InfoLayout = new System.Windows.Forms.TableLayoutPanel();
			this.TimeOffset = new System.Windows.Forms.Label();
			this.Scheduling = new System.Windows.Forms.Label();
			this.SchedulingLabel = new System.Windows.Forms.Label();
			this.Destination = new System.Windows.Forms.Label();
			this.DestinationLabel = new System.Windows.Forms.Label();
			this.Source = new System.Windows.Forms.Label();
			this.SourceLabel = new System.Windows.Forms.Label();
			this.FileTypes = new System.Windows.Forms.Label();
			this.FileTypesLabel = new System.Windows.Forms.Label();
			this.Method = new System.Windows.Forms.Label();
			this.MethodLabel = new System.Windows.Forms.Label();
			this.LimitedCopy = new System.Windows.Forms.Label();
			this.LimitedCopyLabel = new System.Windows.Forms.Label();
			this.ProfileName = new System.Windows.Forms.Label();
			this.NameLabel = new System.Windows.Forms.Label();
			this.TimeOffsetLabel = new System.Windows.Forms.Label();
			this.ActionsMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.PreviewMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.SynchronizeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ChangeSettingsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ActionsMenuToolStripSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.DeleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.RenameMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ViewLogMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ClearLogMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.ActionsMenuToolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.ScheduleMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.AboutLinkLabel = new System.Windows.Forms.LinkLabel();
			this.ApplicationTimer = new System.Windows.Forms.Timer(this.components);
			this.StatusIconMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.ToolStripHeader = new System.Windows.Forms.ToolStripMenuItem();
			this.HeaderSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.ExitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.Donate = new System.Windows.Forms.PictureBox();
			this.TipsLabel = new System.Windows.Forms.Label();
			this.InfoPanel.SuspendLayout();
			this.InfoLayout.SuspendLayout();
			this.ActionsMenu.SuspendLayout();
			this.StatusIconMenu.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)this.Donate).BeginInit();
			this.SuspendLayout();
			//
			//Actions
			//
			this.Actions.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.NameColumn,
			this.MethodsColumn,
			this.LastRunColumn
		});
			this.Actions.Dock = System.Windows.Forms.DockStyle.Fill;
			ListViewGroup1.Header = "\\ACTIONS";
			ListViewGroup1.Name = "Actions";
			ListViewGroup2.Header = "\\PROFILES";
			ListViewGroup2.Name = "Profiles";
			this.Actions.Groups.AddRange(new System.Windows.Forms.ListViewGroup[] {
			ListViewGroup1,
			ListViewGroup2
		});
			this.Actions.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			ListViewItem1.Group = ListViewGroup1;
			ListViewItem1.StateImageIndex = 0;
			this.Actions.Items.AddRange(new System.Windows.Forms.ListViewItem[] { ListViewItem1 });
			this.Actions.LargeImageList = this.SyncIcons;
			this.Actions.Location = new System.Drawing.Point(0, 0);
			this.Actions.MultiSelect = false;
			this.Actions.Name = "Actions";
			this.Actions.Size = new System.Drawing.Size(355, 262);
			this.Actions.TabIndex = 0;
			this.Actions.TileSize = new System.Drawing.Size(160, 40);
			this.Actions.UseCompatibleStateImageBehavior = false;
			this.Actions.View = System.Windows.Forms.View.Tile;
			//
			//NameColumn
			//
			this.NameColumn.Text = "\\NAME";
			//
			//MethodsColumn
			//
			this.MethodsColumn.Text = "\\METHOD";
			//
			//LastRunColumn
			//
			this.LastRunColumn.Text = "";
			//
			//SyncIcons
			//
			this.SyncIcons.ImageStream = (System.Windows.Forms.ImageListStreamer)resources.GetObject("SyncIcons.ImageStream");
			this.SyncIcons.TransparentColor = System.Drawing.Color.Empty;
			this.SyncIcons.Images.SetKeyName(0, "edit-redo.png");
			this.SyncIcons.Images.SetKeyName(1, "edit-redo-add.png");
			this.SyncIcons.Images.SetKeyName(2, "view-refresh-32.png");
			this.SyncIcons.Images.SetKeyName(3, "document-new.png");
			//
			//InfoPanel
			//
			this.InfoPanel.Controls.Add(this.InfoLayout);
			this.InfoPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.InfoPanel.Location = new System.Drawing.Point(0, 262);
			this.InfoPanel.Name = "InfoPanel";
			this.InfoPanel.Size = new System.Drawing.Size(355, 160);
			this.InfoPanel.TabIndex = 2;
			//
			//InfoLayout
			//
			this.InfoLayout.Anchor = (System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right);
			this.InfoLayout.CellBorderStyle = System.Windows.Forms.TableLayoutPanelCellBorderStyle.Single;
			this.InfoLayout.ColumnCount = 4;
			this.InfoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.InfoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70f));
			this.InfoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.InfoLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30f));
			this.InfoLayout.Controls.Add(this.TimeOffset, 3, 2);
			this.InfoLayout.Controls.Add(this.Scheduling, 1, 2);
			this.InfoLayout.Controls.Add(this.SchedulingLabel, 0, 2);
			this.InfoLayout.Controls.Add(this.Destination, 1, 4);
			this.InfoLayout.Controls.Add(this.DestinationLabel, 0, 4);
			this.InfoLayout.Controls.Add(this.Source, 1, 3);
			this.InfoLayout.Controls.Add(this.SourceLabel, 0, 3);
			this.InfoLayout.Controls.Add(this.FileTypes, 3, 1);
			this.InfoLayout.Controls.Add(this.FileTypesLabel, 2, 1);
			this.InfoLayout.Controls.Add(this.Method, 1, 1);
			this.InfoLayout.Controls.Add(this.MethodLabel, 0, 1);
			this.InfoLayout.Controls.Add(this.LimitedCopy, 3, 0);
			this.InfoLayout.Controls.Add(this.LimitedCopyLabel, 2, 0);
			this.InfoLayout.Controls.Add(this.ProfileName, 1, 0);
			this.InfoLayout.Controls.Add(this.NameLabel, 0, 0);
			this.InfoLayout.Controls.Add(this.TimeOffsetLabel, 2, 2);
			this.InfoLayout.Location = new System.Drawing.Point(12, 6);
			this.InfoLayout.Name = "InfoLayout";
			this.InfoLayout.RowCount = 5;
			this.InfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20f));
			this.InfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20f));
			this.InfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20f));
			this.InfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20f));
			this.InfoLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20f));
			this.InfoLayout.Size = new System.Drawing.Size(331, 142);
			this.InfoLayout.TabIndex = 0;
			//
			//TimeOffset
			//
			this.TimeOffset.AutoSize = true;
			this.TimeOffset.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TimeOffset.Location = new System.Drawing.Point(296, 57);
			this.TimeOffset.Name = "TimeOffset";
			this.TimeOffset.Size = new System.Drawing.Size(31, 27);
			this.TimeOffset.TabIndex = 15;
			this.TimeOffset.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//Scheduling
			//
			this.Scheduling.AutoSize = true;
			this.Scheduling.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Scheduling.Location = new System.Drawing.Point(104, 57);
			this.Scheduling.Name = "Scheduling";
			this.Scheduling.Size = new System.Drawing.Size(79, 27);
			this.Scheduling.TabIndex = 14;
			this.Scheduling.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//SchedulingLabel
			//
			this.SchedulingLabel.AutoSize = true;
			this.SchedulingLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SchedulingLabel.Location = new System.Drawing.Point(4, 57);
			this.SchedulingLabel.Name = "SchedulingLabel";
			this.SchedulingLabel.Size = new System.Drawing.Size(93, 27);
			this.SchedulingLabel.TabIndex = 13;
			this.SchedulingLabel.Text = "\\SCH_LABEL";
			this.SchedulingLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//Destination
			//
			this.Destination.AutoSize = true;
			this.InfoLayout.SetColumnSpan(this.Destination, 3);
			this.Destination.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Destination.Location = new System.Drawing.Point(104, 113);
			this.Destination.Name = "Destination";
			this.Destination.Size = new System.Drawing.Size(223, 28);
			this.Destination.TabIndex = 11;
			this.Destination.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Destination.UseMnemonic = false;
			//
			//DestinationLabel
			//
			this.DestinationLabel.AutoSize = true;
			this.DestinationLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.DestinationLabel.Location = new System.Drawing.Point(4, 113);
			this.DestinationLabel.Name = "DestinationLabel";
			this.DestinationLabel.Size = new System.Drawing.Size(93, 28);
			this.DestinationLabel.TabIndex = 10;
			this.DestinationLabel.Text = "\\DESTINATION";
			this.DestinationLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//Source
			//
			this.Source.AutoSize = true;
			this.InfoLayout.SetColumnSpan(this.Source, 3);
			this.Source.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Source.Location = new System.Drawing.Point(104, 85);
			this.Source.Name = "Source";
			this.Source.Size = new System.Drawing.Size(223, 27);
			this.Source.TabIndex = 9;
			this.Source.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.Source.UseMnemonic = false;
			//
			//SourceLabel
			//
			this.SourceLabel.AutoSize = true;
			this.SourceLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.SourceLabel.Location = new System.Drawing.Point(4, 85);
			this.SourceLabel.Name = "SourceLabel";
			this.SourceLabel.Size = new System.Drawing.Size(93, 27);
			this.SourceLabel.TabIndex = 8;
			this.SourceLabel.Text = "\\SOURCE";
			this.SourceLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//FileTypes
			//
			this.FileTypes.AutoSize = true;
			this.FileTypes.Dock = System.Windows.Forms.DockStyle.Fill;
			this.FileTypes.Location = new System.Drawing.Point(296, 29);
			this.FileTypes.Name = "FileTypes";
			this.FileTypes.Size = new System.Drawing.Size(31, 27);
			this.FileTypes.TabIndex = 7;
			this.FileTypes.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//FileTypesLabel
			//
			this.FileTypesLabel.AutoSize = true;
			this.FileTypesLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.FileTypesLabel.Location = new System.Drawing.Point(190, 29);
			this.FileTypesLabel.Name = "FileTypesLabel";
			this.FileTypesLabel.Size = new System.Drawing.Size(99, 27);
			this.FileTypesLabel.TabIndex = 6;
			this.FileTypesLabel.Text = "\\FILETYPES";
			this.FileTypesLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//Method
			//
			this.Method.AutoSize = true;
			this.Method.Dock = System.Windows.Forms.DockStyle.Fill;
			this.Method.Location = new System.Drawing.Point(104, 29);
			this.Method.Name = "Method";
			this.Method.Size = new System.Drawing.Size(79, 27);
			this.Method.TabIndex = 5;
			this.Method.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//MethodLabel
			//
			this.MethodLabel.AutoSize = true;
			this.MethodLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.MethodLabel.Location = new System.Drawing.Point(4, 29);
			this.MethodLabel.Name = "MethodLabel";
			this.MethodLabel.Size = new System.Drawing.Size(93, 27);
			this.MethodLabel.TabIndex = 4;
			this.MethodLabel.Text = "\\METHOD";
			this.MethodLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//LimitedCopy
			//
			this.LimitedCopy.AutoSize = true;
			this.LimitedCopy.Dock = System.Windows.Forms.DockStyle.Fill;
			this.LimitedCopy.Location = new System.Drawing.Point(296, 1);
			this.LimitedCopy.Name = "LimitedCopy";
			this.LimitedCopy.Size = new System.Drawing.Size(31, 27);
			this.LimitedCopy.TabIndex = 3;
			this.LimitedCopy.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//LimitedCopyLabel
			//
			this.LimitedCopyLabel.AutoSize = true;
			this.LimitedCopyLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.LimitedCopyLabel.Location = new System.Drawing.Point(190, 1);
			this.LimitedCopyLabel.Name = "LimitedCopyLabel";
			this.LimitedCopyLabel.Size = new System.Drawing.Size(99, 27);
			this.LimitedCopyLabel.TabIndex = 2;
			this.LimitedCopyLabel.Text = "\\LIMITED_COPY";
			this.LimitedCopyLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//ProfileName
			//
			this.ProfileName.AutoSize = true;
			this.ProfileName.Dock = System.Windows.Forms.DockStyle.Fill;
			this.ProfileName.Location = new System.Drawing.Point(104, 1);
			this.ProfileName.Name = "ProfileName";
			this.ProfileName.Size = new System.Drawing.Size(79, 27);
			this.ProfileName.TabIndex = 1;
			this.ProfileName.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//NameLabel
			//
			this.NameLabel.AutoSize = true;
			this.NameLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.NameLabel.Location = new System.Drawing.Point(4, 1);
			this.NameLabel.Name = "NameLabel";
			this.NameLabel.Size = new System.Drawing.Size(93, 27);
			this.NameLabel.TabIndex = 0;
			this.NameLabel.Text = "\\NAME";
			this.NameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//TimeOffsetLabel
			//
			this.TimeOffsetLabel.AutoSize = true;
			this.TimeOffsetLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.TimeOffsetLabel.Location = new System.Drawing.Point(190, 57);
			this.TimeOffsetLabel.Name = "TimeOffsetLabel";
			this.TimeOffsetLabel.Size = new System.Drawing.Size(99, 27);
			this.TimeOffsetLabel.TabIndex = 12;
			this.TimeOffsetLabel.Text = "\\TIME_OFFSET";
			this.TimeOffsetLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			//ActionsMenu
			//
			this.ActionsMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.PreviewMenuItem,
			this.SynchronizeMenuItem,
			this.ChangeSettingsMenuItem,
			this.ActionsMenuToolStripSeparator,
			this.DeleteToolStripMenuItem,
			this.RenameMenuItem,
			this.ViewLogMenuItem,
			this.ClearLogMenuItem,
			this.ActionsMenuToolStripSeparator2,
			this.ScheduleMenuItem
		});
			this.ActionsMenu.Name = "ActionsMenu";
			this.ActionsMenu.Size = new System.Drawing.Size(185, 192);
			//
			//PreviewMenuItem
			//
			this.PreviewMenuItem.Image = (System.Drawing.Image)resources.GetObject("PreviewMenuItem.Image");
			this.PreviewMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.PreviewMenuItem.Name = "PreviewMenuItem";
			this.PreviewMenuItem.Size = new System.Drawing.Size(184, 22);
			this.PreviewMenuItem.Text = "\\PREVIEW";
			//
			//SynchronizeMenuItem
			//
			this.SynchronizeMenuItem.Image = (System.Drawing.Image)resources.GetObject("SynchronizeMenuItem.Image");
			this.SynchronizeMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.SynchronizeMenuItem.Name = "SynchronizeMenuItem";
			this.SynchronizeMenuItem.Size = new System.Drawing.Size(184, 22);
			this.SynchronizeMenuItem.Text = "\\SYNC";
			//
			//ChangeSettingsMenuItem
			//
			this.ChangeSettingsMenuItem.Image = (System.Drawing.Image)resources.GetObject("ChangeSettingsMenuItem.Image");
			this.ChangeSettingsMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.ChangeSettingsMenuItem.Name = "ChangeSettingsMenuItem";
			this.ChangeSettingsMenuItem.Size = new System.Drawing.Size(184, 22);
			this.ChangeSettingsMenuItem.Text = "\\CHANGE_SETTINGS";
			//
			//ActionsMenuToolStripSeparator
			//
			this.ActionsMenuToolStripSeparator.Name = "ActionsMenuToolStripSeparator";
			this.ActionsMenuToolStripSeparator.Size = new System.Drawing.Size(181, 6);
			//
			//DeleteToolStripMenuItem
			//
			this.DeleteToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("DeleteToolStripMenuItem.Image");
			this.DeleteToolStripMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.DeleteToolStripMenuItem.Name = "DeleteToolStripMenuItem";
			this.DeleteToolStripMenuItem.Size = new System.Drawing.Size(184, 22);
			this.DeleteToolStripMenuItem.Text = "\\DELETE";
			//
			//RenameMenuItem
			//
			this.RenameMenuItem.Image = (System.Drawing.Image)resources.GetObject("RenameMenuItem.Image");
			this.RenameMenuItem.Name = "RenameMenuItem";
			this.RenameMenuItem.Size = new System.Drawing.Size(184, 22);
			this.RenameMenuItem.Text = "\\RENAME";
			//
			//ViewLogMenuItem
			//
			this.ViewLogMenuItem.Image = (System.Drawing.Image)resources.GetObject("ViewLogMenuItem.Image");
			this.ViewLogMenuItem.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
			this.ViewLogMenuItem.Name = "ViewLogMenuItem";
			this.ViewLogMenuItem.Size = new System.Drawing.Size(184, 22);
			this.ViewLogMenuItem.Text = "\\VIEW_LOG";
			//
			//ClearLogMenuItem
			//
			this.ClearLogMenuItem.Image = (System.Drawing.Image)resources.GetObject("ClearLogMenuItem.Image");
			this.ClearLogMenuItem.Name = "ClearLogMenuItem";
			this.ClearLogMenuItem.Size = new System.Drawing.Size(184, 22);
			this.ClearLogMenuItem.Tag = "\\CLEAR_LOG";
			//
			//ActionsMenuToolStripSeparator2
			//
			this.ActionsMenuToolStripSeparator2.Name = "ActionsMenuToolStripSeparator2";
			this.ActionsMenuToolStripSeparator2.Size = new System.Drawing.Size(181, 6);
			//
			//ScheduleMenuItem
			//
			this.ScheduleMenuItem.Image = (System.Drawing.Image)resources.GetObject("ScheduleMenuItem.Image");
			this.ScheduleMenuItem.Name = "ScheduleMenuItem";
			this.ScheduleMenuItem.Size = new System.Drawing.Size(184, 22);
			this.ScheduleMenuItem.Text = "\\SCHEDULING";
			//
			//AboutLinkLabel
			//
			this.AboutLinkLabel.Anchor = (System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right);
			this.AboutLinkLabel.BackColor = System.Drawing.Color.White;
			this.AboutLinkLabel.Image = (System.Drawing.Image)resources.GetObject("AboutLinkLabel.Image");
			this.AboutLinkLabel.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.AboutLinkLabel.Location = new System.Drawing.Point(230, 3);
			this.AboutLinkLabel.Name = "AboutLinkLabel";
			this.AboutLinkLabel.Size = new System.Drawing.Size(118, 16);
			this.AboutLinkLabel.TabIndex = 1;
			this.AboutLinkLabel.TabStop = true;
			this.AboutLinkLabel.Text = "\\ABOUT_SETTINGS";
			this.AboutLinkLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.AboutLinkLabel.VisitedLinkColor = System.Drawing.Color.Blue;
			//
			//StatusIconMenu
			//
			this.StatusIconMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.ToolStripHeader,
			this.HeaderSeparator,
			this.ExitToolStripMenuItem
		});
			this.StatusIconMenu.Name = "StatusIconMenu";
			this.StatusIconMenu.Size = new System.Drawing.Size(184, 54);
			//
			//ToolStripHeader
			//
			this.ToolStripHeader.Name = "ToolStripHeader";
			this.ToolStripHeader.Size = new System.Drawing.Size(183, 22);
			this.ToolStripHeader.Text = "Create Synchronicity";
			//
			//HeaderSeparator
			//
			this.HeaderSeparator.Name = "HeaderSeparator";
			this.HeaderSeparator.Size = new System.Drawing.Size(180, 6);
			//
			//ExitToolStripMenuItem
			//
			this.ExitToolStripMenuItem.Name = "ExitToolStripMenuItem";
			this.ExitToolStripMenuItem.Size = new System.Drawing.Size(183, 22);
			//
			//Donate
			//
			this.Donate.Anchor = (System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right);
			this.Donate.BackColor = System.Drawing.Color.White;
			this.Donate.Image = (System.Drawing.Image)resources.GetObject("Donate.Image");
			this.Donate.Location = new System.Drawing.Point(314, 22);
			this.Donate.Name = "Donate";
			this.Donate.Size = new System.Drawing.Size(34, 34);
			this.Donate.SizeMode = System.Windows.Forms.PictureBoxSizeMode.CenterImage;
			this.Donate.TabIndex = 3;
			this.Donate.TabStop = false;
			//
			//TipsLabel
			//
			this.TipsLabel.BackColor = System.Drawing.Color.White;
			this.TipsLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.TipsLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
			this.TipsLabel.Location = new System.Drawing.Point(0, 227);
			this.TipsLabel.Name = "TipsLabel";
			this.TipsLabel.Size = new System.Drawing.Size(355, 35);
			this.TipsLabel.TabIndex = 4;
			this.TipsLabel.Text = "\\NO_PROFILES";
			this.TipsLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.TipsLabel.Visible = false;
			//
			//MainForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(7f, 13f);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(355, 422);
			this.Controls.Add(this.AboutLinkLabel);
			this.Controls.Add(this.TipsLabel);
			this.Controls.Add(this.Donate);
			this.Controls.Add(this.Actions);
			this.Controls.Add(this.InfoPanel);
			this.Font = new System.Drawing.Font("Verdana", 8.25f);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.KeyPreview = true;
			this.Name = "MainForm";
			this.Text = "Create Synchronicity";
			this.InfoPanel.ResumeLayout(false);
			this.InfoLayout.ResumeLayout(false);
			this.InfoLayout.PerformLayout();
			this.ActionsMenu.ResumeLayout(false);
			this.StatusIconMenu.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)this.Donate).EndInit();
			this.ResumeLayout(false);

		}
		internal System.Windows.Forms.ListView Actions;
		internal System.Windows.Forms.Panel InfoPanel;
		internal System.Windows.Forms.TableLayoutPanel InfoLayout;
		internal System.Windows.Forms.Label ProfileName;
		internal System.Windows.Forms.Label NameLabel;
		internal System.Windows.Forms.Label Destination;
		internal System.Windows.Forms.Label DestinationLabel;
		internal System.Windows.Forms.Label Source;
		internal System.Windows.Forms.Label SourceLabel;
		internal System.Windows.Forms.Label FileTypes;
		internal System.Windows.Forms.Label FileTypesLabel;
		internal System.Windows.Forms.Label Method;
		internal System.Windows.Forms.Label MethodLabel;
		internal System.Windows.Forms.Label LimitedCopy;
		internal System.Windows.Forms.Label LimitedCopyLabel;
		internal System.Windows.Forms.ContextMenuStrip ActionsMenu;
		internal System.Windows.Forms.ToolStripMenuItem SynchronizeMenuItem;
		internal System.Windows.Forms.ToolStripMenuItem PreviewMenuItem;
		internal System.Windows.Forms.ToolStripMenuItem ChangeSettingsMenuItem;
		internal System.Windows.Forms.ToolStripMenuItem ViewLogMenuItem;
		internal System.Windows.Forms.ImageList SyncIcons;
		internal System.Windows.Forms.ColumnHeader NameColumn;
		internal System.Windows.Forms.ColumnHeader MethodsColumn;
		internal System.Windows.Forms.ToolStripSeparator ActionsMenuToolStripSeparator;
		internal System.Windows.Forms.ToolStripMenuItem DeleteToolStripMenuItem;
		internal System.Windows.Forms.LinkLabel AboutLinkLabel;
		internal System.Windows.Forms.ToolStripMenuItem ClearLogMenuItem;
		internal System.Windows.Forms.ToolStripSeparator ActionsMenuToolStripSeparator2;
		internal System.Windows.Forms.ToolStripMenuItem ScheduleMenuItem;
		internal System.Windows.Forms.Timer ApplicationTimer;
		internal System.Windows.Forms.Label TimeOffset;
		internal System.Windows.Forms.Label Scheduling;
		internal System.Windows.Forms.Label SchedulingLabel;
		internal System.Windows.Forms.Label TimeOffsetLabel;
		internal System.Windows.Forms.ToolStripMenuItem ExitToolStripMenuItem;
		internal System.Windows.Forms.ToolStripMenuItem RenameMenuItem;
		internal System.Windows.Forms.PictureBox Donate;
		internal System.Windows.Forms.ToolStripMenuItem ToolStripHeader;
		internal System.Windows.Forms.ToolStripSeparator HeaderSeparator;
		internal System.Windows.Forms.ContextMenuStrip StatusIconMenu;
		internal System.Windows.Forms.Label TipsLabel;
		internal System.Windows.Forms.ColumnHeader LastRunColumn;
	}
}