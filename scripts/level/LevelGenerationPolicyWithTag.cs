using System;

namespace Circumlink.Level;

public record LevelGenerationPolicyWithTag : LevelGenerationPolicy
{
    public string Tag { get; set; }

    public override string ToString() => $"{Enum.GetName(PolicyKind)}#{Tag}";
}
