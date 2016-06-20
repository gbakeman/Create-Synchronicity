using System;
using System.Collections.Generic;
using System.Windows.Forms;
using CreateSync;

namespace CreateSync
{
	struct StatusData
	{
		public enum SyncStep
		{
			Done,
			Scan,
			LR,
			RL
		}


		public DateTime StartTime;
		public long BytesCopied;

		public long BytesToCopy;

		public long FilesScanned;
		public long CreatedFiles;
		public long CreatedFolders;
		public long FilesToCreate;

		public long FoldersToCreate;
		public long DeletedFiles;
		public long DeletedFolders;
		public long FilesToDelete;

		public long FoldersToDelete;
		public long ActionsDone;
		// == SyncingList.Count
		public long TotalActionsCount;
		// Used to set a ProgressBar's maximum => Integer
		public int LeftActionsCount;

		public int RightActionsCount;
		public SyncStep CurrentStep;
		public TimeSpan TimeElapsed;

		public double Speed;
		//= False
		public bool Cancel;
		//= False
		public bool Failed;
		//= False
		public bool ShowingErrors;

		public string FailureMsg;
	}

	public enum TypeOfItem : int
	{
		File = 0,
		Folder = 1
	}

	public enum TypeOfAction : int
	{
		Delete = -1,
		None = 0,
		Copy = 1
	}

	public enum SideOfSource : int
	{
		Left = 0,
		Right = 1
	}

	struct SyncingContext
	{
		public SideOfSource Source;
		public string SourceRootPath;
		public string DestinationRootPath;
	}

	class SyncingItem
	{
		public string Path;
		public TypeOfItem Type;

		public SideOfSource Side;
		public bool IsUpdate;

		public TypeOfAction Action;
		// Keeps track of the order in which items where inserted in the syncing list, hence making it possible to recover this insertion order even after sorting the list on other criterias
		public int RealId;

		public string FormatType()
		{
			switch (Type)
			{
				case TypeOfItem.File:
					return Main.Translation.Translate("\\FILE");
				default:
					return Main.Translation.Translate("\\FOLDER");
			}
		}

		public string FormatAction()
		{
			switch (Action)
			{
				case TypeOfAction.Copy:
					return IsUpdate ? Main.Translation.Translate("\\UPDATE") : Main.Translation.Translate("\\CREATE");
				case TypeOfAction.Delete:
					return Main.Translation.Translate("\\DELETE");
				default:
					return Main.Translation.Translate("\\NONE");
			}
		}

		public string FormatDirection()
		{
			switch (Side)
			{
				case SideOfSource.Left:
					return Action == TypeOfAction.Copy ? Main.Translation.Translate("\\LR") : Main.Translation.Translate("\\LEFT");
				case SideOfSource.Right:
					return Action == TypeOfAction.Copy ? Main.Translation.Translate("\\RL") : Main.Translation.Translate("\\RIGHT");
				default:
					return "";
			}
		}

		public ListViewItem ToListViewItem()
		{
			ListViewItem ListItem = new ListViewItem(new string[] {
			FormatType(),
			FormatAction(),
			FormatDirection(),
			Path
		});

			int Delta = IsUpdate ? 1 : 0;
			switch (Action)
			{
				case TypeOfAction.Copy:
					if (Type == TypeOfItem.Folder)
					{
						ListItem.ImageIndex = 5 + Delta;
					}
					else if (Type == TypeOfItem.File)
					{
						switch (Side)
						{
							case SideOfSource.Left:
								ListItem.ImageIndex = 0 + Delta;
								break;
							case SideOfSource.Right:
								ListItem.ImageIndex = 2 + Delta;
								break;
						}
					}
					break;
				case TypeOfAction.Delete:
					if (Type == TypeOfItem.Folder)
					{
						ListItem.ImageIndex = 7;
					}
					else if (Type == TypeOfItem.File)
					{
						ListItem.ImageIndex = 4;
					}
					break;
			}

			return ListItem;
		}
	}

