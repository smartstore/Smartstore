using Amazon.Pay.API.Types;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Smartstore.Utilities;

namespace Smartstore.AmazonPay
{
    internal static class ILoggerExtensions
    {
        public static string LogAmazonPayFailure(this ILogger logger,
            ApiRequestBody request,
            AmazonPayResponse response,
            LogLevel logLevel = LogLevel.Warning)
        {
            var message = string.Empty;

            if (response != null)
            {
                message = $"{ReasonPhrases.GetReasonPhrase(response.Status)} ({response.Status}) {response.Url}";

                using var psb = StringBuilderPool.Instance.Get(out var sb);

                sb.AppendLine($"{response.Method} {response.Url}");
                sb.AppendLine($"Request-ID: {response.RequestId}");
                sb.AppendLine($"Retries: {response.Retries}");
                sb.AppendLine($"Duration: {response.Duration} ms.");

                try
                {
                    if (request != null)
                    {
                        sb.AppendLine();
                        sb.AppendLine(request.ToJsonNoType(new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            TypeNameHandling = TypeNameHandling.None,
                            Formatting = Formatting.Indented
                        }));
                    }

                    if (response.RawResponse.HasValue())
                    {
                        sb.AppendLine();
                        if (response.RawResponse.StartsWith('['))
                        {
                            sb.AppendLine(JArray.Parse(response.RawResponse).ToString());
                        }
                        else
                        {
                            sb.AppendLine(JToken.Parse(response.RawResponse).ToString());
                        }
                    }
                }
                catch
                {
                }

                logger.Log(logLevel, new Exception(sb.ToString()), message, null);
            }

            return message;
        }
    }
}
