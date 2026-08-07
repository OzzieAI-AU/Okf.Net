namespace Okf.Net.Models
{

    /// <summary>
    /// Represents a complete OKF Knowledge Bundle.
    /// </summary>
    public record OkfBundle
    {
        public required string RootDirectory { get; init; }
        public required IReadOnlyDictionary<string, OkfConcept> Concepts { get; init; }
        public required IReadOnlyDictionary<string, string> IndexFiles { get; init; }
        public required IReadOnlyDictionary<string, string> LogFiles { get; init; }
    }

    /// <summary>
    /// Represents a single OKF Concept.
    /// </summary>
    public record OkfConcept
    {
        public required string Id { get; init; }
        public required string FilePath { get; init; }
        public required OkfFrontmatter Frontmatter { get; init; }
        public required string Body { get; init; }
        public required IReadOnlyList<OkfLink> OutgoingLinks { get; init; }
    }

    /// <summary>
    /// Represents OKF YAML Frontmatter metadata.
    /// </summary>
    public record OkfFrontmatter
    {
        public required string Type { get; init; }
        public string? Title { get; init; }
        public string? Description { get; init; }
        public string? Resource { get; init; }
        public required IReadOnlyList<string> Tags { get; init; }
        public DateTimeOffset? Timestamp { get; init; }

        // Spec §4.1: "Consumers MUST preserve unknown keys on round-trip"
        public required IReadOnlyDictionary<string, object> Extensions { get; init; }
    }

    /// <summary>
    /// Represents a parsed link within a concept's body.
    /// </summary>
    public record OkfLink(string Text, string Target, bool IsAbsolute);
}