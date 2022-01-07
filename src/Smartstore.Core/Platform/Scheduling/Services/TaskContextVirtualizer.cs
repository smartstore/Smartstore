using Microsoft.AspNetCore.Http;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;

namespace Smartstore.Scheduling
{
    public class TaskContextVirtualizer : ITaskContextVirtualizer
    {
        public const string CurrentCustomerIdParamName = "CurrentCustomerId";
        public const string CurrentStoreIdParamName = "CurrentStoreId";

        public async Task VirtualizeAsync(HttpContext httpContext, IDictionary<string, string> taskParameters = null)
        {
            var db = httpContext.RequestServices.GetRequiredService<SmartDbContext>();
            var customerService = httpContext.RequestServices.GetRequiredService<ICustomerService>();
            var workContext = httpContext.RequestServices.GetRequiredService<IWorkContext>();

            // Try virtualize current customer (which is necessary when user manually executes a task).
            Customer customer = null;
            if (taskParameters != null && taskParameters.ContainsKey(CurrentCustomerIdParamName))
            {
                customer = await db.Customers
                    .IncludeCustomerRoles()
                    .FindByIdAsync(taskParameters[CurrentCustomerIdParamName].Convert<int>());
            }

            if (customer == null && !workContext.CurrentCustomer.IsBackgroundTaskAccount())
            {
                // No virtualization: set background task system customer as current customer.
                customer = await customerService.GetCustomerBySystemNameAsync(SystemCustomerNames.BackgroundTask);
            }

            // Set virtual customer.
            if (customer != null)
            {
                workContext.CurrentCustomer = customer;
            }

            // Try virtualize current store.
            var storeContext = httpContext.RequestServices.GetRequiredService<IStoreContext>();

            if (!storeContext.IsSingleStoreMode())
            {
                Store store = null;
                if (taskParameters != null && taskParameters.ContainsKey(CurrentStoreIdParamName))
                {
                    store = storeContext.GetStoreById(taskParameters[CurrentStoreIdParamName].Convert<int>());
                }

                if (store == null)
                {
                    // No store virtualization requested: always set primary store in this case.
                    store = storeContext.GetCachedStores().GetPrimaryStore();
                }

                if (store != null)
                {
                    // Set virtual store.
                    storeContext.CurrentStore = store;
                }
            }
        }
    }
}