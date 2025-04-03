using ActivityMonitor.Data.Interfaces;
using ActivityMonitor.Data.Repositories;
using ActivityMonitor.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
            var host = CreateHostBuilder().Build();
            var logRepository = host.Services.GetRequiredService<ILogRepository>();
            var configuration = host.Services.GetRequiredService<IConfiguration>();

            string logFilePath = configuration["Logging:LogFilePath"];
            ActivityMonitorService service = new ActivityMonitorService(logRepository, logFilePath);

            ProcessHelper.CaptureScreenshot(configuration);

            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                service
            };
            ServiceBase.Run(ServicesToRun);
        }

        private static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<ILogRepository, FileLogRepository>();
                });
    }
}