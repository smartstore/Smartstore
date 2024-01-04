using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Smartstore.Caching;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Common.Configuration;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Seo.Routing;
using Smartstore.Core.Stores;
using Smartstore.Test.Common;
using Smartstore.Threading;

namespace Smartstore.Core.Tests.Seo
{
    [TestFixture]
    public class UrlServiceTests : ServiceTestBase
    {
        private IUrlService _urlService;
        private Store _store;
        private Customer _customer;
        private Language _language;
        private SeoSettings _seoSettings;

        #region Setup & Teardown

        [SetUp]
        public new void SetUp()
        {
            _store = new Store { Id = 1 };
            _customer = new Customer { Id = 1 };
            _language = new Language { Id = 1 };
            _seoSettings = new SeoSettings();

            var storeContextMock = new Mock<IStoreContext>();
            storeContextMock.Setup(x => x.CurrentStore).Returns(_store);

            var workContextMock = new Mock<IWorkContext>();
            workContextMock.Setup(x => x.CurrentCustomer).Returns(_customer);
            workContextMock.Setup(x => x.WorkingLanguage).Returns(_language);

            var languageServiceMock = new Mock<ILanguageService>();
            var reservedSlugTableMock = new Mock<IRouteHelper>();

            _urlService = new UrlService(
                db: DbContext,
                cache: NullCache.Instance,
                httpContextAccessor: null,
                workContext: workContextMock.Object,
                storeContext: storeContextMock.Object,
                languageService: languageServiceMock.Object,
                routeHelper: reservedSlugTableMock.Object,
                localizationSettings: new LocalizationSettings(),
                seoSettings: _seoSettings,
                performanceSettings: new PerformanceSettings(),
                securitySettings: new SecuritySettings());

            PopulateEntities();
        }

        [TearDown]
        public void Reset()
        {
            RemoveEntities();
        }

        private void PopulateEntities()
        {
            int num = 50;
            var db = DbContext;

            for (var i = 1; i <= num; i++)
            {
                db.Products.Add(new Product { Name = $"Product {i} Test" });
                db.Categories.Add(new Category { Name = $"Category {i} Test" });
            }

            db.SaveChanges();
        }

        private void RemoveEntities()
        {
            var db = DbContext;
            db.Products.RemoveRange(db.Products.ToList());
            db.Categories.RemoveRange(db.Categories.ToList());
            db.UrlRecords.RemoveRange(db.UrlRecords.ToList());

            db.SaveChanges();
        }

        #endregion

        [Test]
        public async Task CanDetectCollisions()
        {
            var db = DbContext;

            await PopulateSlugs(db.Products.ToList());

            for (var i = 2; i <= 10; i++)
            {
                // Add another product with existing name
                var product = new Product { Name = "Product 1 Test" };
                db.Products.Add(product);
                await db.SaveChangesAsync();
                var validateSlugResult = await _urlService.ValidateSlugAsync(product, seName: null, ensureNotEmpty: true);
                await _urlService.ApplySlugAsync(validateSlugResult);
                await db.SaveChangesAsync();

                var activeSlug = await _urlService.GetActiveSlugAsync(product.Id, product.GetEntityName(), 0);
                activeSlug.ShouldEqual($"product-1-test-{i}");
            }
        }

        [Test]
        public async Task CanPopulateSlugs()
        {
            var db = DbContext;

            var products = db.Products.ToList();
            var categories = db.Categories.ToList();

            await PopulateSlugs(products);
            await PopulateSlugs(categories);

            var slugs = db.UrlRecords.ToList();
            slugs.Count.ShouldEqual(products.Count + categories.Count);

            var product11 = products.First(x => x.Name == "Product 11 Test");
            var activeSlug = await _urlService.GetActiveSlugAsync(product11.Id, product11.GetEntityName(), 0);
            activeSlug.ShouldEqual("product-11-test");

            var category22 = categories.First(x => x.Name == "Category 22 Test");
            activeSlug = await _urlService.GetActiveSlugAsync(category22.Id, category22.GetEntityName(), 0);
            activeSlug.ShouldEqual("category-22-test");

            var collection = await _urlService.GetUrlRecordCollectionAsync(
                NamedEntity.GetEntityName<Product>(),
                languageIds: null,
                entityIds: products.Select(x => x.Id).Take(20).ToArray(),
                isRange: true,
                isSorted: true);

            collection.Count.ShouldEqual(20);

            var product10 = products.First(x => x.Name == "Product 10 Test");
            var cachedSlug = collection.Find(0, product10.Id);
            Assert.That(cachedSlug, Is.Not.Null);
            cachedSlug.Slug.ShouldEqual("product-10-test");

            // Nor prefetch
            await _urlService.PrefetchUrlRecordsAsync(
                NamedEntity.GetEntityName<Category>(),
                languageIds: null,
                entityIds: null);

            activeSlug = await _urlService.GetActiveSlugAsync(category22.Id, category22.GetEntityName(), 0);
            activeSlug.ShouldEqual("category-22-test");
        }

