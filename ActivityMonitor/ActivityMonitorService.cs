using ActivityMonitor.Core.Models;
using ActivityMonitor.Core.Utilities;
using ActivityMonitor.Data.Interfaces;
using ActivityMonitor.Helpers;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Timers;

namespace ActivityMonitor
{
    public partial class ActivityMonitorService : ServiceBase
    {
        private Timer _monitorTimer;
        private readonly ILogRepository _logRepository;
        private readonly IConfiguration _configuration;
        private readonly Random _random = new Random();
        private DateTime _systemStartTime;
        private DateTime _systemShutdownTime;
        private DateTime _lastLogTime;
        private string _lastState = "Active";
        private readonly string _screenshotDirectory;
        private readonly TimeSpan _idleThreshold;

        private readonly object _timerLock = new object();

        public ActivityMonitorService(ILogRepository logRepository, IConfiguration configuration)
        {
            _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _screenshotDirectory = _configuration["Logging:ScreenshotDirectory"];
            _idleThreshold = TimeSpan.FromMinutes(10);
            _lastLogTime = DateTime.Now;

            EnsureDirectoryExists(_screenshotDirectory);
        }
        protected override void OnStart(string[] args)
        {
            try
            {
                _systemStartTime = DateTime.Now;
                LogSystemEvent("System started.", false);
                StartMonitoring();
            }
            catch (Exception ex)
            {
                LogError("Startup Exception", ex);
                throw;
            }
        }
        protected override void OnStop()
        {
            try
            {
                _systemShutdownTime = DateTime.Now;
                LogSystemEvent("System shut down.", false);
                _monitorTimer?.Stop();
                _monitorTimer?.Dispose();
            }
            catch (Exception ex)
            {
                LogError("Shutdown Exception", ex);
            }
        }
        private void StartMonitoring()
        {
            _monitorTimer = new Timer();
            _monitorTimer.Elapsed += OnMonitorTimerElapsed;
            RunAtInterval();
        }
        private void OnMonitorTimerElapsed(object sender, ElapsedEventArgs e)
        {
            lock (_timerLock)
            {
                try
                {
                    bool isUserIdle = IdleTimeTracker.IsUserIdle(_idleThreshold);
                    string activeApplication = isUserIdle ? "Idle" : ProcessHelper.GetActiveApplication() ?? "Unknown Application";

                    if (ShouldLogStateChange(isUserIdle))
                    {
                        LogUserActivity(activeApplication, isUserIdle);
                        CaptureAndLogScreenshot(activeApplication, isUserIdle);
                    }
                    RunAtInterval();
                }
                catch (Exception ex)
                {
                    LogError("Monitor Timer Exception", ex);
                }
            }
        }
        private bool ShouldLogStateChange(bool isUserIdle)
        {
            string newState = isUserIdle ? "Idle" : "Active";
            if (_lastState == newState)
                return false;

            _lastState = newState;
            return true;
        }
        private void LogUserActivity(string applicationName, bool isIdle)
        {
            TimeSpan duration = DateTime.Now - _lastLogTime;
            string description = isIdle ? $"User was idle for {duration}." : $"User was active in {applicationName}.";

            _logRepository.LogActivityAsync(new ActivityLog
            {
                Id = Guid.NewGuid().GetHashCode(),
                ApplicationName = applicationName,
                Description = description,
                StartTime = _lastLogTime,
                EndTime = DateTime.Now,
                IsIdle = isIdle,
                User = UserHelper.GetActiveUser(),
                IpAddress = UserHelper.GetLocalIPAddress()
            });

            _lastLogTime = DateTime.Now;
        }
        private void CaptureAndLogScreenshot(string applicationName, bool isIdle)
        {
            if (isIdle) return;

            try
            {
                // Capture screenshot using the ProcessHelper method
                string filePath = ProcessHelper.CaptureScreenshot(_configuration);

                // Only log if the screenshot was successfully captured
                if (!string.IsNullOrEmpty(filePath))
                {
                    _logRepository.LogActivityAsync(new ActivityLog
                    {
                        Id = Guid.NewGuid().GetHashCode(),
                        ApplicationName = applicationName,
                        Description = $"Screenshot captured: {filePath}",
                        StartTime = DateTime.Now,
                        EndTime = DateTime.Now,
                        IsIdle = false,
                        User = UserHelper.GetActiveUser()
                    });
                }
            }
            catch (Exception ex)
            {
                LogError("Error during screenshot capture and logging", ex);
            }
        }

        private void RunAtInterval()
        {
            int randomInterval = GetRandomInterval();
            _monitorTimer.Interval = randomInterval;
            _monitorTimer.Start();
        }
        private int GetRandomInterval()
        {
            int minInterval = 60000;  // 1 minute
            int maxInterval = 300000; // 5 minutes
            return _random.Next(minInterval, maxInterval);
        }
        private void EnsureDirectoryExists(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }
        private void LogSystemEvent(string description, bool isIdle)
        {
            _logRepository.LogActivityAsync(new ActivityLog
            {
                Id = Guid.NewGuid().GetHashCode(),
                ApplicationName = "System",
                Description = description,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now,
                IsIdle = isIdle,
                User = UserHelper.GetActiveUser(),
                IpAddress = UserHelper.GetLocalIPAddress()
            });
        }
        private void LogError(string context, Exception ex)
        {
            string message = $"[{DateTime.Now}] {context}: {ex.Message}\nStack Trace: {ex.StackTrace}\n";

            _logRepository.LogActivityAsync(new ActivityLog
            {
                Id = Guid.NewGuid().GetHashCode(),
                ApplicationName = "Error",
                Description = message,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now,
                IsIdle = false,
                User = UserHelper.GetActiveUser(),
            });
        }
    }
}