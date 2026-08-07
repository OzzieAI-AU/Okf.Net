
# Contributing to Okf.Net

First off, thank you for considering contributing to Okf.Net! We welcome pull requests, bug reports, and feature ideas from the community.

![Contributing to Okf.Net](Images/img4.png)

## Getting Involved

Before writing any code, we highly recommend connecting with the community:
* **Join the discussion:** [https://forum.ozzieai.com/](https://forum.ozzieai.com/)
* **Project Home:** [https://www.ozzieai.com/](https://www.ozzieai.com/)

Discussing your proposed changes in the forum ensures that your work aligns with the project's roadmap and OKF specification.

## Understanding the Architecture

When modifying the codebase, please keep the following architectural decisions in mind:

* **Concurrency**: The parser uses `ConcurrentDictionary` and `Parallel.ForEachAsync` to load bundles safely across multiple threads[cite: 5]. Please ensure any new collections or parsing states remain thread-safe.
* **Dependencies**: We rely on `Markdig` for building the Markdown AST and `YamlDotNet` for deserializing YAML frontmatter[cite: 5]. 
* **Link Extraction**: Do not use Regex for extracting markdown links. We traverse the `Markdig` AST to pull `LinkInline` elements to prevent parsing errors inside literal code blocks[cite: 5].

![Okf.Net Architecture](Images/img5.png)

## Error Handling

When parsing OKF documents, follow the established exception patterns:
* Throw an `OkfParseException` if the YAML frontmatter block is missing or unterminated[cite: 5].
* Throw an `OkfParseException` if the required `type` field is missing from the frontmatter[cite: 5].
* The `OkfParseException` class includes a `FilePath` property to help users pinpoint exactly which document failed to parse[cite: 4].

![Okf.Net Community](Images/img6.png)

## License Agreement

By contributing to Okf.Net, you agree that any Contribution intentionally submitted for inclusion in the Work shall be under the terms and conditions of the Apache License, Version 2.0, without any additional terms or conditions[cite: 2]. 

Unless you explicitly state otherwise, you grant a perpetual, worldwide, non-exclusive, royalty-free, irrevocable copyright license to reproduce, prepare Derivative Works of, publicly display, and distribute the Work and your Contributions[cite: 2].

---