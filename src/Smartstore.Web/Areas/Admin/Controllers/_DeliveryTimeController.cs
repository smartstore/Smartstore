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
    //[AdminAuthorize]
    public class DeliveryTimeController : AdminControllerBase
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
        /// TODO: (mh) (core) Add documentation.
        /// </summary>
        public async Task<IActionResult> AllDeliveryTimesAsync(string selectedIds)
        {
            var deliveryTimes = await _db.DeliveryTimes
                .AsNoTracking()
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
