using System;
using System.Windows.Forms;

//TODO: Complete port

namespace Create_Synchronicity
{
	static class Program
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
	}
}
