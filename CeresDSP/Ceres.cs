using DSharpPlus;
using DSharpPlus.Entities;

namespace CeresDSP
{
    internal class Ceres
    {
        public DiscordClient _client;

        public Ceres()
        {
            _client = new(new DiscordConfiguration()
            {
                Token = File.ReadAllText(@"C:\Users\emmia\Desktop\token"),
                TokenType = TokenType.Bot,
                Intents = (DiscordIntents)0x1FFFF
            });
        }

        private static Task<int> CustomPrefixPredicate(DiscordMessage msg)
        {
            var guild = DB.Guilds.GetGuild(msg.Channel.Guild);
            return msg.Content.StartsWith(guild.Prefix) ? Task.FromResult(guild.Prefix.Length) : Task.FromResult(-1);
        }
    }
}
