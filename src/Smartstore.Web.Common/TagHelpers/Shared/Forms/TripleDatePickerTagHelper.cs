using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;
using Smartstore.Utilities;

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

            var daysCol = new TagBuilder("div");
            var monthsCol = new TagBuilder("div");
            var yearsCol = new TagBuilder("div");
            daysCol.AddCssClass("col");
            monthsCol.AddCssClass("col");
            yearsCol.AddCssClass("col");

            var daysList = new TagBuilder("select");
            var monthsList = new TagBuilder("select");
            var yearsList = new TagBuilder("select");

            daysList.Attributes.AddRange(new Dictionary<string, string> {
                { "data-native-menu", "false" },
                { "name", DayName },
                { "id", DayName },
                { "class", "date-part form-control noskin remember" },
                { "data-minimum-results-for-search", "100" }
            });
            monthsList.Attributes.AddRange(new Dictionary<string, string> {
                { "data-native-menu", "false" },
                { "name", MonthName },
                { "id", MonthName },
                { "class", "date-part form-control noskin remember" },
                { "data-minimum-results-for-search", "100" }
            });
            yearsList.Attributes.AddRange(new Dictionary<string, string> {
                { "data-native-menu", "false" },
                { "name", YearName },
                { "id", YearName },
                { "class", "date-part form-control noskin remember" },
                //{ "data-minimum-results-for-search", "100" }
            });

            if (Disabled)
            {
                daysList.Attributes.Add("disabled", "disabled");
                monthsList.Attributes.Add("disabled", "disabled");
                yearsList.Attributes.Add("disabled", "disabled");
            }

            var days = StringBuilderPool.Instance.Get();
            var months = StringBuilderPool.Instance.Get();
            var years = StringBuilderPool.Instance.Get();

            // Add initial options.
            days.AppendFormat("<option value=''>{0}</option>", _localizationService.GetResource("Common.Day"));
            months.AppendFormat("<option value=''>{0}</option>", _localizationService.GetResource("Common.Month"));
            years.AppendFormat("<option value=''>{0}</option>", _localizationService.GetResource("Common.Year"));

            // Add options for days.
            for (int i = 1; i <= 31; i++)
            {
                days.AppendFormat("<option value='{0}'{1}>{0}</option>", i,
                    (Day != null && (Convert.ToInt32(Day) == i)) ? " selected=\"selected\"" : null);
            }
            
            // Add options for months.
            for (int i = 1; i <= 12; i++)
            {
                months.AppendFormat("<option value='{0}'{1}>{2}</option>", i,
                                    (Month != null && Convert.ToInt32(Month) == i) ? " selected=\"selected\"" : null,
                                    CultureInfo.CurrentUICulture.DateTimeFormat.GetMonthName(i));
            }

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
            
            daysList.InnerHtml.AppendHtml(days.ToString());
            monthsList.InnerHtml.AppendHtml(months.ToString());
            yearsList.InnerHtml.AppendHtml(years.ToString());

            daysCol.InnerHtml.AppendHtml(daysList);
            monthsCol.InnerHtml.AppendHtml(monthsList);
            yearsCol.InnerHtml.AppendHtml(yearsList);

            output.Content
                .AppendHtml(DayName.HasValue() ? daysCol : null)
                .AppendHtml(MonthName.HasValue() ? monthsCol : null)
                .AppendHtml(YearName.HasValue() ? yearsCol : null);
        }
    }
}