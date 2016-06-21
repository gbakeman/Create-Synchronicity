using Microsoft.VisualBasic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Data;
using System.Diagnostics;
using System.Windows.Forms;

namespace CreateSync
{
	internal sealed class LanguageHandler
	{

		private static LanguageHandler Singleton;
		public struct LangInfo
		{
			public List<string> CodeNames;
			public string NativeName;
		}

		//Renames : non-english file name -> english file name
		private static Dictionary<string, string> Renames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
		{
			"francais",
			"french"
		},
		{
			"deutsch",
			"german"
		}

	};
		private static string NewFilename(string OldLanguageName)
		{
			return Renames.ContainsKey(OldLanguageName) ? Renames[OldLanguageName] : OldLanguageName;
		}

		private static string GetLanguageFilePath(string LanguageName)
		{
			return Program.ProgramConfig.LanguageRootDir + ProgramSetting.DirSep + NewFilename(LanguageName) + ".lng";
		}

		private LanguageHandler()
		{
			Program.ProgramConfig.LoadProgramSettings();
			Directory.CreateDirectory(Program.ProgramConfig.LanguageRootDir);

			Strings = new Dictionary<string, string>();

			string DictFile = GetLanguageFilePath(Program.ProgramConfig.GetProgramSetting<string>(ProgramSetting.Language, ProgramSetting.DefaultLanguage));

			if (!File.Exists(DictFile))
			{
				DictFile = GetLanguageFilePath(ProgramSetting.DefaultLanguage);
			}

			if (!File.Exists(DictFile))
			{
				Interaction.ShowMsg("No language file found!");
			}
			else
			{
				using (StreamReader Reader = new StreamReader(DictFile, System.Text.Encoding.UTF8))
				{
					while (!Reader.EndOfStream)
					{
						string Line = Reader.ReadLine();
						if (Line.StartsWith("#") || (!Line.Contains("=")))
							continue;

						string[] Pair = Line.Split("=".ToCharArray(), 2);
						try
						{
							if (Pair[0].StartsWith("->"))
								Pair[0] = Pair[0].Remove(0, 2);
							Strings.Add("\\" + Pair[0], Pair[1].Replace("\\n", Environment.NewLine));
						}
						catch (ArgumentException Ex)
						{
							Interaction.ShowDebug("Duplicate line in translation: " + Line);
						}
					}
				}
			}
		}

		public static LanguageHandler GetSingleton(bool Reload = false)
		{
			if (Reload | (Singleton == null))
				Singleton = new LanguageHandler();
			return Singleton;
		}


		Dictionary<string, string> Strings;
		public string Translate(string Code, string DefaultValue = "")
		{
			if (Code == null || string.IsNullOrEmpty(Code))
				return DefaultValue;
			return Strings.ContainsKey(Code) ? Strings[Code] : string.IsNullOrEmpty(DefaultValue) ? Code : DefaultValue;
		}

		#region "TranslateFormat"
		//ParamArray requires building objects array, and adds binsize.
		public string TranslateFormat(string Code, object Arg0)
		{
			return string.Format(Translate(Code), Arg0);
		}
		public string TranslateFormat(string Code, object Arg0, object Arg1)
		{
			return string.Format(Translate(Code), Arg0, Arg1);
		}
		public string TranslateFormat(string Code, object Arg0, object Arg1, object Arg2)
		{
			return string.Format(Translate(Code), Arg0, Arg1, Arg2);
		}
		#endregion

		public void TranslateControl(Control Ctrl)
		{
			if (Ctrl == null)
				return;

			//Add ; in tags so as to avoid errors when tag properties are split.
			Ctrl.Text = Translate(Ctrl.Text);
			TranslateControl(Ctrl.ContextMenuStrip);

			if (Ctrl is ListView)
			{
				ListView List = (ListView)Ctrl;

				foreach (ListViewGroup Group in List.Groups)
				{
					Group.Header = Translate(Group.Header);
				}

				foreach (ColumnHeader Column in List.Columns)
				{
					Column.Text = Translate(Column.Text);
				}

				if (!List.VirtualMode)
				{
					foreach (ListViewItem Item in List.Items)
					{
						foreach (ListViewItem.ListViewSubItem SubItem in Item.SubItems)
						{
							SubItem.Text = Translate(SubItem.Text);
							SubItem.Tag = Translate(Convert.ToString(SubItem.Tag), ";");
						}
					}
				}
			}

			if (Ctrl is ContextMenuStrip)
			{
				ContextMenuStrip ContextMenu = (ContextMenuStrip)Ctrl;
				foreach (ToolStripItem Item in ContextMenu.Items)
				{
					Item.Text = Translate(Item.Text);
					Item.Tag = Translate(Convert.ToString(Item.Tag), ";");
				}
			}

			Ctrl.Tag = Translate(Convert.ToString(Ctrl.Tag), ";");
			foreach (Control ChildCtrl in Ctrl.Controls)
			{
				TranslateControl(ChildCtrl);
			}
		}

