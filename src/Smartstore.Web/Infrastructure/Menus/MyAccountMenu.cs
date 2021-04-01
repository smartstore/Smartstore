using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Core;
using Smartstore.Core.Checkout.Orders;
using Smartstore.Core.Content.Menus;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Core.Localization;
using Smartstore.Engine;

namespace Smartstore.Web.Infrastructure
{
    public partial class MyAccountMenu : IMenu
    {
        // TODO: (mc) (core) Injecting IUrlHelper here reproduces the error I get occasinally when running VS for a long time.
        private readonly SmartDbContext _db;
        private readonly ICommonServices _services;
        //private readonly IUrlHelper _urlHelper;
        private readonly CustomerSettings _customerSettings;
        private readonly OrderSettings _orderSettings;
        private readonly RewardPointsSettings _rewardPointsSettings;

        private TreeNode<MenuItem> _currentNode;
        private bool _currentNodeResolved;

        public MyAccountMenu(
            SmartDbContext db,
            ICommonServices services,
            //IUrlHelper urlHelper,
            CustomerSettings customerSettings,
            OrderSettings orderSettings,
            RewardPointsSettings rewardPointsSettings)
        {
            _db = db;
            _services = services;
            //_urlHelper = urlHelper;
            _customerSettings = customerSettings;
            _orderSettings = orderSettings;
            _rewardPointsSettings = rewardPointsSettings;
            
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public string Name => "MyAccount";

        public bool ApplyPermissions => true;

        public virtual async Task<TreeNode<MenuItem>> GetRootNodeAsync()
        {
            var root = await BuildAsync();

            await _services.EventPublisher.PublishAsync(new MenuBuiltEvent(Name, root));

            return root;
        }

        public virtual Task ResolveElementCountAsync(TreeNode<MenuItem> curNode, bool deep = false)
        {
            return Task.CompletedTask;
        }

        public virtual async Task<TreeNode<MenuItem>> ResolveCurrentNodeAsync(ActionContext actionContext)
        {
            if (!_currentNodeResolved)
            {
                _currentNode = (await GetRootNodeAsync()).SelectNode(x => x.Value.IsCurrent(actionContext), true);
                _currentNodeResolved = true;
            }

            return _currentNode;
        }

        public IDictionary<string, TreeNode<MenuItem>> GetAllCachedMenus()
        {
            // No caching.
            return new Dictionary<string, TreeNode<MenuItem>>();
        }

        public Task ClearCacheAsync()
        {
            // No caching.
            return Task.CompletedTask;
        }

        protected virtual async Task<TreeNode<MenuItem>> BuildAsync()
        {
            var store = _services.StoreContext.CurrentStore;
            var customer = _services.WorkContext.CurrentCustomer;

            // TODO: (mh) (core) Temporary only. Inject if possible.
            var _urlHelper = EngineContext.Current.ResolveService<IUrlHelper>();

            var root = new TreeNode<MenuItem>(new MenuItem { Text = T("Account.Navigation") })
            {
                Id = Name
            };

            root.Append(new MenuItem
            {
                Id = "info",
                Text = T("Account.CustomerInfo"),
                Icon = "fal fa-user",
                Url = _urlHelper.Action("Info", "Customer", new { area = "" })
            });

            root.Append(new MenuItem
            {
                Id = "addresses",
                Text = T("Account.CustomerAddresses"),
                Icon = "fal fa-address-book",
                Url = _urlHelper.Action("Addresses", "Customer", new { area = "" })
            });

            root.Append(new MenuItem
            {
                Id = "orders",
                Text = T("Account.CustomerOrders"),
                Icon = "fal fa-file-invoice",
                Url = _urlHelper.Action("Orders", "Customer", new { area = "" })
            });

            if (_orderSettings.ReturnRequestsEnabled)
            {
                var hasReturnRequests = await _db.ReturnRequests.ApplyStandardFilter(customerId: customer.Id).AnyAsync();
                if (hasReturnRequests)
                {
                    root.Append(new MenuItem
                    {
                        Id = "returnrequests",
                        Text = T("Account.CustomerReturnRequests"),
                        Icon = "fal fa-truck",
                        Url = _urlHelper.Action("ReturnRequests", "Customer", new { area = "" })
                    });
                }
            }

            if (!_customerSettings.HideDownloadableProductsTab)
            {
                root.Append(new MenuItem
                {
                    Id = "downloads",
                    Text = T("Account.DownloadableProducts"),
                    Icon = "fal fa-download",
                    Url = _urlHelper.Action("DownloadableProducts", "Customer", new { area = "" })
                });
            }

            if (!_customerSettings.HideBackInStockSubscriptionsTab)
            {
                root.Append(new MenuItem
                {
                    Id = "backinstock",
                    Text = T("Account.BackInStockSubscriptions"),
                    Icon = "fal fa-truck-loading",
                    Url = _urlHelper.Action("BackInStockSubscriptions", "Customer", new { area = "" })
                });
            }

            if (_rewardPointsSettings.Enabled)
            {
                root.Append(new MenuItem
                {
                    Id = "rewardpoints",
                    Text = T("Account.RewardPoints"),
                    Icon = "fal fa-certificate",
                    Url = _urlHelper.Action("RewardPoints", "Customer", new { area = "" })
                });
            }

            root.Append(new MenuItem
            {
                Id = "changepassword",
                Text = T("Account.ChangePassword"),
                Icon = "fal fa-unlock-alt",
                Url = _urlHelper.Action("ChangePassword", "Customer", new { area = "" })
            });

            if (_customerSettings.AllowCustomersToUploadAvatars)
            {
                root.Append(new MenuItem
                {
                    Id = "avatar",
                    Text = T("Account.Avatar"),
                    Icon = "fal fa-user-circle",
                    Url = _urlHelper.Action("Avatar", "Customer", new { area = "" })
                });
            }

            // TODO: (mh) (core) Append items from Forum module.
            //if (_forumSettings.ForumsEnabled && _forumSettings.AllowCustomersToManageSubscriptions)
            //{
            //    root.Append(new MenuItem
            //    {
            //        Id = "forumsubscriptions",
            //        Text = T("Account.ForumSubscriptions"),
            //        Icon = "fal fa-bell",
            //        Url = _urlHelper.Action("ForumSubscriptions", "Customer", new { area = "" })
            //    });
            //}

            //if (_forumSettings.AllowPrivateMessages)
            //{
            //    var numUnreadMessages = 0;

            //    if (_forumSettings.AllowPrivateMessages && !customer.IsGuest())
            //    {
            //        var privateMessages = _forumService.Value.GetAllPrivateMessages(store.Id, 0, customer.Id, false, null, false, 0, 1);
            //        numUnreadMessages = privateMessages.TotalCount;
            //    }

            //    root.Append(new MenuItem
            //    {
            //        Id = "privatemessages",
            //        Text = T("PrivateMessages.Inbox"),
            //        Icon = "fal fa-envelope",
            //        Url = _urlHelper.RouteUrl("PrivateMessages", new { tab = "inbox" }),
            //        BadgeText = numUnreadMessages > 0 ? numUnreadMessages.ToString() : null,
            //        BadgeStyle = BadgeStyle.Warning
            //    });
            //}

            return root;
        }
    }
}
