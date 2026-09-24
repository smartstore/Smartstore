#nullable enable

namespace Smartstore.Web.Bundling.LightningCss;

/// <summary>
/// Options supported by the Lightning CSS command-line tool.
/// </summary>
public sealed class LightningCssOptions
{
    /// <summary>Minifies the generated CSS.</summary>
    public bool Minify { get; set; }

    /// <summary>A Browserslist query used as the browser target (for example, <c>last 2 versions</c>).</summary>
    public string? Targets { get; set; }

    /// <summary>Loads browser targets from a Browserslist configuration in the working directory.</summary>
    public bool UseBrowserslist { get; set; }

    /// <summary>Resolves CSS imports and bundles file-based input.</summary>
    public bool Bundle { get; set; }

    /// <summary>Ignores invalid rules and declarations where the CLI can recover.</summary>
    public bool ErrorRecovery { get; set; }

    /// <summary>Enables parsing of custom media queries.</summary>
    public bool CustomMedia { get; set; }

    /// <summary>Enables parsing of scroll navigation controls.</summary>
    public bool ScrollNavigationControls { get; set; }

    /// <summary>Writes a source map next to <see cref="OutputFile"/>.</summary>
    public bool SourceMap { get; set; }

    /// <summary>Enables CSS Modules and emits export metadata.</summary>
    public bool CssModules { get; set; }

    /// <summary>Optional CSS Modules export JSON path.</summary>
    public string? CssModulesOutputFile { get; set; }

    /// <summary>Optional CSS Modules name pattern.</summary>
    public string? CssModulesPattern { get; set; }

    /// <summary>Scopes dashed identifiers when CSS Modules are enabled.</summary>
    public bool CssModulesDashedIdents { get; set; }

    /// <summary>Writes CSS to this file instead of standard output.</summary>
    public string? OutputFile { get; set; }

    /// <summary>Writes file-based results to this directory.</summary>
    public string? OutputDirectory { get; set; }

    /// <summary>Working directory used for relative paths and Browserslist discovery.</summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>Maximum time allowed for one CLI invocation.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
}
