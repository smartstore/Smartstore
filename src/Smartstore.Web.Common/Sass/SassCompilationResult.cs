#nullable enable

namespace Smartstore.Web.Sass;

/// <summary>
/// Contains the output of a successful Sass compilation.
/// </summary>
public sealed class SassCompilationResult
{
    /// <summary>
    /// Gets the compiled CSS.
    /// </summary>
    public required string Css { get; init; }

    /// <summary>
    /// Gets the source files imported during compilation.
    /// </summary>
    public IReadOnlyList<string> IncludedFiles { get; init; } = [];

    /// <summary>
    /// Gets non-fatal compiler messages, when the compiler exposes them.
    /// </summary>
    public IReadOnlyList<SassDiagnostic> Diagnostics { get; init; } = [];
}
