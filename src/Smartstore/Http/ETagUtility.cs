using System.Runtime.CompilerServices;
using Microsoft.Extensions.FileProviders;
using Smartstore.Utilities;

namespace Smartstore.Net
{
    public static class ETagUtility
    {
        /// <summary>
        /// Generates an ETag for the given <paramref name="file"/> including last modified time, length and an optional discrimator.
        /// </summary>
        /// <param name="file">File to generate ETag for.</param>
        /// <param name="discriminator">An optional discrimator to avoid collisions.</param>
        /// <returns>The raw ETag (without leading and trailing \")</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GenerateETag(FileInfo file, string discriminator = null)
        {
            Guard.NotNull(file, nameof(file));
            return GenerateETag(file.LastWriteTimeUtc, file.Length, discriminator);
        }

        /// <summary>
        /// Generates an ETag for the given <paramref name="file"/> including last modified time, length and an optional discrimator.
        /// </summary>
        /// <param name="file">File to generate ETag for.</param>
        /// <param name="discriminator">An optional discrimator to avoid collisions.</param>
        /// <returns>The raw ETag (without leading and trailing \")</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GenerateETag(IFileInfo file, string discriminator = null)
        {
            Guard.NotNull(file, nameof(file));
            return GenerateETag(file.LastModified, file.Length, discriminator);
        }

        /// <summary>
        /// Generates an ETag for the given file <paramref name="lastModified"/> and <paramref name="length"/>> parameters.
        /// </summary>
        /// <param name="lastModified">Last modified date of the file.</param>
        /// <param name="length">Length of file in bytes.</param>
        /// <param name="discriminator">An optional discrimator to avoid collisions.</param>
        /// <returns>The raw ETag (without leading and trailing \")</returns>
        public static string GenerateETag(DateTimeOffset lastModified, long length, string discriminator = null)
        {
            var last = lastModified;

            var hash = HashCodeCombiner
                .Start()
                .Add(length)
                .Add(new DateTimeOffset(last.Year, last.Month, last.Day, last.Hour, last.Minute, last.Second, last.Offset).ToUniversalTime())
                .Add(discriminator)
                .CombinedHashString;

            return '\"' + hash + '\"';
        }
    }
}