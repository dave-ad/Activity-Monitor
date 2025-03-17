using ActivityMonitor.Core.Models;
using ActivityMonitor.Core.Utilities;
using ActivityMonitor.Data.Interfaces;
using ActivityMonitor.Helpers;
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
        private readonly string _logFilePath;
        private readonly Random _random = new Random();
        private DateTime _systemStartTime;
        private DateTime _systemShutdownTime;
        private DateTime _lastLogTime = DateTime.Now;
        private string _lastState = "Active";
        private readonly string _screenshotDirectory = @"C:\\Users\\DavidAderibigbe\\source\\repos\\ActivityMonitor\\ActivityMonitor\\logs\\Screenshots";

        public ActivityMonitorService(ILogRepository logRepository, string logFilePath)
        {
            _logRepository = logRepository ?? throw new ArgumentNullException(nameof(logRepository));
            _logFilePath = logFilePath ?? throw new ArgumentNullException(nameof(logFilePath));
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                _systemStartTime = DateTime.Now;
                _lastLogTime = _systemStartTime;
                _systemShutdownTime = DateTime.MinValue;

                _logRepository.LogActivity(new ActivityLog
                {
                    Id = Guid.NewGuid().GetHashCode(),
                    ApplicationName = "System",
                    Description = "System started.",
                    StartTime = _systemStartTime,
                    EndTime = _systemShutdownTime,
                    IsIdle = false,
                    //User = Environment.UserName
                    User = UserHelper.GetActiveUser(),
                    IpAddress = UserHelper.GetLocalIPAddress()
                });

                _monitorTimer = new Timer();
                _monitorTimer.Elapsed += OnMonitorTimerElapsed;
                RunAtInterval();
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
                // System shutdown
                _systemShutdownTime = DateTime.Now;
                _logRepository.LogActivity(new ActivityLog
                {
                    Id = Guid.NewGuid().GetHashCode(),
                    ApplicationName = "System",
                    Description = "System shut down.",
                    StartTime = _systemStartTime,
                    EndTime = _systemShutdownTime,
                    IsIdle = false,
                    //User = Environment.UserName
                    User = UserHelper.GetActiveUser()
                });

                // Total duration the system was active
                TimeSpan uptime = _systemShutdownTime - _systemStartTime;
                _logRepository.LogActivity(new ActivityLog
                {
                    Id = Guid.NewGuid().GetHashCode(),
                    ApplicationName = "System",
                    Description = $"System was active for {uptime}.",
                    StartTime = _systemStartTime,
                    EndTime = _systemShutdownTime,
                    IsIdle = false,
                    //User = Environment.UserName,
                    User = UserHelper.GetActiveUser(),
                    IpAddress = UserHelper.GetLocalIPAddress()
                });

                _monitorTimer.Stop();
                _monitorTimer.Dispose();
            }
            catch (Exception ex)
            {
                LogError("Shutdown Exception", ex);
            }
        }

        private void OnMonitorTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                string activeApplication = ProcessHelper.GetActiveApplication();
                TimeSpan idleThreshold = TimeSpan.FromMinutes(10);
                bool isUserIdle = IdleTimeTracker.IsUserIdle(idleThreshold);
                File.AppendAllText(_logFilePath, $"[{DateTime.Now}] ActiveApp: {activeApplication}, IsIdle: {isUserIdle}{Environment.NewLine}");

                if (string.IsNullOrEmpty(activeApplication) || activeApplication == "Unknown")
                    activeApplication = "Unknown Application";

                if (_lastState == "Idle" && isUserIdle)
                {
                    return; // Skip logging if already idle
                }

                if (_lastState == "Active" && !isUserIdle)
                {
                    return; // Skip logging if already active
                }

                TimeSpan duration = DateTime.Now - _lastLogTime;

                _logRepository.LogActivity(new ActivityLog
                {
                    Id = Guid.NewGuid().GetHashCode(),
                    ApplicationName = isUserIdle ? "Idle" : activeApplication,
                    Description = isUserIdle ? $"User was idle for {duration}." : $"User was active in {activeApplication}.",
                    StartTime = _lastLogTime,
                    EndTime = DateTime.Now,
                    IsIdle = isUserIdle,
                    User = UserHelper.GetActiveUser(),
                    IpAddress = UserHelper.GetLocalIPAddress()
                });

                _lastState = isUserIdle ? "Idle" : "Active";
                _lastLogTime = DateTime.Now;

                if (!isUserIdle)
                {
                    byte[] screenshotBytes = ProcessHelper.CaptureScreenshotAsByteArray();
                    string screenshotFilePath = Path.Combine(_screenshotDirectory, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
                    ProcessHelper.SaveByteArrayToImage(screenshotBytes, screenshotFilePath);

                    _logRepository.LogActivity(new ActivityLog
                    {
                        Id = Guid.NewGuid().GetHashCode(),
                        ApplicationName = activeApplication,
                        Description = $"Screenshot captured: {screenshotFilePath}",
                        StartTime = DateTime.Now,
                        EndTime = DateTime.Now,
                        IsIdle = false,
                        User = UserHelper.GetActiveUser(),
                        ScreenshotByteArray = screenshotBytes
                    });
                }

                RunAtInterval();
            }
            catch (Exception ex)
            {
                LogError("Exception", ex);
            }
        }

        private void RunAtInterval()
        {
            //int minInterval = 7200000;
            //int maxInterval = 10800000;
            int minInterval = 60000; // 1 minute
            int maxInterval = 300000; // 5 minutes

            int randomInterval = _random.Next(minInterval, maxInterval);
            _monitorTimer.Interval = randomInterval;
            _monitorTimer.Start();
        }

        private void LogError(string context, Exception ex)
        {
            string message = $"[{DateTime.Now}] {context}: {ex.Message}{Environment.NewLine}Stack Trace: {ex.StackTrace}{Environment.NewLine}";
            File.AppendAllText(_logFilePath, message);

            _logRepository.LogActivity(new ActivityLog
            {
                Id = Guid.NewGuid().GetHashCode(),
                ApplicationName = "Error",
                Description = message,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now,
                IsIdle = false,
                User = UserHelper.GetActiveUser(),
                //User = Environment.UserName
            });
        }
    }
}