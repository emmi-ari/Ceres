using CeresDSP.Services;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

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
        {
            if (FronterStatusService._commonFronterStatus.StatusToggle)
            {
                await FronterStatusService._commonFronterStatus.SetFronterStatusAsync();
                await ctx.RespondAsync("Updated status.");
            }
            else
                await ctx.RespondAsync("Status is toggled off.");
        }

        [Command("ToggleStatus")]
        [Aliases("toggle", "t")]
        public async Task ToggleStatus(CommandContext ctx)
        {
            if (ctx.Member.Id is not (233018119856062466 or 320989312390922240)) await ctx.RespondAsync("No. Fuck off.");

            FronterStatusService._commonFronterStatus.StatusToggle = !FronterStatusService._commonFronterStatus.StatusToggle;

            if (!FronterStatusService._commonFronterStatus.StatusToggle)
                await ctx.Client.UpdateStatusAsync(new());
            else
                await FronterStatusService._commonFronterStatus.SetFronterStatusAsync();

            await ctx.RespondAsync((FronterStatusService._commonFronterStatus.StatusToggle ? "En" : "Dis") + "abled status");
        }
    }
}
