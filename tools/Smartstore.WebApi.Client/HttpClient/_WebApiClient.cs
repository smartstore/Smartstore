using System.Data;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Windows.Forms;
using Microsoft.AspNetCore.StaticFiles;
using Newtonsoft.Json.Linq;
using Smartstore.WebApi.Client.Models;

namespace Smartstore.WebApi.Client
{
    public class WebApiClient
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

        public static string JsonAcceptType => MediaTypeNames.Application.Json;

        public static bool BodySupported(string method)
            => !string.IsNullOrWhiteSpace(method) && string.Compare(method, "GET", true) != 0 && string.Compare(method, "DELETE", true) != 0;

        // So far all API methods with multipart support are POST methods.
        public static bool MultipartSupported(string method)
            => string.Compare(method, "POST", true) == 0;

        public async Task<WebApiResponse> StartRequestAsync(
            WebApiRequestContext context, 
            string content,
            FileUploadModel uploadModel = null)
        {
            if (context == null || !context.IsValid)
            {
                return null;
            }

            if (context.ProxyPort > 0)
            {
                // API behind a reverse proxy.
                context.Url = new UriBuilder(context.Url) { Port = context.ProxyPort }.Uri.ToString();
            }

            string contentType = null;
            HttpContent body = null;
            var requestContent = new StringBuilder();
            var credentialsStr = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{context.PublicKey}:{context.SecretKey}"));

            var request = new HttpRequestMessage(new HttpMethod(context.HttpMethod), context.Url);
            request.Headers.Add("Accept", context.HttpAcceptType);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", credentialsStr);

            if (context.AdditionalHeaders.HasValue())
            {
                var jsonHeaders = JObject.Parse(context.AdditionalHeaders);
                foreach (var item in jsonHeaders)
                {
                    var value = item.Value?.ToString();
                    if (item.Key.HasValue() && value.HasValue())
                    {
                        request.Headers.Add(item.Key, value);
                    }
                }
            }

            if (BodySupported(context.HttpMethod))
            {
                if (MultipartSupported(context.HttpMethod) && uploadModel != null)
                {
                    //var formDataBoundary = "----------{0:N}".FormatInvariant(Guid.NewGuid());
                    //var data = await GetMultipartFormData(multipartData, formDataBoundary, requestContent);
                    //contentType = "multipart/form-data; boundary=" + formDataBoundary;

                    //body = new ByteArrayContent(data);
                }
                else if (!string.IsNullOrWhiteSpace(content))
                {
                    requestContent.Append(content);
                    body = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
                    body.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json) { CharSet = "UTF-8" };
                }
            }

            if (body != null)
            {
                
            }


            var result = new WebApiResponse();

