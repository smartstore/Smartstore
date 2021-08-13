using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Catalog;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Core.Logging;
using Smartstore.Core.Security;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.DataGrid;

namespace Smartstore.Web.Areas.Admin.Controllers
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

        // Ajax.
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
            return RedirectToAction("List");
        }

        [Permission(Permissions.Catalog.Attribute.Read)]
        public ActionResult List()
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
                query = query.Where(x => x.Name.Contains(model.SearchName));
            }

            var attributes = await query
                .OrderBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ApplyGridCommand(command, false)
                .ToPagedList(command)
                .LoadAsync();

            var rows = await attributes
                .SelectAsync(async x =>
                {
                    var model = await mapper.MapAsync(x);
                    model.EditUrl = Url.Action("Edit", "SpecificationAttribute", new { id = x.Id, area = "Admin" });
                    model.LocalizedFacetSorting = await x.FacetSorting.GetLocalizedEnumAsync(language.Id);
                    model.LocalizedFacetTemplateHint = await x.FacetTemplateHint.GetLocalizedEnumAsync(language.Id);

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
            var numDeleted = 0;
            var ids = selection.GetEntityIds();

            if (ids.Any())
            {
                var attributes = await _db.SpecificationAttributes.GetManyAsync(ids, true);

                _db.SpecificationAttributes.RemoveRange(attributes);

                numDeleted = await _db.SaveChangesAsync();
                success = true;
            }

            return Json(new { Success = success, Count = numDeleted });
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
                    ? RedirectToAction("Edit", new { id = attribute.Id })
                    : RedirectToAction("List");
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
                    ? RedirectToAction("Edit", attribute.Id) 
                    : RedirectToAction("List");
            }

            return View(model);
        }

        #region Specification attribute options

        [Permission(Permissions.Catalog.Attribute.Read)]
        public async Task<IActionResult> SpecificationAttributeOptionList(GridCommand command, int specificationAttributeId)
        {
            var mapper = MapperFactory.GetMapper<SpecificationAttributeOption, SpecificationAttributeOptionModel>();
            var attribute = await _db.SpecificationAttributes
                .Include(x => x.SpecificationAttributeOptions)
                .FindByIdAsync(specificationAttributeId);

            if (attribute == null)
            {
                return NotFound();
            }

            var rows = await attribute.SpecificationAttributeOptions
                .OrderBy(x => x.DisplayOrder)
                .SelectAsync(async x =>
                {
                    var model = await mapper.MapAsync(x);
                    var name = x.Color.IsEmpty() ? x.Name : $"{x.Name} - {x.Color}";
                    model.NameString = name.HtmlEncode();

                    return model;
                })
                .AsyncToList();

            return Json(new GridModel<SpecificationAttributeOptionModel>
            {
                Rows = rows,
                Total = attribute.SpecificationAttributeOptions.Count
            });
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
    }
}
