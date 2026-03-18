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
        private readonly static HashSet<string> _nonInputTypes = new() { "button", "checkbox", "file", "hidden", "image", "radio", "range", "reset", "search", "submit" };
        
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
            if (inputType == InputType.CheckBox && IsBooleanModel(modelExplorer))
            {
                TryNormalizeCheckboxModelState(viewContext, expression);

                try
                {
                    return GenerateInputCore(
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
                }
                catch (FormatException)
                {
                    var key = viewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(expression);
                    if (viewContext.ModelState.Remove(key))
                    {
                        return GenerateInputCore(
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
                    }

                    throw;
                }
            }

            return GenerateInputCore(
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
        }

        private TagBuilder GenerateInputCore(
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
                if (!tag.Attributes.TryGetValue("type", out var strType) && htmlAttributes != null)
                {
                    htmlAttributes.TryGetValueAs("type", out strType);
                }

                if (strType.IsEmpty() || !_nonInputTypes.Contains(strType))
                {
                    tag.Attributes.AddInValue("class", ' ', "form-control");
                }
            }

            return tag;
        }

        private static bool IsBooleanModel(ModelExplorer modelExplorer)
        {
            var t = modelExplorer?.ModelType;
            return t == typeof(bool) || t == typeof(bool?);
        }

        private static void TryNormalizeCheckboxModelState(ViewContext viewContext, string expression)
        {
            var key = viewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(expression);
            if (!viewContext.ModelState.TryGetValue(key, out var entry))
            {
                return;
            }

            var attempted = entry.AttemptedValue;
            if (attempted.IsEmpty())
            {
                return;
            }

            var v = attempted.Trim();
            if (v == "1" || v.EqualsNoCase("on") || v.EqualsNoCase("yes"))
            {
                viewContext.ModelState.SetModelValue(key, new ValueProviderResult("true"));
            }
            else if (v == "0" || v.EqualsNoCase("no"))
            {
                viewContext.ModelState.SetModelValue(key, new ValueProviderResult("false"));
            }
        }
    }
}
