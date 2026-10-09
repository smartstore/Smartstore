using Smartstore.Threading;

namespace Smartstore.Core.Content.Media.Icons;

/// <summary>
/// Publishes immutable icon artifacts atomically, sharing concurrent writes within this process.
/// </summary>
internal static class IconFileCache
{
    /// <summary>
    /// Checks the revision format before request input becomes part of a physical filename.
    /// </summary>
    /// <param name="revision">The requested content fingerprint.</param>
    internal static bool IsRevision(string revision)
        => revision is { Length: 24 } && revision.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

    /// <summary>
    /// Returns an existing file or writes it beside the destination before an atomic rename.
    /// Failed or cancelled writes leave no partial artifact. The writer must reject stale source generations.
    /// </summary>
    /// <param name="path">The immutable destination path.</param>
    /// <param name="write">Writes the artifact to the supplied stream without closing it.</param>
    internal static async Task<string> PublishAsync(string path, Func<Stream, CancellationToken, ValueTask> write,
        CancellationToken cancelToken)
    {
        if (File.Exists(path))
        {
            return path;
        }

        using (await AsyncLock.KeyedAsync("icons:file:" + path, cancelToken: cancelToken))
        {
            if (File.Exists(path))
            {
                return path;
            }

            var directory = Path.GetDirectoryName(path);
            Directory.CreateDirectory(directory);
            var temporary = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                    4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
                {
                    await write(stream, cancelToken);
                }

                cancelToken.ThrowIfCancellationRequested();
                try
                {
                    File.Move(temporary, path);
                }
                catch (IOException) when (File.Exists(path))
                {
                    // Another process sharing the directory published this immutable revision first.
                }
            }
            finally
            {
                File.Delete(temporary);
            }
        }

        return path;
    }
}