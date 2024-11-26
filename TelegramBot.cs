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

        /// <summary>
        /// Конструктор класса TelegramBot
        /// </summary>
        /// <param name="token">Токен бота Telegram</param>
        public TelegramBot(string token)
        {
            _botClient = new TelegramBotClient(token);
            _monitor = new ServerMonitor();
        }

        /// <summary>
        /// Запуск бота с использованием long polling
        /// </summary>
        public async Task StartAsync()
        {
            var cts = new CancellationTokenSource();
            
            // Настройка параметров получения обновлений
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = Array.Empty<UpdateType>(), // Принимаем все типы обновлений
                ThrowPendingUpdates = true // Пропускаем старые обновления при запуске
            };

            // Запуск получения обновлений
            _botClient.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: cts.Token
            );

            // Получение и вывод информации о боте
            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"Bot started successfully. @{me.Username}");
        }

        /// <summary>
        /// Обработчик входящих сообщений
        /// </summary>
        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                // Проверяем, что получили текстовое сообщение
                if (update.Message is not { } message)
                    return;
                if (message.Text is not { } messageText)
                    return;

                var chatId = message.Chat.Id;
                Console.WriteLine($"Received message: {messageText}");

                // Обработка команд, начинающихся с '/'
                if (messageText.StartsWith("/"))
                {
                    await HandleCommand(chatId, messageText, cancellationToken);
                }
                else
                {
                    // Ответ на обычные сообщения
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Use /start to see available commands",
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling update: {ex.Message}");
            }
        }

        /// <summary>
        /// Обработчик команд бота
        /// </summary>
        private async Task HandleCommand(long chatId, string command, CancellationToken cancellationToken)
        {
            switch (command.ToLower())
            {
                case "/start":
                    // Отправка приветственного сообщения и списка команд
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Welcome to Server Monitor Bot!\n" +
                              "Available commands:\n" +
                              "/start - Show this help message\n" +
                              "/status - Show server status",
                        cancellationToken: cancellationToken);
                    break;

                case "/status":
                    // Получение и отправка информации о состоянии сервера
                    var status = GetServerStatus();
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: status,
                        cancellationToken: cancellationToken);
                    break;

                default:
                    // Ответ на неизвестную команду
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "Unknown command. Use /start to see available commands.",
                        cancellationToken: cancellationToken);
                    break;
            }
        }

        /// <summary>
        /// Получение информации о состоянии сервера
        /// </summary>
        private string GetServerStatus()
        {
            // Перенаправляем вывод консоли в строку
            var output = new StringWriter();
            var console = Console.Out;
            Console.SetOut(output);

            // Получаем информацию о CPU и памяти
            _monitor.MonitorCpuUsage();
            _monitor.MonitorMemoryUsage();

            // Восстанавливаем вывод консоли и возвращаем результат
            Console.SetOut(console);
            return output.ToString();
        }

        /// <summary>
        /// Обработчик ошибок при получении обновлений
        /// </summary>
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
