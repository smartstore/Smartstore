using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Forums.Models.Public;
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
            var postCountAttribute = await _db.GenericAttributes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.EntityId == customerId && x.KeyGroup == nameof(Customer) && x.Key == ForumService.ForumPostCountKey);

            var postCount = postCountAttribute?.Value?.ToInt() ?? 0;
            if (postCount == 0)
            {
                return Empty();
            }

            var model = new ForumCustomerStatsModel
            {
                TotalPosts = postCount
            };

            return View(model);
        }
    }
}
