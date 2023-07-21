using DSharpPlus;

namespace CeresDSP
{
    internal class Program
    {
        static async Task Main()
        {
            DiscordClient discord = new(new DiscordConfiguration()
            {
                Token = await File.ReadAllTextAsync(@"C:\Users\emmia\Desktop\token"),
                TokenType = TokenType.Bot,
                Intents = (DiscordIntents)0x1FFFF
            });
            await discord.ConnectAsync();
            discord.MessageCreated += async (s, e) =>
            {
                if (e.Message.Content.ToLower().StartsWith("ping"))
                    await e.Message.RespondAsync("pong!");
            };
            await Task.Delay(-1);
        }
    }
}