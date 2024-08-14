using AngleSharp;
using AngleSharp.Html.Dom;
using Autofac;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Smartstore.Core.Localization;
using Smartstore.Core.Platform.AI;
using Smartstore.Engine.Modularity;

namespace Smartstore.Web.Rendering
{
    public class DefaultAIToolHtmlGenerator : IAIToolHtmlGenerator
    {
        private readonly SmartDbContext _db;
        private readonly IAIProviderFactory _aiProviderFactory;
        private readonly ModuleManager _moduleManager;
        private readonly IUrlHelper _urlHelper;
        private IHtmlHelper _htmlHelper;
        private ViewContext _viewContext;

        public DefaultAIToolHtmlGenerator(
            SmartDbContext db,
            IAIProviderFactory aiProviderFactory,
            ModuleManager moduleManager, 
            IUrlHelper urlHelper)
        {
            _db = db;
            _aiProviderFactory = aiProviderFactory;
            _moduleManager = moduleManager;
            _urlHelper = urlHelper;
        }

        public Localizer T { get; set; } = NullLocalizer.Instance;

        public void Contextualize(ViewContext viewContext)
        {
            _viewContext = Guard.NotNull(viewContext);
        }

        protected internal IHtmlHelper HtmlHelper
        {
            get
            {
                CheckContextualized();
                
                if (_htmlHelper == null)
                {
                    _htmlHelper = _viewContext.HttpContext.GetServiceScope().Resolve<IHtmlHelper>();
                    if (_htmlHelper is IViewContextAware contextAware)
                    {
                        contextAware.Contextualize(_viewContext);
                    }
                }

                return _htmlHelper;
            }
        }

        // TODO: (mh) (ai) Very bad decision to pass and scrape the generated output HTML! TBD with MC.
        public TagBuilder GenerateTranslationTool(string localizedContent)
        {
            CheckContextualized();

            var providers = _aiProviderFactory.GetProviders(AIProviderFeatures.TextTranslation);
            if (providers.Count == 0)
            {
                return null;
            }

            string[] additionalItemClasses = ["ai-translator"];
            var inputGroupColDiv = CreateDialogOpener(true);
            var dropdownUl = new TagBuilder("ul");
            dropdownUl.Attributes["class"] = "dropdown-menu";

            // TODO: (mh) (ai) Dangerous! TBD with MC.
            var properties = ExtractLabelsAndIdsAsync(localizedContent).Await();

            foreach (var provider in providers)
            {
                // Add attributes from tag helper properties.
                var route = provider.Value.GetDialogRoute(AIDialogType.Translation);
                var routeUrl = _urlHelper.Action(route.Action, route.Controller, route.RouteValues);

                var dropdownLiTitle = T("Admin.AI.TranslateTextWith", _moduleManager.GetLocalizedFriendlyName(provider.Metadata)).ToString();
                var headingLi = new TagBuilder("li");
                headingLi.Attributes["class"] = "dropdown-header";
                headingLi.InnerHtml.AppendHtml(dropdownLiTitle);
                dropdownUl.InnerHtml.AppendHtml(headingLi);

                foreach (var prop in properties)
                {
                    var elementId = prop.Item1;
                    var label = prop.Item2;
                    var hasValue = prop.Item3;

                    var additionalClasses = hasValue ? additionalItemClasses : [.. additionalItemClasses, "disabled"];
                    var dropdownLi = CreateDropdownItem(label, true, string.Empty, true, additionalClasses);

                    var attrs = dropdownLi.Attributes;
                    attrs["data-provider-systemname"] = provider.Metadata.SystemName;
                    attrs["data-modal-url"] = routeUrl;
                    attrs["data-target-property"] = elementId;
                    attrs["data-modal-title"] = dropdownLiTitle + ": " + label;

                    dropdownUl.InnerHtml.AppendHtml(dropdownLi);
                }
            }

            inputGroupColDiv.InnerHtml.AppendHtml(dropdownUl);

            return inputGroupColDiv;
        }

