using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

using System.Diagnostics;
using System.Reflection;
using System.Text;
using DSharpPlus;

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
                await ctx.RespondAsync($"{ex.Message}\n*(0x{ex.HResult:x8})*");
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

        public class Globals(CommandContext context)
        {
            public readonly CommandContext ctx = context;

            public async Task<string> Help(string level = "this")
            {
                StringBuilder methods = new();
                switch (level.ToUpper())
                {
                    case "THIS":
                    default:
                        foreach (MethodInfo method in this.GetType().GetMethods())
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
                        break;
                    case "CHANNEL":
                        foreach (MethodInfo method in ctx.Channel.GetType().GetMethods())
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
                        break;
                    case "CLIENT":
                        foreach (MethodInfo method in ctx.Client.GetType().GetMethods())
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
                        break;
                    case "GUILD":
                        foreach (MethodInfo method in ctx.Guild.GetType().GetMethods())
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
                        break;
                    case "MEMBER":
                        foreach (MethodInfo method in ctx.Member.GetType().GetMethods())
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
                        break;
                    case "MESSAGE":
                        foreach (MethodInfo method in ctx.Message.GetType().GetMethods())
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
                        break;
                    case "USER":
                        foreach (MethodInfo method in ctx.User.GetType().GetMethods())
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
                        break;
                }
                Debug.WriteLine(methods.ToString());
                if (methods.Length > 2000)
                    return "todo: message too long for Discord";
                await ctx.RespondAsync(methods.ToString().Replace('`', '´'));
                return methods.ToString().Replace('`', '´');
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