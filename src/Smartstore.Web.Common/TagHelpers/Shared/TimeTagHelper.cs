using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("time", Attributes = ForAttributeName)]
    public class TimeTagHelper : BaseFormTagHelper
    {
        const string FormatAttributeName = "sm-format";
        const string HumanizeAttributeName = "sm-humanize";

        [HtmlAttributeName(FormatAttributeName)]
        public string Format { get; set; }

        [HtmlAttributeName(HumanizeAttributeName)]
        public bool Humanize { get; set; } = false;

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            var value = For?.Model;

            if (value is not DateTime dateTime)
            {
                output.SuppressOutput();
                return;
            }

            output.TagName = "time";
            output.Attributes.SetAttribute("datetime", dateTime.ToIso8601String());
            output.Content.SetContent(Humanize ? dateTime.ToHumanizedString(false) : dateTime.ToNativeString(Format));
        }
    }
}
