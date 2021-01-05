using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Common;
using Smartstore.Core.Domain.Catalog;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Catalog.Products
{
    public static partial class ProductExtensions
    {
        // TODO: (mg) (core) Add MergeWithCombination extension method for products.
        //public static ProductVariantAttributeCombination MergeWithCombination(this Product product, string selectedAttributes, IProductAttributeParser productAttributeParser)
        //{
        //    Guard.NotNull(productAttributeParser, nameof(productAttributeParser));

        //    if (selectedAttributes.IsEmpty())
        //    {
        //        return null;
        //    }

        //    // Let's find appropriate record.
        //    var combination = productAttributeParser.FindProductVariantAttributeCombination(product.Id, selectedAttributes);

        //    if (combination != null && combination.IsActive)
        //    {
        //        product.MergeWithCombination(combination);
        //    }
        //    else if (product.MergedDataValues != null)
        //    {
        //        product.MergedDataValues.Clear();
        //    }

        //    return combination;
        //}

        /// <summary>
        /// Merges the data of an attribute combination with those of the product.
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
        public static async Task<string> FormatStockMessageAsync(this Product product, ILocalizationService localizationService)
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
                        var str = await localizationService.GetResourceAsync("Products.Availability.InStockWithQuantity");
                        stockMessage = string.Format(str, product.StockQuantity);
                    }
                    else
                    {
                        stockMessage = await localizationService.GetResourceAsync("Products.Availability.InStock");
                    }
                }
                else
                {
                    if (product.BackorderMode == BackorderMode.NoBackorders || product.BackorderMode == BackorderMode.AllowQtyBelow0)
                    {
                        stockMessage = await localizationService.GetResourceAsync("Products.Availability.OutOfStock");
                    }
                    else if (product.BackorderMode == BackorderMode.AllowQtyBelow0AndNotifyCustomer)
                    {
                        stockMessage = await localizationService.GetResourceAsync("Products.Availability.Backordering");
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
                .SplitSafe(",")
                .Select(x => int.TryParse(x.Trim(), out var quantity) ? quantity : int.MaxValue)
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
                .SplitSafe(",")
                .Select(x => int.TryParse(x.Trim(), out var id) ? id : int.MaxValue)
                .Where(x => x != int.MaxValue)
                .ToArray();
        }

        // TODO: (mg) (core) Add GetBasePriceInfoAsync extension method for products.
        /// <summary>
        /// Gets the base price info.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <param name="localizationService">Localization service.</param>
        /// <param name="priceFormatter">Price formatter.</param>
		/// <param name="currencyService">Currency service.</param>
		/// <param name="taxService">Tax service.</param>
		/// <param name="priceCalculationService">Price calculation service.</param>
		/// <param name="customer">Customer entity.</param>
		/// <param name="currency">Target currency.</param>
		/// <param name="priceAdjustment">Price adjustment.</param>
        /// <returns>The base price info</returns>
        //public static async Task<string> GetBasePriceInfoAsync(this Product product,
        //    ILocalizationService localizationService,
        //    IPriceFormatter priceFormatter,
        //    ICurrencyService currencyService,
        //    ITaxService taxService,
        //    IPriceCalculationService priceCalculationService,
        //    Customer customer,
        //    Currency currency,
        //    decimal priceAdjustment = decimal.Zero)
        //{
        //    Guard.NotNull(product, nameof(product));
        //    Guard.NotNull(currencyService, nameof(currencyService));
        //    Guard.NotNull(taxService, nameof(taxService));
        //    Guard.NotNull(priceCalculationService, nameof(priceCalculationService));
        //    Guard.NotNull(customer, nameof(customer));
        //    Guard.NotNull(currency, nameof(currency));

        //    if (product.BasePriceHasValue && product.BasePriceAmount != decimal.Zero)
        //    {
        //        var currentPrice = priceCalculationService.GetFinalPrice(product, customer, true);
        //        var price = taxService.GetProductPrice(product, decimal.Add(currentPrice, priceAdjustment), customer, currency, out var taxrate);
        //        price = currencyService.ConvertFromPrimaryStoreCurrency(price, currency);

        //        return await product.GetBasePriceInfoAsync(price, localizationService, priceFormatter, currency);
        //    }

        //    return string.Empty;
        //}

        /// <summary>
        /// Gets the base price info.
        /// </summary>
        /// <param name="product">Product entity.</param>
        /// <param name="productPrice">The calculated product price.</param>
        /// <param name="localizationService">Localization service.</param>
        /// <param name="priceFormatter">Price formatter.</param>
        /// <param name="currency">Target currency.</param>
        /// <returns>The base price info</returns>
        public static string GetBasePriceInfo(this Product product,
            decimal productPrice,
            ILocalizationService localizationService,
            IPriceFormatter priceFormatter,
            Currency currency)
        {
            // TODO: (mg) Move GetBasePriceInfo() extension methods to IPriceFormatter or any other applicable service.

            Guard.NotNull(product, nameof(product));
            Guard.NotNull(localizationService, nameof(localizationService));
            Guard.NotNull(priceFormatter, nameof(priceFormatter));
            Guard.NotNull(currency, nameof(currency));

            if (product.BasePriceHasValue && product.BasePriceAmount != decimal.Zero)
            {
                var value = Convert.ToDecimal((productPrice / product.BasePriceAmount) * product.BasePriceBaseAmount);
                var valueFormatted = priceFormatter.FormatPrice(value, true, currency);
                var amountFormatted = Math.Round(product.BasePriceAmount.Value, 2).ToString("G29");
                var infoTemplate = localizationService.GetResource("Products.BasePriceInfo");

                var result = infoTemplate.FormatInvariant(
                    amountFormatted,
                    product.BasePriceMeasureUnit,
                    valueFormatted,
                    product.BasePriceBaseAmount
                );

                return result;
            }

            return string.Empty;
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
