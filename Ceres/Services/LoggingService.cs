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
        private string LogFile => Path.Combine(LogDirectory, $"{DateTime.UtcNow:yyyy-MM-dd}.txt");

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
            if (!Directory.Exists(LogDirectory))     // Create the log directory if it doesn't exist
                Directory.CreateDirectory(LogDirectory);
            if (!File.Exists(LogFile))               // Create today's log file if it doesn't exist
                File.Create(LogFile).Dispose();

            string logText = $"{DateTime.UtcNow:s} [{msg.Severity}] [{msg.Source}]: {msg.Exception?.ToString() ?? msg.Message}";
            File.AppendAllText(LogFile, logText + "\n");     // Write the log text to a file

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
    }
}
