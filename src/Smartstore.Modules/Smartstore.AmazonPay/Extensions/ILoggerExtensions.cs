using AmazonPay;
using AmazonPay.Responses;
using Microsoft.Extensions.Logging;

namespace Smartstore.AmazonPay
{
    internal static class ILoggerExtensions
    {
        public static string LogAmazonResponse<T>(this ILogger logger,
            DelegateRequest<T> request, 
            IResponse response,
            LogLevel logLevel = LogLevel.Error)
        {
            var message = string.Empty;

            if (response != null)
            {
                var requestMethod = request?.GetAction();

                message = $"{requestMethod.NaIfEmpty()} --> {response.GetErrorCode().NaIfEmpty()}. {response.GetErrorMessage().NaIfEmpty()}";

                logger.Log(logLevel, new Exception(response.GetJson()), message, null);
            }

            return message;
        }
    }
}