        [Test]
        public async Task CanPopulateSlugsBatched()
        {
            var db = DbContext;

            var products = db.Products.ToList();
            var categories = db.Categories.ToList();

            await PopulateSlugsBatched(products.Cast<ISlugSupported>().Concat(categories));

            var slugs = db.UrlRecords.ToList();
            slugs.Count.ShouldEqual(products.Count + categories.Count);

            // Populate new entities with identical names
            PopulateEntities();
            var newProducts = db.Products.ToList().Except(products).ToList();
            var newCategories = db.Categories.ToList().Except(categories).ToList();

            // Populate with collision
            await PopulateSlugsBatched(newProducts.Cast<ISlugSupported>().Concat(newCategories));

            slugs = db.UrlRecords.ToList();
            slugs.Count.ShouldEqual((newProducts.Count + newCategories.Count) * 2);

            // Populate same
            await PopulateSlugsBatched(newProducts.Cast<ISlugSupported>().Concat(newCategories));
            // Should do nothing as nothing changed
            slugs = db.UrlRecords.ToList();
            slugs.Count.ShouldEqual((newProducts.Count + newCategories.Count) * 2);
        }

        //// Unit test fails too often (EF EnterCriticalSection multi-threading issues)
        /// <param name="entities"></param>
        /// <returns></returns>
        //[Test]
        //public async Task CanPopulateConcurrently()
        //{
        //    var db = DbContext;

        //    await PopulateSlugs(db.Products.ToList());

        //    var tasks = new List<Task>();
        //    var resultDictionary = new ConcurrentDictionary<Product, ValidateSlugResult>();

        //    for (var i = 0; i < 100; i++)
        //    {
        //        var product = new Product { Name = "Product 1 Test" };
        //        db.Products.Add(product);
        //        await db.SaveChangesAsync();

        //        tasks.Add(new Task(async state =>
        //        {
        //            var p = (Product)state;
        //            var result = await _urlService.SaveSlugAsync(p, seName: null, ensureNotEmpty: true, displayName: p.GetDisplayName());

        //            resultDictionary[p] = result;
        //        }, product));
        //    }

        //    // Start all tasks at once
        //    foreach (Task task in tasks)
        //    {
        //        task.Start();
        //    }

        //    await Task.WhenAll(tasks);

        //    foreach (var kv in resultDictionary)
        //    {
        //        var activeSlug = await _urlService.GetActiveSlugAsync(kv.Key.Id, kv.Key.GetEntityName(), 0);
        //        activeSlug.ShouldEqual(kv.Value.Slug);
        //    }
        //}

        private async Task PopulateSlugs(IEnumerable<ISlugSupported> entities)
        {
            foreach (var entity in entities)
            {
                var validateSlugResult = await _urlService.ValidateSlugAsync(entity, seName: null, ensureNotEmpty: true);
                await _urlService.ApplySlugAsync(validateSlugResult);
            }

            await DbContext.SaveChangesAsync();
        }

        private async Task PopulateSlugsBatched(IEnumerable<ISlugSupported> entities)
        {
            using var scope = _urlService.CreateBatchScope();

            foreach (var entity in entities)
            {
                scope.ApplySlugs(new ValidateSlugResult
                {
                    Source = entity,
                    Slug = SlugUtility.Slugify(entity.GetDisplayName(), _seoSettings)
                });
            }

            await scope.CommitAsync();
        }
    }
}
