using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Data;
using Smartstore.Forums.Models.Public;
using Smartstore.Forums.Services;
using Smartstore.Web.Components;

namespace Smartstore.Forums.Components
{
    /// <summary>
    /// Forum statistics of a customer injected into the customer's profile page.
    /// </summary>
    public class ForumCustomerStatsViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;

        public ForumCustomerStatsViewComponent(SmartDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync(int customerId)
        {
            var customer = await _db.Customers.FindByIdAsync(customerId, false);
            if (customer == null)
            {
                return Empty();
            }

            var model = new ForumCustomerStatsModel
            {
                // TODO: (mg) (core) (perf) Why loading Customer from DB? Just load ForumPostCount GenericAttribute.
                TotalPosts = customer.GenericAttributes.Get<int>(ForumService.ForumPostCountKey)
            };

            return View(model);
        }
    }
}
