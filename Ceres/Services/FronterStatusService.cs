using Discord.WebSocket;

using Microsoft.Extensions.Configuration;

namespace Ceres.Services
{
    internal class FronterStatusService
    {
        private readonly CommonFronterStatusMethods _commonFronterStatus;

        // DiscordSocketClient, CommandService, and IConfigurationRoot are injected automatically from the IServiceProvider
        public FronterStatusService(DiscordSocketClient discord, IConfigurationRoot config)
        {
            _commonFronterStatus = new(discord, config);
            Timer? timer = new(StatusTimer, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
        }

        private async void StatusTimer(object? _)
        {
            await _commonFronterStatus.SetFronterStatusAsync();
        }
    }
}