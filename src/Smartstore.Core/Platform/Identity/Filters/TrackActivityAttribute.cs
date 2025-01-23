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
            Arguments = [this];
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

        /// <summary>
        /// Whether to save current customer's device family name in <see cref="Customer.LastUserDeviceType"/>. Default is <c>true</c>.
        /// </summary>
        public bool TrackDeviceFamily { get; set; } = true;
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
            var now = DateTime.UtcNow;
            var customer = _workContext.CurrentCustomer;
            if (customer == null || customer.Deleted || customer.IsSystemAccount)
            {
                return;
            }

            var forceTrack = false;

            if (context.HttpContext.Request.IsSubRequest())
            {
                var isNewGuest = (now - customer.CreatedOnUtc) < TimeSpan.FromSeconds(2) && customer.IsGuest();
                if (!isNewGuest)
                {
                    // Only get out in sub requests if the customer is NOT a new guest. We WANT to track new guests to check for abuse.
                    return;
                }
                else
                {
                    forceTrack = true;
                }
            }

            var dirty = false;

            // Last activity date
            if (forceTrack || customer.LastActivityDateUtc.AddMinutes(1.0) < now)
            {
                customer.LastActivityDateUtc = now;
                dirty = true;
            }

            // Last IP address
            if (_attribute.TrackIpAddress && (forceTrack || _privacySettings.StoreLastIpAddress))
            {
                var currentIpAddress = _webHelper.GetClientIpAddress().ToString();
                if (currentIpAddress.HasValue())
                {
                    dirty = dirty || customer.LastIpAddress != currentIpAddress;
                    customer.LastIpAddress = currentIpAddress;
                }
            }

            // Last user agent
            if (_attribute.TrackUserAgent && (forceTrack || _customerSettings.StoreLastUserAgent))
            {
                var currentUserAgent = _userAgent.UserAgent;
                if (currentUserAgent.HasValue() && currentUserAgent.Length <= 255)
                {
                    dirty = dirty || customer.LastUserAgent != currentUserAgent;
                    customer.LastUserAgent = currentUserAgent;
                }
            }

            // Last device type
            if (_attribute.TrackDeviceFamily && (forceTrack || _customerSettings.StoreLastDeviceFamily))
            {
                var currentDeviceName = _userAgent.Device.IsUnknown() ? _userAgent.Platform.Name : _userAgent.Device.Name;
                if (currentDeviceName.HasValue() && currentDeviceName.Length <= 255 && currentDeviceName != "Unknown")
                {
                    dirty = dirty || customer.LastUserDeviceType != currentDeviceName;
                    customer.LastUserDeviceType = currentDeviceName;
                }
            }

            // Last visited page
            if (_attribute.TrackPage && (forceTrack || _customerSettings.StoreLastVisitedPage))
            {
                var currentUrl = _webHelper.GetCurrentPageUrl(withQueryString: true);
                if (currentUrl.HasValue() && SanitizeUrl(ref currentUrl))
                {
                    dirty = dirty || customer.LastVisitedPage != currentUrl;
                    customer.LastVisitedPage = currentUrl;
                }
            }

            if (dirty)
            {
                _db.TryUpdate(customer);
            }
        }

        internal static bool SanitizeUrl(ref string url)
        {
            var len = url.Length;
            
            if (len <= 512)
            {
                return true;
            }

            if (len <= 2048)
            {
                var qindex = url.IndexOf('?');
                if (qindex > -1)
                {
                    url = url[..qindex];
                    return true;
                }
                else
                {
                    // "Too long" for a url without query string. Ignore.
                    return false;
                }
            }

            return false;
        }
    }
}
