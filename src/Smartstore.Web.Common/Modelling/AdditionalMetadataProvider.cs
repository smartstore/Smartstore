using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Smartstore.Web.Modelling
{
    public class AdditionalMetadataProvider : IDisplayMetadataProvider
    {
        public AdditionalMetadataProvider()
        {
        }

        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            if (context.PropertyAttributes != null)
            {
                foreach (var attr in context.PropertyAttributes.OfType<AdditionalMetadataAttribute>())
                {
                    context.DisplayMetadata.AdditionalValues[attr.Name] = attr.Value;
                }
            }
        }
    }
}
