using System;
using System.Runtime.InteropServices;

namespace ActivityMonitor.Core.Utilities
{
    public static class IdleTimeTracker
    {
        // Lock object for thread safety
        private static readonly object _lock = new object();

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        internal readonly struct LASTINPUTINFO
        {
            public readonly uint cbSize;
            public readonly uint dwTime;

            public LASTINPUTINFO(uint cbSize, uint dwTime)
            {
                this.cbSize = cbSize;
                this.dwTime = dwTime;
            }
        }

        /// <summary>
        /// Gets the idle time of the user.
        /// </summary>
        /// <returns>TimeSpan representing the idle time.</returns>
        public static TimeSpan GetIdleTime()
        {
            lock (_lock) 
            {
                LASTINPUTINFO lastInput = new LASTINPUTINFO((uint)Marshal.SizeOf(typeof(LASTINPUTINFO)), 0);

                if (GetLastInputInfo(ref lastInput))
                {
                    uint idleTime = (uint)Environment.TickCount - lastInput.dwTime;
                    return TimeSpan.FromMilliseconds(idleTime);
                }

                return TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Checks if the user is idle based on a threshold.
        /// </summary>
        /// <param name="idleThreshold">The threshold for idle time (e.g., 10 minutes).</param>
        /// <returns>True if the user is idle, otherwise false.</returns>
        public static bool IsUserIdle(TimeSpan idleThreshold)
        {
            try
            {
                return GetIdleTime() >= idleThreshold;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"An error occurred while checking idle time: {ex.Message}");
                return false;
            }
        }
    }
}