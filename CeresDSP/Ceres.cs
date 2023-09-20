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
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

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
            #endregion

            // TODO create class with all client event handlers instead of having those in this class
            Client.MessageReactionAdded += OnReactionAdded;
            Client.MessageCreated += OnMessageCreated;
        }

        private async Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs args)
        {
            DiscordMessage message = args.Message;
            string messageContent = message.Content;
            Regex regex = new(@"\bhttps:\/\/spotify\.link\/[a-zA-Z0-9]{11}\b", RegexOptions.Multiline);
            Match[] matches = regex.Matches(messageContent).ToArray();
            List<string> normalLink = new(matches.Length);

            for (int i = 0; i < matches.Length; i++)
            {
                string link = matches[i].Value;
                normalLink.Add(await GetRedirectUrl(link));
            }

            try
            {
                await message.RespondAsync(string.Join(' ', normalLink));
            }
            catch (ArgumentException ex)
            {
                if (ex.Message == "Message content must not be empty.") return;
                else throw;
            }
        }

        private static async Task<string> GetRedirectUrl(string link)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/117.0.0.0 Safari/537.36");
            HttpResponseMessage response = await client.SendAsync(new(HttpMethod.Get, link));
            string responseContent = await response.Content.ReadAsStringAsync();
            Match[] matches = Regex.Matches(responseContent, @"<meta property=""og:url"" content=""(https:\/\/open\.spotify\.com\/\w+\/\w+)""\/>", RegexOptions.Singleline).ToArray();

            return matches.Length > 0 
                ? matches[0].Groups[1].Value
                : null;
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
