using Smartstore.Admin.Models.Catalog;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Data;
using Smartstore.Web.Models;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class ProductAttributeController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ILocalizedEntityService _localizedEntityService;

        public ProductAttributeController(SmartDbContext db, ILocalizedEntityService localizedEntityService)
        {
            _db = db;
            _localizedEntityService = localizedEntityService;
        }

        // AJAX.
        public async Task<IActionResult> AllProductAttributes(string label, int selectedId)
        {
            var query = _db.ProductAttributes
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name);

            var pager = new FastPager<ProductAttribute>(query, 1000);
            var allAttributes = new List<dynamic>();

            while ((await pager.ReadNextPageAsync<ProductAttribute>()).Out(out var attributes))
            {
                foreach (var attribute in attributes)
                {
                    dynamic obj = new
                    {
                        attribute.Id,
                        attribute.DisplayOrder,
                        attribute.Name
                    };

                    allAttributes.Add(obj);
                }
            }

            var data = allAttributes
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .Select(x => new ChoiceListItem
                {
                    Id = x.Id.ToString(),
                    Text = x.Name,
                    Selected = x.Id == selectedId
                })
                .ToList();

            if (label.HasValue())
            {
                data.Insert(0, new ChoiceListItem
                {
                    Id = "0",
                    Text = label,
                    Selected = false
                });
            }

            return new JsonResult(data);
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Catalog.Variant.Read)]
        public IActionResult List()
        {
            return View(new ProductAttributeListModel());
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Variant.Read)]
        public async Task<IActionResult> ProductAttributeList(GridCommand command, ProductAttributeListModel model)
        {
            string optionSetsInfo = T("Admin.Catalog.Attributes.ProductAttributes.OptionsSetsInfo");
            var language = Services.WorkContext.WorkingLanguage;
            var mapper = MapperFactory.GetMapper<ProductAttribute, ProductAttributeModel>();
            var query = _db.ProductAttributes.AsNoTracking();

            if (model.SearchName.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Name, model.SearchName);
            }

            if (model.SearchAlias.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Alias, model.SearchAlias);
            }

            if (model.SearchAllowFiltering.HasValue)
            {
                query = query.Where(x => x.AllowFiltering == model.SearchAllowFiltering.Value);
            }

            var attributes = await query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var attributeIds = attributes.Select(x => x.Id).ToArray();
            var optionsSetsInfo = await _db.ProductAttributes
                .Where(x => attributeIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    NumberOfSets = _db.ProductAttributeOptionsSets.Count(y => y.ProductAttributeId == x.Id),
                    NumberOfOptions = _db.ProductAttributeOptions.Count(y => y.ProductAttributeOptionsSet.ProductAttributeId == x.Id)
                })
                .ToDictionaryAsync(x => x.Id, x => x);

            var rows = await attributes
                .SelectAwait(async x =>
                {
                    var model = await mapper.MapAsync(x);
                    model.EditUrl = Url.Action(nameof(Edit), "ProductAttribute", new { id = x.Id, area = "Admin" });
                    model.LocalizedFacetTemplateHint = x.FacetTemplateHint.GetLocalizedEnum(language.Id);

                    if (optionsSetsInfo.TryGetValue(x.Id, out var info))
                    {
                        model.NumberOfOptionsSets = info.NumberOfSets;
                        model.OptionsSetsInfo = optionSetsInfo.FormatInvariant(info.NumberOfSets.ToString("N0"), info.NumberOfOptions.ToString("N0"));
                    }

                    return model;
                })
                .AsyncToList();

            return Json(new GridModel<ProductAttributeModel>
            {
                Rows = rows,
                Total = attributes.TotalCount
            });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Variant.Delete)]
        public async Task<IActionResult> ProductAttributeDelete(GridSelection selection)
        {
            var success = false;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var attributes = await _db.ProductAttributes.GetManyAsync(ids, true);
                var deletedNames = string.Join(", ", attributes.Select(x => x.Name));

                _db.ProductAttributes.RemoveRange(attributes);

                await _db.SaveChangesAsync();
                success = true;

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.DeleteProductAttribute, T("ActivityLog.DeleteProductAttribute"), deletedNames);
            }

            return Json(new { Success = success });
        }

        [Permission(Permissions.Catalog.Variant.Create)]
        public IActionResult Create()
        {
            var model = new ProductAttributeModel
            {
                AllowFiltering = true
            };

            AddLocales(model.Locales);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Catalog.Variant.Create)]
        public async Task<IActionResult> Create(ProductAttributeModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var mapper = MapperFactory.GetMapper<ProductAttributeModel, ProductAttribute>();
                var attribute = await mapper.MapAsync(model);
                _db.ProductAttributes.Add(attribute);

                await _db.SaveChangesAsync();

                await ApplyLocales(model, attribute);
                await _db.SaveChangesAsync();

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.AddNewProductAttribute, T("ActivityLog.AddNewProductAttribute"), attribute.Name);
                NotifySuccess(T("Admin.Catalog.Attributes.ProductAttributes.Added"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = attribute.Id })
                    : RedirectToAction(nameof(List));
            }

            return View(model);
        }

        [Permission(Permissions.Catalog.Variant.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var attribute = await _db.ProductAttributes.FindByIdAsync(id, false);
            if (attribute == null)
            {
                return NotFound();
            }

            var mapper = MapperFactory.GetMapper<ProductAttribute, ProductAttributeModel>();
            var model = await mapper.MapAsync(attribute);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = attribute.GetLocalized(x => x.Name, languageId, false, false);
                locale.Alias = attribute.GetLocalized(x => x.Alias, languageId, false, false);
                locale.Description = attribute.GetLocalized(x => x.Description, languageId, false, false);
            });

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Catalog.Variant.Update)]
        public async Task<IActionResult> Edit(ProductAttributeModel model, bool continueEditing)
        {
            var attribute = await _db.ProductAttributes.FindByIdAsync(model.Id);
            if (attribute == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var mapper = MapperFactory.GetMapper<ProductAttributeModel, ProductAttribute>();
                await mapper.MapAsync(model, attribute);

                await ApplyLocales(model, attribute);

                await _db.SaveChangesAsync();

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditProductAttribute, T("ActivityLog.EditProductAttribute"), attribute.Name);
                NotifySuccess(T("Admin.Catalog.Attributes.ProductAttributes.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), attribute.Id)
                    : RedirectToAction(nameof(List));
            }

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Variant.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var attribute = await _db.ProductAttributes.FindByIdAsync(id);
            if (attribute == null)
            {
                return NotFound();
            }

            _db.ProductAttributes.Remove(attribute);
            await _db.SaveChangesAsync();

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.DeleteProductAttribute, T("ActivityLog.DeleteProductAttribute"), attribute.Name);
            NotifySuccess(T("Admin.Catalog.Attributes.ProductAttributes.Deleted"));

            return RedirectToAction(nameof(List));
        }

        #region Product attribute options sets

        [Permission(Permissions.Catalog.Variant.Read)]
        public async Task<IActionResult> ProductAttributeOptionsSetList(GridCommand command, int productAttributeId)
        {
            var mapper = MapperFactory.GetMapper<ProductAttributeOptionsSet, ProductAttributeOptionsSetModel>();
            var optionsSets = await _db.ProductAttributeOptionsSets
                .AsNoTracking()
                .Where(x => x.ProductAttributeId == productAttributeId)
                .OrderBy(x => x.Name)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var rows = await optionsSets
                .SelectAwait(async x => await mapper.MapAsync(x))
                .AsyncToList();

            return Json(new GridModel<ProductAttributeOptionsSetModel>
            {
                Rows = rows,
                Total = optionsSets.TotalCount
            });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Variant.EditSet)]
        public async Task<IActionResult> ProductAttributeOptionsSetInsert(ProductAttributeOptionsSetModel model, int productAttributeId)
        {
            var optionsSet = new ProductAttributeOptionsSet
            {
                Name = model.Name,
                ProductAttributeId = productAttributeId
            };

            _db.ProductAttributeOptionsSets.Add(optionsSet);
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Variant.EditSet)]
        public async Task<IActionResult> ProductAttributeOptionsSetUpdate(ProductAttributeOptionsSetModel model)
        {
            var optionsSet = await _db.ProductAttributeOptionsSets.FindByIdAsync(model.Id);
            if (optionsSet == null)
            {
                return NotFound();
            }

            optionsSet.Name = model.Name;
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Variant.EditSet)]
        public async Task<IActionResult> ProductAttributeOptionsSetDelete(GridSelection selection)
        {
            var success = false;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var optionsSets = await _db.ProductAttributeOptionsSets.GetManyAsync(ids, true);

                _db.ProductAttributeOptionsSets.RemoveRange(optionsSets);

                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success });
        }

        #endregion

        #region Product attribute options

        [Permission(Permissions.Catalog.Variant.Read)]
        public async Task<IActionResult> ProductAttributeOptionList(int optionsSetId)
        {
            var mapper = MapperFactory.GetMapper<ProductAttributeOption, ProductAttributeOptionModel>();
            var options = await _db.ProductAttributeOptions
                .AsNoTracking()
                .ApplyStandardFilter(optionsSetId)
                .ToListAsync();

            var linkedProductIds = options
                .Where(x => x.ValueType == ProductVariantAttributeValueType.ProductLinkage && x.LinkedProductId != 0)
                .Select(x => x.LinkedProductId)
                .Distinct()
                .ToArray();

            var linkedProducts = linkedProductIds.Any()
                ? await _db.Products.AsNoTracking().Where(x => linkedProductIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id)
                : new Dictionary<int, Product>();

            var rows = await options
                .SelectAwait(async x =>
                {
                    var m = await mapper.MapAsync(x);
                    await PrepareProductAttributeOptionModel(m, x, linkedProducts);
                    return m;
                })
                .AsyncToList();

            return Json(new GridModel<ProductAttributeOptionModel>
            {
                Rows = rows,
                Total = options.Count
            });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Variant.EditSet)]
        public async Task<IActionResult> ProductAttributeOptionDelete(GridSelection selection)
        {
            var success = false;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var options = await _db.ProductAttributeOptions.GetManyAsync(ids, true);

                _db.ProductAttributeOptions.RemoveRange(options);

                await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success });
        }

        [Permission(Permissions.Catalog.Variant.EditSet)]
        public async Task<IActionResult> ProductAttributeOptionCreatePopup(int productAttributeOptionsSetId, string btnId, string formId)
        {
            var optionsSet = await _db.ProductAttributeOptionsSets.FindByIdAsync(productAttributeOptionsSetId);
            if (optionsSet == null)
            {
                return NotFound();
            }

            var maxDisplayOrder = (await _db.ProductAttributeOptions
                .Where(x => x.ProductAttributeOptionsSetId == optionsSet.Id)
                .MaxAsync(x => (int?)x.DisplayOrder)) ?? 0;

            var model = new ProductAttributeOptionModel
            {
                Quantity = 1,
                Color = string.Empty,
                DisplayOrder = ++maxDisplayOrder
            };

            await PrepareProductAttributeOptionModel(model, null, null);
            AddLocales(model.Locales);

            ViewBag.btnId = btnId;
            ViewBag.formId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Variant.EditSet)]
        public async Task<IActionResult> ProductAttributeOptionCreatePopup(ProductAttributeOptionModel model, string btnId, string formId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var mapper = MapperFactory.GetMapper<ProductAttributeOptionModel, ProductAttributeOption>();
                    var option = await mapper.MapAsync(model);

                    _db.ProductAttributeOptions.Add(option);
                    await _db.SaveChangesAsync();

                    await ApplyOptionLocales(model, option);
                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            if (ModelState.IsValid)
            {
                ViewBag.RefreshPage = true;
                ViewBag.btnId = btnId;
                ViewBag.formId = formId;
            }

            return View(model);
        }

        [Permission(Permissions.Catalog.Variant.EditSet)]
        public async Task<IActionResult> ProductAttributeOptionEditPopup(int id, string btnId, string formId)
        {
            var option = await _db.ProductAttributeOptions.FindByIdAsync(id, false);
            if (option == null)
            {
                return NotFound();
            }

            var mapper = MapperFactory.GetMapper<ProductAttributeOption, ProductAttributeOptionModel>();
            var model = await mapper.MapAsync(option);

            await PrepareProductAttributeOptionModel(model, option, null);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = option.GetLocalized(x => x.Name, languageId, false, false);
                locale.Alias = option.GetLocalized(x => x.Alias, languageId, false, false);
            });

            ViewBag.btnId = btnId;
            ViewBag.formId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Variant.EditSet)]
        public async Task<IActionResult> ProductAttributeOptionEditPopup(ProductAttributeOptionModel model, string btnId, string formId)
        {
            var option = await _db.ProductAttributeOptions.FindByIdAsync(model.Id);
            if (option == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var mapper = MapperFactory.GetMapper<ProductAttributeOptionModel, ProductAttributeOption>();
                    await mapper.MapAsync(model, option);

                    await ApplyOptionLocales(model, option);

                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }
            }

            if (ModelState.IsValid)
            {
                ViewBag.RefreshPage = true;
                ViewBag.btnId = btnId;
                ViewBag.formId = formId;
            }

            return View(model);
        }

        #endregion;

        private async Task PrepareProductAttributeOptionModel(ProductAttributeOptionModel model, ProductAttributeOption option, Dictionary<int, Product> linkedProducts)
        {
            // TODO: DRY, similar code in ProductController (ProductAttributeValueList, ProductAttributeValueEditPopup...)
            if (option != null)
            {
                model.NameString = option.Color.IsEmpty() ? option.Name : $"{option.Name} - {option.Color}";
                model.NameString = model.NameString.HtmlEncode();
                model.PriceAdjustmentString = option.ValueType == ProductVariantAttributeValueType.Simple ? option.PriceAdjustment.ToString("G29") : string.Empty;
                model.WeightAdjustmentString = option.ValueType == ProductVariantAttributeValueType.Simple ? option.WeightAdjustment.ToString("G29") : string.Empty;
                model.TypeName = Services.Localization.GetLocalizedEnum(option.ValueType);
                model.TypeNameClass = option.ValueType == ProductVariantAttributeValueType.ProductLinkage ? "fa fa-link mr-2" : "d-none hide hidden-xs-up";

                if (option.LinkedProductId != 0)
                {
                    var linkedProduct = linkedProducts?.Get(option.LinkedProductId) ?? await _db.Products.FindByIdAsync(option.LinkedProductId, false);
                    if (linkedProduct != null)
                    {
                        model.LinkedProductName = linkedProduct.GetLocalized(p => p.Name);
                        model.LinkedProductTypeName = linkedProduct.GetProductTypeLabel(Services.Localization);
                        model.LinkedProductTypeLabelHint = linkedProduct.ProductTypeLabelHint;
                        model.LinkedProductEditUrl = Url.Action("Edit", "Product", new { id = linkedProduct.Id });

                        if (model.Quantity > 1)
                        {
                            model.QuantityInfo = $" × {model.Quantity}";
                        }
                    }
                }
            }
        }

        private async Task ApplyLocales(ProductAttributeModel model, ProductAttribute attribute)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(attribute, x => x.Name, localized.Name, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(attribute, x => x.Alias, localized.Alias, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(attribute, x => x.Description, localized.Description, localized.LanguageId);
            }
        }

        private async Task ApplyOptionLocales(ProductAttributeOptionModel model, ProductAttributeOption option)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(option, x => x.Name, localized.Name, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(option, x => x.Alias, localized.Alias, localized.LanguageId);
            }
        }
    }
}
