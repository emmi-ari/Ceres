using Ceres.Models.Apparyllis;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

// (\#if )(DEBUG|RELEASE)
// $1!$2

namespace Ceres.Services
{
    public class CommandHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly CommandService _commands;
        private readonly IConfigurationRoot _config;
        private readonly IServiceProvider _provider;

        public CommandHandler(DiscordSocketClient client, CommandService commands, IConfigurationRoot config, IServiceProvider provider)
        {
            _client = client;
            _commands = commands;
            _config = config;
            _provider = provider;
            _client.MessageReceived += OnMessageReceivedAsync;
            _client.ReactionAdded += OnReactionAdded;
            _client.MessageCommandExecuted += OnMessageCommandAsync;
            _client.SlashCommandExecuted += OnSlashCommandAsync;
            _client.ThreadCreated += OnThreadCreatedAsync;
        }

        private async Task OnThreadCreatedAsync(SocketThreadChannel thread)
        {
            string parentChannelMention = ((SocketTextChannel)thread.ParentChannel).Mention;
            string threadChannelMention = thread.Mention;
            SocketTextChannel threadsChannel = (SocketTextChannel)thread.Guild.GetChannel(1053712925891772467);

            if (!thread.IsPrivateThread)
                await threadsChannel.SendMessageAsync($"{threadChannelMention} - {parentChannelMention}");
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
                case 1039099437357735936:
                    IMessage reactedMessage = WaitFor(arg2.Value.GetMessageAsync(arg3.MessageId));
                    return reactedMessage.RemoveReactionAsync(reactionEmote, arg3.UserId);

                default:
                    return Task.CompletedTask;
            }
            #endregion
        }

        private async Task OnMessageReceivedAsync(SocketMessage message)
        {
            if (message is not SocketUserMessage msg) return;
            if (msg.Author.Id == _client.CurrentUser.Id) return;

            SocketCommandContext context = new(_client, msg);
            string prefix = _config["ceres.prefix"];
            int prefixLength = 0;

            #region Reminder emphasizer
            if (msg.Embeds != null || msg.Embeds.Count != 0)
            {
                IReadOnlyCollection<Embed> msgEmbed = msg.Embeds;
                string embedDescription = msgEmbed?.FirstOrDefault()?.Description;
                embedDescription ??= string.Empty;
                if (msg.Author.Id == 526166150749618178 && embedDescription.Contains("Reminder from"))
                {
                    await context.Channel.SendMessageAsync("<a:DinkDonk:1025546103447355464>");
                    return;
                }
            }
            #endregion

            if (msg.HasStringPrefix(prefix, ref prefixLength) || msg.HasMentionPrefix(_client.CurrentUser, ref prefixLength))
            {
                string commandWithoutPrefix = msg.Content.Replace(prefix, string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(commandWithoutPrefix))
                    return;

                IResult result = _commands.ExecuteAsync(context, prefixLength, _provider).Result;

                if (!result.IsSuccess)
                    await context.Channel.SendMessageAsync(result.ToString());
            }
        }

        private async Task OnMessageCommandAsync(SocketMessageCommand command)
        {
            await command.DeferAsync();
            SocketCommandContext context = new(_client, (SocketUserMessage)command.Data.Message);
            CommandInfo emoteToGif = _commands.Commands.Where(cmd => cmd.Name == "EmoteToGif").ToList()[0];

            IResult result = await _commands.ExecuteAsync(context, "EmoteToGif", _provider);
            //IResult actRes = await emoteToGif.ExecuteAsync(context, (ParseResult)result, _provider);
            if (!result.IsSuccess)
                await context.Channel.SendMessageAsync(result.ToString());
            await command.DeleteOriginalResponseAsync();
        }

        private async Task OnSlashCommandAsync(SocketSlashCommand arg)
        {
            await arg.DeferAsync();
            SocketSlashCommandDataOption[] commandArgumentsArray = arg.Data.Options.ToArray();
            SocketGuild guild = _client.GetGuild((ulong)arg.GuildId);

            switch (arg.CommandName)
            {
                case "addemote":
                    string emoteName = (string)commandArgumentsArray[0].Value;
                    GuildEmote emote = null;
                    if (commandArgumentsArray.Length <= 1) await arg.FollowupAsync("No emote source given");
                    if (commandArgumentsArray.Length == 3) await arg.FollowupAsync("Too many emote source given");
                    try
                    {
                        if (commandArgumentsArray[1].Value is Attachment attachment)
                            CommandsCollection.AddEmote(guild, out emote, emoteName, attachment: attachment);
                        else
                            try { CommandsCollection.AddEmote(guild, out emote, emoteName, (string)commandArgumentsArray[1].Value); }
                            catch (UriFormatException) { await arg.FollowupAsync("You didn't provide a valid URL for EmoteUrl"); }
                    }
                    catch (Exception ex)
                    {
                        await arg.FollowupAsync(ex.Message);
                    }

                    string emoteInMessage = string.Empty;
                    if (emote.Url.EndsWith("gif"))
                        emoteInMessage = $"<a:{emote.Name}:{emote.Id}>";
                    else
                        emoteInMessage = $"<:{emote.Name}:{emote.Id}>";

                    await arg.FollowupAsync($"{emoteInMessage} Emote {emoteName} added successfully");
                    return;
            }
        }

        internal static A WaitFor<A>(Task<A> task)
            => Task.Run(async () => { return await task; }).Result;

        public class CommandsCollection : ModuleBase<SocketCommandContext>
        {
            #region Non Static
            private readonly DiscordSocketClient _client;
            private readonly CommandService _commands;
            private readonly IConfigurationRoot _config;
            private readonly IServiceProvider _provider;
            private readonly CommonFronterStatusMethods _fronterStatusMethods;
            private readonly LoggingService _logger;
            private readonly DirectoryInfo _folderDir;
            private readonly Random _unsafeRng;
            private readonly Emoji _waitEmote = new("⏳");
            private readonly HttpClient _weatherStackApi;

            public CommandsCollection(DiscordSocketClient client, CommandService commands, IConfigurationRoot config, IServiceProvider provider)
            {
                _client = client;
                _commands = commands;
                _config = config;
                _provider = provider;
                _fronterStatusMethods = new(client, config);
                _logger = new();
                _folderDir = new(config["ceres.foldercommandpath"]);
                _unsafeRng = new();
                HttpClient weatherStackApi = new()
                {
                    BaseAddress = new("http://api.weatherstack.com/"),
                    Timeout = new(0, 0, 10)
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

            [Command("ToggleStatus")]
            [Alias("toggle")]
            [Summary("Enables or disables Ceres' status message")]
            public Task ToggleStatus()
            {
                bool isStatusSet = !string.IsNullOrEmpty(_client.CurrentUser.Activities.FirstOrDefault()?.Name);
                if (isStatusSet)
                    _client.SetActivityAsync(null);
                else
                    _ = _fronterStatusMethods.SetFronterStatusAsync();

                return ReplyAsync((isStatusSet ? "Dis" : "En") + "abled status");
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
                    msg = WaitFor(Context.Channel.GetMessageAsync(messageIdUlong));
                    if (msg == null) return CommandError(CeresCommand.AddReaction, "Error: **Command must be executed in the same channel**");
                    if (!channelValid) return CommandError(CeresCommand.AddReaction, "Error: **Something's wrong with the message ID**");
                }
                else
                {
                    if (Context.Message.Reference == null)
                        return CommandError(CeresCommand.AddReaction, "Error: **No message ID specified and not replied to any message**");

                    msg = WaitFor(Context.Channel.GetMessageAsync((ulong)Context.Message.Reference.MessageId));
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
            public Task Say(string msg, string locationLink = "", ulong guildId = 0ul, ulong channelId = 0ul, ulong messageId = 0ul)
            {
                #region Location link parsing
                Match match = Regex.Match(locationLink, @"(\/\d{17,}){2,3}");
                if (match.Groups.Count >= 2)
                {
                    string[] ids = match.Groups[0].Value.Replace('/', ',').TrimStart(',').Split(',');
                    guildId = Convert.ToUInt64(ids[0]);
                    channelId = Convert.ToUInt64(ids[1]);
                    if (ids.Length == 3) messageId = Convert.ToUInt64(ids[2]);
                }
                else
                {
                    return Context.Channel.SendMessageAsync("Make sure it's a discord link with at least two (slash seperated) IDs");
                }

                #endregion

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
                if (messageId != 0ul)
                {
                    IMessage replyMessage = WaitFor(messageChannel.GetMessageAsync(messageId));
                    if (replyMessage == null)
                        return Context.Channel.SendMessageAsync("Invalid Message ID");
                    MessageReference reference = new(messageId, channelId, guildId, true);

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
            public Task EmoteToGif(string providedEmoteName = "")
            {
                static void ConvertEmoteToGif(string emoteUrl, string emoteName, bool emoteIsAnimated)
                {
                    // Download emote
                    HttpClient client = new();
                    Stream stream = WaitFor(client.GetStreamAsync(emoteUrl));
                    Stream file = File.Create($"{emoteName}.gif");
                    stream.CopyTo(file);
                    stream.Dispose();
                    file.Dispose();

                    if (!emoteIsAnimated)
                    {
                        using Process ffmpeg = new();
                        ffmpeg.StartInfo.FileName = "ffmpeg";
                        ffmpeg.StartInfo.Arguments = @$"-i {emoteName} -vf palettegen=reserve_transparent=1 palette.png"; // Create pallet
                        ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        ffmpeg.Start();
                        ffmpeg.WaitForExit();

                        ffmpeg.StartInfo.Arguments = @$"-i {emoteName} -i .\palette.png -lavfi paletteuse=alpha_threshold=64 -gifflags -offsetting {emoteName}.gif"; // Actual gif conversion
                        ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        ffmpeg.Start();
                        ffmpeg.WaitForExit();

                        ffmpeg.StartInfo.Arguments = @$"-i .\{emoteName}.gif -vf scale=-1:48 -lavfi paletteuse=alpha_threshold=64 -gifflags -offsetting {emoteName}.gif"; // Actual gif conversion
                        ffmpeg.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        ffmpeg.Start();
                        ffmpeg.WaitForExit();
                        ffmpeg.Dispose();
                    }
                }

                IDMChannel userDM = WaitFor(((SocketGuildUser)Context.Message.Author).CreateDMChannelAsync());

                if (providedEmoteName == string.Empty)
                {
                    IMessage msg = Context.Message.Reference is not null
                        ? WaitFor(Context.Channel.GetMessageAsync((ulong)Context.Message.Reference.MessageId))
                        : Context.Message;
                    ITag[] emotesInMessage = msg.Tags.Where(tag => tag.Type == TagType.Emoji).ToArray();
                    List<string> emoteNames = new(emotesInMessage.Length);
                    foreach (ITag tag in emotesInMessage)
                    {
                        Emote emote = tag.Value as Emote;
                        emoteNames.Add($"{emote.Name}.gif");
                        ConvertEmoteToGif(emote.Url, emote.Name, emote.Animated);
                    }

                    return Task.Run(async () =>
                    {
                        List<FileAttachment> attachments = new(emotesInMessage.Length);

                        foreach (string fileName in emoteNames)
                        {
                            FileAttachment attachment = new(fileName);
                            attachments.Add(attachment);
                        }

                        foreach (ITag tag in emotesInMessage)
                        {
                            try
                            {
                                await userDM.SendFilesAsync(attachments);
                            }
                            catch (Exception)
                            {
                                await Context.Channel.SendFilesAsync(attachments);
                            }
                        }
                    });
                }

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

                ConvertEmoteToGif(emoteUrl, emoteName, emoteIsAnimated);

                return Context.Channel.SendFileAsync($"{emoteName}.gif");
            }

            [Command("weather")]
            [Alias("wetter", "w")]
            public Task Weather(string place)
            {
                #region Local function(s)
                static List<EmbedFieldBuilder> GetEmbedFields(WeatherStackModel serializedResponse)
                {
                    List<EmbedFieldBuilder> returnValue = new(5);
                    EmbedFieldBuilder cloudCover = new()
                    {
                        Name = "Bedeckung",
                        Value = $"{serializedResponse.Current.Cloudcover} %"
                    };
                    EmbedFieldBuilder feelsLike = new()
                    {
                        Name = "Feels like",
                        Value = $"{serializedResponse.Current.Feelslike} °C"
                    };
                    EmbedFieldBuilder observationTime = new()
                    {
                        Name = "Uhrzeit",
                        Value = $"{serializedResponse.Current.ObservationTime}"
                    };
                    EmbedFieldBuilder precip = new()
                    {
                        Name = "Niederschlagswahrscheinlichkeit",
                        Value = $"{serializedResponse.Current.Precip} %"
                    };
                    EmbedFieldBuilder uvIndex = new()
                    {
                        Name = "UV Index",
                        Value = $"{ParseUvIndex(serializedResponse.Current.UvIndex)}"
                    };
                    returnValue.Add(feelsLike);
                    returnValue.Add(precip);
                    returnValue.Add(cloudCover);
                    returnValue.Add(uvIndex);
                    returnValue.Add(observationTime);
                    return returnValue;
                }

                static string ParseWindDirection(string abbreviation)
                {
                    if (abbreviation.Length > 3)
                        throw new ArgumentException($"{nameof(abbreviation)} can not contain less than 1 or more than 3 chars");

                    StringBuilder parsedWindDirection = new(4, 15);

                    for (int i = 0; i < abbreviation.Length; i++)
                    {
                        switch (abbreviation[i])
                        {
                            case 'N':
                                parsedWindDirection.Append("Nord-");
                                break;
                            case 'E':
                                parsedWindDirection.Append("Ost-");
                                break;
                            case 'S':
                                parsedWindDirection.Append("Süd-");
                                break;
                            case 'W':
                                parsedWindDirection.Append("West-");
                                break;
                        }
                    }
                    return parsedWindDirection.ToString().TrimEnd('-');
                }

                static string ParseUvIndex(int uvIndex)
                {
                    switch (uvIndex)
                    {
                        case 1:
                        case 2:
                            return $"{uvIndex} (Niedrig)";

                        case 3:
                        case 4:
                        case 5:
                            return $"{uvIndex} (Mittel)";

                        case 6:
                        case 7:
                            return $"{uvIndex} (Hoch)";

                        case 8:
                        case 9:
                        case 10:
                            return $"{uvIndex} (Sehr hoch)";

                        case >= 11:
                            return $"{uvIndex} (Extrem)";

                        default:
                            return uvIndex.ToString();
                    }
                }
                #endregion

                if (string.IsNullOrEmpty(place))
                    throw new ArgumentException($"'{nameof(place)}' cannot be null or empty.", nameof(place));

                if (place == "frankfurt".ToLower() && Context.User.Id == 346295434546774016)
                    place = "Frankfurt an der Oder";
                
                HttpResponseMessage response = WaitFor(_weatherStackApi.GetAsync($"current?access_key={_config["weatherstack.token"]}&query={place}"));
                string strResponse = WaitFor(response.Content.ReadAsStringAsync());

                WeatherStackModel serializedResponse = JsonConvert.DeserializeObject<WeatherStackModel>(strResponse);
                string location = place.ToLower() == "frankfurt" ? "Frankfurt am Main" : serializedResponse.Location.Name;

                EmbedBuilder embed = new()
                {
                    Title = $"Wetter für {location}, {serializedResponse.Location.Region}, {serializedResponse.Location.Country}",
                    Color = Color.DarkBlue,
                    Fields = GetEmbedFields(serializedResponse),
                    ThumbnailUrl = serializedResponse.Current.WeatherIcons[0],
                    Description = $"__**{serializedResponse.Current.WeatherDescriptions[0]} {serializedResponse.Current.Temperature} °C**__\n\n" +
                    $"**Luftdruck:** {serializedResponse.Current.Pressure} mBar\n" +
                    $"**Wind:** {serializedResponse.Current.WindSpeed} km/h {ParseWindDirection(serializedResponse.Current.WindDir)} ({serializedResponse.Current.WindDegree} °)\n" +
                    $"**Relative Luftfeuchte:** {serializedResponse.Current.Humidity} %",
                    Footer = new()
                    {
                        Text = "Data provided by WeatherStack API",
                        IconUrl = "https://rapidapi-prod-apis.s3.amazonaws.com/c2139e70-bb7e-4aaa-81e9-b8f70cdb77d4.png"
                    }
                };

                return ReplyAsync(embed: embed.Build());
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
                await Context.Message.RemoveReactionAsync(_waitEmote, 966325392707301416);  // Ceres production
                await Context.Message.RemoveReactionAsync(_waitEmote, 1055501764780113951); // Ceres beta
                await base.AfterExecuteAsync(command);
            }
            #endregion
            #region Static
            static internal void AddEmote(SocketGuild guild, out GuildEmote emote, string emoteName, string emoteUrl = null, Attachment attachment = null)
            {
                emote = null;
                string definitveEmoteUrl = emoteUrl == null
                    ? attachment.Url
                    : emoteUrl;
                Uri emoteUri = new(definitveEmoteUrl); // Throws exception if URL is not valid. Gets handled in CommandHandler.OnSlashCommandAsync(SocketSlashCommand)

                using HttpClient httpClient = new();
                using HttpResponseMessage response = WaitFor(httpClient.GetAsync(emoteUri));
                using Stream stream = WaitFor(response.Content.ReadAsStreamAsync());
                try
                {
                    emote = WaitFor(guild.CreateEmoteAsync(emoteName, new Image(stream)));
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("BINARY_TYPE_MAX_SIZE") || ex.Message.Contains("50138")) throw new Exception("File size must be 2048 KiB or less", ex);
                }
            }
            #endregion
        }
    }
}
