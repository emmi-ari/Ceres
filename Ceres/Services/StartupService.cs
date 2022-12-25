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
            //SlashCommandBuilder slashCommand = new();
            //slashCommand.WithName("weather");
            //slashCommand.WithDescription("Weather");
            //slashCommand.WithDescriptionLocalizations(new Dictionary<string, string>(0));
            //slashCommand.WithDefaultPermission(true);
            //slashCommand.WithNameLocalizations(new Dictionary<string, string>(0));
            //SlashCommandProperties slashCommandBuilder = slashCommand.Build();

            MessageCommandBuilder messageCommand = new()
            {
                Name = "Emote to GIF (in DMs)",
                IsDefaultPermission = true,
                IsDMEnabled = true,
                DefaultMemberPermissions = GuildPermission.SendMessages
            };

            MessageCommandProperties[] messageCommandProperties = { messageCommand.Build() };
            messageCommandProperties[0].NameLocalizations = new Dictionary<string, string>(0);

            await _discord.BulkOverwriteGlobalApplicationCommandsAsync(messageCommandProperties);

            //foreach (var guild in _discord.Guilds)
            //{
            //    await guild.CreateApplicationCommandAsync(slashCommandBuilder);
            //}
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