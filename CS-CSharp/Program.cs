using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

//TODO: Complete port

namespace CreateSync
{
	/*static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			//TODO: Error-handling code used to go here, create new class?
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1());
		}
	}*/
	static class Program
	{
		static internal LanguageHandler Translation;
		static internal ConfigHandler ProgramConfig;
		static internal Dictionary<string, ProfileHandler> Profiles;
		static internal bool ReloadNeeded;

		static internal MainForm MainFormInstance;
		static internal Font SmallFont;

		static internal Font LargeFont;
		static internal MessageLoop MsgLoop;
		internal delegate void Action();
		//LATER: replace with .Net 4.0 standards.

		[STAThread()]
		public static void Main()
		{
			// Must come first
			Application.EnableVisualStyles();

			try
			{
				MsgLoop = new MessageLoop();
				if (!MsgLoop.ExitNeeded)
					Application.Run(MsgLoop);

			}
			catch (Exception Ex)
			{
				if (MessageBox.Show("A critical error has occured. Can we upload the error log? " + Environment.NewLine + "Here's what we would send:" + Environment.NewLine + Environment.NewLine + Ex.ToString() + Environment.NewLine + Environment.NewLine + "If not, you can copy this message using Ctrl+C and send it to createsoftware@users.sourceforge.net." + Environment.NewLine, "Critical error", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					System.Net.WebClient ReportingClient = new System.Net.WebClient();
					try
					{
						System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
						ReportingClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
						MessageBox.Show(ReportingClient.UploadString(Branding.Web + "code/bug.php", "POST",
							"version=" + Application.ProductVersion + "/" + assembly.GetName().Version.Build + "&msg=" + Ex.ToString()), "Bug report submitted!", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					catch (System.Net.WebException SubEx)
					{
						MessageBox.Show("Unable to submit report. Plead send the following to createsoftware@users.sourceforge.net (Ctrl+C): " + Environment.NewLine + Ex.ToString(), "Unable to submit report", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
					finally
					{
						ReportingClient.Dispose();
					}
				}
				throw;
			}
		}
	}

	internal sealed class MessageLoop : ApplicationContext
	{
		//= False
		public bool ExitNeeded;

		//= Nothing
		private System.Threading.Mutex Blocker;
		//= Nothing
		List<SchedulerEntry> ScheduledProfiles = new List<SchedulerEntry>();

		#region "Main program loop & first run"
		public MessageLoop() : base()
		{

			// Initialize ProgramConfig, Translation 
			InitializeSharedObjects();

			// Start logging
			Program.ProgramConfig.LogAppEvent(new string('=', 20));
			Program.ProgramConfig.LogAppEvent("Program started: " + Application.StartupPath);
			Program.ProgramConfig.LogAppEvent(string.Format("Profiles folder: {0}.", Program.ProgramConfig.ConfigRootDir));
			Interaction.ShowDebug(Translation.Translate("\\DEBUG_WARNING"), Translation.Translate("\\DEBUG_MODE"));

			//Read command line settings
			CommandLine.ReadArgs(new List<string>(Environment.GetCommandLineArgs()));

			// Check if multiple instances are allowed.
			if (CommandLine.RunAs == CommandLine.RunMode.Scheduler && SchedulerAlreadyRunning())
			{
				ProgramConfig.LogAppEvent("Scheduler already running; exiting.");
				ExitNeeded = true;
				return;
			}
			else
			{
				this.ThreadExit += MessageLoop_ThreadExit;
			}

			// Setup settings
			ReloadProfiles();
			ProgramConfig.LoadProgramSettings();
			if (!ProgramConfig.ProgramSettingsSet(ProgramSetting.AutoUpdates) | !ProgramConfig.ProgramSettingsSet(ProgramSetting.Language))
			{
				ProgramConfig.LogDebugEvent("Auto updates or language not set; launching first run dialog.");
				HandleFirstRun();
			}

			// Initialize Main, Updates
			InitializeForms();

			// Look for updates
			if ((!CommandLine.NoUpdates) & ProgramConfig.GetProgramSetting<bool>(ProgramSetting.AutoUpdates, false))
			{
				Threading.Thread UpdateThread = new Threading.Thread(Updates.CheckForUpdates);
				UpdateThread.Start();
			}

			if (CommandLine.Help)
			{
				Interaction.ShowMsg(string.Format("Create Synchronicity, version {1}.{0}{0}Profiles folder: \"{2}\".{0}{0}Available commands: see manual.{0}{0}License information: See \"Release notes.txt\".{0}{0}Full manual: See {3}.{0}{0}You can support this software! See {4}.{0}{0}Happy syncing!", Environment.NewLine, Application.ProductVersion, ProgramConfig.ConfigRootDir, Branding.Help, Branding.Contribute), "Help!");
#if DEBUG
				Text.StringBuilder FreeSpace = new Text.StringBuilder();
				foreach (DriveInfo Drive in DriveInfo.GetDrives())
				{
					if (Drive.IsReady)
					{
						FreeSpace.AppendLine(string.Format("{0} -> {1:0,0} B free/{2:0,0} B", Drive.Name, Drive.TotalFreeSpace, Drive.TotalSize));
					}
				}
				Interaction.ShowMsg(FreeSpace.ToString);
#endif
			}
			else
			{
				ProgramConfig.LogDebugEvent(string.Format("Initialization complete. Running as '{0}'.", CommandLine.RunAs.ToString));

				if (CommandLine.RunAs == CommandLine.RunMode.Queue | CommandLine.RunAs == CommandLine.RunMode.Scheduler)
				{
					Interaction.ToggleStatusIcon(true);

					if (CommandLine.RunAs == CommandLine.RunMode.Queue)
					{
						MainFormInstance.ApplicationTimer.Interval = 1000;
						MainFormInstance.ApplicationTimer.Tick += StartQueue;
					}
					else if (CommandLine.RunAs == CommandLine.RunMode.Scheduler)
					{
						MainFormInstance.ApplicationTimer.Interval = 15000;
						MainFormInstance.ApplicationTimer.Tick += Scheduling_Tick;
					}
					MainFormInstance.ApplicationTimer.Start();
					//First tick fires after ApplicationTimer.Interval milliseconds.
#if DEBUG
				}
				else if (CommandLine.RunAs == CommandLine.RunMode.Scanner)
				{
					Explore(CommandLine.ScanPath);
					ExitNeeded = true;
					return;
#endif
				}
				else
				{
					MainFormInstance.FormClosed += ReloadMainForm;
					MainFormInstance.Show();
				}
			}
		}

		private void MessageLoop_ThreadExit(object sender, System.EventArgs e)
		{
			ExitNeeded = true;
			Interaction.ToggleStatusIcon(false);

			// Save last window information. Don't overwrite config file if running in scheduler mode.
			if (!(CommandLine.RunAs == CommandLine.RunMode.Scheduler))
				ProgramConfig.SaveProgramSettings();

			//Calling ReleaseMutex would be the same, since Blocker necessary holds the mutex at this point (otherwise the app would have closed already).
			if (CommandLine.RunAs == CommandLine.RunMode.Scheduler)
				Blocker.Close();
			ProgramConfig.LogAppEvent("Program exited");

#if Debug And 0
		SynchronizeForm.Check_NTFSToFATTime();
#endif
		}

		private void ReloadMainForm(object sender, FormClosedEventArgs e)
		{
			if (ReloadNeeded)
			{
				MainFormInstance = new MainForm();
				MainFormInstance.FormClosed += this.ReloadMainForm;
				MainFormInstance.Show();
			}
			else
			{
				Application.Exit();
			}
		}

		public static void InitializeSharedObjects()
		{
			// Load program configuration
			ProgramConfig = ConfigHandler.GetSingleton;
			Translation = LanguageHandler.GetSingleton;
			Profiles = new Dictionary<string, ProfileHandler>();

			try
			{
				SmallFont = new Drawing.Font("Verdana", 7f);
				LargeFont = new Drawing.Font("Verdana", 8.25f);
			}
			catch (ArgumentException ex)
			{
				SmallFont = new Drawing.Font(Drawing.SystemFonts.MessageBoxFont.FontFamily.Name, 7f);
				LargeFont = Drawing.SystemFonts.MessageBoxFont;
			}

			// Create required folders
			Directory.CreateDirectory(ProgramConfig.LogRootDir);
			Directory.CreateDirectory(ProgramConfig.ConfigRootDir);
			Directory.CreateDirectory(ProgramConfig.LanguageRootDir);
		}

		public static void InitializeForms()
		{
			// Create MainForm
			MainFormInstance = new MainForm();

			//Load status icon
			Interaction.LoadStatusIcon();
			MainFormInstance.ToolStripHeader.Image = Interaction.StatusIcon.Icon.ToBitmap;
			Interaction.StatusIcon.ContextMenuStrip = MainFormInstance.StatusIconMenu;
		}

		public static void HandleFirstRun()
		{
			if (!ProgramConfig.ProgramSettingsSet(ProgramSetting.Language))
			{
				LanguageForm Lng = new LanguageForm();
				Lng.ShowDialog();
				Translation = LanguageHandler.GetSingleton(true);
			}

			if (!ProgramConfig.ProgramSettingsSet(ProgramSetting.AutoUpdates))
			{
				bool AutoUpdates = Interaction.ShowMsg(Translation.Translate("\\WELCOME_MSG"), Translation.Translate("\\FIRST_RUN"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes ? true : false;
				ProgramConfig.SetProgramSetting<bool>(ProgramSetting.AutoUpdates, AutoUpdates);
			}

			ProgramConfig.SaveProgramSettings();
		}

		public static void ReloadProfiles()
		{
			Profiles.Clear();
			//Initialized in InitializeSharedObjects

			foreach (string ConfigFile in Directory.GetFiles(ProgramConfig.ConfigRootDir, "*.sync"))
			{
				string Name = Path.GetFileNameWithoutExtension(ConfigFile);
				Profiles.Add(Name, new ProfileHandler(Name));
			}
		}
		#endregion

		#region "Scheduling"
		private bool SchedulerAlreadyRunning()
		{
			string MutexName = "[[Create Synchronicity scheduler]] " + Application.ExecutablePath.Replace(ProgramSetting.DirSep, '!').ToLower(Interaction.InvariantCulture);
			if (MutexName.Length > 260)
				MutexName = MutexName.Substring(0, 260);

			ProgramConfig.LogDebugEvent(string.Format("Registering mutex: \"{0}\"", MutexName));

			try
			{
				Blocker = new Threading.Mutex(false, MutexName);
			}
			catch (Threading.AbandonedMutexException Ex)
			{
				ProgramConfig.LogDebugEvent("Abandoned mutex detected");
				return false;
			}
			catch (System.UnauthorizedAccessException Ex)
			{
				ProgramConfig.LogDebugEvent("Acess to the Mutex forbidden");
				return true;
			}

			return (!Blocker.WaitOne(0, false));
		}

		public static void RedoSchedulerRegistration()
		{
			bool NeedToRunAtBootTime = false;
			foreach (ProfileHandler Profile in Profiles.Values)
			{
				NeedToRunAtBootTime = NeedToRunAtBootTime | (Profile.Scheduler.Frequency != ScheduleInfo.Freq.Never);
				if (Profile.Scheduler.Frequency != ScheduleInfo.Freq.Never)
					ProgramConfig.LogAppEvent(string.Format("Profile {0} requires the scheduler to run.", Profile.ProfileName));
			}

			try
			{
				if (NeedToRunAtBootTime && ProgramConfig.GetProgramSetting<bool>(ProgramSetting.AutoStartupRegistration, true))
				{
					ProgramConfig.RegisterBoot();
					if (CommandLine.RunAs == CommandLine.RunMode.Normal)
					{
						ProgramConfig.LogAppEvent("Starting scheduler");
						Diagnostics.Process.Start(Application.ExecutablePath, "/scheduler /noupdates" + CommandLine.Log ? " /log" : "");
					}
				}
				else
				{
					if (Microsoft.Win32.Registry.GetValue(ProgramSetting.RegistryRootedBootKey, ProgramSetting.RegistryBootVal, null) != null)
					{
						ProgramConfig.LogAppEvent("Unregistering program from startup list");
						Microsoft.Win32.Registry.CurrentUser.OpenSubKey(ProgramSetting.RegistryBootKey, true).DeleteValue(ProgramSetting.RegistryBootVal);
					}
				}
			}
			catch (Exception Ex)
			{
				Interaction.ShowMsg(Translation.Translate("\\UNREG_ERROR"), Translation.Translate("\\ERROR"), MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void StartQueue(System.Object sender, System.EventArgs e)
		{
			MainFormInstance.ApplicationTimer.Interval = ProgramConfig.GetProgramSetting<int>(ProgramSetting.Pause, 5000);
			//Wait 5s between profiles k and k+1, k > 0
			MainFormInstance.ApplicationTimer.Stop();
			ProcessProfilesQueue();
		}
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_ProcessProfilesQueue_ProfilesQueue_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();

		Queue<string> static_ProcessProfilesQueue_ProfilesQueue;
		private void ProcessProfilesQueue()
		{
			lock (static_ProcessProfilesQueue_ProfilesQueue_Init)
			{
				try
				{
					if (InitStaticVariableHelper(static_ProcessProfilesQueue_ProfilesQueue_Init))
					{
						static_ProcessProfilesQueue_ProfilesQueue = null;
					}
				}
				finally
				{
					static_ProcessProfilesQueue_ProfilesQueue_Init.State = 1;
				}
			}

			if (static_ProcessProfilesQueue_ProfilesQueue == null)
			{
				static_ProcessProfilesQueue_ProfilesQueue = new Queue<string>();

				ProgramConfig.LogAppEvent("Profiles queue: Queue created.");
				List<string> RequestedProfiles = new List<string>();

				if ((CommandLine.RunAll))
				{
					RequestedProfiles.AddRange(Profiles.Keys);
					//Overwrites previous initialization
				}
				else
				{
					List<string> RequestedGroups = new List<string>();
					foreach (string Entry in CommandLine.TasksToRun.Split(ProgramSetting.EnqueuingSeparator))
					{
						if (Entry.StartsWith(ProgramSetting.GroupPrefix))
						{
							RequestedGroups.Add(Entry.Substring(1));
						}
						else
						{
							RequestedProfiles.Add(Entry);
						}
					}

					foreach (ProfileHandler Profile in Profiles.Values)
					{
						if (RequestedGroups.Contains(Profile.GetSetting<string>(ProfileSetting.Group, "")))
							RequestedProfiles.Add(Profile.ProfileName);
					}
				}

				foreach (string Profile in RequestedProfiles)
				{
					if (Profiles.ContainsKey(Profile))
					{
						//Displays a message if there is a problem.
						if (Profiles(Profile).ValidateConfigFile())
						{
							ProgramConfig.LogAppEvent("Profiles queue: Registered profile " + Profile);
							static_ProcessProfilesQueue_ProfilesQueue.Enqueue(Profile);
						}
					}
					else
					{
						Interaction.ShowMsg(Translation.TranslateFormat("\\INVALID_PROFILE", Profile), Profile, , MessageBoxIcon.Error);
					}
				}
			}

			if (static_ProcessProfilesQueue_ProfilesQueue.Count == 0)
			{
				ProgramConfig.LogAppEvent("Profiles queue: Synced all profiles.");
				Application.Exit();
			}
			else
			{
				SynchronizeForm SyncForm = new SynchronizeForm(static_ProcessProfilesQueue_ProfilesQueue.Dequeue(), CommandLine.ShowPreview, false);
				SyncForm.SyncFinished += (string Name, bool Completed) => MainFormInstance.ApplicationTimer.Start();
				//Wait for 5 seconds before moving on.
				SyncForm.StartSynchronization(false);
			}
		}

		private void ScheduledProfileCompleted(string ProfileName, bool Completed)
		{
			if (Completed)
			{
				ProgramConfig.LogAppEvent("Scheduler: " + ProfileName + " completed successfully.");
				if (Profiles.ContainsKey(ProfileName))
					ScheduledProfiles.Add(new SchedulerEntry(ProfileName, Profiles(ProfileName).Scheduler.NextRun(), false, false));
			}
			else
			{
				ProgramConfig.LogAppEvent("Scheduler: " + ProfileName + " reported an error, and will run again in 4 hours.");
				// If ProfileName has been removed, ReloadScheduledProfiles will unschedule it.
				ScheduledProfiles.Add(new SchedulerEntry(ProfileName, System.DateTime.Now.AddHours(4), true, true));
			}
		}

		private void Scheduling_Tick(System.Object sender, System.EventArgs e)
		{
			if (ProgramConfig.CanGoOn == false)
				return;
			//Don't start next sync yet.

			ReloadScheduledProfiles();
			if (ScheduledProfiles.Count == 0)
			{
				ProgramConfig.LogAppEvent("Scheduler: No profiles left to run, exiting.");
				Application.Exit();
				return;
			}
			else
			{
				SchedulerEntry NextInQueue = ScheduledProfiles(0);
				string Status = Translation.TranslateFormat("\\SCH_WAITING", NextInQueue.Name, NextInQueue.NextRun == ScheduleInfo.DATE_CATCHUP ? "..." : NextInQueue.NextRun.ToString);
				Interaction.StatusIcon.Text = Status.Length >= 64 ? Status.Substring(0, 63) : Status;

				if (System.DateTime.Compare(NextInQueue.NextRun, System.DateTime.Now) <= 0)
				{
					ProgramConfig.LogAppEvent("Scheduler: Launching " + NextInQueue.Name);

					SynchronizeForm SyncForm = new SynchronizeForm(NextInQueue.Name, false, NextInQueue.CatchUp);
					SyncForm.SyncFinished += ScheduledProfileCompleted;
					ScheduledProfiles.RemoveAt(0);
					SyncForm.StartSynchronization(false);
				}
			}
		}

		private string Needle;
		private bool EqualityPredicate(SchedulerEntry Item)
		{
			return (Item.Name == Needle);
		}
		readonly Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag static_ReloadScheduledProfiles_OneDay_Init = new Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag();

		//Logic of this function:
		// A new entry is created. The need for catching up is calculated regardless of the current state of the list.
		// Then, a corresponding entry (same name) is searched for. If not found, then the new entry is simply added to the list.
		// OOH, if a corresponding entry is found, then
		//    If it's already late, or if changes would postpone it, then nothing happens.
		//    But if it's not late, and the change will bring the sync forward, then the new entry superseedes the previous one.
		//       Note: In the latter case, if current entry is marked as failed, then the next run time is loaded from it
		//             (that's to avoid infinite loops when eg. the backup medium is unplugged)
		//Needed! This allows to detect config changes.

		TimeSpan static_ReloadScheduledProfiles_OneDay;
		private void ReloadScheduledProfiles()
		{
			ReloadProfiles();
			foreach (KeyValuePair<string, ProfileHandler> Profile in Profiles)
			{
				string Name = Profile.Key;
				ProfileHandler Handler = Profile.Value;
				lock (static_ReloadScheduledProfiles_OneDay_Init)
				{
					try
					{
						if (InitStaticVariableHelper(static_ReloadScheduledProfiles_OneDay_Init))
						{
							static_ReloadScheduledProfiles_OneDay = new TimeSpan(1, 0, 0, 0);
						}
					}
					finally
					{
						static_ReloadScheduledProfiles_OneDay_Init.State = 1;
					}
				}

				if (Handler.Scheduler.Frequency != ScheduleInfo.Freq.Never)
				{
					SchedulerEntry NewEntry = new SchedulerEntry(Name, Handler.Scheduler.NextRun(), false, false);

					//<catchup>
					System.DateTime LastRun = Handler.GetLastRun();
					if (Handler.GetSetting<bool>(ProfileSetting.CatchUpSync, false) & LastRun != ScheduleInfo.DATE_NEVER & (NewEntry.NextRun - LastRun) > (Handler.Scheduler.GetInterval() + static_ReloadScheduledProfiles_OneDay))
					{
						ProgramConfig.LogAppEvent("Scheduler: Profile " + Name + " was last executed on " + LastRun.ToString + ", marked for catching up.");
						NewEntry.NextRun = ScheduleInfo.DATE_CATCHUP;
						NewEntry.CatchUp = true;
					}
					//</catchup>

					Needle = Name;
					int ProfileIndex = ScheduledProfiles.FindIndex(new Predicate<SchedulerEntry>(EqualityPredicate));
					if (ProfileIndex != -1)
					{
						SchedulerEntry CurEntry = ScheduledProfiles(ProfileIndex);

						//Don't postpone queued late backups
						if (NewEntry.NextRun != CurEntry.NextRun & CurEntry.NextRun >= System.DateTime.Now())
						{
							NewEntry.HasFailed = CurEntry.HasFailed;
							if (CurEntry.HasFailed)
								NewEntry.NextRun = CurEntry.NextRun;

							ScheduledProfiles.RemoveAt(ProfileIndex);
							ScheduledProfiles.Add(NewEntry);
							ProgramConfig.LogAppEvent("Scheduler: Re-registered profile for delayed run on " + NewEntry.NextRun.ToString + ": " + Name);
						}
					}
					else
					{
						ScheduledProfiles.Add(NewEntry);
						ProgramConfig.LogAppEvent("Scheduler: Registered profile for delayed run on " + NewEntry.NextRun.ToString + ": " + Name);
					}
				}
			}

			//Remove deleted or disabled profiles
			for (int ProfileIndex = ScheduledProfiles.Count - 1; ProfileIndex >= 0; ProfileIndex += -1)
			{
				if (!Profiles.ContainsKey(ScheduledProfiles(ProfileIndex).Name) || Profiles(ScheduledProfiles(ProfileIndex).Name).Scheduler.Frequency == ScheduleInfo.Freq.Never)
				{
					ScheduledProfiles.RemoveAt(ProfileIndex);
				}
			}

			//Tracker #3000728
			ScheduledProfiles.Sort((SchedulerEntry First, SchedulerEntry Second) => First.NextRun.CompareTo(Second.NextRun));
		}
		#endregion

#if DEBUG
		public static void Explore(string Path)
		{
			Path = Path.GetFullPath(Path);
			using (StreamWriter Writer = new StreamWriter(Path.Combine(ProgramConfig.LogRootDir, "scan-results.txt"), true))
			{
				Writer.WriteLine("== Exploring " + Path);

				Writer.WriteLine("** Not requesting any specific permissions");
				ExploreTree(Path, 1, Writer);

				try
				{
					Writer.WriteLine("** Requesting full permissions");
					Security.Permissions.FileIOPermission Permissions = new Security.Permissions.FileIOPermission(Security.Permissions.PermissionState.Unrestricted);
					Permissions.AllFiles = Security.Permissions.FileIOPermissionAccess.AllAccess;
					Permissions.Demand();
					ExploreTree(Path, 1, Writer);
				}
				catch (Security.SecurityException Ex)
				{
					Writer.WriteLine(Ex.Message);
				}
			}
		}

		public static void ExploreTree(string Path, int Depth, StreamWriter Stream)
		{
			string Indentation = "".PadLeft(Depth * 2);

			foreach (string File in Directory.GetFiles(Path))
			{
				try
				{
					Stream.Write(Indentation);
					Stream.Write(File);
					Stream.Write("\t" + File.Exists(File));
					Stream.Write("\t" + File.GetAttributes(File).ToString());
					Stream.Write("\t" + Interaction.FormatDate(File.GetCreationTimeUtc(File)));
					Stream.Write("\t" + Interaction.FormatDate(File.GetLastWriteTimeUtc(File)));
					Stream.WriteLine();
				}
				catch (Exception ex)
				{
					Stream.WriteLine(Indentation + ex.ToString);
				}
			}

			foreach (string Folder in Directory.GetDirectories(Path))
			{
				try
				{
					Stream.Write(Indentation);
					Stream.Write(Folder);
					Stream.Write("\t" + Directory.Exists(Folder));
					Stream.Write("\t" + File.GetAttributes(Folder).ToString());
					Stream.Write("\t" + Interaction.FormatDate(Directory.GetCreationTimeUtc(Folder)));
					Stream.Write("\t" + Interaction.FormatDate(Directory.GetLastWriteTimeUtc(Folder)));
					Stream.WriteLine();
				}
				catch (Exception ex)
				{
					Stream.WriteLine(Indentation + ex.ToString);
				}
				ExploreTree(Folder, Depth + 1, Stream);
			}
		}
#endif

#if Debug And 0
	public void VariousTests()
	{
		MessageBox.Show(string.IsNullOrEmpty(null));
		MessageBox.Show(string.IsNullOrEmpty(null));
		//MessageBox.Show(Nothing.ToString = "")
		//MessageBox.Show(Nothing.ToString = String.Empty)
		MessageBox.Show(string.IsNullOrEmpty(Convert.ToString(null)));
		MessageBox.Show(Convert.ToString(null) == string.Empty);

		//MessageBox.Show(CBool(""))
		//If "" Then MessageBox.Show(""""" -> True")
		if (null)
			MessageBox.Show("Nothing -> True");
		if (!null)
			MessageBox.Show("Nothing -> False");
		MessageBox.Show(Convert.ToBoolean(null));
		MessageBox.Show(Convert.ToString(null));

		MessageBox.Show(string.IsNullOrEmpty(Convert.ToString("")));
		MessageBox.Show(string.IsNullOrEmpty(Convert.ToString(null)));
	}
	static bool InitStaticVariableHelper(Microsoft.VisualBasic.CompilerServices.StaticLocalInitFlag flag)
	{
		if (flag.State == 0) {
			flag.State = 2;
			return true;
		} else if (flag.State == 2) {
			throw new Microsoft.VisualBasic.CompilerServices.IncompleteInitialization();
		} else {
			return false;
		}
	}
#endif
	}
}
