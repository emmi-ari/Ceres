using DSharpPlus;

namespace CeresDSP
{
    internal class Program
    {
        private static Task Main() => new Program().MainAsync();
        //static async Task Main()
        //{
        //    DiscordClient discord = new(new DiscordConfiguration()
        //    {
        //        Token = await File.ReadAllTextAsync(@"C:\Users\emmia\token"),
        //        TokenType = TokenType.Bot,
        //        Intents = (DiscordIntents)0x1FFFF
        //    });
        //    await discord.ConnectAsync();
        //    discord.MessageCreated += async (s, e) =>
        //    {
        //        if (e.Message.Content.ToLower().StartsWith("ping"))
        //            await e.Message.RespondAsync("pong!");
        //    };
        //    await Task.Delay(-1);
        //}
    
        private async Task MainAsync()
        {
            Ceres ceres = new();
            await ceres.ConnectAsync();
            await Task.Delay(-1);
        }
    }
}