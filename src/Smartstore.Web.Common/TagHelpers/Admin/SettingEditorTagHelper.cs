using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Web.Modelling.Settings;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Admin
{
    [OutputElementHint("div")]
    [HtmlTargetElement(EditorTagName, Attributes = ForAttributeName, TagStructure = TagStructure.NormalOrSelfClosing)]
    public class SettingEditorTagHelper : BaseFormTagHelper
    {
        const string EditorTagName = "setting-editor";
        const string TemplateAttributeName = "asp-template";
        const string PostfixAttributeName = "postfix";
        const string IsEnumAttributeName = "is-enum";
        const string OptionLabelAttributeName = "option-label";
        const string ParentSelectorAttributeName = "parent-selector";
        
        /// <summary>
        /// Specifies the editor template which will be used to render the field.
        /// </summary>
        [HtmlAttributeName(TemplateAttributeName)]
        public string Template { get; set; }

        /// <summary>
        /// Specifies if the model type is an enumeration.
        /// </summary>
        [HtmlAttributeName(IsEnumAttributeName)]
        public bool IsEnum { get; set; }

        /// <summary>
        /// Sets the optional label for select lists.
        /// </summary>
        [HtmlAttributeName(OptionLabelAttributeName)]
        public string OptionLabel { get; set; }

        /// <summary>
        /// Sets the parent selector. TODO: (mh) (core) describe why this must be set occasionally.
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
            output.TagName = null;
            output.SuppressOutput();

            var htmlAttributes = new Dictionary<string, object>();
            var viewContextAware = HtmlHelper as IViewContextAware;
            viewContextAware?.Contextualize(ViewContext);

            // TODO: (mh) (core) This can probably be removed.
            var attrs = output.Attributes;
            
            if (attrs != null && attrs.Count > 0)
            {
                foreach (var attr in attrs)
                {
                    htmlAttributes[attr.Name] = attr.Value;
                }
            }

            IHtmlContent editor;

            if (IsEnum)
            {
                // TODO: (mh) (core) Localization is incorrect.
                editor = HtmlHelper.DropDownList(
                    For.Name,
                    HtmlHelper.GetEnumSelectList(For.Model.GetType()),
                    OptionLabel.HasValue() ? OptionLabel : null, 
                    new { htmlAttributes, postfix = Postfix });
            }
            else
            {
                editor = content.IsEmptyOrWhiteSpace ? HtmlHelper.EditorFor(For, Template, new { htmlAttributes }) : content;
            }

            var data = HtmlHelper.ViewData[StoreDependingSettingHelper.ViewDataKey] as StoreDependingSettingData;
            if (data == null || data.ActiveStoreScopeConfiguration <= 0)
            {
                output.Content.SetHtmlContent(editor);
            }
            else
            {
                var formRowDiv = new TagBuilder("div");
                formRowDiv.AppendCssClass("form-row flex-nowrap multi-store-setting-group");

                var overrideColDiv = new TagBuilder("div");
                overrideColDiv.AddCssClass("col-auto");
                overrideColDiv.InnerHtml.AppendHtml(SettingOverrideCheckboxInternal(HtmlHelper, For.Name, data, ParentSelector));

                var settingColDiv = new TagBuilder("div");
                settingColDiv.AddCssClass("col multi-store-setting-control");
                settingColDiv.InnerHtml.AppendHtml(editor);

                formRowDiv.InnerHtml.AppendHtml(overrideColDiv);
                formRowDiv.InnerHtml.AppendHtml(settingColDiv);
                output.Content.SetHtmlContent(formRowDiv);
            }
        }

        private IHtmlContent SettingOverrideCheckboxInternal(
            IHtmlHelper helper,
            string fieldName,
            StoreDependingSettingData data,
            string parentSelector = null)
        {
            var fieldPrefix = helper.ViewData.TemplateInfo.HtmlFieldPrefix;
            var settingKey = fieldName;

            if (fieldPrefix.HasValue())
            {
                settingKey = fieldPrefix + "." + settingKey;
            }
            else if (data.RootSettingClass.HasValue() && !settingKey.StartsWith(data.RootSettingClass + ".", StringComparison.OrdinalIgnoreCase))
            {
                settingKey = data.RootSettingClass + "." + settingKey;
            }

            var overrideForStore = data.OverrideSettingKeys.Contains(settingKey);
            var fieldId = settingKey.EnsureEndsWith("_OverrideForStore");

            var switchLabel = new TagBuilder("label");
            switchLabel.AppendCssClass("switch switch-blue multi-store-override-switch");

            var overrideInput = new TagBuilder("input");
            overrideInput.AppendCssClass("multi-store-override-option");
            overrideInput.Attributes.Add("type", "checkbox");
            overrideInput.Attributes.Add("id", fieldId);
            overrideInput.Attributes.Add("name", fieldId);
            overrideInput.Attributes.Add("onclick", "Smartstore.Admin.checkOverriddenStoreValue(this)");
            overrideInput.Attributes.Add("data-parent-selector", parentSelector.EmptyNull());

            if (overrideForStore)
            {
                overrideInput.Attributes.Add("checked", "checked");
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
