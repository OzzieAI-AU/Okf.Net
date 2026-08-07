namespace Okf.Net
{

    using Okf.Net;


    /// <summary>
    /// Serves as an illustrative example class demonstrating how to load and consume 
    /// Open Knowledge Framework (OKF) bundles within the <see cref="Okf.Net"/> namespace.
    /// </summary>
    /// <remarks>
    /// This class provides a practical reference implementation for developers to understand
    /// how to integrate the OKF library, parse repositories, and safely access concept metadata.
    /// </remarks>
    internal class Example
    {


        /// <summary>
        /// Asynchronously loads a knowledge bundle from a specified file path, iterates through the loaded 
        /// concepts, and outputs their metadata, extensions, and relationships to the console.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <remarks>
        /// This method demonstrates performing fast, thread-safe read operations on the parsed bundle structure.
        /// Once parsed, the bundle and its retrieved concepts are immutable and can be safely registered
        /// as singletons within Dependency Injection (DI) containers.
        /// </remarks>
        private async Task ExampleAsync()
        {

            // Fast, Thread-Safe, Asynchronous load
            var bundle = await OkfParser.LoadBundleAsync("/path/to/your/knowledge_repo");

            Console.WriteLine($"Loaded {bundle.Concepts.Count} Concepts safely.");

            // Thread-safe reads (can be passed cleanly to Singleton DI services)
            foreach (var (id, concept) in bundle.Concepts)
            {
                Console.WriteLine($"\nConcept ID: {id}");
                Console.WriteLine($"Type: {concept.Frontmatter.Type}");

                if (concept.Frontmatter.Extensions.TryGetValue("owner", out var owner))
                {
                    Console.WriteLine($"Custom Extension Owner: {owner}");
                }

                Console.WriteLine($"Found {concept.OutgoingLinks.Count} cross-links.");
            }
        }
    }
}