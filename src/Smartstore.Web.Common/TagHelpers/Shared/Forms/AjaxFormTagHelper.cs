using System.Globalization;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Smartstore.Web.TagHelpers.Shared
{
    /// <summary>
    /// Enumerates the AJAX script insertion modes.
    /// </summary>
    public enum InsertionMode
    {
        /// <summary>
        /// Replace the element.
        /// </summary>
        Replace,

        /// <summary>
        /// Insert before the element.
        /// </summary>
        InsertBefore,

        /// <summary>
        /// Insert after the element.
        /// </summary>
        InsertAfter,

        /// <summary>
        /// Replace the entire element.
        /// </summary>
        ReplaceWith
    }

    [HtmlTargetElement("form", Attributes = AjaxAttributeName)]
    public class AjaxFormTagHelper : TagHelper
    {
        const string AjaxAttributeName = "sm-ajax";
        const string ConfirmAttributeName = "sm-confirm";
        const string OnBeginAttributeName = "sm-onbegin";
        const string OnCompleteAttributeName = "sm-oncomplete";
        const string OnFailureAttributeName = "sm-onfailure";
        const string OnSuccessAttributeName = "sm-onsuccess";
        const string AllowCacheAttributeName = "sm-allow-cache";
        const string LoadingElementIdAttributeName = "sm-loading-element-id";
        const string LoadingElementDurationAttributeName = "sm-loading-element-duration";
        const string UpdateTargetIdAttributeName = "sm-update-target-id";
        const string InsertionModeAttributeName = "sm-insertion-mode";

        /// <summary>
        /// Whether the form is an unobtrusive Ajax form.
        /// </summary>
        [HtmlAttributeName(AjaxAttributeName)]
        public bool IsAjax { get; set; }

        /// <summary>
        /// Message to display in a confirmation window before a request is submitted.
        /// </summary>
        [HtmlAttributeName(ConfirmAttributeName)]
        public string Confirm { get; set; }

        /// <summary>
        /// Name of the JavaScript function to call immediately before the page is updated.
        /// </summary>
        [HtmlAttributeName(OnBeginAttributeName)]
        public string OnBegin { get; set; }

        /// <summary>
        /// Name of the JavaScript function to call when response data has been instantiated but before the page is updated.
        /// </summary>
        [HtmlAttributeName(OnCompleteAttributeName)]
        public string OnComplete { get; set; }

        /// <summary>
        /// Name of the JavaScript function to call if the page update fails.
        /// </summary>
        [HtmlAttributeName(OnFailureAttributeName)]
        public string OnFailure { get; set; }

        /// <summary>
        /// Name of the JavaScript function to call after the page is successfully updated.
        /// </summary>
        [HtmlAttributeName(OnSuccessAttributeName)]
        public string OnSuccess { get; set; }

        [HtmlAttributeName(AllowCacheAttributeName)]
        public bool AllowCache { get; set; }

        /// <summary>
        /// The id attribute of an HTML element that is displayed while the Ajax function is loading.
        /// </summary>
        [HtmlAttributeName(LoadingElementIdAttributeName)]
        public string LoadingElementId { get; set; }

        /// <summary>
        /// A value, in milliseconds, that controls the duration of the animation when showing or hiding the loading element.
        /// </summary>
        [HtmlAttributeName(LoadingElementDurationAttributeName)]
        public int LoadingElementDuration { get; set; }

        /// <summary>
        /// The ID of the DOM element to update by using the response from the server.
        /// </summary>
        [HtmlAttributeName(UpdateTargetIdAttributeName)]
        public string UpdateTargetId { get; set; }

        /// <summary>
        /// The mode that specifies how to insert the response into the target DOM element.
        /// Default is <see cref="InsertionMode.Replace"/>.
        /// </summary>
        [HtmlAttributeName(InsertionModeAttributeName)]
        public InsertionMode InsertionMode { get; set; }

        [HtmlAttributeNotBound]
        internal string InsertionModeUnobtrusive
        {
            get
            {
                switch (this.InsertionMode)
                {
                    case InsertionMode.Replace:
                        return "replace";

                    case InsertionMode.InsertBefore:
                        return "before";

                    case InsertionMode.InsertAfter:
                        return "after";

                    case InsertionMode.ReplaceWith:
                        return "replace-with";
                }

                return ((int)this.InsertionMode).ToString(CultureInfo.InvariantCulture);
            }
        }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            if (!IsAjax || !output.Attributes.ContainsName("action"))
            {
                return;
            }

            output.MergeAttribute("data-ajax", "true", true);

            AddIfSpecified("data-ajax-url", output.Attributes["action"]?.ValueAsString());
            AddIfSpecified("data-ajax-method", output.Attributes["method"]?.ValueAsString() ?? "post");
            AddIfSpecified("data-ajax-confirm", Confirm);
            AddIfSpecified("data-ajax-begin", OnBegin);
            AddIfSpecified("data-ajax-complete", OnComplete);
            AddIfSpecified("data-ajax-failure", OnFailure);
            AddIfSpecified("data-ajax-success", OnSuccess);

            if (AllowCache)
            {
                AddIfSpecified("data-ajax-cache", "true");
            }

            if (LoadingElementId.HasValue())
            {
                AddIfSpecified("data-ajax-loading", LoadingElementId.EnsureStartsWith('#'));
                if (LoadingElementDuration > 0)
                {
                    output.MergeAttribute("data-ajax-loading-duration", LoadingElementDuration, true);
                }
            }

            if (UpdateTargetId.HasValue())
            {
                AddIfSpecified("data-ajax-update", UpdateTargetId.EnsureStartsWith('#'));
                AddIfSpecified("data-ajax-mode", InsertionModeUnobtrusive);
            }

            void AddIfSpecified(string name, string value)
            {
                if (value.HasValue())
                {
                    output.MergeAttribute(name, value, true);
                }
            }
        }
    }
}
