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

        public virtual IReadOnlyCollection<AlbumInfo> GetAllAlbums()
        {
            return GetAlbumDictionary().Values;
        }

        public IEnumerable<string> GetAlbumNames(bool withTrackDetectors = false)
        {
            var dict = GetAlbumDictionary();

            if (!withTrackDetectors)
            {
                return dict.Keys;
            }

            return dict.Where(x => x.Value.IsTrackDetector).Select(x => x.Key).ToArray();
        }

        private Dictionary<string, AlbumInfo> GetAlbumDictionary()
        {
            var albums = _cache.Get(AlbumInfosKey, o =>
            {
                o.ExpiresIn(TimeSpan.FromHours(24));
                return LoadAllAlbums().ToDictionary(x => x.Name);
            });

            return albums;
        }

        protected virtual IEnumerable<AlbumInfo> LoadAllAlbums()
        {
            var dbAlbums = _db.MediaAlbums
                .AsNoTracking()
                .Select(x => new { x.Id, x.Name })
                .ToDictionary(x => x.Name);

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
                        _db.MediaAlbums.Add(album);
                        _db.SaveChanges();
                        info.Id = album.Id;
                    }

                    yield return info;
                }
            }
        }

        public virtual AlbumInfo GetAlbumByName(string name)
        {
            Guard.NotEmpty(name, nameof(name));

            return GetAlbumDictionary().Get(name);
        }

        public virtual AlbumInfo GetAlbumById(int id)
        {
            if (id > 0)
            {
                return GetAlbumDictionary().Values.FirstOrDefault(x => x.Id == id);
            }

            return null;
        }

        public Task UninstallAlbumAsync(string albumName)
        {
            var album = GetAlbumValidated(albumName, true);

            throw new NotImplementedException();

            //ClearCache();
        }

        public Task DeleteAlbumAsync(string albumName, string moveFilesToAlbum)
        {
            var album = GetAlbumValidated(albumName, true);
            var destinationAlbum = GetAlbumValidated(moveFilesToAlbum, false);

            throw new NotImplementedException();

            //ClearCache();
        }

        private AlbumInfo GetAlbumValidated(string albumName, bool throwWhenSystemAlbum)
        {
            Guard.NotEmpty(albumName, nameof(albumName));

            var album = GetAlbumByName(albumName);

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
