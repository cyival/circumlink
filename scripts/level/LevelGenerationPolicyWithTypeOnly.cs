using System;

namespace Circumlink.Level;

public record LevelGenerationPolicyWithTypeOnly : LevelGenerationPolicy
{
    public override string ToString() => $"{Enum.GetName(PolicyKind)}";
}
