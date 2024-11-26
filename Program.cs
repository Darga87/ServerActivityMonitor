using ServerActivityMonitor;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("1. Monitor Local System");
        Console.WriteLine("2. Monitor Remote Server");
        Console.Write("Choose option (1 or 2): ");
        
        var choice = Console.ReadLine();
        
        if (choice == "1")
        {
            var serverMonitor = new ServerMonitor();
            serverMonitor.MonitorCpuUsage();
            serverMonitor.MonitorMemoryUsage();
        }
        else if (choice == "2")
        {
            Console.Write("Enter host: ");
            var host = Console.ReadLine();
            
            Console.Write("Enter username: ");
            var username = Console.ReadLine();
            
            Console.Write("Enter password: ");
            var password = Console.ReadLine();
            
            var remoteMonitor = new RemoteServerMonitor(host, username, password);
            
            while (true)
            {
                Console.WriteLine("\n1. Monitor server stats");
                Console.WriteLine("2. Execute custom command");
                Console.WriteLine("3. Exit");
                Console.Write("Choose option (1-3): ");
                
                var remoteChoice = Console.ReadLine();
                
                switch (remoteChoice)
                {
                    case "1":
                        remoteMonitor.MonitorRemoteServer();
                        break;
                    case "2":
                        Console.Write("Enter command to execute: ");
                        var command = Console.ReadLine();
                        remoteMonitor.ExecuteCommand(command);
                        break;
                    case "3":
                        return;
                    default:
                        Console.WriteLine("Invalid option");
                        break;
                }
            }
        }
    }
}
