using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;

namespace Ceres.Services
{
    public class CommandsModule : ModuleBase<SocketCommandContext>
    {
        [Command("updatefront")]
        [Alias("update", "ufront", "uf", "updatef")]
        [Summary("Updates the fronting status")]
        [RequireOwner(ErrorMessage = "Only runnable by the bot owner")]
        public Task UpdateFront()
        {
            return ReplyAsync("Front status update started");
        }

        [Command("egg")]
        [Alias("ei")]
        [Summary("egg (engl. \"Ei\"")]
        public Task Egg()
        {
            return Context.Channel.SendFileAsync(@"C:\Users\Emmi\Documents\ähm\ei.png");
        }

        [Command("say")]
        [Alias("echo", "print")]
        [Summary("Echoes a message.")]
        public Task SayAsync([Remainder][Summary("The text to echo")] string echo)
        {
            return ReplyAsync(echo);
        }

        // ~sample square 20 -> 400
        [Command("square")]
        [Summary("Squares a number.")]
        public async Task SquareAsync([Summary("The number to square.")] int num)
        {
            // We can also access the channel from the Command Context.
            await Context.Channel.SendMessageAsync($"{num}^2 = {Math.Pow(num, 2)}");
        }

        // ~sample userinfo --> foxbot#0282
        // ~sample userinfo @Khionu --> Khionu#8708
        // ~sample userinfo Khionu#8708 --> Khionu#8708
        // ~sample userinfo Khionu --> Khionu#8708
        // ~sample userinfo 96642168176807936 --> Khionu#8708
        // ~sample whois 96642168176807936 --> Khionu#8708
        [Command("userinfo")]
        [Summary("Returns info about the current user, or the user parameter, if one passed.")]
        [Alias("user", "whois")]
        public async Task UserInfoAsync([Summary("The (optional) user to get info from")] SocketUser user = null)
        {
            var userInfo = user ?? Context.Client.CurrentUser;
            await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator}");
        }
    }

    public class CommandHandler
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _provider;
        private readonly CommonFronterStatusMethods _fronterStatusMethods;

        public CommandHandler(DiscordSocketClient discord, CommandService commands, IConfigurationRoot config, IServiceProvider provider)
        {
            _discord = discord;
            _commands = commands;
            _config = config;
            _provider = provider;
            _fronterStatusMethods = new(discord, config);
            _discord.MessageReceived += OnMessageReceivedAsync;
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            if (s is not SocketUserMessage msg) return;
            if (msg.Author.Id == _discord.CurrentUser.Id) return;

            SocketCommandContext? context = new(_discord, msg);

            int argPos = 0;
            if (msg.HasStringPrefix(_config["prefix"], ref argPos) || msg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {
                var result = await _commands.ExecuteAsync(context, argPos, _provider);
                switch (msg.Content.Trim().Replace("!", string.Empty))
                {
                    case "update":
                    case "updatefront":
                    case "ufront":
                    case "updatef":
                    case "uf":
                        if (msg.Author.Id.ToString() == _config["botauthor_id"])
                            await _fronterStatusMethods.SetFronterStatusAsync();
                        break;
                }

                if (!result.IsSuccess)
                    await context.Channel.SendMessageAsync(result.ToString());
            }
        }
    }
}
