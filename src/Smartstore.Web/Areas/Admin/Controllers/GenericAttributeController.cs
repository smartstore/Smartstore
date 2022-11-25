using Smartstore.Admin.Models.Common;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Security;
using Smartstore.Web.Models.DataGrid;

namespace Smartstore.Admin.Controllers
{
    public class GenericAttributeController : AdminController
    {

        private readonly SmartDbContext _db;
        private readonly IGenericAttributeService _genericAttributeService;

        public GenericAttributeController(SmartDbContext db, IGenericAttributeService genericAttributeService)
        {
            _db = db;
            _genericAttributeService = genericAttributeService;
        }

        [HttpPost]
        public IActionResult GenericAttributesList(GridCommand command, string entityName, int entityId)
        {
            var storeId = Services.StoreContext.CurrentStore.Id;
            ViewBag.StoreId = storeId;

            var totalCount = 0;
            var attributesItems = new List<GenericAttributeModel>();

            if (entityName.HasValue() && entityId > 0)
            {
                var (readPermission, _) = GetGenericAttributesInfos(entityName);

                if (readPermission.IsEmpty() || Services.Permissions.Authorize(readPermission))
                {
                    var allAttributes = _genericAttributeService.GetAttributesForEntity(entityName, entityId).UnderlyingEntities;

                    var attributes = allAttributes
                        .AsQueryable()
                        .ApplyGridCommand(command, true);

                    totalCount = allAttributes.Count();

                    attributesItems = attributes
                        .Where(x => x.StoreId == storeId || x.StoreId == 0)
                        .Select(x => new GenericAttributeModel
                        {
                            Id = x.Id,
                            AttributeEntityId = x.EntityId,
                            EntityName = x.KeyGroup,
                            Key = x.Key,
                            Value = x.Value
                        })
                        .ToList();
                }
            }

            var gridModel = new GridModel<GenericAttributeModel>
            {
                Rows = attributesItems,
                Total = totalCount
            };

            return Json(gridModel);
        }

        public async Task<IActionResult> GenericAttributeInsert(GenericAttributeModel model, string entityName, int entityId)
        {
            model.Key = model.Key.TrimSafe();
            model.Value = model.Value.TrimSafe() ?? string.Empty;

            if (ModelState.IsValid)
            {
                var storeId = Services.StoreContext.CurrentStore.Id;
                var (readPermission, updatePermission) = GetGenericAttributesInfos(entityName);
                if (updatePermission.HasValue() && !Services.Permissions.Authorize(updatePermission))
                {
                    NotifyError(await Services.Permissions.GetUnauthorizedMessageAsync(updatePermission));
                    return Json(new { success = false });
                }
                else
                {
                    var attr = await _db.GenericAttributes
                        .Where(x => x.EntityId == entityId && x.KeyGroup == entityName && x.Key == model.Key && x.StoreId == storeId)
                        .FirstOrDefaultAsync();

                    if (attr == null)
                    {
                        _db.GenericAttributes.Add(new GenericAttribute
                        {
                            StoreId = storeId,
                            KeyGroup = entityName,
                            EntityId = entityId,
                            Key = model.Key,
                            Value = model.Value
                        });
                        await _db.SaveChangesAsync();
                        return Json(new { success = true });
                    }
                    else
                    {
                        NotifyWarning(T("Admin.Common.GenericAttributes.NameAlreadyExists", model.Key));
                        return Json(new { success = false });
                    }
                }
            }
            else
            {
                var modelStateErrorMessages = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage);
                NotifyError(modelStateErrorMessages.FirstOrDefault());
                return Json(new { success = false });
            }
        }

        public async Task<IActionResult> GenericAttributeUpdate(GenericAttributeModel model)
        {
            var entityName = model.EntityName;
            var entityId = model.AttributeEntityId;

            model.Key = model.Key.TrimSafe();
            model.Value = model.Value.TrimSafe();

            if (ModelState.IsValid)
            {
                var storeId = Services.StoreContext.CurrentStore.Id;
                var (readPermission, updatePermission) = GetGenericAttributesInfos(model.EntityName);

                if (updatePermission.HasValue() && !await Services.Permissions.AuthorizeAsync(updatePermission))
                {
                    NotifyError(await Services.Permissions.GetUnauthorizedMessageAsync(updatePermission));
                    return Json(new { success = false });
                }
                else
                {
                    var attr = await _db.GenericAttributes.FindByIdAsync(model.Id);

                    // If the key changed, ensure it isn't being used by another attribute.
                    if (!attr.Key.EqualsNoCase(model.Key))
                    {
                        var attributes = _genericAttributeService.GetAttributesForEntity(entityName, entityId).UnderlyingEntities;
                        var attr2 = attributes
                            .Where(x => x.StoreId == storeId && x.Key.Equals(model.Key, StringComparison.InvariantCultureIgnoreCase))
                            .FirstOrDefault();

                        if (attr2 != null && attr2.Id != attr.Id)
                        {
                            NotifyWarning(T("Admin.Common.GenericAttributes.NameAlreadyExists", model.Key));
                            return Json(new { success = false });
                        }
                    }

                    attr.Key = model.Key;
                    attr.Value = model.Value;

                    await _db.SaveChangesAsync();

                    return Json(new { success = true });
                }
            }
            else
            {
                var modelStateErrorMessages = ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage);
                NotifyError(modelStateErrorMessages.FirstOrDefault());
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenericAttributeDelete(GridSelection selection, string entityName)
        {
            var ids = selection.GetEntityIds();
            var numDeleted = 0;

            if (ids.Any())
            {
                var (readPermission, updatePermission) = GetGenericAttributesInfos(entityName);

                if (updatePermission.HasValue() && !await Services.Permissions.AuthorizeAsync(updatePermission))
                {
                    NotifyError(await Services.Permissions.GetUnauthorizedMessageAsync(updatePermission));
                    return Json(new { Success = false, Count = numDeleted });
                }

                numDeleted = await _db.GenericAttributes
                    .Where(x => ids.Contains(x.Id))
                    .ExecuteDeleteAsync();
            }

            return Json(new { Success = true, Count = numDeleted });
        }

        private static (string ReadPermission, string UpdatePermission) GetGenericAttributesInfos(string entityName)
        {
            string readPermission = null;
            string updatePermission = null;

            if (entityName.EqualsNoCase(nameof(Order)))
            {
                readPermission = Permissions.Order.Read;
                updatePermission = Permissions.Order.Update;
            }
            else if (entityName.EqualsNoCase(nameof(Topic)))
            {
                readPermission = Permissions.Cms.Topic.Read;
                updatePermission = Permissions.Cms.Topic.Update;
            }

            return (readPermission, updatePermission);
        }
    }
}