        public TagBuilder GenerateTextCreationTool(AttributeDictionary attributes, bool hasContent)
        {
            CheckContextualized();

            var providers = _aiProviderFactory.GetProviders(AIProviderFeatures.TextTranslation);
            if (providers.Count == 0)
            {
                return null;
            }
            
            var inputGroupColDiv = CreateDialogOpener(true);

            var dropdownUl = new TagBuilder("ul");
            dropdownUl.Attributes["class"] = "dropdown-menu";

            // Create a button group for the providers. If there is only one provider, hide the button group.
            // INFO: The button group will be rendered hidden in order to have the same javascript initialization for all cases,
            // because the button contains all the necessary data attributes.
            var btnGroupLi = new TagBuilder("li");
            btnGroupLi.Attributes["class"] = "dropdown-group";

            var btnGroupDiv = new TagBuilder("div");
            btnGroupDiv.Attributes["class"] = "btn-group mb-2";

            if (providers.Count == 1)
            {
                btnGroupDiv.AppendCssClass("d-none");
            }

            var isFirstProvider = true;
            foreach (var provider in providers)
            {
                var btn = new TagBuilder("button");
                btn.Attributes["type"] = "button";
                btn.Attributes["class"] = "btn-ai-provider-chooser btn btn-secondary btn-sm";
                btn.Attributes["aria-haspopup"] = "true";
                btn.Attributes["aria-expanded"] = "false";

                if (isFirstProvider)
                {
                    btn.AppendCssClass("active");
                }

                btn.InnerHtml.AppendHtml(_moduleManager.GetLocalizedFriendlyName(provider.Metadata));
                AddTagHelperProperties(btn, provider, attributes, AIDialogType.Text);

                btnGroupDiv.InnerHtml.AppendHtml(btn);
                isFirstProvider = false;
            }

            btnGroupLi.InnerHtml.AppendHtml(btnGroupDiv);
            dropdownUl.InnerHtml.AppendHtml(btnGroupLi);
            CreateTextCreationOptionsDropdown(hasContent, dropdownUl);
            inputGroupColDiv.InnerHtml.AppendHtml(dropdownUl);

            return inputGroupColDiv;
        }

        /// <summary>
        /// Adds simple text creation option menu items to the dropdown.
        /// </summary>
        /// <param name="hasContent">Indicates whether the target property already has content. If it has we can offer options like: summarize, optimize etc.</param>
        /// <param name="dropdownUl">The UL tag to which the items are  appended.</param>
        private void CreateTextCreationOptionsDropdown(bool hasContent, TagBuilder dropdownUl)
        {
            // Create new always is enabled.
            var builder = dropdownUl.InnerHtml;
            builder.AppendHtml(CreateDropdownItem(T("Admin.AI.TextCreation.CreateNew"), true, "create-new", false, "ai-text-composer"));
            builder.AppendHtml(CreateDropdownItem(T("Admin.AI.TextCreation.Summarize"), hasContent, "summarize", false, "ai-text-composer"));
            builder.AppendHtml(CreateDropdownItem(T("Admin.AI.TextCreation.Improve"), hasContent, "improve", false, "ai-text-composer"));
            builder.AppendHtml(CreateDropdownItem(T("Admin.AI.TextCreation.Simplify"), hasContent, "simplify", false, "ai-text-composer"));
            builder.AppendHtml(CreateDropdownItem(T("Admin.AI.TextCreation.Extend"), hasContent, "extend", false, "ai-text-composer"));

            // Add "Change style" & "Change tone" options from module settings.
            AddMenuItemsFromSetting(dropdownUl, hasContent, "change-style");
            AddMenuItemsFromSetting(dropdownUl, hasContent, "change-tone");
        }

