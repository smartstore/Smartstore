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
                CustomerSince = Services.DateTimeHelper.ConvertToUserTime(customer.CreatedOnUtc, DateTimeKind.Utc),
                RewardPoints = customer.GetRewardPointsBalance()
            };

            return View(model);
        }
    }
}