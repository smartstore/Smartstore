namespace Smartstore.Core.Web
{
    public static class IUserAgentExtensions
    {
        /// <summary>
        /// Checks whether agent is a bot.
        /// </summary>
        public static bool IsBot(this IUserAgent userAgent) 
            => userAgent.Type == UserAgentType.Bot;

        /// <summary>
        /// Checks whether agent is a browser.
        /// </summary>
        public static bool IsBrowser(this IUserAgent userAgent) 
            => userAgent.Type == UserAgentType.Browser;

        /// <summary>
        /// Checks whether agent is a mobile device.
        /// </summary>
        public static bool IsMobileDevice(this IUserAgent userAgent) 
            => userAgent.Device.Type is >= UserAgentDeviceType.Wearable and <= UserAgentDeviceType.Tablet;

        /// <summary>
        /// Checks whether agent is a mobile tablet device.
        /// </summary>
        public static bool IsTablet(this IUserAgent userAgent) 
            => userAgent.Device.Type is UserAgentDeviceType.Tablet;

        /// <summary>
        /// Checks whether agent is the Smartstore application itself.
        /// </summary>
        public static bool IsApplication(this IUserAgent userAgent) 
            => userAgent.Type == UserAgentType.Application;

        /// <summary>
        /// Checks whether agent is the application's PDF converter.
        /// </summary>
        public static bool IsPdfConverter(this IUserAgent userAgent) 
            => userAgent.UserAgent == "wkhtmltopdf";

        /// <summary>
        /// Checks whether the user agent supports the WebP image type.
        /// </summary>
        public static bool SupportsWebP(this IUserAgent userAgent)
        {
            if (userAgent.Version == null)
            {
                return false;
            }
            else
            {
                var name = userAgent.Name;
                var v = userAgent.Version;
                var m = userAgent.IsMobileDevice();

                if (name == "Chrome")
                {
                    return v.Major >= (m ? 79 : 32);
                }
                else if (name == "Firefox")
                {
                    return v.Major >= (m ? 68 : 65);
                }
                else if (name == "Edge")
                {
                    return v.Major >= 18;
                }
                else if (name == "Opera")
                {
                    return m || v.Major >= 19;
                }
                else if (name == "Safari")
                {
                    return v.Major >= (m ? 14 : 16);
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
