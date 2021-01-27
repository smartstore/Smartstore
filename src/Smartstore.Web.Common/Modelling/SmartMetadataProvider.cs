using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Smartstore.Web.Modelling
{
    /// <summary>
    /// This metadata provider adds custom attributes (implementing <see cref="IModelAttribute"/>) 
    /// to the AdditionalValues property of the model's metadata so that it can be retrieved later.
    /// </summary>
    public class SmartMetadataProvider : IDisplayMetadataProvider
    {
        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            string parentDisplayNameResKey = context.Key.MetadataKind == ModelMetadataKind.Property
                ? context.Key.ContainerType?.GetAttribute<LocalizedDisplayNameAttribute>(false)?.ResourceKey
                : null;

            var modelAttributes = context.Attributes.OfType<IModelAttribute>().ToArray();

            for (var i = 0; i < modelAttributes.Length; i++)
            {
                var attribute = modelAttributes[i];
                
                if (context.DisplayMetadata.AdditionalValues.ContainsKey(attribute.Name))
                {
                    throw new SmartException($"An attribute with the name '{attribute.Name}' is already defined on this model.");
                }

                if (parentDisplayNameResKey != null
                    && attribute is LocalizedDisplayNameAttribute displayNameAttr
                    && displayNameAttr.ResourceKey.Length > 0
                    && displayNameAttr.ResourceKey[0] == '*')
                {
                    // Combine ResKeys of parent (type) attribute and local (property) attribute
                    displayNameAttr.ResourceKey = parentDisplayNameResKey + displayNameAttr.ResourceKey[1..];
                }

                context.DisplayMetadata.AdditionalValues.Add(attribute.Name, attribute);
            }
        }
    }
}
