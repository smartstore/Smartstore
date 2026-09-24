#nullable enable

using Microsoft.Extensions.FileProviders;

namespace Smartstore.Web.Sass;

/// <summary>
/// Describes one Sass compilation.
/// </summary>
public sealed class SassCompilationRequest
{
    /// <summary>
    /// Gets the Sass source text.
    /// </summary>
    public required string Source { get; init; }

    /// <summary>
    /// Gets the path of the source file, used to resolve relative imports.
    /// </summary>
    public required string SourcePath { get; init; }

    /// <summary>
    /// Gets the provider used to access Sass source files.
    /// </summary>
    public required IFileProvider FileProvider { get; init; }

    /// <summary>
    /// Gets whether the compiler should produce compressed CSS.
    /// </summary>
    public bool Minify { get; init; }
}
