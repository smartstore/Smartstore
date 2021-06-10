using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;
using Smartstore.ComponentModel;
using Smartstore.Core.Widgets;
using Smartstore.Utilities;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("entity-picker", TagStructure = TagStructure.WithoutEndTag)]
    public class EntityPickerTagHelper : BaseFormTagHelper
    {
        const string EntityTypeAttributeName = "entity-type";
        const string TargetInputSelectorAttributeName = "target-input-selector";
        const string CaptionAttributeName = "caption";
        const string IconCssClassAttributeName = "icon-css-class";
        const string DialogTitleAttributeName = "dialog-title";
        const string DisableGroupedProductsAttributeName = "disable-grouped-products";
        const string DisableBundleProductsAttributeName = "disable-bundle-products";
        const string DisabledEntityIdsAttributeName = "disabled-entity-ids";
        const string SelectedAttributeName = "selected";
        const string EnableThumbZoomerAttributeName = "enable-thumb-zoomer";
        const string HighlightSearchTermAttributeName = "highlight-search-term";
        const string MaxItemsAttributeName = "max-items";
        const string AppendModeAttributeName = "append-mode";
        const string DelimiterAttributeName = "delimiter";
        const string FieldNameAttributeName = "field-name";
        const string OnDialogLoadingAttributeName = "ondialogloading";
        const string OnDialogLoadedAttributeName = "ondialogloaded";
        const string OnSelectionCompletedAttributeName = "onselectioncompleted";

        private readonly IWidgetProvider _widgetProvider;

        public EntityPickerTagHelper(IWidgetProvider widgetProvider)
        {
            _widgetProvider = widgetProvider;
        }
        
        /// <summary>
        /// Sets the entity type which shall be picked. Default = "product"
        /// </summary>
        [HtmlAttributeName(EntityTypeAttributeName)]
        public string EntityType { get; set; } = "product";

        /// <summary>
        /// Sets the target input selector.
        /// </summary>
        [HtmlAttributeName(TargetInputSelectorAttributeName)]
        public string TargetInputSelector { get; set; }

        /// <summary>
        /// Sets the caption of the dialog.
        /// </summary>
        [HtmlAttributeName(CaptionAttributeName)]
        public string Caption { get; set; }

        /// <summary>
        /// Sets the icon of the button which opens the dialog. Default = "fa fa-search"
        /// </summary>
        [HtmlAttributeName(IconCssClassAttributeName)]
        public string IconCssClass { get; set; } = "fa fa-search";

        /// <summary>
        /// Sets the title of the dialog.
        /// </summary>
        [HtmlAttributeName(DialogTitleAttributeName)]
        public string DialogTitle { get; set; }

        /// <summary>
        /// Whether to disable search for grouped products.
        /// </summary>
        [HtmlAttributeName(DisableGroupedProductsAttributeName)]
        public bool DisableGroupedProducts { get; set; }

        /// <summary>
        /// Whether to disable search for bundled products.
        /// </summary>
        [HtmlAttributeName(DisableBundleProductsAttributeName)]
        public bool DisableBundleProducts { get; set; }

        /// <summary>
        /// The ids of disabled entities.
        /// </summary>
        [HtmlAttributeName(DisabledEntityIdsAttributeName)]
        public int[] DisabledEntityIds { get; set; }


        // TODO: (mh) (core) Check & add doku from here.

        /// <summary>
        /// The ids of selected entities. 
        /// </summary>
        [HtmlAttributeName(SelectedAttributeName)]
        public string[] Selected { get; set; }

        [HtmlAttributeName(EnableThumbZoomerAttributeName)]
        public bool EnableThumbZoomer { get; set; }

        [HtmlAttributeName(HighlightSearchTermAttributeName)]
        public bool HighlightSearchTerm { get; set; } = true;

        [HtmlAttributeName(MaxItemsAttributeName)]
        public int MaxItems { get; set; }

        [HtmlAttributeName(AppendModeAttributeName)]
        public bool AppendMode { get; set; } = true;

        [HtmlAttributeName(DelimiterAttributeName)]
        public string Delimiter { get; set; } = ",";

        [HtmlAttributeName(FieldNameAttributeName)]
        public string FieldName { get; set; } = "id";

        [HtmlAttributeName(OnDialogLoadingAttributeName)]
        public string OnDialogLoadingHandler { get; set; }

        [HtmlAttributeName(OnDialogLoadedAttributeName)]
        public string OnDialogLoadedHandler { get; set; }

        [HtmlAttributeName(OnSelectionCompletedAttributeName)]
        public string OnSelectionCompletedHandler { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            if (For != null)
            {
                TargetInputSelector = "#" + HtmlHelper.GenerateIdFromName(For.Name);
            }
            
            var options = new
            {
                entityType = EntityType,
                url = UrlHelper.Action("Picker", "Entity", new { area = string.Empty }),
                caption =  (DialogTitle.NullEmpty() ?? Caption).HtmlEncode(),
                disableIf = DisableGroupedProducts ? "groupedproduct" : (DisableBundleProducts ? "notsimpleproduct" : null),
                disableIds = DisabledEntityIds == null ? null : string.Join(',', DisabledEntityIds),
                thumbZoomer = EnableThumbZoomer,
                highligtSearchTerm = HighlightSearchTerm,
                returnField = FieldName,
                delim = Delimiter,
                targetInput = TargetInputSelector,
                selected = Selected,
                appendMode = AppendMode,
                maxItems = MaxItems,
                onDialogLoading = OnDialogLoadingHandler,
                onDialogLoaded = OnDialogLoadedHandler,
                onSelectionCompleted = OnSelectionCompletedHandler
            };

            var buttonId = "entpicker-toggle-" + CommonHelper.GenerateRandomInteger();

            // INFO: (mh) (core) We talked about this in great detail!! Don't erase output just to replace it!!!
            output.TagName = "button";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.MergeAttribute("id", buttonId);
            output.MergeAttribute("type", "button");
            output.AppendCssClass("btn btn-secondary");

            if (IconCssClass.HasValue())
            {
                output.Content.AppendHtml($"<i class='{ IconCssClass }'></i>");
            }

            if (Caption.HasValue())
            {
                output.Content.AppendHtml($"<span>{ Caption }</span>");
            }

            var json = JsonConvert.SerializeObject(options, new JsonSerializerSettings
            {
                ContractResolver = SmartContractResolver.Instance,
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore
            });

            // INFO: (mh) (core) Don't render this init script in a zone, because we cannot be sure whether the request is AJAX or not.
            // INFO: (mh) (core) Use pooled StringBuilder for large strings only
            // INFO: (mh) (core) Built script string should be human-readable
            output.PostElement.AppendHtmlLine(@$"<script data-origin='EntityPicker'>$(function() {{ $('#{buttonId}').entityPicker({json}); }})</script>");
        }
    }
}
