#nullable enable

using System.Collections.Concurrent;
using EmbeddedSass;
using EmbeddedSass.Compiler;
using EmbeddedSass.Diagnostics;
using EmbeddedSass.Importing;
using Microsoft.Extensions.FileProviders;
using Smartstore.Engine.Modularity;

namespace Smartstore.Web.Sass;

/// <summary>
/// Compiles Sass through EmbeddedSass.Net using its bundled native Dart Sass executable.
/// This avoids a JavaScript engine and runtime downloads when bundles are compiled early in application startup.
/// </summary>
internal sealed class DartSassCompiler : ISassCompiler, IAsyncDisposable
{
    private const string VirtualScheme = "smsass";
    private static readonly string[] RequestScopedImports = ["/.app/themevars.scss", "/.app/moduleimports.scss"];

    // Keep one Embedded Sass connection for all compilations. The compiler starts its native
    // process on demand and reuses it; the bundled executable is resolved from the app output.
    private readonly EmbeddedSass.SassCompiler _compiler = new(new SassCompilerOptions().UseBundledDartSass());

    public async Task<SassCompilationResult> CompileAsync(SassCompilationRequest request, CancellationToken cancellationToken = default)
    {
        Guard.NotNull(request);
        Guard.NotNull(request.FileProvider);

        // Importer callbacks may arrive asynchronously, so diagnostics are collected per
        // compilation rather than stored on the shared compiler instance.
        var diagnostics = new ConcurrentQueue<SassDiagnostic>();

        // Asset paths may resolve to physical files, but must still go through Smartstore's
        // provider: it also serves virtual imports such as /.app/themevars.scss.
        var physicalPath = request.FileProvider is IAssetFileProvider
            ? null
            : request.FileProvider.GetFileInfo(request.SourcePath).PhysicalPath;
        SassStringInput input;
        SassCompileRequest compileRequest;

        if (string.IsNullOrEmpty(physicalPath))
        {
            // The input importer resolves paths relative to this in-memory source; registering
            // it globally also covers root-relative imports such as /shared/variables.
            var sourceUrl = ToVirtualUrl(request.SourcePath);
            var importer = new FileProviderImporter(request.FileProvider, SnapshotRequestScopedImports(request));
            input = new SassStringInput(request.Source, Url: sourceUrl) { Importer = importer };
            compileRequest = new SassCompileRequest(input) { Importers = [importer] };
        }
        else
        {
            // Compile the supplied source text, not the file on disk: bundling may have changed
            // it already. A file URL and load path retain normal relative filesystem imports.
            var sourcePath = Path.GetFullPath(physicalPath);
            input = new SassStringInput(request.Source, Url: new Uri(sourcePath));
            compileRequest = new SassCompileRequest(input)
            {
                LoadPaths = [Path.GetDirectoryName(sourcePath)!]
            };
        }

        compileRequest = compileRequest with
        {
            OutputStyle = request.Minify ? SassOutputStyle.Compressed : SassOutputStyle.Expanded,
            UseAsciiDiagnostics = true,
            LogHandler = (logEvent, _) =>
            {
                diagnostics.Enqueue(ToDiagnostic(logEvent));
                return ValueTask.CompletedTask;
            }
        };

        SassCompileResult result;
        try
        {
            result = await _compiler.CompileAsync(compileRequest, cancellationToken);
        }
        catch (EmbeddedSass.Diagnostics.SassCompilationException ex)
        {
            // Keep Sass's complete diagnostic, including the source excerpt and import
            // chain. Only replace the internal URL scheme with the virtual asset path.
            var span = ex.Span;
            var formattedMessage = NormalizeFormattedMessage(ex.FormattedMessage);
            var diagnostic = new SassDiagnostic(
                SassDiagnosticSeverity.Error,
                ex.Message,
                ToSourcePath(span?.Url) ?? request.SourcePath,
                span is null ? null : (int)span.Start.Line + 1,
                span is null ? null : (int)span.Start.Column + 1,
                FormattedMessage: formattedMessage);

            throw new SassCompilationException(diagnostic, formattedMessage, ex);
        }

        // The root input may appear in LoadedUrls; only imported files are dependencies for
        // bundle invalidation. Translate virtual URLs back to paths understood by IFileProvider.
        var includedFiles = result.LoadedUrls
            .Where(url => url != input.Url)
            .Select(url => url.Scheme == VirtualScheme ? Uri.UnescapeDataString(url.AbsolutePath) : url.LocalPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new SassCompilationResult
        {
            Css = result.Css,
            IncludedFiles = includedFiles,
            Diagnostics = diagnostics.ToArray()
        };
    }

    public ValueTask DisposeAsync() => _compiler.DisposeAsync();

    private static SassDiagnostic ToDiagnostic(SassLogEvent logEvent)
    {
        // Embedded Sass reports zero-based positions; the public diagnostic uses one-based
        // line and column numbers, matching editor locations.
        var span = logEvent.Span;
        var sourcePath = ToSourcePath(span?.Url);

        return new SassDiagnostic(
            logEvent.Level switch
            {
                SassLogLevel.Warning => SassDiagnosticSeverity.Warning,
                SassLogLevel.DeprecationWarning => SassDiagnosticSeverity.DeprecationWarning,
                SassLogLevel.Debug => SassDiagnosticSeverity.Debug,
                _ => SassDiagnosticSeverity.Warning
            },
            logEvent.Message,
            sourcePath,
            span is null ? null : (int)span.Start.Line + 1,
            span is null ? null : (int)span.Start.Column + 1,
            logEvent.DeprecationId,
            NormalizeFormattedMessage(logEvent.FormattedMessage));
    }

    // A dedicated scheme gives virtual assets stable canonical identities without
    // pretending that their paths exist on the physical filesystem.
    private static Uri ToVirtualUrl(string path)
        => new($"{VirtualScheme}:///{path.Replace('\\', '/').TrimStart('/')}");

    private static string? ToSourcePath(Uri? url)
        => url is null
            ? null
            : url.Scheme == VirtualScheme ? Uri.UnescapeDataString(url.AbsolutePath) : url.LocalPath;

    private static string NormalizeFormattedMessage(string message)
        => message.Replace($"{VirtualScheme}://", string.Empty, StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyDictionary<string, string> SnapshotRequestScopedImports(SassCompilationRequest request)
    {
        var imports = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Embedded Sass invokes importer callbacks from its long-lived protocol reader,
        // not necessarily in this HTTP request's ExecutionContext. Materialize only the
        // virtual files used by this source while the request and bundle scopes are active.
        foreach (var path in RequestScopedImports)
        {
            if (!request.Source.Contains(path, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var file = request.FileProvider.GetFileInfo(path);
            if (file.Exists)
            {
                using var stream = file.CreateReadStream();
                imports.Add(path, stream.AsString());
            }
        }

        return imports;
    }

    private sealed class FileProviderImporter(IFileProvider fileProvider, IReadOnlyDictionary<string, string> requestScopedImports) : ISassContentImporter
    {
        public ValueTask<SassCanonicalizeResult?> CanonicalizeAsync(
            SassCanonicalizeContext context,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Sass resolves each import against its containing stylesheet. Only URLs in our
            // scheme belong to this importer; other schemes remain available to Sass itself.
            Uri url;
            if (context.Url.IsAbsoluteUri)
            {
                if (context.Url.Scheme != VirtualScheme)
                {
                    return ValueTask.FromResult<SassCanonicalizeResult?>(null);
                }

                url = context.Url;
            }
            else
            {
                var containingUrl = context.ContainingUrl is { Scheme: VirtualScheme } containing
                    ? containing
                    : ToVirtualUrl("/");
                url = new Uri(containingUrl, context.Url);
            }

            var path = Uri.UnescapeDataString(url.AbsolutePath);

            // Return the concrete partial or stylesheet path as the canonical URL. This lets
            // Sass resolve imports inside that file relative to its actual location.
            foreach (var candidate in GetCandidates(path))
            {
                if (requestScopedImports.ContainsKey(candidate) || fileProvider.GetFileInfo(candidate).Exists)
                {
                    return ValueTask.FromResult<SassCanonicalizeResult?>(new(ToVirtualUrl(candidate)));
                }
            }

            return ValueTask.FromResult<SassCanonicalizeResult?>(null);
        }

        public ValueTask<SassImportResult?> LoadAsync(Uri canonicalUrl, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (canonicalUrl.Scheme != VirtualScheme)
            {
                return ValueTask.FromResult<SassImportResult?>(null);
            }

            var path = Uri.UnescapeDataString(canonicalUrl.AbsolutePath);
            if (requestScopedImports.TryGetValue(path, out var content))
            {
                return ValueTask.FromResult<SassImportResult?>(new(content, SassSyntax.Scss));
            }

            var fileInfo = fileProvider.GetFileInfo(path);
            if (!fileInfo.Exists)
            {
                return ValueTask.FromResult<SassImportResult?>(null);
            }

            using var stream = fileInfo.CreateReadStream();
            // Explicit .sass files use indentation syntax; Smartstore's usual .scss files do not.
            var syntax = canonicalUrl.AbsolutePath.EndsWith(".sass", StringComparison.OrdinalIgnoreCase)
                ? SassSyntax.Indented
                : SassSyntax.Scss;
            return ValueTask.FromResult<SassImportResult?>(new(stream.AsString(), syntax));
        }

        private static IEnumerable<string> GetCandidates(string path)
        {
            // Prefer partials over direct files, and accept directory index files when
            // the import has no extension.
            var extension = Path.GetExtension(path);
            var hasSassExtension = extension.Equals(".scss", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".sass", StringComparison.OrdinalIgnoreCase);
            var basePath = hasSassExtension ? path : path + ".scss";
            var slash = basePath.LastIndexOf('/');
            var partial = basePath.Insert(slash + 1, "_");

            yield return partial;
            yield return basePath;

            if (!hasSassExtension)
            {
                yield return path.TrimEnd('/') + "/_index.scss";
                yield return path.TrimEnd('/') + "/index.scss";
            }
        }
    }
}
