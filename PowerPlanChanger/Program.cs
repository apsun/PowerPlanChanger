using System;
using System.Threading;
using System.Windows.Forms;

namespace PowerPlanChanger
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool firstInstance;
            var mutex = new Mutex(true, "22C2B199-B3D9-4003-9D75-631EBFF72F07", out firstInstance);
            if (!firstInstance)
            {
                MessageBox.Show("PowerPlanChanger is already running!", "", 
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                Application.Run(new Form1());
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }
    }
}
