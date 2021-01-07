using System;
using System.IO;
using System.Threading.Tasks;

namespace Smartstore.Core.Content.Media.Imaging.Impl.ImageSharp
{
    public class ImageSharpImageFactory : IImageFactory
    {
        public bool IsSupportedImage(string extension)
        {
            throw new NotImplementedException();
        }

        public IImageFormat GetImageFormat(string extension)
        {
            throw new NotImplementedException();
        }

        public Task<IProcessableImage> LoadImageAsync(string path, bool preserveExif = false)
        {
            throw new NotImplementedException();
        }

        public Task<IProcessableImage> LoadImageAsync(Stream stream, bool preserveExif = false)
        {
            throw new NotImplementedException();
        }
    }
}
