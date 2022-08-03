using Amazon.Pay.API.Types;
using Microsoft.Extensions.Logging;

namespace Smartstore.AmazonPay
{
    internal static class ILoggerExtensions
    {
        public static void Log(this ILogger logger,
            AmazonPayResponse response,
            string message = null,
            LogLevel logLevel = LogLevel.Warning)
        {
            logger.Log(
                logLevel,
                new Exception(response.GetFullMessage()),
                message ?? response.GetShortMessage(),
                null);
        }
    }
}
