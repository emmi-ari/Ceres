using Ceres.Services;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Ceres
{
    internal class Bot
    {
        public IConfigurationRoot Configuration { get; }

        /// <summary>
        /// Workaround for an asynchronus entry point.
        /// </summary>
        private static Task Main() => new Bot().MainAsync();

        public Bot()
        {
            IConfigurationBuilder builder = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("ceres_config.json");
            Configuration = builder.Build();
        }

        /// <summary>
        /// Actual entry point of application
        /// </summary>
        private async Task MainAsync()
        {
            ServiceCollection services = new();
            ConfigureServices(services);
            var provider = services.BuildServiceProvider();
            provider.GetRequiredService<CommandHandler>();
            provider.GetRequiredService<FronterStatusService>();

            await provider.GetRequiredService<StartupService>().StartAsync();
            await Task.Delay(-1);
        }

        private void ConfigureServices(IServiceCollection services)
        {
#if DEBUG
            LogSeverity logSeverity = LogSeverity.Debug;
#else
            LogSeverity logSeverity = GetLogSeverity();
#endif
            LoggingService logger = new();
            DiscordSocketClient client = new(new DiscordSocketConfig
            {
                LogLevel = logSeverity,
                MessageCacheSize = 50,
                GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildEmojis | GatewayIntents.MessageContent | GatewayIntents.DirectMessages | GatewayIntents.AllUnprivileged
            });
            client.Log += logger.OnLogAsync;

            CommandService commands = new(new CommandServiceConfig
            {
                LogLevel = logSeverity,
                DefaultRunMode = RunMode.Async
            });
            commands.Log += logger.OnLogAsync;

            services.AddSingleton(client)
            .AddSingleton(commands)
            .AddSingleton<CommandHandler>()
            .AddSingleton<StartupService>()
            .AddSingleton<FronterStatusService>()
            .AddSingleton(Configuration);
        }

#if !DEBUG
        private LogSeverity GetLogSeverity()
        {
            return Configuration["ceres.log_level"].ToUpper() switch
            {
                "CRITICAL" => LogSeverity.Critical,
                "ERROR" => LogSeverity.Error,
                "WARNING" => LogSeverity.Warning,
                "INFO" => LogSeverity.Info,
                "DEBUG" => LogSeverity.Debug,
                _ => LogSeverity.Verbose,
            };
        }
#endif
    }
}