	internal sealed class FileNamePattern
	{
		public enum PatternType
		{
			FileExt,
			FileName,
			FolderName,
			Regex
		}

		public PatternType Type;

		public string Pattern;
		public FileNamePattern(PatternType _Type, string _Pattern)
		{
			Type = _Type;
			Pattern = _Pattern;
		}

		private static bool IsBoxed(char Frame, string Str)
		{
			return (Str.StartsWith(Frame.ToString()) & Str.EndsWith(Frame.ToString()) & Str.Length > 2);
		}

		private static string Unbox(string Str)
		{
			return Str.Substring(1, Str.Length - 2).ToLower(Interaction.InvariantCulture);
			// ToLower: Careful on linux ; No need to check that length > 2 here: IsBoxed already has.
		}

		public static FileNamePattern GetPattern(string Str, bool IsFolder = false)
		{
			//Filename
			if (IsBoxed('"', Str))
			{
				return new FileNamePattern(IsFolder ? PatternType.FolderName : PatternType.FileName, Unbox(Str));
				//Regex
			}
			else if (IsBoxed('/', Str))
			{
				return new FileNamePattern(PatternType.Regex, Unbox(Str));
			}
			else
			{
				return new FileNamePattern(PatternType.FileExt, Str.ToLower(Interaction.InvariantCulture));
			}
		}

		private static string SharpInclude(string FileName)
		{
			string Path = Main.ProgramConfig.ConfigRootDir + ProgramSetting.DirSep + FileName;
			return System.IO.File.Exists(Path) ? System.IO.File.ReadAllText(Path) : FileName;
		}

		static internal void LoadPatternsList(ref List<FileNamePattern> PatternsList, string PatternsStr, bool IsFolder, string FolderPrefix = "")
		{
			List<string> Patterns = new List<string>(PatternsStr.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));

			//Prevent circular references
			while (Patterns.Count > 0 & Patterns.Count < 1024)
			{
				string CurPattern = Patterns[0];

				if (IsFolder == CurPattern.StartsWith(FolderPrefix))
				{
					if (IsFolder)
						CurPattern = CurPattern.Substring(FolderPrefix.Length);

					//Load patterns from file
					if (IsBoxed(':', CurPattern))
					{
						string SubPatterns = SharpInclude(Unbox(CurPattern));
						Patterns.AddRange(SubPatterns.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries));
					}
					else
					{
						PatternsList.Add(GetPattern(CurPattern, IsFolder));
					}
				}

				Patterns.RemoveAt(0);
			}
		}

		private static string GetExtension(string File)
		{
			return File.Substring(File.LastIndexOf('.') + 1);
			//Not used when dealing with a folder.
		}

		static internal bool MatchesPattern(string PathOrFileName, ref List<FileNamePattern> Patterns)
		{
			string Extension = GetExtension(PathOrFileName);

			//LINUX: Problem with IgnoreCase
			foreach (FileNamePattern Pattern in Patterns)
			{
				switch (Pattern.Type)
				{
					case FileNamePattern.PatternType.FileExt:
						if (string.Compare(Extension, Pattern.Pattern, true) == 0)
							return true;
						break;
					case FileNamePattern.PatternType.FileName:
						if (string.Compare(PathOrFileName, Pattern.Pattern, true) == 0)
							return true;
						break;
					case FileNamePattern.PatternType.FolderName:
						if (PathOrFileName.EndsWith(Pattern.Pattern, StringComparison.CurrentCultureIgnoreCase))
							return true;
						break;
					case FileNamePattern.PatternType.Regex:
						if (System.Text.RegularExpressions.Regex.IsMatch(PathOrFileName, Pattern.Pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
							return true;
						break;
				}
			}

			return false;
		}
	}

	static class FileHandling
	{
		static internal string GetFileOrFolderName(string Path)
		{
			return Path.Substring(Path.LastIndexOf(ProgramSetting.DirSep) + 1);
			//IO.Path.* -> Bad because of separate file/folder handling.
		}
	}
}
