using System.Net.Http.Headers;

namespace Smartstore.Net.Http
{
    public static class HttpHeadersExtensions
    {
        public static Dictionary<string, string> ToFlatDictionary(this HttpHeaders headers)
        {
            Guard.NotNull(headers, nameof(headers));

            return headers.NonValidated
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToString());
        }
    }
}
