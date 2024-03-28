using System.Runtime.CompilerServices;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Localization;
using Smartstore.Diagnostics;
using Smartstore.Web.Models.Catalog;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.Controllers
{
    public partial class CatalogHelper
    {
        #region Details price modelling

        protected async Task PrepareProductPriceModelAsync(ProductDetailsModel model, ProductDetailsModelContext modelContext, int selectedQuantity)
        {
            using var chronometer = _services.Chronometer.Step("PrepareProductPriceModel");

            var priceModel = model.Price;
            var customer = modelContext.Customer;
            var currency = modelContext.Currency;
            var product = modelContext.Product;
            var productBundleItem = modelContext.ProductBundleItem;
            var bundleItemId = productBundleItem?.Id;
            var isBundle = product.ProductType == ProductType.BundledProduct;
            var isBundleItemPricing = productBundleItem != null && productBundleItem.BundleProduct.BundlePerItemPricing;
            var isBundlePricing = productBundleItem != null && !productBundleItem.BundleProduct.BundlePerItemPricing;

            priceModel.HidePrices = !modelContext.DisplayPrices;
            priceModel.ShowLoginNote = !modelContext.DisplayPrices && productBundleItem == null && _priceSettings.ShowLoginForPriceNote;
            priceModel.BundleItemShowBasePrice = _priceSettings.BundleItemShowBasePrice;

            if (!modelContext.DisplayPrices)
            {
                return;
            }

            if (isBundlePricing)
            {
                // Do not show any bundle item price if parent bundle has bundle pricing.
                return;
            }

            var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, customer, currency, modelContext.BatchContext);
            var calculationContext = new PriceCalculationContext(product, selectedQuantity, calculationOptions)
            {
                AssociatedProducts = modelContext.AssociatedProducts,
                BundleItem = productBundleItem
            };

            // Apply price adjustments of attributes.
            if (modelContext.SelectedAttributes != null)
            {
                // Apply price adjustments of selected attributes.
                calculationContext.AddSelectedAttributes(modelContext.SelectedAttributes, product.Id, bundleItemId);
            }
            else if (isBundle && product.BundlePerItemPricing && modelContext.VariantQuery.Variants.Count > 0)
            {
                // Apply price adjustments of selected bundle items attributes.
                // INFO: bundles themselves don't have attributes, that's why modelContext.SelectedAttributes is null.
                calculationContext.BundleItems = await modelContext.BatchContext.ProductBundleItems.GetOrLoadAsync(product.Id);

                modelContext.BatchContext.Collect(calculationContext.BundleItems.Select(x => x.ProductId).ToArray());

                foreach (var bundleItem in calculationContext.BundleItems)
                {
                    var bundleItemAttributes = await modelContext.BatchContext.Attributes.GetOrLoadAsync(bundleItem.ProductId);
                    var (selection, _) = await _productAttributeMaterializer.CreateAttributeSelectionAsync(modelContext.VariantQuery, bundleItemAttributes, bundleItem.ProductId, bundleItem.Id, false);

                    calculationContext.AddSelectedAttributes(selection, bundleItem.ProductId, bundleItem.Id);

                    // Apply attribute combination price if any.
                    await _productAttributeMaterializer.MergeWithCombinationAsync(bundleItem.Product, selection);
                }
            }
            else
            {
                // Apply price adjustments of attributes preselected by merchant.
                calculationContext.Options.ApplyPreselectedAttributes = true;
            }

            // Calculate unit price now
            var calculatedPrice = await _priceCalculationService.CalculatePriceAsync(calculationContext);
            
            // Map base
            MapPriceBase(calculatedPrice, priceModel, true);

            if ((priceModel.CallForPrice || priceModel.CustomerEntersPrice) && !isBundleItemPricing)
            {
                if (priceModel.CallForPrice)
                {
                    model.HotlineTelephoneNumber = _contactDataSettings.HotlineTelephoneNumber.NullEmpty();
                }
                return;
            }

            // Countdown text
            priceModel.CountdownText = _priceLabelService.GetPromoCountdownText(calculatedPrice);

            // Offer badges
            if (_priceSettings.ShowOfferBadge)
            {
                // Add default promo badges as configured
                AddPromoBadge(calculatedPrice, priceModel.Badges);
            }

            // Bundle per item pricing stuff
            if (isBundle && product.BundlePerItemPricing)
            {
                if (priceModel.RegularPrice != null)
                {
                    // Change regular price label: "Regular/Lowest" --> "Instead of"
                    priceModel.RegularPrice.Label = T("Products.Bundle.PriceWithoutDiscount.Note");
                }
                
                // Add promo badge for bundle: "As bundle only"
                if (calculatedPrice.Saving.HasSaving && !product.HasTierPrices)
                {
                    priceModel.Badges.Add(new ProductBadgeModel
                    {
                        Label = T("Products.Bundle.PriceWithDiscount.Note"),
                        Style = "success"
                    });
                }
            }
            else
            {
                // Tier prices are ignored for bundles with per-item pricing
                await PrepareTierPriceModelAsync(priceModel, modelContext);
            }
        }

        private async Task PrepareTierPriceModelAsync(ProductDetailsPriceModel model, ProductDetailsModelContext modelContext)
        {
            var product = modelContext.Product;

            var tierPrices = product.TierPrices
                .FilterByStore(modelContext.Store.Id)
                .FilterForCustomer(modelContext.Customer)
                .OrderBy(x => x.Quantity)
                .ToList()
                .RemoveDuplicatedQuantities();

            if (!tierPrices.Any())
            {
                return;
            }

            var calculationOptions = _priceCalculationService.CreateDefaultOptions(false, modelContext.Customer, modelContext.Currency, modelContext.BatchContext);
            calculationOptions.TaxFormat = null;

            var calculationContext = new PriceCalculationContext(product, 1, calculationOptions)
            {
                AssociatedProducts = modelContext.AssociatedProducts,
                BundleItem = modelContext.ProductBundleItem
            };

            calculationContext.AddSelectedAttributes(modelContext.SelectedAttributes, product.Id, modelContext.ProductBundleItem?.Id);

            var tierPriceModels = await tierPrices
                .SelectAwait(async (tierPrice) =>
                {
                    calculationContext.Quantity = tierPrice.Quantity;

                    var price = await _priceCalculationService.CalculatePriceAsync(calculationContext);

                    var tierPriceModel = new TierPriceModel
                    {
                        Quantity = tierPrice.Quantity,
                        Price = price.FinalPrice
                    };

                    return tierPriceModel;
                })
                .AsyncToList();

            if (tierPriceModels.Count > 0)
            {
                model.TierPrices.AddRange(tierPriceModels);
            }
        }

        #endregion

        #region Summary price modelling

        /// <returns>
        /// The context product: either passed <paramref name="product"/> or the first associated product of a group
        /// </returns>
        protected async Task<Product> MapSummaryItemPrice(Product product, ProductSummaryItemModel model, ProductSummaryItemContext context)
        {
            var options = context.CalculationOptions;
            var batchContext = context.BatchContext;
            var contextProduct = product;
            var priceModel = model.Price;

            ICollection<Product> associatedProducts = null;

            // Reset child products batch context.
            options.ChildProductsBatchContext = null;

            if (product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing)
            {
                if (!batchContext.ProductBundleItems.FullyLoaded)
                {
                    await batchContext.ProductBundleItems.LoadAllAsync();
                }

                if (context.BundleItemBatchContext == null)
                {
                    // One-time batched retrieval of all bundle items.
                    var bundleItemProductIds = batchContext.ProductBundleItems
                        .SelectMany(x => x.Value)
                        .Where(x => x.BundleProduct.BundlePerItemPricing)
                        .ToDistinctArray(x => x.ProductId);

                    var bundleItemProducts = await _db.Products.GetManyAsync(bundleItemProductIds);
                    context.BundleItemBatchContext = _productService.CreateProductBatchContext(bundleItemProducts, options.Store, options.Customer, false);
                }

                options.ChildProductsBatchContext = context.BundleItemBatchContext;
            }

            if (product.ProductType == ProductType.GroupedProduct)
            {
                priceModel.DisableBuyButton = true;
                priceModel.DisableWishlistButton = true;
                priceModel.AvailableForPreOrder = false;

                if (context.GroupedProducts == null)
                {
                    // One-time batched retrieval of all associated products.
                    var searchQuery = new CatalogSearchQuery()
                        .PublishedOnly(true)
                        .HasStoreId(options.Store.Id)
                        .HasParentGroupedProduct(batchContext.ProductIds.ToArray());

                    // Get all associated products for this batch grouped by ParentGroupedProductId.
                    var searchResult = await _catalogSearchService.SearchAsync(searchQuery);
                    var allAssociatedProducts = (await searchResult.GetHitsAsync())
                        .OrderBy(x => x.ParentGroupedProductId)
                        .ThenBy(x => x.DisplayOrder);

                    context.GroupedProducts = allAssociatedProducts.ToMultimap(x => x.ParentGroupedProductId, x => x);
                    context.AssociatedProductBatchContext = _productService.CreateProductBatchContext(allAssociatedProducts, options.Store, options.Customer, false);
                }

                options.ChildProductsBatchContext = context.AssociatedProductBatchContext;

                associatedProducts = context.GroupedProducts[product.Id];
                if (associatedProducts.Count > 0)
                {
                    contextProduct = associatedProducts.OrderBy(x => x.DisplayOrder).First();
                    _services.DisplayControl.Announce(contextProduct);
                }
            }
            else
            {
                priceModel.DisableBuyButton = product.DisableBuyButton || !context.AllowShoppingCart || !context.AllowPrices;
                priceModel.DisableWishlistButton = product.DisableWishlistButton || !context.AllowWishlist || !context.AllowPrices;
                priceModel.AvailableForPreOrder = product.AvailableForPreOrder;
            }

            // Return if there's no pricing at all.
            if (contextProduct == null || !context.AllowPrices || _priceSettings.PriceDisplayType == PriceDisplayType.Hide)
            {
                return contextProduct;
            }

            // Return if group has no associated products.
            if (product.ProductType == ProductType.GroupedProduct && associatedProducts.Count == 0)
            {
                return contextProduct;
            }

            var calculationContext = new PriceCalculationContext(product, options)
            {
                AssociatedProducts = associatedProducts
            };

            // -----> Perform calculation <-------
            var calculatedPrice = await _priceCalculationService.CalculatePriceAsync(calculationContext);

            // Map base
            MapPriceBase(calculatedPrice, priceModel, model.Parent.ShowBasePrice);

            if (priceModel.CallForPrice || priceModel.CustomerEntersPrice)
            {
                return contextProduct;
            }

            priceModel.ShowPriceLabel = _priceSettings.ShowPriceLabelInLists;

            // Badges
            priceModel.ShowSavingBadge = _priceSettings.ShowSavingBadgeInLists && priceModel.Saving.HasSaving;
            if (priceModel.ShowSavingBadge)
            {
                model.Badges.Add(new ProductBadgeModel { Label = T("Products.SavingBadgeLabel", priceModel.Saving.SavingPercent.ToString("N0")), Style = "danger" });
            }

            if (_priceSettings.ShowOfferBadge && _priceSettings.ShowOfferBadgeInLists)
            {
                AddPromoBadge(calculatedPrice, model.Badges);
            }

            return contextProduct;
        }

        #endregion

        #region Utils

        protected void MapPriceBase(CalculatedPrice price, PriceModel model, bool mapBasePrice = true)
        {
            model.FinalPrice = price.FinalPrice;
            model.CallForPrice = price.PricingType == PricingType.CallForPrice;
            model.CustomerEntersPrice = price.PricingType == PricingType.CustomerEnteredPrice;
            model.HasCalculation = !model.CustomerEntersPrice || model.CallForPrice;
            model.Saving = price.Saving;
            model.ValidUntilUtc = price.ValidUntilUtc;
            model.ShowRetailPriceSaving = _priceSettings.ShowRetailPriceSaving;

            if (model.CallForPrice || model.CustomerEntersPrice)
            {
                return;
            }

            var product = price.Product;
            var forSummary = model is ProductSummaryPriceModel;
            // Never show retail price in grid style listings if we have a regular price already
            var canMapRetailPrice = !price.RegularPrice.HasValue || _priceSettings.AlwaysDisplayRetailPrice;

            // Display "Free" instead of 0.00
            if (_priceSettings.DisplayTextForZeroPrices && price.FinalPrice == 0 && !model.CallForPrice)
            {
                model.FinalPrice = model.FinalPrice.WithPostFormat(T("Products.Free"));
            }

            // Regular price
            if (model.RegularPrice == null && price.Saving.HasSaving && price.RegularPrice.HasValue)
            {
                model.RegularPrice = GetComparePriceModel(price.RegularPrice.Value, price.RegularPriceLabel, forSummary);
                if (product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing)
                {
                    // Change regular price label: "Regular/Lowest" --> "Instead of"
                    model.RegularPrice.Label = T("Products.Bundle.PriceWithoutDiscount.Note");
                }
            }

            // Retail price
            if (model.RetailPrice == null && price.RetailPrice.HasValue && canMapRetailPrice)
            {
                model.RetailPrice = GetComparePriceModel(price.RetailPrice.Value, price.RetailPriceLabel, forSummary);

                // Don't show saving if there is no actual discount and ShowRetailPriceSaving is FALSE
                if (model.RegularPrice == null && !model.ShowRetailPriceSaving)
                {
                    model.Saving = new PriceSaving { SavingPrice = new Money(0, price.FinalPrice.Currency) };
                }
            }

            // BasePrice (PanGV)
            model.IsBasePriceEnabled = mapBasePrice &&
                price.FinalPrice != decimal.Zero &&
                product.BasePriceEnabled &&
                !(product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing);

            if (model.IsBasePriceEnabled)
            {
                model.BasePriceInfo = _priceCalculationService.GetBasePriceInfo(
                    product: product,
                    price: price.FinalPrice,
                    targetCurrency: price.FinalPrice.Currency,
                    // Don't display tax suffix in detail
                    displayTaxSuffix: forSummary ? null : false);
            }

            // Shipping surcharge
            if (product.AdditionalShippingCharge > 0)
            {
                var charge = _currencyService.ConvertFromPrimaryCurrency(product.AdditionalShippingCharge, model.FinalPrice.Currency);
                model.ShippingSurcharge = charge.WithPostFormat(T("Common.AdditionalShippingSurcharge"));
            }
        }

        public void AddPromoBadge(CalculatedPrice price, List<ProductBadgeModel> badges)
        {
            // Add default promo badges as configured
            var (label, style) = _priceLabelService.GetPricePromoBadge(price);

            if (label.HasValue())
            {
                badges.Add(new ProductBadgeModel
                {
                    Label = label,
                    Style = (style.IsNumeric() ? Enum.Parse<BadgeStyle>(style).ToString().ToLower() : style) ?? "dark",
                    DisplayOrder = 10
                });
            }
        }

        private static ComparePriceModel GetComparePriceModel(Money comparePrice, PriceLabel priceLabel, bool forSummary)
        {
            var model = new ComparePriceModel { Price = comparePrice };

            if (forSummary)
            {
                model.Label = priceLabel.GetLocalized(x => x.ShortName);
            }
            else
            {
                // In product detail we should fallback to ShortName if Name is empty.
                model.Label = priceLabel.GetLocalized(x => x.Name).Value.NullEmpty() ?? priceLabel.GetLocalized(x => x.ShortName);
                model.Description = priceLabel.GetLocalized(x => x.Description);
            }

            return model;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Money ToWorkingCurrency(decimal amount, ProductSummaryItemContext ctx, bool showCurrency = true)
        {
            return _currencyService.ConvertToCurrency(new Money(amount, ctx.PrimaryCurrency, !showCurrency), ctx.CalculationOptions.TargetCurrency);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected Money ToWorkingCurrency(Money amount, ProductSummaryItemContext ctx)
        {
            return _currencyService.ConvertToCurrency(amount, ctx.CalculationOptions.TargetCurrency);
        }

        #endregion
    }
}
