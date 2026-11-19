using Microsoft.AspNetCore.Http;
using Smartstore.Engine;
using Smartstore.Threading;
using Smartstore.IO;

namespace Smartstore.IconKit;

/// <summary>
/// Creates icon file systems, validates revision hashes and writes cache files.
/// </summary>
internal static class IconFileHelper
{
    /// <summary>
    /// Opens a relative icon folder, using AppDataRoot for paths starting with App_Data.
    /// </summary>
    internal static IFileSystem CreateFileSystem(IApplicationContext appContext, string path)
    {
        Guard.NotEmpty(path);
        if (Path.IsPathRooted(path) || path.Replace('\\', '/').Split('/').Contains(".."))
        {
            throw new ArgumentException("The icon folder must be relative to the application's content root.", nameof(path));
        }

        var fs = appContext.ContentRoot;
        path = PathUtility.NormalizeRelativePath(path).TrimEnd('/');
        var folder = new PathString("/" + path);
        if (folder.StartsWithSegments("/App_Data", StringComparison.OrdinalIgnoreCase, out var remaining))
        {
            // Preserve any special behavior provided by the application's AppData file system.
            fs = appContext.AppDataRoot;
            path = remaining.Value.TrimStart('/');
        }

        return path.Length == 0 || path == "." ? fs : new ExpandedFileSystem(path, fs);
    }

    /// <summary>
    /// Checks the revision format before request input becomes part of a physical filename.
    /// </summary>
    /// <param name="revision">The requested content fingerprint.</param>
    internal static bool IsValidRevisionHash(string revision)
        => revision is { Length: 24 } && revision.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

    /// <summary>
    /// Returns an existing file or writes it beside the destination before an atomic rename.
    /// Cancellation applies only while waiting for the write lock. Once started, publication completes independently
    /// of the request. Failed writes leave no partial artifact. The writer must reject stale source generations.
    /// </summary>
    /// <param name="file">The destination file in the configured cache file system.</param>
    /// <param name="write">Writes the artifact to the supplied stream without closing it.</param>
    internal static async Task<string> GetOrCreateCacheFileAsync(IFile file, Func<Stream, CancellationToken, ValueTask> write,
        CancellationToken cancelToken)
    {
        var fs = file.FileSystem;
        var destination = file.SubPath;
        var path = file.PhysicalPath;
        if (fs.FileExists(destination))
        {
            return path;
        }

        using (await AsyncLock.KeyedAsync("icons:file:" + path, cancelToken: cancelToken))
        {
            if (fs.FileExists(destination))
            {
                return path;
            }

            var temporary = file.Directory + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = await fs.GetFile(temporary).OpenWriteAsync())
                {
                    // The artifact is shared by subsequent requests, even if this request disconnects.
                    await write(stream, CancellationToken.None);
                }

                try
                {
                    await fs.GetFile(temporary).MoveToAsync(destination);
                }
                catch (Exception ex) when ((ex is IOException or FileSystemException) && fs.FileExists(destination))
                {
                    // Another process sharing the directory published this immutable revision first.
                }
            }
            finally
            {
                // MoveTo changes the file object's path. Resolve the original temporary path again.
                if (fs.FileExists(temporary))
                {
                    await fs.GetFile(temporary).DeleteAsync();
                }
            }
        }

        return path;
    }
}
