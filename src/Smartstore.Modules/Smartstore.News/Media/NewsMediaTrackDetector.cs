using System.Runtime.CompilerServices;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Data;

namespace Smartstore.News.Media
{
    public class NewsMediaTrackDetector : IMediaTrackDetector
    {
        private readonly SmartDbContext _db;

        public NewsMediaTrackDetector(SmartDbContext db)
        {
            _db = db;
        }

        public bool MatchAlbum(string albumName)
            => albumName == SystemAlbumProvider.Content;

        public void ConfigureTracks(string albumName, TrackedMediaPropertyTable table)
        {
            table.Register<NewsItem>(x => x.MediaFileId);
            table.Register<NewsItem>(x => x.PreviewMediaFileId);
        }

        public async IAsyncEnumerable<MediaTrack> DetectAllTracksAsync(string albumName, [EnumeratorCancellation] CancellationToken cancelToken = default)
        {
            var name = nameof(NewsItem);
            var p = new FastPager<NewsItem>(_db.NewsItems().AsNoTracking().Where(x => x.MediaFileId.HasValue || x.PreviewMediaFileId.HasValue));
            while ((await p.ReadNextPageAsync(x => new { x.Id, x.MediaFileId, x.PreviewMediaFileId }, x => x.Id, cancelToken)).Out(out var list))
            {
                foreach (var x in list)
                {
                    if (x.MediaFileId.HasValue)
                        yield return new MediaTrack { EntityId = x.Id, EntityName = name, MediaFileId = x.MediaFileId.Value, Property = nameof(x.MediaFileId) };
                    if (x.PreviewMediaFileId.HasValue)
                        yield return new MediaTrack { EntityId = x.Id, EntityName = name, MediaFileId = x.PreviewMediaFileId.Value, Property = nameof(x.PreviewMediaFileId) };
                }
                list.Clear();
            }

            yield break;
        }
    }
}
