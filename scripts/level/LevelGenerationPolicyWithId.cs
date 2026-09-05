using System;

namespace Circumlink.Level;


public record LevelGenerationPolicyWithId : LevelGenerationPolicy
{
    public string Id { get; set; }

    public override string ToString() => $"{Enum.GetName(PolicyKind)}:{Id}";
}