		public static void FillLanguagesComboBox(ComboBox LanguagesComboBox)
		{
			Dictionary<string, LangInfo> LanguagesInfo = new Dictionary<string, LangInfo>();

			if (File.Exists(Program.ProgramConfig.LocalNamesFile))
			{
				using (StreamReader PropsReader = new StreamReader(Program.ProgramConfig.LocalNamesFile))
				{
					while (!PropsReader.EndOfStream)
					{
						string[] CurLanguage = PropsReader.ReadLine().Split(";".ToCharArray());

						if (CurLanguage.Length != 3)
							continue;
						LanguagesInfo.Add(CurLanguage[0], new LangInfo
						{
							NativeName = CurLanguage[1],
							CodeNames = new List<string>(CurLanguage[2].ToLower(Interaction.InvariantCulture).Split('/'))
						});
					}
				}
			}

			// Use strings to allow for some sorting.
			string SystemLanguageItem = null;
			string ProgramLanguageItem = null;
			string DefaultLanguageItem = null;

			System.Globalization.CultureInfo CurrentCulture = System.Globalization.CultureInfo.InstalledUICulture;

			Program.ProgramConfig.LoadProgramSettings();
			string CurProgramLanguage = NewFilename(Program.ProgramConfig.GetProgramSetting<string>(ProgramSetting.Language, ""));

			//Merge fr-CA, fr-FR, and so on to fr, and distinguish zh-Hans and zh-Hant.
			if (!CurrentCulture.IsNeutralCulture)
				CurrentCulture = CurrentCulture.Parent;

			LanguagesComboBox.Items.Clear();
			foreach (string File in Directory.GetFiles(Program.ProgramConfig.LanguageRootDir, "*.lng"))
			{
				string EnglishName = Path.GetFileNameWithoutExtension(File);
				string NewItemText = EnglishName;

				if (LanguagesInfo.ContainsKey(EnglishName))
				{
					LanguageHandler.LangInfo Info = LanguagesInfo[EnglishName];
					NewItemText = string.Format("{0} - {1} ({2})", EnglishName, Info.NativeName, Info.CodeNames[0]);

					if (Info.CodeNames.Contains(CurrentCulture.Name.ToLower(Interaction.InvariantCulture)))
						SystemLanguageItem = NewItemText;
				}

				LanguagesComboBox.Items.Add(NewItemText);
				if (string.Compare(EnglishName, CurProgramLanguage, true) == 0)
					ProgramLanguageItem = NewItemText;
				if (string.Compare(EnglishName, ProgramSetting.DefaultLanguage, true) == 0)
					DefaultLanguageItem = NewItemText;
			}

			LanguagesComboBox.Sorted = true;

			if (ProgramLanguageItem != null)
			{
				LanguagesComboBox.SelectedItem = ProgramLanguageItem;
			}
			else if (SystemLanguageItem != null)
			{
				LanguagesComboBox.SelectedItem = SystemLanguageItem;
			}
			else if (DefaultLanguageItem != null)
			{
				LanguagesComboBox.SelectedItem = DefaultLanguageItem;
			}
			else if (LanguagesComboBox.Items.Count > 0)
			{
				LanguagesComboBox.SelectedIndex = 0;
			}
		}

/*
	public static void EnumerateCultures()
	{
		Text.StringBuilder Builder = new Text.StringBuilder();
		foreach (Globalization.CultureInfo Culture in Globalization.CultureInfo.GetCultures(Globalization.CultureTypes.AllCultures)) {
			Builder.AppendLine(string.Join(Microsoft.VisualBasic.ControlChars.Tab, new string[] {
				Culture.Name,
				Culture.Parent.Name,
				Culture.IsNeutralCulture.ToString,
				Culture.DisplayName,
				Culture.NativeName,
				Culture.EnglishName,
				Culture.TwoLetterISOLanguageName,
				Culture.ThreeLetterISOLanguageName,
				Culture.ThreeLetterWindowsLanguageName
			}));
			//, LangInfo.LocalName, LangInfo.IsoLanguageName, LangInfo.WindowsCode
		}

		MessageBox.Show(Builder.ToString);
	}
*/
	}
}
