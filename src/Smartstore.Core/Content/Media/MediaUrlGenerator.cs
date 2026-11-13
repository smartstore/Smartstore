using System.Globalization;
using Microsoft.AspNetCore.Http;
using Smartstore.Core.Stores;
using Smartstore.Net;
using Smartstore.Utilities;

namespace Smartstore.Core.Content.Media;

public partial class MediaUrlGenerator : IMediaUrlGenerator
{
    const string _fallbackImagesRootPath = "images/";

    private static readonly char[] _fallbackImageTrimChars = [' ', '\t', '\r', '\n', '~', '/', '\\'];

    private readonly MediaSettings _mediaSettings;
    private readonly string _host;
    private readonly string _pathBase;
    private readonly string _fallbackImageFileName;
    private readonly string _processedImagesRootPath;

    public MediaUrlGenerator(
        IMediaStorageConfiguration storageConfig,
        MediaSettings mediaSettings,
        IStoreContext storeContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _mediaSettings = mediaSettings;
        _processedImagesRootPath = storageConfig.PublicPath;

        var httpContext = httpContextAccessor.HttpContext;
        string pathBase = "/";

        if (httpContext != null)
        {
            var request = httpContext.Request;
            pathBase = request.PathBase;

            var cdn = storeContext.CurrentStore.ContentDeliveryNetwork;
            if (cdn.HasValue() && !CommonHelper.IsDevEnvironment && !httpContext.Connection.IsLocal())
            {
                _host = cdn;
            }
            else if (mediaSettings.AutoGenerateAbsoluteUrls)
            {
                _host = "//{0}{1}".FormatInvariant(request.Host, pathBase);
            }
            else
            {
                _host = pathBase;
            }
        }

        _host = _host.EmptyNull().EnsureEndsWith('/');
        _pathBase = pathBase.EnsureEndsWith('/');

        var defaultImageName = mediaSettings.DefaultImageName.EmptyNull();
        var normalized = NormalizeFallbackImageFileName(defaultImageName);

        _fallbackImageFileName = normalized.Length == defaultImageName.Length
            ? defaultImageName
            : normalized.ToString();
    }

    public static string FallbackImagesRootPath => _fallbackImagesRootPath;

    /// <summary>
    /// Trims whitespace, tildes and separator chars off both ends of the given fallback image file name,
    /// e.g. <c>~/default-image.png</c> --> <c>default-image.png</c>.
    /// </summary>
    /// <remarks>
    /// Trimming all of them in a single pass keeps this idempotent, which matters: <see cref="GenerateUrl"/>
    /// emits the normalized name, and <see cref="IsFallbackImagePath"/> normalizes it again on the way back in.
    /// </remarks>
    public static ReadOnlySpan<char> NormalizeFallbackImageFileName(ReadOnlySpan<char> fileName)
    {
        return fileName.Trim(_fallbackImageTrimChars);
    }

    /// <summary>
    /// Checks whether <paramref name="path"/> denotes the fallback (placeholder) image given by
    /// <paramref name="fallbackImageFileName"/>. Separator chars and casing are not significant.
    /// </summary>
    /// <remarks>
    /// The media route <c>{pub}/0/{path}</c> must serve the configured fallback image only. <paramref name="path"/>
    /// usually comes straight from the request URL, and any other value has to be rejected: the fallback image is
    /// resolved against the web root, so an arbitrary path would allow reading files outside of it.
    /// </remarks>
    /// <param name="path">The requested path, relative to <see cref="FallbackImagesRootPath"/>.</param>
    /// <param name="fallbackImageFileName">The configured fallback image file name, see <see cref="MediaSettings.DefaultImageName"/>.</param>
    /// <returns><c>true</c> if the path denotes the fallback image; otherwise <c>false</c>.</returns>
    public static bool IsFallbackImagePath(ReadOnlySpan<char> path, ReadOnlySpan<char> fallbackImageFileName)
    {
        var fileName = NormalizeFallbackImageFileName(fallbackImageFileName);
        if (fileName.IsEmpty)
        {
            return false;
        }

        var requestedFileName = NormalizeFallbackImageFileName(path);
        if (requestedFileName.Length != fileName.Length)
        {
            return false;
        }

        for (var i = 0; i < fileName.Length; i++)
        {
            var c1 = requestedFileName[i];
            var c2 = fileName[i];

            if (c1 == '\\')
            {
                c1 = '/';
            }

            if (c2 == '\\')
            {
                c2 = '/';
            }

            // INFO: Compare like StringComparison.OrdinalIgnoreCase does, which also folds to upper case.
            if (c1 != c2 && char.ToUpperInvariant(c1) != char.ToUpperInvariant(c2))
            {
                return false;
            }
        }

        return true;
    }

    public virtual string GenerateUrl(
        MediaFileInfo file,
        QueryString query = default,
        string host = null,
        bool doFallback = true)
    {
        string path;

        // Build virtual path with pattern "media/{id}/{album}/{dir}/{NameWithExt}"
        if (file?.Path != null)
        {
            path = _processedImagesRootPath + file.Id.ToString(CultureInfo.InvariantCulture) + "/" + file.Path;
        }
        else if (doFallback)
        {
            path = _processedImagesRootPath + "0/" + _fallbackImageFileName;
        }
        else
        {
            return null;
        }

        if (host == null)
        {
            host = _host;
        }
        else if (host == string.Empty)
        {
            host = _pathBase;
        }
        else
        {
            host = host.EnsureEndsWith('/');
        }

        var url = host;

        // Strip leading "/", the host/pathBase has this already
        if (path[0] == '/')
        {
            path = path[1..];
        }

        // Append media path
        url += path;

        // Append file hash to query
        if (file != null && _mediaSettings.AppendFileVersionToUrl)
        {
            query = query.Add("ver", ETagUtility.GenerateETag(file.LastModified, file.Length, null, true));
        }

        // Append query to url
        if (query.HasValue)
        {
            url += query.ToString();
        }

        return url;
    }
}