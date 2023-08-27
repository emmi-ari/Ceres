using Newtonsoft.Json;

namespace CeresDSP
{
    public class Configuration
    {
        [JsonProperty("ceres.token")]
        public string Token { get; init; }

        [JsonProperty("ceres.prefix")]
        public string Prefix { get; init; }
    }
}
