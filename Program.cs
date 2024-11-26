using ServerActivityMonitor;

class Program
{
    static async Task Main(string[] args)
    {
        var botToken = "7936945903:AAFoRWYpkrkfGsXiegB7z84JhI4LuYOYYNs";
        var bot = new TelegramBot(botToken);
        
        await bot.StartAsync();
        
        Console.WriteLine("Bot started! Press Enter to exit.");
        Console.ReadLine();
    }
}
