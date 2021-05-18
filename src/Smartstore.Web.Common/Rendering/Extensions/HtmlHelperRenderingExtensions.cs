using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Core.Localization;
using Smartstore.Utilities;
using Smartstore.Web.Modelling;
using Smartstore.Web.TagHelpers.Shared;
using Smartstore.Core.Data;
using Smartstore.Web.TagHelpers;
using Smartstore.Web.Rendering.Builders;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Reflection;

namespace Smartstore.Web.Rendering
{
    public static class HtmlHelperRenderingExtensions
    {
        // Get the protected "HtmlHelper.GenerateEditor" method
        private readonly static MethodInfo GenerateEditorMethod = typeof(HtmlHelper).GetMethod("GenerateEditor", BindingFlags.NonPublic | BindingFlags.Instance);

        #region EditorFor

        /// <summary>
        /// Returns HTML markup for the model explorer, using an editor template.
        /// </summary>
        public static IHtmlContent EditorFor(this IHtmlHelper helper, ModelExpression expression)
            => EditorFor(helper, expression, null, null, null);

        /// <summary>
        /// Returns HTML markup for the model explorer, using an editor template.
        /// </summary>
        public static IHtmlContent EditorFor(this IHtmlHelper helper, ModelExpression expression, string templateName, string htmlFieldName)
            => EditorFor(helper, expression, templateName, htmlFieldName, null);

        /// <summary>
        /// Returns HTML markup for the model explorer, using an editor template.
        /// </summary>
        public static IHtmlContent EditorFor(this IHtmlHelper helper, ModelExpression expression, string templateName)
            => EditorFor(helper, expression, templateName, null, null);

        /// <summary>
        /// Returns HTML markup for the model explorer, using an editor template.
        /// </summary>
        public static IHtmlContent EditorFor(this IHtmlHelper helper, ModelExpression expression, string templateName, object additionalViewData)
            => EditorFor(helper, expression, templateName, null, additionalViewData);

        /// <summary>
        /// Returns HTML markup for the model explorer, using an editor template.
        /// </summary>
        public static IHtmlContent EditorFor(this IHtmlHelper helper, ModelExpression expression, object additionalViewData)
            => EditorFor(helper, expression, null, null, additionalViewData);

        /// <summary>
        /// Returns HTML markup for the model explorer, using an editor template.
        /// </summary>
        public static IHtmlContent EditorFor(this IHtmlHelper helper, 
            ModelExpression expression,
            string templateName,
            string htmlFieldName,
            object additionalViewData)
        {
            Guard.NotNull(helper, nameof(helper));
            Guard.NotNull(expression, nameof(expression));

            if (helper is HtmlHelper htmlHelper)
            {
                return (IHtmlContent)GenerateEditorMethod.Invoke(htmlHelper, new object[] 
                { 
                    expression.ModelExplorer,
                    htmlFieldName ?? expression.Name,
                    templateName,
                    additionalViewData
                });
            }
            else
            {
                return helper.Editor(htmlFieldName ?? expression.Name, templateName, additionalViewData);
            }
        }

        #endregion

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
                var tabs = new List<TabItem>(locales.Count + 1);
                var languages = new List<Language>(locales.Count + 1);
                
                // Create the parent tabstrip
                var strip = new TabStripTagHelper 
                {
                    ViewContext = helper.ViewContext,
                    Id = name,
                    SmartTabSelection = false,
                    Style = TabsStyle.Tabs,
                    PublishEvent = false
                };

                if (hasMasterTemplate)
                {
                    var masterLanguage = db.Languages.FindById(languageService.GetMasterLanguageId(), false);
                    languages.Add(masterLanguage);

                    // Add the first default tab for the master template
                    tabs.Add(new TabItem
                    {
                        Selected = true,
                        Text = localizationService.GetResource("Admin.Common.Standard")
                    });
                }

                // Add all language specific tabs
                for (var i = 0; i < locales.Count; i++)
                {
                    var locale = locales[i];
                    var language = db.Languages.FindById(locale.LanguageId, false);
                    languages.Add(language);

                    tabs.Add(new TabItem
                    {
                        Selected = !hasMasterTemplate && i == 0,
                        Text = language.Name,
                        ImageUrl = "~/images/flags/" + language.FlagImageFileName
                    });
                }

                // Create TagHelperContext for tabstrip.
                var stripContext = new TagHelperContext("tabstrip", new TagHelperAttributeList(), new Dictionary<object, object>(), CommonHelper.GenerateRandomDigitCode(10));

                // Must init tabstrip, otherwise "Parent" is null inside tab helpers.
                strip.Init(stripContext);

                // Create tab factory
                var tabFactory = new TabFactory(strip, stripContext);

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

                        tabFactory.AddAsync(builder => 
                        {
                            builder.Item = tabs[i];
                            builder
                                .Content(isMaster ? masterTemplate(helper.ViewData.Model) : localizedTemplate(i - 1))
                                .HtmlAttributes("title", language.Name, !isMaster)
                                .ContentHtmlAttributes(new
                                {
                                    @class = "locale-editor-content",
                                    data_lang = language.LanguageCulture,
                                    data_rtl = language.Rtl.ToString().ToLower()
                                });
                        }).GetAwaiter().GetResult();
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
