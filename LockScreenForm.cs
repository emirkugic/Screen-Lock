using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

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

        public LockScreenForm()
        {
            InitializeComponents();
            hookID = SetHook(proc);
            InitializeFocusTimer();
            cameraService = new CameraService();
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
                this.Activate(); // Bring the application back to focus
            }
        }

        private void LockScreenForm_KeyPress(object sender, KeyPressEventArgs e)
        {
            userInput += e.KeyChar;

            // Check if the last entered characters match the unlock code
            if (userInput.Length >= unlockCode.Length)
            {
                string lastInput = userInput.Substring(userInput.Length - unlockCode.Length);

                if (lastInput.Equals(unlockCode))
                {
                    this.Close(); // Unlock the screen
                }
                else if (userInput.Length > unlockCode.Length)
                {
                    // take a picture if the user enters a wrong key
                    TakePicture();
                }
            }
        }



        private void TakePicture()
        {
            // Text
            string textFilePath = "images/log.txt";
            string message = "Wrong key pressed\n";
            File.AppendAllText(textFilePath, message);

            // Takes a picture
            cameraService.TakePicture((bitmap) =>
            {
                string imageDirectory = "images";
                Directory.CreateDirectory(imageDirectory); // create directory if it doesn't exist

                string imageName = $"capture_{DateTime.Now.Ticks}.jpg";
                string imagePath = Path.Combine(imageDirectory, imageName);

                bitmap.Save(imagePath);
                Console.WriteLine($"Image saved: {imageName}");
            });
        }


        protected override void OnClosed(EventArgs e)
        {
            UnhookWindowsHookEx(hookID);
            focusTimer.Stop();
            base.OnClosed(e);
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
                    return (IntPtr)1; // suppress key
                }
            }
            return CallNextHookEx(hookID, nCode, wParam, lParam);
        }

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
