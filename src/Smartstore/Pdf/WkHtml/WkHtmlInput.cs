using Microsoft.AspNetCore.Http;
using Smartstore.Http;

namespace Smartstore.Pdf.WkHtml
{
    internal class WkHtmlInput : IPdfInput
    {
        private string _html;
        private string _originalHtml;
        private bool _processed;
        private string _tempFilePath;

        private readonly WkHtmlToPdfOptions _options;
        private readonly HttpContext _httpContext;

        public WkHtmlInput(string html, WkHtmlToPdfOptions options, HttpContext httpContext)
        {
            _originalHtml = html;
            _html = html;
            _options = options;
            // Can be null
            _httpContext = httpContext;
        }

        public PdfInputKind Kind { get; private set; } = PdfInputKind.Html;

        public string Content
        {
            get => _tempFilePath ?? _html;
        }

        public void Teardown()
        {
            if (_tempFilePath != null && File.Exists(_tempFilePath))
            {
                try
                {
                    File.Delete(_tempFilePath);
                }
                catch
                {
                }
            }

            Kind = PdfInputKind.Html;
            _html = _originalHtml;
            _tempFilePath = null;
            _processed = false;
        }

        internal async Task ProcessAsync(string flag)
        {
            if (_processed)
            {
                return;
            }

            if (_options.BaseUrl != null)
            {
                _html = WebHelper.MakeAllUrlsAbsolute(_html, _options.BaseUrl.Scheme, _options.BaseUrl.Authority);
            }
            else if (_httpContext?.Request != null)
            {
                _html = WebHelper.MakeAllUrlsAbsolute(_html, _httpContext.Request);
            }

            if (!flag.EqualsNoCase("page"))
            {
                await CreateTempFileAsync();
            }

            _processed = true;
        }

        private async Task CreateTempFileAsync()
        {
            // TODO: (mc) This is a very weak mechanism to determine if html is partial. Find a better way!
            bool isPartial = !_html.Trim().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase);
            if (isPartial)
            {
                _html = WrapPartialHtml(_html);
            }

            _tempFilePath = WkHtmlToPdfConverter.GetTempFileName(_options, ".html");
            await File.WriteAllBytesAsync(_tempFilePath, _html.GetBytes());
            Kind = PdfInputKind.File;
        }

        private static string WrapPartialHtml(string html)
        {
            return $@"<!DOCTYPE html><html><head>
<script>
    function subst() {{
        var vars = {{}};
        var query_strings_from_url = document.location.search.substring(1).split('&');
        for (var query_string in query_strings_from_url) {{
            if (query_strings_from_url.hasOwnProperty(query_string)) {{
                var temp_var = query_strings_from_url[query_string].split('=', 2);
                vars[temp_var[0]] = decodeURI(temp_var[1]);
            }}
        }}
        var css_selector_classes = ['page', 'frompage', 'topage', 'webpage', 'section', 'subsection', 'date', 'isodate', 'time', 'title', 'doctitle', 'sitepage', 'sitepages'];
        for (var css_class in css_selector_classes) {{
            if (css_selector_classes.hasOwnProperty(css_class)) {{
                var element = document.getElementsByClassName(css_selector_classes[css_class]);
                for (var j = 0; j < element.length; ++j) {{
                    element[j].textContent = vars[css_selector_classes[css_class]];
                }}
            }}
        }}
    }}
</script></head><body style='border: 0; margin: 0;' onload='subst()'>{html}</body></html>";
        }
    }
}
