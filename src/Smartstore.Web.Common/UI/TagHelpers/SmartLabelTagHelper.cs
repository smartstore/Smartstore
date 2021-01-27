using System;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core;
using Smartstore.Core.Localization;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.UI.TagHelpers
{
    [OutputElementHint("label")]
    [HtmlTargetElement(LabelTagName, Attributes = ForAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class SmartLabelTagHelper : SmartTagHelper
    {
        const string LabelTagName = "smart-label";
        const string ForAttributeName = "asp-for";

        private readonly IWorkContext _workContext;
        private readonly ILocalizationService _localizationService;
        private readonly IHtmlGenerator _htmlGenerator;

        public SmartLabelTagHelper(IWorkContext workContext, ILocalizationService localizationService, IHtmlGenerator htmlGenerator)
        {
            _workContext = workContext;
            _localizationService = localizationService;
            _htmlGenerator = htmlGenerator;
        }

        /// <summary>
        /// An expression to be evaluated against the current model.
        /// </summary>
        [HtmlAttributeName(ForAttributeName)]
        public ModelExpression For { get; set; }

        /// <summary>
        /// Whether to ignore display hint/description.
        /// Defaults to <see langword="true" />.
        /// </summary>
        public bool IgnoreHint { get; set; }

        /// <summary>
        /// The label text to use instead of the automatically resolved display name from model metadata.
        /// </summary>
        public string Text { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            string hintText = null;
            string labelText = Text 
                ?? For.Metadata.DisplayName.NullEmpty() 
                ?? For.Metadata.PropertyName.SplitPascalCase();

            // Generate main <label/>
            output.TagName = "label";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.MergeAttribute("for", HtmlHelper.GenerateIdFromName(For.Name), true);
            output.Content.SetContent(labelText);

            if (!IgnoreHint 
                && For.Metadata.AdditionalValues.TryGetValue(nameof(LocalizedDisplayNameAttribute), out var value)
                && value is LocalizedDisplayNameAttribute attribute)
            {
                var langId = _workContext.WorkingLanguage.Id;
                hintText = _localizationService.GetResource(attribute.ResourceKey + ".Hint", langId, logIfNotFound: false, returnEmptyIfNotFound: true);

                if (hintText.HasValue())
                {
                    // Append hint element to label
                    output.PostElement.AppendHtml(HtmlHelper.Hint(hintText));
                }
            }

            // <div class="ctl-label">...</div> around content
            output.PreElement.AppendHtml("<div class=\"ctl-label\">");
            output.PostElement.AppendHtml("</div>");
        }

        protected override string GenerateTagId(TagHelperContext context)
            => null;
    }
}
