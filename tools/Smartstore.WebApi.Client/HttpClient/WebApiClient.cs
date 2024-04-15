using System.Data;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json.Linq;
using Smartstore.WebApi.Client.Models;
using IOFile = System.IO.File;

namespace Smartstore.WebApi.Client
{
    public static class WebApiClient
    {
        private static readonly HttpClient Client;

        static WebApiClient()
        {
            Client = new HttpClient();
            Client.DefaultRequestHeaders.Add("User-Agent", Program.ConsumerName);
            Client.DefaultRequestHeaders.Add("Pragma", "no-cache");
            Client.DefaultRequestHeaders.Add("Cache-Control", "no-cache, no-store");
            Client.DefaultRequestHeaders.Add("Accept-Charset", "UTF-8");

#if DEBUG
            // Just for debugging.
            Client.Timeout = TimeSpan.FromMinutes(5);
#endif
        }

        public static bool BodySupported(string method)
            => !string.IsNullOrWhiteSpace(method) && !method.EqualsNoCase("GET") && !method.EqualsNoCase("DELETE");

        // So far all API methods with multipart support are POST methods.
        public static bool MultipartSupported(string method)
            => method.EqualsNoCase("POST");

        public static async Task<WebApiResponse> StartRequestAsync(
            WebApiRequest request, 
            string content,
            FileUploadModel uploadModel = null,
            CancellationToken cancelToken = default)
        {
            if (request == null || !request.IsValid)
            {
                return null;
            }

            var result = new WebApiResponse();
            var credentialsStr = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{request.PublicKey}:{request.SecretKey}"));

            using var message = new HttpRequestMessage(new HttpMethod(request.HttpMethod), request.Url);
            message.Headers.Add("Accept", request.HttpAcceptType);
            message.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentialsStr);

            if (request.AdditionalHeaders.HasValue())
            {
                var jsonHeaders = JObject.Parse(request.AdditionalHeaders);
                foreach (var item in jsonHeaders)
                {
                    var value = item.Value?.ToString();
                    if (item.Key.HasValue() && value.HasValue())
                    {
                        message.Headers.Add(item.Key, value);
                    }
                }
            }

            if (BodySupported(request.HttpMethod))
            {
                if (MultipartSupported(request.HttpMethod) && uploadModel != null)
                {
                    message.Content = CreateMultipartFormData(uploadModel, result);
                }
                else if (content.HasValue())
                {
                    message.Content = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
                    message.Content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json) { CharSet = "UTF-8" };
                    result.RequestContent.Append(content);
                }
            }

            var response = await Client.SendAsync(message, cancelToken);
            var ch = response.Content?.Headers;

            result.Succeeded = response.IsSuccessStatusCode;
            result.Status = $"{(int)response.StatusCode} {response.ReasonPhrase}";
            result.Headers = response.Headers?.ToString();
            result.ContentType = ch?.ContentType?.MediaType;
            result.ContentLength = ch?.ContentLength ?? 0;

            if (response.IsSuccessStatusCode && result.IsFileResponse)
            {
                await SaveFile(request, response, cancelToken);
            }
            else
            {
                result.Content = await response.Content.ReadAsStringAsync(cancelToken);

                if (result.ContentType.EqualsNoCase(MediaTypeNames.Application.Json))
                {
                    result.Content = PrettifyJSON(result.Content);
                }
            }

            result.RequestContent.Insert(0, message.Headers.ToString() + "\r\n");

            return result;
        }

        private static MultipartFormDataContent CreateMultipartFormData(FileUploadModel model, WebApiResponse response)
        {
            var result = new MultipartFormDataContent();
            var count = 0;

            // INFO: additional entity identifiers are no longer recognized this way in Smartstore 5. Use query string parameters instead.
            //if (model.Id != 0)
            //{
            //    var content = new StringContent(model.Id.ToString(), Encoding.UTF8);
            //    content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = "Id" };
            //    result.Add(content);

            //    response.RequestContent.AppendLine("\r\n" + content.Headers.ToString());
            //}

            // Custom properties like deleteing existing import files etc.
            foreach (var pair in model.CustomProperties.Where(x => x.Key.HasValue() && x.Value != null))
            {
                var content = new StringContent(pair.Value.ToString(), Encoding.UTF8);
                content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = pair.Key };
                result.Add(content);

                response.RequestContent.AppendLine(content.Headers.ToString() + pair.Value.ToString() + "\r\n");
            }

            // File data.
            foreach (var file in model.Files.Where(x => IOFile.Exists(x.LocalPath)))
            {
                var id = $"my-file-{++count}";
                var fileName = Path.GetFileName(file.LocalPath);
                var fi = new FileInfo(file.LocalPath);
                new FileExtensionContentTypeProvider().TryGetContentType(fileName, out string contentType);

                if (contentType.IsEmpty() && fi.Extension.EqualsNoCase(".story"))
                {
                    contentType = MediaTypeNames.Application.Zip;
                }

                var content = new StreamContent(new FileStream(file.LocalPath, FileMode.Open));
                content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = '"' + id + '"',
                    FileName = '"' + (fileName ?? id) + '"'
                };

                // Add file parameters. Omit default values (let the server apply them).
                if (file.Id != 0)
                {
                    content.Headers.ContentDisposition.Parameters.Add(CreateParameter("fileId", file.Id.ToString()));
                }
                if (file.Path.HasValue())
                {
                    content.Headers.ContentDisposition.Parameters.Add(CreateParameter("path", file.Path));
                }
                if (!file.IsTransient)
                {
                    content.Headers.ContentDisposition.Parameters.Add(CreateParameter("isTransient", file.IsTransient.ToString()));
                }
                if (file.DuplicateFileHandling != DuplicateFileHandling.ThrowError)
                {
                    content.Headers.ContentDisposition.Parameters.Add(CreateParameter("duplicateFileHandling", ((int)file.DuplicateFileHandling).ToString()));
                }

                result.Add(content);

                response.RequestContent.AppendLine($"{content.Headers.ToString()}<Binary data for {fileName} here (length {fi.Length} bytes)…>");
            }

            return result;

            static NameValueHeaderValue CreateParameter(string key, string value)
            {
                // Quote to avoid InvalidDataException "Form section has invalid Content-Disposition value".
                return new NameValueHeaderValue(key, '"' + value.Replace('\"', '\'') + '"');
            }
        }

        private static async Task SaveFile(WebApiRequest request, HttpResponseMessage responseMessage, CancellationToken cancelToken)
        {
            request.FileDialog.Description = "Please select a folder to save the file returned by Web API.";

            var dialogResult = request.FileDialog.ShowDialog();
            if (dialogResult == DialogResult.OK)
            {
                var fileName = responseMessage.Content?.Headers?.ContentDisposition?.FileName?.NullEmpty() ?? "web-api-response";
                var path = Path.Combine(request.FileDialog.SelectedPath, fileName.Replace("\"", string.Empty));

                using var source = await responseMessage.Content.ReadAsStreamAsync(cancelToken);
                using var target = IOFile.Open(path, FileMode.Create);
                {
                    await source.CopyToAsync(target, cancelToken);
                }

                Process.Start(new ProcessStartInfo("cmd", $"/c start \"\" \"{path}\"") { CreateNoWindow = true });
            }
        }

        private static string PrettifyJSON(string json)
        {
            if (json.IsEmpty())
            {
                return json;
            }

            try
            {
                return json.StartsWith('[')
                    ? JArray.Parse(json).ToString()
                    : JToken.Parse(json).ToString();
            }
            catch
            {
                return json;
            }
        }
    }
}
