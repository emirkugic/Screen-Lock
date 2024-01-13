using System;
using System.Windows.Forms;

namespace ScreenLockApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.Run(new LockScreenForm());

            Application.Exit();
        }
    }
}
