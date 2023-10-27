using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("div", Attributes = "[class ^= 'form-check']")]
    public class FormCheckTagHelper : TagHelper
    {
        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            context.Items["FormCheckOutput"] = output;
            await output.GetChildContentAsync();
        }
    }

    [HtmlTargetElement("select", Attributes = "asp-for, asp-placeholder")]
    [HtmlTargetElement("select", Attributes = "asp-items, asp-placeholder")]
    public class SelectPlaceholderTagHelper : TagHelper
    {
        [HtmlAttributeName("asp-placeholder")]
        public string Placeholder { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (!string.IsNullOrEmpty(Placeholder))
            {
                await output.GetChildContentAsync();
                output.PreContent.AppendHtml($"<option value=\"\">{Placeholder}</option>");
            }
        }
    }

    [HtmlTargetElement("input", Attributes = ForAttributeName, TagStructure = TagStructure.WithoutEndTag)]
    [HtmlTargetElement("select", Attributes = ForAttributeName)]
    [HtmlTargetElement("select", Attributes = SelectItemsAttributeName)]
    [HtmlTargetElement("textarea", Attributes = ForAttributeName)]
    public class FormControlTagHelper : BaseFormTagHelper
    {
        const string SelectItemsAttributeName = "asp-items";
        const string AppendHintAttributeName = "sm-append-hint";
        const string IgnoreLabelAttributeName = "sm-ignore-label";
        const string SwitchAttributeName = "sm-switch";
        const string ControlSizeAttributeName = "sm-control-size";
        const string PlaintextAttributeName = "sm-plaintext";
        protected const string RequiredAttributeName = "sm-required";

        private readonly ILocalizationService _localizationService;

        public FormControlTagHelper(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }

        // Must comer AFTER AspNetCore original TagHelper (Input | Select | TextArea)
        public override int Order => 100;

        [HtmlAttributeName(RequiredAttributeName)]
        public bool? IsRequired { get; set; }

        [HtmlAttributeName(AppendHintAttributeName)]
        public bool AppendHint { get; set; }

        [HtmlAttributeName(SwitchAttributeName)]
        public bool AsSwitch { get; set; } = true;

        [HtmlAttributeName(IgnoreLabelAttributeName)]
        public bool IgnoreLabel { get; set; }

        /// <summary>
        /// Adds .form-control-plaintext instead of .form-control to input
        /// </summary>
        [HtmlAttributeName(PlaintextAttributeName)]
        public bool Plaintext { get; set; }

        [HtmlAttributeName(ControlSizeAttributeName)]
        public ControlSize ControlSize { get; set; } = ControlSize.Medium;

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            if (IsRequired == null && For?.Metadata?.IsRequired == true)
            {
                IsRequired = true;
            }

            switch (output.TagName)
            {
                case "select":
                case "textarea":
                    ProcessFormControl(context, output);
                    break;
                case "input":
                    if (output.Attributes.TryGetAttribute("type", out var typeAttr))
                    {
                        if (typeAttr.Value.Equals("checkbox") && AsSwitch)
                        {
                            ProcessSwitch(context, output);
                        }
                        else if (typeAttr.Value is ("checkbox" or "radio"))
                        {
                            ProcessCheckRadio(context, output);
                        }
                        else if (typeAttr.Value is not ("file" or "hidden"))
                        {
                            ProcessFormControl(context, output);
                        }
                    }
                    break;
            }
        }

        private void ProcessCheckRadio(TagHelperContext context, TagHelperOutput output)
        {
            if (IgnoreLabel)
            {
                return;
            }

            output.AppendCssClass("form-check-input");

            var id = output.Attributes["id"]?.ValueAsString();
            var label = For?.Metadata?.DisplayName;

            if (label.HasValue())
            {
                output.PostElement.AppendHtml($"<label class=\"form-check-label\" for=\"{id}\">{label}</label>");
            }

            if (!context.Items.ContainsKey("FormCheckOutput"))
            {
                output.PreElement.AppendHtml("<div class=\"form-check\">");
                output.PostElement.AppendHtml("</div>");
            }

            ProcessHint(output);
        }

        private void ProcessSwitch(TagHelperContext context, TagHelperOutput output)
        {
            if (context.Items.TryGetValue("FormCheckOutput", out var value) && value is TagHelperOutput formCheckOutput)
            {
                if (!formCheckOutput.Attributes["class"].ValueAsString().Contains("form-switch"))
                {
                    // Add the switch class only if it is not present
                    // on parent check already. In this case, we assume that the UI dev
                    // has an "idea" already.
                    formCheckOutput.AppendCssClass("form-switch");
                }

                output.AppendCssClass("form-check-input");

                ProcessHint(formCheckOutput);
            }
            else
            {
                output.PreElement.AppendHtml("<div class=\"form-check form-check-solo form-check-warning form-switch form-switch-lg\">");
                output.AppendCssClass("form-check-input");
                output.PostElement.AppendHtml("</div>");

                ProcessHint(output);
            } 
        }

        private void ProcessFormControl(TagHelperContext context, TagHelperOutput output)
        {
            bool isPlainText;
            if (Plaintext)
            {
                output.AppendCssClass("form-control-plaintext");
                isPlainText = true;
            }
            else
            {
                isPlainText = output.Attributes.TryGetAttribute("class", out var classAttr) && classAttr.ValueAsString().Contains("form-control-plaintext");
            }

            if (isPlainText)
            {
                // Remove .form-control class added by SmartHtmlGenerator
                output.RemoveClass("form-control", HtmlEncoder.Default);
            }
            else
            {
                // INFO: SmartHtmlGenerator applies .form-control now globally, but still we
                // have tons of select tags that do not trigger the generator.
                if (output.TagName == "select")
                {
                    output.AppendCssClass("form-control");
                }

                // Render "Optional/Unspecified" placeholder
                if (IsRequired == false && output.TagName != "select" && !output.Attributes.ContainsName("placeholder"))
                {
                    output.Attributes.Add("placeholder", _localizationService.GetResource("Common.Optional", logIfNotFound: false, returnEmptyIfNotFound: true));
                }

                // Render "required" attribute
                if (IsRequired == true && !output.Attributes.ContainsName("required"))
                {
                    output.Attributes.Add(new TagHelperAttribute("required", null, HtmlAttributeValueStyle.Minimized));
                }
            }


            if (ControlSize != ControlSize.Medium && !context.Items.ContainsKey("IsNumberInput"))
            {
                output.AppendCssClass("form-control-" + (ControlSize == ControlSize.Small ? "sm" : "lg"));
            }

            // Render hint as .form-text
            ProcessHint(output);
        }

        private void ProcessHint(TagHelperOutput output)
        {
            if (AppendHint)
            {
                var hintText = For?.Metadata?.Description;

                if (hintText.HasValue())
                {
                    // Append hint element to control
                    var div = new TagBuilder("small");
                    div.Attributes.Add("class", "form-text text-muted");
                    div.InnerHtml.SetContent(hintText);

                    output.PostElement.AppendHtml(div);
                }
            }
        }
    }
}
