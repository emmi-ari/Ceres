using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;

namespace Ceres.Services
{
    public class CommandsModule : ModuleBase<SocketCommandContext>
    {
        [Command("updatefront")]
        [Alias("u", "update", "ufront", "uf", "updatef")]
        [Summary("Updates the fronting status")]
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
        private readonly LoggingService _logger;

        private readonly Dictionary<string, ulong> _channelDictionary = new() { { "brett", 1014569711754813620 },
                                                                                { "bot-spam", 1014562565902307359 } };

        public CommandHandler(DiscordSocketClient discord, CommandService commands, IConfigurationRoot config, IServiceProvider provider)
        {
            _discord = discord;
            _commands = commands;
            _config = config;
            _provider = provider;
            _fronterStatusMethods = new(discord, config);
            _logger = new();
            _discord.MessageReceived += OnMessageReceivedAsync;
            _discord.ReactionAdded += OnReactionRecivedAsync;
        }

        private async Task OnReactionRecivedAsync(Cacheable<IUserMessage, ulong> reactedMsgUserContext, Cacheable<IMessageChannel, ulong> msgContext, SocketReaction reactionCtx)
        {
            ulong reactedMessageId = reactedMsgUserContext.Id;
            IMessage reactedMsg = await reactionCtx.Channel.GetMessageAsync(reactedMessageId);
            bool isDeleteReaction = reactionCtx.Emote.Name == "❌";
            string brettOriginalMsgAuthor = reactedMsg.Embeds.Select(embed => embed.Author.Value)
                                                             .Where(author => author.Name.StartsWith(reactionCtx.User.Value.Username)) // Condition
                                                             .FirstOrDefault().Name;

            if (string.IsNullOrEmpty(brettOriginalMsgAuthor))
                return;
            else if (reactionCtx.Channel.Id == _channelDictionary["brett"] && isDeleteReaction)
            {
                string logMsg = $"{reactionCtx.User.Value.Username}#{reactionCtx.User.Value.Discriminator} deleted one of their messages from #{reactionCtx.Channel.Name} (msg ID {reactedMsgUserContext.Id})";
                LogMessage log = new(LogSeverity.Info, nameof(this.OnReactionRecivedAsync), logMsg);
                await _logger.OnLogAsync(log);
                await reactedMsg.DeleteAsync();
            }
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            if (s is not SocketUserMessage msg) return;
            if (msg.Author.Id == _discord.CurrentUser.Id) return;

            SocketCommandContext context = new(_discord, msg);

            int argPos = 0;
            if (msg.HasStringPrefix(_config["ceres.prefix"], ref argPos) || msg.HasMentionPrefix(_discord.CurrentUser, ref argPos))
            {
                string commandWithoutPrefix = msg.Content.Replace(_config["ceres.prefix"], string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(commandWithoutPrefix))
                    return;

                var result = await _commands.ExecuteAsync(context, argPos, _provider);
                switch (commandWithoutPrefix)
                {
                    case "update":
                    case "updatefront":
                    case "ufront":
                    case "updatef":
                    case "uf":
                        await _fronterStatusMethods.SetFronterStatusAsync();
                        break;
                }

                if (!result.IsSuccess)
                    await context.Channel.SendMessageAsync(result.ToString());
            }
        }
    }
}
