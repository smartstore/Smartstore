using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Admin.Models.Catalog;
using Smartstore.ComponentModel;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Common.Settings;
using Smartstore.Core.Data;
using Smartstore.Core.Security;
using Smartstore.Web.Controllers;

namespace Smartstore.Admin.Controllers
{
    public class ProductAttributeController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly AdminAreaSettings _adminAreaSettings;

        public ProductAttributeController(SmartDbContext db, AdminAreaSettings adminAreaSettings)
        {
            _db = db;
            _adminAreaSettings = adminAreaSettings;
        }

        #region Product attribute

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        [Permission(Permissions.Catalog.Variant.Read)]
        public IActionResult List()
        {
            ViewData["GridPageSize"] = _adminAreaSettings.GridPageSize;
            return View();
        }

        [ActionName("List")]
        [HttpPost, IgnoreAntiforgeryToken] // TODO: (core) Why is posted _RequestVerificationToken not valid?
        [Permission(Permissions.Catalog.Variant.Read)]
        public async Task<IActionResult> List_Ajax()
        {
            //var attributes = await _db.ProductAttributes.ToListAsync();
            //var mapper = MapperFactory.GetMapper<ProductAttribute, ProductAttributeModel>();
            var data = await MapperFactory.MapListAsync<ProductAttribute, ProductAttributeModel>(_db.ProductAttributes);

            return Json(data);
        }

        #endregion
    }
}
