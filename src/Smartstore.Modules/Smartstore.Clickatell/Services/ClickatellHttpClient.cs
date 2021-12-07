using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Smartstore.Clickatell.Settings;

namespace Smartstore.Clickatell.Services
{
    public class ClickatellHttpClient
    {
        private readonly HttpClient _client;
        private readonly ILogger _logger;

        public ClickatellHttpClient(HttpClient client, ILogger logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task SendSmsAsync(string text, ClickatellSettings settings, CancellationToken cancelToken = default)
        {
            if (text.IsEmpty())
            {
                return;
            }

            string error = null;
            HttpResponseMessage responseMessage = null;

            var data = new Dictionary<string, object>
            {
                ["content"] = text,
                ["to"] = settings.PhoneNumber.SplitSafe(';')
            };

            try
            {
                _client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", settings.ApiId);

                responseMessage = await _client.PostAsJsonAsync(string.Empty, data, cancelToken);
            }
            //catch (HttpRequestException wexc)
            //{
            //    responseMessage = new HttpResponseMessage((HttpStatusCode)wexc.StatusCode);
            //}
            catch (Exception exception)
            {
                error = exception.ToString();
            }

            if (responseMessage != null)
            {
                var rawResponse = await responseMessage.Content.ReadAsStringAsync(cancelToken);

                if (responseMessage.StatusCode == HttpStatusCode.OK || responseMessage.StatusCode == HttpStatusCode.Accepted)
                {
                    dynamic response = JObject.Parse(rawResponse);

                    error = (string)response.error;
                    if (error.IsEmpty() && response.messages != null)
                    {
                        error = response.messages[0].error;
                    }
                }
                else
                {
                    error = rawResponse;
                }
            }

            if (error.HasValue())
            {
                _logger.Error(error);
            }
        }
    }
}
