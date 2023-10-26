using Smartstore.Web.Models.Common;
using Smartstore.Web.Models.Customers;

namespace Smartstore.Web.Components
{
    public class MyAccountHeaderViewComponent : SmartViewComponent
    {
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var customer = Services.WorkContext.CurrentCustomer;
            var customerName = customer.GetFullName();

            var model = new MyAccountHeaderModel
            {
                CustomerEmail = customer.Email,
                CustomerName = customerName,
                Avatar = await customer.MapAsync(customerName, true, true),
                CustomerSince = customer.CreatedOnUtc.ToString("MMM yyyy"),
                RewardPoints = customer.GetRewardPointsBalance()
            };

            return View(model);
        }
    }
}