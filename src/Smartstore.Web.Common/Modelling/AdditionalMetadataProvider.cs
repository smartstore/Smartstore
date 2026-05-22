using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Smartstore.Web.Modelling;

public class AdditionalMetadataProvider : IDisplayMetadataProvider
{
    public AdditionalMetadataProvider()
    {
    }

    public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
    {
        if (context.PropertyAttributes == null)
        {
            return;
        }

        foreach (var attr in context.PropertyAttributes.OfType<AdditionalMetadataAttribute>())
        {
            context.DisplayMetadata.AdditionalValues[attr.Name] = attr.Value;
        }

        // Fallback: populate min/max from RangeAttribute if not already set explicitly.
        var additionalValues = context.DisplayMetadata.AdditionalValues;
        var rangeAttr = context.PropertyAttributes.OfType<RangeAttribute>().FirstOrDefault();
        if (rangeAttr != null)
        {
            if (!additionalValues.ContainsKey("min"))
            {
                additionalValues["min"] = rangeAttr.Minimum;
            }

            if (!additionalValues.ContainsKey("max"))
            {
                additionalValues["max"] = rangeAttr.Maximum;
            }
        }
    }
}