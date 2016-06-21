using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Windows.Forms;

namespace CreateSync
{
	static internal class Updates
	{
		public static void CheckForUpdates(bool RoutineCheck = true)
		{
			string LatestVersion = "";
			WebClient UpdateClient = new WebClient();

			try
			{
				//Headers
				UpdateClient.Headers.Add("version", Application.ProductVersion);

#if Linux
			UpdateClient.Headers.Add("os", "Linux");
#else
			UpdateClient.UseDefaultCredentials = true;
			//Needed? -- Does no harm
			UpdateClient.Proxy = WebRequest.DefaultWebProxy;
			//Tracker #2976549
			UpdateClient.Proxy.Credentials = CredentialCache.DefaultCredentials;
#endif

				string Url = CommandLine.RunAs == CommandLine.RunMode.Scheduler ? Branding.UpdatesSchedulerUrl : Branding.UpdatesUrl;
				try
				{
					LatestVersion = UpdateClient.DownloadString(Url);
				}
				catch (WebException ex)
				{
					LatestVersion = UpdateClient.DownloadString(Branding.UpdatesFallbackUrl);
				}

				if (((new Version(LatestVersion)) > (new Version(Application.ProductVersion))))
				{
					if (Interaction.ShowMsg(Program.Translation.TranslateFormat("\\UPDATE_MSG", Application.ProductVersion, LatestVersion), Program.Translation.Translate("\\UPDATE_TITLE"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
					{
						Interaction.StartProcess(Branding.UpdatesTarget);
						if (Program.ProgramConfig.CanGoOn)
							Program.MainFormInstance.Invoke(new Action(Application.Exit));
					}
				}
				else
				{
					if (!RoutineCheck)
						Interaction.ShowMsg(Program.Translation.Translate("\\NO_UPDATES"), null, null, MessageBoxIcon.Information);
				}
			}
			catch (InvalidOperationException Ex)
			{
				//Some form couldn't close properly because of thread accesses
				Interaction.ShowDebug(Ex.ToString());
			}
			catch (Exception Ex)
			{
				Interaction.ShowMsg(Program.Translation.Translate("\\UPDATE_ERROR") + LatestVersion + Environment.NewLine + Environment.NewLine + Ex.Message, Program.Translation.Translate("\\UPDATE_ERROR_TITLE"), null, MessageBoxIcon.Error);
				Interaction.ShowDebug(Ex.Message + Environment.NewLine + Ex.StackTrace);
			}
			finally
			{
				UpdateClient.Dispose();
			}
		}
	}
}
