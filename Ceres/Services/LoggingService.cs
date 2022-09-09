﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Ceres.Services
{
    internal class LoggingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;

        private string LogDirectory { get; init; }
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

        internal LoggingService()
        {
            LogDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
        }

        internal Task OnLogAsync(LogMessage msg)
        {
            string logText = $"{DateTime.UtcNow:s} [{msg.Severity}] [{msg.Source}] {msg.Exception?.ToString() ?? msg.Message}";

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
                    Console.ForegroundColor = ConsoleColor.White;
                    return Console.Out.WriteLineAsync(logText);

                case LogSeverity.Debug:
                    Console.ForegroundColor = ConsoleColor.Green;
                    return Console.Out.WriteLineAsync(logText);

                case LogSeverity.Verbose:
                default:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    return Console.Out.WriteLineAsync(logText);
            }
        }
    }
}
