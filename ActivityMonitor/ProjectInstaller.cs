using System.ComponentModel;
using System.Configuration.Install;
using System.ServiceProcess;

namespace ActivityMonitor
{
    [RunInstaller(true)]
    public class ProjectInstaller : Installer
    {
        private ServiceProcessInstaller serviceProcessInstaller;
        private ServiceInstaller serviceInstaller;

        public ProjectInstaller()
        {
            // Initialize the service process installer
            serviceProcessInstaller = new ServiceProcessInstaller();
            serviceProcessInstaller.Account = ServiceAccount.LocalSystem; // Use LocalSystem account (or another if needed)

            // Initialize the service installer
            serviceInstaller = new ServiceInstaller();
            serviceInstaller.ServiceName = "ActivityMonitorService";
            serviceInstaller.DisplayName = "Activity Monitor Service";
            serviceInstaller.StartType = ServiceStartMode.Manual; // Can be automatic, manual, or disabled

            // Add installers to the Installers collection
            Installers.Add(serviceProcessInstaller);
            Installers.Add(serviceInstaller);
        }
    }
}
