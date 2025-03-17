using System;
using System.Runtime.InteropServices;

namespace ActivityMonitor.Core.Utilities
{
    public static class IdleTimeTracker
    {
        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
        internal struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        /// <summary>
        /// Gets the idle time of the user.
        /// </summary>
        /// <returns>TimeSpan representing the idle time.</returns>
        public static TimeSpan GetIdleTime()
        {
            LASTINPUTINFO lastInput = new LASTINPUTINFO();
            lastInput.cbSize = (uint)Marshal.SizeOf(typeof(LASTINPUTINFO));

            if (GetLastInputInfo(ref lastInput))
            {
                uint idleTime = (uint)Environment.TickCount - lastInput.dwTime;
                return TimeSpan.FromMilliseconds(idleTime);
            }

            return TimeSpan.Zero;
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

                return false;
            }
        }
    }
}
