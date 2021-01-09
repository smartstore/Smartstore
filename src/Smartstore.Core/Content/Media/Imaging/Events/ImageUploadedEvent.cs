using System;
using System.Drawing;
using Smartstore.Imaging;

namespace Smartstore.Core.Content.Media.Imaging
{
    /// <summary>
    /// Published for every uploaded image which does NOT exceed maximum
    /// allowed size. This gives subscribers the chance to still process the image,
    /// e.g. to achive better compression before saving image data to storage. 
    /// This event does NOT get published when the uploaded image is about to be processed anyway.
    /// </summary>
    /// <remarks>
    /// A subscriber should NOT resize the image. But if you do - and you shouldn't :-) - , don't forget to set <see cref="ResultSize"/>.
    /// </remarks>
    public class ImageUploadedEvent
    {
        public ImageUploadedEvent(ProcessImageQuery query, Size size)
        {
            Query = query;
            Size = size;
        }

        /// <summary>
        /// Contains the source (as byte[], STream or path string), max size, format and default image quality instructions.
        /// </summary>
        public ProcessImageQuery Query { get; private set; }

        /// <summary>
        /// The original size of the uploaded image. May be empty.
        /// </summary>
        public Size Size { get; private set; }

        /// <summary>
        /// The processing result. If null, the original data
        /// from <c>Query.Source</c> will be put to storage.
        /// </summary>
        public IImage ResultImage { get; set; }
    }
}