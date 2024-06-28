using Smartstore.Core.Content.Media.Imaging;

namespace Smartstore.Core.Content.Media
{
    public class ImageHandler(
        IImageProcessor imageProcessor,
        IImageCache imageCache,
        MediaExceptionFactory exceptionFactory) : ImageHandlerBase(imageCache, exceptionFactory)
    {
        private readonly IImageProcessor _imageProcessor = imageProcessor;

        protected override bool IsProcessable(MediaHandlerContext context) =>
            context.ImageQuery.NeedsProcessing(true) && _imageProcessor.Factory.IsSupportedImage(context.PathData.Extension);

        protected override async Task ProcessImageAsync(MediaHandlerContext context, CachedImage cachedImage, Stream inputStream)
        {
            var processQuery = new ProcessImageQuery(context.ImageQuery)
            {
                Source = inputStream,
                Format = context.ImageQuery.Format ?? cachedImage.Extension,
                FileName = cachedImage.FileName,
                DisposeSource = false
            };

            await using (inputStream)
            using (var result = await _imageProcessor.ProcessImageAsync(processQuery, false))
            {
                Logger.Debug($"Processed image '{cachedImage.FileName}' in {result.ProcessTimeMs} ms.");

                var ext = result.Image.Format.DefaultExtension;

                if (!cachedImage.Extension.EqualsNoCase(ext))
                {
                    // jpg <> jpeg
                    cachedImage.Path = Path.ChangeExtension(cachedImage.Path, ext);
                    cachedImage.Extension = ext;
                }

                context.ResultImage = result.Image;
            }
        }
    }
}