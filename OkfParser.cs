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


    public static class OkfParser
    {
        /// <summary>
        /// Asynchronously parses an OKF Knowledge Bundle into memory safely and concurrently.
        /// </summary>
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