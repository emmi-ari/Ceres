using CeresDSP.Models;
using CeresDSP.Models.Apparyllis;

using DSharpPlus;
using DSharpPlus.Entities;

using Newtonsoft.Json;

namespace CeresDSP.Services
{
    public class FronterStatusService
    {
        internal readonly CommonFronterStatusMethods _commonFronterStatus;
        private readonly PeriodicTimer _timer;

        public FronterStatusService(DiscordClient discord, Configuration config)
        {
            _commonFronterStatus = new(discord, config);
            _timer = new PeriodicTimer(TimeSpan.FromMinutes(10));
        }

        public async Task TriggerStatusRefreshAsync()
        {
            do { await _commonFronterStatus.SetFronterStatusAsync(); }
            while (await _timer.WaitForNextTickAsync());
        }
    }

    public class CommonFronterStatusMethods
    {
        private readonly DiscordClient _discord;
        private readonly Dictionary<string, string> _memberIdRelation;
        private readonly Dictionary<string, string> _customFrontIdRelation;
        private readonly HttpClient _request;

        public bool StatusToggle { get; set; }

        internal CommonFronterStatusMethods(DiscordClient discord, Configuration config)
        {
            _discord = discord;
            _memberIdRelation = GetRelationField(config.Apparyllis.Members);
            _customFrontIdRelation = GetRelationField(config.Apparyllis.CustomFronts);
            HttpClient request = new()
            {
                BaseAddress = new("https://api.apparyllis.com:8443")
            };
            request.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", config.Apparyllis.Token);
            request.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            _request = request;
            StatusToggle = true;
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

        internal async Task SetFronterStatusAsync()
        {
            if (!StatusToggle) return;

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

            await _discord.UpdateStatusAsync(new(statusMessage, ActivityType.Playing), UserStatus.Online);
        }

        private async Task<ApparyllisModel> GetFrontStatusAsync()
        {
            HttpResponseMessage frontingStatusResponse = await _request.GetAsync("/v1/fronters/");
            string response = await frontingStatusResponse.Content.ReadAsStringAsync();
            response = "{\"response\":" + response + "}"; // Because why would an API give valid JSON as response, am I right?
            ApparyllisModel serializedResponse = JsonConvert.DeserializeObject<ApparyllisModel>(response);

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
                if (fronter is null) continue;

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

                serializedFronterList.Add(new(_memberIdRelation[fronter.Member], fronter.StartTime, fronter.EndTime));
            }

            List<FrontMemberInfos>[] retArray = { serializedFronterList, serializedCustomFrontList };

            return retArray;
        }
    }
}
