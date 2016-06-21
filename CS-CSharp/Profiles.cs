using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace CreateSync
{
	static internal class ProfileSetting
	{
		public const string Source = "Source Directory";
		public const string Destination = "Destination Directory";
		public const string IncludedTypes = "Included Filetypes";
		public const string ExcludedTypes = "Excluded FileTypes";
		public const string ReplicateEmptyDirectories = "Replicate Empty Directories";
		public const string Method = "Synchronization Method";
		public const string Restrictions = "Files restrictions";
		public const string LeftSubFolders = "Source folders to be synchronized";
		public const string RightSubFolders = "Destination folders to be synchronized";
		public const string MayCreateDestination = "Create destination folder";
		public const string StrictDateComparison = "Strict date comparison";
		public const string PropagateUpdates = "Propagate Updates";
		public const string StrictMirror = "Strict mirror";
		public const string TimeOffset = "Time Offset";
		public const string LastRun = "Last run";
		public const string CatchUpSync = "Catch up if missed";
		public const string CompressionExt = "Compress";
		public const string Group = "Group";
		public const string CheckFileSize = "Check file size";
		public const string FuzzyDstCompensation = "Fuzzy DST compensation";

		public const string Checksum = "Checksum";
		//Next settings are hidden, not automatically appended to config files.
		public const string ExcludedFolders = "Excluded folder patterns";
		public const string WakeupAction = "Wakeup action";
		public const string PostSyncAction = "Post-sync action";
		public const string ExcludeHidden = "Exclude hidden entries";
		public const string DiscardAfter = "Discard after";
		public const string PreviewOnly = "Preview only";
		public const string SyncFolderAttributes = "Sync folder attributes";
		public const string ErrorsLog = "Track errors separately";
		//TODO: Not ready for mass use yet.
		public const string AutoIncludeNewFolders = "Auto-include new folders";
		public const string LastModified = "Last modified";
		public const string Decompress = "Decompress";
		//</>

		//Disabled: would require keeping a list of modified files to work, since once a source file is deleted in the source, there's no way to tell when it had been last modified, and hence no way to calculate the appropriate deletion date.
		//Public Const Delay As String = "Delay deletions"

		public const string Scheduling = "Scheduling";
		//Frequency;WeekDay;MonthDay;Hour;Minute
		public const int SchedulingSettingsCount = 5;

		public enum SyncMethod
		{
			LRMirror = 0,
			LRIncremental = 1,
			BiIncremental = 2
		}

		public const int DefaultMethod = Convert.ToInt32(SyncMethod.LRIncremental);
	}

	sealed class ProfileHandler
	{
		public string ProfileName;
		public bool IsNewProfile;

		public ScheduleInfo Scheduler = new ScheduleInfo();
		public string ConfigPath;
		public string LogPath;

		public string ErrorsLogPath;
		public Dictionary<string, string> Configuration = new Dictionary<string, string>();
		public Dictionary<string, bool> LeftCheckedNodes = new Dictionary<string, bool>();

		public Dictionary<string, bool> RightCheckedNodes = new Dictionary<string, bool>();
		//NOTE: Only vital settings should be checked for correctness, since the config will be rejected if a mismatch occurs.
		private static readonly string[] RequiredSettings = {
		ProfileSetting.Source,
		ProfileSetting.Destination,
		ProfileSetting.ExcludedTypes,
		ProfileSetting.IncludedTypes,
		ProfileSetting.LeftSubFolders,
		ProfileSetting.RightSubFolders,
		ProfileSetting.Method,
		ProfileSetting.Restrictions,
		ProfileSetting.ReplicateEmptyDirectories

	};
		public ProfileHandler(string Name)
		{
			ProfileName = Name;

			ConfigPath = Program.ProgramConfig.GetConfigPath(Name);
			LogPath = Program.ProgramConfig.GetLogPath(Name);
			ErrorsLogPath = Program.ProgramConfig.GetErrorsLogPath(Name);

			IsNewProfile = !LoadConfigFile();

			//Never use GetSetting(Of SyncMethod). It searches the config file for a string containing an int (eg "0"), but when failing it calls SetSettings which saves a string containing an enum label (eg. "LRIncremental")
			if (GetSetting(ProfileSetting.Method, ProfileSetting.DefaultMethod) != (int)ProfileSetting.SyncMethod.LRMirror)
			{
				//Disable Mirror-Specific settings.
				SetSetting<bool>(ProfileSetting.StrictMirror, false);
				SetSetting<int>(ProfileSetting.DiscardAfter, 0);
			}

			//Post-sync actions require a separate errors log
			if (GetSetting<string>(ProfileSetting.PostSyncAction) != null)
				SetSetting<bool>(ProfileSetting.ErrorsLog, true);

			//Sanity checks: if no folders were included on the right due to automatic destination creation, select all folders
			if (GetSetting(ProfileSetting.MayCreateDestination, false) & string.IsNullOrEmpty(GetSetting(ProfileSetting.RightSubFolders, "")))
				SetSetting<string>(ProfileSetting.RightSubFolders, "*");
		}

		public bool LoadConfigFile()
		{
			if (!File.Exists(ConfigPath))
				return false;

			Configuration.Clear();
			using (StreamReader FileReader = new StreamReader(ConfigPath))
			{
				while (!FileReader.EndOfStream)
				{
					string ConfigLine = "";

					ConfigLine = FileReader.ReadLine();
					string[] Param = ConfigLine.Split(":".ToCharArray(), 2);
					if (Param.Length < 2)
					{
						Interaction.ShowMsg(Translation.TranslateFormat("\\INVALID_SETTING", ConfigLine));
						Program.ProgramConfig.LogAppEvent("Invalid setting for profile '" + ProfileName + "': " + ConfigLine);
					}
					else if (!Configuration.ContainsKey(Param[0]))
					{
						Configuration.Add(Param[0], Param[1]);
					}
				}
			}

			LoadScheduler();
			LoadSubFoldersList(ProfileSetting.LeftSubFolders, ref LeftCheckedNodes);
			LoadSubFoldersList(ProfileSetting.RightSubFolders, ref RightCheckedNodes);

			return true;
		}

		public bool SaveConfigFile()
		{
			try
			{
				using (StreamWriter FileWriter = new StreamWriter(ConfigPath))
				{
					foreach (KeyValuePair<string, string> Setting in Configuration)
					{
						FileWriter.WriteLine(Setting.Key + ":" + Setting.Value);
					}
				}

				return true;
			}
			catch (Exception Ex)
			{
				Program.ProgramConfig.LogAppEvent("Unable to save config file for " + ProfileName + Environment.NewLine + Ex.ToString());
				return false;
			}
		}

		// `ReturnString` is used to pass locally generated error messages to caller.
		public bool ValidateConfigFile(bool WarnUnrootedPaths = false, bool TryCreateDest = false, string FailureMsg = null)
		{
			bool IsValid = true;
			List<string> InvalidListing = new List<string>();
			string Dest = TranslatePath(GetSetting<string>(ProfileSetting.Destination));
			bool NeedsWakeup = true;
			string Action = this.GetSetting<string>(ProfileSetting.WakeupAction);

			if (NeedsWakeup & Program.ProgramConfig.GetProgramSetting<bool>(ProgramSetting.ExpertMode, false) & Action != null)
			{
				try
				{
					//Call Wake-up script in a blocking way
					Process.Start(Action, Dest).WaitForExit();
					NeedsWakeup = false;
				}
				catch (Exception Ex)
				{
					Interaction.ShowMsg(Translation.Translate("\\WAKEUP_FAILED"));
					Program.ProgramConfig.LogAppEvent(Ex.ToString());
					IsValid = false;
				}
			}

			if (!Directory.Exists(TranslatePath(GetSetting<string>(ProfileSetting.Source))))
			{
				InvalidListing.Add(Translation.Translate("\\INVALID_SOURCE"));
				IsValid = false;
			}

			//TryCreateDest <=> When this function returns, the folder should exist.
			//MayCreateDest <=> Creating the destination folder is allowed for this folder.
			bool MayCreateDest = GetSetting(ProfileSetting.MayCreateDestination, false);
			if (MayCreateDest & TryCreateDest)
			{
				try
				{
					Directory.CreateDirectory(Dest);
				}
				catch (Exception Ex)
				{
					InvalidListing.Add(Translation.TranslateFormat("\\FOLDER_FAILED", Dest, Ex.Message));
				}
			}

			if ((!Directory.Exists(Dest)) & (TryCreateDest | (!MayCreateDest)))
			{
				InvalidListing.Add(Translation.Translate("\\INVALID_DEST"));
				IsValid = false;
			}

			foreach (string Key in RequiredSettings)
			{
				if (!Configuration.ContainsKey(Key))
				{
					IsValid = false;
					InvalidListing.Add(Translation.TranslateFormat("\\SETTING_UNSET", Key));
				}
			}

			if (!string.IsNullOrEmpty(GetSetting(ProfileSetting.CompressionExt, "")))
			{
				if (Array.IndexOf( new string[]{ ".gz", ".bz2"}, Configuration[ProfileSetting.CompressionExt] ) < 0) {
					IsValid = false;
					InvalidListing.Add("Unknown compression extension, or missing \".\":" + Configuration[ProfileSetting.CompressionExt]);
				}

				if (!File.Exists(Program.ProgramConfig.CompressionDll))
				{
					IsValid = false;
					InvalidListing.Add(string.Format("{0} not found!", Program.ProgramConfig.CompressionDll));
				}
			}

			if (!IsValid)
			{
				string ErrorsList = string.Join(Environment.NewLine, InvalidListing.ToArray());
				string ErrMsg = string.Format("{0} - {1}{2}{3}", ProfileName, Translation.Translate("\\INVALID_CONFIG"), Environment.NewLine, ErrorsList);

				if ((FailureMsg != null))
					FailureMsg = ErrMsg;
				if (!CommandLine.Quiet)
					Interaction.ShowMsg(ErrMsg, Translation.Translate("\\INVALID_CONFIG"), MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				return false;
			}
			else
			{
				if (WarnUnrootedPaths)
				{
					if (!Path.IsPathRooted(TranslatePath(GetSetting<string>(ProfileSetting.Source))))
					{
						if (Interaction.ShowMsg(Translation.TranslateFormat("\\LEFT_UNROOTED", Path.GetFullPath(GetSetting<string>(ProfileSetting.Source))), , MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
							return false;
					}

					if (!Path.IsPathRooted(TranslatePath(GetSetting<string>(ProfileSetting.Destination))))
					{
						if (Interaction.ShowMsg(Translation.TranslateFormat("\\RIGHT_UNROOTED", Path.GetFullPath(GetSetting<string>(ProfileSetting.Destination))), , MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
							return false;
					}
				}

				return true;
			}
		}

		public bool Rename(string NewName)
		{
			//Don't exit if there's a case change.
			if ((!string.Equals(ProfileName, NewName, StringComparison.OrdinalIgnoreCase)) & (File.Exists(Program.ProgramConfig.GetLogPath(NewName)) | File.Exists(Program.ProgramConfig.GetErrorsLogPath(NewName)) | File.Exists(Program.ProgramConfig.GetConfigPath(NewName))))
				return false;

			try
			{
				if (File.Exists(ErrorsLogPath))
					File.Move(ErrorsLogPath, Program.ProgramConfig.GetErrorsLogPath(NewName));
				if (File.Exists(LogPath))
					File.Move(LogPath, Program.ProgramConfig.GetLogPath(NewName));
				File.Move(ConfigPath, Program.ProgramConfig.GetConfigPath(NewName));

				ProfileName = NewName;
				//Not really useful in the current situation : profiles are reloaded just after renaming anyway.
			}
			catch
			{
				return false;
			}
			return true;
		}

		public void DeleteConfigFile()
		{
			File.Delete(ConfigPath);
			DeleteLogFiles();
		}

		public void DeleteLogFiles()
		{
			File.Delete(LogPath);
			File.Delete(ErrorsLogPath);
		}

		public void SetSetting<T>(string SettingName, T Value)
		{
			Configuration[SettingName] = Value.ToString();
			//Dates are serialized in a locale-dependent way.
		}

		public void CopySetting<T>(string Key, ref T Value, bool Load)
		{
			if (Load)
			{
				Value = GetSetting(Key, Value);
				//Passes the current value as default answer.
			}
			else
			{
				Configuration[Key] = Value != null ? Value.ToString() : null;
			}
		}

		//Modified T DefaultVal = null from VB conversion
		public T GetSetting<T>(string Key, T DefaultVal = default(T))
		{
			string Val = "";
			if (Configuration.TryGetValue(Key, out Val) && !string.IsNullOrEmpty(Val))
			{
				try
				{
					return (T)(object)Val;
				}
				catch
				{
					SetSetting<T>(Key, DefaultVal);
					//Couldn't convert the value to a proper format; resetting.
				}
			}
			return DefaultVal;
		}

		public void LoadScheduler()
		{
			string[] Opts = GetSetting(ProfileSetting.Scheduling, "").Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);

			if (Opts.GetLength(0) == ProfileSetting.SchedulingSettingsCount)
			{
				Scheduler = new ScheduleInfo(Opts[0], Opts[1], Opts[2], Opts[3], Opts[4]);
			}
			else
			{
				Scheduler = new ScheduleInfo { Frequency = ScheduleInfo.Freq.Never };
				//NOTE: Wrong strings default to never
			}
		}

		public void SaveScheduler()
		{
			SetSetting<string>(ProfileSetting.Scheduling, string.Join(";", new string[] {
			Scheduler.Frequency.ToString(),
			Scheduler.WeekDay.ToString(),
			Scheduler.MonthDay.ToString(),
			Scheduler.Hour.ToString(),
			Scheduler.Minute.ToString()
		}));
		}

		public void LoadSubFoldersList(string ConfigLine, ref Dictionary<string, bool> Subfolders)
		{
			Subfolders.Clear();
			List<string> ConfigCheckedFoldersList = new List<string>(GetSetting(ConfigLine, "").Split(';'));
			ConfigCheckedFoldersList.RemoveAt(ConfigCheckedFoldersList.Count - 1);
			//Removes the last, empty element
			// Warning: The trailing comma can't be removed when generating the configuration string.
			// Using StringSplitOptions.RemoveEmptyEntries would make no difference between ';' (root folder selected, no subfolders) and '' (nothing selected at all)

			for (int i = 0; i < ConfigCheckedFoldersList.Count; i++)
			{
				string Dir = ConfigCheckedFoldersList[i];

				bool Recursive = false;

				if (Dir.EndsWith("*"))
				{
					Recursive = true;
					Dir = Dir.Substring(0, Dir.Length - 1);
				}

				if (!Subfolders.ContainsKey(Dir))
					Subfolders.Add(Dir, Recursive);
			}
		}

		public static string TranslatePath(string Path)
		{
			if (Path == null)
				return null;
			return TranslatePath_Unsafe(Path).TrimEnd(ProgramSetting.DirSep);
			//Careful with Linux root
			//Prevents a very annoying bug, where the presence of a slash at the end of the base directory would confuse the engine (#3052979)
		}

		public static string TranslatePath_Inverse(string Path)
		{
#if !Linux
		if (System.Text.RegularExpressions.Regex.IsMatch(Path, "^(?<driveletter>[A-Z]\\:)(\\\\(?<relativepath>.*))?$")) {
			string Label = "";
			foreach (DriveInfo Drive in DriveInfo.GetDrives) {
				if (Drive.Name[0] == Path[0])
					Label = Drive.VolumeLabel;
			}
			if (!string.IsNullOrEmpty(Label))
				return string.Format("\"{0}\"\\{1}", Label, Path.Substring(2).Trim(ProgramSetting.DirSep)).TrimEnd(ProgramSetting.DirSep);
		}
#endif

			return Path;
		}

		private static string TranslatePath_Unsafe(string Path)
		{
			string Translated_Path = Path;

#if !Linux
		string Label = null;
		string RelativePath = null;
		if (Path.StartsWith("\"") | Path.StartsWith(":")) {
			int ClosingPos = Path.LastIndexOfAny("\":".ToCharArray());
			if (ClosingPos == 0)
				return "";
			//LINUX: Currently returns "" (aka linux root) if no closing op is found.

			Label = Path.Substring(1, ClosingPos - 1);
			RelativePath = Path.Substring(ClosingPos + 1);

			if (Path.StartsWith("\"") & !string.IsNullOrEmpty(Label)) {
				foreach (DriveInfo Drive in DriveInfo.GetDrives()) {
					//The drive's name ends with a "\". If RelativePath = "", then TrimEnd on the RelativePath won't do anything; that's why you trim after joining
					if (!(Drive.Name[0] == 'A') && Drive.IsReady && string.Compare(Drive.VolumeLabel, Label, true) == 0) {
						//This is the line why this function is called unsafe: no path should *ever* end with a DirSep, otherwise the system gets confused as to what base and added path sections are.
						Translated_Path = (Drive.Name + RelativePath.TrimStart(ProgramSetting.DirSep)).TrimEnd(ProgramSetting.DirSep);
						//Bug #3052979
						break; // TODO: might not be correct. Was : Exit For
					}
				}
			}
		}
#endif

			// Use a path-friendly version of the DATE constant.
			Environment.SetEnvironmentVariable("MMMYYYY", System.DateTime.Today.ToString("MMMyyyy").ToLower(Interaction.InvariantCulture));
			Environment.SetEnvironmentVariable("DATE", System.DateTime.Today.ToShortDateString.Replace('/', '-'));
			Environment.SetEnvironmentVariable("DAY", System.DateTime.Today.ToString("dd"));
			Environment.SetEnvironmentVariable("MONTH", System.DateTime.Today.ToString("MM"));
			Environment.SetEnvironmentVariable("YEAR", System.DateTime.Today.ToString("yyyy"));

			return Environment.ExpandEnvironmentVariables(Translated_Path);
		}

		public System.DateTime GetLastRun()
		{
			try
			{
				return GetSetting(ProfileSetting.LastRun, ScheduleInfo.DATE_NEVER);
				//NOTE: Conversion seems ok, but there might be locale-dependent problems arising.
			}
			catch
			{
				return ScheduleInfo.DATE_NEVER;
			}
		}

		public void SetLastRun()
		{
			SetSetting<DateTime>(ProfileSetting.LastRun, System.DateTime.Now);
			SaveConfigFile();
		}

		public string FormatLastRun(string Format = "")
		{
			System.DateTime LastRun = GetLastRun();
			return LastRun == ScheduleInfo.DATE_NEVER ? "-" : Translation.TranslateFormat("\\LAST_SYNC", (DateTime.Now - LastRun).Days.ToString(Format), (DateTime.Now - LastRun).Hours.ToString(Format), LastRun.ToString);
		}

		public string FormatMethod()
		{
			switch (GetSetting(ProfileSetting.Method, ProfileSetting.DefaultMethod))
			{
				//Important: (Of Integer)
				case (int)ProfileSetting.SyncMethod.LRMirror:
					return Translation.Translate("\\LR_MIRROR");
				case (int)ProfileSetting.SyncMethod.BiIncremental:
					return Translation.Translate("\\TWOWAYS_INCREMENTAL");
				default:
					return Translation.Translate("\\LR_INCREMENTAL");
			}
		}
	}

	struct SchedulerEntry
	{
		public string Name;
		public DateTime NextRun;
		public bool CatchUp;

		public bool HasFailed;
		public SchedulerEntry(string _Name, System.DateTime _NextRun, bool _Catchup, bool _HasFailed)
		{
			Name = _Name;
			NextRun = _NextRun;
			CatchUp = _Catchup;
			HasFailed = _HasFailed;
		}
	}

	struct ScheduleInfo
	{
		public enum Freq
		{
			Never,
			Daily,
			Weekly,
			Monthly
		}

		public Freq Frequency;
		public int WeekDay;
		public int MonthDay;
		public int Hour;
		//Sunday = 0
		public int Minute;
		public static readonly DateTime DATE_NEVER = DateTime.MaxValue;
		public static readonly DateTime DATE_CATCHUP = DateTime.MinValue;

		public ScheduleInfo(string Frq, string _WeekDay, string _MonthDay, string _Hour, string _Minute)
		{
			Hour = Convert.ToInt32(_Hour);
			Minute = Convert.ToInt32(_Minute);
			WeekDay = Convert.ToInt32(_WeekDay);
			MonthDay = Convert.ToInt32(_MonthDay);
			Frequency = Str2Freq(Frq);
		}

		private static Freq Str2Freq(string Str)
		{
			try
			{
				return (Freq)Enum.Parse(typeof(Freq), Str, true);
			}
			catch (ArgumentException Ex)
			{
				return Freq.Never;
			}
		}

		public TimeSpan GetInterval()
		{
			TimeSpan Interval = default(TimeSpan);
			switch (Frequency)
			{
				case Freq.Daily:
					Interval = new TimeSpan(1, 0, 0, 0);
					break;
				case Freq.Weekly:
					Interval = new TimeSpan(7, 0, 0, 0);
					break;
				case Freq.Monthly:
					Interval = DateTime.Today.AddMonths(1) - DateTime.Today;
					break;
				case Freq.Never:
					Interval = new TimeSpan(0);
					break;
			}

			return Interval;
		}

		public DateTime NextRun()
		{
			DateTime Now = DateTime.Now;
			DateTime Today = DateTime.Today;

			DateTime RunAt = default(DateTime);
			TimeSpan Interval = GetInterval();

			switch (Frequency)
			{
				case Freq.Daily:
					RunAt = Today.AddHours(Hour).AddMinutes(Minute);
					break;
				case Freq.Weekly:
					RunAt = Today.AddDays(WeekDay - (int)Today.DayOfWeek).AddHours(Hour).AddMinutes(Minute);
					break;
				case Freq.Monthly:
					RunAt = Today.AddDays(MonthDay - Today.Day).AddHours(Hour).AddMinutes(Minute);
					break;
				default:
					return DATE_NEVER;
			}

			//">=" prevents double-syncing. Using ">" could cause the scheduler to queue Date.Now as next run time.
			while (Now >= RunAt)
			{
				RunAt += Interval;
			}
			//Loop needed (eg when today = jan 1 and schedule = every 1st month day)
			return RunAt;
		}
	}
}
