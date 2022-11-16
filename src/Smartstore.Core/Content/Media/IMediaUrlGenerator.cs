using Microsoft.AspNetCore.Http;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Generates URLs for media files
    /// </summary>
    public interface IMediaUrlGenerator
    {
        /// <summary>
        /// Generates a public URL for a media file
        /// </summary>
        /// <param name="file">The file to create a URL for</param>
        /// <param name="query">URL query to append.</param>
        /// <param name="host">
        ///     Store host for an absolute URL that also contains scheme and host parts. 
        ///     <c>null</c>: tries to resolve host automatically based on <see langword="Store.ContentDeliveryNetwork"/> or <see cref="MediaSettings.AutoGenerateAbsoluteUrls"/>.
        ///     <c>String.Empty</c>: bypasses automatic host resolution and does NOT prepend host to path.
        ///     <c>Any string</c>: host name to use explicitly.
        /// </param>
        /// <param name="doFallback">
        /// Specifies behaviour in case URL generation fails.
        ///     <c>false</c>: return <c>null</c>.
        ///     <c>true</c>: return URL to a fallback image which is <c>~/Content/images/default-image.png</c> by default but can be modified with hidden setting <c>Media.DefaultImageName</c>
        /// </param>
        /// <returns>The passed file's public URL.</returns>
        string GenerateUrl(MediaFileInfo file, QueryString query, string host = null, bool doFallback = true);
    }
}
