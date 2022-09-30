using System.Collections.Specialized;
using Microsoft.AspNetCore.StaticFiles;

namespace Smartstore.WebApi.Client.Models
{
    public class ApiFileParameter
    {
        public ApiFileParameter(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found {filePath ?? "-"}.");
            }

            FilePath = filePath;
            FileName = Path.GetFileName(filePath);

            new FileExtensionContentTypeProvider().TryGetContentType(FileName, out string contentType);
            ContentType = contentType;
        }

        public string FilePath { get; }
        public string FileName { get; }
        public string ContentType { get; }

        public NameValueCollection Parameters { get; } = new();
    }
}
