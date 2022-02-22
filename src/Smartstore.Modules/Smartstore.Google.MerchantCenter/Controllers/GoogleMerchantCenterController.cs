using System.Globalization;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Smartstore;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Core.Rules.Filters;
using Smartstore.Engine.Modularity;
using Smartstore.Google.MerchantCenter.Domain;
using Smartstore.Google.MerchantCenter.Models;
using Smartstore.Google.MerchantCenter.Providers;
using Smartstore.IO;
using Smartstore.Utilities;
using Smartstore.Web.Controllers;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Google.MerchantCenter.Controllers
{
    public class GoogleMerchantCenterController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IProviderManager _providerManager;

        public GoogleMerchantCenterController(SmartDbContext db, IProviderManager providerManager)
        {
            _db = db;
            _providerManager = providerManager;
        }

        public async Task<IActionResult> ProductEditTab(int productId)
        {
            var culture = CultureInfo.InvariantCulture;
            var model = new GoogleProductModel { ProductId = productId };
            var entity = await _db.GoogleProducts().FirstOrDefaultAsync(x => x.ProductId == productId);
            string notSpecified = T("Common.Unspecified");

            if (entity != null)
            {
                MiniMapper.Map(entity, model);
                model.ProductId = productId;
            }
            else
            {
                model.Export = true;
            }

            ViewBag.DefaultCategory = string.Empty;
            ViewBag.DefaultColor = string.Empty;
            ViewBag.DefaultSize = string.Empty;
            ViewBag.DefaultMaterial = string.Empty;
            ViewBag.DefaultPattern = string.Empty;
            ViewBag.DefaultGender = notSpecified;
            ViewBag.DefaultAgeGroup = notSpecified;
            ViewBag.DefaultIsAdult = string.Empty;
            ViewBag.DefaultMultipack = string.Empty;
            ViewBag.DefaultIsBundle = string.Empty;
            ViewBag.DefaultCustomLabel = string.Empty;
            ViewBag.LanguageSeoCode = Services.WorkContext.WorkingLanguage.UniqueSeoCode.EmptyNull().ToLower();

            // We do not have export profile context here, so we simply use the first profile.
            var profile = await _db.ExportProfiles.FirstOrDefaultAsync(x => x.ProviderSystemName == GmcXmlExportProvider.SystemName);

            if (profile != null)
            {
                if (XmlHelper.Deserialize(profile.ProviderConfigData, typeof(ProfileConfigurationModel)) is ProfileConfigurationModel config)
                {
                    ViewBag.DefaultCategory = config.DefaultGoogleCategory;
                    ViewBag.DefaultColor = config.Color;
                    ViewBag.DefaultSize = config.Size;
                    ViewBag.DefaultMaterial = config.Material;
                    ViewBag.DefaultPattern = config.Pattern;

                    if (config.Gender.HasValue() && config.Gender != GmcXmlExportProvider.Unspecified)
                    {
                        ViewBag.DefaultGender = T("Plugins.Feed.Froogle.Gender" + culture.TextInfo.ToTitleCase(config.Gender));
                    }

                    if (config.AgeGroup.HasValue() && config.AgeGroup != GmcXmlExportProvider.Unspecified)
                    {
                        ViewBag.DefaultAgeGroup = T("Plugins.Feed.Froogle.AgeGroup" + culture.TextInfo.ToTitleCase(config.AgeGroup));
                    }
                }
            }

            ViewBag.AvailableCategories = model.Taxonomy.HasValue()
                ? new List<SelectListItem> { new SelectListItem { Text = model.Taxonomy, Value = model.Taxonomy, Selected = true } }
                : new List<SelectListItem>();

            ViewData.TemplateInfo.HtmlFieldPrefix = "CustomProperties[GMC]";
            return View(model);
        }

        public ActionResult Configure()
        {
            var model = new ConfigurationModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> GoogleProductList(GridCommand command, ConfigurationModel model)
        {
            var textInfo = CultureInfo.InvariantCulture.TextInfo;
            var yes = T("Admin.Common.Yes").Value;
            var no = T("Admin.Common.No").Value;

            var query = from p in _db.Products
                        join gp in _db.GoogleProducts() on p.Id equals gp.ProductId into Products
                        from gp in Products.DefaultIfEmpty()
                        select new
                        {
                            GoogleProduct = gp,
                            ProductId = p.Id,
                            p.Name,
                            p.Sku,
                            p.ProductTypeId
                        };

            if (model.SearchProductName.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Name, model.SearchProductName);
            }

            if (model.SearchProductSku.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Sku, model.SearchProductSku);
            }
            
            if (model.SearchIsTouched != null)
            {
                query = query.Where(x => x.GoogleProduct.IsTouched == model.SearchIsTouched);
            }

            var googleProducts = await query
                .OrderBy(x => x.Name)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var mapper = MapperFactory.GetMapper<GoogleProduct, GoogleProductModel>();
            var googleProductModels = await googleProducts
                .SelectAsync(async x =>
                {
                    var model = new GoogleProductModel();
                    if (x.GoogleProduct != null)
                    {
                        model = await mapper.MapAsync(x.GoogleProduct);

                        if (model.Gender.HasValue())
                        {
                            model.GenderLocalize = T("Plugins.Feed.Froogle.Gender" + textInfo.ToTitleCase(model.Gender));
                        }

                        if (model.AgeGroup.HasValue())
                        {
                            model.AgeGroupLocalize = T("Plugins.Feed.Froogle.AgeGroup" + textInfo.ToTitleCase(model.AgeGroup));
                        }

                        model.Export2Localize = model.Export ? no : yes;
                        model.IsBundleLocalize = model.IsBundle.HasValue ? (model.IsBundle.Value ? yes : no) : null;
                        model.IsAdultLocalize = model.IsAdult.HasValue ? (model.IsAdult.Value ? yes : no) : null;
                    }
                    else 
                    {
                        // INFO: IN classic this was made in SQL COALESCE([t1].[Export],1)
                        model.Export = true;
                        // INFO: IN classic this was made in SQL COALESCE([t1].[Multipack],0)
                        model.Multipack = 0;
                    }

                    model.ProductId = x.ProductId;
                    model.Sku = x.Sku;
                    model.Name = x.Name;
                    model.ProductTypeId = x.ProductTypeId;

                    if (model.ProductType != ProductType.SimpleProduct)
                    {
                        model.ProductTypeName = T($"Admin.Catalog.Products.ProductType.{model.ProductType}.Label");
                    }

                    return model;
                })
                .AsyncToList();

            var gridModel = new GridModel<GoogleProductModel>
            {
                Rows = googleProductModels,
                Total = await googleProducts.GetTotalCountAsync()
            };

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> GoogleProductUpsert(GoogleProductModel model)
        {
            var googleProduct = await _db.GoogleProducts()
                .FirstOrDefaultAsync(x => x.ProductId == model.ProductId);

            var success = false;
            var insert = googleProduct == null;
            var utcNow = DateTime.UtcNow;

            googleProduct ??= new GoogleProduct
            {
                ProductId = model.ProductId,
                CreatedOnUtc = utcNow
            };

            await MapperFactory.MapAsync(model, googleProduct);

            googleProduct.UpdatedOnUtc = utcNow;
            googleProduct.IsTouched = googleProduct.IsTouched();

            if (insert)
            {
                _db.GoogleProducts().Add(googleProduct);
            }
            else if(!googleProduct.IsTouched) 
            {
                _db.GoogleProducts().Remove(googleProduct);
            }
            
            await _db.SaveChangesAsync();
            success = true;

            return Json(new { success });
        }

        public async Task<IActionResult> GetGoogleCategories(string search, int? page)
        {
            const int take = 100;

            page ??= 1;
            
            var skip = (page.Value - 1) * take;
            var (categories, hasMoreItems) = await GetTaxonomyListAsync(search, skip, take);
            var items = categories.Select(x => new { id = x, text = x }).ToList();

            return Json(new 
            {
                hasMoreItems,
                results = items
            });
        }

        private async Task<(List<string> categories, bool hasMoreItems)> GetTaxonomyListAsync(string searchTerm, int skip, int take)
        {
            var categories = new List<string>(take);
            var hasMoreItems = false;

            try
            {
                var provider = _providerManager.GetProvider("Feeds.GoogleMerchantCenterProductXml");
                var module = provider.Metadata.ModuleDescriptor;
                var fileDir = "Files";
                var fileName = $"taxonomy.{Services.WorkContext.WorkingLanguage.LanguageCulture ?? "de-DE"}.txt";
                var filter = searchTerm.HasValue();
                string line;

                var file = module.ContentRoot.GetFile(PathUtility.Combine(fileDir, fileName));
                if (!file.Exists)
                {
                    file = module.ContentRoot.GetFile(PathUtility.Combine(fileDir, "taxonomy.en-US.txt"));
                }

                int numSkipped = 0;
                int numTook = 0;

                using var reader = new StreamReader(file.OpenRead(), Encoding.UTF8);
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    if (filter && !line.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (numSkipped < skip)
                    {
                        numSkipped++;
                        continue;
                    }

                    categories.Add(line);

                    numTook++;
                    if (numTook >= take)
                    {
                        hasMoreItems = await reader.ReadLineAsync() != null;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return (categories, hasMoreItems);
        }
    }
}
