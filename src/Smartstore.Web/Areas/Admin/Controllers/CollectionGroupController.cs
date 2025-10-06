using Smartstore.Admin.Models.Common;
using Smartstore.ComponentModel;
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class CollectionGroupController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly ILocalizedEntityService _localizedEntityService;

        public CollectionGroupController(SmartDbContext db,
            ILocalizedEntityService localizedEntityService)
        {
            _db = db;
            _localizedEntityService = localizedEntityService;
        }

        [Permission(Permissions.Configuration.CollectionGroup.Read)]
        public async Task<IActionResult> List()
        {
            var model = new CollectionGroupListModel();

            var entityNames = await _db.CollectionGroups
                .Select(x => x.EntityName)
                .Distinct()
                .ToListAsync();

            ViewBag.EntityNames = entityNames.OrderBy(x => x).ToList();

            return View(model);
        }

        [HttpPost]
        [Permission(Permissions.Configuration.CollectionGroup.Read)]
        public async Task<IActionResult> CollectionGroupList(GridCommand command, CollectionGroupListModel model)
        {
            var query = _db.CollectionGroups.AsNoTracking();

            if (model.Published != null)
            {
                query = query.Where(x => x.Published == model.Published.Value);
            }

            var collectionGroups = await _db.CollectionGroups
                .AsNoTracking()
                .ApplyEntityFilter(model.EntityName, model.EntityId != null ? [model.EntityId.Value] : null, true)
                .ApplyGridCommand(command)
                .ToPagedList(command)
                .LoadAsync();

            var mapper = MapperFactory.GetMapper<CollectionGroup, CollectionGroupModel>();
            var models = await collectionGroups
                .SelectAwait(async x => await mapper.MapAsync(x))
                .AsyncToList();

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
                    var collectionGroup = await MapperFactory.MapAsync<CollectionGroupModel, CollectionGroup>(model);
                    _db.CollectionGroups.Add(collectionGroup);
                    await _db.SaveChangesAsync();

                    await UpdateLocalesAsync(collectionGroup, model);
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

        [Permission(Permissions.Configuration.CollectionGroup.Read)]
        public async Task<IActionResult> EditCollectionGroupPopup(int id, string btnId, string formId)
        {
            var collectionGroup = await _db.CollectionGroups.FindByIdAsync(id, false);
            if (collectionGroup == null)
            {
                return NotFound();
            }

            var model = await MapperFactory.MapAsync<CollectionGroup, CollectionGroupModel>(collectionGroup);

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
            var collectionGroup = await _db.CollectionGroups.FindByIdAsync(model.Id);
            if (collectionGroup == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await MapperFactory.MapAsync(model, collectionGroup);
                    await UpdateLocalesAsync(collectionGroup, model);
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


        private async Task UpdateLocalesAsync(CollectionGroup collectionGroup, CollectionGroupModel model)
        {
            foreach (var localized in model.Locales)
            {
                await _localizedEntityService.ApplyLocalizedValueAsync(collectionGroup, x => x.Name, localized.Name, localized.LanguageId);
            }
        }
    }
}
