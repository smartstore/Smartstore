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
    [OutputElementHint("button")]
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
        private readonly IUrlHelper _urlHelper;

        public EntityPickerTagHelper(IWidgetProvider widgetProvider, IUrlHelper urlHelper)
        {
            _widgetProvider = widgetProvider;
            _urlHelper = urlHelper;
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
                url = _urlHelper.Action("Picker", "Entity", new { area = "" }),
                caption =  HtmlHelper.Encode(DialogTitle.NullEmpty() ?? Caption),
                disableIf = DisableGroupedProducts ? "groupedproduct" : (DisableBundleProducts ? "notsimpleproduct" : null),
                disableIds = DisabledEntityIds == null ? null : string.Join(",", DisabledEntityIds),
                thumbZoomer = EnableThumbZoomer,
                highligtSearchTerm = HighlightSearchTerm,
                returnField = FieldName,
                delim = Delimiter,
                targetInput = TargetInputSelector.HasValue() ? $"'{TargetInputSelector}'" : null,
                selected = Selected != null && Selected.Length > 0 ? $"[{string.Join(Delimiter, Selected)}]" : null,
                appendMode = AppendMode,
                maxItems = MaxItems,
                onDialogLoading = OnDialogLoadingHandler,
                onDialogLoaded = OnDialogLoadedHandler,
                onSelectionCompleted = OnSelectionCompletedHandler
            };

            var buttonId = "entpicker-toggle-" + CommonHelper.GenerateRandomInteger();

            var toogleButton = new TagBuilder("button");
            toogleButton.Attributes.Add("id", buttonId);
            toogleButton.Attributes.Add("type", "button");
            toogleButton.AddCssClass("btn btn-secondary");

            if (IconCssClass.HasValue())
            {
                toogleButton.InnerHtml.AppendHtml($"<i class='{ IconCssClass }'></i>");
            }

            if (Caption.HasValue())
            {
                toogleButton.InnerHtml.AppendHtml($"<span>{ Caption }</span>");
            }

            output.TagMode = TagMode.StartTagAndEndTag;
            output.Content.AppendHtml(toogleButton);

            var json = JsonConvert.SerializeObject(options, new JsonSerializerSettings
            {
                ContractResolver = SmartContractResolver.Instance,
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Ignore
            });

            using var psb = StringBuilderPool.Instance.Get(out var sb);
            sb.Append("<script data-origin='EntityPicker'>");
            sb.Append("$(function () { $('#" + buttonId + "').entityPicker(");
            sb.Append(json);
            sb.Append("); });");
            sb.Append("</script>");

            _widgetProvider.RegisterHtml("scripts", new HtmlString(sb.ToString()));
        }
    }
}
