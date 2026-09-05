using System;

namespace Circumlink.Level;

public static class LevelGenerationPolicyExtensions
{
    extension(LevelGenerationPolicy)
    {
        public static LevelGenerationPolicy Parse(string input)
        {
            Godot.GD.Print($"Parsing policy: {input}");
            if (TryParse(input, out var result))
                return result!;
            throw new FormatException($"Invalid input format: '{input}'. Expected 'Type', 'Type:Id', or 'Type#Tag'.");
        }

        public static bool TryParse(string input, out LevelGenerationPolicy? result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(input)) return false;

            // Try using ':' to split (Id)
            int colonIdx = input.IndexOf(':');
            if (colonIdx > 0)
            {
                string typePart = input[..colonIdx];
                string id = input[(colonIdx + 1)..];
                if (Enum.TryParse<LevelGenerationPolicyKind>(typePart, true, out var kind) && !string.IsNullOrEmpty(id))
                {
                    result = new LevelGenerationPolicyWithId { PolicyKind = kind, Id = id };
                    return true;
                }
                return false;
            }

            // Try using '#' to split (Tag)
            int hashIdx = input.IndexOf('#');
            if (hashIdx > 0)
            {
                string typePart = input[..hashIdx];
                string tag = input[(hashIdx + 1)..];
                if (Enum.TryParse<LevelGenerationPolicyKind>(typePart, true, out var kind) && !string.IsNullOrEmpty(tag))
                {
                    result = new LevelGenerationPolicyWithTag { PolicyKind = kind, Tag = tag };
                    return true;
                }
                return false;
            }

            // No split char
            if (Enum.TryParse<LevelGenerationPolicyKind>(input, true, out var kindOnly))
            {
                result = new LevelGenerationPolicyWithTypeOnly { PolicyKind = kindOnly };
                return true;
            }

            return false;
        }
    }
}
