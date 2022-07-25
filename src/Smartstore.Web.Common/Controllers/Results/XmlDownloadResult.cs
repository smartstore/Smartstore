using System.Xml;
using Microsoft.Net.Http.Headers;
using Smartstore.Utilities;

namespace Smartstore.Web.Controllers
{
    public class XmlDownloadResult : ActionResult
    {
        public XmlDownloadResult(string xml, string fileDownloadName)
        {
            Guard.NotEmpty(xml, nameof(xml));
            Guard.NotEmpty(fileDownloadName, nameof(fileDownloadName));

            Xml = xml;
            FileDownloadName = fileDownloadName;
        }

        public string FileDownloadName { get; set; }
        public string Xml { get; set; }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            var document = new XmlDocument();
            document.LoadXml(Xml);
            var decl = document.FirstChild as XmlDeclaration;
            if (decl != null)
            {
                decl.Encoding = "utf-8";
            }

            var response = context.HttpContext.Response;

            response.Headers[HeaderNames.ContentEncoding] = "utf-8";
            response.Headers[HeaderNames.ContentDisposition] = string.Format("attachment; filename={0}", FileDownloadName);

            var buffer = Prettifier.PrettifyXML(document.InnerXml).GetBytes();
            await response.Body.WriteAsync(buffer.AsMemory(0, buffer.Length));
        }
    }
}
