namespace Okf.Net
{

    using System.Collections.Concurrent;
    using System.Collections.ObjectModel;
    using System.Text;
    using Markdig;
    using Markdig.Syntax;
    using Markdig.Syntax.Inlines;
    using Okf.Net.Exceptions;
    using Okf.Net.Models;
    using YamlDotNet.Serialization;


    /// <summary>
    /// Provides static methods to parse and load Open Knowledge Format (OKF) Knowledge Bundles 
    /// and individual OKF concept files asynchronously.
    /// </summary>
    /// <remarks>
    /// The parser processes directories containing Markdown files, extracting frontmatter,
    /// content bodies, and outbound links while adhering to the OKF Specification.
    /// </remarks>
    public static class OkfParser
    {


        /// <summary>
        /// Asynchronously parses an OKF Knowledge Bundle into memory safely and concurrently.
        /// </summary>
        /// <param name="rootDirectory">The physical path to the root directory containing the OKF bundle.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous load operation, containing the populated <see cref="OkfBundle"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="rootDirectory"/> is null or whitespace.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the specified <paramref name="rootDirectory"/> does not exist.</exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the <paramref name="cancellationToken"/>.</exception>
        /// <remarks>
        /// This method scans the directory for Markdown (<c>*.md</c>) files recursively. 
        /// It automatically identifies and segregates special reserved files (<c>index.md</c> and <c>log.md</c>) 
        /// from standard concept files according to the OKF Spec §3.1.
        /// </remarks>
        public static async Task<OkfBundle> LoadBundleAsync(string rootDirectory, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
                throw new ArgumentException("Root directory cannot be empty.", nameof(rootDirectory));

            if (!Directory.Exists(rootDirectory))
                throw new DirectoryNotFoundException($"OKF Bundle root not found: {rootDirectory}");

            var normalizedRoot = Path.GetFullPath(rootDirectory);
            var allMarkdownFiles = Directory.EnumerateFiles(normalizedRoot, "*.md", SearchOption.AllDirectories);

            var concepts = new ConcurrentDictionary<string, OkfConcept>();
            var indexFiles = new ConcurrentDictionary<string, string>();
            var logFiles = new ConcurrentDictionary<string, string>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount,
                CancellationToken = cancellationToken
            };

            await Parallel.ForEachAsync(allMarkdownFiles, parallelOptions, async (filePath, ct) =>
            {
                // Normalize path for consistent Concept IDs
                var relativePath = Path.GetRelativePath(normalizedRoot, filePath).Replace('\\', '/');
                var fileName = Path.GetFileName(filePath).ToLowerInvariant();

                // Spec §3.1: Reserved Filenames
                if (fileName == "index.md")
                {
                    var content = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
                    indexFiles.TryAdd(relativePath, content);
                    return;
                }
                if (fileName == "log.md")
                {
                    var content = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);
                    logFiles.TryAdd(relativePath, content);
                    return;
                }

                // Spec §2: Concept ID is the file path minus the .md suffix
                var conceptId = relativePath[..^3];
                var concept = await ParseConceptAsync(filePath, conceptId, ct).ConfigureAwait(false);

                concepts.TryAdd(conceptId, concept);
            }).ConfigureAwait(false);

