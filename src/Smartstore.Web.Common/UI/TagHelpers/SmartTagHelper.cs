using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Utilities;

namespace Smartstore.Web.UI.TagHelpers
{
	public abstract class SmartTagHelper : TagHelper
	{
        public override void Init(TagHelperContext context)
        {
            var requestServices = ViewContext.HttpContext.RequestServices;

            ActionContextAccessor = requestServices.GetRequiredService<IActionContextAccessor>();
            HtmlHelper = requestServices.GetRequiredService<IHtmlHelper>();

            if (HtmlHelper is IViewContextAware contextAware)
            {
                contextAware.Contextualize(ViewContext);
            }
        }

        [HtmlAttributeNotBound]
		[ViewContext]
		public ViewContext ViewContext { get; set; }

        [HtmlAttributeNotBound]
        protected IHtmlHelper HtmlHelper { get; set; }

        [HtmlAttributeNotBound]
        protected IActionContextAccessor ActionContextAccessor { get; set; }

        [HtmlAttributeNotBound]
        protected TagHelperOutput Output { get; set; }

        /// <summary>
        /// Gets or sets the identifier for the control.
        /// </summary>
        public string Id { get; set; }

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
            Id = Id.NullEmpty()?.SanitizeHtmlId() ?? GenerateTagId(context);
            
            if (Id.HasValue())
            {
                output.Attributes.Add("id", Id);
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
