namespace Circumlink.Level;


public record LevelGenerationPolicyWithId : LevelGenerationPolicy
{
    public string Id { get; set; }
}
