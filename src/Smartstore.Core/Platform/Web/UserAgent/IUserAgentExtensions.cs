namespace Smartstore.Core.Web
{
    public static class IUserAgentExtensions
    {
        /// <summary>
        /// Checks if agent is a bot.
        /// </summary>
        public static bool IsBot(this IUserAgent2 userAgent) 
            => userAgent.Type == UserAgentType.Bot;

        /// <summary>
        /// Checks if agent is a browser.
        /// </summary>
        public static bool IsBrowser(this IUserAgent2 userAgent) 
            => userAgent.Type == UserAgentType.Browser;

        /// <summary>
        /// Checks if agent is a mobile device.
        /// </summary>
        public static bool IsMobileDevice(this IUserAgent2 userAgent) 
            => userAgent.Device.Type is >= UserAgentDeviceType.Wearable and <= UserAgentDeviceType.Tablet;

        /// <summary>
        /// Checks if agent is a mobile tablet device.
        /// </summary>
        public static bool IsTablet(this IUserAgent2 userAgent) 
            => userAgent.Device.Type is UserAgentDeviceType.Tablet;

        /// <summary>
        /// Checks if agent is the Smartstore application itself.
        /// </summary>
        public static bool IsApplication(this IUserAgent2 userAgent) 
            => userAgent.Type == UserAgentType.Application;

        /// <summary>
        /// Checks if agent is the application's PDF converter.
        /// </summary>
        public static bool IsPdfConverter(this IUserAgent2 userAgent) 
            => userAgent.UserAgent == "wkhtmltopdf";
    }
}
