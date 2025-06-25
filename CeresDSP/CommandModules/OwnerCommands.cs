using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace CeresDSP.CommandModules
{
    public class OwnerCommands : BaseCommandModule
    {
        [Command("eval"), RequireOwner]
        public async Task Eval(CommandContext ctx, [RemainingText] string input)
        {
            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("💬"));
            await ctx.TriggerTypingAsync();
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
                input = !input.EndsWith(';') ? input : input + ';';
                object evaluation = await CSharpScript.EvaluateAsync(input.Trim('`', '\'', '"'), scriptOptions, globals);

                if (evaluation is not null && evaluation.GetType().IsArray)
                {
                    string messageFromArray = string.Empty;
                    Array.ForEach((object[])evaluation, arrayObject => messageFromArray += $"{arrayObject}\n");
                    await ctx.RespondAsync(messageFromArray);
                }
                else if (evaluation is not null && !evaluation.GetType().IsArray)
                    await ctx.RespondAsync(((Task<string>)evaluation).Result);
            }
            catch (Exception ex)
            {
                await ctx.RespondAsync($"`{ex.Message} (0x{ex.HResult:x8})`");
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            finally
            {
                await ctx.Message.DeleteReactionAsync(DiscordEmoji.FromUnicode("💬"), ctx.Client.CurrentUser);
                await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
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

            public string Help(string level = "this")
            {
                StringBuilder methods = new();
                MethodInfo[] methodInfos = level.ToUpper() switch
                {
                    "CHANNEL" => ctx.Channel.GetType().GetMethods(),
                    "CLIENT" => ctx.Client.GetType().GetMethods(),
                    "GUILD" => ctx.Guild.GetType().GetMethods(),
                    "MEMBER" => ctx.Member.GetType().GetMethods(),
                    "MESSAGE" => ctx.Message.GetType().GetMethods(),
                    "USER" => ctx.User.GetType().GetMethods(),
                    _ => this.GetType().GetMethods()
                };

                foreach (MethodInfo method in methodInfos)
                {
                    if (!method.IsSpecialName && (method.Name != "GetType" &&
                        method.Name != "ToString" &&
                        method.Name != "Equals" &&
                        method.Name != "GetHashCode"))
                    {
                        string parameterDescriptions = string.Join(", ", method.GetParameters()
                            .Select(parmInfo => parmInfo.ParameterType + " " + parmInfo.Name)
                            .ToArray());
                        methods.AppendLine($"{method.Name}({parameterDescriptions})");
                    }
                }

                Debug.WriteLine(methods.ToString());
                return methods.Length > 2000
                    ? "todo: message too long for Discord"
                    : methods.ToString().Replace('`', '´');
            }

            public async Task<DiscordMember> GetGuildMember(ulong guildId, ulong userId)
            {
                DiscordGuild guild = await ctx.Client.GetGuildAsync(guildId);
                if (guild is null)
                {
                    await ctx.RespondAsync($"Guild with ID {guildId} not found or accessible");
                    return null;
                }
                DiscordMember member = await guild.GetMemberAsync(userId);
                if (member is null)
                    await ctx.RespondAsync($"Member with ID {userId} on guild \"{guild.Name}\" not found or accessible");
                return member;
            }

            public async Task<DiscordGuild> GetGuild(ulong snowflake)
            {
                var guild = await ctx.Client.GetGuildAsync(snowflake);
                if (guild is null)
                    await ctx.Channel.SendMessageAsync($"Guild with ID {snowflake} not found or accessible.");
                else
                    await ctx.Channel.SendMessageAsync(guild.ToString());
                return guild;
            }

            public async Task<DiscordChannel> GetChannel(ulong snowflake)
            {
                var channel = await ctx.Client.GetChannelAsync(snowflake);
                if (channel is null)
                    await ctx.Channel.SendMessageAsync($"Channel with ID {snowflake} not found or accessible.");
                else
                    await ctx.Channel.SendMessageAsync(channel.ToString());
                return channel;
            }

            public async Task<DiscordUser> GetUser(ulong snowflake)
            {
                var user = await ctx.Client.GetUserAsync(snowflake);
                if (user is null)
                    await ctx.Channel.SendMessageAsync($"User with ID {snowflake} not found or accessible.");
                else
                    await ctx.Channel.SendMessageAsync(user.ToString());
                return user;
            }
        }
    }
}