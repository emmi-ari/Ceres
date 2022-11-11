using Ceres.Models.Apparyllis;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;

using System.Diagnostics;

// (\#if )(DEBUG|RELEASE)
// $1!$2F

namespace Ceres.Services
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _discord;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _provider;

        public CommandHandler(DiscordSocketClient discord, CommandService commands, IConfigurationRoot config, IServiceProvider provider)
        {
            _discord = discord;
            _commands = commands;
            _config = config;
            _provider = provider;
            _discord.MessageReceived += OnMessageReceivedAsync;
            _discord.ReactionAdded += OnReactionAdded;
        }

        private Task OnReactionAdded(Cacheable<IUserMessage, ulong> arg1, Cacheable<IMessageChannel, ulong> arg2, SocketReaction arg3)
        {
            #region Restrict others from using these reaction emotes
            if (arg3.Emote is not Emote || arg3.UserId == 233018119856062466) return Task.CompletedTask;

            Emote reactionEmote = (Emote)arg3.Emote;
            switch (reactionEmote.Id)
            {
                case 1034143711195582554:
                case 1034143897271676959:
                case 1034143902548107365:
                case 1034143907690332251:
                case 1034143824433401877:
                case 1034143757815271534:
                case 1034143913524600864:
                case 1034143781643108382:
                    IMessage reactedMessage = Task.Run(async () => { return await arg2.Value.GetMessageAsync(arg3.MessageId); }).Result;
                    return reactedMessage.RemoveReactionAsync(reactionEmote, arg3.UserId);

                default:
                    return Task.CompletedTask;
            }
            #endregion
        }

        private async Task OnMessageReceivedAsync(SocketMessage s)
        {
            if (s is not SocketUserMessage msg) return;
            if (msg.Author.Id == _discord.CurrentUser.Id) return;

            SocketCommandContext context = new(_discord, msg);
            string prefix = _config["ceres.prefix"];
            int prefixLength = 0;

            #region Reminder emphasizer
            if (s.Embeds != null || s.Embeds.Count != 0)
            {
                IReadOnlyCollection<Embed> msgEmbed = s.Embeds;
                string embedDescription = msgEmbed?.FirstOrDefault()?.Description;
                embedDescription ??= string.Empty;
                if (s.Author.Id == 526166150749618178 && embedDescription.Contains("Reminder from"))
                {
                    await context.Channel.SendMessageAsync("<a:DinkDonk:1025546103447355464>");
                }
            }
            #endregion

            if (msg.HasStringPrefix(prefix, ref prefixLength) || msg.HasMentionPrefix(_discord.CurrentUser, ref prefixLength))
            {
                string commandWithoutPrefix = msg.Content.Replace(prefix, string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(commandWithoutPrefix))
                    return;

                IResult result = _commands.ExecuteAsync(context, prefixLength, _provider).Result;

                if (!result.IsSuccess)
                    await context.Channel.SendMessageAsync(result.ToString());
            }
        }

        public class CommandsCollection : ModuleBase<SocketCommandContext>
        {
            private readonly DiscordSocketClient _discord;
            private readonly CommandService _commands;
            private readonly IConfigurationRoot _config;
            private readonly IServiceProvider _provider;
            private readonly CommonFronterStatusMethods _fronterStatusMethods;
            private readonly LoggingService _logger;
            private readonly DirectoryInfo _folderDir;
            private readonly Random _unsafeRng;
            private readonly Emoji _waitEmote = new("⏳");
            private readonly HttpClient _weatherStackApi;

            public CommandsCollection(DiscordSocketClient discord, CommandService commands, IConfigurationRoot config, IServiceProvider provider)
            {
                _discord = discord;
                _commands = commands;
                _config = config;
                _provider = provider;
                _fronterStatusMethods = new(discord, config);
                _logger = new();
                _folderDir = new(config["ceres.foldercommandpath"]);
                _unsafeRng = new();
                HttpClient weatherStackApi = new()
                {
                    BaseAddress = new("http://api.weatherstack.com/")
                };
                weatherStackApi.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                _weatherStackApi = weatherStackApi;
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
                _ = _fronterStatusMethods.SetFronterStatusAsync();
                return ReplyAsync("Front status updated");
            }

            [Command("egg")]
            [Alias("ei", "eckeaberaufhessisch")]
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
                    if (msg == null) return CommandError(CeresCommand.AddReaction, "Error: **Command must be executed in the same channel**");
                    if (!channelValid) return CommandError(CeresCommand.AddReaction, "Error: **Something's wrong with the message ID**");
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
            public Task Say(string msg, ulong channelId = 0ul, ulong guildId = 0ul, ulong replyToMsgID = 0ul, string delete = "")
            {
                #region Guild ID parsing
                if (guildId == 0ul)
                    guildId = Context.Guild.Id;
                SocketGuild guild = Context.Client.GetGuild(guildId);
                if (guild == null)
                    return Context.Channel.SendMessageAsync("Invalid Guild ID");
                #endregion

                #region Channel ID parsing
                if (channelId == 0ul)
                    channelId = Context.Channel.Id;
                if (guild.GetChannel(channelId) is not IMessageChannel messageChannel) // Null check
                    return Context.Channel.SendMessageAsync("Invalid Channel ID");
                #endregion

                #region Message ID parsing
                if (replyToMsgID != 0ul)
                {
                    IMessage replyMessage = Task.Run(async () => { return await messageChannel.GetMessageAsync(replyToMsgID); }).Result;
                    if (replyMessage == null)
                        return Context.Channel.SendMessageAsync("Invalid Message ID");
                    MessageReference reference = new(replyToMsgID, channelId, guildId, true);

                    return messageChannel.SendMessageAsync(msg, messageReference: reference);
                }
                #endregion

                return messageChannel.SendMessageAsync(msg);
            }

            [Command("whoknows")]
            [Alias("wk")]
            public Task WhoKnows()
            {
                return ReplyAsync("```ANSI\n[0;31mDid you mean: [4;34m/whoknows```");
            }

            [Command("folder")]
            [Alias("f")]
            public Task Folder()
            {
                FileInfo[] folderFiles = _folderDir.GetFiles()
                                                   .Where(file => file.Name != "ei.png" || !(file.Attributes.HasFlag(FileAttributes.System) || file.Attributes.HasFlag(FileAttributes.Directory)))
                                                   .ToArray();
                int rand = _unsafeRng.Next(0, folderFiles.Length);
                string filePath = folderFiles[rand].FullName;
                string text = filePath switch
                {
                    "redditsave.com_german_spongebob_is_kinda_weird-vrm48d21ch081.mp4"
                        => "CW Laut",
                    "brr_uzi.mp4"
                        => "CW Laut",
                    "Discord_become_hurensohn2.png"
                        => "Credits: Aurora",
                    _ // default
                        => string.Empty
                };

                return Context.Channel.SendFileAsync(filePath, text);
            }

            [Command("EmoteToGif")]
            [Alias("Emote", "Gif", "FuckNitro")]
            public Task EmoteToGif(string providedEmoteName)
            {
                IReadOnlyCollection<GuildEmote> serverEmotes = Context.Guild.Emotes;
                List<GuildEmote> matchedEmotes = new();

                foreach (GuildEmote emote in serverEmotes)
                {
                    if (emote.Name.ToLower().StartsWith(providedEmoteName.ToLower())) matchedEmotes.Add(emote);
                }

                if (matchedEmotes.Count > 1)
                {
                    string matchedEmotesNames = string.Empty;
                    matchedEmotes.ForEach(emote => matchedEmotesNames += $"{emote.Name}, ");
                    matchedEmotesNames = matchedEmotesNames.TrimEnd().TrimEnd(',');
                    return ReplyAsync($"Multiple emotes matches your input: `{matchedEmotesNames}`");
                }
                else if (matchedEmotes.Count == 0)
                    return ReplyAsync($"No emotes found that match {providedEmoteName}");

                string emoteUrl = matchedEmotes[0].Url;
                string emoteName = matchedEmotes[0].Name;
                bool emoteIsAnimated = matchedEmotes[0].Animated;

                // Download emote
                using HttpClient client = new();
                using Stream stream = Task.Run(async () => { return await client.GetStreamAsync(emoteUrl); }).Result;

                if (emoteIsAnimated)
                {
                    using Stream file = File.Create($"{emoteName}.gif");
                    stream.CopyTo(file);
                    stream.Close();
                    file.Close();
                }
                else
                {
                    using Process ffmpeg = new();
                    ffmpeg.StartInfo.FileName = "ffmpeg";
                    ffmpeg.StartInfo.Arguments = @$"-i {emoteUrl} -vf palettegen=reserve_transparent=1 palette.png"; // Create pallet
                    ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    ffmpeg.Start();
                    ffmpeg.WaitForExit();

                    ffmpeg.StartInfo.Arguments = @$"-i {emoteUrl} -i .\palette.png -lavfi paletteuse=alpha_threshold=64 -gifflags -offsetting {emoteName}.gif"; // Actual gif conversion
                    ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    ffmpeg.Start();
                    ffmpeg.WaitForExit();

                    ffmpeg.StartInfo.Arguments = @$"-i .\{emoteName}.gif -vf scale=-1:48 -lavfi paletteuse=alpha_threshold=64 -gifflags -offsetting {emoteName}.gif"; // Actual gif conversion
                    ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    ffmpeg.Start();
                    ffmpeg.WaitForExit();
                    ffmpeg.Close();
                }

                return Context.Channel.SendFileAsync($"{emoteName}.gif");
            }

            [Command("weather")]
            [Alias("wetter", "w")]
            public Task Weather(string place, int forecastDays = 0)
            {
                return ReplyAsync("Not implemented.");
                if (string.IsNullOrEmpty(place))
                    throw new ArgumentException($"'{nameof(place)}' cannot be null or empty.", nameof(place));

                HttpResponseMessage response = Task.Run(async () =>
                {
                    return await _weatherStackApi.GetAsync($"forecast?access_key={_config["weatherstack.token"]}&query={place}");
                }).Result;
                string strResponse = Task.Run(async () => { return await response.Content.ReadAsStringAsync(); }).Result;

                WeatherStackModel serializedResponse = JsonConvert.DeserializeObject<WeatherStackModel>(strResponse);

                return Task.CompletedTask;
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
                await Context.Message.AddReactionAsync(_waitEmote);

                LogMessage log = new(LogSeverity.Info, command.Name, $"{Context.User.Username}#{Context.User.Discriminator} used a command in #{Context.Channel.Name}");
                await _logger.OnLogAsync(log);

                await base.BeforeExecuteAsync(command);
            }

            protected override async Task AfterExecuteAsync(CommandInfo command)
            {
                if (command.Name == "EmoteToGif")
                {
                    foreach (string sFile in Directory.GetFiles(AppContext.BaseDirectory, "*.gif"))
                    {
                        File.Delete(sFile);
                    }
                    File.Delete("palette.png");
                }
                await Context.Message.RemoveReactionAsync(_waitEmote, 966325392707301416);
                await base.AfterExecuteAsync(command);
            }
        }
    }
}
