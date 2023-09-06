using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;

using System.Reflection;

namespace Ceres.Services
{
    public class StartupService
    {
        private readonly IServiceProvider _provider;
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;

        public StartupService(IServiceProvider provider, DiscordSocketClient client, CommandService commands, IConfigurationRoot config)
        {
            _provider = provider;
            _config = config;
            _client = client;
            _commands = commands;
            _client.Ready += OnClientReady;
        }

        private async Task OnClientReady()
        {
            SlashCommandBuilder addEmoteCommand = new();
            addEmoteCommand.WithName("addemote");
            addEmoteCommand.WithDescription("Add Emote");
            addEmoteCommand.WithDescriptionLocalizations(new Dictionary<string, string>(0));
            addEmoteCommand.WithDefaultPermission(true);
            addEmoteCommand.WithNameLocalizations(new Dictionary<string, string>(0));
            addEmoteCommand.WithDefaultMemberPermissions(GuildPermission.ManageEmojisAndStickers);
            SlashCommandProperties addEmoteCommandBuilder = addEmoteCommand.Build();

            List<ApplicationCommandOptionProperties> slashCommandOptions = new(3);
            ApplicationCommandOptionProperties emoteName = new() { Name = "emotename", Description = "Emote name", Type = ApplicationCommandOptionType.String, IsRequired = true };
            ApplicationCommandOptionProperties emoteEntity = new() { Name = "emoteurl", Description = "Entity", Type = ApplicationCommandOptionType.String, IsRequired = false };
            ApplicationCommandOptionProperties emoteUrl = new() { Name = "emoteattachment", Description = "Emote attachment", Type = ApplicationCommandOptionType.Attachment, IsRequired = false};
            slashCommandOptions.Add(emoteName);
            slashCommandOptions.Add(emoteEntity);
            slashCommandOptions.Add(emoteUrl);
            addEmoteCommandBuilder.Options = slashCommandOptions;

            MessageCommandBuilder messageCommand = new()
            {
                Name = "Emote to GIF (in DMs)",
                IsDefaultPermission = true,
                IsDMEnabled = true,
                DefaultMemberPermissions = GuildPermission.SendMessages
            };

            MessageCommandProperties[] messageCommandProperties = { messageCommand.Build() };
            messageCommandProperties[0].NameLocalizations = new Dictionary<string, string>(0);

            await _client.BulkOverwriteGlobalApplicationCommandsAsync(messageCommandProperties);

            foreach (var guild in _client.Guilds)
            {
                await guild.CreateApplicationCommandAsync(addEmoteCommandBuilder);
            }
        }

        [Obsolete("This codebase for Ceres is obsolete. Please use CeresDSP instead.", false)]
        public async Task StartAsync()
        {
            string discordToken = _config["ceres.discord_token"];
            if (string.IsNullOrWhiteSpace(_config["ceres.discord_token"]))
                throw new Exception($"No discord token in ceres_config.json");

            await _client.LoginAsync(TokenType.Bot, discordToken);
            await _client.StartAsync();

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider); // Load commands and modules into the command service

            ConsoleColor fg = Console.ForegroundColor;
            ConsoleColor bg = Console.BackgroundColor;
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Red;
            await Console.Out.WriteLineAsync("THIS BOT IS OBSOLETE. PLEASE USE CeresDSP INSTEAD");
            Console.ForegroundColor = fg;
            Console.BackgroundColor = bg;
        }
    }
}