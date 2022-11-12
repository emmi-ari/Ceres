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
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;

        public StartupService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands, IConfigurationRoot config)
        {
            _provider = provider;
            _config = config;
            _discord = discord;
            _commands = commands;
            _discord.Ready += OnClientReady;
        }

        private async Task OnClientReady()
        {
            Dictionary<string, string> nameLocalization = new()
            {
                { "en-US", "emotetogif" },
                { "en-GB", "emotetogif" },
                { "de", "emotezugif" }
            };
            Dictionary<string, string> descriptionLocalization = new()
            {
                { "en-US", "Emote to gif" },
                { "en-GB", "Emote to gif" },
                { "de", "Emote zu gif" },
            };
            MessageCommandBuilder messageCommand = new()
            {
                Name = "emotetogif",
                IsDefaultPermission = true,
                IsDMEnabled = true,
                DefaultMemberPermissions = GuildPermission.SendMessages,
            };
            MessageCommandProperties[] messageCommandProperties = { messageCommand.WithNameLocalizations(nameLocalization).Build()};
            messageCommandProperties[0].DescriptionLocalizations = descriptionLocalization;
            messageCommandProperties[0].NameLocalizations = nameLocalization;

            await _discord.BulkOverwriteGlobalApplicationCommandsAsync(messageCommandProperties);
        }

        public async Task StartAsync()
        {
            string discordToken = _config["ceres.discord_token"];
            if (string.IsNullOrWhiteSpace(_config["ceres.discord_token"]))
                throw new Exception($"No discord token in ceres_config.json");

            await _discord.LoginAsync(TokenType.Bot, discordToken);
            await _discord.StartAsync();

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _provider); // Load commands and modules into the command service
        }
    }
}