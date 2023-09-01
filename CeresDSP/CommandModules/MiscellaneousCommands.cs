using CeresDSP.Models.Apparyllis;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

using Newtonsoft.Json;

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace CeresDSP.CommandModules
{
    public class MiscellaneousCommands : BaseCommandModule
    {
        private readonly Configuration _config;
        private readonly Random _unsafeRng;

        public MiscellaneousCommands(Configuration config)
        {
            _config = config;
            _unsafeRng = new();
        }

        [Command("GreetSunny")]
        public async Task GreetSunny(CommandContext ctx)
        {
            await ctx.RespondAsync("Hallo Sunny! :blobenjoy:");
        }

        [Command("AddReaction")]
        [Aliases("reaction", "react")]
        public async Task AddReaction(CommandContext ctx, string emote, string locationLink)
        {
            ulong guildId;
            ulong channelId;
            ulong messageId;
            DiscordGuild guild;
            DiscordChannel channel;
            DiscordMessage msg = null;

            Match match = Regex.Match(locationLink, @"(\/\d{17,}){2,3}");
            if (match.Groups.Count >= 2)
            {
                string[] ids = match.Groups[0].Value.Replace('/', ',').TrimStart(',').Split(',');
                guildId = Convert.ToUInt64(ids[0]);
                channelId = Convert.ToUInt64(ids[1]);
                messageId = Convert.ToUInt64(ids[2]);
            }
            else
            {
                await ctx.RespondAsync("Make sure it's a discord link with three (slash seperated) IDs");
                return;
            }

            try
            {
                guild = await ctx.Client.GetGuildAsync(guildId);
                channel = guild.GetChannel(channelId) ?? throw new ArgumentException("Not found: 404");
                msg = await channel.GetMessageAsync(messageId);
            }
            catch (Exception ex)
            {
                if (ex.Message == "Not found: 404")
                {
                    await ctx.RespondAsync("Link is either wrong or I don't have access to it.");
                    return;
                }
            }

            string emoteName = $":{Regex.Match(emote, @"(\w{2,})").Captures[0]}:";
            try
            {
                DiscordEmoji reactEmote = DiscordEmoji.FromName(ctx.Client, emoteName);
                await msg.CreateReactionAsync(reactEmote);
            }
            catch (ArgumentException ex)
            {
                if (ex.Message == "Invalid emoji name specified. (Parameter 'name')")
                    await ctx.RespondAsync("I don't have access to that emote.");
            }
        }

        [Command("Say")]
        public async Task Say(CommandContext ctx, string msg, string locationLink = "", ulong guildId = 0ul, ulong channelId = 0ul, ulong messageId = 0ul)
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
                await ctx.RespondAsync("Make sure it's a discord link with at least two (slash seperated) IDs");
                return;
            }
            #endregion

            #region Guild ID parsing
            if (guildId == 0ul)
                guildId = ctx.Guild.Id;
            DiscordGuild guild = await ctx.Client.GetGuildAsync(guildId);
            if (guild == null)
            {
                await ctx.RespondAsync("Invalid Guild ID");
                return;
            }
            #endregion

            #region Channel ID parsing
            if (channelId == 0ul)
                channelId = ctx.Channel.Id;
            if (guild.GetChannel(channelId) is not DiscordChannel messageChannel) // Null check
            {
                await ctx.RespondAsync("Invalid Channel ID");
                return;
            }
            #endregion

            #region Message ID parsing
            if (messageId != 0ul)
            {
                DiscordMessage replyMessage = await messageChannel.GetMessageAsync(messageId);
                if (replyMessage == null)
                {
                    await ctx.RespondAsync("Invalid Message ID");
                    return;
                }
                DiscordMessageBuilder messageBuilder = new DiscordMessageBuilder().WithContent(msg).WithReply(messageId);
                await messageBuilder.SendAsync(messageChannel);
                return;
            }
            #endregion

            await messageChannel.SendMessageAsync(msg);
        }

        [Command("folder")]
        [Aliases("f")]
        public async Task Folder(CommandContext ctx)
        {
            DirectoryInfo directory = new(_config.Ceres.FolderCommandPath);
            FileInfo[] folderFiles = directory.GetFiles()
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
            using FileStream fs = new(path: filePath, mode: FileMode.Open);
            DiscordMessageBuilder msg = new DiscordMessageBuilder().AddFile(fs);
            await msg.SendAsync(ctx.Channel);
        }

        [Command("EmoteToGif")]
        [Aliases("Emote", "Gif", "FuckNitro")]
        public async Task EmoteToGif(CommandContext ctx)
        {
            #region Local function(s)
            static async Task ConvertEmoteToGif(string emoteUrl, string emoteName, bool emoteIsAnimated)
            {
                // Download emote
                HttpClient client = new();
                Stream stream = await client.GetStreamAsync(emoteUrl);
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
            #endregion

            DiscordMessage msg = ctx.Message.Reference is not null
                ? await ctx.Channel.GetMessageAsync(ctx.Message.Reference.Message.Id)
                : ctx.Message;
            Match emoteSnowflake = Regex.Match(msg.Content, @"<(a|):(\w+):(\d+)>");
            bool isAnimated = emoteSnowflake.Groups[1].Value == "a";
            string emoteName = emoteSnowflake.Groups[2].Value;
            string emoteUrl = $"https://cdn.discordapp.com/emojis/{emoteSnowflake.Groups[3].Value}";

            await ConvertEmoteToGif(emoteUrl, emoteName, isAnimated);
            DiscordMessageBuilder messageBuilder = new();
            FileStream fileStream = new($"{emoteName}.gif", FileMode.Open);
            await messageBuilder.AddFile(fileStream).SendAsync(ctx.Channel);

        }
    }
}
