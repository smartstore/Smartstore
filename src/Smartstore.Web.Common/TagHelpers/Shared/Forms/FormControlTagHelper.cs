using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
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

        // Must comer AFTER AspNetCore origin TagHelper (Input | Select | TextArea)
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
            IsRequired ??= For?.Metadata.IsRequired;

            switch (output.TagName)
            {
                case "select":
                case "textarea":
                    ProcessFormControl(output);
                    break;
                case "input":
                    if (output.Attributes.TryGetAttribute("type", out var typeAttr))
                    {
                        if (typeAttr.Value.Equals("checkbox") && AsSwitch)
                        {
                            ProcessSwitch(output);
                        }
                        else if (typeAttr.Value is ("checkbox" or "radio"))
                        {
                            ProcessCheckRadio(output, typeAttr.Value.ToString());
                        }
                        else if (typeAttr.Value is not ("file" or "hidden"))
                        {
                            ProcessFormControl(output);
                        }
                    }
                    break;
            }
        }

        private void ProcessCheckRadio(TagHelperOutput output, string type)
        {
            output.AppendCssClass("form-check-input");

            if (!IgnoreLabel)
            {
                var id = output.Attributes["id"]?.Value?.ToString();
                var label = For?.Metadata?.DisplayName;

                if (label.HasValue())
                {
                    output.PostElement.AppendHtml($"<label class=\"form-check-label\" for=\"{id}\">{label}</label>");
                }

                output.PreElement.AppendHtml("<div class=\"form-check\">");
                output.PostElement.AppendHtml("</div>");

                ProcessHint(output);
            }
        }

        private void ProcessSwitch(TagHelperOutput output)
        {
            output.PreElement.AppendHtml("<label class=\"switch\">");
            output.PostElement.AppendHtml("<span class=\"switch-toggle\"></span></label>");

            ProcessHint(output);
        }

        private void ProcessFormControl(TagHelperOutput output)
        {
            bool isPlainText;
            if (Plaintext)
            {
                output.AppendCssClass("form-control-plaintext");
                isPlainText = true;
            }
            else
            {
                isPlainText = output.Attributes.TryGetAttribute("class", out var classAttr) && classAttr.Value.ToString().Contains("form-control-plaintext");
            }
            
            if (!isPlainText)
            {
                output.AppendCssClass("form-control");

                // Render "Optional" placeholder
                if (IsRequired == false && !output.Attributes.ContainsName("placeholder"))
                {
                    output.Attributes.Add("placeholder", _localizationService.GetResource("Common.Optional", logIfNotFound: false, returnEmptyIfNotFound: true));
                }

                // Render "required" attribute
                if (IsRequired == true && !output.Attributes.ContainsName("required"))
                {
                    output.Attributes.Add(new TagHelperAttribute("required", null, HtmlAttributeValueStyle.Minimized));
                }
            }
            

            if (ControlSize != ControlSize.Medium)
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
