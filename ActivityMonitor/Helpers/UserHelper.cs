using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management;
using System.Net;

namespace ActivityMonitor.Helpers
{
    public static class UserHelper
    {
        public static string GetActiveUser()
        {
            try
            {
                string query = "SELECT UserName FROM Win32_ComputerSystem";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                using (ManagementObjectCollection results = searcher.Get())
                {
                    foreach (ManagementObject mo in results)
                    {
                        string user = mo["UserName"]?.ToString();
                        return string.IsNullOrEmpty(user) ? "Unknown User" : user;
                    }
                }
            }
            catch (Exception ex)
            {
                ProcessHelper.LogError("Error retrieving active user", ex);
            }
            return "Unknown User";
        }

        public static string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList) 
                { 
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                ProcessHelper.LogError("Error retrieving IP address", ex);
            }
            return "Unknown IP";
        }
    }
}
