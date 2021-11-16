using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Forums.Models.Public;
using Smartstore.Web.Components;

namespace Smartstore.Forums.Components
{
    // TODO: (mh) (core) "View(customerId)" throws InvalidOperationException: The model item passed into the ViewDataDictionary is of type 'System.Int32',
    // but this ViewDataDictionary instance requires a model item of type 'Smartstore.Forums.Models.Public.SendPrivateMessageModel'.
    // T("Common.MethodNotSupportedForGuests") notification not necessary here (just renders something or nothing).

    /// <summary>
    /// Button to send a private message injected into the customer's profile page.
    /// </summary>
    public class AdminPmButtonViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;

        public AdminPmButtonViewComponent(SmartDbContext db)
        {
            _db = db;
        }

        public async Task<IViewComponentResult> InvokeAsync(int customerId)
        {
            if (customerId == 0)
            {
                return Empty();
            }

            var customer = await _db.Customers
                .IncludeCustomerRoles()
                .FindByIdAsync(customerId, false);

            if (customer != null)
            {
                if (!customer.IsGuest())
                {
                    var model = new SendPrivateMessageModel
                    {
                        ToCustomerId = customerId,
                        CustomerToName = customer.FormatUserName()
                    };

                    return View(model);
                }
                else
                {
                    NotifyError(T("Common.MethodNotSupportedForGuests"));
                }
            }
            else
            {
                return Empty();
            }

            return View(customerId);
        }
    }
}
