using System.Runtime.CompilerServices;
using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Threading;

namespace Smartstore.Core.Content.Media
{
    public static class IMediaServiceExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<MediaSearchResult> SearchFilesAsync(this IMediaService service, MediaSearchQuery query, MediaLoadFlags flags = MediaLoadFlags.AsNoTracking)
        {
            return await service.SearchFilesAsync(query, null, flags);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<string> GetUrlAsync(this IMediaService service, int? fileId, ProcessImageQuery imageQuery, string host = null, bool doFallback = true)
        {
            return service.GetUrl(await service.GetFileByIdAsync(fileId ?? 0, MediaLoadFlags.AsNoTracking), imageQuery, host, doFallback);
        }

        public static async Task<string> GetUrlAsync(this IMediaService service, int? fileId, int thumbnailSize, string host = null, bool doFallback = true)
        {
            ProcessImageQuery query = thumbnailSize > 0
                ? new ProcessImageQuery { MaxSize = thumbnailSize }
                : null;

            return service.GetUrl(await service.GetFileByIdAsync(fileId ?? 0, MediaLoadFlags.AsNoTracking), query, host, doFallback);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetUrl(this IMediaService service, MediaFile file, int thumbnailSize, string host = null, bool doFallback = true)
        {
            ProcessImageQuery query = thumbnailSize > 0
                ? new ProcessImageQuery { MaxSize = thumbnailSize }
                : null;

            return service.GetUrl(file != null ? service.ConvertMediaFile(file) : null, query, host, doFallback);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetUrl(this IMediaService service, MediaFileInfo file, int thumbnailSize, string host = null, bool doFallback = true)
        {
            ProcessImageQuery query = thumbnailSize > 0
                ? new ProcessImageQuery { MaxSize = thumbnailSize }
                : null;

            return service.GetUrl(file, query, host, doFallback);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetFallbackUrl(this IMediaService service, int thumbnailSize = 0)
        {
            ProcessImageQuery query = thumbnailSize > 0
                ? new ProcessImageQuery { MaxSize = thumbnailSize }
                : null;

            return service.GetUrl((MediaFileInfo)null, query, null, true);
        }

        /// <summary>
        /// Tries to find an equal file by comparing the source buffer to a list of files.
        /// </summary>
        /// <param name="sourceBuffer">Binary source file data to find a match for.</param>
        /// <param name="files">The sequence of files to seek within for duplicates.</param>
        /// <param name="equalFile">A file from the <paramref name="files"/> collection whose content is equal to <paramref name="sourceBuffer"/>.</param>
        /// <returns>The passed file binary when no file equals in the sequence, <c>null</c> otherwise.</returns>
        public static byte[] FindEqualFile(this IMediaService service, byte[] sourceBuffer, IEnumerable<MediaFile> files, out MediaFile equalFile)
        {
            Guard.NotNull(sourceBuffer);

            if (!service.FindEqualFile(new MemoryStream(sourceBuffer), files, false, out equalFile))
            {
                return sourceBuffer;
            }

            return null;
        }

        /// <summary>
        /// Tries to find an equal file by file name, then by comparing the binary contents of the matched files to <paramref name="sourcePath"/> binary content.
        /// </summary>
        /// <param name="sourcePath">The full physical path to the source file to find a duplicate for (e.g. a local or downloaded file during an import process).</param>
        /// <param name="targetFolderId">The id of the folder in which to look for duplicates.</param>
        /// <param name="deepSearch">Whether to search in subfolders too.</param>
        /// <returns><c>true</c> when a duplicate file was found, <c>false</c> otherwise.</returns>
        public static Task<AsyncOut<MediaFile>> FindEqualFileAsync(this IMediaService service, string sourcePath, int targetFolderId, bool deepSearch)
        {
            Guard.NotEmpty(sourcePath);

            var fi = new FileInfo(sourcePath);
            if (!fi.Exists)
            {
                return Task.FromResult(new AsyncOut<MediaFile>(false));
            }

            return FindEqualFileAsync(service, fi.OpenRead(), fi.Name, targetFolderId, deepSearch);
        }

        /// <summary>
        /// Tries to find an equal file by file name, then by comparing the binary contents of the matched files to <paramref name="source"/> content.
        /// </summary>
        /// <param name="source">The source file stream to find a duplicate for (e.g. a local or downloaded file during an import process).</param>
        /// <param name="fileName">The file name used to determine potential duplicates to check against.</param>
        /// <param name="targetFolderId">The id of the folder in which to look for duplicates.</param>
        /// <param name="deepSearch">Whether to search in subfolders too.</param>
        /// <returns><c>true</c> when a duplicate file was found, <c>false</c> otherwise.</returns>
        public static async Task<AsyncOut<MediaFile>> FindEqualFileAsync(this IMediaService service, Stream source, string fileName, int targetFolderId, bool deepSearch)
        {
            Guard.NotNull(source);
            Guard.NotEmpty(fileName);
            Guard.IsPositive(targetFolderId);

            var query = new MediaSearchQuery
            {
                FolderId = targetFolderId,
                DeepSearch = deepSearch,
                ExactMatch = true,
                Term = fileName,
                IncludeAltForTerm = false
            };

            var matches = await service.SearchFilesAsync(query);

            if (matches.TotalCount == 0)
            {
                return new AsyncOut<MediaFile>(false);
            }

            return await service.FindEqualFileAsync(source, matches.Select(x => x.File), true);
        }
    }
}
