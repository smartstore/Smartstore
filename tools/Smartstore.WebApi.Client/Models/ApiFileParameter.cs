using System.Collections.Specialized;

namespace Smartstore.WebApi.Client.Models
{
    public class ApiFileParameter
    {
        public ApiFileParameter(byte[] data)
            : this(data, null)
        {
        }

        public ApiFileParameter(byte[] data, string fileName)
            : this(data, fileName, null)
        {
        }

        public ApiFileParameter(byte[] data, string fileName, string contentType)
        {
            Data = data;
            FileName = fileName;
            ContentType = contentType;
            Parameters = new NameValueCollection();
        }

        public byte[] Data { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }

        public NameValueCollection Parameters { get; set; }
    }
}
