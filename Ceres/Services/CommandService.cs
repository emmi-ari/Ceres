using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;

// (\#if )(DEBUG|RELEASE)
// $1!$2F

namespace Ceres.Services
{
    public class CommandsModule : ModuleBase<SocketCommandContext>
    {
        LoggingService _log;

        public CommandsModule()
        {
            _log = new LoggingService();
        }
        enum CeresCommand
        {
            UpdateFront,
            Egg,
            AddReaction,
            Say,
            WhoKnows
        }

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

        [Command("react")]
        [Summary("Adds a reaction to a message")]
        public Task AddReaction(string emote, string messageId = null)
        {
            IMessage msg = null;
            if (messageId != null)
            {
                bool channelValid = ulong.TryParse(messageId, out ulong messageIdUlong);
                msg = Task.Run(async () => { return await Context.Channel.GetMessageAsync(messageIdUlong); }).Result;
                if (msg == null)
                {
                    return CommandError(CeresCommand.AddReaction, "Error: **Command must be executed in the same channel**");
                }
                if (!channelValid)
                {
                    return CommandError(CeresCommand.AddReaction, "Error: **Something's wrong with the message ID**");
                }
            }
            else
            {
                if (Context.Message.Reference == null)
                    return CommandError(CeresCommand.AddReaction, "Error: **No message ID specified and not replied to any message**");

                msg = Task.Run(async () => { return await Context.Channel.GetMessageAsync((ulong)Context.Message.Reference.MessageId); }).Result;
            }

            dynamic reaction = null;
            Emote emoteReaction = null;
            Emoji emojiReaction = null;
            bool emoteValid = Emote.TryParse(emote, out emoteReaction);
            if (!emoteValid)
            {
                emojiReaction = new Emoji(emote);
                reaction = emojiReaction;
            }
            else
            {
                reaction = emoteReaction;
            }

            return msg.AddReactionAsync(reaction);
        }

        [Command("echo")]
        [Alias("say")]
        public Task Say(string msg, ulong channelId = 0ul, ulong guildId = 0ul, ulong replyToMsgID = 0ul)
        {
            if (guildId == 0)
                guildId = Context.Guild.Id;
            if (channelId == 0)
                channelId = Context.Channel.Id;

            SocketGuild guild = Context.Client.GetGuild(guildId);
            IMessageChannel channel = guild.GetChannel(channelId) as IMessageChannel;
            MessageReference reference = new(replyToMsgID, channelId, guildId, true);

            return (ulong)reference.MessageId == 0
                ? channel.SendMessageAsync(msg)
                : channel.SendMessageAsync(msg, messageReference: reference);
        }

        [Command("whoknows")]
        [Alias("wk")]
        public Task WhoKnows()
        {
            return ReplyAsync("```ANSI\n[0;31mDid you mean: [4;34m/whoknows```");
        }

        [Command("FrontHistory")]
        [Alias("history")]
        public Task FrontHistory()
        {
            return new Task(() => { });
        }

        private Task CommandError(CeresCommand command, string errorMsg)
        {
            switch (command)
            {
                case CeresCommand.AddReaction:
                    errorMsg += "\nUsage `!react [emote] [messageId]`";
                    errorMsg += "\nUsage `!react [emote]` when replying to a message. The replied to message will be used as reaction target.";
                    break;
            }

            return ReplyAsync(errorMsg);
        }

        protected override async Task BeforeExecuteAsync(CommandInfo command)
        {
            LogMessage log = new(LogSeverity.Info, command.Name.ToUpper(), $"{Context.User.Username}#{Context.User.Discriminator} used a command in #{Context.Channel.Name}");
            await _log.OnLogAsync(log);
            await base.BeforeExecuteAsync(command);
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
#if false
            _discord.ReactionAdded += OnReactionRecivedAsync;
            _discord.ThreadCreated += OnThreadCreated;
#endif
        }

        //private async Task OnThreadCreated(SocketThreadChannel arg)
        //{
        //    return;
        //}

        [Obsolete(message: "Functionality now in BLIMP", error: true)]
        private async Task OnReactionRecivedAsync(Cacheable<IUserMessage, ulong> reactedMsgUserContext, Cacheable<IMessageChannel, ulong> msgContext, SocketReaction reactionCtx)
        {
            if (reactionCtx.Emote.Name == "❌" && reactionCtx.Channel.Id == _channelDictionary["brett"])
            {
                ulong reactedMessageId = reactedMsgUserContext.Id;
                IMessage reactedMsg = await reactionCtx.Channel.GetMessageAsync(reactedMessageId);
                string brettOriginalMsgAuthor = reactedMsg.Embeds?.Select(embed => embed.Author.Value)
                                                                 ?.Where(author => author.Name.StartsWith(reactionCtx.User.Value.Username)) // Condition
                                                                 ?.FirstOrDefault().Name;

                if (string.IsNullOrEmpty(brettOriginalMsgAuthor))
                    return;

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

                IResult result = await _commands.ExecuteAsync(context, argPos, _provider);
                switch (commandWithoutPrefix)
                {
                    case "update":
                    case "updatefront":
                    case "ufront":
                    case "updatef":
                    case "uf":
                    case "u":
                        await _fronterStatusMethods.SetFronterStatusAsync();
                        break;
                    case "fronthistory":
                    case "history":
#if DEBUG
                        long startTime = ((DateTimeOffset)DateTime.Now.AddDays(-7)).ToUnixTimeMilliseconds();
                        long endTime = ((DateTimeOffset)DateTime.Now).ToUnixTimeMilliseconds();
                        string frontHistory = await _fronterStatusMethods.GetFrontHistory(startTime, endTime);
                        await context.Channel.SendMessageAsync(text: frontHistory);
#else
                        context.Channel.SendMessageAsync("Error: FrontHistory is not implemented yet or Ceres is not running in debug mode");
#endif
                        break;
                }

                if (!result.IsSuccess)
                    await context.Channel.SendMessageAsync(result.ToString());
            }
        }
    }
}
