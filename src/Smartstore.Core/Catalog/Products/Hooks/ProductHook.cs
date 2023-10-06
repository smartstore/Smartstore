using AngleSharp.Dom;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;
using EState = Smartstore.Data.EntityState;

namespace Smartstore.Core.Catalog.Products
{
    [Important]
    internal class ProductHook : AsyncDbSaveHook<Product>
    {
        private readonly SmartDbContext _db;
        private readonly Lazy<IStockSubscriptionService> _stockSubscriptionService;
        private readonly Lazy<IProductService> _productService;

        private readonly HashSet<BaseEntity> _toSendStockNotification = new();
        private readonly HashSet<BaseEntity> _toAdjustInventory = new();
        private readonly HashSet<int> _oldSampleDownloadIds = new();
        private string _hookErrorMessage;

        public ProductHook(
            SmartDbContext db,
            Lazy<IStockSubscriptionService> stockSubscriptionService,
            Lazy<IProductService> productService)
        {
            _db = db;
            _stockSubscriptionService = stockSubscriptionService;
            _productService = productService;
        }

        protected override Task<HookResult> OnInsertedAsync(Product entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        // We are overriding OnUpdatedAsync because SoftDeletableHook is pre-processing the entity and updating its entity state.
        protected override Task<HookResult> OnUpdatedAsync(Product entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override Task<HookResult> OnBeforeSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
        {
            if (entry.State == EState.Modified && entry.Entity is Product p)
            {
                if (p.ManageInventoryMethod == ManageInventoryMethod.ManageStock)
                {
                    var prop = entry.Entry.Property(nameof(Product.StockQuantity));
                    if (prop?.CurrentValue != null && prop?.OriginalValue != null)
                    {
                        var stockQuantityInDatabase = (int)prop.OriginalValue;
                        var newStockQuantity = (int)prop.CurrentValue;

                        if (p.BackorderMode == BackorderMode.NoBackorders
                            && p.AllowBackInStockSubscriptions
                            && p.Published
                            && !p.Deleted
                            && !p.IsSystemProduct
                            && newStockQuantity > 0
                            && stockQuantityInDatabase <= 0)
                        {
                            _toSendStockNotification.Add(entry.Entity);
                        }

                        if (newStockQuantity != stockQuantityInDatabase)
                        {
                            _toAdjustInventory.Add(entry.Entity);
                        }
                    }
                }

                var sampleDownloadIdProp = entry.Entry.Property(nameof(Product.SampleDownloadId));
                var oldDownloadId = sampleDownloadIdProp.OriginalValue != null ? (int)sampleDownloadIdProp.OriginalValue : 0;
                var newDownloadId = sampleDownloadIdProp.CurrentValue != null ? (int)sampleDownloadIdProp.CurrentValue : 0;

                if (oldDownloadId != 0 && oldDownloadId != newDownloadId)
                {
                    _oldSampleDownloadIds.Add(oldDownloadId);
                }

                var parentIdProp = entry.Entry.Property(nameof(Product.ParentGroupedProductId));
                var newParentId = parentIdProp.CurrentValue != null ? (int)parentIdProp.CurrentValue : 0;

                if (newParentId != 0 && p.Id == newParentId)
                {
                    entry.ResetState();
                    _hookErrorMessage = $"The product {p.Id} cannot be assigned to itself via the property {nameof(Product.ParentGroupedProductId)}.";
                }
            }

            return Task.FromResult(HookResult.Ok);
        }

        public override Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_hookErrorMessage.HasValue())
            {
                var message = new string(_hookErrorMessage);
                _hookErrorMessage = null;

                throw new HookException(message);
            }

            return Task.CompletedTask;
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var softDeletedProducts = entries
                .Where(x => x.IsSoftDeleted == true)
                .Select(x => x.Entity)
                .OfType<Product>()
                .ToList();

            if (softDeletedProducts.Count > 0)
            {
                foreach (var softDeletedProduct in softDeletedProducts)
                {
                    softDeletedProduct.Deleted = true;
                    softDeletedProduct.DeliveryTimeId = null;
                    softDeletedProduct.QuantityUnitId = null;
                    softDeletedProduct.CountryOfOriginId = null;
                    softDeletedProduct.ComparePriceLabelId = null;
                }

                await _db.SaveChangesAsync(cancelToken);

                // Unassign grouped products.
                var groupedProductIds = softDeletedProducts
                    .Where(x => x.ProductType == ProductType.GroupedProduct)
                    .ToDistinctArray(x => x.Id);

                if (groupedProductIds.Length > 0)
                {
                    var allAssociatedProducts = await _db.Products
                        .IgnoreQueryFilters()
                        .Where(x => groupedProductIds.Contains(x.ParentGroupedProductId))
                        .ExecuteUpdateAsync(x => x.SetProperty(p => p.ParentGroupedProductId, p => 0), cancelToken);
                }
            }

            // Update HasDiscountsApplied property.
            var products = entries
                .Select(x => x.Entity)
                .OfType<Product>()
                .ToList();

            // Process in batches to avoid errors due to too long SQL statements.
            foreach (var productsChunk in products.Chunk(100))
            {
                var productIdsChunk = productsChunk
                    .Select(x => x.Id)
                    .ToArray();

                var appliedProductIds = await _db.Discounts
                    .SelectMany(x => x.AppliedToProducts)
                    .Where(x => productIdsChunk.Contains(x.Id))
                    .Select(x => x.Id)
                    .Distinct()
                    .ToListAsync(cancelToken);

                productsChunk.Each(x => x.HasDiscountsApplied = appliedProductIds.Contains(x.Id));
            }

            await _db.SaveChangesAsync(cancelToken);

            // Set old sample downloads to transient.
            if (_oldSampleDownloadIds.Count > 0)
            {
                await _db.Downloads
                    .Where(x => _oldSampleDownloadIds.Contains(x.Id))
                    .ExecuteUpdateAsync(x => x.SetProperty(d => d.IsTransient, d => true), cancelToken);
            }

            if (_toAdjustInventory.Count > 0)
            {
                foreach (var entity in _toAdjustInventory)
                {
                    await _productService.Value.AdjustInventoryAsync(entity as Product, null, true, 0);
                }

                await _db.SaveChangesAsync(cancelToken);
                _toAdjustInventory.Clear();
            }

            foreach (var entity in _toSendStockNotification)
            {
                await _stockSubscriptionService.Value.SendNotificationsToSubscribersAsync(entity as Product);
            }

            _toSendStockNotification.Clear();
        }
    }
}