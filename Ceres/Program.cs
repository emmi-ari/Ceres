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
            IConfigurationBuilder? builder = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("ceres_config.json");
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
            provider.GetRequiredService<LoggingService>();
            provider.GetRequiredService<CommandHandler>();
            provider.GetRequiredService<FronterStatusService>();

            await provider.GetRequiredService<StartupService>().StartAsync();
            //var timer = new Timer(StatusTimer, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
            await Task.Delay(-1);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            GatewayIntents customIntents = (GatewayIntents)0x7EBD;
            services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
            {                                       // Add discord to the collection
                LogLevel = LogSeverity.Verbose,     // Tell the logger to give Verbose amount of info
                MessageCacheSize = 1000,            // Cache 1,000 messages per channel
                GatewayIntents = customIntents
            }))
            .AddSingleton(new CommandService(new CommandServiceConfig
            {                                       // Add the command service to the collection
                LogLevel = LogSeverity.Verbose,     // Tell the logger to give Verbose amount of info
                DefaultRunMode = RunMode.Async,     // Force all commands to run async by default
            }))
            .AddSingleton<CommandHandler>()         // Add the command handler to the collection
            .AddSingleton<StartupService>()         // Add startupservice to the collection
            .AddSingleton<LoggingService>()         // Add loggingservice to the collection
            .AddSingleton<Random>()                 // Add random to the collection
            .AddSingleton<FronterStatusService>()
            .AddSingleton(Configuration);           // Add the configuration to the collection
        }
    }
}