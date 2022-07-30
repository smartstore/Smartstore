using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Smartstore.Core.Localization;

namespace Smartstore.Web.Modelling
{
    /// <summary>
    /// This metadata provider adds custom resolvers for <see cref="DisplayMetadata.DisplayName"/>
    /// and <see cref="DisplayMetadata.Description"/> that load translations from <see cref="ILocalizationService"/>.
    /// Resource keys are applied with <see cref="LocalizedDisplayAttribute"/> on property or type level.
    /// </summary>
    public class SmartDisplayMetadataProvider : IDisplayMetadataProvider
    {
        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            // We are interested in properties only
            if (context.Key.MetadataKind != ModelMetadataKind.Property)
            {
                return;
            }

            var displayAttribute = context.Attributes.OfType<LocalizedDisplayAttribute>().FirstOrDefault();
            if (displayAttribute == null)
            {
                // No attribute defined on property level. Get out.
                return;
            }

            // Collect resource keys from parent (type) level and property level
            var parentDisplayAttribute = context.Key.ContainerType?.GetAttribute<LocalizedDisplayAttribute>(false);
            var parentNameKey = parentDisplayAttribute?.Name;
            var nameKey = displayAttribute.Name;

            var metadata = context.DisplayMetadata;

            // This will be our fallback for display names.
            var prettyName = context.Key.Name.SplitPascalCase();

            // Handle DisplayName, but only if null. It is not null when "DisplayNameAttribute" or "DisplayAttribute.Name" is defined on property.
            if (metadata.DisplayName == null)
            {
                var nameResKey = CombineResourceKeys(parentNameKey, nameKey);
                if (nameResKey.HasValue())
                {
                    metadata.DisplayName = () => GetResource(nameResKey, true, prettyName);
                }
                else
                {
                    metadata.DisplayName = () => prettyName;
                }
            }

            // Handle Description, but only if null. It is not null when "DescriptionAttribute" or "DisplayAttribute.Description" is defined on property.
            if (metadata.Description == null)
            {
                var parentDescriptionKey = parentDisplayAttribute?.Description;
                var descriptionKey = displayAttribute.Description;
                var descriptionResKey = CombineResourceKeys(
                    parentDescriptionKey ?? parentNameKey,
                    descriptionKey ?? (string.IsNullOrEmpty(nameKey) ? null : nameKey + ".Hint"));

                if (descriptionResKey.HasValue())
                {
                    metadata.Description = () => GetResource(descriptionResKey, false);
                }
            }

            // Handle Placeholder, but only if null. It is not null when "DisplayAttribute.Prompt" is defined on property.
            if (metadata.Placeholder == null)
            {
                var parentPromptKey = parentDisplayAttribute?.Prompt;
                var promptKey = displayAttribute.Prompt;
                var promptResKey = CombineResourceKeys(parentPromptKey ?? parentNameKey, promptKey);
                if (promptResKey.HasValue())
                {
                    metadata.Placeholder = () => GetResource(promptResKey, false);
                }
            }
        }

        private static string GetResource(string resKey, bool logIfNotFound, string defaultValue = null)
        {
            var value = EngineContext.Current.ResolveService<ILocalizationService>().GetResource(
                resKey,
                EngineContext.Current.ResolveService<IWorkContext>().WorkingLanguage.Id,
                logIfNotFound: logIfNotFound,
                returnEmptyIfNotFound: true);

            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            return value;
        }

        /// <summary>
        /// Combines parent type key with local property key. E.g.: Admin.Common. + *.Id = Admin.Common.Id
        /// </summary>
        private static string CombineResourceKeys(string parentKey, string key)
        {
            if (parentKey == null)
            {
                return key;
            }

            if (!string.IsNullOrEmpty(key) && key[0] == '*')
            {
                // Combine ResKeys of parent (type) attribute and local (property) attribute
                return parentKey + key[1..];
            }

            return key;
        }
    }
}
