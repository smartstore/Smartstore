using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Smartstore.Imaging;

namespace Smartstore.Core.Content.Media
{
    public static class MediaFileExtensions
    {
        /// <summary>
        /// Applies Blob to file
        /// </summary>
        /// <param name="blob">The file binary (can be null)</param>
        public static void ApplyBlob(this MediaFile file, byte[] blob)
        {
            Guard.NotNull(file, nameof(file));

            if (blob == null || blob.LongLength == 0)
            {
                file.MediaStorageId = null;
                file.MediaStorage = null;
            }
            else
            {
                if (file.MediaStorage != null)
                {
                    file.MediaStorage.Data = blob;
                }
                else
                {
                    file.MediaStorage = new MediaStorage { Data = blob };
                }
            }
        }

        /// <summary>
        /// Refreshes file metadata like size, dimensions etc.
        /// </summary>
        /// <param name="stream">The file stream (can be null)</param>
        public static async Task RefreshMetadataAsync(this MediaFile file, Stream stream, IImageFactory imageFactory)
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
                    var imageInfo = await imageFactory.DetectInfoAsync(stream);
                    if (imageInfo != null)
                    {
                        file.Width = imageInfo.Width;
                        file.Height = imageInfo.Height;
                        file.PixelSize = imageInfo.Width * imageInfo.Height;
                    }
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
