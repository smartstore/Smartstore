using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Newtonsoft.Json;
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
        /// Sets the icon of the button which opens the dialog, Default: "fa fa-search"
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

        /// <summary>
        /// The ids of selected entities. 
        /// </summary>
        [HtmlAttributeName(SelectedAttributeName)]
        public string[] Selected { get; set; }

        /// <summary>
        /// Whether to enable thumb zoomer.
        /// </summary>
        [HtmlAttributeName(EnableThumbZoomerAttributeName)]
        public bool EnableThumbZoomer { get; set; }

        /// <summary>
        /// Whether to highlight search term in serach results. Default = true
        /// </summary>
        [HtmlAttributeName(HighlightSearchTermAttributeName)]
        public bool HighlightSearchTerm { get; set; } = true;

        /// <summary>
        /// Maximum number of selectable items.
        /// </summary>
        [HtmlAttributeName(MaxItemsAttributeName)]
        public int MaxItems { get; set; }

        /// <summary>
        /// Whether to append selected entity ids to already choosen entities. Default = true
        /// </summary>
        [HtmlAttributeName(AppendModeAttributeName)]
        public bool AppendMode { get; set; } = true;

        /// <summary>
        /// The delemiter for choosen entity ids. Default = ","
        /// </summary>
        [HtmlAttributeName(DelimiterAttributeName)]
        public string Delimiter { get; set; } = ",";

        /// <summary>
        /// FieldName of the input element to paste the selected ids in. Default = "id"
        /// </summary>
        [HtmlAttributeName(FieldNameAttributeName)]
        public string FieldName { get; set; } = "id";

        /// <summary>
        /// Name of the JavaScript function to call before the dialog is loaded.
        /// </summary>
        [HtmlAttributeName(OnDialogLoadingAttributeName)]
        public string OnDialogLoadingHandler { get; set; }

        /// <summary>
        /// Name of the JavaScript function to call after the dialog is loaded.
        /// </summary>
        [HtmlAttributeName(OnDialogLoadedAttributeName)]
        public string OnDialogLoadedHandler { get; set; }

        /// <summary>
        /// Name of the JavaScript function to call after the entity selection was done.
        /// </summary>
        [HtmlAttributeName(OnSelectionCompletedAttributeName)]
        public string OnSelectionCompletedHandler { get; set; }

        protected override void ProcessCore(TagHelperContext context, TagHelperOutput output)
        {
            if (For != null)
            {
                TargetInputSelector = "#" + HtmlHelper.Id(For.Name);
            }

            var options = new
            {
                entityType = EntityType,
                url = UrlHelper.Action("Picker", "Entity", new { area = string.Empty }),
                caption = (DialogTitle.NullEmpty() ?? Caption).HtmlEncode(),
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

            output.TagName = "button";
            output.TagMode = TagMode.StartTagAndEndTag;
            output.MergeAttribute("id", buttonId);
            output.MergeAttribute("type", "button");
            output.AppendCssClass("btn btn-secondary");

            if (IconCssClass.HasValue())
            {
                output.Content.AppendHtml($"<i class='{IconCssClass}'></i>");

                if (Caption.IsEmpty())
                {
                    output.AppendCssClass("btn-icon");
                }
            }

            if (Caption.HasValue())
            {
                output.Content.AppendHtml($"<span>{Caption}</span>");
            }

            var json = JsonConvert.SerializeObject(options, Formatting.None);

            output.PreElement.AppendHtmlLine(@$"<script data-origin='EntityPicker'>$(function() {{ $('#{buttonId}').entityPicker({json}); }})</script>");
        }
    }
}
