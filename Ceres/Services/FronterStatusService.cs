using Ceres.Models;
using Ceres.Models.Apparyllis;

using Discord;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;

using Newtonsoft.Json;

namespace Ceres.Services
{
    internal class FronterStatusService
    {
        private readonly CommonFronterStatusMethods _commonFronterStatus;
        private readonly PeriodicTimer _timer;

        public FronterStatusService(DiscordSocketClient discord, IConfigurationRoot config)
        {
            _commonFronterStatus = new(discord, config);
            _timer = new PeriodicTimer(TimeSpan.FromMinutes(10));
            TriggerStatusRefresh();
        }

        private async void TriggerStatusRefresh()
        {
            do { await _commonFronterStatus.SetFronterStatusAsync(); }
            while (await _timer.WaitForNextTickAsync());
        }
    }

    internal class CommonFronterStatusMethods
    {
        private readonly DiscordSocketClient _discord;
        private readonly IConfigurationRoot _config;
        private readonly LoggingService _logger;
        private readonly Dictionary<string, string> _memberIdRelation;
        private readonly Dictionary<string, string> _customFrontIdRelation;
        private readonly HttpClient _request;

        internal CommonFronterStatusMethods(DiscordSocketClient discord, IConfigurationRoot config)
        {
            _discord = discord;
            _config = config;
            _logger = new();
            _memberIdRelation = GetRelationField(_config["apparyllis.members"]);
            _customFrontIdRelation = GetRelationField(_config["apparyllis.customFronts"]);
            HttpClient request = new()
            {
                BaseAddress = new("https://api.apparyllis.com:8443")
            };
            request.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", _config["apparyllis.simplyplural_token"]);
            request.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _request = request;
        }

        private Dictionary<string, string> GetRelationField(string memberIdsDeserialized)
        {
            string[] memberIdArray = memberIdsDeserialized.Split(' ');
            Dictionary<string, string> memberIdNames = new(memberIdArray.Length);
            foreach (string memberId in memberIdArray)
            {
                string[] memberIdAux = memberId.Split('_');
                memberIdNames.Add(memberIdAux[1].Trim(), memberIdAux[0].Trim());
            }
            return memberIdNames;
        }

        internal async Task<string> SetFronterStatusAsync()
        {
            List<FrontMemberInfos>[] frontInfos = ParseMembers(await GetFrontStatusAsync());
            var serializedFronterList = frontInfos[0];
            var serializedCustomFrontList = frontInfos[1];
            string statusMessage = string.Empty;

            serializedFronterList.ForEach(member => statusMessage += $"{member.MemberName}, ");
            if (serializedCustomFrontList.Count > 0)
            {
                statusMessage = $"{statusMessage.TrimEnd(',').TrimEnd(' ')} (";
                serializedCustomFrontList.ForEach(member => statusMessage += $"{member.MemberName}, ");
                statusMessage = $"{statusMessage.TrimEnd(',').TrimEnd(' ')})";
                statusMessage = statusMessage.Replace(", (", " (").Replace(",)", ")");
            }
            else
                statusMessage = statusMessage.Trim().TrimEnd(',');

            await _discord.SetGameAsync(statusMessage);

            return statusMessage;
        }

        private async Task<ApparyllisModel> GetFrontStatusAsync()
        {
            HttpResponseMessage frontingStatusResponse = await _request.GetAsync("/v1/fronters/");
            string response = await frontingStatusResponse.Content.ReadAsStringAsync();
            response = "{\"response\":" + response + "}"; // Because why would an API give valid JSON as response, am I right?
            ApparyllisModel serializedResponse = JsonConvert.DeserializeObject<ApparyllisModel>(response);
            string logMessage = $"Recieved response from apparyllis server: {frontingStatusResponse.StatusCode}";
            LogSeverity severity = LogSeverity.Info;
            if (frontingStatusResponse.IsSuccessStatusCode)
                logMessage += $" ({response.Length} bytes)";
            else
                severity = LogSeverity.Warning;

            await _logger.OnLogAsync(new(severity, nameof(this.GetFrontStatusAsync), logMessage));

            return serializedResponse;
        }

        private List<FrontMemberInfos>[] ParseMembers(ApparyllisModel responseSerialized)
        {
            if (responseSerialized == null) throw new ArgumentNullException(paramName: nameof(responseSerialized), string.Empty);

            int fronterCount = responseSerialized.Response.Length;
            List<FrontMemberInfos> serializedFronterList = new(fronterCount);
            List<FrontMemberInfos> serializedCustomFrontList = new(fronterCount);

            for (int i = 0; i < fronterCount; i++)
            {
                ApparyllisContentModel fronter = responseSerialized.Response[i]?.Content;
                if (fronter is null)
                    continue;

                try
                {
                    var test = _memberIdRelation[fronter.Member];
                }
                catch (KeyNotFoundException)
                {
                    try
                    {
                        var test = _customFrontIdRelation[fronter.Member];
                    }
                    catch (KeyNotFoundException) { continue; }

                    serializedCustomFrontList.Add(new(_customFrontIdRelation[fronter.Member], fronter.StartTime, fronter.EndTime));
                    continue;
                }

                serializedFronterList.Add(new (_memberIdRelation[fronter.Member], fronter.StartTime, fronter.EndTime));
            }

            List<FrontMemberInfos>[] retArray = {serializedFronterList, serializedCustomFrontList };

            return retArray;
        }
    }
}
