using CeresDSP.CommandModules;
using CeresDSP.Services;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
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
        public DiscordClient Client { get; private set; }

        public InteractivityExtension Interactivity { get; private set; }

        public CommandsNextExtension Commands { get; private set; }

        public Configuration Configuration { get; init; }

        public FronterStatusService StatusService { get; private set; }


        public Ceres()
        {
            #region Get Configuration
            try
            {
                using FileStream configFS = File.OpenRead("config.json");
                using StreamReader configReader = new(configFS, new UTF8Encoding(true));
                Configuration = JsonConvert.DeserializeObject<Configuration>(configReader.ReadToEnd());
            }
            catch (NullReferenceException)
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
#if DEBUG
                MinimumLogLevel = LogLevel.Debug
#endif
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
            #endregion

            Client.MessageReactionAdded += OnReactionAdded;
        }

        private async Task OnReactionAdded(DiscordClient sender, MessageReactionAddEventArgs args)
        {
            if (args.User.Id == 233018119856062466) return;

            DiscordEmoji reactionEmote = args.Emoji;
            DiscordMessage reactedMsg = args.Message;
            DiscordGuild indicatorEmoteServer = await sender.GetGuildAsync(1034142544642183178);
            IReadOnlyDictionary<ulong, DiscordEmoji> indicatorEmotesArray = indicatorEmoteServer.Emojis;

            foreach (var indicatorEmote in indicatorEmotesArray)
            {
                if (reactionEmote.Id.Equals(indicatorEmote.Value.Id))
                    await reactedMsg.DeleteReactionAsync(reactionEmote, args.User);
            }
        }

        public async Task ConnectAsync()
        {
            await Client.ConnectAsync();
            await StatusService.TriggerStatusRefreshAsync();
        }
    }
}
