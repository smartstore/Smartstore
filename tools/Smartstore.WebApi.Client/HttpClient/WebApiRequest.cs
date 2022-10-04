using System.Text;

namespace Smartstore.WebApi.Client
{
    public class WebApiRequest
    {
        public string PublicKey { get; set; }
        public string SecretKey { get; set; }

        public string Url { get; set; }
        public int ProxyPort { get; set; }
        public string HttpMethod { get; set; }

        public string HttpAcceptType { get; set; }
        public string AdditionalHeaders { get; set; }

        public FolderBrowserDialog FileDialog { get; set; }

        public bool IsValid => !string.IsNullOrWhiteSpace(PublicKey) && !string.IsNullOrWhiteSpace(SecretKey) &&
                    !string.IsNullOrWhiteSpace(Url) &&
                    !string.IsNullOrWhiteSpace(HttpMethod) && !string.IsNullOrWhiteSpace(HttpAcceptType);

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("PublicKey: " + PublicKey);
            sb.AppendLine("SecretKey: " + SecretKey);
            sb.AppendLine("Url: " + Url);
            sb.AppendLine("Proxy Port: " + (ProxyPort > 0 ? ProxyPort.ToString() : string.Empty));
            sb.AppendLine("HttpMethod: " + HttpMethod);
            sb.AppendLine("HttpAcceptType: " + HttpAcceptType);

            return sb.ToString();
        }
    }
}
