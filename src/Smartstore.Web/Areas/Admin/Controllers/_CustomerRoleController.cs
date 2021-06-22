using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Brands;
using Smartstore.Core.Catalog.Categories;
using Smartstore.Core.Catalog.Products;
using Smartstore.Core.Content.Topics;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Data;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Controllers
{
    public class CustomerRoleController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        
        public CustomerRoleController(
            SmartDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        /// <summary>
        /// (AJAX) Gets a list of all available customer roles. 
        /// </summary>
        /// <param name="label">Text for optional entry. If not null an entry with the specified label text and the Id 0 will be added to the list.</param>
        /// <param name="selectedIds">Ids of selected entities.</param>
        /// <param name="includeSystemRoles">Specifies whether to include system roles.</param>
        /// <returns>List of all customer roles as JSON.</returns>
        public async Task<IActionResult> AllCustomerRoles(string label, string selectedIds, bool? includeSystemRoles)
        {
            var rolesQuery = _db.CustomerRoles
                .AsNoTracking()
                .ApplyStandardFilter(true).AsQueryable();
            
            if (!(includeSystemRoles ?? true))
            {
                rolesQuery = rolesQuery.Where(x => x.IsSystemRole);
            }

            var rolesPager = new FastPager<CustomerRole>(rolesQuery, 500);
            var customerRoles = new List<CustomerRole>();
            var ids = selectedIds.ToIntArray();

            while ((await rolesPager.ReadNextPageAsync<CustomerRole>()).Out(out var roles))
            {
                customerRoles.AddRange(roles);
            }

            var list = customerRoles
                .OrderBy(x => x.Name)
                .Select(x => new ChoiceListItem
                {
                    Id = x.Id.ToString(),
                    Text = x.Name,
                    Selected = ids.Contains(x.Id)
                })
                .ToList();

            if (label.HasValue())
            {
                list.Insert(0, new ChoiceListItem
                {
                    Id = "0",
                    Text = label,
                    Selected = false
                });
            }

            return new JsonResult(list);
        }
    }
}
