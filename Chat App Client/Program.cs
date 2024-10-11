using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Chat_App_Client
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length == 1)
            {
                MessageBox.Show("Please provide a username as an argument. Examples:\n'client john'\n'client \"john doe\"'", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1(args[1]));
        }
    }
}