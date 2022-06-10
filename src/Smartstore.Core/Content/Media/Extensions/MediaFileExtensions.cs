using Smartstore.Imaging;

namespace Smartstore.Core.Content.Media
{
    public static class MediaFileExtensions
    {
        /// <summary>
        /// Refreshes file metadata like size, dimensions etc.
        /// </summary>
        /// <param name="stream">The file stream (can be null)</param>
        public static void RefreshMetadata(this MediaFile file, Stream stream, IImageFactory imageFactory)
        {
            Guard.NotNull(file, nameof(file));

            file.Size = stream != null ? (int)stream.Length : 0;
            file.Width = null;
            file.Height = null;
            file.PixelSize = null;

            if (stream != null && file.MediaType == MediaType.Image)
            {
                try
                {
                    var size = ImageHeader.GetPixelSize(stream, file.MimeType);
                    file.Width = size.Width;
                    file.Height = size.Height;
                    file.PixelSize = size.Width * size.Height;
                }
                catch
                {
                    // Don't attempt again
                    file.Width = 0;
                    file.Height = 0;
                    file.PixelSize = 0;
                }
            }
        }
    }
}
