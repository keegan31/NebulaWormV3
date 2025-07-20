using System;
using System.Threading;

public static class Cpuload // this module is for fun u can delete this if u want to
{
    private static Random rnd = new Random();
    private static bool running = true;

    public static void Start()
    {
        Thread cpuThread = new Thread(new ThreadStart(SpikeLoop));
        cpuThread.IsBackground = true;
        cpuThread.Start();
    }

    private static void SpikeLoop()
    {
        while (running)
        {
            int waitBeforeSpike = rnd.Next(100000, 300000); 
            Thread.Sleep(waitBeforeSpike);

            int spikeDuration = rnd.Next(9000, 10000); // 9-10 seconds u  can change it 

            DateTime end = DateTime.Now.AddMilliseconds(spikeDuration);
            while (DateTime.Now < end)
            {
             
                for (int i = 0; i < Environment.ProcessorCount; i++)
                {
                    new Thread(() =>
                    {
                        while (DateTime.Now < end) { }
                    }).Start();
                }
            }

        }
    }

    public static void Stop()
    {
        running = false;
    }
}
