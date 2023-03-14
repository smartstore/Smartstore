using System.Text.RegularExpressions;
using Smartstore.Core.Data;
using Smartstore.IO;

namespace Smartstore.Core.Content.Media
{
    public class OffloadImageResult
    {
        public int NumAttempted { get; set; }
        public int NumFailed { get; set; }
        public int NumSucceded { get; set; }
        public IList<MediaFileInfo> OffloadedFiles { get; set; } = new List<MediaFileInfo>();
        public string ResultHtml { get; set; }
    }

    public partial class ImageOffloader
    {
        //[GeneratedRegex(@"(?<=<img[^>]*src\s*=\s*['""])data:image\/(?<format>[a-z]+);base64,(?<data>[^'""]+)(?=['""][^>]*>)")]
        [GeneratedRegex(@"src\s*=\s*['""](data:image\/(?<format>[a-z]+);base64,(?<data>[^'""]+))['""]")]
        private static partial Regex EmbeddedImagesRegex();
        private static readonly Regex _rgEmbeddedImages = EmbeddedImagesRegex();

        private readonly SmartDbContext _db;
        private readonly IMediaService _mediaService;
        private readonly IFolderService _folderService;

        public ImageOffloader(SmartDbContext db, IMediaService mediaService, IFolderService folderService)
        {
            _db = db;
            _mediaService = mediaService;
            _folderService = folderService;
        }

        public async Task<OffloadImageResult> OffloadEmbeddedImageAsync(
            string html, 
            MediaFolderNode destinationFolder,
            Func<string, string> fileNameGenerator)
        {
            Guard.NotEmpty(html);
            Guard.NotNull(destinationFolder);
            Guard.NotNull(fileNameGenerator);

            var result = new OffloadImageResult();

            var resultHtml = await _rgEmbeddedImages.ReplaceAsync(html, async match =>
            {
                result.NumAttempted++;

                var format = match.Groups["format"].Value;
                var data = match.Groups["data"].Value;
                if (format == "jpeg")
                {
                    format = "jpg";
                }

                string fragment = null;

                try
                {
                    using var stream = new MemoryStream(Convert.FromBase64String(data));

                    //var fileName = $"p{p.Id.ToStringInvariant()}-{CommonHelper.GenerateRandomDigitCode(8)}.{format}";
                    var fileName = fileNameGenerator(format); // TODO: Result cannot be null
                    var filePath = PathUtility.Join(destinationFolder.Path, fileName);
                    var fileInfo = await _mediaService.SaveFileAsync(filePath, stream, false);

                    result.OffloadedFiles.Add(fileInfo);

                    fragment = fileInfo.Url;
                    //dirty = true;
                    //numSucceeded++;
                }
                catch
                {
                    fragment = match.Value;
                    result.NumFailed++;
                }

                return $"src=\"{fragment}\"";
            });

            if (result.OffloadedFiles.Count > 0)
            {
                result.ResultHtml = resultHtml;
            }

            return result;
        }
    }
}
