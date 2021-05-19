using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Smartstore.Core.DataExchange.Export.Deployment
{
    public class HttpFilePublisher : IFilePublisher
    {
        public async Task PublishAsync(ExportDeployment deployment, ExportDeploymentContext context, CancellationToken cancellationToken)
        {
            var succeededFiles = 0;
            var url = deployment.Url;
            var files = await context.GetDeploymentFilesAsync(cancellationToken);

            if (!url.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) &&
                !url.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase))
            {
                url = "http://" + url;
            }

            var uri = new Uri(url);

            if (deployment.HttpTransmissionType == ExportHttpTransmissionType.MultipartFormDataPost)
            {
                var num = 0;
                var credentials = deployment.Username.HasValue()
                    ? new NetworkCredential(deployment.Username, deployment.Password)
                    : null;

                using var handler = new HttpClientHandler { Credentials = credentials };
                using var client = new HttpClient(handler);
                using var formData = new MultipartFormDataContent();

                foreach (var file in files)
                {
                    var bytes = await file.ReadAllBytesAsync();
                    formData.Add(new ByteArrayContent(bytes), "file {0}".FormatInvariant(++num), file.Name);
                }

                var response = await client.PostAsync(uri, formData, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    succeededFiles = num;
                }
                else if (response.Content != null)
                {
                    context.Result.LastError = context.T("Admin.Common.HttpStatus", (int)response.StatusCode, response.StatusCode.ToString());

                    var content = await response.Content.ReadAsStringAsync(cancellationToken);

                    var msg = "Multipart form data upload failed. HTTP status {0} ({1}). Response: {2}".FormatInvariant(
                        (int)response.StatusCode, response.StatusCode.ToString(), content.NaIfEmpty().Truncate(2000, "..."));

                    context.Log.Error(msg);
                }
            }
            else
            {
                using var webClient = new WebClient();

                if (deployment.Username.HasValue())
                {
                    webClient.Credentials = new NetworkCredential(deployment.Username, deployment.Password);
                }

                foreach (var file in files)
                {
                    // Not async. Send as a sequence, next after the previous one has been completed.
                    webClient.UploadFile(uri, file.PhysicalPath);

                    ++succeededFiles;
                }
            }

            context.Log.Info($"{succeededFiles} file(s) successfully uploaded via HTTP ({deployment.HttpTransmissionType}).");
        }
    }
}
