using Discord.WebSocket;

using Microsoft.Extensions.Configuration;

namespace Ceres.Services
{
    internal class FronterStatusService
    {
        private readonly CommonFronterStatusMethods _commonFronterStatus;
        private readonly PeriodicTimer _timer;
        private bool _initialState = true;

        // DiscordSocketClient, CommandService, and IConfigurationRoot are injected automatically from the IServiceProvider
        public FronterStatusService(DiscordSocketClient discord, IConfigurationRoot config)
        {
            _commonFronterStatus = new(discord, config);
            _timer = new PeriodicTimer(TimeSpan.FromMinutes(10));
            TriggerStatusRefresh();
        }

        private async void TriggerStatusRefresh()
        {
            if (!_initialState)
            {
                while (await _timer.WaitForNextTickAsync())
                {
                    await _commonFronterStatus.SetFronterStatusAsync();
                }
            }
            else
            {
                await _commonFronterStatus.SetFronterStatusAsync();
            }
        }
    }
}