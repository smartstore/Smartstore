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
using Smartstore.Core.Localization;
using Smartstore.Core.Security;
using Smartstore.Data;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;
using Smartstore.Web.Modelling.DataGrid;

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

        // AJAX.
        public ActionResult AllProductAttributes(string label, int selectedId)
        {
            var query = _db.ProductAttributes.OrderBy(x => x.DisplayOrder);
            var pager = new FastPager<ProductAttribute>(query, 500);
            var allAttributes = new List<dynamic>();

            while (pager.ReadNextPage(out var attributes))
            {
                foreach (var attribute in attributes)
                {
                    dynamic obj = new
                    {
                        attribute.Id,
                        attribute.DisplayOrder,
                        Name = attribute.GetLocalized(x => x.Name).Value
                    };

                    allAttributes.Add(obj);
                }
            }

            var data = allAttributes
                .OrderBy(x => x.DisplayOrder)
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
        public async Task<IActionResult> List(GridCommand command)
        {
            var data = await MapperFactory.MapListAsync<ProductAttribute, ProductAttributeModel>(_db.ProductAttributes);
            var model = new GridModel<ProductAttributeModel>(data) { Total = data.Count };

            return Json(model);
        }

        #endregion
    }
}
