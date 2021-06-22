using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Controllers
{
    public class StoreController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        
        public StoreController(SmartDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        /// <summary>
        /// (AJAX) Gets a list of all available stores. 
        /// </summary>
        /// <param name="label">Text for optional entry. If not null an entry with the specified label text and the Id 0 will be added to the list.</param>
        /// <param name="selectedIds">Ids of selected entities.</param>
        /// <returns>List of all stores as JSON.</returns>
        public IActionResult AllStores(string label, string selectedIds)
        {
            var stores = new List<Store>(Services.StoreContext.GetAllStores());
            var ids = selectedIds.ToIntArray();

            if (label.HasValue())
            {
                stores.Insert(0, new Store { Name = label, Id = 0 });
            }

            var list = from m in stores
                select new ChoiceListItem
                {
                    Id = m.Id.ToString(),
                    Text = m.Name,
                    Selected = ids.Contains(m.Id)
                };

            return new JsonResult(list.ToList());
        }
    }
}
