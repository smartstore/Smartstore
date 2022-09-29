using System.Net;
using System.Text;
using Microsoft.AspNetCore.StaticFiles;
using Smartstore.WebApi.Client.Models;

namespace Smartstore.WebApi.Client
{
    public class WebApiClient
    {
        public static string JsonAcceptType => "application/json";

        public static bool BodySupported(string method)
            => !string.IsNullOrWhiteSpace(method) && string.Compare(method, "GET", true) != 0 && string.Compare(method, "DELETE", true) != 0;

        // So far all API methods with multipart support are POST methods.
        public static bool MultipartSupported(string method)
            => string.Compare(method, "POST", true) == 0;

        public Task<HttpWebRequest> StartRequestAsync(
            WebApiRequestContext context, 
            string content, 
            Dictionary<string, object> multipartData,
            out StringBuilder requestContent)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ProcessResponseAsync(
            HttpWebRequest webRequest,
            WebApiResponse response,
            FolderBrowserDialog folderBrowserDialog)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, object> CreateMultipartData(FileUploadModel model)
        {
            if (!(model?.Files?.Any() ?? false))
            {
                return null;
            }

            var result = new Dictionary<string, object>();
            var isValid = false;
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
                    using var fstream = new FileStream(file.LocalPath, FileMode.Open, FileAccess.Read);

                    byte[] data = new byte[fstream.Length];
                    fstream.Read(data, 0, data.Length);

                    var name = Path.GetFileName(file.LocalPath);
                    var id = $"my-file-{++count}";

                    new FileExtensionContentTypeProvider().TryGetContentType(name, out string contentType);
                    var apiFile = new ApiFileParameter(data, name, contentType);

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

                    result.Add(id, apiFile);
                    isValid = true;
                    fstream.Close();
                }
            }

            if (!isValid)
            {
                return null;
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

        /// <see cref="http://stackoverflow.com/questions/219827/multipart-forms-from-c-sharp-client" />
        private static async Task<byte[]> GetMultipartFormData(Dictionary<string, object> postParameters, string boundary, StringBuilder requestContent)
        {
            var needsCLRF = false;
            var sb = new StringBuilder();
            using var stream = new MemoryStream();

            foreach (var param in postParameters)
            {
                if (needsCLRF)
                {
                    await WriteToStream(stream, requestContent, "\r\n");
                }

                needsCLRF = true;

                if (param.Value is ApiFileParameter file)
                {
                    sb.Clear();
                    sb.AppendFormat("--{0}\r\n", boundary);
                    sb.AppendFormat("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"", param.Key, file.FileName ?? param.Key);

                    foreach (var key in file.Parameters.AllKeys)
                    {
                        sb.AppendFormat("; {0}=\"{1}\"", key, file.Parameters[key].Replace('"', '\''));
                    }

                    sb.AppendFormat("\r\nContent-Type: {0}\r\n\r\n", file.ContentType ?? "application/octet-stream");

                    await WriteToStream(stream, requestContent, sb.ToString());

                    await stream.WriteAsync(file.Data.AsMemory(0, file.Data.Length));
                    requestContent.AppendFormat("<Binary file data here (length {0} bytes)...>", file.Data.Length);
                }
                else
                {
                    var postData = string.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}",
                        boundary,
                        param.Key,
                        param.Value);

                    await WriteToStream(stream, requestContent, postData);
                }
            }

            await WriteToStream(stream, requestContent, "\r\n--" + boundary + "--\r\n");

            stream.Position = 0;
            byte[] formData = new byte[stream.Length];
            await stream.ReadAsync(formData);

            return formData;
        }

        private static async Task WriteToStream(MemoryStream stream, StringBuilder requestContent, string data)
        {
            await stream.WriteAsync(Encoding.UTF8.GetBytes(data).AsMemory(0, Encoding.UTF8.GetByteCount(data)));
            requestContent.Append(data);
        }

        private static void SetTimeout(HttpWebRequest webRequest)
        {
#if DEBUG
            // Just for debugging.
            webRequest.Timeout = 1000 * 60 * 5;
#endif
        }
    }
}
