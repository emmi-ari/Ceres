using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

using System.Diagnostics;

namespace CeresDSP.CommandModules
{
    public class OwnerCommands : BaseCommandModule
    {
        [Command("eval"), RequireOwner]
        public async Task Eval(CommandContext ctx, [RemainingText] string input)
        {
            Globals globals = new() { ctx = ctx };
            ScriptOptions scriptOptions = ScriptOptions.Default;
            scriptOptions = scriptOptions.AddImports("System");
            scriptOptions = scriptOptions.AddImports("System.Diagnostics");

            try
            {
                object evaluation = await CSharpScript.EvaluateAsync(input.Trim('`', '\'', '"'), scriptOptions, globals);
                
                if (evaluation is not null && evaluation.GetType().IsArray)
                {
                    string messageFromArray = string.Empty;
                    Array.ForEach((object[])evaluation, arrayObject => messageFromArray += $"{arrayObject}\n");
                    await ctx.RespondAsync(messageFromArray);
                }
                else if (evaluation is not null && !evaluation.GetType().IsArray)
                    await ctx.RespondAsync(evaluation.ToString());
            }
            catch (Exception ex)
            {
                await ctx.RespondAsync($"{ex.Message}\n*(0x{ex.HResult:x8})*");
                GC.WaitForPendingFinalizers();
                GC.Collect();
                throw;
            }
            
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        [Command("Info")]
        public async Task Info(CommandContext ctx)
        {
            Process process = Process.GetCurrentProcess();
            string authorIcon = (await ctx.Client.GetUserAsync(233018119856062466)).AvatarUrl;
            string commandLine = string.Empty;
            var cmdArgs = Environment.GetCommandLineArgs().ToList();
            cmdArgs.ForEach(arg => commandLine += $"{arg} ");
            DiscordEmbed embed = new DiscordEmbedBuilder()
            {
                ImageUrl = ctx.Client.CurrentUser.AvatarUrl,
                Color = new DiscordColor(45, 122, 185),
                Author = new() { Name = "Bot by emmi.ari", IconUrl = authorIcon },
                Title = "Info about Ceres",
                Description = "My [source code](https://github.com/Nihilopia/Ceres)",
            }
                .AddField("Base directory", AppContext.BaseDirectory, false)
                .AddField("Process Up time", $"{DateTime.Now - process.StartTime}", false)
                .AddField("Debugger attached", $"{Debugger.IsAttached}", false)
                .AddField("Command Line", commandLine.Trim(), false)
                .AddField("Current threads", $"{process.Threads.Count}", false)
                .AddField("Memory usage", $"{process.WorkingSet64 / 1024000} MiB", false)
                .AddField("Guilds", $"{ctx.Client.Guilds.Count}", false)
                .AddField("dotnet version", $"{Environment.Version}", false)
                .AddField("DSharpPlus version", ctx.Client.VersionString, false)
                .Build();
            await ctx.RespondAsync(embed);
        }

        public class Globals
        {
            public CommandContext ctx;
        }
    }
}
