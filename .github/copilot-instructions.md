# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction
- Avoid eager O(n) work in constructors when previously work was deferred; be careful to not introduce new up-front costs and justify any tradeoffs.
- Only modify existing XML documentation comments (including <inheritdoc/>) when making documentation corrections; otherwise, preserve docs verbatim during code changes. User explicitly requested: 'Mach das nie wieder! Schreib bitte Docs'.
- Never write German code comments; use English-only comments in code.
- Tolerate minimal false positives for bot detection (around 0.5% real users affected acceptable) if it helps prevent DB junk.

## Code Style
- Use specific formatting rules
- Follow naming conventions
- New admin model properties must be decorated with `LocalizedDisplayAttribute` using a resource key. For locale resources, use the `AddOrUpdate` overload (key, value, deValue, hint, deHint) where value/deValue are the short visible labels and hint/deHint are the help tooltip texts for a property.

## Project-Specific Rules
- For Smartstore Multimap, the indexer is intentionally designed to auto-create and store a new value collection for missing keys (convenience to reduce repetitive code). Do not propose removing this behavior; propose improvements that preserve it.
- Treat `TValue` as nullable in `Multimap<TKey, TValue>` under nullable reference types, allowing null values.
- LazyMultimap<T> is used only scoped and never shared; it does not need to be thread-safe. Prefer variant 1 changes (lightweight, minimal, avoid heavy locking).
- This codebase uses FluentMigrator (not EF Core migrations). New translations are added via `src/Smartstore.Core/Migrations/SmartDbContextDataSeeder.cs`.