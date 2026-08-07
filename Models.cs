namespace Okf.Net.Models
{


    /// <summary>
    /// Represents a complete Open Knowledge Format (OKF) Knowledge Bundle containing concepts, indexes, and logs.
    /// </summary>
    /// <remarks>
    /// An OKF bundle is a self-contained directory structure representing an integrated knowledge graph
    /// consisting of structured markdown documents (concepts), catalog indexes, and transactional logs.
    /// </remarks>
    public record OkfBundle
    {


        /// <summary>
        /// Gets the root directory path of the OKF bundle on the host file system.
        /// </summary>
        /// <value>
        /// A string representing the absolute or relative file system directory path to the bundle's root.
        /// </value>
        public required string RootDirectory { get; init; }


        /// <summary>
        /// Gets the collection of concepts defined within the bundle, keyed by their unique identifiers.
        /// </summary>
        /// <value>
        /// A read-only dictionary mapping unique string concept IDs to their parsed <see cref="OkfConcept"/> representations.
        /// </value>
        public required IReadOnlyDictionary<string, OkfConcept> Concepts { get; init; }


        /// <summary>
        /// Gets the collection of index files in the bundle, mapping relative paths to their raw text contents.
        /// </summary>
        /// <value>
        /// A read-only dictionary mapping relative file system paths of indexes to their corresponding raw text content.
        /// </value>
        public required IReadOnlyDictionary<string, string> IndexFiles { get; init; }


        /// <summary>
        /// Gets the collection of log files in the bundle, mapping relative paths to their raw text contents.
        /// </summary>
        /// <value>
        /// A read-only dictionary mapping relative file system paths of logs to their corresponding raw text content.
        /// </value>
        public required IReadOnlyDictionary<string, string> LogFiles { get; init; }
    }


    /// <summary>
    /// Represents a single OKF Concept within an integrated knowledge bundle.
    /// </summary>
    /// <remarks>
    /// A concept is the primary node of knowledge in OKF. It consists of a unique identifier, 
    /// a file path, parsed YAML frontmatter metadata, a markdown body, and outgoing relational links.
    /// </remarks>
    public record OkfConcept
    {


        /// <summary>
        /// Gets the unique identifier of the concept.
        /// </summary>
        /// <value>
        /// A unique string identifying this concept across the knowledge bundle.
        /// </value>
        public required string Id { get; init; }


        /// <summary>
        /// Gets the file path of the concept, relative to the bundle's root directory.
        /// </summary>
        /// <value>
        /// The relative file path where the concept file resides.
        /// </value>
        public required string FilePath { get; init; }


        /// <summary>
        /// Gets the metadata extracted from the concept's YAML frontmatter header.
        /// </summary>
        /// <value>
        /// An <see cref="OkfFrontmatter"/> instance containing standard and custom extension properties.
        /// </value>
        public required OkfFrontmatter Frontmatter { get; init; }


        /// <summary>
        /// Gets the body content of the concept, excluding the frontmatter header.
        /// </summary>
        /// <value>
        /// The raw body text, typically formatted as Markdown, representing the concept's substance.
        /// </value>
        public required string Body { get; init; }


        /// <summary>
        /// Gets the collection of parsed outgoing links originating from this concept's body.
        /// </summary>
        /// <value>
        /// A read-only list of <see cref="OkfLink"/> references detected and parsed within the markdown body.
        /// </value>
        public required IReadOnlyList<OkfLink> OutgoingLinks { get; init; }
    }


    /// <summary>
    /// Represents OKF YAML Frontmatter metadata containing standard specifications and custom extension properties.
    /// </summary>
    /// <remarks>
    /// This model handles both the strongly-typed standard properties of the OKF specification 
    /// and arbitrary extension properties to preserve semantic attributes during processing.
    /// </remarks>
    public record OkfFrontmatter
    {


        /// <summary>
        /// Gets the classification or type schema of the concept.
        /// </summary>
        /// <value>
        /// A string specifying the conceptual classification (e.g., "term", "category", "process").
        /// </value>
        public required string Type { get; init; }


        /// <summary>
        /// Gets the optional human-readable title of the concept.
        /// </summary>
        /// <value>
        /// The user-friendly title text, or <see langword="null"/> if no title is specified.
        /// </value>
        public string? Title { get; init; }


        /// <summary>
        /// Gets the optional brief description or summary of the concept.
        /// </summary>
        /// <value>
        /// A short descriptive summary, or <see langword="null"/> if no description is specified.
        /// </value>
        public string? Description { get; init; }


        /// <summary>
        /// Gets the optional external resource URI, URL, or identifier associated with the concept.
        /// </summary>
        /// <value>
        /// A locator referencing external authoritative systems, or <see langword="null"/> if not specified.
        /// </value>
        public string? Resource { get; init; }


        /// <summary>
        /// Gets the list of tags categorizing the concept.
        /// </summary>
        /// <value>
        /// A read-only list of keywords or tags for grouping and indexing.
        /// </value>
        public required IReadOnlyList<string> Tags { get; init; }


        /// <summary>
        /// Gets the optional creation or modification timestamp of the concept.
        /// </summary>
        /// <value>
        /// A <see cref="DateTimeOffset"/> expressing when the concept was recorded, or <see langword="null"/> if omitted.
        /// </value>
        public DateTimeOffset? Timestamp { get; init; }
        // Spec §4.1: "Consumers MUST preserve unknown keys on round-trip"


        /// <summary>
        /// Gets the dictionary of non-standard metadata keys and their values.
        /// </summary>
        /// <remarks>
        /// Spec §4.1: "Consumers MUST preserve unknown keys on round-trip". 
        /// This dictionary stores any non-standard elements encountered during parsing.
        /// </remarks>
        /// <value>
        /// A read-only dictionary containing custom extensions found in the frontmatter.
        /// </value>
        public required IReadOnlyDictionary<string, object> Extensions { get; init; }
    }


    /// <summary>
    /// Represents a parsed semantic link detected within an OKF concept's body.
    /// </summary>
    /// <param name="Text">The visible text label associated with the link.</param>
    /// <param name="Target">The destination target, which can be a concept ID, relative file path, or external URI.</param>
    /// <param name="IsAbsolute">A value indicating whether the <paramref name="Target"/> is an absolute external URI.</param>
    public record OkfLink(string Text, string Target, bool IsAbsolute);
}