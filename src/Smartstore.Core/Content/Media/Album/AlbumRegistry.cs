using System;
using System.Collections.Generic;
using System.Linq;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Smartstore.Core.Content.Media
{
    public class AlbumRegistry : IAlbumRegistry
    {
        internal const string AlbumInfosKey = "media:albums:all";

        private readonly SmartDbContext _db;
        private readonly ICacheManager _cache;
        private readonly IEnumerable<Lazy<IAlbumProvider>> _albumProviders;

        public AlbumRegistry(
            SmartDbContext db,
            ICacheManager cache,
            IEnumerable<Lazy<IAlbumProvider>> albumProviders)
        {
            _db = db;
            _cache = cache;
            _albumProviders = albumProviders;
        }

        public virtual async Task<IReadOnlyCollection<AlbumInfo>> GetAllAlbumsAsync()
        {
            return (await GetAlbumDictionary()).Values;
        }

        public async Task<IEnumerable<string>> GetAlbumNamesAsync(bool withTrackDetectors = false)
        {
            var dict = await GetAlbumDictionary();

            if (!withTrackDetectors)
            {
                return dict.Keys;
            }

            return dict.Where(x => x.Value.IsTrackDetector).Select(x => x.Key).ToArray();
        }

        private async Task<Dictionary<string, AlbumInfo>> GetAlbumDictionary()
        {
            var albums = await _cache.GetAsync(AlbumInfosKey, o =>
            {
                o.ExpiresIn(TimeSpan.FromHours(24));
                return LoadAllAlbumsAsync().AsyncToDictionary(x => x.Name);
            });

            return albums;
        }

        protected virtual async IAsyncEnumerable<AlbumInfo> LoadAllAlbumsAsync()
        {
            var setFolders = _db.MediaFolders;

            var dbAlbums = await setFolders
                .AsNoTracking()
                .OfType<MediaAlbum>()
                .Select(x => new { x.Id, x.Name })
                .ToDictionaryAsync(x => x.Name);

            foreach (var lazyProvider in _albumProviders)
            {
                var provider = lazyProvider.Value;
                var albums = provider.GetAlbums().DistinctBy(x => x.Name).ToArray();

                foreach (var album in albums)
                {
                    var info = new AlbumInfo
                    {
                        Name = album.Name,
                        ProviderType = provider.GetType(),
                        IsSystemAlbum = typeof(SystemAlbumProvider) == provider.GetType(),
                        DisplayHint = provider.GetDisplayHint(album) ?? new AlbumDisplayHint()
                    };

                    if (provider is IMediaTrackDetector detector)
                    {
                        var propertyTable = new TrackedMediaPropertyTable(album.Name);
                        detector.ConfigureTracks(album.Name, propertyTable);

                        info.IsTrackDetector = true;
                        info.TrackedProperties = propertyTable.GetProperties();
                    }

                    if (dbAlbums.TryGetValue(album.Name, out var dbAlbum))
                    {
                        info.Id = dbAlbum.Id;
                    }
                    else
                    {
                        setFolders.Add(album);
                        await _db.SaveChangesAsync();
                        info.Id = album.Id;
                    }

                    yield return info;
                }
            }
        }

        public virtual async Task<AlbumInfo> GetAlbumByNameAsync(string name)
        {
            Guard.NotEmpty(name, nameof(name));

            return (await GetAlbumDictionary()).Get(name);
        }

        public virtual async Task<AlbumInfo> GetAlbumByIdAsync(int id)
        {
            if (id > 0)
            {
                return (await GetAlbumDictionary()).FirstOrDefault(x => x.Value.Id == id).Value;
            }

            return null;
        }

        public async Task UninstallAlbumAsync(string albumName)
        {
            var album = await GetAlbumValidated(albumName, true);

            throw new NotImplementedException();

            //ClearCache();
        }

        public async Task DeleteAlbumAsync(string albumName, string moveFilesToAlbum)
        {
            var album = await GetAlbumValidated(albumName, true);
            var destinationAlbum = await GetAlbumValidated(moveFilesToAlbum, false);

            throw new NotImplementedException();

            //ClearCache();
        }

        private async Task<AlbumInfo> GetAlbumValidated(string albumName, bool throwWhenSystemAlbum)
        {
            Guard.NotEmpty(albumName, nameof(albumName));

            var album = await GetAlbumByNameAsync(albumName);

            if (album == null)
            {
                throw new InvalidOperationException($"The album '{albumName}' does not exist.");
            }

            if (album.IsSystemAlbum && throwWhenSystemAlbum)
            {
                throw new InvalidOperationException($"The media album '{albumName}' is a system album and cannot be deleted.");
            }

            return album;
        }

        public Task ClearCacheAsync()
        {
            return _cache.RemoveAsync(AlbumInfosKey);
        }
    }
}
