#nullable enable

namespace Smartstore.Web.Sass;

/// <summary>
/// Compiles Sass source into CSS.
/// </summary>
public interface ISassCompiler
{
    /// <summary>
    /// Compiles the specified Sass source.
    /// </summary>
    Task<SassCompilationResult> CompileAsync(SassCompilationRequest request, CancellationToken cancellationToken = default);
}
