using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

using Microsoft.CodeAnalysis.CSharp.Scripting;

namespace CeresDSP.CommandModules
{
    public class OwnerCommands : BaseCommandModule
    {
        [Command("eval"), RequireOwner]
        public async Task Eval(CommandContext ctx, [RemainingText] string input)
        {
            Globals globals = new()
            {
                ctx = ctx
            };

            try
            {
                object evaluation = await CSharpScript.EvaluateAsync(input.Trim('`'), globals: globals, globalsType: typeof(Globals));
                await CSharpScript.EvaluateAsync(input.Trim('`'), globals: globals);
            }
            catch (Exception)
            {
                GC.WaitForPendingFinalizers();
                GC.Collect();
                throw;
            }

            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public class Globals
        {
            public CommandContext ctx;
        }
    }
}