        /// <summary>
        /// Adds menu items from module settings to the dropdown.
        /// </summary>
        /// <param name="dropdownUl">The dropdown to append items to.</param>
        /// <param name="hasContent">Defines whether the target field has a value. If there is no value to manipulate the items will be displayed disabled.</param>
        /// <param name="command">The command type choosen by the user. It can be "change-style" or "change-tone".</param>
        private void AddMenuItemsFromSetting(TagBuilder dropdownUl, bool hasContent, string command)
        {
            var settingName = command == "change-style" ? "AISettings.AvailableTextCreationStyles" : "AISettings.AvailableTextCreationTones";
            var title = command == "change-style" ? T("Admin.AI.MenuItemTitle.ChangeStyle").Value : T("Admin.AI.MenuItemTitle.ChangeTone").Value;

            var setting = _db.Settings.FirstOrDefault(x => x.Name == settingName);
            if (setting != null && setting.Value.HasValue())
            {
                var providerDropdownItemLi = CreateDropdownItem(title);
                providerDropdownItemLi.Attributes["class"] = "dropdown-group";

                var settingsUl = new TagBuilder("ul");
                settingsUl.Attributes["class"] = "dropdown-menu";
                
                var options = setting?.Value?.Split([','], StringSplitOptions.RemoveEmptyEntries) ?? [];

                foreach (var option in options)
                {
                    settingsUl.InnerHtml.AppendHtml(CreateDropdownItem(option, hasContent, command, false, "ai-text-composer"));
                }

                providerDropdownItemLi.InnerHtml.AppendHtml(settingsUl);
                dropdownUl.InnerHtml.AppendHtml(providerDropdownItemLi);
            }
        }

        public TagBuilder GenerateSuggestionTool(AttributeDictionary attributes)
        {
            CheckContextualized();

            var providers = _aiProviderFactory.GetProviders(AIProviderFeatures.TextCreation);
            if (providers.Count == 0)
            {
                return null;
            }

            return GenerateOutput(providers, attributes, AIDialogType.Suggestion);
        }

        public TagBuilder GenerateImageCreationTool(AttributeDictionary attributes)
        {
            CheckContextualized();

            var providers = _aiProviderFactory.GetProviders(AIProviderFeatures.ImageCreation);
            if (providers.Count == 0)
            {
                return null;
            }

            return GenerateOutput(providers, attributes, AIDialogType.Image);
        }

        public TagBuilder GenerateRichTextTool(AttributeDictionary attributes)
        {
            CheckContextualized();

            var providers = _aiProviderFactory.GetProviders(AIProviderFeatures.TextCreation);
            if (providers.Count == 0)
            {
                return null;
            }

            return GenerateOutput(providers, attributes, AIDialogType.RichText);
        }

        /// <summary>
        /// Generates the output for the AI dialog openers.
        /// </summary>
        /// <param name="providers">List of providers to generate dropdown items for.</param>
        /// <param name="attributes">The attributes of the taghelper.</param>
        /// <param name="dialogType">The type of dialog to be opened <see cref="AIDialogType"/></param>
        /// <returns>
        /// A button (if there's only one provider) or a dropdown incl. menu items (if there are more then one provider) 
        /// containing all the metadata needed to open the dialog.
        /// </returns>
        private TagBuilder GenerateOutput(IReadOnlyList<Provider<IAIProvider>> providers, AttributeDictionary attributes, AIDialogType dialogType)
        {
            var additionalClasses = GetDialogIdentifierClass(dialogType);

            // If there is only one provider, render a simple button, render a dropdown otherwise.
            if (providers.Count == 1)
            {
                var provider = providers[0];
                var friendlyName = _moduleManager.GetLocalizedFriendlyName(provider.Metadata);
                var dropdownLiTitle = GetDialogOpenerText(dialogType, friendlyName);
                var openerDiv = CreateDialogOpener(false, additionalClasses, dropdownLiTitle);

                AddTagHelperProperties(openerDiv, provider, attributes, dialogType);

                return openerDiv;
            }
            else
            {
                var inputGroupColDiv = CreateDialogOpener(true);
                var dropdownUl = new TagBuilder("ul");
                dropdownUl.Attributes["class"] = "dropdown-menu";

                foreach (var provider in providers)
                {
                    var friendlyName = _moduleManager.GetLocalizedFriendlyName(provider.Metadata);
                    var dropdownLiTitle = GetDialogOpenerText(dialogType, friendlyName);
                    var dropdownLi = CreateDropdownItem(dropdownLiTitle, true, string.Empty, true, additionalClasses);

                    AddTagHelperProperties(dropdownLi, provider, attributes, dialogType);

                    dropdownUl.InnerHtml.AppendHtml(dropdownLi);
                }

                inputGroupColDiv.InnerHtml.AppendHtml(dropdownUl);

                return inputGroupColDiv;
            }
        }

