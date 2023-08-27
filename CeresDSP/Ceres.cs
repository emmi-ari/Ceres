using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

using Newtonsoft.Json;

using System.Text;
using System.Text.Json.Nodes;

namespace CeresDSP
{
    public class Ceres
    {
        public DiscordClient Client { get; private set; }

        public InteractivityExtension Interactivity { get; private set; }

        public CommandsNextExtension Commands { get; private set; }

        public Configuration Configuration { get; init; }


        public Ceres()
        {
            using FileStream configFS = File.OpenRead("config.json");
            using StreamReader configReader = new(configFS, new UTF8Encoding(false));
            
            Configuration = JsonConvert.DeserializeObject<Configuration>(configReader.ReadToEnd());
            Client = new(new DiscordConfiguration()
            {
                Token = Configuration.Token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                Intents = (DiscordIntents)0x1FFFF
            });

            Client.UseInteractivity(new()
            {
                Timeout = TimeSpan.FromMinutes(1)
            });

            CommandsNextConfiguration cmdConfig = new()
            {
                StringPrefixes = new string[1] { Configuration.Prefix },
                EnableMentionPrefix = false,
                EnableDms = true,
                CaseSensitive = false,
                EnableDefaultHelp = false
            };

            Commands = Client.UseCommandsNext(cmdConfig);
            Commands.RegisterCommands<MiscCommands>();
        }

        public async Task ConnectAsync()
        {
            await Client.ConnectAsync();
        }
    }
}
