namespace Circumlink.Level;


public abstract record LevelGenerationPolicy
{
    public LevelGenerationPolicyKind PolicyKind { get; set; }
}
