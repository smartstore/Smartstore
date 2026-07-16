# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction
- Avoid eager O(n) work in constructors when previously work was deferred; be careful to not introduce new up-front costs and justify any tradeoffs.
- Only modify existing XML documentation comments (including <inheritdoc/>) when making documentation corrections; otherwise, preserve docs verbatim during code changes. User explicitly requested: 'Mach das nie wieder! Schreib bitte Docs'.
- Never write German code comments; use English-only comments in code.
- Tolerate minimal false positives for bot detection (around 0.5% real users affected acceptable) if it helps prevent DB junk.
- Account for dynamic asset pipelines; do not assume static stylesheet links when CSS is generated dynamically. Prefer referencing generated asset manifests, pipeline helper functions, or runtime-injected links.

### AI Model Metadata
- In AI model metadata, levels mean 0 = Instant, 1 = Balanced, 2 = Deep Reasoning.
- Prefer common, cost-efficient text-generation models; ensure at least one preferred level-0 (Instant) model is listed.
- Prioritize models optimized for text and image generation when relevant.
- Never mark deep-reasoning (level 2) models as preferred; they may remain available but must not be the default choice.
- Remove nonexistent or deprecated model IDs instead of keeping placeholders.
- When answering questions about AI model capabilities, prefer official API documentation and vendor capabilities over local project metadata mirrors; do not infer model behavior solely from the project's metadata.

## Code Style
- Use specific formatting rules
- Follow naming conventions
- Enable nullable reference types explicitly at file level in new or modified C# files using `#nullable enable`, primarily in interfaces/contracts for Intellisense, but only if really necessary.
- New admin model properties must be decorated with `LocalizedDisplayAttribute` using a resource key. For locale resources, use the `AddOrUpdate` overload (key, value, deValue, hint, deHint) where value/deValue are the short visible labels and hint/deHint are the help tooltip texts for a property.
- Prefer existing utilities like `Smartstore.Utilities.HashCodeCombiner` over ad-hoc hash implementations, provided they cover the use case or can be sensibly extended.

## Project-Specific Rules
- For Smartstore Multimap, the indexer is intentionally designed to auto-create and store a new value collection for missing keys (convenience to reduce repetitive code). Do not propose removing this behavior; propose improvements that preserve it.
- Treat `TValue` as nullable in `Multimap<TKey, TValue>` under nullable reference types, allowing null values.
- LazyMultimap<T> is used only scoped and never shared; it does not need to be thread-safe. Prefer variant 1 changes (lightweight, minimal, avoid heavy locking).
- This codebase uses FluentMigrator (not EF Core migrations). New translations are added via `src/Smartstore.Core/Migrations/SmartDbContextDataSeeder.cs`.
- User prefers .NET naming conventions where acronyms of 2+ letters are fully capitalized (e.g., IPAddress, HTTP, XML) following BCL standards like System.Net.IPAddress.