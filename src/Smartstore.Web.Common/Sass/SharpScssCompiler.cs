#nullable enable

using Microsoft.Extensions.FileProviders;
using SharpScss;
using Smartstore.Engine.Modularity;
using Smartstore.IO;

namespace Smartstore.Web.Sass;

internal sealed class SharpScssCompiler : ISassCompiler
{
    public Task<SassCompilationResult> CompileAsync(SassCompilationRequest request, CancellationToken cancellationToken = default)
    {
        var fileProvider = request.FileProvider;
        var assetFileProvider = fileProvider as IAssetFileProvider;
        var sassOptions = new ScssOptions
        {
            InputFile = assetFileProvider != null
                ? NormalizeSlash(request.SourcePath)
                : fileProvider.GetFileInfo(request.SourcePath).PhysicalPath,

            OutputStyle = request.Minify
                ? ScssOutputStyle.Compressed
                : ScssOutputStyle.Nested
        };

        if (assetFileProvider != null)
        {
            var importedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            sassOptions.TryImport ??= (ref string file, string parentPath, out string scss, out string? map) =>
            {
                return OnTryImportSassFile(assetFileProvider, importedFiles, ref file, parentPath, out scss, out map);
            };
        }

        // We pass the content (data) + input file (virtual),
        // so that relative imports are calculated correctly.
        var result = Scss.ConvertToCss(request.Source, sassOptions);

        return Task.FromResult(new SassCompilationResult
        {
            Css = result.Css,
            IncludedFiles = result.IncludedFiles!
        });
    }

    private static bool OnTryImportSassFile(
        IAssetFileProvider fileProvider,
        ISet<string> importedFiles,
        ref string file,
        string parentPath,
        out string scss,
        out string? map)
    {
        // Defaults
        map = null;
        scss = null!;

        // Normalize requested and parent
        var parentNorm = NormalizeSlash(parentPath);
        var requestedRaw = NormalizeSlash(file);

        // Decide base path: absolute import ignores parent, relative uses parent's dir
        var isAbsolute = requestedRaw.StartsWith('/');
        var basePath = isAbsolute ? string.Empty : GetParentDirectory(parentNorm);

        // Always work with ".scss"
        var requestedWithExt = requestedRaw.EnsureEndsWith(".scss");

        // Build candidates with a *stable* preference:
        // 1) partial (_name.scss), 2) direct (name.scss), 3) dir index (index.scss), 4) partial index (_index.scss)
        static IEnumerable<string> EnumerateCandidates(string basePath, string requestedWithExt)
        {
            var slashIndex = requestedWithExt.LastIndexOf('/');
            var partial = slashIndex >= 0
                ? requestedWithExt.Substring(0, slashIndex + 1) + "_" + requestedWithExt[(slashIndex + 1)..]
                : "_" + requestedWithExt;

            // 1) prefer partial: _name.scss
            yield return PathUtility.Combine(basePath, partial)!;

            // 2) direct: name.scss
            yield return PathUtility.Combine(basePath, requestedWithExt)!;
        }

        IFileInfo? importFile = null;
        string? canonicalSubPath = null;

        foreach (var sub in EnumerateCandidates(basePath, requestedWithExt))
        {
            var subPath = NormalizeSlash(sub);
            var fi = fileProvider.GetFileInfo(subPath);
            if (!fi.Exists) continue;

            importFile = fi;
            canonicalSubPath = subPath;   // what we will report back to Sass for sourcemaps
            break;
        }

        // Not found in our provider -> real miss: allow other importers
        if (importFile == null || !importFile.Exists || canonicalSubPath == null)
        {
            return false;
        }

        // Stable dedupe key: prefer PhysicalPath; fallback to canonicalSubPath
        var key = ComputeImportKey(importFile.PhysicalPath, canonicalSubPath);

        // IMPORTANT:
        //  - Never return empty SCSS; that triggers a fallback load and duplicates.
        //  - Also avoid re-setting 'file' here; keep it as-is to prevent any resolution quirks.
        if (importedFiles.Contains(key))
        {
            // No-op SCSS comment so the import is considered satisfied.
            scss = "/* deduped: " + canonicalSubPath + " */\n";
            // Do NOT change 'file' on dedupe (leave whatever the caller passed in).
            return true;
        }

        // Load content from our provider; LibSass will NOT load it again.
        using (var stream = importFile.CreateReadStream())
        {
            scss = stream.AsString();
        }

        importedFiles.Add(key);

        // Report canonical path so subsequent relatives resolve consistently
        file = canonicalSubPath;

        return true;
    }

    private static string NormalizeSlash(string path)
        => path.Replace('\\', '/');

    private static string GetParentDirectory(string importerPath)
    {
        // Return directory with trailing slash. Works with virtual paths too.
        var dir = Path.GetDirectoryName(importerPath)?.Replace('\\', '/') ?? string.Empty;
        return dir.EnsureEndsWith('/');
    }

    private static string ComputeImportKey(string? physicalPath, string canonicalSubPath)
    {
        // Use physical if available; else use canonical virtual path.
        var key = string.IsNullOrEmpty(physicalPath) ? canonicalSubPath : physicalPath;
        key = key.Replace('\\', '/');
        return key;
    }
}
