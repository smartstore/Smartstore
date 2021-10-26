using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Admin.Models.Orders;
using Smartstore.Core.Checkout.Orders.Reporting;
using Smartstore.Core.Data;
using Smartstore.Web.Components;

namespace Smartstore.Admin.Components
{
    public class DashboardBestsellersViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;

        public DashboardBestsellersViewComponent(SmartDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            const int pageSize = 7;

            var reportByQuantity = await _db.OrderItems
                .AsNoTracking()
                .SelectAsBestsellersReportLine(ReportSorting.ByQuantityDesc)
                .Take(pageSize)
                .ToListAsync();

            var reportByAmount = await _db.OrderItems
                .AsNoTracking()
                .SelectAsBestsellersReportLine(ReportSorting.ByAmountDesc)
                .Take(pageSize)
                .ToListAsync();

            var model = new DashboardBestsellersModel
            {
                BestsellersByQuantity = await reportByQuantity.MapAsync(_db),
                BestsellersByAmount = await reportByAmount.MapAsync(_db)
            };

            return View(model);
        }
    }
}
