using System;
using System.Collections.Generic;

namespace CreateSync
{
	static class Utilities
	{
		static internal string FormatTimespan(TimeSpan T)
		{
			int Hours = Convert.ToInt32(Math.Truncate(T.TotalHours));
			List<string> Blocks = new List<string>();
			if (Hours != 0)
				Blocks.Add(Hours + " h");
			if (T.Minutes != 0)
				Blocks.Add(T.Minutes.ToString() + " m");
			if (T.Seconds != 0 | Blocks.Count == 0)
				Blocks.Add(T.Seconds.ToString() + " s");
			return string.Join(", ", Blocks.ToArray());
		}

		static internal long GetSize(string File)
		{
			return (new System.IO.FileInfo(File)).Length;
			//Faster than My.Computer.FileSystem.GetFileInfo().Length (See FileLen_Speed_Test.vb)
		}

		static internal string FormatSize(double Size, int Digits = 2)
		{
			if (Size >= 1 << 30)
				return Math.Round(Size / (1 << 30), Digits).ToString() + " GB";
			else if (Size >= 1 << 20)
				return Math.Round(Size / (1 << 20), Digits).ToString() + " MB";
			else if (Size >= 1 << 10)
				return Math.Round(Size / (1 << 10), Digits).ToString() + " KB";
			else
				return Math.Round(Size, Digits).ToString() + " B";
		}
	}
}