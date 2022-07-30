using Smartstore.Collections;

namespace Smartstore.Core.Content.Media
{
    public partial class MediaSearchResult : Pageable<MediaFileInfo>
    {
        public MediaSearchResult(IPagedList<MediaFile> pagedList, Func<MediaFile, MediaFileInfo> converter)
            : base(ConvertPageable(pagedList, converter))
        {
        }

        private static IPageable<MediaFileInfo> ConvertPageable(IPageable<MediaFile> pageable, Func<MediaFile, MediaFileInfo> converter)
        {
            Guard.NotNull(pageable, nameof(pageable));
            Guard.NotNull(converter, nameof(converter));

            return pageable.Select(converter).AsQueryable().ToPagedList(pageable.PageIndex, pageable.PageSize, pageable.TotalCount);
        }
    }
}
