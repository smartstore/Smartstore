using Smartstore.Collections;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;

namespace Smartstore.Core.DataExchange.Export.Internal
{
    internal class ManufacturerBatchContext
    {
        protected readonly List<int> _manufacturerIds = new();
        protected readonly List<int> _mediaFileIds = new();

        protected readonly SmartDbContext _db;
        protected readonly IMediaService _mediaService;

        private LazyMultimap<ProductManufacturer> _productManufacturers;
        private LazyMultimap<MediaFileInfo> _mediaFiles;

        public ManufacturerBatchContext(IEnumerable<Manufacturer> manufacturers, ICommonServices services)
        {
            Guard.NotNull(services, nameof(services));

            _db = services.DbContext;
            _mediaService = services.Resolve<IMediaService>();

            if (manufacturers != null)
            {
                _manufacturerIds = new List<int>(manufacturers.Select(x => x.Id));

                _mediaFileIds = manufacturers
                    .Select(x => x.MediaFileId ?? 0)
                    .Where(x => x != 0)
                    .Distinct()
                    .ToList();
            }
        }

        public IReadOnlyList<int> ManufacturerIds => _manufacturerIds;

        public LazyMultimap<ProductManufacturer> ProductManufacturers
        {
            get => _productManufacturers ??=
                new LazyMultimap<ProductManufacturer>(keys => LoadProductManufacturers(keys), _manufacturerIds);
        }

        public LazyMultimap<MediaFileInfo> MediaFiles
        {
            get => _mediaFiles ??=
                new LazyMultimap<MediaFileInfo>(keys => LoadMediaFiles(keys), _mediaFileIds);
        }

        public virtual void Clear()
        {
            _productManufacturers?.Clear();
            _mediaFiles?.Clear();

            _manufacturerIds?.Clear();
            _mediaFileIds?.Clear();
        }

        #region Protected factories

        protected virtual async Task<Multimap<int, ProductManufacturer>> LoadProductManufacturers(int[] manufacturerIds)
        {
            var productManufacturers = await _db.ProductManufacturers
                .AsNoTracking()
                .Where(x => manufacturerIds.Contains(x.ManufacturerId))
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            return productManufacturers.ToMultimap(x => x.ManufacturerId, x => x);
        }

        protected virtual async Task<Multimap<int, MediaFileInfo>> LoadMediaFiles(int[] mediaFileIds)
        {
            var mediaFiles = await _mediaService.GetFilesByIdsAsync(mediaFileIds);

            return mediaFiles.ToMultimap(x => x.Id, x => x);
        }

        #endregion
    }
}
