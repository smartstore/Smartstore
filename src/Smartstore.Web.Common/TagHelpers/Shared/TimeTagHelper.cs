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

            string attr = null;
            string content = null;

            if (value is DateTime dt)
            {
                attr = dt.ToIso8601String();
                content = Humanize ? dt.ToHumanizedString(false) : dt.ToNativeString(Format);
            }
            else if (value is DateTimeOffset dto)
            {
                attr = dto.ToIso8601String();
                content = Humanize ? dto.ToHumanizedString() : dto.ToNativeString(Format);
            }
            else if (value is DateOnly dateOnly)
            {
                attr = dateOnly.ToIso8601String();
                content = Humanize ? dateOnly.ToHumanizedString() : dateOnly.ToNativeString(Format);
            }
            else if (value is TimeSpan timeSpan)
            {
                var timeOnly = TimeOnly.FromTimeSpan(timeSpan);
                attr = timeOnly.ToIso8601String();
                content = Humanize ? timeOnly.ToHumanizedString() : timeOnly.ToNativeString(Format);
            }
            else if (value is TimeOnly timeOnly)
            {
                attr = timeOnly.ToIso8601String();
                content = Humanize ? timeOnly.ToHumanizedString() : timeOnly.ToNativeString(Format);
            }

            if (attr == null && content == null)
            {
                output.SuppressOutput();
            }
            else
            {
                output.Attributes.SetAttribute("datetime", attr);
                output.Content.SetContent(content);
            }
        }
    }
}
