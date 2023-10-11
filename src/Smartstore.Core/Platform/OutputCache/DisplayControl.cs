using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Autofac;

using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Utilities;

namespace Smartstore.Core.OutputCache
{
    public delegate Task<IEnumerable<string>> DisplayControlHandler(BaseEntity entity, SmartDbContext db, IComponentContext ctx);

    public partial class DisplayControl : IDisplayControl
    {
        private static readonly ConcurrentDictionary<Type, DisplayControlHandler> _handlers = new()
        {
            [typeof(Category)] = (x, d, c) => ToTask("c" + x.Id),
            [typeof(Manufacturer)] = (x, d, c) => ToTask("m" + x.Id),
            [typeof(ProductBundleItem)] = (x, d, c) => ToTask("p" + ((ProductBundleItem)x).ProductId),
            [typeof(ProductMediaFile)] = (x, d, c) => ToTask("p" + ((ProductMediaFile)x).ProductId),
            [typeof(ProductSpecificationAttribute)] = (x, d, c) => ToTask("p" + ((ProductSpecificationAttribute)x).ProductId),
            [typeof(ProductVariantAttributeCombination)] = (x, d, c) => ToTask("p" + ((ProductVariantAttributeCombination)x).ProductId),
            [typeof(TierPrice)] = (x, d, c) => ToTask("p" + ((TierPrice)x).ProductId),
            [typeof(CrossSellProduct)] = (x, d, c) => ToTask("p" + ((CrossSellProduct)x).ProductId1, "p" + ((CrossSellProduct)x).ProductId2),
            [typeof(RelatedProduct)] = (x, d, c) => ToTask("p" + ((RelatedProduct)x).ProductId1, "p" + ((RelatedProduct)x).ProductId2),
            [typeof(ProductCategory)] = (x, d, c) => ToTask("p" + ((ProductCategory)x).CategoryId, "p" + ((ProductCategory)x).ProductId),
            [typeof(ProductManufacturer)] = (x, d, c) => ToTask("p" + ((ProductManufacturer)x).ManufacturerId, "p" + ((ProductManufacturer)x).ProductId),
            [typeof(Topic)] = (x, d, c) => ToTask("t" + x.Id),
            [typeof(MenuEntity)] = (x, d, c) => ToTask("mnu" + x.Id),
            [typeof(MenuItemEntity)] = (x, d, c) => ToTask("mnu" + ((MenuItemEntity)x).MenuId),
            [typeof(MediaFile)] = (x, d, c) => ToTask("mf" + x.Id),
            [typeof(SpecificationAttributeOption)] = HandleSpecificationAttributeOptionsAsync,
            [typeof(ProductTag)] = HandleProductTagsAsync,
            [typeof(Product)] = HandleProductAsync,
            [typeof(SpecificationAttribute)] = HandleSpecificationAttributeAsync,
            [typeof(ProductVariantAttributeValue)] = HandleProductVariantAttributeValueAsync,
            [typeof(Discount)] = HandleDiscountAsync,
            [typeof(LocalizedProperty)] = HandleLocalizedPropertyAsync
        };

        private readonly SmartDbContext _db;
        private readonly IComponentContext _componentContext;
        private readonly HashSet<BaseEntity> _entities = new();

        private bool _isIdle;
        private bool? _isUncacheableRequest;

        public DisplayControl(SmartDbContext db, IComponentContext componentContext)
        {
            _db = db;
            _componentContext = componentContext;
        }

        #region Static

        public static bool ContainsHandlerFor(Type type)
            => _handlers.ContainsKey(Guard.NotNull(type, nameof(type)));

        public static void RegisterHandlerFor(Type type, DisplayControlHandler handler)
        {
            Guard.NotNull(type, nameof(type));
            Guard.NotNull(handler, nameof(handler));

            _handlers.TryAdd(type, handler);
        }

        #endregion

        #region Handlers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Task<IEnumerable<string>> ToTask(params string[] tags)
        {
            return Task.FromResult<IEnumerable<string>>(tags);
        }

        private static Task<IEnumerable<string>> HandleProductAsync(BaseEntity entity, SmartDbContext db, IComponentContext ctx)
            => Task.FromResult(ProductTagIterator((Product)entity));

        private static IEnumerable<string> ProductTagIterator(Product product)
        {
            yield return "p" + product.Id;
            if (product.ProductType == ProductType.GroupedProduct && product.ParentGroupedProductId > 0)
            {
                yield return "p" + product.ParentGroupedProductId;
            }
        }

        private static async Task<IEnumerable<string>> HandleProductTagsAsync(BaseEntity entity, SmartDbContext db, IComponentContext ctx)
        {
            var tag = (ProductTag)entity;
            IEnumerable<int> productIds = null;

            if (db.IsCollectionLoaded(tag, x => x.Products, out var entry))
            {
                productIds = tag.Products.Select(x => x.Id);
            }
            else if (entry != null)
            {
                productIds = await entry.Query().Select(x => x.Id).ToListAsync();
            }

            if (productIds != null)
            {
                return productIds.Select(id => "p" + id);
            }

            return Enumerable.Empty<string>();
        }

