using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Catalog.Discounts;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Controllers
{
    //[AdminAuthorize]
    public class DiscountController : AdminControllerBase
    {
        private readonly SmartDbContext _db;
        
        public DiscountController(SmartDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        /// <summary>
        /// TODO: (mh) (core) Add documentation.
        /// </summary>
        /// 
        public async Task<IActionResult> AllDiscountsAsync(string label, string selectedIds, DiscountType? type)
        {
            var discounts = await _db.Discounts
                .AsNoTracking()
                .Where(x => x.DiscountTypeId == (int)type)
                .ToListAsync();
                
            var selectedArr = selectedIds.ToIntArray();

            if (label.HasValue())
            {
                discounts.Insert(0, new Discount { Name = label, Id = 0 });
            }

            var data = discounts
                .Select(x => new ChoiceListItem
                {
                    Id = x.Id.ToString(),
                    Text = x.Name,
                    Selected = selectedArr.Contains(x.Id)
                })
                .ToList();

            return new JsonResult(data);
        }
    }
}
