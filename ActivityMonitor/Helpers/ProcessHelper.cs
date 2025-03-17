using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices.ComTypes;

namespace ActivityMonitor.Helpers
{
    public static class ProcessHelper
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        /// <summary>
        /// Gets the name of the currently active application.
        /// </summary>
        /// <returns>The name of the active application.</returns>
        public static string GetActiveApplication()
        {
            try
            {
                IntPtr foregroundWindow = GetForegroundWindow();
                if (foregroundWindow == IntPtr.Zero)
                {
                    File.AppendAllText("C:\\Users\\DavidAderibigbe\\Temp\\debug_log.txt", $"[{DateTime.Now}] GetForegroundWindow returned zero.{Environment.NewLine}");
                    return "Unknown";
                }

                GetWindowThreadProcessId(foregroundWindow, out uint processId);
                if (processId == 0)
                {
                    File.AppendAllText("C:\\Temp\\debug_log.txt", $"[{DateTime.Now}] Process ID is zero.{Environment.NewLine}");
                    return "Unknown";
                }

                using (Process process = Process.GetProcessById((int)processId))
                {
                    string processName = process.ProcessName;
                    string processPath = "Unknown";
                    string windowTitle = process.MainWindowTitle;

                    return string.IsNullOrEmpty(windowTitle) ? processName : $"{processName} - {windowTitle}";
                }
            }
            catch (Exception ex)
            {
                string activeApplication = ProcessHelper.GetActiveApplication();
                File.AppendAllText("C:\\Users\\DavidAderibigbe\\Temp\\debug_log.txt", $"[{DateTime.Now}] Active App: {activeApplication}{Environment.NewLine}");
                LogError("Error retrieving active application", ex);
                return "Unknown";

            }
        }

        /// <summary>
        /// Captures a screenshot of the current screen and saves it to the specified file path.
        /// </summary>
        /// <param name="filePath">The path where the screenshot will be saved.</param>
        public static byte[] CaptureScreenshotAsByteArray()
        {
            try
            {
                Rectangle bounds = Screen.PrimaryScreen.Bounds;
                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
                    }

                    // Save as PNG to memory stream and convert to byte array
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bitmap.Save(ms, ImageFormat.Png);
                        return ms.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError("Error capturing screenshot", ex);
                return null;
            }
        }

        public static void SaveByteArrayToImage(byte[] imageBytes, string filePath)
        {
            if (imageBytes == null) throw new ArgumentNullException(nameof(imageBytes));

            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                using (Bitmap bitmap = new Bitmap(ms))
                {
                    bitmap.Save(filePath, ImageFormat.Png);
                }
            }
        }

        internal static void LogError(string context, Exception ex)
        {
            string logFilePath = "C:\\Users\\DavidAderibigbe\\source\\repos\\ActivityMonitor\\ActivityMonitor\\logs\\log_error.txt";

            string errorMessage = $"[{DateTime.Now}] {context}: {ex.Message}{Environment.NewLine}Stack Trace: {ex.StackTrace}{Environment.NewLine}";

            File.AppendAllText(logFilePath, errorMessage);
        }
    }
}