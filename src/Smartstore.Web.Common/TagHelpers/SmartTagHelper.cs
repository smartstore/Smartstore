using Autofac;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Localization;
using Smartstore.Utilities;

namespace Smartstore.Web.TagHelpers
{
    public abstract class SmartTagHelper : TagHelper
    {
        private IActionContextAccessor _actionContextAccessor;
        private IHtmlHelper _htmlHelper;
        private IUrlHelper _urlHelper;
        private IHtmlGenerator _htmlGenerator;
        private Localizer _localizer;

        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; }

        [HtmlAttributeNotBound]
        protected internal IActionContextAccessor ActionContextAccessor
        {
            get => _actionContextAccessor ??= ViewContext.HttpContext.GetServiceScope().Resolve<IActionContextAccessor>();
        }


        [HtmlAttributeNotBound]
        protected internal IUrlHelper UrlHelper
        {
            get => _urlHelper ??= ViewContext.HttpContext.GetServiceScope().Resolve<IUrlHelperFactory>().GetUrlHelper(ActionContextAccessor.ActionContext);
        }

        [HtmlAttributeNotBound]
        protected internal IHtmlHelper HtmlHelper
        {
            get
            {
                if (_htmlHelper == null)
                {
                    _htmlHelper = ViewContext.HttpContext.GetServiceScope().Resolve<IHtmlHelper>();
                    if (_htmlHelper is IViewContextAware contextAware)
                    {
                        contextAware.Contextualize(ViewContext);
                    }
                }

                return _htmlHelper;
            }
        }

        [HtmlAttributeNotBound]
        protected internal IHtmlGenerator HtmlGenerator
        {
            get => _htmlGenerator ??= ViewContext.HttpContext.GetServiceScope().Resolve<IHtmlGenerator>();
        }

        [HtmlAttributeNotBound]
        protected internal Localizer T
        {
            get => _localizer ??= ViewContext.HttpContext.GetServiceScope().Resolve<Localizer>();
        }

        [HtmlAttributeNotBound]
        public TagHelperOutput Output { get; set; }

        /// <summary>
        /// Gets or sets the identifier for the tag.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// HTML tag id auto generator. Only called when <see cref="Id"/> is null or empty.
        /// </summary>
        protected virtual string GenerateTagId(TagHelperContext context)
            => context.TagName + '-' + CommonHelper.GenerateRandomDigitCode(10);

        public override sealed void Process(TagHelperContext context, TagHelperOutput output)
        {
            Output = output;
            ProcessCommon(context, output);
            ProcessCore(context, output);
        }

        public override sealed Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            Output = output;
            ProcessCommon(context, output);
            return ProcessCoreAsync(context, output);
        }

        private void ProcessCommon(TagHelperContext context, TagHelperOutput output)
        {
            Id = Id.NullEmpty() ?? GenerateTagId(context);

            if (Id.HasValue())
            {
                output.Attributes.SetAttribute("id", Id);
            }
        }

        /// <summary>
        /// The core process method.
        /// </summary>
        /// <param name="context">Contains information associated with the current HTML tag.</param>
        /// <param name="output">A stateful HTML element used to generate an HTML tag.</param>
        protected virtual void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
        }

        /// <summary>
        /// The core process method.
        /// </summary>
        /// <param name="context">Contains information associated with the current HTML tag.</param>
        /// <param name="output">A stateful HTML element used to generate an HTML tag.</param>
        protected virtual Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            ProcessCore(context, output);
            return Task.CompletedTask;
        }
    }
}
