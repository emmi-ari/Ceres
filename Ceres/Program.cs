using Discord;
using Discord.WebSocket;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System.Diagnostics;

#pragma warning disable CS8618, CS8602, CS8604
namespace Ceres
{
    internal class Program
    {
        private DiscordSocketClient _client;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly string _spAuthKey = File.ReadAllText(@"C:\Users\Emmi\Source\GitHub\CeresBot\simplyplural_token");

        /// <summary>
        /// Workaround for an asynchronus entry point.
        /// </summary>
        private static Task Main() => new Program().MainAsync();

        /// <summary>
        /// Actual entry point of application
        /// </summary>
        private async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _client.Ready += () => {
                Print("Bot is connected!");
                return Task.CompletedTask;
            };
            _client.Disconnected += Client_Disconnected;
            _stopwatch.Start();
            await _client.LoginAsync(TokenType.Bot, File.ReadAllText(@"C:\Users\Emmi\Source\GitHub\CeresBot\discord_token"));
            await _client.StartAsync();

            var timer = new Timer(StatusTimer, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
            await Task.Delay(-1);
        }

        #region Fronter status
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
            await _client.SetGameAsync(statusMessage);
            return statusMessage;
        }

        private async Task<HttpResponseMessage> GetFrontStatusAsync()
        {
            HttpClient request = new()
            {
                BaseAddress = new("https://api.apparyllis.com:8443")
            };
            request.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", _spAuthKey);
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

            Print("Fronters:");
            foreach (string fronter in serializedFronterList)
            {
                Print(fronter, 11);
            }
            Print();

            return serializedFronterList;
        }
        #endregion

        #region Helper methods
#pragma warning disable CA1822
        private void Print(string message, int leadingSpaces)
            => Console.WriteLine($"{new string(' ', leadingSpaces)}{message}");

        private void Print(string message)
            => Console.WriteLine($"[{_stopwatch.ElapsedMilliseconds:00000000}] {message}");

        private void Print()
            => Console.WriteLine();
#pragma warning restore
        #endregion

        #region Timer methods
        private async void StatusTimer(object? e)
        {
            Print("Getting front status");
            string status = await SetFronterStatusAsync();
            Print($"New status: '{status}'");
        }
        #endregion

        #region Bot events
        internal Task Client_Disconnected(Exception arg)
        {
            Console.WriteLine($"[{_stopwatch.ElapsedMilliseconds:x8}] {arg.Message}");
            return Task.CompletedTask;
        }
        #endregion
    }
}
#pragma warning restore CS8618, CS8602, CS8604
