using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using ServerActivityMonitor;

// Токен бота Telegram, полученный от BotFather
var botToken = "7844514365:AAFdfqiZ6BHkXDs6u7mr1oXNJfywTUV6Zs0";

// Создаем экземпляр бота
var bot = new TelegramBot(botToken);

Console.WriteLine("Starting bot...");
Console.WriteLine("Press Ctrl+C to exit.");

try
{
    // Запускаем бота с использованием long polling
    await bot.StartAsync();

    // Создаем CancellationTokenSource для корректного завершения работы
    var cts = new CancellationTokenSource();
    
    // Обработчик нажатия Ctrl+C для graceful shutdown
    Console.CancelKeyPress += (sender, args) =>
    {
        args.Cancel = true;
        cts.Cancel();
    };

    // Держим приложение запущенным до получения сигнала отмены
    await Task.Delay(-1, cts.Token);
}
catch (Exception ex)
{
    // Логирование ошибок
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}
