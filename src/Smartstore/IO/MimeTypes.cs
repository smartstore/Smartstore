namespace Smartstore.IO
{
    public static class MimeTypes
    {
        public static string MapNameToMimeType(string fileNameOrExtension)
        {
            return MimeKit.MimeTypes.GetMimeType(fileNameOrExtension);
        }

        /// <summary>
        /// Returns the (dotless) extension for a mime type
        /// </summary>
        /// <param name="mimeType">The mime type</param>
        /// <returns>The corresponding file extension (without dot)</returns>
        public static string MapMimeTypeToExtension(string mimeType)
        {
            if (mimeType.IsEmpty())
                return null;

            if (MimeKit.MimeTypes.TryGetExtension(mimeType, out var extension))
            {
                return extension.TrimStart('.');
            }

            return null;
        }

        public static void Register(string mimeType, string extension)
        {
            MimeKit.MimeTypes.Register(mimeType, extension);
        }
    }
}