        /// <summary>
        /// Adds the necessary data attributes to the given control.
        /// </summary>
        private void AddTagHelperProperties(TagBuilder ctrl, Provider<IAIProvider> provider, AttributeDictionary attributes, AIDialogType dialogType)
        {
            var route = provider.Value.GetDialogRoute(dialogType);
            ctrl.MergeAttribute("data-provider-systemname", provider.Metadata.SystemName);
            ctrl.MergeAttribute("data-modal-url", _urlHelper.Action(route.Action, route.Controller, route.RouteValues));
            ctrl.MergeAttributes(attributes);
        }

        /// <summary>
        /// Gets the class name used as the dialog identifier.
        /// </summary>
        private static string GetDialogIdentifierClass(AIDialogType dialogType)
        {
            switch (dialogType)
            {
                case AIDialogType.Text:
                case AIDialogType.RichText:
                    return "ai-text-composer";
                case AIDialogType.Image:
                    return "ai-image-composer";
                case AIDialogType.Translation:
                    return "ai-translator";
                case AIDialogType.Suggestion:
                    return "ai-suggestion";
                default:
                    throw new Exception("Unknown modal dialog type");
            }
        }

        /// <summary>
        /// Gets the title of a dropdown item that opens an AI dialog.
        /// </summary>
        private string GetDialogOpenerText(AIDialogType dialogType, string providerName)
        {
            switch (dialogType)
            {
                case AIDialogType.Text:
                case AIDialogType.RichText:
                    return T("Admin.AI.CreateTextWith", providerName);
                case AIDialogType.Image:
                    return T("Admin.AI.CreateImageWith", providerName);
                case AIDialogType.Translation:
                    return T("Admin.AI.TranslateTextWith", providerName);
                case AIDialogType.Suggestion:
                    return T("Admin.AI.MakeSuggestionWith", providerName);
                default:
                    throw new Exception("Unknown modal dialog type");
            }
        }

        /// <summary>
        /// Creates the element to open the dialog.
        /// </summary>
        /// <param name="isDropdown">Defines whether the opener is a dropdown.</param>
        /// <param name="additionalClasses">Additional CSS classes to add to the opener icon.</param>
        /// <param name="title">The title of the opener.</param>
        /// <returns>The dialog opener.</returns>
        private TagBuilder CreateDialogOpener(bool isDropdown, string additionalClasses = "", string title = "")
        {
            var inputGroupColDiv = new TagBuilder("div");
            inputGroupColDiv.Attributes["class"] = "has-icon has-icon-right ai-dialog-opener-root";
            inputGroupColDiv.AppendCssClass(isDropdown ? "dropdown" : "ai-provider-tool");

            var iconA = GenerateOpenerIcon(isDropdown, additionalClasses, title);
            inputGroupColDiv.InnerHtml.AppendHtml(iconA);

            return inputGroupColDiv;
        }

        /// <summary>
        /// Creates the icon button to open the dialog.
        /// </summary>
        /// <param name="isDropdown">Defines whether the opener is a dropdown.</param>
        /// <param name="additionalClasses">Additional CSS classes to add to the opener icon.</param>
        /// <param name="title">The title of the opener.</param>
        /// <returns>The dialog opener icon.</returns>
        private TagBuilder GenerateOpenerIcon(bool isDropdown, string additionalClasses = "", string title = "")
        {
            var icon = (TagBuilder)HtmlHelper.BootstrapIcon("magic", false, htmlAttributes: new Dictionary<string, object>
            {
                ["class"] = "dropdown-icon bi-fw bi"
            });

            var btnTag = new TagBuilder("a");
            btnTag.Attributes["href"] = "javascript:;";
            btnTag.Attributes["class"] = "btn btn-icon btn-flat btn-sm rounded-circle btn-outline-secondary input-group-icon ai-dialog-opener no-chevron";
            btnTag.AppendCssClass(isDropdown ? "dropdown-toggle" : additionalClasses);

            if (isDropdown)
            {
                btnTag.Attributes["data-toggle"] = "dropdown";
            }
            else
            {
                btnTag.Attributes["title"] = title;
            }

            btnTag.InnerHtml.AppendHtml(icon);

            return btnTag;
        }

