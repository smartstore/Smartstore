using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("triple-date-picker", TagStructure = TagStructure.WithoutEndTag)]
    public class TripleDatePickerTagHelper : BaseFormTagHelper
    {
        const string DayNameAttributeName = "day-name";
        const string MonthNameAttributeName = "month-name";
        const string YearNameAttributeName = "year-name";
        const string DayAttributeName = "day";
        const string MonthAttributeName = "month";
        const string YearAttributeName = "year";
        const string BeginYearAttributeName = "begin-year";
        const string EndYearAttributeName = "end-year";
        const string DisabledAttributeName = "disabled";
        const string ControlSizeAttributeName = "control-size";

        private readonly ILocalizationService _localizationService;

        public TripleDatePickerTagHelper(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        [HtmlAttributeName(DayNameAttributeName)]
        public string DayName { get; set; }

        [HtmlAttributeName(MonthNameAttributeName)]
        public string MonthName { get; set; }

        [HtmlAttributeName(YearNameAttributeName)]
        public string YearName { get; set; }

        [HtmlAttributeName(DayAttributeName)]
        public int? Day { get; set; }

        [HtmlAttributeName(MonthAttributeName)]
        public int? Month { get; set; }

        [HtmlAttributeName(YearAttributeName)]
        public int? Year { get; set; }

        [HtmlAttributeName(BeginYearAttributeName)]
        public int? BeginYear { get; set; }

        [HtmlAttributeName(EndYearAttributeName)]
        public int? EndYear { get; set; }

        [HtmlAttributeName(ControlSizeAttributeName)]
        public ControlSize ControlSize { get; set; } = ControlSize.Medium;

        [HtmlAttributeName(DisabledAttributeName)]
        public bool Disabled { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            if (DayName == null && MonthName == null && YearName == null)
            {
                output.SuppressOutput();
                return;
            }

            output.TagName = "div";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.AppendCssClass("row xs-gutters");

            var selectClass = "date-part noskin remember form-control";
            if (ControlSize != ControlSize.Medium)
            {
                selectClass += " form-control-" + (ControlSize == ControlSize.Small ? "sm" : "lg");
            }

            if (DayName.HasValue())
            {
                var daysCol = new TagBuilder("div");
                daysCol.AddCssClass("col");

                var daySelect = new TagBuilder("select");
                daySelect.Attributes.AddRange(new Dictionary<string, string> {
                    { "data-native-menu", "false" },
                    { "name", DayName },
                    { "id", DayName },
                    { "class", selectClass },
                    { "data-minimum-results-for-search", "100" }
                });

                if (Disabled)
                {
                    daySelect.Attributes.Add("disabled", "disabled");
                }

                var days = new StringBuilder(1000);

                // Add initial option.
                days.AppendFormat("<option value=''>{0}</option>", _localizationService.GetResource("Common.Day"));

                // Add options for days.
                for (int i = 1; i <= 31; i++)
                {
                    days.AppendFormat("<option value='{0}'{1}>{0}</option>", i,
                        (Day != null && (Convert.ToInt32(Day) == i)) ? " selected=\"selected\"" : null);
                }

                daySelect.InnerHtml.AppendHtml(days.ToString());
                daysCol.InnerHtml.AppendHtml(daySelect);
                output.Content.AppendHtml(daysCol);
            }

            if (MonthName.HasValue())
            {
                var monthsCol = new TagBuilder("div");
                monthsCol.AddCssClass("col");

                var monthSelect = new TagBuilder("select");
                monthSelect.Attributes.AddRange(new Dictionary<string, string> {
                    { "data-native-menu", "false" },
                    { "name", MonthName },
                    { "id", MonthName },
                    { "class", selectClass },
                    { "data-minimum-results-for-search", "100" }
                });

                if (Disabled)
                {
                    monthSelect.Attributes.Add("disabled", "disabled");
                }

                var months = new StringBuilder(1000);

                // Add initial option.
                months.AppendFormat("<option value=''>{0}</option>", _localizationService.GetResource("Common.Month"));

                // Add options for months.
                for (int i = 1; i <= 12; i++)
                {
                    months.AppendFormat("<option value='{0}'{1}>{2}</option>", i,
                                        (Month != null && Convert.ToInt32(Month) == i) ? " selected=\"selected\"" : null,
                                        CultureInfo.CurrentUICulture.DateTimeFormat.GetMonthName(i));
                }

                monthSelect.InnerHtml.AppendHtml(months.ToString());
                monthsCol.InnerHtml.AppendHtml(monthSelect);
                output.Content.AppendHtml(monthsCol);
            }

            if (YearName.HasValue())
            {
                var yearsCol = new TagBuilder("div");
                yearsCol.AddCssClass("col");

                var yearSelect = new TagBuilder("select");
                yearSelect.Attributes.AddRange(new Dictionary<string, string> {
                    { "data-native-menu", "false" },
                    { "name", YearName },
                    { "id", YearName },
                    { "class", selectClass },
                    //{ "data-minimum-results-for-search", "100" }
                });

                if (Disabled)
                {
                    yearSelect.Attributes.Add("disabled", "disabled");
                }

                var years = new StringBuilder(1000);

                years.AppendFormat("<option value=''>{0}</option>", _localizationService.GetResource("Common.Year"));

                // Add options for year.
                if (BeginYear == null)
                    BeginYear = DateTime.UtcNow.Year - 90;
                if (EndYear == null)
                    EndYear = DateTime.UtcNow.Year + 10;

                for (int i = BeginYear.Value; i <= EndYear.Value; i++)
                {
                    years.AppendFormat("<option value='{0}'{1}>{0}</option>", i,
                        (Year != null && Convert.ToInt32(Year) == i) ? " selected=\"selected\"" : null);
                }

                yearSelect.InnerHtml.AppendHtml(years.ToString());
                yearsCol.InnerHtml.AppendHtml(yearSelect);
                output.Content.AppendHtml(yearsCol);
            }
        }
    }
}