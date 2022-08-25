using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
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
        const string PostfixAttributeName = "sm-postfix";

        /// <summary>
        /// Specifies the editor template which will be used to render the field.
        /// </summary>
        [HtmlAttributeName(TemplateAttributeName)]
        public string Template { get; set; }

        /// <summary>
        /// Sets the parent selector. Must be set if an editor template is used which renders more than only an input so the checkbox can be rendered at the correct location.
        /// </summary>
        [HtmlAttributeName(ParentSelectorAttributeName)]
        public string ParentSelector { get; set; }

        /// <summary>
        /// The text which will be displayed inside the input tag as a post fix.
        /// </summary>
        [HtmlAttributeName(PostfixAttributeName)]
        public string Postfix { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            ProcessCoreAsync(context, output).Await();
        }

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            var content = await output.GetChildContentAsync();
            if (content.IsEmptyOrWhiteSpace)
            {
                var additionalViewData = new RouteValueDictionary() { ["postfix"] = Postfix };
                if (output.Attributes.TryGetAttribute("data-toggler-for", out var attr))
                {
                    // TODO: (mh) (core) Find a better solution to pass custom attributes to auto-generated editors.
                    additionalViewData["htmlAttributes"] = new { data_toggler_for = attr.ValueAsString() };
                }

                output.Content.SetHtmlContent(HtmlHelper.EditorFor(For, Template, additionalViewData));
            }

            var data = HtmlHelper.ViewData[MultiStoreSettingHelper.ViewDataKey] as MultiStoreSettingData;
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
                overrideColDiv.InnerHtml.AppendLine();

                var settingColDiv = new TagBuilder("div");
                settingColDiv.Attributes["class"] = "col multi-store-setting-control";

                output.PreContent.AppendHtml(overrideColDiv);
                output.WrapContentWith(settingColDiv);
            }
        }

        private IHtmlContent SettingOverrideCheckboxInternal(MultiStoreSettingData data)
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
