#nullable enable

namespace Smartstore.Web.Sass;

/// <summary>
/// Represents a Sass compilation error with its source location and formatted compiler output.
/// </summary>
public sealed class SassCompilationException : Exception
{
    /// <summary>
    /// Creates a Sass compilation exception while preserving the provider's original exception.
    /// </summary>
    public SassCompilationException(SassDiagnostic diagnostic, string message, Exception innerException)
        : base(message, innerException)
    {
        Diagnostic = Guard.NotNull(diagnostic);
    }

    /// <summary>
    /// Gets the structured error diagnostic.
    /// </summary>
    public SassDiagnostic Diagnostic { get; }
}
