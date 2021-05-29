using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Admin
{
    [HtmlTargetElement(EditorTagName, Attributes = ForAttributeName, TagStructure = TagStructure.NormalOrSelfClosing)]
    public class SettingEditorTagHelper : BaseFormTagHelper
    {
        const string EditorTagName = "setting-editor";
        const string TemplateAttributeName = "asp-template";
        const string ParentSelectorAttributeName = "parent-selector";
        
        /// <summary>
        /// Specifies the editor template which will be used to render the field.
        /// </summary>
        [HtmlAttributeName(TemplateAttributeName)]
        public string Template { get; set; }

        /// <summary>
        /// Sets the parent selector. TODO: (mh) (core) describe why this must be set occasionally.
        /// </summary>
        [HtmlAttributeName(ParentSelectorAttributeName)]
        public string ParentSelector { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            ProcessCoreAsync(context, output).Await();
        }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            var content = await output.GetChildContentAsync();
            if (content.IsEmptyOrWhiteSpace)
            {
                output.Content.SetHtmlContent(HtmlHelper.EditorFor(For, Template));
            }

            var data = HtmlHelper.ViewData[StoreDependingSettingHelper.ViewDataKey] as StoreDependingSettingData;
            if (data == null || data.ActiveStoreScopeConfiguration <= 0)
            {
                output.TagName = null;
            }
            else
            {
                output.TagName = "div";
                output.AppendCssClass("form-row flex-nowrap multi-store-setting-group");

                var overrideColDiv = new TagBuilder("div");
                overrideColDiv.Attributes["class"] = "col-auto";
                overrideColDiv.InnerHtml.AppendHtml(SettingOverrideCheckboxInternal(data));

                // Controls are not floating, so line-break prevents different distances between them.
                overrideColDiv.InnerHtml.Append("\r\n");

                var settingColDiv = new TagBuilder("div");
                settingColDiv.Attributes["class"] = "col multi-store-setting-control";

                output.PreContent.AppendHtml(overrideColDiv);
                output.WrapContentWith(settingColDiv);
            }
        }

        private IHtmlContent SettingOverrideCheckboxInternal(StoreDependingSettingData data)
        {
            var fieldPrefix = HtmlHelper.ViewData.TemplateInfo.HtmlFieldPrefix;
            var settingKey = For.Name;

            if (fieldPrefix.HasValue())
            {
                settingKey = fieldPrefix + "." + settingKey;
            }
            else if (data.RootSettingClass.HasValue() && !settingKey.StartsWith(data.RootSettingClass + '.', StringComparison.OrdinalIgnoreCase))
            {
                settingKey = data.RootSettingClass + '.' + settingKey;
            }

            var overrideForStore = data.OverrideSettingKeys.Contains(settingKey);
            var fieldId = settingKey.EnsureEndsWith("_OverrideForStore");

            var switchLabel = new TagBuilder("label");
            switchLabel.AppendCssClass("switch switch-blue multi-store-override-switch");

            var overrideInput = new TagBuilder("input");
            overrideInput.Attributes["class"] = "multi-store-override-option";
            overrideInput.Attributes["type"] = "checkbox";
            overrideInput.Attributes["id"] = fieldId;
            overrideInput.Attributes["name"] = fieldId;
            overrideInput.Attributes["onclick"] = "Smartstore.Admin.checkOverriddenStoreValue(this)";
            overrideInput.Attributes["data-parent-selector"] = ParentSelector.EmptyNull();

            if (overrideForStore)
            {
                overrideInput.Attributes["checked"] = "checked";
            }

            var toggleSpan = new TagBuilder("span");
            toggleSpan.AppendCssClass("switch-toggle");
            toggleSpan.Attributes.Add("data-on", T("Common.On").Value.Truncate(3));
            toggleSpan.Attributes.Add("data-off", T("Common.Off").Value.Truncate(3));

            switchLabel.InnerHtml.AppendHtml(overrideInput);
            switchLabel.InnerHtml.AppendHtml(toggleSpan);

            return switchLabel;
        }
    }
}
