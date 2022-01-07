using Smartstore.Collections;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;

namespace Smartstore.Core.DataExchange.Export.Internal
{
    internal class CategoryBatchContext
    {
        protected readonly List<int> _categoryIds = new();
        protected readonly List<int> _mediaFileIds = new();

        protected readonly SmartDbContext _db;
        protected readonly IMediaService _mediaService;

        private LazyMultimap<ProductCategory> _productCategories;
        private LazyMultimap<MediaFileInfo> _mediaFiles;

        public CategoryBatchContext(IEnumerable<Category> categories, ICommonServices services)
        {
            Guard.NotNull(services, nameof(services));

            _db = services.DbContext;
            _mediaService = services.Resolve<IMediaService>();

            if (categories != null)
            {
                _categoryIds.AddRange(categories.Select(x => x.Id));

                _mediaFileIds = categories
                    .Select(x => x.MediaFileId ?? 0)
                    .Where(x => x != 0)
                    .Distinct()
                    .ToList();
            }
        }

        public IReadOnlyList<int> CategoryIds => _categoryIds;

        public LazyMultimap<ProductCategory> ProductCategories
        {
            get => _productCategories ??=
                new LazyMultimap<ProductCategory>(keys => LoadProductCategories(keys), _categoryIds);
        }

        public LazyMultimap<MediaFileInfo> MediaFiles
        {
            get => _mediaFiles ??=
                new LazyMultimap<MediaFileInfo>(keys => LoadMediaFiles(keys), _mediaFileIds);
        }

        public virtual void Clear()
        {
            _productCategories?.Clear();
            _mediaFiles?.Clear();

            _categoryIds?.Clear();
            _mediaFileIds?.Clear();
        }

        #region Protected factories

        protected virtual async Task<Multimap<int, ProductCategory>> LoadProductCategories(int[] categoryIds)
        {
            var productCategories = await _db.ProductCategories
                .AsNoTracking()
                .Where(x => categoryIds.Contains(x.CategoryId))
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            return productCategories.ToMultimap(x => x.CategoryId, x => x);
        }

        protected virtual async Task<Multimap<int, MediaFileInfo>> LoadMediaFiles(int[] mediaFileIds)
        {
            var mediaFiles = await _mediaService.GetFilesByIdsAsync(mediaFileIds);

            return mediaFiles.ToMultimap(x => x.Id, x => x);
        }

        #endregion
    }
}
