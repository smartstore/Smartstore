using System;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core;
using Smartstore.Core.Localization;

namespace Smartstore.Web.UI.TagHelpers.Admin
{
    [OutputElementHint("label")]
    [HtmlTargetElement(LabelTagName, Attributes = ForAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    public class SmartLabelTagHelper : BaseFormTagHelper
    {
        const string LabelTagName = "smart-label";

        private readonly IWorkContext _workContext;
        private readonly ILocalizationService _localizationService;

        public SmartLabelTagHelper(IWorkContext workContext, ILocalizationService localizationService)
        {
            _workContext = workContext;
            _localizationService = localizationService;
        }

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
            base.ProcessCore(context, output);

            string labelText = Text 
                ?? For.Metadata.DisplayName.NullEmpty() 
                ?? For.Metadata.PropertyName.SplitPascalCase();

            // Generate main <label/>
            output.TagName = "label";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.MergeAttribute("for", HtmlHelper.GenerateIdFromName(For.Name), true);
            output.Content.SetContent(labelText);

            if (!IgnoreHint)
            {
                if (LocalizedDisplayName != null)
                {
                    var langId = _workContext.WorkingLanguage.Id;
                    var hintText = _localizationService.GetResource(LocalizedDisplayName.ResourceKey + ".Hint", langId, logIfNotFound: false, returnEmptyIfNotFound: true);

                    if (hintText.HasValue())
                    {
                        // Append hint element to label
                        output.PostElement.AppendHtml(HtmlHelper.Hint(hintText));
                    }
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