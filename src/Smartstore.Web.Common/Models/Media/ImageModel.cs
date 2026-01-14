using System.Drawing;
using System.Text.Json.Serialization;
using Smartstore.Core.Content.Media;
using Smartstore.Json.Converters;
using Smartstore.Web.Modelling;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.Models.Media
{
    public partial class ImageModel : EntityModelBase, IImageModel
    {
        public ImageModel()
        {
        }

        public ImageModel(MediaFileInfo info, int? thumbSize = null)
        {
            Populate(info, thumbSize);
        }

        public void Populate(MediaFileInfo info, int? thumbSize = null)
        {
            if (thumbSize.HasValue)
            {
                ThumbSize = thumbSize;
            }

            if (info == null)
            {
                return;
            }

            var file = info?.File;
            if (file != null)
            {
                // Clone the entity
                File = new MediaFile
                {
                    Id = file.Id,
                    FolderId = file.FolderId,
                    Name = file.Name,
                    Alt = file.Alt,
                    Title = file.Title,
                    Extension = file.Extension,
                    MimeType = file.MimeType,
                    MediaType = file.MediaType,
                    Size = file.Size,
                    PixelSize = file.PixelSize,
                    Width = file.Width,
                    Height = file.Height,
                    CreatedOnUtc = file.CreatedOnUtc,
                    UpdatedOnUtc = file.UpdatedOnUtc,
                    Hidden = file.Hidden,
                    MediaStorageId = file.MediaStorageId
                };
            }
            
            Alt = info.Alt;
            Title = info.TitleAttribute;
            Path = info.Path;
            Url = info.GetUrl(0, Host);
            ThumbUrl = info.GetUrl(ThumbSize ?? info.ThumbSize, Host);
        }

        [JsonPropertyName("file")]
        public MediaFile File { get; set; }

        public override int Id
        {
            get => File?.Id ?? base.Id;
            set => base.Id = value;
        }

        [JsonPropertyName("alt")]
        public string Alt { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; }

        [JsonPropertyName("path")]
        public string Path { get; set; }

        [JsonPropertyName("url")]
        public string Url { get; set; }

        [JsonPropertyName("thumbUrl")]
        public string ThumbUrl { get; set; }

        [JsonPropertyName("thumbSize")]
        public int? ThumbSize { get; set; }

        [JsonPropertyName("noFallback")]
        public bool NoFallback { get; set; }

        [JsonPropertyName("host")]
        public string Host { get; set; }

        [JsonPropertyName("dimensions")]
        [JsonConverter(typeof(TypeConverterJsonConverter<Size>))]
        public Size PixelSize
        {
            get => new(File?.Width ?? 0, File?.Height ?? 0);
        }

        public bool HasImage() => File != null || !NoFallback;
    }
}
