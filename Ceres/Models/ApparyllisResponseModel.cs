using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ceres.Models
{
    internal class ApparyllisModel
    {
        [JsonProperty("response")]
        internal ApparyllisResponseModel[] Apparyllis { get; init; }
    }

    internal class ApparyllisResponseModel
    {
        [JsonProperty("exists")]
        internal bool Exists { get; init; }

        [JsonProperty("id")]
        internal string Id { get; init; }

        [JsonProperty("content")]
        internal ApparyllisContent Content { get; init; }
    }

    internal class ApparyllisContent
    {
        [JsonProperty("custom")]
        internal bool Custom { get; init; }

        [JsonProperty("startTime")]
        internal ulong? StartTime { get; init; }

        [JsonProperty("member")]
        internal string Member { get; init; }

        [JsonProperty("live")]
        internal bool Live { get; init; }

        [JsonProperty("endTime")]
        internal ulong? EndTime { get; init; }

        [JsonProperty("uid")]
        internal string Uid { get; init; }

        [JsonProperty("lastOperationTime")]
        internal ulong? LastOperationTime { get; init; }
    }
}
