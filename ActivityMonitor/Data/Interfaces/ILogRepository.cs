using ActivityMonitor.Core.Models;
using System.Threading.Tasks;

namespace ActivityMonitor.Data.Interfaces
{
    public interface ILogRepository
    {
        Task LogActivityAsync(ActivityLog log);

        //void LogScreenshot(string filePath);



        //void StartProcessingQueue();
    }
}
