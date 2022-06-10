using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Smartstore.Core.Data;
using Smartstore.Core.Web;

namespace Smartstore.Core.Identity
{
    /// <summary>
    /// Saves current user activity information like date, IP address and visited page to database.
    /// </summary>
    public sealed class TrackActivityAttribute : TypeFilterAttribute
    {
        public TrackActivityAttribute()
            : base(typeof(TrackActivityFilter))
        {
            Arguments = new object[] { this };
        }

        /// <summary>
        /// Whether to save the current UTC date in <see cref="Customer.LastActivityDateUtc"/>. Default is <c>true</c>.
        /// </summary>
        public bool TrackDate { get; set; } = true;

        /// <summary>
        /// Whether to save the current customer's IP address in <see cref="Customer.LastIpAddress"/>. Default is <c>true</c>.
        /// </summary>
        public bool TrackIpAddress { get; set; } = true;

        /// <summary>
        /// Whether to save current visited page's URL in <see cref="Customer.GenericAttributes"/>. Default is <c>true</c>.
        /// </summary>
        public bool TrackPage { get; set; } = true;

        /// <summary>
        /// Whether to save current customer's user agent string in <see cref="Customer.LastUserAgent"/>. Default is <c>true</c>.
        /// </summary>
        public bool TrackUserAgent { get; set; } = true;
    }

    internal class TrackActivityFilter : IAsyncActionFilter
    {
        private readonly TrackActivityAttribute _attribute;
        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IWebHelper _webHelper;
        private readonly IUserAgent _userAgent;
        private readonly CustomerSettings _customerSettings;
        private readonly PrivacySettings _privacySettings;

        public TrackActivityFilter(
            TrackActivityAttribute attribute, 
            SmartDbContext db, 
            IWorkContext workContext,
            IWebHelper webHelper,
            IUserAgent userAgent,
            CustomerSettings customerSettings,
            PrivacySettings privacySettings)
        {
            _attribute = attribute;
            _db = db;
            _workContext = workContext;
            _webHelper = webHelper;
            _userAgent = userAgent;
            _customerSettings = customerSettings;
            _privacySettings = privacySettings;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            DoTrack(context);

            if (context.Result == null)
            {
                await next();
            }
        }

        private void DoTrack(ActionExecutingContext context)
        {
            if (!HttpMethods.IsGet(context.HttpContext.Request.Method))
                return;

            var customer = _workContext.CurrentCustomer;
            if (customer == null || customer.Deleted || customer.IsSystemAccount)
                return;

            bool dirty = false;

            // Last activity date
            if (_attribute.TrackDate && customer.LastActivityDateUtc.AddMinutes(1.0) < DateTime.UtcNow)
            {
                customer.LastActivityDateUtc = DateTime.UtcNow;
                dirty = true;
            }

            // Last IP address
            if (_attribute.TrackIpAddress && _privacySettings.StoreLastIpAddress)
            {
                var currentIpAddress = _webHelper.GetClientIpAddress().ToString();
                if (currentIpAddress.HasValue())
                {
                    customer.LastIpAddress = currentIpAddress;
                    dirty = true;
                }
            }

            // Last visited page
            if (_attribute.TrackPage && _customerSettings.StoreLastVisitedPage)
            {
                var currentUrl = _webHelper.GetCurrentPageUrl(true);
                if (currentUrl.HasValue())
                {
                    customer.GenericAttributes.LastVisitedPage = currentUrl;
                    dirty = true;
                }
            }

            // Last user agent
            if (_attribute.TrackUserAgent && _customerSettings.StoreLastVisitedPage)
            {
                // TODO: (mh) (core) Make new setting CustomerSettings.StoreLastUserAgent
                var currentUserAgent = _userAgent.RawValue;
                if (currentUserAgent.HasValue())
                {
                    customer.LastUserAgent = currentUserAgent;
                    dirty = true;
                }
            }

            if (dirty)
            {
                _db.TryUpdate(customer);
            }
        }
    }
}
