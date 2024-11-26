using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;

namespace ServerActivityMonitor
{
    /// <summary>
    /// Основной класс для работы с Telegram ботом
    /// </summary>
    public class TelegramBot
    {
        // Клиент Telegram Bot API
        private readonly ITelegramBotClient _botClient;
        // Монитор системных ресурсов
        private readonly ServerMonitor _monitor;
        // Таймер для периодических проверок
        private Timer? _monitoringTimer;
        // Список чатов для уведомлений
        private readonly HashSet<long> _subscribedChats;

        /// <summary>
        /// Конструктор класса TelegramBot
        /// </summary>
        /// <param name="token">Токен бота Telegram</param>
        public TelegramBot(string token)
        {
            _botClient = new TelegramBotClient(token);
            _monitor = new ServerMonitor();
            _subscribedChats = new HashSet<long>();
        }

        /// <summary>
        /// Запуск бота с использованием long polling
        /// </summary>
        public async Task StartAsync()
        {
            var cts = new CancellationTokenSource();
            
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>(),
                ThrowPendingUpdates = true
            };

            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            // Запускаем таймер для мониторинга
            _monitoringTimer = new Timer(CheckSystemStatus, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));

            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"Bot started successfully. @{me.Username}");
        }

        /// <summary>
        /// Периодическая проверка состояния системы
        /// </summary>
        private async void CheckSystemStatus(object? state)
        {
            if (_subscribedChats.Count == 0) return;

            var alerts = new List<string>();

            // Проверяем CPU
            if (_monitor.MonitorCpuUsage(out double cpuUsage))
            {
                alerts.Add($"⚠️ High CPU usage: {cpuUsage:0.00}%");
            }

            // Проверяем память
            if (_monitor.MonitorMemoryUsage(out double memoryUsage))
            {
                alerts.Add($"⚠️ High memory usage: {memoryUsage:0.00}%");
            }

            // Проверяем диски
            if (_monitor.MonitorDiskSpace())
            {
                alerts.Add("⚠️ Low disk space on one or more drives");
            }

            // Отправляем уведомления, если есть
            if (alerts.Count > 0)
            {
                var message = "🚨 System Alert 🚨\n" + string.Join("\n", alerts);
                foreach (var chatId in _subscribedChats)
                {
                    try
                    {
                        await _botClient.SendTextMessageAsync(chatId, message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error sending alert to {chatId}: {ex.Message}");
                    }
                }
            }
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Message is not { } message)
                    return;
                if (message.Text is not { } messageText)
                    return;

                var chatId = message.Chat.Id;
                Console.WriteLine($"Получено сообщение: {messageText}");

                var command = messageText.Split(' ')[0].ToLower();
                await HandleCommand(chatId, command, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка обработки сообщения: {ex.Message}");
            }
        }

        private async Task HandleCommand(long chatId, string command, CancellationToken cancellationToken)
        {
            switch (command)
            {
                case "/start":
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Добро пожаловать в Server Monitor Bot!\n" +
                              "Доступные команды:\n" +
                              "/start - Показать это сообщение\n" +
                              "/status - Показать состояние сервера\n" +
                              "/disk - Показать информацию о дисках\n" +
                              "/processes - Показать топ процессов\n" +
                              "/network - Показать сетевую статистику\n" +
                              "/subscribe - Включить уведомления\n" +
                              "/unsubscribe - Отключить уведомления",
                        cancellationToken: cancellationToken);
                    break;

                case "/status":
                    _monitor.MonitorCpuUsage(out double cpu);
                    _monitor.MonitorMemoryUsage(out double mem);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"💻 Состояние системы:\n" +
                              $"Загрузка ЦП: {cpu:0.00}%\n" +
                              $"Использование памяти: {mem:0.00}%",
                        cancellationToken: cancellationToken);
                    break;

                case "/disk":
                    var diskOutput = new StringWriter();
                    var diskConsole = Console.Out;
                    Console.SetOut(diskOutput);
                    _monitor.MonitorDiskSpace();
                    Console.SetOut(diskConsole);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"💾 Информация о дисках:\n{diskOutput}",
                        cancellationToken: cancellationToken);
                    break;

                case "/processes":
                    var processOutput = new StringWriter();
                    var processConsole = Console.Out;
                    Console.SetOut(processOutput);
                    _monitor.MonitorTopProcesses();
                    Console.SetOut(processConsole);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"⚙️ Топ процессов:\n{processOutput}",
                        cancellationToken: cancellationToken);
                    break;

                case "/network":
                    var networkOutput = new StringWriter();
                    var networkConsole = Console.Out;
                    Console.SetOut(networkOutput);
                    _monitor.MonitorNetworkActivity();
                    Console.SetOut(networkConsole);
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"🌐 Сетевая активность:\n{networkOutput}",
                        cancellationToken: cancellationToken);
                    break;

                case "/subscribe":
                    if (_subscribedChats.Add(chatId))
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "✅ Вы подписались на уведомления. Вы будете получать оповещения, когда системные ресурсы превысят пороговые значения.",
                            cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Вы уже подписаны на уведомления.",
                            cancellationToken: cancellationToken);
                    }
                    break;

                case "/unsubscribe":
                    if (_subscribedChats.Remove(chatId))
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "❌ Вы отписались от уведомлений.",
                            cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "Вы не подписаны на уведомления.",
                            cancellationToken: cancellationToken);
                    }
                    break;

                default:
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Неизвестная команда. Используйте /start для просмотра доступных команд.",
                        cancellationToken: cancellationToken);
                    break;
            }
        }

        private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var errorMessage = exception switch
            {
                ApiRequestException apiRequestException =>
                    $"Telegram API Error:\n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(errorMessage);
            return Task.CompletedTask;
        }
    }
}
