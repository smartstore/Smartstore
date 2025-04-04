using System.Reflection;
using Autofac;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Smartstore.ComponentModel;
using Smartstore.Core.AI;
using Smartstore.Core.Localization;
using Smartstore.Core.Seo;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Rendering
{
    public class DefaultAIToolHtmlGenerator : IAIToolHtmlGenerator
    {
        private readonly SmartDbContext _db;
        private readonly IAIProviderFactory _aiProviderFactory;
        private readonly IUrlHelper _urlHelper;
        private readonly IWorkContext _workContext;
        private IHtmlHelper _htmlHelper;
        private ViewContext _viewContext;

        public DefaultAIToolHtmlGenerator(
            SmartDbContext db,
            IAIProviderFactory aiProviderFactory,
            IUrlHelper urlHelper,
            IWorkContext workContext)
        {
            _db = db;
            _aiProviderFactory = aiProviderFactory;
            _urlHelper = urlHelper;
            _workContext = workContext;
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

        public virtual TagBuilder GenerateTranslationTool(ILocalizedModel model, string localizedEditorName)
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

            var entityId = 0;
            // Entity model must not be transient
            if (model is EntityModelBase entityModel && (entityId = entityModel.EntityId) == 0)
            {
                return null;
            }

            if (entityId == 0)
            {
                // We are in the context of an entity which is not EntityModelBase and therefore has no ID, e.g. PageBuilder.
                // Lets look in the route data for an ID.
                _viewContext.RouteData.Values.TryGetAndConvertValue("id", out entityId);
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
            var dialogUrl = GetDialogUrl(AIChatTopic.Translation);
            var inputGroupColDiv = CreateDialogOpener(true, title: GetOpenerTitle(AIChatTopic.Translation));

            var dropdownUl = new TagBuilder("ul");
            dropdownUl.Attributes["class"] = "dropdown-menu dropdown-menu-right ai-translator-menu";

            var entityType = model.GetEntityType();
            var entityTypeName = entityType == null ? string.Empty : NamedEntity.GetEntityName(entityType);

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
                var dropdownLi = CreateDropdownItem(displayName, true, string.Empty, null, true, additionalClasses);

                var attrs = dropdownLi.Attributes;
                attrs["data-modal-url"] = dialogUrl;
                attrs["data-modal-title"] = displayName;
                attrs["data-target-property"] = id;
                // For the translation dialog we use a different approach of determining the target field.
                // The target property name is used to find the correct property in the localized editor.
                attrs["data-target-property-name"] = name;
                attrs["data-localized-editor-name"] = localizedEditorName;

                if (entityTypeName.HasValue())
                {
                    attrs["data-entity-id"] = entityId.ToString();
                    attrs["data-entity-type"] = entityTypeName;
                }

                // INFO: there is no explicit item order. To put the menu items in the same order as the HTML elements,
                // the properties of the ILocalizedLocaleModel must be ordered accordingly.
                dropdownUl.InnerHtml.AppendHtml(dropdownLi);
            }

            inputGroupColDiv.InnerHtml.AppendHtml(dropdownUl);

            return inputGroupColDiv;
        }

        public virtual TagBuilder GenerateTextCreationTool(AttributeDictionary attributes, bool enabled = true)
            => GenerateTextToolOutput(attributes, AIChatTopic.Text, enabled);

        public virtual TagBuilder GenerateRichTextTool(AttributeDictionary attributes, bool enabled = true)
            => GenerateTextToolOutput(attributes, AIChatTopic.RichText, enabled);

        protected virtual TagBuilder GenerateTextToolOutput(AttributeDictionary attributes, AIChatTopic topic, bool enabled = true)
        {
            CheckContextualized();

            var providers = _aiProviderFactory.GetProviders(AIProviderFeatures.TextCreation);
            if (providers.Count == 0)
            {
                return null;
            }

            var inputGroupColDiv = CreateDialogOpener(true, title: GetOpenerTitle(AIChatTopic.Text));
            inputGroupColDiv.Attributes["data-modal-url"] = GetDialogUrl(topic);
            inputGroupColDiv.MergeAttributes(attributes);

            var dropdownUl = new TagBuilder("ul");
            dropdownUl.Attributes["class"] = "dropdown-menu dropdown-menu-right";

            dropdownUl.InnerHtml.AppendHtml(GenerateOptimizeCommands(false, enabled));
            inputGroupColDiv.InnerHtml.AppendHtml(dropdownUl);

            return inputGroupColDiv;
        }

        public virtual IHtmlContent GenerateOptimizeCommands(bool forChatDialog, bool enabled = true, bool forHtmlEditor = false)
        {
            var builder = new HtmlContentBuilder();
            var className = forChatDialog ? "ai-text-optimizer" : "ai-text-composer";
            var resRoot = "Admin.AI.TextCreation.";

            builder.AppendHtml(CreateDropdownItem(T($"{resRoot}CreateNew"), true, "create-new", "repeat", false, className));
            builder.AppendHtml("<li class=\"dropdown-divider\"></li>");

            // Add "Change style" & "Change tone" options from module settings.
            var styleDropdown = AddMenuItemsFromSetting(enabled, "change-style", className, "vector-pen");
            var toneDropdown = AddMenuItemsFromSetting(enabled, "change-tone", className, "emoji-wink");

            if (styleDropdown != null || toneDropdown != null)
            {
                builder.AppendHtml(styleDropdown);
                builder.AppendHtml(toneDropdown);
                builder.AppendHtml("<li class=\"dropdown-divider\"></li>");
            }

            builder.AppendHtml(CreateDropdownItem(T($"{resRoot}Summarize"), enabled, "summarize", "highlighter", false, className));
            builder.AppendHtml(CreateDropdownItem(T($"{resRoot}Improve"), enabled, "improve", "lightbulb", false, className));
            builder.AppendHtml(CreateDropdownItem(T($"{resRoot}Simplify"), enabled, "simplify", "text-left", false, className));
            builder.AppendHtml(CreateDropdownItem(T($"{resRoot}Extend"), enabled, "extend", "body-text", false, className));

            if (forChatDialog)
            {
                className += " d-none";
            }

            if (forHtmlEditor)
            {
                builder.AppendHtml(CreateDropdownItem(T($"{resRoot}Continue"), enabled, "continue", "three-dots", false, className));
            }
            
            return builder;
        }

        /// <summary>
        /// Creates a sub-dropdown for text creation styles an tones.
        /// </summary>
        /// <param name="command">The command type choosen by the user. It can be "change-style" or "change-tone".</param>
        private TagBuilder AddMenuItemsFromSetting(bool enabled, string command, string additionalClasses, string iconName = null)
        {
            const string keyGroup = "AISettings";
            var settingName = command == "change-style" ? "TextCreationStyles" : "TextCreationTones";

            // INFO: These settings are not store-dependent (storeId is always 0).
            var settingValue = _db.LocalizedProperties
                .Where(x => x.LocaleKey == settingName && x.LocaleKeyGroup == keyGroup && x.LanguageId == _workContext.WorkingLanguage.Id)
                .Select(x => x.LocaleValue)
                .FirstOrDefault();

            settingValue ??= _db.Settings
                .Where(x => x.Name == keyGroup + '.' + settingName)
                .Select(x => x.Value)
                .FirstOrDefault();

            var options = settingValue.SplitSafe(',').ToArray();
            if (options.IsNullOrEmpty())
            {
                return null;
            }

            var optionsList = new TagBuilder("ul");
            optionsList.Attributes["class"] = "dropdown-menu dropdown-menu-slide dropdown-menu-right";

            foreach (var option in options)
            {
                optionsList.InnerHtml.AppendHtml(CreateDropdownItem(option, enabled, command, null, false, additionalClasses));
            }

            var subDropdown = CreateDropdownItem(
                T(command == "change-style" ? "Admin.AI.MenuItemTitle.ChangeStyle" : "Admin.AI.MenuItemTitle.ChangeTone"),
                true,
                string.Empty,
                iconName,
                false);
            subDropdown.Attributes["class"] = "dropdown-group";
            subDropdown.InnerHtml.AppendHtml(optionsList);

            return subDropdown;
        }

        public virtual TagBuilder GenerateSuggestionTool(AttributeDictionary attributes)
            => GenerateOutput(attributes, AIProviderFeatures.TextCreation, AIChatTopic.Suggestion);

        public virtual TagBuilder GenerateImageCreationTool(AttributeDictionary attributes)
            => GenerateOutput(attributes, AIProviderFeatures.ImageCreation, AIChatTopic.Image);

        /// <summary>
        /// Generates the output for the AI dialog openers.
        /// </summary>
        /// <param name="attributes">The attributes of the TagHelper.</param>
        /// <param name="feature">The <see cref="AIProviderFeatures"/> to be supported for the AI tool.</param>
        /// <param name="topic">The <see cref="AIChatTopic"/> of the AI tool.</param>
        /// <returns>
        /// The TagBuilder for the AI dialog opener.
        /// </returns>
        protected virtual TagBuilder GenerateOutput(AttributeDictionary attributes, AIProviderFeatures feature, AIChatTopic topic)
        {
            CheckContextualized();

            var providers = _aiProviderFactory.GetProviders(feature);
            if (providers.Count == 0)
            {
                return null;
            }

            var openerDiv = CreateDialogOpener(false, GetDialogIdentifierClass(topic), GetOpenerTitle(topic));
            openerDiv.Attributes["data-modal-url"] = GetDialogUrl(topic);
            openerDiv.MergeAttributes(attributes);

            return openerDiv;
        }

        /// <summary>
        /// Creates the element to open the dialog.
        /// </summary>
        /// <param name="isDropdown">Defines whether the opener is a dropdown.</param>
        /// <param name="additionalClasses">Additional CSS classes to add to the opener icon.</param>
        /// <param name="title">The title of the opener.</param>
        /// <returns>The dialog opener.</returns>
        protected virtual TagBuilder CreateDialogOpener(bool isDropdown, string additionalClasses = "", string title = "")
        {
            var inputGroupColDiv = new TagBuilder("div");
            inputGroupColDiv.Attributes["class"] = "has-icon has-icon-right ai-dialog-opener-root";
            inputGroupColDiv.AppendCssClass("ai-provider-tool");

            if (isDropdown)
            {
                inputGroupColDiv.AppendCssClass("dropdown");
            }

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
        protected virtual TagBuilder GenerateOpenerIcon(bool isDropdown, string additionalClasses = "", string title = "")
        {
            var icon = (TagBuilder)HtmlHelper.BootstrapIcon("stars-tricolor", htmlAttributes: new Dictionary<string, object>
            {
                ["class"] = "dropdown-icon ai-icon-stars-tricolor bi-fw bi"
            });

            var btnTag = new TagBuilder("a");
            btnTag.Attributes["href"] = "javascript:;";
            btnTag.Attributes["class"] = "btn btn-clear-dark btn-no-border btn-sm btn-icon rounded-circle input-group-icon ai-dialog-opener no-chevron tooltip-toggle";
            btnTag.Attributes["data-original-title"] = title;
            btnTag.AppendCssClass(isDropdown ? "dropdown-toggle" : additionalClasses);

            if (isDropdown)
            {
                btnTag.Attributes["data-toggle"] = "dropdown";
            }

            btnTag.InnerHtml.AppendHtml(icon);

            return btnTag;
        }

        /// <inheritdoc/>
        public virtual string GetDialogUrl(AIChatTopic topic)
        {
            var action = topic switch
            {
                AIChatTopic.Image => "Image",
                AIChatTopic.Text => "Text",
                AIChatTopic.RichText => "RichText",
                AIChatTopic.Translation => "Translation",
                AIChatTopic.Suggestion => "Suggestion",
                _ => throw new AIException($"Unknown chat topic {topic}.")
            };

            return _urlHelper.Action(action, "AI", new { area = "Admin" });
        }

        /// <summary>
        /// Gets the class name used as the dialog identifier.
        /// </summary>
        private static string GetDialogIdentifierClass(AIChatTopic topic)
        {
            switch (topic)
            {
                case AIChatTopic.Text:
                case AIChatTopic.RichText:
                    return "ai-text-composer";
                case AIChatTopic.Image:
                    return "ai-image-composer";
                case AIChatTopic.Translation:
                    return "ai-translator";
                case AIChatTopic.Suggestion:
                    return "ai-suggestion";
                default:
                    throw new AIException($"Unknown chat topic {topic}.");
            }
        }

        /// <summary>
        /// Gets the title of the dialog opener element.
        /// </summary>
        private string GetOpenerTitle(AIChatTopic topic)
        {
            switch (topic)
            {
                case AIChatTopic.Text:
                case AIChatTopic.RichText:
                    return T("Admin.AI.ToolTitle.CreateText");
                case AIChatTopic.Translation:
                    return T("Admin.AI.ToolTitle.TranslateText");
                case AIChatTopic.Image:
                    return T("Admin.AI.ToolTitle.CreateImage");
                case AIChatTopic.Suggestion:
                    return T("Admin.AI.ToolTitle.MakeSuggestions");
                default:
                    throw new AIException($"Unknown chat topic {topic}.");
            }
        }

        /// <summary>
        /// Creates a dropdown item.
        /// </summary>
        /// <param name="menuText">The text for the menu item.</param>
        /// <returns>A LI tag representing the menu item.</returns>
        protected virtual TagBuilder CreateDropdownItem(string menuText)
            => CreateDropdownItem(menuText, true, string.Empty, null, false);

        /// <summary>
        /// Creates a dropdown item.
        /// </summary>
        /// <param name="menuText">The text for the menu item.</param>
        /// <param name="enabled">Defines whether the menu item is enabled.</param>
        /// <param name="command">The command of the menu item (needed for optimize commands for simple text creation)</param>
        /// <param name="iconName">The optional name of a Bootstrap SVG icon. Can be null.</param>
        /// <param name="isProviderTool">Defines whether the item is a provider tool container.</param>
        /// <param name="additionalClasses">Additional CSS classes to add to the menu item.</param>
        /// <returns>An LI tag representing the menu item.</returns>
        protected virtual TagBuilder CreateDropdownItem(
            string menuText,
            bool enabled,
            string command,
            string iconName,
            bool isProviderTool,
            params string[] additionalClasses)
        {
            var li = new TagBuilder("li");
            if (isProviderTool)
            {
                li.Attributes["class"] = "ai-provider-tool";
            }

            var a = new TagBuilder("a");
            a.Attributes["href"] = "#";
            a.Attributes["class"] = "dropdown-item";

            if (!enabled)
            {
                a.AppendCssClass("disabled");
            }

            additionalClasses.Each(x => a.AppendCssClass(x));

            if (command.HasValue())
            {
                a.Attributes["data-command"] = command;
            }

            if (iconName.HasValue())
            {
                var svg = HtmlHelper.BootstrapIcon(iconName, htmlAttributes: new { @class = "bi-fw" });
                a.InnerHtml.AppendHtml(svg);
            }

            a.InnerHtml.AppendHtml(menuText);

            li.InnerHtml.AppendHtml(a);

            return li;
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