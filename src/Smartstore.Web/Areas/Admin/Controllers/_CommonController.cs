using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Common;
using Smartstore.Caching;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Common;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Data.Batching;
using Smartstore.Data.Caching;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling.DataGrid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Smartstore.Web.Areas.Admin.Controllers
{
    public class CommonController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IRequestCache _requestCache;
        private readonly IDbCache _dbCache;

        // TODO: (mh) (core) dbCache cannot be resolved. Breaks...
        public CommonController(SmartDbContext db, IGenericAttributeService genericAttributeService, IRequestCache requestCache/*, IDbCache dbCache*/)
        {
            _db = db;
            _genericAttributeService = genericAttributeService;
            _requestCache = requestCache;
            //_dbCache = dbCache;
        }

        public async Task<IActionResult> LanguageSelected(int customerlanguage)
        {
            var language = await _db.Languages.FindByIdAsync(customerlanguage, false);
            if (language != null && language.Published)
            {
                Services.WorkContext.WorkingLanguage = language;
            }

            return Content(T("Admin.Common.DataEditSuccess"));
        }

        [Permission(Permissions.System.Maintenance.Execute)]
        [HttpPost]
        public IActionResult RestartApplication()
        {
            // TODO: (mh) (core) This must be tested in production environment. In VS _hostApplicationLifetime.StopApplication() just stops without restarting on next request.
            Services.WebHelper.RestartAppDomain();

            return new JsonResult(null);
        }

        [Permission(Permissions.System.Maintenance.Execute)]
        [HttpPost]
        public IActionResult ClearCache()
        {
            Services.Cache.Clear();

            _requestCache.RemoveByPattern("*");

            return new JsonResult
            (
                new
                {
                    Success = true,
                    Message = T("Admin.Common.TaskSuccessfullyProcessed").Value
                }
            );
        }

        [Permission(Permissions.System.Maintenance.Execute)]
        [HttpPost]
        public ActionResult ClearDatabaseCache()
        {
            // TODO: (mh) (core) Uncomment when dbCache can be resolved.
            //_dbCache.Clear();

            return new JsonResult
            (
                new
                {
                    Success = true,
                    Message = T("Admin.Common.TaskSuccessfullyProcessed").Value
                }
            );
        }



        #region Generic Attributes

        [HttpPost]
        public IActionResult GenericAttributesList(string entityName, int entityId)
        {
            var storeId = Services.StoreContext.CurrentStore.Id;
            ViewBag.StoreId = storeId;

            var attributesItems = new List<GenericAttributeModel>();

            if (entityName.HasValue() && entityId > 0)
            {
                var infos = GetGenericAttributesInfos(entityName);
                if (infos.ReadPermission.IsEmpty() || Services.Permissions.Authorize(infos.ReadPermission))
                {
                    var attributes = _genericAttributeService.GetAttributesForEntity(entityName, entityId).UnderlyingEntities;

                    attributesItems = attributes
                        .Where(x => x.StoreId == storeId || x.StoreId == 0)
                        .OrderBy(x => x.Key)
                        .Select(x => new GenericAttributeModel
                        {
                            Id = x.Id,
                            EntityId = x.EntityId,
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
                Total = attributesItems.Count
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

        public async Task<IActionResult> GenericAttributeUpdate(GenericAttributeModel model, string entityName, int entityId)
        {
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
        public async Task<IActionResult> GenericAttributeDelete(GenericAttributeModel model, GridSelection selection, string entityName)
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
                    .BatchDeleteAsync();
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

        #endregion
    }
}
