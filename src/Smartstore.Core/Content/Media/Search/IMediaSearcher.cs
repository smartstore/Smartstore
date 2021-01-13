using System;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Collections;

namespace Smartstore.Core.Content.Media
{
    public interface IMediaSearcher
    {
        Task<IPagedList<MediaFile>> SearchFilesAsync(MediaSearchQuery query, MediaLoadFlags flags = MediaLoadFlags.AsNoTracking);
        IQueryable<MediaFile> PrepareQuery(MediaSearchQuery query, MediaLoadFlags flags);
        IQueryable<MediaFile> ApplyFilterQuery(MediaFilesFilter filter, IQueryable<MediaFile> sourceQuery = null);
        IQueryable<MediaFile> ApplyLoadFlags(IQueryable<MediaFile> query, MediaLoadFlags flags);
    }
}
