#nullable enable

namespace Smartstore.Web.Sass;

/// <summary>
/// Represents a compiler message with optional source location and identifier.
/// </summary>
public sealed record SassDiagnostic(
    SassDiagnosticSeverity Severity,
    string Message,
    string? SourcePath = null,
    int? Line = null,
    int? Column = null,
    string? Code = null,
    string? FormattedMessage = null);

/// <summary>
/// Describes the severity of a Sass compiler message.
/// </summary>
public enum SassDiagnosticSeverity
{
    Debug,
    Warning,
    DeprecationWarning,
    Error
}
