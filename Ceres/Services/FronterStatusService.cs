using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ceres.Services
{
    internal class FronterStatusService
    {
        private readonly DiscordSocketClient _discord;
        private readonly IConfigurationRoot _config;

        // DiscordSocketClient, CommandService, and IConfigurationRoot are injected automatically from the IServiceProvider
        public FronterStatusService(IServiceProvider provider, DiscordSocketClient discord, CommandService commands, IConfigurationRoot config)
        {
            _config = config;
            _discord = discord;
            Timer? timer = new(StatusTimer, null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
        }

        private async void StatusTimer(object? _)
            => await SetFronterStatusAsync();
        
        private async Task<string> SetFronterStatusAsync()
        {
            List<string> serializedFronterList = await GetFrontersList();

            string statusMessage = serializedFronterList.Count switch
            {
                1 => $"{serializedFronterList[0]} is fronting",
                2 => $"{serializedFronterList[0]} and {serializedFronterList[1]} are fronting",
                3 => $"{serializedFronterList[0]}, {serializedFronterList[1]} and {serializedFronterList[2]} are fronting",
                _ => throw new ArgumentException($"Unusual amount ({serializedFronterList?.Count}) of fronters in response", nameof(serializedFronterList))
            };

            await _discord.SetGameAsync(statusMessage);
            return statusMessage;
        }

        private async Task<HttpResponseMessage> GetFrontStatusAsync()
        {
            HttpClient request = new()
            {
                BaseAddress = new("https://api.apparyllis.com:8443")
            };
            request.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", _config["simplyplural_token"]);
            request.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            return await request.GetAsync("/v1/fronters/");
        }

        private async Task<List<string>> GetFrontersList()
        {
            HttpResponseMessage frontingStatusResponse = await GetFrontStatusAsync();
            string response = await frontingStatusResponse.Content.ReadAsStringAsync();
            JArray? responseSerialized = JsonConvert.DeserializeObject<JArray?>(response);
            return ParseMembers(responseSerialized);
        }

        private List<string> ParseMembers(JArray? responseSerialized)
        {
            if (responseSerialized == null) throw new ArgumentNullException(paramName: nameof(responseSerialized), string.Empty);

            Dictionary<string, string> memberIdNames = new()
            {
                { "623cca89820d5b982fb848b6", "Emmi" },
                { "62a8972a7cc97c017b0ea31a", "Lio" },
                { "623ccab6820d5b982fb848b7", "Luana" }
            };

            int fronterCount = responseSerialized.Count;
            List<string> serializedFronterList = new(fronterCount);

            for (int i = 0; i < fronterCount; i++)
            {
                JToken? fronter = responseSerialized[i];
                serializedFronterList.Add(memberIdNames[fronter["content"].Value<string>("member")]);
            }

            return serializedFronterList;
        }
    }
}