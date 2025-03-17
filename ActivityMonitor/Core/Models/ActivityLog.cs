using System;

namespace ActivityMonitor.Core.Models
{
    public class ActivityLog
    {
        public int Id { get; set; }
        public string ApplicationName { get; set; }
        public string Description { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public string WebsiteUrl { get; set; }
        public bool IsIdle { get; set; }
        public string User { get; set; }
        public string IpAddress { get; set; }
        public byte[] ScreenshotByteArray { get; set; }
        public string ScreenshotUrl { get; set; } 
    }
}
