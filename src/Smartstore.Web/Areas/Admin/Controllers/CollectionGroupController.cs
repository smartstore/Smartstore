using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Admin.Models.Catalog;
using Smartstore.Admin.Models.Common;
using Smartstore.Admin.Models.Customers;
using Smartstore.ComponentModel;
using Smartstore.Core.Localization;
using Smartstore.Core.Rules.Filters;
using Smartstore.Core.Security;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class CollectionGroupController : AdminController
    {
        private readonly SmartDbContext _db;

        public CollectionGroupController(SmartDbContext db)
        {
            _db = db;
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
                .Select(x => new SelectListItem { Text = x, Value = x })
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
                    await _db.SaveChangesAsync();

                    NotifySuccess(T("Admin.Common.DataSuccessfullySaved"));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(string.Empty, ex.Message);
                    return View(model);
                }

                ViewBag.RefreshPage = true;
                ViewBag.BtnId = btnId;
                ViewBag.FormId = formId;
            }

            return View(model);
        }
    }
}
