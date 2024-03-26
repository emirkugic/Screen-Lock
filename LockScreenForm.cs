using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ScreenLockApp
{
    public class LockScreenForm : Form
    {
        private static IntPtr hookID = IntPtr.Zero;
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private static LowLevelKeyboardProc proc = HookCallback;

        private string unlockCode = "emir";
        private string userInput = "";
        private System.Windows.Forms.Timer focusTimer;
        private CameraService cameraService;

        private const int MYACTION_HOTKEY_ID = 1;



        public LockScreenForm()
        {
            InitializeComponents();
            hookID = SetHook(proc);
            InitializeFocusTimer();
            cameraService = new CameraService();
            RegisterHotKey(this.Handle, MYACTION_HOTKEY_ID, 6, (int)Keys.Delete);
            KillCtrlAltDelete();



        }

        private void InitializeComponents()
        {
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.BackColor = Color.Gray;

            this.KeyPreview = true;
            Cursor.Hide();

            Label lockLabel = new Label
            {
                Text = "This screen is currently locked",
                Font = new Font("Arial", 20),
                AutoSize = true
            };

            int centerX = (Screen.PrimaryScreen.Bounds.Width - lockLabel.Width) / 2 - 150;
            int centerY = (Screen.PrimaryScreen.Bounds.Height - lockLabel.Height) / 2;

            lockLabel.Location = new Point(centerX, centerY);
            this.Controls.Add(lockLabel);

            this.KeyPress += LockScreenForm_KeyPress;
        }

        private void InitializeFocusTimer()
        {
            focusTimer = new System.Windows.Forms.Timer
            {
                Interval = 30
            };
            focusTimer.Tick += FocusTimer_Tick;
            focusTimer.Start();
        }

        private void FocusTimer_Tick(object sender, EventArgs e)
        {
            if (!this.Focused)
            {
                this.Activate();
            }
        }

        private void LockScreenForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            userInput += e.KeyChar.ToString();

            string debugFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "KeyPressLog.txt");
            File.AppendAllText(debugFilePath, $"User Input: {userInput}{Environment.NewLine}");

            if (userInput.Length >= unlockCode.Length)
            {
                string lastInput = userInput.Substring(userInput.Length - unlockCode.Length);

                File.AppendAllText(debugFilePath, $"Last Input: {lastInput}{Environment.NewLine}");

                if (lastInput.Equals(unlockCode, StringComparison.OrdinalIgnoreCase))
                {
                    File.AppendAllText(debugFilePath, "Unlocking...{Environment.NewLine}");
                    this.Close(); // Unlocks the screen
                }
                else if (userInput.Length > unlockCode.Length)
                {
                    userInput = userInput.Substring(1); // Keep the last 'unlockCode.Length' characters
                    TakePicture();
                }
            }
        }

        private void TakePicture()
        {

        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {

            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            EnableCTRLALTDEL();

            UnregisterHotKey(this.Handle, MYACTION_HOTKEY_ID);
            UnhookWindowsHookEx(hookID);
            focusTimer.Stop();
            base.Dispose(disposing);
        }



        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                var key = (Keys)vkCode;


                if (key == Keys.LControlKey || key == Keys.RControlKey ||
                    key == Keys.LWin || key == Keys.RWin ||
                    key == Keys.LMenu || key == Keys.RMenu)
                {
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        private const int WM_KEYDOWN = 0x0100;
        private const int WH_KEYBOARD_LL = 13;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);




        public void KillCtrlAltDelete()
        {
            string keyValueInt = "1";
            string subKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
            try
            {
                using (RegistryKey regkey = Registry.CurrentUser.CreateSubKey(subKey))
                {
                    regkey.SetValue("DisableTaskMgr", keyValueInt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public static void EnableCTRLALTDEL()
        {
            string subKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
            try
            {
                using (RegistryKey rk = Registry.CurrentUser)
                {
                    RegistryKey sk1 = rk.OpenSubKey(subKey);
                    if (sk1 != null)
                        rk.DeleteSubKeyTree(subKey);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        public void KillStartMenu()
        {
            int hwnd = FindWindow("Shell_TrayWnd", "");
            ShowWindow(hwnd, SW_HIDE);
        }

        public static void ShowStartMenu()
        {
            int hwnd = FindWindow("Shell_TrayWnd", "");
            ShowWindow(hwnd, SW_SHOW);
        }

        [DllImport("user32.dll")]
        private static extern int FindWindow(string className, string windowText);

        [DllImport("user32.dll")]
        private static extern int ShowWindow(int hwnd, int command);

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 1;


    }
}