---
name: smartstore-argument-validation
description: Apply Smartstore's Guard conventions when writing or reviewing C# argument validation in the Smartstore platform or its modules.
---

# Smartstore argument validation

Use the existing `Smartstore.Guard` helpers for argument validation in Smartstore code. Never introduce `ArgumentNullException.ThrowIfNull`, `ArgumentException.ThrowIfNullOrEmpty`, `ArgumentException.ThrowIfNullOrWhiteSpace`, or other `ThrowIf*` helpers on argument exception types, including `ArgumentOutOfRangeException`.

Inspect `src/Smartstore/Common/Guard.cs` in the platform checkout when choosing an overload. The existing helpers include:

- `Guard.NotNull(value)` for null checks. The generic overload returns the non-null value and carries the nullability annotation.
- `Guard.NotEmpty(text)` for strings that must not be null, empty, or whitespace.
- `Guard.NotNegative(value)` when zero is allowed, and `Guard.IsPositive(value)` when the value must be greater than zero.
- `Guard.InRange(value, min, max)` for an inclusive numeric range.

These helpers infer the argument name with `CallerArgumentExpression`; omit a redundant `nameof` argument unless a different public parameter name is required. Preserve validation order so dependent expressions are safe, for example validate a buffer before accessing its length and validate an offset before calculating remaining capacity.

If no existing helper expresses the required contract, use an explicit guard clause with the appropriate argument exception rather than an exception-type `ThrowIf*` helper. Do not weaken validation or add a new Guard API merely to avoid a direct throw.

During a review, flag violations of this convention separately from functional defects. Apply corrections only when implementation changes are authorized; do not turn a focused change into a repository-wide cleanup.
