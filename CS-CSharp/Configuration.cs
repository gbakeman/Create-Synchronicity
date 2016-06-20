//TODO: Massive file cleanup, code analysis, testing, etc. (freshly ported from VB.Net)

using CreateSync;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

static internal class ProgramSetting
{
	//Main program settings
	public const string Language = "Language";
	public const string DefaultLanguage = "english";
	public const string AutoUpdates = "Auto updates";
	public const string MaxLogEntries = "Archived log entries";
	public const string MainView = "Main view";
	public const string FontSize = "Font size";
	public const string MainFormAttributes = "Window size and position";
	public const string ExpertMode = "Expert mode";
	public const string DiffProgram = "Diff program";
	public const string DiffArguments = "Diff arguments";
	public const string TextLogs = "Text logs";
	public const string Autocomplete = "Autocomplete";
	public const string Forecast = "Forecast";
	public const string Pause = "Pause";

	public const string AutoStartupRegistration = "Auto startup registration";
	//Program files
	public const string ConfigFolderName = "config";
	public const string LogFolderName = "log";
	public const string SettingsFileName = "mainconfig.ini";
	public const string AppLogName = "app.log";
	public const string DllName = "compress-decompress.dll";
	//Public CompressionThreshold As Integer = 0 'Better not filter at all

	//Used to parse excluded file types. For example, `folder"Documents"` means that folders named documents should be excluded.
	public const string ExcludedFolderPrefix = "folder";
	public const char GroupPrefix = ':';
/*#if CONFIG = "Linux"
	public const char EnqueuingSeparator = '|';
#elif
	public const char DirSep = '/';
#endif*/
	public const char DirSep = '\\';

#if DEBUG
	public const bool Debug = true;
#else
	public const int ForecastDelay = 0;
	public const bool Debug = false;
#endif
	public const int ForecastDelay = 60;

	//8 MB
	public const int AppLogThreshold = 1 << 23;

	public const string RegistryBootVal = "Create Synchronicity - Scheduler";
	public const string RegistryBootKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
	public const string RegistryRootedBootKey = "HKEY_CURRENT_USER\\" + RegistryBootKey;
}

sealed class ConfigHandler
{

	private static ConfigHandler Singleton;
	public string LogRootDir;
	public string ConfigRootDir;

	public string LanguageRootDir;
	public string CompressionDll;
	public string LocalNamesFile;
	public string MainConfigFile;
	public string AppLogFile;

	public string StatsFile;
	//To check whether a synchronization is already running (in scheduler mode only, queuing uses callbacks).
	public bool CanGoOn = true;

	internal System.Drawing.Icon Icon;
	private bool SettingsLoaded = false;

	private Dictionary<string, string> Settings = new Dictionary<string, string>();
	private ConfigHandler()
	{
		LogRootDir = GetUserFilesRootDir() + ProgramSetting.LogFolderName;
		ConfigRootDir = GetUserFilesRootDir() + ProgramSetting.ConfigFolderName;
		LanguageRootDir = Application.StartupPath + ProgramSetting.DirSep + "languages";

		StatsFile = ConfigRootDir + ProgramSetting.DirSep + "syncs-count.txt";
		LocalNamesFile = LanguageRootDir + ProgramSetting.DirSep + "local-names.txt";
		MainConfigFile = ConfigRootDir + ProgramSetting.DirSep + ProgramSetting.SettingsFileName;
		CompressionDll = Application.StartupPath + ProgramSetting.DirSep + ProgramSetting.DllName;
		AppLogFile = GetUserFilesRootDir() + ProgramSetting.AppLogName;

		try
		{
			Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
		}
		catch (ArgumentException Ex)
		{
			Icon = System.Drawing.Icon.FromHandle((new System.Drawing.Bitmap(32, 32)).GetHicon());
		}

		TrimAppLog();
		//Prevents app log from getting too large.
	}

	public static ConfigHandler GetSingleton()
	{
		if (Singleton == null)
			Singleton = new ConfigHandler();
		return Singleton;
	}

	// Useful for renaming logs, or in cases where a ProfileHandler isn't available.
	public string GetConfigPath(string Name)
	{
		return ConfigRootDir + ProgramSetting.DirSep + Name + ".sync";
	}

	public string GetLogPath(string Name)
	{
		//Return LogRootDir & ProgramSetting.DirSep & Name & ".log" & If(ProgramSetting.Debug Or GetProgramSetting(Of Boolean)(ProgramSetting.TextLogs, False), "", ".html")
		return LogRootDir + ProgramSetting.DirSep + Name + ".log" + (ProgramSetting.Debug | GetProgramSetting<bool>(ProgramSetting.TextLogs, false) ? "" : ".html");
	}

	public string GetErrorsLogPath(string Name)
	{
		return LogRootDir + ProgramSetting.DirSep + Name + ".errors.log";
	}

