using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;
using Smartstore.Core.Widgets;
using Smartstore.Forums.Components;
using Smartstore.Forums.Models.Public;
using Smartstore.Forums.Services;

namespace Smartstore.Forums.Filters
{
    public class PmAccountDropdownFilter : IAsyncResultFilter
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly Lazy<IWidgetProvider> _widgetProvider;
        private readonly ForumSettings _forumSettings;

        public PmAccountDropdownFilter(
            SmartDbContext db,
            IWorkContext workContext,
            IStoreContext storeContext,
            Lazy<IWidgetProvider> widgetProvider,
            ForumSettings forumSettings)
        {
            _db = db;
            _workContext = workContext;
            _storeContext = storeContext;
            _widgetProvider = widgetProvider;
            _forumSettings = forumSettings;
        }

        public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            if (context.Result is StatusCodeResult || context.Result.IsHtmlViewResult())
            {
                if (_forumSettings.AllowPrivateMessages)
                {
                    var storeId = _storeContext.CurrentStore.Id;
                    var customer = _workContext.CurrentCustomer;

                    var model = new PmAccountDropdownModel
                    {
                        UnreadMessagesCount = await GetUnreadPrivateMessages(customer, storeId)
                    };

                    if (_forumSettings.ShowAlertForPM)
                    {
                        var ga = customer.GenericAttributes;
                        if (!ga.Get<bool>(ForumService.NotifiedAboutNewPrivateMessagesKey, storeId))
                        {
                            model.Alert = true;

                            ga.Set(ForumService.NotifiedAboutNewPrivateMessagesKey, true, storeId);
                            await _db.SaveChangesAsync();
                        }
                    }

                    var widget = new ComponentWidgetInvoker(typeof(PmAccountDropdownViewComponent), model);
                    
                    _widgetProvider.Value.RegisterWidget(new[] { "account_dropdown_after" }, widget);
                }
            }

            await next();
        }

        private async Task<int> GetUnreadPrivateMessages(Customer customer, int storeId)
        {
            if (_forumSettings.AllowPrivateMessages && !customer.IsGuest())
            {
                return await _db.PrivateMessages().CountAsync(x => 
                    x.ToCustomerId == customer.Id
                    && !x.IsRead
                    && !x.IsDeletedByRecipient
                    && x.StoreId == storeId);
            }

            return 0;
        }
    }
}
