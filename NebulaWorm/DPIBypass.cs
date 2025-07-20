using System;
using System.Runtime.InteropServices;

public static class DPIBypass
{
    [DllImport("user32.dll")]
    private static extern bool SetProcessDPIAware();

    public static void Apply()
    {
        try { SetProcessDPIAware(); } catch { }
    }
}
