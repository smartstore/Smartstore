using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Stores;
using Smartstore.Forums.Models.Public;
using Smartstore.Web.Components;

namespace Smartstore.Forums.Components
{
    public class PmAccountDropdownViewComponent : SmartViewComponent
    {
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IStoreContext _storeContext;
        private readonly ForumSettings _forumSettings;

        public PmAccountDropdownViewComponent(
            SmartDbContext db,
            IWorkContext workContext,
            IStoreContext storeContext,
            ForumSettings forumSettings)
        {
            _db = db;
            _workContext = workContext;
            _storeContext = storeContext;
            _forumSettings = forumSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
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

            return View(model);
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
