using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Ceres.Services
{
    internal class LoggingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly ConsoleColor _defaultConsoleFGColor = Console.ForegroundColor;

        private string LogDirectory { get; }
        //private string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");

        // DiscordSocketClient and CommandService are injected automatically from the IServiceProvider
        public LoggingService(DiscordSocketClient discord, CommandService commands)
        {
            LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

            _discord = discord;
            _commands = commands;

            _discord.Log += OnLogAsync;
            _commands.Log += OnLogAsync;
        }

        private Task OnLogAsync(LogMessage msg)
        {
            string logText = $"{DateTime.UtcNow:s} [{msg.Severity}] [{msg.Source}]: {msg.Exception?.ToString() ?? msg.Message}";

            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    return Console.Out.WriteLineAsync(logText);

                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    return Console.Out.WriteLineAsync(logText);

                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return Console.Out.WriteLineAsync(logText);

                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.Green;
                    return Console.Out.WriteLineAsync(logText);

                case LogSeverity.Verbose:
                default:
                    Console.ForegroundColor = _defaultConsoleFGColor;
                    return Console.Out.WriteLineAsync(logText);
            }
        }

        private string GetExceptionStringForLog(Exception exception)
        {
            if (exception == null)
                throw new ArgumentNullException(nameof(exception));

            List<string> messages = new()
            {
                $"{exception.Message} (0x{exception.HResult:X8})",
                string.IsNullOrWhiteSpace(exception.StackTrace) ? null : exception.StackTrace + Environment.NewLine
            };

            if (exception.InnerException != null)
            {
                Exception inner = exception.InnerException;
                while (inner != null)
                {
                    messages.Add($"{inner.Message} (0x{inner.HResult:X8})");
                    messages.Add(inner.StackTrace);
                    inner = inner.InnerException;
                }
            }

            string endErrorMessage = null;
            foreach (string line in messages)
            {
                endErrorMessage += line + Environment.NewLine;
            }

            endErrorMessage = endErrorMessage.Trim('\n');
            endErrorMessage = endErrorMessage.Trim('\r');
            return endErrorMessage ?? string.Empty;
        }
    }
}
