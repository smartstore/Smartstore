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
            var hasMasterTemplate = masterTemplate != null;

            if (locales.Count > 1)
            {
                var services = helper.ViewContext.HttpContext.RequestServices;
                var db = services.GetRequiredService<SmartDbContext>();
                var languageService = services.GetRequiredService<ILanguageService>();
                var localizationService = services.GetRequiredService<ILocalizationService>();
                var tabs = new List<TabTagHelper>(locales.Count + 1);
                var languages = new List<Language>(locales.Count + 1);
                
                // Create the parent tabstrip
                var strip = new TabStripTagHelper 
                {
                    ViewContext = helper.ViewContext,
                    Id = name,
                    SmartTabSelection = false,
                    Style = TabsStyle.Tabs
                };

                if (hasMasterTemplate)
                {
                    var masterLanguage = db.Languages.FindById(languageService.GetMasterLanguageId(), false);
                    languages.Add(masterLanguage);

                    // Add the first default tab for the master template
                    tabs.Add(new TabTagHelper
                    {
                        ViewContext = helper.ViewContext,
                        Selected = true,
                        Title = localizationService.GetResource("Admin.Common.Standard")
                    });
                }

                // Add all language specific tabs
                for (var i = 0; i < locales.Count; i++)
                {
                    var locale = locales[i];
                    var language = db.Languages.FindById(locale.LanguageId, false);
                    languages.Add(language);

                    tabs.Add(new TabTagHelper
                    {
                        ViewContext = helper.ViewContext,
                        Selected = !hasMasterTemplate && i == 0,
                        Title = language.Name,
                        ImageUrl = "~/images/flags/" + language.FlagImageFileName
                    });
                }

                // Create TagHelperContext for tabstrip.
                var stripContext = new TagHelperContext("tabstrip", new TagHelperAttributeList(), new Dictionary<object, object>(), CommonHelper.GenerateRandomDigitCode(10));

                // Must init tabstrip, otherwise "Parent" is null inside tab helpers.
                strip.Init(stripContext);

                // Create AttributeList for tabstrip
                var stripOutputAttrList = new TagHelperAttributeList { new TagHelperAttribute("class", "nav-locales") };

                // Emulate tabstrip output
                var stripOutput = new TagHelperOutput("tabstrip", stripOutputAttrList, (_, _) => 
                {
                    // getChildContentAsync for tabstrip
                    for (var i = 0; i < tabs.Count; i++)
                    {
                        var isMaster = hasMasterTemplate && i == 0;
                        var language = languages[i];

                        // Create TagHelperContext for tab passing it parent context's items dictionary (that's what Razor does)
                        var context = new TagHelperContext("tab", new TagHelperAttributeList(), stripContext.Items, CommonHelper.GenerateRandomDigitCode(10));

                        // Must init tabstrip, otherwise "Tabs" list is empty inside tabstrip helper.
                        tabs[i].Init(context);

                        var outputAttrList = new TagHelperAttributeList();
                        if (!isMaster)
                        {
                            outputAttrList.Add("title", language.Name);
                        }

                        var output = new TagHelperOutput("tab", outputAttrList, (_, _) => 
                        {
                            // getChildContentAsync for tab
                            var contentTag = new TagBuilder("div");

                            // Wrap tab's template result with specific element
                            contentTag.Attributes.Add("class", "locale-editor-content");
                            contentTag.Attributes.Add("data-lang", language.LanguageCulture);
                            contentTag.Attributes.Add("data-rtl", language.Rtl.ToString().ToLower());

                            TagHelperContent tabContent = new DefaultTagHelperContent()
                                .AppendHtml(contentTag.RenderStartTag())
                                .AppendHtml(isMaster ? masterTemplate(helper.ViewData.Model) : localizedTemplate(i - 1))
                                .AppendHtml(contentTag.RenderEndTag());

                            return Task.FromResult(tabContent);
                        });

                        // Process single tab
                        tabs[i].ProcessAsync(context, output).GetAwaiter().GetResult();
                    }

                    // We don't need the child content for tabstrip. It builds everything without any child content.
                    return Task.FromResult<TagHelperContent>(new DefaultTagHelperContent());
                });

                // Process parent tabstrip
                strip.ProcessAsync(stripContext, stripOutput).GetAwaiter().GetResult();

                var wrapper = new TagBuilder("div");
                wrapper.Attributes.Add("class", "locale-editor");

                return stripOutput.WrapElementWith(wrapper);
            }
            else if (hasMasterTemplate)
            {
                return masterTemplate(helper.ViewData.Model);
            }

            return HtmlString.Empty;
        }

        #endregion
    }
}
