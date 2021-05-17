using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Collections;
using Smartstore.Core.Catalog;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Data;
using Smartstore.Web.Controllers;

namespace Smartstore.Admin.Controllers
{
    public class CategoryController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly CatalogSettings _catalogSettings;

        public CategoryController(
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
            // TODO: (mh) (core) How to do this?
            string customerChoice = null;
            //var customerChoice = _genericAttributeService.Value.GetAttribute<string>("Customer", _workContext.CurrentCustomer.Id, "AdminCategoriesType");

            if (customerChoice != null && customerChoice.Equals("Tree"))
            {
                return RedirectToAction("Tree");
            }

            return RedirectToAction("List");
        }

        /// <summary>
        /// Gets a list of all available categories. 
        /// </summary>
        /// <param name="label">Text for optional entry. If not null an entry with the specified label text and the Id 0 will be added to the list.</param>
        /// <param name="selectedIds">Ids of selected entities.</param>
        /// <returns>List of all categories as JSON.</returns>
        public async Task<IActionResult> AllCategories(string label, string selectedIds)
        {
            var categoryTree = await _categoryService.GetCategoryTreeAsync(includeHidden: true);
            var categories = categoryTree.Flatten(false);
            var selectedArr = selectedIds.ToIntArray();

            if (label.HasValue())
            {
                categories = (new[] { new Category { Name = label, Id = 0 } }).Concat(categories);
            }

            var query = categories.SelectAsync(async c => new
            {
                id = c.Id.ToString(),
                text = await _categoryService.GetCategoryPathAsync(c, aliasPattern: "<span class='badge badge-secondary'>{0}</span>"),
                selected = selectedArr.Contains(c.Id)
            });

            // INFO: Call AsyncToList() to avoid upcoming conflicts with EF.
            var mainList = await query.AsyncToList();

            var mruList = new TrimmedBuffer<string>(
                Services.WorkContext.CurrentCustomer.GenericAttributes.MostRecentlyUsedCategories,
                _catalogSettings.MostRecentlyUsedCategoriesMaxSize)
                .Reverse()
                .Select(x =>
                {
                    var item = categoryTree.SelectNodeById(x.ToInt());
                    if (item != null)
                    {
                        return new
                        {
                            id = x,
                            text = _categoryService.GetCategoryPath(item, aliasPattern: "<span class='badge badge-secondary'>{0}</span>"),
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
                    new Dictionary<string, object> { ["text"] = T("Admin.Catalog.Categories").Value, ["children"] = mainList, ["main"] = true }
                };
            }

            return new JsonResult(data);
        }
    }
}
