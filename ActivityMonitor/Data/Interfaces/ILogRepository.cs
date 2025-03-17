using ActivityMonitor.Core.Models;

namespace ActivityMonitor.Data.Interfaces
{
    public interface ILogRepository
    {
        void LogActivity(ActivityLog log);
        //void StartProcessingQueue();
        //void LogScreenshot(string filePath);
    }
}
