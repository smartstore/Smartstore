using System.Net;
using System.Net.Http;

namespace Smartstore.Core.DataExchange.Export.Deployment
{
    public class HttpFilePublisher : IFilePublisher
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HttpFilePublisher(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = Guard.NotNull(httpClientFactory, nameof(httpClientFactory));
        }

        public async Task PublishAsync(ExportDeployment deployment, ExportDeploymentContext context, CancellationToken cancelToken)
        {
            var succeededFiles = 0;
            var url = deployment.Url;
            var files = await context.GetDeploymentFilesAsync(cancelToken);

            if (!url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
            {
                url = "http://" + url;
            }

            var uri = new Uri(url);

            var credentials = deployment.Username.HasValue()
                ? new NetworkCredential(deployment.Username, deployment.Password)
                : null;

            var client = credentials == null
                ? _httpClientFactory.CreateClient()
                : new HttpClient(new HttpClientHandler { Credentials = credentials }, true);

            if (deployment.HttpTransmissionType == ExportHttpTransmissionType.MultipartFormDataPost)
            {
                var num = 0;
                using var formData = new MultipartFormDataContent();

                foreach (var file in files)
                {
                    num++;
                    formData.Add(new StreamContent(await file.OpenReadAsync(cancelToken)), $"file {num}", file.Name);
                }

                var response = await client.PostAsync(uri, formData, cancelToken);

                if (response.IsSuccessStatusCode)
                {
                    succeededFiles = num;
                }
                else if (response.Content != null)
                {
                    context.Result.LastError = context.T("Admin.Common.HttpStatus", (int)response.StatusCode, response.StatusCode.ToString());

                    var content = await response.Content.ReadAsStringAsync(cancelToken);

                    var msg = "Multipart form data upload failed. HTTP status {0} ({1}). Response: {2}".FormatInvariant(
                        (int)response.StatusCode, response.StatusCode.ToString(), content.NaIfEmpty().Truncate(2000, "..."));

                    context.Log.Error(msg);
                }
            }
            else
            {
                foreach (var file in files)
                {
                    using var content = new StreamContent(await file.OpenReadAsync(cancelToken));
                    await client.PostAsync(uri, content, cancelToken);
                    ++succeededFiles;
                }
            }

            context.Log.Info($"{succeededFiles} file(s) successfully uploaded via HTTP ({deployment.HttpTransmissionType}).");
        }
    }
}
