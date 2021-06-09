using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Smartstore.Core.Widgets;
using Smartstore.Web.Rendering;

namespace Smartstore.Web.TagHelpers.Shared
{
    [HtmlTargetElement("entity-picker", TagStructure = TagStructure.WithoutEndTag)]
    public class EntityPickerTagHelper : BaseFormTagHelper
    {
        // TODO: (mh) (core) Move to Admin namespace.
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

        // TODO: (mh) (core) Remove comment after review. 
        // INFO: LanguageId was removed because it was never handelled and thus never used in classic code.
        // Also it doesn't make sense setting the language for this control explicitly

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

        protected override async Task ProcessCoreAsync(TagHelperContext context, TagHelperOutput output)
        {
            // TODO: (mh) (core) Don't suppress output, BUILD output here (instead of relying on a view component).
            // It's just a button and a tiny script.
            
            output.SuppressOutput();

            var model = new EntityPickerConfigurationModel
            {
                AppendMode = AppendMode,
                Caption = Caption,
                Delimiter = Delimiter,
                DialogTitle = DialogTitle,
                DisableBundleProducts = DisableBundleProducts,
                DisabledEntityIds = DisabledEntityIds,
                DisableGroupedProducts = DisableGroupedProducts,
                EnableThumbZoomer = EnableThumbZoomer,
                EntityType = EntityType,
                FieldName = FieldName,
                HighlightSearchTerm = HighlightSearchTerm,
                IconCssClass = IconCssClass,
                MaxItems = MaxItems,
                OnDialogLoadedHandlerName = OnDialogLoadedHandler,
                OnDialogLoadingHandlerName = OnDialogLoadingHandler,
                OnSelectionCompletedHandlerName = OnSelectionCompletedHandler,
                Selected = Selected,
                TargetInputSelector = TargetInputSelector
            };

            if (For != null)
            {
                TargetInputSelector = "#" + HtmlHelper.GenerateIdFromName(For.Name);
            }

            var widget = new ComponentWidgetInvoker("EntityPicker", new { model });
            var partial = await widget.InvokeAsync(ViewContext);

            output.TagMode = TagMode.StartTagAndEndTag;
            output.TagName = null;
            output.Content.SetHtmlContent(partial);
        }
    }
}
