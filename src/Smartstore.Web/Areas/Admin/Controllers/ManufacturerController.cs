using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Catalog;
using Smartstore.Collections;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Core.Stores;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class ManufacturerController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IDiscountService _discountService;
        private readonly IUrlService _urlService;
        private readonly IStoreMappingService _storeMappingService;
        private readonly IAclService _aclService;
        private readonly ILocalizedEntityService _localizedEntityService;
        private readonly CatalogSettings _catalogSettings;

        public ManufacturerController(
            SmartDbContext db,
            IDiscountService discountService,
            IUrlService urlService,
            IStoreMappingService storeMappingService,
            IAclService aclService,
            ILocalizedEntityService localizedEntityService,
            CatalogSettings catalogSettings)
        {
            _db = db;
            _discountService = discountService;
            _urlService = urlService;
            _storeMappingService = storeMappingService;
            _aclService = aclService;
            _localizedEntityService = localizedEntityService;
            _catalogSettings = catalogSettings;
        }

        /// <summary>
        /// (AJAX) Gets a list of all available manufacturers. 
        /// </summary>
        /// <param name="label">Text for optional entry. If not null an entry with the specified label text and the Id 0 will be added to the list.</param>
        /// <param name="selectedId">Id of selected entity.</param>
        /// <returns>List of all manufacturers as JSON.</returns>
        public async Task<IActionResult> AllManufacturers(string label, int selectedId)
        {
            var manufacturers = await _db.Manufacturers
                .AsNoTracking()
                .ApplyStandardFilter(true)
                .ToListAsync();

            if (label.HasValue())
            {
                manufacturers.Insert(0, new Manufacturer { Name = label, Id = 0 });
            }

            var list = from m in manufacturers
                       select new
                       {
                           id = m.Id.ToString(),
                           text = m.GetLocalized(x => x.Name).Value,
                           selected = m.Id == selectedId
                       };

            var mainList = list.ToList();

            var mruList = new TrimmedBuffer<string>(
                Services.WorkContext.CurrentCustomer.GenericAttributes.MostRecentlyUsedManufacturers,
                _catalogSettings.MostRecentlyUsedManufacturersMaxSize)
                .Reverse()
                .Select(x =>
                {
                    var item = manufacturers.FirstOrDefault(m => m.Id.ToString() == x);
                    if (item != null)
                    {
                        return new
                        {
                            id = x,
                            text = item.GetLocalized(y => y.Name).Value,
                            selected = false
                        };
                    }

                    return null;
                })
                .Where(x => x != null)
                .ToList();

            object data = mainList;
            if (mruList.Count > 0)
            {
                data = new List<object>
                {
                    new Dictionary<string, object> { ["text"] = T("Common.Mru").Value, ["children"] = mruList },
                    new Dictionary<string, object> { ["text"] = T("Admin.Catalog.Manufacturers").Value, ["children"] = mainList, ["main"] = true }
                };
            }

            return new JsonResult(data);
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Catalog.Manufacturer.Read)]
        public IActionResult List()
        {
            ViewBag.IsSingleStoreMode = Services.StoreContext.IsSingleStoreMode();

            return View();
        }

        [Permission(Permissions.Catalog.Manufacturer.Read)]
        public async Task<IActionResult> ManufacturerList(GridCommand command, ManufacturerListModel model)
        {
            var query = _db.Manufacturers.AsNoTracking();

            if (model.SearchManufacturerName.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Name, model.SearchManufacturerName);
            }

            var manufacturers = await query
                .ApplyStandardFilter(true, null, model.SearchStoreId)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var rows = manufacturers.Select(x => new ManufacturerModel
            {
                Id = x.Id,
                Name = x.Name,
                Published = x.Published,
                DisplayOrder = x.DisplayOrder,
                LimitedToStores = x.LimitedToStores,
                EditUrl = Url.Action("Edit", "Manufacturer", new { id = x.Id, area = "Admin" }),
                CreatedOn = Services.DateTimeHelper.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                UpdatedOn = Services.DateTimeHelper.ConvertToUserTime(x.UpdatedOnUtc, DateTimeKind.Utc)
            })
            .ToList();

            return Json(new GridModel<ManufacturerModel>
            {
                Rows = rows,
                Total = manufacturers.TotalCount
            });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Manufacturer.Delete)]
        public async Task<IActionResult> ManufacturerDelete(GridSelection selection)
        {
            var entities = await _db.Manufacturers.GetManyAsync(selection.GetEntityIds(), true);
            if (entities.Count > 0)
            {
                _db.Manufacturers.RemoveRange(entities);
                await _db.SaveChangesAsync();

                Services.ActivityLogger.LogActivity(
                    KnownActivityLogTypes.DeleteManufacturer, 
                    T("ActivityLog.DeleteManufacturer"), 
                    string.Join(", ", entities.Select(x => x.Name)));
            }

            return Json(new { Success = true, entities.Count });
        }

        [Permission(Permissions.Catalog.Manufacturer.Create)]
        public async Task<IActionResult> Create()
        {
            var model = new ManufacturerModel
            {
                Published = true
            };

            AddLocales(model.Locales);
            await PrepareManufacturerModel(model, null);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Catalog.Manufacturer.Create)]
        public async Task<IActionResult> Create(ManufacturerModel model, bool continueEditing, IFormCollection form)
        {
            if (ModelState.IsValid)
            {
                var mapper = MapperFactory.GetMapper<ManufacturerModel, Manufacturer>();
                var manufacturer = await mapper.MapAsync(model);
                _db.Manufacturers.Add(manufacturer);

                await _db.SaveChangesAsync();

                var slugResult = await _urlService.SaveSlugAsync(manufacturer, model.SeName, manufacturer.GetDisplayName(), true);
                model.SeName = slugResult.Slug;

                await ApplyLocales(model, manufacturer);

                await _discountService.ApplyDiscountsAsync(manufacturer, model?.SelectedDiscountIds, DiscountType.AssignedToManufacturers);
                await _storeMappingService.ApplyStoreMappingsAsync(manufacturer, model.SelectedStoreIds);
                await _aclService.ApplyAclMappingsAsync(manufacturer, model.SelectedCustomerRoleIds);

                await _db.SaveChangesAsync();

                await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, manufacturer, form));

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.AddNewManufacturer, T("ActivityLog.AddNewManufacturer"), manufacturer.Name);
                NotifySuccess(T("Admin.Catalog.Manufacturers.Added"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = manufacturer.Id })
                    : RedirectToAction(nameof(List));
            }

            await PrepareManufacturerModel(model, null);

            return View(model);
        }

        [Permission(Permissions.Catalog.Manufacturer.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var manufacturer = await _db.Manufacturers
                .Include(x => x.AppliedDiscounts)
                .FindByIdAsync(id, false);

            if (manufacturer == null)
            {
                return NotFound();
            }

            var mapper = MapperFactory.GetMapper<Manufacturer, ManufacturerModel>();
            var model = await mapper.MapAsync(manufacturer);

            await AddLocalesAsync(model.Locales, async (locale, languageId) =>
            {
                locale.Name = manufacturer.GetLocalized(x => x.Name, languageId, false, false);
                locale.Description = manufacturer.GetLocalized(x => x.Description, languageId, false, false);
                locale.BottomDescription = manufacturer.GetLocalized(x => x.BottomDescription, languageId, false, false);
                locale.MetaKeywords = manufacturer.GetLocalized(x => x.MetaKeywords, languageId, false, false);
                locale.MetaDescription = manufacturer.GetLocalized(x => x.MetaDescription, languageId, false, false);
                locale.MetaTitle = manufacturer.GetLocalized(x => x.MetaTitle, languageId, false, false);
                locale.SeName = await manufacturer.GetActiveSlugAsync(languageId, false, false);
            });

            await PrepareManufacturerModel(model, manufacturer);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Catalog.Manufacturer.Update)]
        public async Task<IActionResult> Edit(ManufacturerModel model, bool continueEditing, IFormCollection form)
        {
            var manufacturer = await _db.Manufacturers
                .Include(x => x.AppliedDiscounts)
                .FindByIdAsync(model.Id);

            if (manufacturer == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var mapper = MapperFactory.GetMapper<ManufacturerModel, Manufacturer>();
                await mapper.MapAsync(model, manufacturer);

                var slugResult = await _urlService.SaveSlugAsync(manufacturer, model.SeName, manufacturer.GetDisplayName(), true);
                model.SeName = slugResult.Slug;

                await ApplyLocales(model, manufacturer);
                await _discountService.ApplyDiscountsAsync(manufacturer, model?.SelectedDiscountIds, DiscountType.AssignedToManufacturers);
                await _storeMappingService.ApplyStoreMappingsAsync(manufacturer, model.SelectedStoreIds);
                await _aclService.ApplyAclMappingsAsync(manufacturer, model.SelectedCustomerRoleIds);

                _db.Manufacturers.Update(manufacturer);
                await _db.SaveChangesAsync();

                await Services.EventPublisher.PublishAsync(new ModelBoundEvent(model, manufacturer, form));

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditManufacturer, T("ActivityLog.EditManufacturer"), manufacturer.Name);
                NotifySuccess(T("Admin.Catalog.Manufacturers.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), manufacturer.Id)
                    : RedirectToAction(nameof(List));
            }

            await PrepareManufacturerModel(model, manufacturer);

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Manufacturer.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var manufacturer = await _db.Manufacturers.FindByIdAsync(id);
            if (manufacturer == null)
            {
                return NotFound();
            }

            _db.Manufacturers.Remove(manufacturer);
            await _db.SaveChangesAsync();

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.DeleteManufacturer, T("ActivityLog.DeleteManufacturer"), manufacturer.Name);
            NotifySuccess(T("Admin.Catalog.Manufacturers.Deleted"));

            return RedirectToAction(nameof(List));
        }

        #region Product manufacturers

        [Permission(Permissions.Catalog.Manufacturer.Read)]
        public async Task<IActionResult> ProductManufacturerList(GridCommand command, int manufacturerId)
        {
            var productManufacturers = await _db.ProductManufacturers
                .AsNoTracking()
                .ApplyManufacturerFilter(manufacturerId)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var rows = productManufacturers.Select(x =>
            {
                var product = x.Product;
                var model = MiniMapper.Map<ProductManufacturer, ManufacturerProductModel>(x);

                model.ProductName = product.GetLocalized(x => x.Name);
                model.Sku = product.Sku;
                model.ProductTypeName = product.GetProductTypeLabel(Services.Localization);
                model.ProductTypeLabelHint = product.ProductTypeLabelHint;
                model.Published = product.Published;
                model.EditUrl = Url.Action("Edit", "Product", new { id = x.ProductId, area = "Admin" });

                return model;
            })
            .ToList();

            return Json(new GridModel<ManufacturerProductModel>
            {
                Rows = rows,
                Total = productManufacturers.TotalCount
            });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Manufacturer.EditProduct)]
        public async Task<IActionResult> ProductManufacturerInsert(ManufacturerProductModel model, int manufacturerId)
        {
            var success = false;

            if (!await _db.ProductManufacturers.AnyAsync(x => x.ManufacturerId == manufacturerId && x.ProductId == model.ProductId))
            {
                _db.ProductManufacturers.Add(new ProductManufacturer
                {
                    ManufacturerId = manufacturerId,
                    ProductId = model.ProductId,
                    IsFeaturedProduct = model.IsFeaturedProduct,
                    DisplayOrder = model.DisplayOrder
                });

                await _db.SaveChangesAsync();
                success = true;
            }
            else
            {
                NotifyError(T("Admin.Catalog.Products.Manufacturers.NoDuplicatesAllowed"));
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Manufacturer.EditProduct)]
        public async Task<IActionResult> ProductManufacturerUpdate(ManufacturerProductModel model)
        {
            var success = false;

            var productManufacturer = await _db.ProductManufacturers.FindByIdAsync(model.Id);
            if (productManufacturer != null)
            {
                if (model.ProductId != productManufacturer.ProductId &&
                    await _db.ProductManufacturers.AnyAsync(x => x.ManufacturerId == model.ManufacturerId && x.ProductId == model.ProductId))
                {
                    NotifyError(T("Admin.Catalog.Products.Manufacturers.NoDuplicatesAllowed"));
                }
                else
                {
                    productManufacturer.ProductId = model.ProductId;
                    productManufacturer.IsFeaturedProduct = model.IsFeaturedProduct;
                    productManufacturer.DisplayOrder = model.DisplayOrder;

                    await _db.SaveChangesAsync();
                    success = true;
                }
            }

            return Json(new { success });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Manufacturer.EditProduct)]
        public async Task<IActionResult> ProductManufacturerDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var productManufacturers = await _db.ProductManufacturers.GetManyAsync(ids, true);

                _db.ProductManufacturers.RemoveRange(productManufacturers);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        #endregion

        private async Task PrepareManufacturerModel(ManufacturerModel model, Manufacturer manufacturer)
        {
            if (manufacturer != null)
            {
                model.CreatedOn = Services.DateTimeHelper.ConvertToUserTime(manufacturer.CreatedOnUtc, DateTimeKind.Utc);
                model.UpdatedOn = Services.DateTimeHelper.ConvertToUserTime(manufacturer.UpdatedOnUtc, DateTimeKind.Utc);
                model.SelectedDiscountIds = manufacturer.AppliedDiscounts.Select(d => d.Id).ToArray();
                model.SelectedStoreIds = await _storeMappingService.GetAuthorizedStoreIdsAsync(manufacturer);
                model.SelectedCustomerRoleIds = await _aclService.GetAuthorizedCustomerRoleIdsAsync(manufacturer);
                model.ManufacturerUrl = await GetEntityPublicUrlAsync(manufacturer);
            }

            var manufacturerTemplates = await _db.ManufacturerTemplates
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            ViewBag.ManufacturerTemplates = manufacturerTemplates
                .Select(x => new SelectListItem { Text = x.Name, Value = x.Id.ToString() })
                .ToList();
        }

        private async Task ApplyLocales(ManufacturerModel model, Manufacturer manufacturer)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(manufacturer, x => x.Name, localized.Name, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(manufacturer, x => x.Description, localized.Description, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(manufacturer, x => x.BottomDescription, localized.BottomDescription, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(manufacturer, x => x.MetaKeywords, localized.MetaKeywords, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(manufacturer, x => x.MetaDescription, localized.MetaDescription, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(manufacturer, x => x.MetaTitle, localized.MetaTitle, localized.LanguageId);

                await _urlService.SaveSlugAsync(manufacturer, localized.SeName, localized.Name, false, localized.LanguageId);
            }
        }
    }
}
