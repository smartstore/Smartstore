//using System;
//using System.IO;
//using System.Text;
//using Microsoft.AspNetCore.Http;
//using Smartstore.Http;

//namespace Smartstore.Pdf
//{
//    public class PdfHtmlContent : PdfContent
//    {
//        private string _html;
//        private string _originalHtml;
//        private bool _processed;
//        private string _tempFilePath;
//        private PdfContentKind _kind = PdfContentKind.Html;

//        public PdfHtmlContent(string html, HttpRequest request)
//            : base(request)
//        {
//            Guard.NotEmpty(html, nameof(html));

//            _originalHtml = html;
//            _html = html;
//        }

//        public override PdfContentKind Kind => _kind;

//        public override string ProcessContent(string flag)
//        {
//            if (!_processed)
//            {
//                if (EngineBaseUri != null)
//                {
//                    _html = WebHelper.MakeAllUrlsAbsolute(_html, EngineBaseUri.Scheme, EngineBaseUri.Authority);
//                }
//                else if (Request != null)
//                {
//                    _html = WebHelper.MakeAllUrlsAbsolute(_html, Request);
//                }

//                if (!flag.EqualsNoCase("page"))
//                {
//                    CreateTempFile();
//                }

//                _processed = true;
//            }

//            return _tempFilePath ?? _html;
//        }

//        //protected internal override void Apply(string flag, HtmlToPdfDocument document)
//        //{
//        //    // TODO: (core) Apply PdfHtmlContent
//        //}

//        private void CreateTempFile()
//        {
//            // TODO: (mc) This is a very weak mechanism to determine if html is partial. Find a better way!
//            bool isPartial = !_html.Trim().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase);
//            if (isPartial)
//            {
//                _html = WrapPartialHtml(_html);
//            }

//            string tempPath = Path.GetTempPath();
//            _tempFilePath = Path.Combine(tempPath, "pdfgen-" + Path.GetRandomFileName() + ".html");
//            File.WriteAllBytes(_tempFilePath, Encoding.UTF8.GetBytes(_html));

//            _kind = PdfContentKind.Url;
//        }

//        private static string WrapPartialHtml(string html)
//        {
//            return string.Format("<!DOCTYPE html><html><head>\r\n<meta http-equiv=\"content-type\" content=\"text/html; charset=utf-8\" />\r\n<script>\r\nfunction subst() {{\r\n    var vars={{}};\r\n    var x=document.location.search.substring(1).split('&');\r\n\r\n    for(var i in x) {{var z=x[i].split('=',2);vars[z[0]] = unescape(z[1]);}}\r\n    var x=['frompage','topage','page','webpage','section','subsection','subsubsection'];\r\n    for(var i in x) {{\r\n      var y = document.getElementsByClassName(x[i]);\r\n      for(var j=0; j<y.length; ++j) y[j].textContent = vars[x[i]];\r\n    }}\r\n}}\r\n</script></head><body style=\"border:0; margin: 0;\" onload=\"subst()\">{0}</body></html>\r\n", html);
//        }

//        public override void Teardown()
//        {
//            if (_tempFilePath != null && File.Exists(_tempFilePath))
//            {
//                try
//                {
//                    File.Delete(_tempFilePath);
//                }
//                catch 
//                { 
//                }
//            }

//            _kind = PdfContentKind.Html;
//            _html = _originalHtml;
//            _tempFilePath = null;
//            _processed = false;
//        }
//    }
//}