	//Return the place were config files are stored
	public string GetUserFilesRootDir()
	{
		string UserPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + ProgramSetting.DirSep + Branding.Brand + ProgramSetting.DirSep + Branding.Name + ProgramSetting.DirSep;

		//To change folder attributes: http://support.microsoft.com/default.aspx?scid=kb;EN-US;326549
		List<string> WriteNeededFolders = new List<string> {
			Application.StartupPath,
			Application.StartupPath + ProgramSetting.DirSep + ProgramSetting.LogFolderName,
			Application.StartupPath + ProgramSetting.DirSep + ProgramSetting.ConfigFolderName
		};
		bool ProgramPathExists = System.IO.Directory.Exists(Application.StartupPath + ProgramSetting.DirSep + ProgramSetting.ConfigFolderName);
		List<string> ToDelete = new List<string>();

		try
		{
			foreach (string Folder in WriteNeededFolders)
			{
				if (!System.IO.Directory.Exists(Folder))
					continue;

				string TestPath = Folder + ProgramSetting.DirSep + "write-permissions." + System.IO.Path.GetRandomFileName();
				System.IO.File.Create(TestPath).Close();
				ToDelete.Add(TestPath);

				if (Folder == Application.StartupPath)
					continue;
				foreach (string File in System.IO.Directory.GetFiles(Folder))
				{
					if ((System.IO.File.GetAttributes(File) & System.IO.FileAttributes.ReadOnly) == System.IO.FileAttributes.ReadOnly)
						throw new System.IO.IOException(File);
				}
			}

			foreach (string TestFile in ToDelete)
			{
				try
				{
					System.IO.File.Delete(TestFile);
				}
				catch (System.IO.IOException Ex)
				{
					// Silently fail when the file can't be found or is being used by another process 
				}
			}
		}
		catch (Exception Ex)
		{
			if (ProgramPathExists)
				Interaction.ShowMsg("Create Synchronicity cannot write to your installation directory, although it contains configuration files. Your Application Data folder will therefore be used instead." + Environment.NewLine + Ex.Message, "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
			return UserPath;
		}

		// When a user folder exists, and no config folder exists in the install dir, use the user's folder.
		return ProgramPathExists | !System.IO.Directory.Exists(UserPath) ? Application.StartupPath + ProgramSetting.DirSep : UserPath;
	}

	public T GetProgramSetting<T>(string Key, T DefaultVal)
	{
		string Val = "";
		if (Settings.TryGetValue(Key, out Val) && !string.IsNullOrEmpty(Val))
		{
			try
			{
				return (T)(object)Val;
			}
			catch
			{
				SetProgramSetting<T>(Key, DefaultVal);
				//Couldn't convert the value to a proper format; resetting.
			}
		}
		return DefaultVal;
	}

	public void SetProgramSetting<T>(string Key, T Value)
	{
		Settings[Key] = Value.ToString();
	}

	public void LoadProgramSettings()
	{
		if (SettingsLoaded)
			return;

		System.IO.Directory.CreateDirectory(ConfigRootDir);
		if (!System.IO.File.Exists(MainConfigFile))
		{
			System.IO.File.Create(MainConfigFile).Close();
			return;
		}

		string ConfigString = null;
		try
		{
			ConfigString = System.IO.File.ReadAllText(MainConfigFile);
		}
		catch (System.IO.IOException Ex)
		{
			System.Threading.Thread.Sleep(200);
			//Freeze for 1/5 of a second to allow for the other user to release the file.
			ConfigString = System.IO.File.ReadAllText(MainConfigFile);
		}

		foreach (string Setting in ConfigString.Split(';'))
		{
			string[] Pair = Setting.Split(":".ToCharArray(), 2);
			if (Pair.Length < 2)
				continue;
			if (Settings.ContainsKey(Pair[0]))
				Settings.Remove(Pair[0]);
			Settings.Add(Pair[0].Trim(), Pair[1].Trim());
		}

		SettingsLoaded = true;
	}

	//LATER: Unify the 'mainconfig.ini' and 'profile.sync' formats.
	public void SaveProgramSettings()
	{
		System.Text.StringBuilder ConfigStrB = new System.Text.StringBuilder();
		foreach (KeyValuePair<string, string> Setting in Settings)
		{
			ConfigStrB.AppendFormat("{0}:{1};", Setting.Key, Setting.Value);
		}

		try
		{
			System.IO.File.WriteAllText(MainConfigFile, ConfigStrB.ToString());
			//IO.File.WriteAllText overwrites the file.
		}
		catch
		{
			Interaction.ShowDebug("Unable to save main config file.");
		}
	}

	public bool ProgramSettingsSet(string Setting)
	{
		return Settings.ContainsKey(Setting);
	}

	//[Diagnostics.Conditional("Debug")]
#if DEBUG
	public void LogDebugEvent(string EventData)
	{
		LogAppEvent(EventData);
#endif
	}

	private void TrimAppLog()
	{
		if (System.IO.File.Exists(AppLogFile) && Utilities.GetSize(AppLogFile) > ProgramSetting.AppLogThreshold)
		{
			string AppLogBackup = AppLogFile + ".old";

			if (System.IO.File.Exists(AppLogBackup))
				System.IO.File.Delete(AppLogBackup);
			System.IO.File.Move(AppLogFile, AppLogBackup);

			LogAppEvent("Moved " + AppLogFile + " to " + AppLogBackup);
		}
	}

	public void LogAppEvent(string EventData)
	{
		if (ProgramSetting.Debug | CommandLine.Silent | CommandLine.Log)
		{
			String UniqueID = Guid.NewGuid().ToString();

			try
			{
				using (System.IO.StreamWriter AppLog = new System.IO.StreamWriter(AppLogFile, true))
				{
					AppLog.WriteLine(string.Format("[{0}][{1}] {2}", UniqueID, System.DateTime.Now.ToString(), EventData.Replace(Environment.NewLine, " // ")));
				}
			}
			catch (System.IO.IOException Ex)
			{
				// File in use.
			}
		}
	}

	public void RegisterBoot()
	{
		if (Main.ProgramConfig.GetProgramSetting<bool>(ProgramSetting.AutoStartupRegistration, true))
		{
			if (Microsoft.Win32.Registry.GetValue(ProgramSetting.RegistryRootedBootKey, ProgramSetting.RegistryBootVal, null) == null)
			{
				LogAppEvent("Registering program in startup list");
				Microsoft.Win32.Registry.SetValue(ProgramSetting.RegistryRootedBootKey, ProgramSetting.RegistryBootVal, string.Format("\"{0}\" /scheduler", Application.ExecutablePath));
			}
		}
	}

	public void IncrementSyncsCount()
	{
		try
		{
			int Count = 0;
			if (System.IO.File.Exists(StatsFile) && int.TryParse(System.IO.File.ReadAllText(StatsFile), out Count))
				System.IO.File.WriteAllText(StatsFile, (Count + 1).ToString());
		}
		catch
		{
		}
	}
}

struct CommandLine
{
	public enum RunMode
	{
		Normal,
		Scheduler,
		Queue,
#if DEBUG
		Scanner
#endif
	}

