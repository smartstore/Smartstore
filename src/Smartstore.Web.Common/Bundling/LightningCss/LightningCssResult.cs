#nullable enable

namespace Smartstore.Web.Bundling.LightningCss;

/// <summary>
/// Output of a successful Lightning CSS CLI invocation.
/// </summary>
/// <param name="Output">Standard output, usually the transformed CSS when no output file is specified.</param>
/// <param name="Diagnostics">Standard error, which may contain warnings even when the process succeeds.</param>
public sealed record LightningCssResult(string Output, string Diagnostics);
