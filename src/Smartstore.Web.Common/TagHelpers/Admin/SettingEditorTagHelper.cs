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

        private readonly MultiStoreSettingHelper _settingHelper;

        public SettingEditorTagHelper(MultiStoreSettingHelper settingHelper)
        {
            _settingHelper = settingHelper;
        }

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
            output.TagMode = TagMode.StartTagAndEndTag;

            var content = await output.GetChildContentAsync();
            if (content.IsEmptyOrWhiteSpace)
            {
                var additionalViewData = new RouteValueDictionary { ["postfix"] = Postfix };

                if (output.Attributes.TryGetAttribute("placeholder", out var placeholder))
                {
                    additionalViewData["placeholder"] = placeholder.ValueAsString();
                }

                if (output.Attributes.TryGetAttribute("data-toggler-for", out var togglerFor))
                {
                    // TODO: (mh) (core) Find a better solution to pass custom attributes to auto-generated editors.
                    additionalViewData["htmlAttributes"] = output.Attributes.TryGetAttribute("data-toggler-reverse", out var reverse)
                        ? new { data_toggler_for = togglerFor.ValueAsString(), data_toggler_reverse = reverse.ValueAsString() }
                        : new { data_toggler_for = togglerFor.ValueAsString() };
                }

                output.Content.SetHtmlContent(HtmlHelper.EditorFor(For, Template, additionalViewData));
            }

            var data = _settingHelper.Data;
            if (data == null || data.StoreScope <= 0)
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

            var overrideForStore = data.OverriddenKeys.Contains(settingKey);
            var fieldId = settingKey.EnsureEndsWith("_OverrideForStore");

            var switchContainer = new TagBuilder("div");
            switchContainer.AppendCssClass("form-check form-check-solo form-switch form-switch-lg multi-store-override-switch");

            var overrideInput = new TagBuilder("input");
            overrideInput.Attributes["class"] = "form-check-input multi-store-override-option";
            overrideInput.Attributes["type"] = "checkbox";
            overrideInput.Attributes["id"] = fieldId.SanitizeHtmlId();
            overrideInput.Attributes["name"] = fieldId;
            overrideInput.Attributes["onclick"] = "Smartstore.Admin.checkOverriddenStoreValue(this)";
            overrideInput.Attributes["data-parent-selector"] = ParentSelector.EmptyNull();

            if (overrideForStore)
            {
                overrideInput.Attributes["checked"] = "checked";
            }

            switchContainer.InnerHtml.AppendHtml(overrideInput);

            return switchContainer;
        }
    }
}
