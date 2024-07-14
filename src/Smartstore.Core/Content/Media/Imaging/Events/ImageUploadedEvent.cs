﻿using Smartstore.Imaging;

namespace Smartstore.Core.Content.Media.Imaging
{
    /// <summary>
    /// Published for every uploaded image which does NOT exceed maximum
    /// allowed size. This gives subscribers the chance to still process the image,
    /// e.g. to achieve better compression before saving image data to storage. 
    /// This event does NOT get published when the uploaded image is about to be processed anyway.
    /// </summary>
    /// <remarks>
    /// A subscriber should NOT resize the image. But if you do - and you shouldn't :-) - , don't forget to set <see cref="ResultImage"/>.
    /// </remarks>
    public class ImageUploadedEvent(ProcessImageQuery query, IImageInfo info)
    {

        /// <summary>
        /// Contains the source (as byte[], Stream or path string), max size, format and default image quality instructions.
        /// </summary>
        public ProcessImageQuery Query { get; } = Guard.NotNull(query);

        /// <summary>
        /// Info/metadata of uploaded image.
        /// </summary>
        public IImageInfo Info { get; } = Guard.NotNull(info);

        /// <summary>
        /// The processing result. If null, the original data
        /// from <c>Query.Source</c> will be put to storage.
        /// </summary>
        public IImage ResultImage { get; set; }
    }
}