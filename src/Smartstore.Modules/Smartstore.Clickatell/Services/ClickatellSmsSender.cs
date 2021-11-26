using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Smartstore.Clickatell.Settings;

namespace Smartstore.Clickatell.Services
{
    public static class ClickatellSmsSender
    {
        private static ILogger Logger { get; set; } = NullLogger.Instance;

        private static HttpClient Client = new();

        public static async Task SendSmsAsync(ClickatellSettings settings, string text)
        {
            if (text.IsEmpty())
                return;

            string error = null;
            HttpResponseMessage responseMessage = null;

            try
            {
                // https://www.clickatell.com/developers/api-documentation/rest-api-request-parameters/
                var request = new HttpRequestMessage(HttpMethod.Post, "https://platform.clickatell.com/messages" );

                request.Headers.Add("Accept", "application/json");
                request.Headers.Add("ContentType", "application/json");
                // TODO: (mh) (core) Remove comments after review 
                // INFO: the following two commented lines won't work as a API key looks like this 2GzrIg2NLLa2dKvBPZdYOXQ== and is not valid as Authorization header because of the ==
                //request.Headers.Authorization = new AuthenticationHeaderValue(settings.ApiId);
                //request.Headers.Add("Authorization", settings.ApiId);
                Client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", settings.ApiId);

                var data = new Dictionary<string, object>
                {
                    { "content", text },
                    { "to", settings.PhoneNumber.SplitSafe(";") }
                };

                var json = JsonConvert.SerializeObject(data);

                // UTF8 is default encoding
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                responseMessage = await Client.SendAsync(request);
            }
            catch (HttpRequestException wexc)
            {
                responseMessage = new HttpResponseMessage((HttpStatusCode)wexc.StatusCode);
            }
            catch (Exception exception)
            {
                error = exception.ToString();
            }

            if (responseMessage != null)
            {
                using var reader = new StreamReader(await responseMessage.Content.ReadAsStreamAsync(), Encoding.UTF8);
                var rawResponse = reader.ReadToEnd();

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
                Logger.Error(error);
                throw new SmartException(error);
            }
        }
    }
}
