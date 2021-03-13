using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Cart;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Identity;
using Smartstore.Core.Data;
using Smartstore.Core.Domain.Catalog;
using Smartstore.Core.Stores;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Catalog.Pricing
{
    public partial class PriceCalculationService2
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly ICommonServices _services;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;
        private readonly IProductAttributeMaterializer _productAttributeMaterializer;
        private readonly IDiscountService _discountService;
        private readonly CatalogSettings _catalogSettings;
        private readonly Currency _primaryCurrency;

        public PriceCalculationService2(
            SmartDbContext db,
            IWorkContext workContext,
            IStoreContext storeContext,
            ICommonServices services,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            ITaxService taxService,
            ICurrencyService currencyService,
            IProductAttributeMaterializer productAttributeMaterializer,
            IDiscountService discountService,
            CatalogSettings catalogSettings)
        {
            _db = db;
            _workContext = workContext;
            _storeContext = storeContext;
            _services = services;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _taxService = taxService;
            _currencyService = currencyService;
            _productAttributeMaterializer = productAttributeMaterializer;
            _discountService = discountService;
            _catalogSettings = catalogSettings;

            _primaryCurrency = currencyService.PrimaryCurrency;
        }

        public async Task CalculatePriceAsync(PriceCalculationContext context)
        {
            Guard.NotNull(context, nameof(context));

            context.Result ??= new PriceCalculationResult();

            var product = context.Product;
            var batchContext = context.BatchContext;
            var result = context.Result;

            if (product.ProductType == ProductType.BundledProduct && product.BundlePerItemPricing && !batchContext.ProductBundleItems.FullyLoaded)
            {
                await batchContext.ProductBundleItems.LoadAllAsync();
            }

            await Task.Delay(0);
        }
    }
}