        /// <summary>
        /// Creates a dropdown item.
        /// </summary>
        /// <param name="menuText">The text for the menu item.</param>
        /// <returns>A LI tag representing the menu item.</returns>
        private static TagBuilder CreateDropdownItem(string menuText)
            => CreateDropdownItem(menuText, true, string.Empty, false);

        /// <summary>
        /// Creates a dropdown item.
        /// </summary>
        /// <param name="menuText">The text for the menu item.</param>
        /// <param name="enabled">Defines whether the menu item is enabled.</param>
        /// <param name="command">The command of the menu item (needed for optimize commands for simple text creation)</param>
        /// <param name="isProviderTool">Defines whether the item is a provider tool container.</param>
        /// <param name="additionalClasses">Additional CSS classes to add to the menu item.</param>
        /// <returns>A LI tag representing the menu item.</returns>
        private static TagBuilder CreateDropdownItem(
            string menuText,
            bool enabled,
            string command,
            bool isProviderTool,
            params string[] additionalClasses)
        {
            var listItem = new TagBuilder("li");
            if (isProviderTool)
            {
                listItem.Attributes["class"] = "ai-provider-tool";
            }

            var dropdownItem = new TagBuilder("a");
            dropdownItem.Attributes["href"] = "#";
            dropdownItem.Attributes["class"] = "dropdown-item";

            if (!enabled)
            {
                dropdownItem.AppendCssClass("disabled");
            }

            additionalClasses.Each(dropdownItem.AppendCssClass);

            if (command.HasValue())
            {
                dropdownItem.Attributes["data-command"] = command;
            }

            dropdownItem.InnerHtml.AppendHtml(menuText);
            listItem.InnerHtml.AppendHtml(dropdownItem);

            return listItem;
        }

        /// <summary>
        /// Extracts labels and ids from the given html content which is the inner html of a localized editor.
        /// Only needed for translation tool to determine the properties to be translated.
        /// </summary>
        /// <param name="htmlContent">Inner html of a localized editor.</param>
        /// <returns>
        /// List<(string, string, bool)> 
        /// where the first string is the element id, the second the label of the form element and the bool indicates whether the element contains a value.
        /// </returns>
        private static async Task<List<(string, string, bool)>> ExtractLabelsAndIdsAsync(string htmlContent)
        {
            // TODO: (mh) (ai) VERY BAD decision to implement a scraper. TBD with MC.
            var context = BrowsingContext.New(Configuration.Default);
            var document = await context.OpenAsync(req => req.Content(htmlContent));
            var elements = document.QuerySelectorAll("input[id], textarea[id]").OfType<IHtmlElement>();

            var list = new List<(string, string, bool)>();

            foreach (var element in elements)
            {
                string value = null;

                if (element is IHtmlInputElement inputElement)
                {
                    value = inputElement.Value;
                }
                else if (element is IHtmlTextAreaElement textAreaElement)
                {
                    value = textAreaElement.Value;
                }

                var label = document.QuerySelector($"label[for='{element.Id}']")?.TextContent.Trim();
                if (label != null)
                {
                    list.Add((element.Id, label, value.HasValue()));
                }
            }

            return list;
        }

        private void CheckContextualized()
        {
            if (_viewContext == null)
            {
                throw new InvalidOperationException($"Call '{nameof(Contextualize)}' before calling any {nameof(IAIToolHtmlGenerator)} method.");
            }
        }
    }
}