        private static async Task<IEnumerable<string>> HandleSpecificationAttributeAsync(BaseEntity entity, SmartDbContext db, IComponentContext ctx)
        {
            // Determine all affected products (which are assigned to this attribute).
            var specAttrId = ((SpecificationAttribute)entity).Id;
            var affectedProductIds = await db.ProductSpecificationAttributes
                .AsNoTracking()
                .Where(x => x.SpecificationAttributeOption.SpecificationAttributeId == specAttrId)
                .Select(x => x.ProductId)
                .Distinct()
                .ToListAsync();

            return affectedProductIds.Select(id => "p" + id);
        }

        private static async Task<IEnumerable<string>> HandleSpecificationAttributeOptionsAsync(BaseEntity entity, SmartDbContext db, IComponentContext ctx)
        {
            var option = (SpecificationAttributeOption)entity;
            IEnumerable<int> productIds = null;

            if (db.IsCollectionLoaded(option, x => x.ProductSpecificationAttributes, out var entry))
            {
                productIds = option.ProductSpecificationAttributes.Select(x => x.ProductId);
            }
            else if (entry != null)
            {
                productIds = await entry.Query().Select(x => x.ProductId).ToListAsync();
            }

            if (productIds != null)
            {
                return productIds.Select(id => "p" + id);
            }

            return Enumerable.Empty<string>();
        }

        private static async Task<IEnumerable<string>> HandleProductVariantAttributeValueAsync(BaseEntity entity, SmartDbContext db, IComponentContext ctx)
        {
            var value = ((ProductVariantAttributeValue)entity);
            await db.LoadReferenceAsync(value, x => x.ProductVariantAttribute);
            var pva = value.ProductVariantAttribute;

            if (pva != null)
            {
                return new[] { "p" + pva.ProductId };
            }

            return Enumerable.Empty<string>();
        }

        private static async Task<IEnumerable<string>> HandleDiscountAsync(BaseEntity entity, SmartDbContext db, IComponentContext ctx)
        {
            var discount = (Discount)entity;
            if (discount.DiscountType == DiscountType.AssignedToCategories)
            {
                await db.LoadCollectionAsync(discount, x => x.AppliedToCategories);
                return discount.AppliedToCategories.Select(category => "c" + category.Id);
            }
            else if (discount.DiscountType == DiscountType.AssignedToSkus)
            {
                await db.LoadCollectionAsync(discount, x => x.AppliedToProducts);
                return discount.AppliedToProducts.Select(product => "p" + product.Id);
            }

            return Enumerable.Empty<string>();
        }

        private static async Task<IEnumerable<string>> HandleLocalizedPropertyAsync(BaseEntity entity, SmartDbContext db, IComponentContext ctx)
        {
            var lp = (LocalizedProperty)entity;
            string prefix = null;
            BaseEntity targetEntity = null;

            switch (lp.LocaleKeyGroup)
            {
                case nameof(Product):
                    prefix = "p";
                    break;
                case nameof(Category):
                    prefix = "c";
                    break;
                case nameof(Manufacturer):
                    prefix = "m";
                    break;
                case nameof(Topic):
                    prefix = "t";
                    break;
                case nameof(MediaFile):
                    prefix = "mf";
                    break;
                case nameof(SpecificationAttribute):
                    targetEntity = await db.SpecificationAttributes.FindByIdAsync(lp.EntityId, false);
                    break;
                case nameof(SpecificationAttributeOption):
                    targetEntity = await db.SpecificationAttributeOptions.FindByIdAsync(lp.EntityId, false);
                    break;
                case nameof(ProductVariantAttributeValue):
                    targetEntity = await db.ProductVariantAttributeValues.FindByIdAsync(lp.EntityId, false);
                    break;
            }

            if (prefix.HasValue())
            {
                return new[] { prefix + lp.EntityId };
            }
            else if (targetEntity != null)
            {
                return await ctx.Resolve<IDisplayControl>().GetCacheControlTagsForAsync(targetEntity);
            }

            return Enumerable.Empty<string>();
        }

        #endregion

        public IDisposable BeginIdleScope()
        {
            _isIdle = true;
            return new ActionDisposable(() => _isIdle = false);
        }

        public virtual void Announce(BaseEntity entity)
        {
            if (!_isIdle && entity != null)
            {
                _entities.Add(entity);
            }
        }

        public bool IsDisplayed(BaseEntity entity)
        {
            if (entity == null)
                return false;

            return _entities.Contains(entity);
        }

        public void MarkRequestAsUncacheable()
        {
            // First wins: subsequent calls should not be able to cancel this
            if (!_isIdle)
                _isUncacheableRequest = true;
        }

        public bool IsUncacheableRequest
            => _isUncacheableRequest.GetValueOrDefault() == true;

        public virtual Task<IEnumerable<string>> GetCacheControlTagsForAsync(BaseEntity entity)
        {
            var empty = Enumerable.Empty<string>();

            if (entity == null || entity.IsTransientRecord())
            {
                return Task.FromResult(empty);
            }

            if (!_handlers.TryGetValue(entity.GetType(), out var handler))
            {
                return Task.FromResult(empty);
            }

            return handler.Invoke(entity, _db, _componentContext);
        }

        public async Task<string[]> GetAllCacheControlTagsAsync()
        {
            var allTags = new HashSet<string>();

            foreach (var entity in _entities)
            {
                if (entity.Id > 0)
                {
                    var entityTags = await GetCacheControlTagsForAsync(entity);
                    if (entityTags != null)
                    {
                        foreach (var tag in entityTags)
                        {
                            allTags.Add(tag);
                        }
                    }
                }
            }

            return allTags.ToArray();
        }
    }
}