using System.Collections;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Smartstore.IconKit;

/// <summary>
/// A deferred provider drawing. File values are relative to the application's content root.
/// </summary>
internal sealed record IconProviderDrawing(string Value, bool IsFile)
{
    // Identity is independent of kit membership. File content revisions are fingerprinted on demand.
    internal string GetName()
        => "svg-" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes((IsFile ? "file:" : "svg:") + Value)));
}

/// <summary>
/// Adapts provider drawings to the existing loose-SVG pipeline without materializing files on disk.
/// </summary>
internal sealed class IconProviderFiles(IReadOnlyDictionary<string, IconProviderDrawing> drawings, IFileProvider appFiles) : IFileProvider
{
    internal const string LibraryName = "icon-provider";
    private const string _directory = "default/icons";

    public IFileInfo GetFileInfo(string subpath)
    {
        var prefix = _directory + "/";
        if (subpath.StartsWith(prefix, StringComparison.Ordinal) && subpath.EndsWith(".svg", StringComparison.Ordinal)
            && drawings.TryGetValue(subpath[prefix.Length..^4], out var drawing))
        {
            return new DrawingFile(Path.GetFileName(subpath), drawing, drawing.IsFile ? appFiles.GetFileInfo(drawing.Value) : null);
        }

        return new NotFoundFileInfo(subpath);
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
        => subpath == _directory ? new DrawingDirectory(this, drawings.Keys) : NotFoundDirectoryContents.Singleton;

    public IChangeToken Watch(string filter) => NullChangeToken.Singleton;

    private sealed class DrawingDirectory(IconProviderFiles files, IEnumerable<string> names) : IDirectoryContents
    {
        public bool Exists => true;

        public IEnumerator<IFileInfo> GetEnumerator()
            => names.Select(x => files.GetFileInfo(_directory + "/" + x + ".svg")).Where(x => x.Exists).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private sealed class DrawingFile(string name, IconProviderDrawing drawing, IFileInfo file) : IFileInfo
    {
        public bool Exists => !drawing.IsFile || file.Exists && !file.IsDirectory;

        public long Length => drawing.IsFile ? file.Length : Encoding.UTF8.GetByteCount(drawing.Value);

        public string PhysicalPath => file?.PhysicalPath;

        public string Name => name;

        public DateTimeOffset LastModified => file?.LastModified ?? DateTimeOffset.MinValue;

        public bool IsDirectory => false;

        public Stream CreateReadStream()
            => drawing.IsFile ? file.CreateReadStream() : new MemoryStream(Encoding.UTF8.GetBytes(drawing.Value), writable: false);
    }
}
