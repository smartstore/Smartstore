using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Smartstore.Web.Modelling
{
    public class AdditionalMetadataProvider : IDisplayMetadataProvider
    {
        public AdditionalMetadataProvider() { }

        public void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            if (context.PropertyAttributes != null)
            {
                foreach (object propAttr in context.PropertyAttributes)
                {
                    var addMetaAttr = propAttr as AdditionalMetadataAttribute;
                    if (addMetaAttr != null)
                    {
                        context.DisplayMetadata.AdditionalValues.Add(addMetaAttr.Name, addMetaAttr.Value);
                    }
                }
            }
        }
    }
}
