using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Pricing;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Checkout.Tax;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Domain.Catalog;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Core.DataExchange.Export
{
    public partial class DataExporter : IDataExporter
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly ICommonServices _services;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IManufacturerService _manufacturerService;
        private readonly ITaxService _taxService;
        private readonly ICurrencyService _currencyService;

        private readonly CatalogSettings _catalogSettings;

        public DataExporter(
            SmartDbContext db,
            IWorkContext workContext,
            IStoreContext storeContext,
            ICommonServices services,
            IProductService productService,
            ICategoryService categoryService,
            IManufacturerService manufacturerService,
            ITaxService taxService,
            ICurrencyService currencyService,
            CatalogSettings catalogSettings)
        {
            _db = db;
            _workContext = workContext;
            _storeContext = storeContext;
            _services = services;
            _productService = productService;
            _categoryService = categoryService;
            _manufacturerService = manufacturerService;
            _taxService = taxService;
            _currencyService = currencyService;
            _catalogSettings = catalogSettings;
        }
    }
}
