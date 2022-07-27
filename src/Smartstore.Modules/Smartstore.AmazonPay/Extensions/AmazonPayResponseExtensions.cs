using System.Text;
using Amazon.Pay.API.Types;
using Microsoft.AspNetCore.WebUtilities;
using Smartstore.Utilities;

namespace Smartstore.AmazonPay
{
    internal static class AmazonPayResponseExtensions
    {
        public static string GetShortMessage(this AmazonPayResponse response)
        {
            return $"{ReasonPhrases.GetReasonPhrase(response.Status)} {response.Method} {response.Url}";
        }

        public static string GetFullMessage(this AmazonPayResponse response)
        {
            if (response == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder(200);

            sb.AppendLine($"{response.Method} {response.Url}");
            sb.AppendLine($"Status: {ReasonPhrases.GetReasonPhrase(response.Status)} ({response.Status})");
            sb.AppendLine($"Request-ID: {response.RequestId}");
            sb.AppendLine($"Retries: {response.Retries}");
            sb.AppendLine($"Duration: {response.Duration} ms.");

            if (response.RawRequest.HasValue())
            {
                sb.AppendLine();
                sb.AppendLine(Prettifier.PrettifyJSON(response.RawRequest));
            }

            if (response.RawResponse.HasValue())
            {
                sb.AppendLine();
                sb.AppendLine(Prettifier.PrettifyJSON(response.RawResponse));
            }

            return sb.ToString();
        }
    }
}
