using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace CeresDSP.CommandModules
{
    internal static class Helper
    {
        internal static async Task RespondToCommand(dynamic ctx, string response)
        {
            try { await ((CommandContext)ctx).RespondAsync(response); }
            catch { await ((InteractionContext)ctx).CreateResponseAsync(response); }
        }
        internal static async Task RespondToCommand(dynamic ctx, DiscordEmbed embed)
        {
            try { await ((CommandContext)ctx).RespondAsync(embed: embed); }
            catch { await ((InteractionContext)ctx).CreateResponseAsync(embed: embed); }
        }
    }
}
