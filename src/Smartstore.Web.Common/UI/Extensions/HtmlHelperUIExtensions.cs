using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Localization;
using Smartstore.Utilities;
using Smartstore.Web.Modelling;
using Smartstore.Web.UI.TagHelpers.Shared;
using Smartstore.Core.Data;
using Smartstore.Web.UI.TagHelpers;

namespace Smartstore.Web.UI
{
    public static class HtmlHelperUIExtensions
    {
        #region Hint

        /// <summary>
        /// Generates a question mark icon that pops a description tooltip on hover
        /// </summary>
        public static IHtmlContent HintTooltipFor<TModel, TResult>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TResult>> expression)
        {
            Guard.NotNull(expression, nameof(expression));

            var modelExpression = helper.ModelExpressionFor(expression);
            var hintText = modelExpression?.Metadata?.Description;

            return HintTooltip(helper, hintText);
        }

        /// <summary>
        /// Generates a question mark icon that pops a description tooltip on hover
        /// </summary>
        public static IHtmlContent HintTooltip(this IHtmlHelper _, string hintText)
        {
            if (string.IsNullOrEmpty(hintText))
            {
                return HtmlString.Empty;
            }

            // create a
            var a = new TagBuilder("a");
            a.Attributes.Add("href", "#");
            a.Attributes.Add("onclick", "return false;");
            //a.Attributes.Add("rel", "tooltip");
            a.Attributes.Add("title", hintText);
            a.Attributes.Add("tabindex", "-1");
            a.Attributes.Add("class", "hint");

            // Create symbol
            var img = new TagBuilder("i");
            img.Attributes.Add("class", "fa fa-question-circle");

            a.InnerHtml.SetHtmlContent(img);

            // Return <a> tag
            return a;
        }

        /// <summary>
        /// Generates control description text.
        /// </summary>
        public static IHtmlContent HintFor<TModel, TResult>(this IHtmlHelper<TModel> helper, Expression<Func<TModel, TResult>> expression)
        {
            Guard.NotNull(expression, nameof(expression));

            var modelExpression = helper.ModelExpressionFor(expression);
            var hintText = modelExpression?.Metadata?.Description;

            return Hint(helper, hintText);
        }

        /// <summary>
        /// Generates control description text.
        /// </summary>
        public static IHtmlContent Hint(this IHtmlHelper _, string hintText)
        {
            if (string.IsNullOrEmpty(hintText))
            {
                return HtmlString.Empty;
            }

            // create <small>
            var small = new TagBuilder("small");
            small.Attributes.Add("class", "form-text text-muted");

            small.InnerHtml.SetContent(hintText);

            return small;
        }

        #endregion

        #region LocalizedEditor

        public static IHtmlContent LocalizedEditor<TModel, TLocalizedModelLocal>(this IHtmlHelper<TModel> helper,
            string name,
            Func<int, HelperResult> localizedTemplate,
            Func<TModel, HelperResult> masterTemplate)
            where TModel : ILocalizedModel<TLocalizedModelLocal>
            where TLocalizedModelLocal : ILocalizedLocaleModel
        {
            var locales = helper.ViewData.Model.Locales;
            int i = -1;

            if (locales.Count > 1)
            {
                var services = helper.ViewContext.HttpContext.RequestServices;
                var db = services.GetRequiredService<SmartDbContext>();
                var languageService = services.GetRequiredService<ILanguageService>();
                var localizationService = services.GetRequiredService<ILocalizationService>();
                
                var strip = new TabStripTagHelper 
                {
                    ViewContext = helper.ViewContext,
                    Id = name,
                    SmartTabSelection = false,
                    Style = TabsStyle.Tabs
                };

                var masterLanguage = db.Languages.FindById(languageService.GetMasterLanguageId(), false);
                var contentTag = new TagBuilder("div");
                contentTag.Attributes.Add("class", "locale-editor-content");
                contentTag.Attributes.Add("data-lang", masterLanguage.LanguageCulture);
                contentTag.Attributes.Add("data-rtl", masterLanguage.Rtl.ToString().ToLower());

                strip.Tabs.Add(new TabTagHelper
                {
                    ViewContext = helper.ViewContext,
                    Selected = true,
                    Title = localizationService.GetResource("Admin.Common.Standard"),
                    TabInnerContent = new DefaultTagHelperContent()
                        .AppendHtml(contentTag.RenderStartTag())
                        .AppendHtml(masterTemplate(helper.ViewData.Model))
                        .AppendHtml(contentTag.RenderEndTag()),
                    Attributes = new TagHelperAttributeList(),
                    Parent = strip,
                    Index = 0
                });

                for (i = 0; i < locales.Count; i++)
                {
                    var locale = helper.ViewData.Model.Locales[i];
                    var language = db.Languages.FindById(locale.LanguageId, false);

                    contentTag.MergeAttribute("data-lang", language.LanguageCulture, true);
                    contentTag.MergeAttribute("data-rtl", language.Rtl.ToString().ToLower(), true);

                    strip.Tabs.Add(new TabTagHelper
                    {
                        ViewContext = helper.ViewContext,
                        Selected = i == 0 && masterTemplate == null,
                        Title = language.Name,
                        ImageUrl = "~/images/flags/" + language.FlagImageFileName,
                        TabInnerContent = new DefaultTagHelperContent()
                            .AppendHtml(contentTag.RenderStartTag())
                            .AppendHtml(localizedTemplate(i))
                            .AppendHtml(contentTag.RenderEndTag()),
                        Attributes = new TagHelperAttributeList
                        {
                            new TagHelperAttribute("title", language.Name)
                        },
                        Parent = strip,
                        Index = i + 1
                    });
                }

                var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), CommonHelper.GenerateRandomDigitCode(10));
                var outputAttrList = new TagHelperAttributeList { new TagHelperAttribute("class", "nav-locales") };
                var output = new TagHelperOutput("tabstrip", outputAttrList, (useCachedResult, encoder) => 
                {
                    return Task.FromResult<TagHelperContent>(new DefaultTagHelperContent());
                });

                strip.ProcessAsync(context, output).GetAwaiter().GetResult();
            }
            else if (masterTemplate != null)
            {
                return masterTemplate(helper.ViewData.Model);
            }

            return HtmlString.Empty;
        }

        #endregion
    }
}
