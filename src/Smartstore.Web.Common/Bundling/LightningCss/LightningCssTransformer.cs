#nullable enable

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Smartstore.Web.Bundling.LightningCss;

/// <summary>
/// Runs the native Lightning CSS CLI shipped with Smartstore.LightningCss.Native.
/// This class only handles process execution; bundle integration belongs to a bundle processor.
/// </summary>
public sealed class LightningCssCli
{
    private readonly string? _executablePath;

    /// <summary>Creates a CLI wrapper that resolves the executable from the application output directory.</summary>
    public LightningCssCli()
    {
    }

    internal LightningCssCli(string executablePath)
    {
        _executablePath = executablePath;
    }

    /// <summary>Gets or sets the logger used for process diagnostics.</summary>
    public ILogger Logger { get; set; } = NullLogger.Instance;

    /// <summary>
    /// Transforms CSS passed to the CLI through standard input.
    /// </summary>
    /// <param name="css">CSS source text.</param>
    /// <param name="options">CLI options. When omitted, the CLI performs a non-minifying transform.</param>
    /// <param name="cancelToken">Cancels the invocation and stops the process.</param>
    /// <returns>Standard output and non-fatal diagnostics.</returns>
    public Task<LightningCssResult> TransformAsync(
        string css,
        LightningCssOptions? options = null,
        CancellationToken cancelToken = default)
    {
        Guard.NotNull(css);
        return RunAsync(css, [], options ?? new LightningCssOptions(), cancelToken);
    }

    /// <summary>
    /// Transforms one or more CSS files. File mode supports import bundling and directory output.
    /// </summary>
    /// <param name="inputFiles">Input file paths, absolute or relative to the working directory.</param>
    /// <param name="options">CLI options.</param>
    /// <param name="cancelToken">Cancels the invocation and stops the process.</param>
    /// <returns>Standard output and non-fatal diagnostics.</returns>
    public Task<LightningCssResult> TransformFilesAsync(
        IEnumerable<string> inputFiles,
        LightningCssOptions? options = null,
        CancellationToken cancelToken = default)
    {
        Guard.NotNull(inputFiles);

        var files = inputFiles.ToArray();
        if (files.Length == 0 || files.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("At least one non-empty input file is required.", nameof(inputFiles));
        }

        return RunAsync(null, files, options ?? new LightningCssOptions(), cancelToken);
    }

    private async Task<LightningCssResult> RunAsync(
        string? css,
        string[] inputFiles,
        LightningCssOptions options,
        CancellationToken cancelToken)
    {
        ValidateOptions(css, inputFiles, options);
        cancelToken.ThrowIfCancellationRequested();

        var executablePath = _executablePath ?? GetExecutablePath();
        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException(
                $"Lightning CSS CLI was not found at '{executablePath}'. Install Smartstore.LightningCss.Native in the host project and build for a supported runtime.",
                executablePath);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = options.WorkingDirectory ?? AppContext.BaseDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardInput = css is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardInputEncoding = css is not null ? new UTF8Encoding(false) : null,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        AddArguments(startInfo.ArgumentList, inputFiles, options);
        Logger.Debug($"Starting Lightning CSS process '{executablePath}'.");

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        // Drain both output streams concurrently so a full pipe cannot block the child process.
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancelToken);
        timeout.CancelAfter(options.Timeout);

        try
        {
            if (css is not null)
            {
                try
                {
                    await process.StandardInput.WriteAsync(css.AsMemory(), timeout.Token);
                }
                catch (IOException)
                {
                    // The CLI may reject an option and close stdin before consuming all CSS.
                    // Its exit code and stderr provide the useful error in that case.
                }
                finally
                {
                    process.StandardInput.Close();
                }
            }

            await process.WaitForExitAsync(timeout.Token);
            var output = await outputTask;
            var diagnostics = await errorTask;

            if (process.ExitCode != 0)
            {
                throw new LightningCssException(process.ExitCode, diagnostics);
            }

            return new LightningCssResult(output, diagnostics);
        }
        catch (OperationCanceledException) when (!cancelToken.IsCancellationRequested)
        {
            throw new TimeoutException($"Lightning CSS did not finish within {options.Timeout}.");
        }
        finally
        {
            try
            {
                if (!process.HasExited)
                {
                    // WaitForExitAsync cancellation does not terminate the process by itself.
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // The process may exit between HasExited and Kill.
            }
        }
    }

    private static string GetExecutablePath()
    {
        if (RuntimeInformation.ProcessArchitecture != Architecture.X64)
        {
            throw new PlatformNotSupportedException("The Lightning CSS package contains x64 executables only.");
        }

        var rid = OperatingSystem.IsWindows() ? "win-x64"
            : OperatingSystem.IsLinux() ? "linux-x64"
            : OperatingSystem.IsMacOS() ? "osx-x64"
            : throw new PlatformNotSupportedException("No Lightning CSS executable is packaged for this operating system.");

        var fileName = OperatingSystem.IsWindows() ? "lightningcss.exe" : "lightningcss";
        return Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native", fileName);
    }

    private static void ValidateOptions(string? css, string[] inputFiles, LightningCssOptions options)
    {
        if (options.Timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "The CLI timeout must be greater than zero.");
        }
        if (options.UseBrowserslist && !string.IsNullOrWhiteSpace(options.Targets))
        {
            throw new ArgumentException("Targets and Browserslist discovery cannot be used together.", nameof(options));
        }
        if (options.OutputFile is not null && options.OutputDirectory is not null)
        {
            throw new ArgumentException("OutputFile and OutputDirectory cannot be used together.", nameof(options));
        }
        if (options.SourceMap && options.OutputFile is null)
        {
            throw new ArgumentException("SourceMap requires OutputFile.", nameof(options));
        }
        if (css is not null && (options.Bundle || options.OutputDirectory is not null))
        {
            throw new ArgumentException("Bundling and directory output require file-based input.", nameof(options));
        }
        if (inputFiles.Length > 1 && options.OutputFile is not null)
        {
            throw new ArgumentException("OutputFile can only be used with one input file.", nameof(options));
        }
        if (!options.CssModules &&
            (options.CssModulesOutputFile is not null || options.CssModulesPattern is not null || options.CssModulesDashedIdents))
        {
            throw new ArgumentException("CSS Modules options require CssModules to be enabled.", nameof(options));
        }
    }

    private static void AddArguments(
        ICollection<string> args,
        string[] inputFiles,
        LightningCssOptions options)
    {
        if (options.Minify) args.Add("--minify");
        if (options.Bundle) args.Add("--bundle");
        if (options.UseBrowserslist) args.Add("--browserslist");
        if (options.ErrorRecovery) args.Add("--error-recovery");
        if (options.CustomMedia) args.Add("--custom-media");
        if (options.ScrollNavigationControls) args.Add("--scroll-navigation-controls");
        if (options.SourceMap) args.Add("--sourcemap");
        if (options.CssModulesDashedIdents) args.Add("--css-modules-dashed-idents");

        AddValue("--targets", options.Targets);
        AddValue("--output-file", options.OutputFile);
        AddValue("--output-dir", options.OutputDirectory);
        AddValue("--css-modules-pattern", options.CssModulesPattern);

        foreach (var file in inputFiles)
        {
            args.Add(file);
        }

        if (options.CssModules)
        {
            // The optional value must not accidentally consume the first input filename.
            args.Add(options.CssModulesOutputFile is null
                ? "--css-modules"
                : "--css-modules=" + options.CssModulesOutputFile);
        }

        void AddValue(string name, string? value)
        {
            if (value is not null)
            {
                args.Add(name);
                args.Add(value);
            }
        }
    }
}
