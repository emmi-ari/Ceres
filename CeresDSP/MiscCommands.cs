using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace CeresDSP
{
    public class MiscCommands : BaseCommandModule
    {
        [Command("test")]
        public async Task TestCommand(CommandContext ctx)
        {
            await ctx.Channel.SendMessageAsync("Hello World!");
        }
    }
}
