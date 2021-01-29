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
            var services = helper.ViewContext.HttpContext.RequestServices;
            int i = -1;

            if (locales.Count > 1)
            {
                var languageService = services.GetRequiredService<ILanguageService>();
                var localizationService = services.GetRequiredService<ILocalizationService>();

                var context = new TagHelperContext(new TagHelperAttributeList(), new Dictionary<object, object>(), CommonHelper.GenerateRandomDigitCode(10));
                var stripOutputAttrList = new TagHelperAttributeList(new[] { new TagHelperAttribute("class", "nav-locales") });
                var stripOutput = new TagHelperOutput("tabstrip", stripOutputAttrList, GetTabStripChildContentAsync);
                
                var strip = new TabStripTagHelper 
                {
                    Id = name,
                    SmartTabSelection = false,
                    Style = TabsStyle.Tabs
                };

                var tabs = new List<TabTagHelper>(locales.Count + 1);
                var masterLanguage = languageService.GetMasterLanguage();

                tabs.Add(new TabTagHelper
                {
                    Selected = true,
                    Title = localizationService.GetResource("Admin.Common.Standard")
                    // ...
                });

                for (i = 0; i < locales.Count; i++)
                {
                    var locale = helper.ViewData.Model.Locales[i];
                    var language = languageService.GetMasterLanguage(); // languageService.GetLanguageById(locale.LanguageId);
                    var tabInnerContent = new DefaultTagHelperContent();
                    tabInnerContent.SetHtmlContent(localizedTemplate(i));

                    tabs.Add(new TabTagHelper
                    {
                        Selected = i == 0 && masterTemplate == null,
                        Title = language.Name,
                        // ...
                        TabInnerContent = tabInnerContent
                    });
                }
            }
            else if (masterTemplate != null)
            {
                return masterTemplate(helper.ViewData.Model);
            }

            return HtmlString.Empty;

            Task<TagHelperContent> GetTabStripChildContentAsync(bool useCachedResult, HtmlEncoder encoder)
            {
                return null;
            }

            //Task<TagHelperContent> GetTabChildContentAsync(bool useCachedResult, HtmlEncoder encoder)
            //{
            //    TagHelperContent content = new DefaultTagHelperContent();

            //    if (i < 0 && masterTemplate != null)
            //    {
            //        content.SetHtmlContent(masterTemplate(helper.ViewData.Model));
            //    }
            //    else if (i >= 0)
            //    {
            //        content.SetHtmlContent(localizedTemplate(i));
            //    }

            //    return Task.FromResult(content);
            //}
        }

        #endregion
    }
}
