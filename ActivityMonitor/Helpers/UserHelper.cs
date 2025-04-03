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
        private static string _cachedUser = null;
        private static string _cachedIpAddress = null;
        private static DateTime _lastCacheTime = DateTime.MinValue;

        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
        public static string GetActiveUser()
        {
            if (_cachedUser != null && DateTime.Now - _lastCacheTime < CacheDuration)
            {
                return _cachedUser;
            }

            try
            {
                string query = "SELECT UserName FROM Win32_ComputerSystem";
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(query))
                using (ManagementObjectCollection results = searcher.Get())
                {
                    foreach (ManagementObject mo in results)
                    {
                        string user = mo["UserName"]?.ToString();
                        _cachedUser = string.IsNullOrEmpty(user) ? "Unknown User" : user;
                        _lastCacheTime = DateTime.Now;
                        return _cachedUser;
                    }
                }
            }
            catch (Exception ex)
            {
                ProcessHelper.LogError("Error retrieving active user", ex);
            }

            _cachedUser = "Unknown User";
            _lastCacheTime = DateTime.Now;
            return _cachedUser;
        }

        public static string GetLocalIPAddress()
        {
            if (_cachedIpAddress != null && DateTime.Now - _lastCacheTime < CacheDuration)
            {
                return _cachedIpAddress;
            }

            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList) 
                { 
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        _cachedIpAddress = ip.ToString();
                        _lastCacheTime = DateTime.Now;
                        return _cachedIpAddress;
                    }
                }
            }
            catch (Exception ex)
            {
                ProcessHelper.LogError("Error retrieving IP address", ex);
            }
            _cachedIpAddress = "Unknown IP";
            _lastCacheTime = DateTime.Now;
            return _cachedIpAddress;
        }
    }
}