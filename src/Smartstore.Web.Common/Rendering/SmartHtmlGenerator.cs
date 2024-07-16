using System.Collections.Frozen;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using Smartstore.Core.Identity;

namespace Smartstore.Web.Rendering
{
    /// <summary>
    /// Subclass of <see cref="DefaultHtmlGenerator"/> that adds some Bootstrap classes to generated Html.
    /// </summary>
    public class SmartHtmlGenerator : DefaultHtmlGenerator
    {
        private readonly static FrozenSet<string> _nonInputTypes = 
            new string[] { "button", "checkbox", "file", "hidden", "image", "radio", "range", "reset", "search", "submit" }.ToFrozenSet();
        
        public SmartHtmlGenerator(
            IAntiforgery antiforgery,
            IOptions<MvcViewOptions> optionsAccessor,
            IModelMetadataProvider metadataProvider,
            IUrlHelperFactory urlHelperFactory,
            HtmlEncoder htmlEncoder,
            ValidationHtmlAttributeProvider validationAttributeProvider)
            : base(
                  antiforgery,
                  optionsAccessor,
                  metadataProvider,
                  urlHelperFactory,
                  htmlEncoder,
                  validationAttributeProvider)
        {
        }

        #region Static helpers

        public static TagBuilder GenerateConsentableScript(bool consented, CookieType consentType, string src)
        {
            // TODO: (mh) Call this method where applicable (instead of the ugly string concat mess)
            Guard.NotEmpty(src);

            var script = new TagBuilder("script");
            if (consented)
            {
                script.Attributes["src"] = src;
            }
            else
            {
                script.Attributes["data-src"] = src;
                // TODO: (mh) Check casing/dasherization
                script.Attributes["data-consent"] = consentType.ToString().ToLowerInvariant();
            }

            return script;
        }

        public static TagBuilder GenerateConsentableInlineScript(bool consented, CookieType consentType, string code)
        {
            // TODO: (mh) Call this method where applicable (instead of the ugly string concat mess)
            Guard.NotEmpty(code);

            var script = new TagBuilder("script");
            script.InnerHtml.AppendHtml(code);

            if (!consented)
            {
                script.Attributes["type"] = "text/plain";
                // TODO: (mh) Check casing/dasherization
                script.Attributes["data-consent"] = consentType.ToString().ToLowerInvariant();
            }

            return script;
        }

        #endregion

        public override TagBuilder GenerateTextArea(
            ViewContext viewContext,
            ModelExplorer modelExplorer,
            string expression,
            int rows,
            int columns,
            object htmlAttributes)
        {
            var tag = base.GenerateTextArea(
                viewContext,
                modelExplorer,
                expression,
                rows,
                columns,
                htmlAttributes);

            tag.Attributes.AddInValue("class", ' ', "form-control");

            return tag;
        }

        public override TagBuilder GenerateSelect(
            ViewContext viewContext,
            ModelExplorer modelExplorer,
            string optionLabel,
            string expression,
            IEnumerable<SelectListItem> selectList,
            ICollection<string> currentValues,
            bool allowMultiple,
            object htmlAttributes)
        {
            var tag = base.GenerateSelect(
                viewContext,
                modelExplorer,
                optionLabel,
                expression,
                selectList,
                currentValues,
                allowMultiple,
                htmlAttributes);

            tag.Attributes.AddInValue("class", ' ', "form-control");

            return tag;
        }

        protected override TagBuilder GenerateInput(
            ViewContext viewContext,
            InputType inputType,
            ModelExplorer modelExplorer,
            string expression,
            object value,
            bool useViewData,
            bool isChecked,
            bool setId,
            bool isExplicitValue,
            string format,
            IDictionary<string, object> htmlAttributes)
        {
            var tag = base.GenerateInput(
                viewContext,
                inputType,
                modelExplorer,
                expression,
                value,
                useViewData,
                isChecked,
                setId,
                isExplicitValue,
                format,
                htmlAttributes);
            
            if (inputType is InputType.Text or InputType.Password)
            {
                // Determine input type attr value first from generated tag, then from htmlAttributes
                if (!tag.Attributes.TryGetValue("type", out var strType) && htmlAttributes != null)
                {
                    htmlAttributes.TryGetValueAs("type", out strType);
                }
                
                if (strType.IsEmpty() || !_nonInputTypes.Contains(strType))
                {
                    // Add .form-control to text, password, number..., but not to hidden, checkbox, radio...
                    tag.Attributes.AddInValue("class", ' ', "form-control");
                }
            }

            return tag;
        }
    }
}
