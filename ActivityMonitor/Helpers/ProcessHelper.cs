using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace ActivityMonitor.Helpers
{
    public static class ProcessHelper
    {
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        // Retrieve log file path from environment variable or fallback to a default path
        private static string GetLogFilePath(IConfiguration configuration, string logFileName)
        {
            string logDirectory = configuration["Logging:LogDirectory"] ?? "C:\\ActivityMonitorLogs";
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            return Path.Combine(logDirectory, logFileName);
        }

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
                    //File.AppendAllText("C:\\Users\\DavidAderibigbe\\Temp\\debug_log.txt", $"[{DateTime.Now}] GetForegroundWindow returned zero.{Environment.NewLine}");
                    LogError("GetForegroundWindow returned zero.", null);
                    return "Unknown";
                }

                GetWindowThreadProcessId(foregroundWindow, out uint processId);
                if (processId == 0)
                {
                    //File.AppendAllText("C:\\Temp\\debug_log.txt", $"[{DateTime.Now}] Process ID is zero.{Environment.NewLine}");
                    LogError("Process ID is zero.", null);
                    return "Unknown";
                }

                using (Process process = Process.GetProcessById((int)processId))
                {
                    string processName = process.ProcessName;
                    string windowTitle = process.MainWindowTitle;

                    return string.IsNullOrEmpty(windowTitle) ? processName : $"{processName} - {windowTitle}";
                }
            }
            catch (Exception ex)
            {
                LogError("Error retrieving active application", ex);
                return "Unknown";

            }
        }

        /// <summary>
        /// Captures a screenshot of the current screen and saves it to the specified file path.
        /// </summary>
        /// <param name="filePath">The path where the screenshot will be saved.</param>
        public static string CaptureScreenshot(IConfiguration configuration)
        {
            try
            {
                string screenshotDirectory = configuration["Logging:ScreenshotDirectory"];

                if (!Directory.Exists(screenshotDirectory))
                {
                    Directory.CreateDirectory(screenshotDirectory);
                }
                
                string fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                string filePath = Path.Combine(screenshotDirectory, fileName);


                Rectangle bounds = GetCombinedScreenBounds();
                using (Bitmap bitmap = new Bitmap(bounds.Width, bounds.Height))
                {
                    using (Graphics g = Graphics.FromImage(bitmap))
                    {
                        foreach (var screen in Screen.AllScreens)
                        {
                            g.CopyFromScreen(screen.Bounds.Location, screen.Bounds.Location, screen.Bounds.Size);
                        }
                    }

                    bitmap.Save(filePath, ImageFormat.Png);

                    LogInfo($"Screenshot saved at: {filePath}");
                }

                return filePath;
            }
            catch (Exception ex)
            {
                LogError("Error capturing screenshot", ex);
                return null;
            }
        }

        private static Rectangle GetCombinedScreenBounds()
        {
            int minX = int.MaxValue;
            int minY = int.MaxValue;
            int maxX = int.MinValue;
            int maxY = int.MinValue;

            foreach (var screen in Screen.AllScreens)
            {
                minX = Math.Min(minX, screen.Bounds.Left);
                minY = Math.Min(minY, screen.Bounds.Top);
                maxX = Math.Max(maxX, screen.Bounds.Right);
                maxY = Math.Max(maxY, screen.Bounds.Bottom);
            }
            return new Rectangle(minX, minY, maxX - minX, maxY - minY);
        }

        //public static void SaveByteArrayToImage(byte[] imageBytes, string filePath)
        //{
        //    if (imageBytes == null) throw new ArgumentNullException(nameof(imageBytes));

        //    using (MemoryStream ms = new MemoryStream(imageBytes))
        //    {
        //        using (Bitmap bitmap = new Bitmap(ms))
        //        {
        //            bitmap.Save(filePath, ImageFormat.Png);
        //        }
        //    }
        //}

        private static void LogInfo(string message)
        {
            string logFilePath = GetLogFilePath(new ConfigurationBuilder().AddJsonFile("appsettings.json").Build(), "log_info.txt");
            string logMessage = $"[{DateTime.Now}] INFO: {message}{Environment.NewLine}";
            File.AppendAllText(logFilePath, logMessage);
        }

        internal static void LogError(string context, Exception ex)
        {
            string logFilePath = GetLogFilePath(new ConfigurationBuilder().AddJsonFile("appsettings.json").Build(), "log_error.txt");
            string errorMessage = $"[{DateTime.Now}] ERROR: {context}: " +
                $"{ex?.Message ?? "No exception message"}{Environment.NewLine}Stack Trace: " +
                $"{ex?.StackTrace ?? "No stack trace"}{Environment.NewLine}";

            File.AppendAllText(logFilePath, errorMessage);
        }
    }
}