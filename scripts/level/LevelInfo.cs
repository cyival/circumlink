using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Circumlink.Level;

public struct LevelInfo()
{
    public string Id;

    [JsonConverter(typeof(LevelGenerationPolicyListConverter))]
    public List<LevelGenerationPolicy> Policies { get; set; } = [];

    public override string ToString()
    {
        var stringBuilder = new System.Text.StringBuilder();
        stringBuilder.Append(Id);
        stringBuilder.Append("<");
        stringBuilder.Append(string.Join(", ", Policies));
        stringBuilder.Append(">");

        return stringBuilder.ToString();
    }
}
