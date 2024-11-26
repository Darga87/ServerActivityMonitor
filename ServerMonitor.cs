using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;

namespace ServerActivityMonitor
{
    public class ServerMonitor
    {
        private Process process = Process.GetCurrentProcess();

        public void MonitorCpuUsage()
        {
            TimeSpan startCpuUsage = process.TotalProcessorTime;
            var startTime = DateTime.Now;
            
            System.Threading.Thread.Sleep(1000); // Wait for 1 second
            
            TimeSpan endCpuUsage = process.TotalProcessorTime;
            var endTime = DateTime.Now;

            double cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            double totalMsPassed = (endTime - startTime).TotalMilliseconds;
            double cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed) * 100;

            Console.WriteLine($"CPU Usage: {cpuUsageTotal:F2}%");
        }

        public void MonitorMemoryUsage()
        {
            var currentProcess = Process.GetCurrentProcess();
            var workingSet = currentProcess.WorkingSet64 / (1024 * 1024); // Convert to MB
            var virtualMemorySize = currentProcess.VirtualMemorySize64 / (1024 * 1024); // Convert to MB
            var privateMemorySize = currentProcess.PrivateMemorySize64 / (1024 * 1024); // Convert to MB

            Console.WriteLine($"Process Working Set (Memory): {workingSet} MB");
            Console.WriteLine($"Process Virtual Memory: {virtualMemorySize} MB");
            Console.WriteLine($"Process Private Memory: {privateMemorySize} MB");
        }
    }
}
