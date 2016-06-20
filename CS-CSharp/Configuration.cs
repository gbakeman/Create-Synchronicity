using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CreateSync
{
	/// <summary>
	/// Maintains program-wide variables for runtime purposes, as well as an instance of persistently stored settings.
	/// </summary>
	sealed class Configuration
	{
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
		public Settings ProgramSettings;

		public Configuration()
		{

		}
	}

	/// <summary>
	/// Settings that are intended to persist in between sessions.
	/// </summary>
	[Serializable]
	sealed class Settings
	{
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
		//In VB version config files were stored in user profile, but this app is supposed to be portable? I think they
		//ought to be stored relative to the executable then.
		public const string ConfigFolderName = "config";
		public const string LogFolderName = "log";
		public const string SettingsFileName = "mainconfig.bin";
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

		public Settings Load()
		{
			FileStream settingsFile = File.Open(Path.Combine(ConfigFolderName, MainConfigFile), FileMode.OpenOrCreate,
				FileAccess.Read, FileShare.None);
		}
		/*
		 * 
public void LoadProgramSettings()
{
	if (SettingsLoaded)
		return;

	IO.Directory.CreateDirectory(ConfigRootDir);
	if (!IO.File.Exists(MainConfigFile)) {
		IO.File.Create(MainConfigFile).Close();
		return;
	}

	string ConfigString = null;
	try {
		ConfigString = IO.File.ReadAllText(MainConfigFile);
	} catch (IO.IOException Ex) {
		System.Threading.Thread.Sleep(200);
		//Freeze for 1/5 of a second to allow for the other user to release the file.
		ConfigString = IO.File.ReadAllText(MainConfigFile);
	}

	foreach (string Setting in ConfigString.Split(';')) {
		string[] Pair = Setting.Split(":".ToCharArray, 2);
		if (Pair.Length() < 2)
			continue;
		if (Settings.ContainsKey(Pair(0)))
			Settings.Remove(Pair(0));
		Settings.Add(Pair(0).Trim, Pair(1).Trim);
	}

	SettingsLoaded = true;
}

//=======================================================
//Service provided by Telerik (www.telerik.com)
//Conversion powered by NRefactory.
//Twitter: @telerik
//Facebook: facebook.com/telerik
//=======================================================
*/
	}
}
