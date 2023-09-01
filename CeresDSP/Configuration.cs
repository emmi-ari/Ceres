using Newtonsoft.Json;

namespace CeresDSP
{
    public class Configuration
    {
        [JsonProperty("ceres")]
        public CeresData Ceres { get; set; }

        [JsonProperty("apparyllis")]
        public ApparyllisData Apparyllis { get; set; }

        [JsonProperty("weatherstack")]
        public WeatherstackData Weatherstack { get; set; }

        public class CeresData
        {
            [JsonProperty("token")]
            public string Token { get; init; }

            [JsonProperty("prefix")]
            public string Prefix { get; init; }

            [JsonProperty("foldercommandpath")]
            public string FolderCommandPath { get; init; }
        }

        public class ApparyllisData
        {
            [JsonProperty("simplyplural_token")]
            public string Token { get; init; }

            [JsonProperty("systemid")]
            public string SystemId { get; init; }

            [JsonProperty("members")]
            public string Members { get; init; }

            [JsonProperty("customFronts")]
            public string CustomFronts { get; init; }

            [JsonProperty("systemName")]
            public string SystemName { get; init; }
        }

        public class WeatherstackData
        {
            [JsonProperty("token")]
            public string Token { get; init; }
        }
    }
}
