using ActivityMonitor.Data.Interfaces;
using ActivityMonitor.Data.Repositories;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ActivityMonitor
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {

            string logFilePath = "C:\\Users\\DavidAderibigbe\\source\\repos\\ActivityMonitor\\ActivityMonitor\\logs\\activity_log.txt";
            ILogRepository logRepository = new FileLogRepository();

            //// Read log file path from environment variable
            //string logFilePath = Environment.GetEnvironmentVariable("LOG_FILE_PATH");

            //if (string.IsNullOrEmpty(logFilePath))
            //{
            //    // Fallback default value if the environment variable is not set
            //    logFilePath = "C:\\Logs\\activity_log.txt";
            //}
            //ILogRepository logRepository = new FileLogRepository(logFilePath); // Pass log file path to repository

            ActivityMonitorService service = new ActivityMonitorService(logRepository, logFilePath);

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                //new ActivityMonitorService()
                service
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}