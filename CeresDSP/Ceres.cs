using CeresDSP.CommandModules;
using CeresDSP.Models;
using CeresDSP.Services;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using System.Diagnostics;
using System.Text;

namespace CeresDSP
{
    public class Ceres
    {
        public DiscordClient Client { get; init; }

        public InteractivityExtension Interactivity { get; init; }

        public CommandsNextExtension Commands { get; init; }

        public Configuration Configuration { get; init; }

        public FronterStatusService StatusService { get; init; }


        public Ceres()
        {
            #region Get Configuration
            try
            {
                using FileStream configFs = File.OpenRead("config.json");
                using StreamReader configReader = new(configFs, new UTF8Encoding(true));
                Configuration = JsonConvert.DeserializeObject<Configuration>(configReader.ReadToEnd());
            }
            catch (FileNotFoundException)
            {
                string errorMsg = $"\"config.json\" was not found. Please put it in the current working directory ({Environment.CurrentDirectory})";
                Console.WriteLine(errorMsg);
                Debug.WriteLine(errorMsg);
                Environment.Exit(-1);
            }
            #endregion

            #region Build Client
            Client = new(new DiscordConfiguration()
            {
                Token = Configuration.Ceres.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                Intents = (DiscordIntents)0x1FFFF,
                MinimumLogLevel = GetLogLevel()
            });

            Client.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromMinutes(1)
            });
            #endregion

            #region Configure Commands
            StatusService = new(Client, Configuration);
            ServiceCollection services = new();
            ServiceProvider srvProvider = services
                .AddSingleton(StatusService)
                .AddSingleton(Configuration)
                .BuildServiceProvider();

            CommandsNextConfiguration cmdConfig = new()
            {
                StringPrefixes = new string[1] { Configuration.Ceres.Prefix },
                EnableMentionPrefix = false,
                EnableDms = true,
                CaseSensitive = false,
                EnableDefaultHelp = false,
                Services = srvProvider
            };

            Commands = Client.UseCommandsNext(cmdConfig);
            Commands.RegisterCommands<FrontingCommands>();
            Commands.RegisterCommands<WeatherCommands>();
            Commands.RegisterCommands<OwnerCommands>();
            Commands.RegisterCommands<MiscellaneousCommands>();

            SlashCommandsExtension slashCommands = Client.UseSlashCommands(new()
            {
                Services = srvProvider
            });
            slashCommands.RegisterCommands<FrontingCommandsSlash>();
            slashCommands.RegisterCommands<WeatherCommandsSlash>();
            slashCommands.RegisterCommands<MiscellaneousCommandsSlash>();
            slashCommands.RegisterCommands<TonalIndicatorCommands>();
            #endregion

            #region Event Handling
            Client.MessageReactionAdded += ClientEventsHandlerService.OnReactionAdded;
            Client.MessageCreated += ClientEventsHandlerService.OnMessageCreated;
            Client.ThreadCreated += ClientEventsHandlerService.OnThreadCreated;
            #endregion
        }

        public async Task ConnectAsync()
        {
            await Client.ConnectAsync();
            await StatusService.TriggerStatusRefreshAsync();
        }

        private LogLevel GetLogLevel()
        {
            return Configuration.Ceres.LogLevel.ToUpper() switch
            {
                "CRITICAL" => LogLevel.Critical,
                "ERROR" => LogLevel.Error,
                "WARNING" => LogLevel.Warning,
                "DEBUG" => LogLevel.Debug,
                "TRACE" => LogLevel.Trace,
                "INFORMATION" or "INFO" or _ => LogLevel.Information
            };
        }
    }
}
