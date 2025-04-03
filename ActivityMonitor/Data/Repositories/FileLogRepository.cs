using ActivityMonitor.Core.Models;
using ActivityMonitor.Data.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace ActivityMonitor.Data.Repositories
{
    public class FileLogRepository : ILogRepository
    {
        private string LogFilePath { get; }
        private DateTime SystemStartTime { get; set; }

        private string ScreenshotDirectory { get; }

        public FileLogRepository(IConfiguration configuration)
        {
            LogFilePath = configuration["Logging:LogFilePath"] 
                ?? Environment.GetEnvironmentVariable("ACTIVITY_LOG_PATH") 
                ?? "./logs/activity_log.txt";

            ScreenshotDirectory = configuration["Logging:ScreenshotDirectory"] 
                ?? Environment.GetEnvironmentVariable("ACTIVITY_SCREENSHOT_PATH") 
                ?? "./logs/screenshots";

            EnsureLogDirectoryExists();
        }

        private void EnsureLogDirectoryExists()
        {
            try
            {
                var logDirectory = Path.GetDirectoryName(LogFilePath);
                if (!Directory.Exists(logDirectory))
                    Directory.CreateDirectory(logDirectory);

                if (!Directory.Exists(ScreenshotDirectory))
                    Directory.CreateDirectory(ScreenshotDirectory);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error creating directories: {ex.Message}");
            }
        }

        public async Task LogActivityAsync(ActivityLog log)
        {
            try
            {
                if (log.Description == "System started.")
                {
                    SystemStartTime = log.StartTime;
                }
                else if (log.Description == "System shut down.")
                {
                    log.StartTime = SystemStartTime;
                    log.EndTime = DateTime.Now;
                }

                string screenshotPath = "No screenshot captured";
                if (log.ScreenshotByteArray != null)
                {
                    try
                    {
                        screenshotPath = Path.Combine(ScreenshotDirectory, $"screenshot_{Guid.NewGuid()}.png");
                        await Task.Run(() => File.WriteAllBytes(screenshotPath, log.ScreenshotByteArray));
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error saving screenshot: {ex.Message}");
                        screenshotPath = "Screenshot save failed";
                    }
                }

                string logEntry = BuildLogEntry(log, screenshotPath);
                File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);

                //// Send log to SharePoint
                //await SendLogToSharePoint(log);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An error occurred while logging activity: {ex.Message}");
            }
        }

        private string BuildLogEntry(ActivityLog log, string screenShotPath)
        {
            string startTime = log.StartTime.ToString("M/d/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
            string endTime = log.EndTime != DateTime.MinValue ? log.EndTime.ToString("M/d/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture) : "";
            string duration = (log.EndTime != DateTime.MinValue ? (log.EndTime - log.StartTime).ToString(@"hh\:mm\:ss") : "");
            string screenshotBase64 = log.ScreenshotByteArray != null ? Convert.ToBase64String(log.ScreenshotByteArray) : "No screenshot captured";

            return $"Title: {log.Description}\n"
                 + $"\nID: {log.Id}\n"
                 + $"Application: {log.ApplicationName}\n"
                 + $"Description: {log.Description}\n"
                 + $"Start Time: {startTime}\n"
                 + (!string.IsNullOrEmpty(endTime) ? $"End Time: {endTime}\n" : "")
                 + (!string.IsNullOrEmpty(duration) ? $"Duration: {duration}\n" : "")
                 + (!string.IsNullOrEmpty(log.WebsiteUrl) ? $"Website URL: {log.WebsiteUrl}\n" : "")
                 + $"Is Idle: {log.IsIdle}\n"
                 + $"User: {log.User}\n"
                 + $"IP Address: {log.IpAddress}\n"
                 + $"Screenshot: {screenshotBase64}\n"

                 + $"Screenshot Path: {screenShotPath}\n"  // Log the file path instead of Base64
                 + "----------------------------------------";
        }
    }
}