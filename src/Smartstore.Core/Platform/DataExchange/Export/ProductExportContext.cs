using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Core.DataExchange.Export
{
    /// <summary>
    /// Cargo data to reduce database roundtrips during work with product batches (export, list model creation etc.)
    /// </summary>
    public class ProductExportContext : PriceCalculationContext
    {
        protected readonly IProductService _productService;
        protected readonly int? _maxMediaPerProduct;

        private LazyMultimap<ProductMediaFile> _productMediaFiles;
        private LazyMultimap<ProductTag> _productTags;
        private LazyMultimap<ProductSpecificationAttribute> _specificationAttributes;
        private LazyMultimap<Download> _downloads;

        public ProductExportContext(
            IEnumerable<Product> products,
            ICommonServices services,
            Store store,
            Customer customer,
            bool includeHidden, 
            int? maxMediaPerProduct = null)
            : base(products, services, store, customer, includeHidden)
        {
            Guard.NotNull(services, nameof(services));

            _productService = services.Resolve<IProductService>();
            _maxMediaPerProduct = maxMediaPerProduct;
        }

        public override void Clear()
        {
            _productMediaFiles?.Clear();
            _productTags?.Clear();
            _specificationAttributes?.Clear();
            _downloads?.Clear();

            base.Clear();
        }

        public LazyMultimap<ProductMediaFile> ProductMediaFiles
        {
            get => _productMediaFiles ??=
                new LazyMultimap<ProductMediaFile>(keys => LoadProductMediaFiles(keys), _productIds);
        }

        public LazyMultimap<ProductTag> ProductTags
        {
            get => _productTags ??=
                new LazyMultimap<ProductTag>(keys => LoadProductTags(keys), _productIds);
        }

        public LazyMultimap<ProductSpecificationAttribute> SpecificationAttributes
        {
            get => _specificationAttributes ??=
                new LazyMultimap<ProductSpecificationAttribute>(keys => LoadSpecificationAttributes(keys), _productIds);
        }

        public LazyMultimap<Download> Downloads
        {
            get => _downloads ??=
                new LazyMultimap<Download>(keys => LoadDownloads(keys), _productIds);
        }

        #region Protected factories

        protected virtual async Task<Multimap<int, ProductMediaFile>> LoadProductMediaFiles(int[] ids)
        {
            var files = await _db.ProductMediaFiles
                .AsNoTracking()
                .ApplyProductFilter(ids, _maxMediaPerProduct)
                .ToListAsync();

            return files.ToMultimap(x => x.ProductId, x => x);
        }

        protected virtual async Task<Multimap<int, ProductTag>> LoadProductTags(int[] ids)
        {
            return await _productService.GetProductTagsByProductIdsAsync(ids, _includeHidden);
        }

        protected virtual async Task<Multimap<int, ProductSpecificationAttribute>> LoadSpecificationAttributes(int[] ids)
        {
            var attributes = await _db.ProductSpecificationAttributes
                .AsNoTracking()
                .Include(x => x.SpecificationAttributeOption)
                .ThenInclude(x => x.SpecificationAttribute)
                .ApplyProductsFilter(ids)
                .ToListAsync();

            return attributes.ToMultimap(x => x.ProductId, x => x);
        }

        protected virtual async Task<Multimap<int, Download>> LoadDownloads(int[] ids)
        {
            var downloads = await _db.Downloads
                .AsNoTracking()
                .Include(x => x.MediaFile)
                .ApplyEntityFilter(nameof(Product), ids)
                .OrderBy(x => x.FileVersion)
                .ToListAsync();

            return downloads.ToMultimap(x => x.EntityId, x => x);
        }

        #endregion
    }
}
