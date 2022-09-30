using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Ceres.Services
{
#pragma warning disable CA1822
    internal class LoggingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;

        private string LogDirectory { get; init; }
        private readonly string _logFilePath;

        internal LoggingService()
        {
            LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
            _logFilePath = Path.Combine(LogDirectory, "ceres.log");
            InitializeLogFile();
        }

        internal LoggingService(DiscordSocketClient discord, CommandService commands)
        {
            LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

            _discord = discord;
            _commands = commands;
            _logFilePath = Path.Combine(LogDirectory, "ceres.log");

            _discord.Log += OnLogAsync;
            _commands.Log += OnLogAsync;

            InitializeLogFile();
        }

        private void InitializeLogFile()
        {
            string logFile = Path.Combine(LogDirectory, "ceres.log");
            if (File.Exists(logFile))
            {
                FileInfo logFileInfo = new(logFile);
                if (logFileInfo.Length > 1024 * 1024) logFileInfo.MoveTo($"{DateTime.Now:uMM}_ceres.log");
            }
        }

        internal async Task OnLogAsync(LogMessage msg)
        {
            string logString = msg.Message;
            if (msg.Exception != null)
                logString = GetExceptionStringForLog(msg.Exception);

#if DEBUG
            string logText = $"{DateTime.UtcNow:s} DBG [{msg.Severity}] [{msg.Source}] {logString}";
#else
            string logText = $"{DateTime.UtcNow:s} [{msg.Severity}] [{msg.Source}] {logString}";
#endif
            await LogToFile(logText);
            await LogToConsole(msg.Severity, logText);
        }

        private async Task LogToFile(string logText, bool secondTry = false)
        {
            try
            {
                using FileStream file = new(_logFilePath, FileMode.Append, FileAccess.Write);
                using StreamWriter sw = new(file, System.Text.Encoding.UTF8);
                await sw.WriteAsync(logText + Environment.NewLine);
            }
            catch (Exception)
            {
                if (!secondTry)
                {
                    await Task.Delay(500);
                    await LogToFile(logText, true);
                }
                if (secondTry)
                    await LogToConsole(LogSeverity.Error, $"Couldn't access the log file.");
            }
        }

        private async Task LogToConsole(LogSeverity severity, string logText)
        {
            ConsoleColor defaultForegroundColor = Console.ForegroundColor;
            ConsoleColor defaultBackgroundColor = Console.BackgroundColor;

            switch (severity)
            {
                case LogSeverity.Critical:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.BackgroundColor = ConsoleColor.White;
#if WINDOWS
                    Console.Beep(2500, 150);
                    Console.Beep(2000, 100);
                    Console.Beep(2500, 200);
                    Console.Beep(2000, 150);
                    Console.Beep(2500, 250);
                    Console.Beep(2000, 200);
#else
                    Console.Beep();
#endif
                    await Console.Out.WriteLineAsync(logText);
                    break;

                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    goto default;                                   // ►──────┐
                                                                    //        │
                case LogSeverity.Warning:                           //        │
                    Console.ForegroundColor = ConsoleColor.Yellow;  //        │
                    goto default;                                   // ►─────┐│
                                                                    //       ││
                case LogSeverity.Info:                              //       ││
                    Console.ForegroundColor = ConsoleColor.White;   //       ││
                    goto default;                                   // ►────┐││
                                                                    //      |││
                case LogSeverity.Debug:                             //      |││
                    Console.ForegroundColor = ConsoleColor.Green;   //      |││
                    goto default;                                   // ►───┐│││
                                                                    //     |│││
                case LogSeverity.Verbose:                           //     |│││
                    Console.ForegroundColor = ConsoleColor.DarkGray;//     |│││
                    goto default;                                   // ►──┐││││
                                                                    //    │││││
                default:                                            //◄───┴┴┴┴┘
                    await Console.Out.WriteLineAsync(logText);
                    Console.ForegroundColor = defaultForegroundColor;
                    break;
            }

            Console.ForegroundColor = defaultForegroundColor;
            Console.BackgroundColor = defaultBackgroundColor;
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

            endErrorMessage += $"{new('=', (Console.WindowWidth / 2) - 9)} End of exception {new('=', (Console.WindowWidth / 2) - 10)}" + Environment.NewLine;
            endErrorMessage += $"Exception was of type {exception.GetType()}" + Environment.NewLine;
            endErrorMessage += $"{new('=', Console.WindowWidth - 1)}";

            return endErrorMessage;
        }
    }
#pragma warning restore CA1822
}
