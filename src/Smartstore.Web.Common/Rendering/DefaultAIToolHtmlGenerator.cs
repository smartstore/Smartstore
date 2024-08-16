using System.Reflection;
using Autofac;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Smartstore.ComponentModel;
using Smartstore.Core.Localization;
using Smartstore.Core.Platform.AI;
using Smartstore.Engine.Modularity;
using Smartstore.Web.Modelling;

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

        public TagBuilder GenerateTranslationTool(ILocalizedModel model)
        {
            Guard.NotNull(model);

            CheckContextualized();

            var providers = _aiProviderFactory.GetProviders(AIProviderFeatures.TextTranslation);
            if (providers.Count == 0)
            {
                return null;
            }

            // Model must implement ILocalizedModel<T> where T : ILocalizedLocaleModel
            var modelType = model.GetType();
            if (!modelType.IsClosedGenericTypeOf(typeof(ILocalizedModel<>)))
            {
                return null;
            }

            // Entity model must not be transient
            if (model is EntityModelBase entityModel && entityModel.EntityId == 0)
            {
                return null;
            }

            var localesProperty = modelType.GetProperty("Locales", BindingFlags.Public | BindingFlags.Instance);
            if (localesProperty == null || !localesProperty.PropertyType.IsEnumerableType(out var localeModelType))
            {
                return null;
            }

            var propertyNames = FastProperty.GetProperties(localeModelType)
                .Where(x => x.Value.Property.PropertyType == typeof(string))
                .Select(x => x.Key)
                .ToList();

            var propertyInfoMap = FastProperty.GetProperties(modelType)
                .Where(x => propertyNames.Contains(x.Key))
                .Select(x => x.Value.Property)
                .Select(x => new AILocalizedPropertyInfo
                {
                    Prop = x,
                    HasValue = x.GetValue(model)?.ToString()?.HasValue() == true
                })
                .ToDictionarySafe(x => x.Prop.Name);

            if (propertyInfoMap.Count == 0 || propertyInfoMap.Values.All(x => !x.HasValue))
            {
                // Nothing to translate.
                return null;
            }

            string[] additionalItemClasses = ["ai-translator"];
            var inputGroupColDiv = CreateDialogOpener(true);

            var dropdownUl = new TagBuilder("ul");
            dropdownUl.Attributes["class"] = "dropdown-menu ai-translator-menu";

            foreach (var provider in providers)
            {
                // Add attributes from tag helper properties.
                var route = provider.Value.GetDialogRoute(AIDialogType.Translation);
                var routeUrl = _urlHelper.Action(route.Action, route.Controller, route.RouteValues);

                var dropdownLiTitle = T("Admin.AI.TranslateTextWith", _moduleManager.GetLocalizedFriendlyName(provider.Metadata)).ToString();
                var headingLi = new TagBuilder("li");
                headingLi.Attributes["class"] = "dropdown-header h6";
                headingLi.InnerHtml.AppendHtml(dropdownLiTitle);
                dropdownUl.InnerHtml.AppendHtml(headingLi);

                // INFO: we often have several localized editors per ILocalizedLocaleModel (e.g. "blogpost-info-localized" and "blogpost-seo-localized")
                // but we only want to have the properties in the translator menu that can also be edited in the associated localized editor.
                // We do not have this information here. Solution: we put all properties in the menu and later remove those that are not included using JavaScript.
                foreach (var name in propertyNames)
                {
                    var id = HtmlHelper.Id(name);
                    var displayName = HtmlHelper.DisplayName(name);
                    var info = propertyInfoMap.Get(name) ?? new();
                    //$"- id:{id} displayName:{displayName} hasValue:{info.HasValue}".Dump();

                    var additionalClasses = info.HasValue ? additionalItemClasses : [.. additionalItemClasses, "disabled"];
                    var dropdownLi = CreateDropdownItem(displayName, true, string.Empty, true, additionalClasses);

                    var attrs = dropdownLi.Attributes;
                    attrs["data-provider-systemname"] = provider.Metadata.SystemName;
                    attrs["data-modal-url"] = routeUrl;
                    attrs["data-target-property"] = id;
                    attrs["data-modal-title"] = dropdownLiTitle + ": " + displayName;

                    // INFO: there is no explicit item order. To put the menu items in the same order as the HTML elements,
                    // the properties of the ILocalizedLocaleModel must be ordered accordingly.
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
                MergeDataAttributes(btn, provider, attributes, AIDialogType.Text);

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
            var setting = _db.Settings.FirstOrDefault(x => x.Name == settingName);
            if (setting != null && setting.Value.HasValue())
            {
                var title = T(command == "change-style" ? "Admin.AI.MenuItemTitle.ChangeStyle" : "Admin.AI.MenuItemTitle.ChangeTone");
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

                MergeDataAttributes(openerDiv, provider, attributes, dialogType);

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

                    MergeDataAttributes(dropdownLi, provider, attributes, dialogType);

                    dropdownUl.InnerHtml.AppendHtml(dropdownLi);
                }

                inputGroupColDiv.InnerHtml.AppendHtml(dropdownUl);

                return inputGroupColDiv;
            }
        }

        /// <summary>
        /// Adds the necessary data attributes to the given control.
        /// </summary>
        private void MergeDataAttributes(TagBuilder ctrl, Provider<IAIProvider> provider, AttributeDictionary attributes, AIDialogType dialogType)
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

        private void CheckContextualized()
        {
            if (_viewContext == null)
            {
                throw new InvalidOperationException($"Call '{nameof(Contextualize)}' before calling any {nameof(IAIToolHtmlGenerator)} method.");
            }
        }
    }

    internal class AILocalizedPropertyInfo
    {
        public PropertyInfo Prop { get; set; }
        public bool HasValue { get; set; }
    }
}