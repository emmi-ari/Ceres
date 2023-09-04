using CeresDSP.Services;
using CeresDSP.CommandModules;

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

using System.Text;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System.Diagnostics;

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
            using FileStream configFS = File.OpenRead("config.json");
            using StreamReader configReader = new(configFS, new UTF8Encoding(true));
            Configuration = JsonConvert.DeserializeObject<Configuration>(configReader.ReadToEnd());
            #endregion

            #region Build Client
            Client = new(new DiscordConfiguration()
            {
                Token = Configuration.Ceres.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                Intents = (DiscordIntents)0x1FFFF
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
            Commands.RegisterCommands<MiscellaneousCommands>();
            #endregion

            Client.MessageReactionAdded += OnReactionAdded;
        }

        private async Task OnReactionAdded(DiscordClient sender, MessageReactionAddEventArgs args)
        {
            if (args.User.Id == 233018119856062466) return;

            DiscordEmoji reactionEmote = args.Emoji;
            DiscordMessage reactedMsg = args.Message;
            DiscordGuild indicatorEmoteServer = await sender.GetGuildAsync(1034142544642183178);
            KeyValuePair<ulong, DiscordEmoji>[] indicatorEmotesArray = indicatorEmoteServer.Emojis.ToArray();

            Stopwatch stopwatch = new();
            stopwatch.Start();
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
