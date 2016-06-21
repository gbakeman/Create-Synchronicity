using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CreateSync
{
	struct ErrorItem
	{
		public Exception Ex;

		public string Path;
		public ListViewItem ToListViewItem()
		{
			return new ListViewItem(new string[] {
			Ex.Source,
			Ex.Message,
			Path
		}, 8);
			//TODO: Display something better than error source.
		}
	}

	struct LogItem
	{
		public SyncingItem Item;
		public SideOfSource Side;

		public bool Success;
		public string GetHeader()
		{
			return Success ? Program.Translation.Translate("\\SUCCEDED") : Program.Translation.Translate("\\FAILED");
		}
	}

	internal sealed class LogHandler
	{

		string LogName;
		public List<ErrorItem> Errors;

		public List<LogItem> Log;
#if DEBUG
#endif
		public List<string> DebugInfo;

		//= False
		private bool Disposed;

		private bool GenerateErrorsLog;
		//HTML 'id' to current log
		private string LogId;
		//Store current date when launching sync
		private System.DateTime LogDate;

		public LogHandler(string _LogName, bool _GenerateErrorsLog)
		{
			System.IO.Directory.CreateDirectory(Program.ProgramConfig.LogRootDir);

			LogId = "cs_" + DateTime.UtcNow.Ticks.ToString();
			LogDate = System.DateTime.Now;

			LogName = _LogName;
			GenerateErrorsLog = _GenerateErrorsLog;

			Errors = new List<ErrorItem>();
			Log = new List<LogItem>();

#if DEBUG
			DebugInfo = new List<string>();
#endif
		}

		public void HandleError(Exception Ex, string Path = "")
		{
			if (Ex == null || Ex is ThreadAbortException)
				return;
			Errors.Add(new ErrorItem
			{
				Ex = Ex,
				Path = Path
			});
		}

		public void LogAction(SyncingItem Item, SideOfSource Side, bool Success)
		{
			Log.Add(new LogItem
			{
				Item = Item,
				Side = Side,
				Success = Success
			});
		}

		[System.Diagnostics.Conditional("Debug")]
#if DEBUG
		public void HandleSilentError(Exception Ex, string Details = "Debug message")
		{
			HandleError(Ex, Details);
#endif
		}

		[System.Diagnostics.Conditional("Debug")]
#if DEBUG
		public void LogInfo(string Info)
		{
			DebugInfo.Add(Info);
#endif
		}

		private static bool Html()
		{
			return !(ProgramSetting.Debug | Program.ProgramConfig.GetProgramSetting<bool>(ProgramSetting.TextLogs, false));
		}

		private static void WriteSummary(System.IO.StreamWriter LogW, string Left, string Right, StatusData Status, bool IncludeHtml)
		{
			IncludeHtml = IncludeHtml & Html();

			if (IncludeHtml)
				LogW.WriteLine("<p>");

			string LineSeparator = IncludeHtml ? "<br />" : "";
			LogW.WriteLine("Create Synchronicity v{0}{1}", Application.ProductVersion, LineSeparator);
			LogW.WriteLine("{0}: {1}{2}", Program.Translation.Translate("\\LEFT"), Left, LineSeparator);
			LogW.WriteLine("{0}: {1}{2}", Program.Translation.Translate("\\RIGHT"), Right, LineSeparator);
			LogW.WriteLine("{0} {1}/{2}" + LineSeparator, Program.Translation.Translate("\\DONE"), Status.ActionsDone, Status.TotalActionsCount);
			LogW.WriteLine("{0} {1}{2}", Program.Translation.Translate("\\ELAPSED"), TimeSpan.FromSeconds(Convert.ToInt32(Status.TimeElapsed.Seconds)).ToString(), LineSeparator);

			if (Status.Failed & (Status.FailureMsg != null))
			{
				LogW.WriteLine(Status.FailureMsg);
			}

			if (IncludeHtml)
				LogW.WriteLine("</p>");
		}

		private static void PutFormatted(string[] Contents, System.IO.StreamWriter LogW, bool TextOnly = false)
		{
			if (ProgramSetting.Debug | TextOnly | Program.ProgramConfig.GetProgramSetting<bool>(ProgramSetting.TextLogs, false))
			{
				LogW.WriteLine(string.Join("\t", Contents));
			}
			else
			{
				LogW.WriteLine("<tr>");
				foreach (string Cell in Contents)
				{
					LogW.WriteLine("\t<td>" + Cell + "</td>");
				}
				LogW.WriteLine("</tr>");
			}
		}

		private static void PutHtml(System.IO.StreamWriter LogW, string Text)
		{
			if (Html())
				LogW.WriteLine(Text);
		}

		public void SaveAndDispose(string Left, string Right, StatusData Status)
		{
			if (Disposed)
				return;
			Disposed = true;

			string LogPath = Program.ProgramConfig.GetLogPath(LogName);
			string ErrorsLogPath = Program.ProgramConfig.GetErrorsLogPath(LogName);

			try
			{
				bool NewLog = !System.IO.File.Exists(LogPath);

				//Load the contents of the previous log, excluding the closing tags
				int MaxArchivesCount = Program.ProgramConfig.GetProgramSetting<int>(ProgramSetting.MaxLogEntries, 7);
				List<StringBuilder> Archives = new List<StringBuilder>();

				System.Text.RegularExpressions.Regex TitleLine = new System.Text.RegularExpressions.Regex("<h2.*>");
				System.Text.RegularExpressions.Regex StrippedLines = new System.Text.RegularExpressions.Regex("<h1>|<a.*>|</body>|</html>");

				if (!NewLog & !ProgramSetting.Debug)
				{
					using (System.IO.StreamReader LogReader = new System.IO.StreamReader(LogPath))
					{
						while (!LogReader.EndOfStream)
						{
							string Line = LogReader.ReadLine();
							if (TitleLine.IsMatch(Line))
							{
								Archives.Add(new System.Text.StringBuilder());
								if (Archives.Count > MaxArchivesCount)
									Archives.RemoveAt(0);
								//Don't store more than ConfigOptions.MaxLogEntries in memory
							}
							if (Archives.Count > 0 && (!StrippedLines.IsMatch(Line)))
								Archives[Archives.Count - 1].AppendLine(Line);
						}
					}
				}

				//This erases log contents.
				System.IO.StreamWriter ErrorsLogWriter = null;
				System.IO.StreamWriter LogWriter = new System.IO.StreamWriter(LogPath, false, Encoding.UTF8);
				if (GenerateErrorsLog)
					ErrorsLogWriter = new System.IO.StreamWriter(ErrorsLogPath, false, Encoding.UTF8);

				string LogTitle = Program.Translation.TranslateFormat("\\LOG_TITLE", LogName);

				PutHtml(LogWriter, "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.1//EN\" \"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd\"><html xmlns=\"http://www.w3.org/1999/xhtml\"><head><title>" + LogTitle + "</title><meta http-equiv=\"Content-Type\" content=\"text/html;charset=utf-8\" /><style type=\"text/css\">body{font-family:Consolas, Courier, monospace;font-size:0.8em;margin:auto;width:80%;}table{border-collapse:collapse;margin:1em 0;width:100%;}th, td{border:solid grey;border-width:1px 0;padding-right:2em;}tr:nth-child(odd){background-color:#EEE;}.actions tr td{white-space:nowrap;}.actions tr td:nth-child(5), .errors tr td:nth-child(2){white-space:normal;word-break:break-all;}tr td:last-child{padding-right:0;}</style></head><body>");

				LogWriter.WriteLine("<h1>{0}</h1>", LogTitle);
				LogWriter.WriteLine("<p><a href=\"#{0}\">{1}</a></p>", LogId, Program.Translation.Translate("\\LATEST"));

				for (int LogId = 0; LogId <= Archives.Count - 1; LogId++)
				{
					LogWriter.Write(Archives[LogId].ToString());
				}

				try
				{
					//Log format: <h2>, then two <table>s (info, errors)
					LogWriter.WriteLine("<h2 id=\"{0}\">{1}</h2>", LogId, LogDate.ToString("g"));
					//Must be kept, to detect log boundaries

					WriteSummary(LogWriter, Left, Right, Status, true);
					if (GenerateErrorsLog)
						WriteSummary(ErrorsLogWriter, Left, Right, Status, false);

#if DEBUG
					foreach (string Info in DebugInfo)
					{
						PutFormatted(new string[] {
						"Info",
						Info
					}, LogWriter);
					}
#endif

					if (Log.Count > 0)
					{
						PutHtml(LogWriter, "<table class=\"actions\">");
						foreach (LogItem Record in Log)
						{
							PutFormatted(new string[] {
							Record.GetHeader(),
							Record.Item.FormatType(),
							Record.Item.FormatAction(),
							Record.Item.FormatDirection(),
							Record.Item.Path
						}, LogWriter);
						}
						PutHtml(LogWriter, "</table>");
					}

					if (Errors.Count > 0)
					{
						PutHtml(LogWriter, "<table class=\"errors\">");
						foreach (ErrorItem Err in Errors)
						{
							PutFormatted(new string[] {
							Program.Translation.Translate("\\ERROR"),
							Err.Path,
							Err.Ex.Message
						}, LogWriter);
							if (GenerateErrorsLog)
								PutFormatted(new string[] {
								Program.Translation.Translate("\\ERROR"),
								Err.Path,
								Err.Ex.Message
							}, ErrorsLogWriter, true);
#if DEBUG
							if (ProgramSetting.Debug)
								PutFormatted(new string[] {
								"Stack Trace",
								Err.Ex.StackTrace.Replace(Environment.NewLine, "\\n")
							}, LogWriter);
#endif
						}
						PutHtml(LogWriter, "</table>");
					}

					PutHtml(LogWriter, "</body></html>");

				}
				catch (ThreadAbortException Ex)
				{
					return;
				}
				catch (Exception Ex)
				{
					Interaction.ShowMsg(Program.Translation.Translate("\\LOGFILE_WRITE_ERROR") + Environment.NewLine + Ex.Message + Environment.NewLine + Environment.NewLine + Ex.ToString());

				}
				finally
				{
					LogWriter.Close();
					if (GenerateErrorsLog)
						ErrorsLogWriter.Close();
				}

			}
			catch (Exception Ex)
			{
				Interaction.ShowMsg(Program.Translation.Translate("\\LOGFILE_OPEN_ERROR") + Environment.NewLine + Ex.Message + Environment.NewLine + Environment.NewLine + Ex.ToString());
			}
		}
	}
}
