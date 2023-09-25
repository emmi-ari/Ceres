using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

using System.Text.RegularExpressions;

namespace CeresDSP.Services
{
    internal static class ClientEventsHandlerService
    {
        internal static async Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs args)
        {
            DiscordMessage message = args.Message;
            string messageContent = message.Content;
            MatchCollection matches = Regex.Matches(messageContent, @"\bhttps:\/\/spotify\.link\/[a-zA-Z0-9]{11}\b", RegexOptions.Multiline); // This should never match anything else from a message than just the link due to the \b at the beginning and end
            if (matches.Count == 0) return; 
            List<string> normalLink = new(matches.Count);

            for (int i = 0; i < matches.Count; i++)
            {
                string link = matches[i].Value;
                normalLink.Add(await GetRedirectUrl(link));
            }

            try
            {
                await message.RespondAsync(string.Join('\n', normalLink));
            }
            catch (ArgumentException ex)
            {
                if (ex.Message == "Message content must not be empty.") return;
                else throw;
            }
        }

        private static async Task<string> GetRedirectUrl(string link)
        {
            using HttpClient client = new();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/117.0.0.0 Safari/537.36");
            HttpResponseMessage response = await client.SendAsync(new(HttpMethod.Get, link));
            string responseContent = await response.Content.ReadAsStringAsync();
            MatchCollection matches = Regex.Matches(responseContent, @"<meta property=""og:url"" content=""(https:\/\/open\.spotify\.com\/\w+\/\w+)""\/>", RegexOptions.Singleline);

            return matches.Count > 0
                ? matches[0].Groups[1].Value
                : null;
        }

        internal static async Task OnReactionAdded(DiscordClient sender, MessageReactionAddEventArgs args)
        {
            if (args.User.Id == 233018119856062466) return;

            DiscordEmoji reactionEmote = args.Emoji;
            DiscordMessage reactedMsg = args.Message;
            DiscordGuild indicatorEmoteServer = await sender.GetGuildAsync(1034142544642183178);
            IReadOnlyDictionary<ulong, DiscordEmoji> indicatorEmotesArray = indicatorEmoteServer.Emojis;

            foreach (var indicatorEmote in indicatorEmotesArray)
            {
                if (reactionEmote.Id.Equals(indicatorEmote.Value.Id))
                    await reactedMsg.DeleteReactionAsync(reactionEmote, args.User);
            }
        }

        internal static async Task OnThreadCreated(DiscordClient sender, ThreadCreateEventArgs args)
        {
            string parentChannelMention = args.Parent.Mention;
            string threadChannelMention = args.Thread.Mention;
            DiscordChannel threadsChannel = args.Guild.GetChannel(1053712925891772467);

            if (!args.Thread.IsPrivate)
                await threadsChannel.SendMessageAsync($"{threadChannelMention} - {parentChannelMention}");
        }
    }
}
