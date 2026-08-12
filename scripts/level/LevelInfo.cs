using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Circumlink.Level;

public struct LevelInfo
{
    public string Id;

    [JsonConverter(typeof(LevelGenerationPolicyListConverter))]
    public List<LevelGenerationPolicy> Policies { get; init; }

}
