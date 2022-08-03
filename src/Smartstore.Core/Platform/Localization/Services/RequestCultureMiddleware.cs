using System.Globalization;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Smartstore.Caching;
using Smartstore.Core.Widgets;
using Smartstore.Utilities;

namespace Smartstore.Core.Localization
{
    /// <summary>
    /// Uses culture from current working language and sets globalization clients scripts accordingly.
    /// </summary>
    public class RequestCultureMiddleware
    {
        // DIN 5008.
        private static string[] _deMonthAbbreviations = new[] { "Jan.", "Feb.", "März", "Apr.", "Mai", "Juni", "Juli", "Aug.", "Sept.", "Okt.", "Nov.", "Dez.", "" };

        private readonly RequestDelegate _next;
        private readonly ICacheManager _cache;

        public RequestCultureMiddleware(RequestDelegate next, ICacheManager cache)
        {
            _next = next;
            _cache = cache;
        }

        public async Task Invoke(HttpContext context, IWorkContext workContext)
        {
            var request = context.Request;
            var language = workContext.WorkingLanguage;

            var culture = workContext.CurrentCustomer != null && language != null
                ? new CultureInfo(language.LanguageCulture)
                : new CultureInfo("en-US");

            if (language?.UniqueSeoCode?.EqualsNoCase("de") ?? false)
            {
                culture.DateTimeFormat.AbbreviatedMonthNames = culture.DateTimeFormat.AbbreviatedMonthGenitiveNames = _deMonthAbbreviations;
            }

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            if (!context.Request.IsAjax())
            {
                var widgetProvider = context.RequestServices.GetService<IWidgetProvider>();

                if (widgetProvider != null && culture.Name != "en-US")
                {
                    var script = _cache.Get($"globalizationscript:{culture.Name}", o =>
                    {
                        // Write globalization script
                        var json = CreateCultureJson(culture, language);

                        using var psb = StringBuilderPool.Instance.Get(out var sb);
                        sb.Append("<script data-origin='globalization'>");
                        sb.Append("document.addEventListener('DOMContentLoaded', function () { if (Smartstore.globalization) { Smartstore.globalization.culture = ");
                        sb.Append(json);
                        sb.Append("; }; });");
                        sb.Append("</script>");

                        return sb.ToString();
                    });

                    widgetProvider.RegisterHtml("head", new HtmlString(script));
                }
            }

            await _next(context);
        }

        private static string CreateCultureJson(CultureInfo ci, Language language)
        {
            var nf = ci.NumberFormat;
            var df = ci.DateTimeFormat;

            var dict = new Dictionary<string, object>
            {
                { "name", ci.Name },
                { "englishName", ci.EnglishName },
                { "nativeName", ci.NativeName },
                { "isRTL", language?.Rtl ?? ci.TextInfo.IsRightToLeft }, // favor RTL property of Language
				{ "language", ci.TwoLetterISOLanguageName },
                { "numberFormat", new Dictionary<string, object>
                {
                    { ",", nf.NumberGroupSeparator },
                    { ".", nf.NumberDecimalSeparator },
                    { "pattern", new[] { nf.NumberNegativePattern } },
                    { "decimals", nf.NumberDecimalDigits },
                    { "groupSizes", nf.NumberGroupSizes },
                    { "+", nf.PositiveSign },
                    { "-", nf.NegativeSign },
                    { "NaN", nf.NaNSymbol },
                    { "negativeInfinity", nf.NegativeInfinitySymbol },
                    { "positiveInfinity", nf.PositiveInfinitySymbol },
                    { "percent", new Dictionary<string, object>
                    {
                        { ",", nf.PercentGroupSeparator },
                        { ".", nf.PercentDecimalSeparator },
                        { "pattern", new[] { nf.PercentNegativePattern, nf.PercentPositivePattern } },
                        { "decimals", nf.PercentDecimalDigits },
                        { "groupSizes", nf.PercentGroupSizes },
                        { "symbol", nf.PercentSymbol }
                    } },
                    { "currency", new Dictionary<string, object>
                    {
                        { ",", nf.CurrencyGroupSeparator },
                        { ".", nf.CurrencyDecimalSeparator },
                        { "pattern", new[] { nf.CurrencyNegativePattern, nf.CurrencyPositivePattern } },
                        { "decimals", nf.CurrencyDecimalDigits },
                        { "groupSizes", nf.CurrencyGroupSizes },
                        { "symbol", nf.CurrencySymbol }
                    } },
                } },
                { "dateTimeFormat", new Dictionary<string, object>
                {
                    { "calendarName", df.NativeCalendarName },
                    { "/", df.DateSeparator },
                    { ":", df.TimeSeparator },
                    { "firstDay", (int)df.FirstDayOfWeek },
                    { "twoDigitYearMax", ci.Calendar.TwoDigitYearMax },
                    { "AM", df.AMDesignator.IsEmpty() ? null : new[] { df.AMDesignator, df.AMDesignator.ToLower(), df.AMDesignator.ToUpper() } },
                    { "PM", df.PMDesignator.IsEmpty() ? null : new[] { df.PMDesignator, df.PMDesignator.ToLower(), df.PMDesignator.ToUpper() } },
                    { "days", new Dictionary<string, object>
                    {
                        { "names", df.DayNames },
                        { "namesAbbr", df.AbbreviatedDayNames },
                        { "namesShort", df.ShortestDayNames },
                    } },
                    { "months", new Dictionary<string, object>
                    {
                        { "names", df.MonthNames },
                        { "namesAbbr", df.AbbreviatedMonthNames },
                    } },
                    { "patterns", new Dictionary<string, object>
                    {
                        { "d", df.ShortDatePattern },
                        { "D", df.LongDatePattern },
                        { "t", df.ShortTimePattern },
                        { "T", df.LongTimePattern },
                        { "g", df.ShortDatePattern + " " + df.ShortTimePattern },
                        { "G", df.ShortDatePattern + " " + df.LongTimePattern },
                        { "f", df.FullDateTimePattern }, // TODO: (core) find it actually
						{ "F", df.FullDateTimePattern },
                        { "M", df.MonthDayPattern },
                        { "Y", df.YearMonthPattern },
                        { "u", df.UniversalSortableDateTimePattern },
                    } }
                } }
            };

            var json = JsonConvert.SerializeObject(dict, new JsonSerializerSettings
            {
                Formatting = Formatting.None
            });

            return json;
        }
    }
}