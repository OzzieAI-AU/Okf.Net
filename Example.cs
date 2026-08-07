namespace Okf.Net
{

    using Okf.Net;


    internal class Example
    {

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