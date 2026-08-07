
# Okf.Net - Open Knowledge Format Parser for .NET

![Okf.Net Overview](Images/img1.png)

Welcome to **Okf.Net**, the premier .NET parser for the Open Knowledge Format (OKF) (see [OKF Spec](https://okf.md/spec/)). 

Okf.Net provides a fast, thread-safe, and asynchronous way to load OKF Knowledge Bundles into memory[cite: 1]. Designed with dependency injection in mind, the parsed objects can be passed cleanly to Singleton DI services[cite: 1].

## Features

* **Asynchronous & Concurrent**: Uses `Parallel.ForEachAsync` to parse knowledge bundles safely and concurrently based on your system's processor count[cite: 5].
* **Robust Markdown Parsing**: Utilizes the Markdig AST to safely extract markdown links, properly ignoring links rendered inside code blocks[cite: 5].
* **Spec Compliant**: Adheres to the OKF specification, including separating reserved files like `index.md` and `log.md` (Spec §3.1)[cite: 5].
* **Extensible Frontmatter**: Parses YAML frontmatter using YamlDotNet and preserves unknown keys on round-trip within an `Extensions` dictionary (Spec §4.1)[cite: 3, 5].

![Okf.Net Features](Images/img2.png)

## Getting Started

To load your knowledge repo, simply use the `OkfParser` class[cite: 1]. Here is an example of how to parse a bundle and read its concepts:

```csharp
using Okf.Net;

internal class Example
{
    private async Task ExampleAsync()
    {
        // Fast, Thread-Safe, Asynchronous load
        var bundle = await OkfParser.LoadBundleAsync("/path/to/your/knowledge_repo"); //[cite: 1]

        Console.WriteLine($"Loaded {bundle.Concepts.Count} Concepts safely."); //[cite: 1]

        // Thread-safe reads (can be passed cleanly to Singleton DI services)
        foreach (var (id, concept) in bundle.Concepts) //[cite: 1]
        {
            Console.WriteLine($"\nConcept ID: {id}"); //[cite: 1]
            Console.WriteLine($"Type: {concept.Frontmatter.Type}"); //[cite: 1]

            if (concept.Frontmatter.Extensions.TryGetValue("owner", out var owner)) //[cite: 1]
            {
                Console.WriteLine($"Custom Extension Owner: {owner}"); //[cite: 1]
            }

            Console.WriteLine($"Found {concept.OutgoingLinks.Count} cross-links."); //[cite: 1]
        }
    }
}

```

## Data Models

The parsed data is mapped to strictly typed C# records:

* **`OkfBundle`**: Represents the complete bundle, including the `RootDirectory`, `Concepts`, `IndexFiles`, and `LogFiles`[cite: 3].
* **`OkfConcept`**: Represents a single concept with its `Id`, `FilePath`, `Frontmatter`, `Body`, and `OutgoingLinks`[cite: 3].
* **`OkfFrontmatter`**: Contains required metadata like `Type`, optional properties like `Title` and `Tags`, and custom data in an `Extensions` dictionary[cite: 3].
* **`OkfLink`**: Represents parsed links with `Text`, `Target`, and a boolean indicating if it `IsAbsolute`[cite: 3].

## Community & Support

* **Project Home:** [https://www.ozzieai.com/](https://www.ozzieai.com/)
* **Join the discussion:** [https://forum.ozzieai.com/](https://forum.ozzieai.com/)

## License

This software is released under the **Apache License, Version 2.0**.

```

---
