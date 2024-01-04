using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Moq;
using NUnit.Framework;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Catalog.Search;
using Smartstore.Core.Common;
using Smartstore.Core.Localization;
using Smartstore.Core.Search;
using Smartstore.Core.Security;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Tests.Catalog.Search
{
    [TestFixture]
    public class LinqCatalogSearchServiceTests : ServiceTestBase
    {
        private LinqCatalogSearchService _linqCatalogSearchService;
        private MockCommonServices _services;

        private async Task InitTestDataAsync(
            IEnumerable<Product> products, 
            IEnumerable<Category> categories = null,
            IEnumerable<LocalizedProperty> translations = null)
        {
            DbContext.StoreMappings.RemoveRange(DbContext.StoreMappings);
            DbContext.AclRecords.RemoveRange(DbContext.AclRecords);
            DbContext.Categories.RemoveRange(DbContext.Categories);
            DbContext.Products.RemoveRange(DbContext.Products);
            DbContext.Languages.RemoveRange(DbContext.Languages);
            DbContext.LocalizedProperties.RemoveRange(DbContext.LocalizedProperties);
            await DbContext.SaveChangesAsync();

            DbContext.StoreMappings.Add(new StoreMapping { Id = 1, StoreId = 3, EntityName = "Product", EntityId = 99 });
            DbContext.AclRecords.Add(new AclRecord { Id = 1, CustomerRoleId = 3, EntityName = "Product", EntityId = 99 });
            DbContext.Languages.Add(new Language { Id = 1, Name = "Deutsch", LanguageCulture = "de-DE", UniqueSeoCode = "de", Published = true });

            if (categories != null)
            {
                DbContext.Categories.AddRange(categories);
            }
            if (translations != null)
            {
                DbContext.LocalizedProperties.AddRange(translations);
            }

            DbContext.Products.AddRange(products);
            await DbContext.SaveChangesAsync();
        }

        private async Task<CatalogSearchResult> SearchAsync(
            CatalogSearchQuery query,
            IEnumerable<Product> products,
            IEnumerable<Category> categories = null)
        {
            Trace.WriteLine(query.ToString());

            await InitTestDataAsync(products, categories);

            return await _linqCatalogSearchService.SearchAsync(query);
        }

        [SetUp]
        public new void SetUp()
        {
            var builder = new ContainerBuilder();

            _services = new MockCommonServices(DbContext, builder.Build());

            _linqCatalogSearchService = new LinqCatalogSearchService(
                DbContext,
                new[] { new CatalogSearchQueryVisitor() },
                _services, 
                It.IsAny<ICategoryService>());
        }

        [Test]
        public async Task LinqSearch_can_order_by_name()
        {
            var products = new List<Product>();

            for (var i = 97; i <= 110; ++i)
            {
                products.Add(new SearchProduct(i) { Name = Convert.ToChar(i).ToString(), ShortDescription = "smart" });
            }

            var query = new CatalogSearchQuery(new string[] { "shortdescription" }, "smart");
            query.SortBy(ProductSortingEnum.NameDesc);

            var result = await SearchAsync(query, products);

            Assert.That(string.Join(",", (await result.GetHitsAsync()).Select(x => x.Name)), Is.EqualTo("n,m,l,k,j,i,h,g,f,e,d,c,b,a"));
        }

        [Test]
        public async Task LinqSearch_can_page_result()
        {
            var products = new List<Product>();

            for (var i = 1; i <= 20; ++i)
            {
                products.Add(new SearchProduct(i) { Name = "smart", Sku = i.ToString() });
            }

            var result = await SearchAsync(new CatalogSearchQuery(new string[] { "name" }, "smart").Slice(10, 5), products);
            var hits = await result.GetHitsAsync();
            Assert.That(hits.Count, Is.EqualTo(5));
            Assert.That(hits.Select(x => x.Sku), Is.EqualTo(new string[] { "11", "12", "13", "14", "15" }));
        }

        #region Term search

        [Test]
        public async Task LinqSearch_not_find_anything()
        {
            var products = new List<Product>
            {
                new SearchProduct { Name = "Smartstore" },
                new SearchProduct { Name = "Apple iPhone Smartphone 6" },
                new SearchProduct { Name = "Energistically recaptiualize superior e-markets without next-generation platforms" },
                new SearchProduct { Name = "Rapidiously conceptualize future-proof imperatives", ShortDescription = "Shopping System powered by Smartstore" }
            };

            var result = await SearchAsync(new CatalogSearchQuery(new string[] { "name", "shortdescription" }, "cook"), products);

            Assert.That(result.TotalHitsCount, Is.EqualTo(0));
            Assert.That(result.SpellCheckerSuggestions.Any(), Is.EqualTo(false));
        }

        [TestCase(3, "Smart")]
        [TestCase(4, "Smart", SearchMode.Contains, 1)]
        public async Task LinqSearch_find_term(int hits, string term, SearchMode mode = SearchMode.Contains, int languageId = 0)
        {
            var products = new List<Product>
            {
                new SearchProduct(1) { Name = "Smartstore" },
                new SearchProduct(2) { Name = "Apple iPhone Smartphone 6" },
                new SearchProduct(3) { Name = "Energistically recaptiualize superior e-markets without next-generation platforms" },
                new SearchProduct(4) { Name = "Rapidiously conceptualize future-proof imperatives", ShortDescription = "Shopping System powered by Smartstore" },
                new SearchProduct(5) { Name = "Enthusiastically pursue leading-edge e-tailers with worldwide schemas", ShortDescription = "Authoritatively evisculate open-source after interdependent data." },
            };

            List<LocalizedProperty> translations = null;
            if (languageId > 0)
            {
                translations = new List<LocalizedProperty>
                {
                    new() { LocaleKeyGroup = "Product", LocaleKey = "Name", EntityId = 5, LocaleValue = "Holisticly leadership extensible for Smartstore pontificate.", LanguageId = languageId }
                };
            }

            await InitTestDataAsync(products, null, translations);

            var query = new CatalogSearchQuery(new[] { "name", "shortdescription" }, term, mode);
            if (languageId > 0)
            {
                query = query.WithLanguage(await DbContext.Languages.FindByIdAsync(languageId));
            }

            Trace.WriteLine(query.ToString());

            var result = await _linqCatalogSearchService.SearchAsync(query);
            Assert.That(result.TotalHitsCount, Is.EqualTo(hits));
        }

        [Test]
        public async Task LinqSearch_find_exact_match()
        {
            var products = new List<Product>
            {
                new SearchProduct(1) { Name = "P-6000-2" },
                new SearchProduct(2) { Name = "Apple iPhone Smartphone 6" },
                new SearchProduct(3) { Name = "Energistically recaptiualize superior e-markets without next-generation platforms" },
                new SearchProduct(4) { Name = "Rapidiously conceptualize future-proof imperatives", ShortDescription = "Shopping System powered by SmartStore", Sku = "P-6000-2" }
            };

            var result = await SearchAsync(new CatalogSearchQuery(new string[] { "name", "sku" }, "P-6000-2", SearchMode.ExactMatch), products);

            Assert.That(result.TotalHitsCount, Is.EqualTo(2));
        }

        #endregion

        #region Filter

        [Test]
        public async Task LinqSearch_filter_visible_only()
        {
            var products = new List<Product>
            {
                new SearchProduct(1) { Published = true },
                new SearchProduct(2) { Published = false },
                new SearchProduct(3) { Published = true, AvailableStartDateTimeUtc = new DateTime(2016, 1, 1), AvailableEndDateTimeUtc = new DateTime(2016, 1, 20) },
                new SearchProduct(4) { Published = true, Id = 99, SubjectToAcl = true }
            };

            var result = await SearchAsync(new CatalogSearchQuery().VisibleOnly(new int[0]), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(2));

            result = await SearchAsync(new CatalogSearchQuery().VisibleOnly(new int[] { 1, 5, 6 }), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(1));
        }

        [Test]
        public async Task LinqSearch_filter_published_only()
        {
            var products = new List<Product>
            {
                new SearchProduct(1) { Published = true },
                new SearchProduct(2) { Published = false },
                new SearchProduct(3) { Published = true }
            };

            var result = await SearchAsync(new CatalogSearchQuery().PublishedOnly(true), products);

            Assert.That(result.TotalHitsCount, Is.EqualTo(2));
        }

        [Test]
        public async Task LinqSearch_filter_visibility()
        {
            var products = new List<Product>
            {
                new SearchProduct(1),
                new SearchProduct(2) { Visibility = ProductVisibility.Hidden },
                new SearchProduct(3),
                new SearchProduct(4) { Visibility = ProductVisibility.SearchResults }
            };

            var result = await SearchAsync(new CatalogSearchQuery().WithVisibility(ProductVisibility.Full), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(2));

            result = await SearchAsync(new CatalogSearchQuery().WithVisibility(ProductVisibility.SearchResults), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(3));

            result = await SearchAsync(new CatalogSearchQuery().WithVisibility(ProductVisibility.Hidden), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(1));
        }

        [Test]
        public async Task LinqSearch_filter_homepage_products_only()
        {
            var products = new List<Product>
            {
                new SearchProduct(1),
                new SearchProduct(2) { ShowOnHomePage = true },
                new SearchProduct(3)
            };

            var result = await SearchAsync(new CatalogSearchQuery().HomePageProductsOnly(true), products);

            Assert.That(result.TotalHitsCount, Is.EqualTo(1));
        }

        [Test]
        public async Task LinqSearch_filter_has_parent_grouped_product_id()
        {
            var products = new List<Product>
            {
                new SearchProduct(1),
                new SearchProduct(2) { ParentGroupedProductId = 16, Visibility = ProductVisibility.Hidden },
                new SearchProduct(3) { ParentGroupedProductId = 36, Visibility = ProductVisibility.Hidden },
                new SearchProduct(4) { ParentGroupedProductId = 9 },
                new SearchProduct(5) { ParentGroupedProductId = 36 }
            };

            var result = await SearchAsync(new CatalogSearchQuery().HasParentGroupedProduct(36), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(2));
        }

        [Test]
        public async Task LinqSearch_filter_has_store_id()
        {
            var products = new List<Product>
            {
                new SearchProduct { },
                new SearchProduct { LimitedToStores = true, Id = 99 }
            };

            var result = await SearchAsync(new CatalogSearchQuery().HasStoreId(1), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(1));

            result = await SearchAsync(new CatalogSearchQuery().HasStoreId(3), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(2));
        }

        [Test]
        public async Task LinqSearch_filter_is_product_type()
        {
            var products = new List<Product>
            {
                new SearchProduct(1),
                new SearchProduct(2) { ProductType = ProductType.BundledProduct },
                new SearchProduct(3) { ProductType = ProductType.GroupedProduct }
            };

            var result = await SearchAsync(new CatalogSearchQuery().IsProductType(ProductType.SimpleProduct), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(1));

            result = await SearchAsync(new CatalogSearchQuery().IsProductType(ProductType.GroupedProduct), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(1));
        }

        [Test]
        public async Task LinqSearch_filter_with_product_ids()
        {
            var products = new List<Product>
            {
                new SearchProduct(1),
                new SearchProduct(2),
                new SearchProduct(3),
                new SearchProduct(4),
                new SearchProduct(5)
            };

            var result = await SearchAsync(new CatalogSearchQuery().WithProductIds(2, 3, 4, 99), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(3));

            result = await SearchAsync(new CatalogSearchQuery().WithProductIds(98), products);
            Assert.That((await result.GetHitsAsync()).FirstOrDefault(), Is.Null);
        }

        [Test]
        public async Task LinqSearch_filter_with_product_id()
        {
            var products = new List<Product>
            {
                new SearchProduct(1),
                new SearchProduct(2),
                new SearchProduct(3),
                new SearchProduct(4),
                new SearchProduct(5),
                new SearchProduct(6),
                new SearchProduct(7),
                new SearchProduct(8),
                new SearchProduct(9),
                new SearchProduct(10)
            };

            var result = await SearchAsync(new CatalogSearchQuery().WithProductId(4, 7), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(4));

            result = await SearchAsync(new CatalogSearchQuery().WithProductId(6, null), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(5));

            result = await SearchAsync(new CatalogSearchQuery().WithProductId(null, 3), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(3));
        }

        [Test]
        public async Task LinqSearch_filter_with_category_ids()
        {
            var products = new List<Product>
            {
                new SearchProduct(new ProductCategory[] { new() { CategoryId = 11 } }) { Id = 1 },
                new SearchProduct(new ProductCategory[] { new() { CategoryId = 12, IsFeaturedProduct = true } }) { Id = 2 },
                new SearchProduct(new ProductCategory[] { new() { CategoryId = 13 } }) { Id = 3 },
                new SearchProduct(new ProductCategory[] { new() { CategoryId = 14 } }) { Id = 4 },
                new SearchProduct(new ProductCategory[] { new() { CategoryId = 15 } }) { Id = 5 },
                new SearchProduct(new ProductCategory[] { new() { CategoryId = 16, IsFeaturedProduct = true } }) { Id = 6 },
                new SearchProduct(new ProductCategory[] { new() { CategoryId = 17 } }) { Id = 7 },
                new SearchProduct(new ProductCategory[] { new() { CategoryId = 18 } }) { Id = 8 }
            };

            var result = await SearchAsync(new CatalogSearchQuery().WithCategoryIds(null, 68, 98), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(0));

            result = await SearchAsync(new CatalogSearchQuery().WithCategoryIds(null, 12, 15, 18, 24), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(3));

            result = await SearchAsync(new CatalogSearchQuery().WithCategoryIds(true, 12, 15, 18, 24), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(1));

            result = await SearchAsync(new CatalogSearchQuery().WithCategoryIds(false, 12, 15, 18, 24), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(2));
        }

        [TestCase(4, "/1/11/")]
        [TestCase(3, "/2/21/")]
        [TestCase(7, "/2/")]
        [TestCase(3, "/2/", true)]
        [TestCase(4, "/2/", false)]
        [TestCase(6, "/2/", null, false)]
        [TestCase(1, "/2/21/", true, false)]
        public async Task LinqSearch_filter_with_category_treepath(int hits, string treePath, bool? featuredOnly = null, bool includeSelf = true)
        {
            var idCount = 9999;
            var categories = new List<Category>();
            var products = new List<Product>();

            var treePaths = new string[]
            {
                "/1/",
                "/1/11/", "/1/11/111/", "/1/11/112/", "/1/11/113/",
                "/1/12/", "/1/12/121/",

                "/2/",
                "/2/21/", "/2/21/211/", "/2/21/212/",
                "/2/22/",
                "/2/23/",
                "/2/24/",

                "/3/",
            };

            foreach (var path in treePaths)
            {
                var categoryId = path.TrimEnd('/').SplitSafe('/').LastOrDefault().ToInt();
                var pc = new ProductCategory[]
                {
                    new() { CategoryId = categoryId, IsFeaturedProduct = path == "/2/21/211/" || path == "/2/23/" || path == "/2/21/" }
                };

                categories.Add(new() { Id = categoryId, TreePath = path, Name = $"Category {categoryId}" });
                products.Add(new SearchProduct(pc) { Id = ++idCount });
            }

            var result = await SearchAsync(new CatalogSearchQuery().WithCategoryTreePath(treePath, featuredOnly, includeSelf), products, categories);
            Assert.That(result.TotalHitsCount, Is.EqualTo(hits));
        }

        [Test]
        public async Task LinqSearch_filter_has_any_category()
        {
            var products = new List<Product>
            {
                new SearchProduct(new ProductCategory[] { new ProductCategory { CategoryId = 11 } }) { Id = 1 },
                new SearchProduct(new ProductCategory[] { new ProductCategory { CategoryId = 12, IsFeaturedProduct = true } }) { Id = 2 },
                new SearchProduct(new ProductCategory[] { }) { Id = 3 },
                new SearchProduct(new ProductCategory[] { new ProductCategory { CategoryId = 14 } }) { Id = 4 },
                new SearchProduct(new ProductCategory[] { new ProductCategory { CategoryId = 15 } }) { Id = 5 },
                new SearchProduct(new ProductCategory[] { }) { Id = 6 },
                new SearchProduct(new ProductCategory[] { }) { Id = 7 },
                new SearchProduct(new ProductCategory[] { new ProductCategory { CategoryId = 18 } }) { Id = 8 }
            };

            var result = await SearchAsync(new CatalogSearchQuery().HasAnyCategory(true), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(5));

            result = await SearchAsync(new CatalogSearchQuery().HasAnyCategory(false), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(3));
        }

        [Test]
        public async Task LinqSearch_filter_with_manufacturer_ids()
        {
            var products = new List<Product>
            {
                new SearchProduct(new ProductManufacturer[] { new ProductManufacturer { ManufacturerId = 11 } }) { Id = 1 },
                new SearchProduct(new ProductManufacturer[] { new ProductManufacturer { ManufacturerId = 12, IsFeaturedProduct = true } }) { Id = 2 },
                new SearchProduct(new ProductManufacturer[] { new ProductManufacturer { ManufacturerId = 13 } }) { Id = 3 },
                new SearchProduct(new ProductManufacturer[] { new ProductManufacturer { ManufacturerId = 14 } }) { Id = 4 },
                new SearchProduct(new ProductManufacturer[] { new ProductManufacturer { ManufacturerId = 15 } }) { Id = 5 },
                new SearchProduct(new ProductManufacturer[] { new ProductManufacturer { ManufacturerId = 16, IsFeaturedProduct = true } }) { Id = 6 },
                new SearchProduct(new ProductManufacturer[] { new ProductManufacturer { ManufacturerId = 17 } }) { Id = 7 },
                new SearchProduct(new ProductManufacturer[] { new ProductManufacturer { ManufacturerId = 18 } }) { Id = 8 }
            };

            var result = await SearchAsync(new CatalogSearchQuery().WithManufacturerIds(null, 68, 98), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(0));

            result = await SearchAsync(new CatalogSearchQuery().WithManufacturerIds(null, 12, 15, 18, 24), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(3));

            result = await SearchAsync(new CatalogSearchQuery().WithManufacturerIds(true, 12, 15, 18, 24), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(1));

            result = await SearchAsync(new CatalogSearchQuery().WithManufacturerIds(false, 12, 15, 18, 24), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(2));
        }

        [Test]
        public async Task LinqSearch_filter_has_any_manufacturer()
        {
            var products = new List<Product>
            {
                new SearchProduct(new ProductManufacturer[] { new ProductManufacturer { ManufacturerId = 11 } }) { Id = 1 },
                new SearchProduct(new ProductManufacturer[] { new ProductManufacturer { ManufacturerId = 12, IsFeaturedProduct = true } }) { Id = 2 },
                new SearchProduct(new ProductManufacturer[] { }) { Id = 3 },
                new SearchProduct(new ProductManufacturer[] { new ProductManufacturer { ManufacturerId = 14 } }) { Id = 4 },
                new SearchProduct(new ProductManufacturer[] { new ProductManufacturer { ManufacturerId = 15 } }) { Id = 5 },
                new SearchProduct(new ProductManufacturer[] { }) { Id = 6 },
                new SearchProduct(new ProductManufacturer[] { }) { Id = 7 },
                new SearchProduct(new ProductManufacturer[] { new ProductManufacturer { ManufacturerId = 18 } }) { Id = 8 }
            };

            var result = await SearchAsync(new CatalogSearchQuery().HasAnyManufacturer(true), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(5));

            result = await SearchAsync(new CatalogSearchQuery().HasAnyManufacturer(false), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(3));
        }

        [Test]
        public async Task LinqSearch_filter_with_product_tag_ids()
        {
            var products = new List<Product>
            {
                new SearchProduct(new ProductTag[] { new ProductTag { Id = 16, Name = "Tag 1" } }) { Id = 1 },
                new SearchProduct(new ProductTag[] { }) { Id = 2 },
                new SearchProduct(new ProductTag[] { new ProductTag { Id = 32, Name = "Tag 2" } }) { Id = 3 },
                new SearchProduct(new ProductTag[] { new ProductTag { Id = 17, Name = "Tag 3" } }) { Id = 4 },
                new SearchProduct(new ProductTag[] { }) { Id = 5 }
            };

            var result = await SearchAsync(new CatalogSearchQuery().WithProductTagIds(16, 17, 32), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(3));

            result = await SearchAsync(new CatalogSearchQuery().WithProductTagIds(22), products);
            Assert.That((await result.GetHitsAsync()).FirstOrDefault(), Is.Null);
        }

        [Test]
        public async Task LinqSearch_filter_with_stock_quantity()
        {
            var products = new List<Product>
            {
                new SearchProduct(1) { StockQuantity = 10000 },
                new SearchProduct(2) { StockQuantity = 10001 },
                new SearchProduct(3) { StockQuantity = 10002 },
                new SearchProduct(4) { StockQuantity = 10003 },
                new SearchProduct(5) { StockQuantity = 10004 },
                new SearchProduct(6) { StockQuantity = 0 },
                new SearchProduct(7) { StockQuantity = 650 },
                new SearchProduct(8) { StockQuantity = 0 }
            };

            var result = await SearchAsync(new CatalogSearchQuery().WithStockQuantity(10001, 10003), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(3));

            result = await SearchAsync(new CatalogSearchQuery().WithStockQuantity(10003, null), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(2));

            result = await SearchAsync(new CatalogSearchQuery().WithStockQuantity(10003, null, false), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(1));

            result = await SearchAsync(new CatalogSearchQuery().WithStockQuantity(null, 10002), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(6));

            result = await SearchAsync(new CatalogSearchQuery().WithStockQuantity(null, 10002, null, false), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(5));

            result = await SearchAsync(new CatalogSearchQuery().WithStockQuantity(10000, 10000), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(1));

            result = await SearchAsync(new CatalogSearchQuery().WithStockQuantity(20000, 20000), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(0));

            result = await SearchAsync(new CatalogSearchQuery().WithStockQuantity(0, 0, false, false), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(6));
        }

        [Test]
        public async Task LinqSearch_filter_with_price()
        {
            var products = new List<Product>
            {
                new SearchProduct(1) { Price = 102.0M },
                new SearchProduct(2) { Price = 22.5M },
                new SearchProduct(3) { Price = 658.99M },
                new SearchProduct(4) { Price = 25.3M },
                new SearchProduct(5) { Price = 14.9M }
            };

            var eur = new Currency { CurrencyCode = "EUR" };

            var money100 = 100M;
            var money200 = 200M;
            var money14_90 = 14.90M;
            var money59_90 = 59.90M;

            var result = await SearchAsync(new CatalogSearchQuery().WithCurrency(eur).PriceBetween(money100, money200), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(1));

            result = await SearchAsync(new CatalogSearchQuery().WithCurrency(eur).PriceBetween(money100, null), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(2));

            result = await SearchAsync(new CatalogSearchQuery().WithCurrency(eur).PriceBetween(null, money100), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(3));

            result = await SearchAsync(new CatalogSearchQuery().WithCurrency(eur).PriceBetween(money14_90, money14_90), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(1));

            result = await SearchAsync(new CatalogSearchQuery().WithCurrency(eur).PriceBetween(money59_90, money59_90), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(0));

            result = await SearchAsync(new CatalogSearchQuery().WithCurrency(eur).PriceBetween(money14_90, money14_90, false, false), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(4));
        }

        [Test]
        public async Task LinqSearch_filter_with_created_utc()
        {
            var products = new List<Product>
            {
                new SearchProduct(1) { CreatedOnUtc = new DateTime(2016, 2, 16) },
                new SearchProduct(2) { CreatedOnUtc = new DateTime(2016, 2, 23) },
                new SearchProduct(3) { CreatedOnUtc = new DateTime(2016, 3, 20) },
                new SearchProduct(4) { CreatedOnUtc = new DateTime(2016, 4, 5) },
                new SearchProduct(5) { CreatedOnUtc = new DateTime(2016, 6, 25) },
                new SearchProduct(6) { CreatedOnUtc = new DateTime(2016, 8, 4) }
            };

            var result = await SearchAsync(new CatalogSearchQuery().CreatedBetween(new DateTime(2016, 1, 1), new DateTime(2016, 3, 1)), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(2));

            result = await SearchAsync(new CatalogSearchQuery().CreatedBetween(new DateTime(2016, 4, 1), null), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(3));

            result = await SearchAsync(new CatalogSearchQuery().CreatedBetween(null, new DateTime(2016, 7, 1)), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(5));

            result = await SearchAsync(new CatalogSearchQuery().CreatedBetween(new DateTime(2016, 8, 4), new DateTime(2016, 8, 4)), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(1));

            result = await SearchAsync(new CatalogSearchQuery().CreatedBetween(new DateTime(2012, 8, 4), new DateTime(2012, 8, 4)), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(0));

            result = await SearchAsync(new CatalogSearchQuery().CreatedBetween(new DateTime(2016, 8, 4), new DateTime(2016, 8, 4), false, false), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(5));
        }

        [Test]
        public async Task LinqSearch_filter_available_only()
        {
            var products = new List<Product>
            {
                new SearchProduct(1),
                new SearchProduct(2)
                {
                    StockQuantity = 0,
                    ManageInventoryMethod = ManageInventoryMethod.ManageStock,
                    BackorderMode = BackorderMode.NoBackorders
                },
                new SearchProduct(3)
                {
                    StockQuantity = 0,
                    ManageInventoryMethod = ManageInventoryMethod.ManageStock,
                    BackorderMode = BackorderMode.AllowQtyBelow0OnBackorder
                }
            };

            var result = await SearchAsync(new CatalogSearchQuery().AvailableOnly(true), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(2));
        }

        [Test]
        public async Task LinqSearch_filter_with_rating()
        {
            var products = new List<Product>
            {
                new SearchProduct(1),
                new SearchProduct(2) { ApprovedRatingSum = 14, ApprovedTotalReviews = 3 },  // 4.66
                new SearchProduct(3) { ApprovedRatingSum = 9, ApprovedTotalReviews = 3 },   // 3.00
                new SearchProduct(4) { ApprovedRatingSum = 17, ApprovedTotalReviews = 4 },  // 4.25
                new SearchProduct(5) { ApprovedRatingSum = 20, ApprovedTotalReviews = 10 }  // 2.00
            };

            var result = await SearchAsync(new CatalogSearchQuery().WithRating(3.0, null), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(3));

            result = await SearchAsync(new CatalogSearchQuery().WithRating(4.0, 5.0), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(2));

            result = await SearchAsync(new CatalogSearchQuery().WithRating(3.0, 3.0), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(1));

            result = await SearchAsync(new CatalogSearchQuery().WithRating(4.0, 4.0), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(0));

            result = await SearchAsync(new CatalogSearchQuery().WithRating(2.0, 2.0, false, false), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(3));
        }

        [Test]
        public async Task LinqSearch_filter_with_deliverytime_ids()
        {
            var products = new List<Product>
            {
                new SearchProduct(1),
                new SearchProduct(2) { DeliveryTimeId = 16 },
                new SearchProduct(3) { DeliveryTimeId = 16 },
                new SearchProduct(4) { DeliveryTimeId = 9 }
            };

            var result = await SearchAsync(new CatalogSearchQuery().WithDeliveryTimeIds(new int[] { 16, 9 }), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(3));

            result = await SearchAsync(new CatalogSearchQuery().WithDeliveryTimeIds(new int[] { 9 }), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(1));
        }

        [Test]
        public async Task LinqSearch_filter_with_condition()
        {
            var products = new List<Product>
            {
                new SearchProduct(1) { Condition = ProductCondition.New },
                new SearchProduct(2) { Condition = ProductCondition.Used },
                new SearchProduct(3) { Condition = ProductCondition.New },
                new SearchProduct(4) { Condition = ProductCondition.Damaged },
                new SearchProduct(5) { Condition = ProductCondition.New },
                new SearchProduct(6) { Condition = ProductCondition.Refurbished }
            };

            var result = await SearchAsync(new CatalogSearchQuery().WithCondition(ProductCondition.New), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(3));

            result = await SearchAsync(new CatalogSearchQuery().WithCondition(ProductCondition.Used, ProductCondition.Damaged), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(2));

            result = await SearchAsync(new CatalogSearchQuery().WithCondition(ProductCondition.Refurbished), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(1));

            result = await SearchAsync(new CatalogSearchQuery().WithCondition(ProductCondition.New, ProductCondition.Used), products);
            Assert.That(result.TotalHitsCount, Is.EqualTo(4));
        }

        #endregion

        #region SearchProduct

        internal class SearchProduct : Product
        {
            internal SearchProduct()
                : this(0, null, null, null)
            {
            }

            internal SearchProduct(int id)
                : this(id, null, null, null)
            {
            }

            internal SearchProduct(ICollection<ProductCategory> categories)
                : this(0, categories, null, null)
            {
            }

            internal SearchProduct(ICollection<ProductManufacturer> manufacturers)
                : this(0, null, manufacturers, null)
            {
            }

            internal SearchProduct(ICollection<ProductTag> tags)
                : this(0, null, null, tags)
            {
            }

            internal SearchProduct(
                int id,
                ICollection<ProductCategory> categories,
                ICollection<ProductManufacturer> manufacturers,
                ICollection<ProductTag> tags)
            {
                Id = id == 0 ? new Random().Next(100, int.MaxValue) : id;
                ProductCategories.AddRange(categories ?? new HashSet<ProductCategory>());
                ProductManufacturers.AddRange(manufacturers ?? new HashSet<ProductManufacturer>());
                ProductTags.AddRange(tags ?? new HashSet<ProductTag>());

                Name = "Holisticly implement optimal web services";
                ShortDescription = "Continually synthesize fully researched benefits with granular benefits.";
                FullDescription = "Enthusiastically utilize compelling systems with vertical collaboration and idea-sharing. Interactively incubate bleeding-edge innovation with future-proof catalysts for change. Distinctively exploit parallel paradigms rather than progressive scenarios. Compellingly synergize visionary ROI after process-centric resources. Objectively negotiate performance based best practices with 24/7 vortals. Globally pontificate reliable processes for innovative services. Monotonectally enable mission - critical information and quality.";
                Sku = "X-" + id.ToString();
                Published = true;
                Visibility = ProductVisibility.Full;
                ProductTypeId = (int)ProductType.SimpleProduct;
                StockQuantity = 10000;
                CreatedOnUtc = new DateTime(2016, 8, 24);
            }
        }

        #endregion
    }
}
