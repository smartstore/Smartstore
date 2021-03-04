using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
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

        public async Task VirtualizeAsync(HttpContext httpContext)
        {
            var db = httpContext.RequestServices.GetRequiredService<SmartDbContext>();
            var customerService = httpContext.RequestServices.GetRequiredService<ICustomerService>();
            var workContext = httpContext.RequestServices.GetRequiredService<IWorkContext>();
            var taskParameters = httpContext.Request.Query;

            // Try virtualize current customer (which is necessary when user manually executes a task).
            Customer customer = null;
            if (taskParameters != null && taskParameters.ContainsKey(CurrentCustomerIdParamName))
            {
                customer = await db.Customers
                    .IncludeCustomerRoles()
                    .FindByIdAsync(taskParameters[CurrentCustomerIdParamName].Convert<int>());
            }

            if (customer == null)
            {
                // No virtualization: set background task system customer as current customer.
                customer = await customerService.GetCustomerBySystemNameAsync(SystemCustomerNames.BackgroundTask);
            }

            // Set virtual customer.
            workContext.CurrentCustomer = customer;

            // Try virtualize current store.
            if (taskParameters != null && taskParameters.ContainsKey(CurrentStoreIdParamName))
            {
                var storeContext = httpContext.RequestServices.GetRequiredService<IStoreContext>();

                var store = storeContext.GetStoreById(taskParameters[CurrentStoreIdParamName].Convert<int>());
                if (store != null)
                {
                    // Set virtual store.
                    storeContext.CurrentStore = store;
                }
            }
        }
    }
}