            return result;
        }

        public async Task<bool> ProcessResponseAsync(
            HttpWebRequest webRequest,
            WebApiResponse response,
            FolderBrowserDialog folderBrowserDialog)
        {
            if (webRequest == null)
            {
                return false;
            }

            var result = true;
            HttpWebResponse webResponse = null;

            try
            {
                webResponse = webRequest.GetResponse() as HttpWebResponse;
                await GetResponse(webResponse, response, folderBrowserDialog);
            }
            catch (WebException wex)
            {
                result = false;
                webResponse = wex.Response as HttpWebResponse;
                await GetResponse(webResponse, response, folderBrowserDialog);
            }
            catch (Exception ex)
            {
                result = false;
                response.Content = $"{ex.Message}\r\n{ex.StackTrace}";
            }
            finally
            {
                if (webResponse != null)
                {
                    webResponse.Close();
                    webResponse.Dispose();
                }
            }

            return result;
        }

        // TODO: (mg) (core) obsolete
        public Dictionary<string, object> CreateMultipartData(FileUploadModel model)
        {
            if (!(model?.Files?.Any() ?? false))
            {
                return null;
            }

            var result = new Dictionary<string, object>();
            var count = 0;

            // Identify entity by its identifier.
            if (model.Id != 0)
            {
                result.Add("Id", model.Id);
            }

            // Custom properties like SKU etc.
            foreach (var kvp in model.CustomProperties)
            {
                if (kvp.Key.HasValue() && kvp.Value != null)
                {
                    result.Add(kvp.Key, kvp.Value);
                }
            }

            // File data.
            foreach (var file in model.Files)
            {
                if (File.Exists(file.LocalPath))
                {
                    var apiFile = new ApiFileParameter(file.LocalPath);

                    // Add file parameters. Omit default values (let the server apply them).
                    if (file.Id != 0)
                    {
                        apiFile.Parameters.Add("PictureId", file.Id.ToString());
                    }
                    if (file.Path.HasValue())
                    {
                        apiFile.Parameters.Add("Path", file.Path);
                    }
                    if (!file.IsTransient)
                    {
                        apiFile.Parameters.Add("IsTransient", file.IsTransient.ToString());
                    }
                    if (file.DuplicateFileHandling != DuplicateFileHandling.ThrowError)
                    {
                        apiFile.Parameters.Add("DuplicateFileHandling", ((int)file.DuplicateFileHandling).ToString());
                    }

                    // Test pass through of custom parameters but the API ignores them anyway.
                    //apiFile.Parameters.Add("CustomValue1", string.Format("{0:N}", Guid.NewGuid()));
                    //apiFile.Parameters.Add("CustomValue2", string.Format("say hello to {0}", id));

                    result.Add($"my-file-{++count}", apiFile);
                }
            }

            return result;
        }

        private async Task GetResponse(HttpWebResponse webResponse, WebApiResponse response, FolderBrowserDialog dialog)
        {
            if (webResponse == null)
            {
                return;
            }

            response.Status = $"{(int)webResponse.StatusCode} {webResponse.StatusDescription}";
            response.Headers = webResponse.Headers.ToString();
            response.ContentType = webResponse.ContentType;
            response.ContentLength = webResponse.ContentLength;

            var ct = response.ContentType;

            if (ct.HasValue() && (ct.StartsWith("image/") || ct.StartsWith("video/") || ct == "application/pdf"))
            {
                dialog.Description = "Please select a folder to save the file return by Web API.";

                var dialogResult = dialog.ShowDialog();
                if (dialogResult == DialogResult.OK)
                {
                    string fileName = null;
                    if (webResponse.Headers["Content-Disposition"] != null)
                    {
                        fileName = webResponse.Headers["Content-Disposition"].Replace("inline; filename=", "").Replace("\"", "");
                    }
                    if (fileName.IsEmpty())
                    {
                        fileName = "web-api-response";
                    }

                    var path = Path.Combine(dialog.SelectedPath, fileName);

                    using (var stream = File.Create(path))
                    {
                        await webResponse.GetResponseStream().CopyToAsync(stream);
                    }

                    System.Diagnostics.Process.Start(path);
                }
            }
            else
            {
                using var reader = new StreamReader(webResponse.GetResponseStream(), Encoding.UTF8);

                response.Content = await reader.ReadToEndAsync();
            }
        }

        private static MultipartFormDataContent CreateMultipartFormData(FileUploadModel model, StringBuilder requestContent)
        {
            // TODO: (mg) (core) add to requestContent
            var result = new MultipartFormDataContent();

            // Identify entity by its identifier.
            if (model.Id != 0)
            {
                var content = new StringContent(model.Id.ToString(), Encoding.UTF8);
                content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = "Id" };
                result.Add(content);
            }

            // Custom properties like SKU etc.
            foreach (var pair in model.CustomProperties.Where(x => x.Key.HasValue() && x.Value != null))
            {
                var content = new StringContent(pair.Value.ToString(), Encoding.UTF8);
                content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { Name = pair.Key };
                result.Add(content);
            }

            // TODO: (mg) (core) go on with work here

            return result;
        }

        // TODO: (mg) (core) obsolete
        private static MultipartFormDataContent CreateMultipartFormData(Dictionary<string, object> postParameters, StringBuilder requestContent)
        {
            var result = new MultipartFormDataContent();

            foreach (var param in postParameters)
            {
                if (param.Value is ApiFileParameter file)
                {
                    var content = new StreamContent(new FileStream(file.FilePath, FileMode.Open));
                    content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                    content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = param.Key,
                        FileName = file.FileName ?? param.Key
                    };

                    foreach (var key in file.Parameters.AllKeys)
                    {
                        content.Headers.ContentDisposition.Parameters.Add(new NameValueHeaderValue(key, file.Parameters[key]));
                    }

                    result.Add(content);
                }
                else if (param.Value != null)
                {
                    // Custom properties like SKU etc.
                    var content = new StringContent(param.Value.ToString(), Encoding.UTF8);
                    content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = param.Key
                    };

                    result.Add(content);
                }
            }

            return result;
        }
    }
}
