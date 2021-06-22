using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Web.Controllers;

namespace Smartstore.Admin.Controllers
{
    public class ManufacturerController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly CatalogSettings _catalogSettings;

        public ManufacturerController(
            SmartDbContext db,
            IProductService productService,
            ICategoryService categoryService,
            CatalogSettings catalogSettings)
        {
            _db = db;
            _productService = productService;
            _categoryService = categoryService;
            _catalogSettings = catalogSettings;
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        /// <summary>
        /// (AJAX) Gets a list of all available manufacturers. 
        /// </summary>
        /// <param name="label">Text for optional entry. If not null an entry with the specified label text and the Id 0 will be added to the list.</param>
        /// <param name="selectedId">Id of selected entity.</param>
        /// <returns>List of all manufacturers as JSON.</returns>
        public async Task<IActionResult> AllManufacturers(string label, int selectedId)
        {
            var manufacturers = await _db.Manufacturers
                .AsNoTracking()
                .ApplyStandardFilter(true)
                .ToListAsync();
            
            if (label.HasValue())
            {
                manufacturers.Insert(0, new Manufacturer { Name = label, Id = 0 });
            }

            var list = from m in manufacturers
                select new
                {
                    id = m.Id.ToString(),
                    text = m.GetLocalized(x => x.Name).Value,
                    selected = m.Id == selectedId
                };

            var mainList = list.ToList();

            var mruList = new TrimmedBuffer<string>(
                Services.WorkContext.CurrentCustomer.GenericAttributes.MostRecentlyUsedManufacturers,
                _catalogSettings.MostRecentlyUsedManufacturersMaxSize)
                .Reverse()
                .Select(x =>
                {
                    var item = manufacturers.FirstOrDefault(m => m.Id.ToString() == x);
                    if (item != null)
                    {
                        return new
                        {
                            id = x,
                            text = item.GetLocalized(y => y.Name).Value,
                            selected = false
                        };
                    }

                    return null;
                })
                .Where(x => x != null)
                .ToList();

            object data = mainList;
            if (mruList.Count > 0)
            {
                data = new List<object>
                {
                    new Dictionary<string, object> { ["text"] = T("Common.Mru").Value, ["children"] = mruList },
                    new Dictionary<string, object> { ["text"] = T("Admin.Catalog.Manufacturers").Value, ["children"] = mainList, ["main"] = true }
                };
            }

            return new JsonResult(data);
        }


    }
}
