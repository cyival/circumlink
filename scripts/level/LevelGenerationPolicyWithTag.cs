namespace Circumlink.Level;


public record LevelGenerationPolicyWithTag : LevelGenerationPolicy
{
    public string Tag { get; set; }
}
