using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Catalog;
using Smartstore.Admin.Models.Common;
using Smartstore.Admin.Models.Customers;
using Smartstore.ComponentModel;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Core.Seo;
using Smartstore.Data;
using Smartstore.Web.Models;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class CollectionGroupController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ILocalizedEntityService _localizedEntityService;

        public CollectionGroupController(SmartDbContext db, ILocalizedEntityService localizedEntityService)
        {
            _db = db;
            _localizedEntityService = localizedEntityService;
        }

        // AJAX.
        public async Task<IActionResult> AllCollectionGroups(string entityName, string selectedName)
        {
            Guard.NotEmpty(entityName);

            var query = _db.CollectionGroups
                .AsNoTracking()
                .Where(x => x.EntityName == entityName)
                .OrderBy(x => x.Name);

            var pager = new FastPager<CollectionGroup>(query, 1000);
            var unpublishedStr = T("Common.Unpublished").Value;
            var allGroups = new List<dynamic>();

            while ((await pager.ReadNextPageAsync<CollectionGroup>()).Out(out var collectionGroups))
            {
                foreach (var group in collectionGroups)
                {
                    dynamic obj = new { group.Id, group.Name, group.Published };
                    allGroups.Add(obj);
                }
            }

            var data = allGroups
                .OrderBy(x => x.Name)
                .Select(x => new ChoiceListItem
                {
                    Id = x.Name,
                    Text = x.Name,
                    Selected = selectedName != null && x.Name == selectedName,
                    Title = !x.Published ? unpublishedStr : null,
                    CssClass = !x.Published ? "choice-item-unavailable" : null
                })
                .ToList();

            return new JsonResult(data);
        }

        [Permission(Permissions.Configuration.CollectionGroup.Read)]
        public async Task<IActionResult> List()
        {
            var model = new CollectionGroupListModel();

            var entityNames = await _db.CollectionGroups
                .Select(x => x.EntityName)
                .Distinct()
                .ToListAsync();

            ViewBag.EntityNames = entityNames
                .OrderBy(x => x)
                .Select(x => new SelectListItem 
                { 
                    Text = Services.Localization.GetResource("Common.Entity." + x, 0, false, string.Empty, true), 
                    Value = x 
                })
                .ToList();

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.CollectionGroup.Read)]
        public async Task<IActionResult> CollectionGroupList(GridCommand command, CollectionGroupListModel model)
        {
            var query = _db.CollectionGroups.AsNoTracking();
            if (model.EntityName.HasValue())
            {
                query = query.Where(x => x.EntityName == model.EntityName);
            }
            if (model.Published != null)
            {
                query = query.Where(x => x.Published == model.Published.Value);
            }

            var collectionGroups = await query
                .OrderBy(x => x.EntityName)
                .ThenBy(x => x.DisplayOrder)
                .ThenBy(x => x.Name)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var models = await collectionGroups.MapAsync(_db);

            return Json(new GridModel<CollectionGroupModel>
            {
                Rows = models,
                Total = await collectionGroups.GetTotalCountAsync()
            });
        }

        [HttpPost]
        [Permission(Permissions.Configuration.CollectionGroup.Delete)]
        public async Task<IActionResult> CollectionGroupDelete(GridSelection selection)
        {
            var entities = await _db.CollectionGroups.GetManyAsync(selection.GetEntityIds(), true);
            if (entities.Count > 0)
            {
                _db.CollectionGroups.RemoveRange(entities);
                await _db.SaveChangesAsync();
            }

            return Json(new { Success = true, entities.Count });
        }

        [Permission(Permissions.Configuration.CollectionGroup.Create)]
        public IActionResult CreateCollectionGroupPopup(string btnId, string formId)
        {
            var model = new CollectionGroupModel();
            AddLocales(model.Locales);

            ViewBag.EntityNames = GetEntityNames();
            ViewBag.BtnId = btnId;
            ViewBag.FormId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.CollectionGroup.Create)]
        public async Task<IActionResult> CreateCollectionGroupPopup(CollectionGroupModel model, string btnId, string formId)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var mapper = MapperFactory.GetMapper<CollectionGroupModel, CollectionGroup>();
                    var collectionGroup = await mapper.MapAsync(model);
                    _db.CollectionGroups.Add(collectionGroup);
                    await _db.SaveChangesAsync();

                    await UpdateLocales(model, collectionGroup);
                    await _db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }

                ViewBag.EntityNames = GetEntityNames();
                ViewBag.RefreshPage = true;
                ViewBag.BtnId = btnId;
                ViewBag.FormId = formId;
            }

            return View(model);
        }

        [Permission(Permissions.Configuration.CollectionGroup.Read)]
        public async Task<IActionResult> EditCollectionGroupPopup(int id, string btnId, string formId)
        {
            var collectionGroup = await _db.CollectionGroups
                .Include(x => x.CollectionGroupMappings)
                .FindByIdAsync(id, false);
            if (collectionGroup == null)
            {
                return NotFound();
            }

            var mapper = MapperFactory.GetMapper<CollectionGroup, CollectionGroupModel>();
            var model = await mapper.MapAsync(collectionGroup);

            AddLocales(model.Locales, (locale, languageId) =>
            {
                locale.Name = collectionGroup.GetLocalized(x => x.Name, languageId, false, false);
            });

            ViewBag.BtnId = btnId;
            ViewBag.FormId = formId;

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.CollectionGroup.Update)]
        public async Task<IActionResult> EditCollectionGroupPopup(CollectionGroupModel model, string btnId, string formId)
        {
            var collectionGroup = await _db.CollectionGroups
                .Include(x => x.CollectionGroupMappings)
                .FindByIdAsync(model.Id);
            if (collectionGroup == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var mapper = MapperFactory.GetMapper<CollectionGroupModel, CollectionGroup>();
                    await mapper.MapAsync(model, collectionGroup);
                    await UpdateLocales(model, collectionGroup);
                    await _db.SaveChangesAsync();

                    NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                }

                ViewBag.RefreshPage = true;
                ViewBag.BtnId = btnId;
                ViewBag.FormId = formId;
            }

            return View(model);
        }

        private async Task UpdateLocales(CollectionGroupModel model, CollectionGroup collectionGroup)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(collectionGroup, x => x.Name, localized.Name, localized.LanguageId);
            }
        }

        private List<SelectListItem> GetEntityNames()
        {
            var entityNames = Services.Cache.Get("CollectionGroup:EntityNames", () =>
            {
                return Services.ApplicationContext.TypeScanner
                    .FindTypes<IGroupedEntity>()
                    .Select(x => NamedEntity.GetEntityName(x))
                    .ToArray();
            });

            return entityNames
                .Select(x => new SelectListItem 
                { 
                    Text = Services.Localization.GetResource("Common.Entity." + x, 0, false, string.Empty, true), 
                    Value = x 
                })
                .ToList();
        }
    }
}
