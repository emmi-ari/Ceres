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
            input = input.EndsWith(';') ? input : input + ";";
            Globals globals = new(ctx);
            ScriptOptions scriptOptions = ScriptOptions.Default;

#if RELEASE
            scriptOptions.WithOptimizationLevel(Microsoft.CodeAnalysis.OptimizationLevel.Release);
#endif
            scriptOptions = scriptOptions.AddImports("System");
            scriptOptions = scriptOptions.AddImports("System.Collections.Generic");
            scriptOptions = scriptOptions.AddImports("System.Diagnostics");
            scriptOptions = scriptOptions.AddImports("System.IO");
            scriptOptions = scriptOptions.AddImports("System.Threading");
            scriptOptions = scriptOptions.AddImports("System.Threading.Tasks");
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
                await ctx.RespondAsync($"`{ex.Message} (0x{ex.HResult:x8})`");
                GC.WaitForPendingFinalizers();
                GC.Collect();
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
            Environment.GetCommandLineArgs().ToList()
                .ForEach(arg => commandLine += $"{arg} ");
            DiscordEmbed embed = new DiscordEmbedBuilder()
            {
                Color = new DiscordColor(45, 122, 185),
                Author = new() { Name = "Bot by emmi.ari", IconUrl = authorIcon },
                Title = "Info about Ceres",
                Description = "My [source code](https://github.com/emmi-ari/Ceres)",
            }
                .AddField("Base directory", AppContext.BaseDirectory, false)
                .AddField("Process Up time", $"{DateTime.Now - process.StartTime}", false)
                .AddField("Debugger attached", $"{Debugger.IsAttached}", false)
                .AddField("Command Line", commandLine.TrimEnd(), false)
                .AddField("Current threads", $"{process.Threads.Count}", false)
                .AddField("Memory usage", $"{process.WorkingSet64 / 1024000} MiB", false)
                .AddField("Guilds", $"{ctx.Client.Guilds.Count}", false)
                .AddField("dotnet version", $"{Environment.Version}", false)
                .AddField("DSharpPlus version", ctx.Client.VersionString, false)
                .Build();
            await ctx.RespondAsync(embed);
        }

        public override string ToString()
        {
            return "Woah! Krass geile Liste mit allen Members und deren Typen und so!";
        }

        public class Globals(CommandContext context)
        {
            public readonly CommandContext ctx = context;
        }
    }
}
