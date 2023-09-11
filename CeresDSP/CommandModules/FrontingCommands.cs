using CeresDSP.Services;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;

namespace CeresDSP.CommandModules
{
    public class FrontingCommands : BaseCommandModule
    {
        private FronterStatusService FronterStatusService { get; set; }

        public FrontingCommands(FronterStatusService fronterStatusService)
        {
            FronterStatusService = fronterStatusService;
        }

        [Command("updatefront")]
        [Aliases("u", "uf")]
        public async Task UpdateFront(CommandContext ctx)
            => await UpdateFrontDynamic(ctx);
        internal async Task UpdateFront(InteractionContext ctx)
            => await UpdateFrontDynamic(ctx);
        private async Task UpdateFrontDynamic(dynamic ctx)
        {
            if (FronterStatusService._commonFronterStatus.StatusToggle)
            {
                await FronterStatusService._commonFronterStatus.SetFronterStatusAsync();
                await Helper.RespondToCommand(ctx, "Updated status.");
            }
            else
                await Helper.RespondToCommand(ctx, "Status is toggled off.");
        }

        [Command("ToggleStatus")]
        [Aliases("toggle", "t")]
        public async Task ToggleStatus(CommandContext ctx)
        {
            if (ctx.Member.Id is not (233018119856062466 or 320989312390922240)) await ctx.RespondAsync("No. Fuck off.");

            FronterStatusService._commonFronterStatus.StatusToggle = !FronterStatusService._commonFronterStatus.StatusToggle;

            if (!FronterStatusService._commonFronterStatus.StatusToggle)
                await ctx.Client.UpdateStatusAsync(new DiscordActivity());
            else
                await FronterStatusService._commonFronterStatus.SetFronterStatusAsync();

            await ctx.RespondAsync((FronterStatusService._commonFronterStatus.StatusToggle ? "En" : "Dis") + "abled status");
        }
    }

    [SlashCommandGroup("FrontingStatus", "Commands for Ceres' status")]
    public class FrontingCommandsSlash : ApplicationCommandModule
    {
        FrontingCommands FrontingCommads { get; init; }

        public FrontingCommandsSlash(FronterStatusService fronterStatusService)
        {
            FrontingCommads = new(fronterStatusService);
        }

        [SlashCommand("Update", "Updates Ceres status with refreshed information about who's fronting")]
        public async Task Update(InteractionContext ctx)
            => await FrontingCommads.UpdateFront(ctx);
    }
}
