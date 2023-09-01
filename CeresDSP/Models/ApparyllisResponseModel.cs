using Newtonsoft.Json;

namespace CeresDSP.Models.Apparyllis
{
    internal class ApparyllisModel
    {
        [JsonProperty("response")]
        internal ApparyllisResponseModel[] Response { get; init; }
    }

    internal class ApparyllisResponseModel
    {
        [JsonProperty("exists")]
        internal bool Exists { get; init; }

        [JsonProperty("id")]
        internal string Id { get; init; }

        [JsonProperty("content")]
        internal ApparyllisContentModel Content { get; init; }
    }

    internal class ApparyllisContentModel
    {
        [JsonProperty("custom")]
        internal bool Custom { get; init; }

        [JsonProperty("startTime")]
        internal long? StartTime { get; init; }

        [JsonProperty("member")]
        internal string Member { get; init; }

        [JsonProperty("live")]
        internal bool Live { get; init; }

        [JsonProperty("endTime")]
        internal long? EndTime { get; init; }

        [JsonProperty("uid")]
        internal string Uid { get; init; }

        [JsonProperty("lastOperationTime")]
        internal long? LastOperationTime { get; init; }
    }
}
