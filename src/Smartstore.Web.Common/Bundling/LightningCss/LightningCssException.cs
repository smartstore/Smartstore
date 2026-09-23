#nullable enable

namespace Smartstore.Web.Bundling.LightningCss;

/// <summary>
/// Indicates that the Lightning CSS CLI exited with an error.
/// </summary>
public sealed class LightningCssException : Exception
{
    /// <summary>Creates an exception containing the CLI exit code and diagnostics.</summary>
    public LightningCssException(int exitCode, string diagnostics)
        : base($"Lightning CSS exited with code {exitCode}: {diagnostics.Trim()}")
    {
        ExitCode = exitCode;
        Diagnostics = diagnostics;
    }

    /// <summary>Gets the native process exit code.</summary>
    public int ExitCode { get; }

    /// <summary>Gets the native process standard error output.</summary>
    public string Diagnostics { get; }
}
