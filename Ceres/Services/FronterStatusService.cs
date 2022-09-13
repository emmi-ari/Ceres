using Discord;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#pragma warning disable CA1822
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
            _timer = new PeriodicTimer(TimeSpan.FromMinutes(10));
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

    internal class CommonFronterStatusMethods
    {
        private readonly DiscordSocketClient _discord;
        private readonly IConfigurationRoot _config;
        private readonly LoggingService _logger;
        private readonly Dictionary<string, string> _memberIdNames;

        internal CommonFronterStatusMethods(DiscordSocketClient discord, IConfigurationRoot config)
        {
            _discord = discord;
            _config = config;
            _logger = new();
            _memberIdNames = GetMemberIdNames();
        }

        private Dictionary<string, string> GetMemberIdNames()
        {
            string[] memberIdArray = _config["apparyllis.members"].Split(' ');
            Dictionary<string, string> memberIdNames = new(memberIdArray.Length);
            foreach (string memberId in memberIdArray)
            {
                string[] memberIdAux = memberId.Split('_');
                memberIdNames.Add(memberIdAux[1], memberIdAux[0]);
            }
            return memberIdNames;
        }

        internal async Task<string> SetFronterStatusAsync()
        {
#if DEBUG
            string statusMessage = "DBG - No fronter info";
            await _logger.OnLogAsync(new(LogSeverity.Debug, nameof(this.SetFronterStatusAsync), $"Debug"));
#else
            List<string> serializedFronterList = await GetFrontersList();
            string statusMessage = string.Empty;

            try
            {
                statusMessage = serializedFronterList.Count switch
                {
                    1 => $"{serializedFronterList[0]} is fronting",
                    2 => $"{serializedFronterList[0]} and {serializedFronterList[1]} are fronting",
                    3 => $"{serializedFronterList[0]}, {serializedFronterList[1]} and {serializedFronterList[2]} are fronting",
                    _ => throw new ArgumentException($"Unusual amount ({serializedFronterList?.Count}) of fronters in response", nameof(serializedFronterList))
                };
            }
            catch (ArgumentException ex)
            {
                statusMessage = "StatusGeneratorException (lol)";
                await _logger.OnLogAsync(new(LogSeverity.Error, nameof(this.SetFronterStatusAsync), $"Unusual amount of fronters recieved ({string.Join(", ", serializedFronterList.ToArray())})", ex));
                throw ex;
            }
#endif

            await _discord.SetGameAsync(statusMessage);

            return statusMessage;
        }

        private async Task<string> GetFrontStatusAsync()
        {
            HttpClient request = new()
            {
                BaseAddress = new("https://api.apparyllis.com:8443")
            };
            request.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", _config["apparyllis.simplyplural_token"]);
            request.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage frontingStatusResponse = await request.GetAsync("/v1/fronters/");
            string response = await frontingStatusResponse.Content.ReadAsStringAsync();
            string logMessage = $"Recieved response from apparyllis server: {frontingStatusResponse.StatusCode}";
            LogSeverity severity = LogSeverity.Info;
            if (frontingStatusResponse.IsSuccessStatusCode)
            {
                logMessage += $" ({response.Length} bytes)";
            }
            else
                severity = LogSeverity.Warning;

            await _logger.OnLogAsync(new(severity, nameof(this.GetFrontStatusAsync), logMessage));

            return response;
        }

        private async Task<List<string>> GetFrontersList()
        {
            string response = await GetFrontStatusAsync();
            JArray responseSerialized = JsonConvert.DeserializeObject<JArray>(response);
            return ParseMembers(responseSerialized);
        }

        private List<string> ParseMembers(JArray responseSerialized)
        {
            if (responseSerialized == null) throw new ArgumentNullException(paramName: nameof(responseSerialized), string.Empty);

            int fronterCount = responseSerialized.Count;
            List<string> serializedFronterList = new(fronterCount);

            for (int i = 0; i < fronterCount; i++)
            {
                JToken fronter = responseSerialized[i];
                if (!_memberIdNames.ContainsKey(fronter["content"].Value<string>("member")))
                    continue;
                serializedFronterList.Add(_memberIdNames[fronter["content"].Value<string>("member")]);
            }

            return serializedFronterList;
        }
    }
}
#pragma warning restore CA1822