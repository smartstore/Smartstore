using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Dasync.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Common.Services;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;
using Smartstore.Web.Controllers;
using Smartstore.Web.Modelling;

namespace Smartstore.Admin.Controllers
{
    public class DeliveryTimeController : AdminController
    {
        private readonly SmartDbContext _db;
        private readonly IDeliveryTimeService _deliveryTimeService;
        
        public DeliveryTimeController(
            SmartDbContext db,
            IDeliveryTimeService deliveryTimeService)
        {
            _db = db;
            _deliveryTimeService = deliveryTimeService;
        }

        public IActionResult Index()
        {
            return RedirectToAction("List");
        }

        /// <summary>
        /// (AJAX) Gets a list of all available delivery times. 
        /// </summary>
        /// <param name="selectedIds">Ids of selected entities.</param>
        /// <returns>List of all delivery times as JSON.</returns>
        public async Task<IActionResult> AllDeliveryTimes(string selectedIds)
        {
            var deliveryTimes = await _db.DeliveryTimes
                .AsNoTracking()
                .OrderBy(x => x.DisplayOrder)
                .ToListAsync();

            var selectedArr = selectedIds.ToIntArray();

            var data = deliveryTimes
                .Select(x => new ChoiceListItem
                {
                    Id = x.Id.ToString(),
                    Text = x.GetLocalized(y => y.Name).Value,
                    Description = _deliveryTimeService.GetFormattedDeliveryDate(x),
                    Selected = selectedArr.Contains(x.Id)
                })
                .ToList();

            return new JsonResult(data);
        }
    }
}
