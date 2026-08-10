using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Circumlink.Level;

public struct LevelInfo
{
    [JsonConverter(typeof(LevelGenerationPolicyListConverter))]
    public List<LevelGenerationPolicy> Policies { get; init; }

    public required string Id { get; init; }
}
