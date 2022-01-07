using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Data;

namespace Smartstore.Core.Checkout.Affiliates
{
    public sealed class CheckAffiliateAttribute : TypeFilterAttribute
    {
        public CheckAffiliateAttribute()
            : base(typeof(CheckAffiliateFilter))
        {
        }

        class CheckAffiliateFilter : IAsyncActionFilter
        {
            private readonly SmartDbContext _db;
            private readonly IWorkContext _workContext;

            public CheckAffiliateFilter(SmartDbContext db, IWorkContext workContext)
            {
                _db = db;
                _workContext = workContext;
            }

            public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
            {
                var customer = _workContext.CurrentCustomer;
                var query = context.HttpContext.Request.Query["affiliateId"].ToString();

                if (int.TryParse(query, out int affiliateId) && affiliateId > 0 && !customer.IsSystemAccount && customer.AffiliateId != affiliateId)
                {
                    var isValidAffiliate = await _db.Affiliates
                        .Where(x => x.Id == affiliateId && !x.Deleted && x.Active)
                        .AnyAsync();

                    if (isValidAffiliate)
                    {
                        customer.AffiliateId = affiliateId;
                    }
                }

                await next();
            }
        }
    }
}
