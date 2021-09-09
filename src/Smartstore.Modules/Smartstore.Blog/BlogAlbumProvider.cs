using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Smartstore.Blog.Domain;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Data;

namespace Smartstore.Blog
{
    class BlogAlbumProvider : IMediaTrackDetector
    {
        private readonly SmartDbContext _db;

        public BlogAlbumProvider(SmartDbContext db)
        {
            _db = db;
        }

        public bool MatchAlbum(string albumName)
        {
            if (albumName == "content")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void ConfigureTracks(string albumName, TrackedMediaPropertyTable table)
        {
            if (albumName == "content")
            {
                table.Register<BlogPost>(x => x.MediaFileId);
                table.Register<BlogPost>(x => x.PreviewMediaFileId);
            }
        }

        public async IAsyncEnumerable<MediaTrack> DetectAllTracksAsync(string albumName, [EnumeratorCancellation] CancellationToken cancelToken = default)
        {
            if (albumName == "content")
            { 
                var name = nameof(BlogPost);
                var p = new FastPager<BlogPost>(_db.BlogPosts().AsNoTracking().Where(x => x.MediaFileId.HasValue || x.PreviewMediaFileId.HasValue));
                while ((await p.ReadNextPageAsync(x => new { x.Id, x.MediaFileId, x.PreviewMediaFileId }, x => x.Id)).Out(out var list))
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
}
