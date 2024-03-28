using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Catalog.Products
{
    public static partial class ProductExtensions
    {
        /// <summary>
        /// Applies data of an attribute combination to the product.
        /// </summary>
        /// <param name="product">Target product entity.</param>
        /// <param name="combination">Source attribute combination.</param>
        public static void MergeWithCombination(this Product product, ProductVariantAttributeCombination combination)
        {
            Guard.NotNull(product);

            var values = product.MergedDataValues;

            values?.Clear();

            if (combination == null)
                return;

            if (values == null)
                product.MergedDataValues = values = [];

            if (ManageInventoryMethod.ManageStockByAttributes == (ManageInventoryMethod)product.ManageInventoryMethodId)
            {
                values.Add("StockQuantity", combination.StockQuantity);
                values.Add("BackorderModeId", combination.AllowOutOfStockOrders ? (int)BackorderMode.AllowQtyBelow0 : (int)BackorderMode.NoBackorders);
            }

            if (combination.Sku.HasValue())
                values.Add("Sku", combination.Sku);
            if (combination.Gtin.HasValue())
                values.Add("Gtin", combination.Gtin);
            if (combination.ManufacturerPartNumber.HasValue())
                values.Add("ManufacturerPartNumber", combination.ManufacturerPartNumber);

            if (combination.Price.HasValue)
                values.Add("Price", combination.Price.Value);

            if (combination.DeliveryTimeId.HasValue && combination.DeliveryTimeId.Value > 0)
                values.Add("DeliveryTimeId", combination.DeliveryTimeId);

            if (combination.QuantityUnitId.HasValue && combination.QuantityUnitId.Value > 0)
                values.Add("QuantityUnitId", combination.QuantityUnitId);

            if (combination.Length.HasValue)
                values.Add("Length", combination.Length.Value);
            if (combination.Width.HasValue)
                values.Add("Width", combination.Width.Value);
            if (combination.Height.HasValue)
                values.Add("Height", combination.Height.Value);

            if (combination.BasePriceAmount.HasValue)
                values.Add("BasePriceAmount", combination.BasePriceAmount);
            if (combination.BasePriceBaseAmount.HasValue)
                values.Add("BasePriceBaseAmount", combination.BasePriceBaseAmount);
        }

        /// <summary>
        /// Gets a value indicating whether the product is available by stock.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <returns>A value indicating whether the product is available by stock</returns>
        public static bool IsAvailableByStock(this Product product)
        {
            Guard.NotNull(product);

            if (product.ManageInventoryMethod != ManageInventoryMethod.DontManageStock && product.StockQuantity <= 0 && product.BackorderMode == BackorderMode.NoBackorders)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Formats a message for stock availability.
        /// </summary>
		/// <param name="product">Product entity.</param>
        /// <param name="localizationService">Localization service.</param>
        /// <returns>Product stock message.</returns>
        public static string FormatStockMessage(this Product product, ILocalizationService localizationService)
        {
            Guard.NotNull(product);
            Guard.NotNull(localizationService);

            var stockMessage = string.Empty;

            if (product.ManageInventoryMethod != ManageInventoryMethod.DontManageStock && product.DisplayStockAvailability)
            {
                if (product.StockQuantity > 0)
                {
                    if (product.DisplayStockQuantity)
                    {
                        var str = localizationService.GetResource("Products.Availability.InStockWithQuantity");
                        stockMessage = str.FormatInvariant(product.StockQuantity.ToString("N0"));
                    }
                    else
                    {
                        stockMessage = localizationService.GetResource("Products.Availability.InStock");
                    }
                }
                else
                {
                    if (product.BackorderMode == BackorderMode.NoBackorders || product.BackorderMode == BackorderMode.AllowQtyBelow0)
                    {
                        stockMessage = localizationService.GetResource("Products.Availability.OutOfStock");
                    }
                    else if (product.BackorderMode == BackorderMode.AllowQtyBelow0OnBackorder)
                    {
                        stockMessage = localizationService.GetResource("Products.Availability.Backordering");
                    }
                }
            }

            return stockMessage;
        }

        /// <summary>
        /// Gets a value indicating whether to display the delivery time according to stock quantity.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <param name="catalogSettings">Catalog settings.</param>
        /// <param name="isStockManaged">
        /// A value indicating whether the stock of the product is managed.
        /// Will be obtained via <see cref="Product.ManageInventoryMethod"/> if <c>null</c>.
        /// </param>
        /// <returns>A value indicating whether to display the delivery time according to stock quantity.</returns>
        public static bool DisplayDeliveryTimeAccordingToStock(this Product product, CatalogSettings catalogSettings, bool? isStockManaged = null)
        {
            Guard.NotNull(product);
            Guard.NotNull(catalogSettings);

            isStockManaged ??= product.ManageInventoryMethod != ManageInventoryMethod.DontManageStock;

            if (isStockManaged == true)
            {
                return product.StockQuantity > 0 || (product.StockQuantity <= 0 && catalogSettings.DeliveryTimeIdForEmptyStock.HasValue);
            }

            return true;
        }

        /// <summary>
        /// Gets the delivery time identifier according to stock quantity.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <param name="catalogSettings">Catalog settings.</param>
        /// <param name="isStockManaged">
        /// A value indicating whether the stock of the product is managed.
        /// Will be obtained via <see cref="Product.ManageInventoryMethod"/> if <c>null</c>.
        /// </param>
        /// <returns>The delivery time identifier according to stock quantity. <c>null</c> if not specified.</returns>
        public static int? GetDeliveryTimeIdAccordingToStock(this Product product, CatalogSettings catalogSettings, bool? isStockManaged = null)
        {
            Guard.NotNull(catalogSettings);

            if (product == null)
            {
                return null;
            }

            isStockManaged ??= product.ManageInventoryMethod != ManageInventoryMethod.DontManageStock;

            if (isStockManaged == true && catalogSettings.DeliveryTimeIdForEmptyStock.HasValue && product.StockQuantity <= 0)
            {
                return catalogSettings.DeliveryTimeIdForEmptyStock.Value;
            }

            return product.DeliveryTimeId;
        }

        /// <summary>
        /// Gets a value indicating whether the product is labeled as NEW.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <param name="catalogSettings">Catalog settings.</param>
        /// <returns>A alue indicating whether the product is labeled as NEW.</returns>
        public static bool IsNew(this Product product, CatalogSettings catalogSettings)
        {
            if (catalogSettings.LabelAsNewForMaxDays.HasValue)
            {
                return (DateTime.UtcNow - product.CreatedOnUtc).Days <= catalogSettings.LabelAsNewForMaxDays.Value;
            }

            return false;
        }

        /// <summary>
        /// Gets a list of allowed quantities.
        /// </summary>
		/// <param name="product">Product entity.</param>
        /// <returns>List of allowed quantities.</returns>
		public static int[] ParseAllowedQuantities(this Product product)
        {
            Guard.NotNull(product);

            if (product.AllowedQuantities.IsEmpty())
            {
                return [];
            }

            return product.AllowedQuantities
                .SplitSafe(',', StringSplitOptions.TrimEntries)
                .Select(x => int.TryParse(x, out var quantity) ? quantity : int.MaxValue)
                .Where(x => x != int.MaxValue)
                .OrderBy(x => x)
                .ToArray();
        }

        /// <summary>
        /// Gets the lowest possible order quantity for a given product,
        /// which is either <see cref="Product.OrderMinimumQuantity"/> or the first item
        /// in <see cref="Product.AllowedQuantities" />.
        /// </summary>
        /// <param name="product">The product to get min order quantity for.</param>
        public static int GetMinOrderQuantity(this Product product)
        {
            Guard.NotNull(product);

            var allowedQuantities = ParseAllowedQuantities(product);
            return Math.Max(1, allowedQuantities.Length > 0 ? allowedQuantities[0] : product.OrderMinimumQuantity);
        }

        /// <summary>
        /// Gets the highest possible order quantity for a given product,
        /// which is either <see cref="Product.OrderMaximumQuantity"/> or the last item
        /// in <see cref="Product.AllowedQuantities" />.
        /// </summary>
        /// <param name="product">The product to get min order quantity for.</param>
        public static int GetMaxOrderQuantity(this Product product)
        {
            Guard.NotNull(product);

            var allowedQuantities = ParseAllowedQuantities(product);
            return allowedQuantities.Length > 0 ? allowedQuantities.Last() : product.OrderMaximumQuantity;
        }

        /// <summary>
        /// Gets a list of required product identifiers.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <returns>List of required product identifiers.</returns>
        public static int[] ParseRequiredProductIds(this Product product)
        {
            Guard.NotNull(product);

            return product.RequiredProductIds
                .SplitSafe(',', StringSplitOptions.TrimEntries)
                .Select(x => int.TryParse(x, out var id) ? id : int.MaxValue)
                .Where(x => x < int.MaxValue)
                .ToArray();
        }

        public static string GetProductTypeLabel(this Product product, ILocalizationService localizationService)
        {
            if (product != null && product.ProductType != ProductType.SimpleProduct)
            {
                var key = "Admin.Catalog.Products.ProductType.{0}.Label".FormatInvariant(product.ProductType.ToStringInvariant());
                return localizationService.GetResource(key);
            }

            return string.Empty;
        }

        public static bool CanBeBundleItem(this Product product)
        {
            return product != null && product.ProductType == ProductType.SimpleProduct && !product.IsRecurring && !product.IsDownload;
        }
    }
}
