using System;
using System.Collections.Generic;
using System.Threading;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace CreateSync.Forms
{
	public partial class SynchronizeForm : Form
	{
		private LogHandler Log;

		private ProfileHandler Handler;
		private Dictionary<string, bool?> ValidFiles = new Dictionary<string, bool?>();
		private List<SyncingItem> SyncingList = new List<SyncingItem>();
		private List<FileNamePattern> IncludedPatterns = new List<FileNamePattern>();
		private List<FileNamePattern> ExcludedPatterns = new List<FileNamePattern>();

		private List<FileNamePattern> ExcludedDirPatterns = new List<FileNamePattern>();
		private string[] Labels = null;
		private string StatusLabel = "";

		private object Lock = new object();
		//Indicates whether this operation was started due to catchup rules.
		private bool Catchup;
		//Should show a preview.
		private bool Preview;

		private StatusData Status;
		private string TitleText;

		private SyncingListSorter Sorter = new SyncingListSorter(3);
		private Thread FullSyncThread;
		private Thread ScanThread;

		private Thread SyncThread;
		private bool AutoInclude;
		private DateTime MDate;
		private string LeftRootPath;
		//Translated path to left and right folders
		private string RightRootPath;

		private delegate void StepCompletedCall(StatusData.SyncStep Id);
		private delegate void SetIntCall(StatusData.SyncStep Id, int Max, bool Finished);

		internal event SyncFinishedEventHandler SyncFinished;
		internal delegate void SyncFinishedEventHandler(string Name, bool Completed);

		//Not evaluating file size gives better performance (See speed-test.vb for tests):
		//With size evaluation: 1'20, 46'', 36'', 35'', 31''
		//Without:                    41'', 42'', 26'', 29''

		#region " Events "
		public SynchronizeForm(string ConfigName, bool DisplayPreview, bool _Catchup)
		{
			Resize += SynchronizeForm_Resize;
			FormClosed += SynchronizeForm_FormClosed;
			KeyDown += SynchronizeForm_KeyDown;
			// This call is required by the Windows Form Designer.
			InitializeComponent();

			// Add any initialization after the InitializeComponent() call.
			Catchup = _Catchup;
			Preview = DisplayPreview;
			SyncBtn.Enabled = false;
			SyncBtn.Visible = Preview;

			Status = new StatusData();
			Status.CurrentStep = StatusData.SyncStep.Scan;
			Status.StartTime = System.DateTime.Now;
			// NOTE: This call should be useless; it however seems that when the messagebox.show method is called when a profile is not found, the syncingtimecounter starts ticking. This is not suitable, but until the cause is found there this call remains, for display consistency.

			Handler = new ProfileHandler(ConfigName);
			Log = new LogHandler(ConfigName, Handler.GetSetting<bool>(ProfileSetting.ErrorsLog, false));

			MDate = Handler.GetSetting<DateTime>(ProfileSetting.LastModified, System.IO.File.GetLastWriteTimeUtc(Handler.ConfigPath));
			AutoInclude = Handler.GetSetting<bool>(ProfileSetting.AutoIncludeNewFolders, false);
			LeftRootPath = ProfileHandler.TranslatePath(Handler.GetSetting<string>(ProfileSetting.Source));
			RightRootPath = ProfileHandler.TranslatePath(Handler.GetSetting<string>(ProfileSetting.Destination));

			FileNamePattern.LoadPatternsList(ref IncludedPatterns, Handler.GetSetting<string>(ProfileSetting.IncludedTypes, ""), false, ProgramSetting.ExcludedFolderPrefix);
			FileNamePattern.LoadPatternsList(ref ExcludedPatterns, Handler.GetSetting<string>(ProfileSetting.ExcludedTypes, ""), false, ProgramSetting.ExcludedFolderPrefix);
			FileNamePattern.LoadPatternsList(ref ExcludedDirPatterns, Handler.GetSetting<string>(ProfileSetting.ExcludedFolders, ""), true, "");
			FileNamePattern.LoadPatternsList(ref ExcludedDirPatterns, Handler.GetSetting<string>(ProfileSetting.ExcludedTypes, ""), true, ProgramSetting.ExcludedFolderPrefix);

			FullSyncThread = new Thread(FullSync);
			ScanThread = new Thread(Scan);
			SyncThread = new Thread(Sync);

			this.CreateHandle();
			Program.Translation.TranslateControl(this);
			this.Icon = Program.ProgramConfig.Icon;
			TitleText = string.Format(this.Text, Handler.ProfileName, LeftRootPath, RightRootPath);

			Labels = new string[] { "",
			Step1StatusLabel.Text,
			Step2StatusLabel.Text,
			Step3StatusLabel.Text
		};

#if LINUX
		Step1ProgressBar.MarqueeAnimationSpeed = 5000;
		SyncingTimer.Interval = 1000;
#endif
		}

		public void StartSynchronization(bool CalledShowModal)
		{
			Program.ProgramConfig.CanGoOn = false;

#if DEBUG
			Log.LogInfo("Synchronization started.");
			Log.LogInfo("Profile settings:");
			foreach (KeyValuePair<string, string> Pair in Handler.Configuration)
			{
				Log.LogInfo(string.Format("    {0,-50}: {1}", Pair.Key, Pair.Value));
			}
			Log.LogInfo("Done.");
#endif

			if (CommandLine.Quiet)
			{
				this.Visible = false;

				Interaction.StatusIcon.ContextMenuStrip = null;
				Interaction.StatusIcon.Click += StatusIcon_Click;

				Interaction.StatusIcon.Text = Program.Translation.Translate("\\RUNNING");

				Interaction.ToggleStatusIcon(true);
				if (Catchup)
				{
					Interaction.ShowBalloonTip(Program.Translation.TranslateFormat("\\CATCHING_UP", Handler.ProfileName, Handler.FormatLastRun()));
				}
				else
				{
					Interaction.ShowBalloonTip(Program.Translation.TranslateFormat("\\RUNNING_TASK", Handler.ProfileName));
				}
			}
			else
			{
				if (!CalledShowModal)
					this.Visible = true;
			}

			Status.FailureMsg = "";

			bool IsValid = Handler.ValidateConfigFile(false, true, Status.FailureMsg);
			if (Handler.GetSetting<bool>(ProfileSetting.PreviewOnly, false) & (!Preview))
				IsValid = false;

			Status.Failed = !IsValid;

			if (IsValid)
			{
				Program.ProgramConfig.IncrementSyncsCount();
				if (Preview)
				{
					ScanThread.Start();
				}
				else
				{
					FullSyncThread.Start();
				}
			}
			else
			{
				EndAll();
				//Also saves the log file
			}
		}

		private void SynchronizeForm_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			if (e.Control)
			{
				if (e.KeyCode == Keys.L && Status.CurrentStep == StatusData.SyncStep.Done)
				{
					Interaction.StartProcess(Handler.LogPath);
				}
				else if (e.KeyCode == Keys.D & PreviewList.SelectedIndices.Count != 0)
				{
					string DiffProgram = Program.ProgramConfig.GetProgramSetting<string>(ProgramSetting.DiffProgram, "");

					string NewFile = "";
					string OldFile = "";
					if (!SetPathFromSelectedItem(ref NewFile, ref OldFile))
						return;

					try
					{
						if (!string.IsNullOrEmpty(DiffProgram) && File.Exists(OldFile) && File.Exists(NewFile))
							Interaction.StartProcess(DiffProgram, "\"" + OldFile + "\" \"" + NewFile + "\"");
					}
					catch (Exception Ex)
					{
						Interaction.ShowMsg("Error loading diff: " + Ex.ToString());
					}
				}
			}
		}

		private void SynchronizeForm_FormClosed(System.Object sender, System.Windows.Forms.FormClosedEventArgs e)
		{
			EndAll();
			Program.ProgramConfig.CanGoOn = true;
			Interaction.StatusIcon.ContextMenuStrip = Program.MainFormInstance.StatusIconMenu;
			Interaction.StatusIcon.Click -= StatusIcon_Click;

			Interaction.StatusIcon.Text = Program.Translation.Translate("\\WAITING");
			if (SyncFinished != null)
			{
				SyncFinished(Handler.ProfileName, !(Status.Failed | Status.Cancel));
			}
			//These parameters are not used atm.
		}

		private void CancelBtn_Click(object sender, EventArgs e)
		{
			if (StopBtn.Text == StopBtn.Tag.ToString().Split(';')[0])
				EndAll();
			else if (StopBtn.Text == StopBtn.Tag.ToString().Split(';')[1])
				this.Close();
		}

		private void SyncBtn_Click(object sender, EventArgs e)
		{
			PreviewList.Visible = false;
			SyncBtn.Visible = false;
			StopBtn.Text = StopBtn.Tag.ToString().Split(';')[0];

			SyncThread.Start();
		}

		//Handler dynamically added
		private void StatusIcon_Click(object sender, EventArgs e)
		{
			this.Visible = !this.Visible;
			this.WindowState = FormWindowState.Normal;
			if (this.Visible)
				this.Activate();
		}

		private void SynchronizeForm_Resize(object sender, EventArgs e)
		{
			if (this.WindowState == FormWindowState.Minimized & CommandLine.Quiet)
				this.Visible = false;
		}

		private void SyncingTimeCounter_Tick(object sender, EventArgs e)
		{
			UpdateStatuses();
		}

		/*#if 0
			//Works, but not really efficiently, and flickers a lot.
			int static_PreviewList_CacheVirtualItems_PrevStartIndex;
			private void PreviewList_CacheVirtualItems(object sender, System.Windows.Forms.CacheVirtualItemsEventArgs e)
			{
				lock (static_PreviewList_CacheVirtualItems_PrevStartIndex_Init) {
					try {
						if (InitStaticVariableHelper(static_PreviewList_CacheVirtualItems_PrevStartIndex_Init)) {
							static_PreviewList_CacheVirtualItems_PrevStartIndex = -1;
						}
					} finally {
						static_PreviewList_CacheVirtualItems_PrevStartIndex_Init.State = 1;
					}
				}
				return;
				if (static_PreviewList_CacheVirtualItems_PrevStartIndex != e.StartIndex) {
					static_PreviewList_CacheVirtualItems_PrevStartIndex = e.StartIndex;
					for (int id = 0; id <= PreviewList.Columns.Count - 1; id++) {
						PreviewList.AutoResizeColumn(id, ColumnHeaderAutoResizeStyle.ColumnContent);
					}
				}
			}
		#endif*/

		private void PreviewList_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
		{
			if (Status.ShowingErrors)
			{
				e.Item = Log.Errors[e.ItemIndex].ToListViewItem();
			}
			else
			{
				e.Item = SyncingList[e.ItemIndex].ToListViewItem();
			}

			//TODO: Auto-resizing would be nice, but:
			//      * AutoResizeColumns raises RetrieveVirtualItem, causing a StackOverflowException
			//      * Checking TopItem to conditionally resize columns doesn't work in virtual mode (it even crashes the debugger).
			//      * Handling the CacheVirtualItems event works, but does flicker a lot.
		}

		private void PreviewList_ColumnClick(object sender, ColumnClickEventArgs e)
		{
			if (Status.ShowingErrors)
				return;

			Sorter.RegisterClick(e);
			SyncingList.Sort(Sorter);
			PreviewList.Refresh();
		}

		private void PreviewList_DoubleClick(object sender, EventArgs e)
		{
			string Source = "";
			string Dest = "";
			if (!SetPathFromSelectedItem(ref Source, ref Dest))
				return;

			string Path = (Control.ModifierKeys & Keys.Alt) != 0 ? Dest : Source;
			if ((Control.ModifierKeys & Keys.Control) != 0)
				Path = System.IO.Path.GetDirectoryName(Path);

			if (System.IO.File.Exists(Path) || System.IO.Directory.Exists(Path))
				Interaction.StartProcess(Path);
		}

		private bool SetPathFromSelectedItem(ref string Source, ref string Dest)
		{
			//Exit if nothing is selected, or if in error mode
			if (PreviewList.SelectedIndices.Count == 0 || Status.ShowingErrors)
				return false;

			SyncingItem CurItem = SyncingList[PreviewList.SelectedIndices[0]];

			string LeftFile = null;
			string RightFile = null;
			LeftFile = LeftRootPath + CurItem.Path;
			RightFile = RightRootPath + CurItem.Path;

			switch (CurItem.Side)
			{
				case SideOfSource.Left:
					Source = LeftFile;
					Dest = RightFile;
					break;
				case SideOfSource.Right:
					Source = RightFile;
					Dest = LeftFile;
					break;
			}

			return true;
		}

		private void UpdateStatuses()
		{
			long PreviousActionsDone = -1;
			bool CanDelete = (Handler.GetSetting<int>(ProfileSetting.Method, ProfileSetting.DefaultMethod) == (int)ProfileSetting.SyncMethod.LRMirror);

			Status.TimeElapsed = (DateTime.Now - Status.StartTime) + new TimeSpan(1000000); // ie +0.1s

			string EstimateString = "";
			bool Copying = Status.CurrentStep == StatusData.SyncStep.LR | (!CanDelete & Status.CurrentStep == StatusData.SyncStep.RL);

			if (Status.CurrentStep == StatusData.SyncStep.Scan)
			{
				Speed.Text = Math.Round(Status.FilesScanned / Status.TimeElapsed.TotalSeconds).ToString() + " files/s";
			}
			else if (CanDelete & Status.CurrentStep == StatusData.SyncStep.RL)
			{
				Speed.Text = Math.Round(Status.DeletedFiles / Status.TimeElapsed.TotalSeconds).ToString() + " files/s";
			}
			else if (Copying && PreviousActionsDone != Status.ActionsDone)
			{
				PreviousActionsDone = Status.ActionsDone;

				Status.Speed = Status.BytesCopied / Status.TimeElapsed.TotalSeconds;
				Speed.Text = Utilities.FormatSize(Status.Speed) + "/s";
			}


			if (Copying && Status.Speed > (1 << 10) && Status.TimeElapsed.TotalSeconds > ProgramSetting.ForecastDelay
				&& Program.ProgramConfig.GetProgramSetting<bool>(ProgramSetting.Forecast, false))
			{
				int TotalTime = Convert.ToInt32(Math.Min(int.MaxValue, Status.BytesToCopy / Status.Speed));
				EstimateString = string.Format(" / ~{0}", Utilities.FormatTimespan(new TimeSpan(0, 0, TotalTime)));
			}

			ElapsedTime.Text = Utilities.FormatTimespan(Status.TimeElapsed) + EstimateString;

			Done.Text = Status.ActionsDone + "/" + Status.TotalActionsCount;
			FilesDeleted.Text = Status.DeletedFiles + "/" + Status.FilesToDelete;
			FilesCreated.Text = Status.CreatedFiles + "/" + Status.FilesToCreate + " (" + Utilities.FormatSize(Status.BytesCopied) + ")";
			FoldersDeleted.Text = Status.DeletedFolders + "/" + Status.FoldersToDelete;
			FoldersCreated.Text = Status.CreatedFolders + "/" + Status.FoldersToCreate;

			lock (Lock)
			{
				if (Labels != null)
				{
					Step1StatusLabel.Text = Labels[1];
					Step2StatusLabel.Text = Labels[2];
					Step3StatusLabel.Text = Labels[3];
				}
				Interaction.StatusIcon.Text = StatusLabel;
			}

			int PercentProgress = 0;
			if (Status.CurrentStep == StatusData.SyncStep.Scan)
			{
				PercentProgress = 0;
			}
			else if (Status.CurrentStep == StatusData.SyncStep.Done || Status.TotalActionsCount == 0)
			{
				PercentProgress = 100;
			}
			else
			{
				PercentProgress = Convert.ToInt32(100 * Status.ActionsDone / Status.TotalActionsCount);
			}

			//Later: No need to update every time when CurrentStep \in {Scan, Done}
			this.Text = string.Format("({0}%) ", PercentProgress) + TitleText;
			//Feature requests #3037548, #3055740
		}
		#endregion

		#region " Interface "
		private void UpdateLabel(StatusData.SyncStep Id, string Text)
		{
			string StatusText = Text;
			if (Text.Length > 30)
			{
				StatusText = "..." + Text.Substring(Text.Length - 30, 30);
			}

			switch (Id)
			{
				case StatusData.SyncStep.Scan:
					StatusText = Program.Translation.TranslateFormat("\\STEP_1_STATUS", StatusText);
					break;
				case StatusData.SyncStep.LR:
					StatusText = Program.Translation.TranslateFormat("\\STEP_2_STATUS", Step2ProgressBar.Value, Step2ProgressBar.Maximum, StatusText);
					break;
				case StatusData.SyncStep.RL:
					StatusText = Program.Translation.TranslateFormat("\\STEP_3_STATUS", Step3ProgressBar.Value, Step3ProgressBar.Maximum, StatusText);
					break;
			}

			lock (Lock)
			{
				Labels[(int)Id] = Text;
				StatusLabel = StatusText;
			}
		}

		private ProgressBar GetProgressBar(StatusData.SyncStep Id)
		{
			switch (Id)
			{
				case StatusData.SyncStep.Scan:
					return Step1ProgressBar;
				case StatusData.SyncStep.LR:
					return Step2ProgressBar;
				default:
					return Step3ProgressBar;
			}
		}

		private void Increment(StatusData.SyncStep Id, int Progress, bool Finished)
		{
			ProgressBar CurBar = GetProgressBar(Id);
			if (CurBar.Value + Progress < CurBar.Maximum)
				CurBar.Value += Progress;
		}

		//Careful: MaxValue is an Integer.
		private void SetMax(StatusData.SyncStep Id, int MaxValue, bool Finished = false)
		{
			ProgressBar CurBar = GetProgressBar(Id);

			CurBar.Style = ProgressBarStyle.Blocks;
			CurBar.Maximum = Math.Max(0, MaxValue);
			CurBar.Value = Finished ? MaxValue : 0;
		}

		private void StepCompleted(StatusData.SyncStep StepId)
		{
			if (!(Status.CurrentStep == StepId))
				return;
			//Prevents a potentially infinite exit loop.

			SetMax(StepId, 100, true);
			UpdateLabel(StepId, Program.Translation.Translate("\\FINISHED"));
			UpdateStatuses();

			switch (StepId)
			{
				case StatusData.SyncStep.Scan:
					SyncingTimer.Stop();
					Status.CurrentStep = StatusData.SyncStep.LR;
					if (Preview)
					{
						ShowPreviewList();
						StopBtn.Text = StopBtn.Tag.ToString().Split(';')[1];
					}

					break;
				case StatusData.SyncStep.LR:
					Status.CurrentStep = StatusData.SyncStep.RL;

					break;
				case StatusData.SyncStep.RL:
					SyncingTimer.Stop();
					Status.CurrentStep = StatusData.SyncStep.Done;

					UpdateStatuses();
					//Last update, to remove forecasts.

					if (Status.Failed)
					{
						Interaction.ShowBalloonTip(Status.FailureMsg);
					}
					else if (Log.Errors.Count > 0)
					{
						PreviewList.Visible = true;
						Status.ShowingErrors = true;

						PreviewList.VirtualMode = true;
						//In case it hadn't been enabled (ie. if there was no preview)
						PreviewList.VirtualListSize = Log.Errors.Count;

						PreviewList.Columns.Clear();
						PreviewList.Columns.Add(Program.Translation.Translate("\\ERROR"));
						PreviewList.Columns.Add(Program.Translation.Translate("\\ERROR_DETAIL"));
						PreviewList.Columns.Add(Program.Translation.Translate("\\PATH"));

						PreviewList.Refresh();
						PreviewList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);

						if (!Status.Cancel)
							Interaction.ShowBalloonTip(Program.Translation.TranslateFormat("\\SYNCED_W_ERRORS", Handler.ProfileName), Handler.LogPath);
					}
					else
					{
						if (!Status.Cancel)
							Interaction.ShowBalloonTip(Program.Translation.TranslateFormat("\\SYNCED_OK", Handler.ProfileName), Handler.LogPath);
					}

					Log.SaveAndDispose(LeftRootPath, RightRootPath, Status);
					if (!(Status.Failed | Status.Cancel))
						Handler.SetLastRun();
					//Required to implement catching up

					RunPostSync();

					if ((CommandLine.Quiet & !this.Visible) | CommandLine.NoStop)
					{
						this.Close();
					}
					else
					{
						StopBtn.Text = StopBtn.Tag.ToString().Split(';')[1];
					}
					break;
			}
		}

		private void ShowPreviewList()
		{
			// This part computes acceptable defaut values for column widths, since using VirtualMode prevents from resizing based on actual values.
			// This part requires that VirtualMode be set to False.
			SyncingItem i1 = new SyncingItem
			{
				Action = TypeOfAction.Copy,
				Side = SideOfSource.Left,
				Type = TypeOfItem.File,
				Path = "".PadLeft(260)
			};
			SyncingItem i2 = new SyncingItem
			{
				Action = TypeOfAction.Copy,
				Side = SideOfSource.Right,
				Type = TypeOfItem.File,
				IsUpdate = true
			};
			SyncingItem i3 = new SyncingItem
			{
				Action = TypeOfAction.Delete,
				Side = SideOfSource.Right,
				Type = TypeOfItem.Folder
			};

			PreviewList.Items.Add(i1.ToListViewItem());
			PreviewList.Items.Add(i2.ToListViewItem());
			PreviewList.Items.Add(i3.ToListViewItem());

			PreviewList.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
			PreviewList.Items.Clear();

			PreviewList.VirtualMode = true;
			PreviewList.Visible = true;
			PreviewList.VirtualListSize = SyncingList.Count;

			if (!(Status.Cancel | Handler.GetSetting<bool>(ProfileSetting.PreviewOnly, false)))
				SyncBtn.Enabled = true;
		}

		public void RunPostSync()
		{
			// Search for a post-sync action, requiring that Expert mode be enabled.
			string PostSyncAction = Handler.GetSetting<string>(ProfileSetting.PostSyncAction);

			if (Program.ProgramConfig.GetProgramSetting<bool>(ProgramSetting.ExpertMode, false) && PostSyncAction != null)
			{
				try
				{
					Environment.CurrentDirectory = Application.StartupPath;
					Interaction.ShowBalloonTip(string.Format(Program.Translation.Translate("\\POST_SYNC"), PostSyncAction));
					System.Diagnostics.Process.Start(PostSyncAction, string.Format("\"{0}\" \"{1}\" \"{2}\" \"{3}\" \"{4}\" \"{5}\"", Handler.ProfileName, !(Status.Cancel | Status.Failed), Log.Errors.Count, LeftRootPath, RightRootPath, Handler.ErrorsLogPath));
				}
				catch (Exception Ex)
				{
					string Err = Program.Translation.Translate("\\POSTSYNC_FAILED") + Environment.NewLine + Ex.Message;
					Interaction.ShowBalloonTip(Err);
					Program.ProgramConfig.LogAppEvent(Err);
				}
			}
		}

		private void LaunchTimer()
		{
			Status.BytesCopied = 0;
			Status.StartTime = DateTime.Now;
			SyncingTimer.Start();
		}

		private void EndAll()
		{
			Status.Cancel = Status.Cancel | (Status.CurrentStep != StatusData.SyncStep.Done);
			FullSyncThread.Abort();
			ScanThread.Abort();
			SyncThread.Abort();
			StepCompleted(StatusData.SyncStep.Scan);
			StepCompleted(StatusData.SyncStep.LR);
			StepCompleted(StatusData.SyncStep.RL);
			//This call will sleep for 5s after displaying its failure message if the backup failed.
		}
		#endregion

		#region " Scanning / IO "
		private void FullSync()
		{
			Scan();
			Sync();
		}

		private void Scan()
		{
			SyncingContext Context = new SyncingContext();
			StepCompletedCall StepCompletedCallback = new StepCompletedCall(StepCompleted);

			//Pass 1: Create actions L->R for files/folder copy, and mark dest files that should be kept
			//Pass 2: Create actions R->L for files/folder copy/deletion, based on what was marked as Valid.

			SyncingList.Clear();
			ValidFiles.Clear();

			this.Invoke(new Action(LaunchTimer));
			Context.Source = SideOfSource.Left;
			Context.SourceRootPath = LeftRootPath;
			Context.DestinationRootPath = RightRootPath;
			Init_Synchronization(ref Handler.LeftCheckedNodes, Context, TypeOfAction.Copy);

			Context.Source = SideOfSource.Right;
			Context.SourceRootPath = RightRootPath;
			Context.DestinationRootPath = LeftRootPath;
			switch (Handler.GetSetting<int>(ProfileSetting.Method, ProfileSetting.DefaultMethod))
			{
				//Important: (Of Integer)
				case (int)ProfileSetting.SyncMethod.LRMirror:
					Init_Synchronization(ref Handler.RightCheckedNodes, Context, TypeOfAction.Delete);
					break;
				case (int)ProfileSetting.SyncMethod.LRIncremental:
					break;
				//Pass
				case (int)ProfileSetting.SyncMethod.BiIncremental:
					Init_Synchronization(ref Handler.RightCheckedNodes, Context, TypeOfAction.Copy);
					break;
			}
			this.Invoke(StepCompletedCallback, StatusData.SyncStep.Scan);
		}

		private void Sync()
		{
			StepCompletedCall StepCompletedCallback = new StepCompletedCall(StepCompleted);
			SetIntCall SetMaxCallback = new SetIntCall(SetMax);

			//Restore original order before syncing.
			Sorter.SortColumn = -1;
			// Sorts according to initial index.
			Sorter.Order = SortOrder.Ascending;
			SyncingList.Sort(Sorter);

			this.Invoke(new Action(LaunchTimer));
			this.Invoke(SetMaxCallback, new object[] {
			StatusData.SyncStep.LR,
			Status.LeftActionsCount
		});
			Do_Tasks(SideOfSource.Left, StatusData.SyncStep.LR);
			this.Invoke(StepCompletedCallback, StatusData.SyncStep.LR);

			this.Invoke(SetMaxCallback, new object[] {
			StatusData.SyncStep.RL,
			Status.RightActionsCount
		});
			Do_Tasks(SideOfSource.Right, StatusData.SyncStep.RL);
			this.Invoke(StepCompletedCallback, StatusData.SyncStep.RL);
		}

		//"Source" is "current side", with the corresponding side stored in "Side"
		private void Do_Tasks(SideOfSource Side, StatusData.SyncStep CurrentStep)
		{
			SetIntCall IncrementCallback = new SetIntCall(Increment);

			string Source = Side == SideOfSource.Left ? LeftRootPath : RightRootPath;
			string Destination = Side == SideOfSource.Left ? RightRootPath : LeftRootPath;

			foreach (SyncingItem Entry in SyncingList)
			{
				if (Entry.Side != Side)
					continue;

				string SourcePath = Source + Entry.Path;
				string DestPath = Destination + Entry.Path;

				try
				{
					UpdateLabel(CurrentStep, Entry.Action == TypeOfAction.Delete ? SourcePath : Entry.Path);

					switch (Entry.Type)
					{
						case TypeOfItem.File:
							switch (Entry.Action)
							{
								case TypeOfAction.Copy:
									//FIXME: File attributes are never updated
									CopyFile(SourcePath, DestPath);
									break;
								case TypeOfAction.Delete:
									File.SetAttributes(SourcePath, FileAttributes.Normal);
									File.Delete(SourcePath);
									Status.DeletedFiles += 1;
									break;
							}

							break;
						case TypeOfItem.Folder:
							switch (Entry.Action)
							{
								case TypeOfAction.Copy:
									Directory.CreateDirectory(DestPath);

									//FIXME: Folder attributes sometimes don't apply well.
									File.SetAttributes(DestPath, File.GetAttributes(SourcePath));

									//When a file is updated, so is its parent folder's last-write time.
									//LATER: Remove this line: manual copying doesn't preserve creation time.
									Directory.SetCreationTimeUtc(DestPath, Directory.GetCreationTimeUtc(SourcePath).AddHours(Handler.GetSetting<int>(ProfileSetting.TimeOffset, 0)));

									Status.CreatedFolders += 1;
									break;
								case TypeOfAction.Delete:
#if DEBUG
									string[] RemainingFiles = Directory.GetFiles(SourcePath);
									string[] RemainingFolders = Directory.GetDirectories(SourcePath);
									if (RemainingFiles.Length > 0 | RemainingFolders.Length > 0)
										Log.LogInfo(string.Format("Do_Tasks: Removing non-empty folder {0} ({1}) ({2})", SourcePath, string.Join(", ", RemainingFiles), string.Join(", ", RemainingFolders)));
#endif
									try
									{
										Directory.Delete(SourcePath, true);
									}
									catch (Exception ex)
									{
										DirectoryInfo DirInfo = new DirectoryInfo(SourcePath);
										DirInfo.Attributes = FileAttributes.Directory;
										//Using "DirInfo.Attributes = IO.FileAttributes.Normal" does just the same, and actually sets DirInfo.Attributes to "IO.FileAttributes.Directory"
										DirInfo.Delete();
									}
									Status.DeletedFolders += 1;
									break;
							}
							break;
					}
					Status.ActionsDone += 1;
					Log.LogAction(Entry, Side, true);

				}
				catch (ThreadAbortException StopEx)
				{
					return;

				}
				catch (Exception ex)
				{
					Log.HandleError(ex, SourcePath);
					Log.LogAction(Entry, Side, false);
					//Side parameter is only used for logging purposes.
				}

				if (!Status.Cancel)
					this.Invoke(IncrementCallback, new object[] {
					CurrentStep,
					1
				});
			}
		}

		private void Init_Synchronization(ref Dictionary<string, bool> FoldersList, SyncingContext Context, TypeOfAction Action)
		{
			foreach (string Folder in FoldersList.Keys)
			{
				Log.LogInfo(string.Format("=> Scanning \"{0}\" top level folders: \"{1}\"", Context.SourceRootPath, Folder));
				if (Directory.Exists(CombinePathes(Context.SourceRootPath, Folder)))
				{
					if (Action == TypeOfAction.Copy)
					{
						//FIXED-BUG: Every ancestor of this folder should be added too.
						//Careful with this, for it's a performance issue. Ancestors should only be added /once/.
						//How to do that? Well, if ancestors of a folder have not been scanned, it means that this folder wasn't reached by a recursive call, but by a initial call.
						//Therefore, only the folders in the sync config file should be added.
						AddValidAncestors(Folder);
						SearchForChanges(Folder, FoldersList[Folder], Context);
					}
					else if (Action == TypeOfAction.Delete)
					{
						SearchForCrap(Folder, FoldersList[Folder], Context);
					}
				}
			}
		}

		private void AddToSyncingList(string Path, TypeOfItem Type, SideOfSource Side, TypeOfAction Action, bool IsUpdate)
		{
			SyncingItem Entry = new SyncingItem
			{
				Path = Path,
				Type = Type,
				Side = Side,
				Action = Action,
				IsUpdate = IsUpdate,
				RealId = SyncingList.Count
			};

			SyncingList.Add(Entry);
			if (Entry.Action != TypeOfAction.Delete)
				AddValidFile(Type == TypeOfItem.Folder ? Entry.Path : GetCompressedName(Entry.Path));

			switch (Entry.Action)
			{
				case TypeOfAction.Copy:
					if (Entry.Type == TypeOfItem.Folder)
					{
						Status.FoldersToCreate += 1;
					}
					else if (Entry.Type == TypeOfItem.File)
					{
						Status.FilesToCreate += 1;
					}
					break;
				case TypeOfAction.Delete:
					if (Entry.Type == TypeOfItem.Folder)
					{
						Status.FoldersToDelete += 1;
					}
					else if (Entry.Type == TypeOfItem.File)
					{
						Status.FilesToDelete += 1;
					}
					break;
			}
			switch (Entry.Side)
			{
				case SideOfSource.Left:
					Status.LeftActionsCount += 1;
					break;
				case SideOfSource.Right:
					Status.RightActionsCount += 1;
					break;
			}
			Status.TotalActionsCount += 1;
		}

		private void AddValidFile(string File)
		{
			if (!IsValidFile(File))
				ValidFiles.Add(File.ToLower(Interaction.InvariantCulture), null);
		}

		private void AddValidAncestors(string Folder)
		{
			Log.LogInfo(string.Format("AddValidAncestors: Folder \"{0}\" is a top level folder, adding it's ancestors.", Folder));
			StringBuilder CurrentAncestor = new StringBuilder();
			List<string> Ancestors = new List<string>(Folder.Split(new char[] { ProgramSetting.DirSep }, StringSplitOptions.RemoveEmptyEntries));

			//The last ancestor is the folder itself, and will be added in SearchForChanges.
			for (int Depth = 0; Depth <= (Ancestors.Count - 1) - 1; Depth++)
			{
				CurrentAncestor.Append(ProgramSetting.DirSep).Append(Ancestors[Depth]);
				AddValidFile(CurrentAncestor.ToString());
				Log.LogInfo(string.Format("AddValidAncestors: [Valid folder] \"{0}\"", CurrentAncestor.ToString()));
			}
		}

		private void RemoveValidFile(string File)
		{
			if (IsValidFile(File))
				ValidFiles.Remove(File.ToLower(Interaction.InvariantCulture));
		}

		private bool IsValidFile(string File)
		{
			return ValidFiles.ContainsKey(File.ToLower(Interaction.InvariantCulture));
		}

		private void PopSyncingList(SideOfSource Side)
		{
			ValidFiles.Remove(SyncingList[SyncingList.Count - 1].Path);
			SyncingList.RemoveAt(SyncingList.Count - 1);

			Status.TotalActionsCount -= 1;
			switch (Side)
			{
				case SideOfSource.Left:
					Status.LeftActionsCount -= 1;
					break;
				case SideOfSource.Right:
					Status.RightActionsCount -= 1;
					break;
			}
		}


		// This procedure searches for changes in the source directory.
		private void SearchForChanges(string Folder, bool Recursive, SyncingContext Context)
		{
			string SourceFolder = CombinePathes(Context.SourceRootPath, Folder);
			string DestinationFolder = CombinePathes(Context.DestinationRootPath, Folder);

			//Exit on excluded folders (and optionally on hidden ones).
			if (!HasAcceptedDirname(Folder) || IsExcludedSinceHidden(SourceFolder) || IsSymLink(SourceFolder))
				return;

			UpdateLabel(StatusData.SyncStep.Scan, SourceFolder);
			Log.LogInfo(string.Format("=> Scanning folder \"{0}\" for new or updated files.", Folder));

			//LATER: Factor out.
			bool IsNewFolder = !Directory.Exists(DestinationFolder);
			bool ShouldUpdateFolder = IsNewFolder || AttributesChanged(SourceFolder, DestinationFolder);
			if (ShouldUpdateFolder)
			{
				AddToSyncingList(Folder, TypeOfItem.Folder, Context.Source, TypeOfAction.Copy, !IsNewFolder);
				Log.LogInfo(string.Format("SearchForChanges: {0} \"{1}\" \"{2}\" ({3})", IsNewFolder ? "[New folder]" : "[Updated folder]", SourceFolder, DestinationFolder, Folder));
			}
			else
			{
				AddValidFile(Folder);
				Log.LogInfo(string.Format("SearchForChanges: [Valid folder] \"{0}\" \"{1}\" ({2})", SourceFolder, DestinationFolder, Folder));
			}

			int InitialValidFilesCount = ValidFiles.Count;
			try
			{
				foreach (string SourceFile in Directory.GetFiles(SourceFolder))
				{
					Log.LogInfo("Scanning " + SourceFile);
					string DestinationFile = GetCompressedName(CombinePathes(DestinationFolder, Path.GetFileName(SourceFile)));

					try
					{
						if (IsIncludedInSync(SourceFile))
						{
							bool IsNewFile = !File.Exists(DestinationFile);
							string RelativeFilePath = SourceFile.Substring(Context.SourceRootPath.Length);

							if (IsNewFile || SourceIsMoreRecent(SourceFile, DestinationFile))
							{
								AddToSyncingList(RelativeFilePath, TypeOfItem.File, Context.Source, TypeOfAction.Copy, !IsNewFile);
								Log.LogInfo(string.Format("SearchForChanges: {0} \"{1}\" \"{2}\" ({3}).", IsNewFile ? "[New File]" : "[Updated file]", SourceFile, DestinationFile, RelativeFilePath));

								if (Program.ProgramConfig.GetProgramSetting<bool>(ProgramSetting.Forecast, false))
									Status.BytesToCopy += Utilities.GetSize(SourceFile);
								//Degrades performance.
							}
							else
							{
								//Adds an entry to not delete this when cleaning up the other side.
								AddValidFile(GetCompressedName(RelativeFilePath));
								Log.LogInfo(string.Format("SearchForChanges: [Valid] \"{0}\" \"{1}\" ({2})", SourceFile, DestinationFile, RelativeFilePath));
							}
						}
						else
						{
							Log.LogInfo(string.Format("SearchForChanges: [Excluded file] \"{0}\"", SourceFile));
						}

					}
					catch (Exception Ex)
					{
						Log.HandleError(Ex, SourceFile);
					}

					Status.FilesScanned += 1;
				}
			}
			catch (Exception Ex)
			{
				Log.HandleSilentError(Ex);
				//Error with entering the folder || Thread aborted.
			}

			if (Recursive | AutoInclude)
			{
				try
				{
					foreach (string SubFolder in Directory.GetDirectories(SourceFolder))
					{
						if (Recursive || (AutoInclude && Directory.GetCreationTimeUtc(SubFolder) > MDate))
						{
							SearchForChanges(SubFolder.Substring(Context.SourceRootPath.Length), true, Context);
						}
					}
				}
				catch (Exception Ex)
				{
					Log.HandleSilentError(Ex);
				}
			}

			if (InitialValidFilesCount == ValidFiles.Count)
			{
				if (!Handler.GetSetting<bool>(ProfileSetting.ReplicateEmptyDirectories, true))
				{
					if (ShouldUpdateFolder)
					{
						//Don't create/update this folder.
						Status.FoldersToCreate -= 1;
						PopSyncingList(Context.Source);
					}

					RemoveValidFile(Folder);
					//Folders added for creation are marked as valid in AddToSyncingList. Removing this mark is vital to ensuring that the ReplicateEmptyDirectories setting works correctly (otherwise the count increases.

					//Problem: What if ancestors of a folder have been marked valid, and the folder is empty?
					//If the folder didn't exist, it's ancestors won't be created, since only the folder itself is added.
					//Yet if ancestors exist, should they be removed? Let's say NO for now.
				}
			}
		}

		private void SearchForCrap(string Folder, bool Recursive, SyncingContext Context)
		{
			//Here, Source is set to be the right folder, and dest to be the left folder
			string SourceFolder = CombinePathes(Context.SourceRootPath, Folder);
			string DestinationFolder = CombinePathes(Context.DestinationRootPath, Folder);

			// Folder exclusion doesn't work exactly the same as file exclusion: if "Source\a" is excluded, "Dest\a" doesn't get deleted. That way one can safely exclude "Source\System Volume Information" and the like.
			if (!HasAcceptedDirname(Folder) || IsExcludedSinceHidden(SourceFolder) || IsSymLink(SourceFolder))
				return;

			UpdateLabel(StatusData.SyncStep.Scan, SourceFolder);
			Log.LogInfo(string.Format("=> Scanning folder \"{0}\" for files to delete.", Folder));
			try
			{
				foreach (string File in System.IO.Directory.GetFiles(SourceFolder))
				{
					string RelativeFName = File.Substring(Context.SourceRootPath.Length);

					try
					{
						if (!IsValidFile(RelativeFName))
						{
							AddToSyncingList(RelativeFName, TypeOfItem.File, Context.Source, TypeOfAction.Delete, false);
							Log.LogInfo(string.Format("Cleanup: [Delete] \"{0}\" ({1})", File, RelativeFName));
						}
						else
						{
							Log.LogInfo(string.Format("Cleanup: [Keep] \"{0}\" ({1})", File, RelativeFName));
						}

					}
					catch (Exception Ex)
					{
						Log.HandleError(Ex);
					}

					Status.FilesScanned += 1;
				}
			}
			catch (Exception Ex)
			{
				Log.HandleSilentError(Ex);
			}

			if (Recursive | AutoInclude)
			{
				try
				{
					foreach (string SubFolder in Directory.GetDirectories(SourceFolder))
					{
						if (Recursive || (AutoInclude && Directory.GetCreationTimeUtc(SubFolder) > MDate))
						{
							SearchForCrap(SubFolder.Substring(Context.SourceRootPath.Length), true, Context);
						}
					}
				}
				catch (Exception Ex)
				{
					Log.HandleSilentError(Ex);
				}
			}

			// Folder.Length = 0 <=> This is the root folder, not to be deleted.
			if (Folder.Length != 0 && !IsValidFile(Folder))
			{
				Log.LogInfo(string.Format("Cleanup: [Delete folder] \"{0}\" ({1}).", DestinationFolder, Folder));
				AddToSyncingList(Folder, TypeOfItem.Folder, Context.Source, TypeOfAction.Delete, false);
			}
		}

		private void SyncFileAttributes(string SourceFile, string DestFile)
		{
			//Updating attributes is needed.
			if (Handler.GetSetting<int>(ProfileSetting.TimeOffset, 0) != 0)
			{
				Log.LogInfo("SyncFileAttributes: DST: Setting attributes to normal; current attributes: " + File.GetAttributes(DestFile));
				File.SetAttributes(DestFile, FileAttributes.Normal);
				//Tracker #2999436
				Log.LogInfo("SyncFileAttributes: DST: Setting last write time");
				//Must use IO.File.GetLastWriteTimeUtc(**DestFile**), because it might differ from IO.File.GetLastWriteTimeUtc(**SourceFile**) (rounding, DST settings, ...)
				File.SetLastWriteTimeUtc(DestFile, File.GetLastWriteTimeUtc(DestFile).AddHours(Handler.GetSetting<int>(ProfileSetting.TimeOffset, 0)));
				Log.LogInfo("SyncFileAttributes: DST: Last write time set to " + File.GetLastWriteTimeUtc(DestFile));
			}

			Log.LogInfo("SyncFileAttributes: Setting attributes to " + File.GetAttributes(SourceFile));
			File.SetAttributes(DestFile, File.GetAttributes(SourceFile));
			Log.LogInfo("SyncFileAttributes: Attributes set to " + File.GetAttributes(DestFile));
		}

		private static void SafeCopy(string SourceFile, string DestFile)
		{
			string TempDest = null;
			string DestBack = null;
			do
			{
				TempDest = DestFile + "-" + Path.GetRandomFileName();
				DestBack = DestFile + "-" + Path.GetRandomFileName();
			} while (File.Exists(TempDest) | File.Exists(DestBack));

			File.Copy(SourceFile, TempDest, false);
			File.Move(DestFile, DestBack);
			File.Move(TempDest, DestFile);
			File.Delete(DestBack);
		}

		private void CopyFile(string SourceFile, string DestFile)
		{
			string CompressedFile = GetCompressedName(DestFile);
			Log.LogInfo(string.Format("CopyFile: Source: {0}, Destination: {1}", SourceFile, DestFile));
			if (File.Exists(DestFile))
			{
				File.SetAttributes(DestFile, FileAttributes.Normal);
			}
			bool Compression = CompressedFile != DestFile;
			DestFile = CompressedFile;
			if (Compression)
			{
				Compressor GZipCompressor = LoadCompressionDll();
				bool Decompress = Handler.GetSetting<bool>(ProfileSetting.Decompress, false);
				GZipCompressor.CompressFile(SourceFile, CompressedFile, Decompress, (long Progress) => { Status.BytesCopied += Progress; }); //, ByRef ContinueRunning As Boolean) 'ContinueRunning = Not [STOP]
			}
			else
			{
				if (File.Exists(DestFile))
				{
					try
					{
						using (FileStream TestForAccess = new FileStream(SourceFile, FileMode.Open, FileAccess.Read, FileShare.None))
						{
						}
						//Checks whether the file can be accessed before trying to copy it. This line was added because if the file is only partially locked, CopyFileEx starts copying it, then fails on the way, and deletes the destination.
						File.Copy(SourceFile, DestFile, true);
					}
					catch (IOException Ex)
					{
						Log.LogInfo(string.Format("Copy failed with message \"{0}\": Retrying in safe mode", Ex.Message));
						SafeCopy(SourceFile, DestFile);
					}
				}
				else
				{
					File.Copy(SourceFile, DestFile);
				}
			}

			try
			{
				SyncFileAttributes(SourceFile, DestFile);
			}
			catch (UnauthorizedAccessException Ex)
			{
				//This section addresses a subtle bug on NAS drives: if you reconfigure your NAS and change user settings, some files may cause access denied exceptions when trying to update their attributes. Resetting the file is the only solution I've found.
				Log.LogInfo("CopyFile: Syncing file attributes failed. Retrying");
				SafeCopy(SourceFile, DestFile);
				SyncFileAttributes(SourceFile, DestFile);
			}

			Status.CreatedFiles += 1;
			if (!Compression)
				Status.BytesCopied += Utilities.GetSize(SourceFile);
			if (Handler.GetSetting<bool>(ProfileSetting.Checksum, false) && Md5(SourceFile) != Md5(DestFile))
				throw new System.Security.Cryptography.CryptographicException("MD5 validation: failed.");
		}
		#endregion

		#region " Functions "
		private bool IsExcludedSinceHidden(string Path)
		{
			//File.GetAttributes works for folders ; see http://stackoverflow.com/questions/8110646/
			return Handler.GetSetting<bool>(ProfileSetting.ExcludeHidden, false) && (File.GetAttributes(Path) & FileAttributes.Hidden) != 0;
		}

		private bool IsTooOld(string Path)
		{
			int Days = Handler.GetSetting<int>(ProfileSetting.DiscardAfter, 0);
			return ((Days > 0) && (DateTime.UtcNow - File.GetLastWriteTimeUtc(Path)).TotalDays > Days);
		}

		private bool IsIncludedInSync(string FullPath)
		{
			if (IsExcludedSinceHidden(FullPath) || IsTooOld(FullPath))
				return false;

			try
			{
				switch (Handler.GetSetting<int>(ProfileSetting.Restrictions))
				{
					case 1:
						return FileNamePattern.MatchesPattern(GetFileOrFolderName(FullPath), IncludedPatterns);
					case 2:
						return !FileNamePattern.MatchesPattern(GetFileOrFolderName(FullPath), ExcludedPatterns);
				}
				//TODO: When?
			}
			catch (Exception Ex)
			{
				Log.HandleSilentError(Ex);
			}

			return true;
		}

		private bool HasAcceptedDirname(string Path)
		{
			return !FileNamePattern.MatchesPattern(Path, ref ExcludedDirPatterns);
		}


		private string GetCompressedName(string OriginalName)
		{
			string Extension = Handler.GetSetting<string>(ProfileSetting.CompressionExt, "");

			if (!string.IsNullOrEmpty(Extension) && Handler.GetSetting<bool>(ProfileSetting.Decompress, false))
			{
				return OriginalName.EndsWith(Extension) ? OriginalName.Substring(0, OriginalName.LastIndexOf(Extension)) : OriginalName;
			}
			else
			{
				return OriginalName + Extension;
			}
			//AndAlso Utilities.GetSize(File) > ConfigOptions.CompressionThreshold
		}

		private bool AttributesChanged(string AbsSource, string AbsDest)
		{
			const FileAttributes AttributesMask = FileAttributes.Hidden | FileAttributes.System | FileAttributes.Encrypted;

			// Disabled by default, and in two-ways mode.
			// TODO: Enable by default. It's currently disabled because some network drives do not update attributes correctly.
			if (!Handler.GetSetting<bool>(ProfileSetting.SyncFolderAttributes, false))
				return false;
			if (Handler.GetSetting<int>(ProfileSetting.Method, ProfileSetting.DefaultMethod) == (int)ProfileSetting.SyncMethod.BiIncremental)
				return false;

			try
			{
				return ((File.GetAttributes(AbsSource) & AttributesMask) != (File.GetAttributes(AbsDest) & AttributesMask));
			}
			catch (Exception Ex)
			{
				return false;
			}
		}

		//Error catching for this function is done in the calling section
		private bool SourceIsMoreRecent(string AbsSource, string AbsDest)
		{
			//Assumes Source and Destination exist.
			if ((!Handler.GetSetting<bool>(ProfileSetting.PropagateUpdates, true)))
				return false;
			//LATER: Require expert mode?

			Log.LogInfo(string.Format("SourceIsMoreRecent: {0}, {1}", AbsSource, AbsDest));

			DateTime SourceFATTime = NTFSToFATTime(IO.File.GetLastWriteTimeUtc(AbsSource)).AddHours(Handler.GetSetting<int>(ProfileSetting.TimeOffset, 0));
			DateTime DestFATTime = NTFSToFATTime(IO.File.GetLastWriteTimeUtc(AbsDest));
			Log.LogInfo(string.Format("SourceIsMoreRecent: S:({0}, {1}); D:({2}, {3})",
				Interaction.FormatDate(File.GetLastWriteTimeUtc(AbsSource)), Interaction.FormatDate(SourceFATTime),
				Interaction.FormatDate(File.GetLastWriteTimeUtc(AbsDest)), Interaction.FormatDate(DestFATTime)));

			if (Handler.GetSetting<bool>(ProfileSetting.FuzzyDstCompensation, false))
			{
				int HoursDiff = Convert.ToInt32((SourceFATTime - DestFATTime).TotalHours);
				if (Math.Abs(HoursDiff) == 1)
					DestFATTime = DestFATTime.AddHours(HoursDiff);
			}

			//StrictMirror is disabled in constructor if Method != LRMirror
			if (SourceFATTime < DestFATTime && (!Handler.GetSetting<bool>(ProfileSetting.StrictMirror, false)))
				return false;

			//User-enabled checks
			if (Handler.GetSetting<bool>(ProfileSetting.Checksum, false) && Md5(AbsSource) != Md5(AbsDest))
				return true;
			if (Handler.GetSetting<bool>(ProfileSetting.CheckFileSize, false) && Utilities.GetSize(AbsSource) != Utilities.GetSize(AbsDest))
				return true;

			if (Handler.GetSetting<bool>(ProfileSetting.StrictDateComparison, true))
			{
				if (SourceFATTime == DestFATTime)
					return false;
			}
			else
			{
				if (Math.Abs((SourceFATTime - DestFATTime).TotalSeconds) <= 4)
					return false;
				//Note: NTFSToFATTime introduces additional fuzziness (justifies the <= ('=')).
			}
			Log.LogInfo("SourceIsMoreRecent: Filetimes differ");

			return true;
		}

		private bool IsSymLink(string SubFolder)
		{
#if LINUX
		if ((IO.File.GetAttributes(SubFolder) & IO.FileAttributes.ReparsePoint) != 0) {
			Log.LogInfo(string.Format("Symlink detected: {0}; not following.", SubFolder));
			return true;
		}
#endif
			return false;
		}
		#endregion

		#region Shared functions
		private static string CombinePathes(string Dir, string File)
		{
			//TODO: Should be optimized; IO.Path?
			return Dir.TrimEnd(ProgramSetting.DirSep) + ProgramSetting.DirSep + File.TrimStart(ProgramSetting.DirSep);
		}

		private static Compressor LoadCompressionDll()
		{
			System.Reflection.Assembly DLL = System.Reflection.Assembly.LoadFrom(Program.ProgramConfig.CompressionDll);

			foreach (Type SubType in DLL.GetTypes())
			{
				if (typeof(Compressor).IsAssignableFrom(SubType))
					return (Compressor)Activator.CreateInstance(SubType);
			}

			throw new ArgumentException("Invalid DLL: " + Program.ProgramConfig.CompressionDll);
		}

		private static string Md5(string Path)
		{
			using (StreamReader DataStream = new StreamReader(Path))
			{
				using (System.Security.Cryptography.MD5CryptoServiceProvider CryptObject = new System.Security.Cryptography.MD5CryptoServiceProvider())
				{
					return Convert.ToBase64String(CryptObject.ComputeHash(DataStream.BaseStream));
				}
			}
		}

		private static DateTime NTFSToFATTime(DateTime NTFSTime)
		{
			return (new DateTime(NTFSTime.Year, NTFSTime.Month, NTFSTime.Day, NTFSTime.Hour, NTFSTime.Minute, NTFSTime.Second).AddSeconds(NTFSTime.Millisecond == 0 ? NTFSTime.Second % 2 : 2 - (NTFSTime.Second % 2)));
		}
		#endregion

		#region " Tests "
