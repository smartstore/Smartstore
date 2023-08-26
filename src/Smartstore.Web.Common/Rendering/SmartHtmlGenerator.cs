using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;

namespace Smartstore.Web.Rendering
{
    /// <summary>
    /// Subclass of <see cref="DefaultHtmlGenerator"/> that adds some Bootstrap classes to generated Html.
    /// </summary>
    public class SmartHtmlGenerator : DefaultHtmlGenerator
    {
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

            if (inputType is (InputType.Text or InputType.Password))
            {
                if (htmlAttributes == null || (htmlAttributes.TryGetValueAs<string>("type", out var strType) && strType is ("text" or "password")))
                {
                    tag.Attributes.AddInValue("class", ' ', "form-control");
                }
            }

            return tag;
        }
    }
}
