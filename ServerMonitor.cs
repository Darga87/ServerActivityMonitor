using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Management;
using System.IO;

namespace ServerActivityMonitor
{
    public class ServerMonitor
    {
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _ramCounter;
        private const double CPU_THRESHOLD = 80.0;
        private const double MEMORY_THRESHOLD = 90.0;
        private const double DISK_THRESHOLD = 90.0;

        public ServerMonitor()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _ramCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");
                // First call to NextValue() always returns 0
                _cpuCounter.NextValue();
                _ramCounter.NextValue();
            }
        }

        public bool MonitorCpuUsage(out double cpuUsage)
        {
            cpuUsage = 0;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("CPU monitoring is only supported on Windows.");
                return false;
            }

            cpuUsage = _cpuCounter.NextValue();
            Console.WriteLine($"CPU Usage: {cpuUsage:0.00}%");
            return cpuUsage > CPU_THRESHOLD;
        }

        public bool MonitorMemoryUsage(out double memoryUsage)
        {
            memoryUsage = 0;
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("Memory monitoring is only supported on Windows.");
                return false;
            }

            memoryUsage = _ramCounter.NextValue();
            Console.WriteLine($"Memory Usage: {memoryUsage:0.00}%");
            return memoryUsage > MEMORY_THRESHOLD;
        }

        public bool MonitorDiskSpace()
        {
            bool thresholdExceeded = false;
            DriveInfo[] allDrives = DriveInfo.GetDrives();

            foreach (DriveInfo drive in allDrives)
            {
                if (!drive.IsReady) continue;

                double usedSpace = 100.0 * (drive.TotalSize - drive.AvailableFreeSpace) / drive.TotalSize;
                Console.WriteLine($"Drive {drive.Name}:");
                Console.WriteLine($"  Total space: {FormatSize(drive.TotalSize)}");
                Console.WriteLine($"  Free space: {FormatSize(drive.AvailableFreeSpace)}");
                Console.WriteLine($"  Used space: {usedSpace:0.00}%");

                if (usedSpace > DISK_THRESHOLD)
                {
                    thresholdExceeded = true;
                }
            }

            return thresholdExceeded;
        }

        public void MonitorTopProcesses()
        {
            var processes = Process.GetProcesses()
                .OrderByDescending(p => p.WorkingSet64)
                .Take(5);

            Console.WriteLine("Top 5 processes by memory usage:");
            foreach (var process in processes)
            {
                try
                {
                    Console.WriteLine($"{process.ProcessName}: {FormatSize(process.WorkingSet64)}");
                }
                catch (Exception)
                {
                    // Skip processes we can't access
                }
            }
        }

        public void MonitorNetworkActivity()
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface ni in interfaces)
            {
                if (ni.OperationalStatus == OperationalStatus.Up)
                {
                    Console.WriteLine($"Interface: {ni.Name}");
                    Console.WriteLine($"  Speed: {FormatSize(ni.Speed)}");
                    Console.WriteLine($"  Type: {ni.NetworkInterfaceType}");
                    
                    IPv4InterfaceStatistics stats = ni.GetIPv4Statistics();
                    Console.WriteLine($"  Bytes received: {FormatSize(stats.BytesReceived)}");
                    Console.WriteLine($"  Bytes sent: {FormatSize(stats.BytesSent)}");
                }
            }
        }

        private string FormatSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }
            return $"{size:0.##} {sizes[order]}";
        }
    }
}