	public static bool Help;    //= False
	public static bool Quiet; //= False
	public static string TasksToRun = "";
	public static bool RunAll; //= False
	public static bool ShowPreview; //= False
	public static RunMode RunAs; //= RunMode.Normal
	public static bool Silent; //= False
	public static bool Log; //= False
	public static bool NoUpdates; //= False
	public static bool NoStop; //= False
#if DEBUG
	public static string ScanPath = "";
#endif

	public static void ReadArgs(List<string> ArgsList)
	{
#if DEBUG
		Main.ProgramConfig.LogDebugEvent("Parsing command line settings");
		foreach (string Param in ArgsList)
		{
			Main.ProgramConfig.LogDebugEvent("  Got: " + Param);
		}
		Main.ProgramConfig.LogDebugEvent("Done.");
#endif

		if (ArgsList.Count > 1)
		{
			CommandLine.Help = ArgsList.Contains("/help");
			CommandLine.Quiet = ArgsList.Contains("/quiet");
			CommandLine.ShowPreview = ArgsList.Contains("/preview");
			CommandLine.Silent = ArgsList.Contains("/silent");
			CommandLine.Log = ArgsList.Contains("/log");
			CommandLine.NoUpdates = ArgsList.Contains("/noupdates");
			CommandLine.NoStop = ArgsList.Contains("/nostop");
			CommandLine.RunAll = ArgsList.Contains("/all");

			int RunArgIndex = ArgsList.IndexOf("/run");
			if ((!CommandLine.RunAll) && RunArgIndex != -1 && RunArgIndex + 1 < ArgsList.Count)
			{
				CommandLine.TasksToRun = ArgsList[RunArgIndex + 1];
			}

#if DEBUG
			int ScanArgIndex = ArgsList.IndexOf("/scan");
			if (ScanArgIndex != -1 && ScanArgIndex + 1 < ArgsList.Count)
			{
				CommandLine.ScanPath = ArgsList[ScanArgIndex + 1];
			}
#endif
		}

		if (CommandLine.RunAll | !string.IsNullOrEmpty(CommandLine.TasksToRun))
		{
			CommandLine.RunAs = CommandLine.RunMode.Queue;
		}
		else if (ArgsList.Contains("/scheduler"))
		{
			CommandLine.RunAs = CommandLine.RunMode.Scheduler;
#if DEBUG
		}
		else if (!string.IsNullOrEmpty(CommandLine.ScanPath))
		{
			CommandLine.RunAs = CommandLine.RunMode.Scanner;
#endif
		}

		CommandLine.Quiet = CommandLine.Quiet | CommandLine.RunAs == RunMode.Scheduler | CommandLine.Silent;
	}
}
