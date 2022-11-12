using System.Globalization;
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
            Guard.NotNull(product, nameof(product));

            var values = product.MergedDataValues;

            if (values != null)
                values.Clear();

            if (combination == null)
                return;

            if (values == null)
                product.MergedDataValues = values = new Dictionary<string, object>();

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
            Guard.NotNull(product, nameof(product));

            if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock || product.ManageInventoryMethod == ManageInventoryMethod.ManageStockByAttributes)
            {
                if (product.StockQuantity <= 0 && product.BackorderMode == BackorderMode.NoBackorders)
                {
                    return false;
                }
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
            Guard.NotNull(product, nameof(product));
            Guard.NotNull(localizationService, nameof(localizationService));

            var stockMessage = string.Empty;

            if ((product.ManageInventoryMethod == ManageInventoryMethod.ManageStock || product.ManageInventoryMethod == ManageInventoryMethod.ManageStockByAttributes)
                && product.DisplayStockAvailability)
            {
                if (product.StockQuantity > 0)
                {
                    if (product.DisplayStockQuantity)
                    {
                        var str = localizationService.GetResource("Products.Availability.InStockWithQuantity");
                        stockMessage = str.FormatCurrent(product.StockQuantity);
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
                    else if (product.BackorderMode == BackorderMode.AllowQtyBelow0AndNotifyCustomer)
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
        /// <returns>A value indicating whether to display the delivery time according to stock quantity.</returns>
        public static bool DisplayDeliveryTimeAccordingToStock(this Product product, CatalogSettings catalogSettings)
        {
            Guard.NotNull(product, nameof(product));
            Guard.NotNull(catalogSettings, nameof(catalogSettings));

            if (product.ManageInventoryMethod == ManageInventoryMethod.ManageStock || product.ManageInventoryMethod == ManageInventoryMethod.ManageStockByAttributes)
            {
                if (catalogSettings.DeliveryTimeIdForEmptyStock.HasValue && product.StockQuantity <= 0)
                {
                    return true;
                }

                return product.StockQuantity > 0;
            }

            return true;
        }

        /// <summary>
        /// Gets the delivery time identifier according to stock quantity.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <param name="catalogSettings">Catalog settings.</param>
        /// <returns>The delivery time identifier according to stock quantity. <c>null</c> if not specified.</returns>
        public static int? GetDeliveryTimeIdAccordingToStock(this Product product, CatalogSettings catalogSettings)
        {
            Guard.NotNull(catalogSettings, nameof(catalogSettings));

            if (product == null)
            {
                return null;
            }

            if ((product.ManageInventoryMethod == ManageInventoryMethod.ManageStock || product.ManageInventoryMethod == ManageInventoryMethod.ManageStockByAttributes)
                && catalogSettings.DeliveryTimeIdForEmptyStock.HasValue
                && product.StockQuantity <= 0)
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
            Guard.NotNull(product, nameof(product));

            return product.AllowedQuantities
                .SplitSafe(',', StringSplitOptions.TrimEntries)
                .Select(x => int.TryParse(x, out var quantity) ? quantity : int.MaxValue)
                .Where(x => x != int.MaxValue)
                .ToArray();
        }

        /// <summary>
        /// Gets a list of required product identifiers.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <returns>List of required product identifiers.</returns>
        public static int[] ParseRequiredProductIds(this Product product)
        {
            Guard.NotNull(product, nameof(product));

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
                var key = "Admin.Catalog.Products.ProductType.{0}.Label".FormatInvariant(product.ProductType.ToString());
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
