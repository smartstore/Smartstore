using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Core.Localization;
using Smartstore.Core.Theming;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.Theming
{
    public static class ThemingHtmlHelper
    {
        public static IHtmlContent ThemeVarLabel(this IHtmlHelper helper, ThemeVariableInfo info, string hint = null)
        {
            Guard.NotNull(info, "info");

            var resKey = "ThemeVar.{0}.{1}".FormatInvariant(info.ThemeDescriptor.Name, info.Name);
            var services = helper.ViewContext.HttpContext.RequestServices;
            var langId = services.GetRequiredService<IWorkContext>().WorkingLanguage.Id;
            var locService = services.GetRequiredService<ILocalizationService>();
            var builder = new SmartHtmlContentBuilder();

            var displayName = locService.GetResource(resKey, langId, false, string.Empty, true);

            if (displayName.HasValue() && hint.IsEmpty())
            {
                hint = locService.GetResource(resKey + ".Hint", langId, false, string.Empty, true);
                hint = "${0}{1}".FormatInvariant(info.Name, hint.HasValue() ? "\n" + hint : string.Empty);
            }

            builder.AppendHtml("<div class='ctl-label'>");
            builder.AppendHtml(helper.Label(helper.NameForThemeVar(info), displayName.NullEmpty() ?? "$" + info.Name, new { @class = "x-col-form-label" }));

            if (hint.HasValue())
            {
                builder.AppendHtml(helper.HintTooltip(hint));
            }

            builder.AppendHtml("</div>");

            return builder;
        }

        public static IHtmlContent ThemeVarChainInfo(this IHtmlHelper helper, ThemeVariableInfo info)
        {
            Guard.NotNull(info, "info");

            var currentTheme = helper.ViewContext.HttpContext.RequestServices.GetRequiredService<IThemeContext>().CurrentTheme;

            if (currentTheme != info.ThemeDescriptor)
            {
                // the variable is inherited from a base theme: display an info badge
                var chainInfo = "<span class='themevar-chain-info'><i class='fa fa-link fa-flip-horizontal'></i><span class='pl-1'>{0}</span></span>".FormatCurrent(info.ThemeDescriptor.Name);
                return new HtmlString(chainInfo);
            }

            return HtmlString.Empty;
        }

        public static IHtmlContent ThemeVarEditor(this IHtmlHelper helper, ThemeVariableInfo info, object value)
        {
            Guard.NotNull(info);

            string expression = helper.NameForThemeVar(info);
            string strValue = value?.ToString().EmptyNull();

            var isDefault = strValue.EqualsNoCase(info.DefaultValue);
            var isValidColor = info.Type == ThemeVariableType.Color
                && ((strValue.HasValue() && ThemeVariableRepository.IsValidColor(strValue)) || (strValue.IsEmpty() && ThemeVariableRepository.IsValidColor(info.DefaultValue)));

            IHtmlContent control;

            if (isValidColor)
            {
                // Don't allow color swatches in theme configuration because of Sass color functions
                control = helper.ColorBox(expression, strValue, info.DefaultValue, false);
            }
            else if (info.Type == ThemeVariableType.Boolean)
            {
                var label = new TagBuilder("div");
                label.Attributes.Add("class", "form-check form-check-solo form-check-warning form-switch form-switch-lg");
                label.InnerHtml.AppendHtml(helper.CheckBox(expression, strValue.ToBool(), new { @class = "form-check-input" }));

                control = label;
            }
            else if (info.Type == ThemeVariableType.Select)
            {
                var descriptor = info.ThemeDescriptor;
                if (!descriptor.Selects.ContainsKey(info.SelectRef))
                {
                    throw new InvalidOperationException($"A select list with id '{info.SelectRef}' was not specified. Please specify a 'Select' element with at least one 'Option' child.");
                }

                var selectList = from x in descriptor.Selects[info.SelectRef]
                                 select new SelectListItem
                                 {
                                     Value = x,
                                     Text = x, // TODO: (mc) Localize
                                     Selected = x.EqualsNoCase(strValue)
                                 };

                control = helper.DropDownList(expression, selectList, info.DefaultValue, new { placeholder = info.DefaultValue, @class = "form-control" });
            }
            else
            {
                control = helper.TextBox(expression, isDefault ? string.Empty : strValue, new { placeholder = info.DefaultValue, @class = "form-control" });
            }

            return control;
        }

        public static string IdForThemeVar(this IHtmlHelper helper, ThemeVariableInfo info)
        {
            return helper.NameForThemeVar(info).SanitizeHtmlId();
        }

        public static string NameForThemeVar(this IHtmlHelper _, ThemeVariableInfo info)
        {
            return "values[{0}]".FormatInvariant(info.Name);
        }
    }
}
