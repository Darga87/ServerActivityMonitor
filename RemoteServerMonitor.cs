using System;
using Renci.SshNet;

namespace ServerActivityMonitor
{
    public class RemoteServerMonitor
    {
        private readonly string host;
        private readonly string username;
        private readonly string password;
        private readonly int port;

        public RemoteServerMonitor(string host, string username, string password, int port = 22)
        {
            this.host = host;
            this.username = username;
            this.password = password;
            this.port = port;
        }

        public void MonitorRemoteServer()
        {
            try
            {
                using (var client = new SshClient(host, port, username, password))
                {
                    client.Connect();
                    Console.WriteLine($"Connected to {host}");

                    // Get CPU usage
                    var cpuCommand = client.RunCommand("top -bn1 | grep \"Cpu(s)\" | awk '{print $2 + $4}'");
                    Console.WriteLine($"CPU Usage: {cpuCommand.Result}%");

                    // Get memory usage
                    var memoryCommand = client.RunCommand("free -m | grep Mem | awk '{print $2,$3,$4}'");
                    var memoryParts = memoryCommand.Result.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (memoryParts.Length >= 3)
                    {
                        Console.WriteLine($"Total Memory: {memoryParts[0]} MB");
                        Console.WriteLine($"Used Memory: {memoryParts[1]} MB");
                        Console.WriteLine($"Free Memory: {memoryParts[2]} MB");
                    }

                    // Get disk usage
                    var diskCommand = client.RunCommand("df -h / | tail -1 | awk '{print $5}'");
                    Console.WriteLine($"Disk Usage: {diskCommand.Result}");

                    client.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to remote server: {ex.Message}");
            }
        }

        public void ExecuteCommand(string command)
        {
            try
            {
                using (var client = new SshClient(host, port, username, password))
                {
                    client.Connect();
                    Console.WriteLine($"Connected to {host}");

                    var result = client.RunCommand(command);
                    Console.WriteLine("Command output:");
                    Console.WriteLine(result.Result);

                    client.Disconnect();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error executing command: {ex.Message}");
            }
        }
    }
}
