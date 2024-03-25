using Smartstore.Admin.Models.Catalog;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Data;
using Smartstore.Web.Models;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class SpecificationAttributeController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ILocalizedEntityService _localizedEntityService;

        public SpecificationAttributeController(SmartDbContext db, ILocalizedEntityService localizedEntityService)
        {
            _db = db;
            _localizedEntityService = localizedEntityService;
        }

        // AJAX.
        public async Task<IActionResult> AllSpecificationAttributes(string label, string selectedIds)
        {
            var query = _db.SpecificationAttributes
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name);

            var ids = selectedIds.ToIntArray().ToList();
            var pager = new FastPager<SpecificationAttribute>(query, 1000);
            var allAttributes = new List<dynamic>();

            while ((await pager.ReadNextPageAsync<SpecificationAttribute>()).Out(out var attributes))
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
                    Selected = ids.Contains(x.Id)
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

        // AJAX.
        public async Task<IActionResult> GetOptionsByAttributeId(int attributeId)
        {
            var options = await _db.SpecificationAttributeOptions
                .AsNoTracking()
                .Where(x => x.SpecificationAttributeId == attributeId)
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            var result =
                from o in options
                select new { id = o.Id, name = o.Name, text = o.Name };

            return Json(result.ToList());
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Attribute.Update)]
        public async Task<IActionResult> SetAttributeValue(string pk, string value, string name)
        {
            var success = false;
            var message = string.Empty;

            // "Name" is the entity ID of product specification attribute mapping.
            var attribute = await _db.ProductSpecificationAttributes.FindByIdAsync(Convert.ToInt32(name));

            try
            {
                attribute.SpecificationAttributeOptionId = Convert.ToInt32(value);
                await _db.SaveChangesAsync();
                success = true;
            }
            catch (Exception ex)
            {
                message = ex.Message;
            }

            // We give back the name to xeditable to overwrite the grid data in success event when a grid element got updated.
            return Json(new { success, message, name = attribute.SpecificationAttributeOption?.Name });
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        [Permission(Permissions.Catalog.Attribute.Read)]
        public IActionResult List()
        {
            return View(new SpecificationAttributeListModel());
        }

        [Permission(Permissions.Catalog.Attribute.Read)]
        public async Task<IActionResult> SpecificationAttributeList(GridCommand command, SpecificationAttributeListModel model)
        {
            var language = Services.WorkContext.WorkingLanguage;
            var mapper = MapperFactory.GetMapper<SpecificationAttribute, SpecificationAttributeModel>();
            var query = _db.SpecificationAttributes.AsNoTracking();

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

            if (model.SearchShowOnProductPage.HasValue)
            {
                query = query.Where(x => x.ShowOnProductPage == model.SearchShowOnProductPage.Value);
            }

            if (model.SearchEssential.HasValue)
            {
                query = query.Where(x => x.Essential == model.SearchEssential.Value);
            }

            var attributes = await query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var attributeIds = attributes.Select(x => x.Id).ToArray();
            var numberOfOptions = await _db.SpecificationAttributes
                .Where(x => attributeIds.Contains(x.Id))
                .Select(x => new
                {
                    x.Id,
                    NumberOfOptions = _db.SpecificationAttributeOptions.Count(y => y.SpecificationAttributeId == x.Id)
                })
                .ToDictionaryAsync(x => x.Id, x => x);

            var rows = await attributes
                .SelectAwait(async x =>
                {
                    var model = await mapper.MapAsync(x);
                    model.EditUrl = Url.Action(nameof(Edit), "SpecificationAttribute", new { id = x.Id, area = "Admin" });
                    model.LocalizedFacetSorting = x.FacetSorting.GetLocalizedEnum(language.Id);
                    model.LocalizedFacetTemplateHint = x.FacetTemplateHint.GetLocalizedEnum(language.Id);

                    if (numberOfOptions.TryGetValue(x.Id, out var info))
                    {
                        model.NumberOfOptions = info.NumberOfOptions;
                    }

                    return model;
                })
                .AsyncToList();

            return Json(new GridModel<SpecificationAttributeModel>
            {
                Rows = rows,
                Total = attributes.TotalCount
            });
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Attribute.Delete)]
        public async Task<IActionResult> SpecificationAttributeDelete(GridSelection selection)
        {
            var success = false;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var attributes = await _db.SpecificationAttributes.GetManyAsync(ids, true);
                var deletedNames = string.Join(", ", attributes.Select(x => x.Name));

                _db.SpecificationAttributes.RemoveRange(attributes);

                await _db.SaveChangesAsync();
                success = true;

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.DeleteSpecAttribute, T("ActivityLog.DeleteSpecAttribute"), deletedNames);
            }

            return Json(new { Success = success });
        }

        [Permission(Permissions.Catalog.Attribute.Create)]
        public IActionResult Create()
        {
            var model = new SpecificationAttributeModel();

            AddLocales(model.Locales);

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Catalog.Attribute.Create)]
        public async Task<IActionResult> Create(SpecificationAttributeModel model, bool continueEditing)
        {
            if (ModelState.IsValid)
            {
                var mapper = MapperFactory.GetMapper<SpecificationAttributeModel, SpecificationAttribute>();
                var attribute = await mapper.MapAsync(model);
                _db.SpecificationAttributes.Add(attribute);

                await _db.SaveChangesAsync();

                await ApplyLocales(model, attribute);
                await _db.SaveChangesAsync();

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.AddNewSpecAttribute, T("ActivityLog.AddNewSpecAttribute"), attribute.Name);
                NotifySuccess(T("Admin.Catalog.Attributes.SpecificationAttributes.Added"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), new { id = attribute.Id })
                    : RedirectToAction(nameof(List));
            }

            return View(model);
        }

        [Permission(Permissions.Catalog.Attribute.Read)]
        public async Task<IActionResult> Edit(int id)
        {
            var attribute = await _db.SpecificationAttributes.FindByIdAsync(id, false);
            if (attribute == null)
            {
                return NotFound();
            }

            var mapper = MapperFactory.GetMapper<SpecificationAttribute, SpecificationAttributeModel>();
            var model = await mapper.MapAsync(attribute);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = attribute.GetLocalized(x => x.Name, languageId, false, false);
                locale.Alias = attribute.GetLocalized(x => x.Alias, languageId, false, false);
            });

            return View(model);
        }

        [HttpPost, ParameterBasedOnFormName("save-continue", "continueEditing")]
        [Permission(Permissions.Catalog.Attribute.Update)]
        public async Task<IActionResult> Edit(SpecificationAttributeModel model, bool continueEditing)
        {
            var attribute = await _db.SpecificationAttributes.FindByIdAsync(model.Id);
            if (attribute == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var mapper = MapperFactory.GetMapper<SpecificationAttributeModel, SpecificationAttribute>();
                await mapper.MapAsync(model, attribute);

                await ApplyLocales(model, attribute);

                await _db.SaveChangesAsync();

                Services.ActivityLogger.LogActivity(KnownActivityLogTypes.EditSpecAttribute, T("ActivityLog.EditSpecAttribute"), attribute.Name);
                NotifySuccess(T("Admin.Catalog.Attributes.SpecificationAttributes.Updated"));

                return continueEditing
                    ? RedirectToAction(nameof(Edit), attribute.Id)
                    : RedirectToAction(nameof(List));
            }

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Attribute.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            var attribute = await _db.SpecificationAttributes.FindByIdAsync(id);
            if (attribute == null)
            {
                return NotFound();
            }

            _db.SpecificationAttributes.Remove(attribute);
            await _db.SaveChangesAsync();

            Services.ActivityLogger.LogActivity(KnownActivityLogTypes.DeleteSpecAttribute, T("ActivityLog.DeleteSpecAttribute"), attribute.Name);
            NotifySuccess(T("Admin.Catalog.Attributes.SpecificationAttributes.Deleted"));

            return RedirectToAction(nameof(List));
        }

        #region Specification attribute options

        [Permission(Permissions.Catalog.Attribute.Read)]
        public async Task<IActionResult> SpecificationAttributeOptionList(GridCommand command, int specificationAttributeId, SpecificationAttributeModel model)
        {
            var mapper = MapperFactory.GetMapper<SpecificationAttributeOption, SpecificationAttributeOptionModel>();
            var query = _db.SpecificationAttributeOptions.AsNoTracking();

            if (model.SearchName.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Name, model.SearchName);
            }

            if (model.SearchAlias.HasValue())
            {
                query = query.ApplySearchFilterFor(x => x.Alias, model.SearchAlias);
            }

            var options = await query
                .Where(x => x.SpecificationAttributeId == specificationAttributeId)
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Id)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var rows = await options
                .SelectAwait(async x => await mapper.MapAsync(x))
                .AsyncToList();

            return Json(new GridModel<SpecificationAttributeOptionModel>
            {
                Rows = rows,
                Total = options.TotalCount
            });
        }

        [Permission(Permissions.Catalog.Attribute.EditOption)]
        public async Task<IActionResult> SpecificationAttributeOptionDelete(GridSelection selection)
        {
            var success = false;
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var options = await _db.SpecificationAttributeOptions.GetManyAsync(ids, true);

                _db.SpecificationAttributeOptions.RemoveRange(options);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
        }

        [Permission(Permissions.Catalog.Attribute.EditOption)]
        public async Task<IActionResult> SpecificationAttributeOptionCreatePopup(string btnId, string formId, int specificationAttributeId)
        {
            var maxDisplayOrder = (await _db.SpecificationAttributeOptions
                .Where(x => x.SpecificationAttributeId == specificationAttributeId)
                .MaxAsync(x => (int?)x.DisplayOrder)) ?? 0;

            var model = new SpecificationAttributeOptionModel
            {
                SpecificationAttributeId = specificationAttributeId,
                DisplayOrder = ++maxDisplayOrder
            };

            AddLocales(model.Locales);

            ViewBag.MultipleEnabled = true;
            ViewBag.btnId = btnId;
            ViewBag.formId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Catalog.Attribute.EditOption)]
        public async Task<IActionResult> SpecificationAttributeOptionCreatePopup(string btnId, string formId, SpecificationAttributeOptionModel model)
        {
            var attribute = await _db.SpecificationAttributes.FindByIdAsync(model.SpecificationAttributeId, false);
            if (attribute == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await AddSpecificationAttributeOption(model);
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

        [Permission(Permissions.Catalog.Attribute.Read)]
        public async Task<IActionResult> SpecificationAttributeOptionEditPopup(string btnId, string formId, int id)
        {
            var option = await _db.SpecificationAttributeOptions.FindByIdAsync(id, false);
            if (option == null)
            {
                return NotFound();
            }

            var mapper = MapperFactory.GetMapper<SpecificationAttributeOption, SpecificationAttributeOptionModel>();
            var model = await mapper.MapAsync(option);

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
        [Permission(Permissions.Catalog.Attribute.EditOption)]
        public async Task<IActionResult> SpecificationAttributeOptionEditPopup(string btnId, string formId, SpecificationAttributeOptionModel model)
        {
            var option = await _db.SpecificationAttributeOptions.FindByIdAsync(model.Id);
            if (option == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var mapper = MapperFactory.GetMapper<SpecificationAttributeOptionModel, SpecificationAttributeOption>();
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

        #endregion

        private async Task ApplyLocales(SpecificationAttributeModel model, SpecificationAttribute attribute)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(attribute, x => x.Name, localized.Name, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(attribute, x => x.Alias, localized.Alias, localized.LanguageId);
            }
        }

        private async Task ApplyOptionLocales(SpecificationAttributeOptionModel model, SpecificationAttributeOption option)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(option, x => x.Name, localized.Name, localized.LanguageId);
                await _localizedEntityService.ApplyLocalizedValueAsync(option, x => x.Alias, localized.Alias, localized.LanguageId);
            }
        }

        private async Task AddSpecificationAttributeOption(SpecificationAttributeOptionModel model)
        {
            if (model.Multiple)
            {
                var values = model.Name.SplitSafe(';').ToArray();
                var alias = model.Alias.SplitSafe(';').ToArray();
                var displayOrder = model.DisplayOrder;
                // Array index to added option.
                var options = new Dictionary<int, SpecificationAttributeOption>();

                for (var i = 0; i < values.Length; ++i)
                {
                    var name = values.ElementAtOrDefault(i)?.Trim();
                    if (name.HasValue())
                    {
                        options[i] = new SpecificationAttributeOption
                        {
                            Name = name,
                            Alias = alias.ElementAtOrDefault(i)?.Trim(),
                            DisplayOrder = displayOrder++,
                            SpecificationAttributeId = model.SpecificationAttributeId
                        };
                    }
                }

                if (options.Count > 0)
                {
                    _db.SpecificationAttributeOptions.AddRange(options.Select(x => x.Value));
                    await _db.SaveChangesAsync();

                    // Save localized values.
                    foreach (var option in options.Where(x => !x.Value.IsTransientRecord()))
                    {
                        foreach (var locale in model.Locales.Where(x => x.Name.HasValue()))
                        {
                            var localizedNames = locale.Name.SplitSafe(';').ToArray();
                            var localizedName = option.Key < localizedNames.Length
                                ? localizedNames[option.Key].Trim()
                                : option.Value.Name;

                            await _localizedEntityService.ApplyLocalizedValueAsync(option.Value, x => x.Name, localizedName, locale.LanguageId);
                        }

                        foreach (var locale in model.Locales.Where(x => x.Alias.HasValue()))
                        {
                            var localizedAlias = locale.Alias.SplitSafe(';').ToArray();
                            var value = localizedAlias.ElementAtOrDefault(option.Key)?.Trim();

                            if (value.HasValue())
                            {
                                await _localizedEntityService.ApplyLocalizedValueAsync(option.Value, x => x.Alias, value, locale.LanguageId);
                            }
                        }
                    }

                    await _db.SaveChangesAsync();
                }
            }
            else
            {
                var mapper = MapperFactory.GetMapper<SpecificationAttributeOptionModel, SpecificationAttributeOption>();
                var option = await mapper.MapAsync(model);

                _db.SpecificationAttributeOptions.Add(option);
                await _db.SaveChangesAsync();

                await ApplyOptionLocales(model, option);
                await _db.SaveChangesAsync();
            }
        }
    }
}
