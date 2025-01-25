using Microsoft.AspNetCore.Http;
using Smartstore.Core.Data;
using Smartstore.Core.Web;

namespace Smartstore.Core.Identity
{
    internal class TrackActivityMiddleware
    {
        private readonly RequestDelegate _next;

        public TrackActivityMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext httpContext,
            SmartDbContext db,
            IWorkContext workContext,
            IWebHelper webHelper,
            IUserAgent userAgent,
            CustomerSettings customerSettings,
            PrivacySettings privacySettings)
        {
            var now = DateTime.UtcNow;
            var customer = workContext.CurrentCustomer;
            if (customer == null || customer.Deleted || customer.IsSystemAccount)
            {
                return;
            }

            var forceTrack = false;

            if (httpContext.Request.IsSubRequest())
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
            if (forceTrack || privacySettings.StoreLastIpAddress)
            {
                var currentIpAddress = webHelper.GetClientIpAddress().ToString();
                if (currentIpAddress.HasValue())
                {
                    dirty = dirty || customer.LastIpAddress != currentIpAddress;
                    customer.LastIpAddress = currentIpAddress;
                }
            }

            // Last user agent
            if (forceTrack || customerSettings.StoreLastUserAgent)
            {
                var currentUserAgent = userAgent.UserAgent;
                if (currentUserAgent.HasValue() && currentUserAgent.Length <= 255)
                {
                    dirty = dirty || customer.LastUserAgent != currentUserAgent;
                    customer.LastUserAgent = currentUserAgent;
                }
            }

            // Last device type
            if (forceTrack || customerSettings.StoreLastDeviceFamily)
            {
                var currentDeviceName = userAgent.Device.IsUnknown() ? userAgent.Platform.Name : userAgent.Device.Name;
                if (currentDeviceName.HasValue() && currentDeviceName.Length <= 255 && currentDeviceName != "Unknown")
                {
                    dirty = dirty || customer.LastUserDeviceType != currentDeviceName;
                    customer.LastUserDeviceType = currentDeviceName;
                }
            }

            // Last visited page
            if (forceTrack || customerSettings.StoreLastVisitedPage)
            {
                var currentUrl = webHelper.GetCurrentPageUrl(withQueryString: true);
                if (currentUrl.HasValue() && SanitizeUrl(ref currentUrl))
                {
                    dirty = dirty || customer.LastVisitedPage != currentUrl;
                    customer.LastVisitedPage = currentUrl;
                }
            }

            if (dirty)
            {
                db.TryUpdate(customer);
            }

            await _next(httpContext);
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
