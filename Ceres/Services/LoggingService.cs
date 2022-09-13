﻿using Discord;
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

        internal LoggingService()
        {
            LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        }

        internal async Task OnLogAsync(LogMessage msg)
        {
            string logText = $"{DateTime.UtcNow:s} [{msg.Severity}] [{msg.Source}] {msg.Exception?.ToString() ?? msg.Message}";
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
            catch (Exception ex)
            {
                if (!secondTry)
                {
                    await Task.Delay(500);
                    await LogToFile(logText, true);
                }
                await LogToConsole(LogSeverity.Error, $"Can't access the log file.\n{GetExceptionStringForLog(ex)}");
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
                    await Console.Out.WriteLineAsync(logText);
                    Console.ForegroundColor = defaultForegroundColor;
                    Console.BackgroundColor = defaultBackgroundColor;
                    return;

                case LogSeverity.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    await Console.Out.WriteLineAsync(logText);
                    Console.ForegroundColor = defaultForegroundColor;
                    return;

                case LogSeverity.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    await Console.Out.WriteLineAsync(logText);
                    Console.ForegroundColor = defaultForegroundColor;
                    return;

                case LogSeverity.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    await Console.Out.WriteLineAsync(logText);
                    Console.ForegroundColor = defaultForegroundColor;
                    return;

                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.Green;
                    await Console.Out.WriteLineAsync(logText);
                    Console.ForegroundColor = defaultForegroundColor;
                    return;

                case LogSeverity.Verbose:
                default:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    await Console.Out.WriteLineAsync(logText);
                    Console.ForegroundColor = defaultForegroundColor;
                    return;
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

            endErrorMessage += $"{new('=', (Console.WindowWidth / 2) - 9)} End of exception {new('=', (Console.WindowWidth / 2) - 9)}";
            endErrorMessage += $"Exception was of type {exception.GetType()}";
            endErrorMessage += $"{new('=', Console.WindowWidth)}";

            return endErrorMessage;
        }
    }
#pragma warning restore CA1822
}
