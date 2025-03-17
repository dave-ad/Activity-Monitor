using ActivityMonitor.Core.Models;
using ActivityMonitor.Data.Interfaces;
using ActivityMonitor.Helpers;
using System;
using System.Globalization;
using System.IO;

namespace ActivityMonitor.Data.Repositories
{
    public class FileLogRepository : ILogRepository
    {
        private string LogFilePath { get; }
        private DateTime SystemStartTime { get; set; }

        public FileLogRepository(string logFilePath = null)
        {
            LogFilePath = logFilePath ?? "C:\\Users\\DavidAderibigbe\\source\\repos\\ActivityMonitor\\ActivityMonitor\\logs\\activity_log.txt";
            EnsureLogDirectoryExists();
        }

        private void EnsureLogDirectoryExists()
        {
            var logDirectory = Path.GetDirectoryName(LogFilePath);
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);
        }

        public void LogActivity(ActivityLog log)
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

            string logEntry = BuildLogEntry(log);
            File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
        }

        private string BuildLogEntry(ActivityLog log)
        {
            string startTime = log.StartTime.ToString("M/d/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
            string endTime = log.EndTime != DateTime.MinValue ? log.EndTime.ToString("M/d/yyyy hh:mm:ss tt", CultureInfo.InvariantCulture) : "";
            string duration = (log.EndTime != DateTime.MinValue ? (log.EndTime - log.StartTime).ToString(@"hh\:mm\:ss") : "");
            string screenshotBase64 = log.ScreenshotByteArray != null ? "(Base64 string of screenshot)" : "No screenshot captured";

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
                 + $"Screenshot ByteArray: {screenshotBase64}\n"
                 + "----------------------------------------";
        }
    }
}