using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;

namespace ServerActivityMonitor
{
    public class TelegramBot
    {
        private readonly ITelegramBotClient _botClient;
        private readonly ConcurrentDictionary<long, ServerCredentials> _serverCredentials;
        private readonly ServerMonitor _localMonitor;

        public TelegramBot(string token)
        {
            _botClient = new TelegramBotClient(token);
            _serverCredentials = new ConcurrentDictionary<long, ServerCredentials>();
            _localMonitor = new ServerMonitor();
        }

        public async Task StartAsync()
        {
            var cts = new CancellationTokenSource();

            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { }
            };

            _botClient.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken: cts.Token);

            var me = await _botClient.GetMeAsync();
            Console.WriteLine($"Start listening for @{me.Username}");
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                if (update.Message is not { Text: { } messageText } message)
                    return;

                var chatId = message.Chat.Id;

                Console.WriteLine($"Received a '{messageText}' message in chat {chatId}.");

                if (messageText.StartsWith("/"))
                {
                    await HandleCommand(message, cancellationToken);
                }
                else if (_serverCredentials.TryGetValue(chatId, out var credentials) && credentials.AwaitingInput)
                {
                    await HandleCredentialsInput(message, credentials, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling message: {ex}");
            }
        }

        private async Task HandleCommand(Message message, CancellationToken cancellationToken)
        {
            var command = message.Text.Split(' ')[0].ToLower();
            var chatId = message.Chat.Id;

            switch (command)
            {
                case "/start":
                    await SendWelcomeMessage(chatId, cancellationToken);
                    break;

                case "/local":
                    await MonitorLocalSystem(chatId, cancellationToken);
                    break;

                case "/addserver":
                    await InitiateServerAdd(chatId, cancellationToken);
                    break;

                case "/listservers":
                    await ListServers(chatId, cancellationToken);
                    break;

                case "/monitor":
                    if (_serverCredentials.TryGetValue(chatId, out var credentials))
                    {
                        await MonitorRemoteServer(chatId, credentials, cancellationToken);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId,
                            "No server configured. Use /addserver first.",
                            cancellationToken: cancellationToken);
                    }
                    break;

                case "/execute":
                    if (message.Text.Split(' ').Length < 2)
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId,
                            "Please provide a command to execute. Format: /execute <command>",
                            cancellationToken: cancellationToken);
                        return;
                    }

                    if (_serverCredentials.TryGetValue(chatId, out var creds))
                    {
                        var cmd = message.Text.Substring(message.Text.IndexOf(' ') + 1);
                        await ExecuteRemoteCommand(chatId, creds, cmd, cancellationToken);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId,
                            "No server configured. Use /addserver first.",
                            cancellationToken: cancellationToken);
                    }
                    break;

                default:
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "Unknown command. Use /start to see available commands.",
                        cancellationToken: cancellationToken);
                    break;
            }
        }

        private async Task SendWelcomeMessage(long chatId, CancellationToken cancellationToken)
        {
            var welcomeMessage = "Welcome to Server Monitor Bot!\n\n" +
                               "Available commands:\n" +
                               "/start - Show this message\n" +
                               "/local - Monitor local system\n" +
                               "/addserver - Add a remote server\n" +
                               "/listservers - List configured servers\n" +
                               "/monitor - Monitor remote server\n" +
                               "/execute <command> - Execute command on remote server";

            await _botClient.SendTextMessageAsync(
                chatId,
                welcomeMessage,
                cancellationToken: cancellationToken);
        }

        private async Task MonitorLocalSystem(long chatId, CancellationToken cancellationToken)
        {
            try
            {
                var output = new System.IO.StringWriter();
                var console = Console.Out;
                Console.SetOut(output);

                _localMonitor.MonitorCpuUsage();
                _localMonitor.MonitorMemoryUsage();

                Console.SetOut(console);
                await _botClient.SendTextMessageAsync(
                    chatId,
                    output.ToString(),
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"Error monitoring local system: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task InitiateServerAdd(long chatId, CancellationToken cancellationToken)
        {
            var credentials = new ServerCredentials { AwaitingInput = true, InputStep = InputStep.Host };
            _serverCredentials.AddOrUpdate(chatId, credentials, (_, _) => credentials);

            await _botClient.SendTextMessageAsync(
                chatId,
                "Please enter the server host (IP address or domain):",
                cancellationToken: cancellationToken);
        }

        private async Task HandleCredentialsInput(Message message, ServerCredentials credentials, CancellationToken cancellationToken)
        {
            var chatId = message.Chat.Id;
            var input = message.Text;

            switch (credentials.InputStep)
            {
                case InputStep.Host:
                    credentials.Host = input;
                    credentials.InputStep = InputStep.Username;
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "Please enter the username:",
                        cancellationToken: cancellationToken);
                    break;

                case InputStep.Username:
                    credentials.Username = input;
                    credentials.InputStep = InputStep.Password;
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "Please enter the password:",
                        cancellationToken: cancellationToken);
                    break;

                case InputStep.Password:
                    credentials.Password = input;
                    credentials.AwaitingInput = false;
                    await _botClient.SendTextMessageAsync(
                        chatId,
                        "Server credentials saved! You can now use /monitor to monitor the server.",
                        cancellationToken: cancellationToken);
                    break;
            }
        }

        private async Task ListServers(long chatId, CancellationToken cancellationToken)
        {
            if (_serverCredentials.TryGetValue(chatId, out var credentials) && !string.IsNullOrEmpty(credentials.Host))
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"Configured server:\nHost: {credentials.Host}\nUsername: {credentials.Username}",
                    cancellationToken: cancellationToken);
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    "No servers configured. Use /addserver to add a server.",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task MonitorRemoteServer(long chatId, ServerCredentials credentials, CancellationToken cancellationToken)
        {
            try
            {
                var remoteMonitor = new RemoteServerMonitor(credentials.Host, credentials.Username, credentials.Password);
                
                var output = new System.IO.StringWriter();
                var console = Console.Out;
                Console.SetOut(output);

                remoteMonitor.MonitorRemoteServer();

                Console.SetOut(console);
                await _botClient.SendTextMessageAsync(
                    chatId,
                    output.ToString(),
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"Error monitoring remote server: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task ExecuteRemoteCommand(long chatId, ServerCredentials credentials, string command, CancellationToken cancellationToken)
        {
            try
            {
                var remoteMonitor = new RemoteServerMonitor(credentials.Host, credentials.Username, credentials.Password);
                
                var output = new System.IO.StringWriter();
                var console = Console.Out;
                Console.SetOut(output);

                remoteMonitor.ExecuteCommand(command);

                Console.SetOut(console);
                await _botClient.SendTextMessageAsync(
                    chatId,
                    output.ToString(),
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await _botClient.SendTextMessageAsync(
                    chatId,
                    $"Error executing command: {ex.Message}",
                    cancellationToken: cancellationToken);
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }

    public class ServerCredentials
    {
        public string Host { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool AwaitingInput { get; set; }
        public InputStep InputStep { get; set; }
    }

    public enum InputStep
    {
        Host,
        Username,
        Password
    }
}
