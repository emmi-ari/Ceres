using Discord.WebSocket;

using Microsoft.Extensions.Configuration;

namespace Ceres.Services
{
    internal class FronterStatusService
    {
        private readonly CommonFronterStatusMethods _commonFronterStatus;
        private readonly PeriodicTimer _timer;

        public FronterStatusService(DiscordSocketClient discord, IConfigurationRoot config)
        {
            _commonFronterStatus = new(discord, config);
#if DEBUG
            _timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
#else
            _timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
#endif
            TriggerStatusRefresh();
        }

        private async void TriggerStatusRefresh()
        {
            do
            {
                await _commonFronterStatus.SetFronterStatusAsync();
            }
            while (await _timer.WaitForNextTickAsync());
        }
    }
}