            return new OkfBundle
            {
                RootDirectory = normalizedRoot,
                Concepts = new ReadOnlyDictionary<string, OkfConcept>(concepts),
                IndexFiles = new ReadOnlyDictionary<string, string>(indexFiles),
                LogFiles = new ReadOnlyDictionary<string, string>(logFiles)
            };
        }


        /// <summary>
        /// Parses an individual OKF concept file asynchronously, separating its frontmatter metadata and Markdown body content.
        /// </summary>
        /// <param name="filePath">The physical path to the concept Markdown file.</param>
        /// <param name="conceptId">The unique identifier of the concept, usually derived from the relative file path.</param>
        /// <param name="ct">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation, containing the parsed <see cref="OkfConcept"/>.</returns>
        /// <exception cref="OkfParseException">
        /// Thrown when the concept file is missing the required YAML frontmatter block or if the frontmatter is unterminated.
        /// </exception>
        /// <exception cref="OperationCanceledException">Thrown when the operation is canceled via the <paramref name="ct"/>.</exception>
        /// <remarks>
        /// In accordance with Spec §4, the YAML frontmatter block must be delimited by <c>---</c> on its own line 
        /// at the very start of the file. The method locates the block boundaries, isolates the YAML configuration, 
        /// extracts outbound links from the body content, and populates the model.
        /// </remarks>
        private static async Task<OkfConcept> ParseConceptAsync(string filePath, string conceptId, CancellationToken ct)
        {

            var content = await File.ReadAllTextAsync(filePath, ct).ConfigureAwait(false);

            // Spec §4: YAML block delimited by `---` on its own line at the start
            if (!content.StartsWith("---\n") && !content.StartsWith("---\r\n"))
                throw new OkfParseException("Missing YAML frontmatter block.", filePath);

            int endOfFrontmatter = content.IndexOf("\n---", 3, StringComparison.Ordinal);
            if (endOfFrontmatter == -1)
                throw new OkfParseException("Unterminated YAML frontmatter.", filePath);

            string yaml = content.Substring(3, endOfFrontmatter - 3);

            // Determine where the body starts
            int bodyStartIndex = endOfFrontmatter + 4;
            if (bodyStartIndex < content.Length && content[bodyStartIndex] == '\r') bodyStartIndex++;
            if (bodyStartIndex < content.Length && content[bodyStartIndex] == '\n') bodyStartIndex++;

            string body = bodyStartIndex < content.Length ? content.Substring(bodyStartIndex) : string.Empty;

            var frontmatter = ParseFrontmatter(yaml, filePath);
            var links = ExtractLinks(body);

            return new OkfConcept
            {
                Id = conceptId,
                FilePath = filePath,
                Frontmatter = frontmatter,
                Body = body,
                OutgoingLinks = links.AsReadOnly()
            };
        }


        /// <summary>
        /// Parses the YAML frontmatter block extracted from a concept Markdown file.
        /// </summary>
        /// <param name="yaml">The raw YAML content extracted from the frontmatter block.</param>
        /// <param name="filePath">The file path being parsed, used for error reporting inside exceptions.</param>
        /// <returns>A populated <see cref="OkfFrontmatter"/> containing standard properties and any schema extensions.</returns>
        /// <exception cref="OkfParseException">
        /// Thrown when the YAML block is malformed, cannot be deserialized, or is missing the required "type" field.
        /// </exception>
        /// <remarks>
        /// This parser strictly verifies the existence of the <c>type</c> parameter. Other standard properties 
        /// such as <c>title</c>, <c>description</c>, <c>resource</c>, <c>tags</c>, and <c>timestamp</c> are extracted and mapped. 
        /// Any non-standard fields are preserved and populated within the <see cref="OkfFrontmatter.Extensions"/> dictionary.
        /// </remarks>
        private static OkfFrontmatter ParseFrontmatter(string yaml, string filePath)
        {

            var deserializer = new DeserializerBuilder().Build();
            Dictionary<string, object> rawDict;

            try
            {
                rawDict = deserializer.Deserialize<Dictionary<string, object>>(yaml) ?? new Dictionary<string, object>();
            }
            catch (Exception ex)
            {
                throw new OkfParseException($"YAML deserialization failed: {ex.Message}", filePath, ex);
            }

            // Spec §4.1: type is REQUIRED. Search case-insensitively just in case.
            var typeKey = rawDict.Keys.FirstOrDefault(k => k.Equals("type", StringComparison.OrdinalIgnoreCase));
            if (typeKey == null || rawDict[typeKey] is not string type)
                throw new OkfParseException("Missing required 'type' field in frontmatter.", filePath);

            var extensions = new Dictionary<string, object>();
            string? title = null, description = null, resource = null;
            var tags = new List<string>();
            DateTimeOffset? timestamp = null;

            foreach (var (key, value) in rawDict)
            {
                switch (key.ToLowerInvariant())
                {
                    case "type":
                        break;
                    case "title":
                        title = value?.ToString(); break;
                    case "description":
                        description = value?.ToString(); break;
                    case "resource":
                        resource = value?.ToString(); break;
                    case "tags":
                        if (value is IEnumerable<object> list)
                            tags.AddRange(list.Select(x => x?.ToString() ?? string.Empty));
                        else if (value is string singleTag) // Safety net for bad YAML
                            tags.Add(singleTag);
                        break;
                    case "timestamp":
                        if (value is string tsStr && DateTimeOffset.TryParse(tsStr, out var ts))
                            timestamp = ts;
                        break;
                    default:
                        // Preserve unknown keys cleanly.
                        extensions[key] = value;
                        break;
                }
            }

            return new OkfFrontmatter
            {
                Type = type,
                Title = title,
                Description = description,
                Resource = resource,
                Tags = tags.AsReadOnly(),
                Timestamp = timestamp,
                Extensions = new ReadOnlyDictionary<string, object>(extensions)
            };
        }


        /// <summary>
        /// Extracts outbound Markdown links from the Markdown body content using the Markdig Abstract Syntax Tree (AST).
        /// </summary>
        /// <param name="body">The Markdown content body to analyze.</param>
        /// <returns>A list of <see cref="OkfLink"/> objects representing standard, non-image links found within the body text.</returns>
        /// <remarks>
        /// In accordance with Spec §5, this method uses a structured Markdig AST parser instead of regular expressions. 
        /// This safely ignores non-semantic links, such as links defined inside raw markdown code blocks (<c></c>).
        /// It processes inline link children to extract full raw link text.
        /// </remarks>
        private static List<OkfLink> ExtractLinks(string body)
        {
            // Spec §5: Extract Markdown Links.
            // We use Markdig AST instead of Regex to safely ignore links rendered inside ```code blocks```.
            var pipeline = new MarkdownPipelineBuilder().Build();
            var document = Markdown.Parse(body, pipeline);
            var links = new List<OkfLink>();

            foreach (var node in document.Descendants())
            {
                if (node is LinkInline linkInline && !linkInline.IsImage && linkInline.Url != null)
                {
                    var target = linkInline.Url;
                    var isAbsolute = target.StartsWith('/');

                    // Traverse inline literal children for the link text
                    var sb = new StringBuilder();
                    foreach (var inline in linkInline)
                    {
                        if (inline is LiteralInline literal)
                            sb.Append(literal.Content);
                    }

                    links.Add(new OkfLink(sb.ToString(), target, isAbsolute));
                }
            }

            return links;
        }
    }
}