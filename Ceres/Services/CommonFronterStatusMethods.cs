using Discord.WebSocket;

using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Ceres.Services
{
    internal class CommonFronterStatusMethods
    {
        private readonly DiscordSocketClient _discord;
        private readonly IConfigurationRoot _config;

        internal CommonFronterStatusMethods(DiscordSocketClient discord, IConfigurationRoot config)
        {
            _discord = discord;
            _config = config;
        }

        internal async Task<string> SetFronterStatusAsync()
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

#pragma warning disable CA1822
        private List<string> ParseMembers(JArray? responseSerialized)
#pragma warning restore CA1822
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
                if (!memberIdNames.ContainsKey(fronter["content"].Value<string>("member")))
                    continue;
                serializedFronterList.Add(memberIdNames[fronter["content"].Value<string>("member")]);
            }

            return serializedFronterList;
        }
    }
}
