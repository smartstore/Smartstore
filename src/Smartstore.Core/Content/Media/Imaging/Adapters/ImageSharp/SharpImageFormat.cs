using System.Collections.Generic;
using System.Linq;

namespace Smartstore.Core.Content.Media.Imaging.Adapters.ImageSharp
{
    internal sealed class SharpImageFormat : IImageFormat
    {
        private readonly SixLabors.ImageSharp.Formats.IImageFormat _format;

        public SharpImageFormat(SixLabors.ImageSharp.Formats.IImageFormat format)
        {
            Guard.NotNull(format, nameof(format));
            _format = format;
        }

        public SixLabors.ImageSharp.Formats.IImageFormat WrappedFormat 
            => _format;

        public string Name 
            => _format.Name;

        public string DefaultExtension 
            => _format.FileExtensions.First();

        public string DefaultMimeType 
            => _format.DefaultMimeType;

        public IEnumerable<string> FileExtensions 
            => _format.FileExtensions;

        public IEnumerable<string> MimeTypes 
            => _format.MimeTypes;
    }
}