using Microsoft.AspNetCore.StaticFiles;

namespace Smartstore.IO
{
    public static class MimeTypes
    {
        const string DefaultMimeType = "application/octet-stream";

        static FileExtensionContentTypeProvider _contentTypeProvider = new SmartFileExtensionContentTypeProvider();

        public static IContentTypeProvider ContentTypeProvider
        {
            get => _contentTypeProvider;
        }

        public static IDictionary<string, string> Mappings
        {
            get => _contentTypeProvider.Mappings;
        }

        /// <summary>
        /// Given a file path, determine the MIME type.
        /// </summary>
        /// <param name="subpath">A file path, name or extension.</param>
        /// <returns>The resulting MIME type or <c>application/octet-stream</c> as fallback.</returns>
        public static string MapNameToMimeType(string subpath)
        {
            if (_contentTypeProvider.TryGetContentType(subpath, out var mimeType))
            {
                return mimeType;
            }

            return DefaultMimeType;
        }

        /// <summary>
        /// Given a file path, determine the MIME type.
        /// </summary>
        /// <param name="subpath">A file path, name or extension.</param>
        /// <param name="mimeType">The resulting MIME type</param>
        /// <returns><c>true</c> if MIME type could be determined</returns>
        public static bool TryMapNameToMimeType(string subpath, out string mimeType)
        {
            return _contentTypeProvider.TryGetContentType(subpath, out mimeType);
        }

        /// <summary>
        /// Given a MIME type, determine the (dotless) default extension.
        /// </summary>
        /// <param name="mimeType">The mime type</param>
        /// <returns>The corresponding default file extension (without dot) or <c>null</c>.</returns>
        public static string MapMimeTypeToExtension(string mimeType)
        {
            if (TryMapMimeTypeToExtension(mimeType, out var extension))
            {
                return extension;
            }

            return null;
        }

        /// <summary>
        /// Given a MIME type, determine the (dotless) default extension.
        /// </summary>
        /// <param name="mimeType">The mime type</param>
        /// <param name="extension">The corresponding default file extension (without dot)</param>
        /// <returns><c>true</c> if default extension could be determined</returns>
        public static bool TryMapMimeTypeToExtension(string mimeType, out string extension)
        {
            extension = null;

            if (mimeType.HasValue() && MimeKit.MimeTypes.TryGetExtension(mimeType, out extension))
            {
                extension = extension.TrimStart('.');
            }

            return extension != null;
        }

        public static void Register(string mimeType, string extension)
        {
            Guard.NotEmpty(mimeType, nameof(mimeType));
            Guard.NotEmpty(extension, nameof(extension));

            MimeKit.MimeTypes.Register(mimeType, extension);
            _contentTypeProvider.Mappings[extension.EnsureStartsWith('.')] = mimeType;
        }
    }
}