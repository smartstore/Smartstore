using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
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
            // INFO: (mh) (core) The HttpClient instance is automatically injected and managed by IHttpClientFactory. See Startup.cs.
            // INFO: (mh) (core) ILogger needs to be ctor injected, because this class is not instantiated by Autofac (and only Autofac can inject property dependencies).
            _client = client;
            _logger = logger;
        }

        public async Task SendSmsAsync(string text, ClickatellSettings settings, CancellationToken cancelToken = default)
        {
            // INFO: (mh) (core) Untested code!
            
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
                //throw new SmartException(error); // INFO: (mh) (core) EventInvoker will log twice otherwise.
            }
        }
    }
}
