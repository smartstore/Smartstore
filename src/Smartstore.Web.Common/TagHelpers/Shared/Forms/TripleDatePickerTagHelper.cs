using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("triple-date-picker", TagStructure = TagStructure.WithoutEndTag)]
    public class TripleDatePickerTagHelper : BaseFormTagHelper
    {
        const string DayNameAttributeName = "day-name";
        const string MonthNameAttributeName = "month-name";
        const string YearNameAttributeName = "year-name";
        const string BeginYearAttributeName = "begin-year";
        const string EndYearAttributeName = "end-year";
        const string DisabledAttributeName = "disabled";

        private readonly ILocalizationService _localizationService;

        public TripleDatePickerTagHelper(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        [HtmlAttributeName(DayNameAttributeName)]
        public ModelExpression DayName { get; set; }

        [HtmlAttributeName(MonthNameAttributeName)]
        public ModelExpression MonthName { get; set; }

        [HtmlAttributeName(YearNameAttributeName)]
        public ModelExpression YearName { get; set; }

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
                { "name", DayName.Name },
                { "id", DayName.Name },
                { "class", "date-part form-control noskin remember" },
                { "data-minimum-results-for-search", "100" }
            });
            monthsList.Attributes.AddRange(new Dictionary<string, string> {
                { "data-native-menu", "false" },
                { "name", MonthName.Name },
                { "id", MonthName.Name },
                { "class", "date-part form-control noskin remember" },
                { "data-minimum-results-for-search", "100" }
            });
            yearsList.Attributes.AddRange(new Dictionary<string, string> {
                { "data-native-menu", "false" },
                { "name", YearName.Name },
                { "id", YearName.Name },
                { "class", "date-part form-control noskin remember" },
                //{ "data-minimum-results-for-search", "100" }
            });

            if (Disabled)
            {
                daysList.Attributes.Add("disabled", "disabled");
                monthsList.Attributes.Add("disabled", "disabled");
                yearsList.Attributes.Add("disabled", "disabled");
            }

            var days = new StringBuilder();
            var months = new StringBuilder();
            var years = new StringBuilder();

            // Add initial options.
            days.AppendFormat("<option value=''>{0}</option>", _localizationService.GetResource("Common.Day"));
            months.AppendFormat("<option value=''>{0}</option>", _localizationService.GetResource("Common.Month"));
            years.AppendFormat("<option value=''>{0}</option>", _localizationService.GetResource("Common.Year"));

            // Add options for days.
            for (int i = 1; i <= 31; i++)
            {
                days.AppendFormat("<option value='{0}'{1}>{0}</option>", i,
                    (DayName.Model != null && (Convert.ToInt32(DayName.Model) == i)) ? " selected=\"selected\"" : null);
            }
                

            // Add options for months.
            for (int i = 1; i <= 12; i++)
            {
                months.AppendFormat("<option value='{0}'{1}>{2}</option>", i,
                                    (MonthName.Model != null && Convert.ToInt32(DayName.Model) == i) ? " selected=\"selected\"" : null,
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
                    (YearName.Model != null && Convert.ToInt32(YearName.Model) == i) ? " selected=\"selected\"" : null);
            }
            
            daysList.InnerHtml.AppendHtml(days.ToString());
            monthsList.InnerHtml.AppendHtml(months.ToString());
            yearsList.InnerHtml.AppendHtml(years.ToString());

            daysCol.InnerHtml.AppendHtml(daysList);
            monthsCol.InnerHtml.AppendHtml(monthsList);
            yearsCol.InnerHtml.AppendHtml(yearsList);

            output.Content
                .AppendHtml(daysCol)
                .AppendHtml(monthsCol)
                .AppendHtml(yearsCol);
        }
    }
}