#if DEBUG
		public struct DatePair
		{
			public DateTime Ntfs;

			public System.DateTime FAT;
			[System.Diagnostics.DebuggerStepThrough()]
			public DatePair(DateTime NtfsTime, DateTime FatTime)
			{
				Ntfs = NtfsTime;
				FAT = FatTime;
			}
		}

		public static void Check_NTFSToFATTime()
		{
			Check_StaticFATTimes();
			Check_HardwareFATTimes();
		}

		//Note: This could be a useful function for NAS drives known to round NTFS timestamps, but currently only DLink does, and they do it incorrectly (there's a bug in their drivers)
		private static DateTime RoundToSecond(DateTime NTFSTime)
		{
			return (new DateTime(NTFSTime.Year, NTFSTime.Month, NTFSTime.Day, NTFSTime.Hour, NTFSTime.Minute, NTFSTime.Second).AddSeconds(NTFSTime.Millisecond > 500 ? 1 : 0));
		}

		public static void Check_StaticFATTimes()
		{
			System.Diagnostics.Debug.WriteLine("Starting hardcoded NTFS -> FAT tests");
			DateTime sevenThirtyOne = DateTime.Parse("7:31:00 AM");
			List<DatePair> Tests = new List<DatePair> {
				new DatePair(sevenThirtyOne, sevenThirtyOne),
				new DatePair(sevenThirtyOne.AddMilliseconds(1), sevenThirtyOne.AddSeconds(2)),
				new DatePair(sevenThirtyOne.AddSeconds(1), sevenThirtyOne.AddSeconds(2)),
				new DatePair(sevenThirtyOne.AddSeconds(1).AddMilliseconds(999), sevenThirtyOne.AddSeconds(2))
			};
			foreach (DatePair Test in Tests)
			{
				DateTime Actual = NTFSToFATTime(Test.Ntfs);
				string Result = string.Format("Check_NTFSToFATTime: {0} -> {1} ({2} expected) --> {3}", Test.Ntfs, Actual, Test.FAT, Actual == Test.FAT ? "Ok" : "Failed");
				System.Diagnostics.Debug.WriteLine(Result);
			}
			System.Diagnostics.Debug.WriteLine("Done!");
		}

		public static void Check_HardwareFATTimes()
		{
			using (StreamWriter LogWriter = new StreamWriter("C:\\FatTimes.txt", false))
			{
				LogWriter.WriteLine("Starting dynamic NTFS -> FAT tests");
				string Source = "C:\\NtfsTests";
				string Destination = "Z:\\NtfsTests";
				if (Directory.Exists(Source))
					Directory.Delete(Source, true);
				if (Directory.Exists(Destination))
					Directory.Delete(Destination, true);

				Directory.CreateDirectory(Source);
				Directory.CreateDirectory(Destination);

				DateTime BaseDate = DateTime.Today.AddHours(8);
				string FormatString = "{0,-15}{1,-15}{2,-15}{3,-15}{4,-15}{5,-15}{6,-15}";

				LogWriter.WriteLine(string.Format(FormatString, "Input", "Source", "Dest (Created)", "Dest (Copied)", "ForecastedDate", "Rounded", "Equal?"));

				for (int ms = 0; ms <= 61000; ms += 71)
				{
					DateTime InputDate = BaseDate.AddMilliseconds(ms);
					string SourcePath = Path.Combine(Source, ms.ToString());
					string DestPath_Created = Path.Combine(Destination, ms.ToString() + "-created");
					string DestPath_Copied = Path.Combine(Destination, ms.ToString() + "-copied");
					File.Create(SourcePath).Close();
					File.Create(DestPath_Created).Close();

					File.SetLastWriteTime(SourcePath, InputDate);
					File.SetLastWriteTime(DestPath_Created, InputDate);
					File.Copy(SourcePath, DestPath_Copied);

					DateTime SourceDate = File.GetLastWriteTime(SourcePath);
					DateTime DestCreatedDate = File.GetLastWriteTime(DestPath_Created);
					DateTime DestCopiedDate = File.GetLastWriteTime(DestPath_Copied);
					DateTime ForecastedDate = NTFSToFATTime(InputDate);
					DateTime RoundedDate = RoundToSecond(InputDate);
					bool Equal = InputDate == SourceDate & DestCreatedDate == DestCopiedDate & DestCopiedDate == ForecastedDate;

					File.Delete(SourcePath);
					File.Delete(DestPath_Copied);
					File.Delete(DestPath_Created);

					LogWriter.WriteLine(FormatString, FormatDate(InputDate), FormatDate(SourceDate), FormatDate(DestCreatedDate), FormatDate(DestCopiedDate), FormatDate(ForecastedDate), FormatDate(RoundedDate), Equal);
				}

				Directory.Delete(Source, true);
				Directory.Delete(Destination, true);

				LogWriter.WriteLine("Done!");
			}
		}
#endif
		#endregion
	}

}
