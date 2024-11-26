using ServerActivityMonitor;

class Program
{
    static void Main(string[] args)
    {
        var serverMonitor = new ServerMonitor();
        serverMonitor.MonitorCpuUsage();
        serverMonitor.MonitorMemoryUsage();
        Console.WriteLine("Hello, World!");
    }
}
