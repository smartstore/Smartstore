using Smartstore.ComponentModel;
using Smartstore.Core;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Localization;
using Smartstore.Web.Models.Media;

namespace Smartstore.Blog.Models.Mappers
{
    public class BlogItemImageMapper : Mapper<BlogPost, ImageModel>
    {
        private readonly IMediaService _mediaService;
        private readonly ICommonServices _services;

        public BlogItemImageMapper(IMediaService mediaService, ICommonServices services)
        {
            _mediaService = mediaService;
            _services = services;
        }

        protected override void Map(BlogPost from, ImageModel to, dynamic parameters = null)
            => throw new NotImplementedException();

        public override async Task MapAsync(BlogPost from, ImageModel to, dynamic parameters = null)
        {
            Guard.NotNull(from, nameof(from));
            Guard.NotNull(to, nameof(to));

            var fileId = Convert.ToInt32(parameters?.FileId as int?);
            var file = await _mediaService.GetFileByIdAsync(fileId, MediaLoadFlags.AsNoTracking);

            to.File = file;
            to.ThumbSize = MediaSettings.ThumbnailSizeLg;
            to.Title = file?.File?.GetLocalized(x => x.Title)?.Value.NullEmpty() ?? from.GetLocalized(x => x.Title);
            to.Alt = file?.File?.GetLocalized(x => x.Alt)?.Value.NullEmpty() ?? from.GetLocalized(x => x.Title);

            _services.DisplayControl.Announce(file?.File);
        }
    }
}
